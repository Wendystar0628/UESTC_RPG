using System.Numerics;
using Campus.Contracts;

namespace Campus.Camera.Walk;

/// <summary>Engine-independent movement and safety rules, shared by the live controller and focused checks.</summary>
public static class WalkMath
{
    public static float StepFrequency(float speed) => Math.Clamp(
        WalkTuning.StepFrequencyScale * MathF.Pow(speed, WalkTuning.StepFrequencyPower),
        WalkTuning.MinimumStepFrequency, WalkTuning.MaximumStepFrequency);

    /// <summary>X is lateral metres, Y is vertical metres, Z is presentation roll in degrees.</summary>
    public static Vector3 BobOffset(float phase, bool nextFootRight, float scale)
    {
        float side = MathF.Sin((phase + (nextFootRight ? 1f : 0f)) * float.Pi);
        return new(side * WalkTuning.BobLateralMetres * scale,
            -MathF.Cos(phase * float.Tau) * WalkTuning.BobVerticalMetres * scale,
            side * WalkTuning.BobRollDegrees * scale);
    }
    public static bool ValidCylinderDimensions(float radius, float height) =>
        float.IsFinite(radius) && float.IsFinite(height) && radius > 0 && height > 0;

    public static bool SwimmingAt(WaterSample water, float feetY, bool swimming) =>
        water.CanSwim && feetY <= water.SurfaceY && (swimming
            ? water.SurfaceY - water.BottomY > WaterMotionRules.ExitColumnDepth
            : water.SurfaceY - water.BottomY >= WaterMotionRules.EnterColumnDepth);

    public static bool FreezeUnlessPlaying(bool isPlaying, Vector3 feet, ref Vector3 previousFeet, ref Vector3 velocity)
    {
        if (isPlaying) return false;
        previousFeet = feet;
        velocity = Vector3.Zero;
        return true;
    }

    public static bool CanWalk(WalkClass value) => value switch
    {
        WalkClass.Walk or WalkClass.Interior or WalkClass.Wade => true,
        WalkClass.Building or WalkClass.Water or WalkClass.Forbidden or WalkClass.Outside or WalkClass.Steep => false,
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    public static Vector3 Direction(Vector2 input, float yawDeg)
    {
        if (!float.IsFinite(input.X) || !float.IsFinite(input.Y) || !float.IsFinite(yawDeg))
            throw new ArgumentOutOfRangeException(nameof(input));
        if (input.LengthSquared() > 1f) input = Vector2.Normalize(input);
        float yaw = yawDeg * (float.Pi / 180f);
        float sin = MathF.Sin(yaw), cos = MathF.Cos(yaw);
        return new(input.X * cos + input.Y * sin, 0, input.X * sin - input.Y * cos);
    }

    public static Vector3 HorizontalVelocity(Vector3 current, Vector3 target, bool grounded, float seconds)
    {
        float retention = MathF.Exp(-(grounded ? WalkTuning.GroundResponse : WalkTuning.AirResponse) * seconds);
        var value = new Vector3(target.X + (current.X - target.X) * retention, 0,
            target.Z + (current.Z - target.Z) * retention);
        return LimitHorizontal(value) with { Y = current.Y };
    }

    public static Vector3 LimitHorizontal(Vector3 value)
    {
        float speed = MathF.Sqrt(value.X * value.X + value.Z * value.Z);
        if (speed <= WalkTuning.MaximumHorizontalSpeed) return value;
        float scale = WalkTuning.MaximumHorizontalSpeed / speed;
        return new(value.X * scale, value.Y, value.Z * scale);
    }

    public static Vector3 SlowAtBoundaries(Vector3 velocity, ReadOnlySpan<BoundaryHit> hits)
    {
        foreach (var hit in hits)
        {
            float inward = Vector3.Dot(velocity, hit.Inward);
            if (inward >= 0) continue;
            float retained = ScalarMath.Smoothstep(0, WalkRules.BoundarySlowdownDistance, hit.Distance);
            velocity -= hit.Inward * (inward * (1f - retained));
        }
        return velocity;
    }

}
