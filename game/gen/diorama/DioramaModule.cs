using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Diorama;

/// <summary>
/// The diorama's resident backdrop (WLD-DIORAMA; ENG-PERF "远景剪影 L4 常驻"): the fog sea, the city rings and the
/// far ring with the snow mountains, one surface each, all on diorama_far.gdshader, none casting shadows, no colliders.
/// It is a whole-map product: Build answers L4 tile (0, 0) only, every other level and tile is empty; Tiles reports the
/// single L0 cell that holds the map centre so a streamer that keys products by occupied cells still finds it.
/// Inputs: the map's backdrop file (<c>MapDescriptor.Env.Backdrop</c>) from <c>BuildContext.SourceInputs</c>, the data
/// rectangle (the ground's extent) or the outskirts cut line for the fog sea's inner edge. A map without that file has no
/// backdrop product (Tiles is empty). Section and water-column surfaces (diorama_section) are not built here yet.
/// </summary>
[ContentModule("diorama", Owner = "WP-22")]
public sealed class DioramaModule : IContentModule
{
    private readonly object _gate = new();
    private (IWorldModel World, MapId Map, IReadOnlyList<GeometryBundle> Meshes)? _cache;

    public IReadOnlyList<Claim> Claims => [];

    // ENG-BUDGET "diorama": 3 % of main triangles (75 k fine), 2 % of draw calls (30); no shadow casting at all.
    public ModuleBudget Budget { get; } = new(75_000, 75_000, 30);

    public IEnumerable<TileKey> Tiles(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (Data(ctx) is null) return [];
        var c = Centre(ctx);
        return [TileGrid.At(c)];
    }

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (lod != LodLevel.L4 || tile != new TileKey(0, 0)) return ContentOutput.Empty;
        var meshes = Meshes(ctx);
        return meshes.Count == 0 ? ContentOutput.Empty : new(meshes, []);
    }

    /// <summary>The parsed backdrop file of the map, or null when the map ships none.</summary>
    public static BackdropData? Data(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var name = ctx.Map.Env.Backdrop;
        return ctx.SourceInputs.TryGetValue(name, out var bytes) ? BackdropData.Parse(bytes.Span) : null;
    }

    /// <summary>Centre of the rings and of the parallax: the data rectangle's centre, the same point the shader takes from map_rect.</summary>
    public static Vector2 Centre(BuildContext ctx) => (ctx.Map.DataRect.Min + ctx.Map.DataRect.Max) * 0.5f;

    private IReadOnlyList<GeometryBundle> Meshes(BuildContext ctx)
    {
        lock (_gate)
        {
            if (_cache is { } c && ReferenceEquals(c.World, ctx.World) && c.Map == ctx.Map.Id) return c.Meshes;
            var meshes = Generate(ctx);
            _cache = (ctx.World, ctx.Map.Id, meshes);
            return meshes;
        }
    }

    private static IReadOnlyList<GeometryBundle> Generate(BuildContext ctx)
    {
        var data = Data(ctx);
        if (data is null) return [];
        var centre = Centre(ctx);
        var seed = StableHash.Hash32(ctx.WorldSeed, "diorama", ctx.Map.Id.Value);
        var rect = new MapRectLike(ctx.Map.DataRect.Min, ctx.Map.DataRect.Max);
        var cut = ctx.World.Outskirts?.CutLine;
        var meshes = new List<GeometryBundle> { FogSea.Build(data.FogSea, centre, cut, rect, seed) };
        for (var i = 0; i < data.CityRings.Count; i++) meshes.Add(CityRing.Build(data, data.CityRings[i], i, centre, seed));
        meshes.Add(FarRing.Build(data.FarRing, data.Snow, centre, seed));
        return meshes;
    }
}
