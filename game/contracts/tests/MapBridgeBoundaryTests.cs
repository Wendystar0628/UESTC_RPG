using System.Numerics;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class MapBridgeBoundaryTests
{
    [TestCase]
    public void ExplicitLoadInputsSnapshotOwnership()
    {
        var feature = new FeatureId("fixture");
        var claims = new Dictionary<FeatureId, WpId> { [feature] = new("WP-01") };
        var inputs = new WorldModelLoadInputs(Map(), ClaimedBy: claims);
        claims[feature] = new("WP-02");
        if (inputs.ClaimedBy![feature] != new WpId("WP-01")) throw new InvalidOperationException("Claim input was not snapshotted.");
        if (inputs.ClaimedBy is IDictionary<FeatureId, WpId> exposed)
            Expect<NotSupportedException>(() => exposed[feature] = new("WP-02"));
    }

    [TestCase]
    public void AtlasPublicationOwnsInputsAndKeepsOptionalReadinessIndependent()
    {
        var pixels = new byte[16]; pixels[0] = 128;
        var labels = new List<AtlasPlacedLabel> { new("label", "Measured", Vector2.One, Vector2.Zero, new(20, 10)) };
        var bands = new List<AtlasLabelBand> { new("zh", 0, 2, labels) };
        var places = new[] { "", "place" };
        var regions = new[] { "region" };
        var lookup = Enumerable.Repeat(byte.MaxValue, 256).ToArray(); lookup[1] = 0;
        var snapshot = Snapshot(pixels, bands, true, places, regions, lookup, true);
        pixels[0] = 0; labels.Clear(); bands.Clear(); places[1] = "changed"; regions[0] = "changed"; lookup[1] = byte.MaxValue;
        if (snapshot.SdfA.Rgba8.Span[0] != 128 || snapshot.LabelBands[0].Labels.Count != 1 ||
            snapshot.PlaceIds[1] != "place" || snapshot.TopRegionIds[0] != "region" || snapshot.PlaceToRegion.Span[1] != 0)
            throw new InvalidOperationException("Published atlas data still aliases mutable producer inputs.");
        if (!snapshot.LabelsReady || snapshot.GlyphsReady || snapshot.Glyphs is not null || !snapshot.RegionsReady)
            throw new InvalidOperationException("Four SDFs or one measured band falsely implied a glyph atlas/full language set.");
        if (snapshot.PlaceToRegion.Span[0] != byte.MaxValue || snapshot.PlaceToRegion.Span[255] != byte.MaxValue)
            throw new InvalidOperationException("Unknown places must remain distinct from valid region bit zero.");
    }

    [TestCase]
    public void AtlasRejectsMalformedBuffersAndReadinessClaimsWhileSupportingAll64MaskBits()
    {
        Expect<ArgumentException>(() => new AtlasBaseTexture("map_sdf_a", 2, 2, Rect(), new byte[15]));
        Expect<ArgumentException>(() => new AtlasLabelBand("zh", 0, 1,
            [new("bad", "Text", new(float.NaN, 0), Vector2.Zero, Vector2.One)]));
        Expect<ArgumentException>(() => Snapshot(new byte[16], labelsReady: true));
        Expect<ArgumentException>(() => Snapshot(new byte[16], places: ["", "place"], regions: ["region"], regionsReady: true));
        var regions = Enumerable.Range(0, 64).Select(i => "region-" + i.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var lookup = Enumerable.Repeat(byte.MaxValue, 256).ToArray(); lookup[1] = 63;
        var snapshot = Snapshot(new byte[16], places: ["", "place"], regions: regions, lookup: lookup, regionsReady: true);
        if ((1UL << snapshot.PlaceToRegion.Span[1]) != (1UL << 63)) throw new InvalidOperationException("The 64th region mask bit was lost.");
        lookup[1] = 64;
        Expect<ArgumentException>(() => Snapshot(new byte[16], places: ["", "place"], regions: regions, lookup: lookup));
    }

    private static AtlasBaseSnapshot Snapshot(byte[] pixels, IReadOnlyList<AtlasLabelBand>? bands = null,
        bool labelsReady = false, IReadOnlyList<string>? places = null, IReadOnlyList<string>? regions = null,
        byte[]? lookup = null, bool regionsReady = false) => new(new("fixture"), Rect(),
            new("map_sdf_a", 2, 2, Rect(), pixels), new("map_sdf_b", 2, 2, Rect(), pixels),
            new("map_sdf_out", 2, 2, Rect(), pixels), new("map_sdf_ring", 2, 2, Rect(), pixels),
            bands, labelsReady, placeIds: places, topRegionIds: regions,
            placeToRegion: lookup is null ? default : new ReadOnlyMemory<byte>(lookup), regionsReady: regionsReady);

    private static MapRect Rect() => new(Vector2.Zero, new(64, 64));
    private static MapDescriptor Map() => new(new("fixture"), new("Fixture", "Fixture"), MapKind.Outdoor, 1, 1,
        new(0, 0, 32648, Vector2.Zero, new(64, 64)), new(32, 32), new(Vector3.Zero, new(64, 64, 64)),
        Rect(), new(1, 0, 64, 0, 64, 1, 1, 1), new(0, 0), new(0, 0), "start", [],
        new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("Default", "Default"), false, "fixture"),
        new("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);

    private static void Expect<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
