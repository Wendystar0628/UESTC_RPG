using Campus.Gen.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Vegetation.Tests;

[TestSuite]
public sealed class PlantingTests
{
    private static readonly Lazy<(IWorldModel World, SpeciesTable Species, PlantingRules Rules)> Data = new(Load);

    private static (IWorldModel, SpeciesTable, PlantingRules) Load()
    {
        var root = RepositoryFixture.Root;
        var species = SpeciesTable.Parse(File.ReadAllBytes(Path.Combine(root, "game", "data", "vegetation", "species.json")));
        var rules = PlantingRules.Parse(RealMapFixture.ReadFiles()[VegetationInputs.PlantingKey].Span, species);
        return (PavementTests.World(), species, rules);
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TheCampusIsPlantedAndTheAvenueHasOneRowEachSide()
    {
        var (world, species, rules) = Data.Value;
        var a = PavementTests.WorldLayout.Value;
        var b = Planting.Build(world, species, rules, 1u);
        if (!a.Trees.Select(t => (t.Key, t.Position, t.Variant, t.Species.Id)).SequenceEqual(b.Trees.Select(t => (t.Key, t.Position, t.Variant, t.Species.Id))))
            throw new InvalidOperationException("two builds differ");
        // The avenue (max-011 23d): one ginkgo row each side of the road, 6 m apart, so it reads as an avenue and not as a
        // grove (rows × length / spacing; cross streets, crossing paths and buildings take some out); a spring band of
        // cherry and flowering plum outside them.
        var avenue = a.Trees.Where(t => t.Source == PlantSource.Road && t.Species.Id == "ginkgo").ToList();
        if (avenue.Count == 0) throw new InvalidOperationException($"{avenue.Count} avenue ginkgos");
        var avenueRows = rules.Roads.Where(r => r.Species.Contains("ginkgo")).Sum(r => r.Rows.Count) * 2;
        if (avenue.Select(t => t.Key.Split(':')[3] + t.Key.Split(':')[4]).Distinct().Count() != avenueRows)
            throw new InvalidOperationException("the avenue does not have one row each side");
        var band = a.Trees.Where(t => t.Source == PlantSource.Band).ToList();
        var bandSpecies = rules.Roads.Where(r => r.Band is not null).SelectMany(r => r.Band!.Species).ToHashSet(StringComparer.Ordinal);
        if (band.Count == 0 || band.Any(t => !bandSpecies.Contains(t.Species.Id))) throw new InvalidOperationException($"band of {band.Count}");
        if (a.Count(PlantSource.Grove) == 0) throw new InvalidOperationException($"only {a.Count(PlantSource.Grove)} grove trees");
        if (a.Count(PlantSource.OsmTree) == 0) throw new InvalidOperationException($"only {a.Count(PlantSource.OsmTree)} OSM trees");
        if (a.Trees.Select(t => t.Key).Distinct().Count() != a.Trees.Count) throw new InvalidOperationException("duplicate keys");
        if (a.Trees.Any(t => t.Custom.X is < 0f or >= 1f || t.Custom.Z is < 0f or > 1f || !(t.Custom.W > 0f)))
            throw new InvalidOperationException("INSTANCE_CUSTOM out of range");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void EditingADistantGreenParcelKeepsLocalTrees()
    {
        var original = PavementTests.FixtureLayout.Value.BeforeGivingWay();
        if (!original.Any(t => t.Source == PlantSource.Grove)) throw new InvalidOperationException("the local parcel has no grove trees");
        var keys = original.Select(t => t.Key).ToHashSet(StringComparer.Ordinal);
        var small = PavementTests.PlantFixture(PavementTests.Fixture(RemoteGreen(-75f))).BeforeGivingWay();
        var enlarged = PavementTests.PlantFixture(PavementTests.Fixture(RemoteGreen(-50f))).BeforeGivingWay();
        if (!small.Any(t => !keys.Contains(t.Key)) || !enlarged.Any(t => !keys.Contains(t.Key)))
            throw new InvalidOperationException("the distant parcel has no grove trees");
        if (!PavementTests.SameRecords(original, small.Where(t => keys.Contains(t.Key))) ||
            !PavementTests.SameRecords(original, enlarged.Where(t => keys.Contains(t.Key))))
            throw new InvalidOperationException("a distant green edit changed a local tree or its order");
        if (PavementTests.SameRecords(small.Where(t => !keys.Contains(t.Key)), enlarged.Where(t => !keys.Contains(t.Key))))
            throw new InvalidOperationException("enlarging the distant parcel did not exercise a planting change");

        static string RemoteGreen(float east) =>
            "{\"id\":\"design:fixture-remote-green\",\"kind\":\"green\",\"geometry\":{\"type\":\"polygon\",\"rings\":{\"outer\":[[-95,-70],[" +
            PavementTests.F(east) + ",-70],[" + PavementTests.F(east) + ",70],[-95,70]],\"holes\":[]}},\"note\":\"Fixture: a distant green parcel.\"}";
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void GinkgosStayWhereTheyAreRegistered()
    {
        // WLD-VEGETATION: ginkgo only on the avenue (with its OSM street trees), beside the main building and at the
        // student centre; never spread elsewhere.
        var (world, _, rules) = Data.Value;
        var layout = PavementTests.WorldLayout.Value;
        var allowed = rules.Groves.Areas.Where(a => a.Mix.Any(m => m.Species == "ginkgo"))
            .Select(a => (a.Radius, Features: world.Features.Values.Where(f => f.Name == a.Name).ToList())).ToList();
        foreach (var t in layout.Trees.Where(t => t.Species.Id == "ginkgo" && t.Source == PlantSource.Grove))
        {
            var near = allowed.Any(area => area.Features.Any(f => Distance(f.Bounds, t.Position) <= area.Radius + TestTolerance.Layout));
            if (!near) throw new InvalidOperationException($"grove ginkgo {t.Key} at ({t.Position.X:F0}, {t.Position.Z:F0})");
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TrunksKeepOffBuildingsWaterAndRoads()
    {
        var world = Data.Value.World;
        var layout = PavementTests.WorldLayout.Value;
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
                    if (PlanLines.SegmentDistanceSquared(p, road.Line.Points[i - 1], road.Line.Points[i]) < road.Width * road.Width * 0.25f)
                        throw new InvalidOperationException($"{t.Key} stands on road {road.Id.Value}");
            if (!PlanGeometry.Contains(world.Boundary, p)) throw new InvalidOperationException($"{t.Key} outside the campus");
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void TreesLandOnTheTerrainAndAvoidSteepGround()
    {
        // WLD-TERRAIN: y = HeightAt(centre); no tree where the grade exceeds 25%. A tilted test terrain with one steep band,
        // judged on the exclusive fixture (a local rule: the real campus is planted elsewhere).
        var terrain = TiltedTerrain.Create();
        var layout = Planting.Build(new FakeWorld(PavementTests.FixtureWorld.Value) { Terrain = terrain }, PavementTests.Data.Species, PavementTests.FixtureRules, 1u);
        var flat = PavementTests.FixtureLayout.Value;
        foreach (var t in layout.Trees)
        {
            if (MathF.Abs(t.Position.Y - terrain.HeightAt(t.Position.X, t.Position.Z)) > TestTolerance.Position) throw new InvalidOperationException($"{t.Key} floats");
            if (terrain.FactsAt(t.Position.X, t.Position.Z).Grade > Planting.MaxGrade) throw new InvalidOperationException($"{t.Key} on a steep slope");
        }
        if (flat.Trees.Count(t => TiltedTerrain.Steep(t.Position.X)) == 0) throw new InvalidOperationException("the steep band had no trees to remove");
        if (layout.Trees.Count >= flat.Trees.Count) throw new InvalidOperationException("the steep band removed nothing");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void TrunkCollidersFollowTheTrees()
    {
        // Judged on the exclusive fixture's trees over flat ground: the colliders follow any layout.
        var layout = PavementTests.FixtureLayout.Value;
        var terrain = FlatTerrain.Default;
        var tree = layout.Trees.First(t => t.Source == PlantSource.Road);
        var tile = TileGrid.At(tree.Position);
        var colliders = TreeColliders.For(layout, tile, terrain).Cast<PostCollider>().ToList();
        var inTile = layout.Trees.Count(t => TileGrid.At(t.Position) == tile);
        if (colliders.Count != inTile || colliders.Count == 0) throw new InvalidOperationException($"{colliders.Count} posts for {inTile} trees");
        foreach (var c in colliders)
        {
            if (c.Foot != FootMat.Bark || c.Tag != SurfaceTag.Barrier) throw new InvalidOperationException("trunk material");
            if (!(c.R > 0.05f && c.R < 1f && c.Y1 > c.Y0 + 0.5f)
                || TileGrid.At(c.C) != tile)
                throw new InvalidOperationException($"post {c.C} r {c.R} y {c.Y0}–{c.Y1}");
        }
        foreach (var cell in new TileKey[] { new(3, 2), new(-4, -3) })
        {
            var moved = tree with { Position = new(cell.X * TileGrid.BaseEdge + 12, tree.Position.Y, cell.Z * TileGrid.BaseEdge + 24) };
            var post = (PostCollider)TreeColliders.For(new([moved]), cell, terrain).Single();
            if (post.C != new Vector2(moved.Position.X, moved.Position.Z))
                throw new InvalidOperationException("Tree collision did not retain world coordinates: " + cell);
            var local = (BoxCollider)Campus.Gen.Collision.CollisionGeometry.Localize([post], cell).Single();
            if (new Vector2(local.At.Translation.X, local.At.Translation.Z) != new Vector2(12, 24))
                throw new InvalidOperationException("The kernel did not localize the tree exactly once: " + cell);
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void LeavesFallUnderTheCrownsAndCarpetTheAvenue()
    {
        var (world, _, rules) = Data.Value;
        var layout = PavementTests.WorldLayout.Value;
        var rect = new MapRect(world.Boundary.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min),
            world.Boundary.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max));
        const float step = 1f;
        var w = (int)MathF.Round((rect.Max.X - rect.Min.X) / step);
        var h = (int)MathF.Round((rect.Max.Y - rect.Min.Y) / step);
        var m1 = new byte[w * h * 4];
        var m2 = new byte[w * h * 4];
        CanopyProjection.Paint(layout, rules, world, rect, step, m1, m2);
        byte At(byte[] m, int channel, float x, float z) => m[(((int)((z - rect.Min.Y) / step)) * w + (int)((x - rect.Min.X) / step)) * 4 + channel];
        var ginkgo = layout.Trees.First(t => t.Source == PlantSource.Road);
        if (At(m1, 0, ginkgo.Position.X, ginkgo.Position.Z) < 200) throw new InvalidOperationException("no ginkgo leaves under an avenue tree");
        if (At(m2, 2, ginkgo.Position.X, ginkgo.Position.Z) < 200) throw new InvalidOperationException("no canopy shelter under an avenue tree");
        var camphor = layout.Trees.First(t => t.Species.Id == "camphor" && layout.Trees.All(o => o == t || Vector3.Distance(o.Position, t.Position) > o.CrownRadius * 1.2f + 2f));
        if (At(m1, 1, camphor.Position.X, camphor.Position.Z) != 0) throw new InvalidOperationException("an evergreen dropped leaves");
        if (At(m1, 3, camphor.Position.X, camphor.Position.Z) != 0) throw new InvalidOperationException("an evergreen contributed maple potential");
        var maple = layout.Trees.First(t => t.Species.Deciduous && t.Species.Leaf == "maple");
        if (At(m1, 3, maple.Position.X, maple.Position.Z) == 0) throw new InvalidOperationException("no maple potential under a maple crown");
        for (var i = 0; i < m1.Length; i += 4)
            if (m1[i + 3] > m1[i + 1]) throw new InvalidOperationException("maple potential exceeds other leaves");
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
            : feature.Parts.Where(part => (part.Props?.OptionalText("kind") ?? feature.Kind.Value) == kind);

    private static float Distance(Box3 box, Vector3 p)
    {
        var dx = MathF.Max(0f, MathF.Max(box.Min.X - p.X, p.X - box.Max.X));
        var dz = MathF.Max(0f, MathF.Max(box.Min.Z - p.Z, p.Z - box.Max.Z));
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>A gentle 4% tilt along x, with a 40% band between x = −100 and 0 m.</summary>
    private static class TiltedTerrain
    {
        public static bool Steep(float x) => x is > -100f and < 0f;
        public static FormulaTerrain Create() => new((x, z) => 0.04f * x + 0.36f * Math.Clamp(x + 100f, 0f, 100f),
            (x, z) => new TerrainFacts(Steep(x) ? 0.4f : 0.04f, Steep(x) ? TerrainClass.Slope : TerrainClass.Flat, 0f, 0f));
    }

}
