using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation.Geometry;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>Which sides of a straight flight are open (stringer, balustrade, barrier) rather than against a wall.</summary>
internal readonly record struct Sides(bool OpenLeft, bool OpenRight);

/// <summary>Shared straight-flight builder for StairStraight and StairOpen.</summary>
internal static class StraightFlight
{
    internal static Flight Of(StairParams p)
    {
        if (p.Steps < 3 || p.Steps > 18) throw new ArgumentOutOfRangeException(nameof(p), "An indoor flight has 3 to 18 risers.");
        var f = new Flight(p.Steps, p.Height / p.Steps, p.Depth / p.Steps);
        CheckProportions(f);
        return f;
    }

    /// <summary>Indoor proportions: riser 0.1–0.2 m, tread 0.22–0.4 m, pitch no steeper than the 40° gate (WLD-INTERIORS).</summary>
    internal static void CheckProportions(Flight f)
    {
        if (!(f.Riser >= 0.1f && f.Riser <= 0.2f)) throw new ArgumentOutOfRangeException(nameof(f), $"Riser {f.Riser} m is outside 0.1–0.2 m.");
        if (!(f.Tread >= 0.22f && f.Tread <= 0.4f)) throw new ArgumentOutOfRangeException(nameof(f), $"Tread {f.Tread} m is outside 0.22–0.4 m.");
        if (!(f.Slope <= 0.839f)) throw new ArgumentOutOfRangeException(nameof(f), "Flight is steeper than 40°.");
    }

    internal static Box3 Bounds(StairParams p) => new(new(-p.Width * 0.5f, -0.4f, -p.Depth * 0.5f),
        new(p.Width * 0.5f, p.Height + S.HandrailHeight + S.HandrailKnob + 0.01f, p.Depth * 0.5f));

    internal static void Compose(MeshBuilder mesh, StairParams p, Sides sides, Detail detail, ToonSpec body)
    {
        var f = Of(p);
        var w = p.Width * 0.5f;
        var lane = new FlightLane(f, -w + (sides.OpenLeft ? FlightLane.StringerCover : 0), w - (sides.OpenRight ? FlightLane.StringerCover : 0), -p.Depth * 0.5f, 1, 0);
        lane.Solid(mesh, body, detail, landingFoot: false, topJoint: 0, Frame.Identity);
        if (detail == Detail.Mass) return;
        foreach (var (open, sign) in new[] { (sides.OpenLeft, -1f), (sides.OpenRight, 1f) })
        {
            var edge = sign * w;
            if (open)
            {
                lane.Stringer(mesh, body, edge, sign, detail, Frame.Identity);
                var x = edge - sign * S.StringerWidth * 0.5f;
                var rail = lane.RailPath(f.Tread * 0.5f, f.Run - 0.03f);
                RailKit.Handrail(mesh, rail, x, _ => 0, detail, Frame.Identity, posts: false);
                RailKit.Balusters(mesh, rail, x, lane.BalusterStations(), detail, Frame.Identity);
            }
            else
                // Wall-side handrail; the wall itself belongs to the building.
                RailKit.Handrail(mesh, lane.RailPath(f.Tread * 0.5f, f.Run - 0.03f), edge - sign * 0.06f, _ => 0, detail, Frame.Identity, posts: false);
        }
    }

    internal static IReadOnlyList<Collider> Colliders(StairParams p, Sides sides)
    {
        var f = Of(p);
        var w = p.Width * 0.5f;
        var lane = new FlightLane(f, -w, w, -p.Depth * 0.5f, 1, 0);
        var foot = p.Foot == FootMat.Auto ? S.IndoorStairFoot : p.Foot;
        var list = new List<Collider> { lane.Ramp(-w, w) with { Foot = foot } };
        var inset = w - S.BarrierMinHalfThickness;
        if (sides.OpenLeft) list.Add(lane.OpenSide(-inset) with { Foot = S.MetalFoot });
        if (sides.OpenRight) list.Add(lane.OpenSide(inset) with { Foot = S.MetalFoot });
        return list;
    }
}

/// <summary>
/// Straight indoor flight (WLD-INTERIORS group C) in the WP-51a convention: Steps risers over Depth = Steps × tread,
/// the strip [0, tread] at the front is the lower floor and the last riser is the upper slab's edge. Height is floor to
/// floor. Variation: 0 wall on −X and balustrade on +X, 1 walls on both sides (wall handrails only), 2 open both sides.
/// Collision: the WP-51a RampCollider (26–34°, top flush, bottom buried 0.2 m, StairSpec) and a SlopedWallCollider on
/// every open side whose top is 1.5 m over the nosings.
/// </summary>
[Part(PartCatalog.StairStraight, Owner = "WP-11")]
public sealed class StairStraightPart() : CirculationPart<StairParams>(new(660, 500))
{
    private static Sides SidesOf(int variation) => variation switch
    {
        0 => new(false, true),
        1 => new(false, false),
        2 => new(true, true),
        _ => throw new ArgumentOutOfRangeException(nameof(variation), variation, "Stair variation must be 0–2."),
    };

