using System.Numerics;
using System.Text.Json;
using GdUnit4;

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
    public void LegacyAndAbsolutePreserveAllFieldsWithoutQueryingTerrain()
    {
        var terrain = new ProbeTerrain(9, new NotSupportedException("Pad lookup must not run."));
        Require(Parse(Entry) == Absolute && Parse(WithRef("\"absolute\"")) == Absolute,
            "Legacy absolute coordinates changed without terrain.");
        Require(Parse(Entry, terrain) == Absolute && Parse(WithRef("\"absolute\""), terrain) == Absolute && terrain.Calls == 0,
            "Absolute coordinates queried terrain or changed authored values.");
    }

    [TestCase]
    public void PositiveAndNegativePadsMoveThreePointsOnceAndPreserveRelativeOffsets()
    {
        foreach (var padY in new[] { 2f, -1.5f, 0f })
        {
            var terrain = new ProbeTerrain(padY);
            var resolved = Parse(WithRef("\"pad\""), terrain);
            var shift = new Vector3(0, padY, 0);
            Require(resolved.Outside == Absolute.Outside + shift && resolved.Inside == Absolute.Inside + shift &&
                resolved.Center == Absolute.Center + shift && resolved.Inside - resolved.Outside == Absolute.Inside - Absolute.Outside &&
                resolved.Center - resolved.Outside == Absolute.Center - Absolute.Outside,
                "The three points did not share one pad offset.");
            Require(resolved.Id == Absolute.Id && resolved.YawDeg == Absolute.YawDeg &&
                resolved.ClearWidth == Absolute.ClearWidth && resolved.Open == Absolute.Open &&
                terrain.Calls == 1 && terrain.LastFeature == Feature, "Pad lookup changed metadata or queried the wrong feature.");
            var json = JsonSerializer.Serialize(resolved, ContractJson.Options);
            Require(!json.Contains("heightRef", StringComparison.Ordinal) &&
                JsonSerializer.Deserialize<EntranceDecl>(json, ContractJson.Options) == resolved,
                "Resolved entrances must remain absolute across their normal contract round trip.");
        }
        // Readiness belongs to the caller. An explicitly supplied zero-height implementation is valid.
        Require(Parse(WithRef("\"pad\""), FlatTerrain.Default) == Absolute, "Explicit zero pad was mistaken for missing input.");
    }

    [TestCase]
    public void PadRequiresExplicitInputAndPropagatesUnsupportedOrUnknownFeatures()
    {
        Throws<JsonException>(() => Parse(WithRef("\"pad\"")));
        foreach (var cause in new Exception[] { new NotSupportedException("unimplemented"), new KeyNotFoundException("missing pad") })
        {
            var terrain = new ProbeTerrain(0, cause);
            var error = Throws<JsonException>(() => Parse(WithRef("\"pad\""), terrain));
            Require(ReferenceEquals(error.InnerException, cause) && terrain.Calls == 1 &&
                error.Message.Contains("entry.main", StringComparison.Ordinal) &&
                error.Message.Contains(Feature.Value, StringComparison.Ordinal), "Pad failure lost its source or identity.");
        }
        var mismatched = new ProbeTerrain(2);
        using var document = JsonDocument.Parse(WithRef("\"pad\""));
        Throws<JsonException>(() => InteriorEntranceJson.Parse(document.RootElement, new("fixture:other"), mismatched));
        Require(mismatched.Calls == 0, "An entrance was resolved against a different feature's pad.");
    }

    [TestCase]
    public void UnknownNullDuplicateAndMalformedReferencesAreRejected()
    {
        foreach (var reference in new[] { "\"ground\"", "\"PAD\"", "\"\"", "null", "0", "[\"pad\"]", "{}" })
            Throws<JsonException>(() => Parse(WithRef(reference), new ProbeTerrain(2)));
        Throws<JsonException>(() => Parse(WithRef("\"absolute\",\"heightRef\":\"pad\""), new ProbeTerrain(2)));
        Throws<JsonException>(() => Parse(Entry.Insert(Entry.Length - 1, ",\"heightReference\":\"pad\""), new ProbeTerrain(2)));
    }

    [TestCase]
    public void NonFiniteRawPadAndResolvedHeightsAreRejected()
    {
        foreach (var padY in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            Throws<JsonException>(() => Parse(WithRef("\"pad\""), new ProbeTerrain(padY)));
        foreach (var field in new[] { "0.25", "0.5", "0.75", "127", "1.25" })
            Throws<JsonException>(() => Parse(Entry.Replace(field, "1e999", StringComparison.Ordinal)));
        Throws<JsonException>(() => Parse(WithRef("\"pad\"").Replace("0.25", "3e38", StringComparison.Ordinal), new ProbeTerrain(3e38f)));
        Throws<JsonException>(() => Parse(Entry.Replace("[1,0.25,2]", "[1,2]", StringComparison.Ordinal)));
    }

    private static string WithRef(string literal) => Entry.Insert(Entry.Length - 1, ",\"heightRef\":" + literal);
    private static EntranceDecl Parse(string text, ITerrain? terrain = null)
    {
        using var document = JsonDocument.Parse(text);
        return InteriorEntranceJson.Parse(document.RootElement, Feature, terrain);
    }

    private sealed class ProbeTerrain(float padY, Exception? failure = null) : ITerrain
    {
        public int Calls { get; private set; }
        public FeatureId LastFeature { get; private set; }
        public float PadY(FeatureId feature) { Calls++; LastFeature = feature; if (failure is not null) throw failure; return padY; }
        public bool IsFlat(TileKey tile) => throw new NotSupportedException();
        public float HeightAt(float x, float z) => throw new NotSupportedException();
        public void FillTile(TileKey tile, int level, Span<short> cm) => throw new NotSupportedException();
        public float? WaterY(FeatureId water) => throw new NotSupportedException();
        public TerrainFacts FactsAt(float x, float z) => throw new NotSupportedException();
    }

    private static T Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T error) { return error; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
