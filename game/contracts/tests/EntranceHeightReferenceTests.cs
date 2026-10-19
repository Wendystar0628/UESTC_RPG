using Campus.Tests;
using System.Numerics;
using System.Text.Json;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class EntranceHeightReferenceTests
{
    private static readonly FeatureId Feature = new("fixture:building");
    private const string Entry = """
        {"id":"entry.main","featureId":"fixture:building","outsideLanding":[1,0.25,2],
         "insideLanding":[3,0.5,4],"doorCenter":[5,0.75,6],"facingDeg":127,"clearWidth":1.25,"open":true}
        """;
    private static readonly EntranceDecl Absolute = new("entry.main", new(1, .25f, 2), new(3, .5f, 4), 1.25f, true)
    { Center = new(5, .75f, 6), YawDeg = 127 };

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LegacyAndAbsolutePreserveAllFieldsWithoutQueryingTerrain()
    {
        var terrain = new ProbeTerrain(9, new NotSupportedException("Pad lookup must not run."));
        AssertThat(Parse(Entry) == Absolute && Parse(WithRef("\"absolute\"")) == Absolute).OverrideFailureMessage("Legacy absolute coordinates changed without terrain.").IsTrue();
        AssertThat(Parse(Entry, terrain) == Absolute && Parse(WithRef("\"absolute\""), terrain) == Absolute && terrain.Calls == 0).OverrideFailureMessage("Absolute coordinates queried terrain or changed authored values.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void PositiveAndNegativePadsMoveThreePointsOnceAndPreserveRelativeOffsets()
    {
        foreach (var padY in new[] { 2f, -1.5f, 0f })
        {
            var terrain = new ProbeTerrain(padY);
            var resolved = Parse(WithRef("\"pad\""), terrain);
            var shift = new Vector3(0, padY, 0);
            AssertThat(resolved.Outside == Absolute.Outside + shift && resolved.Inside == Absolute.Inside + shift &&
                resolved.Center == Absolute.Center + shift && resolved.Inside - resolved.Outside == Absolute.Inside - Absolute.Outside &&
                resolved.Center - resolved.Outside == Absolute.Center - Absolute.Outside).OverrideFailureMessage("The three points did not share one pad offset.").IsTrue();
            AssertThat(resolved.Id == Absolute.Id && resolved.YawDeg == Absolute.YawDeg &&
                resolved.ClearWidth == Absolute.ClearWidth && resolved.Open == Absolute.Open &&
                terrain.Calls == 1 && terrain.LastFeature == Feature).OverrideFailureMessage("Pad lookup changed metadata or queried the wrong feature.").IsTrue();
            var json = JsonSerializer.Serialize(resolved, ContractJson.Options);
            AssertThat(!json.Contains("heightRef", StringComparison.Ordinal) &&
                JsonSerializer.Deserialize<EntranceDecl>(json, ContractJson.Options) == resolved).OverrideFailureMessage("Resolved entrances must remain absolute across their normal contract round trip.").IsTrue();
        }
        // Readiness belongs to the caller. An explicitly supplied zero-height implementation is valid.
        AssertThat(Parse(WithRef("\"pad\""), FlatTerrain.Default) == Absolute).OverrideFailureMessage("Explicit zero pad was mistaken for missing input.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void PadRequiresExplicitInputAndPropagatesUnsupportedOrUnknownFeatures()
    {
        {
            bool rejected = false;
            try { Parse(WithRef("\"pad\"")); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
        foreach (var cause in new Exception[] { new NotSupportedException("unimplemented"), new KeyNotFoundException("missing pad") })
        {
            var terrain = new ProbeTerrain(0, cause);
            JsonException? error = null;
            try { Parse(WithRef("\"pad\""), terrain); }
            catch (JsonException caught) { error = caught; }
            AssertThat(error is not null).OverrideFailureMessage("Expected JsonException.").IsTrue();
            AssertThat(ReferenceEquals(error!.InnerException, cause) && terrain.Calls == 1).OverrideFailureMessage("Pad failure lost its cause or repeated the terrain query.").IsTrue();
        }
        var mismatched = new ProbeTerrain(2);
        using var document = JsonDocument.Parse(WithRef("\"pad\""));
        {
            bool rejected = false;
            try { InteriorEntranceJson.Parse(document.RootElement, new("fixture:other"), mismatched); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
        AssertThat(mismatched.Calls == 0).OverrideFailureMessage("An entrance was resolved against a different feature's pad.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void UnknownNullDuplicateAndMalformedReferencesAreRejected()
    {
        foreach (var reference in new[] { "\"ground\"", "\"PAD\"", "\"\"", "null", "0", "[\"pad\"]", "{}" })
        {
            bool rejected = false;
            try { Parse(WithRef(reference), new ProbeTerrain(2)); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
        {
            bool rejected = false;
            try { Parse(WithRef("\"absolute\",\"heightRef\":\"pad\""), new ProbeTerrain(2)); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
        {
            bool rejected = false;
            try { Parse(Entry.Insert(Entry.Length - 1, ",\"heightReference\":\"pad\""), new ProbeTerrain(2)); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NonFiniteRawPadAndResolvedHeightsAreRejected()
    {
        foreach (var padY in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            bool rejected = false;
            try { Parse(WithRef("\"pad\""), new ProbeTerrain(padY)); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
        foreach (var field in new[] { "0.25", "0.5", "0.75", "127", "1.25" })
        {
            bool rejected = false;
            try { Parse(Entry.Replace(field, "1e999", StringComparison.Ordinal)); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
        {
            bool rejected = false;
            try { Parse(WithRef("\"pad\"").Replace("0.25", "3e38", StringComparison.Ordinal), new ProbeTerrain(3e38f)); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
        {
            bool rejected = false;
            try { Parse(Entry.Replace("[1,0.25,2]", "[1,2]", StringComparison.Ordinal)); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RequiredEntranceFieldsAndMalformedClosedDoorsAreRejected()
    {
        using var entry = JsonDocument.Parse(Entry);
        foreach (var field in entry.RootElement.EnumerateObject())
        {
            var missing = "{" + string.Join(",", entry.RootElement.EnumerateObject()
                .Where(property => property.Name != field.Name)
                .Select(property => JsonSerializer.Serialize(property.Name) + ":" + property.Value.GetRawText())) + "}";
            {
                bool rejected = false;
                try { Parse(missing); }
                catch (JsonException) { rejected = true; }
                AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
            }
        }
        {
            bool rejected = false;
            try { Parse(Entry.Replace("\"open\":true", "\"open\":null", StringComparison.Ordinal)); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
        {
            bool rejected = false;
            try { Parse(Entry.Replace("\"clearWidth\":1.25", "\"clearWidth\":\"1.25\"", StringComparison.Ordinal)); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
        foreach (var door in new[]
        {
            "null",
            "{}",
            Entry.Replace("\"open\":true", "\"open\":false,\"heightRef\":\"pad\"", StringComparison.Ordinal)
        })
        {
            using var catalog = JsonDocument.Parse("{\"interiors\":[],\"closedEntrances\":[" + door + "]}");
            AssertThrown(() => InteriorEntranceJson.ParseClosedEntrances(catalog.RootElement)).IsInstanceOf<JsonException>();
        }
    }

    private static string WithRef(string literal) => Entry.Insert(Entry.Length - 1, ",\"heightRef\":" + literal);
    private static EntranceDecl Parse(string text, ITerrain? terrain = null)
    {
        using var document = JsonDocument.Parse(text);
        return InteriorEntranceJson.Parse(document.RootElement, Feature, terrain);
    }

    private sealed class ProbeTerrain(float padY, Exception? failure = null) : ITerrain
    {
        public TerrainInputSummary SourceInputs => throw new NotSupportedException();
        public int Calls { get; private set; }
        public FeatureId LastFeature { get; private set; }
        public float PadY(FeatureId feature) { Calls++; LastFeature = feature; if (failure is not null) throw failure; return padY; }
        public bool TryGetPadY(FeatureId feature, out float y)
        {
            if (failure is KeyNotFoundException) { Calls++; LastFeature = feature; y = default; return false; }
            y = PadY(feature);
            return true;
        }
        public bool IsFlat(TileKey tile) => throw new NotSupportedException();
        public float HeightAt(float x, float z) => throw new NotSupportedException();
        public (float Height, TerrainFacts Facts) SampleAt(float x, float z) => throw new NotSupportedException();
        public void FillTile(TileKey tile, int level, Span<short> cm) => throw new NotSupportedException();
        public float? WaterY(FeatureId water) => throw new NotSupportedException();
        public TerrainFacts FactsAt(float x, float z) => throw new NotSupportedException();
    }
}
