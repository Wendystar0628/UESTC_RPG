using System.Numerics;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.West.WestTune.Look;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixLayout;

namespace Campus.Gen.Maps.Qingshuihe.West.Six;

/// <summary>六号科研楼的开放玻璃门厅与关闭入口。</summary>
internal static class SixEntrances
{
    internal static void Vestibule(SiteBuilder b, GridEdgeInfo face, float x, float y)
    {
        var f = face.Frame;
        float hw = S.VestibuleWidth * 0.5f, top = y + S.VestibuleHeight, back = -S.Recess - 0.2f;
        var bevel = b.Detail == SiteDetail.Full ? 0.04f : 0f;
        // Roof slab and fascia, and the cheeks' stone frames at the stone line.
        GridFacade.Box(b, f, x - hw - 0.3f, x + hw + 0.3f, top, top + 0.55f, back, 0.35f, L.Trim, bevel);
        foreach (var s in new[] { -1f, 1f })
            GridFacade.Box(b, f, x + s * hw - 0.2f, x + s * hw + 0.2f, y - 0.1f, top, -0.25f, 0.25f, L.Metal, bevel * 0.5f);
        if (b.Detail == SiteDetail.Mass) return;
        foreach (var s in new[] { -1f, 1f })
        {
            // Cheek glazing, running back to the set-back glass.
            var cheek = PlanFrame.Toward(f.P(x + s * hw, back), f.P(x + s * hw, -0.2f));
            b.Place(PartCatalog.CurtainWall, new WindowParams(-back - 0.2f, top - y - 0.1f, 0.1f, 2, 2), cheek.At((-back - 0.2f) * 0.5f, y, 0));
        }
        var side = hw - S.DoorWidth * 0.5f - 0.2f;
        foreach (var s in new[] { -1f, 1f })
            b.Place(PartCatalog.CurtainWall, new WindowParams(side, top - y - 0.1f, 0.1f, 1, 2), f.AtReversed(x + s * (S.DoorWidth * 0.5f + 0.1f + side * 0.5f), y, 0));
        b.Place(PartCatalog.CurtainWall, new WindowParams(S.DoorWidth, top - y - S.DoorHeight - 0.1f, 0.1f, 2, 1), f.AtReversed(x, y + S.DoorHeight, 0));
        // The doors stand open into the vestibule (WLD-INTERIORS: an open entrance's leaves are fixed open).
        b.Place(PartCatalog.DoorGlass, new DoorParams(S.DoorWidth, S.DoorHeight, 0.08f, 90f, 2) { Foot = FootMat.Glass }, f.AtReversed(x, y, 0));
        GridFacade.Box(b, f, x - S.ApronWidth * 0.5f, x + S.ApronWidth * 0.5f, y - 0.1f, y + S.PavingTop + 0.03f, -0.1f, S.ApronDepth, L.Granite, 0.02f);
    }

    internal static void ShutDoor(SiteBuilder b, Vector2 at)
    {
        if (b.Detail == SiteDetail.Mass) return;
        var (face, x, y) = ShutFace(b.Context, at);
        var f = face.Frame;
        var z = -S.Recess;
        b.Place(PartCatalog.DoorGlass, new DoorParams(S.ShutDoorWidth, S.ShutDoorHeight, 0.08f, 0f, 2) { Foot = FootMat.Glass }, f.AtReversed(x, y, z));
        GridFacade.Box(b, f, x - S.ShutDoorWidth * 0.5f - 0.12f, x + S.ShutDoorWidth * 0.5f + 0.12f, y + S.ShutDoorHeight, y + S.ShutDoorHeight + 0.18f, z - 0.05f, z + 0.12f, L.Metal, 0);
        // The lobby card behind the shut glass (WLD-INTERIORS): hall light, gate standby lamps, floor index.
        b.Place(PartCatalog.InteriorCard, new PanelParams(S.ShutDoorWidth + 1.6f, S.ShutDoorHeight + 0.6f, 0.06f) { Variation = 1 }, f.AtReversed(x, y, z - 1.4f));
    }
}
