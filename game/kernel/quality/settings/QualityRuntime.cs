using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
using Campus.Kernel.Quality.Adaptive;
using RuntimeKernel = Campus.Kernel.Registry.Kernel;
using Godot;

namespace Campus.Kernel.Quality.Settings;

[KernelNode("quality", Owner = "WP-32")]
[QualityResponsive(Owns = [QualityKnob.DisplayMode, QualityKnob.WindowSize, QualityKnob.VSync,
    QualityKnob.FrameCap, QualityKnob.Vrr, QualityKnob.RenderScale, QualityKnob.FsrSharpness,
    QualityKnob.Adaptive, QualityKnob.GraphicsApi, QualityKnob.GpuIndex])]
public partial class QualityRuntime : Node, IQualityAware, IQualityRuntime
{
    private QualityService _service = null!;
    private IVeilService _veil = null!;
    private CancellationToken _lifetime;
    private bool _closed;
    private List<TaskCompletionSource<double>> _frames = [];
    public override void _Ready()
    {
        ProcessPriority = FramePriority.Quality;
        _service = (QualityService)RuntimeKernel.Get<IQualityService>();
        _veil = RuntimeKernel.Get<IVeilService>();
        _closed = false;
        _lifetime = _service.Bind(this);
    }

    public override void _ExitTree()
    {
        _closed = true;
        _service?.Unbind(this);
        foreach (var frame in _frames) frame.TrySetCanceled();
        _frames.Clear();
    }

    public override void _Process(double delta)
    {
        if (_closed) return;
        _service.Tick(delta);
        if (_frames.Count == 0) return;
        var frames = _frames;
        _frames = [];
        foreach (var frame in frames)
        {
            if (_closed) frame.TrySetCanceled();
            else frame.TrySetResult(delta);
        }
    }

    public Task<double> NextFrame(CancellationToken cancellationToken)
    {
        if (_closed) return Task.FromCanceled<double>(new CancellationToken(true));
        cancellationToken.ThrowIfCancellationRequested();
        var frame = new TaskCompletionSource<double>();
        _frames.Add(frame);
        return frame.Task.WaitAsync(cancellationToken);
    }

    public bool Transitioning
    {
        get
        {
            var app = RuntimeKernel.Get<IAppFlow>().State;
            var appBusy = app switch
            {
                AppState.Boot or AppState.Entering or AppState.Leaving or AppState.Quitting => true,
                AppState.Title or AppState.Playing => false,
                _ => throw new ArgumentOutOfRangeException(nameof(app)),
            };
            return appBusy || RuntimeKernel.Get<ICameraRig>().ActiveTransit is { Done.IsCompleted: false } ||
                RuntimeKernel.Get<IMapService>().Transition is { Done.IsCompleted: false } ||
                RuntimeKernel.Get<ISaveService>().Busy || RuntimeKernel.Get<IWorldState>().Transition.HasFlag(WorldTransition.Season) ||
                _veil.Opacity > 0 && OtherVeil(_veil.Reason);
        }
    }

