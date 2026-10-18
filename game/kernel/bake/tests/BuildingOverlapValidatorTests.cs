using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Kernel.Bake.Tests;

/// <summary>Area and canonical-source regressions, including the reviewed original campus footprints.</summary>
[TestSuite]
public sealed class BuildingOverlapValidatorTests
{
    [TestCase]
    public void Holes()
    {
        var donut = new PolygonRings(Rect(0, 0, 10, 10).Outer, [Rect(2, 2, 8, 8).Outer]);
        var hole = BuildingOverlapValidator.Measure(donut, Rect(3, 3, 7, 7));
        Near(64, hole.AreaA); Near(0, hole.Intersection); Near(0, hole.IoU);
        var overlap = BuildingOverlapValidator.Measure(donut, Rect(0, 0, 5, 10));
        Near(32, overlap.Intersection); Near(82, overlap.Union);
        var identical = BuildingOverlapValidator.Measure(donut, donut);
        Near(1, identical.IoU); Near(64, identical.Intersection);
    }

    [TestCase]
    public void Contact()
    {
        foreach (var other in new[] { Rect(1, 0, 2, 1), Rect(1, 1, 2, 2), Rect(2, 2, 3, 3) })
            Near(0, BuildingOverlapValidator.Measure(Rect(0, 0, 1, 1), other).Intersection);
        // Intersecting bounding boxes alone cannot turn a shared diagonal into positive area.
        var a = new PolygonRings([new(0, 0), new(2, 0), new(0, 2)]);
        var b = new PolygonRings([new(2, 0), new(2, 2), new(0, 2)]);
        Near(0, BuildingOverlapValidator.Measure(a, b).Intersection);
    }

    [TestCase]
    public void Threshold()
    {
        var a = new PolygonRings([new(0, 0), new(20, 0), new(0, 10)]);
        PolygonRings B(float width) => new([new(0, 0), new(width, 0), new(0, 10)]);
        Require(BuildingOverlapValidator.Measure(a, B(18)).IoU == 0.9, "Exact threshold fixture");
        BuildingOverlapValidator.Validate(World(Feature("a", "a", a), Feature("b", "b", B(18))));
        BuildingOverlapValidator.Validate(World(Feature("a", "a", a), Feature("b", "b", B(17.98f))));
        Contains(Failure(World(Feature("a", "a", a), Feature("b", "b", B(18.02f)))), "IoU=", "> 0.9");
    }

    [TestCase]
    public void MultiPolygon()
    {
        var a = Feature("a", "source-a", Rect(0, 0, 2, 2)) with
        { Parts = [new("source-a", new MultiPolygonGeometry([Rect(0, 0, 2, 2), Rect(10, 0, 12, 2)]), 5, 1)] };
        Contains(Failure(World(a, Feature("b", "source-b", Rect(10, 0, 12, 2)))), "source=\"source-a\"; polygon=1", "1 pair(s)");
    }

    [TestCase]
    public void Shared()
    {
        var p = new PolygonRings(Rect(0, 0, 10, 10).Outer, [Rect(1, 1, 2, 2).Outer, Rect(7, 7, 9, 9).Outer]);
        var reversed = new PolygonRings([new(10, 10), new(10, 0), new(5, 0), new(0, 0), new(0, 10), new(10, 10)],
            p.Holes.Reverse().Select(h => (IReadOnlyList<Vector2>)h.Reverse().ToArray()).ToArray());
        BuildingOverlapValidator.Validate(World(Feature("a", "shared", p), Feature("b", "shared", reversed)));
        var report = Failure(World(Feature("a", "shared", p), Feature("b", "shared", reversed), Feature("c", "different", p)));
        Contains(report, "1 pair(s)", "entity=\"a\"", "entity=\"b\"", "entity=\"c\"");
    }

    [TestCase]
    public void Conflicts()
    {
        Contains(Failure(World(Feature("a", "same", Rect(0, 0, 2, 2)), Feature("b", "same", Rect(0, 0, 3, 2)), Feature("c", "same", Rect(0, 0, 2, 2)))),
            "geometry differs at polygon=0", "source=\"same\"", "entity=\"a\"", "entity=\"b\"", "entity=\"c\"");
        var b = Feature("b", "same", Rect(0, 0, 2, 2)) with
        { Parts = [new("same", new MultiPolygonGeometry([Rect(0, 0, 2, 2), Rect(10, 10, 11, 11)]), 5, 1)] };
        Contains(Failure(World(Feature("a", "same", Rect(0, 0, 2, 2)), b)), "polygon count differs");
        var bowtie = new PolygonRings([new(0, 0), new(2, 2), new(0, 2), new(2, 0)]);
        Contains(Failure(World(Feature("a", "invalid", bowtie))), "Invalid building source geometry", "source=\"invalid\"");
    }

