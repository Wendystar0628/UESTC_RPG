using System.Numerics;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixLayout;

namespace Campus.Gen.Maps.Qingshuihe.West.Six;

/// <summary>六号科研楼的屋顶设备与围挡。</summary>
internal static class SixRoofPlant
{
    internal static void Build(SiteBuilder b, Vector2 at, float roof)
    {
        if (b.Detail == SiteDetail.Mass) return;
        var local = Frame.Local(at);
        float hx = S.PlantSize.X * 0.5f, hz = S.PlantSize.Y * 0.5f;
        foreach (var s in new[] { -1f, 1f })
        {
            b.Place(PartCatalog.EquipmentFence, new RailingParams(S.PlantSize.X, S.PlantFence, 0.06f, 1.4f) { Variation = s > 0 ? 1 : 0 }, Frame.At(local.X, roof, local.Y + s * hz));
            b.Place(PartCatalog.EquipmentFence, new RailingParams(S.PlantSize.Y, S.PlantFence, 0.06f, 1.4f), Frame.AtTurned(local.X + s * hx, roof, local.Y));
        }
        foreach (var dx in new[] { -2.4f, 0f, 2.4f })
            b.Place(PartCatalog.HeatPump, new EquipmentParams(1.6f, 1.9f, 0.9f), Frame.At(local.X + dx, roof, local.Y));
    }
}
