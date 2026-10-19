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
        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _auxiliaryCodes =
            new Dictionary<string, IReadOnlyDictionary<string, string>>();
        private IReadOnlyDictionary<BakeKey, string> _auxiliaryStamps = new Dictionary<BakeKey, string>();

        private void PrepareAuxiliaryInputs(LightInventoryDocument lighting)
        {
            _auxiliaryInputs = PreparedTiles.Auxiliary.ToDictionary(entry => entry.Key);
            var vegetation = Codes(value => value is IVegetationRuntimeSource);
            var fauna = Codes(value => value is IFaunaContentSource);
            var light = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in Modules)
                if (pair.Value.Value is ILightMetadataSource or IWindowMetadataSource or IWindowNumberingProvider)
                    light.Add(pair.Key, pair.Value.CodeHash);
            foreach (var pair in Interiors) light.Add(pair.Key, pair.Value.CodeHash);
            _auxiliaryCodes = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
            {
                [BakeModuleIds.VegetationTrees] = vegetation,
                [BakeModuleIds.VegetationGrass] = vegetation,
                [BakeModuleIds.FaunaRuntime] = fauna,
                [BakeModuleIds.NightlightLamps] = light,
                [BakeModuleIds.NightlightWindows] = light
            };
            var hash = new BakeInputHash();
            hash.Add(new { lighting.Nightlight, lighting.WindowNumbering });
            var lightingStamp = hash.Finish();
            _auxiliaryStamps = PreparedTiles.Auxiliary.ToDictionary(entry => entry.Key, entry =>
            {
                var stamp = new BakeInputHash();
                stamp.Add(entry.Bounds);
                if (entry.Key.Module is BakeModuleIds.NightlightLamps or BakeModuleIds.NightlightWindows) stamp.Add(lightingStamp);
                return stamp.Finish();
            });

            IReadOnlyDictionary<string, string> Codes(Func<IContentModule, bool> accepts) =>
                new ReadOnlyDictionary<string, string>(Modules.Where(pair => accepts(pair.Value.Value))
                    .ToDictionary(pair => pair.Key, pair => pair.Value.CodeHash, StringComparer.Ordinal));
        }

        internal bool TryAuxiliaryInput(BakeKey key, out BakeTileEntry entry,
            out IReadOnlyDictionary<string, string> dependencies, out string auxiliaryStamp)
        {
            if (_auxiliaryInputs.TryGetValue(key, out entry!))
            {
                dependencies = _auxiliaryCodes[key.Module];
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
        if (key.Module is BakeModuleIds.VegetationTrees or BakeModuleIds.VegetationGrass or BakeModuleIds.FaunaRuntime or
            BakeModuleIds.NightlightLamps or BakeModuleIds.NightlightWindows)
        {
            if (!map.TryAuxiliaryInput(key, out _, out _, out _)) throw new InvalidDataException("Auxiliary key is absent from its prepared directory.");
            var valid = key.Module switch
            {
                BakeModuleIds.VegetationTrees or BakeModuleIds.VegetationGrass => product is VegetationTileContent,
                BakeModuleIds.FaunaRuntime => product is FaunaTileContent,
                BakeModuleIds.NightlightLamps or BakeModuleIds.NightlightWindows => product is NightlightTileContent,
                _ => false
            };
            if (!valid) throw new InvalidDataException("Auxiliary producer returned the wrong fixed payload.");
        }
    }
}
