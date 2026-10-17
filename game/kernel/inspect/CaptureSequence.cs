using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Boot;
using Campus.Kernel.Renderer;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>The single capture queue for both composition roots; every pose gets a fresh readiness barrier.</summary>
public static class CaptureSequence
{
    public static async Task RunAsync(Node host, ViewContentReadiness producers, BootArguments options, Func<string, byte[]> read,
        Action syncCamera, Func<bool> contentReady, CancellationToken cancellationToken = default,
        Action<Pose>? validatePose = null)
    {
        var destination = options.Value("out");
        if (!Path.IsPathFullyQualified(destination)) throw new ArgumentException("--out must be an absolute directory.");
        var context = Services.Get<IMapService>().Context ?? throw new InvalidOperationException("Capture requires a loaded map.");
        var rig = Services.Get<ICameraRig>();
        var runtime = Services.Get<IMapRuntime>();
        var veil = Services.Get<IVeilService>();
        Directory.CreateDirectory(destination);
        foreach (var id in options.Value("shots").Split(','))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (id == "FRONTIS") throw new NotSupportedException("FRONTIS capture requires the unconnected frontispiece presentation.");
            var pose = id == "T0"
                ? options.Title ? context.Descriptor.Title?.T0 ?? throw new InvalidDataException("This map has no title pose.")
                    : throw new ArgumentException("T0 requires --title=1.")
                : ReviewPoseResolver.Resolve(id, context, read);
            if (id != "T0") validatePose?.Invoke(pose);
            await veil.Cover(VeilReason.Boot, 0);
            rig.SetPose(pose);
            syncCamera();
            var appliedPose = rig.Pose;
            producers.PrepareCapture(appliedPose);
            await host.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            cancellationToken.ThrowIfCancellationRequested();
            if (!Services.Get<IVeilPresentation>().IsCovered)
                throw new InvalidOperationException("Capture warmup requires a drawn opaque veil.");
            runtime.Prefetch(appliedPose, rig.Mode);
            runtime.BeginWarm();
            await CaptureBarrier.WaitForReadyAsync(host, producers, () => rig.Pose == appliedPose && contentReady(),
                cancellationToken, includeCanvas: options.Value("ui") == "1");
            await veil.Reveal(VeilReason.Boot, 0);
            await CaptureBarrier.WaitForReadyAsync(host, producers, () => rig.Pose == appliedPose && contentReady() && veil.Opacity == 0,
                cancellationToken, includeCanvas: options.Value("ui") == "1");
            cancellationToken.ThrowIfCancellationRequested();
            using var image = host.GetViewport().GetTexture().GetImage();
            if (image.IsEmpty()) throw new IOException("Screenshot viewport returned an empty image.");
            var path = Path.Combine(destination, id + ".png");
            if (image.SavePng(path) != Error.Ok) throw new IOException("Screenshot could not be saved.");
            GD.Print($"[WP-01][phase=capture] pose={id} {path}");
        }
    }
}
