using Campus.Contracts;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Stubs.Visual;

/// <summary>D0 geometry mounting with explicit material bridges and same-lifetime reflection proxies.</summary>
public sealed class D0RenderHost : IRenderHost, IDisposable
{
    private sealed class Surface(ArrayMesh mesh, MeshInstance3D instance, bool reflect)
    {
        public ArrayMesh Mesh { get; } = mesh;
        public MeshInstance3D Instance { get; } = instance;
        public bool Reflect { get; } = reflect;
        public Rid Proxy { get; set; }
    }

    private sealed class Entry(Node3D root)
    {
        public Node3D Root { get; } = root;
        public List<Surface> Surfaces { get; } = [];
    }

    private readonly Node3D _stage;
    private readonly Dictionary<ulong, Entry> _roots = [];
    private readonly Dictionary<ShaderId, ShaderMaterial> _shaderMaterials = [];
    private IReflectionWorld? _reflection;
    private ulong _next;
    private bool _disposed;

    public D0RenderHost(Node3D stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        _stage = stage;
        _stage.TreeExiting += Dispose;
    }

    public RenderHandle Mount(ContentOutput content, WpId owner)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(owner.Value);
        if (content.Parts.Count != 0)
            throw new NotSupportedException("D0RenderHost mounts GeometryBundle only. PlacedPart generation and mounting belong to the WP-01 render host.");

