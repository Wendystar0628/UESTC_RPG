using Campus.Gen.BakeInput;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using static Campus.Kernel.Bake.BakeService;
using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts.Fauna;
using Campus.Contracts.Vegetation;

namespace Campus.Kernel.Bake;

internal sealed class BakeAuxiliaryInputs
{
    private IReadOnlyDictionary<BakeKey, BakeTileEntry> _auxiliaryInputs = new Dictionary<BakeKey, BakeTileEntry>();
    private IReadOnlyDictionary<BakeKey, IReadOnlyDictionary<string, string>> _auxiliaryCodes =
        new Dictionary<BakeKey, IReadOnlyDictionary<string, string>>();
    private IReadOnlyDictionary<BakeKey, string> _auxiliaryStamps = new Dictionary<BakeKey, string>();

    internal void Prepare(PreparedBakeTiles tiles, BakeContentSource content, IMapContext context, LightInventoryDocument lighting)
    {
        _auxiliaryInputs = tiles.Auxiliary.ToDictionary(entry => entry.Key);
        var codes = new Dictionary<BakeKey, IReadOnlyDictionary<string, string>>();
        foreach (var entry in tiles.Auxiliary)
        {
            var key = entry.Key;
            var producer = content.Producers.Single(value => value.Value.CanBuild(key)).Value;
            var lightingInput = (producer.Inputs(key).Kinds & BakeInputKinds.Lighting) != 0;
            var sources = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in content.Modules)
                if (producer.UsesModule(pair.Value.Value) && (!lightingInput || tiles.Modules[pair.Key].Any(tile =>
                    (tile.X + 1) * TileGrid.BaseEdge >= entry.Bounds.Min.X && tile.X * TileGrid.BaseEdge <= entry.Bounds.Max.X &&
                    (tile.Z + 1) * TileGrid.BaseEdge >= entry.Bounds.Min.Z && tile.Z * TileGrid.BaseEdge <= entry.Bounds.Max.Z)))
                    sources.Add(pair.Key, pair.Value.CodeHash);
            if (lightingInput)
                foreach (var pair in content.Interiors)
                    if (pair.Value.Value.FeatureIds.Any(id => context.World.Features.TryGetValue(id, out var feature) &&
                        feature.Bounds.Min.X <= entry.Bounds.Max.X && feature.Bounds.Max.X >= entry.Bounds.Min.X &&
                        feature.Bounds.Min.Z <= entry.Bounds.Max.Z && feature.Bounds.Max.Z >= entry.Bounds.Min.Z))
                        sources.Add(pair.Key, pair.Value.CodeHash);
            codes.Add(key, new ReadOnlyDictionary<string, string>(sources));
        }
        _auxiliaryCodes = codes;
        _auxiliaryStamps = tiles.Auxiliary.ToDictionary(entry => entry.Key, entry =>
        {
            var stamp = new BakeInputHash();
            stamp.Add(entry.Bounds);
            var producer = content.Producers.Single(value => value.Value.CanBuild(entry.Key)).Value;
            if ((producer.Inputs(entry.Key).Kinds & BakeInputKinds.Lighting) != 0)
            {
                if (entry.Key.Module == BakeModuleIds.NightlightWindows) AddWindowInputs(stamp, lighting, entry);
                else stamp.Add(lighting.Nightlight.Lamps);
            }
            return stamp.Finish();
        });
    }

    private static void AddWindowInputs(BakeInputHash stamp, LightInventoryDocument lighting, BakeTileEntry entry)
    {
        var facades = lighting.Facades.Where(facade => Intersects(facade, entry.Bounds)).ToArray();
        var windows = lighting.Windows.Where(window => window.Center.X >= entry.Bounds.Min.X && window.Center.X <= entry.Bounds.Max.X &&
            window.Center.Z >= entry.Bounds.Min.Z && window.Center.Z <= entry.Bounds.Max.Z).ToArray();
        var bases = facades.Select(facade => facade.BaseId).Concat(windows.Select(window => window.BaseId)).ToHashSet();
        var allocations = lighting.WindowNumbering?.Allocations.Where(allocation => bases.Contains(allocation.BaseId)).ToArray() ?? [];
        stamp.Add(new { facades, windows, allocations });
    }

    private static bool Intersects(WindowFacadeDecl facade, Box3 bounds)
    {
        if (facade.Columns <= 0 || facade.Rows <= 0) return false;
        var right = Vector3.Normalize(facade.Right);
        var up = Vector3.Normalize(facade.Up);
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        for (var row = 0; row < facade.Rows; row++)
        {
            var first = facade.Origin + right * (facade.Width * 0.5f) + up *
                (facade.SillY[row] + facade.Height * 0.5f);
            var last = first + right * ((facade.Columns - 1) * facade.Pitch);
            min = Vector3.Min(min, Vector3.Min(first, last));
            max = Vector3.Max(max, Vector3.Max(first, last));
        }
        // The unshifted facade centers sit inside the baked halo bounds, including their stand-off.
        return min.X <= bounds.Max.X && max.X >= bounds.Min.X && min.Z <= bounds.Max.Z && max.Z >= bounds.Min.Z;
    }

    internal bool TryAuxiliaryInput(BakeKey key, out BakeTileEntry entry,
        out IReadOnlyDictionary<string, string> dependencies, out string auxiliaryStamp)
    {
        if (_auxiliaryInputs.TryGetValue(key, out entry!))
        {
            dependencies = _auxiliaryCodes[key];
            auxiliaryStamp = _auxiliaryStamps[key];
            return true;
        }
        dependencies = null!; auxiliaryStamp = "";
        return false;
    }

    internal static void RequireProduct(MapBuild map, BakeKey key, object product)
    {
        if (product is NightlightTileContent lights && lights.LampIds.Any(id => id >= map.Content.Metadata!.Lighting.Nightlight.Lamps.Count))
            throw new InvalidDataException("Nightlight tile references an absent resident catalog lamp.");
        var producer = map.Content.Producers.SingleOrDefault(value => value.Value.CanBuild(key));
        if (producer is not null)
        {
            var valid = producer.Value.ProductKind switch
            {
                BakeProductKind.Vegetation => product is VegetationTileContent,
                BakeProductKind.Fauna => product is FaunaTileContent,
                BakeProductKind.Nightlight => product is NightlightTileContent,
                BakeProductKind.Collision => product is CollisionTile,
                BakeProductKind.Map => product is Campus.Contracts.Atlas.MapBakeProduct,
                _ => throw new InvalidDataException("Unknown fixed bake payload kind.")
            };
            if ((producer.Value.ProductKind is BakeProductKind.Vegetation or BakeProductKind.Fauna or BakeProductKind.Nightlight) &&
                !map.Directory.Auxiliary.TryAuxiliaryInput(key, out _, out _, out _)) throw new InvalidDataException("Auxiliary key is absent from its prepared directory.");
            if (!valid) throw new InvalidDataException("Auxiliary producer returned the wrong fixed payload.");
        }
    }
}
