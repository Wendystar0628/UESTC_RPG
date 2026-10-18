using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Ground.Tests;

/// <summary>
/// Tree pits and planting strips (overlay kind tree-pit): the ground carves them out of the paving round them, lays a
/// granite frame of the authored height and thickness and a sunken soil face inside, never lays anything over them, and
/// makes the tall ones solid. Also: a pitch that names no sport is plain grass whatever its proportions.
/// </summary>
[TestSuite]
public sealed class GroundTreePitTests
{
    private static readonly GroundSection Section = GroundSection.Default;
    /// <summary>Triangles this far inside an outline are checked; a pit corner smaller than a lattice cell (0.25 m) rounds off, so the last 0.15 m is not.</summary>
    private const float InsideMargin = 0.15f;
    /// <summary>Design heights above the paving (0.15 m): a pit frame 0.20, a strip kerb 0.15, soil 0.05 below; the records ask for 0.42 and 0.2.</summary>
    private const float PitRim = 0.35f, PitSoil = 0.30f, PitKerb = 0.22f, StripRim = 0.30f, StripSoil = 0.25f, StripKerb = 0.15f;
    private const float AuthoredPitRim = 0.42f, AuthoredStripRim = 0.2f;

    [TestCase]
    public void PitFrameAndSoilStandAtTheDesignHeightsAbovePaving()
    {
        var world = PlazaWorld();
        var pits = GroundSources.Read(world).Shapes.Where(s => s.Kind == SurfaceKind.Pit).ToArray();
        Check.True(pits.Length == 3, $"Two square pits and one strip are three pit shapes, got {pits.Length}.");
        var (rim, soil, kerb) = pits[0].PitHeights(Section);
        Check.Close(PitRim, rim, 0.01, "pit frame top");
        Check.Close(PitSoil, soil, 0.01, "pit soil top");
        Check.Close(PitKerb, kerb, 0.01, "pit frame thickness");

        var mesher = Mesher(world, out var field);
        Check.True(SurfaceAt(mesher, field, 20f, 20f) == GroundSurface.PitSoil, "The middle of a pit is soil.");
        Check.True(SurfaceAt(mesher, field, 19.25f, 20f) == GroundSurface.PitRim && SurfaceAt(mesher, field, 20.75f, 20f) == GroundSurface.PitRim,
            "A quarter metre in from the outer face (0.15 m) is the frame.");
        Check.True(SurfaceAt(mesher, field, 19.5f, 19.5f) == GroundSurface.PitSoil, "Inside the frame's thickness is soil.");
        Check.Close(PitRim, HeightAt(mesher, field, 19.25f, 20f), 0.01, "frame height");
        Check.Close(PitSoil, HeightAt(mesher, field, 20f, 20f), 0.01, "soil height");
        Check.True(PitSoil < PitRim, "The soil lies below the frame top.");

        // The strip: 2 m wide, a thinner and lower kerb (0.15 m above the paving), soil a little below it.
        Check.True(SurfaceAt(mesher, field, 40f, 31f) == GroundSurface.PitSoil, "The strip is soil inside its frame.");
        Check.Close(StripSoil, HeightAt(mesher, field, 40f, 31f), 0.01, "strip soil height");
        Check.Close(Section.PavingY + 0.15, pits[2].PitHeights(Section).Rim, 0.01, "strip kerb stands 0.15 m above the paving");
        Check.Close(Section.PavingY + 0.20, rim, 0.01, "pit frame stands 0.20 m above the paving");
        Check.Close(rim - 0.05, soil, 0.01, "soil lies 5 cm below the frame top");
        Check.True(StripSoil < StripRim && StripSoil > Section.PavingY, "The strip soil lies a little below its kerb, above the paving.");
    }

