using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Ground;

/// <summary>
/// Ground, roads and paving for the whole map (WP-15): lawn, carriageways with kerbs, sidewalks, plazas, courts,
/// parking, lawn edging, centre lines and zebra crossings, plus the walking height field. It owns no feature: it is
/// the default ground everywhere; zone packages that claim the Ground aspect of an area replace it there later.
/// </summary>
[ContentModule("ground", Owner = "WP-15")]
public sealed class GroundModule : IGroundCoverSource
{
    /// <summary>Tiles beyond the source data that still get lawn (the campus edge before the outskirts take over).</summary>
    public const int MarginTiles = 2;

    private readonly GroundSection _section;

    public GroundModule() : this(GroundSection.Default) { }

    internal GroundModule(GroundSection section)
    {
        _section = section ?? throw new ArgumentNullException(nameof(section));
    }

    public IReadOnlyList<Claim> Claims => [];

    public GroundCoverData PrepareGroundCover(BuildContext context)
    {
        var masks = GroundMasks.Build(context, _section);
        return new(new GroundMaskGrid(masks.Rect, masks.Width, masks.Height, masks.MetersPerPixel), masks.Mask1, masks.Mask2);
    }

    /// <summary>Per-tile upper bounds measured on Qingshuihe (G8 checks the totals); draw calls are colour slots.</summary>
    public ModuleBudget Budget { get; } = new(TrianglesL0: 60000, TrianglesL1: 30000, DrawCalls: 12);

    /// <summary>L0 tiles: the map's tile range clipped to the campus data plus a margin, inside the data rectangle.</summary>
    public IEnumerable<TileKey> Tiles(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return TileKeys(ctx.Map);
    }

    internal static IEnumerable<TileKey> TileKeys(Contracts.Maps.MapDescriptor map)
    {
        var geo = map.Geo;
        var first = TileGrid.At(geo.BoundaryMin);
        var x0 = first.X - MarginTiles;
        var z0 = first.Z - MarginTiles;
        var x1 = (int)MathF.Ceiling(geo.BoundaryMax.X / TileGrid.BaseEdge) - 1 + MarginTiles;
        var z1 = (int)MathF.Ceiling(geo.BoundaryMax.Y / TileGrid.BaseEdge) - 1 + MarginTiles;
        var dataFirst = TileGrid.At(map.DataRect.Min);
        var rx0 = dataFirst.X;
        var rz0 = dataFirst.Z;
        var rx1 = (int)MathF.Ceiling(map.DataRect.Max.X / TileGrid.BaseEdge) - 1;
        var rz1 = (int)MathF.Ceiling(map.DataRect.Max.Y / TileGrid.BaseEdge) - 1;
        x0 = Math.Max(Math.Max(x0, rx0), map.TileMin.X);
        z0 = Math.Max(Math.Max(z0, rz0), map.TileMin.Z);
        x1 = Math.Min(Math.Min(x1, rx1), map.TileMax.X);
        z1 = Math.Min(Math.Min(z1, rz1), map.TileMax.Z);
        for (var z = z0; z <= z1; z++)
            for (var x = x0; x <= x1; x++)
                yield return new TileKey(x, z);
    }

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var layout = GroundLayout.Of(ctx.World);
        var extent = GroundLod.Tile(tile, lod, ctx.Map.DataRect);
        // Surfaces are classified on the flat plan, then draped: every vertex takes the terrain grid's height (D-180).
        var terrain = TerrainGrid.For(ctx.World.Terrain, tile, lod, extent);
        var field = TileField.Build(layout, extent, _section, terrain, LawnNoise.Seed(ctx.WorldSeed), GroundLod.ToneCell(lod));
        var sink = new MeshSink(SurfaceMaterialCatalog.FromInputs(ctx.SourceInputs));
        new GroundMesher(field, _section, lod, layout, terrain, sink).Run();
        if (!GroundLod.HasMarkings(lod)) return new ContentOutput(sink.Finish(), []);
        var markings = layout.Markings(_section);
        markings.Emit(extent, sink, _section, terrain);
        return new ContentOutput([.. sink.Finish(), .. markings.EmitManholeText(ctx, extent, _section, terrain)], []);
    }

    public IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile) =>
        [GroundCollision.Build(ctx, tile, _section)];
}
