using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Zones.Common;
using L = Campus.Gen.Zones.West.WestTune.Look;
using S = Campus.Gen.Zones.West.WestTune.Six;
using W = Campus.Gen.Zones.West.WestTune.Windows;

namespace Campus.Gen.Zones.West.Six;

/// <summary>六号科研楼的碰撞：玻璃底座、石柱、门厅侧壁、连廊与塔楼的墙体。</summary>
public sealed partial class SixSite
{
    // ---- Collision ----

    public override void Collide(SiteColliders c) => Collide(c, null);

    private static void Collide(SiteColliders c, string? entity)
    {
        var world = c.Context.World;
        var volumes = Volumes(world);
        var eastDoor = EastDoor(world);
        var east = (Face: default(GridEdgeInfo), X: 0f);
        foreach (var k in Blocks.Where(k => entity is null || k.Entity == entity))
        {
            if (!c.Includes(SiteRanges.Bounds(Plan(world, k.Entity, k.Part).Outer, S.ApronDepth))) continue;
            var plan = Plan(world, k.Entity, k.Part);
            var y = BaseY(c.Context, k.Entity);
            var style = Style(k.Upper(world));
            GridFacade.Collide(c, plan, y + S.BaseHeight, style, e => Dress(e, volumes), FootMat.StonePaving);
            if (k.Entity == S.EntityC) east = EastFace(world, eastDoor);
            // The set-back glass at walking height: open between the vestibule cheeks, DoorClosed across each shut door.
            foreach (var (e, _, _) in GridFacade.Edges(BasePlan(plan)))
            {
                if (Dress(e, volumes, S.Recess).Kind == EdgeKind.Hidden) continue;
                var cuts = new List<(float U0, float U1, SurfaceTag Tag)>();
                var door = e.Frame.Local(eastDoor);
                if (k.Entity == S.EntityC && MathF.Abs(door.Y - S.Recess) < 1.2f && door.X > 0 && door.X < e.Length)
                    cuts.Add((door.X - S.VestibuleWidth * 0.5f, door.X + S.VestibuleWidth * 0.5f, SurfaceTag.Ground));
                foreach (var shut in S.ShutDoors(c.Context.World))
                {
                    var p = e.Frame.Local(shut);
                    if (MathF.Abs(p.Y - S.Recess) < 1.2f && p.X > 0 && p.X < e.Length)
                        cuts.Add((p.X - S.ShutDoorWidth * 0.5f, p.X + S.ShutDoorWidth * 0.5f, SurfaceTag.DoorClosed));
                }
                foreach (var (u0, u1) in GridFacade.Open(0, e.Length, cuts.Select(cut => (cut.U0, cut.U1))))
                    c.Wall(e.Frame.P(u0, 0.1f), e.Frame.P(u1, 0.1f), 0.15f, y, y + S.BaseHeight, SurfaceTag.Wall, FootMat.Glass);
                foreach (var cut in cuts.Where(cut => cut.Tag == SurfaceTag.DoorClosed))
                    c.Wall(e.Frame.P(cut.U0, 0.1f), e.Frame.P(cut.U1, 0.1f), 0.15f, y, y + S.BaseHeight, SurfaceTag.DoorClosed, FootMat.Glass);
            }
            foreach (var (x, f) in Columns(plan, style, volumes, eastDoor, world))
                c.Box(f.At(x, y + S.BaseHeight * 0.5f, S.PierDepth * 0.5f - 0.1f), new(S.ColumnSize * 0.5f + 0.06f, S.BaseHeight * 0.5f, S.ColumnSize * 0.5f + 0.06f), SurfaceTag.Wall, FootMat.StonePaving);
        }
        if (entity is not null && entity != S.EntityC) return;
        // Vestibule cheeks; the doorway between them stays open for WP-31b's hall (EntranceDecl.Open).
        var yc = BaseY(c.Context, S.EntityC);
        east = EastFace(world, eastDoor);
        var vf = east.Face.Frame;
        foreach (var s in new[] { -1f, 1f }.Where(_ => c.Includes(SiteRanges.Point(eastDoor, S.ApronWidth))))
            c.Wall(vf.P(east.X + s * S.VestibuleWidth * 0.5f, -S.Recess), vf.P(east.X + s * S.VestibuleWidth * 0.5f, 0), 0.15f, yc, yc + S.VestibuleHeight, SurfaceTag.Wall, FootMat.Glass);
        // Links and tower: the A–B link and the tower are closed volumes; the B–C link stands on its piers.
        foreach (var ring in new[] { Plan(world, S.EntityC, S.LinkAB).Outer, Plan(world, S.EntityC, S.Tower).Outer }.Where(ring => c.Includes(SiteRanges.Bounds(ring, S.PierDepth))))
            foreach (var (e, _, _) in GridFacade.Edges(ring))
                c.Wall(e.Frame.P(0, -0.15f), e.Frame.P(e.Length, -0.15f), 0.15f, yc, yc + S.TowerHeight, SurfaceTag.Wall, FootMat.StonePaving);
        foreach (var (e, _, _) in GridFacade.Edges(Plan(world, S.EntityC, S.LinkBC).Outer).Where(edge => c.Includes(SiteRanges.Bounds([edge.Item1.A, edge.Item1.B], S.LinkPier))))
            foreach (var x in new[] { S.LinkPier * 0.5f, e.Length * 0.5f })
                c.Box(e.Frame.At(x, yc + S.BaseHeight * 0.5f, -S.LinkPier * 0.5f), new(S.LinkPier * 0.5f, S.BaseHeight * 0.5f, S.LinkPier * 0.5f), SurfaceTag.Wall, FootMat.StonePaving);
    }
}
