using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.DesignBoundary.Fence;
using Campus.Gen.Zones.DesignBoundary.Framework;
using F = Campus.Gen.Zones.DesignBoundary.BoundaryTune.Fence;
using G = Campus.Gen.Zones.DesignBoundary.BoundaryTune.Gate;
using L = Campus.Gen.Zones.DesignBoundary.BoundaryTune.Look;

namespace Campus.Gen.Zones.DesignBoundary.Gates;

/// <summary>
/// The plan of one gate, resolved from the data: frame (origin on the ring where the gate road crosses it, X along the ring
/// toward the guard house, +Z out of the campus), the carriageway half width and the local x stations of every piece.
/// </summary>
public sealed record GateLayout(PlanFrame Frame, float Carriage, BoundaryRing Ring, IReadOnlyList<FenceRun> Wings)
{
    /// <summary>Left pier centre (street view: left of the carriageway) and the island centre carrying the right pier.</summary>
    public float LeftPierX => -Carriage - G.PierSize * 0.5f;
    public float IslandX => Carriage + G.IslandWidth * 0.5f;
    public float BankStart => Carriage + G.IslandWidth;
    public float BankEnd => BankStart + G.Passages * G.Passage + (G.Passages + 1) * G.CabinetWidth;
    public float HouseStart => BankEnd + G.HouseGap;
    public float HouseEnd => HouseStart + G.HouseWidth;

    public IEnumerable<float> Cabinets()
    {
        for (var k = 0; k <= G.Passages; k++) yield return BankStart + G.CabinetWidth * 0.5f + k * (G.Passage + G.CabinetWidth);
    }
}

/// <summary>
/// A campus gate (WLD-OUTSKIRTS §7.1; MAP-QSH-NORTH §8; MAP-QSH-WEST §12): a landmark kept sturdy and clean. Two stone piers
/// flank the carriageway — the left one, seen from the street, carries the name plaque — the barrier arm is down, the
/// turnstile bank beside the island is closed, and a flat-roofed grey stone guard house closes the line beyond it. A fence wing
/// runs off each side (<see cref="BoundaryFence"/>). Walk mode: every gap is under 0.5 m and the closure is Boundary.
/// </summary>
public sealed class GateSite(BoundaryTune.GateSpec spec) : BoundarySite
{
    public BoundaryTune.GateSpec Spec { get; } = spec;

    public override string Id => "30a." + Spec.Key + "-gate";

