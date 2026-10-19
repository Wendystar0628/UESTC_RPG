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
        var point = new WorldFeature(new("point"), new("building"), new PointGeometry(new(0, 0, 0)));
        var line = point with { Id = new("line"), Geometry = new PolylineGeometry(new([new(0, 0), new(10, 10)])) };
        BuildingOverlapValidator.Validate(World(hugeEnvelope, Feature("b", "b", Rect(0, 0, 10, 10)), point, line));
        var design = new WorldFeature(new("design:a"), new("building"), new PolygonGeometry(Rect(0, 0, 2, 2)));
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

    [TestCase, TestCategory(TestReason.Contract)]
    public void RealApprovedWorldHasNoDuplicateBuildingFootprints() =>
        BuildingOverlapValidator.Validate(RealMapFixture.Load().World);

    static PolygonRings Rect(float x1, float y1, float x2, float y2) => new([new(x1, y1), new(x2, y1), new(x2, y2), new(x1, y2)]);
    static WorldFeature Feature(string entity, string source, PolygonRings rings) =>
        WorldFeatureFixture.Create(entity, "building", new PolygonGeometry(rings),
            parts: [new(source, new PolygonGeometry(rings), 5, 1)]);
    static FakeWorld World(params WorldFeature[] features) => new() { Features = features.ToDictionary(f => f.Id) };

    static void Reject(IWorldModel world, Func<FeatureId, Aspects, WpId?>? owner = null) =>
        AssertThrown(() => BuildingOverlapValidator.Validate(world, owner)).IsInstanceOf<InvalidDataException>();

}
