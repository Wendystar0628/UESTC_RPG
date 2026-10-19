using Campus.Tests;
using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Zones.Common;
using Campus.Gen.Zones.South.Framework;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Gen.Zones.South.Tests;

using static GdUnit4.Assertions;
using static Campus.Gen.Zones.South.Tests.SouthTestSupport;

[TestSuite]
public sealed class SouthSiteTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void GateClosureHasNoGapAboveHalfAMetre()
    {
        var ctx = Context();
        var module = new SouthModule();
        var boundary = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).Where(c => c.Tag == SurfaceTag.Boundary).ToArray();
        AssertThat(boundary.Length > 0).OverrideFailureMessage("houses, islands, carriageway and both turnstile rows are boundary colliders").IsTrue();
        // March along the gate line between the guard houses' walls: every 0.25 m sample is inside some boundary primitive.
        var gate = Gate.SouthGateSite.LayoutOf(ctx.World);
        var along = Vector2.Normalize(gate.HouseEast - gate.HouseWest);
        var west = gate.HouseWest + along * (SouthTune.Gate.HouseSize * 0.5f);
        var east = gate.HouseEast - along * (SouthTune.Gate.HouseSize * 0.5f);
        // The barrier arms stand 1.4 m inside the turnstile rows; sample on the arms' line, which crosses every element.
        var inward = new Vector2(-along.Y, along.X) * SouthTune.Gate.BarrierZ;
        for (var s = 0f; s <= 1f; s += 0.25f / Vector2.Distance(west, east))
        {
            var p = Vector2.Lerp(west, east, s) + inward;
            AssertThat(boundary.Any(c => Covers(c, p))).OverrideFailureMessage($"gate line covered at {p}").IsTrue();
        }
        AssertThat(boundary.All(c => c is not BoxCollider box || box.Half.Y * 2 >= WalkRules.BarrierHeight)).OverrideFailureMessage("boundary boxes reach 1.5 m").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void GateBollardsAreCrashBollardsInTheirOwnColours()
    {
        // §1.4: yellow-and-black; a tint or a material would paint the black stripe yellow as well (13b, 12:00).
        var bollards = new SouthModule().ItemsFor(Context(), LodLevel.L0).Select(i => i.Part).Where(p => p?.Part.Value == PartCatalog.Bollard).ToArray();
        AssertThat(bollards.Length > 0).OverrideFailureMessage($"three bollards off each island nose ({bollards.Length})").IsTrue();
        AssertThat(bollards.All(p => p!.Tint is null && p.Params is ColumnParams { Variation: 1 } c && c.Material == new ColumnParams().Material)).OverrideFailureMessage("variation 1, no tint, no material").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ClockSideDoorOpensOntoAPavedForecourt()
    {
        // 13b review: an open doorway stands on paving at least the door width + 1 m wide and 2 m deep, tied to the nearest paving.
        var world = RealMapFixture.Load().World;
        var door = world.Interiors.Get(new(Xuanbang.XuanbangSite.Feature))!.Entrances.Single(e => e.Id == "xb.clock");
        var outside = new Vector2(door.Outside.X, door.Outside.Z);
        var court = world.Features.Values.Single(feature => feature.Kind.Value == "road" && feature.TryText("highway") == "pedestrian"
            && feature.Geometry is PolygonGeometry area && PlanGeometry.Contains(area.Rings, outside));
        var walk = world.Features.Values.Single(feature => feature.Kind.Value == "road"
            && feature.TryText("highway") == "footway" && feature.Geometry is PolylineGeometry path
            && PlanGeometry.Contains(((PolygonGeometry)court.Geometry).Rings, path.Line.Points[0]));
        AssertThat(court.Kind.Value == "road" && court.TryText("highway") == "pedestrian" && walk.TryText("highway") == "footway").OverrideFailureMessage("plaza paving and a footway, drawn by WP-15").IsTrue();
        var yaw = door.YawDeg * MathF.PI / 180f;
        var outward = new Vector2(MathF.Sin(yaw), -MathF.Cos(yaw));
        var across = new Vector2(-outward.Y, outward.X);
        var centre = new Vector2(door.Center.X, door.Center.Z);
        var ring = ((PolygonGeometry)court.Geometry).Rings;
        var u = ring.Outer.Select(p => Vector2.Dot(p - centre, across)).ToArray();
        var v = ring.Outer.Select(p => Vector2.Dot(p - centre, outward)).ToArray();
        var half = SouthTune.Xuanbang.PortalWidth * 0.5f + 0.5f;
        AssertThat(u.Min() <= -half && u.Max() >= half).OverrideFailureMessage($"forecourt {u.Max() - u.Min():0.0} m wide, the door {SouthTune.Xuanbang.PortalWidth} m").IsTrue();
        AssertThat(MathF.Abs(v.Min()) < 0.5f && v.Max() >= 2f).OverrideFailureMessage($"forecourt from the door face ({v.Min():0.00}) out to {v.Max():0.0} m").IsTrue();
        var line = ((PolylineGeometry)walk.Geometry).Line.Points;
        var parking = (PolygonGeometry)world.Features[new(SouthIds.ClockParking.Entity)].Geometry;
        AssertThat(PlanGeometry.Contains(ring, line[0]) && PlanGeometry.Contains(parking.Rings, line[^1])).OverrideFailureMessage("the walk runs from the forecourt onto the parking by the road").IsTrue();
        AssertThat(SouthTune.Xuanbang.Threshold == GroundLevels.Paving).OverrideFailureMessage("the threshold is level with WP-15's paving top").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void GatePlazaGivesA01ItsFooting()
    {
        var world = RealMapFixture.Load().World;
        var plaza = world.Features[new(Gate.GatePlazaSite.Feature)];
        AssertThat(plaza.Kind.Value == "road" && plaza.TryText("highway") == "pedestrian").OverrideFailureMessage("plaza paving, drawn by WP-15 with its kerbs and edging").IsTrue();
        var (frame, across, along) = Gate.GatePlazaSite.FrameOf(world)!.Value;
        AssertThat(MathF.Abs(Angles.Bearing(frame.W) - 36.8f) < TestTolerance.AngleDegrees).OverrideFailureMessage($"the long side runs along the main axis ({Angles.Bearing(frame.W):0.0}°)").IsTrue();
        var ring = ((PolygonGeometry)plaza.Geometry).Rings;
        // WP-13's kiosk (2 × 0.8 m) on the A01 anchor (WP-48c's world record): on the paving, at least 3 m from every carriageway edge.
        var a01 = AtlasAnchor(world, "A01");
        var kioskSpot = PlanGeometry.Centroid(((PolygonGeometry)a01.Geometry).Rings.Outer, meanIfDegenerate: true);
        var yaw = a01.Number("yawDeg") * MathF.PI / 180f;
        var face = new Vector2(MathF.Sin(yaw), -MathF.Cos(yaw));
        var side = new Vector2(-face.Y, face.X);
        var corners = new[] { -1f, 1f }.SelectMany(u => new[] { -1f, 1f }.Select(v => kioskSpot + face * (0.4f * u) + side * (1.0f * v))).ToArray();
        AssertThat(corners.All(p => PlanGeometry.Contains(ring, p))).OverrideFailureMessage("the kiosk stands on the plaza").IsTrue();
        var lanes = world.Network.Roads.Where(r => r.Props.TryText("highway") is "residential" or "service" or "tertiary" or "secondary" or "unclassified").ToArray();
        var clearance = corners.Min(p => lanes.Min(r => MathF.Sqrt(PlanLines.PolylineDistanceSquared(r.Line.Points, p)) - r.Width * 0.5f));
        AssertThat(clearance >= 3f).OverrideFailureMessage($"the kiosk stands {clearance:0.00} m from the nearest carriageway edge").IsTrue();
        // The planter and benches stay on the paving and clear of the kiosk and the space in front of its panel.
        var items = new SouthModule().ItemsFor(Context(), LodLevel.L0).Where(i => i.Part?.Part.Value is PartCatalog.Bench or PartCatalog.Planter).ToArray();
        AssertThat(items.Length > 0).OverrideFailureMessage($"the plaza carries its planter and benches ({items.Length})").IsTrue();
        AssertThat(items.All(i => PlanGeometry.Contains(ring, i.Anchor) && Vector2.Distance(i.Anchor, kioskSpot) > 6f)).OverrideFailureMessage("furniture on the plaza, clear of the kiosk").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void XuanbangKeepsItsSkyline()
    {
        var ctx = Context();
        var items = new SouthModule().ItemsFor(ctx, LodLevel.L0).Where(i => i.Mesh is not null).ToArray();
        var f = Xuanbang.XuanbangSite.Rings(ctx.World);
        float Top(IReadOnlyList<Vector2> ring) => items.Where(i => Near(ring, i.Anchor, 3f)).Max(i => MaxY(GeometryPublication.Publish(i.Mesh!, i.HasLodChannel)));
        AssertThat(MathF.Abs(Top(f.Clock) - f.ClockTop) < TestTolerance.Layout).OverrideFailureMessage($"clock tower top {Top(f.Clock)}").IsTrue();
        var towerTop = ctx.World.Feature(Xuanbang.XuanbangSite.Feature).Part(Xuanbang.XuanbangSite.TowerPart).Height!.Value;
        AssertThat(MathF.Abs(Top(f.Tower) - towerTop) < TestTolerance.Layout).OverrideFailureMessage($"tower top {Top(f.Tower)}").IsTrue();
        var main = items.Where(i => Near(f.Main, i.Anchor, 1f) && !Near(f.Tower, i.Anchor, 3f) && !Near(f.Clock, i.Anchor, 3f)
            && Vector2.Distance(i.Anchor, f.Skylight) > 10f).Max(i => MaxY(GeometryPublication.Publish(i.Mesh!, i.HasLodChannel)));
        AssertThat(main > SouthTune.Xuanbang.MainGround + (f.MainStoreys - 1) * SouthTune.Xuanbang.MainStorey).OverrideFailureMessage($"main wing roof line {main} stands above its wall top").IsTrue();
        var all = items.Max(i => MaxY(GeometryPublication.Publish(i.Mesh!, i.HasLodChannel)));
        AssertThat(all < towerTop + TestTolerance.Layout).OverrideFailureMessage($"the Xuanbang tower stays the highest point of the zone ({all})").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void LirenGatewayIsOpenUnderTheBridge()
    {
        var ctx = Context();
        var module = new SouthModule();
        var f = Liren.LirenSite.Rings(ctx.World);
        var walls = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).Where(c => c.Tag is SurfaceTag.Wall or SurfaceTag.DoorClosed).ToArray();
        var open = Common.GridFacade.Edges(f.Bridge).Select(e => e.Info).First(e => Liren.LirenSite.Dress(e, f.BridgeNeighbours).Kind != Common.EdgeKind.Hidden);
        var centre = PlanGeometry.Centroid(f.Bridge, meanIfDegenerate: true);
        for (var t = -14f; t <= 14f; t += 0.25f)
        {
            var p = centre + open.Frame.W * t;
            AssertThat(!walls.Any(c => Covers(c, p, 1.0f))).OverrideFailureMessage($"the gateway passage is clear at {p}").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void HardBankBalustradeGuardsTheWaterButTheWadeStep()
    {
        var ctx = Context();
        var module = new SouthModule();
        var barriers = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).OfType<WallCollider>().Where(w => w.Tag == SurfaceTag.Barrier).ToArray();
        var water = EastLake.EastLakeSite.Water(ctx.World);
        var shore = EastLake.EastLakeSite.ShoreOf(ctx.World);
        // The bank and its collision share the authored platform's terrain height.
        var bankOrigin = shore.Bank[shore.WadeSegment];
        var bankY = ctx.World.Terrain.HeightAt(bankOrigin.X, bankOrigin.Y);
        AssertThat(EastLake.EastLakeSite.InWater(water, shore.WadeFrame.P(shore.WadeLength * 0.5f, 1f))).OverrideFailureMessage("the wade frame looks over the water").IsTrue();
        AssertThat(shore.Tread2 < -0.5f && shore.Tread2 > -0.8f).OverrideFailureMessage("the wade tread is under the -0.5 m water by less than 0.3 m").IsTrue();
        AssertThat(SouthTune.Lake.RailTop >= new Campus.Contracts.Parts.RailingParams().Height).OverrideFailureMessage("balustrade at least the railing part's height").IsTrue();
        foreach (var (f, l, i) in EastLake.EastLakeSite.BankSegments(shore, water))
        {
            var openings = shore.Openings(i, l);
            for (var u = 0.3f; u <= l - 0.3f; u += 0.25f)
            {
                var p = f.P(u, -SouthTune.Lake.RailSetback);
                var inOpening = openings.Any(o => u > o.U0 + 0.5f && u < o.U1 - 0.5f);
                var nearOpening = openings.Any(o => u > o.U0 - 0.5f && u < o.U1 + 0.5f);
                var guarded = barriers.Any(w => w.Y1 >= bankY + WalkRules.BarrierHeight && Covers(w, p, bankY + 1.0f));
                if (inOpening) AssertThat(!guarded).OverrideFailureMessage($"the wade step at {p} is open").IsTrue();
                else if (!nearOpening) AssertThat(guarded).OverrideFailureMessage($"the bank at {p} is guarded").IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PlatformDeckCarriesA06AndOpensOnlyWhereStairsLand()
    {
        var ctx = Context();
        var p = Sports.PlatformSite.PlanOf(ctx.World);
        AssertThat(p.Decks.Count == 1).OverrideFailureMessage("one deck: A06 moves onto it, no terrace is added").IsTrue();
        var a06 = AtlasAnchor(ctx.World, "A06");
        AssertThat(MathF.Abs(p.Top - p.Y - a06.Number("groundY")) < TestTolerance.Position).OverrideFailureMessage("the deck top is A06's y, the platform storey").IsTrue();
        // A06's kiosk (2 × 0.8 m, WP-13's) on the deck between the gym and the natatorium, its panel toward the stadium: every corner
        // on the deck and at least 3 m from the deck's edges and the parapets' inner faces.
        var stadium = Sports.GrandstandSite.PlanOf(ctx.World).Centre;
        var anchor = PlanGeometry.Centroid(a06.Outer(), meanIfDegenerate: true);
        var yaw = a06.Number("yawDeg");
        var a = yaw * MathF.PI / 180f;
        var face = new Vector2(MathF.Sin(a), -MathF.Cos(a));
        var side = new Vector2(-face.Y, face.X);
        var corners = new[] { -1f, 1f }.SelectMany(u => new[] { -1f, 1f }.Select(v => anchor + face * (0.4f * u) + side * (1.0f * v))).ToArray();
        AssertThat(corners.All(c => PlanGeometry.InsideByCrossings(p.Decks[0], c))).OverrideFailureMessage("A06 stands on the deck").IsTrue();
        var parapet = SouthTune.Platform.ParapetInset + SouthTune.Platform.ParapetDepth * 0.5f;
        var clearance = corners.Min(c => p.Edges.Min(e => MathF.Sqrt(PlanLines.SegmentDistanceSquared(c, e.A, e.B)) - (e.Free ? parapet : 0f)));
        AssertThat(clearance >= 3f).OverrideFailureMessage($"A06 keeps {clearance:0.00} m from the deck edges and parapets").IsTrue();
        var turn = Angles.SignedDelta(yaw, Angles.Bearing(stadium - anchor));
        AssertThat(MathF.Abs(turn) < 5 * TestTolerance.AngleDegrees).OverrideFailureMessage($"A06 faces the stadium ({turn:0.0}° off)").IsTrue();
        AssertThat(p.Stairs.Count > 0 && p.Bays.Count > 0 && p.Columns.Count > 0).OverrideFailureMessage($"stairs, {p.Bays.Count} shop bays, {p.Columns.Count} columns").IsTrue();
        AssertThat(p.Edges.Any(e => !e.Free) && p.Edges.Any(e => e.Free)).OverrideFailureMessage("the edges against the venues stay open, the rest are guarded").IsTrue();
        var module = new SouthModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        AssertThat(colliders.OfType<SlabCollider>().Count(slab => MathF.Abs(slab.Y - p.Top) < TestTolerance.Position) == 1).OverrideFailureMessage("the deck top is walkable").IsTrue();
        var walls = colliders.OfType<WallCollider>().ToArray();
        foreach (var stair in p.Stairs)
            AssertThat(!walls.Any(w => Covers(w, stair.Frame.P(0, -0.3f), p.Top + 0.8f))).OverrideFailureMessage($"a stair lands through an open parapet at {stair.Frame.Origin}").IsTrue();
        var edge = p.Edges.First(e => e.Free && e.Length > 10f && !p.Stairs.Any(s => Vector2.Distance(s.Frame.Origin, e.Mid) < 12f));
        AssertThat(walls.Any(w => Covers(w, edge.Mid - edge.Out * 0.2f, p.Top + 1.2f))).OverrideFailureMessage("a free edge carries a parapet to 1.5 m").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void SportsVenuesKeepTheirProfilesAndTheDeckDoorway()
    {
        var ctx = Context();
        var module = new SouthModule();
        var items = module.ItemsFor(ctx, LodLevel.L0).Where(i => i.Mesh is not null).ToArray();
        var gym = Sports.GymSite.PlanOf(ctx.World);
        var pool = Sports.NatatoriumSite.PlanOf(ctx.World);
        AssertThat(pool.A >= pool.B && pool.B > 0f).OverrideFailureMessage($"natatorium ellipse {pool.A} × {pool.B}").IsTrue();
        float Top(Vector2 centre, float radius) => items.Where(i => Vector2.Distance(i.Anchor, centre) < radius).Max(i => MaxY(GeometryPublication.Publish(i.Mesh!, i.HasLodChannel)));
        var gymTop = Top(gym.Centre, gym.Radius);
        AssertThat(gymTop > gym.Top - 0.1f && gymTop < gym.Top + 1.2f).OverrideFailureMessage($"gym roof at the data's height, vent caps above it ({gymTop} against {gym.Top})").IsTrue();
        var poolTop = Top(pool.Frame.Origin, pool.B);
        AssertThat(poolTop >= SouthTune.Sports.PoolCapProfile[^1].Y - TestTolerance.Layout).OverrideFailureMessage($"natatorium dome reaches its crown ({poolTop})").IsTrue();
        // The platform-storey doorway (gym.deck) is open between the platform and the door head; the drum is walled elsewhere.
        var walls = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).OfType<WallCollider>().ToArray();
        var deck = gym.Centre + Angles.Heading(SouthTune.Sports.GymDoorBearing) * (gym.Radius - SouthTune.Sports.GymBaseInset);
        AssertThat(!walls.Any(w => Covers(w, deck, gym.DeckTop + 1.5f))).OverrideFailureMessage("the gym's deck doorway is open").IsTrue();
        AssertThat(walls.Any(w => Covers(w, deck, gym.DeckTop - 1f))).OverrideFailureMessage("the drum is closed below the platform storey").IsTrue();
        var back = gym.Centre - Angles.Heading(SouthTune.Sports.GymDoorBearing) * (gym.Radius - SouthTune.Sports.GymBaseInset);
        AssertThat(walls.Any(w => Covers(w, back, gym.DeckTop + 1.5f))).OverrideFailureMessage("the drum is closed opposite the door").IsTrue();
    }

    private static WorldFeature AtlasAnchor(IWorldModel world, string number)
    {
        using var markers = JsonDocument.Parse(RealMapFixture.ReadFiles()["atlas/markers.json"]);
        var marker = markers.RootElement.GetProperty("markers").EnumerateArray()
            .Single(marker => marker.GetProperty("id").GetString() == number);
        return world.Features[new(marker.GetProperty("feature").GetString()!)];
    }
}
