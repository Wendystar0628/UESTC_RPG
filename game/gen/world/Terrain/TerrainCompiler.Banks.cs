using Campus.Contracts;

namespace Campus.Gen.World;

internal static partial class TerrainCompiler
{
    private sealed record RegisteredBank(WorldFeature Feature, FeatureId Water);

    private static IReadOnlyList<RegisteredBank> ReadRegisteredBanks(Builder b)
    {
        var result = new List<RegisteredBank>();
        foreach (var feature in b.Context.Features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Kind.Value != "bank" || feature.OptionalText("form") != "hard") continue;
            var water = new FeatureId(feature.Text("water"));
            if (!b.WaterSpecs.Any(spec => spec.Id == water))
                throw new WorldDataException("world", feature.Id.Value, "Hard bank must reference a registered surface-water feature.");
            if (feature.Geometry is not PolylineGeometry { Line.Points.Count: 2 })
                throw new WorldDataException("world", feature.Id.Value, "Hard bank selection requires two actual shore endpoints.");
            result.Add(new(feature, water));
        }
        return result;
    }

    private static IReadOnlyList<TerrainBankInterval> RegisteredBankIntervals(IReadOnlyList<RegisteredBank> banks,
        WaterSpec[] members, TerrainRegion domain)
    {
        var intervals = new List<TerrainBankInterval>();
        foreach (var bank in banks.Where(bank => members.Any(member => member.Id == bank.Water)))
        {
            var points = ((PolylineGeometry)bank.Feature.Geometry).Line.Points;
            var source = members.Single(member => member.Id == bank.Water).Region;
            if (points.Any(point => Math.Abs(source.Distance(point.X, point.Y).Height) > WorldFeatureGeometry.SelectionEndpointTolerance))
                throw new WorldDataException("world", bank.Feature.Id.Value, "Hard bank endpoints must lie on the referenced water shore.");
            var matches = TerrainBankGeometry.Rings(domain).Select(ring =>
                (ring.Polygon, ring.Ring, ring.Path,
                    From: ring.Path.Project(points[0].X, points[0].Y),
                    To: ring.Path.Project(points[1].X, points[1].Y)))
                .Where(ring => ring.From.Distance <= WorldFeatureGeometry.SelectionEndpointTolerance
                    && ring.To.Distance <= WorldFeatureGeometry.SelectionEndpointTolerance).ToArray();
            if (matches.Length != 1)
                throw new WorldDataException("world", bank.Feature.Id.Value, "Hard bank endpoints must select one actual exposed water ring.");
            var match = matches[0];
            var from = Math.Min(match.From.Station, match.To.Station);
            var to = Math.Max(match.From.Station, match.To.Station);
            // The two authored endpoints select the local arc. Keep the canonical ring
            // direction, including when that arc crosses its zero station.
            if (to - from > match.Path.Length / 2) (from, to) = (to, from + match.Path.Length);
            if (to - from <= TerrainMath.HeightTolerance)
                throw new WorldDataException("world", bank.Feature.Id.Value, "Hard bank endpoints must select a nonempty shore interval.");
            intervals.Add(new(match.Polygon, match.Ring, from, to, match.Path.Length, [], []));
        }
        return TerrainBankGeometry.Merge(intervals);
    }
}
