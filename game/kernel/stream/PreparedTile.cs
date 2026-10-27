using System.Buffers;
using System.Runtime.InteropServices;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Diagnostics;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Stream;

/// <summary>Owns its meshes, multimeshes and instances. Scenario and material RIDs remain borrowed.</summary>
public sealed class PreparedTile
{
    private static readonly StringName _shaderInstanceFadeT0 = InstanceShaderParameters.FadeT0;
    // Diagnostic index of existing owners; release remains entirely with each PreparedTile.
    private static readonly object _liveGate = new();
    private static readonly HashSet<PreparedTile> _live = [];

    public sealed record Draw(Rid Base, Rid Mesh, Transform3D At, ToonRef Material,
        bool IgnoreOcclusionCulling, ShaderId? Shader, bool Reflect, bool Resident = false, bool NoShadow = false)
    {
        public Rid Base { get; internal set; } = Base;
        internal BakedMultiMesh? PendingMultiMesh { get; set; }
        public bool IsWater => Shader is ShaderId.Water;
        public bool Reflects => Reflect && !IsWater;
    }
    private readonly List<(Rid Rid, bool Multi)> _storage = [];
    private readonly List<Rid> _instances = [];
    private readonly List<Rid> _reflectionInstances = [];
    private readonly List<bool> _mainVisible = [];
    private readonly List<bool> _mainDetached = [], _reflectionDetached = [];
    private readonly List<Draw> _draws = [];
    private bool _initializationStarted;
    private int _multiStep;
    private bool _disposed;
    private bool _retiring;
    private int _detachCursor = -1;
    private bool _hardVisible;
    public BakeKey Key { get; }
    public IReadOnlyList<Draw> Draws => _draws;
    public IReadOnlyList<Rid> Instances => _instances;
    public bool IsDisposed => _disposed;
    public bool IsRetiring => _retiring;
    public int VisibleInstanceCount => _mainVisible.Count(value => value);
    public int ToonInstanceCount => _draws.Take(_instances.Count).Count(draw => draw.Shader is null);
    public int ReflectionInstanceCount => _reflectionInstances.Count(rid => rid.IsValid);
    public long Bytes { get; }
    public PreparedTile(TileBundle bundle) { Key = bundle.Key; Bytes = bundle.Stats.Bytes; }

    /// <summary>Main-thread diagnostic snapshot, including hidden and retiring native instances.</summary>
    public static (Rid[] Instances, int Detached) CaptureParameterInstances()
    {
        lock (_liveGate)
        {
            var instances = new List<Rid>();
            var detached = 0;
            foreach (var tile in _live)
                for (var index = 0; index < tile._instances.Count; index++)
                {
                    instances.Add(tile._instances[index]);
                    if (tile._mainDetached[index]) detached++;
                    if (!tile._reflectionInstances[index].IsValid) continue;
                    instances.Add(tile._reflectionInstances[index]);
                    if (tile._reflectionDetached[index]) detached++;
                }
            return (instances.ToArray(), detached);
        }
    }

