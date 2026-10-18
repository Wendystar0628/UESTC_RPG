using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

/// <summary>The network lines of one tier and one name that get lamps and rest stops by one <see cref="TierRule"/>, with the feature of their own that <see cref="FurnitureStyle.DesignedRoads"/> gives them, if any. <paramref name="Key"/> names them in item keys.</summary>
internal sealed record RoadGroup(string Key, RoadTier Tier, string? Name, IReadOnlyList<NetworkLine> Lines, DesignedRoad? Designed);

public static partial class FurniturePlanner
{
    // ---------------------------------------------------------------------------------------------------------
    // Roads: lamps at the tier's spacing; rest stops midway between lamps.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The roads that get lamps, grouped: the network lines of one tier (<see cref="FurnitureStyle.TierOf"/>, by each line's own road
    /// class) and one name (the unnamed ones of a tier make one group), in tier then name order, so a group's key is stable for one map.
    /// A line under cover stays out. A road the planting rules give <see cref="FurnitureStyle.AvenueRows"/> offsets is tree-lined, the avenue.
    /// </summary>
    internal static IReadOnlyList<RoadGroup> RoadGroups(IWorldModel world, IReadOnlyList<RoadRows> rows, IReadOnlyList<DesignedRoad> designed)
    {
        var treeLined = rows.Where(r => r.Offsets.Count >= S.AvenueRows).Select(r => r.Name).ToHashSet(StringComparer.Ordinal);
        var graded = world.Network.Roads
            .Where(l => l.Line.Points.Count >= 2 && !StreetSite.Covered(l))
            .Select(l => (Line: l, Name: StreetSite.RoadName(l)))
            .Select(g => (g.Line, g.Name, Tier: S.TierOf(StreetSite.RoadClass(g.Line), g.Name is { } name && treeLined.Contains(name))))
            .Where(g => g.Tier is not null)
            .GroupBy(g => (Tier: g.Tier!.Value, Name: g.Name ?? ""))
            .OrderBy(g => g.Key.Tier).ThenBy(g => g.Key.Name, StringComparer.Ordinal);
        var groups = new List<RoadGroup>();
        var inTier = new Dictionary<RoadTier, int>();
        foreach (var g in graded)
        {
            var index = inTier.GetValueOrDefault(g.Key.Tier);
            inTier[g.Key.Tier] = index + 1;
            var name = g.Key.Name.Length > 0 ? g.Key.Name : null;
            groups.Add(new($"{g.Key.Tier}{Num(index)}", g.Key.Tier, name, [.. g.Select(t => t.Line).OrderBy(l => l.Id.Value, StringComparer.Ordinal)],
                name is null ? null : designed.FirstOrDefault(d => d.Name == name)));
        }
        return groups.AsReadOnly();
    }

    private static void Road(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics, RoadGroup group)
    {
        var rule = S.Rule(group.Tier);
        var half = group.Lines.Max(StreetSite.HalfWidth);
        var spacing = rule.LampSpacing;
        var lamp = rule.DiscLamp ? S.MainLamp : S.SecondaryLamp;
        var nodes = group.Designed?.Nodes;
        var chains = StreetSite.Chains(group.Lines);
        for (var c = 0; c < chains.Count; c++)
        {
            var run = new StreetSite.Run(chains[c]);
            var n = (int)MathF.Floor(run.Length / spacing);
            if (n < 1) continue;
            var s0 = (run.Length - (n - 1) * spacing) * 0.5f;
            // Both sides lit: opposite each other; else the side with more free slots.
            float[] lampSides = rule.BothSides ? [1f, -1f] : [BestSide(site, run, s0, spacing, n, half)];
            foreach (var side in lampSides)
                for (var k = 0; k < n; k++)
                    PlaceLamp(site, items, run, s0 + k * spacing, side, half, lamp, $"lamp:{group.Key}:{Num(c)}:{Side(side)}:{Num(k)}");
            // Rest stops: every RestEvery lamp gaps on a side; two lit sides alternate, a one-sided road's sit opposite its lamps.
            // A road with rest nodes (Opus review 16:23) has none along its line: its stops are at the nodes.
            float[] restSides = nodes is not null || rule.RestEvery == 0 ? [] : rule.BothSides ? [1f, -1f] : [-lampSides[0]];
            foreach (var side in restSides)
            {
                var phase = rule.BothSides && side < 0f ? rule.RestEvery / 2 : 0;
                int stops = 0, bins = 0;
                for (var k = 0; k + 1 < n; k++)
                {
                    if ((k + phase) % rule.RestEvery != rule.RestEvery / 2) continue;
                    var key = $"rest:{group.Key}:{Num(c)}:{Side(side)}:{Num(k)}";
                    // A bin at every RestBinEvery-th stop; one that found no room passes to the next stop.
                    var (bench, bin) = PlaceRest(site, items, run, s0 + (k + 0.5f) * spacing, side, half, withBin: bins * S.RestBinEvery <= stops, key);
                    if (bench) stops++;
                    if (bin) bins++;
                }
            }
        }
        if (nodes is not null) NodeStops(site, items, diagnostics, group.Key, group.Name!, nodes);
    }

