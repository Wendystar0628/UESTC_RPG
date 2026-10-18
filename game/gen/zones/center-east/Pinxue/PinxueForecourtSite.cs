using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.Common;
using L = Campus.Gen.Zones.CenterEast.CenterEastTune.Look;
using P = Campus.Gen.Zones.CenterEast.CenterEastTune.Pinxue;

namespace Campus.Gen.Zones.CenterEast.Pinxue;

/// <summary>
/// 品学楼 forecourt (MAP-QSH-CENTER-EAST; MAP-QSH-TERRAIN): the light-grey paving is WP-15 ground and the trees
/// are WP-23's (registered as tree pits in overlay/center-east.d/). This site builds what is stone: the two rows of
/// square tree pits, the kerbed lawn panel with the low three-tier stepped stone cascade on the frame's axis (its water is
/// WP-24's, WLD-WATER), the seat ring of the round bed and the planter seat walls stepping down both
/// sides of the 5 % ramp. The entrance kit at the A-wing hall door (px.a.hall: mat, open sign, door lamp) is WP-13's.
/// </summary>
public sealed class PinxueForecourtSite : ZoneSite
{
    private static PlanFrame F => PinxueFrameSite.F;
    /// <summary>Frame of the A hall's diagonal facade: X along it, +Z outward (bearing 216°).</summary>
    private static readonly PlanFrame Hall = PlanFrame.Toward(P.HallFacadeFrom, P.HallFacadeTo);

    public override string Id => "21a.pinxue-forecourt";

    public override IReadOnlyList<InspectionProbe> Probes
    {
        get
        {
            // Q4 (wall-hugging, K1): 0.5 m from the A hall's ground-floor glazing and dark plinth, on the forecourt.
            var door = Hall.Local(P.HallDoor).X;
            var eye = Hall.P(door - 4.5f, 0.5f);
            return [Probe("Q4", P.WingA, new(eye.X, P.PlatformY + 1.62f, eye.Y), -Hall.W, -20f, 0.5f)];
        }
    }

    public override void Build(SiteBuilder b)
    {
        TreePits(b);
        LawnPanel(b);
        Fountain(b);
        RoundBed(b);
        foreach (var x in P.RampWallX) RampWall(b, x);
    }

    // ---- Square tree pits ----

    internal static IEnumerable<Vector2> TreePitCentres()
    {
        foreach (var x in P.TreePitRowsX)
            foreach (var z in P.TreePitZ)
                yield return F.P(x, z);
    }

    private static void TreePits(SiteBuilder b)
    {
        float h = (P.TreePitSize - P.TreePitKerb) * 0.5f, inner = P.TreePitSize - 2 * P.TreePitKerb;
        var kerb = PartGeometryKernel.BeveledRectangleProfile(P.TreePitKerb, P.TreePitHeight + 0.05f, 0.02f);
        foreach (var x in P.TreePitRowsX)
            foreach (var z in P.TreePitZ)
            {
                var g = b.Ground(F.P(x, z));
                Vector2[] square = [F.P(x - h, z - h), F.P(x + h, z - h), F.P(x + h, z + h), F.P(x - h, z + h)];
                b.Add(PartGeometryKernel.Sweep(kerb, square, L.Granite, closed: true), Matrix4x4.CreateTranslation(0, g - 0.05f, 0));
                b.Add(PartGeometryKernel.BeveledBox(inner + 0.02f, P.TreePitSoil + 0.05f, inner + 0.02f, 0.01f, L.Soil), F.At(x, g - 0.05f, z));
            }
    }

    // ---- Lawn panel and the stepped cascade's stone tiers ----

    /// <summary>The forecourt hole nearest a point (the lawn panel round the fountain, the round bed on the ramp).</summary>
    internal static IReadOnlyList<Vector2>? HoleNear(IWorldModel world, Vector2 point)
    {
        if (!world.Features.TryGetValue(new(P.Forecourt), out var court) || court.Geometry is not PolygonGeometry polygon) return null;
        IReadOnlyList<Vector2>? best = null;
        var bestDistance = 12f;
        foreach (var hole in polygon.Rings.Holes)
        {
            var c = Vector2.Zero;
            foreach (var p in hole) c += p;
            var d = Vector2.Distance(c / hole.Count, point);
            if (d < bestDistance) { best = hole; bestDistance = d; }
        }
        return best;
    }

    private static void LawnPanel(SiteBuilder b)
    {
        if (HoleNear(b.Context.World, P.Fountain) is not { } hole) return;
        var g = b.Ground(P.Fountain);
        var kerb = PartGeometryKernel.BeveledRectangleProfile(P.PanelKerbWidth, P.PanelKerbHeight + 0.05f, 0.02f);
        b.Add(PartGeometryKernel.Sweep(kerb, hole.ToArray(), L.Granite, closed: true), Matrix4x4.CreateTranslation(0, g - 0.05f, 0));
    }

