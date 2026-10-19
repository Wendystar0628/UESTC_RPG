using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Sites;
using F = Campus.Gen.Maps.Qingshuihe.North.NorthTune.Fence;
using G = Campus.Gen.Maps.Qingshuihe.North.NorthTune.Gate;
using L = Campus.Gen.Maps.Qingshuihe.North.NorthTune.Look;
using FacadeRules = Campus.Gen.Buildings.Rules.FacadeRules;

namespace Campus.Gen.Maps.Qingshuihe.North.Clusters;

/// <summary>
/// One dormitory cluster's enclosure (MAP-QSH-NORTH; MAP-QSH-RENOVATION N-22): the 2.2 m see-through steel fence
/// with the 1.2 m clipped hedge inside it, a gate at every road crossing — vehicle lane with the deep grey barrier arm down,
/// a face-scan turnstile bank beside it, the guard house at the main gate — and a closed steel side gate on footways.
/// Walk mode (SYS-WALK): every gate is closed (wings shut, red cross) except an open cluster's main gate, whose wings are
/// retracted and arrows green; its barrier arm stays down (WLD-INTERIORS). The buildings stay with WP-14.
/// </summary>
public sealed class ClusterSite(NorthTune.Cluster cluster) : ZoneSite
{
    private static readonly IronGatePart SideGate = new();

    public NorthTune.Cluster Cluster { get; } = cluster;

    public override string Id => "cluster." + Cluster.Key;

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        world.Features.TryGetValue(new(Cluster.Id), out var feature)
            ? [SiteRanges.Bounds(feature, F.MaxInset + F.HedgeOffset + F.HedgeWidth + G.HouseWidth +
                G.HouseGap + G.IslandWidth + G.Passages * G.OpenPassage + (G.Passages + 1) * G.CabinetWidth + G.HouseEave)] : [];

    private static Campus.Contracts.Maps.MapRect GateRange(GateSpot gate) => SiteRanges.Bounds(
        gate.Frame.Rect(MathF.Min(gate.SpanMinus, -gate.LaneHalf - G.PierSize), -G.HouseDepth - G.HouseEave,
            MathF.Max(gate.SpanPlus, gate.Main ? gate.HouseEnd : gate.BankEnd + G.PierSize), G.HouseOut + G.HouseEave), G.HouseEave);

    /// <summary>Q1 (walk, K1) outside the open main gate and Q6 (45° down, K2–K3) over its courtyard.</summary>
    public override IReadOnlyList<InspectionProbe> Probes => Cluster.Key == NorthTune.Probes.OpenCluster
        ?
        [
            Probe("Q1", Cluster.Id, NorthTune.Probes.GateEye, NorthTune.Probes.GateLook, 3f, 1f),
            Probe("Q6", Cluster.Id, NorthTune.Probes.CourtEye, NorthTune.Probes.CourtLook, -45f, 40f),
        ]
        : [];

    public override void Build(SiteBuilder b)
    {
        if (ClusterLayout.Of(b.Context.World, Cluster) is not { } layout) return;
        foreach (var run in layout.Runs) Fence(b, run);
        foreach (var gate in layout.Gates)
        {
            if (!b.Includes(GateRange(gate))) continue;
            switch (gate.Kind)
            {
                case GateKind.Vehicle: VehicleGate(b, gate); break;
                case GateKind.Footway: FootGate(b, gate); break;
                default: throw new InvalidOperationException($"Unknown gate kind {gate.Kind}.");
            }
        }
    }

    // ---- Fence and hedge ----

