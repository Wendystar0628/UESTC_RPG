using Campus.Contracts;

namespace Campus.Kernel.Stubs;

/// <summary>Only movement events remain stubbed until the physical swimming controller is attached.</summary>
[KernelService(typeof(IWaterMotionEvents), Owner = "WP-00", IsStub = true)]
public sealed class WaterTraversalStub : IWaterMotionEvents
{
    public event Action<WaterMotionEvent>? Emitted { add { } remove { } }
}
