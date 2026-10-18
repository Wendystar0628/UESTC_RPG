using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Cinematic;

namespace Campus.Camera.Cinematic;

/// <summary>Pure cinematic math shared by sampling and semantic checks.</summary>
public static class CinematicMath
{
    public static float Smooth(float t) { t = Math.Clamp(t, 0, 1); return t * t * t * (t * (t * 6 - 15) + 10); }
    public static float Ease(float t, CinematicEase? easing)
    {
        t = Math.Clamp(t, 0, 1);
        if (easing?.Bezier is not { } p) return Smooth(t);
        var lo = 0f; var hi = 1f;
        for (var i = 0; i < 20; i++)
        {
            var u = (lo + hi) * 0.5f;
            if (Cubic(u, p.X, p.Z) < t) lo = u; else hi = u;
        }
        return Cubic((lo + hi) * 0.5f, p.Y, p.W);
    }
    private static float Cubic(float t, float a, float b) => 3 * (1 - t) * (1 - t) * t * a + 3 * (1 - t) * t * t * b + t * t * t;
    public static float BlendAngle(float from, float to, float weight) => from + CinematicPoseMath.AngleDelta(from, to) * weight;
    public static Pose Face(Vector3 position, Vector3 target, Pose fallback)
    {
        var direction = target - position;
        if (direction.LengthSquared() < 0.000001f) return fallback with { Position = position };
        var horizontal = MathF.Sqrt(direction.X * direction.X + direction.Z * direction.Z);
        return new(position, MathF.Atan2(direction.X, -direction.Z) * 180 / float.Pi,
            MathF.Atan2(direction.Y, horizontal) * 180 / float.Pi, fallback.FovDeg);
    }
    public static Pose BlendOrientation(Pose from, Pose to, float weight)
    {
        const float radians = float.Pi / 180;
        var a = Quaternion.CreateFromYawPitchRoll(-from.YawDeg * radians, from.PitchDeg * radians, 0);
        var b = Quaternion.CreateFromYawPitchRoll(-to.YawDeg * radians, to.PitchDeg * radians, 0);
        var forward = Vector3.Transform(-Vector3.UnitZ, Quaternion.Slerp(a, b, Math.Clamp(weight, 0, 1)));
        return Face(to.Position, to.Position + forward, to);
    }
    public static bool FullyCovered(CinematicDef definition, double time)
    {
        for (var i = 0; i < definition.Masks.Count; i++)
        {
            var mask = definition.Masks[i];
            switch (mask.Kind)
            {
                case MaskKind.Letterbox: break;
                case MaskKind.CloudCover:
                case MaskKind.Veil:
                    if (time >= mask.T0 + mask.FadeIn && time <= mask.T1 - mask.FadeOut && time < mask.T1) return true;
                    break;
                default: throw new InvalidDataException("Unknown mask kind.");
            }
        }
        return false;
    }
    public static Pose Approach(Pose destination)
    {
        var yaw = destination.YawDeg * float.Pi / 180;
        var pitch = destination.PitchDeg * float.Pi / 180;
        var forward = new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), -MathF.Cos(yaw) * MathF.Cos(pitch));
        return destination with
        {
            Position = destination.Position - forward * CinematicTuning.ApproachBack +
            Vector3.UnitY * (destination.Position.Y > CinematicTuning.HighDestination ? 0 : CinematicTuning.ApproachRise)
        };
    }
    public static float MaskOpacity(MaskSeg mask, double time)
    {
        if (time < mask.T0 || time >= mask.T1) return 0;
        var enter = mask.FadeIn > 0 ? (time - mask.T0) / mask.FadeIn : 1;
        var leave = mask.FadeOut > 0 ? (mask.T1 - time) / mask.FadeOut : 1;
        return (float)Math.Clamp(Math.Min(enter, leave), 0, 1);
    }
}
