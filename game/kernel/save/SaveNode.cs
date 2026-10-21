using Campus.Kernel.Diagnostics;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Startup;
using Campus.Kernel.Registry;
using RuntimeKernel = Campus.Kernel.Registry.Kernel;
using Campus.Kernel.Save.Format;
using Godot;

namespace Campus.Kernel.Save;

[KernelNode("save")]
public partial class SaveNode : Node, IBootParticipant
{
    private SaveService _save = null!;
    private IMapRuntime _mapRuntime = null!;
    private IAppLoadingPresentation? _loading;
    private bool PresentedCover => _loading?.IsCovered == true;
    private readonly CancellationTokenSource _exit = new();
    private ulong _frame, _drawFrame;
    private bool _drawCovered, _exiting;
    public string Id => "save";
    public int Order => 50;

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Save;
        _save = RuntimeKernel.Get<SaveService>();
        RenderingServer.FramePostDraw += OnFramePostDraw;
    }

    public async Task Initialize(BootPhase phase, BootContext context)
    {
        switch (phase)
        {
            case BootPhase.BeforeMap:
                {
                    var save = context.Run.Save;
                    _mapRuntime = RuntimeKernel.Get<IMapRuntime>();
                    _loading = RuntimeKernel.Get<IAppLoadingPresentation>();
                    var sections = RuntimeKernel.Services.Query<ISaveSection>(RegistrationKind.Save).Select(e =>
                        new SaveSectionRegistration(e.Type.GetCustomAttributes<SaveSectionAttribute>()
                            .Single(a => a.Id == e.Id && a.Scope.ToString() == e.Scope),
                            (ISaveSection)RuntimeKernel.Services.Create(e.Type))).ToArray();
                    var bindings = new SaveRuntimeBindings(RuntimeKernel.Get<IWorldState>(), RuntimeKernel.Get<IMapService>(), RuntimeKernel.Get<ICameraRig>(),
                        RuntimeKernel.Get<IObserverState>(), RuntimeKernel.Get<IRuleset>(), RuntimeKernel.Get<IAtlasService>(), RuntimeKernel.Get<IAppLoadingPresentation>(),
                        RuntimeKernel.Get<IAppFlow>(), id => RuntimeKernel.Get<IRulesetActivation>().Select(id),
                        RuntimeKernel.Get<IWorldRestore>(), WaitDestination, CaptureThumbnail,
                        () => (long)Time.GetUnixTimeFromSystem(), pose => RuntimeKernel.Get<ICameraPoseValidator>().LegalizePrepared(pose));
                    await _save.Initialize(new(context.SaveDirectory, context.Build, save.Enabled, save.WriteSlot, save.AtFrame, save.WeatherSeed), bindings, new(sections));
                    break;
                }
            case BootPhase.AfterMapWarm: break;
            case BootPhase.BeforeReveal: _save.WarmSerialization(); break;
            default: throw new ArgumentOutOfRangeException(nameof(phase));
        }
    }

    public override void _Process(double delta)
    {
        _frame++;
        _save.Tick(delta);
    }

    private async Task<byte[]?> CaptureThumbnail()
    {
        var bytes = await RuntimeKernel.Get<IGameCapture>().CaptureThumbnailAsync(_exit.Token);
        return bytes is not null && SaveContainer.ValidateThumbnail(bytes) ? bytes : null;
    }

    private async Task WaitDestination(Pose pose, NavSpace? space, ViewMode mode)
    {
        // Flow owns the actual cover and keeps it drawn through this whole restoration barrier.
        // Pose was restored in this Save frame, after Camera's process step. Its same-frame draw is insufficient.
        var poseFrame = _frame;
        var cancellation = _exit.Token;
        var identity = await InSaveFrame(() => (RuntimeKernel.Get<IWorldState>().Instance,
            RuntimeKernel.Get<IMapService>().Current,
            Body: mode == ViewMode.Walk ? RuntimeKernel.Get<IWalkController>() : null), cancellation).ConfigureAwait(false);
        var body = identity.Body;
        Task<ModeSwitchResult>? flyModeSwitch = null;
        var arrival = await InSaveFrame(() =>
        {
            if (body is not null) return body.PlacementArrival;
            return _mapRuntime.Arrive(pose, mode, space);
        }, cancellation).ConfigureAwait(false);
        while (!await InSaveFrame(() =>
        {
            if (RuntimeKernel.Get<IWorldState>().Instance != identity.Instance
                || RuntimeKernel.Get<IMapService>().Current != identity.Current)
                throw new OperationCanceledException("The restore world or map was replaced.");
            if (arrival is null)
            {
                arrival = body?.PlacementArrival;
                if (arrival is null && body?.ExitRequested != true) return false;
            }
            if (arrival is not null && !ReferenceEquals(_mapRuntime.Arrival, arrival))
            {
                var repair = body?.PlacementArrival;
                if (mode != ViewMode.Walk || repair is null || !ReferenceEquals(_mapRuntime.Arrival, repair))
                    throw new OperationCanceledException("The save destination was replaced.");
                arrival = repair;
            }
            if (body?.ExitRequested == true)
            {
                flyModeSwitch ??= RuntimeKernel.Get<IViewControl>().SetMode(ViewMode.Fly, ModeSwitchVia.Ui);
                if (!flyModeSwitch.IsCompleted) return false;
                if (flyModeSwitch.GetAwaiter().GetResult() == ModeSwitchResult.Refused)
                    throw new InvalidOperationException("Invalid walking save could not return to fly mode.");
                body = null;
                mode = ViewMode.Fly;
                arrival = _mapRuntime.Arrive(RuntimeKernel.Get<ICameraRig>().Pose, mode);
                DiagnosticLog.Warning("", "walk: no safe landing within range; restored in fly mode.");
            }
            if (arrival is null) return false;
            if (arrival.Done.IsCompleted) arrival.Done.GetAwaiter().GetResult();
            if (_drawFrame <= poseFrame || !_drawCovered || !PresentedCover) return false;
            return arrival.Done.IsCompletedSuccessfully && (body is null || body.CollisionReady);
        }, cancellation).ConfigureAwait(false)) { }
        await InSaveFrame(() =>
        {
            if (!ReferenceEquals(_mapRuntime.Arrival, arrival)
                || RuntimeKernel.Get<IWorldState>().Instance != identity.Instance
                || RuntimeKernel.Get<IMapService>().Current != identity.Current)
                throw new OperationCanceledException("The save destination was replaced.");
            return true;
        }, cancellation).ConfigureAwait(false);
    }

    private Task<T> InSaveFrame<T>(Func<T> action, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        return _save.OnFrame(() => { cancellation.ThrowIfCancellationRequested(); return action(); }).WaitAsync(cancellation);
    }

    private void OnFramePostDraw()
    {
        if (_exiting) return;
        _drawFrame = _frame;
        _drawCovered = PresentedCover;
    }

    public override void _ExitTree()
    {
        _exiting = true;
        _save.StopNewWork();
        RenderingServer.FramePostDraw -= OnFramePostDraw;
        _exit.Cancel();
        _exit.Dispose();
    }
}
