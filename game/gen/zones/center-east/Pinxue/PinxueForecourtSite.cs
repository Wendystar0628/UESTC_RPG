using Campus.Gen.World;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.CenterEast.Framework;
using Campus.Gen.Zones.Common;
using L = Campus.Gen.Zones.CenterEast.CenterEastTune.Look;
using P = Campus.Gen.Zones.CenterEast.CenterEastTune.Pinxue;

namespace Campus.Gen.Zones.CenterEast.Pinxue;

/// <summary>
/// 品学楼 forecourt (MAP-QSH-CENTER-EAST; MAP-QSH-TERRAIN): the light-grey paving is WP-15 ground and the trees
/// are WP-23's (registered as tree pits in world/center-east.json). This site builds what is stone: the two rows of
/// square tree pits, the kerbed lawn panel with the low three-tier stepped stone cascade on the frame's axis (its water is
/// WP-24's, WLD-WATER), the seat ring of the round bed and the planter seat walls stepping down both
/// sides of the 5 % ramp. Where each stands is read back from those registrations (D-239). The entrance kit at the A-wing
/// hall door (px.a.hall: mat, open sign, door lamp) is WP-13's.
/// </summary>
public sealed class PinxueForecourtSite : ZoneSite
{
    private static PlanFrame Frame(IWorldModel world) => PinxueFrameSite.GateOf(world).F;

    public override string Id => "21a.pinxue-forecourt";

    public override IReadOnlyList<InspectionProbe> Probes =>
        // Q4 (wall-hugging, K1): 0.5 m from the A hall's ground-floor glazing and dark plinth, on the forecourt.
        [Probe("Q4", CenterEastIds.Pinxue.WingA, new(P.Q4Eye.X, P.PlatformY + WalkRules.StandingEye, P.Q4Eye.Y), P.Q4Look, -20f, 0.5f)];

    public override void Build(SiteBuilder b)
    {
        var world = b.Context.World;
        var gate = PinxueFrameSite.GateOf(world);
        TreePits(b, gate.F);
        LawnPanel(b);
        Fountain(b);
        RoundBed(b);
        foreach (var (x, z0, z1) in RampWalls(world)) RampWall(b, gate.F, gate.Axis, x, z0, z1);
    }

    // ---- Square tree pits ----

    /// <summary>Centres of the registered tree pits.</summary>
    internal static IEnumerable<Vector2> TreePitCentres(IWorldModel world) =>
        world.Feature(CenterEastIds.Pinxue.TreePitsId).Polygons().Select(pit => PlanGeometry.Centroid(pit));

    private static void TreePits(SiteBuilder b, PlanFrame f)
    {
        float h = (P.TreePitSize - P.TreePitKerb) * 0.5f, inner = P.TreePitSize - 2 * P.TreePitKerb;
        var kerb = PartGeometryKernel.BeveledRectangleProfile(P.TreePitKerb, P.TreePitHeight + 0.05f, 0.02f);
        foreach (var centre in TreePitCentres(b.Context.World))
        {
            var local = f.Local(centre);
            float x = local.X, z = local.Y;
            var g = b.Ground(centre);
            Vector2[] square = [f.P(x - h, z - h), f.P(x + h, z - h), f.P(x + h, z + h), f.P(x - h, z + h)];
            b.Add(PartGeometryKernel.Sweep(kerb, square, L.Granite, closed: true), Matrix4x4.CreateTranslation(0, g - 0.05f, 0));
            b.Add(PartGeometryKernel.BeveledBox(inner + 0.02f, P.TreePitSoil + 0.05f, inner + 0.02f, 0.01f, L.Soil), f.At(x, g - 0.05f, z));
        }
    }

    // ---- Lawn panel and the stepped cascade's stone tiers ----

    /// <summary>The forecourt hole containing its authored placement point.</summary>
    internal static IReadOnlyList<Vector2>? HoleNear(IWorldModel world, Vector2 point)
    {
        if (!world.Features.TryGetValue(new(CenterEastIds.Pinxue.Forecourt), out var court) || court.Geometry is not PolygonGeometry polygon) return null;
        return polygon.Rings.Holes.SingleOrDefault(hole => PlanGeometry.PointInRing(point, hole));
    }

    private static void LawnPanel(SiteBuilder b)
    {
        var fountain = b.Context.World.Feature(CenterEastIds.Pinxue.FountainId).Point();
        if (HoleNear(b.Context.World, fountain) is not { } hole) return;
        var g = b.Ground(fountain);
        var kerb = PartGeometryKernel.BeveledRectangleProfile(P.PanelKerbWidth, P.PanelKerbHeight + 0.05f, 0.02f);
        b.Add(PartGeometryKernel.Sweep(kerb, hole.ToArray(), L.Granite, closed: true), Matrix4x4.CreateTranslation(0, g - 0.05f, 0));
    }

