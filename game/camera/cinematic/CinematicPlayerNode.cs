using Campus.Contracts;
using Campus.Contracts.Cinematic;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera.Cinematic;

[KernelNode("cinematic.player", Owner = "WP-49a")]
public sealed partial class CinematicPlayerNode : Node
{
    private ICinematicPlayback? _playback;
    public override void _Ready()
    {
        ProcessPriority = FramePriority.Camera;
        _playback = Services.Get<ICinematicPlayback>();
        RenderingServer.Singleton.FramePostDraw += OnFramePostDraw;
    }
    public override void _Process(double delta) => _playback?.Tick(delta);
    private void OnFramePostDraw() => _playback?.FramePresented();
    public override void _ExitTree()
    {
        RenderingServer.Singleton.FramePostDraw -= OnFramePostDraw;
        _playback?.Clear();
    }
}
