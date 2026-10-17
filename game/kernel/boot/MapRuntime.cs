using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
using Campus.Kernel.Registry;
using Campus.Kernel.Renderer;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Boot;

/// <summary>Scene-side map lifetime. The map service owns transition ordering; this host owns engine nodes.</summary>
[KernelService(typeof(IMapRuntime), Owner = "WP-01")]
[KernelNode("map-runtime", Owner = "WP-01")]
public partial class MapRuntime : Node, IMapRuntime
{
    private readonly List<(Type Type, Node Node)> _scoped = [];
    private IMapStreamState? _stream;
    private ICollisionWorld? _collision;
    private RenderHost? _renderer;
    private MaterialWarmup? _materials;
    private MapId? _loaded;
    private Task? _preparing;
    private bool _mountPrepared;
    private bool _unloading;
    private bool _stopping;
    private long _mapGeneration;
    private string? _error;
    private bool _warming;
    private bool _samplesStarted;
    private long _drawFrames;
    private long _lastWarmDraw;
    private int _stable;
    private PipelineCounts _previous;
    private ViewContentReadiness? _contentReadiness;
    public bool IsUnloaded => !_unloading && (_preparing is null || _preparing.IsCompleted) && _stream?.IsUnloaded == true && _scoped.Count == 0;
    public bool ResidentReady => _mountPrepared && _stream?.ResidentReady == true && _renderer?.Pending == false && Error is null;
    public bool DestinationReady => _mountPrepared && _stream?.DestinationReady == true && _renderer?.PipelineReady == true && Error is null;
    public bool WarmReady { get; private set; }
    public string? Error => _error ?? _materials?.Error ?? _stream?.Error ?? _renderer?.Error ?? _collision?.Error;

    public override void _Ready()
    {
        ProcessPriority = FramePriority.StreamMount + 1;
        _stream = Services.Get<IMapStreamState>();
        _collision = Services.Get<ICollisionWorld>();
        _renderer = Services.Get<IRenderHost>() as RenderHost
            ?? throw new InvalidOperationException("The stage requires its dynamic render host.");
        _materials = Services.Get<MaterialWarmup>();
        RenderingServer.Singleton.FramePostDraw += CountDraw;
    }

    public void Unload(MapId from)
    {
        if (_unloading) return;
        _error = null;
        _mountPrepared = false;
        _contentReadiness = null;
        _unloading = true;
        _mapGeneration++;
        _warming = false;
        WarmReady = false;
        _materials?.End(reset: true);
        _ = UnloadAfterPreparation(from, _preparing);
    }

    private async Task UnloadAfterPreparation(MapId from, Task? preparation)
    {
        var failures = new List<Exception>();
        try
        {
            // Drain preparation before releasing its data, so a late CPU result cannot republish an unloaded map.
            if (preparation is not null) await preparation;
            var receivers = Instances(from).ToArray();
            foreach (var aware in receivers.OfType<IMapAware>())
                try { aware.OnMapUnloading(from); }
                catch (Exception error) { ReportFailure(from, "map-aware-unload", aware.GetType().FullName, error); failures.Add(error); }
            foreach (var participant in receivers.OfType<IMapDataParticipant>())
                try { participant.Unload(from); }
                catch (Exception error) { ReportFailure(from, "map-data-unload", participant.GetType().FullName, error); failures.Add(error); }
            foreach (var (type, node) in _scoped)
            {
                Services.Services.Release(type);
                node.GetParent()?.RemoveChild(node);
                node.QueueFree();
            }
            _scoped.Clear();
            _loaded = null;
        }
        catch (Exception error) { ReportFailure(from, "map-unload", null, error); failures.Add(error); }
        finally
        {
            _preparing = null;
            _unloading = false;
            if (failures.Count > 0) _error = string.Join("; ", failures.Select(error => error.GetBaseException().Message));
        }
    }