    private static void Fence(SiteBuilder b, FenceRun run)
    {
        var posts = run.Posts;
        if (b.Lod is LodLevel.L3 or LodLevel.L4)
        {
            // Far levels keep only the hedge line, in longer blocks; the steel fence is below a pixel there.
            for (var i = 0; i + 1 < posts.Count; i += 3) Hedge(b, run.Hedge[i], run.Hedge[Math.Min(i + 3, posts.Count - 1)]);
            return;
        }
        for (var i = 0; i + 1 < posts.Count; i++)
        {
            Vector2 p = posts[i], q = posts[i + 1];
            if (!b.Includes(SiteRanges.Bounds([p, q, run.Hedge[i], run.Hedge[i + 1]], F.HedgeWidth))) continue;
            var length = Vector2.Distance(p, q);
            if (length < 0.05f) continue;
            float y0 = b.Ground(p), y1 = b.Ground(q), y = MathF.Min(y0, y1);
            var u = (q - p) / length;
            // Rails run between post faces; bars stand on the bottom rail and rise just past the top rail.
            var a = p + u * (F.PostSize * 0.5f);
            var c = q - u * (F.PostSize * 0.5f);
            b.Run(L.Steel, a, c, F.RailSize, y + F.BottomRail, y + F.BottomRail + F.RailSize);
            b.Run(L.Steel, a, c, F.RailSize, y + F.TopRail, y + F.TopRail + F.RailSize);
            if (b.Detail != SiteDetail.Mass)
            {
                var pitch = b.Detail == SiteDetail.Full ? F.BarPitch : F.BarPitch * 4;
                var span = length - F.PostSize;
                var count = (int)MathF.Floor(span / pitch);
                var start = (span - (count - 1) * pitch) * 0.5f;
                for (var k = 0; k < count; k++)
                {
                    var at = a + u * (start + k * pitch);
                    b.Box(L.SteelBar, at, u, F.BarSize, F.BarSize, y + F.BottomRail + F.RailSize, y + F.Height - 0.03f, caps: false, noShadow: true);
                }
            }
            Hedge(b, run.Hedge[i], run.Hedge[i + 1]);
        }
        for (var j = 0; j < posts.Count; j++)
        {
            var p = posts[j];
            if (!b.Includes(SiteRanges.Point(p, F.PostSize + .04f))) continue;
            var y = b.Ground(p);
            // Square posts turn with the run: along the chord of their two neighbours.
            var u = VectorMath.Unit(posts[Math.Min(j + 1, posts.Count - 1)] - posts[Math.Max(j - 1, 0)], Vector2.Zero, 1e-9);
            if (u == Vector2.Zero) u = Vector2.UnitX;
            b.Box(L.Steel, p, u, F.PostSize, F.PostSize, y - 0.05f, y + F.Height);
            if (b.Detail == SiteDetail.Full) b.Box(L.Steel, p, u, F.PostSize + 0.04f, F.PostSize + 0.04f, y + F.Height, y + F.Height + 0.04f);
        }
    }

    private static void Hedge(SiteBuilder b, Vector2 p, Vector2 q)
    {
        if (!b.Includes(SiteRanges.Bounds([p, q], F.HedgeWidth))) return;
        var length = Vector2.Distance(p, q);
        if (length < 0.05f) return;
        var u = (q - p) / length;
        // Each block overlaps its neighbours a little so mitred corners close without a seam; a soft bevel rounds the clipped edges.
        var y = MathF.Min(b.Ground(p), b.Ground(q));
        if (b.Detail != SiteDetail.Full) { b.Box(L.Hedge, (p + q) * 0.5f, u, length + 0.3f, F.HedgeWidth, y - 0.05f, y + F.HedgeHeight); return; }
        b.Add(PartGeometryKernel.BeveledBox(length + 0.3f, F.HedgeHeight + 0.05f, F.HedgeWidth, F.HedgeBevel, L.Hedge), PlanFrame.Along((p + q) * 0.5f, u).At(0, y - 0.05f, 0));
    }

    // ---- Vehicle gate: barrier lane, island, turnstile bank, guard house at the main gate ----

    private static void VehicleGate(SiteBuilder b, GateSpot g)
    {
        var f = g.Frame;
        var y = b.Ground(f.Origin);
        var mass = b.Detail == SiteDetail.Mass;
        // Island between the lane and the bank, carrying the barrier cabinet.
        var islandX = g.LaneHalf + G.IslandWidth * 0.5f;
        b.Add(PartGeometryKernel.BeveledBox(G.IslandWidth, G.CurbRise + 0.1f, G.CabinetDepth + 0.8f, 0.02f, L.Granite), f.At(islandX, y - 0.1f, 0));
        b.Add(PartGeometryKernel.BeveledBox(G.BarrierCabinet, G.BarrierHeight, G.BarrierCabinet, 0.02f, L.Stainless), f.At(islandX, y + G.CurbRise, -0.25f));
        // Barrier arm down across the lane (closed even at the open cluster), its tip resting on a short fork post.
        var tip = -g.LaneHalf + 0.15f;
        var root = islandX - G.BarrierCabinet * 0.5f;
        b.Add(PartGeometryKernel.BeveledBox(root - tip, G.ArmHeight, G.ArmDepth, 0.012f, L.Steel), f.At((root + tip) * 0.5f, y + G.ArmY, -0.25f));
        b.Add(PartGeometryKernel.BeveledBox(0.08f, G.ArmY, 0.08f, 0.01f, L.Steel), f.At(tip - 0.1f, y, -0.25f));
        // Lane-side pier where the fence ends at −X.
        b.Add(PartGeometryKernel.BeveledBox(G.PierSize, F.Height + 0.1f, G.PierSize, 0.02f, L.Granite), f.At(-g.LaneHalf - G.PierSize * 0.5f, y - 0.05f, 0));
        if (!mass) b.Add(PartGeometryKernel.BeveledBox(G.PierSize + 0.06f, 0.08f, G.PierSize + 0.06f, 0.015f, L.Trim), f.At(-g.LaneHalf - G.PierSize * 0.5f, y + F.Height + 0.05f, 0));
        Turnstiles(b, g, y);
        if (g.Main) GuardHouse(b, g, y);
        else
        {
            var x = g.BankEnd + G.PierSize * 0.5f;
            b.Add(PartGeometryKernel.BeveledBox(G.PierSize, F.Height + 0.1f, G.PierSize, 0.02f, L.Granite), f.At(x, y - 0.05f, 0));
        }
    }

