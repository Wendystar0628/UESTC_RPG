using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.Common;
using F = Campus.Gen.Zones.West.WestTune.Four;
using L = Campus.Gen.Zones.West.WestTune.Look;

namespace Campus.Gen.Zones.West.Four;

/// <summary>
/// 四号科研楼 (MAP-QSH-WEST). The three courtyard blocks are WP-14's (world/west.json gives them the research family);
/// this site adds the identity pieces the facade rules cannot express:
/// <list type="bullet">
/// <item>each block's entrance toward the fan lawn: a two-storey deep-blue glass box set in a white door frame whose head
/// carries the sign band (§6.3). All three are shut (WLD-INTERIORS): glass doors, the lobby card behind, DoorClosed.</item>
/// <item>the 0.45 m seat wall on the lawn's outer arc (MAP-QSH-TERRAIN); the lawn's dish is west.terrain.json's basin.</item>
/// </list>
/// It claims nothing: the blocks keep WP-14's massing and windows, the box stands in front of their stone line.
/// </summary>
public sealed class FourSite : ZoneSite
{
    public override string Id => "19b.four";

    internal static PlanFrame Frame((string Entity, string Key, Vector2 Door, float Facing) box) => PlanMath.Facing(box.Door, box.Facing);

    public override void Build(SiteBuilder b)
    {
        var world = b.Context.World;
        foreach (var box in F.Boxes(world)) GlassBox(b, Frame(box), b.Ground(box.Door), box.Key);
        foreach (var arc in F.SeatArcs(world)) SeatWall(b, SeatLine(world, arc));
    }

    // ---- The deep-blue glass box in its white frame ----

    private static void GlassBox(SiteBuilder b, PlanFrame f, float y, string key)
    {
        float hw = F.GlassWidth * 0.5f, d = F.GlassDepth, top = y + F.GlassHeight, m = F.FrameMember, front = d + F.FrameProud;
        float bevel = b.Detail == SiteDetail.Full ? 0.04f : 0f, head = top + F.SignBand;
        // White frame: two jambs and a head beam (the sign band) as one ring in front, a roof slab back to the stone line.
        foreach (var s in new[] { -1f, 1f })
            GridFacade.Box(b, f, s < 0 ? -hw - m : hw, s < 0 ? -hw : hw + m, y - 0.1f, head, d - 0.7f, front, L.WhiteFrame, bevel);
        GridFacade.Box(b, f, -hw - m, hw + m, top, head, d - 0.7f, front, L.WhiteFrame, bevel);
        GridFacade.Box(b, f, -hw - 0.1f, hw + 0.1f, top, head - 0.2f, -0.3f, d - 0.6f, L.WhiteFrame, bevel);
        // A thin coping proud of the head, so its top edge catches light (收边).
        GridFacade.Box(b, f, -hw - m - 0.08f, hw + m + 0.08f, head, head + 0.14f, d - 0.78f, front + 0.08f, L.Trim, bevel * 0.5f);
        if (b.Detail == SiteDetail.Mass)
        {
            GridFacade.Box(b, f, -hw, hw, y, top, -0.3f, d, L.DarkGrey, 0);
            return;
        }
        // Glass: the two cheeks back to the stone line, and the front around the door.
        float dw = F.DoorWidth * 0.5f, dh = y + F.DoorHeight;
        foreach (var s in new[] { -1f, 1f })
        {
            GridFacade.Box(b, f, s < 0 ? -hw : hw - 0.08f, s < 0 ? -hw + 0.08f : hw, y + 0.3f, top, -0.3f, d, L.BlueGlass, 0);
            GridFacade.Box(b, f, s < 0 ? -hw : dw + 0.1f, s < 0 ? -dw - 0.1f : hw, y + 0.3f, top, d - 0.08f, d, L.BlueGlass, 0);
        }
        GridFacade.Box(b, f, -dw - 0.1f, dw + 0.1f, dh + 0.2f, top, d - 0.08f, d, L.BlueGlass, 0);
        // Dark plinth strip under the glass, the transom at the first floor line and the door head.
        foreach (var s in new[] { -1f, 1f })
        {
            GridFacade.Box(b, f, s < 0 ? -hw - 0.02f : hw - 0.1f, s < 0 ? -hw + 0.1f : hw + 0.02f, y - 0.05f, y + 0.3f, -0.3f, d, L.DarkGrey, 0.01f);
            GridFacade.Box(b, f, s < 0 ? -hw : dw + 0.1f, s < 0 ? -dw - 0.1f : hw, y - 0.05f, y + 0.3f, d - 0.12f, d + 0.04f, L.DarkGrey, 0.01f);
        }
        var transom = y + F.Transom;
        GridFacade.Box(b, f, -hw, hw, transom - 0.14f, transom + 0.14f, d - 0.04f, d + 0.16f, L.Metal, 0.02f);
        foreach (var s in new[] { -1f, 1f })
            GridFacade.Box(b, f, s < 0 ? -hw - 0.16f : hw, s < 0 ? -hw : hw + 0.16f, transom - 0.14f, transom + 0.14f, -0.3f, d, L.Metal, 0.02f);
        GridFacade.Box(b, f, -dw - 0.12f, dw + 0.12f, dh, dh + 0.2f, d - 0.06f, d + 0.12f, L.Metal, 0.02f);
        // Mullions: slim aluminium fins on the front and on both cheeks (clear of the door).
        var bays = Math.Max(2, (int)MathF.Round(F.GlassWidth / F.MullionPitch));
        for (var i = 1; i < bays; i++)
        {
            var x = -hw + i * F.GlassWidth / bays;
            var y0 = MathF.Abs(x) < dw + 0.15f ? dh + 0.2f : y + 0.3f;
            GridFacade.Box(b, f, x - 0.04f, x + 0.04f, y0, top, d - 0.02f, d + 0.12f, L.Metal, 0);
        }
        var side = Math.Max(2, (int)MathF.Round(F.GlassDepth / F.MullionPitch));
        foreach (var s in new[] { -1f, 1f })
            for (var i = 1; i < side; i++)
            {
                var z = -0.3f + i * (d + 0.3f) / side;
                var x0 = s < 0 ? -hw - 0.12f : hw - 0.02f;
                GridFacade.Box(b, f, x0, x0 + 0.14f, y + 0.3f, top, z - 0.04f, z + 0.04f, L.Metal, 0);
            }
        // Shut glass doors with the lobby card behind them (WLD-INTERIORS), and the granite landing in front.
        b.Place(PartCatalog.DoorGlass, new DoorParams(F.DoorWidth, F.DoorHeight, 0.08f, 0f, 2) { Foot = FootMat.Glass }, f.AtReversed(0, y, d - 0.04f));
        b.Place(PartCatalog.InteriorCard, new PanelParams(F.GlassWidth - 1.2f, F.DoorHeight + 1.2f, 0.06f) { Variation = 1 }, f.AtReversed(0, y, d - 1.5f));
        GridFacade.Box(b, f, -hw - m, hw + m, y - 0.12f, y + F.PavingTop + 0.03f, d - 0.2f, front + F.LandingDepth, L.Granite, 0.02f);
        Framework.WestLettering.Write(b, "四号科研楼 " + key.ToUpperInvariant() + "区", F.SignBand * .6f,
            L.DarkGrey, L.WhiteFrame, f.At(0, top + F.SignBand * .5f, front + .015f));
    }