        var materials = Services.Get<IToonMaterialProvider>();
        if (_reflection is null)
        {
            _reflection = Services.Get<IReflectionWorld>();
            _reflection.Changed += RebuildReflectionProxies;
        }
        var handle = ++_next;
        var root = new Node3D { Name = $"D0_{owner.Value}_{handle}" };
        var entry = new Entry(root);
        _roots.Add(handle, entry);
        _stage.AddChild(root);
        try
        {
            foreach (var geometry in content.Meshes)
            {
                geometry.ValidateVertexData();
                if (geometry.Index.Length == 0) continue;
                var useD0Rgb = materials is FallbackToonMaterialProvider && geometry.Shader is null;
                var spec = useD0Rgb ? geometry.Material.Spec : ApprovedD0Spec(geometry.Material.Spec);
                var material = geometry.Shader is { } shader ? ResolveShaderMaterial(shader) : materials.Get(spec);
                var mesh = Upload(geometry, spec, materials, useD0Rgb);
                var instance = new MeshInstance3D
                {
                    Mesh = mesh,
                    MaterialOverride = material,
                    IgnoreOcclusionCulling = geometry.IgnoreOcclusionCulling,
                    CastShadow = geometry.Flags is { Length: > 0 } flags && flags.All(value => (value & 8) != 0)
                        ? GeometryInstance3D.ShadowCastingSetting.Off : GeometryInstance3D.ShadowCastingSetting.On
                };
                var surface = new Surface(mesh, instance, geometry.Reflect);
                entry.Surfaces.Add(surface);
                root.AddChild(instance);
                CreateReflectionProxy(surface);
            }
            return new(handle);
        }
        catch
        {
            Unmount(new(handle));
            throw;
        }
    }

    private Material ResolveShaderMaterial(ShaderId shader)
    {
        if (_shaderMaterials.TryGetValue(shader, out var material)) return material;
        material = Services.Get<IShaderMaterialSource>().Create(shader);
        try { Services.Get<IToonMaterialProvider>().Track(shader, material); }
        catch { material.Dispose(); throw; }
        _shaderMaterials.Add(shader, material);
        return material;
    }

    private static ToonSpec ApprovedD0Spec(ToonSpec spec) => spec.Lit.Value switch
    {
        "d0.building" => spec with { Lit = new("wall.greyWhite.lit"), Shade = new("wall.greyWhite.shade"), Fx = spec.Fx | ToonFx.BevelLift },
        "d0.ground" => spec with { Lit = new("grass.lit"), Shade = new("grass.shade") },
        "d0.road" => spec with { Lit = new("stone.beige.lit"), Shade = new("stone.beige.shade") },
        "d0.water" => spec with { Lit = new("uestc.techblue.lit"), Shade = new("uestc.techblue.shade") },
        "interior.wall.lit" => spec with { Lit = new("wall.lit"), Shade = new("wall.shade") },
        _ => spec
    };

    private static ArrayMesh Upload(GeometryBundle source, ToonSpec spec, IToonMaterialProvider materials, bool useD0Rgb)
    {
        var count = source.Position.Length / 3;
        var vertices = new Vector3[count];
        var normals = new Vector3[count];
        var colors = new Color[count];
        var defaultId = !useD0Rgb && source.ColorIds is null ? materials.PaletteId(spec.Lit) : (byte)0;
        for (var i = 0; i < count; i++)
        {
            vertices[i] = new(source.Position[3 * i], source.Position[3 * i + 1], source.Position[3 * i + 2]);
            normals[i] = new(source.Normal[3 * i], source.Normal[3 * i + 1], source.Normal[3 * i + 2]);
            if (useD0Rgb)
            {
                colors[i] = ((FallbackToonMaterialProvider)materials).VertexColor(spec, source.ColorIds?[i]);
                continue;
            }
            var ao = (byte)Math.Clamp((int)MathF.Round((source.Ao?[i] ?? 1f) * 255f), 0, 255);
            colors[i] = new(EncodeByte(source.ColorIds?[i] ?? defaultId), EncodeByte(ao),
                EncodeByte(source.Flags?[i] ?? 0), EncodeByte(source.Reserved?[i] ?? 0));
        }

        using var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Index] = source.Index;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        if (source.Uv is { } uv) arrays[(int)Mesh.ArrayType.TexUV] = Pairs(uv);
        if (source.Uv2 is { } uv2) arrays[(int)Mesh.ArrayType.TexUV2] = Pairs(uv2);
        // Godot compresses UV2 to UNorm16 with this flag; window IDs and negative reflection slots require float32.
        var format = source.Uv2 is null ? Mesh.ArrayFormat.FlagCompressAttributes : (Mesh.ArrayFormat)0;
        SetCustom(Mesh.ArrayType.Custom0, source.Custom0, Mesh.ArrayFormat.FormatCustom0Shift);
        SetCustom(Mesh.ArrayType.Custom1, source.Custom1, Mesh.ArrayFormat.FormatCustom1Shift);
        SetCustom(Mesh.ArrayType.Custom2, source.Custom2, Mesh.ArrayFormat.FormatCustom2Shift);
        using var lods = new Godot.Collections.Dictionary();
        if (source.Lods is { } levels)
            foreach (var lod in levels.OrderBy(level => level.Error)) lods[lod.Error] = lod.Index;
        var mesh = new ArrayMesh();
        try { mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, lods: lods, flags: format); }
        catch { mesh.Dispose(); throw; }
        return mesh;

        void SetCustom(Mesh.ArrayType channel, float[]? values, Mesh.ArrayFormat shift)
        {
            if (values is null) return;
            arrays[(int)channel] = values;
            format |= (Mesh.ArrayFormat)((ulong)Mesh.ArrayCustomFormat.RgbaFloat << (int)shift);
        }
    }

    private static float EncodeByte(byte value) => (value + 0.5f) / 255f;
    private static Vector2[] Pairs(float[] values)
    {
        var result = new Vector2[values.Length / 2];
        for (var i = 0; i < result.Length; i++) result[i] = new(values[2 * i], values[2 * i + 1]);
        return result;
    }

    private void CreateReflectionProxy(Surface surface)
    {
        if (!surface.Reflect || _reflection is null || !_reflection.Scenario.IsValid) return;
        var proxy = RenderingServer.InstanceCreate2(surface.Mesh.GetRid(), _reflection.Scenario);
        try
        {
            RenderingServer.InstanceGeometrySetMaterialOverride(proxy, _reflection.ProxyMaterial.GetRid());
            RenderingServer.InstanceGeometrySetCastShadowsSetting(proxy, RenderingServer.ShadowCastingSetting.Off);
            RenderingServer.InstanceSetTransform(proxy, surface.Instance.GlobalTransform);
            surface.Proxy = proxy;
        }
        catch { RenderingServer.FreeRid(proxy); throw; }
    }

    private static void FreeReflectionProxy(Surface surface)
    {
        if (!surface.Proxy.IsValid) return;
        RenderingServer.FreeRid(surface.Proxy);
        surface.Proxy = default;
    }

    private void RebuildReflectionProxies()
    {
        var surfaces = _roots.OrderBy(pair => pair.Key).SelectMany(pair => pair.Value.Surfaces).ToArray();
        foreach (var surface in surfaces) FreeReflectionProxy(surface);
        try { foreach (var surface in surfaces) CreateReflectionProxy(surface); }
        catch
        {
            foreach (var surface in surfaces) FreeReflectionProxy(surface);
            throw;
        }
    }

    public void Unmount(RenderHandle handle)
    {
        if (!_roots.Remove(handle.Value, out var entry)) return;
        foreach (var surface in entry.Surfaces) FreeReflectionProxy(surface);
        if (entry.Root.IsInsideTree()) entry.Root.QueueFree();
        else entry.Root.Free();
        foreach (var surface in entry.Surfaces) surface.Mesh.Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stage.TreeExiting -= Dispose;
        if (_reflection is not null) _reflection.Changed -= RebuildReflectionProxies;
        foreach (var handle in _roots.Keys.Order().ToArray()) Unmount(new(handle));
        foreach (var pair in _shaderMaterials.OrderBy(pair => pair.Key)) pair.Value.Dispose();
        _shaderMaterials.Clear();
    }
}
