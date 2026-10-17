using Campus.Camera.Walk;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Bake;
using Campus.Kernel.Maps;
using Campus.Kernel.Registry;
using Campus.Kernel.Renderer;
using GdUnit4;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Collision.Tests;

/// <summary>The existing G15 core scope, over the same map inputs and generators as the running game.</summary>
[TestSuite]
public sealed partial class CollisionCoreTests
{
    private const int SampleCount = 2000;
    private const double MountSliceBudgetMilliseconds = .3;
    private readonly List<string> _failures = [];
    private readonly Dictionary<string, int> _failureSamples = new(StringComparer.Ordinal);
    private int _failureCount;

    [TestCase(Timeout = 1200000)]
    [TestCategory("G15Core")]
    [RequireGodotRuntime]
    public async Task FormalMapGeometryAndCylinders()
    {
        GD.Print("G15Core begin formal map input loading");
        Require(Engine.GetMainLoop() is SceneTree && OS.GetThreadCallerId() == OS.GetMainThreadId(),
            "G15 core requires the existing Godot test runner on the main thread.");
        Require(Engine.PhysicsTicksPerSecond == 120, "G15 requires the production 120 Hz physics configuration.");
        Require(ProjectSettings.GetSetting("physics/3d/physics_engine").AsString() == "Jolt Physics",
            "G15 requires the configured Jolt backend.");
        var tree = (SceneTree)Engine.GetMainLoop();
        var source = new MapDataSource();
        var catalog = await source.ReadCatalog();
        GD.Print("G15Core map catalog loaded");
        var requested = System.Environment.GetEnvironmentVariable("UESTC_G15_MAP");
        var id = string.IsNullOrWhiteSpace(requested) ? catalog.Default : new MapId(requested);
        Require(catalog.TryGet(id, out var entry), "G15 requested an unknown map: " + id.Value);
        var claims = source.ReadClaims(id);
        var input = await source.PrepareLoadInput(source.ReadLoadInput(entry));
        var loaded = await Task.Run(() => MapWorldLoading.LoadPrepared(new WorldModelLoaderAdapter(), input, claims));
        GD.Print("G15Core formal world loaded");
        var context = MapWorldLoading.CreateContext(entry, loaded, input.SourceInputs);
        var walk = context.World.Walk ?? throw new InvalidDataException("G15 requires the actual resident walk bake, not a fallback mask.");
        walk.Validate();
        var repository = System.Environment.GetEnvironmentVariable("UESTC_REPOSITORY")
            ?? throw new InvalidOperationException("The existing test runner must supply UESTC_REPOSITORY.");
        using var bake = new BakeService(new(Path.Combine(repository, "output", "build", "gates", "G15Core-cache"),
            Live: false, Strict: false, Background: false, AllowUnlisted: true, Reference: false, Verify: false,
            MapOnly: false, LogicalProcessors: 3, SeedOverride: null));
        var map = bake.Mount(context);
        GD.Print($"G15Core freezing generation inputs map={id.Value}");
        _ = await Task.Run(() => map.Data.Value);
        CaptureParts(id);
        var products = await Task.Run(() => BuildAndCheck(bake, map));
        GD.Print($"G15Core map={id.Value} tiles={products.Count} geometryFailures={_failureCount}");
        foreach (var failure in _failures) GD.Print($"G15Core geometryFailure {failure}");
        await CheckCylinders(tree, context, walk, products);
        Require(_failureCount == 0, $"G15 core failed {_failureCount} assertions:\n" + string.Join('\n', _failures));
    }

    private SortedDictionary<TileKey, CollisionTile> BuildAndCheck(BakeService bake, BakeService.MapBuild map)
    {
        var result = new SortedDictionary<TileKey, CollisionTile>(Comparer<TileKey>.Create((a, b) =>
            a.Z != b.Z ? a.Z.CompareTo(b.Z) : a.X.CompareTo(b.X)));
        var ground = map.Modules.Single(pair => pair.Value.Value is Campus.Gen.Ground.GroundModule);
        var groundContext = map.CreateBuildContext(ground.Value.CodeHash);
        var groundTiles = ground.Value.Value.Tiles(groundContext).ToHashSet();
        var descriptor = map.Context.Descriptor;
        GD.Print($"G15Core building C range={descriptor.TileMin}..{descriptor.TileMax}");
        for (var z = descriptor.TileMin.Z; z <= descriptor.TileMax.Z; z++)
            for (var x = descriptor.TileMin.X; x <= descriptor.TileMax.X; x++)
            {
                var tile = new TileKey(x, z);
                var product = bake.Build(map, CollisionBakeProducer.Key(descriptor.Id, tile)) as CollisionTile
                    ?? throw new InvalidDataException("The formal collision producer returned another product.");
                Require(product.Tile == tile && product.Key.Map == descriptor.Id, "Collision product identity mismatch.");
                result.Add(tile, product);
                if (groundTiles.Contains(tile))
                {
                    var visible = bake.Build(map, BakeRequestKeys.Tile(descriptor.Id, ground.Key, tile, LodLevel.L0)) as TileBundle
                        ?? throw new InvalidDataException("The formal ground producer returned another product.");
                    CheckGround(product, visible);
                }
                if (result.Count % 128 == 0) GD.Print($"G15Core C/L0 checked tiles={result.Count}");
            }
        GD.Print($"G15Core built C tiles={result.Count}; checking building edges and source expansion");
        CheckBuildingEdges(map.Context, result);
        CheckSourceExpansion(map);
        return result;
    }

    private static IReadOnlyList<N.Vector3> SampleMask(WalkBakeData data)
    {
        var grid = data.Walk;
        var cells = Enumerable.Range(0, grid.Classes.Length).Where(i => (WalkClass)grid.Classes[i] is
            WalkClass.Walk or WalkClass.Interior or WalkClass.Wade).ToArray();
        Require(cells.Length != 0, "The real walk mask contains no supported cells.");
        var random = new Rng(0);
        var points = new N.Vector3[SampleCount];
        for (var i = 0; i < points.Length; i++)
        {
            var cell = cells[random.NextInt(cells.Length)];
            points[i] = new(grid.Origin.X + (cell % grid.Width + random.NextFloat()) * WalkGrid.CellSize,
                grid.SupportY[cell], grid.Origin.Y + (cell / grid.Width + random.NextFloat()) * WalkGrid.CellSize);
        }
        return points;
    }

    private void Fail(string message, [System.Runtime.CompilerServices.CallerMemberName] string check = "")
    {
        _failureCount++;
        var count = _failureSamples.GetValueOrDefault(check);
        _failureSamples[check] = count + 1;
        if (count < 3) _failures.Add($"[{check}] {message}");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