    // ---- Seat wall on the fan lawn's outer arc ----

    /// <summary>An arc of the lawn edge moved <see cref="F.SeatInset"/> into the grass, toward the fan's centre.</summary>
    internal static Vector2[] SeatLine(IWorldModel world, IReadOnlyList<Vector2> arc) =>
        [.. arc.Select(p => p + Vector2.Normalize(F.LawnCentre(world) - p) * F.SeatInset)];

    private static void SeatWall(SiteBuilder b, IReadOnlyList<Vector2> line)
    {
        var (y0, y1) = SeatSpan(line, b.Ground);
        var bevel = b.Detail == SiteDetail.Full ? 0.05f : 0.02f;
        b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(F.SeatWidth, y1 - F.SeatCap - y0, bevel), line, L.Granite, baseY: y0), Matrix4x4.Identity);
        if (b.Detail == SiteDetail.Mass) return;
        b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(F.SeatWidth + 0.08f, F.SeatCap, 0.025f), line, L.WhiteFrame, baseY: y1 - F.SeatCap), Matrix4x4.Identity);
    }

    /// <summary>Foot (a little under the lowest ground along the line) and the flat top, SeatHeight over the highest ground.</summary>
    private static (float Y0, float Y1) SeatSpan(IReadOnlyList<Vector2> line, Func<float, float, float> ground)
    {
        float lo = float.MaxValue, hi = float.MinValue;
        foreach (var p in line) { var g = ground(p.X, p.Y); lo = MathF.Min(lo, g); hi = MathF.Max(hi, g); }
        return (lo - 0.1f, hi + F.SeatHeight);
    }

    public override void Collide(SiteColliders c)
    {
        var world = c.Context.World;
        foreach (var box in F.Boxes(world))
        {
            var f = Frame(box);
            var y = c.Ground(box.Door.X, box.Door.Y);
            float hw = F.GlassWidth * 0.5f, d = F.GlassDepth, h = F.GlassHeight + F.SignBand;
            c.Box(f.At(0, y + h * 0.5f, (d - 0.3f) * 0.5f), new(hw, h * 0.5f, (d + 0.3f) * 0.5f), SurfaceTag.Wall, FootMat.Glass);
            foreach (var s in new[] { -1f, 1f })
                c.Box(f.At(s * (hw + F.FrameMember * 0.5f), y + h * 0.5f, d - 0.7f + (F.FrameProud + 0.7f) * 0.5f),
                    new(F.FrameMember * 0.5f, h * 0.5f, (F.FrameProud + 0.7f) * 0.5f), SurfaceTag.Wall, FootMat.StonePaving);
            // The shut front: walking into the recess between the jambs reports a closed door (WLD-INTERIORS).
            c.Wall(f.P(-hw, d + 0.12f), f.P(hw, d + 0.12f), 0.1f, y, y + F.DoorHeight + 0.4f, SurfaceTag.DoorClosed, FootMat.Glass);
        }
        foreach (var arc in F.SeatArcs(world))
        {
            var line = SeatLine(world, arc);
            var (y0, y1) = SeatSpan(line, c.Ground);
            for (var i = 0; i + 1 < line.Length; i++)
                c.Wall(line[i], line[i + 1], F.SeatWidth * 0.5f, y0, y1, SurfaceTag.Prop, FootMat.StonePaving);
        }
    }
}