    /// <summary>
    /// A road's rest stops at its nodes (Opus review 16:23): at each node, up to <see cref="FurnitureStyle.NodeBenches"/> benches on the
    /// pedestrian paving within <see cref="FurnitureStyle.NodeReach"/> of it (a plaza's edge, a walk's), nearest first, each standing
    /// just off the paving edge and facing the paving under the 13c1 rules (<see cref="PavingSlot"/>), <see cref="FurnitureStyle.NodeBenchGap"/>
    /// apart. A node that takes none says why, in the layout diagnostics, and the rules stay as they are.
    /// </summary>
    private static void NodeStops(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics, string key, string road, IReadOnlyList<RestNode> nodes)
    {
        for (var n = 0; n < nodes.Count; n++)
        {
            var node = nodes[n];
            var placed = new List<Vector2>();
            var why = new SortedDictionary<string, int>(StringComparer.Ordinal);
            void Seat(WalkFit bench)
            {
                items.Add(Placed($"node:{key}:{Num(n)}:{Num(placed.Count)}:bench", FurnitureKind.Bench, PartCatalog.Bench, S.Bench, bench, BenchDef.Collision(S.Bench)));
                site.Occupy(bench.Ends.A, bench.Ends.B, bench.Ends.R);
                placed.Add(bench.Centre);
            }
            // On the plaza first, its back to the plaza's outer edge, facing in (Opus review 17:28, 13c2b rework): the plaza's edges
            // within NodePlazaReach, nearest first.
            var plaza = site.PavingEdgesNear(node.At, S.NodePlazaReach, S.NodeStep).Where(e => e.Road is null).ToList();
            foreach (var tier in new[] { 0, 1, 2 })
            {
                foreach (var edge in plaza)
                {
                    if (placed.Count >= S.NodeBenches) break;
                    if (tier > 0 && placed.Count > 0) break;
                    var (fit, reason) = PlazaSlot(site, edge.Edge, edge.Inward, S.Bench, tier);
                    if (fit is not { } bench)
                    {
                        why["on the plaza: " + reason!] = why.GetValueOrDefault("on the plaza: " + reason!) + 1;
                        continue;
                    }
                    if (placed.Any(q => Vector2.Distance(q, bench.Centre) < S.NodeBenchGap)) continue;
                    Seat(bench);
                }
            }
            // Then at the edge of a walk 4 m or wider, its back to the edge (13c2b 21:05); else off the paving, facing it (13c1).
            var candidates = site.PavingEdgesNear(node.At, S.NodeReach, S.NodeStep);
            foreach (var edge in candidates)
            {
                if (placed.Count >= S.NodeBenches) break;
                var (onWalk, walkWhy) = WalkEdgeSlot(site, edge.Edge, edge.Inward, edge.Road, S.Bench);
                var (beside, besideWhy) = onWalk is null ? PavingSlot(site, edge.Edge, edge.Inward, edge.Road, S.Bench, facesWalk: true) : (null, null);
                if ((onWalk ?? beside) is not { } bench)
                {
                    why["on the walk: " + walkWhy!] = why.GetValueOrDefault("on the walk: " + walkWhy!) + 1;
                    why["beside it: " + besideWhy!] = why.GetValueOrDefault("beside it: " + besideWhy!) + 1;
                    continue;
                }
                if (placed.Any(q => Vector2.Distance(q, bench.Centre) < S.NodeBenchGap)) continue;
                Seat(bench);
            }
            if (placed.Count == 0)
                diagnostics.Add(new(DiagnosticLevel.Info, "wp13.node.no-bench", $"road {road} node {node.Label} has no bench: {plaza.Count} plaza edge spots within {Num(S.NodePlazaReach)} m and {candidates.Count} paving edge spots within {Num(S.NodeReach)} m, " +
                    (why.Count == 0 ? "none of them on pedestrian paving" : string.Join(", ", why.Select(w => $"{w.Value} {w.Key}")))));
        }
    }

    private static float BestSide(StreetSite site, StreetSite.Run run, float s0, float spacing, int n, float half)
    {
        int Count(float side)
        {
            var free = 0;
            for (var k = 0; k < n; k++)
                if (run.Normal(s0 + k * spacing) is { } normal && site.Free(run.At(s0 + k * spacing) + normal * (side * (half + S.LampKerbGap)), PoleRadius)) free++;
            return free;
        }
        return Count(-1f) > Count(1f) ? -1f : 1f;
    }

