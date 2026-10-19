using Campus.Tests;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Tests.Support;

namespace Campus.Gen.Maps.Qingshuihe.South.Tests;

internal static class SouthTestSupport
{
    public static bool Covers(Collider c, Vector2 p, float y) => c switch
    {
        BoxCollider box => InBox(box, p) && MathF.Abs(y - box.At.M42) <= box.Half.Y,
        WallCollider wall => MathF.Sqrt(PlanLines.SegmentDistanceSquared(p, wall.A, wall.B)) <= wall.HalfT + 0.26f && y >= wall.Y0 && y <= wall.Y1,
        PostCollider post => Vector2.Distance(p, post.C) <= post.R + 0.26f && y >= post.Y0 && y <= post.Y1,
        _ => false,
    };

    public static bool Near(IReadOnlyList<Vector2> ring, Vector2 p, float margin) =>
        p.X >= ring.Min(q => q.X) - margin && p.X <= ring.Max(q => q.X) + margin && p.Y >= ring.Min(q => q.Y) - margin && p.Y <= ring.Max(q => q.Y) + margin;

    public static float MaxY(GeometryBundle mesh)
    {
        var y = float.MinValue;
        for (var i = 1; i < mesh.Position.Length; i += 3) y = MathF.Max(y, mesh.Position[i]);
        return y;
    }

    public static bool Covers(Collider c, Vector2 p) => c switch
    {
        BoxCollider box => InBox(box, p),
        WallCollider wall => MathF.Sqrt(PlanLines.SegmentDistanceSquared(p, wall.A, wall.B)) <= wall.HalfT + 0.26f,
        _ => false,
    };

    public static bool InBox(BoxCollider box, Vector2 p)
    {
        var ux = new Vector2(box.At.M11, box.At.M13);
        var uz = new Vector2(box.At.M31, box.At.M33);
        var d = p - new Vector2(box.At.M41, box.At.M43);
        return MathF.Abs(Vector2.Dot(d, ux)) <= box.Half.X + 0.26f && MathF.Abs(Vector2.Dot(d, uz)) <= box.Half.Z + 0.26f;
    }

    public static BuildContext Context()
    {
        // The south sites never read the map descriptor; the world model and seed are their whole input.
        return new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = Campus.Tests.RealMapFixture.Load().World, Map = null!, WorldSeed = 1 };
    }

}