    /// <summary>The caller retains this owner before initialization can fault, including cleanup failure.</summary>
    public void Initialize(TileBundle bundle)
    {
        if (_initializationStarted || _retiring || _disposed) throw new InvalidOperationException("Prepared tile initialization starts once.");
        _initializationStarted = true;
        if (bundle.Key != Key) throw new InvalidOperationException("Prepared tile input does not match its owner.");
        // Reserve ownership before native creation, including the later main-thread MultiMeshes.
        _storage.EnsureCapacity(bundle.Surfaces.Count + bundle.MultiMeshes.Count * 2);
        _draws.EnsureCapacity(bundle.Surfaces.Count + bundle.MultiMeshes.Count);
        foreach (var surface in bundle.Surfaces)
        {
            if (surface.VertexCount == 0) continue;
            var mesh = CreateMesh(surface);
            _draws.Add(new(mesh, mesh, surface.At.ToGodot(), surface.Material,
                surface.IgnoreOcclusionCulling, surface.Shader, surface.Reflect,
                bundle.Level == LodLevel.L4 && surface.Shader == ShaderId.DioramaFar, surface.NoShadow));
        }
        foreach (var item in bundle.MultiMeshes)
        {
            if (item.InstanceCount == 0 || item.Surface.VertexCount == 0) continue;
            if (!BitConverter.IsLittleEndian) throw new PlatformNotSupportedException("Packed instance buffers require a little-endian engine.");
            var mesh = CreateMesh(item.Surface);
            _draws.Add(new(default, mesh, Transform3D.Identity, item.Surface.Material,
                item.Surface.IgnoreOcclusionCulling, item.Surface.Shader, item.Surface.Reflect,
                bundle.Level == LodLevel.L4 && item.Surface.Shader == ShaderId.DioramaFar, item.Surface.NoShadow)
            { PendingMultiMesh = item });
        }
        lock (_liveGate) _live.Add(this);
    }
    /// <summary>Advances one render-server operation in the existing mount budget before any instances mount.</summary>
    internal bool InitializeDrawOne(int index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_retiring) throw new InvalidOperationException("A retiring tile cannot initialize native storage.");
        var draw = _draws[index];
        if (draw.PendingMultiMesh is not { } item) return true;
        // Safe rendering defers worker MultiMesh calls to the next main-thread server call. Keep all
        // of them here, with a budget check between operations and ownership retained before each call.
        switch (_multiStep)
        {
            case 0:
                var multi = RenderingServer.MultimeshCreate();
                if (!multi.IsValid) throw new InvalidOperationException("MultiMesh creation failed.");
                _storage.Add((multi, true));
                draw.Base = multi;
                break;
            case 1:
                RenderingServer.MultimeshAllocateData(draw.Base, item.InstanceCount,
                    RenderingServer.MultimeshTransformFormat.Transform3D, false, true);
                break;
            case 2:
                RenderingServer.MultimeshSetMesh(draw.Base, draw.Mesh);
                break;
            case 3:
                RenderingServer.MultimeshSetBuffer(draw.Base, MemoryMarshal.Cast<byte, float>(item.InstanceData.Span));
                break;
            case 4:
                RenderingServer.MultimeshSetCustomAabb(draw.Base, Bounds(item.Bounds));
                draw.PendingMultiMesh = null;
                _multiStep = 0;
                return false;
            default: throw new InvalidOperationException("Unknown MultiMesh initialization step.");
        }
        _multiStep++;
        return false;
    }
    private Rid CreateMesh(BakedSurface surface)
    {
        PackedSurfaceValidation.Validate(surface);
        using var data = new Godot.Collections.Dictionary
        {
            ["primitive"] = (int)(surface.Primitive switch
            {
                BakedPrimitive.Points => RenderingServer.PrimitiveType.Points,
                BakedPrimitive.Lines => RenderingServer.PrimitiveType.Lines,
                BakedPrimitive.LineStrip => RenderingServer.PrimitiveType.LineStrip,
                BakedPrimitive.Triangles => RenderingServer.PrimitiveType.Triangles,
                BakedPrimitive.TriangleStrip => RenderingServer.PrimitiveType.TriangleStrip,
                _ => throw new InvalidDataException("Unknown baked primitive.")
            }),
            ["format"] = unchecked((long)surface.Format),
            ["vertex_count"] = surface.VertexCount,
            ["aabb"] = Bounds(surface.Bounds),
            ["index_count"] = surface.IndexCount,
            ["uv_scale"] = new Vector4(surface.UvScale.X, surface.UvScale.Y, surface.UvScale.Z, surface.UvScale.W)
        };
        SetPackedData(data, "vertex_data", surface.VertexData);
        SetPackedData(data, "attribute_data", surface.AttributeData);
        SetPackedData(data, "skin_data", surface.SkinData);
        SetPackedData(data, "index_data", surface.IndexData);
        using var lods = new Godot.Collections.Array();
        foreach (var lod in surface.Lods)
        {
            using var dictionary = new Godot.Collections.Dictionary { ["edge_length"] = lod.Error };
            SetPackedData(dictionary, "index_data", lod.IndexData);
            using var value = Variant.CreateFrom(dictionary);
            lods.Add(value);
        }
        using var lodsValue = Variant.CreateFrom(lods);
        data["lods"] = lodsValue;
        using var surfacesOwner = new Godot.Collections.Array();
        var surfaces = new Godot.Collections.Array<Godot.Collections.Dictionary>(surfacesOwner);
        surfaces.Add(data);
        var mesh = RenderingServer.MeshCreateFromSurfaces(surfaces);
        if (!mesh.IsValid) throw new InvalidDataException($"GPU mesh creation failed for {Key}.");
        _storage.Add((mesh, false)); return mesh;
    }
    private static void SetPackedData(Godot.Collections.Dictionary target, string key, ReadOnlyMemory<byte> source)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(source.Length);
        try
        {
            source.Span.CopyTo(buffer);
            // Godot 4.7.2 copies this exact span into native PackedByteArray storage synchronously.
            using var value = Variant.CreateFrom(buffer.AsSpan(0, source.Length));
            using var nativeKey = Variant.CreateFrom(key);
            target[nativeKey] = value;
        }
        finally { ArrayPool<byte>.Shared.Return(buffer); }
    }
    public Rid MountOne(int index, Rid scenario, Rid material, Rid reflectionScenario = default,
        Material? reflectionMaterial = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_retiring) throw new InvalidOperationException("A retiring tile cannot accept new native instances.");
        var draw = _draws[index];
        if (draw.PendingMultiMesh is not null) throw new InvalidOperationException("MultiMesh storage must finish initializing before mounting.");
        // MultiMesh references a prototype mesh; all material assignment remains on the main thread.
        RenderingServer.MeshSurfaceSetMaterial(draw.Mesh, 0, material);
        var instance = RenderingServer.InstanceCreate2(draw.Base, scenario);
        if (!instance.IsValid) throw new InvalidOperationException($"Instance creation failed for {Key}.");

        _instances.Add(instance);

        _reflectionInstances.Add(default);
        _mainVisible.Add(draw.Resident);
        _mainDetached.Add(false); _reflectionDetached.Add(false);
        // LOD selection may go directly to ready children. The resident backdrop belongs to the
        // map lifecycle, so activate it on mount independently of LOD visibility transitions.
        RenderingServer.InstanceSetVisible(instance, draw.Resident);
        RenderingServer.InstanceSetTransform(instance, draw.At);
        var water = draw.IsWater;
        RenderingServer.InstanceSetLayerMask(instance, water ? RenderLayers.Water : RenderLayers.BuildingsAndProps);
        RenderingServer.InstanceGeometrySetCastShadowsSetting(instance, water || draw.Resident || draw.NoShadow
            ? RenderingServer.ShadowCastingSetting.Off : RenderingServer.ShadowCastingSetting.On);
        RenderingServer.InstanceGeometrySetVisibilityRange(instance, 0, 0, 0, 0, RenderingServer.VisibilityRangeFadeMode.Disabled);
        RenderingServer.InstanceGeometrySetFlag(instance, RenderingServer.InstanceFlags.IgnoreOcclusionCulling, draw.IgnoreOcclusionCulling);
        RefreshReflectionOne(index, reflectionScenario, reflectionMaterial);
        return instance;
    }
    public void RefreshReflectionOne(int index, Rid scenario, Material? material)
    {
        if (_disposed || _retiring) return;
        UpdateReflection(index, scenario, material);
    }
    private void UpdateReflection(int index, Rid scenario, Material? material)
    {
        var draw = _draws[index];
        var proxy = _reflectionInstances[index];
        if (!draw.Reflects || !scenario.IsValid || material is null)
        {
            if (proxy.IsValid) { DetachInstance(proxy); FreeOwnedInstance(proxy); }
            _reflectionInstances[index] = default; return;
        }
        if (!proxy.IsValid)
        {
            proxy = RenderingServer.InstanceCreate2(draw.Base, scenario);
            if (!proxy.IsValid) throw new InvalidOperationException($"Reflection instance creation failed for {Key}.");

            _reflectionInstances[index] = proxy;
            _reflectionDetached[index] = false;

            RenderingServer.InstanceSetTransform(proxy, draw.At);
            RenderingServer.InstanceGeometrySetCastShadowsSetting(proxy, RenderingServer.ShadowCastingSetting.Off);
            RenderingServer.InstanceGeometrySetVisibilityRange(proxy, 0, 0, 0, 0, RenderingServer.VisibilityRangeFadeMode.Disabled);
        }
        else RenderingServer.InstanceSetScenario(proxy, scenario);
        // A proxy override is required: assigning this to the shared mesh would replace the main-world material.
        RenderingServer.InstanceGeometrySetMaterialOverride(proxy, material.GetRid());
        RenderingServer.InstanceSetVisible(proxy, _hardVisible);
    }
    public void SetVisible(bool visible, float fadeStart)
    {
        if (_disposed || _retiring) return;
        _hardVisible = visible && fadeStart >= 0;
        for (var i = 0; i < _instances.Count; i++)
        {
            // Resident Diorama is shown at mount and stays visible until the owner detaches.
            if (_draws[i].Resident) continue;
            var instance = _instances[i];
            if (_draws[i].Shader is null)
            {
                RenderingServer.InstanceGeometrySetShaderParameter(instance, _shaderInstanceFadeT0, fadeStart);
                RenderingServer.InstanceSetVisible(instance, visible);
                _mainVisible[i] = visible;
            }
            else
            {
                var shown = _hardVisible || _draws[i].Resident;
                RenderingServer.InstanceSetVisible(instance, shown); _mainVisible[i] = shown;
            }
        }
        // Current water/reflect_flat shaders do not declare fade_t0. Replace them atomically, without overlap.
        for (var i = 0; i < _reflectionInstances.Count; i++)
            if (_reflectionInstances[i].IsValid)
                RenderingServer.InstanceSetVisible(_reflectionInstances[i], _hardVisible);
    }
    private static Aabb Bounds(Box3 box) => new(box.Min.ToGodot(), box.Size.ToGodot());
    public void BeginRetirement()
    {
        if (_retiring || _disposed) return;
        _retiring = true; _detachCursor = _instances.Count - 1;
    }
    /// <summary>Detaches one native instance without freeing it. No dirty material allocation is needed for retired geometry.</summary>
    public bool DetachOne()
    {
        BeginRetirement();
        if (_disposed || _detachCursor < 0) return true;
        var index = _detachCursor;
        if (_reflectionInstances[index].IsValid && !_reflectionDetached[index])
        {
            DetachInstance(_reflectionInstances[index]); _reflectionDetached[index] = true; return false;
        }
        if (_instances[index].IsValid && !_mainDetached[index])
        {
            DetachInstance(_instances[index]); _mainDetached[index] = true; _mainVisible[index] = false;
        }
        _detachCursor--; return false;
    }
    private static void DetachInstance(Rid instance)
    {

        // FreeRid(instance) flushes ALL dirty instances first. Drop base/scenario before that flush so a
        // doomed instance cannot allocate an instance-uniform block merely as a side effect of destruction.
        RenderingServer.InstanceSetVisible(instance, false);
        RenderingServer.InstanceSetScenario(instance, default);
        RenderingServer.InstanceSetBase(instance, default);
    }
    private static void FreeOwnedInstance(Rid instance)
    {

        RenderingServer.FreeRid(instance);

    }
    /// <summary>Releases at most one owned RID. Ordinary eviction calls this inside the 900 budget.</summary>
    public bool ReleaseOne()
    {
        if (_disposed) return true;
        if (!DetachOne()) return false;
        if (_instances.Count > 0)
        {
            var index = _instances.Count - 1;
            if (_reflectionInstances[index].IsValid)
            {
                FreeOwnedInstance(_reflectionInstances[index]);
                _reflectionInstances[index] = default; return false;
            }
            if (_instances[index].IsValid) FreeOwnedInstance(_instances[index]);
            _instances.RemoveAt(index); _reflectionInstances.RemoveAt(index); _mainVisible.RemoveAt(index);
            _mainDetached.RemoveAt(index); _reflectionDetached.RemoveAt(index);
            return false;
        }
        if (_storage.Count > 0)
        {
            // Reverse construction order releases each MultiMesh before its prototype mesh.
            var index = _storage.Count - 1; var storage = _storage[index];
            RenderingServer.FreeRid(storage.Rid); _storage.RemoveAt(index);
            return false;
        }
        _disposed = true; _draws.Clear();
        lock (_liveGate) _live.Remove(this);
        return true;
    }
}
