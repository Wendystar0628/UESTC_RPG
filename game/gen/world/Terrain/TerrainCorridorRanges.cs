using Campus.Contracts;
using Campus.Contracts.Maps;
using static Campus.Gen.World.TerrainCorridorCompiler;

namespace Campus.Gen.World;

internal static class TerrainCorridorRanges
{
    internal sealed record CorridorPlan(TerrainRoad Road, TerrainCorridorOp[] Operations,
        IReadOnlyList<(double From, double To)> Exclusions)
    {
        public bool HasGaps
        {
            get
            {
                if (Operations[0].Anchors[0].S > 0 || Operations[^1].Anchors[^1].S < Road.Path.Length) return true;
                for (var i = 1; i < Operations.Length; i++)
                    if (Operations[i - 1].Anchors[^1].S < Operations[i].Anchors[0].S) return true;
                return false;
            }
        }

        public TerrainCorridorOp? At(double station)
        {
            foreach (var op in Operations)
                if (station >= op.Anchors[0].S && station <= op.Anchors[^1].S) return op;
            return null;
        }

        public (TerrainDifferential Cross, TerrainDifferential Shoulder) Parameters(TerrainPathHit hit, double defaultShoulder)
        {
            var current = At(hit.Station);
            if (current is not null) return (TerrainDifferential.Constant(current.CrossFall), TerrainDifferential.Constant(current.Shoulder));
            TerrainCorridorOp? left = null; TerrainCorridorOp? right = null;
            foreach (var op in Operations)
            {
                if (op.Anchors[^1].S < hit.Station) left = op;
                else if (op.Anchors[0].S > hit.Station) { right = op; break; }
            }
            var from = left?.Anchors[^1].S ?? 0; var to = right?.Anchors[0].S ?? Road.Path.Length;
            var t = (hit.Station - from) / (to - from);
            return (Blend(left?.CrossFall ?? 0.015, right?.CrossFall ?? 0.015),
                Blend(left?.Shoulder ?? defaultShoulder, right?.Shoulder ?? defaultShoulder));

            TerrainDifferential Blend(double a, double c)
            {
                var slope = (c - a) / (to - from);
                return new(a + (c - a) * t, slope * hit.StationDx, slope * hit.StationDz);
            }
        }
    }

    internal static CorridorPlan[] CorridorPlans(TerrainBuildState b, IEnumerable<TerrainCorridorOp> operations)
    {
        var resolved = operations.Select(op => (Operation: op, Road: b.References.Road(op.Road, b.Files[op.Id], op.Id))).ToArray();
        var canonical = b.References.AutomaticRoads().ToDictionary(road => road.Source);
        var plans = new List<CorridorPlan>();
        foreach (var group in resolved.GroupBy(entry => entry.Road.Source).OrderBy(group => group.Key.Value, StringComparer.Ordinal))
        {
            var road = canonical.TryGetValue(group.Key, out var source) ? source : group.First().Road;
            var normalized = group.Select(entry =>
            {
                var op = entry.Operation; var file = b.Files[op.Id]; var length = road.Path.Length;
                if (op.Range is null && (op.Anchors[0].S != 0 || Math.Abs(op.Anchors[^1].S - length) > 0.01))
                    throw new WorldDataException(file, op.Id, "A full-source corridor must cover the actual source centreline endpoints.");
                var end = TerrainRoadProfile.ResolveStation(op.Anchors[^1].S, op.Anchors[^1].S, length, file, op.Id);
                if (op.Anchors[^2].S >= end)
                    throw new WorldDataException(file, op.Id, "Corridor interval has no length on the actual source road.");
                var anchors = op.Anchors.Select((anchor, index) => index == op.Anchors.Count - 1 ? anchor with { S = end } : anchor).ToArray();
                foreach (var anchor in anchors) TerrainRoadProfile.ResolveStation(anchor.S, end, length, file, op.Id);
                return op with
                {
                    Anchors = anchors,
                    Range = op.Range is null ? null : new(anchors[0].S, end),
                    Steps = op.Steps.Select(step => step with
                    {
                        S0 = TerrainRoadProfile.ResolveStation(step.S0, op.Anchors[^1].S, length, file, op.Id),
                        S1 = TerrainRoadProfile.ResolveStation(step.S1, op.Anchors[^1].S, length, file, op.Id)
                    }).ToArray()
                };
            }).OrderBy(op => op.Anchors[0].S).ThenBy(op => op.Id, StringComparer.Ordinal).ToArray();
            for (var i = 1; i < normalized.Length; i++)
            {
                var prior = normalized[i - 1]; var current = normalized[i];
                if (current.Anchors[0].S < prior.Anchors[^1].S)
                    throw new WorldDataException(b.Files[current.Id], current.Id, "Corridor intervals overlap on the same original source road.");
                if (current.Anchors[0].S == prior.Anchors[^1].S
                    && (Math.Abs(current.Anchors[0].Y - prior.Anchors[^1].Y) > TerrainRoadProfile.JunctionHeightTolerance
                        || Math.Abs(current.CrossFall - prior.CrossFall) > TerrainMath.GradeTolerance || Math.Abs(current.Shoulder - prior.Shoulder) > TerrainMath.HeightTolerance))
                    throw new WorldDataException(b.Files[current.Id], current.Id, "Touching source-road intervals need matching endpoint height, cross-fall and shoulder.");
            }
            var exclusions = BridgeExclusions(b, road);
            foreach (var op in normalized)
            {
                if (b.Context.Zones is { } zones && zones.TryGetValue(op.Zone, out var polygons)
                    && !TerrainUnion.CoversPath(new(polygons), road.Path, op.Anchors[0].S, op.Anchors[^1].S))
                    throw new WorldDataException(b.Files[op.Id], op.Id, "Corridor source interval leaves its authoritative zone, including an unchanged flat interval.");
                foreach (var step in op.Steps)
                {
                    var structure = b.References.Structure(step.Structure, b.Files[op.Id], op.Id, "stair", "stairs", "steps", "terrace");
                    foreach (var station in new[] { step.S0, (step.S0 + step.S1) / 2, step.S1 })
                        if (!TerrainReferences.NearStructure(structure, road.Path.At(station), 0.05))
                            throw new WorldDataException(b.Files[op.Id], op.Id, "Corridor step interval is not covered by its named real stair footprint/alignment.");
                    exclusions.Add((step.S0, step.S1));
                    b.Diagnostics.Add($"NOT_VERIFIED: stair '{step.Structure.Value}' plan coverage resolved; its generated end elevations/collision require content acceptance.");
                }
            }
            plans.Add(new(road, normalized, exclusions));
        }
        return plans.ToArray();
    }
}
