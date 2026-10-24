using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>Owns capture presentation, viewport readback and image export for every game consumer.</summary>
[KernelService(typeof(IGameCapture))]
[KernelService(typeof(GameCaptureNode))]
[KernelNode("game-capture")]
public partial class GameCaptureNode : Node, IGameCapture, IRegistryExitParticipant
{
    /// <summary>The exact prepared request; a moving Walk eye is presentation, not destination identity.</summary>
    public sealed record CaptureDestination(string RequestId, ViewMode Mode, IMapContext Context,
        long WorldInstance, IMapArrival Arrival, Pose FixedPose)
    {
        internal Pose ReadCurrentPose(IMapContext? context, long worldInstance, ICameraRig rig,
            IMapRuntime runtime, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (runtime.Failure is { } failure)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            if (Arrival.Done.IsCompleted) Arrival.Done.GetAwaiter().GetResult();
            if (!ReferenceEquals(context, Context) || worldInstance != WorldInstance || rig.Mode != Mode ||
                !ReferenceEquals(runtime.Arrival, Arrival))
                throw new OperationCanceledException("Capture destination was replaced.");
            var livePose = rig.Pose;
            if (Mode switch
                {
                    ViewMode.Fly => livePose != FixedPose,
                    ViewMode.Walk => false,
                    _ => throw new ArgumentOutOfRangeException(nameof(Mode))
                })
                throw new OperationCanceledException("Capture fixed view was replaced.");
            return livePose;
        }
    }

    // Win32 HRESULTs reported by CreateNew when the destination already exists.
    private const int FileExists = unchecked((int)0x80070050);
    private const int AlreadyExists = unchecked((int)0x800700B7);
    private IAppLoadingPresentation _loading = null!;
    private readonly CancellationTokenSource _exit = new();
    // Retain only work in flight; completed results and failures belong to the capture caller.
    private Task? _captureTask;
    private IDisposable? _hiddenHud;
    private string? _path;
    private string _diagnosticId = null!;
    private bool _busy, _exiting;
    private PerformanceRecordingService _performance = null!;

    public override void _Ready()
    {
        _loading = KernelServices.Get<IAppLoadingPresentation>();
        _performance = KernelServices.Get<PerformanceRecordingService>();
        _diagnosticId = KernelServices.Services.Entries.Single(entry =>
            entry.Kind == RegistrationKind.Node && entry.Type == GetType()).Id;
    }

