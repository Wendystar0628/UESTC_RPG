using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.North.Framework;
using Campus.Gen.World;
using C = Campus.Gen.Zones.North.NorthTune.Court;
using L = Campus.Gen.Zones.North.NorthTune.Look;

namespace Campus.Gen.Zones.North.Xuezhi;

/// <summary>
/// The 学知一 courtyard's stay point (20a-2, moved in 20a-3; D-185, MAP-QSH-ADDITIONS R6, registered as
/// design:north-xz1-seatwall): a gently bowed granite seat wall with a wood seat beside the courtyard walk, facing the hill
/// across it, with a paved pad joining it to the walk. The courtyard hill and the rain garden are terrain operations in
/// north.terrain.json; this site adds the wall and its pad. Both sit on level ground, so their base is the lowest ground under
/// the wall's path and the body is sunk below it (WLD-TERRAIN 带厚度的小实体).
/// </summary>
public sealed class CourtyardSite : NorthSite
{
    public override string Id => "20a2.xz1-court";

    /// <summary>The wall's centreline: a quadratic bow from start to end through the tuned mid point, evenly sampled.</summary>
    internal static Vector2[] Path(int segments, float holdBack = 0f)
    {
        var control = 2f * C.SeatMid - 0.5f * (C.SeatStart + C.SeatEnd);
        var chord = Vector2.Distance(C.SeatStart, C.SeatEnd);
        float t0 = holdBack / chord, t1 = 1f - t0;
        var points = new Vector2[segments + 1];
        for (var i = 0; i <= segments; i++)
        {
            var t = t0 + (t1 - t0) * i / segments;
            var u = 1f - t;
            points[i] = u * u * C.SeatStart + 2f * u * t * control + t * t * C.SeatEnd;
        }
        return points;
    }

    /// <summary>Unit plan direction from the chord toward the walk: the sweep's positive lateral (t.z, −t.x).</summary>
    internal static Vector2 Front()
    {
        var t = Vector2.Normalize(C.SeatEnd - C.SeatStart);
        return new(t.Y, -t.X);
    }

    /// <summary>
    /// The pad's plan outline, counter-clockwise and convex: the wall's front face (1 cm inside the stone, hidden under it)
    /// along the bow, then straight across at <see cref="C.PadReach"/> from the chord.
    /// </summary>
    internal static Vector2[] PadOutline()
    {
        var path = Path(C.SeatSegments);
        var front = Front();
        var face = new Vector2[path.Length];
        for (var i = 0; i < path.Length; i++)
        {
            var t = Vector2.Normalize(path[Math.Min(i + 1, path.Length - 1)] - path[Math.Max(i - 1, 0)]);
            face[i] = path[i] + new Vector2(t.Y, -t.X) * (C.BodyWidth * 0.5f - 0.01f);
        }
        Vector2 Edge(Vector2 p) => p + front * (C.PadReach - Vector2.Dot(p - C.SeatStart, front));
        Vector2[] ring = [.. face, Edge(face[^1]), Edge(face[0])];
        if (PlanGeometry.SignedArea(ring) < 0) Array.Reverse(ring);
        return ring;
    }

    /// <summary>Base level: the lowest ground under the path, so the body never floats on a slope.</summary>
    internal static float Base(Func<Vector2, float> ground, IReadOnlyList<Vector2> path) => path.Min(ground);

    public override void Build(SiteBuilder b)
    {
        var path = Path(b.Detail == SiteDetail.Mass ? C.SeatSegments / 2 : C.SeatSegments);
        var g = Base(b.Ground, path);
        var top = g + C.PavingLevel + C.BodyHeight;
        var body = PartGeometryKernel.BeveledRectangleProfile(C.BodyWidth, top - g + C.Sink, C.BodyBevel);
        b.Add(PartGeometryKernel.Sweep(body, path, L.Granite, baseY: g - C.Sink), Matrix4x4.Identity);
        if (b.Detail == SiteDetail.Mass) return;
        var seat = PartGeometryKernel.BeveledRectangleProfile(C.SeatWidth, C.SeatThickness, C.SeatBevel);
        b.Add(PartGeometryKernel.Sweep(seat, Path(C.SeatSegments, C.SeatHoldBack), L.Wood, baseY: top), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.BeveledExtrude(PadOutline(), C.PadThickness, 0.01f, C.Pad, g + C.PavingLevel + C.PadLift - C.PadThickness), Matrix4x4.Identity);
    }

    /// <summary>One box per chord, body and seat together: 0.45 m above the pad, a prop the player can step onto.</summary>
    public override void Collide(SiteColliders c)
    {
        var path = Path(C.SeatSegments);
        var g = Base(c.Ground, path);
        var half = (C.Sink + C.PavingLevel + C.BodyHeight + C.SeatThickness) * 0.5f;
        for (var i = 0; i + 1 < path.Length; i++)
        {
            var length = Vector2.Distance(path[i], path[i + 1]);
            var frame = PlanFrame.Along(path[i], path[i + 1] - path[i]);
            c.Box(frame.At(length * 0.5f, g - C.Sink + half, 0), new(length * 0.5f, half, C.SeatWidth * 0.5f), SurfaceTag.Prop, FootMat.WoodDeck);
        }
    }
}
