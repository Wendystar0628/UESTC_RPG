using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;
using Campus.Gen.Geometry;
using static Campus.Gen.Vegetation.Planting;
using static Campus.Gen.Vegetation.TreePlacement;

namespace Campus.Gen.Vegetation;

/// <summary>Plants road rows, avenue bands and existing tree points in their stable order.</summary>
internal static class RoadTreePlanting
{
    // ---------------------------------------------------------------------------------------------------------
    // Linear planting (§3): rows parallel to every road of a rule's name, chained end to end.
    // ---------------------------------------------------------------------------------------------------------

    internal static void PlantRoads(PlantingSite site, SpeciesTable species, PlantingRules rules, List<PlantedTree> trees)
    {
        for (var r = 0; r < rules.Roads.Count; r++)
        {
            var rule = rules.Roads[r];
            var chains = Chains(site.World.Network.Roads.Where(l => (l.Props?.OptionalText("name:zh") ?? l.Props?.OptionalText("name")) == rule.Name).ToList());
            for (var c = 0; c < chains.Count; c++)
            {
                var chain = chains[c];
                for (var side = -1; side <= 1; side += 2)
                {
                    for (var w = 0; w < rule.Rows.Count; w++)
                    {
                        var row = rule.Rows[w];
                        var start = rule.Spacing * (row.Stagger ? 1f : 0.5f);
                        var i = 0;
                        foreach (var (p, n) in Samples(chain, start, rule.Spacing))
                        {
                            var index = i++;
                            var key = Key("road", r, c, side, w, index);
                            var id = rule.Species[index % rule.Species.Count];
                            Try(site, species.Get(id), p + n * (side * row.Offset), key, rule.Wash, PlantSource.Road, trees, crownClear: false);
                        }
                    }
                    if (rule.Band is not { } band) continue;
                    var j = 0;
                    foreach (var (p, n) in Samples(chain, band.Spacing * 0.5f, band.Spacing))
                    {
                        var index = j++;
                        var key = Key("band", r, c, side, 0, index);
                        var id = band.Species[index % band.Species.Count];
                        Try(site, species.Get(id), p + n * (side * band.Offset), key, false, PlantSource.Band, trees, crownClear: false);
                    }
                }
            }
        }
    }

    /// <summary>Joins lines that share end points (within 1 m) into chains, in ID order, so a road split by a bridge stays one row.</summary>
    internal static List<List<Vector2>> Chains(List<NetworkLine> lines)
    {
        var left = lines.OrderBy(l => l.Id.Value, StringComparer.Ordinal).Select(l => l.Line.Points.ToList()).Where(p => p.Count >= 2).ToList();
        var chains = new List<List<Vector2>>();
        while (left.Count > 0)
        {
            var chain = left[0];
            left.RemoveAt(0);
            for (var grown = true; grown;)
            {
                grown = false;
                for (var i = 0; i < left.Count; i++)
                {
                    var l = left[i];
                    if (Vector2.Distance(chain[^1], l[0]) < 1f) chain.AddRange(l.Skip(1));
                    else if (Vector2.Distance(chain[^1], l[^1]) < 1f) chain.AddRange(Enumerable.Reverse(l).Skip(1));
                    else if (Vector2.Distance(chain[0], l[^1]) < 1f) chain.InsertRange(0, l.Take(l.Count - 1));
                    else if (Vector2.Distance(chain[0], l[0]) < 1f) chain.InsertRange(0, Enumerable.Reverse(l).Take(l.Count - 1));
                    else continue;
                    left.RemoveAt(i);
                    grown = true;
                    break;
                }
            }
            chains.Add(chain);
        }
        return chains;
    }

    /// <summary>Points every <paramref name="spacing"/> along a polyline from <paramref name="start"/>, with the unit left normal there.</summary>
    internal static IEnumerable<(Vector2 P, Vector2 N)> Samples(List<Vector2> line, float start, float spacing)
    {
        var total = PlanLines.Length(line);
        for (var s = start; s < total; s += spacing)
        {
            // The normal comes from a 4 m chord, so rows round the bends instead of kinking at the vertices.
            var a = PlanLines.Sample(line, MathF.Max(0f, s - 2f)).Point;
            var b = PlanLines.Sample(line, Math.Min(total, s + 2f)).Point;
            var t = b - a;
            if (t.LengthSquared() < 1e-6f) continue;
            t = Vector2.Normalize(t);
            yield return (PlanLines.Sample(line, s).Point, new Vector2(-t.Y, t.X));
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // OSM tree points (§3): the species of the nearest linear-planting road, else the default (camphor).
    // ---------------------------------------------------------------------------------------------------------

    internal static void PlantOsmTrees(PlantingSite site, SpeciesTable species, PlantingRules rules, List<PlantedTree> trees)
    {
        var named = rules.Roads.SelectMany(rule => site.World.Network.Roads.Where(l => (l.Props?.OptionalText("name:zh") ?? l.Props?.OptionalText("name")) == rule.Name)
            .Select(l => (Rule: rule, Line: l))).ToList();
        foreach (var point in site.World.Network.Trees.OrderBy(t => t.Id.Value, StringComparer.Ordinal))
        {
            var p = new Vector2(point.Position.X, point.Position.Z);
            if (trees.Any(t => Vector2.Distance(new(t.Position.X, t.Position.Z), p) < SameTree) ||
                site.Avoided.Any(a => Vector2.Distance(new(a.Tree.Position.X, a.Tree.Position.Z), p) < SameTree)) continue;
            var id = rules.OsmDefault;
            var wash = false;
            var best = rules.OsmNear;
            foreach (var (rule, line) in named)
            {
                var d = (float)PlanLines.Project(line.Line.Points, p).Distance;
                if (d < best) (best, id, wash) = (d, rule.Species[0], rule.Wash);
            }
            if (point.Species is { Length: > 0 } given && species.TryGet(given, out var known)) id = known!.Id;
            Try(site, species.Get(id), p, point.Id.Value, wash, PlantSource.OsmTree, trees, crownClear: false);
        }
    }
}