    /// <summary>The resolved plan; null when the map has no such road or it does not cross the boundary.</summary>
    public static GateLayout? Of(IWorldModel world, BoundaryTune.GateSpec spec)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(spec);
        var road = world.Network.Roads.FirstOrDefault(r => r.Id.Value == spec.Road);
        if (road is null || world.Boundary.Outer.Count < 3) return null;
        var ring = new BoundaryRing(world.Boundary.Outer);
        if (ring.Crossing(road.Line.Points) is not { } crossing) return null;
        var (a, b) = ring.Edge(crossing.Edge);
        // X runs along the ring with the campus on −Z, whatever the ring's winding: the guard house is on the right seen from the street.
        var frame = PlanFrame.Along(crossing.Point, b - a);
        if (ring.Contains(frame.P(0, 1f))) frame = PlanFrame.Along(crossing.Point, a - b);
        if (ring.Contains(frame.P(0, 1f)) || !ring.Contains(frame.P(0, -1f)))
            throw new InvalidOperationException($"{spec.Name}: the gate frame does not leave the campus on +Z (origin {frame.Origin}).");
        var carriage = (road.Width > 0 ? road.Width : 6f) * 0.5f + G.CarriageMargin;
        var layout = new GateLayout(frame, carriage, ring, []);
        return layout with { Wings = [.. spec.Wings.Select(w => Wing(layout, w))] };
    }

    private static FenceRun Wing(GateLayout g, BoundaryTune.Wing wing)
    {
        var f = g.Frame;
        var ring = g.Ring;
        var origin = f.Origin;
        IReadOnlyList<Vector2> line;
        if (!wing.Closure)
        {
            var seed = wing.Side < 0 ? f.P(-g.Carriage - G.PierSize, G.PierZ - G.PierSize * 0.5f) : f.P(g.HouseEnd, G.HouseFront - 0.05f);
            var (edge, t) = ring.Project(seed);
            line = ring.Walk(edge, t, origin, wing.Length, F.Inset);
        }
        else
        {
            var x = wing.Side < 0 ? -g.Carriage - G.PierSize - F.PillarSize * 0.5f : g.HouseEnd + F.PillarSize * 0.5f;
            var seed = f.P(x, G.HouseFront);
            var hit = ring.Ray(seed, f.W, 200f) ?? throw new InvalidOperationException("A closure wing does not reach the ring.");
            line = [seed, .. ring.Walk(hit.Edge, hit.T, origin, wing.Length, F.Inset)];
        }
        var hedge = wing.Hedge ? BoundaryFence.Offset(line, F.HedgeOffset, ring.Contains, 0.25f) : null;
        return new FenceRun(line, StartPillar: wing.Closure, EndPillar: true, hedge);
    }

    public override void Build(SiteBuilder b)
    {
        if (Of(b.Context.World, Spec) is not { } g) return;
        var y = b.Ground(g.Frame.Origin);
        Pier(b, g.Frame, g.LeftPierX, y, plaque: true);
        Island(b, g, y);
        Barrier(b, g, y);
        Turnstiles(b, g, y);
        GuardHouse(b, g, y);
        foreach (var wing in g.Wings) BoundaryFence.Build(b, wing);
        Bollards(b, g);
    }

    // ---- Piers ----

    /// <summary>
    /// Gate pier: granite plinth, light grey stone shaft with two shallow joint bands, a beige cap with a drip and a top slab.
    /// The left pier's street face carries the name plaque: a darker field in a beige frame, blank until the scene text atlas.
    /// </summary>
    private static void Pier(SiteBuilder b, PlanFrame f, float x, float y, bool plaque)
    {
        float s = G.PierSize, h = G.PierHeight, z = G.PierZ;
        var mass = b.Detail == SiteDetail.Mass;
        b.Add(PartGeometryKernel.BeveledBox(s + 0.2f, G.PierPlinth + 0.1f, s + 0.2f, 0.03f, L.Granite), f.At(x, y - 0.1f, z));
        b.Add(PartGeometryKernel.BeveledBox(s, h - G.PierPlinth - G.PierCap, s, 0.03f, L.Stone), f.At(x, y + G.PierPlinth, z));
        var cap = s + 2 * G.PierCapOver;
        b.Add(PartGeometryKernel.BeveledBox(cap, G.PierCap, cap, 0.03f, L.StoneTrim), f.At(x, y + h - G.PierCap, z));
        if (mass) return;
        b.Add(PartGeometryKernel.BeveledBox(cap - 0.2f, 0.08f, cap - 0.2f, 0.015f, L.StoneTrim), f.At(x, y + h, z));
        foreach (var k in new[] { 0.42f, 0.78f })
            b.Add(PartGeometryKernel.BeveledBox(s + 0.04f, 0.05f, s + 0.04f, 0.01f, L.StoneTrim), f.At(x, y + G.PierPlinth + (h - G.PierPlinth - G.PierCap) * k, z));
        if (!plaque) return;
        var face = z + s * 0.5f;
        var y0 = y + G.PierPlinth + G.PlaqueBottom;
        float w = G.PlaqueWidth, ph = G.PlaqueHeight, fr = G.PlaqueFrame;
        b.Add(PartGeometryKernel.BeveledBox(w, ph, 0.04f, 0.005f, L.PlaqueField), f.At(x, y0, face + 0.02f));
        if (b.Detail != SiteDetail.Full) return;
        b.Add(PartGeometryKernel.BeveledBox(w + 2 * fr, fr, 0.07f, 0.01f, L.StoneTrim), f.At(x, y0 - fr, face + 0.035f));
        b.Add(PartGeometryKernel.BeveledBox(w + 2 * fr, fr, 0.07f, 0.01f, L.StoneTrim), f.At(x, y0 + ph, face + 0.035f));
        foreach (var side in new[] { -1f, 1f })
            b.Add(PartGeometryKernel.BeveledBox(fr, ph, 0.07f, 0.01f, L.StoneTrim), f.At(x + side * (w + fr) * 0.5f, y0, face + 0.035f));
    }

    // ---- Island, barrier arm ----

    private static void Island(SiteBuilder b, GateLayout g, float y)
    {
        var f = g.Frame;
        b.Add(PartGeometryKernel.BeveledBox(G.IslandWidth, G.IslandRise + 0.1f, G.IslandLength, 0.03f, L.Granite), f.At(g.IslandX, y - 0.1f, -G.IslandLength * 0.5f));
        Pier(b, f, g.IslandX, y + G.IslandRise, plaque: false);
    }

    /// <summary>Barrier cabinet at the island's campus end, the arm down across the carriageway onto a drop foot.</summary>
    private static void Barrier(SiteBuilder b, GateLayout g, float y)
    {
        var f = g.Frame;
        var top = y + G.IslandRise;
        b.Add(PartGeometryKernel.BeveledBox(G.BarrierCabinet, G.BarrierCabinetHeight, G.BarrierCabinet, 0.02f, L.Stainless), f.At(g.IslandX, top, G.BarrierZ));
        if (b.Detail == SiteDetail.Mass) return;
        var root = g.IslandX - G.BarrierCabinet * 0.5f;
        var tip = -g.Carriage + 0.1f;
        b.Add(PartGeometryKernel.BeveledBox(root - tip, G.ArmHeight, G.ArmDepth, 0.012f, L.LightSteel), f.At((root + tip) * 0.5f, y + G.ArmY, G.BarrierZ));
        b.Add(PartGeometryKernel.BeveledBox(0.07f, G.ArmY, 0.07f, 0.01f, L.Stainless), f.At(tip + 0.12f, y, G.BarrierZ));
        if (b.Detail == SiteDetail.Full)
            b.Add(PartGeometryKernel.BeveledBox(G.BarrierCabinet - 0.06f, 0.03f, G.BarrierCabinet - 0.06f, 0, L.Glass), f.At(g.IslandX, top + G.BarrierCabinetHeight, G.BarrierZ));
    }

    // ---- Turnstiles ----

    /// <summary>Flap turnstiles: cabinets at both lane edges and between passages, glass flaps closed (gaps under 0.5 m).</summary>
    private static void Turnstiles(SiteBuilder b, GateLayout g, float y)
    {
        var f = g.Frame;
        var cabinets = g.Cabinets().ToArray();
        b.Add(PartGeometryKernel.BeveledBox(g.BankEnd - g.BankStart, 0.06f, G.CabinetDepth + 0.6f, 0.015f, L.Granite), f.At((g.BankStart + g.BankEnd) * 0.5f, y - 0.03f, G.TurnstileZ));
        foreach (var x in cabinets)
        {
            b.Add(PartGeometryKernel.BeveledBox(G.CabinetWidth, G.CabinetHeight, G.CabinetDepth, 0.03f, L.Stainless), f.At(x, y, G.TurnstileZ));
            if (b.Detail == SiteDetail.Full)
                b.Add(PartGeometryKernel.BeveledBox(G.CabinetWidth - 0.04f, 0.02f, 0.5f, 0, L.Glass), f.At(x, y + G.CabinetHeight, G.TurnstileZ + 0.4f));
        }
        if (b.Detail == SiteDetail.Mass) return;
        for (var k = 0; k + 1 < cabinets.Length; k++)
        {
            var x0 = cabinets[k] + G.CabinetWidth * 0.5f;
            var x1 = cabinets[k + 1] - G.CabinetWidth * 0.5f;
            var flap = (x1 - x0 - 0.08f) * 0.5f;
            b.Add(PartGeometryKernel.BeveledBox(flap, G.FlapHeight, 0.02f, 0, L.Glass), f.At(x0 + flap * 0.5f, y + G.FlapBase, G.TurnstileZ));
            b.Add(PartGeometryKernel.BeveledBox(flap, G.FlapHeight, 0.02f, 0, L.Glass), f.At(x1 - flap * 0.5f, y + G.FlapBase, G.TurnstileZ));
        }
    }

    // ---- Guard house ----

    /// <summary>Guard house (值班室): granite plinth, grey stone walls, beige roof slab with an eave, watch windows, a door inside.</summary>
    private static void GuardHouse(SiteBuilder b, GateLayout g, float y)
    {
        var f = g.Frame;
        var cx = (g.HouseStart + g.HouseEnd) * 0.5f;
        var cz = G.HouseFront - G.HouseDepth * 0.5f;
        float w = G.HouseWidth, d = G.HouseDepth, h = G.HouseHeight;
        b.Add(PartGeometryKernel.BeveledBox(w + 0.14f, G.HousePlinth + 0.1f, d + 0.14f, 0.02f, L.Granite), f.At(cx, y - 0.1f, cz));
        b.Add(PartGeometryKernel.BeveledBox(w, h - G.HousePlinth, d, 0.03f, L.Stone), f.At(cx, y + G.HousePlinth, cz));
        b.Add(PartGeometryKernel.BeveledBox(w + 2 * G.HouseEave, G.HouseRoof, d + 2 * G.HouseEave, 0.03f, L.StoneTrim), f.At(cx, y + h, cz));
        if (b.Detail == SiteDetail.Mass) return;
        b.Add(PartGeometryKernel.BeveledBox(w + 2 * G.HouseEave - 0.5f, 0.04f, d + 2 * G.HouseEave - 0.5f, 0.01f, L.RoofSlab), f.At(cx, y + h + G.HouseRoof, cz));
        // Watch windows: toward the turnstiles (−X face), toward the street (+Z face), toward the campus beside the door (−Z face).
        var house = f.Shift(cx, cz);
        Window(b, house, -w * 0.5f, 0, turned: true, outward: -1, 2.2f, y);
        Window(b, house, 0.2f, d * 0.5f, turned: false, outward: 1, 2.6f, y);
        Window(b, house, -0.8f, -d * 0.5f, turned: false, outward: -1, 1.6f, y);
        var doorX = 1.05f;
        var doorZ = -d * 0.5f;
        b.Add(PartGeometryKernel.BeveledBox(0.95f, 2.15f, 0.05f, 0, L.Door), house.At(doorX, y + G.HousePlinth, doorZ - 0.02f));
        b.Add(PartGeometryKernel.BeveledBox(1.15f, 0.08f, 0.1f, 0.01f, L.StoneTrim), house.At(doorX, y + G.HousePlinth + 2.15f, doorZ - 0.05f));
        b.Add(PartGeometryKernel.BeveledBox(1.3f, G.HousePlinth, 0.55f, 0.02f, L.Granite), house.At(doorX, y - 0.05f, doorZ - 0.27f));
    }

    /// <summary>A window on a house face: glass in an aluminium surround over a beige sill. Turned panes sit on ±X faces.</summary>
    private static void Window(SiteBuilder b, PlanFrame f, float x, float z, bool turned, int outward, float width, float y)
    {
        float y0 = y + G.WindowSill, y1 = y + G.WindowHead, h = y1 - y0;
        Matrix4x4 At(float along, float yy, float off) => turned
            ? f.AtTurned(x + outward * off, yy, z + along)
            : f.At(x + along, yy, z + outward * off);
        b.Add(PartGeometryKernel.BeveledBox(width, h, 0.04f, 0, L.Glass), At(0, y0, 0.01f));
        b.Add(PartGeometryKernel.BeveledBox(width + 0.12f, 0.06f, 0.1f, 0.01f, L.Stainless), At(0, y1, 0.04f));
        b.Add(PartGeometryKernel.BeveledBox(width + 0.12f, 0.06f, 0.1f, 0.01f, L.Stainless), At(0, y0 - 0.06f, 0.04f));
        b.Add(PartGeometryKernel.BeveledBox(width + 0.3f, 0.07f, 0.18f, 0.015f, L.StoneTrim), At(0, y0 - 0.13f, 0.08f));
        foreach (var u in new[] { -0.5f, 0f, 0.5f })
            b.Add(PartGeometryKernel.BeveledBox(0.06f, h, 0.08f, 0.01f, L.Stainless), At(u * (width + 0.06f), y0, 0.04f));
    }

    /// <summary>Bollards along the forecourt's road edge (WP-13's part), standing on WP-15's paving.</summary>
    private void Bollards(SiteBuilder b, GateLayout g)
    {
        if (b.Detail == SiteDetail.Mass || Spec.Bollards.Length == 0) return;
        var bollard = new ColumnParams(0.18f, 0.8f, 0.18f);
        foreach (var p in Spec.Bollards)
        {
            var at = g.Frame.P(p.X, p.Y);
            b.Place(PartCatalog.Bollard, bollard, g.Frame.Shift(p.X, p.Y).At(0, b.Ground(at) + BoundaryTune.Plaza.PavingTop, 0));
        }
    }

    // ---- Collision (SYS-WALK §7, WLD-OUTSKIRTS §7.1): Boundary, gaps under 0.5 m ----

    public override void Collide(SiteColliders c)
    {
        if (Of(c.Context.World, Spec) is not { } g) return;
        var f = g.Frame;
        var y = c.Ground(f.Origin);
        var pierHalf = G.PierSize * 0.5f;
        // Left pier, and the L that closes the carriageway: the lane line behind the arm (the gate's invisible top-up to
        // 2.2 m) and a short return from the pier's back to that line.
        c.Box(f.At(g.LeftPierX, y + G.PierHeight * 0.5f, G.PierZ), new(pierHalf, G.PierHeight * 0.5f, pierHalf), SurfaceTag.Boundary, FootMat.StonePaving);
        c.Box(f.At((g.LeftPierX - pierHalf + g.Carriage) * 0.5f, y + G.LaneTop * 0.5f, G.BarrierZ), new((g.Carriage - g.LeftPierX + pierHalf) * 0.5f + 0.05f, G.LaneTop * 0.5f, 0.15f), SurfaceTag.Boundary, FootMat.Metal);
        c.Wall(f.P(g.LeftPierX, G.PierZ), f.P(g.LeftPierX, G.BarrierZ), 0.2f, y - 0.5f, y + G.LaneTop, SurfaceTag.Boundary, FootMat.StonePaving);
        // Island with its pier and barrier cabinet.
        c.Box(f.At(g.IslandX, y + G.LaneTop * 0.5f, -G.IslandLength * 0.5f), new(G.IslandWidth * 0.5f, G.LaneTop * 0.5f, G.IslandLength * 0.5f), SurfaceTag.Boundary, FootMat.StonePaving);
        // Turnstile bank, closed: one box across the bank reaching 1.5 m.
        c.Box(f.At((g.BankStart + g.BankEnd) * 0.5f, y + G.LowTop * 0.5f, G.TurnstileZ), new((g.BankEnd - g.BankStart) * 0.5f + 0.05f, G.LowTop * 0.5f, G.CabinetDepth * 0.5f), SurfaceTag.Boundary, FootMat.Metal);
        // Guard house: its four walls close the line.
        var ring = f.Rect(g.HouseStart, G.HouseFront - G.HouseDepth, g.HouseEnd, G.HouseFront);
        for (var i = 0; i < 4; i++)
            c.Wall(ring[i], ring[(i + 1) % 4], 0.12f, y - 0.5f, y + G.HouseHeight + G.HouseRoof, SurfaceTag.Boundary, FootMat.StonePaving);
        c.Wall(f.P(g.BankEnd - 0.05f, G.TurnstileZ), f.P(g.HouseStart + 0.1f, G.TurnstileZ), 0.12f, y - 0.5f, y + G.LowTop, SurfaceTag.Boundary, FootMat.Metal);
        foreach (var wing in g.Wings) BoundaryFence.Collide(c, wing);
    }
}