    /// <summary>
    /// Face-scan speed gates: stainless cabinets with a dark glass top, glass wings across each passage when closed; open
    /// wings fold into the cabinets and the end indicators show green arrows instead of red crosses (WLD-INTERIORS).
    /// </summary>
    private static void Turnstiles(SiteBuilder b, GateSpot g, float y)
    {
        var f = g.Frame;
        var mass = b.Detail == SiteDetail.Mass;
        float start = g.BankStart, end = g.BankEnd;
        b.Add(PartGeometryKernel.BeveledBox(end - start + 0.3f, 0.06f, G.CabinetDepth + 0.8f, 0.015f, L.Granite), f.At((start + end) * 0.5f, y - 0.03f, 0));
        var cabinets = g.Cabinets().ToArray();
        foreach (var x in cabinets)
        {
            b.Add(PartGeometryKernel.BeveledBox(G.CabinetWidth, G.CabinetHeight, G.CabinetDepth, 0.03f, L.Stainless), f.At(x, y, 0));
            if (mass) continue;
            b.Add(PartGeometryKernel.BeveledBox(G.CabinetWidth - 0.06f, 0.02f, G.CabinetDepth - 0.1f, 0, L.Glass), f.At(x, y + G.CabinetHeight, 0));
            if (b.Detail != SiteDetail.Full) continue;
            // End indicators on both faces: green arrow when the lane is open, red cross otherwise.
            var indicator = g.Open ? L.ArrowGreen : L.CrossRed;
            foreach (var side in new[] { -1f, 1f })
                b.Add(PartGeometryKernel.BeveledBox(G.Indicator, G.Indicator, 0.02f, 0, indicator),
                    f.At(x, y + G.CabinetHeight - G.Indicator - 0.08f, side * (G.CabinetDepth * 0.5f + 0.005f)));
        }
        if (mass) return;
        for (var k = 0; k < G.Passages; k++)
        {
            var x0 = cabinets[k] + G.CabinetWidth * 0.5f;
            var x1 = cabinets[k + 1] - G.CabinetWidth * 0.5f;
            if (!g.Open)
            {
                var wing = (x1 - x0 - 0.04f) * 0.5f;
                b.Add(PartGeometryKernel.BeveledBox(wing, G.WingHeight, 0.02f, 0, L.Glass), f.At(x0 + wing * 0.5f, y + G.WingBase, 0));
                b.Add(PartGeometryKernel.BeveledBox(wing, G.WingHeight, 0.02f, 0, L.Glass), f.At(x1 - wing * 0.5f, y + G.WingBase, 0));
            }
            if (b.Detail != SiteDetail.Full) continue;
            // Face scanner on a stalk at the outer (entry) end of the passage's right-hand cabinet.
            var sx = cabinets[k + 1];
            var sz = G.CabinetDepth * 0.5f - 0.18f;
            b.Add(PartGeometryKernel.BeveledBox(0.04f, 0.32f, 0.04f, 0, L.Stainless), f.At(sx, y + G.CabinetHeight, sz));
            b.Add(PartGeometryKernel.BeveledBox(0.17f, 0.24f, 0.03f, 0.005f, L.Stainless), f.At(sx, y + G.CabinetHeight + 0.3f, sz + 0.02f));
            b.Add(PartGeometryKernel.BeveledBox(0.13f, 0.18f, 0.01f, 0, L.Glass), f.At(sx, y + G.CabinetHeight + 0.33f, sz + 0.04f));
        }
    }

