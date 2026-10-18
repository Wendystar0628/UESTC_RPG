using System.Numerics;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

/// <summary>The first visible tread's front-top left endpoint, horizontal unit uphill direction, +Y and physical width.</summary>
/// <remarks>It is not the buried RampCollider.P0. Coordinates follow the collider: tile-local in CollisionTile, world after mount.</remarks>
public readonly record struct StairFrame(Vector3 Nosing0, Vector3 Run, Vector3 Up, float Width)
{
    /// <summary>Checks the straight-flight frame and its existing StairSpec; does not infer an absent visual origin.</summary>
    public void Validate(StairSpec spec)
    {
        if (!VectorMath.Finite(Nosing0) || !VectorMath.Finite(Run) || !VectorMath.Finite(Up) || !float.IsFinite(Width) || Width <= 0)
            throw new ArgumentException("Stair frame must contain finite geometry and positive width.");
        if (MathF.Abs(Run.Y) > .0001f || MathF.Abs(Run.LengthSquared() - 1) > .0001f ||
            Vector3.DistanceSquared(Up, Vector3.UnitY) > .00000001f)
            throw new ArgumentException("Stair Run must be horizontal and unit length; Up must be +Y.");
        if (!float.IsFinite(spec.RiserM) || spec.RiserM <= 0 || !float.IsFinite(spec.TreadM) || spec.TreadM <= 0 || spec.Count <= 0)
            throw new ArgumentException("Stair specification requires positive riser, tread and count.", nameof(spec));
    }

}

/// <summary>A mounted analytical flight selected for the requested support layer; no native body or RID escapes the boundary.</summary>
/// <remarks>Tread k occupies [k*TreadM,(k+1)*TreadM) along Run and has height Nosing0.Y+k*RiserM.
/// The caller still checks collision readiness, finite lateral extent, valid tread index and leg reach.</remarks>
public readonly record struct StairHit(Vector3 Nosing0, Vector3 Run, Vector3 Up, StairSpec Spec)
{
    public float Width { get; init; }
}
