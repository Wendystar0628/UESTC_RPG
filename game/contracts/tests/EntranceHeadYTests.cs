using System.Text.Json;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class EntranceHeadYTests
{
    private static readonly FeatureId Feature = new("fixture:building");
    private const string Entry = """
        {"id":"entry.main","featureId":"fixture:building","outsideLanding":[1,0.25,2],
         "insideLanding":[3,0.5,4],"doorCenter":[5,0.75,6],"facingDeg":127,"clearWidth":1.25,"open":true}
        """;

    [TestCase]
    public void OldConstructorAndMissingOrZeroRawHeadRemainUnknownAfterPadTranslation()
    {
        var oldDeclaration = new EntranceDecl("entry.main", new(1, 2, 3), new(4, 5, 6), 1.25f, true);
        Require(oldDeclaration.HeadY == 0, "The existing constructor must keep an unknown head.");
        foreach (var height in new[] { 2f, -2f, 0f })
        {
            var terrain = new PadTerrain(height);
            var omitted = Parse(Add("\"heightRef\":\"pad\""), terrain);
            var zero = Parse(Add("\"heightRef\":\"pad\",\"headY\":0"), terrain);
            Require(omitted.HeadY == 0 && zero.HeadY == 0 && omitted == zero && omitted.Center.Y == .75f + height,
                "An unknown head acquired a pad height or prevented the three points from moving.");
        }
    }

    [TestCase]
    public void KnownHeadFollowsRawReferenceAndSerializesOnlyItsAbsoluteResult()
    {
        var unused = new PadTerrain(20);
        Require(Parse(Add("\"headY\":3.5"), unused).HeadY == 3.5f &&
            Parse(Add("\"heightRef\":\"absolute\",\"headY\":-0.5"), unused).HeadY == -.5f && unused.Calls == 0,
            "Absolute heads must remain absolute, including negative world elevations.");
        foreach (var pad in new[] { 2f, -4f })
        {
            var terrain = new PadTerrain(pad);
            var raw = Add("\"heightRef\":\"pad\",\"headY\":3.5");
            var resolved = Parse(raw, terrain);
            Require(resolved.HeadY == 3.5f + pad && terrain.Calls == 1,
                "Known head must use the same single pad sample as the entrance points.");
            var json = JsonSerializer.Serialize(resolved, ContractJson.Options);
            Require(!json.Contains("heightRef", StringComparison.Ordinal) &&
                JsonSerializer.Deserialize<EntranceDecl>(json, ContractJson.Options) == resolved &&
                Parse(raw, new PadTerrain(pad)) == resolved,
                "Raw parsing or resolved-object round trip applied the head offset twice.");
        }
    }

    [TestCase]
    public void InvalidHeadsAndKnownValuesCollidingWithUnknownSentinelAreRejected()
    {
        foreach (var value in new[] { "null", "\"3.5\"", "true", "1e999", "-1e999" })
            Throws<JsonException>(() => Parse(Add("\"headY\":" + value)));
        Throws<JsonException>(() => Parse(Add("\"headY\":3.5,\"headY\":4")));
        Throws<JsonException>(() => Parse(Add("\"heightRef\":\"pad\",\"headY\":3.5"), new PadTerrain(-3.5f)));
        Throws<JsonException>(() => Parse(Add("\"heightRef\":\"pad\",\"headY\":3e38"), new PadTerrain(3e38f)));
    }

    private static string Add(string fields) => Entry.Insert(Entry.Length - 1, "," + fields);
    private static EntranceDecl Parse(string text, ITerrain? terrain = null)
    {
        using var document = JsonDocument.Parse(text);
        return InteriorEntranceJson.Parse(document.RootElement, Feature, terrain);
    }

    private sealed class PadTerrain(float value) : ITerrain
    {
        public int Calls { get; private set; }
        public float PadY(FeatureId feature) { Calls++; return feature == Feature ? value : throw new KeyNotFoundException(); }
        public bool IsFlat(TileKey tile) => throw new NotSupportedException();
        public float HeightAt(float x, float z) => throw new NotSupportedException();
        public void FillTile(TileKey tile, int level, Span<short> cm) => throw new NotSupportedException();
        public float? WaterY(FeatureId water) => throw new NotSupportedException();
        public TerrainFacts FactsAt(float x, float z) => throw new NotSupportedException();
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
