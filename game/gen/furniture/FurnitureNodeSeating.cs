using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using S = Campus.Gen.Furniture.FurnitureStyle;
using static Campus.Gen.Furniture.FurniturePlacement;
using static Campus.Gen.Furniture.FurnitureParts;
using static Campus.Gen.Furniture.FurnitureSlots;

namespace Campus.Gen.Furniture;

/// <summary>Seats rest-node benches in plaza-first and walk-edge order.</summary>
internal static class FurnitureNodeSeating
{
    /// <summary>
    /// A road's rest stops at its nodes (Opus review 16:23): at each node, up to <see cref="FurnitureStyle.NodeBenches"/> benches on the
    /// pedestrian paving within <see cref="FurnitureStyle.NodeReach"/> of it (a plaza's edge, a walk's), nearest first, each standing
    /// just off the paving edge and facing the paving under the 13c1 rules (<see cref="PavingSlot"/>), <see cref="FurnitureStyle.NodeBenchGap"/>
    /// apart. A node that takes none says why, in the layout diagnostics, and the rules stay as they are.
    /// </summary>
    internal static void Plan(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics, string key, string road, IReadOnlyList<RestNode> nodes)
    {
        for (var n = 0; n < nodes.Count; n++)
        {
            var node = nodes[n];
            var placed = new List<Vector2>();
            var why = new SortedDictionary<string, int>(StringComparer.Ordinal);
            void Seat(WalkFit bench)
            {
                items.Add(Fitted($"node:{key}:{Num(n)}:{Num(placed.Count)}:bench", FurnitureKind.Bench, PartCatalog.Bench, S.Bench, bench, BenchDef.Collision(S.Bench)));
                site.Occupy(bench.Ends.A, bench.Ends.B, bench.Ends.R);
                placed.Add(bench.Centre);
            }
            // On the plaza first, its back to the plaza's outer edge, facing in (Opus review 17:28, 13c2b rework): the plaza's edges
            // within NodePlazaReach, nearest first.
            var plaza = site.Paving.PavingEdgesNear(node.At, S.NodePlazaReach, S.NodeStep).Where(e => e.Road is null).ToList();
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
            var candidates = site.Paving.PavingEdgesNear(node.At, S.NodeReach, S.NodeStep);
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
                diagnostics.Add(new(DiagnosticLevel.Info, "furniture.node.no-bench", $"road {road} node {node.Id.Value} has no bench: {plaza.Count} plaza edge spots within {Num(S.NodePlazaReach)} m and {candidates.Count} paving edge spots within {Num(S.NodeReach)} m, " +
                    (why.Count == 0 ? "none of them on pedestrian paving" : string.Join(", ", why.Select(w => $"{w.Value} {w.Key}")))));
        }
    }
}
