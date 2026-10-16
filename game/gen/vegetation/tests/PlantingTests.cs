using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Vegetation.Tests;

[TestSuite]
public sealed class PlantingTests
{
    private static readonly Lazy<(IWorldModel World, SpeciesTable Species, PlantingRules Rules)> Data = new(Load);

    private static (IWorldModel, SpeciesTable, PlantingRules) Load()
    {
        var root = FindRepository();
        var species = SpeciesTable.Parse(File.ReadAllBytes(Path.Combine(root, "game", "data", "vegetation", "species.json")));
        var rules = PlantingRules.Parse(File.ReadAllBytes(Path.Combine(root, "game", "data", "maps", "qingshuihe", "vegetation", "planting.json")), species);
        var data = Path.Combine(root, "game", "data", "maps", "qingshuihe");
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return (WorldModelLoader.Load(new("qingshuihe"), files).World, species, rules);
    }

    [TestCase]
    public void TheCampusIsPlantedAndTheAvenueHasFourRows()
    {
        var (world, species, rules) = Data.Value;
        var a = Planting.Build(world, species, rules, 1u);
        var b = Planting.Build(world, species, rules, 1u);
        if (!a.Trees.Select(t => (t.Key, t.Position, t.Variant, t.Species.Id)).SequenceEqual(b.Trees.Select(t => (t.Key, t.Position, t.Variant, t.Species.Id))))
            throw new InvalidOperationException("two builds differ");
        // WLD-VEGETATION §3: four rows, about 1600 ginkgos (rows × length / spacing; cross streets, crossing paths and
        // buildings take some out); a spring band of cherry and flowering plum outside them.
        var avenue = a.Trees.Where(t => t.Source == PlantSource.Road && t.Species.Id == "ginkgo").ToList();
        if (avenue.Count is < 1100 or > 1800) throw new InvalidOperationException($"{avenue.Count} avenue ginkgos");
        if (avenue.Select(t => t.Key.Split(':')[3] + t.Key.Split(':')[4]).Distinct().Count() != 4)
            throw new InvalidOperationException("the avenue does not have four rows");
        var band = a.Trees.Where(t => t.Source == PlantSource.Band).ToList();
        if (band.Count < 100 || band.Any(t => t.Species.Id is not ("cherry" or "plum"))) throw new InvalidOperationException($"band of {band.Count}");
        if (a.Count(PlantSource.Grove) < 600) throw new InvalidOperationException($"only {a.Count(PlantSource.Grove)} grove trees");
        if (a.Count(PlantSource.OsmTree) < 20) throw new InvalidOperationException($"only {a.Count(PlantSource.OsmTree)} OSM trees");
        if (a.Trees.Select(t => t.Key).Distinct().Count() != a.Trees.Count) throw new InvalidOperationException("duplicate keys");
        if (a.Trees.Any(t => t.Custom.X is < 0f or >= 1f || t.Custom.Z is < 0f or > 1f || !(t.Custom.W > 0f)))
            throw new InvalidOperationException("INSTANCE_CUSTOM out of range");
        Console.WriteLine($"planting: {a.Trees.Count} trees; avenue {avenue.Count}, band {band.Count}, OSM {a.Count(PlantSource.OsmTree)}, groves {a.Count(PlantSource.Grove)}; "
            + string.Join(", ", a.Trees.GroupBy(t => t.Species.Id).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}")));
    }

    [TestCase]
    public void GinkgosStayWhereTheyAreRegistered()
    {
        // WLD-VEGETATION §2: ginkgo only on the avenue (with its OSM street trees), beside the main building and at the
        // student centre; never spread elsewhere.
        var (world, species, rules) = Data.Value;
        var layout = Planting.Build(world, species, rules, 1u);
        var allowed = new[] { "主楼", "学生活动中心" }.Select(n => world.Features.Values.Where(f => f.Name == n).ToList()).ToList();
        foreach (var t in layout.Trees.Where(t => t.Species.Id == "ginkgo" && t.Source == PlantSource.Grove))
        {
            var near = allowed.Any(group => group.Any(f => Distance(f.Bounds, t.Position) <= 75f));
            if (!near) throw new InvalidOperationException($"grove ginkgo {t.Key} at ({t.Position.X:F0}, {t.Position.Z:F0})");
        }
    }

    [TestCase]
    public void TrunksKeepOffBuildingsWaterAndRoads()
    {
        var (world, species, rules) = Data.Value;
        var layout = Planting.Build(world, species, rules, 1u);
        var blocking = world.Features.Values.SelectMany(f => new[] { "building", "water", "sport", "parking" }
            .SelectMany(kind => Parts(f, kind))).SelectMany(p => p.Geometry switch
            {
                PolygonGeometry polygon => new[] { polygon.Rings },
                MultiPolygonGeometry multi => multi.Polygons.ToArray(),
                _ => []
            }).ToList();
        foreach (var t in layout.Trees)
        {
            var p = new Vector2(t.Position.X, t.Position.Z);
            if (blocking.Any(r => PlanGeometry.Contains(r, p))) throw new InvalidOperationException($"{t.Key} stands in a building, water, court or car park");
            foreach (var road in world.Network.Roads)
                for (var i = 1; i < road.Line.Points.Count; i++)
                    if (PlanGeometry.SegmentDistanceSquared(p, road.Line.Points[i - 1], road.Line.Points[i]) < road.Width * road.Width * 0.25f)
                        throw new InvalidOperationException($"{t.Key} stands on road {road.Id.Value}");
            if (!PlanGeometry.Contains(world.Boundary, p)) throw new InvalidOperationException($"{t.Key} outside the campus");
        }
    }

    [TestCase]
    public void TreesLandOnTheTerrainAndAvoidSteepGround()
    {
        // WLD-TERRAIN §4.4: y = HeightAt(centre); no tree where the grade exceeds 25%. A tilted test terrain with one steep band.
        var (world, species, rules) = Data.Value;
        var terrain = new TiltedTerrain();
        var layout = Planting.Build(new TerrainWorld(world, terrain), species, rules, 1u);
        var flat = Planting.Build(world, species, rules, 1u);
        foreach (var t in layout.Trees)
        {
            if (MathF.Abs(t.Position.Y - terrain.HeightAt(t.Position.X, t.Position.Z)) > 1e-4f) throw new InvalidOperationException($"{t.Key} floats");
            if (terrain.FactsAt(t.Position.X, t.Position.Z).Grade > Planting.MaxGrade) throw new InvalidOperationException($"{t.Key} on a steep slope");
        }
        if (flat.Trees.Count(t => TiltedTerrain.Steep(t.Position.X)) == 0) throw new InvalidOperationException("the steep band had no trees to remove");
        if (layout.Trees.Count >= flat.Trees.Count) throw new InvalidOperationException("the steep band removed nothing");
    }

    [TestCase]
    public void TrunkCollidersFollowTheTrees()
    {
        var (world, species, rules) = Data.Value;
        var layout = Planting.Build(world, species, rules, 1u);
        var tree = layout.Trees.First(t => t.Source == PlantSource.Road);
        var tile = new TileKey((int)MathF.Floor(tree.Position.X / 64f), (int)MathF.Floor(tree.Position.Z / 64f));
        var colliders = TreeColliders.For(layout, tile, world.Terrain).Cast<PostCollider>().ToList();
        var inTile = layout.Trees.Count(t => (int)MathF.Floor(t.Position.X / 64f) == tile.X && (int)MathF.Floor(t.Position.Z / 64f) == tile.Z);
        if (colliders.Count != inTile || colliders.Count == 0) throw new InvalidOperationException($"{colliders.Count} posts for {inTile} trees");
        foreach (var c in colliders)
        {
            if (c.Foot != FootMat.Bark || c.Tag != SurfaceTag.Barrier) throw new InvalidOperationException("trunk material");
            if (!(c.R > 0.05f && c.R < 1f && c.Y1 > c.Y0 + 0.5f) || c.C.X is < 0f or >= 64f || c.C.Y is < 0f or >= 64f)
                throw new InvalidOperationException($"post {c.C} r {c.R} y {c.Y0}–{c.Y1}");
        }
    }

    [TestCase]
    public void LeavesFallUnderTheCrownsAndCarpetTheAvenue()
    {
        var (world, species, rules) = Data.Value;
        var layout = Planting.Build(world, species, rules, 1u);
        var rect = new MapRect(new(-1280f, -1280f), new(1280f, 1280f));
        const float step = 1f;
        var w = (int)(2560f / step);
        var m1 = new byte[w * w * 4];
        var m2 = new byte[w * w * 4];
        CanopyProjection.Paint(layout, rules, world, rect, step, m1, m2);
        byte At(byte[] m, int channel, float x, float z) => m[(((int)((z + 1280f) / step)) * w + (int)((x + 1280f) / step)) * 4 + channel];
        var ginkgo = layout.Trees.First(t => t.Source == PlantSource.Road);
        if (At(m1, 0, ginkgo.Position.X, ginkgo.Position.Z) < 200) throw new InvalidOperationException("no ginkgo leaves under an avenue tree");
        if (At(m2, 2, ginkgo.Position.X, ginkgo.Position.Z) < 200) throw new InvalidOperationException("no canopy shelter under an avenue tree");
        var camphor = layout.Trees.First(t => t.Species.Id == "camphor" && layout.Trees.All(o => o == t || Vector3.Distance(o.Position, t.Position) > o.CrownRadius * 1.2f + 2f));
        if (At(m1, 1, camphor.Position.X, camphor.Position.Z) != 0) throw new InvalidOperationException("an evergreen dropped leaves");
        // The avenue carriageway: carpeted, with two lighter tracks (§6).
        var road = world.Network.Roads.First(l => l.Props?.TryGetProperty("name:zh", out var n) == true && n.GetString() == rules.Roads[0].Name && l.Line.Points.Count > 4);
        var a = road.Line.Points[2];
        var b = road.Line.Points[3];
        var mid = (a + b) * 0.5f;
        var normal = Vector2.Normalize(new Vector2(-(b - a).Y, (b - a).X));
        if (At(m1, 0, mid.X, mid.Y) != 255 || At(m1, 0, mid.X + normal.X * 2f, mid.Y + normal.Y * 2f) < CanopyProjection.TrackPotential)
            throw new InvalidOperationException("avenue not carpeted");
        if (!m1.Where((_, i) => i % 4 == 0).Any(v => v == CanopyProjection.TrackPotential)) throw new InvalidOperationException("no worn tracks on the avenue");
        if (At(m1, 1, mid.X, mid.Y) < CanopyProjection.AvenuePetals) throw new InvalidOperationException("no petal floor on the avenue");
    }

    private static IEnumerable<WorldFeaturePart> Parts(WorldFeature feature, string kind) =>
        feature.Parts.Count == 0
            ? (feature.Kind.Value == kind && feature.Geometry is not (PointGeometry or PolylineGeometry) ? [new("", feature.Geometry, null, null)] : [])
            : feature.Parts.Where(part => (part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var k) ? k.GetString() : feature.Kind.Value) == kind);