    public Task Cover(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _veil.Cover(VeilReason.Quality, 0.3f).WaitAsync(cancellationToken);
    }
    public bool OwnsQualityVeil => _veil.Opacity > 0 && !OtherVeil(_veil.Reason);
    public Task Reveal(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return OwnsQualityVeil ? _veil.Reveal(VeilReason.Quality, 0.3f).WaitAsync(cancellationToken) : Task.CompletedTask;
    }
    public GraphicsDevice ReadDevice() => DeviceProbe.Read();
    public void ReportError(string message) => GD.PushError(message);
    public void ReportInfo(string message) => GD.Print(message);
    public bool Booting => RuntimeKernel.Get<IAppFlow>().State switch
    {
        AppState.Boot => true,
        AppState.Title or AppState.Entering or AppState.Playing or AppState.Leaving or AppState.Quitting => false,
        _ => throw new InvalidOperationException("Unknown application state."),
    };
    public float RefreshRate => DisplayServer.ScreenGetRefreshRate(DisplayServer.WindowGetCurrentScreen());
    public Vector2I OutputSize => DisplayServer.WindowGetSize();
    public Vector2I ScreenSize => DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());

    public async Task WaitForDestination(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stream = RuntimeKernel.Get<IMapStreamState>();
        var camera = RuntimeKernel.Get<ICameraRig>();
        stream.Prefetch(camera.Pose, camera.Mode);
        var elapsed = 0d;
        while (!stream.DestinationReady && stream.Error is null && elapsed < 1.2)
            elapsed += await NextFrame(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (stream.Error is not null) throw new InvalidOperationException($"Quality destination failed: {stream.Error}");
        if (!stream.DestinationReady) throw new TimeoutException("Quality destination was not ready within 1.2 seconds.");
    }

    public void Dispatch(QualityView view, in QualityChange change)
    {
        _lifetime.ThrowIfCancellationRequested();
        var notification = change;
        var recoveryErrors = change.Origin == ApplyOrigin.Revert ? new List<Exception>() : null;
        var currentMap = RuntimeKernel.Get<IMapService>().Current.Value;
        var registrations = RuntimeKernel.Services.ForMap(currentMap).Where(entry => entry.Kind == "node")
            .Select(entry => (Entry: entry, Quality: entry.Type.GetCustomAttribute<QualityResponsiveAttribute>()))
            .Where(item => item.Quality is not null)
            .OrderBy(item => notification.Veiled ? OwnerOrder(item.Entry.Owner) : 0)
            .ThenBy(item => item.Entry.Id, StringComparer.Ordinal).ToArray();
        foreach (var item in registrations)
        {
            _lifetime.ThrowIfCancellationRequested();
            if (!item.Quality!.Owns.Concat(item.Quality.Reads).Any(notification.Has)) continue;
            if (RuntimeKernel.Services.Create(item.Entry.Type) is IQualityAware aware && aware is Node node && node.IsInsideTree())
            {
                try { aware.ApplyQuality(view, change); }
                catch (Exception error) when (recoveryErrors is not null) { recoveryErrors.Add(error); }
            }
        }
        try
        {
            _lifetime.ThrowIfCancellationRequested();
            RenderingServer.GlobalShaderParameterSet(ShaderGlobals.Quality, new Vector4(view[QualityKnob.DetailScale],
                view[QualityKnob.FoliageDensity], view[QualityKnob.GrassRadius], 0));
            RenderingServer.GlobalShaderParameterSet(ShaderGlobals.StepRate, (float)view.StepRate);
        }
        catch (Exception error) when (recoveryErrors is not null) { recoveryErrors.Add(error); }
        if (recoveryErrors is { Count: > 0 }) throw new AggregateException("Quality consumers failed to restore.", recoveryErrors);
    }

    public void ApplyQuality(QualityView view, in QualityChange change)
    {
        if (change.Has(QualityKnob.DisplayMode) && !_service.CaptureMode)
            GetWindow().Mode = view[QualityKnob.DisplayMode] switch
            {
                0 => Window.ModeEnum.Windowed,
                1 => Window.ModeEnum.Fullscreen,
                2 => Window.ModeEnum.ExclusiveFullscreen,
                _ => throw new InvalidOperationException("Invalid display mode."),
            };
        if (change.Has(QualityKnob.WindowSize) && view[QualityKnob.DisplayMode] == 0 && !_service.CaptureMode)
        {
            var size = QualityCatalog.WindowSizes[(int)view[QualityKnob.WindowSize]];
            DisplayServer.WindowSetSize(new(size.Width, size.Height));
        }
        if (change.Has(QualityKnob.RenderScale))
        {
            var viewport = GetViewport();
            viewport.Scaling3DMode = view[QualityKnob.RenderScale] < 1 ? Viewport.Scaling3DModeEnum.Fsr : Viewport.Scaling3DModeEnum.Bilinear;
            viewport.Scaling3DScale = view[QualityKnob.RenderScale];
        }
        if (change.Has(QualityKnob.FsrSharpness)) GetViewport().FsrSharpness = view[QualityKnob.FsrSharpness];
        if (change.Has(QualityKnob.VSync)) DisplayServer.WindowSetVsyncMode(view[QualityKnob.VSync] != 0 ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        if (change.Has(QualityKnob.FrameCap) || change.Has(QualityKnob.Vrr))
            Engine.MaxFps = QualityCatalog.FrameLimit(view[QualityKnob.FrameCap], view[QualityKnob.Vrr] != 0, RefreshRate);
    }

    public async Task<DeviceTier> Sample(GraphicsDevice device, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var viewport = GetViewport();
        var previousScale = viewport.Scaling3DScale;
        var previousMode = viewport.Scaling3DMode;
        var rid = viewport.GetViewportRid();
        RenderingServer.ViewportSetMeasureRenderTime(rid, true);
        var full = new List<double>();
        var reduced = new List<double>();
        var time = 0d;
        var reducedPhase = false;
        var discard = 3;
        try
        {
            viewport.Scaling3DMode = Viewport.Scaling3DModeEnum.Bilinear;
            viewport.Scaling3DScale = 1;
            while (time < 2.4)
            {
                time += await NextFrame(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (time >= 2.4) break;
                if (!reducedPhase && time >= 1.4)
                {
                    reducedPhase = true;
                    viewport.Scaling3DMode = Viewport.Scaling3DModeEnum.Fsr;
                    viewport.Scaling3DScale = 0.67f;
                    discard = 3;
                }
                if (discard-- > 0 || time < 0.4) continue;
                var milliseconds = RenderingServer.ViewportGetMeasuredRenderTimeGpu(rid);
                if (double.IsFinite(milliseconds) && milliseconds > 0) (reducedPhase ? reduced : full).Add(milliseconds);
            }
            var size = OutputSize;
            return DeviceProbe.FromSamples(device, full, reduced, (double)size.X * size.Y);
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                viewport.Scaling3DMode = previousMode;
                viewport.Scaling3DScale = previousScale;
            }
        }
    }

    private static int OwnerOrder(string owner) => owner switch
    {
        "WP-32" => 0,
        "WP-03" => 1,
        "WP-05" => 2,
        "WP-04" => 3,
        "WP-02" => 4,
        _ => 5,
    };

    private static bool OtherVeil(VeilReason reason) => reason switch
    {
        VeilReason.Quality => false,
        VeilReason.Boot or VeilReason.Transit or VeilReason.Teleport or VeilReason.Season or
            VeilReason.Respawn or VeilReason.Cinematic or VeilReason.MapTravel => true,
        _ => throw new ArgumentOutOfRangeException(nameof(reason)),
    };
}
