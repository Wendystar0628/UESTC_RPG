using System.Runtime.InteropServices;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Diagnostics;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Stream;

/// <summary>Owns its meshes, multimeshes and instances. Scenario and material RIDs remain borrowed.</summary>
public sealed class PreparedTile : IDisposable
{
    private static readonly StringName _shaderInstanceFadeT0 = InstanceShaderParameters.FadeT0;
    // Diagnostic index of existing owners; release remains entirely with each PreparedTile.
    private static readonly object _liveGate = new();
    private static readonly HashSet<PreparedTile> _live = [];

    public sealed record Draw(Rid Base, Rid Mesh, Transform3D At, ToonRef Material,
        bool IgnoreOcclusionCulling, ShaderId? Shader, bool Reflect, bool Resident = false)
    {
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
        try
        {
            foreach (var surface in bundle.Surfaces)
            {
                if (surface.VertexCount == 0) continue;
                var mesh = CreateMesh(surface);
                _draws.Add(new(mesh, mesh, surface.At.ToGodot(), surface.Material,
                    surface.IgnoreOcclusionCulling, surface.Shader, surface.Reflect, bundle.Level == LodLevel.L4 && surface.Shader == ShaderId.DioramaFar));
            }
            foreach (var item in bundle.MultiMeshes)
            {
                if (item.InstanceCount == 0 || item.Surface.VertexCount == 0) continue;
                var mesh = CreateMesh(item.Surface);
                var multi = RenderingServer.MultimeshCreate();
                if (!multi.IsValid) throw new InvalidOperationException("MultiMesh creation failed.");
                _storage.Add((multi, true));
                RenderingServer.MultimeshAllocateData(multi, item.InstanceCount,
                    RenderingServer.MultimeshTransformFormat.Transform3D, false, true);
                RenderingServer.MultimeshSetMesh(multi, mesh);
                if (!BitConverter.IsLittleEndian) throw new PlatformNotSupportedException("Packed instance buffers require a little-endian engine.");
                RenderingServer.MultimeshSetBuffer(multi, MemoryMarshal.Cast<byte, float>(item.InstanceData.Span));
                RenderingServer.MultimeshSetCustomAabb(multi, Bounds(item.Bounds));
                _draws.Add(new(multi, mesh, Transform3D.Identity, item.Surface.Material,
                    item.Surface.IgnoreOcclusionCulling, item.Surface.Shader, item.Surface.Reflect, bundle.Level == LodLevel.L4 && item.Surface.Shader == ShaderId.DioramaFar));
            }
            lock (_liveGate) _live.Add(this);
        }
        catch (Exception failure)
        {
            try { Dispose(); }
            catch (Exception cleanup)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(RetainedFailure.Append(failure, cleanup)).Throw();
                throw;
            }
            throw;
        }
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
            ["vertex_data"] = surface.VertexData.ToArray(),
            ["vertex_count"] = surface.VertexCount,
            ["aabb"] = Bounds(surface.Bounds),
            ["attribute_data"] = surface.AttributeData.ToArray(),
            ["skin_data"] = surface.SkinData.ToArray(),
            ["index_data"] = surface.IndexData.ToArray(),
            ["index_count"] = surface.IndexCount,
            ["uv_scale"] = new Vector4(surface.UvScale.X, surface.UvScale.Y, surface.UvScale.Z, surface.UvScale.W)
        };
        using var lods = new Godot.Collections.Array();
        var lodOwners = new List<Godot.Collections.Dictionary>();
        try
        {
            foreach (var lod in surface.Lods)
            {
                var dictionary = new Godot.Collections.Dictionary { ["edge_length"] = lod.Error, ["index_data"] = lod.IndexData.ToArray() };
                lodOwners.Add(dictionary); lods.Add(dictionary);
            }
            data["lods"] = lods;
            using var surfacesOwner = new Godot.Collections.Array();
            var surfaces = new Godot.Collections.Array<Godot.Collections.Dictionary>(surfacesOwner);
            surfaces.Add(data);
            var mesh = RenderingServer.MeshCreateFromSurfaces(surfaces);
            if (!mesh.IsValid) throw new InvalidDataException($"GPU mesh creation failed for {Key}.");
            _storage.Add((mesh, false)); return mesh;
        }
        finally { foreach (var lod in lodOwners) lod.Dispose(); }
    }
    public Rid MountOne(int index, Rid scenario, Rid material, Rid reflectionScenario = default,
        Material? reflectionMaterial = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_retiring) throw new InvalidOperationException("A retiring tile cannot accept new native instances.");
        var draw = _draws[index];
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
        RenderingServer.InstanceGeometrySetCastShadowsSetting(instance, water || draw.Resident
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
    public void Dispose()
    {
        if (_disposed) return;
        BeginRetirement();
        List<Exception>? failures = null;
        // Each successful detach/free advances only its own ownership. A failed RID remains retryable.
        for (var i = _instances.Count - 1; i >= 0; i--)
        {
            if (_reflectionInstances[i].IsValid)
            {
                if (!_reflectionDetached[i])
                    try { DetachInstance(_reflectionInstances[i]); _reflectionDetached[i] = true; }
                    catch (Exception error) { (failures ??= []).Add(error); }
                try { FreeOwnedInstance(_reflectionInstances[i]); _reflectionInstances[i] = default; }
                catch (Exception error) { (failures ??= []).Add(error); }
            }
            if (_instances[i].IsValid)
            {
                if (!_mainDetached[i])
                    try { DetachInstance(_instances[i]); _mainDetached[i] = true; _mainVisible[i] = false; }
                    catch (Exception error) { (failures ??= []).Add(error); }
                try { FreeOwnedInstance(_instances[i]); _instances[i] = default; }
                catch (Exception error) { (failures ??= []).Add(error); }
            }
            if (_instances[i].IsValid || _reflectionInstances[i].IsValid) continue;
            _instances.RemoveAt(i); _reflectionInstances.RemoveAt(i); _mainVisible.RemoveAt(i);
            _mainDetached.RemoveAt(i); _reflectionDetached.RemoveAt(i);
        }
        // An instance may still borrow any base. Preserve all bases until every instance is gone.
        // Stop on a storage failure: a MultiMesh must release before its prototype mesh.
        if (_instances.Count == 0)
            while (_storage.Count > 0)
            {
                var index = _storage.Count - 1;
                try { RenderingServer.FreeRid(_storage[index].Rid); _storage.RemoveAt(index); }
                catch (Exception error) { (failures ??= []).Add(error); break; }
            }
        _detachCursor = _instances.Count - 1;
        _disposed = _instances.Count == 0 && _storage.Count == 0;
        if (_disposed)
        {
            _draws.Clear();
            lock (_liveGate) _live.Remove(this);
        }
        if (failures is not null) throw new AggregateException("Prepared tile release failed.", failures);
    }
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
    public void DetachAll() { while (!DetachOne()) { } }
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
