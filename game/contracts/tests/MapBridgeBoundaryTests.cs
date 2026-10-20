using Campus.Tests;
using System.Numerics;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class MapBridgeBoundaryTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
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
    [TestCategory(TestReason.Contract)]
    public void AtlasRejectsMalformedBuffersAndReadinessClaimsWhileSupportingAll64MaskBits()
    {
        AssertThrown(() => new AtlasBaseTexture("map_sdf_a", 2, 2, Rect(), new byte[15])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new AtlasLabelBand("zh", 0, 1,
            [new("bad", "Text", new(float.NaN, 0), Vector2.Zero, Vector2.One)])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Snapshot(new byte[16], labelsReady: true)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => Snapshot(new byte[16], places: ["", "place"], regions: ["region"], regionsReady: true)).IsInstanceOf<ArgumentException>();
        var regions = Enumerable.Range(0, 64).Select(i => "region-" + i.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var lookup = Enumerable.Repeat(byte.MaxValue, 256).ToArray(); lookup[1] = 63;
        var snapshot = Snapshot(new byte[16], places: ["", "place"], regions: regions, lookup: lookup, regionsReady: true);
        if ((1UL << snapshot.PlaceToRegion.Span[1]) != (1UL << 63)) throw new InvalidOperationException("The 64th region mask bit was lost.");
        lookup[1] = 64;
        AssertThrown(() => Snapshot(new byte[16], places: ["", "place"], regions: regions, lookup: lookup)).IsInstanceOf<ArgumentException>();
    }

    private static AtlasBaseSnapshot Snapshot(byte[] pixels, IReadOnlyList<AtlasLabelBand>? bands = null,
        bool labelsReady = false, IReadOnlyList<string>? places = null, IReadOnlyList<string>? regions = null,
        byte[]? lookup = null, bool regionsReady = false) => new(new("fixture"), Rect(),
            new("map_sdf_a", 2, 2, Rect(), pixels), new("map_sdf_b", 2, 2, Rect(), pixels),
            new("map_sdf_out", 2, 2, Rect(), pixels), new("map_sdf_ring", 2, 2, Rect(), pixels),
            bands, labelsReady, placeIds: places, topRegionIds: regions,
            placeToRegion: lookup is null ? default : new ReadOnlyMemory<byte>(lookup), regionsReady: regionsReady);

    private static MapRect Rect() => new(Vector2.Zero, new(64, 64));
}
