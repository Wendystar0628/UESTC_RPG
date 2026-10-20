using Campus.Contracts;
using Campus.Gen.Sites;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;
using W = Campus.Gen.Maps.Qingshuihe.West.WestTune.Windows;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixLayout;

namespace Campus.Gen.Maps.Qingshuihe.West.Six;

/// <summary>六号科研楼的窗格编号与声明，使用与石质体量相同的立面布局。</summary>
internal static class SixWindows
{
    internal static GridNumbering Numbering(Block k) => new(new(k.Entity), k.Key, 0, W.Research, W.Lab) { AltCard = W.Office, AltShare = W.OfficeShare };

    internal static IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> Enumerate(IWorldModel world)
    {
        var volumes = Volumes(world);
        foreach (var k in Blocks)
        {
            var layout = GridFacade.Layout(Plan(world, k.Entity, k.Part), Style(k.Upper(world)), e => Dress(e, volumes));
            if (layout.Columns > 0) yield return (new(k.Entity), new(k.Key, layout.Columns, layout.Rows));
        }
    }

    internal static void Describe(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var volumes = Volumes(ctx.World);
        foreach (var k in Blocks)
        {
            var plan = Plan(ctx.World, k.Entity, k.Part);
            var style = Style(k.Upper(ctx.World));
            if (GridFacade.Layout(plan, style, e => Dress(e, volumes)).Columns == 0) continue;
            GridFacade.Declare(facades, plan, BaseY(ctx, k.Entity) + S.BaseHeight, style, e => Dress(e, volumes), Numbering(k),
                WindowNumbers.Required(ctx, new(k.Entity), k.Key));
        }
    }
}
