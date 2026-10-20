using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Settings;
using Campus.Contracts.Audio;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Camera.Walk;

/// <summary>Native walking shapes, body probes, stair traversal and contact sampling.</summary>
internal sealed class WalkBodyCollision(CharacterBody3D body)
{
    // WP-43 assigns one layer per SurfaceTag. Water is semantic, never a blocking surface.
    private const uint BlockingLayers = (1u << (int)SurfaceTag.Ground) | (1u << (int)SurfaceTag.Stair)
        | (1u << (int)SurfaceTag.Barrier) | (1u << (int)SurfaceTag.Wall) | (1u << (int)SurfaceTag.Prop)
        | (1u << (int)SurfaceTag.Invisible) | (1u << (int)SurfaceTag.Boundary) | (1u << (int)SurfaceTag.DoorClosed);
    private const uint NoStepLayers = (1u << (int)SurfaceTag.Barrier) | (1u << (int)SurfaceTag.Boundary)
        | (1u << (int)SurfaceTag.DoorClosed);
    private readonly PhysicsTestMotionParameters3D _probe = new();
    private readonly PhysicsTestMotionResult3D _probeResult = new();
    private readonly CylinderShape3D _cylinder = new() { Radius = WalkRules.Radius, Height = WalkRules.StandingHeight };
    private readonly CylinderShape3D _standingProbe = new() { Radius = WalkRules.Radius, Height = WalkRules.StandingHeight };
    private readonly PhysicsShapeQueryParameters3D _clearance = new();
    private CollisionShape3D? _shape;

    public bool Initialized => _shape is not null;
    public float Radius => _cylinder.Radius;
    public float Height => _cylinder.Height;
    public static uint CollisionLayers => BlockingLayers;

    public void Initialize()
    {
        _shape = new CollisionShape3D { Shape = _cylinder, Position = Vector3.Up * (_cylinder.Height * .5f) };
        body.AddChild(_shape);
        _probe.Margin = WalkTuning.SafeMargin;
        _probe.MaxCollisions = 4;
        _probe.RecoveryAsCollision = false;
        _clearance.CollisionMask = BlockingLayers;
        _clearance.CollideWithAreas = false;
        _clearance.CollideWithBodies = true;
        _clearance.Margin = 0;
        _clearance.Exclude = [body.GetRid()];
    }

    public void ApplyHeight(bool swimming, bool crouching)
    {
        _cylinder.Radius = swimming ? WaterMotionRules.SwimmingShape.Radius : WalkRules.Radius;
        _cylinder.Height = swimming ? WaterMotionRules.SwimmingShape.Height
            : crouching ? WalkTuning.CrouchingHeight : WalkRules.StandingHeight;
        if (_shape is not null) _shape.Position = Vector3.Up * (_cylinder.Height * .5f);
    }

    public bool CanStand(N.Vector3 feet)
    {
        // Existing crouch volume is already collision-resolved. Query only the added crown, so a sloped
        // supporting floor does not falsely count as an overhead obstruction during the full-cylinder query.
        float extra = WalkRules.StandingHeight - WalkTuning.CrouchingHeight;
        _standingProbe.Height = extra;
        _clearance.Shape = _standingProbe;
        _clearance.Transform = new(Basis.Identity,
            (feet + N.Vector3.UnitY * (WalkTuning.CrouchingHeight + extra * .5f)).ToGodot());
        return body.GetWorld3D().DirectSpaceState.IntersectShape(_clearance, 1).Count == 0;
    }

    private bool Probe(Vector3 from, Vector3 motion)
    {
        _probe.From = new(Basis.Identity, from);
        _probe.Motion = motion;
        return PhysicsServer3D.BodyTestMotion(body.GetRid(), _probe, _probeResult);
    }

