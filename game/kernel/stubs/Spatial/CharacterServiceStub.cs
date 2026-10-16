using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Stubs;

/// <summary>Read-only absence, not a functional character or a physical-body substitute.</summary>
[KernelService(typeof(ICharacterService), Owner = "WP-00", IsStub = true)]
public sealed class CharacterServiceStub : ICharacterService
{
    public bool Present => false;
    public WalkPerson Person => WalkPerson.First;
    public float Fade => 0;
    public Vector3 HeadAnchor => default; // Undefined while Present is false; never a valid origin fallback.
}
