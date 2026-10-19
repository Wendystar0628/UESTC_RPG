using System.Collections.ObjectModel;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Fauna;
using Campus.Contracts.Vegetation;

namespace Campus.Kernel.Bake;

public sealed partial class BakeService
{
    internal sealed partial record MapBuild
    {
        private IReadOnlyDictionary<BakeKey, BakeTileEntry> _auxiliaryInputs = new Dictionary<BakeKey, BakeTileEntry>();
        private IReadOnlyDictionary<BakeKey, IReadOnlyDictionary<string, string>> _auxiliaryCodes =
            new Dictionary<BakeKey, IReadOnlyDictionary<string, string>>();
        private IReadOnlyDictionary<BakeKey, string> _auxiliaryStamps = new Dictionary<BakeKey, string>();

        private void PrepareAuxiliaryInputs(LightInventoryDocument lighting)
        {
            _auxiliaryInputs = PreparedTiles.Auxiliary.ToDictionary(entry => entry.Key);
            var codes = new Dictionary<BakeKey, IReadOnlyDictionary<string, string>>();
            foreach (var entry in PreparedTiles.Auxiliary)
            {
                var key = entry.Key;
                var producer = Producers.Single(value => value.Value.CanBuild(key)).Value;
                var lightingInput = (producer.Inputs(key).Kinds & BakeInputKinds.Lighting) != 0;
                var sources = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (var pair in Modules)
                    if (producer.UsesModule(pair.Value.Value) && (!lightingInput || PreparedTiles.Modules[pair.Key].Any(tile =>
                        (tile.X + 1) * TileGrid.BaseEdge >= entry.Bounds.Min.X && tile.X * TileGrid.BaseEdge <= entry.Bounds.Max.X &&
                        (tile.Z + 1) * TileGrid.BaseEdge >= entry.Bounds.Min.Z && tile.Z * TileGrid.BaseEdge <= entry.Bounds.Max.Z)))
                        sources.Add(pair.Key, pair.Value.CodeHash);
                if (lightingInput)
                    foreach (var pair in Interiors)
                        if (pair.Value.Value.FeatureIds.Any(id => Context.World.Features.TryGetValue(id, out var feature) &&
                            feature.Bounds.Min.X <= entry.Bounds.Max.X && feature.Bounds.Max.X >= entry.Bounds.Min.X &&
                            feature.Bounds.Min.Z <= entry.Bounds.Max.Z && feature.Bounds.Max.Z >= entry.Bounds.Min.Z))
                            sources.Add(pair.Key, pair.Value.CodeHash);
                codes.Add(key, new ReadOnlyDictionary<string, string>(sources));
            }
            _auxiliaryCodes = codes;
            var hash = new BakeInputHash();
            hash.Add(new { lighting.Nightlight, lighting.WindowNumbering });
            var lightingStamp = hash.Finish();
            _auxiliaryStamps = PreparedTiles.Auxiliary.ToDictionary(entry => entry.Key, entry =>
            {
                var stamp = new BakeInputHash();
                stamp.Add(entry.Bounds);
                var producer = Producers.Single(value => value.Value.CanBuild(entry.Key)).Value;
                if ((producer.Inputs(entry.Key).Kinds & BakeInputKinds.Lighting) != 0) stamp.Add(lightingStamp);
                return stamp.Finish();
            });
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
    }

    private static void RequireAuxiliaryProduct(MapBuild map, BakeKey key, object product)
    {
        if (product is NightlightTileContent lights && lights.LampIds.Any(id => id >= map.Metadata!.Lighting.Nightlight.Lamps.Count))
            throw new InvalidDataException("Nightlight tile references an absent resident catalog lamp.");
        var producer = map.Producers.SingleOrDefault(value => value.Value.CanBuild(key));
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
                !map.TryAuxiliaryInput(key, out _, out _, out _)) throw new InvalidDataException("Auxiliary key is absent from its prepared directory.");
            if (!valid) throw new InvalidDataException("Auxiliary producer returned the wrong fixed payload.");
        }
    }
}
