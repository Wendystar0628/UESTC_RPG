using Campus.Tests;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts.Maps;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class TerrainInputBoundaryTests
{
    private const string DefaultsJson = """
        {"lakeBankSlope":"1:5","lakeMaxDepth":1.5,"channelBankSlope":"1:4","channelDepth":1.2,"corridorShoulder":0.75}
        """;

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MapDefaultsRoundTripAndCompletePreserveExplicitValues()
    {
        var defaults = JsonSerializer.Deserialize<TerrainDefaults>(DefaultsJson, ContractJson.Options)!;
        var file = File() with { TerrainDefaults = defaults };
        var copy = JsonSerializer.Deserialize<MapFile>(JsonSerializer.Serialize(file, ContractJson.Options), ContractJson.Options)!;
        var descriptor = Complete(copy);
        if (copy.TerrainDefaults != defaults || descriptor.TerrainDefaults != defaults ||
            defaults != new TerrainDefaults("1:5", 1.5f, "1:4", 1.2f, .75f))
            throw new InvalidOperationException("Explicit terrain defaults were not preserved.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MissingAndExplicitNullStayMissingWithoutInventedDefaults()
    {
        var json = JsonSerializer.Serialize(File(), ContractJson.Options);
        if (json.Contains("terrainDefaults", StringComparison.Ordinal)) throw new InvalidOperationException("Null defaults should be omitted.");
        foreach (var input in new[] { json, json.Insert(json.Length - 1, ",\"terrainDefaults\":null") })
        {
            var file = JsonSerializer.Deserialize<MapFile>(input, ContractJson.Options)!;
            if (file.TerrainDefaults is not null || Complete(file).TerrainDefaults is not null)
                throw new InvalidOperationException("Missing terrain defaults were invented.");
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void InvalidRatiosAndAllNonPositiveOrNonFiniteNumbersAreRejected()
    {
        var valid = new TerrainDefaults("1:5", 1.5f, "1:4", 1.2f, .75f);
        foreach (var ratio in new[] { "1:0", "1:-4", "2:5", "1:NaN", "1:5\n", "1:1e2", "1:" + new string('9', 400), "", null })
        {
            AssertThrown(() => TerrainOverlayJson.ValidateTerrainDefaults(valid with { LakeBankSlope = ratio! })).IsInstanceOf<ArgumentException>();
            AssertThrown(() => TerrainOverlayJson.ValidateTerrainDefaults(valid with { ChannelBankSlope = ratio! })).IsInstanceOf<ArgumentException>();
        }
        foreach (var number in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            AssertThrown(() => TerrainOverlayJson.ValidateTerrainDefaults(valid with { LakeMaxDepth = number })).IsInstanceOf<ArgumentException>();
            AssertThrown(() => TerrainOverlayJson.ValidateTerrainDefaults(valid with { ChannelDepth = number })).IsInstanceOf<ArgumentException>();
            AssertThrown(() => TerrainOverlayJson.ValidateTerrainDefaults(valid with { CorridorShoulder = number })).IsInstanceOf<ArgumentException>();
            AssertThrown(() => Complete(File() with { TerrainDefaults = valid with { LakeMaxDepth = number } })).IsInstanceOf<ArgumentException>();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LocalJsonBoundaryRejectsMissingDuplicateUnknownAndInvalidValues()
    {
        foreach (var invalid in new[]
        {
            DefaultsJson.Replace("\"corridorShoulder\":0.75", "\"unknown\":0.75", StringComparison.Ordinal),
            DefaultsJson.Replace("\"corridorShoulder\":0.75", "\"CorridorShoulder\":0.75", StringComparison.Ordinal),
            DefaultsJson.Replace(",\"corridorShoulder\":0.75", "", StringComparison.Ordinal),
            DefaultsJson.Replace("1.5", "1e1000", StringComparison.Ordinal),
            DefaultsJson.Replace("1.5", "0", StringComparison.Ordinal),
            DefaultsJson.Replace("1.5", "-1", StringComparison.Ordinal),
            DefaultsJson.Replace("1.5", "\"1.5\"", StringComparison.Ordinal),
            DefaultsJson.Replace("\"1:5\"", "null", StringComparison.Ordinal),
            DefaultsJson.Replace("1:5", "1:0", StringComparison.Ordinal),
            DefaultsJson.Insert(DefaultsJson.Length - 1, ",\"channelDepth\":1.2"),
            DefaultsJson.Insert(DefaultsJson.Length - 1, ",\"extra\":true")
        })
        {
            bool rejected = false;
            try { JsonSerializer.Deserialize<TerrainDefaults>(invalid, ContractJson.Options); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
        // The local converter must not change the repository-wide compatibility options.
        if (!ContractJson.Options.PropertyNameCaseInsensitive) throw new InvalidOperationException("Global JSON options were changed.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ZoneBoundarySnapshotOwnsDictionaryPolygonsAndEveryCoordinateRing()
    {
        var outer = new[] { new Vector2(0, 0), new Vector2(8, 0), new Vector2(0, 8) };
        var hole = new[] { new Vector2(1, 1), new Vector2(1, 2), new Vector2(2, 1) };
        var holes = new List<IReadOnlyList<Vector2>> { hole };
        var polygons = new List<PolygonRings> { new(outer, holes) };
        var source = new Dictionary<ZoneId, IReadOnlyList<PolygonRings>> { [new("z")] = polygons, [new("a")] = [] };
        var inputs = new WorldModelLoadInputs(Complete(File())) { ZoneBoundaries = source };
        outer[0] = new(99, 99); hole[0] = new(99, 99); holes.Clear(); polygons.Clear(); source.Clear();
        var snapshot = inputs.ZoneBoundaries!;
        if (!snapshot.Keys.Select(z => z.Value).SequenceEqual(new[] { "a", "z" }) ||
            snapshot[new("z")][0].Outer[0] != Vector2.Zero || snapshot[new("z")][0].Holes[0][0] != Vector2.One)
            throw new InvalidOperationException("Zone boundaries still alias producer collections or coordinates.");
        AssertThrown(() => ((IDictionary<ZoneId, IReadOnlyList<PolygonRings>>)snapshot).Clear()).IsInstanceOf<NotSupportedException>();
        AssertThrown(() => ((IList<PolygonRings>)snapshot[new("z")]).Clear()).IsInstanceOf<NotSupportedException>();
        AssertThrown(() => ((IList<Vector2>)snapshot[new("z")][0].Outer)[0] = Vector2.One).IsInstanceOf<NotSupportedException>();
        AssertThrown(() => ((IList<IReadOnlyList<Vector2>>)snapshot[new("z")][0].Holes).Clear()).IsInstanceOf<NotSupportedException>();
        AssertThrown(() => ((IList<Vector2>)snapshot[new("z")][0].Holes[0])[0] = Vector2.Zero).IsInstanceOf<NotSupportedException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MissingZoneBoundariesRemainDistinctFromProvidedEmptyBoundaries()
    {
        var missing = new WorldModelLoadInputs(Complete(File()));
        var empty = missing with { ZoneBoundaries = new Dictionary<ZoneId, IReadOnlyList<PolygonRings>>() };
        var reset = empty with { ZoneBoundaries = null };
        if (missing.ZoneBoundaries is not null || empty.ZoneBoundaries is not { Count: 0 } || reset.ZoneBoundaries is not null)
            throw new InvalidOperationException("Zone boundary availability was collapsed.");
        // This asserts transport availability only. Missing boundaries do not mean terrain validation passed.
    }

    private static MapFile File() => new(new("fixture"), new("Fixture", "Fixture"), MapKind.Outdoor, 1, 1,
        new(Vector2.Zero, new(64, 64)), new(1, 0, 64, 0, 64, 1, 1, 1), "start", [],
        new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("Default", "Default"), false, "fixture"),
        new("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);

    private static MapDescriptor Complete(MapFile file) => file.Complete(
        new(0, 0, 32648, Vector2.Zero, new(64, 64)), new(32, 32),
        new(Vector3.Zero, new(64, 64, 64)), new(0, 0), new(0, 0));
}
