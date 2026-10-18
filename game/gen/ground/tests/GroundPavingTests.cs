using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Tests.Support;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Ground.Tests;

using static GdUnit4.Assertions;

/// <summary>
/// Q-015 plaza paving: the forecourt of a library's open entrance carries the granite profile with metre UVs square to
/// the entrance, one joint on its axis; curbs, borders and other plazas stay plain, a place laid inside it continues the grid.
/// </summary>
[TestSuite]
public sealed class GroundPavingTests
{
    // Door at (0, 0) facing east (yaw 90): the axis is +X, the plaza starts 10 m out.
    private static readonly Vector2 Door = new(0f, 0f);

    [TestCase, TestCategory(TestReason.Contract)]
    public void LibraryForecourtGetsGraniteFrameOnTheEntranceAxis()
    {
        var world = PavingWorld.Library("library", 90f);
        var meshes = Build(world);
        var patterned = meshes.Where(m => m.Material.Spec.SurfacePattern is not null).ToArray();
        AssertThat(patterned.Length > 0).OverrideFailureMessage("The library forecourt must produce patterned plaza geometry.").IsTrue();
        foreach (var mesh in patterned)
        {
            mesh.ValidateVertexData();   // contract: UV and UV2 present, gate/K constant per triangle
            AssertThat(mesh.Material.Spec.SurfacePattern == GroundPaving.Granite).OverrideFailureMessage("Granite 600 profile.").IsTrue();
            AssertThat(mesh.Material.Spec with { SurfacePattern = null } == GroundPalette.Spec(GroundMaterial.Plaza)).OverrideFailureMessage("The profile is the only material difference from the plain plaza class.").IsTrue();
            AssertThat(mesh.ColorIndices!.All(id => id == GroundPalette.Id(GroundColor.Curb))).OverrideFailureMessage("Granite keeps its colour.").IsTrue();
            for (var v = 0; v < mesh.Position.Length / 3; v++)
            {
                var x = mesh.Position[3 * v];
                var z = mesh.Position[3 * v + 2];
                // Frame: V along the axis from the plaza edge (x = 10), U across it with U = 0 on the axis line (z = 0).
                AssertThat(Math.Abs((double)(z) - (double)(mesh.Uv![2 * v])) <= TestTolerance.Position).OverrideFailureMessage($"U is the signed distance from the entrance axis: expected {z}, got {mesh.Uv![2 * v]}.").IsTrue();
                AssertThat(Math.Abs((double)(x - 10f) - (double)(mesh.Uv[2 * v + 1])) <= TestTolerance.Position).OverrideFailureMessage($"V starts at the plaza edge on the axis: expected {x - 10f}, got {mesh.Uv[2 * v + 1]}.").IsTrue();
                AssertThat(mesh.Uv2![2 * v] == 1f && mesh.Uv2[2 * v + 1] == 3f).OverrideFailureMessage("Without a fidelity model the D0 reading K3 applies.").IsTrue();
            }
            for (var i = 0; i < mesh.Index.Length; i += 3)
                AssertThat(Centre(mesh, i).Y < 25f).OverrideFailureMessage("The neighbouring plaza off the axis is not patterned.").IsTrue();
        }
        var layout = GroundLayout.Of(world);
        var sources = new GroundSourceContext(layout.Near(new(0, -64), new(64, 64)), GroundSection.Default, 0f, layout.Paving);
        var top = new GroundFaceInput(GroundSurface.Granite, GroundFaceRole.Top, sources);
        AssertThat(top.PavingAt(new(35, 0)) is { } inner && inner == top.PavingAt(new(20, 0))).OverrideFailureMessage("An unbound place laid inside the forecourt continues the forecourt grid (no seam along the triangulation).").IsTrue();
        AssertThat(top.PavingAt(new(35, 40)) is null &&
            new GroundFaceInput(GroundSurface.Granite, GroundFaceRole.Vertical, sources).PavingAt(new(20, 0)) is null &&
            new GroundFaceInput(GroundSurface.PlazaBorder, GroundFaceRole.Top, sources).PavingAt(new(20, 0)) is null).OverrideFailureMessage("Other plazas, vertical faces and plaza borders take no frame.").IsTrue();
        var plain = meshes.Where(m => m.Material.Spec.SurfacePattern is null).ToArray();
        AssertThat(plain.All(m => m.Uv is null && m.Uv2 is null)).OverrideFailureMessage("Plain chunks keep the legacy vertex layout.").IsTrue();
        AssertThat(plain.Any(m => m.Material.Spec == GroundPalette.Spec(GroundMaterial.Plaza))).OverrideFailureMessage("Other plaza tops stay plain granite.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void OnlyLibraryEntrancesSelectAForecourt()
    {
        var meshes = Build(PavingWorld.Library("museum", 90f));
        AssertThat(meshes.All(m => m.Material.Spec.SurfacePattern is null)).OverrideFailureMessage("Other building categories do not pave their forecourts (yet).").IsTrue();
        var away = Build(PavingWorld.Library("library", 270f));
        AssertThat(away.All(m => m.Material.Spec.SurfacePattern is null)).OverrideFailureMessage("An entrance facing away from every plaza binds nothing.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FarPrecisionZonesSwitchTheGateOff()
    {
        var world = PavingWorld.Library("library", 90f, x => x > 25f ? FidelityZone.K4 : FidelityZone.K1);
        foreach (var mesh in Build(world).Where(m => m.Material.Spec.SurfacePattern is not null))
            for (var i = 0; i < mesh.Index.Length; i += 3)
            {
                var c = Centre(mesh, i);
                var v = mesh.Index[i] * 2;
                var expected = c.X > 25f ? (0f, 4f) : (1f, 1f);
                AssertThat((mesh.Uv2![v], mesh.Uv2[v + 1]) == expected).OverrideFailureMessage($"Gate/K at {c} must follow the exported zone.").IsTrue();
            }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void MainAxisIsTheLongestRunAcrossThePlaza()
    {
        // A second entrance reaches the same plaza from the side, across a short run: the façade axis stays the main axis.
        var world = PavingWorld.Library("library", 90f, side: true);
        var mesh = Build(world).First(m => m.Material.Spec.SurfacePattern is not null);
        AssertThat(Math.Abs((double)(mesh.Position[2]) - (double)(mesh.Uv![0])) <= TestTolerance.Position).OverrideFailureMessage($"U stays measured from the long east axis: expected {mesh.Position[2]}, got {mesh.Uv![0]}.").IsTrue();
    }

    private static IReadOnlyList<GeometryBundle> Build(FakeWorld world)
    {
        var ctx = TestWorld.Context(world);
        var meshes = new List<GeometryBundle>();
        foreach (var tile in new[] { new TileKey(0, 0), new TileKey(0, -1) })
            meshes.AddRange(new GroundModule().Build(ctx, tile, LodLevel.L0).Meshes);
        return meshes;
    }

    private static Vector2 Centre(GeometryBundle mesh, int i)
    {
        var c = Vector2.Zero;
        for (var k = 0; k < 3; k++) c += new Vector2(mesh.Position[3 * mesh.Index[i + k]], mesh.Position[3 * mesh.Index[i + k] + 2]);
        return c / 3f;
    }

    private static class PavingWorld
    {
        public static FakeWorld Library(string category, float yaw, Func<float, FidelityZone>? zone = null, bool side = false)
        {
            var building = WorldFeatureFixture.Create("fixture:hall", "building", new PolygonGeometry(Rect(-20f, -15f, 0f, 15f)),
                $$"""{"kind":"building","category":"{{category}}"}""", FeatureSource.Osm, height: 20f);
            const string pedestrian = """{"kind":"road","roadType":"pedestrian"}""";
            var features = new List<WorldFeature>
            {
                building,
                WorldFeatureFixture.Create("fixture:forecourt", "road", new PolygonGeometry(Rect(10f, -20f, 60f, 20f)), pedestrian, FeatureSource.Osm),
                WorldFeatureFixture.Create("fixture:place", "road", new PolygonGeometry(Rect(30f, -5f, 40f, 5f)), pedestrian, FeatureSource.Osm),
                WorldFeatureFixture.Create("fixture:neighbour", "road", new PolygonGeometry(Rect(10f, 30f, 60f, 50f)), pedestrian, FeatureSource.Osm),
            };
            var entrances = new List<EntranceDecl> { Entrance("hall.main", Door, yaw) };
            if (side) entrances.Add(Entrance("hall.side", new(20f, -25f), 180f));   // faces south across the forecourt (40 m run)
            return new FakeWorld
            {
                Boundary = new([new(-200f, -200f), new(200f, -200f), new(200f, 200f), new(-200f, 200f)]),
                Features = features.ToDictionary(f => f.Id),
                Interiors = new FakeInteriors(new InteriorIndexEntry(building.Id, [], entrances, true, null)),
                Fidelity = zone is { } zoneAt ? new FakeFidelity(groundAt: (x, _) => zoneAt(x), walkDistance: 15f) : null,
            };
        }

        private static EntranceDecl Entrance(string id, Vector2 at, float yaw) =>
            new(id, new(at.X, 0f, at.Y), new(at.X, 0f, at.Y), 1.2f, true) { Center = new(at.X, 0f, at.Y), YawDeg = yaw };

        private static PolygonRings Rect(float x0, float z0, float x1, float z1) =>
            new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

    }
}