    [TestCase]
    public void Parts()
    {
        using var props = JsonDocument.Parse("{\"kind\":\"building\"}");
        var poi = Feature("poi", "under-poi", Rect(0, 0, 2, 2)) with { Kind = new("poi") };
        poi = poi with { Parts = [poi.Parts[0] with { Props = props.RootElement.Clone() }] };
        Contains(Failure(World(poi, Feature("b", "b", Rect(0, 0, 2, 2)))), "under-poi");
        var hugeEnvelope = Feature("a", "a", Rect(20, 20, 21, 21)) with { Geometry = new PolygonGeometry(Rect(0, 0, 10, 10)) };
        var point = new WorldFeature(new("point"), FeatureSource.Osm, new("building"), new PointGeometry(new(0, 0, 0)));
        var line = point with { Id = new("line"), Geometry = new PolylineGeometry(new([new(0, 0), new(10, 10)])) };
        BuildingOverlapValidator.Validate(World(hugeEnvelope, Feature("b", "b", Rect(0, 0, 10, 10)), point, line));
        var design = new WorldFeature(new("design:a"), FeatureSource.Design, new("building"), new PolygonGeometry(Rect(0, 0, 2, 2)));
        Contains(Failure(World(design, Feature("b", "b", Rect(0, 0, 2, 2)))), "source=\"design:a\"");
    }

    [TestCase]
    public void StableReport()
    {
        var a = Feature("same-entity", "source-a", Rect(0, 0, 2, 2));
        a = a with { Name = "same name", Parts = [a.Parts[0], new("source-b", a.Parts[0].Geometry, 500, 100)] };
        var b = Feature("other-entity", "source-c", Rect(0, 0, 2, 2)) with { Name = a.Name, Height = 1000, Storeys = 200 };
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var left = Failure(World(a, b), (_, _) => new("WP-test"));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var right = Failure(World(b, a with { Parts = a.Parts.Reverse().ToArray() }), (_, _) => new("WP-test"));
            Require(left == right, "Report differs with input order/culture");
            Contains(left, "3 pair(s)", "massing:\"WP-test\"", "facade:\"WP-test\"", "roof:\"WP-test\"", "areaA=4 m2", "union=4 m2");
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
    }

    [TestCase]
    public void RealSix()
    {
        var keep = Raw("osm:relation:10807930");
        var remove = Raw("osm:way:779496166");
        var a = ReadRings(keep.GetProperty("polygons")[0]);
        var b = ReadRings(remove.GetProperty("polygons")[0]);
        Require(a.Holes.Count == 1, "Real C source must retain its hole");
        var m = BuildingOverlapValidator.Measure(a, b);
        // Campus coordinates can evolve independently of this package.
        // Exact areas are covered by the synthetic fixtures above; this real pair locks rejection and source identity.
        Require(m.AreaA > m.AreaB && m.AreaB > 0 && m.Intersection > 0 && m.Intersection <= m.AreaB &&
            m.IoU > BuildingOverlapValidator.MaximumIntersectionOverUnion && m.IoU < 1,
            "The distinct real source footprints must overlap above the rejection threshold.");
        Contains(Failure(FromRaw([keep, remove])), "qsh:relation:14153711", "qsh:way:779496166", "osm:relation:10807930", "osm:way:779496166", "1 pair(s)");
    }

    [TestCase]
    public void RealLiren()
    {
        string[] ids = ["osm:relation:11185936", "osm:way:814133732", "osm:way:814133733", "osm:way:814133734", "osm:way:814133736"];
        var model = FromRaw(ids.Select(Raw));
        var references = model.Features.Values.ToList();
        // Source membership shared by parent and A/B child entities, as allowed by the approved canonical scheme.
        var parent = references.Single(f => f.Id.Value == "qsh:relation:11185938");
        var extras = references.Where(f => f != parent).SelectMany(f => f.Parts).ToArray();
        references[references.IndexOf(parent)] = parent with { Parts = parent.Parts.Concat(extras).ToArray() };
        BuildingOverlapValidator.Validate(World(references.ToArray()), (_, _) => new("WP-16b"));
    }