    public Vector3 LimitCrouchEdge(Vector3 motion)
    {
        var from = body.GlobalPosition;
        if (HasSupport(from + motion)) return motion;
        // Axis-wise support checks let the player slide along a ledge without stepping past its edge.
        var zOnly = new Vector3(0, 0, motion.Z);
        if (HasSupport(from + zOnly)) return zOnly;
        var xOnly = new Vector3(motion.X, 0, 0);
        return HasSupport(from + xOnly) ? xOnly : Vector3.Zero;
    }

    private bool HasSupport(Vector3 feet) => Probe(feet + Vector3.Up * WalkTuning.SafeMargin,
        Vector3.Down * (WalkTuning.SnapLength + WalkTuning.SafeMargin))
        && _probeResult.GetCollisionNormal().Y >= MathF.Cos(body.FloorMaxAngle);

    public bool TryStep(Vector3 requested, Vector3 before)
    {
        if (requested.LengthSquared() < .0000001f) return false;
        for (int i = 0; i < body.GetSlideCollisionCount(); i++)
        {
            var hit = body.GetSlideCollision(i);
            if (hit.GetNormal().Y < MathF.Cos(body.FloorMaxAngle) && hit.GetCollider() is CollisionObject3D blocker
                && (blocker.CollisionLayer & NoStepLayers) != 0) return false;
        }
        var current = body.GlobalPosition;
        var actual = current - before;
        var remaining = new Vector3(requested.X - actual.X, 0, requested.Z - actual.Z);
        if (remaining.LengthSquared() < .0000001f || remaining.Dot(requested) <= 0) return false;
        var lift = Vector3.Up * WalkRules.StepHeight;
        if (Probe(current, lift)) lift = _probeResult.GetTravel();
        if (lift.Y <= WalkTuning.SafeMargin) return false;
        var raised = current + lift;
        if (Probe(raised, remaining)) return false;
        var advanced = raised + remaining;
        if (!Probe(advanced, Vector3.Down * (lift.Y + WalkTuning.SnapLength))) return false;
        if (_probeResult.GetCollisionNormal().Y < MathF.Cos(body.FloorMaxAngle)) return false;
        if (_probeResult.GetCollider() is CollisionObject3D floor && (floor.CollisionLayer & NoStepLayers) != 0) return false;
        var destination = advanced + _probeResult.GetTravel();
        if (destination.Y - before.Y > WalkRules.StepHeight + WalkTuning.SafeMargin) return false;
        body.GlobalPosition = destination;
        body.FloorSnapLength = 0;
        body.Velocity = new(body.Velocity.X, 0, body.Velocity.Z);
        return true;
    }

    public void LimitSteepSlide()
    {
        for (int i = 0; i < body.GetSlideCollisionCount(); i++)
        {
            var normal = body.GetSlideCollision(i).GetNormal();
            if (normal.Y < MathF.Cos(WalkTuning.SlowSlideAngleDegrees * (float.Pi / 180f)) || normal.Y >= MathF.Cos(body.FloorMaxAngle)) continue;
            if (body.Velocity.Y >= 0) continue;
            var downhill = (Vector3.Down - normal * Vector3.Down.Dot(normal)).Normalized();
            float along = body.Velocity.Dot(downhill);
            if (along > WalkTuning.SlowSlideSpeed) body.Velocity -= downhill * (along - WalkTuning.SlowSlideSpeed);
        }
    }

