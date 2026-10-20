using Campus.Gen.Geometry;
using Campus.Contracts;
using Campus.Gen.World;
using Campus.Gen.Sites;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixLayout;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixConnections;

namespace Campus.Gen.Maps.Qingshuihe.West.Six;

/// <summary>六号科研楼的内容入口：按 A/B/C 来源交付外观、碰撞与窗格声明（MAP-QSH-WEST）。</summary>
public sealed class SixSite : ZoneSite
{
    public override string Id => "19a.six";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [.. new[] { S.EntityA, S.EntityB, S.EntityC }.Select(id => SiteRanges.Bounds(world.Feature(id), S.ApronDepth)),
         .. Bridges(world).Select(bridge => SiteRanges.Bounds([bridge.From, bridge.To], S.Recess))];

    public override IReadOnlyList<Claim> Claims =>
        [.. new[] { S.EntityA, S.EntityB, S.EntityC }.Select(e => (Claim)new FeatureClaim(new(e), Aspects.Massing | Aspects.Facade | Aspects.Roof))];

    public override IEnumerable<ContentSourceTrace> TraceSources(BuildContext context, LodLevel lod)
    {
        foreach (var block in Blocks)
        {
            var contours = new List<PolygonRings> { Plan(context.World, block.Entity, block.Part) };
            if (block.Entity == S.EntityC)
                contours.AddRange(new[] { S.LinkAB, S.LinkBC, S.Tower }.Select(part => Plan(context.World, S.EntityC, part)));
            yield return SiteSourceTrace.Build(context, lod, $"{Id}/{block.Key}", new(block.Entity),
                b => SixBuilding.Build(b, block.Entity), c => SixCollision.Build(c, block.Entity), contours);
        }
    }

    public override void Build(SiteBuilder b) => SixBuilding.Build(b, null);

    public override void Collide(SiteColliders c) => SixCollision.Build(c, null);

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world) =>
        SixWindows.Enumerate(world);

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows) =>
        SixWindows.Describe(ctx, facades, windows);
}
