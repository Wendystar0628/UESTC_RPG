using Campus.Gen.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using System.Collections.Concurrent;

namespace Campus.Gen.Vegetation.Tests;

/// <summary>Shared planting worlds, rules, immutable layouts and pavement census for vegetation suites.</summary>
internal static class VegetationTestSupport
{
    internal static readonly Lazy<(SpeciesTable Table, PaletteCatalog Palette)> ModelData = new(Load);

    internal static PaletteCatalog Palette => ModelData.Value.Palette;

    private static (SpeciesTable, PaletteCatalog) Load()
    {
        var root = RepositoryFixture.Root;
        var table = SpeciesTable.Parse(File.ReadAllBytes(Path.Combine(root, "game", "data", "vegetation", "species.json")));
        return (table, Campus.Contracts.Tests.PaletteFixture.Catalog);
    }

    private static readonly ConcurrentDictionary<(string Species, int Variant, VegLevel Level), MotherModel> Mothers = new();

    /// <summary>A mother model built once per species, variant and level; the tests only read it.</summary>
    internal static MotherModel Mother(SpeciesInfo species, int variant, VegLevel level) =>
        Mothers.GetOrAdd((species.Id, variant, level), key => MotherModels.Build(species, key.Variant, key.Level, Palette));

    private static readonly Lazy<(SpeciesTable Species, PlantingRules Rules)> Rules = new(() =>
    {
        var species = SpeciesTable.Parse(File.ReadAllBytes(Path.Combine(RepositoryFixture.Root, "game", "data", "vegetation", "species.json")));
        return (species, PlantingRules.Parse(RealMapFixture.ReadFiles()[VegetationInputs.PlantingKey].Span, species));
    });

    private static readonly Lazy<IWorldModel> Authored = new(() => RealMapFixture.Load().World);
    internal static readonly Lazy<PlantingLayout> WorldLayout = new(() => Plant(Authored.Value));

    /// <summary>The exclusive fixture without extra records, and its layout: the baseline most fixture cases compare with.</summary>
    internal static readonly Lazy<IWorldModel> FixtureWorld = new(() => Fixture());
    internal static readonly Lazy<PlantingLayout> FixtureLayout = new(() => PlantFixture(FixtureWorld.Value));

    // Compare every stored field of the planted instances.
    internal static bool SameRecords(IEnumerable<PlantedTree> a, IEnumerable<PlantedTree> b) => a.SequenceEqual(b);

    /// <summary>The trees of the four original rule sources, before pit and design planting.</summary>
    internal static List<PlantedTree> Original(IEnumerable<PlantedTree> trees) => trees.Where(t => t.Source <= PlantSource.Grove).ToList();

    internal static PlantingLayout Plant(IWorldModel world) => Planting.Build(world, Rules.Value.Species, Rules.Value.Rules, 1u);

    internal static (SpeciesTable Species, PlantingRules Rules) Data => Rules.Value;

    internal static PlantingRules FixtureRules { get; } = new(
        [new("fixture-avenue", ["ginkgo"], [new(7f, false)], 6f, false, new(["cherry", "plum"], 13f, 7f))],
        15f, "camphor", new([("camphor", 1f)], [], []))
    {
        Pits = new("koelreuteria", "camphor"),
        Understorey = new(new("loropetalum", 0.42f, 1f),
            new(1.3f, 1.2f, 8f, 1f, 0.5f, 8f, ["boxhedge"], [("photinia", 1f)]))
    };

    internal static PlantingLayout PlantFixture(IWorldModel world) => Planting.Build(world, Data.Species, FixtureRules, 1u);

