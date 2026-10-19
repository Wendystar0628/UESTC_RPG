using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>The production capture queue; every pose gets a fresh readiness barrier.</summary>
public static class CaptureSequence
{
    public static async Task RunAsync(Node host, ViewContentReadiness producers, BootRunOptions options,
        Action syncCamera, Func<bool> contentReady, CancellationToken cancellationToken = default,
        Action<Pose>? validatePose = null)
    {
        var destination = options.Capture.Output ?? throw new ArgumentException("--out must be an absolute directory.");
        var context = Services.Get<IMapService>().Context ?? throw new InvalidOperationException("Capture requires a loaded map.");
        var rig = Services.Get<ICameraRig>();
        var stream = Services.Get<IMapStreamState>();
        Directory.CreateDirectory(destination);
        var shots = AppBoot.FixedShots.Count > 0 ? AppBoot.FixedShots
            : options.Capture.Shots.Select(id => new FixedShot(id, id, options)).ToArray();
        foreach (var shot in shots)
        {
            var id = shot.Pose;
            var shotOptions = shot.Options;
            var started = FrameClock.MonotonicMilliseconds;
            var previousPhase = started;
            LogPhase("start");
            cancellationToken.ThrowIfCancellationRequested();
            if (id == "FRONTIS") throw new NotSupportedException("FRONTIS capture requires the unconnected frontispiece presentation.");
            var pose = id == "T0"
                ? shotOptions.Title ? context.Descriptor.Title?.T0 ?? throw new InvalidDataException("This map has no title pose.")
                    : throw new ArgumentException("T0 requires --title=1.")
                : ReviewPoseResolver.Resolve(id, context);
            var terrainPoint = new System.Numerics.Vector2(pose.Position.X, pose.Position.Z);
            await Services.Get<ITerrainPreparation>().PrepareRegion(context, new(terrainPoint, terrainPoint), cancellationToken);
            if (id != "T0") validatePose?.Invoke(pose);
            LogPhase("terrain-ready");
            if (AppBoot.FixedShots.Count > 0) CaptureWorldState.Start(shotOptions, context.Descriptor);
            rig.SetPose(pose);
            syncCamera();
            var appliedPose = rig.Pose;
            producers.PrepareCapture(appliedPose);
            stream.Prefetch(appliedPose, rig.Mode);
            LogPhase("pose-applied");
            await CaptureBarrier.WaitForReadyAsync(host, appliedPose, contentReady, cancellationToken);
            LogPhase("coarse-ready");
            cancellationToken.ThrowIfCancellationRequested();
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
}
