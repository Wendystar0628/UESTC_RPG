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
/// toward the guard house, +Z out of the campus), the carriageway half width and the local x stations of every piece. Local x
/// is written as if the house stood on +X; <see cref="S"/> = −1 mirrors every station (the house on the street view's left).
/// The ring stays open from <see cref="OpeningStart"/> for <see cref="OpeningLength"/> metres (arc positions on the ring);
/// <see cref="Closures"/> are the gate's own fence pieces off the ring (the north closure leg, the 便门's short panel).
/// </summary>
public sealed record GateLayout(PlanFrame Frame, float S, float Carriage, BoundaryRing Ring, BoundaryTune.GateSpec Spec)
{
    public float OpeningStart { get; init; }
    public float OpeningLength { get; init; }
    /// <summary>What the ring run ending at the opening's start, and the one starting at its end, stand there.</summary>
    public RunEnd StartPost { get; init; }
    public RunEnd EndPost { get; init; }
    public IReadOnlyList<FenceRun> Closures { get; init; } = [];

    /// <summary>Left pier centre (street view: left of the carriageway) and the island centre carrying the right pier.</summary>
    public float LeftPierX => -Carriage - G.PierSize * 0.5f;
    public float IslandX => Carriage + G.IslandWidth * 0.5f;
    public static float BankWidth => G.Passages * G.Passage + (G.Passages + 1) * G.CabinetWidth;
    public float BankStart => Carriage + G.IslandWidth;
    public float BankEnd => BankStart + BankWidth;
    public float HouseStart => BankEnd + G.HouseGap;
    public float HouseEnd => HouseStart + G.HouseWidth;

    /// <summary>The 便门 bank beyond the left pier: its near (pier-side) and far edges, and the end pillar closing it.</summary>
    public bool HasWicket => Spec.Wicket is not null;
    public float WicketNear => (Spec.Wicket ?? 0f) + BankWidth * 0.5f;
    public float WicketFar => (Spec.Wicket ?? 0f) - BankWidth * 0.5f;
    public float WicketPierX => WicketFar - F.EndPillarSize * 0.5f;

    /// <summary>Where the ring fence takes over on the pier side: the pier's (or the 便门 pillar's) outer face.</summary>
    public float LeftEnd => HasWicket ? WicketFar - F.EndPillarSize : -Carriage - G.PierSize;

    public Vector2 P(float x, float z) => Frame.P(S * x, z);
    public Matrix4x4 At(float x, float y, float z) => Frame.At(S * x, y, z);

    public IEnumerable<float> Cabinets(float bankStart)
    {
        for (var k = 0; k <= G.Passages; k++) yield return bankStart + G.CabinetWidth * 0.5f + k * (G.Passage + G.CabinetWidth);
    }
}

/// <summary>
/// A campus gate (WLD-OUTSKIRTS §7.1; MAP-QSH-NORTH §8; MAP-QSH-WEST §12): a landmark kept sturdy and clean. Two stone piers
/// flank the carriageway — the left one, seen from the street, carries the name plaque — the barrier arm is down, the
/// turnstile bank beside the island is closed, and a flat-roofed grey stone guard house closes the line beyond it. A fence wing
/// runs off each side (the ring fence, Ring.BoundaryPlan). Walk mode: every gap is under 0.5 m and the closure is Boundary.
/// </summary>
public sealed class GateSite(BoundaryTune.GateSpec spec) : BoundarySite
{
    public BoundaryTune.GateSpec Spec { get; } = spec;