    [TestCase]
    public void RealApproved()
    {
        string[] removed = ["osm:way:779496166", "osm:relation:11334254", "osm:relation:11665642", "osm:way:691679138"];
        string[] entities = ["qsh:relation:11185936", "qsh:relation:11185938", "qsh:relation:11334255",
            "qsh:relation:11665640", "qsh:relation:11665641", "qsh:relation:14153711",
            "qsh:way:691679140", "qsh:way:779496166", "qsh:way:814133732"];
        var raw = Fixture.GetProperty("features").EnumerateArray()
            .Where(f => entities.Contains(f.GetProperty("entityId").GetString(), StringComparer.Ordinal)).ToArray();
        var report = Failure(FromRaw(raw));
        Contains(report, "4 pair(s)", "osm:relation:11334254", "osm:relation:11665641", "osm:relation:11334255", "osm:relation:11665642", "osm:way:691679138", "osm:way:691679140");
        // This is the intended effective-source projection, not an implementation/test of overlay HideOp.
        BuildingOverlapValidator.Validate(FromRaw(raw.Where(f => !removed.Contains(f.GetProperty("id").GetString()!, StringComparer.Ordinal))));
        Console.WriteLine("REAL_REPORT_BEGIN\n" + report + "\nREAL_REPORT_END");
    }

    JsonElement Raw(string id) => Fixture.GetProperty("features").EnumerateArray().Single(f => f.GetProperty("id").GetString() == id);
    FixtureWorld FromRaw(IEnumerable<JsonElement> features) => World(features.GroupBy(f => f.GetProperty("entityId").GetString()!, StringComparer.Ordinal).Select(group =>
    {
        var parts = group.Select(f =>
        {
            var polygons = f.GetProperty("polygons").EnumerateArray().Select(ReadRings).ToArray();
            OverlayGeometry geometry = polygons.Length == 1 ? new PolygonGeometry(polygons[0]) : new MultiPolygonGeometry(polygons);
            return new WorldFeaturePart(f.GetProperty("id").GetString()!, geometry, null, null) { Props = f.Clone() };
        }).ToArray();
        return new WorldFeature(new(group.Key), FeatureSource.Osm, new("building"), parts[0].Geometry) { Parts = parts };
    }).ToArray());

    static PolygonRings ReadRings(JsonElement polygon)
    {
        var rings = polygon.EnumerateArray().Select(r => r.EnumerateArray().Select(p => new Vector2(p[0].GetSingle(), p[1].GetSingle())).ToArray()).ToArray();
        return new(rings[0], rings.Skip(1).Cast<IReadOnlyList<Vector2>>().ToArray());
    }
    static PolygonRings Rect(float x1, float y1, float x2, float y2) => new([new(x1, y1), new(x2, y1), new(x2, y2), new(x1, y2)]);
    static WorldFeature Feature(string entity, string source, PolygonRings rings) =>
        new(new(entity), FeatureSource.Osm, new("building"), new PolygonGeometry(rings)) { Parts = [new(source, new PolygonGeometry(rings), 5, 1)] };
    static FixtureWorld World(params WorldFeature[] features) => new(features.ToDictionary(f => f.Id));
    static string Failure(IWorldModel world, Func<FeatureId, Aspects, WpId?>? owner = null)
    {
        try { BuildingOverlapValidator.Validate(world, owner); }
        catch (InvalidDataException e) { return e.Message; }
        throw new InvalidOperationException("Expected validation failure.");
    }
    static void Contains(string report, params string[] expected)
    {
        foreach (var text in expected) Require(report.Contains(text, StringComparison.Ordinal), "Missing: " + text + "\n" + report);
    }
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static void Near(double expected, double actual, double tolerance = 1e-12) => Require(Math.Abs(expected - actual) <= tolerance,
        FormattableString.Invariant($"Expected {expected:R}, actual {actual:R}, tolerance {tolerance:R}"));

    sealed class FixtureWorld(IReadOnlyDictionary<FeatureId, WorldFeature> features) : IWorldModel
    {
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => features;
        public PolygonRings Boundary => throw new NotSupportedException();
        public WorldNetwork Network => throw new NotSupportedException();
        public ISpatialIndex Spatial => throw new NotSupportedException();
        public IInteriorIndex Interiors => throw new NotSupportedException();
        public IOutskirtsModel? Outskirts => throw new NotSupportedException();
        public IFidelityModel? Fidelity => throw new NotSupportedException();
    }

    private static readonly Lazy<JsonElement> RawCampus = new(() =>
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(GameRoot(),
            "data", "maps", "qingshuihe", "campus.json")));
        return document.RootElement.Clone();
    });
    private static string GameRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "project.godot"))) return directory.FullName;
            var game = Path.Combine(directory.FullName, "game");
            if (File.Exists(Path.Combine(game, "project.godot"))) return game;
        }
        throw new DirectoryNotFoundException("The campus fixture must run from a repository game build.");
    }
    private static JsonElement Fixture => RawCampus.Value;
}