    private protected override Box3 BoundsOf(StairParams p) => StraightFlight.Bounds(p);
    private protected override IReadOnlyList<Collider> Colliders(StairParams p) => StraightFlight.Colliders(p, SidesOf(p.Variation));
    private protected override void Compose(MeshBuilder mesh, StairParams p, Detail detail) =>
        StraightFlight.Compose(mesh, p, SidesOf(p.Variation), detail, Pick(p.Material, S.StairBody));
}

/// <summary>
/// Open straight flight for atria and stair halls (WLD-INTERIORS "宽梯井"): both sides carry a stringer, balusters
/// and handrail, and both sides get a SlopedWallCollider. Parameters as <see cref="StairStraightPart"/>.
/// </summary>
[Part(PartCatalog.StairOpen, Owner = "WP-11")]
public sealed class StairOpenPart() : CirculationPart<StairParams>(new(860, 660))
{
    private static readonly Sides Open = new(true, true);
    private protected override Box3 BoundsOf(StairParams p) => StraightFlight.Bounds(p);
    private protected override IReadOnlyList<Collider> Colliders(StairParams p) => StraightFlight.Colliders(p, Open);
    private protected override void Compose(MeshBuilder mesh, StairParams p, Detail detail) =>
        StraightFlight.Compose(mesh, p, Open, detail, Pick(p.Material, S.StairBody));
}

/// <summary>
/// Dog-leg stair of one storey (WLD-INTERIORS, the golden-sample stair), laid out exactly like WP-51a's plan:
/// Width = 2 flights + a 0.2 m well, Height = floor to floor, Steps = risers per flight, Depth = flight run + landing,
/// where the landing is max(1.5 m, flight width) deep and the tread is what remains over Steps. The lower floor is
/// in front (−Z); flight 1 climbs to the landing at the back, flight 2 returns to the upper slab edge at the front.
/// Variation: 0 flight 1 on −X, 1 flight 1 on +X (mirror).
/// Collision equals WP-51a's: two RampColliders, the landing SlabCollider and a full wall in the well from the lower
/// floor to 1.5 m above the upper floor.
/// </summary>
[Part(PartCatalog.StairDouble, Owner = "WP-11")]
public sealed class StairDoublePart() : CirculationPart<StairParams>(new(1200, 900))
{
    private readonly record struct Layout(float W, float H, float D, float FlightWidth, float Landing, Flight Flight, float Mirror)
    {
        internal float LandingZ => -D * 0.5f + Flight.Run;
        internal (float X0, float X1) Lane(int index)
        {
            var a = index == 1 ? -W * 0.5f : W * 0.5f - FlightWidth;
            var b = a + FlightWidth;
            return Mirror > 0 ? (a, b) : (-b, -a);
        }
        /// <summary>Well-side edge of a flight and the direction from that flight toward the well.</summary>
        internal (float Edge, float Toward) WellEdge(int index) =>
            index == 1 ? (Mirror * (-W * 0.5f + FlightWidth), Mirror) : (Mirror * (W * 0.5f - FlightWidth), -Mirror);
        internal FlightLane Lane1(float inner) => Make(1, inner, -D * 0.5f, 1, 0);
        internal FlightLane Lane2(float inner) => Make(2, inner, LandingZ, -1, H * 0.5f);
        private FlightLane Make(int index, float inner, float z0, float dir, float y0)
        {
            var (x0, x1) = Lane(index);
            var (_, toward) = WellEdge(index);
            return toward > 0 ? new(Flight, x0, x1 - inner, z0, dir, y0) : new(Flight, x0 + inner, x1, z0, dir, y0);
        }
    }

    private static Layout Plan(StairParams p)
    {
        if (p.Steps < 3 || p.Steps > 18) throw new ArgumentOutOfRangeException(nameof(p), "Each flight has 3 to 18 risers.");
        var flightWidth = (p.Width - S.WellWidth) * 0.5f;
        Range(flightWidth, 0.9f, 4f, "flight width");
        var landing = Math.Max(S.StairLandingMin, flightWidth);
        var flight = new Flight(p.Steps, p.Height * 0.5f / p.Steps, (p.Depth - landing) / p.Steps);
        StraightFlight.CheckProportions(flight);
        var mirror = p.Variation switch
        {
            0 => 1f,
            1 => -1f,
            2 => 1f,
            _ => throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Stair variation must be 0–2."),
        };
        return new(p.Width, p.Height, p.Depth, flightWidth, landing, flight, mirror);
    }

