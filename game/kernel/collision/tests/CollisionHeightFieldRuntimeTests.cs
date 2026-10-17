using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Gen.Collision;
using Campus.Kernel.Collision;
using GdUnit4;
using Godot;
using N = System.Numerics;

namespace Campus.Tests;

[TestSuite]
public sealed class CollisionHeightFieldRuntimeTests
{
    [TestCase(Timeout = 30000)]
    [TestCategory("G15Core")]
    [RequireGodotRuntime]
    public async Task WorldPostsMountAtTheirActualPositiveAndNegativeTiles()
    {
        var tree = Engine.GetMainLoop() as SceneTree ?? throw new InvalidOperationException("Native scene tree required.");
        using var native = new World3D();
        var viewport = new SubViewport { World3D = native, Size = new(32, 32) };
        var root = new Node3D();
        var world = new CollisionWorld();
        try
        {
            tree.Root.AddChild(viewport); viewport.AddChild(root); world.Attach(root, FixtureContext.Create());
            foreach (var tile in new TileKey[] { new(3, 2), new(-4, -3) })
            {
                var at = new N.Vector2(tile.X * 64 + 12, tile.Z * 64 + 24);
                var post = new PostCollider(at, .4f, 0, 3, SurfaceTag.Barrier);
                world.BeginTile(tile);
                world.MountBody(tile, CollisionGeometry.Localize([post], tile));
                world.CompleteTile(tile);
            }
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            foreach (var tile in new TileKey[] { new(3, 2), new(-4, -3) })
            {
                var at = new N.Vector3(tile.X * 64 + 12, .01f, tile.Z * 64 + 24);
                var hit = world.Sweep(at - N.Vector3.UnitX * 2, at + N.Vector3.UnitX * 2, .3f, 1.8f);
                Require(hit is { Tag: SurfaceTag.Barrier } && hit.Value.T > 0 && hit.Value.T < 1,
                    "A world-coordinate post was shifted away from its actual tile: " + tile);
            }
        }
        finally { world.Unload(); if (GodotObject.IsInstanceValid(viewport)) viewport.Free(); }
    }