    /// <summary>The cascade's frame: centred on the registered fountain point, X along the bar, +Z toward the forecourt.</summary>
    internal static PlanFrame FountainFrame(IWorldModel world) => Frame(world) with { Origin = world.Feature(CenterEastIds.Pinxue.FountainId).Point() };

    /// <summary>The three tiers bottom to top: plan half extents, coping width, rim and water heights above the lawn.</summary>
    internal static IReadOnlyList<(Vector2 Half, float Coping, float Rim, float Water)> FountainTiers() =>
    [
        (P.BasinHalf, P.BasinCoping, P.BasinRim, P.BasinWater),
        (P.MiddleHalf, P.TierCoping, P.MiddleRim, P.MiddleWater),
        (P.TopHalf, P.TierCoping, P.TopRim, P.TopWater),
    ];

    /// <summary>
    /// Each tier is a light stone coping wall round a dark pool floor; the next tier stands on that floor, so the stone
    /// steps up in three low terraces and the water sheets fall about 0.3 m from rim to pool. The basin sinks 5 cm into
    /// the lawn.
    /// </summary>
    private static void Fountain(SiteBuilder b)
    {
        var f = FountainFrame(b.Context.World);
        var g = b.Ground(f.Origin);
        var baseY = g - 0.05f;
        foreach (var (half, coping, rim, water) in FountainTiers())
        {
            var floorY = g + water - P.PoolDepth;
            float hx = half.X - coping * 0.5f, hz = half.Y - coping * 0.5f;
            Vector2[] path = [f.P(-hx, -hz), f.P(hx, -hz), f.P(hx, hz), f.P(-hx, hz)];
            var wall = PartGeometryKernel.BeveledRectangleProfile(coping, g + rim - baseY, b.Detail == SiteDetail.Mass ? 0f : 0.03f);
            b.Add(PartGeometryKernel.Sweep(wall, path, L.Granite, closed: true), Matrix4x4.CreateTranslation(0, baseY, 0));
            float ix = 2 * (half.X - coping) + 0.02f, iz = 2 * (half.Y - coping) + 0.02f;
            b.Add(PartGeometryKernel.BeveledBox(ix, floorY - baseY, iz, 0.01f, L.DarkGranite), f.At(0, baseY, 0));
            baseY = floorY;
        }
    }

    // ---- Round bed on the ramp: one tree in a 0.45 m granite seat ring ----

    private static void RoundBed(SiteBuilder b)
    {
        var at = b.Context.World.Feature(CenterEastIds.Pinxue.RoundBedId).Point();
        var (low, high) = GroundRange(b, at, P.RoundBedRadius);
        var n = b.Detail == SiteDetail.Full ? 40 : 20;
        b.Add(Shapes.Ring(at, P.RoundBedRadius, P.SeatWidth, high + P.SeatHeight - (low - 0.1f), n, L.Granite), Matrix4x4.CreateTranslation(0, low - 0.1f, 0));
    }

    private static (float Low, float High) GroundRange(SiteBuilder b, Vector2 c, float r)
    {
        float low = float.MaxValue, high = float.MinValue;
        foreach (var p in Shapes.Circle(c, r, 8).Append(c))
        {
            var y = b.Ground(p);
            low = MathF.Min(low, y); high = MathF.Max(high, y);
        }
        return (low, high);
    }

    // ---- Ramp planter seat walls, stepping down with the 5 % ramp ----

    /// <summary>
    /// Frame-local x of each ramp seat wall's plaza face and its z span from the top of the ramp to its foot, from the registered
    /// centre lines (the face stands half a wall thickness toward the axis).
    /// </summary>
    internal static IReadOnlyList<(float X, float Z0, float Z1)> RampWalls(IWorldModel world)
    {
        var gate = PinxueFrameSite.GateOf(world);
        var walls = new List<(float X, float Z0, float Z1)>();
        foreach (var id in new[] { CenterEastIds.Pinxue.RampWallNorthId, CenterEastIds.Pinxue.RampWallSouthId })
        {
            var line = world.Feature(id).Line();
            Vector2 a = gate.F.Local(line[0]), b = gate.F.Local(line[^1]);
            walls.Add((a.X - Outward(gate.Axis, a.X) * P.RampWallThickness * 0.5f, MathF.Min(a.Y, b.Y), MathF.Max(a.Y, b.Y)));
        }
        return walls;
    }

