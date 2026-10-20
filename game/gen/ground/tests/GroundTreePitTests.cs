using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Ground.Tests;

using static GdUnit4.Assertions;

/// <summary>
/// Tree pits and planting strips (world kind tree-pit): the ground carves them out of the paving round them, lays a
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
    private static readonly float PitRim = Section.PavingY + Section.Pits.PitRimAbovePaving;
    private static readonly float PitSoil = PitRim - Section.Pits.SoilBelowRim;
    private static readonly float StripRim = Section.PavingY + Section.Pits.StripRimAbovePaving;
    private static readonly float StripSoil = StripRim - Section.Pits.SoilBelowRim;
    private const float PitKerb = 0.22f;
    private const float AuthoredPitRim = 0.42f, AuthoredStripRim = 0.2f;

    [TestCase, TestCategory(TestReason.Contract)]
    public void PitFrameAndSoilStandAtTheDesignHeightsAbovePaving()
    {
        var world = PlazaWorld();
        var pits = GroundSources.Read(world).Shapes.Where(s => s.Kind == SurfaceKind.Pit).ToArray();
        AssertThat(pits.Length == 3).OverrideFailureMessage($"Two square pits and one strip are three pit shapes, got {pits.Length}.").IsTrue();
        var (rim, soil, kerb) = pits[0].PitHeights(Section);
        AssertThat(Math.Abs((double)(PitRim) - (double)(rim)) <= TestTolerance.Position).OverrideFailureMessage($"pit frame top: expected {PitRim}, got {rim}.").IsTrue();
        AssertThat(Math.Abs((double)(PitSoil) - (double)(soil)) <= TestTolerance.Position).OverrideFailureMessage($"pit soil top: expected {PitSoil}, got {soil}.").IsTrue();
        AssertThat(Math.Abs((double)(PitKerb) - (double)(kerb)) <= TestTolerance.Position).OverrideFailureMessage($"pit frame thickness: expected {PitKerb}, got {kerb}.").IsTrue();

        var mesher = Mesher(world, out var field);
        AssertThat(SurfaceAt(mesher, field, 20f, 20f) == GroundSurface.PitSoil).OverrideFailureMessage("The middle of a pit is soil.").IsTrue();
        AssertThat(SurfaceAt(mesher, field, 19.25f, 20f) == GroundSurface.PitRim && SurfaceAt(mesher, field, 20.75f, 20f) == GroundSurface.PitRim).OverrideFailureMessage("A quarter metre in from the outer face (0.15 m) is the frame.").IsTrue();
        AssertThat(SurfaceAt(mesher, field, 19.5f, 19.5f) == GroundSurface.PitSoil).OverrideFailureMessage("Inside the frame's thickness is soil.").IsTrue();
        AssertThat(Math.Abs((double)(PitRim) - (double)(HeightAt(mesher, field, 19.25f, 20f))) <= TestTolerance.Position).OverrideFailureMessage($"frame height: expected {PitRim}, got {HeightAt(mesher, field, 19.25f, 20f)}.").IsTrue();
        AssertThat(Math.Abs((double)(PitSoil) - (double)(HeightAt(mesher, field, 20f, 20f))) <= TestTolerance.Position).OverrideFailureMessage($"soil height: expected {PitSoil}, got {HeightAt(mesher, field, 20f, 20f)}.").IsTrue();
        AssertThat(PitSoil < PitRim).OverrideFailureMessage("The soil lies below the frame top.").IsTrue();

        // The strip: 2 m wide, a thinner and lower kerb (0.15 m above the paving), soil a little below it.
        AssertThat(SurfaceAt(mesher, field, 40f, 31f) == GroundSurface.PitSoil).OverrideFailureMessage("The strip is soil inside its frame.").IsTrue();
        AssertThat(Math.Abs((double)(StripSoil) - (double)(HeightAt(mesher, field, 40f, 31f))) <= TestTolerance.Position).OverrideFailureMessage($"strip soil height: expected {StripSoil}, got {HeightAt(mesher, field, 40f, 31f)}.").IsTrue();
        AssertThat(Math.Abs((double)(Section.PavingY + 0.15) - (double)(pits[2].PitHeights(Section).Rim)) <= TestTolerance.Position).OverrideFailureMessage($"strip kerb stands 0.15 m above the paving: expected {Section.PavingY + 0.15}, got {pits[2].PitHeights(Section).Rim}.").IsTrue();
        AssertThat(Math.Abs((double)(Section.PavingY + 0.20) - (double)(rim)) <= TestTolerance.Position).OverrideFailureMessage($"pit frame stands 0.20 m above the paving: expected {Section.PavingY + 0.20}, got {rim}.").IsTrue();
        AssertThat(Math.Abs((double)(rim - 0.05) - (double)(soil)) <= TestTolerance.Position).OverrideFailureMessage($"soil lies 5 cm below the frame top: expected {rim - 0.05}, got {soil}.").IsTrue();
        AssertThat(StripSoil < StripRim && StripSoil > Section.PavingY).OverrideFailureMessage("The strip soil lies a little below its kerb, above the paving.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
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
                if (d < -TestTolerance.Position) { inside++; AssertThat(isPit).OverrideFailureMessage($"({x}, {z}) lies inside a pit's outline but is {surface}.").IsTrue(); }
                if (d > TestTolerance.Position) AssertThat(!isPit).OverrideFailureMessage($"({x}, {z}) lies outside every pit but is {surface}.").IsTrue();
            }
        AssertThat(inside > 500).OverrideFailureMessage($"The pits and the strip cover many lattice points, got {inside}.").IsTrue();

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
                AssertThat(color == frame || color == earth).OverrideFailureMessage($"A triangle of colour {color} lies over a pit at ({cx}, {cz}).").IsTrue();
                // A flat triangle (a top, not a vertical face) is never at the paving level: nothing is laid over a pit.
                var flat = MathF.Abs(mesh.Position[3 * a + 1] - mesh.Position[3 * b + 1]) < TestTolerance.Position && MathF.Abs(mesh.Position[3 * a + 1] - mesh.Position[3 * c + 1]) < TestTolerance.Position;
                if (!flat) continue;
                AssertThat(Math.Abs(cy - Section.PavingY) > Campus.Gen.Geometry.QuantizedPlan.Tolerance).OverrideFailureMessage($"A triangle at paving level lies over a pit at ({cx}, {cz}).").IsTrue();
                if (color == earth) tops.Add(cy);
            }
        AssertThat(checkedTriangles > 20).OverrideFailureMessage($"The pits are meshed, got {checkedTriangles} triangles inside them.").IsTrue();
        AssertThat(tops.Any(y => Math.Abs(y - PitSoil) < TestTolerance.Position) && tops.Any(y => Math.Abs(y - StripSoil) < TestTolerance.Position)).OverrideFailureMessage("Soil faces lie at the pit and strip soil heights.").IsTrue();
        foreach (var mesh in output.Meshes) mesh.ValidateVertexData();
        var maxY = output.Meshes.Max(m => Enumerable.Range(0, m.Position.Length / 3).Max(v => m.Position[3 * v + 1]));
        AssertThat(Math.Abs((double)(PitRim) - (double)(maxY)) <= TestTolerance.Position).OverrideFailureMessage($"highest point of the ground is the frame top: expected {PitRim}, got {maxY}.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
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
                if (top < PitRim - TestTolerance.Position || top > PitRim + TestTolerance.Position) continue;
                var ny = (mesh.Normal[3 * a + 1] + mesh.Normal[3 * b + 1] + mesh.Normal[3 * c + 1]) / 3f;
                if (ny < 0.05f) faces++;                           // vertical face of the frame
                else if (ny is > 0.5f and < 0.95f) chamfers++;     // tilted chamfer on a top edge
            }
        AssertThat(faces > 0).OverrideFailureMessage("The frame has vertical stone faces.").IsTrue();
        AssertThat(chamfers > 0).OverrideFailureMessage("The frame's top edges are chamfered, not cut square.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void NoPitDoesNotChangeTheGroundAndSiteStoneIsLeftAlone()
    {
        var bare = new TestWorldBuilder().Plaza().Build();
        AssertThat(GroundSources.Read(bare).Shapes.All(s => s.Kind != SurfaceKind.Pit)).OverrideFailureMessage("No tree-pit, no pit shape.").IsTrue();

        var site = new TestWorldBuilder().Plaza()
            .Pit("design:t-site", Square(20f, 20f, 1.8f, 0f), 0.42f, """{"plant":"tree","kerb":0.22,"soilAbove":0.32,"stone":"site"}""")
            .Pit("design:t-bed", null, 0.45f, """{"plant":"tree","radius":3.45,"seat":0.45}""")
            .Build();
        AssertThat(GroundSources.Read(site).Shapes.All(s => s.Kind != SurfaceKind.Pit)).OverrideFailureMessage("Stone built by a site package and a round bed (a point) are not the ground's.").IsTrue();
        var mesher = Mesher(site, out var field);
        AssertThat(SurfaceAt(mesher, field, 20f, 20f) is GroundSurface.Granite or GroundSurface.Paver or GroundSurface.PlazaBorder).OverrideFailureMessage("The paving runs on where a site builds its own pit.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PitDefaultsAndAuthoredHeights()
    {
        var bare = new PitProfile(null, null, null).Resolve(Section, compact: true);
        AssertThat(Math.Abs((double)(Section.PavingY + Section.Pits.PitRimAbovePaving) - (double)(bare.Rim)) <= TestTolerance.Position).OverrideFailureMessage($"default pit frame top: expected {Section.PavingY + Section.Pits.PitRimAbovePaving}, got {bare.Rim}.").IsTrue();
        AssertThat(Math.Abs((double)(Section.PavingY + Section.Pits.StripRimAbovePaving) - (double)(new PitProfile(null, null, null).Resolve(Section, compact: false).Rim)) <= TestTolerance.Position).OverrideFailureMessage($"default strip kerb top: expected {Section.PavingY + Section.Pits.StripRimAbovePaving}, got {new PitProfile(null, null, null).Resolve(Section, compact: false).Rim}.").IsTrue();
        AssertThat(bare.Soil < bare.Rim && bare.Kerb == Section.Pits.DefaultKerb).OverrideFailureMessage("default soil below the frame, default thickness").IsTrue();
        // The record's own heights are drawn only when asked for, measured from the paving unless a datum is given.
        var authored = Section with { Pits = Section.Pits with { UseAuthoredHeights = true } };
        var drawn = new PitProfile(0.42f, 0.32f, 0.22f).Resolve(authored, compact: true);
        AssertThat(Math.Abs((double)(Section.PavingY + 0.42f) - (double)(drawn.Rim)) <= TestTolerance.Position).OverrideFailureMessage($"authored frame top from the paving: expected {Section.PavingY + 0.42f}, got {drawn.Rim}.").IsTrue();
        AssertThat(Math.Abs((double)(Section.PavingY + 0.32f) - (double)(drawn.Soil)) <= TestTolerance.Position).OverrideFailureMessage($"authored soil top from the paving: expected {Section.PavingY + 0.32f}, got {drawn.Soil}.").IsTrue();
        var fromRoad = new PitProfile(0.42f, 0.32f, 0.22f).Resolve(authored with { Pits = authored.Pits with { Datum = 0f } }, compact: true);
        AssertThat(Math.Abs((double)(0.42) - (double)(fromRoad.Rim)) <= TestTolerance.Position).OverrideFailureMessage($"Datum 0 measures the authored heights from the carriageway level: expected {0.42}, got {fromRoad.Rim}.").IsTrue();
        var thin = new PitProfile(0.3f, 0.29f, 0.001f).Resolve(authored, compact: true);
        AssertThat(thin.Kerb >= Section.Pits.MinKerb && thin.Rim - thin.Soil >= Section.Pits.MinSoilBelowRim - TestTolerance.Position).OverrideFailureMessage("A degenerate record still gets a frame and a sunken soil face.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void PitSoilUsesTheGroundHeightFieldAndFootMaterial()
    {
        var ctx = TestWorld.Context(PlazaWorld());
        var colliders = new GroundModule().Collide(ctx, new(0, 0));
        AssertThat(colliders.Count == 1 && colliders[0] is HeightFieldCollider).OverrideFailureMessage("Tree pits must not add exact support shapes.").IsTrue();
        var field = (HeightFieldCollider)colliders[0];
        AssertThat(Math.Abs((double)(PitSoil) - (double)(GroundCollision.SampleHeight(field, 20f, 20f))) <= TestTolerance.Position).OverrideFailureMessage($"pit soil height: expected {PitSoil}, got {GroundCollision.SampleHeight(field, 20f, 20f)}.").IsTrue();
        AssertThat((FootMat)field.FootMatHalfMeter![40 * GroundCollision.FootCells + 40] == FootMat.Soil).OverrideFailureMessage("Soil remains the foot material inside a pit.").IsTrue();
    }


    [TestCase, TestCategory(TestReason.Contract)]
    public void CoarseLevelsDoNotMarkPits()
    {
        var world = PlazaWorld();
        var layout = GroundLayout.Of(world);
        var tile = new GroundTile(0f, 0f, 256f, 2f);
        var grid = TerrainGrid.For(FlatTerrain.Default, new TileKey(0, 0), LodLevel.L2, tile);
        var field = TileField.Build(layout, tile, Section, grid);
        var mesher = new GroundMesher(field, Section, LodLevel.L2, layout, grid, new GroundSurfaceSink());
        for (var j = 0; j <= field.N; j++)
            for (var i = 0; i <= field.N; i++)
                AssertThat(mesher.SurfaceAt(i, j) is not (GroundSurface.PitRim or GroundSurface.PitBevel or GroundSurface.PitSoil)).OverrideFailureMessage("L2 and beyond resolve no pits.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AnUntaggedPitchGetsNoWhiteLinesWhateverItsProportions()
    {
        // A 105 x 68 rectangle is football proportions. With a sport tag it is marked; without one it is plain grass.
        var marked = TestWorld.Of([Pitch("test:way:1", "soccer", 0f, 0f)]);
        var plain = TestWorld.Of([Pitch("test:way:2", null, 0f, 0f)]);
        var markedShape = GroundSources.Read(marked).Shapes.Single();
        var plainShape = GroundSources.Read(plain).Shapes.Single();
        AssertThat(markedShape.Kind == SurfaceKind.Turf && markedShape.Marked).OverrideFailureMessage("A pitch that names its sport is marked turf.").IsTrue();
        AssertThat(plainShape.Kind == SurfaceKind.Turf && !plainShape.Marked).OverrideFailureMessage("A pitch without a sport tag is turf that is not marked.").IsTrue();
        AssertThat(Quads(TestWorld.Context(marked)).Count > 100).OverrideFailureMessage("The tagged pitch has its lines.").IsTrue();
        AssertThat(Quads(TestWorld.Context(plain)).Count == 0).OverrideFailureMessage("The untagged pitch has no lines.").IsTrue();
        // The same football-sized rectangle as the athletics-style untagged field of the real campus: still plain grass.
        var output = new GroundModule().Build(TestWorld.Context(plain), new TileKey(0, 0), LodLevel.L0);
        AssertThat(output.Meshes.All(m => m.Material.Spec.Lit.Value != "ground.marking.lit")).OverrideFailureMessage("No marking mesh is built for an untagged pitch.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Smoke)]
    [TestCategory("Slow")]
    public void RealCampusPitsAndStripBecomeGroundShapes()
    {
        var world = RealMapFixture.Load().World;
        var shapes = GroundSources.Read(world).Shapes.Where(s => s.Kind == SurfaceKind.Pit).ToArray();
        var authored = world.Features.Values.Where(feature => feature.Kind.Value == GroundSources.PitKind &&
            WorldProps.TryText(feature.Props, "stone") != "site" && feature.Geometry is PolygonGeometry or MultiPolygonGeometry).ToArray();
        AssertThat(authored.Length > 0).IsTrue();
        foreach (var feature in authored)
        {
            var polygons = feature.Geometry is PolygonGeometry ? 1 : ((MultiPolygonGeometry)feature.Geometry).Polygons.Count;
            var pieces = shapes.Where(shape => shape.Id == feature.Id).ToArray();
            AssertThat(pieces.Length == polygons).OverrideFailureMessage($"Every authored polygon of {feature.Id} becomes one pit.").IsTrue();
            foreach (var piece in pieces)
            {
                var (rim, soil, kerb) = piece.PitHeights(Section);
                AssertThat(rim > Section.PavingY && soil < rim && kerb >= Section.Pits.MinKerb && kerb <= Section.Pits.MaxKerb)
                    .OverrideFailureMessage("The stone frame stands above paving and contains sunken soil.").IsTrue();
                AssertThat(MathF.Abs(rim - soil - Section.Pits.SoilBelowRim) <= TestTolerance.Position).IsTrue();
            }
        }
        AssertThat(shapes.All(shape => shape.Rings is { Length: >= 1 })).IsTrue();
    }

    // ---------------------------------------------------------------- fixtures and helpers

    /// <summary>An 80 m plaza with two square pits (one turned 30 degrees) and a 2 m strip, all inside it, in tile (0, 0).</summary>
    private static FakeWorld PlazaWorld() => new TestWorldBuilder().Plaza()
        .Pit("design:t-pits", null, AuthoredPitRim, """{"plant":"tree","perPit":1,"size":1.8,"kerb":0.22,"soilAbove":0.32}""",
            Square(20f, 20f, 1.8f, 0f), Square(36f, 20f, 1.8f, 30f))
        .Pit("design:t-strip", null, AuthoredStripRim, """{"plant":"tree","kerb":0.15,"soilAbove":0.14}""", Strip(30f, 30.05f, 50f, 32.05f))
        .Build();

    private static PolygonRings Strip(float x0, float z0, float x1, float z1) => new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

    private static PolygonRings Square(float cx, float cz, float size, float degrees)
    {
        var a = degrees * Angles.DegreesToRadians;
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
        var tile = new GroundTile(0f, 0f, TileGrid.BaseEdge, TileGrid.BaseEdge / 256f);
        var grid = TerrainGrid.For(FlatTerrain.Default, new TileKey(0, 0), LodLevel.L0, tile);
        field = TileField.Build(layout, tile, Section, grid);
        return new GroundMesher(field, Section, LodLevel.L0, layout, grid, new GroundSurfaceSink());
    }

    private static GroundSurface SurfaceAt(GroundMesher mesher, TileField field, float x, float z) =>
        mesher.SurfaceAt((int)MathF.Round((x - field.X0) / field.Cell), (int)MathF.Round((z - field.Z0) / field.Cell));

    private static float HeightAt(GroundMesher mesher, TileField field, float x, float z) =>
        mesher.HeightAt((int)MathF.Round((x - field.X0) / field.Cell), (int)MathF.Round((z - field.Z0) / field.Cell));

    private static List<GroundMarkings.Quad> Quads(BuildContext context)
    {
        var markings = GroundModule.AssembleMarkings(context, Section).Markings;
        var quads = new List<GroundMarkings.Quad>();
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
        return WorldFeatureFixture.Create(id, "sport", new PolygonGeometry(rings),
            tags, parts: [part]);
    }



    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>Collects the features of a small test world: a plaza and any number of tree-pit records.</summary>
    private sealed class TestWorldBuilder
    {
        private readonly List<WorldFeature> _features = [];

        public TestWorldBuilder Plaza()
        {
            var rings = new PolygonRings([new(4f, 4f), new(60f, 4f), new(60f, 60f), new(4f, 60f)]);
            _features.Add(WorldFeatureFixture.Create("osm:way:plaza", "road", new PolygonGeometry(rings),
                """{"highway":"pedestrian"}"""));
            return this;
        }

        /// <summary>A tree-pit record: polygons, or a point at (20, 20) when none are given.</summary>
        public TestWorldBuilder Pit(string id, PolygonRings? single, float height, string props, params PolygonRings[] polygons)
        {
            PolygonRings[] all = single is null ? polygons : [single, .. polygons];
            WorldGeometry geometry = all.Length > 0 ? new MultiPolygonGeometry(all) : new PointGeometry(new Vector3(20f, 0f, 20f));
            _features.Add(WorldFeatureFixture.Create(id, GroundSources.PitKind, geometry, props, height: height)
                with { Bounds = new Box3(new(4f, 0f, 4f), new(60f, height, 60f)) });
            return this;
        }

        public FakeWorld Build() => TestWorld.Of(_features);
    }
}
