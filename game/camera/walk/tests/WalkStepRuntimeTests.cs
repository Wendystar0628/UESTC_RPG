using Campus.Camera.Walk;
using Campus.Contracts;
using Campus.Contracts.Audio;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class WalkStepRuntimeTests
{
    [After]
    public void ReleaseDroppedNatives() => DroppedNatives.Release();

    [TestCase(SurfaceTag.Stair, true, Timeout = 30000)]
    [TestCase(SurfaceTag.Barrier, false, Timeout = 30000)]
    [TestCase(SurfaceTag.Boundary, false, Timeout = 30000)]
    [TestCase(SurfaceTag.DoorClosed, false, Timeout = 30000)]
    [TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task ShortPhysicsStepsTraverseRoundedStairEdgesButRespectNoStepSurfaces(SurfaceTag tag, bool canStep)
    {
        var tree = Engine.GetMainLoop() as SceneTree ?? throw new InvalidOperationException("Native scene tree required.");
        using var world = new World3D();
        using var floorShape = new BoxShape3D
        {
            Size = new(WalkRules.Radius * 16, WalkRules.StepHeight, WalkRules.Radius * 8)
        };
        using var stepShape = new BoxShape3D
        {
            Size = new(WalkRules.Radius * 4, WalkRules.StepHeight, WalkRules.Radius * 4)
        };
        using var landingProbe = new PhysicsTestMotionParameters3D
        {
            Margin = WalkTuning.SafeMargin, MaxCollisions = 4, RecoveryAsCollision = false
        };
        using var landingHit = new PhysicsTestMotionResult3D();
        var viewport = new SubViewport { World3D = world, Size = new(32, 32) };
        try
        {
            tree.Root.AddChild(viewport);
            float dt = 1f / Engine.PhysicsTicksPerSecond;
            var (body, step, collision) = CreateNativeBodies(viewport, floorShape, stepShape, tag, dt);
            await SettleBody(tree, body, dt);
            var observation = await MoveAcrossStep(tree, body, step, collision, landingProbe, landingHit, dt, canStep);
            AssertStepResult(tag, canStep, body.GlobalPosition, observation);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(viewport)) viewport.Free();
        }
    }

    private static (CharacterBody3D Body, StaticBody3D Step, WalkBodyCollision Collision) CreateNativeBodies(
        SubViewport viewport, BoxShape3D floorShape, BoxShape3D stepShape, SurfaceTag tag, float dt)
    {
        var floor = new StaticBody3D
        {
            CollisionLayer = 1u << (int)SurfaceTag.Ground,
            Position = Vector3.Down * (floorShape.Size.Y * .5f)
        };
        viewport.AddChild(floor);
        floor.AddChild(new CollisionShape3D { Shape = floorShape });
        var step = new StaticBody3D
        {
            CollisionLayer = 1u << (int)tag,
            Position = new(stepShape.Size.X * .5f, stepShape.Size.Y * .5f, 0)
        };
        viewport.AddChild(step);
        step.AddChild(new CollisionShape3D { Shape = stepShape });
        var body = new CharacterBody3D
        {
            // One real walk tick ends just inside the rounded rim, using the body's existing margin.
            // Whole-radius starting distances instead align this tick exactly tangent to the box.
            Position = new(-WalkRules.Radius + WalkTuning.SafeMargin - WalkRules.WalkSpeed * dt,
                WalkTuning.SafeMargin, 0),
            CollisionLayer = 0, CollisionMask = WalkBodyCollision.CollisionLayers,
            MotionMode = CharacterBody3D.MotionModeEnum.Grounded, UpDirection = Vector3.Up,
            FloorMaxAngle = WalkRules.FloorAngleDegrees * (float.Pi / 180f),
            FloorSnapLength = WalkTuning.SnapLength, SafeMargin = WalkTuning.SafeMargin,
            FloorConstantSpeed = true, FloorStopOnSlope = true, FloorBlockOnWall = true
        };
        viewport.AddChild(body);
        var collision = new WalkBodyCollision(body);
        collision.Initialize();
        return (body, step, collision);
    }

    private static async Task SettleBody(SceneTree tree, CharacterBody3D body, float dt)
    {
        await body.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await body.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        for (int frame = 0; frame < TestSamples.PerProperty && !body.IsOnFloor(); frame++)
        {
            await body.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            body.Velocity = Vector3.Down * (WalkTuning.Gravity * dt);
            body.MoveAndSlide();
        }
        AssertThat(body.IsOnFloor()).OverrideFailureMessage("The native walking cylinder did not settle on its fixture floor.").IsTrue();
    }

    private static async Task<StepObservation> MoveAcrossStep(SceneTree tree, CharacterBody3D body,
        StaticBody3D step, WalkBodyCollision collision, PhysicsTestMotionParameters3D landingProbe,
        PhysicsTestMotionResult3D landingHit, float dt, bool canStep)
    {
        var requested = Vector3.Right * (WalkRules.WalkSpeed * dt);
        AssertThat(requested.Length() < WalkTuning.StepSupportForwardDistance)
            .OverrideFailureMessage("The runtime fixture needs a short physics step to exercise rounded-edge support.").IsTrue();
        bool sawRoundedLanding = false;
        bool crossedRoundedLanding = false;
        bool stepped = false;
        bool grounded = true;
        for (int frame = 0; frame < TestSamples.PerProperty; frame++)
        {
            await body.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            var before = body.GlobalPosition;
            body.Velocity = new(WalkRules.WalkSpeed, -WalkTuning.Gravity * dt, 0);
            body.FloorSnapLength = WalkTuning.SnapLength;
            body.MoveAndSlide();
            bool movedUp = false;
            if (grounded && body.IsOnWall())
            {
                var actual = body.GlobalPosition - before;
                var remaining = new Vector3(requested.X - actual.X, 0, requested.Z - actual.Z);
                bool rounded = HasRoundedLanding(body, step, remaining, landingProbe, landingHit);
                sawRoundedLanding |= rounded;
                movedUp = collision.TryStep(requested, before);
                crossedRoundedLanding |= rounded && movedUp;
                stepped |= movedUp;
            }
            grounded = body.IsOnFloor() || movedUp;
            if ((!canStep && sawRoundedLanding) || body.GlobalPosition.X >= WalkRules.Radius) break;
        }
        return new(sawRoundedLanding, crossedRoundedLanding, stepped);
    }

    private static bool HasRoundedLanding(CharacterBody3D body, StaticBody3D step, Vector3 remaining,
        PhysicsTestMotionParameters3D landingProbe, PhysicsTestMotionResult3D landingHit)
    {
        // Observe the first native landing sweep; production alone decides how to cross it.
        landingProbe.From = new(Basis.Identity,
            body.GlobalPosition + Vector3.Up * WalkRules.StepHeight + remaining);
        landingProbe.Motion = Vector3.Down * (WalkRules.StepHeight + WalkTuning.SnapLength);
        bool rounded = PhysicsServer3D.BodyTestMotion(body.GetRid(), landingProbe, landingHit)
            && landingHit.GetCollisionCount() > 0;
        for (int contact = 0; contact < landingHit.GetCollisionCount() && rounded; contact++)
            rounded = landingHit.GetColliderRid(contact) == step.GetRid()
                && landingHit.GetCollisionNormal(contact).Y < MathF.Cos(body.FloorMaxAngle);
        return rounded;
    }

    private static void AssertStepResult(SurfaceTag tag, bool canStep, Vector3 feet, StepObservation observation)
    {
        AssertThat(observation.SawRoundedLanding)
            .OverrideFailureMessage("Small native physics steps did not reproduce the non-walkable first stair-edge landing contact.").IsTrue();
        if (canStep)
        {
            AssertThat(observation.CrossedRoundedLanding && feet.X >= WalkRules.Radius
                && MathF.Abs(feet.Y - WalkRules.StepHeight) <= TestTolerance.Position)
                .OverrideFailureMessage("The production stepper did not cross the rounded edge and reach the top of its rule-height stair.").IsTrue();
        }
        else
        {
            AssertThat(!observation.Stepped && feet.X < 0 && MathF.Abs(feet.Y) <= TestTolerance.Position)
                .OverrideFailureMessage($"The production stepper climbed the same low box on the forbidden {tag} layer.").IsTrue();
        }
    }

    private readonly record struct StepObservation(bool SawRoundedLanding, bool CrossedRoundedLanding, bool Stepped);
}
