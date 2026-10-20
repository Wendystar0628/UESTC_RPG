using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Geometry;

/// <summary>
/// A level frame on the plan: origin (world x, z), local X along <see cref="U"/>, local Y up, local Z = X × Y, which in plan
/// is (−U.z, U.x). Matrices are rigid (no trigonometry), so the same plan input gives the same bytes on every machine.
/// </summary>
public readonly record struct PlanFrame(Vector2 Origin, Vector2 U)
{
    /// <summary>Local +Z in plan (world x, z).</summary>
    public Vector2 W => new(-U.Y, U.X);

    /// <summary>A frame through <paramref name="origin"/> whose X runs toward <paramref name="toward"/>.</summary>
    public static PlanFrame Toward(Vector2 origin, Vector2 toward) => new(origin, VectorMath.Unit(toward - origin, minimumLength: 1e-6));

    /// <summary>A frame whose X runs along <paramref name="direction"/>.</summary>
    public static PlanFrame Along(Vector2 origin, Vector2 direction) => new(origin, VectorMath.Unit(direction, minimumLength: 1e-6));

    /// <summary>World plan point of local (x, z).</summary>
    public Vector2 P(float x, float z) => new(Origin.X + U.X * x + W.X * z, Origin.Y + U.Y * x + W.Y * z);

    /// <summary>Local (x, z) of a world plan point.</summary>
    public Vector2 Local(Vector2 world)
    {
        var d = world - Origin;
        return new(d.X * U.X + d.Y * U.Y, d.X * W.X + d.Y * W.Y);
    }

    /// <summary>Placement matrix (row-vector convention) of a part whose origin sits at local (x, y, z).</summary>
    public Matrix4x4 At(float x, float y, float z)
    {
        var p = P(x, z);
        return new(U.X, 0, U.Y, 0,
            0, 1, 0, 0,
            W.X, 0, W.Y, 0,
            p.X, y, p.Y, 1);
    }

    /// <summary>Placement matrix turned a quarter about Y: the part's local X runs along this frame's Z.</summary>
    public Matrix4x4 AtTurned(float x, float y, float z)
    {
        var p = P(x, z);
        return new(W.X, 0, W.Y, 0,
            0, 1, 0, 0,
            -U.X, 0, -U.Y, 0,
            p.X, y, p.Y, 1);
    }

    /// <summary>Placement matrix turned half round: the part faces this frame's −Z.</summary>
    public Matrix4x4 AtReversed(float x, float y, float z)
    {
        var p = P(x, z);
        return new(-U.X, 0, -U.Y, 0,
            0, 1, 0, 0,
            -W.X, 0, -W.Y, 0,
            p.X, y, p.Y, 1);
    }

    /// <summary>The same frame moved to local (x, z).</summary>
    public PlanFrame Shift(float x, float z) => this with { Origin = P(x, z) };

    /// <summary>Plan ring of a local rectangle, counter-clockwise in local (x, z).</summary>
    public Vector2[] Rect(float x0, float z0, float x1, float z1) => [P(x0, z0), P(x1, z0), P(x1, z1), P(x0, z1)];
}
