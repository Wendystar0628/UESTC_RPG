using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Zones.Common;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Gen.Zones.South.Tests;

using static Campus.Gen.Zones.South.Tests.SouthTestSupport;

[TestSuite]
public sealed class SouthSiteTests
{
    [TestCase]
    public void ClockTowerFootprintAcceptsExtraAndCollinearPoints()
    {
        var quad = new List<Vector2> { new(0, 0), new(12, 8), new(5, 17), new(-7, 9) };
        var (f0, hx0, hz0) = Xuanbang.ClockTower.FrameOf(quad);
        Require(ReferenceEquals(Xuanbang.ClockTower.Quadrilateral(quad), quad), "four points pass through untouched");
        var padded = new List<Vector2> { quad[0], (quad[0] + quad[1]) * 0.5f, quad[1], quad[2], (quad[2] + quad[3]) * 0.5f, quad[3], (quad[3] + quad[0]) * 0.5f };
        var (f1, hx1, hz1) = Xuanbang.ClockTower.FrameOf(padded);
        Require(f1.U == f0.U && f1.P(0, 0) == f0.P(0, 0) || (Vector2.Distance(f1.P(0, 0), f0.P(0, 0)) < 0.01f && Vector2.Distance(f1.U, f0.U) < 0.01f), "collinear points leave the frame unchanged");
        Require(MathF.Abs(hx1 - hx0) < 0.01f && MathF.Abs(hz1 - hz0) < 0.01f, "collinear points leave the size unchanged");
        var ragged = new List<Vector2> { new(0, 0), new(10, 0), new(11, 4), new(10, 8), new(0, 8), new(-1, 4) };
        var (_, hx, hz) = Xuanbang.ClockTower.FrameOf(ragged);
        var rect = Xuanbang.ClockTower.Quadrilateral(ragged);
        Require(rect.Count == 4 && hx > 0 && hz > 0 && hx * hz * 4 >= 80f && hx * hz * 4 < 110f, $"non-rectangular footprint gets a bounding rectangle ({hx}, {hz})");
    }

