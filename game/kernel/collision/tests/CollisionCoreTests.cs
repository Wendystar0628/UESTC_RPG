using Campus.Camera.Walk;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Gen.Collision;
using Campus.Kernel.Bake;
using Campus.Kernel.Maps;
using Campus.Kernel.Registry;
using Campus.Kernel.Renderer;
using GdUnit4;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Collision.Tests;

/// <summary>Daily G15: two authored Stand points and short walks in their formal collision neighbourhood.</summary>
[TestSuite]
public sealed class CollisionCoreTests
{
    [TestCase(Timeout = 180000)]
    [TestCategory("G15Core")]
    [RequireGodotRuntime]
    public async Task FormalMapNearbyWalking()
    {
        Require(Engine.GetMainLoop() is SceneTree && OS.GetThreadCallerId() == OS.GetMainThreadId(),
            "G15 core requires the Godot test runner on the main thread.");
        var tree = (SceneTree)Engine.GetMainLoop();
        var source = new MapDataSource();
        var catalog = await source.ReadCatalog();
        var requested = System.Environment.GetEnvironmentVariable("UESTC_G15_MAP");
        var id = string.IsNullOrWhiteSpace(requested) ? catalog.Default : new MapId(requested);
        Require(catalog.TryGet(id, out var entry), "G15 requested an unknown map: " + id.Value);
        // Fly-only viewpoints never become landing candidates.
        var route = new[] { "P01", "P08" }.Select(pointId =>
            entry.Waypoints.Single(point => point.Id == pointId)).ToArray();
        Require(route.All(point => point.Stand is not null), "The G15 short route requires both authored Stand points.");
        var claims = source.ReadClaims(id);
        var input = source.ReadLoadInput(entry);
        var loaded = await Task.Run(() => MapWorldLoading.LoadPrepared(new WorldModelLoaderAdapter(), input, claims));
        var context = MapWorldLoading.CreateContext(entry, loaded, input.SourceInputs);
        var repository = System.Environment.GetEnvironmentVariable("UESTC_REPOSITORY")
            ?? throw new InvalidOperationException("The test runner must supply UESTC_REPOSITORY.");
        var cache = System.Environment.GetEnvironmentVariable("UESTC_BAKE_CACHE")
            ?? Path.Combine(repository, "output", "cache", "bake");
        using var bake = new BakeService(new(cache, Live: false, Strict: false, Background: false,
            MapOnly: false, LogicalProcessors: 3, SeedOverride: null));
        var map = bake.Mount(context);
        using var native = new World3D();
        var viewport = new SubViewport { World3D = native, Size = new(32, 32) };
        var root = new Node3D();
        var collision = new CollisionWorld();
        var resident = new HashSet<TileKey>();
        var character = CreateCharacter();
        try
        {
            tree.Root.AddChild(viewport);
            viewport.AddChild(root);
            root.AddChild(character);
            collision.Attach(root, context);
            using var admission = new WalkPlacementAdmission(character);
            for (var index = 0; index < route.Length; index++)
            {
                var point = route[index];
                var stand = point.Stand!;
                await LoadNearby(stand.Feet);
                Require(collision.Ready(stand.Feet, WalkRules.Radius), point.Id + " collision is not Ready.");
                var landing = admission.Resolve(context.World.Spatial, collision, stand.Feet, WalkRules.StandingHeight)
                    ?? admission.Nearby(context.World.Spatial, collision, stand.Feet, WalkRules.StandingHeight);
                Require(landing is not null, point.Id + " has no legal raycast landing with cylinder clearance.");
                var feet = landing!.Value;
                character.GlobalPosition = feet.ToGodot();
                character.Velocity = Vector3.Zero;
                await MoveFor(1, N.Vector3.Zero, point.Id, feet);
                Require(character.IsOnFloor(), point.Id + " did not settle on native collision.");
                var start = character.GlobalPosition.ToNumerics();
                var destination = route[1 - index].Stand!.Feet;
                var direction = N.Vector3.Normalize(new(destination.X - feet.X, 0, destination.Z - feet.Z));
                await MoveFor(.5f, direction * WalkTuning.WalkSpeed, point.Id, start);
                await MoveFor(.5f, N.Vector3.Zero, point.Id, start);
                var finish = character.GlobalPosition.ToNumerics();
                Require(character.IsOnFloor(), point.Id + " lost the floor after its short walk.");
                Require(N.Vector2.Distance(new(start.X, start.Z), new(finish.X, finish.Z)) > WalkRules.Radius,
                    point.Id + " could not move away from its landing.");
                GD.Print($"G15Core point={point.Id} authored={stand.Feet} landing={feet} finish={finish} resident={resident.Count}");
            }
            collision.Unload();
            Require(collision.Diagnostics.Tiles == 0 && collision.Diagnostics.Bodies == 0,
                "The short route retained collision after unloading.");
            GD.Print("G15Core complete: P01/P08 landing and nearby walking only; no full route traversal.");
        }
        finally
        {
            collision.Unload();
            if (GodotObject.IsInstanceValid(viewport)) viewport.Free();
        }

        async Task LoadNearby(N.Vector3 feet)
        {
            var center = CollisionGeometry.TileAt(feet);
            var needed = new HashSet<TileKey>();
            for (var z = center.Z - 1; z <= center.Z + 1; z++)
                for (var x = center.X - 1; x <= center.X + 1; x++) needed.Add(new(x, z));
            foreach (var tile in resident.Where(tile => !needed.Contains(tile)).ToArray())
            {
                collision.RemoveTile(tile);
                resident.Remove(tile);
            }
            foreach (var tile in needed.OrderBy(tile => tile.Z).ThenBy(tile => tile.X))
            {
                if (resident.Contains(tile)) continue;
                var product = await Task.Run(() => bake.Build(map, CollisionBakeProducer.Key(id, tile))) as CollisionTile
                    ?? throw new InvalidDataException("The formal collision producer returned another product.");
                var work = new CollisionMountWork(collision, product, 0);
                while (!work.Step(.3)) await root.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                Require(work.Complete, "Nearby collision did not mount: " + work.Error);
                resident.Add(tile);
            }
            Require(resident.Count <= 9, "The short route loaded more than a 3x3 neighbourhood.");
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }

        async Task MoveFor(float seconds, N.Vector3 horizontal, string point, N.Vector3 start)
        {
            var frames = (int)MathF.Ceiling(seconds * Engine.PhysicsTicksPerSecond);
            for (var frame = 0; frame < frames; frame++)
            {
                await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                var feet = character.GlobalPosition.ToNumerics();
                Require(collision.Ready(feet, WalkRules.Radius), point + " left its Ready neighbourhood.");
                var delta = (float)character.GetPhysicsProcessDeltaTime();
                character.Velocity = new(horizontal.X, character.Velocity.Y - WalkTuning.Gravity * delta, horizontal.Z);
                character.MoveAndSlide();
                Require(character.GlobalPosition.Y >= start.Y - WalkTuning.BelowSupportLimit,
                    point + " fell through or away from its nearby floor.");
            }
        }
    }

    private static CharacterBody3D CreateCharacter()
    {
        var character = new CharacterBody3D
        {
            CollisionLayer = 0, CollisionMask = CollisionWorld.WalkMask,
            FloorMaxAngle = WalkRules.FloorAngleDegrees * float.Pi / 180,
            FloorSnapLength = WalkTuning.SnapLength, FloorConstantSpeed = true,
            FloorStopOnSlope = true, FloorBlockOnWall = true,
            SafeMargin = WalkTuning.SafeMargin, MaxSlides = 4,
            PhysicsInterpolationMode = Node.PhysicsInterpolationModeEnum.Off
        };
        character.AddChild(new CollisionShape3D
        {
            Shape = new CylinderShape3D { Radius = WalkRules.Radius, Height = WalkRules.StandingHeight },
            Position = Vector3.Up * (WalkRules.StandingHeight / 2)
        });
        return character;
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
