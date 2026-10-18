using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Systems.Atlas;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class AtlasBaseSourceTests
{
    private static readonly MapId TestMap = new("atlas-test");
    private static readonly MapRect Page = new(new(-32), new(32));
    private static readonly AtlasRegion[] RegionTable =
    [new("z-region", new("测试区", "Test Region"), null), new("a-place", new("测试地点", "Test Place"), "z-region")];

    [TestCase]
    public void FourSdfReadinessDoesNotInventRegionsFontMetricsOrGlyphs()
    {
        var product = Product(regionsReady: false, labelsReady: false);
        var source = new AtlasBaseSource();
        source.Reset(TestMap);
        source.Publish(TestMap, Page, RegionTable, product);
        var snapshot = source.Snapshot!;
        Check(source.Ready && !snapshot.RegionsReady && !snapshot.LabelsReady && !snapshot.GlyphsReady,
            "Four SDF textures incorrectly marked unproduced content ready.");
        Check(snapshot.SdfA.Rect == product.Textures[0].Rect && snapshot.SdfOut.Rect == product.Textures[2].Rect
            && snapshot.SdfRing.Rect == Page, "Per-texture rectangles were replaced by one page rectangle.");
        Check(snapshot.PlaceToRegion.Span[0] == byte.MaxValue && snapshot.PlaceToRegion.Span[255] == byte.MaxValue,
            "Unknown/unused place index was mapped to the first region.");
    }

    [TestCase]
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
        Check(snapshot.SdfA.Rgba8.Span[0] == original && snapshot.PlaceIds[1] == "a-place",
            "Snapshot borrowed mutable producer bytes or IDs.");
        Check(ReferenceEquals(snapshot, source.Snapshot) && snapshot.SdfA.Rgba8.Equals(source.Snapshot!.SdfA.Rgba8),
            "Reading Snapshot copied data instead of returning the published view.");
        Check(snapshot.RegionsReady && snapshot.PlaceToRegion.Span[1] == 0 && snapshot.PlaceToRegion.Span[2] == 0,
            "Known places did not map to the stable zero-based region bit.");
        Check(snapshot.LabelsReady && snapshot.LabelBands.Count == 1 && snapshot.LabelBands[0].Language == "zh",
            "Actual measured label bands were replaced with fabricated language/zoom coverage.");
    }

    [TestCase]
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
        Check(!source.Snapshot!.RegionsReady, "A nonzero grid concealed missing boundary evidence.");
        var revision = source.Rev;
        source.Reset(default);
        Check(!source.Ready && source.Snapshot is null && source.Rev > revision, "Unload retained the previous map snapshot.");
    }

    [TestCase]
    public void WrongMapAndMissingRequiredPlaneAreRejected()
    {
        var source = new AtlasBaseSource();
        source.Reset(TestMap);
        var product = Product(regionsReady: true, labelsReady: true);
        ExpectInvalid(() => source.Publish(new("another-map"), Page, RegionTable, product));
        ExpectInvalid(() => source.Publish(TestMap, Page, RegionTable, product with { Textures = product.Textures.Take(3).ToArray() }));
        Check(!source.Ready && source.Snapshot is null, "Incomplete product became drawable.");
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

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void ExpectInvalid(Action action)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("Invalid map product was accepted.");
    }
}
