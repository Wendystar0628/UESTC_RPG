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
    private IAppLoadingPresentation _loading = null!;
    private readonly CancellationTokenSource _exit = new();
    private TaskCompletionSource<CaptureResult>? _photo;
    private Task? _encoding;
    private IDisposable? _hiddenHud;
    private string? _path;
    private string _diagnosticId = null!;
    private bool _busy, _exiting;

    public override void _Ready()
    {
        _loading = KernelServices.Get<IAppLoadingPresentation>();
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
        _photo = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = CapturePhotoAsync(request, _photo, cancellationToken);
        return _photo.Task;
    }

    private async Task CapturePhotoAsync(CaptureRequest request, TaskCompletionSource<CaptureResult> completion,
        CancellationToken cancellationToken)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(_exit.Token, cancellationToken);
        try
        {
            _path = request.OutputPath ?? Path.Combine(OS.GetUserDataDir(), "photos", $"photo-{Time.GetTicksUsec()}.png");
            if (!Path.IsPathFullyQualified(_path)) throw new ArgumentException("Capture output must be an absolute filename.");
            var path = await SaveFrameAsync(_path, request.HideUi, request.OutputPath is null, lifetime.Token);
            completion.TrySetResult(new(CaptureStatus.Saved, path));
        }
        catch (OperationCanceledException) { completion.TrySetResult(new(CaptureStatus.Unavailable, _path)); }
        catch (Exception error)
        {
            GD.PushWarning($"[{_diagnosticId}] Photo export failed ({error.GetType().Name}).");
            completion.TrySetResult(new(CaptureStatus.Failed, _path));
        }
        finally { EndCapture(); }
    }

    public async Task<byte[]?> CaptureThumbnailAsync(CancellationToken cancellationToken = default)
    {
        RequireMainThread();
        if (_busy || !Available() || _loading.Active || cancellationToken.IsCancellationRequested) return null;
        _busy = true;
        _path = null;
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(_exit.Token, cancellationToken);
        try
        {
            var image = await ReadFrameAsync(hideUi: true, lifetime.Token, () =>
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
            _encoding = encoding;
            return await encoding;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception error)
        {
            GD.PushWarning($"[{_diagnosticId}] Thumbnail capture failed ({error.GetType().Name}).");
            return null;
        }
        finally { EndCapture(); }
    }

    private bool Available() => !_exiting && IsInsideTree() && IsNodeReady()
        && DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Minimized;

    private Task WaitForFrameAsync(Func<bool>? ready, CancellationToken cancellationToken) =>
        FrameAwaiter.ReadyDrawAsync(this, () => Available() && (ready?.Invoke() ?? true),
            TimeSpan.FromSeconds(BootTuning.StalledSeconds), cancellationToken);

    private async Task<Image> ReadFrameAsync(bool hideUi, CancellationToken cancellationToken, Func<bool>? ready = null)
    {
        HideUi(hideUi);
        try
        {
            await WaitForFrameAsync(ready, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var image = GetViewport().GetTexture().GetImage();
            if (!image.IsEmpty()) return image;
            image.Dispose();
            throw new IOException("The capture viewport returned an empty image.");
        }
        finally { RestoreUi(); }
    }

    private async Task<string> SaveFrameAsync(string path, bool hideUi, bool unique, CancellationToken cancellationToken,
        Func<bool>? ready = null)
    {
        var image = await ReadFrameAsync(hideUi, cancellationToken, ready);
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
        _encoding = encoding;
        return await encoding;
    }

    private static string WritePhoto(byte[] bytes, string requestedPath, bool unique, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(Path.GetDirectoryName(requestedPath)!);
        for (var suffix = 0; ; suffix++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = suffix == 0 ? requestedPath : Path.ChangeExtension(requestedPath, null) + $"-{suffix}.png";
            FileStream stream;
            try { stream = new(path, unique ? FileMode.CreateNew : FileMode.Create, System.IO.FileAccess.Write, FileShare.Read); }
            catch (IOException) when (unique && File.Exists(path)) { continue; }
            try { using (stream) stream.Write(bytes); }
            catch { File.Delete(path); throw; }
            return path;
        }
    }

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
        RestoreUi();
        _busy = false;
        _path = null;
        _encoding = null;
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
        _photo?.TrySetResult(new(CaptureStatus.Unavailable, _path));
        // Drain only the worker that owns a detached native Image; it never waits for the scene thread.
        try { _encoding?.GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { }
        catch (Exception error) { GD.PushWarning($"[{_diagnosticId}] Capture shutdown failed ({error.GetType().Name})."); }
    }

    public override void _ExitTree()
    {
        StopCapture();
        _exit.Dispose();
    }

    /// <summary>Every fixed destination uses the same service owner, presentation and image export as F12.</summary>
    public async Task RunSequenceAsync(Node host, ViewContentReadiness producers, BootRunOptions options,
        Action syncCamera, Func<bool> contentReady, CancellationToken cancellationToken = default,
        Action<Pose>? validatePose = null)
    {
        RequireMainThread();
        if (_busy) throw new InvalidOperationException("Another game capture is in progress.");
        if (!Available()) throw new InvalidOperationException("The game capture service is not available.");
        var destination = options.Capture.Output ?? throw new ArgumentException("--out must be an absolute directory.");
        if (!Path.IsPathFullyQualified(destination)) throw new ArgumentException("--out must be an absolute directory.");
        _busy = true;
        _path = null;
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(_exit.Token, cancellationToken);
        try
        {
            var context = KernelServices.Get<IMapService>().Context ?? throw new InvalidOperationException("Capture requires a loaded map.");
            Directory.CreateDirectory(destination);
            var shots = AppBoot.FixedShots.Count > 0 ? AppBoot.FixedShots
                : options.Capture.Shots.Select(id => new FixedShot(id, id, options)).ToArray();
            if (options.Capture.MovieFrames is not null)
            {
                if (shots.Count != 1) throw new ArgumentException("Movie Maker captures exactly one fixed pose per process.");
                MovieCapture.RequireMovieMaker();
            }
            foreach (var shot in shots)
                await CaptureShotAsync(host, producers, shot, destination, context, syncCamera, contentReady, validatePose, lifetime.Token);
        }
        finally { EndCapture(); }
    }

    private async Task CaptureShotAsync(Node host, ViewContentReadiness producers, FixedShot shot,
        string destination, IMapContext context, Action syncCamera, Func<bool> contentReady,
        Action<Pose>? validatePose, CancellationToken cancellationToken)
    {
        var rig = KernelServices.Get<ICameraRig>();
        var runtime = KernelServices.Get<IMapRuntime>();
        var started = FrameClock.MonotonicMilliseconds;
        var previousPhase = started;
        LogPhase("start");
        cancellationToken.ThrowIfCancellationRequested();
        var pose = ResolvePose(shot.Pose, shot.Options, context);
        await using var cover = _loading.Show();
        await cover.Covered;
        cancellationToken.ThrowIfCancellationRequested();
        var terrainPoint = new System.Numerics.Vector2(pose.Position.X, pose.Position.Z);
        await KernelServices.Get<ITerrainPreparation>().PrepareRegion(context, new(terrainPoint, terrainPoint), cancellationToken);
        if (shot.Pose != "T0") validatePose?.Invoke(pose);
        LogPhase("terrain-ready");
        var appliedPose = ApplyPose(pose, shot.Options, context, rig, syncCamera, producers);
        var arrival = BeginArrival(context, appliedPose, runtime, rig);
        void ReportProgress() => cover.Report(arrival.Completed, arrival.Total);
        arrival.ProgressChanged += ReportProgress;
        try
        {
            ReportProgress();
            LogPhase("pose-applied");
            await producers.WaitForReadyAsync(host, arrival, contentReady, cancellationToken);
            if (rig.Pose != appliedPose) throw new OperationCanceledException("Capture pose was replaced.");
            LogPhase("destination-ready");
            cover.Report(arrival.Total, arrival.Total);
            await cover.Hide();
            await CaptureBarrier.WaitForPresentedAsync(host, appliedPose, ReadyForShot, cancellationToken);
            LogPhase("scene-presented");
        }
        finally { arrival.ProgressChanged -= ReportProgress; }
        cancellationToken.ThrowIfCancellationRequested();
        if (shot.Options.Capture.MovieFrames is not null)
        {
            await RecordMovieAsync(shot.Options.Capture, appliedPose, ReadyForShot, destination, cancellationToken);
            LogPhase("movie-complete");
        }
        else
        {
            _path = Path.Combine(destination, shot.Name + ".png");
            await SaveFrameAsync(_path, hideUi: false, unique: false, cancellationToken, ReadyForShot);
            LogPhase("png-written");
            GD.Print($"[{_diagnosticId}][phase=capture] pose={shot.Pose} {_path}");
        }

        bool ReadyForShot()
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (rig.Pose != appliedPose || !ReferenceEquals(runtime.Arrival, arrival))
                throw new OperationCanceledException("Capture destination was replaced.");
            return contentReady() && producers.IsReady(appliedPose);
        }

        void LogPhase(string stage)
        {
            var now = FrameClock.MonotonicMilliseconds;
            GD.Print(FormattableString.Invariant($"[{_diagnosticId}][phase=capture-timing] shot={shot.Name} pose={shot.Pose} stage={stage} monotonicMs={now:F1} shotMs={now - started:F1} phaseMs={now - previousPhase:F1}"));
            previousPhase = now;
        }
    }

    private async Task RecordMovieAsync(CaptureBootOptions options, Pose pose, Func<bool> ready,
        string destination, CancellationToken cancellationToken)
    {
        HideUi(hideUi: false);
        try
        {
            await WaitForFrameAsync(ready, cancellationToken);
            var segment = await MovieCapture.RunAsync(this, options, pose, ready, _diagnosticId, cancellationToken);
            // Godot retains its native prelude and tail. Only this interval belongs to the requested clip.
            var metadata = JsonSerializer.SerializeToUtf8Bytes(segment, ContractJson.Options);
            await File.WriteAllBytesAsync(Path.Combine(destination, "movie-segment.json"), metadata, cancellationToken);
        }
        finally { RestoreUi(); }
    }

    private static Pose ResolvePose(string id, BootRunOptions options, IMapContext context)
    {
        if (id == "FRONTIS") throw new NotSupportedException("FRONTIS capture requires the unconnected frontispiece presentation.");
        return id == "T0"
            ? options.Title ? context.Descriptor.Title?.T0 ?? throw new InvalidDataException("This map has no title pose.")
                : throw new ArgumentException("T0 requires --title=1.")
            : ReviewPoseResolver.Resolve(id, context);
    }

    private static Pose ApplyPose(Pose pose, BootRunOptions options, IMapContext context,
        ICameraRig rig, Action syncCamera, ViewContentReadiness producers)
    {
        if (AppBoot.FixedShots.Count > 0) CaptureWorldState.Start(options, context.Descriptor);
        rig.SetPose(pose);
        syncCamera();
        var appliedPose = rig.Pose;
        producers.PrepareCapture(appliedPose);
        return appliedPose;
    }

    private static IMapArrival BeginArrival(IMapContext context, Pose appliedPose, IMapRuntime runtime, ICameraRig rig)
    {
        var medium = context.World.Spatial.MediumAt(appliedPose.Position);
        NavSpace? space = medium is { Kind: MediumKind.Interior, Feature: { } building }
            ? new(building, checked((short)medium.Storey)) : null;
        return runtime.Arrive(appliedPose, rig.Mode, space);
    }
}