    [TestCase]
    public void PavingStopsAtTheFrameAndNothingLiesOverAPit()
    {
        var world = PlazaWorld();
        var mesher = Mesher(world, out var field);
        // Every lattice point inside a pit's outline is frame or soil: no paving, edging or lawn underneath or on top of it.
        var inside = 0;
        for (var j = 0; j <= field.N; j++)
            for (var i = 0; i <= field.N; i++)
            {
                var x = field.X(i);
                var z = field.Z(j);
                var surface = mesher.SurfaceAt(i, j);
                var isPit = surface is GroundSurface.PitRim or GroundSurface.PitBevel or GroundSurface.PitSoil;
                var d = world.Features.Values.Where(f => f.Kind.Value == "tree-pit").Select(f => PitDistance(f, x, z)).Min();
                if (d < -0.01f) { inside++; Check.True(isPit, $"({x}, {z}) lies inside a pit's outline but is {surface}."); }
                if (d > 0.01f) Check.True(!isPit, $"({x}, {z}) lies outside every pit but is {surface}.");
            }
        Check.True(inside > 500, $"The pits and the strip cover many lattice points, got {inside}.");

        // In the mesh: every triangle whose centre lies inside a pit (a little in from its outline, InsideMargin) is stone frame or soil
        // at the pit's own heights, never a paving layer at the paving level.
        var output = new GroundModule().Build(TestWorld.Context(world), new TileKey(0, 0), LodLevel.L0);
        var frame = GroundPalette.Id(GroundColor.Curb);
        var earth = GroundPalette.Id(GroundColor.Soil);
        var tops = new List<float>();
        var checkedTriangles = 0;
        foreach (var mesh in output.Meshes)
            for (var t = 0; t + 2 < mesh.Index.Length; t += 3)
            {
                var a = mesh.Index[t];
                var b = mesh.Index[t + 1];
                var c = mesh.Index[t + 2];
                var cx = (mesh.Position[3 * a] + mesh.Position[3 * b] + mesh.Position[3 * c]) / 3f;
                var cy = (mesh.Position[3 * a + 1] + mesh.Position[3 * b + 1] + mesh.Position[3 * c + 1]) / 3f;
                var cz = (mesh.Position[3 * a + 2] + mesh.Position[3 * b + 2] + mesh.Position[3 * c + 2]) / 3f;
                var d = world.Features.Values.Where(f => f.Kind.Value == "tree-pit").Select(f => PitDistance(f, cx, cz)).Min();
                if (d >= -InsideMargin) continue;
                checkedTriangles++;
                var color = mesh.ColorIndices![a];
                Check.True(color == frame || color == earth, $"A triangle of colour {color} lies over a pit at ({cx}, {cz}).");
                // A flat triangle (a top, not a vertical face) is never at the paving level: nothing is laid over a pit.
                var flat = MathF.Abs(mesh.Position[3 * a + 1] - mesh.Position[3 * b + 1]) < 1e-4f && MathF.Abs(mesh.Position[3 * a + 1] - mesh.Position[3 * c + 1]) < 1e-4f;
                if (!flat) continue;
                Check.True(Math.Abs(cy - Section.PavingY) > 0.005f, $"A triangle at paving level lies over a pit at ({cx}, {cz}).");
                if (color == earth) tops.Add(cy);
            }
        Check.True(checkedTriangles > 20, $"The pits are meshed, got {checkedTriangles} triangles inside them.");
        Check.True(tops.Any(y => Math.Abs(y - PitSoil) < 0.01f) && tops.Any(y => Math.Abs(y - StripSoil) < 0.01f), "Soil faces lie at the pit and strip soil heights.");
        foreach (var mesh in output.Meshes) mesh.ValidateVertexData();
        var maxY = output.Meshes.Max(m => Enumerable.Range(0, m.Position.Length / 3).Max(v => m.Position[3 * v + 1]));
        Check.Close(PitRim, maxY, 0.01, "highest point of the ground is the frame top");
    }

