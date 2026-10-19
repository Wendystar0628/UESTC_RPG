using System.Numerics;
using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Zones.Common;

namespace Campus.Gen.Zones.North.Identity;

/// <summary>The shopping street's authored open arcade replaces its former solid building footprint.</summary>
public sealed class ShoppingStreetSite : ZoneSite
{
    private static readonly RailingPart Rail = new();
    public override string Id => "20c.shopping-street";

    private static Campus.Contracts.Maps.MapRect BlockRange(Block block) => SiteRanges.Bounds(block.Rings.Outer,
        MathF.Max(Style(block).CorniceProjection, Style(block).PierDepth) + new RailingParams().Depth);

    private static Campus.Contracts.Maps.MapRect ArcadeRange(IWorldModel world)
    {
        var record = world.Feature(NorthFeatureIds.ShoppingAxis);
        return SiteRanges.Bounds(record, record.Number("width") * .5f + new RailingParams().Depth);
    }

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [.. Blocks(world).Select(BlockRange), ArcadeRange(world)];
    public override IReadOnlyList<Claim> Claims =>
        [new FeatureClaim(new(NorthFeatureIds.Shopping.Entity), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    private sealed record Block(string Key, PolygonRings Rings, float Height, int Storeys);

    private static IEnumerable<Block> Blocks(IWorldModel world)
    {
        var feature = world.Feature(NorthFeatureIds.Shopping.Entity);
        foreach (var source in new[] { NorthFeatureIds.Shopping.Part, NorthFeatureIds.ShoppingPavilion.Part })
        {
            var part = feature.Part(source);
            var (height, storeys) = part.Levels();
            var polygons = WorldFeatureGeometry.PolygonsOf(WorldFeatureGeometry.FacadeGeometryOf(part));
            for (var i = 0; i < polygons.Count; i++)
                yield return new(polygons.Count == 1 ? source : source + "#" + i.ToString(CultureInfo.InvariantCulture),
                    polygons[i], height, storeys);
        }
    }

    private static GridStyle Style(Block block)
    {
        var height = block.Height;
        var storeys = block.Storeys;
        var trim = NorthTune.Look.Trim;
        var wall = ZoneLooks.S("wall.dormYellow", ToonFx.BevelLift);
        var defaults = new GridStyle(wall, trim, NorthTune.Look.Plinth, NorthTune.Look.Glass,
            NorthTune.Look.Stainless, NorthTune.Look.RoofSlab);
        var floor = (height - defaults.Cornice - defaults.Parapet) / storeys;
        return defaults with { GroundStorey = floor, Storey = floor, Storeys = storeys,
            GroundSill = 0, GroundWindow = floor - new PanelParams().Depth,
            Sill = new PanelParams().Depth, Window = floor - new PanelParams().Depth * 2 };
    }

    private static GridEdge Dress(GridEdgeInfo edge) => new(edge.Length < new GridStyle(
        NorthTune.Look.Trim, NorthTune.Look.Trim, NorthTune.Look.Trim, NorthTune.Look.Glass,
        NorthTune.Look.Steel, NorthTune.Look.RoofSlab).Bay ? EdgeKind.Blank : EdgeKind.Plain);

    private static GridNumbering Numbering(IWorldModel world, Block block) => new(new(NorthFeatureIds.Shopping.Entity),
        block.Key, 0, new("window.general"), checked((byte)world.Feature(NorthFeatureIds.ShoppingAxis).Number("windowCard")));

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod) =>
        [.. Blocks(world).Select(b => b.Rings)];

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world)
    {
        foreach (var block in Blocks(world))
        {
            var layout = GridFacade.Layout(block.Rings, Style(block), Dress);
            if (layout.Columns > 0) yield return (new(NorthFeatureIds.Shopping.Entity), new(block.Key, layout.Columns, layout.Rows));
        }
    }

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        foreach (var block in Blocks(ctx.World))
        {
            var style = Style(block);
            if (GridFacade.Layout(block.Rings, style, Dress).Columns == 0) continue;
            var feature = new FeatureId(NorthFeatureIds.Shopping.Entity);
            GridFacade.Declare(facades, block.Rings, ctx.World.Terrain.PadY(feature), style, Dress,
                Numbering(ctx.World, block), WindowNumbers.Required(ctx, feature, block.Key));
        }
    }

    public override void Build(SiteBuilder b)
    {
        var world = b.Context.World;
        var feature = world.Feature(NorthFeatureIds.Shopping.Entity);
        foreach (var block in Blocks(world))
        {
            if (!b.Includes(BlockRange(block))) continue;
            var blockY = world.Terrain.PadY(feature.Id);
            var style = Style(block);
            var grid = GridFacade.Layout(block.Rings, style, Dress);
            GridFacade.Build(b, block.Rings, blockY, style, Dress, grid.Columns > 0 ? Numbering(world, block) : null);
        }
        if (!b.Includes(ArcadeRange(world))) return;
        var y = world.Terrain.PadY(feature.Id);
        var record = world.Feature(NorthFeatureIds.ShoppingAxis);
        var (a, z) = record.PointPair();
        var frame = PlanFrame.Toward((a + z) * .5f, z);
        var length = Vector2.Distance(a, z);
        var half = record.Number("width") * .5f;
        var balcony = record.Number("balcony");
        var floor = Style(Blocks(world).First(block => block.Storeys == feature.Storeys)).GroundStorey;
        var slab = new RoofParams().Height;
        for (var level = 1; level < feature.Storeys; level++)
            foreach (var side in new[] { -1f, 1f })
            {
                var offset = side * (half - balcony * .5f);
                NorthIdentityGeometry.Box(b, frame, 0, y + floor * level - slab, offset, length, slab, balcony, NorthTune.Look.Trim);
                var rp = new RailingParams(length) { Variation = 1, Material = NorthTune.Look.Glass, Foot = FootMat.Metal };
                b.Add(Rail.Lods[b.Lod](rp), frame.At(0, y + floor * level, side * (half - balcony)));
            }
        foreach (var id in new[] { NorthFeatureIds.ShoppingBridge2, NorthFeatureIds.ShoppingBridge3 })
        {
            var bridge = world.Feature(id);
            var p = frame.Local(bridge.Point()); var deckY = y + bridge.Number("floor") * floor;
            var width = bridge.Number("width");
            NorthIdentityGeometry.Box(b, frame, p.X, deckY - slab, 0, width, slab,
                record.Number("width"), NorthTune.Look.Trim);
            var rp = new RailingParams(record.Number("width")) { Variation = 1, Material = NorthTune.Look.Glass };
            foreach (var side in new[] { -1f, 1f })
                b.Add(Rail.Lods[b.Lod](rp), frame.AtTurned(p.X + side * width * .5f, deckY, 0));
        }
        var arch = new ArchedCanopyParams(record.Number("width"), 0, record.Number("archRise"), length,
            record.Number("ribPitch"), NorthTune.Look.Glass, NorthTune.Look.Steel);
        b.Add(ArchedCanopy.Build(arch, b.Lod), frame.AtTurned(0, y + feature.Height!.Value, 0));
    }

    public override void Collide(SiteColliders c)
    {
        foreach (var block in Blocks(c.Context.World))
            if (c.Includes(BlockRange(block)))
                GridFacade.Collide(c, block.Rings, c.Context.World.Terrain.PadY(new(NorthFeatureIds.Shopping.Entity)), Style(block), Dress, FootMat.Concrete);
        // Upper galleries and bridges are blocked at their entrances; the ground-floor arcade stays open at both ends.
    }
}
