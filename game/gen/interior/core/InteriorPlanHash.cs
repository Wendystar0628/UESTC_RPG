using System.Globalization;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Interior;

internal static class InteriorPlanHash
{
    internal static uint Compute(uint worldSeed, InteriorPlan plan)
    {
        var h = new Fields();
        h.Add("interior.plan.v1"); h.Add(plan.Feature.Value); h.Add(plan.WingId); h.Add(plan.Facade.GridHash);
        h.Add(plan.Storeys.Count);
        foreach (StoreyInfo s in plan.Storeys) { h.Add(s.Index); h.Add(s.FloorY); h.Add(s.Height); h.Add(s.Footprint); }
        h.Add(plan.Zones.Count);
        foreach (ZoneVolume z in plan.Zones)
        {
            h.Add(z.Storey); h.Add(z.Id); h.Add((int)z.Kind); h.Add(z.Ring);
            h.Add(z.LightChannel.Value); h.Add(z.LightHash); h.Add(z.Open);
        }
        h.Add(plan.Portals.Count);
        foreach (PortalDecl p in plan.Portals)
        {
            h.Add(p.Storey); h.Add(p.ZoneA); h.Add(p.ZoneB); h.Add(p.A); h.Add(p.B); h.Add(p.Height); h.Add((int)p.Kind);
        }
        h.Add(plan.Doors.Count);
        foreach (DoorLeafDecl d in plan.Doors)
        {
            h.Add(d.Id); h.Add(d.Storey); h.Add(d.ZoneA); h.Add(d.ZoneB); h.Add(d.Hinge);
            h.Add(d.ClosedYawDeg); h.Add(d.OpenDeg); h.Add(d.Width); h.Add(d.Height); h.Add((int)d.Kind); h.Add(d.TriggerM);
        }
        h.Add(plan.Entrances.Count);
        foreach (EntranceDecl e in plan.Entrances)
        {
            h.Add(e.Id); h.Add(e.Center); h.Add(e.YawDeg); h.Add(e.Outside); h.Add(e.Inside); h.Add(e.ClearWidth); h.Add(e.Open);
        }
        h.Add(plan.Rooms.Count);
        foreach (InteriorRoom r in plan.Rooms)
        {
            h.Add(r.Storey); h.Add(r.ZoneId); h.Add(r.FacadeBayIndex); h.Add(r.Type); h.Add(r.ClearRing); h.Add(r.Open);
        }
        h.Add(plan.Windows.Count);
        foreach (InteriorWindow w in plan.Windows) { h.Add(w.Storey); h.Add(w.FacadeBayIndex); h.Add(w.ZoneId); h.Add((int)w.Kind); }
        h.Add(plan.Walls.Count);
        foreach (InteriorWallSpan w in plan.Walls)
        {
            h.Add(w.Storey); h.Add(w.A); h.Add(w.B); h.Add(w.Thickness); h.Add(w.BottomY); h.Add(w.TopY); h.Add((int)w.Role);
            h.Add(w.Closure is { } closure ? (int)closure : -1); h.Add(w.LeafKind is { } leafKind ? (int)leafKind : -1);
        }
        h.Add(plan.Slabs.Count);
        foreach (InteriorSlab slab in plan.Slabs) h.Add(slab);
        h.Add(plan.Stairs.Count);
        foreach (InteriorStair s in plan.Stairs)
        {
            h.Add(s.LowerStorey); h.Add(s.UpperStorey); h.Add(s.LowerZone); h.Add(s.UpperZone);
            h.Add(s.UpFlight); h.Add(s.ReturnFlight); h.Add(s.MidLanding); h.Add(s.FloorOpening);
        }
        return StableHash.Hash32(worldSeed, h.Values.ToArray());
    }

    private sealed class Fields
    {
        internal List<string> Values { get; } = [];
        internal void Add(string value) => Values.Add(value);
        internal void Add(int value) => Add(value.ToString(CultureInfo.InvariantCulture));
        internal void Add(uint value) => Add(value.ToString(CultureInfo.InvariantCulture));
        internal void Add(float value) => Add(value.ToString("R", CultureInfo.InvariantCulture));
        internal void Add(bool value) => Add(value ? "1" : "0");
        internal void Add(Vector2 value) { Add(value.X); Add(value.Y); }
        internal void Add(Vector3 value) { Add(value.X); Add(value.Y); Add(value.Z); }
        internal void Add(IReadOnlyList<Vector2> ring) { Add(ring.Count); foreach (Vector2 p in ring) Add(p); }
        internal void Add(InteriorSlab slab) { Add(slab.Storey); Add(slab.Ring); Add(slab.TopY); Add(slab.Thickness); }
        internal void Add(RampCollider ramp)
        {
            Add(ramp.P0); Add(ramp.P1); Add(ramp.P2); Add(ramp.P3); Add(ramp.Thick); Add((int)ramp.Tag); Add((int)ramp.Foot);
            Add(ramp.Stair.HasValue);
            if (ramp.Stair is { } stair) { Add(stair.RiserM); Add(stair.TreadM); Add(stair.Count); }
        }
    }
}
