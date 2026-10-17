using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Materials;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;
using N = System.Numerics;

namespace Campus.Kernel.Renderer;

public readonly record struct RenderAccount(WpId Owner, int Handles, int Surfaces, int Triangles);

/// <summary>Dynamic geometry host; static streamed TileBundles belong to WP-02.</summary>
public partial class RenderHost : Node3D, IRenderHost, IMapAware
{
    private sealed class Entry(WpId owner, Node3D root)
    {
        public WpId Owner { get; } = owner;
        public Node3D Root { get; } = root;
        public int Surfaces { get; set; }
        public int Triangles { get; set; }
        public bool Pending { get; set; } = true;
        public List<ArrayMesh> Meshes { get; } = [];
        public List<Draw> Draws { get; } = [];
    }
    private sealed class Draw(MeshInstance3D instance, ArrayMesh mesh, GeometryBundle source)
    {
        public MeshInstance3D Instance { get; } = instance;
        public ArrayMesh Mesh { get; } = mesh;
        public GeometryBundle Source { get; } = source;
        public Rid Proxy { get; set; }
    }
    private sealed record Surface(GeometryBundle Geometry, N.Matrix4x4 Transform, PaletteToken? Tint, bool IgnoreOcclusion);
    private readonly Dictionary<ulong, Entry> _entries = [];
    private readonly List<(Node3D Root, int Draws)> _retiring = [];
    private readonly Dictionary<ShaderId, ShaderMaterial> _shaderMaterials = [];
    private int _peakDraws;
    private IToonMaterialProvider? _materials;
    private PaletteCatalog? _catalog;
    private IReflectionWorld? _reflection;
    private ulong _next;
    public string? Error { get; private set; }
    public bool Pending => _entries.Values.Any(entry => entry.Pending);
    public bool PipelineReady => _materials is not null && _entries.Values.All(entry => !entry.Pending &&
        entry.Draws.All(IsReady));