    public bool WantsAutoJump(N.Vector3 horizontalVelocity, float dt, N.Vector3 feet, WalkInput input,
        bool crouching, float sinceJump, ISettingsSection? settings, IMapContext map, ICollisionWorld collision)
    {
        if (crouching || input.Move.LengthSquared() == 0 || sinceJump < WalkTuning.HeldJumpInterval
            || !(settings?.Get(UiSettingDefinitions.AutoJump) ?? UiSettingDefinitions.AutoJump.Default)) return false;
        var motion = new Vector3(horizontalVelocity.X * dt, 0, horizontalVelocity.Z * dt);
        if (motion.LengthSquared() <= WalkTuning.SafeMargin * WalkTuning.SafeMargin
            || !Probe(body.GlobalPosition, motion) || _probeResult.GetCollisionNormal().Y >= MathF.Cos(body.FloorMaxAngle)
            || ProbeHasNoStepSurface()) return false;
        float apex = WalkTuning.JumpSpeed * WalkTuning.JumpSpeed / (2f * WalkTuning.Gravity);
        var lift = Vector3.Up * apex;
        if (Probe(body.GlobalPosition, lift)) return false;
        var ahead = motion.Normalized() * (WalkRules.Radius * 2f + motion.Length());
        var raised = body.GlobalPosition + lift;
        if (Probe(raised, ahead)) return false;
        var advanced = raised + ahead;
        if (!Probe(advanced, Vector3.Down * apex)
            || _probeResult.GetCollisionNormal().Y < MathF.Cos(body.FloorMaxAngle) || ProbeHasNoStepSurface()) return false;
        var destination = (advanced + _probeResult.GetTravel()).ToNumerics();
        float height = destination.Y - feet.Y;
        if (height <= WalkRules.StepHeight + WalkTuning.SafeMargin || height > apex
            || !collision!.Ready(destination, WalkRules.Radius)
            || !WalkSafetyQueries.AllowsFeet(map!.World.Spatial, destination)) return false;
        _standingProbe.Height = WalkRules.StandingHeight;
        _clearance.Shape = _standingProbe;
        _clearance.Transform = new(Basis.Identity,
            (destination + N.Vector3.UnitY * (WalkRules.StandingHeight * .5f + WalkTuning.SafeMargin)).ToGodot());
        return body.GetWorld3D().DirectSpaceState.IntersectShape(_clearance, 1).Count == 0;
    }

    private bool ProbeHasNoStepSurface()
    {
        for (int i = 0; i < _probeResult.GetCollisionCount(); i++)
            if (_probeResult.GetCollider(i) is CollisionObject3D collider && (collider.CollisionLayer & NoStepLayers) != 0)
                return true;
        return false;
    }

    public FootfallInfo? SampleFootfall(ulong tick, bool nextFootRight, float speed)
    {
        if (!Probe(body.GlobalPosition + Vector3.Up * WalkTuning.SafeMargin,
            Vector3.Down * (WalkTuning.SnapLength + WalkTuning.SafeMargin))) return null;
        var normal = _probeResult.GetCollisionNormal();
        var tag = _probeResult.GetCollider() is CollisionObject3D collider ? CollisionTag(collider.CollisionLayer) : SurfaceTag.Ground;
        return new(tick, nextFootRight ? FootSide.Right : FootSide.Left,
            _probeResult.GetCollisionPoint().ToNumerics(), speed, normal.ToNumerics(),
            _probeResult.GetColliderId(), _probeResult.GetColliderShape(), tag == SurfaceTag.Stair,
            MathF.Acos(Math.Clamp(normal.Y, -1, 1)) * (180f / float.Pi));
    }

    public static SurfaceTag CollisionTag(uint layers) => layers switch
    {
        1u << (int)SurfaceTag.Ground => SurfaceTag.Ground,
        1u << (int)SurfaceTag.Stair => SurfaceTag.Stair,
        1u << (int)SurfaceTag.Barrier => SurfaceTag.Barrier,
        1u << (int)SurfaceTag.Wall => SurfaceTag.Wall,
        1u << (int)SurfaceTag.Prop => SurfaceTag.Prop,
        1u << (int)SurfaceTag.Water => SurfaceTag.Water,
        1u << (int)SurfaceTag.Invisible => SurfaceTag.Invisible,
        1u << (int)SurfaceTag.Boundary => SurfaceTag.Boundary,
        1u << (int)SurfaceTag.DoorClosed => SurfaceTag.DoorClosed,
        _ => throw new InvalidOperationException("Walking contact has no registered surface layer.")
    };
}