    [TestCase(Timeout = 30000)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task NativeShapesWaitForCompleteNeighborhoodAndUnloadTogether()
    {
        if (Engine.GetMainLoop() is not SceneTree tree || OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Collision tests require Godot's scene tree and main thread.");
        using var nativeWorld = new World3D();
        var viewport = new SubViewport { World3D = nativeWorld, Size = new(32, 32) };
        var root = new Node3D();
        var world = new CollisionWorld();
        try
        {
            tree.Root.AddChild(viewport);
            viewport.AddChild(root);
            Require(root.GetWorld3D().Space == nativeWorld.Space && nativeWorld.Space != tree.Root.FindWorld3D().Space,
                "Native collision fixture shares the application physics space.");
            var context = FixtureContext.Create();
            world.Attach(root, context);
            for (var x = -1; x <= 1; x++)
                for (var z = -1; z <= 1; z++)
                    if (x != 0 || z != 0) { world.BeginTile(new(x, z)); world.CompleteTile(new(x, z)); }
            var feet = new N.Vector3(8, .01f, 8);
            Require(!world.Ready(feet, .3f), "A missing center collision tile became ready.");
            var colliders = await Task.Run(() => CollisionGeometry.Localize(Fixture(), new(0, 0)));
            var key = CollisionBakeProducer.Key(context.Descriptor.Id, new(0, 0));
            var tile = new CollisionTile(key, new(0, 0), new(new(0, -5, 0), new(64, 10, 64)), colliders);
            var mount = new CollisionMountWork(world, tile, 0);
            while (!mount.Step(.3))
            {
                Require(!world.Ready(feet, .3f), "A partially mounted collision tile became ready.");
                await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            }
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            Require(world.Ready(feet, .3f), "The mounted 3x3 neighborhood is not ready.");
            foreach (var body in root.GetChildren().OfType<StaticBody3D>())
                Require(PhysicsServer3D.BodyGetSpace(body.GetRid()) == nativeWorld.Space, "A body entered another physics space.");
            for (var i = 0; i < 2000; i++)
            {
                var x = 1f + i % 50 * .2f;
                var z = 1f + i / 50 * .2f;
                CheckSupport(x, z, 0, SurfaceTag.Ground);
            }
            var character = new CharacterBody3D
            {
                Position = new(8, .3f, 8),
                CollisionLayer = 0,
                CollisionMask = CollisionWorld.WalkMask,
                SafeMargin = .01f,
                FloorSnapLength = .5f
            };
            character.AddChild(new CollisionShape3D
            {
                Shape = new CylinderShape3D { Radius = .3f, Height = 1.8f },
                Position = Vector3.Up * .9f
            });
            root.AddChild(character);
            for (var step = 0; step < 120; step++)
            {
                await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                character.Velocity += Vector3.Down * (27.8f / 120);
                character.MoveAndSlide();
                Require(character.Position.Y >= -.02f, "The walking cylinder penetrated the mounted ground.");
            }
            Require(character.IsOnFloor() && MathF.Abs(character.Position.Y) <= .02f,
                "The walking cylinder did not settle on native ground within one second.");
            character.Free();
            CheckSweep(new(18, .01f, 24), new(22, .01f, 24), SurfaceTag.Wall);
            CheckSweep(new(28, .01f, 24), new(32, .01f, 24), SurfaceTag.Barrier);
            CheckSweep(new(38, .01f, 24), new(42, .01f, 24), SurfaceTag.Prop);
            CheckSupport(50, 24, 1, SurfaceTag.Prop);
            foreach (var point in new N.Vector2[] { new(22, 42), new(21.998f, 42), new(22.002f, 42), new(22, 41.998f), new(22, 42.002f) })
                CheckSupport(point.X, point.Y, 2, SurfaceTag.Ground);
            CheckSupport(31, 42, 1, SurfaceTag.Stair);
            CheckSweep(new(33, .01f, 42), new(37, .01f, 42), SurfaceTag.Barrier);
            Require(world.Sweep(new(33, 2.8f, 42), new(37, 2.8f, 42), .3f, 1.8f) is null,
                "Sloped wall fills space above its top.");
            var mounted = world.Diagnostics;
            Require(mounted.Bodies > 0 && mounted.MaxBodyShapes <= 250 && mounted.RegisteredBodies == mounted.Bodies
                && mounted.MainThreadViolations == 0 && mounted.IncompleteMounts == 0, "Invalid atomic collision mounting.");
            world.Unload();
            var unloaded = world.Diagnostics;
            Require(unloaded.Bodies == 0 && unloaded.Shapes == 0 && unloaded.Tiles == 0 && unloaded.RegisteredBodies == 0
                && unloaded.RegisteredSlabShapes == 0 && !world.Ready(feet, .3f), "Unload retained native collision state.");

            void CheckSupport(float x, float z, float y, SurfaceTag tag)
            {
                var hit = world.SupportY(x, z, y + .1f);
                Require(hit is { } support && support.Tag == tag && MathF.Abs(support.Y - y) <= .02f,
                    $"Incorrect {tag} support at ({x}, {z}): expected {y}, actual {hit?.Y}.");
            }
            void CheckSweep(N.Vector3 from, N.Vector3 to, SurfaceTag tag)
            {
                var hit = world.Sweep(from, to, .3f, 1.8f);
                Require(hit is { } block && block.Tag == tag && block.T > 0 && block.T < 1 && block.Normal.X < -.8f,
                    $"Cylinder sweep missed {tag}.");
            }
        }
        finally
        {
            world.Unload();
            if (GodotObject.IsInstanceValid(viewport)) viewport.Free();
        }
    }

    private static IReadOnlyList<Collider> Fixture() =>
    [
        new HeightFieldCollider(new(0, 0), new short[65 * 65], SurfaceTag.Ground),
        new WallCollider(new(20, 20), new(20, 28), .1f, 0, 3, SurfaceTag.Wall),
        new WallCollider(new(30, 20), new(30, 28), .1f, 0, 1.5f, SurfaceTag.Barrier),
        new PostCollider(new(40, 24), .4f, 0, 3, SurfaceTag.Prop),
        new BoxCollider(N.Matrix4x4.CreateTranslation(50, .5f, 24), new(1, .5f, 1), SurfaceTag.Prop),
        new SlabCollider(new N.Vector2[] { new(20, 40), new(24, 40), new(24, 44), new(20, 44) }, 2, .2f, SurfaceTag.Ground),
        new RampCollider(new(30, 0, 40), new(32, 0, 40), new(32, 2, 44), new(30, 2, 44), .2f, SurfaceTag.Stair),
        new SlopedWallCollider(new(35, 40), new(35, 44), .1f, 0, 1.5f, 2, 3.5f, SurfaceTag.Barrier)
    ];

    private sealed record FixtureContext(MapDescriptor Descriptor) : IMapContext
    {
        public static FixtureContext Create() => new(new MapDescriptor(
            new("fixture-collision-jolt"), new("原生碰撞测试", "Native collision fixture"), MapKind.Instance, 1, 1,
            new(0, 0, 0, new(-128, -128), new(128, 128)), N.Vector2.Zero,
            new(new(-128, -5, -128), new(128, 10, 128)), new(new(-128, -128), new(128, 128)),
            new(1, 0, 128, -5, 10, 0, 0, 0), new(-2, -2), new(1, 1), "fixture", [],
            new(MapTimeRule.Shared, new(12, Season.Autumn), new("测试", "Fixture"), false, "fixture-weather"),
            new("fixture-no-sky", "fixture-no-backdrop", []), new Dictionary<string, string>(), []));
        public BuildInputSnapshot SourceInputs => BuildInputSnapshot.Empty;
        public IWorldModel World => throw new NotSupportedException();
        public string DataRoot => throw new NotSupportedException();
        public string BakeRoot => throw new NotSupportedException();
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
