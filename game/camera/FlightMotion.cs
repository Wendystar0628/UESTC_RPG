using System.Numerics;
using Campus.Contracts;

namespace Campus.Camera;

/// <summary>Render-frame flight integration, independent of physics and the camera's pitch.</summary>
public sealed class FlightMotion(FlightTuning tuning)
{
    public Vector3 Velocity { get; private set; }

    public Vector3 Step(in Pose pose, Vector3 localMove, bool sprint, int speedStep,
        float groundY, float seconds)
    {
        if (!float.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (localMove.LengthSquared() > 1f) localMove = Vector3.Normalize(localMove);
        float yaw = pose.YawDeg * (float.Pi / 180f);
        float sin = MathF.Sin(yaw), cos = MathF.Cos(yaw);
        // Local +Z is forward; a compass yaw of 90 degrees points east (+X).
        Vector3 direction = new(localMove.X * cos + localMove.Z * sin,
            localMove.Y, localMove.X * sin - localMove.Z * cos);
        float height = Math.Max(0, pose.Position.Y - groundY);
        float speed = Math.Min(tuning.SpeedAt(speedStep), tuning.GroundSpeed + tuning.HeightSpeedFactor * height);
        if (sprint) speed *= tuning.SprintMultiplier;
        Vector3 target = direction * speed;
        float rate = target.LengthSquared() > Velocity.LengthSquared() ? tuning.Acceleration : tuning.Deceleration;
        // Analytic displacement gives the same acceleration travel at different render frame rates.
        float decay = MathF.Exp(-rate * seconds);
        Vector3 displacement = target * seconds + (Velocity - target) * ((1f - decay) / rate);
        Velocity = target + (Velocity - target) * decay;
        if (Velocity.LengthSquared() < .000001f && target == Vector3.Zero) Velocity = Vector3.Zero;
        return displacement;
    }

    public void Reset() => Velocity = Vector3.Zero;
    public void ConstrainVelocity(Vector3 velocity) => Velocity = velocity;
}
