using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Startup;
using Campus.Kernel.Registry;
using RuntimeKernel = Campus.Kernel.Registry.Kernel;
using Campus.Kernel.Renderer;
using Campus.Kernel.Save.Format;
using Godot;

namespace Campus.Kernel.Save;

[KernelNode("save", Owner = "WP-50")]
public partial class SaveNode : Node, IBootParticipant
{
    private SaveService _save = null!;
    private IMapRuntime _mapRuntime = null!;
    private IVeilPresentation? _veilPresentation;
    private readonly CancellationTokenSource _exit = new();
    private TaskCompletionSource<byte[]?>? _capture;
    private IDisposable? _hiddenHud;
    private ulong _frame, _readbackAt, _drawFrame;
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
                    _veilPresentation = RuntimeKernel.Get<IVeilPresentation>();
                    var sections = RuntimeKernel.Services.Entries.Where(e => e.Kind == "save").Select(e =>
                        new SaveSectionRegistration(e.Type.GetCustomAttributes<SaveSectionAttribute>()
                            .Single(a => a.Id == e.Id && a.Scope == e.Scope),
                            (ISaveSection)RuntimeKernel.Services.Create(e.Type))).ToArray();
                    var bindings = new SaveRuntimeBindings(RuntimeKernel.Get<IWorldState>(), RuntimeKernel.Get<IMapService>(), RuntimeKernel.Get<ICameraRig>(),
                        RuntimeKernel.Get<IObserverState>(), RuntimeKernel.Get<IRuleset>(), RuntimeKernel.Get<IAtlasService>(), RuntimeKernel.Get<IVeilState>(),
                        () => RuntimeKernel.Get<IAppFlow>().State, id => RuntimeKernel.Get<IRulesetActivation>().Select(id),
                        RuntimeKernel.Get<IWorldRestore>(), WaitDestination, CaptureThumbnail,
                        () => (long)Time.GetUnixTimeFromSystem());
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
        if (_capture is null || _frame < _readbackAt) return;
        var completion = _capture; _capture = null;
        Image? image = null;
        try
        {
            if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Minimized)
                image = GetViewport().GetTexture().GetImage();
        }
        catch (Exception) { /* Thumbnail failure must never prevent saving state. */ }
        finally { _hiddenHud?.Dispose(); _hiddenHud = null; }
        if (image is null) { completion.TrySetResult(null); return; }
        _ = EncodeThumbnail(image, completion);
    }

    private Task<byte[]?> CaptureThumbnail()
    {
        if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Minimized) return Task.FromResult<byte[]?>(null);
        if (_capture is not null) return _capture.Task;
        _capture = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _hiddenHud = CapturePresentation.HideHud();
        _readbackAt = _frame + 1;
        return _capture.Task;
    }

    private static async Task EncodeThumbnail(Image image, TaskCompletionSource<byte[]?> completion)
    {
        try
        {
            var result = await Task.Run(() =>
            {
                using (image)
                {
                    var width = image.GetWidth(); var height = image.GetHeight();
                    if (width < 1 || height < 1) return null;
                    var cropWidth = Math.Min(width, height * 16 / 9);
                    var cropHeight = Math.Min(height, width * 9 / 16);
                    using var cropped = image.GetRegion(new Rect2I((width - cropWidth) / 2, (height - cropHeight) / 2, cropWidth, cropHeight));
                    while (cropped.GetWidth() > 768) cropped.ShrinkX2();
                    cropped.Resize(384, 216, Image.Interpolation.Lanczos);
                    var bytes = cropped.SaveWebpToBuffer(true, .8f);
                    return SaveContainer.ValidateThumbnail(bytes) ? bytes : null;
                }
            }).ConfigureAwait(false);
            completion.TrySetResult(result);
        }
        catch (Exception) { completion.TrySetResult(null); }
    }

    private async Task WaitDestination(Pose pose, NavSpace? space, ViewMode mode)
    {
        // Flow owns the actual cover and keeps it drawn through this whole restoration barrier.
        // Pose was restored in this Save frame, after Camera's process step. Its same-frame draw is insufficient.
        var poseFrame = _frame;
        var cancellation = _exit.Token;
        var readinessWait = new SaveReadinessObservation();
        try
        {
            var arrival = await InSaveFrame(() =>
            {
                // Boot has one observation spanning every startup dependency, with the frontispiece policy.
                if (RuntimeKernel.Get<IAppFlow>().State != AppState.Boot)
                    readinessWait.Attach(RuntimeKernel.Get<IVeilService>().ObserveReadinessWait());
                return _mapRuntime.Arrive(pose, mode, space);
            }, cancellation).ConfigureAwait(false);
            while (!await InSaveFrame(() =>
            {
                if (arrival.Done.IsCompleted) arrival.Done.GetAwaiter().GetResult();
                if (_drawFrame <= poseFrame || !_drawCovered || _veilPresentation?.IsCovered != true) return false;
                return true;
            }, cancellation).ConfigureAwait(false)) { }
            await arrival.Done.WaitAsync(cancellation).ConfigureAwait(false);
            await InSaveFrame(() =>
            {
                if (!ReferenceEquals(_mapRuntime.Arrival, arrival)) throw new OperationCanceledException("The save destination was replaced.");
                return true;
            }, cancellation).ConfigureAwait(false);
        }
        finally { readinessWait.Dispose(); }
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
        _drawCovered = _veilPresentation?.IsCovered == true;
    }

    public override void _ExitTree()
    {
        _exiting = true;
        RenderingServer.FramePostDraw -= OnFramePostDraw;
        _exit.Cancel();
        _hiddenHud?.Dispose(); _capture?.TrySetResult(null); _save?.Dispose();
        _exit.Dispose();
    }
}
