using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Systems.Atlas;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class AtlasBaseSourceTests
{
    private static readonly MapId TestMap = new("atlas-test");
    private static readonly MapRect Page = new(new(-32), new(32));
    private static readonly AtlasRegion[] RegionTable =
    [new("z-region", new("测试区", "Test Region"), null), new("a-place", new("测试地点", "Test Place"), "z-region")];

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void FourSdfReadinessDoesNotInventRegionsFontMetricsOrGlyphs()
    {
        var product = Product(regionsReady: false, labelsReady: false);
        var source = new AtlasBaseSource();
        source.Reset(TestMap);
        source.Publish(TestMap, Page, RegionTable, product);
        var snapshot = source.Snapshot!;
        AssertThat(source.Ready && !snapshot.RegionsReady && !snapshot.LabelsReady && !snapshot.GlyphsReady).OverrideFailureMessage("Four SDF textures incorrectly marked unproduced content ready.").IsTrue();
        AssertThat(snapshot.SdfA.Rect == product.Textures[0].Rect && snapshot.SdfOut.Rect == product.Textures[2].Rect
            && snapshot.SdfRing.Rect == Page).OverrideFailureMessage("Per-texture rectangles were replaced by one page rectangle.").IsTrue();
        AssertThat(snapshot.PlaceToRegion.Span[0] == byte.MaxValue && snapshot.PlaceToRegion.Span[255] == byte.MaxValue).OverrideFailureMessage("Unknown/unused place index was mapped to the first region.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ProjectionCopiesBytesOnceAndPreservesActualPartialLabelBands()
    {
        var product = Product(regionsReady: true, labelsReady: true);
        var source = new AtlasBaseSource();
        source.Reset(TestMap);
        source.Publish(TestMap, Page, RegionTable, product);
        var snapshot = source.Snapshot!;
        var original = snapshot.SdfA.Rgba8.Span[0];
        product.Textures[0].Rgba8[0] = 99;
        product.Regions.Ids[1] = "changed-after-publication";
        AssertThat(snapshot.SdfA.Rgba8.Span[0] == original && snapshot.PlaceIds[1] == "a-place").OverrideFailureMessage("Snapshot borrowed mutable producer bytes or IDs.").IsTrue();
        AssertThat(ReferenceEquals(snapshot, source.Snapshot) && snapshot.SdfA.Rgba8.Equals(source.Snapshot!.SdfA.Rgba8)).OverrideFailureMessage("Reading Snapshot copied data instead of returning the published view.").IsTrue();
        AssertThat(snapshot.RegionsReady && snapshot.PlaceToRegion.Span[1] == 0 && snapshot.PlaceToRegion.Span[2] == 0).OverrideFailureMessage("Known places did not map to the stable zero-based region bit.").IsTrue();
        AssertThat(snapshot.LabelsReady && snapshot.LabelBands.Count == 1 && snapshot.LabelBands[0].Language == "zh").OverrideFailureMessage("Actual measured label bands were replaced with fabricated language/zoom coverage.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MissingBoundaryEvidenceOverridesNonzeroGridAndResetDropsSnapshot()
    {
        var product = Product(regionsReady: true, labelsReady: false);
        product = product with
        {
            Manifest = product.Manifest with
            { Warnings = ["region-boundaries-missing-region-query-returns-unknown"] }
        };
        var source = new AtlasBaseSource();
        source.Reset(TestMap);
        source.Publish(TestMap, Page, RegionTable, product);
        AssertThat(!source.Snapshot!.RegionsReady).OverrideFailureMessage("A nonzero grid concealed missing boundary evidence.").IsTrue();
        var revision = source.Rev;
        source.Reset(default);
        AssertThat(!source.Ready && source.Snapshot is null && source.Rev > revision).OverrideFailureMessage("Unload retained the previous map snapshot.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void WrongMapAndMissingRequiredPlaneAreRejected()
    {
        var source = new AtlasBaseSource();
        source.Reset(TestMap);
        var product = Product(regionsReady: true, labelsReady: true);
        AssertThrown(() => source.Publish(new("another-map"), Page, RegionTable, product)).IsInstanceOf<InvalidDataException>();
        AssertThrown(() => source.Publish(TestMap, Page, RegionTable, product with { Textures = product.Textures.Take(3).ToArray() })).IsInstanceOf<InvalidDataException>();
        AssertThat(!source.Ready && source.Snapshot is null).OverrideFailureMessage("Incomplete product became drawable.").IsTrue();
    }

    private static MapBakeProduct Product(bool regionsReady, bool labelsReady)
    {
        var campus = new MapRect(new(-8), new(8));
        var data = new MapRect(new(-16), new(16));
        MapTexture[] textures =
        [new("map_sdf_a", 1, 1, campus, [128, 128, 128, 128]), new("map_sdf_b", 1, 1, campus, [0, 0, 128, 128]),
            new("map_sdf_out", 1, 1, data, [255, 255, 255, 128]), new("map_sdf_ring", 1, 1, Page, [255, 255, 255, 128])];
        string[] ids = ["", "a-place", "z-region"];
        var cells = new byte[16];
        if (regionsReady) cells[0] = 1;
        var grid = new MapRegionGrid(data, 4, 4, 8, ids, cells);
        var manifest = new MapManifest(1, TestMap.Value, "world-hash", "code-hash", "labels-hash", data, Page, 1,
            [], new("regions.u8", data, 4, 4, 8, ids, "grid-hash"),
            regionsReady ? [] : ["region-boundaries-missing-region-query-returns-unknown"]);
        LabelBand[] bands = labelsReady
            ? [new("zh", 0, .4f, [new("label-test", "测试", Vector2.Zero, new(2, 3), new(40, 18))])]
            : [];
        return new(manifest, textures, grid, new(1, "labels-hash", labelsReady ? "measured-font-hash" : "", labelsReady, bands));
    }

}
