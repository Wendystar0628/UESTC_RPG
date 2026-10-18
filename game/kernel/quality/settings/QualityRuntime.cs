using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
using RuntimeKernel = Campus.Kernel.Registry.Kernel;
using Godot;

namespace Campus.Kernel.Quality.Settings;

[KernelNode("quality", Owner = "WP-32")]
[QualityResponsive(Owns = [QualityKnob.DisplayMode, QualityKnob.WindowSize, QualityKnob.VSync,
    QualityKnob.FrameCap, QualityKnob.RenderScale])]
public partial class QualityRuntime : Node, IQualityAware, IQualityRuntime
{
    // Fixed style defaults written into the global quality vector (SYS-GRAPHICS): detail distance factor and grass radius.
    private const float DetailScale = 0.8f;
    private const float GrassRadius = 20f;

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
        ApplyStepRate(_service.Effective);
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
                _veil.Opacity > 0;
        }
    }

    public IVeilLease Cover(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _veil.Cover(VeilReason.Quality, QualityService.VeilSeconds);
    }
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
        var runtime = RuntimeKernel.Get<IMapRuntime>();
        var camera = RuntimeKernel.Get<ICameraRig>();
        var arrival = runtime.Arrive(camera.Pose, camera.Mode, RuntimeKernel.Get<IObserverState>().Space);
        try
        {
            var elapsed = 0d;
            while (!arrival.Done.IsCompleted && elapsed < 1.2)
                elapsed += await NextFrame(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!arrival.Done.IsCompleted) throw new TimeoutException("Quality destination was not ready within 1.2 seconds.");
            await arrival.Done;
            if (!ReferenceEquals(runtime.Arrival, arrival)) throw new OperationCanceledException("The quality destination was replaced.");
        }
        finally
        {
            if (!arrival.Done.IsCompleted) arrival.Cancel();
        }
    }

    public void Dispatch(QualityView view, in QualityChange change)
    {
        _lifetime.ThrowIfCancellationRequested();
        var notification = change;
        // Every consumer is attempted so a failed apply or its rollback leaves no consumer behind.
        var errors = new List<Exception>();
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
                catch (Exception error) { errors.Add(error); }
            }
        }
        _lifetime.ThrowIfCancellationRequested();
        try
        {
            RenderingServer.GlobalShaderParameterSet(ShaderGlobals.Quality, new Vector4(DetailScale,
                view[QualityKnob.FoliageDensity], GrassRadius, 0));
            ApplyStepRate(view);
        }
        catch (Exception error) { errors.Add(error); }
        if (errors.Count > 0) throw new AggregateException("Quality consumers failed.", errors);
    }

    private static void ApplyStepRate(QualityView view) =>
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.StepRate, (float)view.StepRate);

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
        if (change.Has(QualityKnob.VSync)) DisplayServer.WindowSetVsyncMode(view[QualityKnob.VSync] != 0 ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        if (change.Has(QualityKnob.FrameCap))
            Engine.MaxFps = QualityCatalog.FrameLimit(view[QualityKnob.FrameCap], RefreshRate);
    }

    private static int OwnerOrder(string owner) => owner switch
    {
        "WP-32" => 0,
        "WP-03" => 1,
        "WP-05" => 2,
        _ => 3,
    };

}
