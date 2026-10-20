using Campus.Contracts;
using Campus.Contracts.Maps;
using static Campus.Gen.World.TerrainCorridorCompiler;
using static Campus.Gen.World.TerrainFieldGrading;
using static Campus.Gen.World.TerrainWaterNetwork;

namespace Campus.Gen.World;

internal static class TerrainRoadWaterComposition
{
    internal sealed record RoadWaterComposition(WaterShore Shore, TerrainShoreEnvelope Envelope);

    internal static void PrepareRoadWaterComposition(TerrainBuildState b)
    {
        ValidateFixedRoadWaterAnchors(b);
        if (b.Roads.Count == 0) return;
        foreach (var shore in b.WaterShores.OrderBy(shore => shore.Id, StringComparer.Ordinal))
            b.RoadWaterCompositions.Add(new(shore, new(shore.Region,
                (x, z) => b.LandBeforeCorridors.Evaluate(x, z).Surface.Height, shore.File, shore.Id, 1d / 3)));
    }

    private static void ValidateFixedRoadWaterAnchors(TerrainBuildState b)
    {
        foreach (var anchor in b.RoadAnchors)
            foreach (var shore in b.WaterShores)
            {
                if (shore.Region.Distance(anchor.X, anchor.Z).Height > 0) continue;
                var bed = b.LandBeforeCorridors.Evaluate(anchor.X, anchor.Z).Surface.Height;
                if (Math.Abs(anchor.Y - bed) > TerrainMath.HeightTolerance)
                    throw new WorldDataException(anchor.File, anchor.Id,
                        $"Authored centreline anchor conflicts with fixed water core '{shore.Id}' at ({anchor.X:R},{anchor.Z:R}): anchorY={anchor.Y:R}, waterBedY={bed:R}, deltaY={bed - anchor.Y:R}; a real bridge/underpass exclusion is required.");
            }
    }

    internal static void ValidateSourceRoadWaterCrossings(TerrainBuildState b)
    {
        foreach (var entry in b.Roads)
            foreach (var shore in b.WaterShores)
            {
                var road = entry.Road;
                if (road.Path.Bounds.Max.X < shore.Region.Bounds.Min.X || road.Path.Bounds.Min.X > shore.Region.Bounds.Max.X
                    || road.Path.Bounds.Max.Y < shore.Region.Bounds.Min.Y || road.Path.Bounds.Min.Y > shore.Region.Bounds.Max.Y) continue;
                // Split only at real source vertices, shore intersections and structural
                // interval ends. Containment is constant on each resulting straight span.
                var stations = new SortedSet<double>(road.Path.VertexStations);
                foreach (var polygon in shore.Region.Polygons)
                    foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                        foreach (var hit in road.Path.Intersections(new(new(ring.Append(ring[0]).ToArray())))) stations.Add(hit.Station);
                var exclusions = entry.Plan?.Exclusions ?? BridgeExclusions(b, road)
                    .Concat(entry.Operation?.Steps.Select(step => (From: step.S0, To: step.S1)) ?? []).ToArray();
                foreach (var span in exclusions)
                {
                    stations.Add(Math.Clamp(span.From, 0, road.Path.Length));
                    stations.Add(Math.Clamp(span.To, 0, road.Path.Length));
                }
                var values = stations.ToArray();
                for (var i = 1; i < values.Length; i++)
                {
                    var station = (values[i - 1] + values[i]) / 2;
                    if (entry.Profile.Excluded(station)) continue;
                    var point = road.Path.AtDouble(station);
                    if (shore.Region.Distance(point.X, point.Z).Height >= -TerrainMath.HeightTolerance) continue;
                    var op = entry.Plan?.At(station) ?? entry.Operation;
                    var file = op is null ? entry.File : b.Files[op.Id];
                    throw new WorldDataException(file, op?.Id ?? "auto-corridor-" + road.Feature.Value,
                        $"Source road centreline crosses registered surface-water core '{shore.Id}' without a real bridge/underpass exclusion: road='{road.Source.Value}', station={station:R}, point=({point.X:R},{point.Z:R}). Pavement-edge grading cannot authorize a crossing.");
                }
            }
    }

    internal static TerrainEvaluation ConstrainRoadAtWater(TerrainBuildState b,
        TerrainEvaluation previous, TerrainEvaluation proposed, double x, double z)
    {
        if (proposed.Surface == previous.Surface) return proposed;
        var surface = proposed.Surface; var toe = proposed.ToeDistance;
        foreach (var composition in b.RoadWaterCompositions)
        {
            var distance = composition.Shore.Region.Distance(x, z).Height;
            // The union's real bed and shoreline outrank every corridor edge and shoulder.
            // Source-centreline crossings are content acceptance, checked offline; they
            // never authorize road-side fill or an invented bridge in the published field.
            if (distance <= 0) return previous;
            // Intersect with fixed-shore slope cones instead of multiplying competing masks.
            // Min/max preserves the original field's grade and finite support: inherited banks
            // can remain, but a road cannot create a steeper connection or reverse fill/cut.
            surface = composition.Envelope.Restrict(previous.Surface, surface, x, z);
            toe = Math.Min(toe, distance);
        }
        return WithSurface(previous, surface, proposed.CrestDistance, toe);
    }
}
