using Campus.Contracts;
using Campus.Contracts.Maps;
using KernelServices = Campus.Kernel.Registry.Kernel;
using Godot;

namespace Campus.Kernel.Maps;

[KernelNode("map-service")]
public partial class MapServiceNode : Node
{
    private IMapFrame? _service;
    public override void _Ready()
    {
        ProcessPriority = FramePriority.Map;
        _service = KernelServices.Get<IMapFrame>();
    }
    public override void _Process(double delta) => _service?.Step();
}