    [TestCase]
    public void PitFramesCarryChamfersAndFacesInStone()
    {
        var world = PlazaWorld();
        var output = new GroundModule().Build(TestWorld.Context(world), new TileKey(0, 0), LodLevel.L0);
        var frame = GroundPalette.Id(GroundColor.Curb);
        // The outer face and the inner face of the frame are vertical stone faces up to the frame top.
        var faces = 0;
        var chamfers = 0;
        foreach (var mesh in output.Meshes)
            for (var t = 0; t + 2 < mesh.Index.Length; t += 3)
            {
                var a = mesh.Index[t];
                var b = mesh.Index[t + 1];
                var c = mesh.Index[t + 2];
                if (mesh.ColorIndices![a] != frame) continue;
                var top = MathF.Max(mesh.Position[3 * a + 1], MathF.Max(mesh.Position[3 * b + 1], mesh.Position[3 * c + 1]));
                if (top < PitRim - 0.02f || top > PitRim + 1e-4f) continue;
                var ny = (mesh.Normal[3 * a + 1] + mesh.Normal[3 * b + 1] + mesh.Normal[3 * c + 1]) / 3f;
                if (ny < 0.05f) faces++;                           // vertical face of the frame
                else if (ny is > 0.5f and < 0.95f) chamfers++;     // tilted chamfer on a top edge
            }
        Check.True(faces > 0, "The frame has vertical stone faces.");
        Check.True(chamfers > 0, "The frame's top edges are chamfered, not cut square.");
    }

    [TestCase]
    public void NoPitDoesNotChangeTheGroundAndSiteStoneIsLeftAlone()
    {
        var bare = new TestWorldBuilder().Plaza().Build();
        Check.True(GroundSources.Read(bare).Shapes.All(s => s.Kind != SurfaceKind.Pit), "No tree-pit, no pit shape.");

        var site = new TestWorldBuilder().Plaza()
            .Pit("design:t-site", Square(20f, 20f, 1.8f, 0f), 0.42f, """{"plant":"tree","kerb":0.22,"soilAbove":0.32,"stone":"site"}""")
            .Pit("design:t-bed", null, 0.45f, """{"plant":"tree","radius":3.45,"seat":0.45}""")
            .Build();
        Check.True(GroundSources.Read(site).Shapes.All(s => s.Kind != SurfaceKind.Pit), "Stone built by a site package and a round bed (a point) are not the ground's.");
        var mesher = Mesher(site, out var field);
        Check.True(SurfaceAt(mesher, field, 20f, 20f) is GroundSurface.Granite or GroundSurface.Paver or GroundSurface.PlazaBorder, "The paving runs on where a site builds its own pit.");
    }

    [TestCase]
    public void PitDefaultsAndAuthoredHeights()
    {
        var bare = new PitProfile(null, null, null).Resolve(Section, compact: true);
        Check.Close(Section.PavingY + Section.Pits.PitRimAbovePaving, bare.Rim, 0.01, "default pit frame top");
        Check.Close(Section.PavingY + Section.Pits.StripRimAbovePaving, new PitProfile(null, null, null).Resolve(Section, compact: false).Rim, 0.01, "default strip kerb top");
        Check.True(bare.Soil < bare.Rim && bare.Kerb == Section.Pits.DefaultKerb, "default soil below the frame, default thickness");
        // The record's own heights are drawn only when asked for, measured from the paving unless a datum is given.
        var authored = Section with { Pits = Section.Pits with { UseAuthoredHeights = true } };
        var drawn = new PitProfile(0.42f, 0.32f, 0.22f).Resolve(authored, compact: true);
        Check.Close(Section.PavingY + 0.42f, drawn.Rim, 0.01, "authored frame top from the paving");
        Check.Close(Section.PavingY + 0.32f, drawn.Soil, 0.01, "authored soil top from the paving");
        var fromRoad = new PitProfile(0.42f, 0.32f, 0.22f).Resolve(authored with { Pits = authored.Pits with { Datum = 0f } }, compact: true);
        Check.Close(0.42, fromRoad.Rim, 0.01, "Datum 0 measures the authored heights from the carriageway level");
        var thin = new PitProfile(0.3f, 0.29f, 0.001f).Resolve(authored, compact: true);
        Check.True(thin.Kerb >= Section.Pits.MinKerb && thin.Rim - thin.Soil >= Section.Pits.MinSoilBelowRim - 0.005f, "A degenerate record still gets a frame and a sunken soil face.");
    }

