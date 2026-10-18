using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using GdUnit4;

namespace Campus.Gen.Interior.Tests;

[TestSuite]
public sealed class InteriorPlanTests
{
    [TestCase]
    public void FacadeGridIgnoresRingStartWindingAndCollinearVertices()
    {
        var ring = Rectangle();
        FacadeGrid first = FacadeGridGenerator.Generate(new(ring));
        FacadeGrid equivalent = FacadeGridGenerator.Generate(new(new Vector2[]
            { ring[2], ring[1], new(18, 0), ring[0], ring[3], ring[2] }));
        Equal(first.GridHash, equivalent.GridHash);
        Equal(first.Bays.Count, equivalent.Bays.Count);
        for (int i = 0; i < first.Bays.Count; i++) Equal(first.Bays[i], equivalent.Bays[i]);
        foreach (FacadeBay bay in first.Bays)
        {
            Equal(bay.Index + first.Bays.Count, bay.WindowIndex(1, first.Bays.Count));
            True(bay.Width <= 4.201f, "A facade bay exceeds the configured maximum width.");
        }
    }

    [TestCase]
    public void FacadeGridKeepsCourtyardNormalsPointingIntoTheBuilding()
    {
        Vector2[] hole = [new(10, 4), new(20, 4), new(20, 9), new(10, 9)];
        FacadeGrid grid = FacadeGridGenerator.Generate(new(Rectangle(), [hole]));
        foreach (FacadeBay bay in grid.Bays.Where(b => b.Ring == 1))
        {
            Vector2 point = bay.Center + bay.Inward * 0.1f;
            True(!(point.X > 10 && point.X < 20 && point.Y > 4 && point.Y < 9), "A courtyard normal faces the void.");
        }
        Throws<ArgumentException>(() => FacadeGridGenerator.Generate(new(Rectangle(), [Rectangle()])));
        Throws<ArgumentException>(() => FacadeGridGenerator.Generate(new(new Vector2[]
            { new(0, 0), new(10, 10), new(0, 10), new(10, 0) })));
    }

    [TestCase]
    public void GoldWingHasExplicitOpenRoomsAndNoBakedMovingLeaves()
    {
        InteriorPlan plan = InteriorPlanGenerator.Generate(Request());
        Equal(2, plan.Storeys.Count);
        Equal(2, plan.Rooms.Count(r => r.Open));
        True(plan.Rooms.Count > 2, "The wing has no closed rooms besides the two selected.");
        True(plan.Doors.Count >= 2, "The selected rooms have no doors.");
        Equal(2, plan.Stairs.Count);
        True(plan.Rooms.Where(r => !r.Open).All(r => plan.Portals.Any(p => p.ZoneB == r.ZoneId && p.Kind == PortalKind.Sealed)),
            "A non-selected room was silently opened.");
        foreach (DoorLeafDecl door in plan.Doors)
        {
            PortalDecl portal = plan.Portals.Single(p => p.ZoneA == door.ZoneA && p.ZoneB == door.ZoneB && p.Kind == PortalKind.Door);
            Vector3 center = (portal.A + portal.B) * 0.5f + Vector3.UnitY;
            True(!InteriorPlanCollision.BuildStorey(plan, door.Storey).OfType<WallCollider>().Any(w => Covers(w, center)),
                "An automatic doorway is blocked by a baked wall or leaf.");
        }
        True(InteriorPlanCollision.BuildStorey(plan, 2).All(c => c.Foot != FootMat.Auto), "A collider lacks a foot material.");
    }

    [TestCase]
    public void StairFlightsMeetLandingsAndUpperFloorsHaveRealHoles()
    {
        InteriorPlan plan = InteriorPlanGenerator.Generate(Request());
        foreach (InteriorStair stair in plan.Stairs)
        {
            Equal(stair.MidLanding.TopY, stair.UpFlight.P2.Y);
            Equal(3.6f, stair.ReturnFlight.P2.Y);
            True(stair.UpFlight.P0.Y <= -0.199f, "Stair bottom is not buried.");
            True(stair.ReturnFlight.P0.Y <= stair.MidLanding.TopY - 0.199f, "Return flight is not buried into the mid-landing.");
            Equal(11, stair.UpFlight.Stair!.Value.Count);
            Vector2 a = stair.FloorOpening[0], c = stair.FloorOpening[2];
            foreach (float fraction in new[] { 0.1f, 0.5f, 0.9f })
            {
                Vector2 sample = a + (c - a) * fraction;
                True(!plan.Slabs.Where(s => s.Storey == 2).Any(s => InAxisAlignedRing(s.Ring, sample)),
                    "An upper floor slab covers the stair opening.");
            }
            True(plan.Walls.Any(w => w.Storey == 2 && w.Role == InteriorWallRole.StairBarrier && w.TopY >= 5.1f),
                "The upper stair opening has no fall barrier.");
        }
    }

