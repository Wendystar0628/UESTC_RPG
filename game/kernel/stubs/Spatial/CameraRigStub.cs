using Campus.Contracts;

namespace Campus.Kernel.Stubs;

[KernelService(typeof(IMediumState), Owner = "WP-00", IsStub = true)]
public sealed class MediumStateStub : IMediumState
{
    public CameraMedium Current => new(MediumKind.Air);
    public MediumKind From => MediumKind.Air;
    public MediumKind To => MediumKind.Air;
    public float Blend => 1;
}

[KernelService(typeof(IInteriorDoors), Owner = "WP-00", IsStub = true)]
public sealed class InteriorDoorsStub : IInteriorDoors
{
    public DoorPhase PhaseOf(FeatureId building, string doorId) => DoorPhase.Open;
    public float OpenOf(FeatureId building, string doorId) => 1;
    public event Action<DoorEvent>? Changed { add { } remove { } }
}
