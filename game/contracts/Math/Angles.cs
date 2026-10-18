using System.Numerics;

namespace Campus.Contracts;

/// <summary>World bearings: x east, z south, 0 north, clockwise positive. Wrapped degrees always use [0,360).</summary>
public static class Angles
{
    public const float DegreesToRadians = MathF.PI / 180f;
    public const float RadiansToDegrees = 180f / MathF.PI;

    public static float WrapDegrees(float degrees)
    {
        float value = degrees % 360f;
        if (value < 0f) value += 360f;
        return value == 0f || value >= 360f ? 0f : value;
    }

    public static double WrapDegrees(double degrees)
    {
        double value = degrees % 360d;
        if (value < 0d) value += 360d;
        return value == 0d || value >= 360d ? 0d : value;
    }

    /// <summary>Shortest signed clockwise change from one bearing to another; an exact half-turn is +180 degrees.</summary>
    public static float SignedDelta(float fromDegrees, float toDegrees)
    {
        var change = WrapDegrees(toDegrees - fromDegrees);
        return change > 180f ? change - 360f : change;
    }

    /// <summary>Plan direction (x,z) from compass degrees; does not wrap before trigonometry.</summary>
    public static Vector2 Heading(float bearingDegrees)
    {
        float a = bearingDegrees * DegreesToRadians;
        return new(MathF.Sin(a), -MathF.Cos(a));
    }

    public static float Bearing(Vector2 direction) => WrapDegrees(MathF.Atan2(direction.X, -direction.Y) * RadiansToDegrees);

    /// <summary>Camera forward, pitch positive upward. Heading is a compass bearing.</summary>
    public static Vector3 Forward(float bearingDegrees, float pitchDegrees)
    {
        float yaw = bearingDegrees * DegreesToRadians, pitch = pitchDegrees * DegreesToRadians;
        return new(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), -MathF.Cos(yaw) * MathF.Cos(pitch));
    }

    /// <summary>Godot camera local -Z forward: its Y rotation has the opposite sign to compass yaw.</summary>
    public static float CompassYawToGodotRadians(float bearingDegrees) => -bearingDegrees * DegreesToRadians;
}