    private static void PlaceLamp(StreetSite site, List<FurnitureItem> items, StreetSite.Run run, float s, float side, float half, LampParams p, string key)
    {
        var terrain = site.World.Terrain;
        foreach (var shift in Shifts(S.LampMaxShift))
        {
            var at = s + shift;
            if (at < 0f || at > run.Length || run.Normal(at) is not { } normal) continue;
            var pos = run.At(at) + normal * (side * (half + S.LampKerbGap));
            if (!site.Free(pos, PoleRadius) || Land.Point(terrain, pos, PoleRadius) is not { } y) continue;
            // The front (−Z) faces the road, so a single-arm head reaches over the carriageway.
            var frame = Frame(pos, -normal * side, y);
            items.Add(Item(key, FurnitureKind.StreetLamp, PartCatalog.StreetLamp, p, frame, Lamp.Collision(p), [StreetLampPart.Emitter(p, frame)]));
            site.Occupy(pos, PoleRadius);
            return;
        }
    }

    /// <summary>
    /// The rest stop at arc length <paramref name="s"/> of the road, on <paramref name="side"/>: a bench lining the nearest walk on that
    /// side (within <see cref="FurnitureStyle.RestReach"/> of the centre line, sliding along it by up to <see cref="FurnitureStyle.RestSlide"/>),
    /// its front <see cref="FurnitureStyle.WalkGap"/> off the walk's edge and facing it, on a bay joined to the walk. The walk's road
    /// side is tried first (back to the trees and the road, looking away from it); the far side only takes a bench where no
    /// carriageway lies <see cref="FurnitureStyle.FaceClear"/> ahead. With <paramref name="withBin"/>, a bin on its own bay along the
    /// same edge, <see cref="FurnitureStyle.BinGaps"/> clear of the bench, on the side nearer another walk (a junction).
    /// </summary>
    private static (bool Bench, bool Bin) PlaceRest(StreetSite site, List<FurnitureItem> items, StreetSite.Run run, float s, float side, float half, bool withBin, string key)
    {
        if (run.Normal(s) is not { } normal) return (false, false);
        var road = run.At(s);
        var outward = normal * side;
        foreach (var walk in site.Walks(road, S.RestReach))
        {
            // The walk must lie on this side, beyond the carriageway, and stay wide enough to pass a sitter's feet.
            var whalf = StreetSite.HalfWidth(walk.Line);
            if (Vector2.Dot(walk.Point - road, outward) <= half || 2f * whalf - S.FeetReach < S.PassClear) continue;
            var path = new StreetSite.Run([.. walk.Line.Line.Points]);
            var ends = S.Bench.Width * 0.5f + S.ItemGap;
            foreach (var shift in Shifts(S.RestSlide))
            {
                var at = walk.Along + shift;
                if (at < ends || at > path.Length - ends || path.Normal(at) is not { } n) continue;
                var roadSide = Vector2.Dot(n, outward) < 0f ? 1f : -1f;
                foreach (var bside in new[] { roadSide, -roadSide })
                {
                    if (WalkSlot(site, path, at, bside, walk.Line, S.Bench, facesWalk: true) is not { } bench) continue;
                    items.Add(Placed(key + ":bench", FurnitureKind.Bench, PartCatalog.Bench, S.Bench, bench, BenchDef.Collision(S.Bench)));
                    site.Occupy(bench.Ends.A, bench.Ends.B, bench.Ends.R);
                    return (true, withBin && PlaceBin(site, items, path, at, bside, walk.Line, key + ":bin"));
                }
            }
        }
        return (false, false);
    }

    /// <summary>A bin along the same walk edge as the bench at <paramref name="benchAt"/>, 2–4 m clear of it, nearer side to a junction first.</summary>
    private static bool PlaceBin(StreetSite site, List<FurnitureItem> items, StreetSite.Run path, float benchAt, float bside, NetworkLine walk, string key)
    {
        var centre = S.Bench.Width * 0.5f + S.Bin.Width * 0.5f;
        float Junction(float sign) => site.OtherWalk(path.At(benchAt + sign * (centre + S.BinGaps[0])), walk);
        var first = Junction(-1f) < Junction(1f) ? -1f : 1f;
        foreach (var sign in new[] { first, -first })
            foreach (var gap in S.BinGaps)
            {
                var at = benchAt + sign * (centre + gap);
                if (at < S.Bin.Width * 0.5f || at > path.Length - S.Bin.Width * 0.5f) continue;
                if (WalkSlot(site, path, at, bside, walk, S.Bin, facesWalk: false) is not { } bin) continue;
                items.Add(Placed(key, FurnitureKind.Bin, PartCatalog.BinFour, S.Bin, bin, BinDef.Collision(S.Bin)));
                site.Occupy(bin.Ends.A, bin.Ends.B, bin.Ends.R);
                return true;
            }
        return false;
    }
}