    [TestCase]
    public void PitSoilUsesTheGroundHeightFieldAndFootMaterial()
    {
        var ctx = TestWorld.Context(PlazaWorld());
        var colliders = new GroundModule().Collide(ctx, new(0, 0));
        Check.True(colliders.Count == 1 && colliders[0] is HeightFieldCollider, "Tree pits must not add exact support shapes.");
        var field = (HeightFieldCollider)colliders[0];
        Check.Close(PitSoil, GroundCollision.SampleHeight(field, 20f, 20f), .02, "pit soil height");
        Check.True((FootMat)field.FootMatHalfMeter![40 * GroundCollision.FootCells + 40] == FootMat.Soil,
            "Soil remains the foot material inside a pit.");
    }


    [TestCase]
    public void CoarseLevelsDoNotMarkPits()
    {
        var world = PlazaWorld();
        var layout = GroundLayout.Of(world);
        var tile = new GroundTile(0f, 0f, 256f, 2f);
        var grid = TerrainGrid.For(FlatTerrain.Default, new TileKey(0, 0), LodLevel.L2, tile);
        var field = TileField.Build(layout, tile, Section, grid);
        var mesher = new GroundMesher(field, Section, LodLevel.L2, layout, grid, new MeshSink());
        for (var j = 0; j <= field.N; j++)
            for (var i = 0; i <= field.N; i++)
                Check.True(mesher.SurfaceAt(i, j) is not (GroundSurface.PitRim or GroundSurface.PitBevel or GroundSurface.PitSoil), "L2 and beyond resolve no pits.");
    }

    [TestCase]
    public void AnUntaggedPitchGetsNoWhiteLinesWhateverItsProportions()
    {
        // A 105 x 68 rectangle is football proportions. With a sport tag it is marked; without one it is plain grass.
        var marked = TestWorld.Of([Pitch("test:way:1", "soccer", 0f, 0f)]);
        var plain = TestWorld.Of([Pitch("test:way:2", null, 0f, 0f)]);
        var markedShape = GroundSources.Read(marked).Shapes.Single();
        var plainShape = GroundSources.Read(plain).Shapes.Single();
        Check.True(markedShape.Kind == SurfaceKind.Turf && markedShape.Marked, "A pitch that names its sport is marked turf.");
        Check.True(plainShape.Kind == SurfaceKind.Turf && !plainShape.Marked, "A pitch without a sport tag is turf that is not marked.");
        Check.True(Quads(GroundLayout.Of(marked)).Count > 100, "The tagged pitch has its lines.");
        Check.True(Quads(GroundLayout.Of(plain)).Count == 0, "The untagged pitch has no lines.");
        // The same football-sized rectangle as the athletics-style untagged field of the real campus: still plain grass.
        var output = new GroundModule().Build(TestWorld.Context(plain), new TileKey(0, 0), LodLevel.L0);
        Check.True(output.Meshes.All(m => m.Material.Spec.Lit.Value != "ground.marking.lit"), "No marking mesh is built for an untagged pitch.");
    }

