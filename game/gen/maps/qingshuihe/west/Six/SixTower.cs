using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.West.WestTune.Look;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;

namespace Campus.Gen.Maps.Qingshuihe.West.Six;

/// <summary>六号科研楼北侧石质楼梯塔。</summary>
internal static class SixTower
{
    internal static void Build(SiteBuilder b, IReadOnlyList<Vector2> ring, float y)
    {
        var plan = new PolygonRings(ring);
        b.Add(PartSolids.Extrude(plan, S.TowerHeight - 0.4f + 0.1f, L.Stone, y - 0.1f), Matrix4x4.Identity);
        b.Add(PartSweeps.Sweep(PartSweeps.BeveledRectangleProfile(0.7f, 0.4f, 0.04f), PlanMath.OutsideRight(ring), L.Trim, closed: true, baseY: y + S.TowerHeight - 0.4f), Matrix4x4.Identity);
        b.Add(PartSweeps.Sweep(PartSweeps.BeveledRectangleProfile(0.3f, 0.7f, 0.03f), PlanMath.OutsideRight(ring), L.DarkGrey, closed: true, baseY: y - 0.1f), Matrix4x4.Identity);
        if (b.Detail == SiteDetail.Mass) return;
        foreach (var (e, _, _) in GridFacade.Edges(ring))
        {
            if (e.Length < 12f) continue;
            var x = e.Length * 0.5f;
            GridFacade.Box(b, e.Frame, x - 0.8f, x + 0.8f, y + 1.2f, y + S.TowerHeight - 2.0f, -0.05f, 0.04f, L.Glass, 0);
            GridFacade.Box(b, e.Frame, x - 1.0f, x + 1.0f, y + S.TowerHeight - 2.0f, y + S.TowerHeight - 1.8f, -0.05f, 0.12f, L.Trim, 0);
        }
    }
}