    private static float Distance(Box3 box, Vector3 p)
    {
        var dx = MathF.Max(0f, MathF.Max(box.Min.X - p.X, p.X - box.Max.X));
        var dz = MathF.Max(0f, MathF.Max(box.Min.Z - p.Z, p.Z - box.Max.Z));
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>A gentle 4% tilt along x, with a 40% band between x = −100 and 0 m.</summary>
    private sealed class TiltedTerrain : ITerrain
    {
        public static bool Steep(float x) => x is > -100f and < 0f;
        public bool IsFlat(TileKey tile) => false;
        public float HeightAt(float x, float z) => 0.04f * x + 0.36f * Math.Clamp(x + 100f, 0f, 100f);
        public void FillTile(TileKey tile, int level, Span<short> cm) => throw new NotSupportedException();
        public float PadY(FeatureId feature) => 0f;
        public float? WaterY(FeatureId water) => null;
        public TerrainFacts FactsAt(float x, float z) => new(Steep(x) ? 0.4f : 0.04f, Steep(x) ? TerrainClass.Slope : TerrainClass.Flat, 0f, 0f);
    }

    private sealed class TerrainWorld(IWorldModel inner, ITerrain terrain) : IWorldModel
    {
        public ITerrain Terrain => terrain;
        public PolygonRings Boundary => inner.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => inner.Features;
        public WorldNetwork Network => inner.Network;
        public ISpatialIndex Spatial => inner.Spatial;
        public IInteriorIndex Interiors => inner.Interiors;
        public IOutskirtsModel? Outskirts => inner.Outskirts;
        public IFidelityModel? Fidelity => inner.Fidelity;
    }

    private static string FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ownership.json"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("repository root not found");
    }
}