    public override void _Ready()
    {
        _materials = Services.Get<IToonMaterialProvider>();
        // The source publishes one immutable object shared with the baker and other indexed hosts.
        _catalog ??= Services.Get<IPaletteCatalogSource>().Catalog;
        _reflection = Services.Get<IReflectionWorld>();
        _reflection.Changed += RebuildReflectionProxies;
        ProcessPriority = FramePriority.StreamMount;
        RenderingServer.Singleton.FramePostDraw += ReportReleasedInstances;
        GD.Print($"[WP-01][phase=render-instances] event=ready globalUniformSlots=" +
            ProjectSettings.GetSetting("rendering/limits/global_shader_variables/buffer_size").AsInt64());
    }
    public override void _Process(double delta)
    {
        if (_materials is null) return;
        // Independent visibility decisions do not depend on dictionary iteration order.
        foreach (var entry in _entries.Values)
            foreach (var draw in entry.Draws)
                if (!draw.Instance.Visible && IsReady(draw)) draw.Instance.Visible = true;
    }
    public RenderHandle Mount(ContentOutput content, WpId owner)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Instances.Count > 0)
            throw new NotSupportedException("Dynamic RenderHost does not support ContentOutput.Instances; submit static instance batches through the WP-02 bake/stream path.");
        if (_materials is null) throw new InvalidOperationException("RenderHost has not entered the stage.");
        var catalog = _catalog ?? throw new InvalidOperationException("RenderHost has no captured palette catalog.");
        ArgumentException.ThrowIfNullOrWhiteSpace(owner.Value);
        var handle = ++_next;
        var root = new Node3D { Name = $"Render_{owner.Value}_{handle}" };
        var entry = new Entry(owner, root);
        _entries.Add(handle, entry);
        AddChild(root);
        try
        {
            var parts = content.Parts.Select(part =>
            {
                var matches = Services.Services.Entries.Where(item => item.Kind == "part" && item.Id == part.Part.Value).ToArray();
                if (matches.Length != 1)
                    throw new InvalidOperationException($"Dynamic content owner '{owner.Value}' requires exactly one registration for part '{part.Part.Value}' " +
                        $"with parameters {part.Params.GetType().FullName}; found {matches.Length}. " +
                        $"Matching types: {string.Join(", ", matches.Select(item => item.Type.FullName).Order(StringComparer.Ordinal))}.");
                var registration = matches[0];
                return (Part: part, Definition: Services.Services.Create(registration.Type));
            }).ToArray();
            _ = PrepareAndMount(handle, entry, content.Meshes, parts, catalog);
        }
        catch (Exception error)
        {
            GD.PushError($"[WP-01][phase=render-part-resolve][owner={owner.Value}][handle={handle}] {error}");
            Unmount(new(handle));
            throw;
        }
        return new(handle);
    }

    private async Task PrepareAndMount(ulong handle, Entry entry, IReadOnlyList<GeometryBundle> geometry,
        (PlacedPart Part, object Definition)[] parts, PaletteCatalog catalog)
    {
        var phase = "render-geometry-build";
        try
        {
            var surfaces = await Task.Run(() =>
            {
                var result = new List<Surface>();
                foreach (var mesh in geometry) result.Add(new(mesh, N.Matrix4x4.Identity, null, mesh.IgnoreOcclusionCulling));
                foreach (var (part, definition) in parts)
                {
                    try
                    {
                        var method = typeof(RenderHost).GetMethod(nameof(BuildPart), BindingFlags.NonPublic | BindingFlags.Static)!
                            .MakeGenericMethod(part.Params.GetType());
                        var built = (PartGeometry)method.Invoke(null, [definition, part.Params])!;
                        foreach (var mesh in built.Meshes) result.Add(new(mesh, part.At, part.Tint, part.IgnoreOcclusionCulling || mesh.IgnoreOcclusionCulling));
                    }
                    catch (Exception error)
                    {
                        throw new InvalidOperationException($"Dynamic part '{part.Part.Value}' failed in {definition.GetType().FullName} " +
                            $"with parameters {part.Params.GetType().FullName}.", error);
                    }
                }
                foreach (var surface in result)
                {
                    surface.Geometry.ValidateVertexData();
                    catalog.Validate(surface.Geometry.Material.Spec);
                }
                return result;
            });
            if (!_entries.TryGetValue(handle, out var current) || !ReferenceEquals(entry, current)) return;
            phase = "render-geometry-upload";
            foreach (var surface in surfaces)
            {
                if (surface.Geometry.Index.Length == 0) continue;
                var mesh = Upload(surface.Geometry, surface.Tint, catalog);
                entry.Meshes.Add(mesh);
                var material = surface.Geometry.Shader is { } shader ? ResolveShader(shader) : _materials!.Get(surface.Geometry.Material.Spec);
                _materials!.TrackLayout(material, new WarmDrawLayout(MeshSurfaceFormat.Read(mesh), false, BakedPrimitive.Triangles));
                var instance = new MeshInstance3D
                {
                    Mesh = mesh,
                    MaterialOverride = material,
                    Transform = surface.Transform.ToGodot(),
                    IgnoreOcclusionCulling = surface.IgnoreOcclusion,
                    CastShadow = surface.Geometry.Flags is { Length: > 0 } flags && flags.All(value => (value & 8) != 0)
                        ? GeometryInstance3D.ShadowCastingSetting.Off : GeometryInstance3D.ShadowCastingSetting.On,
                    Visible = false
                };
                entry.Root.AddChild(instance);
                var draw = new Draw(instance, mesh, surface.Geometry);
                entry.Draws.Add(draw);
                CreateReflectionProxy(draw);
                instance.Visible = IsReady(draw);
                entry.Surfaces++;
                entry.Triangles += surface.Geometry.Index.Length / 3;
            }
            entry.Pending = false;
            ReportInstanceCounts("mounted", entry.Owner.Value, handle);
        }
        catch (Exception error)
        {
            GD.PushError($"[WP-01][phase={phase}][owner={entry.Owner.Value}][handle={handle}] {error}");
            if (!_entries.ContainsKey(handle)) return;
            Error = $"Dynamic content mount failed for {entry.Owner.Value}: {error.GetBaseException().Message}";
            Unmount(new(handle));
        }
    }

    private static PartGeometry BuildPart<T>(object definition, object parameters) where T : notnull
    {
        var part = (IPartDef<T>)definition;
        if (!part.Lods.TryGetValue(LodLevel.L0, out var build)) throw new InvalidDataException("Dynamic part has no L0 geometry.");
        return build((T)parameters);
    }

    private ArrayMesh Upload(GeometryBundle source, PaletteToken? tint, PaletteCatalog catalog)
    {
        var mesh = new ArrayMesh();
        try
        {
            GeometrySurfaceUpload.Append(mesh, source, _materials!, catalog, tint);
            return mesh;
        }
        catch { mesh.Dispose(); throw; }
    }
    public void Unmount(RenderHandle handle)
    {
        if (!_entries.Remove(handle.Value, out var entry)) return;
        foreach (var draw in entry.Draws) FreeReflectionProxy(draw);
        _retiring.Add((entry.Root, entry.Draws.Count));
        entry.Root.QueueFree();
        foreach (var mesh in entry.Meshes) mesh.Dispose();
        ReportInstanceCounts("queued-free", entry.Owner.Value, handle.Value);
    }
    private void ReportReleasedInstances()
    {
        var count = _retiring.RemoveAll(item => !GodotObject.IsInstanceValid(item.Root));
        if (count > 0) ReportInstanceCounts("nodes-freed-postdraw", "-", 0);
    }
    private void ReportInstanceCounts(string action, string owner, ulong handle)
    {
        var draws = _entries.Values.SelectMany(entry => entry.Draws).ToArray();
        var retiring = _retiring.Sum(item => item.Draws);
        _peakDraws = Math.Max(_peakDraws, draws.Length + retiring);
        var materials = draws.Select(draw => draw.Instance.MaterialOverride).Distinct(ReferenceEqualityComparer.Instance).Count();
        GD.Print($"[WP-01][phase=render-instances] event={action} owner={owner} handle={handle} handles={_entries.Count} " +
            $"draws={draws.Length} materials={materials} retiring={retiring} peak={_peakDraws} frame={Engine.GetProcessFrames()}");
    }
    public override void _ExitTree()
    {
        RenderingServer.Singleton.FramePostDraw -= ReportReleasedInstances;
        if (_reflection is not null) _reflection.Changed -= RebuildReflectionProxies;
        foreach (var handle in _entries.Keys.Order().ToArray()) Unmount(new(handle));
        foreach (var pair in _shaderMaterials.OrderBy(pair => pair.Key)) pair.Value.Dispose();
        _shaderMaterials.Clear();
    }
    public IReadOnlyList<RenderAccount> Accounts() => _entries.Values.GroupBy(entry => entry.Owner)
        .OrderBy(group => group.Key.Value, StringComparer.Ordinal)
        .Select(group => new RenderAccount(group.Key, group.Count(), group.Sum(entry => entry.Surfaces), group.Sum(entry => entry.Triangles))).ToArray();
    public void OnMapUnloading(MapId from)
    {
        foreach (var handle in _entries.Keys.Order().ToArray()) Unmount(new(handle));
        Error = null;
    }
    public void OnMapLoaded(IMapContext context) { }

    private bool IsReady(Draw draw) => draw.Source.Shader is { } shader
        ? _materials!.IsReady(shader, draw.Instance.MaterialOverride)
        : _materials!.IsReady(draw.Source.Material.Spec);

    private Material ResolveShader(ShaderId shader)
    {
        if (_shaderMaterials.TryGetValue(shader, out var cached)) return cached;
        var material = Services.Get<IShaderMaterialSource>().Create(shader);
        try { _materials!.Track(shader, material); }
        catch { material.Dispose(); throw; }
        _shaderMaterials.Add(shader, material);
        return material;
    }

    private void CreateReflectionProxy(Draw draw)
    {
        if (!draw.Source.Reflect || _reflection is null || !_reflection.Scenario.IsValid) return;
        var proxy = RenderingServer.InstanceCreate2(draw.Mesh.GetRid(), _reflection.Scenario);
        try
        {
            RenderingServer.InstanceGeometrySetMaterialOverride(proxy, _reflection.ProxyMaterial.GetRid());
            RenderingServer.InstanceGeometrySetCastShadowsSetting(proxy, RenderingServer.ShadowCastingSetting.Off);
            RenderingServer.InstanceSetTransform(proxy, draw.Instance.GlobalTransform);
            draw.Proxy = proxy;
        }
        catch { RenderingServer.FreeRid(proxy); throw; }
    }

    private static void FreeReflectionProxy(Draw draw)
    {
        if (!draw.Proxy.IsValid) return;
        RenderingServer.FreeRid(draw.Proxy);
        draw.Proxy = default;
    }

    private void RebuildReflectionProxies()
    {
        var draws = _entries.OrderBy(pair => pair.Key).SelectMany(pair => pair.Value.Draws).ToArray();
        foreach (var draw in draws) FreeReflectionProxy(draw);
        try { foreach (var draw in draws) CreateReflectionProxy(draw); }
        catch
        {
            foreach (var draw in draws) FreeReflectionProxy(draw);
            throw;
        }
    }
}