    /// <summary>Guard house (值班室): plinth, warm yellow walls, a window toward the bank and one outward, door inside.</summary>
    private static void GuardHouse(SiteBuilder b, GateSpot g, float y)
    {
        var f = g.Frame;
        var cx = (g.HouseStart + g.HouseEnd) * 0.5f;
        var cz = G.HouseOut - G.HouseDepth * 0.5f;
        float w = G.HouseWidth, d = G.HouseDepth, h = G.HouseHeight;
        b.Add(PartGeometryKernel.BeveledBox(w + 0.1f, G.HousePlinth + 0.1f, d + 0.1f, 0.02f, L.Plinth), f.At(cx, y - 0.1f, cz));
        b.Add(PartGeometryKernel.BeveledBox(w, h - G.HousePlinth, d, 0.03f, L.HouseWall), f.At(cx, y + G.HousePlinth, cz));
        var roof = w + 2 * G.HouseEave;
        b.Add(PartGeometryKernel.BeveledBox(roof, G.HouseRoof, d + 2 * G.HouseEave, 0.03f, L.Trim), f.At(cx, y + h, cz));
        if (b.Detail == SiteDetail.Mass) return;
        b.Add(PartGeometryKernel.BeveledBox(roof - 0.4f, 0.04f, d + 2 * G.HouseEave - 0.4f, 0.01f, L.RoofSlab), f.At(cx, y + h + G.HouseRoof, cz));
        // Watch window on the bank side (−X face) and the outward face (+Z), door on the cluster side (−Z).
        Window(b, f, g.HouseStart, cz, turned: true, outward: -1, 2.0f, y);
        Window(b, f, cx + 0.3f, G.HouseOut, turned: false, outward: 1, 1.8f, y);
        var doorZ = G.HouseOut - d;
        b.Add(PartGeometryKernel.BeveledBox(0.95f, 2.15f, 0.05f, 0, L.Wood), f.At(cx + 0.6f, y + G.HousePlinth, doorZ - 0.02f));
        b.Add(PartGeometryKernel.BeveledBox(1.15f, 0.08f, 0.1f, 0.01f, L.Trim), f.At(cx + 0.6f, y + G.HousePlinth + 2.15f, doorZ - 0.05f));
        b.Add(PartGeometryKernel.BeveledBox(1.4f, 0.12f, 0.9f, 0.015f, L.Trim), f.At(cx + 0.6f, y + G.HousePlinth + 2.45f, doorZ - 0.45f));
        b.Add(PartGeometryKernel.BeveledBox(1.2f, G.HousePlinth, 0.5f, 0.02f, L.Granite), f.At(cx + 0.6f, y - 0.05f, doorZ - 0.25f));
    }

    /// <summary>A window on a guard-house face: glass in an aluminium surround over a sill with a drip.</summary>
    private static void Window(SiteBuilder b, PlanFrame f, float x, float z, bool turned, int outward, float width, float y)
    {
        float y0 = y + FacadeRules.HutWindowSill, y1 = y + 2.4f, h = y1 - y0;
        Matrix4x4 At(float along, float yy, float off) => turned
            ? f.AtTurned(x + outward * off, yy, z + along)
            : f.At(x + along, yy, z + outward * off);
        b.Add(PartGeometryKernel.BeveledBox(width, h, 0.04f, 0, L.Glass), At(0, y0, 0.01f));
        b.Add(PartGeometryKernel.BeveledBox(width + 0.12f, 0.06f, 0.1f, 0.01f, L.Stainless), At(0, y1, 0.04f));
        b.Add(PartGeometryKernel.BeveledBox(width + 0.3f, 0.07f, 0.18f, 0.015f, L.Trim), At(0, y0 - 0.1f, 0.08f));
        foreach (var u in new[] { -0.5f, 0f, 0.5f })
            b.Add(PartGeometryKernel.BeveledBox(0.06f, h, 0.08f, 0.01f, L.Stainless), At(u * (width + 0.06f), y0, 0.04f));
    }

    // ---- Footway side gate: closed steel bar gate between two piers ----

    private static DoorParams SideGateParams(GateSpot g) =>
        new(g.RoadWidth + 0.3f, G.SideGateHeight, 0.08f, 0, 2) { Material = L.Steel, Foot = FootMat.Metal };

