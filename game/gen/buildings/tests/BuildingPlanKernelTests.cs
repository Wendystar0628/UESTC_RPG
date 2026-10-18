using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Buildings.Build;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using GdUnit4;

namespace Campus.Gen.Buildings.Tests;

using static Campus.Gen.Buildings.Tests.BuildingsTestSupport;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class BuildingPlanKernelTests
{
    [TestCase]
    public void PartsAreBuiltSeparatelyWithTheirOwnHeights()
    {
        var parts = new[]
        {
            new WorldFeaturePart("osm:way:2", new PolygonGeometry(new PolygonRings(Rect(40, 0, 20, 12))), 7f, 2),
            new WorldFeaturePart("osm:way:1", new PolygonGeometry(new PolygonRings(Rect(0, 0, 30, 12))), 21f, 6),
        };
        var entity = Feature("qsh:way:1", Rect(0, 0, 60, 12), 6, 21f, "teaching") with { Parts = parts };
        var set = BuildingSet.For(World(entity), 1u);
        AssertThat(set.Plans.Count == 2).OverrideFailureMessage("Expected: " + ("one plan per part, none for the aggregate")).IsTrue();
        AssertThat(set.Plans[0].Key == "osm:way:1" && set.Plans[0].Storeys == 6).OverrideFailureMessage("Expected: " + ("first part by source id, six storeys")).IsTrue();
        AssertThat(set.Plans[1].Key == "osm:way:2" && set.Plans[1].Storeys == 2).OverrideFailureMessage("Expected: " + ("second part keeps its own storeys")).IsTrue();
        AssertThat(set.Blocks[1].Base == set.Blocks[0].Count).OverrideFailureMessage("Expected: " + ("parts take consecutive window blocks")).IsTrue();
    }

    [TestCase]
    public void BuildingPartsCountUnderAnyParentKind()
    {
        // WP-08 layout: the entity keeps its own kind (here poi), each part carries its source record with "kind".
        static JsonElement Raw(string kind, string category) =>
            JsonDocument.Parse("{\"kind\":\"" + kind + "\",\"category\":\"" + category + "\"}").RootElement.Clone();
        var parts = new[]
        {
            new WorldFeaturePart("osm:way:10", new PolygonGeometry(new PolygonRings(Rect(0, 0, 30, 14))), 12f, 3) { Props = Raw("building", "teaching") },
            new WorldFeaturePart("osm:way:11", new PolygonGeometry(new PolygonRings(Rect(40, 0, 20, 14))), 8f, 2) { Props = Raw("building", "teaching") },
            new WorldFeaturePart("osm:way:12", new PolygonGeometry(new PolygonRings(Rect(0, 30, 60, 30))), null, null) { Props = Raw("green", "landscape") },
        };
        var park = new WorldFeature(new FeatureId("qsh:way:9"), FeatureSource.Osm, new DesignKind("poi"),
            new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 60))))
        { Parts = parts, Props = Raw("poi", "teaching") };
        var lawn = new WorldFeature(new FeatureId("qsh:way:8"), FeatureSource.Osm, new DesignKind("green"),
            new PolygonGeometry(new PolygonRings(Rect(100, 0, 20, 20))));
        var set = BuildingSet.For(World(park, lawn), 1u);
        AssertThat(set.Plans.Count == 2).OverrideFailureMessage("Expected: " + ("the two building parts of a poi entity, not its green part nor the lawn")).IsTrue();
        AssertThat(set.Plans.All(p => p.Style == Rules.BuildingStyle.Academic)).OverrideFailureMessage("Expected: " + ("style from the part's own category")).IsTrue();
    }

    [TestCase]
    public void BaysAndWindowColumnsComeFromThePlanKernel()
    {
        var world = World(
            Feature("t:dorm", Rect(0, 0, 36, 14), 6, 19.8f, "residence"),
            Feature("t:l", LShape(100, 0), 4, 15.6f, "research"),
            Feature("t:court", Courtyard(0, 100), 3, 12f, "teaching"));
        var set = BuildingSet.For(world, 1u);
        foreach (var plan in set.Plans)
        {
            var bays = plan.Grid.Bays;
            AssertThat(plan.TotalColumns == bays.Count).OverrideFailureMessage("Expected: " + ("one window column per kernel bay")).IsTrue();
            var seen = 0;
            foreach (var f in plan.Facades)
                for (var k = 0; k < f.Bays; k++, seen++)
                {
                    var bay = bays[f.ColumnStart + k];
                    AssertThat(bay.Index == f.ColumnStart + k && bay.Ring == f.Ring && bay.Edge == f.Edge && bay.Bay == k).OverrideFailureMessage("Expected: " + ($"{plan.Key}: column {f.ColumnStart + k} is kernel bay r{bay.Ring}/e{bay.Edge}/b{bay.Bay}")).IsTrue();
                    AssertThat((bay.WindowKind == Campus.Gen.Interior.FacadeWindowKind.Blank) == (f.Kinds[k] == BayKind.Blank)).OverrideFailureMessage("Expected: " + ("blank bays agree")).IsTrue();
                }
            AssertThat(seen == bays.Count).OverrideFailureMessage("Expected: " + ("every kernel bay is laid out once")).IsTrue();
            var again = Campus.Gen.Interior.FacadeGridGenerator.Generate(plan.Grid.Footprint, BuildingPlanner.GridOptions(plan.Profile));
            AssertThat(again.GridHash == plan.Grid.GridHash).OverrideFailureMessage("Expected: " + ("same grid for the interiors from the same footprint and options")).IsTrue();
        }
        var dorm = set.Plans.Single(p => p.Key == "t:dorm");
        AssertThat(dorm.Facades.Where(f => f.Ring == 0).Max(f => f.Bays) == 10).OverrideFailureMessage("Expected: " + ("dormitory 36 m facade: ten 3.6 m bays")).IsTrue();
    }

    /// <summary>
    /// Shared-boundary restoration (WP-08) gives a building part a detailed physical outline with extra collinear vertices and keeps
    /// the legacy outline as its facade outline. The generic building's footprint, bay grid and window block must follow the facade
    /// outline, vertex for vertex, so a part with a facade outline lays out exactly the windows the old data gave it.
    /// </summary>
    [TestCase]
    public void GenericBuildingsLayWindowsOnTheFacadeOutlineOfAPart()
    {
        var world = RealCampus();
        var checkedParts = 0;
        var differing = 0;
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            foreach (var part in feature.Parts.Where(p => p.FacadeGeometry is not null).OrderBy(p => p.SourceId, StringComparer.Ordinal))
            {
                var sources = BuildingPlanner.Sources(feature).Where(s => s.Key == part.SourceId || s.Key.StartsWith(part.SourceId + "#", StringComparison.Ordinal)).ToArray();
                if (sources.Length == 0) continue; // the part is not of the building kind
                var facade = part.FacadeGeometry switch
                {
                    PolygonGeometry p => [p.Rings],
                    MultiPolygonGeometry m => m.Polygons,
                    _ => Array.Empty<PolygonRings>(),
                };
                AssertThat(sources.Length == facade.Count).OverrideFailureMessage("Expected: " + ($"{part.SourceId}: one source per facade polygon")).IsTrue();
                for (var i = 0; i < sources.Length; i++)
                {
                    AssertThat(sources[i].Rings.Outer.SequenceEqual(facade[i].Outer)).OverrideFailureMessage("Expected: " + ($"{part.SourceId}: the footprint's outer ring is the facade ring")).IsTrue();
                    AssertThat(sources[i].Rings.Holes.Count == facade[i].Holes.Count
                        && sources[i].Rings.Holes.Zip(facade[i].Holes).All(h => h.First.SequenceEqual(h.Second))).OverrideFailureMessage("Expected: " + ($"{part.SourceId}: the footprint's holes are the facade holes")).IsTrue();
                    var reference = BuildingPlanner.Plan(sources[i] with { Rings = facade[i] }, world, 4u);
                    var actual = BuildingPlanner.Plan(sources[i], world, 4u);
                    AssertThat((reference is null) == (actual is null)).OverrideFailureMessage("Expected: " + ($"{part.SourceId}: the same footprint is built or skipped")).IsTrue();
                    if (actual is null || reference is null) continue;
                    AssertThat(actual.TotalColumns == reference.TotalColumns && actual.Grid.GridHash == reference.Grid.GridHash).OverrideFailureMessage("Expected: " + ($"{part.SourceId}: the window grid is the one of the facade outline")).IsTrue();
                    checkedParts++;
                }
                var detailed = part.Geometry switch
                {
                    PolygonGeometry p => [p.Rings],
                    MultiPolygonGeometry m => m.Polygons,
                    _ => Array.Empty<PolygonRings>(),
                };
                if (detailed.Count != facade.Count || detailed.Zip(facade).Any(t => !t.First.Outer.SequenceEqual(t.Second.Outer))) differing++;
            }
        AssertThat(checkedParts >= 20).OverrideFailureMessage("Expected: " + ($"enough building parts with a facade outline are covered ({checkedParts})")).IsTrue();
        AssertThat(differing > 0).OverrideFailureMessage("Expected: " + ("the detailed and the facade outlines do differ, so the guard has teeth")).IsTrue();
    }
}
