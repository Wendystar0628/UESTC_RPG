using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.North.Framework;
using C = Campus.Gen.Zones.North.NorthTune.Court;
using L = Campus.Gen.Zones.North.NorthTune.Look;

namespace Campus.Gen.Zones.North.Xuezhi;

/// <summary>
/// The 学知一 courtyard's stay point (20a-2; D-185, MAP-QSH-ADDITIONS §0 R6, registered as design:north-xz1-seatwall): a
/// gently bowed granite seat wall with a wood seat, on the lawn between 学知苑 1 栋's east wing and the courtyard road. The
/// courtyard hill and the rain garden are terrain operations in north.terrain.json; this site adds only the wall. It sits on
/// level ground, so its base is the lowest ground under its path and the body is sunk below it (WLD-TERRAIN §4.1 带厚度的小实体).
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
        // The apron runs along the courtyard side (toward about 235°), butting the stone body. The sweep's positive lateral is
        // (t.z, −t.x), which with z south lies to the left of the walking direction, so the courtyard side is negative.
        var slab = PartGeometryKernel.BeveledRectangleProfile(C.ApronWidth, C.ApronThickness, 0.01f);
        var shift = new Vector2(-(C.BodyWidth * 0.5f + C.ApronWidth * 0.5f), 0);
        var apron = new SweepProfile(slab.Points.Select(p => p + shift).ToArray(), slab.Normals);
        b.Add(PartGeometryKernel.Sweep(apron, path, C.Apron, baseY: g + C.PavingLevel - C.ApronThickness), Matrix4x4.Identity);
    }

    /// <summary>One box per chord, body and seat together: 0.45 m above the apron, a prop the player can step onto.</summary>
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