    /// <summary>The cascade's frame: centred on the lawn panel, X along the bar, +Z toward the forecourt.</summary>
    internal static PlanFrame FountainFrame
    {
        get
        {
            var local = F.Local(P.Fountain);
            return F.Shift(local.X, local.Y);
        }
    }

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
        var f = FountainFrame;
        var g = b.Ground(P.Fountain);
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
        var (low, high) = GroundRange(b, P.RoundBed, P.RoundBedRadius);
        var n = b.Detail == SiteDetail.Full ? 40 : 20;
        b.Add(Shapes.Ring(P.RoundBed, P.RoundBedRadius, P.SeatWidth, high + P.SeatHeight - (low - 0.1f), n, L.Granite), Matrix4x4.CreateTranslation(0, low - 0.1f, 0));
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
    /// Runs of one wall: 6 m segments whose top is the highest ground under them plus the seat height; neighbours at the
    /// same level merge, so on flat ground the wall is one piece and on the ramp it steps about 0.3 m per segment.
    /// </summary>
    internal static List<(float Z0, float Z1, float Base, float Top)> RampRuns(Func<Vector2, float> ground, float x)
    {
        var runs = new List<(float Z0, float Z1, float Base, float Top)>();
        for (var z0 = P.RampWallZ0; z0 < P.RampWallZ1 - 0.01f; z0 += P.RampWallSegment)
        {
            var z1 = MathF.Min(z0 + P.RampWallSegment, P.RampWallZ1);
            float low = float.MaxValue, high = float.MinValue;
            foreach (var z in new[] { z0, (z0 + z1) * 0.5f, z1 })
            {
                var y = ground(F.P(x, z));
                low = MathF.Min(low, y); high = MathF.Max(high, y);
            }
            var top = high + P.SeatHeight;
            if (runs.Count > 0 && MathF.Abs(runs[^1].Top - top) < 0.01f)
                runs[^1] = (runs[^1].Z0, z1, MathF.Min(runs[^1].Base, low - 0.1f), top);
            else runs.Add((z0, z1, low - 0.1f, top));
        }
        return runs;
    }

    /// <summary>Outward (away from the plaza) sign of a ramp wall in the frame's x.</summary>
    private static float Outward(float x) => x < PinxueFrameSite.Axis ? -1f : 1f;

    private static void RampWall(SiteBuilder b, float x)
    {
        var o = Outward(x);
        float t = P.RampWallThickness, bed = P.RampBedWidth, kerb = P.RampBedKerb;
        foreach (var (z0, z1, baseY, top) in RampRuns(b.Ground, x))
        {
            float len = z1 - z0, zc = (z0 + z1) * 0.5f;
            b.Add(PartGeometryKernel.BeveledBox(t, top - 0.06f - baseY, len, 0.02f, L.Granite), F.At(x + o * t * 0.5f, baseY, zc));
            b.Add(PartGeometryKernel.BeveledBox(t + 0.08f, 0.06f, len, 0.015f, L.Trim), F.At(x + o * t * 0.5f, top - 0.06f, zc));
            if (b.Detail == SiteDetail.Mass) continue;
            b.Add(PartGeometryKernel.BeveledBox(bed, top - 0.12f - baseY, len, 0.01f, L.Bed), F.At(x + o * (t + bed * 0.5f), baseY, zc));
            b.Add(PartGeometryKernel.BeveledBox(kerb, top - 0.05f - baseY, len, 0.015f, L.Granite), F.At(x + o * (t + bed + kerb * 0.5f), baseY, zc));
        }
    }

    public override void Collide(SiteColliders c)
    {
        float Ground(Vector2 p) => c.Ground(p.X, p.Y);
        foreach (var p in TreePitCentres())
        {
            var g = Ground(p);
            var local = F.Local(p);
            c.Box(F.At(local.X, g + P.TreePitHeight * 0.5f, local.Y), new(P.TreePitSize * 0.5f, P.TreePitHeight * 0.5f, P.TreePitSize * 0.5f), SurfaceTag.Prop, FootMat.StonePaving);
        }
        // One box per tier up to its rim: the low copings can be stepped onto, as a seat edge would.
        var fg = Ground(P.Fountain);
        foreach (var (half, _, rim, _) in FountainTiers())
            c.Box(FountainFrame.At(0, fg + (rim - 0.05f) * 0.5f, 0), new(half.X, (rim + 0.05f) * 0.5f, half.Y), SurfaceTag.Prop, FootMat.StonePaving);
        var bed = P.RoundBedRadius + P.SeatWidth * 0.5f;
        var rg = Ground(P.RoundBed);
        c.Add(new PostCollider(P.RoundBed, bed, rg - 0.1f, rg + P.SeatHeight, SurfaceTag.Prop) { Foot = FootMat.StonePaving });
        foreach (var x in P.RampWallX)
        {
            var o = Outward(x);
            var width = P.RampWallThickness + P.RampBedWidth + P.RampBedKerb;
            foreach (var (z0, z1, baseY, top) in RampRuns(Ground, x))
                c.Box(F.At(x + o * width * 0.5f, (baseY + top) * 0.5f, (z0 + z1) * 0.5f), new(width * 0.5f, (top - baseY) * 0.5f, (z1 - z0) * 0.5f), SurfaceTag.Prop, FootMat.StonePaving);
        }
    }
}