    public void Mount(IMapContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_stopping) throw new InvalidOperationException("The map runtime is shutting down.");
        if (_loaded.HasValue || _unloading || _preparing is { IsCompleted: false })
            throw new InvalidOperationException("Unload the current map before mounting another.");
        _error = null;
        _mountPrepared = false;
        _contentReadiness = null;
        WarmReady = false;
        try
        {
            var rect = context.Descriptor.DataRect;
            RenderingServer.GlobalShaderParameterSet(ShaderGlobals.MapRect,
                new Vector4(rect.Min.X, rect.Min.Y, 1f / (rect.Max.X - rect.Min.X), 1f / (rect.Max.Y - rect.Min.Y)));
            var camera = Services.Get<Camera3D>();
            camera.Far = BackdropInputs.CameraFar(context.Descriptor, context.SourceInputs, BootTuning.CameraFar, camera.Near);
            var nodes = Services.Services.ForMap(context.Descriptor.Id.Value)
                .Where(entry => entry.Kind == "node" && !string.IsNullOrEmpty(entry.Map))
                .OrderBy(entry => entry.Id, StringComparer.Ordinal)
                .Select(entry => (entry.Type, Node: Services.Services.Create(entry.Type) as Node
                    ?? throw new InvalidOperationException($"KernelNode does not inherit Node: {entry.Id}.")))
                .ToArray();
            _scoped.AddRange(nodes);
            foreach (var (_, node) in nodes) GetParent().AddChild(node);
            if (nodes.Any(item => !item.Node.IsInsideTree()))
                throw new InvalidOperationException("Every map kernel node must enter the tree before map callbacks.");
            _loaded = context.Descriptor.Id;
            _preparing = PrepareMap(context, ++_mapGeneration);
        }
        catch (Exception error)
        {
            _error = error.GetBaseException().Message;
            ReportFailure(context.Descriptor.Id, "map-node-mount", null, error);
            throw;
        }
    }

    private async Task PrepareMap(IMapContext context, long generation)
    {
        var phase = "map-instances";
        string? receiver = null;
        try
        {
            // Snapshot by reference before callbacks; producers prepare first, then consumers see one complete map.
            var receivers = Instances(context.Descriptor.Id).ToArray();
            foreach (var participant in receivers.OfType<IMapDataParticipant>())
            {
                phase = "map-data-prepare";
                receiver = participant.GetType().FullName;
                await participant.Prepare(context);
                if (generation != _mapGeneration) return;
            }
            foreach (var aware in receivers.OfType<IMapAware>())
            {
                phase = "map-aware-load";
                receiver = aware.GetType().FullName;
                aware.OnMapLoaded(context);
            }
            _contentReadiness = ViewContentReadiness.Capture(GetParent());
            _mountPrepared = true;
        }
        catch (Exception error)
        {
            ReportFailure(context.Descriptor.Id, phase, receiver, error);
            if (generation == _mapGeneration) _error = error.GetBaseException().Message;
        }
    }

    public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null)
    {
        if (_stopping) throw new InvalidOperationException("The map runtime is shutting down.");
        WarmReady = false;
        _stream!.Prefetch(destination, mode, space);
    }
    public void BeginWarm()
    {
        if (_stopping) throw new InvalidOperationException("The map runtime is shutting down.");
        _warming = true;
        _samplesStarted = false;
        _stable = 0;
        _lastWarmDraw = _drawFrames;
        _previous = PipelineCounts.Read();
        WarmReady = false;
        _materials!.End(reset: true);
    }
    public string DescribeReadiness() =>
        $"map={_loaded?.Value ?? "none"},mountPrepared={_mountPrepared},preparation={_preparing?.Status}," +
        $"unloading={_unloading},stopping={_stopping},resident={ResidentReady},destination={DestinationReady},warm={WarmReady}," +
        $"warming={_warming},samplesStarted={_samplesStarted},stable={_stable}/{BootTuning.WarmFrames}," +
        $"drawFrames={_drawFrames},lastWarmDraw={_lastWarmDraw},pipeline={_previous},error={Error ?? "none"}; " +
        $"stream[resident={_stream?.ResidentReady},destination={_stream?.DestinationReady},unloaded={_stream?.IsUnloaded}," +
        $"target={_stream?.Destination},error={_stream?.Error ?? "none"}]; " +
        $"dynamic[pending={_renderer?.Pending},pipelineReady={_renderer?.PipelineReady},error={_renderer?.Error ?? "none"}]; " +
        $"materials[{_materials?.DescribeReadiness() ?? "missing"}]";
    public override void _Process(double delta)
    {
        if (!_warming || Error is not null) return;
        if (!_samplesStarted)
        {
            if (!ResidentReady) return;
            try
            {
                if (_contentReadiness?.IsReady(Services.Get<ICameraRig>().Pose) != true) return;
            }
            catch (Exception error)
            {
                _error = error.GetBaseException().Message;
                ReportFailure(_loaded ?? default, "content-readiness", null, error);
                return;
            }
            _materials!.Begin();
            _samplesStarted = true;
        }
        if (_drawFrames == _lastWarmDraw) return;
        _lastWarmDraw = _drawFrames;
        var counts = PipelineCounts.Read();
        _stable = ResidentReady && _materials!.IsReady && counts == _previous ? _stable + 1 : 0;
        _previous = counts;
        WarmReady = _stable >= BootTuning.WarmFrames && DestinationReady;
        if (WarmReady) { _materials!.End(); _warming = false; }
    }

    private void CountDraw() => _drawFrames++;
    public void StopNewWork()
    {
        _stopping = true;
        _warming = false;
        _materials?.End(reset: true);
    }
    public override void _ExitTree()
    {
        _mapGeneration++;
        RenderingServer.Singleton.FramePostDraw -= CountDraw;
        if (_materials is not null && GodotObject.IsInstanceValid(_materials)) _materials.End(reset: true);
    }

    private static void ReportFailure(MapId map, string phase, string? receiver, Exception error) =>
        GD.PushError($"[WP-01][map={map.Value}][phase={phase}][receiver={receiver ?? "-"}] {error}");

    private static IEnumerable<object> Instances(MapId map) => Services.Services.ForMap(map.Value)
        .Where(entry => entry.Kind is "node" or "service" or "save")
        .GroupBy(entry => entry.Type)
        .Select(group => group.OrderBy(entry => entry.Kind == "node" ? 0 : 1).ThenBy(entry => entry.Id, StringComparer.Ordinal).First())
        .OrderBy(entry => entry.Id, StringComparer.Ordinal)
        .Select(entry =>
        {
            try { return RuntimeComposition.Resolve(Services.Services, entry); }
            catch (Exception error)
            {
                throw new InvalidOperationException($"Map runtime cannot resolve {entry.Kind} '{entry.Id}' ({entry.Type.FullName}).", error);
            }
        }).Distinct(ReferenceEqualityComparer.Instance);
}
