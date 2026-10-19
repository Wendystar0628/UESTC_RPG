using Campus.Pipeline.Map;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Kernel.Bake;

public sealed record BakedEntrance(string Module, EntranceDecl Entrance);
public sealed record LightInventoryDocument(MapId Map,
    IReadOnlyList<LightEmitter> Lamps, IReadOnlyList<WindowFacadeDecl> Facades, IReadOnlyList<WindowDecl> Windows)
{
    public WindowNumberingSnapshot? WindowNumbering { get; init; }
}
public sealed record EntranceInventoryDocument(MapId Map,
    IReadOnlyList<BakedEntrance> Entrances);
internal sealed record BakeMapMetadata(LightInventoryDocument Lighting, EntranceInventoryDocument Entrances);
internal sealed record BakeResidentData(BakeMapMetadata Metadata, WindowMetadata Windows,
    BakeService.InteriorCoverage[] Coverage, InteriorLayout[] Layouts, BakeModuleDirectory[] Directory, TileKey[] TerrainPoints);
internal sealed record BakeModuleDirectory(string Module, TileKey[] Tiles);

/// <summary>Runtime read-only inventory; map preparation publishes all three immutable tables together.</summary>
[KernelService(typeof(BakeLightInventory), Owner = "WP-02a")]
[KernelService(typeof(ILightInventory), Owner = "WP-02a")]
public sealed class BakeLightInventory : ILightInventory
{
    private LightInventoryDocument _current = new(default, [], [], []);
    private long _revision;
    public long Rev => Interlocked.Read(ref _revision);
    public IReadOnlyList<LightEmitter> Lamps => Volatile.Read(ref _current).Lamps;
    public IReadOnlyList<WindowFacadeDecl> Facades => Volatile.Read(ref _current).Facades;
    public IReadOnlyList<WindowDecl> Windows => Volatile.Read(ref _current).Windows;
    public WindowNumberingSnapshot? WindowNumbering => Volatile.Read(ref _current).WindowNumbering;
    internal void Publish(LightInventoryDocument document)
    {
        if (document.WindowNumbering is { } snapshot && snapshot.Map != document.Map)
            throw new InvalidDataException("Lighting inventory and window numbering belong to different maps.");
        Volatile.Write(ref _current, document);
        Interlocked.Increment(ref _revision);
    }
    internal void Clear() => Publish(new(default, [], [], []));
}

internal static class BakeMapMetadataBuilder
{
    public static BakeMapMetadata Build(BakeService.MapBuild map)
    {
        var data = map.Data.Value;
        var lamps = new SortedDictionary<string, LightEmitter>(StringComparer.Ordinal);
        var facades = data.Windows.Metadata.Facades;
        var windows = data.Windows.Metadata.Windows;
        var entrances = new SortedDictionary<string, BakedEntrance>(StringComparer.Ordinal);
        void CollectLights(ILightMetadataSource source, BuildContext context)
        {
            foreach (var lamp in source.DescribeLights(context)) lamps.TryAdd(Canonical(lamp), lamp);
        }
        foreach (var module in map.Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value))
        {
            if (!map.Active) throw new OperationCanceledException("Map unloaded while preparing resident metadata.");
            if (module.Value is ILightMetadataSource source) CollectLights(source, map.CreateAssetContext(module.CodeHash));
        }
        foreach (var binding in data.InteriorBindings.OrderBy(binding => binding.ProviderId, StringComparer.Ordinal))
        {
            var id = "interior." + binding.ProviderId;
            var interior = map.Interiors[id];
            var context = map.CreateAssetContext(interior.CodeHash);
            if (interior.Value is ILightMetadataSource source) CollectLights(source, context);
            foreach (var feature in binding.FeatureIds.OrderBy(feature => feature.Value, StringComparer.Ordinal))
            {
                if (!map.Active) throw new OperationCanceledException("Map unloaded while preparing interior metadata.");
                var layout = InteriorAssembly.ValidatedLayout(interior.Value, binding, context, feature);
                foreach (var entrance in layout.Entrances)
                {
                    var item = new BakedEntrance(id, entrance);
                    entrances.TryAdd(Canonical(item), item);
                }
            }
        }
        var lightData = new LightInventoryDocument(map.Context.Descriptor.Id,
            Array.AsReadOnly(lamps.Values.ToArray()), Array.AsReadOnly(facades.OrderBy(facade => Canonical(facade), StringComparer.Ordinal).ToArray()),
            Array.AsReadOnly(windows.OrderBy(window => window.Id).ToArray()))
        { WindowNumbering = data.Windows.Snapshot };
        var entranceData = new EntranceInventoryDocument(map.Context.Descriptor.Id, Array.AsReadOnly(entrances.Values.ToArray()));
        Validate(lightData, entranceData);
        BakeWindowPreparation.ValidateMetadata(new(lightData.Facades, lightData.Windows), data.Windows.Snapshot);
        return new(lightData, entranceData);
    }
    private static string Canonical<T>(T value) => JsonSerializer.Serialize(value, ContractJson.Options);
    private static void Validate(LightInventoryDocument lights, EntranceInventoryDocument doors)
    {
        if (lights.Lamps is null || lights.Facades is null || lights.Windows is null || doors.Entrances is null ||
            lights.Lamps.Count > 100000 || lights.Facades.Count > 100000 || lights.Windows.Count > 100000 || doors.Entrances.Count > 100000)
            throw new InvalidDataException("Invalid resident metadata collection.");
        foreach (var lamp in lights.Lamps)
            if (lamp is null || !VectorMath.Finite(lamp.Position) || !float.IsFinite(lamp.Energy) || !float.IsFinite(lamp.Range) || lamp.Energy < 0 || lamp.Range <= 0 || LightChannelCatalog.Slot(lamp.Channel) < 0)
                throw new InvalidDataException("Invalid lamp declaration.");
        // Window ranges, Skip cells and cross-list duplicate IDs are validated together against the prepared table.
        foreach (var item in doors.Entrances)
            if (item?.Entrance is null || string.IsNullOrWhiteSpace(item.Module) || string.IsNullOrWhiteSpace(item.Entrance.Id) || !VectorMath.Finite(item.Entrance.Outside) ||
                !VectorMath.Finite(item.Entrance.Inside) || !VectorMath.Finite(item.Entrance.Center) || !float.IsFinite(item.Entrance.ClearWidth) || item.Entrance.ClearWidth <= 0 ||
                !float.IsFinite(item.Entrance.YawDeg)) throw new InvalidDataException("Invalid entrance declaration.");
    }
}
