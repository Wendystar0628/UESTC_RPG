using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
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
        var runtime = Services.Get<IMapRuntime>();
        var veil = Services.Get<IVeilService>();
        Directory.CreateDirectory(destination);
        var shots = AppBoot.FixedShots.Count > 0 ? AppBoot.FixedShots
            : options.Capture.Shots.Select(id => new FixedShot(id, id, options)).ToArray();
        foreach (var shot in shots)
        {
            var id = shot.Pose;
            var shotOptions = shot.Options;
            cancellationToken.ThrowIfCancellationRequested();
            if (id == "FRONTIS") throw new NotSupportedException("FRONTIS capture requires the unconnected frontispiece presentation.");
            var pose = id == "T0"
                ? shotOptions.Title ? context.Descriptor.Title?.T0 ?? throw new InvalidDataException("This map has no title pose.")
                    : throw new ArgumentException("T0 requires --title=1.")
                : ReviewPoseResolver.Resolve(id, context);
            if (id != "T0") validatePose?.Invoke(pose);
            using var cover = veil.Cover(VeilReason.Boot, 0);
            await cover.Covered.WaitAsync(cancellationToken);
            if (AppBoot.FixedShots.Count > 0) CaptureWorldState.Start(shotOptions, context.Descriptor);
            rig.SetPose(pose);
            syncCamera();
            var appliedPose = rig.Pose;
            producers.PrepareCapture(appliedPose);
            // The veil layer processes before this stage in a frame, so the cover set above is first drawn one frame later.
            var presentation = Services.Get<IVeilPresentation>();
            for (var frame = 0; frame < 2 && !presentation.IsCovered; frame++)
            {
                await host.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                cancellationToken.ThrowIfCancellationRequested();
            }
            if (!presentation.IsCovered)
                throw new InvalidOperationException("Capture warmup requires a drawn opaque veil.");
            var arrival = runtime.Arrive(appliedPose, rig.Mode);
            await CaptureBarrier.WaitForReadyAsync(host, producers, arrival, () => rig.Pose == appliedPose && contentReady(),
                cancellationToken, includeCanvas: shotOptions.Capture.IncludeCanvas);
            await cover.Reveal(0).WaitAsync(cancellationToken);
            await CaptureBarrier.WaitForReadyAsync(host, producers, arrival, () => rig.Pose == appliedPose && contentReady() && veil.Opacity == 0,
                cancellationToken, includeCanvas: shotOptions.Capture.IncludeCanvas);
            cancellationToken.ThrowIfCancellationRequested();
            using var image = host.GetViewport().GetTexture().GetImage();
            if (image.IsEmpty()) throw new IOException("Screenshot viewport returned an empty image.");
            var path = Path.Combine(destination, shot.Name + ".png");
            if (image.SavePng(path) != Error.Ok) throw new IOException("Screenshot could not be saved.");
            GD.Print($"[WP-01][phase=capture] pose={id} {path}");
        }
    }
}
