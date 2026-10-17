using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.AppFlow;

namespace Campus.Camera.Walk;

/// <summary>Engine-independent movement and safety rules, shared by the live controller and focused checks.</summary>
public static class WalkMath
{
    public static bool ValidCylinderDimensions(float radius, float height) =>
        float.IsFinite(radius) && float.IsFinite(height) && radius > 0 && height > 0;

    public static bool FreezeForAppState(AppState state, Vector3 feet, ref Vector3 previousFeet, ref Vector3 velocity)
    {
        bool frozen = state switch
        {
            AppState.Boot or AppState.Title or AppState.Entering or AppState.Leaving or AppState.Quitting => true,
            AppState.Playing => false,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };
        if (!frozen) return false;
        previousFeet = feet;
        velocity = Vector3.Zero;
        return true;
    }

    public static bool Finite(Vector3 p) => float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z);

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

    public static float SmoothStep(float value)
    {
        float t = Math.Clamp(value, 0f, 1f);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    public static float DescentSeconds(float height) => Math.Clamp(.4f + .4f * MathF.Log10(MathF.Max(height, 1f)), .5f, 1.6f);
}