    [TestCase]
    public void GateClosureHasNoGapAboveHalfAMetre()
    {
        var ctx = Context();
        var module = new SouthModule();
        var boundary = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).Where(c => c.Tag == SurfaceTag.Boundary).ToArray();
        Require(boundary.Length >= 11, "houses, islands, carriageway and both turnstile rows are boundary colliders");
        // March along the gate line between the guard houses' walls: every 0.25 m sample is inside some boundary primitive.
        var along = Vector2.Normalize(SouthTune.Gate.HouseEast - SouthTune.Gate.HouseWest);
        var west = SouthTune.Gate.HouseWest + along * (SouthTune.Gate.HouseSize * 0.5f);
        var east = SouthTune.Gate.HouseEast - along * (SouthTune.Gate.HouseSize * 0.5f);
        // The barrier arms stand 1.4 m inside the turnstile rows; sample on the arms' line, which crosses every element.
        var inward = new Vector2(-along.Y, along.X) * SouthTune.Gate.BarrierZ;
        for (var s = 0f; s <= 1f; s += 0.25f / Vector2.Distance(west, east))
        {
            var p = Vector2.Lerp(west, east, s) + inward;
            Require(boundary.Any(c => Covers(c, p)), $"gate line covered at {p}");
        }
        Require(boundary.All(c => c is not BoxCollider box || box.Half.Y * 2 >= WalkRules.BarrierHeight), "boundary boxes reach 1.5 m");
    }

    [TestCase]
    public void GateBollardsAreCrashBollardsInTheirOwnColours()
    {
        // §1.4: yellow-and-black; a tint or a material would paint the black stripe yellow as well (13b, 12:00).
        var bollards = new SouthModule().ItemsFor(Context(), LodLevel.L0).Select(i => i.Part).Where(p => p?.Part.Value == PartCatalog.Bollard).ToArray();
        Require(bollards.Length == 6, $"three bollards off each island nose ({bollards.Length})");
        Require(bollards.All(p => p!.Tint is null && p.Params is ColumnParams { Variation: 1 } c && c.Material == new ColumnParams().Material), "variation 1, no tint, no material");
    }

    [TestCase]
    public void ClockSideDoorOpensOntoAPavedForecourt()
    {
        // 13b review: an open doorway stands on paving at least the door width + 1 m wide and 2 m deep, tied to the nearest paving.
        var world = World();
        var court = world.Features[new("design:south-clock-forecourt")];
        var walk = world.Features[new("design:south-clock-walk")];
        Require(court.Kind.Value == "road" && Text(court, "roadType") == "pedestrian" && Text(walk, "roadType") == "footway", "plaza paving and a footway, drawn by WP-15");
        var door = world.Interiors.Get(new(Xuanbang.XuanbangSite.Feature))!.Entrances.Single(e => e.Id == "xb.clock");
        var yaw = door.YawDeg * MathF.PI / 180f;
        var outward = new Vector2(MathF.Sin(yaw), -MathF.Cos(yaw));
        var across = new Vector2(-outward.Y, outward.X);
        var centre = new Vector2(door.Center.X, door.Center.Z);
        var ring = ((PolygonGeometry)court.Geometry).Rings;
        var u = ring.Outer.Select(p => Vector2.Dot(p - centre, across)).ToArray();
        var v = ring.Outer.Select(p => Vector2.Dot(p - centre, outward)).ToArray();
        var half = SouthTune.Xuanbang.PortalWidth * 0.5f + 0.5f;
        Require(u.Min() <= -half && u.Max() >= half, $"forecourt {u.Max() - u.Min():0.0} m wide, the door {SouthTune.Xuanbang.PortalWidth} m");
        Require(MathF.Abs(v.Min()) < 0.5f && v.Max() >= 2f, $"forecourt from the door face ({v.Min():0.00}) out to {v.Max():0.0} m");
        var line = ((PolylineGeometry)walk.Geometry).Line.Points;
        var parking = (PolygonGeometry)world.Features[new("qsh:way:1383996369")].Geometry;
        Require(PlanGeometry.Contains(ring, line[0]) && PlanGeometry.Contains(parking.Rings, line[^1]), "the walk runs from the forecourt onto the parking by the road");
        Require(SouthTune.Xuanbang.Threshold == 0.15f, "the threshold is level with WP-15's paving top");
    }

    [TestCase]
    public void GatePlazaGivesA01ItsFooting()
    {
        var world = World();
        var plaza = world.Features[new(Gate.GatePlazaSite.Feature)];
        Require(plaza.Kind.Value == "road" && Text(plaza, "roadType") == "pedestrian", "plaza paving, drawn by WP-15 with its kerbs and edging");
        var (frame, across, along) = Gate.GatePlazaSite.FrameOf(world)!.Value;
        Require(MathF.Abs(along * 2f - 18f) < 0.1f && MathF.Abs(across * 2f - 12f) < 0.1f, $"about 18 × 12 m ({along * 2f:0.0} × {across * 2f:0.0})");
        Require(MathF.Abs(ZoneSite.Bearing(frame.W) - 36.8f) < 1f, $"the long side runs along the main axis ({ZoneSite.Bearing(frame.W):0.0}°)");
        var ring = ((PolygonGeometry)plaza.Geometry).Rings;
        // WP-13's kiosk (2 × 0.8 m) on the spot the plaza keeps for A01: on the paving, at least 3 m from every carriageway edge.
        var yaw = SouthTune.Plaza.KioskYaw * MathF.PI / 180f;
        var face = new Vector2(MathF.Sin(yaw), -MathF.Cos(yaw));
        var side = new Vector2(-face.Y, face.X);
        var corners = new[] { -1f, 1f }.SelectMany(u => new[] { -1f, 1f }.Select(v => SouthTune.Plaza.KioskSpot + face * (0.4f * u) + side * (1.0f * v))).ToArray();
        Require(corners.All(p => PlanGeometry.Contains(ring, p)), "the kiosk stands on the plaza");
        var lanes = world.Network.Roads.Where(r => Text(r.Props, "highway") is "residential" or "service" or "tertiary" or "secondary" or "unclassified").ToArray();
        var clearance = corners.Min(p => lanes.Min(r => PolylineDistance(p, r.Line.Points) - r.Width * 0.5f));
        Require(clearance >= 3f, $"the kiosk stands {clearance:0.00} m from the nearest carriageway edge");
        // The planter and benches stay on the paving and clear of the kiosk and the space in front of its panel.
        var items = new SouthModule().ItemsFor(Context(), LodLevel.L0).Where(i => i.Part?.Part.Value is PartCatalog.Bench or PartCatalog.Planter).ToArray();
        Require(items.Length == 3, $"a planter and two benches ({items.Length})");
        Require(items.All(i => PlanGeometry.Contains(ring, i.Anchor) && Vector2.Distance(i.Anchor, SouthTune.Plaza.KioskSpot) > 6f), "furniture on the plaza, clear of the kiosk");
    }

    [TestCase]
    public void XuanbangKeepsItsSkyline()
    {
        var ctx = Context();
        var items = new SouthModule().ItemsFor(ctx, LodLevel.L0).Where(i => i.Mesh is not null).ToArray();
        var f = Xuanbang.XuanbangSite.Rings(ctx.World);
        float Top(IReadOnlyList<Vector2> ring) => items.Where(i => Near(ring, i.Anchor, 3f)).Max(i => MaxY(GeometryPublication.Publish(i.Mesh!, i.HasLodChannel)));
        Require(MathF.Abs(Top(f.Clock) - SouthTune.Xuanbang.Top) < 0.05f, $"clock tower top {Top(f.Clock)}");
        Require(MathF.Abs(Top(f.Tower) - 73.85f) < 0.05f, $"tower top {Top(f.Tower)}");
        var main = items.Where(i => Near(f.Main, i.Anchor, 1f) && !Near(f.Tower, i.Anchor, 3f) && !Near(f.Clock, i.Anchor, 3f)
            && Vector2.Distance(i.Anchor, SouthTune.Xuanbang.Skylight) > 10f).Max(i => MaxY(GeometryPublication.Publish(i.Mesh!, i.HasLodChannel)));
        Require(main > 35.6f && main < 38.8f, $"main wing roof line {main} (35.7 m, gables up to 38.3 m)");
        var all = items.Max(i => MaxY(GeometryPublication.Publish(i.Mesh!, i.HasLodChannel)));
        Require(all < 73.9f, $"the Xuanbang tower stays the highest point of the zone ({all})");
    }

    [TestCase]
    public void LirenGatewayIsOpenUnderTheBridge()
    {
        var ctx = Context();
        var module = new SouthModule();
        var f = Liren.LirenSite.Rings(ctx.World);
        var walls = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).Where(c => c.Tag is SurfaceTag.Wall or SurfaceTag.DoorClosed).ToArray();
        var open = Common.GridFacade.Edges(f.Bridge).Select(e => e.Info).First(e => Liren.LirenSite.Dress(e, f.BridgeNeighbours).Kind != Common.EdgeKind.Hidden);
        var centre = Common.PlanMath.Centroid(f.Bridge);
        for (var t = -14f; t <= 14f; t += 0.25f)
        {
            var p = centre + open.Frame.W * t;
            Require(!walls.Any(c => Covers(c, p, 1.0f)), $"the gateway passage is clear at {p}");
        }
        Require(SouthTune.Liren.Soffit >= 10.5f, "the void is about three storeys high");
    }

    [TestCase]
    public void HardBankBalustradeGuardsTheWaterButTheWadeStep()
    {
        var ctx = Context();
        var module = new SouthModule();
        var barriers = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).OfType<WallCollider>().Where(w => w.Tag == SurfaceTag.Barrier).ToArray();
        var water = EastLake.EastLakeSite.Water(ctx.World);
        Require(EastLake.EastLakeSite.InWater(water, EastLake.EastLakeSite.WadeFrame.P(EastLake.EastLakeSite.WadeLength * 0.5f, 1f)), "the wade frame looks over the water");
        Require(SouthTune.Lake.Tread2 < -0.5f && SouthTune.Lake.Tread2 > -0.8f, "the wade tread is under the -0.5 m water by less than 0.3 m");
        Require(SouthTune.Lake.RailTop >= 1.05f, "balustrade at least 1.05 m");
        foreach (var (f, l, i) in EastLake.EastLakeSite.BankSegments(water))
        {
            var openings = EastLake.EastLakeSite.Openings(i, l);
            for (var u = 0.3f; u <= l - 0.3f; u += 0.25f)
            {
                var p = f.P(u, -SouthTune.Lake.RailSetback);
                var inOpening = openings.Any(o => u > o.U0 + 0.5f && u < o.U1 - 0.5f);
                var nearOpening = openings.Any(o => u > o.U0 - 0.5f && u < o.U1 + 0.5f);
                var guarded = barriers.Any(w => w.Y1 >= WalkRules.BarrierHeight && Covers(w, p, 1.0f));
                if (inOpening) Require(!guarded, $"the wade step at {p} is open");
                else if (!nearOpening) Require(guarded, $"the bank at {p} is guarded");
            }
        }
    }

    /// <summary>A06 on the deck (SYS-ATLAS): x, z and the panel's bearing, the main session's pick, 29 m from the old anchor.</summary>
    private static readonly Vector2 A06Spot = new(-166.5f, 734f);
    private const float A06Yaw = 99f;

    [TestCase]
    public void PlatformDeckCarriesA06AndOpensOnlyWhereStairsLand()
    {
        var ctx = Context();
        var p = Sports.PlatformSite.PlanOf(ctx.World);
        Require(p.Decks.Count == 1, "one deck: A06 moves onto it, no terrace is added");
        Require(SouthTune.Sports.PlatformTop == 5.0f && MathF.Abs(p.Top - p.Y - SouthTune.Sports.PlatformTop) < 0.01f, "the deck top, A06's y, is the 5.0 m platform storey");
        // A06's kiosk (2 × 0.8 m, WP-13's) on the deck between the gym and the natatorium, its panel toward the stadium: every corner
        // on the deck and at least 3 m from the deck's edges and the parapets' inner faces. The anchor data stays as it is until
        // Astra moves it after the merge.
        var stadium = Sports.GrandstandSite.PlanOf(ctx.World).Centre;
        var a = A06Yaw * MathF.PI / 180f;
        var face = new Vector2(MathF.Sin(a), -MathF.Cos(a));
        var side = new Vector2(-face.Y, face.X);
        var corners = new[] { -1f, 1f }.SelectMany(u => new[] { -1f, 1f }.Select(v => A06Spot + face * (0.4f * u) + side * (1.0f * v))).ToArray();
        Require(corners.All(c => PlanGeometry.InsideByCrossings(p.Decks[0], c)), "A06 stands on the deck");
        var parapet = SouthTune.Platform.ParapetInset + SouthTune.Platform.ParapetDepth * 0.5f;
        var clearance = corners.Min(c => p.Edges.Min(e => DistanceToSegment(c, e.A, e.B) - (e.Free ? parapet : 0f)));
        Require(clearance >= 3f, $"A06 keeps {clearance:0.00} m from the deck edges and parapets");
        var turn = Common.PlanMath.Turn(ZoneSite.Bearing(stadium - A06Spot) - A06Yaw);
        Require(MathF.Abs(turn) < 5f, $"A06 faces the stadium ({turn:0.0}° off)");
        Require(p.Stairs.Count == 2 && p.Bays.Count > 20 && p.Columns.Count > 20, $"two stairs, {p.Bays.Count} shop bays, {p.Columns.Count} columns");
        Require(p.Edges.Count(e => !e.Free) > 10 && p.Edges.Count(e => e.Free) > 20, "the edges against the venues stay open, the rest are guarded");
        var module = new SouthModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        Require(colliders.OfType<SlabCollider>().Count(slab => MathF.Abs(slab.Y - p.Top) < 0.01f) == 1, "the deck top is walkable");
        var walls = colliders.OfType<WallCollider>().ToArray();
        foreach (var stair in p.Stairs)
            Require(!walls.Any(w => Covers(w, stair.Frame.P(0, -0.3f), p.Top + 0.8f)), $"a stair lands through an open parapet at {stair.Frame.Origin}");
        var edge = p.Edges.First(e => e.Free && e.Length > 10f && !p.Stairs.Any(s => Vector2.Distance(s.Frame.Origin, e.Mid) < 12f));
        Require(walls.Any(w => Covers(w, edge.Mid - edge.Out * 0.2f, p.Top + 1.2f)), "a free edge carries a parapet to 1.5 m");
    }

    [TestCase]
    public void SportsVenuesKeepTheirProfilesAndTheDeckDoorway()
    {
        var ctx = Context();
        var module = new SouthModule();
        var items = module.ItemsFor(ctx, LodLevel.L0).Where(i => i.Mesh is not null).ToArray();
        var gym = Sports.GymSite.PlanOf(ctx.World);
        var pool = Sports.NatatoriumSite.PlanOf(ctx.World);
        Require(gym.Radius is > 57f and < 62f && gym.Bays > 90, $"gym drum radius {gym.Radius}, {gym.Bays} bays");
        Require(pool.A >= pool.B && pool.B > 35f && pool.A < 60f, $"natatorium ellipse {pool.A} × {pool.B}");
        float Top(Vector2 centre, float radius) => items.Where(i => Vector2.Distance(i.Anchor, centre) < radius).Max(i => MaxY(GeometryPublication.Publish(i.Mesh!, i.HasLodChannel)));
        var gymTop = Top(gym.Centre, gym.Radius);
        Require(gymTop > SouthTune.Sports.GymTop - 0.1f && gymTop < SouthTune.Sports.GymTop + 1.2f, $"gym roof at 26 m, vent caps above it ({gymTop})");
        var poolTop = Top(pool.Frame.Origin, pool.B);
        Require(poolTop is > 14.5f and < 16f, $"natatorium a low dome near 15 m ({poolTop})");
        // The platform-storey doorway (gym.deck) is open between the platform and the door head; the drum is walled elsewhere.
        var walls = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).OfType<WallCollider>().ToArray();
        var deck = gym.Centre + Common.PlanMath.Heading(SouthTune.Sports.GymDoorBearing) * (gym.Radius - SouthTune.Sports.GymBaseInset);
        Require(!walls.Any(w => Covers(w, deck, SouthTune.Sports.PlatformTop + 1.5f)), "the gym's deck doorway is open");
        Require(walls.Any(w => Covers(w, deck, SouthTune.Sports.PlatformTop - 1f)), "the drum is closed below the platform storey");
        var back = gym.Centre - Common.PlanMath.Heading(SouthTune.Sports.GymDoorBearing) * (gym.Radius - SouthTune.Sports.GymBaseInset);
        Require(walls.Any(w => Covers(w, back, SouthTune.Sports.PlatformTop + 1.5f)), "the drum is closed opposite the door");
    }

    private static float PolylineDistance(Vector2 p, IReadOnlyList<Vector2> line)
    {
        var best = float.MaxValue;
        for (var i = 0; i + 1 < line.Count; i++) best = MathF.Min(best, DistanceToSegment(p, line[i], line[i + 1]));
        return best;
    }

    private static string? Text(WorldFeature feature, string name) => Text(feature.Props, name);

    private static string? Text(System.Text.Json.JsonElement? props, string name) =>
        props is { ValueKind: System.Text.Json.JsonValueKind.Object } p && p.TryGetProperty(name, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : null;
}
