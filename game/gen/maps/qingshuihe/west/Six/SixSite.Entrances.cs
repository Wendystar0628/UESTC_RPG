using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.West.WestTune.Look;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;
using W = Campus.Gen.Maps.Qingshuihe.West.WestTune.Windows;

namespace Campus.Gen.Maps.Qingshuihe.West.Six;

/// <summary>六号科研楼的入口：C 区东门玻璃门厅、关闭的玻璃门与楼顶红色标志牌。</summary>
public sealed partial class SixSite
{
    // ---- C 区东门: the open entrance ----

    /// <summary>
    /// A glass vestibule from the set-back glass out to the stone line under the soffit: two glazed cheeks, a stone roof
    /// with a fascia, a transom light over the open doors and side lights either side; a granite threshold apron in front.
    /// </summary>
    private static void Vestibule(SiteBuilder b, GridEdgeInfo face, float x, float y)
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

    // ---- Shut entrances ----

    private static void ShutDoor(SiteBuilder b, Vector2 at)
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

    private static string ShutEntity(IWorldModel world, Vector2 at)
    {
        var index = Array.IndexOf(S.ShutDoors(world), at);
        return world.Feature(WestIds.SixShutFaces[index]).Text("parent");
    }

    internal static (GridEdgeInfo Face, float X, float Y) ShutFace(BuildContext ctx, Vector2 at)
    {
        var index = Array.IndexOf(S.ShutDoors(ctx.World), at);
        var face = SelectedFace(ctx.World, WestIds.SixShutFaces[index]);
        return (face, face.Frame.Local(at).X, BaseY(ctx, ShutEntity(ctx.World, at)));
    }

    // ---- Crown sign: red emblem and silver characters ----

    /// <summary>A rigid placement on a face: local X along the face, local Y out of it, local Z down (an upright disc's plane).</summary>
    private static Matrix4x4 Upright(PlanFrame f, float x, float y, float z)
    {
        var p = f.P(x, z);
        return new(f.U.X, 0, f.U.Y, 0, f.W.X, 0, f.W.Y, 0, 0, -1, 0, 0, p.X, y, p.Y, 1);
    }

    private static void Sign(SiteBuilder b, GridEdgeInfo face, float top)
    {
        var f = face.Frame;
        var x = face.Length * 0.5f;
        float hw = S.SignWidth * 0.5f, y0 = top - S.SignDrop, y1 = top + S.SignRise, z1 = S.SignProud;
        var bevel = b.Detail == SiteDetail.Full ? 0.05f : 0f;
        GridFacade.Box(b, f, x - hw, x + hw, y0, y1, 0, z1, L.Stone, bevel);
        GridFacade.Box(b, f, x - hw - 0.12f, x + hw + 0.12f, y1, y1 + 0.22f, -0.1f, z1 + 0.12f, L.Trim, bevel);
        GridFacade.Box(b, f, x - hw - 0.08f, x + hw + 0.08f, y0 - 0.25f, y0, -0.1f, z1 + 0.08f, L.Trim, bevel);
        if (b.Detail == SiteDetail.Mass) return;
        // The emblem: a thin outer ring, an octagon and circuit traces ending in round pads, single colour (楼顶红).
        var r = S.EmblemDiameter * 0.5f;
        var centre = Upright(f, x, y1 - 0.45f - r, z1);
        var d = S.EmblemDepth;
        b.Add(Shapes.Ring(Vector2.Zero, r - 0.09f, 0.18f, d, 48, L.Red), centre);
        b.Add(Shapes.Ring(Vector2.Zero, r * 0.66f, 0.15f, d, 8, L.Red), centre);
        b.Add(Shapes.Ring(Vector2.Zero, r * 0.17f, 0.12f, d, 16, L.Red), centre);
        for (var i = 0; i < 6; i++)
        {
            var a = MathF.PI / 6f + i * MathF.PI / 3f;
            float c = MathF.Cos(a), s = MathF.Sin(a);
            var length = r * 0.38f;
            var mid = r * (0.23f + 0.19f);
            var at = new Matrix4x4(c, 0, s, 0, 0, 1, 0, 0, -s, 0, c, 0, c * mid, 0, s * mid, 1);
            b.Add(PartGeometryKernel.BeveledBox(length, d, 0.1f, 0, L.Red), GeometryPlacement.Multiply(at, centre));
            var pad = new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, c * r * 0.62f, 0, s * r * 0.62f, 1);
            b.Add(Shapes.Shaft(0.11f, 0.11f, d, 12, L.Red), GeometryPlacement.Multiply(pad, centre));
        }
        Framework.WestLettering.Write(b, "基础与前沿研究院", S.CharacterSize, L.Silver, L.Stone,
            f.At(x, y0 + 1.8f, z1 + .015f));
    }
}
