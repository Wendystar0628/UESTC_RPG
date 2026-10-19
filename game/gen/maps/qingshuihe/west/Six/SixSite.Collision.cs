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
                var closed = new List<(float U0, float U1)>();
                var openings = BaseOpenings(world, k.Entity, e);
                foreach (var shut in S.ShutDoors(c.Context.World))
                {
                    var p = e.Frame.Local(shut);
                    if (MathF.Abs(p.Y - S.Recess) < 1.2f && p.X > 0 && p.X < e.Length)
                        closed.Add((p.X - S.ShutDoorWidth * 0.5f, p.X + S.ShutDoorWidth * 0.5f));
                }
                var levels = openings.SelectMany(o => new[] { o.Bottom, o.Top }).Append(0).Append(S.BaseHeight).Distinct().Order().ToArray();
                for (var j = 1; j < levels.Length; j++)
                {
                    var middle = (levels[j - 1] + levels[j]) * 0.5f;
                    var cuts = openings.Where(o => middle >= o.Bottom && middle <= o.Top).Select(o => (o.From, o.To)).Concat(closed);
                    foreach (var (u0, u1) in GridFacade.Open(0, e.Length, cuts))
                        c.Wall(e.Frame.P(u0, 0.1f), e.Frame.P(u1, 0.1f), 0.15f, y + levels[j - 1], y + levels[j], SurfaceTag.Wall, FootMat.Glass);
                }
                foreach (var cut in closed)
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
            {
                if (LinkOpenings(world, e).Any(o => x + S.LinkPier * 0.5f > o.From && x - S.LinkPier * 0.5f < o.To)) continue;
                c.Box(e.Frame.At(x, yc + S.BaseHeight * 0.5f, -S.LinkPier * 0.5f), new(S.LinkPier * 0.5f, S.BaseHeight * 0.5f, S.LinkPier * 0.5f), SurfaceTag.Wall, FootMat.StonePaving);
            }
    }
}
