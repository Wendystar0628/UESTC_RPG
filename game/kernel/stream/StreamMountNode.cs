using Campus.Contracts;
using Campus.Contracts.Maps;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Stream;

[KernelNode("stream.mount")]
public partial class StreamMountNode : Node
{
    private IStreamMountFrame? _queue;
    private IStreamVisualFrame? _director;
    private IAppScreenCoverage? _coverage;
    private Action<double>? _finish;
    private Action<double>? _release;
    private Func<bool>? _visualAdmission;
    public override void _Ready()
    {
        ProcessPriority = FramePriority.StreamMount;
        _queue = KernelServices.Get<IStreamMountFrame>();
        _director = KernelServices.Get<IStreamVisualFrame>();
        _coverage = KernelServices.Get<IAppScreenCoverage>();
        _finish = _director.FinishMountFrame;
        _release = _director.ReleaseRetired;
        _visualAdmission = VisualAdmission;
    }
    public override void _Process(double delta) => _queue?.Drain(_coverage?.IsCovered == true, _finish, _release, _visualAdmission);
    private bool VisualAdmission() => _director?.CanActivateVisual ?? true;
}
