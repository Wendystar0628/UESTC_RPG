using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.CenterEast.Framework;
using L = Campus.Gen.Zones.CenterEast.CenterEastTune.Look;
using P = Campus.Gen.Zones.CenterEast.CenterEastTune.Pinxue;

namespace Campus.Gen.Zones.CenterEast.Pinxue;

/// <summary>
/// 品学楼 forecourt (MAP-QSH-CENTER-EAST §2.3; MAP-QSH-TERRAIN §10): the light-grey paving is WP-15 ground and the trees
/// are WP-23's (registered as tree pits in center-east.overlay.json). This site builds what is stone: the two rows of
/// square tree pits, the kerbed lawn panel with the three dark granite tiers of the cascade fountain on the frame's axis
/// (its water is WP-24's, WLD-WATER §9), the seat ring of the round bed and the planter seat walls stepping down both
/// sides of the 5 % ramp, plus the entrance presentation at the A-wing hall door (px.a.hall).
/// </summary>
public sealed class PinxueForecourtSite : CenterEastSite
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
        HallEntrance(b);
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

    // ---- Lawn panel and the cascade fountain's stone tiers ----

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

    private static void Fountain(SiteBuilder b)
    {
        var c = P.Fountain;
        var g = b.Ground(c);
        var n = b.Detail switch { SiteDetail.Full => 40, SiteDetail.Plain => 24, _ => 12 };
        Matrix4x4 At(float y) => Matrix4x4.CreateTranslation(c.X, g + y, c.Y);
        // Bottom basin: floor, a sittable rim wall (from 5 cm below the paving to the coping) and its coping.
        b.Add(Shapes.Shaft(P.BasinRadius, P.BasinRadius, 0.2f, n, L.DarkGranite), At(-0.05f));
        b.Add(Shapes.Ring(c, P.BasinRadius, P.BasinWall, P.BasinRim - 0.01f, n, L.DarkGranite), Matrix4x4.CreateTranslation(0, g - 0.05f, 0));
        b.Add(Shapes.Ring(c, P.BasinRadius, P.BasinWall + 0.1f, 0.06f, n, L.DarkGranite, 0.01f), Matrix4x4.CreateTranslation(0, g + P.BasinRim - 0.06f, 0));
        // Middle tier: pedestal, a flared bowl and its lip.
        b.Add(Shapes.Shaft(0.55f, 0.45f, 1.05f, n, L.DarkGranite), At(0.1f));
        b.Add(Shapes.Shaft(0.6f, P.MiddleRadius, 0.3f, n, L.DarkGranite), At(P.MiddleRim - 0.42f));
        b.Add(Shapes.Ring(c, P.MiddleRadius - 0.06f, 0.14f, 0.12f, n, L.DarkGranite, 0.01f), Matrix4x4.CreateTranslation(0, g + P.MiddleRim - 0.12f, 0));
        // Top tier: a slimmer pedestal, a small bowl, the spout.
        b.Add(Shapes.Shaft(0.32f, 0.26f, 0.6f, n, L.DarkGranite), At(P.MiddleRim - 0.12f));
        b.Add(Shapes.Shaft(0.3f, P.TopRadius, 0.22f, n, L.DarkGranite), At(P.TopRim - 0.32f));
        b.Add(Shapes.Ring(c, P.TopRadius - 0.05f, 0.1f, 0.1f, n, L.DarkGranite, 0.01f), Matrix4x4.CreateTranslation(0, g + P.TopRim - 0.1f, 0));
        if (b.Detail == SiteDetail.Mass) return;
        b.Add(Shapes.Shaft(0.12f, 0.07f, P.SpoutTop - P.TopRim + 0.1f, Math.Max(8, n / 3), L.DarkGranite), At(P.TopRim - 0.1f));
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

    // ---- A-wing hall entrance (px.a.hall): mat, open sign and door lamp; the doorway itself is WP-14's and WP-31b's ----

    private static void HallEntrance(SiteBuilder b)
    {
        if (b.Detail == SiteDetail.Mass) return;
        var y = b.Context.World.Terrain.PadY(new(P.WingA));
        var floor = y + 0.15f;
        var x = Hall.Local(P.HallDoor).X;
        b.Place(PartCatalog.EntranceMat, new PanelParams(2.0f, 0.03f, 1.3f), Hall.At(x, y, 1.0f));
        b.Place(PartCatalog.OpenSign, new SignParams(0.42f, 0.24f, 0.02f), Hall.At(x + 1.5f, floor + 1.45f, 0.3f));
        b.Place(PartCatalog.EntranceLamp, new LampParams(0.3f, 0.3f, 0.2f), Hall.At(x, floor + 2.9f, 0.3f));
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
        var fg = Ground(P.Fountain);
        c.Add(new PostCollider(P.Fountain, P.BasinRadius + P.BasinWall * 0.5f, fg - 0.05f, fg + P.BasinRim, SurfaceTag.Prop) { Foot = FootMat.StonePaving });
        c.Add(new PostCollider(P.Fountain, P.MiddleRadius, fg, fg + P.TopRim, SurfaceTag.Prop) { Foot = FootMat.StonePaving });
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
