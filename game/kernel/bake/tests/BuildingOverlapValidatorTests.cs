using Campus.Tests;
using Campus.Gen.Tests.Support;
using static GdUnit4.Assertions;
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
    [TestCategory(TestReason.Contract)]
    public void Holes()
    {
        var donut = new PolygonRings(Rect(0, 0, 10, 10).Outer, [Rect(2, 2, 8, 8).Outer]);
        var hole = BuildingOverlapValidator.Measure(donut, Rect(3, 3, 7, 7));
        { var __nearExpected = 64; var __nearActual = hole.AreaA; var __nearTolerance = TestTolerance.FloatRelative * Math.Max(1, Math.Abs(__nearExpected)); AssertThat(Math.Abs(__nearExpected - __nearActual) <= __nearTolerance).OverrideFailureMessage(FormattableString.Invariant($"Expected {__nearExpected:R}, actual {__nearActual:R}, tolerance {__nearTolerance:R}")).IsTrue(); }; { var __nearExpected = 0; var __nearActual = hole.Intersection; var __nearTolerance = TestTolerance.FloatRelative * Math.Max(1, Math.Abs(__nearExpected)); AssertThat(Math.Abs(__nearExpected - __nearActual) <= __nearTolerance).OverrideFailureMessage(FormattableString.Invariant($"Expected {__nearExpected:R}, actual {__nearActual:R}, tolerance {__nearTolerance:R}")).IsTrue(); }; { var __nearExpected = 0; var __nearActual = hole.IoU; var __nearTolerance = TestTolerance.FloatRelative * Math.Max(1, Math.Abs(__nearExpected)); AssertThat(Math.Abs(__nearExpected - __nearActual) <= __nearTolerance).OverrideFailureMessage(FormattableString.Invariant($"Expected {__nearExpected:R}, actual {__nearActual:R}, tolerance {__nearTolerance:R}")).IsTrue(); };
        var overlap = BuildingOverlapValidator.Measure(donut, Rect(0, 0, 5, 10));
        { var __nearExpected = 32; var __nearActual = overlap.Intersection; var __nearTolerance = TestTolerance.FloatRelative * Math.Max(1, Math.Abs(__nearExpected)); AssertThat(Math.Abs(__nearExpected - __nearActual) <= __nearTolerance).OverrideFailureMessage(FormattableString.Invariant($"Expected {__nearExpected:R}, actual {__nearActual:R}, tolerance {__nearTolerance:R}")).IsTrue(); }; { var __nearExpected = 82; var __nearActual = overlap.Union; var __nearTolerance = TestTolerance.FloatRelative * Math.Max(1, Math.Abs(__nearExpected)); AssertThat(Math.Abs(__nearExpected - __nearActual) <= __nearTolerance).OverrideFailureMessage(FormattableString.Invariant($"Expected {__nearExpected:R}, actual {__nearActual:R}, tolerance {__nearTolerance:R}")).IsTrue(); };
        var identical = BuildingOverlapValidator.Measure(donut, donut);
        { var __nearExpected = 1; var __nearActual = identical.IoU; var __nearTolerance = TestTolerance.FloatRelative * Math.Max(1, Math.Abs(__nearExpected)); AssertThat(Math.Abs(__nearExpected - __nearActual) <= __nearTolerance).OverrideFailureMessage(FormattableString.Invariant($"Expected {__nearExpected:R}, actual {__nearActual:R}, tolerance {__nearTolerance:R}")).IsTrue(); }; { var __nearExpected = 64; var __nearActual = identical.Intersection; var __nearTolerance = TestTolerance.FloatRelative * Math.Max(1, Math.Abs(__nearExpected)); AssertThat(Math.Abs(__nearExpected - __nearActual) <= __nearTolerance).OverrideFailureMessage(FormattableString.Invariant($"Expected {__nearExpected:R}, actual {__nearActual:R}, tolerance {__nearTolerance:R}")).IsTrue(); };
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void Contact()
    {
        foreach (var other in new[] { Rect(1, 0, 2, 1), Rect(1, 1, 2, 2), Rect(2, 2, 3, 3) })
            { var __nearExpected = 0; var __nearActual = BuildingOverlapValidator.Measure(Rect(0, 0, 1, 1), other).Intersection; var __nearTolerance = TestTolerance.FloatRelative * Math.Max(1, Math.Abs(__nearExpected)); AssertThat(Math.Abs(__nearExpected - __nearActual) <= __nearTolerance).OverrideFailureMessage(FormattableString.Invariant($"Expected {__nearExpected:R}, actual {__nearActual:R}, tolerance {__nearTolerance:R}")).IsTrue(); };
        // Intersecting bounding boxes alone cannot turn a shared diagonal into positive area.
        var a = new PolygonRings([new(0, 0), new(2, 0), new(0, 2)]);
        var b = new PolygonRings([new(2, 0), new(2, 2), new(0, 2)]);
        { var __nearExpected = 0; var __nearActual = BuildingOverlapValidator.Measure(a, b).Intersection; var __nearTolerance = TestTolerance.FloatRelative * Math.Max(1, Math.Abs(__nearExpected)); AssertThat(Math.Abs(__nearExpected - __nearActual) <= __nearTolerance).OverrideFailureMessage(FormattableString.Invariant($"Expected {__nearExpected:R}, actual {__nearActual:R}, tolerance {__nearTolerance:R}")).IsTrue(); };
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void Threshold()
    {
        var a = new PolygonRings([new(0, 0), new(20, 0), new(0, 10)]);
        PolygonRings B(float width) => new([new(0, 0), new(width, 0), new(0, 10)]);
        AssertThat(BuildingOverlapValidator.Measure(a, B(18)).IoU == 0.9).OverrideFailureMessage("Exact threshold fixture").IsTrue();
        BuildingOverlapValidator.Validate(World(Feature("a", "a", a), Feature("b", "b", B(18))));
        BuildingOverlapValidator.Validate(World(Feature("a", "a", a), Feature("b", "b", B(17.98f))));
        Reject(World(Feature("a", "a", a), Feature("b", "b", B(18.02f))));
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MultiPolygon()
    {
        var a = Feature("a", "source-a", Rect(0, 0, 2, 2)) with
        { Parts = [new("source-a", new MultiPolygonGeometry([Rect(0, 0, 2, 2), Rect(10, 0, 12, 2)]), 5, 1)] };
        Reject(World(a, Feature("b", "source-b", Rect(10, 0, 12, 2))));
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void Shared()
    {
        var p = new PolygonRings(Rect(0, 0, 10, 10).Outer, [Rect(1, 1, 2, 2).Outer, Rect(7, 7, 9, 9).Outer]);
        var reversed = new PolygonRings([new(10, 10), new(10, 0), new(5, 0), new(0, 0), new(0, 10), new(10, 10)],
            p.Holes.Reverse().Select(h => (IReadOnlyList<Vector2>)h.Reverse().ToArray()).ToArray());
        BuildingOverlapValidator.Validate(World(Feature("a", "shared", p), Feature("b", "shared", reversed)));
        Reject(World(Feature("a", "shared", p), Feature("b", "shared", reversed), Feature("c", "different", p)));
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void Conflicts()
    {
        Reject(World(Feature("a", "same", Rect(0, 0, 2, 2)), Feature("b", "same", Rect(0, 0, 3, 2)), Feature("c", "same", Rect(0, 0, 2, 2))));
        var b = Feature("b", "same", Rect(0, 0, 2, 2)) with
        { Parts = [new("same", new MultiPolygonGeometry([Rect(0, 0, 2, 2), Rect(10, 10, 11, 11)]), 5, 1)] };
        Reject(World(Feature("a", "same", Rect(0, 0, 2, 2)), b));
        var bowtie = new PolygonRings([new(0, 0), new(2, 2), new(0, 2), new(2, 0)]);
        Reject(World(Feature("a", "invalid", bowtie)));
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void Parts()
    {
        using var props = JsonDocument.Parse("{\"kind\":\"building\"}");
        var poi = Feature("poi", "under-poi", Rect(0, 0, 2, 2)) with { Kind = new("poi") };
        poi = poi with { Parts = [poi.Parts[0] with { Props = props.RootElement.Clone() }] };
        Reject(World(poi, Feature("b", "b", Rect(0, 0, 2, 2))));
        var hugeEnvelope = Feature("a", "a", Rect(20, 20, 21, 21)) with { Geometry = new PolygonGeometry(Rect(0, 0, 10, 10)) };
        var point = new WorldFeature(new("point"), FeatureSource.Osm, new("building"), new PointGeometry(new(0, 0, 0)));
        var line = point with { Id = new("line"), Geometry = new PolylineGeometry(new([new(0, 0), new(10, 10)])) };
        BuildingOverlapValidator.Validate(World(hugeEnvelope, Feature("b", "b", Rect(0, 0, 10, 10)), point, line));
        var design = new WorldFeature(new("design:a"), FeatureSource.Design, new("building"), new PolygonGeometry(Rect(0, 0, 2, 2)));
        Reject(World(design, Feature("b", "b", Rect(0, 0, 2, 2))));
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RejectionDoesNotDependOnCultureOrInputOrder()
    {
        var a = Feature("same-entity", "source-a", Rect(0, 0, 2, 2));
        a = a with { Name = "same name", Parts = [a.Parts[0], new("source-b", a.Parts[0].Geometry, 500, 100)] };
        var b = Feature("other-entity", "source-c", Rect(0, 0, 2, 2)) with { Name = a.Name, Height = 1000, Storeys = 200 };
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Reject(World(a, b), (_, _) => new("WP-test"));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Reject(World(b, a with { Parts = a.Parts.Reverse().ToArray() }), (_, _) => new("WP-test"));
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RealSix()
    {
        var hidden = WestOps.Single(op => op.GetProperty("op").GetString() == "hide")
            .GetProperty("target").GetString()!;
        var parentId = Entity(hidden).GetProperty("parentId").GetString()!;
        var keep = Fixture.GetProperty("features").EnumerateArray().Single(feature =>
            feature.GetProperty("entityId").GetString() == parentId &&
            feature.GetProperty("polygons").EnumerateArray().Any(polygon => polygon.GetArrayLength() > 1));
        var remove = Fixture.GetProperty("features").EnumerateArray().Single(feature =>
            feature.GetProperty("entityId").GetString() == hidden);
        var a = ReadRings(keep.GetProperty("polygons")[0]);
        var b = ReadRings(remove.GetProperty("polygons")[0]);
        AssertThat(a.Holes.Count == 1).OverrideFailureMessage("Real C source must retain its hole").IsTrue();
        var m = BuildingOverlapValidator.Measure(a, b);
        // Campus coordinates can evolve independently of this package.
        // Exact areas are covered by the synthetic fixtures above; this real pair checks rejection.
        AssertThat(m.AreaA > m.AreaB && m.AreaB > 0 && m.Intersection > 0 && m.Intersection <= m.AreaB &&
            m.IoU > BuildingOverlapValidator.MaximumIntersectionOverUnion && m.IoU < 1).OverrideFailureMessage("The distinct real source footprints must overlap above the rejection threshold.").IsTrue();
        Reject(FromRaw([keep, remove]));
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RealLiren()
    {
        var record = NamedEntity("立人楼");
        var ids = record.GetProperty("featureIds").EnumerateArray()
            .Select(id => id.GetString()!).ToHashSet(StringComparer.Ordinal);
        var model = FromRaw(Fixture.GetProperty("features").EnumerateArray()
            .Where(feature => ids.Contains(feature.GetProperty("id").GetString()!)));
        var references = model.Features.Values.ToList();
        // Source membership shared by parent and A/B child entities, as allowed by the approved canonical scheme.
        var parent = references.Single(f => f.Id.Value == record.GetProperty("id").GetString());
        var extras = references.Where(f => f != parent).SelectMany(f => f.Parts).ToArray();
        references[references.IndexOf(parent)] = parent with { Parts = parent.Parts.Concat(extras).ToArray() };
        BuildingOverlapValidator.Validate(World(references.ToArray()), (_, _) => new("WP-16b"));
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RealApproved()
    {
        var hidden = WestOps.Single(op => op.GetProperty("op").GetString() == "hide")
            .GetProperty("target").GetString()!;
        var removed = new HashSet<string>(WestOps.Concat(SouthOps)
            .Where(op => op.GetProperty("op").GetString() == "hidePart")
            .Select(op => op.GetProperty("sourceId").GetString()!), StringComparer.Ordinal);
        foreach (var feature in Fixture.GetProperty("features").EnumerateArray()
            .Where(feature => feature.GetProperty("entityId").GetString() == hidden))
            removed.Add(feature.GetProperty("id").GetString()!);

        var liren = NamedEntity("立人楼").GetProperty("id").GetString()!;
        var research = WestOps.First(op => op.GetProperty("op").GetString() == "hidePart")
            .GetProperty("target").GetString()!;
        var south = SouthOps.Single(op => op.GetProperty("op").GetString() == "hidePart")
            .GetProperty("target").GetString()!;
        var six = Entity(hidden).GetProperty("parentId").GetString()!;
        var entities = Fixture.GetProperty("entities").EnumerateArray()
            .Where(entity => entity.GetProperty("parentId").GetString() == liren ||
                (entity.GetProperty("parentId").GetString() == research &&
                    entity.GetProperty("name").GetString() is ("A区" or "B区")) ||
                new[] { liren, research, six, south, hidden }.Contains(entity.GetProperty("id").GetString()))
            .Select(entity => entity.GetProperty("id").GetString()!).ToHashSet(StringComparer.Ordinal);
        var raw = Fixture.GetProperty("features").EnumerateArray()
            .Where(f => entities.Contains(f.GetProperty("entityId").GetString()!)).ToArray();
        Reject(FromRaw(raw));

        // This is the intended effective-source projection, not an implementation/test of overlay HideOp.
        BuildingOverlapValidator.Validate(FromRaw(raw.Where(f => !removed.Contains(f.GetProperty("id").GetString()!))));
    }

    JsonElement Entity(string id) => Fixture.GetProperty("entities").EnumerateArray()
        .Single(entity => entity.GetProperty("id").GetString() == id);
    JsonElement NamedEntity(string name) => Fixture.GetProperty("entities").EnumerateArray()
        .Single(entity => entity.GetProperty("name").GetString() == name);
    FakeWorld FromRaw(IEnumerable<JsonElement> features) => World(features.GroupBy(f => f.GetProperty("entityId").GetString()!, StringComparer.Ordinal).Select(group =>
    {
        var parts = group.Select(f =>
        {
            var polygons = f.GetProperty("polygons").EnumerateArray().Select(ReadRings).ToArray();
            OverlayGeometry geometry = polygons.Length == 1 ? new PolygonGeometry(polygons[0]) : new MultiPolygonGeometry(polygons);
            return new WorldFeaturePart(f.GetProperty("id").GetString()!, geometry, null, null) { Props = f.Clone() };
        }).ToArray();
        return WorldFeatureFixture.Create(group.Key, "building", parts[0].Geometry, source: FeatureSource.Osm, parts: parts);
    }).ToArray());

    static PolygonRings ReadRings(JsonElement polygon)
    {
        var rings = polygon.EnumerateArray().Select(r => r.EnumerateArray().Select(p => new Vector2(p[0].GetSingle(), p[1].GetSingle())).ToArray()).ToArray();
        return new(rings[0], rings.Skip(1).Cast<IReadOnlyList<Vector2>>().ToArray());
    }
    static PolygonRings Rect(float x1, float y1, float x2, float y2) => new([new(x1, y1), new(x2, y1), new(x2, y2), new(x1, y2)]);
    static WorldFeature Feature(string entity, string source, PolygonRings rings) =>
        WorldFeatureFixture.Create(entity, "building", new PolygonGeometry(rings), source: FeatureSource.Osm,
            parts: [new(source, new PolygonGeometry(rings), 5, 1)]);
    static FakeWorld World(params WorldFeature[] features) => new() { Features = features.ToDictionary(f => f.Id) };

    static void Reject(IWorldModel world, Func<FeatureId, Aspects, WpId?>? owner = null) =>
        AssertThrown(() => BuildingOverlapValidator.Validate(world, owner)).IsInstanceOf<InvalidDataException>();

    private static readonly Lazy<JsonElement> RawCampus = new(() =>
    {
        using var document = JsonDocument.Parse(RealMapFixture.ReadFiles()["campus.json"]);
        return document.RootElement.Clone();
    });

    private static JsonElement Fixture => RawCampus.Value;
    private static readonly Lazy<JsonElement> WestOverlay = new(() => Overlay("west.d", "0000-wp-19-base.json"));
    private static readonly Lazy<JsonElement> SouthOverlay = new(() => Overlay("south.d", "0000-wp-18-base.json"));
    private static IEnumerable<JsonElement> WestOps => WestOverlay.Value.GetProperty("ops").EnumerateArray();
    private static IEnumerable<JsonElement> SouthOps => SouthOverlay.Value.GetProperty("ops").EnumerateArray();
    private static JsonElement Overlay(string zone, string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            RealMapFixture.DataDirectory(), "overlay", zone, name)));
        return document.RootElement.Clone();
    }
}
