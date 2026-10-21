using System.Numerics;
using Campus.Contracts;
using T = Campus.Gen.Maps.Qingshuihe.Waterfront.WaterfrontTune;

namespace Campus.Gen.Maps.Qingshuihe.Waterfront.Bridges;

/// <summary>
/// Collision of one bridge (MAP-QSH-ADDITIONS game/gen/maps/qingshuihe/waterfront/ 行, SYS-WALK), from the same plan as the geometry:
/// every walking strip and the carriageway as sloped boxes cut every 2 m along the bridge (walkable, <see cref="SurfaceTag.Ground"/>),
/// and each parapet as one barrier wall whose top is max(visual, 1.5 m) above the walk and that reaches no further than
/// the base. Each collider is filed by its own centre.
/// </summary>
internal static class BridgeColliders
{
    public static IEnumerable<(Vector2 Anchor, Collider Collider)> Of(BridgePlan plan)
    {
        foreach (var band in plan.Bands)
        {
            var carriage = band.Kind == BandKind.Carriage;
            var y = carriage ? plan.RoadY : plan.WalkY;
            var foot = carriage ? T.CarriageFoot : T.WalkFoot;
            float u0 = band.U0, u1 = band.U1;
            // The walking strips run out under the parapet bases so no seam opens at their foot.
            if (!carriage) { if (Math.Abs(u0 - plan.Left) < 1e-4f) u0 = plan.OuterLeft; if (Math.Abs(u1 - plan.Right) < 1e-4f) u1 = plan.OuterRight; }
            float a0 = plan.StartS(u0), a1 = plan.EndS(u0), b0 = plan.StartS(u1), b1 = plan.EndS(u1);
            var count = Math.Max(1, (int)MathF.Ceiling(Math.Max(a1 - a0, b1 - b0) / T.RampStep));
            for (var i = 0; i < count; i++)
            {
                float t0 = (float)i / count, t1 = (float)(i + 1) / count;
                var p0 = plan.P(a0 + (a1 - a0) * t0, u0, y); var p1 = plan.P(b0 + (b1 - b0) * t0, u1, y);
                var p2 = plan.P(b0 + (b1 - b0) * t1, u1, y); var p3 = plan.P(a0 + (a1 - a0) * t1, u0, y);
                var anchor = new Vector2((p0.X + p2.X) * 0.5f, (p0.Z + p2.Z) * 0.5f);
                yield return (anchor, new RampCollider(p0, p1, p2, p3, T.RampThick, SurfaceTag.Ground) { Foot = foot });
            }
        }
        foreach (var (inner, outer) in BridgeMesher.Sides(plan))
        {
            var u = (inner + outer) * 0.5f;
            var half = Math.Abs(outer - inner) * 0.5f;
            var a = plan.P(plan.StartS(u), u); var b = plan.P(plan.EndS(u), u);
            var wall = new WallCollider(a, b, half, plan.WalkY - 0.3f, plan.WalkY + Math.Max(WalkRules.BarrierHeight, T.HeadTop), SurfaceTag.Barrier) { Foot = T.RailFoot };
            yield return ((a + b) * 0.5f, wall);
        }
    }
}