    [TestCase]
    public void RealCampusPitsAndStripBecomeGroundShapes()
    {
        var world = WorldModelLoader.Load(new("qingshuihe"), RealFiles()).World;
        var shapes = GroundSources.Read(world).Shapes.Where(s => s.Kind == SurfaceKind.Pit).ToArray();
        int Authored(string id) => world.Features[new(id)].Geometry switch { MultiPolygonGeometry m => m.Polygons.Count, PolygonGeometry => 1, _ => 0 };
        var squares = shapes.Count(s => s.Id.Value == "design:north-dangui-plaza-tree-pits");
        Check.True(squares > 0 && squares == Authored("design:north-dangui-plaza-tree-pits"), "Every square pit of the north plaza is one shape.");
        var strip = shapes.Where(s => s.Id.Value == "design:north-dangui-planting-strip").ToArray();
        Check.True(strip.Length > 0 && strip.Length == Authored("design:north-dangui-planting-strip"), "Every segment of the planting strip is one shape.");
        var (rim, soil, kerb) = strip[0].PitHeights(Section);
        Check.Close(StripRim, rim, 0.01, "strip kerb top");
        Check.Close(StripSoil, soil, 0.01, "strip soil top");
        Check.Close(0.15, kerb, 0.01, "strip frame thickness");
        Check.True(shapes.All(s => s.Rings is { Length: >= 1 }), "Every pit is an outline.");
    }

    // ---------------------------------------------------------------- fixtures and helpers

    /// <summary>An 80 m plaza with two square pits (one turned 30 degrees) and a 2 m strip, all inside it, in tile (0, 0).</summary>
    private static TestWorld PlazaWorld() => new TestWorldBuilder().Plaza()
        .Pit("design:t-pits", null, AuthoredPitRim, """{"plant":"tree","perPit":1,"size":1.8,"kerb":0.22,"soilAbove":0.32}""",
            Square(20f, 20f, 1.8f, 0f), Square(36f, 20f, 1.8f, 30f))
        .Pit("design:t-strip", null, AuthoredStripRim, """{"plant":"tree","kerb":0.15,"soilAbove":0.14}""", Strip(30f, 30.05f, 50f, 32.05f))
        .Build();

    private static PolygonRings Strip(float x0, float z0, float x1, float z1) => new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

    private static PolygonRings Square(float cx, float cz, float size, float degrees)
    {
        var a = degrees * MathF.PI / 180f;
        var u = new Vector2(MathF.Cos(a), MathF.Sin(a)) * (size * 0.5f);
        var w = new Vector2(-u.Y, u.X);
        var c = new Vector2(cx, cz);
        return new([c - u - w, c + u - w, c + u + w, c - u + w]);
    }

    private static float PitDistance(WorldFeature feature, float x, float z)
    {
        var best = float.MaxValue;
        if (feature.Geometry is not MultiPolygonGeometry multi) return best;
        foreach (var rings in multi.Polygons)
        {
            var shape = GroundSources.Polygon(feature.Id, SurfaceKind.Pit, rings)!;
            best = MathF.Min(best, Sdf.Shape(shape, new(x, z), 50f));
        }
        return best;
    }

    private static GroundMesher Mesher(IWorldModel world, out TileField field)
    {
        var layout = GroundLayout.Of(world);
        var tile = new GroundTile(0f, 0f, GroundLod.TileSize, GroundLod.TileSize / 256f);
        var grid = TerrainGrid.For(FlatTerrain.Default, new TileKey(0, 0), LodLevel.L0, tile);
        field = TileField.Build(layout, tile, Section, grid);
        return new GroundMesher(field, Section, LodLevel.L0, layout, grid, new MeshSink());
    }

    private static GroundSurface SurfaceAt(GroundMesher mesher, TileField field, float x, float z) =>
        mesher.SurfaceAt((int)MathF.Round((x - field.X0) / field.Cell), (int)MathF.Round((z - field.Z0) / field.Cell));

    private static float HeightAt(GroundMesher mesher, TileField field, float x, float z) =>
        mesher.HeightAt((int)MathF.Round((x - field.X0) / field.Cell), (int)MathF.Round((z - field.Z0) / field.Cell));

