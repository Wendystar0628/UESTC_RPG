using Campus.Contracts;
using Campus.Contracts.Maps;
using static Campus.Gen.World.TerrainWaterSources;

namespace Campus.Gen.World;

internal static class TerrainBankRegistration
{
    internal sealed record RegisteredBank(WorldFeature Feature, FeatureId Water);

    internal static IReadOnlyList<RegisteredBank> ReadRegisteredBanks(TerrainBuildState b)
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

    internal static IReadOnlyList<TerrainBankInterval> RegisteredBankIntervals(IReadOnlyList<RegisteredBank> banks,
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

    internal static IReadOnlyList<ResolvedTerrainBank> ExportBanks(TerrainBuildState b)
    {
        var result = new List<ResolvedTerrainBank>();
        foreach (var shore in b.WaterShores.OrderBy(shore => shore.Id, StringComparer.Ordinal))
        {
            var intervals = shore.OriginalHard.ToList();
            if (shore.Banks is { } bank)
            {
                intervals.AddRange(bank.RegisteredHard);
                intervals.AddRange(bank.Contacts.Select(contact => new TerrainBankInterval(contact.Polygon, contact.Ring,
                    contact.ShoreStation - TerrainBridgeBanks.HardHalfLength, contact.ShoreStation + TerrainBridgeBanks.HardHalfLength,
                    contact.RingLength, [], [contact.Bridge])));
                intervals.AddRange(bank.RoadSpans.Select(span => new TerrainBankInterval(span.Polygon, span.Ring, span.From, span.To,
                    span.RingLength, span.Roads.Select(road => road.Road).ToArray(), [])));
            }
            var index = 0;
            foreach (var interval in TerrainBankGeometry.Merge(intervals))
            {
                var id = $"{shore.Waters.OrderBy(water => water.Value, StringComparer.Ordinal).First().Value}:bank:{interval.Polygon}:{interval.Ring}:{index++}";
                var span = TerrainBankGeometry.Create(id, shore.Waters, shore.Region, interval);
                result.Add(new(span, shore.ShoreTop));
            }
        }
        return result;
    }
}