    [TestCase]
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
        Equal(2, outgoing.Length);
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
                    True(!middleWalls.Any(w => BlocksWalkingCylinder(w, feet)),
                        "A wall blocks the 2F-to-3F first flight even though its portal graph is connected.");
                }
                Vector3 terminalFeet = Vector3.Lerp(low, high, floorT);
                terminalFeet.Y = 7.2f;
                True(terminalWalls.Any(w => BlocksWalkingCylinder(w, terminalFeet)),
                    "The final storey must still guard the first-flight opening where no upward flight follows.");
            }
        }
    }

    [TestCase]
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
        Equal(original.PlanHash, reordered.PlanHash);
        True(original.PlanHash != InteriorPlanGenerator.Generate(request with { WorldSeed = 99 }).PlanHash, "World seed is absent from the hash.");
        True(original.PlanHash != InteriorPlanGenerator.Generate(request with { Rooms = [] }).PlanHash, "Opening decisions are absent from the hash.");
        True(original.PlanHash != InteriorPlanGenerator.Generate(request with
        { Options = request.Options with { DoorHeight = 2.2f } }).PlanHash, "Door geometry is absent from the hash.");
    }

    [TestCase]
    public void RotatedWingAndReservedZoneRangeRemainUsable()
    {
        InteriorPlanRequest request = Request();
        Vector2 Transform(Vector2 p) => new(100f + p.X * 0.8f - p.Y * 0.6f, -200f + p.X * 0.6f + p.Y * 0.8f);
        PolygonRings rotated = new(request.Footprint.Outer.Select(Transform).ToArray());
        // No entry is supplied here: a composing provider connects this wing through its authored hall.
        InteriorPlan plan = InteriorPlanGenerator.Generate(request with { Footprint = rotated, Entrances = [], Rooms = [], ZoneIdBase = 1000 });
        Equal(1000, plan.Zones.Min(z => z.Id));
        True(plan.Zones.All(z => z.Ring.All(p => p.X > 90 && p.Y < -160)), "The rotated plan escaped its wing.");
        True(plan.Doors.All(d => d.ZoneA >= 1000 && d.ZoneB >= 1000), "Composed wing IDs were not offset consistently.");
        True(plan.Stairs.All(s => s.LowerZone >= 1000 && s.UpperZone >= 1000), "Stair links use local IDs.");
    }

    [TestCase]
    public void InvalidFootprintsFloorsAndUnmatchedSelectionsFailExplicitly()
    {
        InteriorPlanRequest request = Request();
        Throws<ArgumentException>(() => InteriorPlanGenerator.Generate(request with
        { Footprint = new(new Vector2[] { new(0, 0), new(36, 0), new(36, 13.6f), new(18, 13.6f), new(18, 8), new(0, 8) }) }));
        Throws<ArgumentException>(() => InteriorPlanGenerator.Generate(request with
        { Storeys = [new(1, 0, 3.6f, Rectangle()), new(3, 7.2f, 3.6f, Rectangle())] }));
        Throws<ArgumentException>(() => InteriorPlanGenerator.Generate(request with
        { Rooms = [new(2, int.MaxValue, "dorm.four", true)] }));
        EntranceDecl entry = request.Entrances[0] with { Center = new(10, 0, 6.8f) };
        Throws<ArgumentException>(() => InteriorPlanGenerator.Generate(request with { Entrances = [entry] }));
    }

    [TestCase]
    public void LocaleDoesNotChangeThePlanHashOrDoorKeys()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            InteriorPlan expected = InteriorPlanGenerator.Generate(Request());
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            InteriorPlan actual = InteriorPlanGenerator.Generate(Request());
            Equal(expected.PlanHash, actual.PlanHash);
            Equal(string.Join(';', expected.Doors.Select(d => d.Id)), string.Join(';', actual.Doors.Select(d => d.Id)));
        }
        finally { CultureInfo.CurrentCulture = previous; }
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
        p.X > ring.Min(v => v.X) + 0.001f && p.X < ring.Max(v => v.X) - 0.001f &&
        p.Y > ring.Min(v => v.Y) + 0.001f && p.Y < ring.Max(v => v.Y) - 0.001f;
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
    private static void True(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Equal<T>(T expected, T actual) where T : notnull =>
        True(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected {expected}; got {actual}.");
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