    /// <summary>Exclusive final-world records and rules, independent of campus content.</summary>
    internal static IWorldModel Fixture(params string[] records)
    {
        const string baseline = """
            {"id":"fixture:green","kind":"green","geometry":{"type":"polygon","rings":{"outer":[[20,-80],[80,-80],[80,80],[20,80]],"holes":[]}}},
            {"id":"fixture:plaza","kind":"road","geometry":{"type":"polygon","rings":{"outer":[[-40,40],[-3,40],[-3,50],[-40,50]],"holes":[]}},"props":{"highway":"pedestrian"}},
            {"id":"fixture:source:way:1","kind":"road","name":"fixture-avenue","geometry":{"type":"polyline","line":{"points":[[0,-80],[0,80]]}},"props":{"name":"fixture-avenue","highway":"residential","width":4}},
            {"id":"fixture:source:way:2","kind":"road","geometry":{"type":"polyline","line":{"points":[[83,-80],[83,80]]}},"props":{"highway":"residential","width":2}},
            {"id":"fixture:source:way:3","kind":"road","geometry":{"type":"polyline","line":{"points":[[20,83],[80,83]]}},"props":{"highway":"residential","width":2}}
            """;
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["world/geo.json"] = """
                {"origin":[103,30],"crs":"EPSG:32648","bounds":[-100,-100,100,100],"entityPrefix":"fixture",
                 "boundary":{"outer":[[-100,-100],[100,-100],[100,100],[-100,100]],"holes":[]}}
                """,
            ["world/fixture.json"] = "{\"features\":[" + baseline + (records.Length == 0 ? "" : "," + string.Join(',', records)) +
                "],\"trees\":" + """
                [{"id":"fixture:tree-a","position":[-30,0,-20]},{"id":"fixture:tree-b","position":[-30,0,0]},{"id":"fixture:tree-on-plaza","position":[-30,0,45]}]
                """ + "}"
        };
        return WorldModelLoader.Load(new("qingshuihe"), files, new() { Terrain = FlatTerrain.Default }).World;
    }

    internal static IWorldModel World() => Authored.Value;

    internal static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>
    /// The hard pavement and tree pits of a world, read again from its features and network the way game/gen/ground/ paves them: the
    /// pedestrian, footway, path, steps and cycleway areas and lines of its features (parts of kind road, and design roads) and of
    /// its network.
    /// </summary>
    internal sealed class Census
    {
        private static readonly string[] Paved = ["pedestrian", "footway", "path", "steps", "cycleway"];

        public List<Pave> Paving { get; } = [];
        public List<Pit> Pits { get; } = [];

        public sealed record Pave(string Id, PolygonRings? Rings, IReadOnlyList<Vector2>? Line, float Half);
        public sealed record Pit(PolygonRings? Rings, Vector2 Centre, float Radius);

        public static Census Of(IWorldModel world)
        {
            var census = new Census();
            foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            {
                foreach (var (props, geometry) in RoadSources(feature))
                {
                    var type = props?.OptionalText("highway");
                    if (type is null || !Paved.Contains(type)) continue;
                    switch (geometry)
                    {
                        case PolygonGeometry polygon: census.Paving.Add(new(feature.Id.Value, polygon.Rings, null, 0f)); break;
                        case MultiPolygonGeometry multi: census.Paving.AddRange(multi.Polygons.Select(rings => new Pave(feature.Id.Value, rings, null, 0f))); break;
                        case PolylineGeometry line:
                            var width = props?.OptionalNumber("width") is { } w and > 0f ? w : type switch { "path" => 2f, "cycleway" => 3f, "pedestrian" => 5f, _ => 2.5f };
                            census.Paving.Add(new(feature.Id.Value, null, line.Line.Points, width * 0.5f));
                            break;
                    }
                }
                if (feature.Kind.Value != Planting.TreePitKind) continue;
                switch (feature.Geometry)
                {
                    case PolygonGeometry polygon: census.Pits.Add(new(polygon.Rings, default, 0f)); break;
                    case MultiPolygonGeometry multi: census.Pits.AddRange(multi.Polygons.Select(rings => new Pit(rings, default, 0f))); break;
                    case PointGeometry point:
                        var radius = feature.Props?.OptionalNumber("radius") is { } r and > 0f ? r : feature.Props?.OptionalNumber("size") is { } size and > 0f ? size * 0.5f : 0.9f;
                        census.Pits.Add(new(null, new(point.Position.X, point.Position.Z), radius));
                        break;
                }
            }
            foreach (var road in world.Network.Roads)
            {
                var type = road.Props?.OptionalText("highway");
                if (type is not null && Paved.Contains(type)) census.Paving.Add(new(road.Id.Value, null, road.Line.Points, MathF.Max(road.Width, 2f) * 0.5f));
            }
            return census;
        }

        private static IEnumerable<(JsonElement? Props, WorldGeometry Geometry)> RoadSources(WorldFeature feature)
        {
            if (feature.Parts.Count == 0)
            {
                if (feature.Kind.Value == "road") yield return (feature.Props, feature.Geometry);
                yield break;
            }
            foreach (var part in feature.Parts)
            {
                var kind = part.Props is { ValueKind: JsonValueKind.Object } own && own.TryGetProperty("kind", out var k) && k.ValueKind == JsonValueKind.String
                    ? k.GetString() : feature.Kind.Value;
                if (kind == "road") yield return (part.Props ?? feature.Props, part.Geometry);
            }
        }

        /// <summary>True when <paramref name="p"/> is on the pavement, or within <paramref name="clearance"/> of it.</summary>
        public bool Within(Pave pave, Vector2 p, float clearance)
        {
            if (pave.Rings is { } rings)
                return PlanRings.Contains(rings, p) ||
                    rings.Holes.Prepend(rings.Outer).Any(ring => PlanRings.DistanceToRing(p, ring) < clearance);
            var r = pave.Half + clearance;
            for (var i = 1; i < pave.Line!.Count; i++)
                if (PlanLines.SegmentDistanceSquared(p, pave.Line[i - 1], pave.Line[i]) < r * r) return true;
            return false;
        }

        public bool InPit(Vector2 p) => Pits.Any(pit => pit.Rings is { } rings ? PlanRings.Contains(rings, p) : Vector2.Distance(p, pit.Centre) <= pit.Radius);

        /// <summary>A trunk here gives way: on pavement or within the trunk clearance of it, and in no pit.</summary>
        public bool GivesWay(Vector2 p) => !InPit(p) && Paving.Any(pave => Within(pave, p, Planting.TrunkClearance));

    }
}
