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

    private async Task CheckCylinders(SceneTree tree, IMapContext context, WalkBakeData walk,
        IReadOnlyDictionary<TileKey, CollisionTile> products)
    {
        var samples = SampleMask(walk);
        using var native = new World3D();
        var viewport = new SubViewport { World3D = native, Size = new(32, 32) };
        var root = new Node3D();
        var collision = new CollisionWorld();
        var characters = new List<CharacterBody3D>();
        using var shape = new CylinderShape3D { Radius = WalkTuning.Radius, Height = WalkTuning.StandingHeight };
        using var query = new PhysicsShapeQueryParameters3D
        {
            Shape = shape,
            Margin = 0,
            CollisionMask = CollisionWorld.WalkMask,
            CollideWithAreas = false,
            CollideWithBodies = true
        };
        try
        {
            tree.Root.AddChild(viewport); viewport.AddChild(root);
            Require(root.GetWorld3D().Space == native.Space && native.Space != tree.Root.FindWorld3D().Space,
                "G15 fixture shares the application's physics space.");
            collision.Attach(root, context);
            var needed = samples.SelectMany(sample => Neighbors(Campus.Gen.Collision.CollisionGeometry.TileAt(sample)))
                .Distinct().OrderBy(tile => tile.Z).ThenBy(tile => tile.X).ToArray();
            GD.Print($"G15Core preparing cylinders={samples.Count} collisionTiles={needed.Length}");
            var mounted = 0;
            foreach (var tile in needed)
            {
                if (!products.TryGetValue(tile, out var product))
                    throw new InvalidDataException("A sampled mask neighborhood lies outside real collision data: " + tile);
                var work = new CollisionMountWork(collision, product, 0);
                while (!work.Step(MountSliceBudgetMilliseconds)) await root.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                if (++mounted % 128 == 0) GD.Print($"G15Core cylinder collisionTiles mounted={mounted}/{needed.Length}");
            }
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            foreach (var sample in samples)
            {
                Require(collision.Ready(sample, WalkTuning.Radius), "A real sampled 3x3 neighborhood is not ready: " + sample);
                var character = new CharacterBody3D
                {
                    Position = (sample + N.Vector3.UnitY * WalkTuning.LandingLift).ToGodot(),
                    CollisionLayer = 0,
                    CollisionMask = CollisionWorld.WalkMask,
                    FloorMaxAngle = WalkTuning.FloorAngleDegrees * float.Pi / 180,
                    FloorSnapLength = WalkTuning.SnapLength,
                    FloorConstantSpeed = true,
                    FloorStopOnSlope = true,
                    FloorBlockOnWall = true,
                    SafeMargin = WalkTuning.SafeMargin,
                    MaxSlides = 4,
                    PhysicsInterpolationMode = Node.PhysicsInterpolationModeEnum.Off
                };
                character.AddChild(new CollisionShape3D { Shape = shape, Position = Vector3.Up * (WalkTuning.StandingHeight / 2) });
                root.AddChild(character); characters.Add(character);
            }
            var deepest = new float[samples.Count];
            GD.Print($"G15Core simulating cylinders={characters.Count} physicsSteps={Engine.PhysicsTicksPerSecond}");
            for (var i = 0; i < characters.Count; i++) RecordPenetration(i);
            for (var step = 0; step < Engine.PhysicsTicksPerSecond; step++)
            {
                await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                for (var i = 0; i < characters.Count; i++)
                {
                    var character = characters[i];
                    character.Velocity += Vector3.Down * (WalkTuning.Gravity / Engine.PhysicsTicksPerSecond);
                    character.MoveAndSlide();
                    RecordPenetration(i);
                }
            }
            for (var i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                if (deepest[i] > WalkTuning.SafeMargin)
                    Fail($"sample={i} maskFeet={samples[i]} penetration={deepest[i]:R}m exceeds SafeMargin={WalkTuning.SafeMargin:R}m.");
                if (!character.IsOnFloor()) Fail($"sample={i} maskFeet={samples[i]} finalFeet={character.GlobalPosition} did not ground within 120 actual physics frames.");
            }
            GD.Print($"G15Core cylinders={characters.Count} steps={Engine.PhysicsTicksPerSecond} failures={_failureCount}");

            void RecordPenetration(int i)
            {
                query.Transform = new(Basis.Identity, characters[i].GlobalPosition + Vector3.Up * (WalkTuning.StandingHeight / 2));
                var contacts = native.DirectSpaceState.CollideShape(query, 64);
                Require(contacts.Count % 2 == 0, "Jolt returned incomplete contact pairs.");
                if (contacts.Count == 128) Fail($"sample={i} feet={samples[i]} contact buffer saturated; penetration is not fully measured.");
                for (var contact = 0; contact < contacts.Count; contact += 2)
                    deepest[i] = MathF.Max(deepest[i], contacts[contact].DistanceTo(contacts[contact + 1]));
            }
        }
        finally
        {
            collision.Unload();
            if (GodotObject.IsInstanceValid(viewport)) viewport.Free();
        }
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

    private static IEnumerable<TileKey> Neighbors(TileKey center)
    {
        for (var z = center.Z - 1; z <= center.Z + 1; z++)
            for (var x = center.X - 1; x <= center.X + 1; x++) yield return new(x, z);
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