    private protected override Box3 BoundsOf(StairParams p) => new(new(-p.Width * 0.5f, -0.4f, -p.Depth * 0.5f),
        new(p.Width * 0.5f, p.Height + S.HandrailHeight + S.HandrailKnob + 0.01f, p.Depth * 0.5f));

    private protected override IReadOnlyList<Collider> Colliders(StairParams p)
    {
        var l = Plan(p);
        var foot = p.Foot == FootMat.Auto ? S.IndoorStairFoot : p.Foot;
        var one = l.Lane1(0); var two = l.Lane2(0);
        var (wellEdge, toward) = l.WellEdge(1);
        var wellX = wellEdge + toward * S.WellWidth * 0.5f;
        float w = l.W * 0.5f, z0 = l.LandingZ, z1 = l.D * 0.5f;
        return
        [
            one.Ramp(one.X0, one.X1) with { Foot = foot },
            two.Ramp(two.X0, two.X1) with { Foot = foot },
            new SlabCollider([new(-w, z0), new(w, z0), new(w, z1), new(-w, z1)], l.H * 0.5f, S.LandingThickness, SurfaceTag.Ground) { Foot = foot },
            new WallCollider(new(wellX, -l.D * 0.5f), new(wellX, z0), S.WellWidth * 0.5f, 0, l.H + S.StairBarrierAbove, SurfaceTag.Barrier) { Foot = S.MetalFoot },
        ];
    }

    private protected override void Compose(MeshBuilder mesh, StairParams p, Detail detail)
    {
        var l = Plan(p);
        var body = Pick(p.Material, S.StairBody);
        var one = l.Lane1(FlightLane.StringerCover); var two = l.Lane2(FlightLane.StringerCover);
        one.Solid(mesh, body, detail, landingFoot: false, topJoint: S.Joint, Frame.Identity);
        two.Solid(mesh, body, detail, landingFoot: true, topJoint: 0, Frame.Identity);
        mesh.Box(body, new(-l.W * 0.5f, l.H * 0.5f - S.LandingThickness, l.LandingZ), new(l.W * 0.5f, l.H * 0.5f, l.D * 0.5f),
            detail == Detail.Full ? S.NosingBevel : 0, Frame.Identity);
        if (detail == Detail.Mass) return;
        var (edge1, toward1) = l.WellEdge(1); var (edge2, toward2) = l.WellEdge(2);
        one.Stringer(mesh, body, edge1, toward1, detail, Frame.Identity);
        two.Stringer(mesh, body, edge2, toward2, detail, Frame.Identity);
        // Balustrade on the well: both rails reach the landing edge 0.9 m up and meet over the well.
        var turn = S.HandrailRadius * 2 + 0.04f;
        var x1 = edge1 - toward1 * S.StringerWidth * 0.5f; var x2 = edge2 - toward2 * S.StringerWidth * 0.5f;
        var rail1 = one.RailPath(l.Flight.Tread * 0.5f, l.Flight.Run, levelAfter: turn);
        var rail2 = two.RailPath(0, l.Flight.Run - 0.03f, levelBefore: turn);
        // Each rail's landing end meets the connector over the well; only the free ends get knobs.
        RailKit.Handrail(mesh, rail1, x1, _ => 0, detail, Frame.Identity, posts: false, capLast: false);
        RailKit.Handrail(mesh, rail2, x2, _ => 0, detail, Frame.Identity, posts: false, capLast: false);
        if (detail == Detail.Full || detail == Detail.Plain)
        {
            var profile = Profiles.Circle(S.HandrailRadius, detail == Detail.Full ? S.HandrailSegments : 8);
            var y = l.H * 0.5f + S.HandrailHeight; var z = l.LandingZ + turn;
            mesh.Add(Profiles.Bar(profile, [new(Math.Min(x1, x2), z), new(Math.Max(x1, x2), z)], S.MetalTube, y), Frame.Identity, MeshBuilder.NoShadow);
        }
        RailKit.Balusters(mesh, rail1, x1, one.BalusterStations(), detail, Frame.Identity);
        RailKit.Balusters(mesh, rail2, x2, two.BalusterStations(), detail, Frame.Identity);
    }
}
