using Campus.Kernel.Diagnostics;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using RuntimeKernel = Campus.Kernel.Registry.Kernel;
using Godot;

namespace Campus.Kernel.Quality.Settings;

[KernelNode("quality")]
[QualityResponsive(Order = 0, Owns = [QualityKnob.DisplayMode, QualityKnob.WindowSize, QualityKnob.VSync,
    QualityKnob.FrameRateLimit, QualityKnob.RenderScale, QualityKnob.FoliageDensity])]
public partial class QualityRuntime : Node, IQualityAware, IQualityRuntime
{
    // Fixed detail distance factor; grass range and transition duration come from the preset catalog.
    private const float DetailScale = 0.8f;
    private const double DestinationWaitSeconds = 1.2;

    private IQualityFrame _service = null!;
    private IAppLoadingPresentation _screen = null!;
    private IWorldState _world = null!;
    private Vector4 _quality;
    private Vector4 _qualityFrom;
    private Vector4 _qualityTarget;
    private double _qualityElapsed;
    private bool _qualityInitialized;
    private CancellationToken _lifetime;
    private bool _closed;
    private List<TaskCompletionSource<double>> _frames = [];
    public override void _Ready()
    {
        ProcessPriority = FramePriority.Quality;
        _service = RuntimeKernel.Get<IQualityFrame>();
        _screen = RuntimeKernel.Get<IAppLoadingPresentation>();
        _world = RuntimeKernel.Get<IWorldState>();
        _world.InstanceChanged += OnWorldInstanceChanged;
        _closed = false;
        _qualityInitialized = false;
        _lifetime = _service.Bind(this);
    }

    public override void _ExitTree()
    {
        _closed = true;
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        _service?.Unbind(this);
        foreach (var frame in _frames) frame.TrySetCanceled();
        _frames.Clear();
    }

    public override void _Process(double delta)
    {
        if (_closed) return;
        _service.Tick(delta);
        if (_qualityInitialized && _quality != _qualityTarget)
        {
            _qualityElapsed += Math.Max(0, delta);
            var weight = (float)Math.Min(1, _qualityElapsed / _service.FoliageTransitionSeconds);
            _quality = weight >= 1 ? _qualityTarget : _qualityFrom.Lerp(_qualityTarget, weight);
            RenderingServer.GlobalShaderParameterSet(ShaderGlobals.Quality, _quality);
        }
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
            var flow = RuntimeKernel.Get<IAppFlow>();
            var app = flow.State;
            var appBusy = app switch
            {
                AppState.Boot or AppState.Entering or AppState.Leaving or AppState.Quitting => true,
                AppState.Title or AppState.Playing => false,
                _ => throw new ArgumentOutOfRangeException(nameof(app)),
            };
            return appBusy || flow.Activity != AppActivity.None || _screen.Active;
        }
    }

    public IAppLoadingLease ShowLoading(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _screen.Show();
    }
    public void ReportError(string context, Exception error) => DiagnosticLog.Error(context, error);
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
        // SYS-TITLE has no world destination, even when a previous map/camera remains resident.
        if (_world.Instance == 0)
        {
            await FrameAwaiter.ReadyDrawAsync(this, static () => true,
                TimeSpan.FromSeconds(DestinationWaitSeconds), cancellationToken);
            return;
        }
        var runtime = RuntimeKernel.Get<IMapRuntime>();
        var camera = RuntimeKernel.Get<ICameraRig>();
        var arrival = runtime.Arrive(camera.Pose, camera.Mode, RuntimeKernel.Get<IObserverState>().Space);
        try
        {
            var elapsed = 0d;
            while (!arrival.Done.IsCompleted && elapsed < DestinationWaitSeconds)
                elapsed += await NextFrame(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!arrival.Done.IsCompleted) throw new TimeoutException($"Quality destination was not ready within {DestinationWaitSeconds} seconds.");
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
        // Global quality initializes before map foundation; only a mounted map owns scoped consumers.
        var currentMap = RuntimeKernel.Get<IMapService>().Context?.Descriptor.Id;
        var registrations = RuntimeKernel.Services.Query<IQualityAware>(RegistrationKind.Node, currentMap)
            .Where(entry => currentMap.HasValue || entry.Map is null)
            .Select(entry => (Entry: entry, Quality: entry.Type.GetCustomAttribute<QualityResponsiveAttribute>()))
            .Where(item => item.Quality is not null)
            .OrderBy(item => notification.Loading ? item.Quality!.Order : 0)
            .ThenBy(item => item.Entry.Id, StringComparer.Ordinal).ToArray();
        foreach (var item in registrations)
        {
            _lifetime.ThrowIfCancellationRequested();
            if (!item.Quality!.Owns.Concat(item.Quality.Reads).Any(notification.Has)) continue;
            if (RuntimeKernel.Services.Create(item.Entry.Type, currentMap) is IQualityAware aware && aware is Node node && node.IsInsideTree())
            {
                try { aware.ApplyQuality(view, change); }
                catch (Exception error) { errors.Add(error); }
            }
        }
        _lifetime.ThrowIfCancellationRequested();
        try
        {
            SetVegetationQuality(view, change);
        }
        catch (Exception error) { errors.Add(error); }
        if (errors.Count > 0) throw new AggregateException("Quality consumers failed.", errors);
    }

    private void SetVegetationQuality(QualityView view, in QualityChange change)
    {
        var target = new Vector4(DetailScale, view[QualityKnob.FoliageDensity], _service.GrassRadius, 0);
        if (!_qualityInitialized || change.Origin == ApplyOrigin.Startup || Booting)
        {
            _qualityInitialized = true;
            _quality = _qualityFrom = _qualityTarget = target;
            RenderingServer.GlobalShaderParameterSet(ShaderGlobals.Quality, _quality);
        }
        else if (_qualityTarget != target)
        {
            _qualityFrom = _quality;
            _qualityTarget = target;
            _qualityElapsed = 0;
        }
    }

    private void OnWorldInstanceChanged(long _)
    {
        if (!_qualityInitialized) return;
        _quality = _qualityFrom = _qualityTarget;
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.Quality, _quality);
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
        if (change.Has(QualityKnob.VSync)) DisplayServer.WindowSetVsyncMode(view[QualityKnob.VSync] != 0 ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        if (change.Has(QualityKnob.FrameRateLimit))
            Engine.MaxFps = QualityCatalog.FrameLimit(view[QualityKnob.FrameRateLimit], RefreshRate);
    }

}