    public override string Id => "30." + Spec.Key + "-gate";

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
        var g = new GateLayout(frame, spec.Mirror ? -1f : 1f, carriage, ring, spec);
        var closures = new List<FenceRun>();
        var left = ring.Arc(g.P(g.LeftEnd, 0f));
        float right;
        if (spec.Closure)
        {
            // From the house's front corner straight out of the campus to the ring; the ring fence starts on a pillar there.
            var seed = g.P(g.HouseEnd + F.PillarSize * 0.5f, G.HouseFront);
            var hit = ring.Ray(seed, frame.W, 200f) ?? throw new InvalidOperationException($"{spec.Name}: the closure does not reach the ring.");
            right = ring.Arc(hit.Edge, hit.T);
            closures.Add(new FenceRun([seed, hit.Point + ring.Inward(hit.Edge) * F.Inset], RunEnd.Pillar, RunEnd.None, []));
        }
        else right = ring.Arc(g.P(g.HouseEnd, 0f));
        if (g.HasWicket)
            closures.Add(new FenceRun([g.P(-g.Carriage - G.PierSize, -F.Inset), g.P(g.WicketNear, -F.Inset)], RunEnd.None, RunEnd.None, []));
        var origin = ring.Arc(frame.Origin);
        var rightPost = spec.Closure ? RunEnd.Pillar : RunEnd.None;
        return ring.Forward(left, origin) <= ring.Forward(left, right)
            ? g with { OpeningStart = left, OpeningLength = ring.Forward(left, right), StartPost = RunEnd.None, EndPost = rightPost, Closures = closures }
            : g with { OpeningStart = right, OpeningLength = ring.Forward(right, left), StartPost = rightPost, EndPost = RunEnd.None, Closures = closures };
    }

    public override void Build(SiteBuilder b)
    {
        if (Of(b.Context.World, Spec) is not { } g) return;
        var y = b.Ground(g.Frame.Origin);
        Pier(b, g, g.LeftPierX, y, Spec.Plaque);
        Island(b, g, y);
        Barrier(b, g, y);
        Turnstiles(b, g, g.BankStart, y);
        GuardHouse(b, g, y);
        if (g.HasWicket)
        {
            Turnstiles(b, g, g.WicketFar, y);
            BoundaryFence.Pillar(b, g.P(g.WicketPierX, -F.Inset), g.Frame.U, end: true);
        }
        Bollards(b, g);
        if (Spec.Locker is { } locker && b.Detail != SiteDetail.Mass)
            b.Place(PartCatalog.DeliveryLocker, LockerSize, g.Frame.AtReversed(g.S * locker.X, b.Ground(g.P(locker.X, locker.Y)), locker.Y));
    }

    /// <summary>Delivery locker by the fence inside 西二门 (WLD-LIFE: 进门右侧的围墙边), WP-13's part; its door faces the campus.</summary>
    public static readonly EquipmentParams LockerSize = new(2.4f, 2f, 0.6f);

    // ---- Piers ----

    /// <summary>
    /// Gate pier: granite plinth, light grey stone shaft with two shallow joint bands, a beige cap with a drip and a top slab.
    /// The left pier's street face can carry the name plaque: a darker field in a beige frame, blank until the scene text atlas.
    /// </summary>
    private static void Pier(SiteBuilder b, GateLayout g, float x, float y, bool plaque)
    {
        float s = G.PierSize, h = G.PierHeight, z = G.PierZ;
        var mass = b.Detail == SiteDetail.Mass;
        b.Add(PartGeometryKernel.BeveledBox(s + 0.2f, G.PierPlinth + 0.1f, s + 0.2f, 0.03f, L.Granite), g.At(x, y - 0.1f, z));
        b.Add(PartGeometryKernel.BeveledBox(s, h - G.PierPlinth - G.PierCap, s, 0.03f, L.Stone), g.At(x, y + G.PierPlinth, z));
        var cap = s + 2 * G.PierCapOver;
        b.Add(PartGeometryKernel.BeveledBox(cap, G.PierCap, cap, 0.03f, L.StoneTrim), g.At(x, y + h - G.PierCap, z));
        if (mass) return;
        b.Add(PartGeometryKernel.BeveledBox(cap - 0.2f, 0.08f, cap - 0.2f, 0.015f, L.StoneTrim), g.At(x, y + h, z));
        foreach (var k in new[] { 0.42f, 0.78f })
            b.Add(PartGeometryKernel.BeveledBox(s + 0.04f, 0.05f, s + 0.04f, 0.01f, L.StoneTrim), g.At(x, y + G.PierPlinth + (h - G.PierPlinth - G.PierCap) * k, z));
        if (!plaque) return;
        var face = z + s * 0.5f;
        var y0 = y + G.PierPlinth + G.PlaqueBottom;
        float w = G.PlaqueWidth, ph = G.PlaqueHeight, fr = G.PlaqueFrame;
        b.Add(PartGeometryKernel.BeveledBox(w, ph, 0.04f, 0.005f, L.PlaqueField), g.At(x, y0, face + 0.02f));
        if (b.Detail != SiteDetail.Full) return;
        b.Add(PartGeometryKernel.BeveledBox(w + 2 * fr, fr, 0.07f, 0.01f, L.StoneTrim), g.At(x, y0 - fr, face + 0.035f));
        b.Add(PartGeometryKernel.BeveledBox(w + 2 * fr, fr, 0.07f, 0.01f, L.StoneTrim), g.At(x, y0 + ph, face + 0.035f));
        foreach (var side in new[] { -1f, 1f })
            b.Add(PartGeometryKernel.BeveledBox(fr, ph, 0.07f, 0.01f, L.StoneTrim), g.At(x + side * (w + fr) * 0.5f, y0, face + 0.035f));
    }

    // ---- Island, barrier arm ----

    private static void Island(SiteBuilder b, GateLayout g, float y)
    {
        b.Add(PartGeometryKernel.BeveledBox(G.IslandWidth, G.IslandRise + 0.1f, G.IslandLength, 0.03f, L.Granite), g.At(g.IslandX, y - 0.1f, -G.IslandLength * 0.5f));
        Pier(b, g, g.IslandX, y + G.IslandRise, plaque: false);
    }

    /// <summary>Barrier cabinet at the island's campus end, the arm down across the carriageway onto a drop foot.</summary>
    private static void Barrier(SiteBuilder b, GateLayout g, float y)
    {
        var top = y + G.IslandRise;
        b.Add(PartGeometryKernel.BeveledBox(G.BarrierCabinet, G.BarrierCabinetHeight, G.BarrierCabinet, 0.02f, L.Stainless), g.At(g.IslandX, top, G.BarrierZ));
        if (b.Detail == SiteDetail.Mass) return;
        var root = g.IslandX - G.BarrierCabinet * 0.5f;
        var tip = -g.Carriage + 0.1f;
        b.Add(PartGeometryKernel.BeveledBox(root - tip, G.ArmHeight, G.ArmDepth, 0.012f, L.LightSteel), g.At((root + tip) * 0.5f, y + G.ArmY, G.BarrierZ));
        b.Add(PartGeometryKernel.BeveledBox(0.07f, G.ArmY, 0.07f, 0.01f, L.Stainless), g.At(tip + 0.12f, y, G.BarrierZ));
        if (b.Detail == SiteDetail.Full)
            b.Add(PartGeometryKernel.BeveledBox(G.BarrierCabinet - 0.06f, 0.03f, G.BarrierCabinet - 0.06f, 0, L.Glass), g.At(g.IslandX, top + G.BarrierCabinetHeight, G.BarrierZ));
    }

    // ---- Turnstiles ----

    /// <summary>Flap turnstiles from bankStart: cabinets at both lane edges and between passages, glass flaps closed (gaps under 0.5 m).</summary>
    private static void Turnstiles(SiteBuilder b, GateLayout g, float bankStart, float y)
    {
        var cabinets = g.Cabinets(bankStart).ToArray();
        var bankEnd = bankStart + GateLayout.BankWidth;
        b.Add(PartGeometryKernel.BeveledBox(GateLayout.BankWidth, 0.06f, G.CabinetDepth + 0.6f, 0.015f, L.Granite), g.At((bankStart + bankEnd) * 0.5f, y - 0.03f, G.TurnstileZ));
        foreach (var x in cabinets)
        {
            b.Add(PartGeometryKernel.BeveledBox(G.CabinetWidth, G.CabinetHeight, G.CabinetDepth, 0.03f, L.Stainless), g.At(x, y, G.TurnstileZ));
            if (b.Detail == SiteDetail.Full)
                b.Add(PartGeometryKernel.BeveledBox(G.CabinetWidth - 0.04f, 0.02f, 0.5f, 0, L.Glass), g.At(x, y + G.CabinetHeight, G.TurnstileZ + 0.4f));
        }
        if (b.Detail == SiteDetail.Mass) return;
        for (var k = 0; k + 1 < cabinets.Length; k++)
        {
            var x0 = cabinets[k] + G.CabinetWidth * 0.5f;
            var x1 = cabinets[k + 1] - G.CabinetWidth * 0.5f;
            var flap = (x1 - x0 - 0.08f) * 0.5f;
            b.Add(PartGeometryKernel.BeveledBox(flap, G.FlapHeight, 0.02f, 0, L.Glass), g.At(x0 + flap * 0.5f, y + G.FlapBase, G.TurnstileZ));
            b.Add(PartGeometryKernel.BeveledBox(flap, G.FlapHeight, 0.02f, 0, L.Glass), g.At(x1 - flap * 0.5f, y + G.FlapBase, G.TurnstileZ));
        }
    }

    // ---- Guard house ----

    /// <summary>Guard house (值班室): granite plinth, grey stone walls, beige roof slab with an eave, watch windows, a door inside.</summary>
    private static void GuardHouse(SiteBuilder b, GateLayout g, float y)
    {
        var cx = (g.HouseStart + g.HouseEnd) * 0.5f;
        var cz = G.HouseFront - G.HouseDepth * 0.5f;
        float w = G.HouseWidth, d = G.HouseDepth, h = G.HouseHeight, s = g.S;
        b.Add(PartGeometryKernel.BeveledBox(w + 0.14f, G.HousePlinth + 0.1f, d + 0.14f, 0.02f, L.Granite), g.At(cx, y - 0.1f, cz));
        b.Add(PartGeometryKernel.BeveledBox(w, h - G.HousePlinth, d, 0.03f, L.Stone), g.At(cx, y + G.HousePlinth, cz));
        b.Add(PartGeometryKernel.BeveledBox(w + 2 * G.HouseEave, G.HouseRoof, d + 2 * G.HouseEave, 0.03f, L.StoneTrim), g.At(cx, y + h, cz));
        if (b.Detail == SiteDetail.Mass) return;
        b.Add(PartGeometryKernel.BeveledBox(w + 2 * G.HouseEave - 0.5f, 0.04f, d + 2 * G.HouseEave - 0.5f, 0.01f, L.RoofSlab), g.At(cx, y + h + G.HouseRoof, cz));
        // Watch windows: toward the turnstiles (the island-side face), toward the street (+Z face), toward the campus beside the door (-Z face).
        var house = g.Frame.Shift(s * cx, cz);
        Window(b, house, -s * w * 0.5f, 0, turned: true, outward: s < 0 ? 1 : -1, 2.2f, y);
        Window(b, house, s * 0.2f, d * 0.5f, turned: false, outward: 1, 2.6f, y);
        Window(b, house, -s * 0.8f, -d * 0.5f, turned: false, outward: -1, 1.6f, y);
        var doorX = s * 1.05f;
        var doorZ = -d * 0.5f;
        b.Add(PartGeometryKernel.BeveledBox(0.95f, 2.15f, 0.05f, 0, L.Door), house.At(doorX, y + G.HousePlinth, doorZ - 0.02f));
        b.Add(PartGeometryKernel.BeveledBox(1.15f, 0.08f, 0.1f, 0.01f, L.StoneTrim), house.At(doorX, y + G.HousePlinth + 2.15f, doorZ - 0.05f));
        b.Add(PartGeometryKernel.BeveledBox(1.3f, G.HousePlinth, 0.55f, 0.02f, L.Granite), house.At(doorX, y - 0.05f, doorZ - 0.27f));
    }

    /// <summary>A window on a house face: glass in an aluminium surround over a beige sill. Turned panes sit on +/-X faces.</summary>
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
            b.Place(PartCatalog.Bollard, bollard, g.At(p.X, b.Ground(g.P(p.X, p.Y)) + BoundaryTune.Plaza.PavingTop, p.Y));
    }

    // ---- Collision (SYS-WALK §7, WLD-OUTSKIRTS §7.1): Boundary, gaps under 0.5 m ----

    public override void Collide(SiteColliders c)
    {
        if (Of(c.Context.World, Spec) is not { } g) return;
        var y = c.Ground(g.Frame.Origin);
        var pierHalf = G.PierSize * 0.5f;
        // Left pier, and the L that closes the carriageway: the lane line behind the arm (the gate's invisible top-up to
        // 2.2 m) and a short return from the pier's back to that line.
        c.Box(g.At(g.LeftPierX, y + G.PierHeight * 0.5f, G.PierZ), new(pierHalf, G.PierHeight * 0.5f, pierHalf), SurfaceTag.Boundary, FootMat.StonePaving);
        c.Box(g.At((g.LeftPierX - pierHalf + g.Carriage) * 0.5f, y + G.LaneTop * 0.5f, G.BarrierZ), new((g.Carriage - g.LeftPierX + pierHalf) * 0.5f + 0.05f, G.LaneTop * 0.5f, 0.15f), SurfaceTag.Boundary, FootMat.Metal);
        c.Wall(g.P(g.LeftPierX, G.PierZ), g.P(g.LeftPierX, G.BarrierZ), 0.2f, y - 0.5f, y + G.LaneTop, SurfaceTag.Boundary, FootMat.StonePaving);
        // Island with its pier and barrier cabinet.
        c.Box(g.At(g.IslandX, y + G.LaneTop * 0.5f, -G.IslandLength * 0.5f), new(G.IslandWidth * 0.5f, G.LaneTop * 0.5f, G.IslandLength * 0.5f), SurfaceTag.Boundary, FootMat.StonePaving);
        // Turnstile banks, closed: one box across each bank reaching 1.5 m.
        Bank(c, g, g.BankStart, y);
        if (g.HasWicket)
        {
            Bank(c, g, g.WicketFar, y);
            var half = F.EndPillarSize * 0.5f;
            c.Box(g.At(g.WicketPierX, y + F.ColliderTop * 0.5f, -F.Inset), new(half, F.ColliderTop * 0.5f, half), SurfaceTag.Boundary, FootMat.StonePaving);
        }
        // Guard house: its four walls close the line.
        Vector2[] ring = [g.P(g.HouseStart, G.HouseFront - G.HouseDepth), g.P(g.HouseEnd, G.HouseFront - G.HouseDepth), g.P(g.HouseEnd, G.HouseFront), g.P(g.HouseStart, G.HouseFront)];
        for (var i = 0; i < 4; i++)
            c.Wall(ring[i], ring[(i + 1) % 4], 0.12f, y - 0.5f, y + G.HouseHeight + G.HouseRoof, SurfaceTag.Boundary, FootMat.StonePaving);
        c.Wall(g.P(g.BankEnd - 0.05f, G.TurnstileZ), g.P(g.HouseStart + 0.1f, G.TurnstileZ), 0.12f, y - 0.5f, y + G.LowTop, SurfaceTag.Boundary, FootMat.Metal);
        if (Spec.Locker is { } locker)
            c.Box(g.At(locker.X, c.Ground(g.P(locker.X, locker.Y)) + LockerSize.Height * 0.5f, locker.Y), new(LockerSize.Width * 0.5f, LockerSize.Height * 0.5f, LockerSize.Depth * 0.5f), SurfaceTag.Barrier, FootMat.Metal);
    }

    private static void Bank(SiteColliders c, GateLayout g, float bankStart, float y) =>
        c.Box(g.At(bankStart + GateLayout.BankWidth * 0.5f, y + G.LowTop * 0.5f, G.TurnstileZ), new(GateLayout.BankWidth * 0.5f + 0.05f, G.LowTop * 0.5f, G.CabinetDepth * 0.5f), SurfaceTag.Boundary, FootMat.Metal);
}
