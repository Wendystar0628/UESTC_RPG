using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Interior.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class InteriorPlanTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void FacadeGridIgnoresRingStartWindingAndCollinearVertices()
    {
        var ring = Rectangle();
        FacadeGrid first = FacadeGridGenerator.Generate(new(ring));
        FacadeGrid equivalent = FacadeGridGenerator.Generate(new(new Vector2[]
            { ring[2], ring[1], new(18, 0), ring[0], ring[3], ring[2] }));
        AssertThat(equivalent.GridHash).IsEqual(first.GridHash);
        AssertThat(EqualityComparer<int>.Default.Equals(first.Bays.Count, equivalent.Bays.Count)).OverrideFailureMessage($"Expected {first.Bays.Count}; got {equivalent.Bays.Count}.").IsTrue();
        for (int i = 0; i < first.Bays.Count; i++) AssertThat(EqualityComparer<FacadeBay>.Default.Equals(first.Bays[i], equivalent.Bays[i])).OverrideFailureMessage($"Expected {first.Bays[i]}; got {equivalent.Bays[i]}.").IsTrue();
        foreach (FacadeBay bay in first.Bays)
        {
            AssertThat(EqualityComparer<int>.Default.Equals(bay.Index + first.Bays.Count, bay.WindowIndex(1, first.Bays.Count))).OverrideFailureMessage($"Expected {bay.Index + first.Bays.Count}; got {bay.WindowIndex(1, first.Bays.Count)}.").IsTrue();
            AssertThat(bay.Width <= 4.201f).OverrideFailureMessage("A facade bay exceeds the configured maximum width.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FacadeGridKeepsCourtyardNormalsPointingIntoTheBuilding()
    {
        Vector2[] hole = [new(10, 4), new(20, 4), new(20, 9), new(10, 9)];
        FacadeGrid grid = FacadeGridGenerator.Generate(new(Rectangle(), [hole]));
        foreach (FacadeBay bay in grid.Bays.Where(b => b.Ring == 1))
        {
            Vector2 point = bay.Center + bay.Inward * 0.1f;
            AssertThat(!(point.X > 10 && point.X < 20 && point.Y > 4 && point.Y < 9)).OverrideFailureMessage("A courtyard normal faces the void.").IsTrue();
        }
        AssertThrown(() => FacadeGridGenerator.Generate(new(Rectangle(), [Rectangle()]))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => FacadeGridGenerator.Generate(new(new Vector2[]
            { new(0, 0), new(10, 10), new(0, 10), new(10, 0) }))).IsInstanceOf<ArgumentException>();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void GoldWingHasExplicitOpenRoomsAndNoBakedMovingLeaves()
    {
        InteriorPlan plan = InteriorPlanGenerator.Generate(Request());
        AssertThat(EqualityComparer<int>.Default.Equals(2, plan.Storeys.Count)).OverrideFailureMessage($"Expected {2}; got {plan.Storeys.Count}.").IsTrue();
        AssertThat(EqualityComparer<int>.Default.Equals(2, plan.Rooms.Count(r => r.Open))).OverrideFailureMessage($"Expected {2}; got {plan.Rooms.Count(r => r.Open)}.").IsTrue();
        AssertThat(plan.Rooms.Count > 2).OverrideFailureMessage("The wing has no closed rooms besides the two selected.").IsTrue();
        AssertThat(plan.Doors.Count >= 2).OverrideFailureMessage("The selected rooms have no doors.").IsTrue();
        AssertThat(EqualityComparer<int>.Default.Equals(2, plan.Stairs.Count)).OverrideFailureMessage($"Expected {2}; got {plan.Stairs.Count}.").IsTrue();
        AssertThat(plan.Rooms.Where(r => !r.Open).All(r => plan.Portals.Any(p => p.ZoneB == r.ZoneId && p.Kind == PortalKind.Sealed))).OverrideFailureMessage("A non-selected room was silently opened.").IsTrue();
        foreach (DoorLeafDecl door in plan.Doors)
        {
            PortalDecl portal = plan.Portals.Single(p => p.ZoneA == door.ZoneA && p.ZoneB == door.ZoneB && p.Kind == PortalKind.Door);
            Vector3 center = (portal.A + portal.B) * 0.5f + Vector3.UnitY;
            AssertThat(!InteriorPlanCollision.BuildStorey(plan, door.Storey).OfType<WallCollider>().Any(w => Covers(w, center))).OverrideFailureMessage("An automatic doorway is blocked by a baked wall or leaf.").IsTrue();
        }
        AssertThat(InteriorPlanCollision.BuildStorey(plan, 2).All(c => c.Foot != FootMat.Auto)).OverrideFailureMessage("A collider lacks a foot material.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void ExteriorEntranceHeadUsesAbsoluteWorldHeightAndUnknownHeadKeepsDefault()
    {
        InteriorPlanRequest request = Request();
        const float floorY = 20f;
        foreach (bool open in new[] { true, false })
        foreach (float headY in new[] { 0f, floorY + 2.8f })
        {
            EntranceDecl entrance = request.Entrances[0];
            entrance = entrance with
            {
                Center = entrance.Center + Vector3.UnitY * floorY,
                Inside = entrance.Inside + Vector3.UnitY * floorY,
                Outside = entrance.Outside + Vector3.UnitY * floorY,
                HeadY = headY, Open = open
            };
            InteriorPlan plan = InteriorPlanGenerator.Generate(request with
            {
                Storeys = request.Storeys.Select(s => s with { FloorY = s.FloorY + floorY }).ToArray(),
                Entrances = [entrance]
            });
            PortalDecl portal = plan.Portals.Single(p => p.ZoneA == -1 && p.Kind != PortalKind.Window);
            float expected = headY == 0f ? request.Options.DoorHeight : headY - floorY;
            AssertThat(MathF.Abs(portal.Height - expected) <= TestTolerance.Position).OverrideFailureMessage("Entrance head was treated as a relative height or ignored.").IsTrue();
            AssertThat(plan.Walls.Any(w => w.Role == InteriorWallRole.Exterior &&
                MathF.Abs(w.BottomY - floorY - expected) <= TestTolerance.Position &&
                Covers(new(w.A, w.B, w.Thickness * .5f, w.BottomY, w.TopY, SurfaceTag.Wall),
                    entrance.Center + Vector3.UnitY * (expected + TestTolerance.Position)))).OverrideFailureMessage("The entrance lintel does not follow its portal head.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void StairFlightsMeetLandingsAndUpperFloorsHaveRealHoles()
    {
        InteriorPlan plan = InteriorPlanGenerator.Generate(Request());
        foreach (InteriorStair stair in plan.Stairs)
        {
            AssertThat(EqualityComparer<float>.Default.Equals(stair.MidLanding.TopY, stair.UpFlight.P2.Y)).OverrideFailureMessage($"Expected {stair.MidLanding.TopY}; got {stair.UpFlight.P2.Y}.").IsTrue();
            AssertThat(EqualityComparer<float>.Default.Equals(3.6f, stair.ReturnFlight.P2.Y)).OverrideFailureMessage($"Expected {3.6f}; got {stair.ReturnFlight.P2.Y}.").IsTrue();
            AssertThat(stair.UpFlight.P0.Y <= -0.199f).OverrideFailureMessage("Stair bottom is not buried.").IsTrue();
            AssertThat(stair.ReturnFlight.P0.Y <= stair.MidLanding.TopY - 0.199f).OverrideFailureMessage("Return flight is not buried into the mid-landing.").IsTrue();
            AssertThat(EqualityComparer<int>.Default.Equals(11, stair.UpFlight.Stair!.Value.Count)).OverrideFailureMessage($"Expected {11}; got {stair.UpFlight.Stair!.Value.Count}.").IsTrue();
            Vector2 a = stair.FloorOpening[0], c = stair.FloorOpening[2];
            foreach (float fraction in new[] { 0.1f, 0.5f, 0.9f })
            {
                Vector2 sample = a + (c - a) * fraction;
                AssertThat(!plan.Slabs.Where(s => s.Storey == 2).Any(s => InAxisAlignedRing(s.Ring, sample))).OverrideFailureMessage("An upper floor slab covers the stair opening.").IsTrue();
            }
            AssertThat(plan.Walls.Any(w => w.Storey == 2 && w.Role == InteriorWallRole.StairBarrier && w.TopY >= 5.1f)).OverrideFailureMessage("The upper stair opening has no fall barrier.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void IntermediateStoreyStairEntryStaysClearWhileTerminalStoreyIsGuarded()
    {
        InteriorPlanRequest request = Request();
        InteriorPlan plan = InteriorPlanGenerator.Generate(request with
        {
            Storeys = [new(1, 0, 3.6f, Rectangle()), new(2, 3.6f, 3.6f, Rectangle()), new(3, 7.2f, 3.6f, Rectangle())]
        });
        WallCollider[] middleWalls = InteriorPlanCollision.BuildStorey(plan, 2).OfType<WallCollider>().ToArray();
        WallCollider[] terminalWalls = InteriorPlanCollision.BuildStorey(plan, 3).OfType<WallCollider>().ToArray();
        InteriorStair[] outgoing = plan.Stairs.Where(s => s.LowerStorey == 2).ToArray();
        AssertThat(EqualityComparer<int>.Default.Equals(2, outgoing.Length)).OverrideFailureMessage($"Expected {2}; got {outgoing.Length}.").IsTrue();
        foreach (InteriorStair stair in outgoing)
        {
            RampCollider ramp = stair.UpFlight;
            float floorT = (3.6f - ramp.P0.Y) / (ramp.P3.Y - ramp.P0.Y);
            Vector2 run = new(ramp.P3.X - ramp.P0.X, ramp.P3.Z - ramp.P0.Z);
            float runLength = run.Length();
            foreach (float across in new[] { 0.25f, 0.5f, 0.75f })
            {
                Vector3 low = Vector3.Lerp(ramp.P0, ramp.P1, across);
                Vector3 high = Vector3.Lerp(ramp.P3, ramp.P2, across);
                // Check the actual baked walls along a 0.6 m-wide walker's approach, threshold and ascent.
                foreach (float offset in new[] { -0.4f, 0f, 0.4f })
                {
                    Vector3 feet = Vector3.Lerp(low, high, floorT + offset / runLength);
                    feet.Y = MathF.Max(3.6f, feet.Y);
                    AssertThat(!middleWalls.Any(w => BlocksWalkingCylinder(w, feet))).OverrideFailureMessage("A wall blocks the 2F-to-3F first flight even though its portal graph is connected.").IsTrue();
                }
                Vector3 terminalFeet = Vector3.Lerp(low, high, floorT);
                terminalFeet.Y = 7.2f;
                AssertThat(terminalWalls.Any(w => BlocksWalkingCylinder(w, terminalFeet))).OverrideFailureMessage("The final storey must still guard the first-flight opening where no upward flight follows.").IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PlanHashCoversOpeningsSeedsAndParametersAndIgnoresInputOrdering()
    {
        InteriorPlanRequest request = Request();
        InteriorPlan original = InteriorPlanGenerator.Generate(request);
        InteriorPlan reordered = InteriorPlanGenerator.Generate(request with
        {
            Storeys = request.Storeys.Reverse().ToArray(),
            Rooms = request.Rooms.Reverse().ToArray(),
            Footprint = new(request.Footprint.Outer.Reverse().ToArray())
        });
        AssertThat(reordered.PlanHash).IsEqual(original.PlanHash);
        AssertThat(original.PlanHash != InteriorPlanGenerator.Generate(request with { WorldSeed = 99 }).PlanHash).OverrideFailureMessage("World seed is absent from the hash.").IsTrue();
        AssertThat(original.PlanHash != InteriorPlanGenerator.Generate(request with { Rooms = [] }).PlanHash).OverrideFailureMessage("Opening decisions are absent from the hash.").IsTrue();
        AssertThat(original.PlanHash != InteriorPlanGenerator.Generate(request with
        { Options = request.Options with { DoorHeight = 2.2f } }).PlanHash).OverrideFailureMessage("Door geometry is absent from the hash.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void RotatedWingAndReservedZoneRangeRemainUsable()
    {
        InteriorPlanRequest request = Request();
        Vector2 Transform(Vector2 p) => new(100f + p.X * 0.8f - p.Y * 0.6f, -200f + p.X * 0.6f + p.Y * 0.8f);
        PolygonRings rotated = new(request.Footprint.Outer.Select(Transform).ToArray());
        // No entry is supplied here: a composing provider connects this wing through its authored hall.
        InteriorPlan plan = InteriorPlanGenerator.Generate(request with { Footprint = rotated, Entrances = [], Rooms = [], ZoneIdBase = 1000 });
        AssertThat(EqualityComparer<int>.Default.Equals(1000, plan.Zones.Min(z => z.Id))).OverrideFailureMessage($"Expected {1000}; got {plan.Zones.Min(z => z.Id)}.").IsTrue();
        AssertThat(plan.Zones.All(z => z.Ring.All(p => p.X > 90 && p.Y < -160))).OverrideFailureMessage("The rotated plan escaped its wing.").IsTrue();
        AssertThat(plan.Doors.All(d => d.ZoneA >= 1000 && d.ZoneB >= 1000)).OverrideFailureMessage("Composed wing IDs were not offset consistently.").IsTrue();
        AssertThat(plan.Stairs.All(s => s.LowerZone >= 1000 && s.UpperZone >= 1000)).OverrideFailureMessage("Stair links use local IDs.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void InvalidFootprintsFloorsAndUnmatchedSelectionsFailExplicitly()
    {
        InteriorPlanRequest request = Request();
        AssertThrown(() => InteriorPlanGenerator.Generate(request with
        { Footprint = new(new Vector2[] { new(0, 0), new(36, 0), new(36, 13.6f), new(18, 13.6f), new(18, 8), new(0, 8) }) })).IsInstanceOf<ArgumentException>();
        AssertThrown(() => InteriorPlanGenerator.Generate(request with
        { Storeys = [new(1, 0, 3.6f, Rectangle()), new(3, 7.2f, 3.6f, Rectangle())] })).IsInstanceOf<ArgumentException>();
        AssertThrown(() => InteriorPlanGenerator.Generate(request with
        { Rooms = [new(2, int.MaxValue, "dorm.four", true)] })).IsInstanceOf<ArgumentException>();
        EntranceDecl entry = request.Entrances[0] with { Center = new(10, 0, 6.8f) };
        AssertThrown(() => InteriorPlanGenerator.Generate(request with { Entrances = [entry] })).IsInstanceOf<ArgumentException>();
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void LocaleDoesNotChangeThePlanHashOrDoorKeys()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            InteriorPlan expected = InteriorPlanGenerator.Generate(Request());
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            InteriorPlan actual = InteriorPlanGenerator.Generate(Request());
            AssertThat(actual.PlanHash).IsEqual(expected.PlanHash);
            AssertThat(EqualityComparer<string>.Default.Equals(string.Join(';', expected.Doors.Select(d => d.Id)), string.Join(';', actual.Doors.Select(d => d.Id)))).OverrideFailureMessage($"Expected {string.Join(';', expected.Doors.Select(d => d.Id))}; got {string.Join(';', actual.Doors.Select(d => d.Id))}.").IsTrue();
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ConvexWingKeepsSlopingWorldEdgesAndUsesThreePointTwoMetreStoreys()
    {
        var footprint=new PolygonRings([new(0,0),new(36,0),new(40,15),new(0,13.6f)]);
        var request=Request() with {Footprint=footprint,Storeys=[new(1,0,3.2f,footprint.Outer),new(2,3.2f,3.2f,footprint.Outer)]};
        var plan=InteriorPlanGenerator.Generate(request);
        AssertThat(plan.Storeys.All(s=>s.Height==3.2f)&&plan.Storeys[1].FloorY==3.2f).IsTrue();
        AssertThat(plan.Stairs.All(s => s.UpFlight.Stair is { Count: 10 } && s.ReturnFlight.Stair is { Count: 10 })).IsTrue();
        var floor=plan.Slabs.Where(s=>s.Storey==1).Sum(s=>Math.Abs(Campus.Gen.Geometry.PlanRings.SignedArea(s.Ring)));
        var footprintArea=Math.Abs(Campus.Gen.Geometry.PlanRings.SignedArea(footprint.Outer));
        AssertThat(Math.Abs(floor-footprintArea)/footprintArea<TestTolerance.FloatRelative).IsTrue();
        AssertThat(plan.Rooms.SelectMany(r=>r.ClearRing).Any(p=>p.Y>13.6f)).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void GeneratedWallsKeepThinWindowFillsAndTheirCollisionDimensions()
    {
        InteriorPlanRequest request = Request();
        InteriorPlan plan = InteriorPlanGenerator.Generate(request with
        { Storeys = request.Storeys.Select(s => s with { FloorY = (s.Index - 1) * 3.2f, Height = 3.2f }).ToArray() });
        AssertThat(plan.Walls.Any(w => w.Role == InteriorWallRole.WindowGlass && w.Thickness < 2 * PartStyleDefaults.TrimEdgeBevel)).IsTrue();
        foreach (InteriorWallSpan span in plan.Walls)
        {
            var sink = new InteriorGeometry();
            var material = span.Role == InteriorWallRole.WindowGlass ? PartMaterials.Glazing() : new ToonSpec(new("wall.lit"), new("wall.shade"));
            sink.Wall(span.A, span.B, span.BottomY, span.TopY, span.Thickness, material);
            var meshes = sink.Finish().Meshes;
            AssertThat(meshes.Count > 0 && meshes.All(m => m.Index.Length > 0 && m.Position.All(float.IsFinite)))
                .OverrideFailureMessage($"A valid {span.Role} wall disappeared or produced invalid vertices.").IsTrue();
            AssertThat(sink.Collision.Items.Single()).IsEqual(new WallCollider(span.A, span.B, span.Thickness * 0.5f,
                span.BottomY, span.TopY, SurfaceTag.Wall) { Foot = FootMat.Concrete });
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ObliqueBentFootprintSplitsIntoTwoConvexWingsWithoutInventedVertices()
    {
        var footprint=new PolygonRings([new(0,0),new(40,0),new(48,30),new(32,34),new(25,14),new(0,14)]);
        var wings=InteriorWingDecomposition.Split(footprint,"fixture:oblique");
        var reversed=InteriorWingDecomposition.Split(new(footprint.Outer.Reverse().ToArray()),"fixture:oblique");
        AssertThat(wings.Count==2&&wings.All(w=>w.Outer.Count==4&&w.Outer.All(footprint.Outer.Contains))).IsTrue();
        AssertThat(wings.Zip(reversed).All(pair=>pair.First.Outer.SequenceEqual(pair.Second.Outer))).IsTrue();
        var footprintArea=Math.Abs(Campus.Gen.Geometry.PlanRings.SignedArea(footprint.Outer));
        AssertThat(Math.Abs(wings.Sum(w=>Math.Abs(Campus.Gen.Geometry.PlanRings.SignedArea(w.Outer)))-
            footprintArea)/footprintArea<TestTolerance.FloatRelative).IsTrue();
    }

    private static InteriorPlanRequest Request()
    {
        PolygonRings footprint = new(Rectangle());
        FacadeGrid facade = FacadeGridGenerator.Generate(footprint);
        int room = facade.Bays.Single(b => b.Edge == 0 && b.Bay == 2).Index;
        int service = facade.Bays.Single(b => b.Edge == 0 && b.Bay == 3).Index;
        EntranceDecl entry = new("hall", new(-2.5f, 0, 6.8f), new(3, 0, 6.8f), 1.8f, true)
        { Center = new(0, 0, 6.8f), YawDeg = 270 };
        return new(new("test:dormitory"), "gold-wing", 7, footprint,
            [new(1, 0, 3.6f, footprint.Outer), new(2, 3.6f, 3.6f, footprint.Outer)], [entry],
            [new(2, room, "dorm.four", true), new(2, service, "service.hot-water", true, ZoneKind.Service)], []);
    }

    private static Vector2[] Rectangle() => [new(0, 0), new(36, 0), new(36, 13.6f), new(0, 13.6f)];
    private static bool InAxisAlignedRing(IReadOnlyList<Vector2> ring, Vector2 p) =>
        p.X > ring.Min(v => v.X) + TestTolerance.Position && p.X < ring.Max(v => v.X) - TestTolerance.Position &&
        p.Y > ring.Min(v => v.Y) + TestTolerance.Position && p.Y < ring.Max(v => v.Y) - TestTolerance.Position;
    private static bool Covers(WallCollider wall, Vector3 p)
    {
        if (p.Y <= wall.Y0 || p.Y >= wall.Y1) return false;
        Vector2 d = wall.B - wall.A, delta = new Vector2(p.X, p.Z) - wall.A;
        double length = Math.Sqrt((double)d.X * d.X + (double)d.Y * d.Y);
        double along = ((double)delta.X * d.X + (double)delta.Y * d.Y) / length;
        double across = Math.Abs((double)d.X * delta.Y - (double)d.Y * delta.X) / length;
        return along >= 0 && along <= length && across <= wall.HalfT;
    }
    private static bool BlocksWalkingCylinder(WallCollider wall, Vector3 feet)
    {
        const float radius = 0.3f, height = 1.8f;
        if (wall.Y1 <= feet.Y || wall.Y0 >= feet.Y + height) return false;
        Vector2 p = new(feet.X, feet.Z), d = wall.B - wall.A;
        float t = Math.Clamp(Vector2.Dot(p - wall.A, d) / d.LengthSquared(), 0f, 1f);
        Vector2 nearest = wall.A + d * t;
        return Vector2.Distance(p, nearest) < radius + wall.HalfT;
    }
}
