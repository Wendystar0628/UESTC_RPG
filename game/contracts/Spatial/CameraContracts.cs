using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

// 相机（WP-06）与介质呈现（WP-07），详见 SYS-VIEW
public readonly record struct Pose(Vector3 Position, float YawDeg, float PitchDeg, float? FovDeg = null);
public enum EntryPath { Skylight, Door, WaterSurface }

public static class PoseMath
{
    public static Pose PoseFacing(Vector3 target, float azDeg, float elDeg, float dist)
    {
        if (!float.IsFinite(dist) || dist < 0) throw new ArgumentOutOfRangeException(nameof(dist));
        if (!float.IsFinite(azDeg) || !float.IsFinite(elDeg)) throw new ArgumentOutOfRangeException(nameof(azDeg));
        float az = azDeg * (float.Pi / 180f), el = elDeg * (float.Pi / 180f);
        var forward = new Vector3(MathF.Sin(az) * MathF.Cos(el), -MathF.Sin(el), -MathF.Cos(az) * MathF.Cos(el));
        return new(target - forward * dist, azDeg, -elDeg);
    }
}

public enum MediumKind { Air, Interior, Underground, Underwater, FogSea }
public readonly record struct CameraMedium(  // 值类型，每帧查询不分配内存
    MediumKind Kind,
    FeatureId? Feature = null,               // Interior：所在楼；Underwater：水体
    int Storey = 0,                          // Interior
    float WallDist = 0,                      // Interior
    float Depth = 0);                        // Underground、Underwater、FogSea
