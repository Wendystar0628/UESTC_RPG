using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

// 外围（照 WLD-OUTSKIRTS，WP-47 生成、WP-08 解析；命名空间 Campus.Contracts）
public enum OutskirtsBand : byte { O0, O1, O2 }        // O0 街道带、O1 外围街区、O2 雾中楼顶；O3 是现有的远景卡片和雪山（WP-22）
public enum OutskirtsKind : byte { SlabResidential, PointTower, Factory, ParkOffice, Mall, School, Landmark }
public sealed record OutskirtsBlock(string Id, OutskirtsKind Kind, OutskirtsBand Band,
    IReadOnlyList<Vector2> Footprint, float Height, int Floors,
    byte FacadeLevel, byte RoofLevel, uint Seed);           // 级别由管线按最近可达距离定（WLD-OUTSKIRTS）

public interface IOutskirtsModel                             // WP-08 解析 outskirts.json 与外围覆盖层，只读
{
    IReadOnlyList<Vector2> FrontLine { get; }               // 前沿线，闭合
    IReadOnlyList<Vector2> CutLine { get; }                 // 截面线，闭合，2 m 等距；WP-22 与 WP-47 共用这些点
    IReadOnlyList<OutskirtsBlock> Blocks { get; }
    float FrontDistance(float x, float z);                  // 到前沿线的距离，线内为 0（16 m 网格，双线性）
    float SoftFloor(float x, float z);                      // 屋顶净空 + 40 m（8 m 网格，外扩一格取最大）
    // 道路横断面、轨道与站台、水系、用地、树点、灯位、地标、水柱交点：字段由 WP-47 定，经主代理并入
}
// 与外围有关的 IWorldModel.Outskirts、RoamLimits 与 MapEnvironment 的字段、SurfaceTag.Boundary 分别在 §3 覆盖层块、多地图块、碰撞块
