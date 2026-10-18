using Campus.Contracts;
using Campus.Contracts.Maps;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Stream;

[KernelNode("stream.mount", Owner = "WP-02b")]
public partial class StreamMountNode : Node
{
    private IStreamMountFrame? _queue;
    private IStreamVisualFrame? _director;
    public override void _Ready()
    {
        ProcessPriority = FramePriority.StreamMount;
        _queue = Services.Get<IStreamMountFrame>();
        _director = Services.Get<IStreamVisualFrame>();
    }
    public override void _Process(double delta) => _queue?.Drain(_director is null ? null : _director.FinishMountFrame,
        _director is null ? null : _director.ReleaseRetired, VisualAdmission);
    private bool VisualAdmission() => _director?.CanActivateVisual ?? true;
}
