using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

// 相机（SYS-VIEW）与介质呈现（SYS-VIEW-MEDIUM），详见 SYS-VIEW
public readonly record struct Pose(Vector3 Position, float YawDeg, float PitchDeg, float? FovDeg = null);
public enum EntryPath { Skylight, Door, WaterSurface }

public enum MediumKind { Air, Interior, Underground, Underwater, FogSea }
public readonly record struct CameraMedium(  // 值类型，每帧查询不分配内存
    MediumKind Kind,
    FeatureId? Feature = null,               // Interior：所在楼；Underwater：水体
    int Storey = 0,                          // Interior
    float WallDist = 0,                      // Interior
    float Depth = 0);                        // Underground、Underwater、FogSea