    private static List<RoadMarkings.Quad> Quads(GroundLayout layout)
    {
        var markings = layout.Markings(Section);
        var quads = new List<RoadMarkings.Quad>();
        for (var z = -2; z <= 2; z++)
            for (var x = -2; x <= 2; x++) quads.AddRange(markings.In(x, z));
        return quads;
    }

    /// <summary>A 105 x 68 pitch centred at (cx, cz) with its long side along x; the sport tag is optional.</summary>
    private static WorldFeature Pitch(string id, string? sport, float cx, float cz)
    {
        var rings = new PolygonRings([new(cx - 52.5f, cz - 34f), new(cx + 52.5f, cz - 34f), new(cx + 52.5f, cz + 34f), new(cx - 52.5f, cz + 34f)]);
        var tags = sport is null ? """{"leisure":"pitch"}""" : $$"""{"leisure":"pitch","sport":"{{sport}}"}""";
        var props = sport is null ? """{"kind":"sport"}""" : $$"""{"kind":"sport","sport":"{{sport}}"}""";
        var part = new WorldFeaturePart(id.Replace("test:", "osm:", StringComparison.Ordinal), new PolygonGeometry(rings), null, null) { Props = Json(props) };
        return new(new FeatureId(id), FeatureSource.Osm, new DesignKind("sport"), new PolygonGeometry(rings))
        {
            Parts = [part],
            Bounds = new Box3(new(cx - 52.5f, 0f, cz - 34f), new(cx + 52.5f, 0f, cz + 34f)),
            Props = Json($$"""{"id":"{{id}}","kind":"sport","sourceTags":{{tags}}}""")
        };
    }

    private static Dictionary<string, string> RealFiles()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        var data = Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
        var files = new[] { "campus.json", "network.json", "interiors.json" }
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        foreach (var path in Directory.GetDirectories(Path.Combine(data, "overlay"), "*.d")
                    .Where(dir => Path.GetFileName(dir) is not "interior.d" and not "terrain.d" and not "outskirts.d" and not "walkable.d" and not "fidelity.d")
                    .SelectMany(dir => Directory.GetFiles(dir, "*.json")).OrderBy(p => p, StringComparer.Ordinal))
            files[Path.GetRelativePath(data, path).Replace('\\', '/')] = File.ReadAllText(path);
        return files;
    }

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>Collects the features of a small test world: a plaza and any number of tree-pit records.</summary>
    private sealed class TestWorldBuilder
    {
        private readonly List<WorldFeature> _features = [];

        public TestWorldBuilder Plaza()
        {
            var rings = new PolygonRings([new(4f, 4f), new(60f, 4f), new(60f, 60f), new(4f, 60f)]);
            _features.Add(new(new FeatureId("osm:way:plaza"), FeatureSource.Osm, new DesignKind("road"), new PolygonGeometry(rings))
            {
                Bounds = new Box3(new(4f, 0f, 4f), new(60f, 0f, 60f)),
                Props = Json("""{"roadType":"pedestrian"}""")
            });
            return this;
        }

        /// <summary>A tree-pit record: polygons, or a point at (20, 20) when none are given.</summary>
        public TestWorldBuilder Pit(string id, PolygonRings? single, float height, string props, params PolygonRings[] polygons)
        {
            PolygonRings[] all = single is null ? polygons : [single, .. polygons];
            OverlayGeometry geometry = all.Length > 0 ? new MultiPolygonGeometry(all) : new PointGeometry(new Vector3(20f, 0f, 20f));
            _features.Add(new(new FeatureId(id), FeatureSource.Design, new DesignKind("tree-pit"), geometry)
            {
                Height = height,
                Bounds = new Box3(new(4f, 0f, 4f), new(60f, height, 60f)),
                Props = Json(props)
            });
            return this;
        }

        public TestWorld Build() => TestWorld.Of(_features);
    }
}
