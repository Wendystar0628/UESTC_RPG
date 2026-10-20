using Campus.Gen.Geometry;
using Campus.Gen.Sites;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixLayout;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixConnections;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixWindows;

namespace Campus.Gen.Maps.Qingshuihe.West.Six;

/// <summary>六号科研楼外观装配：依来源块顺序构建石质立面，再构建入口、连接体和塔楼。</summary>
internal static class SixBuilding
{
    internal static void Build(SiteBuilder b, string? entity)
    {
        var world = b.Context.World;
        var volumes = Volumes(world);
        foreach (var k in Blocks.Where(k => entity is null || k.Entity == entity))
        {
            if (!b.Includes(SiteRanges.Bounds(Plan(world, k.Entity, k.Part).Outer, S.ApronDepth))) continue;
            var plan = Plan(world, k.Entity, k.Part);
            var y = BaseY(b.Context, k.Entity);
            var style = Style(k.Upper(world));
            var windowed = GridFacade.Layout(plan, style, e => Dress(e, volumes)).Columns > 0;
            GridFacade.Build(b, plan, y + S.BaseHeight, style, e => Dress(e, volumes), windowed ? Numbering(k) : null);
            SixGlassBase.Build(b, k.Entity, plan, y, style, volumes);
            var roof = y + S.BaseHeight + style.RoofTop;
            SixRoofPlant.Build(b, S.Plants(world)[Blocks.ToList().IndexOf(k)], roof);
        }
        if (entity is not null && entity != S.EntityC)
        {
            foreach (var at in S.ShutDoors(world).Where(at => ShutEntity(b.Context.World, at) == entity && b.Includes(SiteRanges.Point(at, S.ApronWidth))))
                SixEntrances.ShutDoor(b, at);
            return;
        }
        var yc = BaseY(b.Context, S.EntityC);
        var (face, door) = EastFace(world, EastDoor(world));
        if (b.Includes(SiteRanges.Point(EastDoor(world), S.SignWidth)))
        {
            SixEntrances.Vestibule(b, face, door, yc);
            SixCrownSign.Build(b, face, yc + S.BaseHeight + Style(S.UpperC).Top);
        }
        foreach (var at in S.ShutDoors(world).Where(at => (entity is null || ShutEntity(world, at) == entity) && b.Includes(SiteRanges.Point(at, S.ApronWidth))))
            SixEntrances.ShutDoor(b, at);
        if (b.Includes(SiteRanges.Bounds(Plan(world, S.EntityC, S.LinkAB).Outer, S.PierDepth)))
            SixConnections.LinkAB(b, Plan(world, S.EntityC, S.LinkAB).Outer, yc);
        if (b.Includes(SiteRanges.Bounds(Plan(world, S.EntityC, S.LinkBC).Outer, S.ApronDepth)) || Bridges(world).Any(bridge => b.Includes(SiteRanges.Bounds([bridge.From, bridge.To], S.Recess))))
            SixConnections.LinkBC(b, world, yc);
        if (b.Includes(SiteRanges.Bounds(Plan(world, S.EntityC, S.Tower).Outer, S.PierDepth)))
            SixTower.Build(b, Plan(world, S.EntityC, S.Tower).Outer, yc);
    }
}
