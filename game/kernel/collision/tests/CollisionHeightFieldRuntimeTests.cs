using Campus.Kernel.Stream;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Gen.Collision;
using Campus.Kernel.Collision;
using GdUnit4;
using Godot;
using N = System.Numerics;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class CollisionHeightFieldRuntimeTests
{
    // 本组建的碰撞形状要等包装终结才归还原生引用；整组跑完在这里收齐，单跑本组时才不漏到引擎退出（DroppedNatives）。
    [After]
    public void ReleaseDroppedNatives() => DroppedNatives.Release();

    [TestCase(Timeout = 30000)]
    [TestCategory(TestReason.Smoke)]
    [TestCategory("G15Core")]
    [RequireGodotRuntime]
    public async Task WorldBoxesMountAtTheirActualTiles()
    {
        var tree = Engine.GetMainLoop() as SceneTree ?? throw new InvalidOperationException("Native scene tree required.");
        using var native = new World3D();
        var viewport = new SubViewport { World3D = native, Size = new(32, 32) };
        var root = new Node3D();
        var world = new CollisionWorld();
        var owners = new List<object>();
        try
        {
            tree.Root.AddChild(viewport); viewport.AddChild(root); world.Attach(root, FixtureContext());
            foreach (var tile in new TileKey[] { new(3, 2), new(-4, -3) })
            {
                var at = new N.Vector2(tile.X * 64 + 12, tile.Z * 64 + 24);
                var post = new PostCollider(at, .4f, 0, 3, SurfaceTag.Barrier);
                var owner = world.BeginTile(tile); owners.Add(owner);
                var colliders = CollisionGeometry.Localize([post], tile);
                while (!world.MountBodyStep(owner, colliders, .3)) await FrameAwaiter.ProcessAsync(root);
                world.CompleteTile(owner);
            }
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            foreach (var tile in new TileKey[] { new(3, 2), new(-4, -3) })
            {
                var at = new N.Vector3(tile.X * 64 + 12, .01f, tile.Z * 64 + 24);
                var hit = world.Sweep(at - N.Vector3.UnitX * 2, at + N.Vector3.UnitX * 2, .3f, 1.8f);
                AssertThat(hit is { Tag: SurfaceTag.Barrier } && hit.Value.T > 0 && hit.Value.T < 1).OverrideFailureMessage("A world-coordinate post was shifted away from its actual tile: " + tile).IsTrue();
            }
        }
        finally
        {
            world.Unload();
            foreach (var owner in owners) while (!world.RemoveTileStep(owner, .3)) await FrameAwaiter.ProcessAsync(root);
            if (GodotObject.IsInstanceValid(viewport)) viewport.Free();
        }
    }

    [TestCase(Timeout = 30000)]
    [TestCategory(TestReason.Contract)]
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
        var owners = new List<object>();
        try
        {
            tree.Root.AddChild(viewport);
            viewport.AddChild(root);
            AssertThat(root.GetWorld3D().Space == nativeWorld.Space && nativeWorld.Space != tree.Root.FindWorld3D().Space).OverrideFailureMessage("Native collision fixture shares the application physics space.").IsTrue();
            var context = FixtureContext();
            world.Attach(root, context);
            for (var x = -1; x <= 1; x++)
                for (var z = -1; z <= 1; z++)
                    if (x != 0 || z != 0) { var owner = world.BeginTile(new(x, z)); owners.Add(owner); world.CompleteTile(owner); }
            var feet = new N.Vector3(8, .01f, 8);
            AssertThat(!world.Ready(feet, .3f)).OverrideFailureMessage("A missing center collision tile became ready.").IsTrue();
            var colliders = await Task.Run(() => CollisionGeometry.Localize(Fixture(), new(0, 0)));
            var key = CollisionBakeProducer.Key(context.Descriptor.Id, new(0, 0));
            var tile = new CollisionTile(key, new(0, 0), new(new(0, -5, 0), new(64, 10, 64)), colliders);
            var mount = new CollisionMountWork(world, tile, 0);
            while (!mount.Step(.3))
            {
                AssertThat(!world.Ready(feet, .3f)).OverrideFailureMessage("A partially mounted collision tile became ready.").IsTrue();
                await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            }
            owners.Add(mount.Owner!);
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            AssertThat(world.Ready(feet, .3f)).OverrideFailureMessage("The mounted 3x3 neighborhood is not ready.").IsTrue();
            foreach (var body in root.GetChildren().OfType<StaticBody3D>())
                AssertThat(PhysicsServer3D.BodyGetSpace(body.GetRid()) == nativeWorld.Space).OverrideFailureMessage("A body entered another physics space.").IsTrue();
            CheckSupport(8, 8, 0, SurfaceTag.Ground);
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
                AssertThat(character.Position.Y >= -.02f).OverrideFailureMessage("The walking cylinder penetrated the mounted ground.").IsTrue();
            }
            AssertThat(character.IsOnFloor() && MathF.Abs(character.Position.Y) <= TestTolerance.Position).OverrideFailureMessage("The walking cylinder did not settle on native ground within one second.").IsTrue();
            character.Free();
            CheckSweep(new(18, .01f, 24), new(22, .01f, 24), SurfaceTag.Wall);
            CheckSweep(new(28, .01f, 24), new(32, .01f, 24), SurfaceTag.Barrier);
            CheckSweep(new(38, .01f, 24), new(42, .01f, 24), SurfaceTag.Prop);
            CheckSupport(50, 24, 1, SurfaceTag.Prop);
            CheckSupport(22, 42, 2, SurfaceTag.Ground);
            CheckSupport(31, 42, 1, SurfaceTag.Stair);
            CheckSweep(new(33, .01f, 42), new(37, .01f, 42), SurfaceTag.Barrier);
            var boundary = Fixture().OfType<WallCollider>().Single(wall => wall.Tag == SurfaceTag.Boundary);
            var boundaryMiddle = (boundary.A + boundary.B) * .5f;
            float clearance = WalkRules.BoundarySlowdownDistance * .5f;
            var approaching = new N.Vector3(boundaryMiddle.X - boundary.HalfT - WalkRules.Radius - clearance,
                boundary.Y0 + WalkRules.StepHeight, boundaryMiddle.Y);
            var boundaryHits = new BoundaryHit[8];
            int boundaryCount = world.NearbyBoundaries(approaching, WalkRules.Radius, WalkRules.StandingHeight, boundaryHits);
            AssertThat(boundaryCount > 0 && boundaryHits.Take(boundaryCount).Any(hit =>
                MathF.Abs(hit.Distance - clearance) <= TestTolerance.Position
                && N.Vector3.Distance(hit.Inward, -N.Vector3.UnitX) <= TestTolerance.FloatRelative))
                .OverrideFailureMessage("The native Boundary probe missed the actual surface clearance and inward normal.").IsTrue();
            AssertThat(world.NearbyBoundaries(approaching with { Y = boundary.Y1 + WalkRules.StepHeight },
                WalkRules.Radius, WalkRules.StandingHeight, boundaryHits) == 0)
                .OverrideFailureMessage("A Boundary below the cylinder still slowed airborne motion.").IsTrue();
            var barrier = Fixture().OfType<WallCollider>().Single(wall => wall.Tag == SurfaceTag.Barrier);
            var barrierMiddle = (barrier.A + barrier.B) * .5f;
            var ordinaryBarrier = new N.Vector3(barrierMiddle.X - barrier.HalfT - WalkRules.Radius - clearance,
                barrier.Y0 + WalkRules.StepHeight, barrierMiddle.Y);
            AssertThat(world.NearbyBoundaries(ordinaryBarrier, WalkRules.Radius, WalkRules.StandingHeight, boundaryHits) == 0)
                .OverrideFailureMessage("An ordinary campus Barrier was classified as a slowdown boundary.").IsTrue();
            var mounted = world.Diagnostics;
            AssertThat(mounted.Bodies > 0 && mounted.MaxBodyShapes <= 250 && mounted.RegisteredBodies == mounted.Bodies
                && mounted.MainThreadViolations == 0 && mounted.IncompleteMounts == 0).OverrideFailureMessage("Invalid atomic collision mounting.").IsTrue();
            var mountedBodies = root.GetChildren().OfType<StaticBody3D>().ToArray();
            var original = new InvalidDataException("collision tile failed");
            world.FailTile(mount.Owner!, original);
            AssertThat(ReferenceEquals(world.Failure, original) && !world.Ready(feet, .3f)).OverrideFailureMessage("Collision failure lost its original exception or left its tile ready.").IsTrue();
            AssertThat(mountedBodies.Any(GodotObject.IsInstanceValid)).OverrideFailureMessage("FailTile synchronously destroyed its retained owner.").IsTrue();
            while (!mount.ReleaseStep(.3)) await FrameAwaiter.ProcessAsync(root);
            AssertThat(mountedBodies.All(body => !GodotObject.IsInstanceValid(body))).OverrideFailureMessage("Removing a tile retained its native bodies.").IsTrue();
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            AssertThat(world.SupportY(8, 8, 1) is null).OverrideFailureMessage("Removed height-field collision is still present.").IsTrue();
            world.Unload();
            foreach (var owner in owners) while (!world.RemoveTileStep(owner, .3)) await FrameAwaiter.ProcessAsync(root);
            var unloaded = world.Diagnostics;
            AssertThat(ReferenceEquals(world.Failure, original)).OverrideFailureMessage("Unload lost the original collision failure.").IsTrue();
            AssertThat(unloaded.Bodies == 0 && unloaded.Shapes == 0 && unloaded.Tiles == 0 && unloaded.RegisteredBodies == 0
                && !world.Ready(feet, .3f)).OverrideFailureMessage("Unload retained native collision state.").IsTrue();

            void CheckSupport(float x, float z, float y, SurfaceTag tag)
            {
                var hit = world.SupportY(x, z, y + .1f);
                AssertThat(hit is { } support && support.Tag == tag && MathF.Abs(support.Y - y) <= TestTolerance.Position).OverrideFailureMessage($"Incorrect {tag} support at ({x}, {z}): expected {y}, actual {hit?.Y}.").IsTrue();
            }
            void CheckSweep(N.Vector3 from, N.Vector3 to, SurfaceTag tag)
            {
                var hit = world.Sweep(from, to, .3f, 1.8f);
                AssertThat(hit is { } block && block.Tag == tag && block.T > 0 && block.T < 1 && block.Normal.X < -.8f).OverrideFailureMessage($"Cylinder sweep missed {tag}.").IsTrue();
            }
        }
        finally
        {
            world.Unload();
            foreach (var owner in owners) while (!world.RemoveTileStep(owner, .3)) await FrameAwaiter.ProcessAsync(root);
            if (GodotObject.IsInstanceValid(viewport)) viewport.Free();
        }
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task HalfBuiltCancelledBodyKeepsOwnershipAndCannotRemoveANewerTile()
    {
        var tree = (SceneTree)Engine.GetMainLoop(); var root = new Node3D(); tree.Root.AddChild(root);
        var world = new CollisionWorld(); var context = FixtureContext(); world.Attach(root, context);
        var tile = new TileKey(0, 0); var key = CollisionBakeProducer.Key(context.Descriptor.Id, tile);
        var content = new CollisionTile(key, tile, context.Descriptor.Bounds,
            CollisionGeometry.Localize(Fixture(), tile));
        var old = new CollisionMountWork(world, content, 0);
        object? current = null;
        try
        {
            AssertThat(!old.Step(double.Epsilon) && world.Diagnostics.Bodies > 0 && root.GetChildCount() == 0).IsTrue();
            old.Cancel();
            AssertThat(world.Diagnostics.Bodies > 0 && !world.IsTileReady(tile)).IsTrue();
            current = world.BeginTile(tile); world.CompleteTile(current);
            while (!old.ReleaseStep(0)) await FrameAwaiter.ProcessAsync(root);
            AssertThat(world.IsTileReady(tile)).OverrideFailureMessage("Retiring an old token removed the current same-key owner.").IsTrue();
        }
        finally
        {
            world.Unload();
            while (!old.ReleaseStep(.3)) await FrameAwaiter.ProcessAsync(root);
            if (current is not null) while (!world.RemoveTileStep(current, .3)) await FrameAwaiter.ProcessAsync(root);
            root.Free();
        }
    }

    private static IReadOnlyList<Collider> Fixture() =>
    [
        new HeightFieldCollider(new(0, 0), new short[65 * 65], SurfaceTag.Ground),
        new WallCollider(new(20, 20), new(20, 28), .1f, 0, 3, SurfaceTag.Wall),
        new WallCollider(new(12, 20), new(12, 28), .1f, 0, WalkRules.BoundaryHeight, SurfaceTag.Boundary),
        new WallCollider(new(30, 20), new(30, 28), .1f, 0, 1.5f, SurfaceTag.Barrier),
        new PostCollider(new(40, 24), .4f, 0, 3, SurfaceTag.Prop),
        new BoxCollider(N.Matrix4x4.CreateTranslation(50, .5f, 24), new(1, .5f, 1), SurfaceTag.Prop),
        new SlabCollider(new N.Vector2[] { new(20, 40), new(24, 40), new(24, 44), new(20, 44) }, 2, .2f, SurfaceTag.Ground),
        new RampCollider(new(30, 0, 40), new(32, 0, 40), new(32, 2, 44), new(30, 2, 44), .2f, SurfaceTag.Stair),
        new SlopedWallCollider(new(35, 40), new(35, 44), .1f, 0, 1.5f, 2, 3.5f, SurfaceTag.Barrier)
    ];

    private static TestMapContext FixtureContext() => new(new MapDescriptor(
        new("fixture-collision-jolt"), new("原生碰撞测试", "Native collision fixture"), MapKind.Instance, 1, 1,
        new(0, 0, 0, new(-128, -128), new(128, 128)), N.Vector2.Zero,
        new(new(-128, -5, -128), new(128, 10, 128)), new(new(-128, -128), new(128, 128)),
        new(1, 0, 128, -5, 10, 0, 0, 0), new(-2, -2), new(1, 1), "fixture", [],
        new(MapTimeRule.Shared, new(12, Season.Autumn), new("测试", "Fixture"), false, "fixture-weather"),
        new("fixture-no-sky", "fixture-no-backdrop", []), []));
}
