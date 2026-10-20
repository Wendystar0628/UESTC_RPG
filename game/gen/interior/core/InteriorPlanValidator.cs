using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Interior;

/// <summary>Delivery-critical validation for the supported wing, independent of the engine or provider registry.</summary>
public static class InteriorPlanValidator
{
    public static void Validate(InteriorPlan plan, DormitoryPlanOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        options ??= new();
        var zones = plan.Zones.ToDictionary(z => z.Id);
        if (plan.Zones.Count == 0 || plan.Storeys.Count == 0) throw new ArgumentException("An interior wing cannot be empty.");
        foreach (ZoneVolume zone in plan.Zones)
        {
            if (!plan.Storeys.Any(s => s.Index == zone.Storey) || Math.Abs(PlanRings.SignedArea(zone.Ring)) < 0.5d ||
                zone.Ring.Any(p => !QuantizedPlan.Contains(plan.Facade.Footprint.Outer, p)))
                throw new ArgumentException("A room has no valid floor area inside its wing.");
        }
        foreach (PortalDecl portal in plan.Portals)
        {
            if (portal.ZoneA >= 0 && !zones.ContainsKey(portal.ZoneA) || portal.ZoneB >= 0 && !zones.ContainsKey(portal.ZoneB))
                throw new ArgumentException("A portal refers to a missing zone.");
            if (portal.Kind == PortalKind.Door && Distance(portal.A, portal.B) < options.DoorClearWidth - 0.003f)
                throw new ArgumentException("A walkable doorway is narrower than its required clearance.");
            if (!float.IsFinite(portal.Height) || portal.Height <= 0f || !VectorMath.Finite(portal.A) || !VectorMath.Finite(portal.B))
                throw new ArgumentException("A portal has invalid geometry.");
        }
        if (plan.Doors.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() != plan.Doors.Count)
            throw new ArgumentException("Moving door IDs must be unique.");
        foreach (DoorLeafDecl door in plan.Doors)
        {
            if (!plan.Portals.Any(p => p.Storey == door.Storey && p.ZoneA == door.ZoneA && p.ZoneB == door.ZoneB &&
                p.Kind == PortalKind.Door && Distance(p.A, door.Hinge) <= 0.003f))
                throw new ArgumentException("A moving door has no matching open baked doorway.");
            if (!zones[door.ZoneA].Open || !zones[door.ZoneB].Open || door.OpenDeg != 90f)
                throw new ArgumentException("Only open rooms and traffic cores may declare an automatic door.");
        }
        foreach (StoreyInfo storey in plan.Storeys)
            if (plan.Doors.Count(d => d.Storey == storey.Index) > 40)
                throw new ArgumentException("The moving-door budget is 40 per storey.");
        foreach (EntranceDecl entrance in plan.Entrances)
        {
            if (!VectorMath.Finite(entrance.Center) || !VectorMath.Finite(entrance.Inside) || !VectorMath.Finite(entrance.Outside) || !float.IsFinite(entrance.YawDeg))
                throw new ArgumentException("Entrance coordinates must be finite.");
            PortalDecl[] matching = plan.Portals.Where(p => p.ZoneA == -1 && p.Kind != PortalKind.Window &&
                Distance((p.A + p.B) * 0.5f, entrance.Center) <= 0.003f).ToArray();
            if (matching.Length != 1 || entrance.Open != (matching[0].Kind == PortalKind.Door))
                throw new ArgumentException("An authoritative entrance must match exactly one generated boundary opening.");
            if (entrance.Open && (!QuantizedPlan.Contains(plan.Facade.Footprint.Outer, new(entrance.Inside.X, entrance.Inside.Z)) ||
                QuantizedPlan.Contains(plan.Facade.Footprint.Outer, new(entrance.Outside.X, entrance.Outside.Z))))
                throw new ArgumentException("Entrance landing directions do not cross the wing boundary.");
        }
        foreach (InteriorStair stair in plan.Stairs)
        {
            StoreyInfo lower = plan.Storeys.Single(s => s.Index == stair.LowerStorey);
            StoreyInfo upper = plan.Storeys.Single(s => s.Index == stair.UpperStorey);
            if (MathF.Abs(stair.UpFlight.P2.Y - stair.MidLanding.TopY) > 0.001f ||
                MathF.Abs(stair.ReturnFlight.P2.Y - upper.FloorY) > 0.001f ||
                stair.UpFlight.P0.Y > lower.FloorY - 0.199f || stair.ReturnFlight.P0.Y > stair.MidLanding.TopY - 0.199f)
                throw new ArgumentException("A stair flight is not flush with its landing or buried into the lower floor.");
            foreach (RampCollider ramp in new[] { stair.UpFlight, stair.ReturnFlight })
            {
                float horizontal = VectorMath.Length(new(ramp.P3.X - ramp.P0.X, ramp.P3.Z - ramp.P0.Z));
                float slope = (ramp.P3.Y - ramp.P0.Y) / horizontal;
                if (slope < 0.487f || slope > 0.675f || ramp.Stair is not { Count: >= 3 and <= 18 })
                    throw new ArgumentException("A dormitory flight must stay within 26 to 34 degrees and 3 to 18 steps.");
            }
            Vector2 center = new(stair.FloorOpening.Average(p => p.X), stair.FloorOpening.Average(p => p.Y));
            if (plan.Slabs.Any(s => s.Storey == upper.Index && QuantizedPlan.Contains(s.Ring, center, false)))
                throw new ArgumentException("The upper floor incorrectly covers its stair opening.");
        }
        foreach (InteriorWallSpan wall in plan.Walls)
            if (wall.Thickness <= 0f || wall.TopY <= wall.BottomY || VectorMath.Length(wall.B - wall.A) <= 0.001f)
                throw new ArgumentException("A wall span is degenerate.");
        ValidateConnectivity(plan);
    }

    private static void ValidateConnectivity(InteriorPlan plan)
    {
        int[] open = plan.Zones.Where(z => z.Open).Select(z => z.Id).Order().ToArray();
        if (open.Length == 0) throw new ArgumentException("An open wing requires traffic zones.");
        var reached = new HashSet<int> { open[0] };
        var queue = new Queue<int>();
        queue.Enqueue(open[0]);
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (PortalDecl portal in plan.Portals)
            {
                if (portal.Kind is not (PortalKind.Door or PortalKind.StairOpening)) continue;
                int next = portal.ZoneA == current ? portal.ZoneB : portal.ZoneB == current ? portal.ZoneA : -1;
                if (next >= 0 && Array.BinarySearch(open, next) >= 0 && reached.Add(next)) queue.Enqueue(next);
            }
        }
        if (open.Any(id => !reached.Contains(id))) throw new ArgumentException("An open room or storey is disconnected from its wing corridor.");
    }

    private static float Distance(Vector3 a, Vector3 b)
    {
        Vector3 d = b - a;
        return (float)Math.Sqrt((double)d.X * d.X + (double)d.Y * d.Y + (double)d.Z * d.Z);
    }
}
