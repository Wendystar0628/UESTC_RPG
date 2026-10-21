using Campus.Kernel.Diagnostics;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;
using N = System.Numerics;

namespace Campus.Kernel.Renderer;

/// <summary>Dynamic geometry host; static streamed TileBundles belong to game/kernel/bake/.</summary>
[KernelService(typeof(IRenderReadiness))]
[KernelService(typeof(IRenderHost))]
[KernelNode("render-host")]
public partial class RenderHost : Node3D, IRenderHost, IRenderReadiness, IMapAware
{
    private sealed class Entry(Node3D root)
    {
        public Node3D Root { get; } = root;
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
    public Exception? Failure { get; private set; }
    public bool Pending => _entries.Values.Any(entry => entry.Pending);
    public bool IsReady(RenderHandle handle) => _entries.TryGetValue(handle.Value, out var entry) && !entry.Pending;

    /// <summary>Raw reflection instances are not scene-tree nodes; main instances are scanned there.</summary>
    public Rid[] CaptureReflectionInstances() => _entries.Values.SelectMany(entry => entry.Draws)
        .Select(draw => draw.Proxy).Where(rid => rid.IsValid).ToArray();

    public override void _Ready()
    {
        _materials = KernelServices.Get<IToonMaterialProvider>();
        // The source publishes one immutable object shared with the baker and other indexed hosts.
        _catalog ??= KernelServices.Get<IPaletteCatalogSource>().Catalog;
        _reflection = KernelServices.Get<IReflectionWorld>();
        _reflection.Changed += RebuildReflectionProxies;
        ProcessPriority = FramePriority.StreamMount;
        RenderingServer.Singleton.FramePostDraw += ReportReleasedInstances;
        GD.Print($"[ENG-ARCH][phase=render-instances] event=ready globalUniformSlots=" +
            ProjectSettings.GetSetting("rendering/limits/global_shader_variables/buffer_size").AsInt64());
    }
    public RenderHandle Mount(ContentOutput content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Instances.Count > 0)
            throw new NotSupportedException("Dynamic RenderHost does not support ContentOutput.Instances; submit static instance batches through the game/kernel/bake/ bake/stream path.");
        if (_materials is null) throw new InvalidOperationException("RenderHost has not entered the stage.");
        var catalog = _catalog ?? throw new InvalidOperationException("RenderHost has no captured palette catalog.");
        var handle = ++_next;
        var root = new Node3D { Name = $"Render_{handle}" };
        var entry = new Entry(root);
        _entries.Add(handle, entry);
        AddChild(root);
        try
        {
            var parts = content.Parts.Select(part =>
            {
                var matches = KernelServices.Services.Query<IPartDef>(RegistrationKind.Part).Where(item => item.Id == part.Part.Value).ToArray();
                if (matches.Length != 1)
                    throw new InvalidOperationException($"Dynamic content handle '{handle}' requires exactly one registration for part '{part.Part.Value}' " +
                        $"with parameters {part.Params.GetType().FullName}; found {matches.Length}. " +
                        $"Matching types: {string.Join(", ", matches.Select(item => item.Type.FullName).Order(StringComparer.Ordinal))}.");
                var registration = matches[0];
                return (Part: part, Definition: KernelServices.Services.Create(registration.Type));
            }).ToArray();
            _ = PrepareAndMount(handle, entry, content.Meshes, parts, catalog);
        }
        catch (Exception error)
        {
            DiagnosticLog.Error($"[ENG-ARCH][phase=render-part-resolve][handle={handle}]", error);
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
            // Prepared geometry must finish native submission inside the caller's mount budget.
            var surfaces = parts.Length == 0 ? PrepareSurfaces(geometry, parts, catalog)
                : await Task.Run(() => PrepareSurfaces(geometry, parts, catalog));
            if (!_entries.TryGetValue(handle, out var current) || !ReferenceEquals(entry, current)) return;
            phase = "render-geometry-upload";
            foreach (var surface in surfaces)
            {
                if (surface.Geometry.Index.Length == 0) continue;
                var mesh = Upload(surface.Geometry, surface.Tint, catalog);
                entry.Meshes.Add(mesh);
                var material = surface.Geometry.Shader is { } shader ? ResolveShader(shader) : _materials!.Get(surface.Geometry.Material.Spec);
                var instance = new MeshInstance3D
                {
                    Mesh = mesh,
                    MaterialOverride = material,
                    Transform = surface.Transform.ToGodot(),
                    IgnoreOcclusionCulling = surface.IgnoreOcclusion,
                    CastShadow = surface.Geometry.Flags is { Length: > 0 } flags && flags.All(value => (value & 8) != 0)
                        ? GeometryInstance3D.ShadowCastingSetting.Off : GeometryInstance3D.ShadowCastingSetting.On,
                    Visible = true
                };
                entry.Root.AddChild(instance);
                var draw = new Draw(instance, mesh, surface.Geometry);
                entry.Draws.Add(draw);
                CreateReflectionProxy(draw);
            }
            entry.Pending = false;
            ReportInstanceCounts("mounted", handle);
        }
        catch (Exception error)
        {
            DiagnosticLog.Error($"[ENG-ARCH][phase={phase}][handle={handle}]", error);
            Failure = Failure is null ? error : new AggregateException(Failure, error);
            if (!_entries.ContainsKey(handle)) return;
            try { Unmount(new(handle)); }
            catch (Exception cleanup) { Failure = new AggregateException(Failure, cleanup); }
        }
    }

    private static List<Surface> PrepareSurfaces(IReadOnlyList<GeometryBundle> geometry,
        (PlacedPart Part, object Definition)[] parts, PaletteCatalog catalog)
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
            GeometrySurfaceUpload.Append(mesh, source, catalog, tint);
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
        ReportInstanceCounts("queued-free", handle.Value);
    }
    private void ReportReleasedInstances()
    {
        var count = _retiring.RemoveAll(item => !GodotObject.IsInstanceValid(item.Root));
        if (count > 0) ReportInstanceCounts("nodes-freed-postdraw", 0);
    }
    private void ReportInstanceCounts(string action, ulong handle)
    {
        var draws = _entries.Values.SelectMany(entry => entry.Draws).ToArray();
        var retiring = _retiring.Sum(item => item.Draws);
        _peakDraws = Math.Max(_peakDraws, draws.Length + retiring);
        var materials = draws.Select(draw => draw.Instance.MaterialOverride).Distinct(ReferenceEqualityComparer.Instance).Count();
        GD.Print($"[ENG-ARCH][phase=render-instances] event={action} handle={handle} handles={_entries.Count} " +
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
    public void OnMapUnloading(MapId from)
    {
        foreach (var handle in _entries.Keys.Order().ToArray()) Unmount(new(handle));
        Failure = null;
    }
    public void OnMapLoaded(IMapContext context) { }

    private Material ResolveShader(ShaderId shader)
    {
        if (_shaderMaterials.TryGetValue(shader, out var cached)) return cached;
        var material = KernelServices.Get<IShaderMaterialSource>().Create(shader);
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
