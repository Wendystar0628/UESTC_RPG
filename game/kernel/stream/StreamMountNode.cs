using Campus.Contracts;
using Campus.Contracts.Maps;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Stream;

[KernelNode("stream.mount", Owner = "WP-02b")]
public partial class StreamMountNode : Node
{
    private StreamMountQueue? _queue;
    private StreamDirector? _director;
    public override void _Ready()
    {
        ProcessPriority = FramePriority.StreamMount;
        _queue = (StreamMountQueue)Services.Get<IStreamMountQueue>();
        _director = Services.Get<IMapStreamState>() as StreamDirector;
    }
    public override void _Process(double delta) => _queue?.Drain(_director is null ? null : _director.FinishMountFrame,
        _director is null ? null : _director.ReleaseRetired, VisualAdmission);
    private bool VisualAdmission() => _director?.CanActivateVisual ?? true;
}
