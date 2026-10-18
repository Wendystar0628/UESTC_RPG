using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Gen.Ground.Tests;

/// <summary>
/// Q-015 plaza paving: the forecourt of a library's open entrance carries the granite profile with metre UVs square to
/// the entrance, one joint on its axis; curbs, borders and other plazas stay plain, a place laid inside it continues the grid.
/// </summary>
[TestSuite]
public sealed class GroundPavingTests
{
    // Door at (0, 0) facing east (yaw 90): the axis is +X, the plaza starts 10 m out.
    private static readonly Vector2 Door = new(0f, 0f);

    [TestCase]
    public void LibraryForecourtGetsGraniteFrameOnTheEntranceAxis()
    {
        var world = PavingWorld.Library("library", 90f);
        var meshes = Build(world);
        var patterned = meshes.Where(m => m.Material.Spec.SurfacePattern is not null).ToArray();
        Check.True(patterned.Length > 0, "The library forecourt must produce patterned plaza geometry.");
        foreach (var mesh in patterned)
        {
            mesh.ValidateVertexData();   // contract: UV and UV2 present, gate/K constant per triangle
            Check.True(mesh.Material.Spec.SurfacePattern == GroundPaving.Granite, "Granite 600 profile.");
            Check.True(mesh.Material.Spec with { SurfacePattern = null } == GroundPalette.Spec(GroundMaterial.Plaza),
                "The profile is the only material difference from the plain plaza class.");
            Check.True(mesh.ColorIndices!.All(id => id == GroundPalette.Id(GroundColor.Curb)), "Granite keeps its colour.");
            for (var v = 0; v < mesh.Position.Length / 3; v++)
            {
                var x = mesh.Position[3 * v];
                var z = mesh.Position[3 * v + 2];
                // Frame: V along the axis from the plaza edge (x = 10), U across it with U = 0 on the axis line (z = 0).
                Check.Close(z, mesh.Uv![2 * v], 1e-3, "U is the signed distance from the entrance axis");
                Check.Close(x - 10f, mesh.Uv[2 * v + 1], 2e-3, "V starts at the plaza edge on the axis");
                Check.True(mesh.Uv2![2 * v] == 1f && mesh.Uv2[2 * v + 1] == 3f, "Without a fidelity model the D0 reading K3 applies.");
            }
            for (var i = 0; i < mesh.Index.Length; i += 3)
                Check.True(Centre(mesh, i).Y < 25f, "The neighbouring plaza off the axis is not patterned.");
        }
        var layout = GroundLayout.Of(world);
        var sources = new GroundSourceContext(layout.Near(new(0, -64), new(64, 64)), GroundSection.Default, 0f, layout.Paving);
        var top = new GroundFaceInput(GroundSurface.Granite, GroundFaceRole.Top, sources);
        Check.True(top.PavingAt(new(35, 0)) is { } inner && inner == top.PavingAt(new(20, 0)),
            "An unbound place laid inside the forecourt continues the forecourt grid (no seam along the triangulation).");
        Check.True(top.PavingAt(new(35, 40)) is null &&
            new GroundFaceInput(GroundSurface.Granite, GroundFaceRole.Vertical, sources).PavingAt(new(20, 0)) is null &&
            new GroundFaceInput(GroundSurface.PlazaBorder, GroundFaceRole.Top, sources).PavingAt(new(20, 0)) is null,
            "Other plazas, vertical faces and plaza borders take no frame.");
        var plain = meshes.Where(m => m.Material.Spec.SurfacePattern is null).ToArray();
        Check.True(plain.All(m => m.Uv is null && m.Uv2 is null), "Plain chunks keep the legacy vertex layout.");
        Check.True(plain.Any(m => m.Material.Spec == GroundPalette.Spec(GroundMaterial.Plaza)), "Other plaza tops stay plain granite.");
    }

    [TestCase]
    public void OnlyLibraryEntrancesSelectAForecourt()
    {
        var meshes = Build(PavingWorld.Library("museum", 90f));
        Check.True(meshes.All(m => m.Material.Spec.SurfacePattern is null), "Other building categories do not pave their forecourts (yet).");
        var away = Build(PavingWorld.Library("library", 270f));
        Check.True(away.All(m => m.Material.Spec.SurfacePattern is null), "An entrance facing away from every plaza binds nothing.");
    }

    [TestCase]
    public void FarPrecisionZonesSwitchTheGateOff()
    {
        var world = PavingWorld.Library("library", 90f, x => x > 25f ? FidelityZone.K4 : FidelityZone.K1);
        foreach (var mesh in Build(world).Where(m => m.Material.Spec.SurfacePattern is not null))
            for (var i = 0; i < mesh.Index.Length; i += 3)
            {
                var c = Centre(mesh, i);
                var v = mesh.Index[i] * 2;
                var expected = c.X > 25f ? (0f, 4f) : (1f, 1f);
                Check.True((mesh.Uv2![v], mesh.Uv2[v + 1]) == expected, $"Gate/K at {c} must follow the exported zone.");
            }
    }

    [TestCase]
    public void MainAxisIsTheLongestRunAcrossThePlaza()
    {
        // A second entrance reaches the same plaza from the side, across a short run: the façade axis stays the main axis.
        var world = PavingWorld.Library("library", 90f, side: true);
        var mesh = Build(world).First(m => m.Material.Spec.SurfacePattern is not null);
        Check.Close(mesh.Position[2], mesh.Uv![0], 1e-3, "U stays measured from the long east axis");
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
            var building = Feature("fixture:hall", "building", Rect(-20f, -15f, 0f, 15f),
                $$"""{"kind":"building","category":"{{category}}"}""");
            const string pedestrian = """{"kind":"road","roadType":"pedestrian"}""";
            var features = new List<WorldFeature>
            {
                building,
                Feature("fixture:forecourt", "road", Rect(10f, -20f, 60f, 20f), pedestrian),
                Feature("fixture:place", "road", Rect(30f, -5f, 40f, 5f), pedestrian),
                Feature("fixture:neighbour", "road", Rect(10f, 30f, 60f, 50f), pedestrian),
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

        private static WorldFeature Feature(string id, string kind, PolygonRings rings, string props) =>
            new(new FeatureId(id), FeatureSource.Osm, new DesignKind(kind), new PolygonGeometry(rings))
            {
                Bounds = new Box3(new(rings.Outer.Min(p => p.X), 0f, rings.Outer.Min(p => p.Y)),
                    new(rings.Outer.Max(p => p.X), kind == "building" ? 20f : 0f, rings.Outer.Max(p => p.Y))),
                Props = JsonDocument.Parse(props).RootElement.Clone()
            };
    }
}