    public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default)
    {
        RequireMainThread();
        if (!Available() || cancellationToken.IsCancellationRequested)
            return Task.FromResult(new CaptureResult(CaptureStatus.Unavailable, null));
        if (_busy) return Task.FromResult(new CaptureResult(CaptureStatus.Busy, _path));
        _busy = true;
        _path = null;
        var capture = CapturePhotoAsync(request, cancellationToken);
        _captureTask = capture.IsCompleted ? null : capture;
        return capture;
    }

    private async Task<CaptureResult> CapturePhotoAsync(CaptureRequest request, CancellationToken cancellationToken)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(_exit.Token, cancellationToken);
        try
        {
            _path = request.OutputPath ?? Path.Combine(OS.GetUserDataDir(), "photos", $"photo-{Time.GetTicksUsec()}.png");
            if (!Path.IsPathFullyQualified(_path)) throw new ArgumentException("Capture output must be an absolute filename.");
            var path = await SaveFrameAsync(_path, request.HideUi, request.OutputPath is null, lifetime.Token);
            return new(CaptureStatus.Saved, path);
        }
        catch (OperationCanceledException) { return new(CaptureStatus.Unavailable, _path); }
        finally { EndCapture(); }
    }

    public Task<byte[]?> CaptureThumbnailAsync(CancellationToken cancellationToken = default)
    {
        RequireMainThread();
        if (_busy || !Available() || _loading.Active || cancellationToken.IsCancellationRequested) return Task.FromResult<byte[]?>(null);
        _busy = true;
        _path = null;
        var capture = CaptureThumbnailCore(cancellationToken);
        _captureTask = capture.IsCompleted ? null : capture;
        return capture;
    }

    private async Task<byte[]?> CaptureThumbnailCore(CancellationToken cancellationToken)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(_exit.Token, cancellationToken);
        try
        {
            var image = await ReadFrameAsync("saveThumbnail", hideUi: true, lifetime.Token, () =>
            {
                if (_loading.Active) throw new OperationCanceledException("A thumbnail cannot replace the loading cover.");
                return true;
            });
            // Only the detached Image belongs to the worker. Viewport, HUD and tree stay on the main thread.
            var encoding = Task.Run(() =>
            {
                using (image)
                {
                    lifetime.Token.ThrowIfCancellationRequested();
                    var width = image.GetWidth(); var height = image.GetHeight();
                    var cropWidth = Math.Min(width, height * 16 / 9);
                    var cropHeight = Math.Min(height, width * 9 / 16);
                    using var cropped = image.GetRegion(new Rect2I((width - cropWidth) / 2, (height - cropHeight) / 2, cropWidth, cropHeight));
                    while (cropped.GetWidth() > 768) cropped.ShrinkX2();
                    cropped.Resize(384, 216, Image.Interpolation.Lanczos);
                    var bytes = cropped.SaveWebpToBuffer(true, .8f);
                    lifetime.Token.ThrowIfCancellationRequested();
                    return bytes;
                }
            });
            return await encoding;
        }
        catch (OperationCanceledException) { return null; }
        finally { EndCapture(); }
    }

    private bool Available() => !_exiting && IsInsideTree() && IsNodeReady()
        && DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Minimized;

    private Task WaitForFrameAsync(Func<bool>? ready, CancellationToken cancellationToken) =>
        FrameAwaiter.ReadyDrawAsync(this, () => Available() && (ready?.Invoke() ?? true),
            TimeSpan.FromSeconds(BootTuning.StalledSeconds), cancellationToken);

    private async Task<Image> ReadFrameAsync(string purpose, bool hideUi, CancellationToken cancellationToken, Func<bool>? ready = null)
    {
        HideUi(hideUi);
        try
        {
            await WaitForFrameAsync(ready, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            ulong frame = Engine.GetProcessFrames();
            _performance.Readback("begin", purpose, frame);
            Image image;
            try { image = GetViewport().GetTexture().GetImage(); }
            finally { _performance.Readback("end", purpose, frame); }
            if (!image.IsEmpty())
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (ready?.Invoke() == false) throw new InvalidOperationException("Capture content changed during viewport readback.");
                    return image;
                }
                catch { image.Dispose(); throw; }
            }
            image.Dispose();
            throw new IOException("The capture viewport returned an empty image.");
        }
        finally { RestoreUi(); }
    }

    private async Task<string> SaveFrameAsync(string path, bool hideUi, bool unique, CancellationToken cancellationToken,
        Func<bool>? ready = null)
    {
        var image = await ReadFrameAsync("photo", hideUi, cancellationToken, ready);
        var encoding = Task.Run(() =>
        {
            using (image)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytes = image.SavePngToBuffer();
                if (bytes.Length == 0) throw new IOException("Capture PNG encoding failed.");
                return WritePhoto(bytes, path, unique, cancellationToken);
            }
        });
        return await encoding;
    }

    private static string WritePhoto(byte[] bytes, string requestedPath, bool unique, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(Path.GetDirectoryName(requestedPath)!);
        var path = requestedPath;
        var suffix = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FileStream stream;
            try { stream = new(path, unique ? FileMode.CreateNew : FileMode.Create, System.IO.FileAccess.Write, FileShare.Read); }
            catch (IOException error) when (unique && error.HResult is FileExists or AlreadyExists)
            {
                path = NextPhotoPath(error, requestedPath, ++suffix);
                continue;
            }
            try { using (stream) stream.Write(bytes); }
            catch (Exception writeError)
            {
                try { File.Delete(path); }
                catch (Exception cleanupError)
                {
                    throw new AggregateException("Photo export and partial file cleanup failed.", writeError, cleanupError);
                }
                throw;
            }
            return path;
        }
    }

    // UI-NAV requires an occupied photo name to select the next suffix, never overwrite the existing photo.
    private static string NextPhotoPath([FailureHandler("UI-NAV")] IOException error, string requestedPath, int suffix) =>
        Path.ChangeExtension(requestedPath, null) + $"-{suffix}.png";

    private void HideUi(bool hideUi)
    {
        if (hideUi) _hiddenHud = CapturePresentation.HideHud();
    }

    private void RestoreUi()
    {
        _hiddenHud?.Dispose();
        _hiddenHud = null;
    }

    private void EndCapture()
    {
        try { RestoreUi(); }
        finally
        {
            _busy = false;
            _path = null;
            _captureTask = null;
        }
    }

    private static void RequireMainThread()
    {
        if (!GodotThread.IsMainThread()) throw new InvalidOperationException("Capture begins on the Godot main thread.");
    }

    public void PrepareForExit() => StopCapture();

    private void StopCapture()
    {
        if (_exiting) return;
        _exiting = true;
        _exit.Cancel();
        RestoreUi();
    }

    public async Task DrainForExitAsync()
    {
        if (_captureTask is not { } capture) return;
        // Every retained capture was returned to its caller. Observe that result only after its
        // complete lifetime; StopCapture's own cancellation and UI-release failures stay with exit.
        await capture.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing |
            ConfigureAwaitOptions.ContinueOnCapturedContext);
    }

    public override void _ExitTree()
    {
        try { StopCapture(); }
        finally { _exit.Dispose(); }
    }

    /// <summary>Read the presented title UI without creating or preparing a world destination.</summary>
    public Task CaptureStaticUiAsync(Node host, CancellationToken cancellationToken = default)
    {
        RequireMainThread();
        if (_busy) throw new InvalidOperationException("Another game capture is in progress.");
        if (!Available()) throw new InvalidOperationException("The game capture service is not available.");
        var destination = AppBoot.Options.Capture.Output ?? throw new ArgumentException("--out must be an absolute directory.");
        if (!Path.IsPathFullyQualified(destination)) throw new ArgumentException("--out must be an absolute directory.");
        _busy = true;
        _path = null;
        var capture = CaptureStaticUiCore(host, destination, cancellationToken);
        _captureTask = capture.IsCompleted ? null : capture;
        return capture;
    }

    private async Task CaptureStaticUiCore(Node host, string destination, CancellationToken cancellationToken)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(_exit.Token, cancellationToken);
        try
        {
            Directory.CreateDirectory(destination);
            _path = Path.Combine(destination, "ui.png");
            await SaveFrameAsync(_path, hideUi: false, unique: false, lifetime.Token, Ready);
            GD.Print($"[{_diagnosticId}][phase=capture] ui {_path}");
        }
        finally { EndCapture(); }

        bool Ready()
        {
            if (!host.IsInsideTree()) throw new OperationCanceledException("Capture stage left the tree.");
            return !_loading.Active;
        }
    }

    /// <summary>Every fixed destination uses the same service owner, presentation and image export as F12.</summary>
    public Task RunSequenceAsync(Node host, ViewContentReadiness producers, BootRunOptions options,
        CaptureDestination? startupCapture, Action syncCamera, Func<bool> contentReady, CancellationToken cancellationToken = default,
        Action<Pose>? validatePose = null)
    {
        RequireMainThread();
        if (_busy) throw new InvalidOperationException("Another game capture is in progress.");
        if (!Available()) throw new InvalidOperationException("The game capture service is not available.");
        var destination = options.Capture.Output ?? throw new ArgumentException("--out must be an absolute directory.");
        if (!Path.IsPathFullyQualified(destination)) throw new ArgumentException("--out must be an absolute directory.");
        _busy = true;
        _path = null;
        var capture = RunSequenceCore(host, producers, options, startupCapture, syncCamera, contentReady,
            destination, cancellationToken, validatePose);
        _captureTask = capture.IsCompleted ? null : capture;
        return capture;
    }

    private async Task RunSequenceCore(Node host, ViewContentReadiness producers, BootRunOptions options,
        CaptureDestination? startupCapture, Action syncCamera, Func<bool> contentReady, string destination,
        CancellationToken cancellationToken, Action<Pose>? validatePose)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(_exit.Token, cancellationToken);
        try
        {
            var context = KernelServices.Get<IMapService>().Context ?? throw new InvalidOperationException("Capture requires a loaded map.");
            var worldInstance = KernelServices.Get<IWorldState>().Instance;
            Directory.CreateDirectory(destination);
            var shots = AppBoot.FixedShots.Count > 0 ? AppBoot.FixedShots
                : options.Capture.Shots.Select(id => new FixedShot(id, id, options)).ToArray();
            if (options.Capture.MovieFrames is not null)
            {
                if (shots.Count != 1) throw new ArgumentException("Movie Maker captures exactly one fixed pose per process.");
                MovieCapture.RequireMovieMaker();
            }
            foreach (var shot in shots)
                worldInstance = await CaptureShotAsync(host, producers, shot, destination, context, worldInstance,
                    AppBoot.FixedShots.Count == 0 && shots.Count == 1 ? startupCapture : null,
                    syncCamera, contentReady, validatePose, lifetime.Token);
        }
        finally { EndCapture(); }
    }

    private async Task<long> CaptureShotAsync(Node host, ViewContentReadiness producers, FixedShot shot,
        string destination, IMapContext context, long worldInstance, CaptureDestination? startupCapture,
        Action syncCamera, Func<bool> contentReady,
        Action<Pose>? validatePose, CancellationToken cancellationToken)
    {
        var rig = KernelServices.Get<ICameraRig>();
        var runtime = KernelServices.Get<IMapRuntime>();
        var maps = KernelServices.Get<IMapService>();
        var world = KernelServices.Get<IWorldState>();
        var started = FrameClock.MonotonicMilliseconds;
        var previousPhase = started;
        LogPhase("start");
        cancellationToken.ThrowIfCancellationRequested();
        var mode = shot.Options.World.ViewMode ?? startupCapture?.Mode ?? rig.Mode;
        var capture = startupCapture is { } startup && startup.RequestId == shot.Pose && startup.Mode == mode
            ? startup : null;
        if (capture is null && (mode != ViewMode.Fly || rig.Mode != ViewMode.Fly))
            throw new NotSupportedException("Walk capture requires the matching prepared startup request; live walking relocation is not supported.");
        if (capture is not null) ValidateDestination();
        var pose = ReviewPoseResolver.Resolve(shot.Pose, context);
        await using var cover = _loading.Show();
        await cover.Covered;
        cancellationToken.ThrowIfCancellationRequested();
        _ = await KernelServices.Get<ICameraPoseValidator>().LegalizePrepared(pose, cancellationToken);
        validatePose?.Invoke(pose);
        var terrainPoint = new System.Numerics.Vector2(pose.Position.X, pose.Position.Z);
        await KernelServices.Get<ITerrainPreparation>().PreparePoints(context, [terrainPoint], cancellationToken);
        LogPhase("terrain-ready");
        if (!host.IsInsideTree() || !ReferenceEquals(maps.Context, context) || world.Instance != worldInstance || rig.Mode != mode)
            throw new OperationCanceledException("Capture stage or view was replaced during preparation.");
        if (capture is null)
        {
            var appliedPose = ApplyPose(pose, shot.Options, context, rig, syncCamera);
            // Fixed rows commit a fresh world; bind the destination to that completed preparation.
            capture = new(shot.Pose, mode, context, world.Instance, BeginArrival(context, appliedPose, runtime, rig), appliedPose);
        }
        // A startup handoff already placed the real body; each capture prepares only its current view.
        producers.PrepareCapture(ValidateDestination());
        var arrival = capture.Arrival;
        void ReportProgress() => cover.Report(arrival.Completed, arrival.Total);
        arrival.ProgressChanged += ReportProgress;
        try
        {
            ReportProgress();
            LogPhase("pose-applied");
            await producers.WaitForReadyAsync(host, arrival, () => { ValidateDestination(); return contentReady(); }, cancellationToken);
            ValidateDestination();
            LogPhase("destination-ready");
            cover.Report(arrival.Total, arrival.Total);
            await cover.Hide();
            await CaptureBarrier.WaitForPresentedAsync(host, ReadyForShot, cancellationToken);
            LogPhase("scene-presented");
        }
        finally { arrival.ProgressChanged -= ReportProgress; }
        cancellationToken.ThrowIfCancellationRequested();
        await CaptureOutputAsync(shot, destination, ReadyForShot, ValidateDestination, LogPhase, cancellationToken);
        return capture.WorldInstance;

        bool ReadyForShot()
        {
            var livePose = ValidateDestination();
            return contentReady() && arrival.Done.IsCompletedSuccessfully && producers.IsReady(livePose);
        }

        Pose ValidateDestination()
        {
            if (!host.IsInsideTree()) throw new OperationCanceledException("Capture stage left the tree.");
            return capture!.ReadCurrentPose(maps.Context, world.Instance, rig, runtime, cancellationToken);
        }

        void LogPhase(string stage)
        {
            var now = FrameClock.MonotonicMilliseconds;
            GD.Print(FormattableString.Invariant($"[{_diagnosticId}][phase=capture-timing] shot={shot.Name} pose={shot.Pose} stage={stage} monotonicMs={now:F1} shotMs={now - started:F1} phaseMs={now - previousPhase:F1}"));
            previousPhase = now;
        }
    }

    private async Task CaptureOutputAsync(FixedShot shot, string destination, Func<bool> ready,
        Func<Pose> validateDestination, Action<string> logPhase, CancellationToken cancellationToken)
    {
        if (shot.Options.Capture.MovieFrames is not null)
        {
            await RecordMovieAsync(shot.Options.Capture, ready, destination, cancellationToken);
            validateDestination();
            logPhase("movie-complete");
        }
        else
        {
            _path = Path.Combine(destination, shot.Name + ".png");
            await SaveFrameAsync(_path, hideUi: false, unique: false, cancellationToken, ready);
            validateDestination();
            logPhase("png-written");
            GD.Print($"[{_diagnosticId}][phase=capture] pose={shot.Pose} {_path}");
        }
    }

    private async Task RecordMovieAsync(CaptureBootOptions options, Func<bool> ready,
        string destination, CancellationToken cancellationToken)
    {
        HideUi(hideUi: false);
        try
        {
            await WaitForFrameAsync(ready, cancellationToken);
            var segment = await MovieCapture.RunAsync(this, options, ready, _diagnosticId, cancellationToken);
            // Godot retains its native prelude and tail. Only this interval belongs to the requested clip.
            var metadata = JsonSerializer.SerializeToUtf8Bytes(segment, ContractJson.Options);
            await File.WriteAllBytesAsync(Path.Combine(destination, "movie-segment.json"), metadata, cancellationToken);
        }
        finally { RestoreUi(); }
    }

    private static Pose ApplyPose(Pose pose, BootRunOptions options, IMapContext context,
        ICameraRig rig, Action syncCamera)
    {
        if (AppBoot.FixedShots.Count > 0) CaptureWorldState.Start(options, context.Descriptor);
        rig.SetPose(pose);
        syncCamera();
        return rig.Pose;
    }

    private static IMapArrival BeginArrival(IMapContext context, Pose appliedPose, IMapRuntime runtime, ICameraRig rig)
    {
        var medium = context.World.Spatial.MediumAt(appliedPose.Position);
        NavSpace? space = medium is { Kind: MediumKind.Interior, Feature: { } building }
            ? new(building, checked((short)medium.Storey)) : null;
        return runtime.Arrive(appliedPose, rig.Mode, space);
    }
}
