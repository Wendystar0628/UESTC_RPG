using System.Text.Json;
using Campus.Gen.Tests.Support;
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
            var omitted = Parse(Add("\"heightRef\":\"pad\""), terrain.Terrain);
            var zero = Parse(Add("\"heightRef\":\"pad\",\"headY\":0"), terrain.Terrain);
            Require(omitted.HeadY == 0 && zero.HeadY == 0 && omitted == zero && omitted.Center.Y == .75f + height,
                "An unknown head acquired a pad height or prevented the three points from moving.");
        }
    }

    [TestCase]
    public void KnownHeadFollowsRawReferenceAndSerializesOnlyItsAbsoluteResult()
    {
        var unused = new PadTerrain(20);
        Require(Parse(Add("\"headY\":3.5"), unused.Terrain).HeadY == 3.5f &&
            Parse(Add("\"heightRef\":\"absolute\",\"headY\":-0.5"), unused.Terrain).HeadY == -.5f && unused.Calls == 0,
            "Absolute heads must remain absolute, including negative world elevations.");
        foreach (var pad in new[] { 2f, -4f })
        {
            var terrain = new PadTerrain(pad);
            var raw = Add("\"heightRef\":\"pad\",\"headY\":3.5");
            var resolved = Parse(raw, terrain.Terrain);
            Require(resolved.HeadY == 3.5f + pad && terrain.Calls == 1,
                "Known head must use the same single pad sample as the entrance points.");
            var json = JsonSerializer.Serialize(resolved, ContractJson.Options);
            Require(!json.Contains("heightRef", StringComparison.Ordinal) &&
                JsonSerializer.Deserialize<EntranceDecl>(json, ContractJson.Options) == resolved &&
                Parse(raw, new PadTerrain(pad).Terrain) == resolved,
                "Raw parsing or resolved-object round trip applied the head offset twice.");
        }
    }

    [TestCase]
    public void InvalidHeadsAndKnownValuesCollidingWithUnknownSentinelAreRejected()
    {
        foreach (var value in new[] { "null", "\"3.5\"", "true", "1e999", "-1e999" })
            Throws<JsonException>(() => Parse(Add("\"headY\":" + value)));
        Throws<JsonException>(() => Parse(Add("\"headY\":3.5,\"headY\":4")));
        Throws<JsonException>(() => Parse(Add("\"heightRef\":\"pad\",\"headY\":3.5"), new PadTerrain(-3.5f).Terrain));
        Throws<JsonException>(() => Parse(Add("\"heightRef\":\"pad\",\"headY\":3e38"), new PadTerrain(3e38f).Terrain));
    }

    private static string Add(string fields) => Entry.Insert(Entry.Length - 1, "," + fields);
    private static EntranceDecl Parse(string text, ITerrain? terrain = null)
    {
        using var document = JsonDocument.Parse(text);
        return InteriorEntranceJson.Parse(document.RootElement, Feature, terrain);
    }

    /// <summary>A terrain whose only pad is the fixture building's, at <paramref name="value"/>; counts PadY calls.</summary>
    private sealed class PadTerrain
    {
        private readonly float value;
        public PadTerrain(float value)
        {
            this.value = value;
            Terrain = new FormulaTerrain((_, _) => 0f, padY: PadY);
        }
        public FormulaTerrain Terrain { get; }
        public int Calls { get; private set; }
        private float PadY(FeatureId feature) { Calls++; return feature == Feature ? value : throw new KeyNotFoundException(); }
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