    private static void FootGate(SiteBuilder b, GateSpot g)
    {
        var f = g.Frame;
        var y = b.Ground(f.Origin);
        var p = SideGateParams(g);
        b.Add(SideGate.Lods[b.Lod](p), f.At(0, y, 0));
        foreach (var side in new[] { -1f, 1f })
        {
            var x = side * (p.Width * 0.5f + G.PierSize * 0.5f);
            b.Add(PartGeometryKernel.BeveledBox(G.PierSize, F.Height + 0.1f, G.PierSize, 0.02f, L.Granite), f.At(x, y - 0.05f, 0));
            if (b.Detail != SiteDetail.Mass)
                b.Add(PartGeometryKernel.BeveledBox(G.PierSize + 0.06f, 0.08f, G.PierSize + 0.06f, 0.015f, L.Trim), f.At(x, y + F.Height + 0.05f, 0));
        }
    }

    // ---- Collision (SYS-WALK): tops at max(visual, 1.5 m), gaps under 0.5 m ----

    public override void Collide(SiteColliders c)
    {
        if (ClusterLayout.Of(c.Context.World, Cluster) is not { } layout) return;
        foreach (var run in layout.Runs)
            for (var i = 0; i + 1 < run.Posts.Count; i++)
            {
                Vector2 p = run.Posts[i], q = run.Posts[i + 1];
                if (!c.Includes(SiteRanges.Bounds([p, q, run.Hedge[i], run.Hedge[i + 1]], F.HedgeWidth))) continue;
                if (Vector2.Distance(p, q) < 0.05f) continue;
                var lo = MathF.Min(c.Ground(p), c.Ground(q));
                var hi = MathF.Max(c.Ground(p), c.Ground(q));
                c.Wall(p, q, 0.06f, lo - 1f, hi + F.Height, SurfaceTag.Barrier, FootMat.Metal);
                Vector2 hp = run.Hedge[i], hq = run.Hedge[i + 1];
                if (Vector2.Distance(hp, hq) >= 0.05f)
                    c.Wall(hp, hq, F.HedgeWidth * 0.5f, lo - 0.5f, hi + MathF.Max(F.HedgeHeight, WalkRules.BarrierHeight), SurfaceTag.Barrier, FootMat.Hedge);
            }
        foreach (var g in layout.Gates)
        {
            if (!c.Includes(GateRange(g))) continue;
            var f = g.Frame;
            var y = c.Ground(f.Origin);
            var top = y + WalkRules.BarrierHeight;
            switch (g.Kind)
            {
                case GateKind.Footway:
                    c.Add(SideGate.Collision(SideGateParams(g)), f.At(0, y, 0));
                    break;
                case GateKind.Vehicle:
                    var laneTo = g.Open ? g.BankStart : g.BankEnd;
                    // Lane, island and (closed) the whole bank: one barrier line on the gate axis.
                    c.Wall(f.P(-g.LaneHalf - G.PierSize, 0), f.P(laneTo, 0), 0.15f, y - 0.5f, top, SurfaceTag.Barrier, FootMat.Metal);
                    if (g.Open)
                        foreach (var x in g.Cabinets())
                            c.Box(f.At(x, y + WalkRules.BarrierHeight * 0.5f, 0), new(G.CabinetWidth * 0.5f + 0.02f, WalkRules.BarrierHeight * 0.5f, G.CabinetDepth * 0.5f), SurfaceTag.Barrier, FootMat.Metal);
                    if (g.Main)
                    {
                        float x0 = g.HouseStart, x1 = g.HouseEnd, z1 = G.HouseOut, z0 = G.HouseOut - G.HouseDepth;
                        Vector2[] ring = [f.P(x0, z0), f.P(x1, z0), f.P(x1, z1), f.P(x0, z1)];
                        for (var i = 0; i < 4; i++)
                            c.Wall(ring[i], ring[(i + 1) % 4], 0.12f, y - 0.5f, y + WalkRules.BuildingCollisionHeight, SurfaceTag.Wall, FootMat.Concrete);
                        // Close the gap between the bank's last cabinet and the house.
                        c.Wall(f.P(g.BankEnd - 0.05f, 0), f.P(g.HouseStart + 0.05f, 0), 0.1f, y - 0.5f, top, SurfaceTag.Barrier, FootMat.Metal);
                    }
                    else c.Wall(f.P(g.BankEnd - 0.05f, 0), f.P(g.BankEnd + G.PierSize, 0), 0.15f, y - 0.5f, y + F.Height, SurfaceTag.Barrier, FootMat.StonePaving);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown gate kind {g.Kind}.");
            }
        }
    }
}
