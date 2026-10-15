using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using GdUnit4;

namespace Campus.Pipeline.Map.Tests;

[TestSuite]
public sealed class MapBakeTests
{
    [TestCase]
    public void VectorDistancePreservesHolesAndBoundaryEncoding()
    {
        var polygon = new PolygonRings(Square(-8, -8, 8, 8).Outer, [Square(-2, -2, 2, 2).Outer]);
        Equal(-2d, MapDistance.Polygon(polygon, 4, 0));
        Equal(2d, MapDistance.Polygon(polygon, 0, 0));
        Equal(3d, MapDistance.Polygon(polygon, 11, 0));
        Equal((byte)0, MapDistance.Encode(-8)); Equal((byte)128, MapDistance.Encode(0)); Equal((byte)255, MapDistance.Encode(8));
        Equal((byte)144, MapDistance.Encode(1));
    }

    [TestCase]
    public void MultiplePolygonsAndNarrowRoadSurviveNorthUpRasterization()
    {
        var multi = new WorldFeature(new("fixture:building"), FeatureSource.Design, new("building"),
            new MultiPolygonGeometry([Square(-12, -12, -8, -8), Square(8, 8, 12, 12)]));
        var road = new NetworkLine(new("fixture:road"), new([new(-16, 0), new(16, 0)]), 0.1f);
        var world = new TestWorld([multi], new([road], [], [], [], []));
        var product = MapBaker.Build(Input(world));
        var a = product.Textures[0];
        Check(Pixel(a, 5, 5, 3) < 128 && Pixel(a, 26, 26, 3) < 128, "A multipolygon island was lost.");
        Equal((byte)255, Pixel(a, 26, 5, 3));
        Check(Pixel(a, 16, 16, 2) is > 128 and < 144, "Subtexel road distance was eroded.");
        Equal("z-increasing", product.Manifest.RowOrder);
    }

    [TestCase]
    public void MixedPoiUsesOnlyBuildingPartsAndLeavesCampusGroundUnfilled()
    {
        var grounds = new PolygonGeometry(Square(-14, -14, 14, 14));
        var poi = new WorldFeature(new("fixture:campus"), FeatureSource.Osm, new("poi"), grounds)
        {
            Parts =
            [
                new("source:grounds", grounds, null, null) { Props = JsonSerializer.SerializeToElement(new { kind = "poi" }) },
                new("source:building-a", new MultiPolygonGeometry([Square(-12, -12, -8, -8), Square(8, 8, 12, 12)]), 15.6f, 4)
                    { Props = JsonSerializer.SerializeToElement(new { kind = "building" }) },
                new("source:building-b", new PolygonGeometry(Square(8, -12, 12, -8)), 58.5f, 15)
                    { Props = JsonSerializer.SerializeToElement(new { kind = "building" }) }
            ]
        };
        var product = MapBaker.Build(Input(new TestWorld([poi])));
        var a = product.Textures[0];
        Check(Pixel(a, 5, 5, 3) < 128 && Pixel(a, 26, 26, 3) < 128 && Pixel(a, 26, 5, 3) < 128,
            "A building part of the POI was omitted.");
        Equal((byte)255, Pixel(a, 16, 16, 3));
        Equal((byte)MapFeatureClass.None, Pixel(product.Textures[1], 8, 8, 1));
        Equal((byte)MapFeatureClass.Building, Pixel(product.Textures[1], 2, 2, 1));
        var reordered = poi with { Parts = poi.Parts.Reverse().ToArray() };
        Check(MapBakeCodec.Serialize(product).SequenceEqual(MapBakeCodec.Serialize(MapBaker.Build(Input(new TestWorld([reordered]))))),
            "Source part insertion order changed map bytes.");
    }

    [TestCase]
    public void TextureRectsKeepCampusDetailSeparateFromDataAndPageCoverage()
    {
        var input = Input(new TestWorld([]));
        var product = MapBaker.Build(input with
        {
            AtlasRect = new(new(-128, -128), new(128, 128)),
            Options = input.Options with { CampusMarginMetres = 64 }
        });
        Equal(new MapRect(new(-80, -80), new(80, 80)), product.Textures[0].Rect);
        Equal(product.Textures[0].Rect, product.Textures[1].Rect);
        Equal(input.Map.DataRect, product.Textures[2].Rect);
        Equal(new MapRect(new(-128, -128), new(128, 128)), product.Textures[3].Rect);
        Equal(input.Map.DataRect, product.Regions.Rect);
        MapBakeCodec.Validate(product);
    }

