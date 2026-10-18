using System.Numerics;

namespace Campus.Contracts;

public enum WalkPerson : byte { First, Third }

/// <summary>Read-only character presentation, owned by WP-54a. It never controls the physical body.</summary>
public interface ICharacterService
{
    /// <summary>A character presentation exists, including the approved Walk-to-Fly presentation tail; stable Fly is absent.</summary>
    /// <remarks>Physical mode and camera trajectory never wait for the fade. Animal perception uses IWalkSnapshot, not this property.</remarks>
    bool Present { get; }
    WalkPerson Person { get; }
    /// <summary>Current presentation visibility weight, in [0,1].</summary>
    float Fade { get; }
    /// <summary>Stable logical head pivot from the controller's interpolated eye, never the current animated head bone.</summary>
    /// <remarks>Undefined when Present is false. Camera/listener code can directly use the physical eye source without depending on the model.</remarks>
    Vector3 HeadAnchor { get; }
}