    /// <summary>
    /// Runs of one wall: 6 m segments whose top is the highest ground under them plus the seat height; neighbours at the
    /// same level merge, so on flat ground the wall is one piece and on the ramp it steps about 0.3 m per segment.
    /// </summary>
    internal static List<(float Z0, float Z1, float Base, float Top)> RampRuns(Func<Vector2, float> ground, PlanFrame f, float x, float wallZ0, float wallZ1)
    {
        var runs = new List<(float Z0, float Z1, float Base, float Top)>();
        for (var z0 = wallZ0; z0 < wallZ1 - 0.01f; z0 += P.RampWallSegment)
        {
            var z1 = MathF.Min(z0 + P.RampWallSegment, wallZ1);
            float low = float.MaxValue, high = float.MinValue;
            foreach (var z in new[] { z0, (z0 + z1) * 0.5f, z1 })
            {
                var y = ground(f.P(x, z));
                low = MathF.Min(low, y); high = MathF.Max(high, y);
            }
            var top = high + P.SeatHeight;
            if (runs.Count > 0 && MathF.Abs(runs[^1].Top - top) < 0.01f)
                runs[^1] = (runs[^1].Z0, z1, MathF.Min(runs[^1].Base, low - 0.1f), top);
            else runs.Add((z0, z1, low - 0.1f, top));
        }
        return runs;
    }

    /// <summary>Outward (away from the plaza) sign of a ramp wall in the frame's x, given the bar's mid-axis x.</summary>
    private static float Outward(float axis, float x) => x < axis ? -1f : 1f;

    private static void RampWall(SiteBuilder b, PlanFrame f, float axis, float x, float wallZ0, float wallZ1)
    {
        var o = Outward(axis, x);
        float t = P.RampWallThickness, bed = P.RampBedWidth, kerb = P.RampBedKerb;
        foreach (var (z0, z1, baseY, top) in RampRuns(b.Ground, f, x, wallZ0, wallZ1))
        {
            float len = z1 - z0, zc = (z0 + z1) * 0.5f;
            b.Add(PartGeometryKernel.BeveledBox(t, top - 0.06f - baseY, len, 0.02f, L.Granite), f.At(x + o * t * 0.5f, baseY, zc));
            b.Add(PartGeometryKernel.BeveledBox(t + 0.08f, 0.06f, len, 0.015f, L.Trim), f.At(x + o * t * 0.5f, top - 0.06f, zc));
            if (b.Detail == SiteDetail.Mass) continue;
            b.Add(PartGeometryKernel.BeveledBox(bed, top - 0.12f - baseY, len, 0.01f, L.Bed), f.At(x + o * (t + bed * 0.5f), baseY, zc));
            b.Add(PartGeometryKernel.BeveledBox(kerb, top - 0.05f - baseY, len, 0.015f, L.Granite), f.At(x + o * (t + bed + kerb * 0.5f), baseY, zc));
        }
    }

    public override void Collide(SiteColliders c)
    {
        var world = c.Context.World;
        var gate = PinxueFrameSite.GateOf(world);
        var f = gate.F;
        float Ground(Vector2 p) => c.Ground(p.X, p.Y);
        foreach (var p in TreePitCentres(world))
        {
            var g = Ground(p);
            var local = f.Local(p);
            c.Box(f.At(local.X, g + P.TreePitHeight * 0.5f, local.Y), new(P.TreePitSize * 0.5f, P.TreePitHeight * 0.5f, P.TreePitSize * 0.5f), SurfaceTag.Prop, FootMat.StonePaving);
        }
        // One box per tier up to its rim: the low copings can be stepped onto, as a seat edge would.
        var fountainFrame = FountainFrame(world);
        var fg = Ground(fountainFrame.Origin);
        foreach (var (half, _, rim, _) in FountainTiers())
            c.Box(fountainFrame.At(0, fg + (rim - 0.05f) * 0.5f, 0), new(half.X, (rim + 0.05f) * 0.5f, half.Y), SurfaceTag.Prop, FootMat.StonePaving);
        var bed = P.RoundBedRadius + P.SeatWidth * 0.5f;
        var roundBed = world.Feature(CenterEastIds.Pinxue.RoundBedId).Point();
        var rg = Ground(roundBed);
        c.Add(new PostCollider(roundBed, bed, rg - 0.1f, rg + P.SeatHeight, SurfaceTag.Prop) { Foot = FootMat.StonePaving });
        foreach (var (x, wallZ0, wallZ1) in RampWalls(world))
        {
            var o = Outward(gate.Axis, x);
            var width = P.RampWallThickness + P.RampBedWidth + P.RampBedKerb;
            foreach (var (z0, z1, baseY, top) in RampRuns(Ground, f, x, wallZ0, wallZ1))
                c.Box(f.At(x + o * width * 0.5f, (baseY + top) * 0.5f, (z0 + z1) * 0.5f), new(width * 0.5f, (top - baseY) * 0.5f, (z1 - z0) * 0.5f), SurfaceTag.Prop, FootMat.StonePaving);
        }
    }
}
