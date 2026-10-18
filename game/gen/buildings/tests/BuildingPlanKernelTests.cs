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
        Require(set.Plans.Count == 2, "one plan per part, none for the aggregate");
        Require(set.Plans[0].Key == "osm:way:1" && set.Plans[0].Storeys == 6, "first part by source id, six storeys");
        Require(set.Plans[1].Key == "osm:way:2" && set.Plans[1].Storeys == 2, "second part keeps its own storeys");
        Require(set.Blocks[1].Base == set.Blocks[0].Count, "parts take consecutive window blocks");
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
        Require(set.Plans.Count == 2, "the two building parts of a poi entity, not its green part nor the lawn");
        Require(set.Plans.All(p => p.Style == Rules.BuildingStyle.Academic), "style from the part's own category");
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
            Require(plan.TotalColumns == bays.Count, "one window column per kernel bay");
            var seen = 0;
            foreach (var f in plan.Facades)
                for (var k = 0; k < f.Bays; k++, seen++)
                {
                    var bay = bays[f.ColumnStart + k];
                    Require(bay.Index == f.ColumnStart + k && bay.Ring == f.Ring && bay.Edge == f.Edge && bay.Bay == k,
                        $"{plan.Key}: column {f.ColumnStart + k} is kernel bay r{bay.Ring}/e{bay.Edge}/b{bay.Bay}");
                    Require((bay.WindowKind == Campus.Gen.Interior.FacadeWindowKind.Blank) == (f.Kinds[k] == BayKind.Blank), "blank bays agree");
                }
            Require(seen == bays.Count, "every kernel bay is laid out once");
            var again = Campus.Gen.Interior.FacadeGridGenerator.Generate(plan.Grid.Footprint, BuildingPlanner.GridOptions(plan.Profile));
            Require(again.GridHash == plan.Grid.GridHash, "same grid for the interiors from the same footprint and options");
        }
        var dorm = set.Plans.Single(p => p.Key == "t:dorm");
        Require(dorm.Facades.Where(f => f.Ring == 0).Max(f => f.Bays) == 10, "dormitory 36 m facade: ten 3.6 m bays");
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
                Require(sources.Length == facade.Count, $"{part.SourceId}: one source per facade polygon");
                for (var i = 0; i < sources.Length; i++)
                {
                    Require(sources[i].Rings.Outer.SequenceEqual(facade[i].Outer), $"{part.SourceId}: the footprint's outer ring is the facade ring");
                    Require(sources[i].Rings.Holes.Count == facade[i].Holes.Count
                        && sources[i].Rings.Holes.Zip(facade[i].Holes).All(h => h.First.SequenceEqual(h.Second)), $"{part.SourceId}: the footprint's holes are the facade holes");
                    var reference = BuildingPlanner.Plan(sources[i] with { Rings = facade[i] }, world, 4u);
                    var actual = BuildingPlanner.Plan(sources[i], world, 4u);
                    Require((reference is null) == (actual is null), $"{part.SourceId}: the same footprint is built or skipped");
                    if (actual is null || reference is null) continue;
                    Require(actual.TotalColumns == reference.TotalColumns && actual.Grid.GridHash == reference.Grid.GridHash,
                        $"{part.SourceId}: the window grid is the one of the facade outline");
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
        Require(checkedParts >= 20, $"enough building parts with a facade outline are covered ({checkedParts})");
        Require(differing > 0, "the detailed and the facade outlines do differ, so the guard has teeth");
    }
}
