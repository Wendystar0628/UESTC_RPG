using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>The production capture queue; every pose gets a fresh readiness barrier.</summary>
public static class CaptureSequence
{
    public static async Task RunAsync(Node host, ViewContentReadiness producers, BootRunOptions options,
        Action syncCamera, Func<bool> contentReady, CancellationToken cancellationToken = default,
        Action<Pose>? validatePose = null)
    {
        var destination = options.Capture.Output ?? throw new ArgumentException("--out must be an absolute directory.");
        var context = KernelServices.Get<IMapService>().Context ?? throw new InvalidOperationException("Capture requires a loaded map.");
        var rig = KernelServices.Get<ICameraRig>();
        var loading = KernelServices.Get<IAppLoadingPresentation>();
        var runtime = KernelServices.Get<IMapRuntime>();
        Directory.CreateDirectory(destination);
        var shots = AppBoot.FixedShots.Count > 0 ? AppBoot.FixedShots
            : options.Capture.Shots.Select(id => new FixedShot(id, id, options)).ToArray();
        if (options.Capture.MovieFrames is not null)
        {
            if (shots.Count != 1) throw new ArgumentException("Movie Maker captures exactly one fixed pose per process.");
            MovieCapture.RequireMovieMaker();
        }
        foreach (var shot in shots)
        {
            var id = shot.Pose;
            var shotOptions = shot.Options;
            var started = FrameClock.MonotonicMilliseconds;
            var previousPhase = started;
            LogPhase("start");
            cancellationToken.ThrowIfCancellationRequested();
            var pose = ResolvePose(id, shotOptions, context);
            await using var cover = loading.Show();
            await cover.Covered;
            cancellationToken.ThrowIfCancellationRequested();
            var terrainPoint = new System.Numerics.Vector2(pose.Position.X, pose.Position.Z);
            await KernelServices.Get<ITerrainPreparation>().PrepareRegion(context, new(terrainPoint, terrainPoint), cancellationToken);
            if (id != "T0") validatePose?.Invoke(pose);
            LogPhase("terrain-ready");
            var appliedPose = ApplyPose(pose, shotOptions, context, rig, syncCamera, producers);
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
                await CaptureBarrier.WaitForPresentedAsync(host, appliedPose, () =>
                {
                    if (!ReferenceEquals(runtime.Arrival, arrival))
                        throw new OperationCanceledException("Capture arrival was replaced.");
                    return contentReady() && producers.IsReady(appliedPose);
                }, cancellationToken);
                LogPhase("scene-presented");
            }
            finally { arrival.ProgressChanged -= ReportProgress; }
            cancellationToken.ThrowIfCancellationRequested();
            if (shotOptions.Capture.MovieFrames is not null)
            {
                await MovieCapture.RunAsync(host, shotOptions.Capture, appliedPose, contentReady, cancellationToken);
                LogPhase("movie-complete");
                continue;
            }
            using var image = host.GetViewport().GetTexture().GetImage();
            if (image.IsEmpty()) throw new IOException("Screenshot viewport returned an empty image.");
            LogPhase("readback-complete");
            var path = Path.Combine(destination, shot.Name + ".png");
            if (image.SavePng(path) != Error.Ok) throw new IOException("Screenshot could not be saved.");
            LogPhase("png-written");
            GD.Print($"[WP-01][phase=capture] pose={id} {path}");

            void LogPhase(string stage)
            {
                var now = FrameClock.MonotonicMilliseconds;
                GD.Print(FormattableString.Invariant($"[WP-01][phase=capture-timing] shot={shot.Name} pose={id} stage={stage} monotonicMs={now:F1} shotMs={now - started:F1} phaseMs={now - previousPhase:F1}"));
                previousPhase = now;
            }
        }
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
        // Both outdoor rings and the current indoor floor use the production destination barrier.
        return runtime.Arrive(appliedPose, rig.Mode, space);
    }
}
