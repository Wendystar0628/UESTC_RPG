using Campus.Camera.Walk;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Gen.Collision;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Collision.Tests;

public sealed partial class CollisionCoreTests
{
    private async Task CheckCylinders(SceneTree tree, IMapContext context, WalkBakeData walk,
        IReadOnlyDictionary<TileKey, CollisionTile> products)
    {
        var samples = SampleMask(walk);
        using var native = new World3D();
        var viewport = new SubViewport { World3D = native, Size = new(32, 32) };
        var root = new Node3D();
        var collision = new CollisionWorld();
        var characters = new Dictionary<int, CharacterBody3D>();
        var admitted = new bool[samples.Count];
        var placements = new N.Vector3?[samples.Count];
        var supportFeet = new N.Vector3?[samples.Count];
        var deepest = new float[samples.Count];
        using var shape = new CylinderShape3D { Radius = WalkTuning.Radius, Height = WalkTuning.StandingHeight };
        using var query = new PhysicsShapeQueryParameters3D
        {
            Shape = shape, Margin = 0, CollisionMask = CollisionWorld.WalkMask,
            CollideWithAreas = false, CollideWithBodies = true
        };
        try
        {
            tree.Root.AddChild(viewport); viewport.AddChild(root);
            Require(root.GetWorld3D().Space == native.Space && native.Space != tree.Root.FindWorld3D().Space,
                "G15 fixture shares the application's physics space.");
            collision.Attach(root, context);
            GD.Print($"G15Core preparing cylinders={samples.Count} collisionTiles={products.Count}");
            var resident = new HashSet<TileKey>();
            var recovered = 0;
            var corrected = 0;
            var processed = 0;
            var distanceTotal = 0d;
            var distanceMaximum = 0f;
            var batches = Enumerable.Range(0, samples.Count).GroupBy(i => CollisionGeometry.TileAt(samples[i]))
                .OrderBy(batch => batch.Key.Z).ThenBy(batch => batch.Key.X);
            foreach (var batch in batches)
            {
                var indices = batch.ToArray();
                // Cover the original tile's full recovery search, then each candidate tile's Ready neighbours.
                // Only formal products can enter this window; an absent edge tile must still fail Ready.
                var origin = CollisionGeometry.Origin(batch.Key);
                var reach = new N.Vector3(WalkTuning.SafetySearchRadius, 0, WalkTuning.SafetySearchRadius);
                var min = CollisionGeometry.TileAt(origin - reach);
                var max = CollisionGeometry.TileAt(origin + new N.Vector3(CollisionGeometry.TileSize, 0, CollisionGeometry.TileSize) + reach);
                var needed = new HashSet<TileKey>();
                for (var z = min.Z - 1; z <= max.Z + 1; z++)
                    for (var x = min.X - 1; x <= max.X + 1; x++)
                    {
                        var tile = new TileKey(x, z);
                        if (products.ContainsKey(tile)) needed.Add(tile);
                    }
                foreach (var tile in resident.Where(tile => !needed.Contains(tile)).ToArray())
                {
                    collision.RemoveTile(tile);
                    resident.Remove(tile);
                }
                foreach (var tile in needed.OrderBy(tile => tile.Z).ThenBy(tile => tile.X))
                {
                    if (resident.Contains(tile)) continue;
                    var work = new CollisionMountWork(collision, products[tile], 0);
                    while (!work.Step(MountSliceBudgetMilliseconds)) await root.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    resident.Add(tile);
                }
                await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                GD.Print($"G15Core cylinder batch tile={batch.Key} samples={indices.Length} collisionTiles={resident.Count}/{products.Count} processed={processed}/{samples.Count}");
                foreach (var i in indices)
                {
                    var sample = samples[i];
                    var character = new CharacterBody3D
                    {
                        CollisionLayer = 0, CollisionMask = CollisionWorld.WalkMask,
                        FloorMaxAngle = WalkTuning.FloorAngleDegrees * float.Pi / 180,
                        FloorSnapLength = WalkTuning.SnapLength, FloorConstantSpeed = true,
                        FloorStopOnSlope = true, FloorBlockOnWall = true,
                        SafeMargin = WalkTuning.SafeMargin, MaxSlides = 4,
                        PhysicsInterpolationMode = Node.PhysicsInterpolationModeEnum.Off
                    };
                    character.AddChild(new CollisionShape3D { Shape = shape, Position = Vector3.Up * (WalkTuning.StandingHeight / 2) });
                    root.AddChild(character); characters.Add(i, character);
                    using var admission = new WalkPlacementAdmission(character);
                    var safe = new WalkSafetyRing();
                    var candidate = sample;
                    var lift = WalkTuning.LandingLift;
                    var returns = 0;
                    N.Vector3? rejected = null;
                    while (true)
                    {
                        var support = WalkPlacementAdmission.ResolveSupport(context.World.Spatial, candidate);
                        var feet = (support ?? candidate) + N.Vector3.UnitY * lift;
                        character.GlobalPosition = feet.ToGodot();
                        if (!collision.Ready(feet, WalkTuning.Radius))
                        {
                            Fail($"sample={i} maskFeet={sample} candidateFeet={feet} formal collision neighbourhood is not Ready.");
                            break;
                        }
                        if (admission.Accept(context.World.Spatial, collision, feet, WalkTuning.StandingHeight, lift))
                        {
                            admitted[i] = true; placements[i] = feet; supportFeet[i] = support;
                            character.Velocity = Vector3.Down * WalkTuning.LandingSpeed;
                            break;
                        }
                        rejected = feet;
                        // Exactly the production selection, followed by the same placement check again.
                        // A returned point is never accepted merely because Recover chose it.
                        var target = WalkPlacementAdmission.Recover(context.World.Spatial, safe, new(feet, 0, 0), false, 0, rejected);
                        if (target is not { } destination)
                        {
                            Fail($"sample={i} maskFeet={sample} rejectedFeet={feet} has no legal formal landing within {WalkTuning.SafetySearchRadius}m.");
                            break;
                        }
                        candidate = destination.Position; lift = 0; returns++;
                        character.GlobalPosition = (WalkPlacementAdmission.ResolveSupport(context.World.Spatial, candidate) ?? candidate).ToGodot();
                        await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                    }
                    if (returns != 0) recovered++;
                    if (placements[i] is { } actual)
                    {
                        var distance = N.Vector3.Distance(sample, supportFeet[i]!.Value);
                        if (distance > 0) corrected++;
                        distanceTotal += distance; distanceMaximum = MathF.Max(distanceMaximum, distance);
                        GD.Print($"G15Core placement sample={i} original={sample} actualSupport={supportFeet[i]} initialBody={actual} lift={lift:R}m returns={returns} distance={distance:R}m");
                        if (distance > 2) GD.Print($"G15Core relocationOver2m sample={i} original={sample} actualSupport={supportFeet[i]} initialBody={actual} distance={distance:R}m");
                    }
                    else GD.Print($"G15Core placement sample={i} original={sample} actualSupport=none initialBody=none returns={returns}");
                }
                GD.Print($"G15Core simulating tile={batch.Key} cylinders={indices.Count(i => admitted[i])} physicsSteps={Engine.PhysicsTicksPerSecond}");
                foreach (var i in indices) if (admitted[i]) RecordPenetration(i);
                for (var step = 0; step < Engine.PhysicsTicksPerSecond; step++)
                {
                    await root.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                    foreach (var i in indices)
                    {
                        if (!admitted[i]) continue;
                        var character = characters[i];
                        if (!collision.Ready(character.GlobalPosition.ToNumerics(), WalkTuning.Radius))
                        {
                            Fail($"sample={i} original={samples[i]} actual={placements[i]} lost formal collision readiness during step={step}.");
                            admitted[i] = false; continue;
                        }
                        character.Velocity += Vector3.Down * (WalkTuning.Gravity / Engine.PhysicsTicksPerSecond);
                        character.MoveAndSlide();
                        RecordPenetration(i);
                    }
                }
                foreach (var i in indices)
                {
                    var character = characters[i];
                    if (placements[i] is not null)
                    {
                        if (deepest[i] > WalkTuning.SafeMargin)
                            Fail($"sample={i} original={samples[i]} actual={placements[i]} penetration={deepest[i]:R}m exceeds SafeMargin={WalkTuning.SafeMargin:R}m.");
                        if (!character.IsOnFloor()) Fail($"sample={i} original={samples[i]} actual={placements[i]} finalFeet={character.GlobalPosition} did not ground within 120 actual physics frames.");
                    }
                    character.Free();
                }
                characters.Clear();
                processed += indices.Length;
            }
            GD.Print($"G15Core placement total={samples.Count} admitted={placements.Count(value => value is not null)} recovered={recovered} corrected={corrected} distanceTotal={distanceTotal:R}m distanceMeanAll={distanceTotal / samples.Count:R}m distanceMax={distanceMaximum:R}m");
            GD.Print($"G15Core cylinders={processed} steps={Engine.PhysicsTicksPerSecond} failures={_failureCount}");

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
}