    [TestCase]
    public void MissingRegionalPolygonsReturnUnknownAndKnownRegionsClipAtTheCampus()
    {
        var input = Input(new TestWorld([]));
        var unknown = MapBaker.Build(input);
        Check(unknown.Regions.Cells.All(v => v == 0), "Missing regional boundaries invented a region.");
        Equal<string?>(null, unknown.Regions.RegionAt(0, 0));
        var known = MapBaker.Build(input with { RegionAreas = [new("district", [Square(-16, -16, 16, 16)])] });
        Equal("district", known.Regions.RegionAt(0, 0));
        Equal<string?>(null, known.Regions.RegionAt(16, 0));
        Equal<string?>(null, known.Regions.RegionAt(float.NaN, 0));
    }

    [TestCase]
    public void OutputIsIndependentOfFeatureDictionaryInsertionOrder()
    {
        var one = new WorldFeature(new("fixture:one"), FeatureSource.Design, new("water"), new PolygonGeometry(Square(-8, -8, 0, 0)));
        var two = new WorldFeature(new("fixture:two"), FeatureSource.Design, new("green"), new PolygonGeometry(Square(0, 0, 8, 8)));
        var a = MapBakeCodec.Serialize(MapBaker.Build(Input(new TestWorld([one, two]))));
        var b = MapBakeCodec.Serialize(MapBaker.Build(Input(new TestWorld([two, one]))));
        Check(a.SequenceEqual(b), "Dictionary insertion order changed bytes.");
        Check(a.SequenceEqual(MapBakeCodec.Serialize(MapBakeCodec.Deserialize(a))), "Map codec changed a valid product.");
    }

    [TestCase]
    public void CodecRejectsTruncationOversizedCountsAndCorruptTextureBytes()
    {
        var product = MapBaker.Build(Input(new TestWorld([])));
        var bytes = MapBakeCodec.Serialize(product);
        Throws<InvalidDataException>(() => MapBakeCodec.Deserialize(bytes.AsSpan(0, bytes.Length - 1)));
        var badCount = (byte[])bytes.Clone(); badCount[8] = 200;
        Throws<InvalidDataException>(() => MapBakeCodec.Deserialize(badCount));
        var files = MapBakeCodec.Files(product).ToDictionary(p => p.Key, p => (byte[])p.Value.Clone(), StringComparer.Ordinal);
        files["map_sdf_a.rgba8"][0] ^= 1;
        Throws<InvalidDataException>(() => MapBakeCodec.FromFiles(files));
    }

    [TestCase]
    public void LabelPlacementNeedsRealMeasurementsAndProducesBothLanguages()
    {
        var source = Labels();
        var pending = LabelLayouter.Build(source, "label-input", null);
        Check(!pending.Ready && pending.Bands.Count == 0, "Unmeasured font sizes were invented.");
        List<LabelMeasurement> measurements = [];
        foreach (string language in new[] { "zh", "en" })
            for (byte band = 0; band < 5; band++) measurements.Add(new("district-label", language, band, "District", new(60, 20)));
        var layout = new LabelLayoutInput(measurements, [], [0.4f, 0.6f, 0.9f, 1.4f, 2.1f], "font-metrics");
        var baked = LabelLayouter.Build(source, "label-input", layout);
        Check(baked.Ready && baked.Bands.Count == 10 && baked.Bands.All(b => b.Labels.Count == 1), "Language/zoom layout incomplete.");
        Throws<ArgumentException>(() => LabelLayouter.Build(source, "label-input", layout with { Measurements = [] }));
    }

    private static MapBakeInput Input(IWorldModel world)
    {
        var rect = new MapRect(new(-16, -16), new(16, 16));
        var map = new MapDescriptor(new("fixture"), new("Fixture", "Fixture"), MapKind.Outdoor, 1, 1,
            null!, Vector2.Zero, default, rect, null!, default, default, "", [], null!, null!, new Dictionary<string, string>(), []);
        return new(world, map, rect, Labels(), "world-hash", "code-hash", "label-hash")
        {
            Options = new() { MainSize = 32, ClassSize = 16, OutskirtsSize = 16, RingSize = 16, CampusMarginMetres = 0 }
        };
    }

    private static AtlasLabels Labels() => new(1, [new AtlasRegion("district", new("District", "District"), null)], [],
        [new("district-label", "region", 100, Ref: "district", At: Vector2.Zero)]);
    private static PolygonRings Square(float x0, float z0, float x1, float z1) => new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);
    private static byte Pixel(MapTexture texture, int x, int z, int c) => texture.Rgba8[(z * texture.Width + x) * 4 + c];
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}."); }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private sealed class TestWorld : IWorldModel
    {
        internal TestWorld(IReadOnlyList<WorldFeature> features, WorldNetwork? network = null)
        { Features = features.ToDictionary(f => f.Id); Network = network ?? WorldNetwork.Empty; }
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; }
        public WorldNetwork Network { get; }
        public PolygonRings Boundary => Square(-16, -16, 16, 16);
        public ISpatialIndex Spatial => throw new NotSupportedException();
        public IInteriorIndex Interiors => throw new NotSupportedException();
        public IOutskirtsModel? Outskirts => null;
        public IFidelityModel? Fidelity => null;
    }
}
