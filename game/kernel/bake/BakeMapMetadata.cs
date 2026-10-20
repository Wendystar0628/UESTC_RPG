using Campus.Pipeline.Map;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

public sealed record BakedEntrance(string Module, EntranceDecl Entrance);
public sealed record EntranceInventoryDocument(MapId Map,
    IReadOnlyList<BakedEntrance> Entrances);
internal sealed record BakeMapMetadata(LightInventoryDocument Lighting, EntranceInventoryDocument Entrances);
internal sealed record BakeResidentData(BakeMapMetadata Metadata, WindowMetadata Windows,
    BakeService.InteriorCoverage[] Coverage, InteriorLayout[] Layouts, PreparedBakeTiles Directory, TileKey[] TerrainPoints);

/// <summary>Runtime read-only inventory; map preparation publishes all three immutable tables together.</summary>
[KernelService(typeof(BakeLightInventory), Owner = "WP-02a")]
[KernelService(typeof(ILightInventory), Owner = "WP-02a")]
public sealed class BakeLightInventory : ILightInventory
{
    private LightInventoryDocument _current = new(default, [], [], [], new([], ReadOnlyMemory<float>.Empty, ReadOnlyMemory<float>.Empty, 0, 0));
    private long _revision;
    public long Rev => Interlocked.Read(ref _revision);
    public IReadOnlyList<LightEmitter> Lamps => Volatile.Read(ref _current).Lamps;
    public IReadOnlyList<WindowFacadeDecl> Facades => Volatile.Read(ref _current).Facades;
    public IReadOnlyList<WindowDecl> Windows => Volatile.Read(ref _current).Windows;
    public NightlightCatalog Nightlight => Volatile.Read(ref _current).Nightlight;
    public WindowNumberingSnapshot? WindowNumbering => Volatile.Read(ref _current).WindowNumbering;
    internal void Publish(LightInventoryDocument document)
    {
        if (document.WindowNumbering is { } snapshot && snapshot.Map != document.Map)
            throw new InvalidDataException("Lighting inventory and window numbering belong to different maps.");
        Volatile.Write(ref _current, document);
        Interlocked.Increment(ref _revision);
    }
    internal void Clear() => Publish(new(default, [], [], [], new([], ReadOnlyMemory<float>.Empty, ReadOnlyMemory<float>.Empty, 0, 0)));
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
        var emitters = Array.AsReadOnly(lamps.Values.ToArray());
        var builders = map.Producers.Where(producer => producer.Value is INightlightCatalogBuilder).ToArray();
        if (builders.Length > 1 || emitters.Count > 0 && builders.Length == 0)
            throw new InvalidDataException("Lamp declarations require exactly one nightlight catalog builder.");
        var nightlight = builders.Length == 0
            ? new NightlightCatalog([], ReadOnlyMemory<float>.Empty, ReadOnlyMemory<float>.Empty, 0, 0)
            : ((INightlightCatalogBuilder)builders[0].Value).BuildCatalog(map.CreateAssetContext(builders[0].CodeHash), emitters);
        nightlight = new(Array.AsReadOnly(nightlight.Lamps.ToArray()), nightlight.LampTable.ToArray(), nightlight.LampGrid.ToArray(), nightlight.GridWidth, nightlight.GridHeight) { WindowThresholds = nightlight.WindowThresholds.ToArray() };
        var lightData = new LightInventoryDocument(map.Context.Descriptor.Id,
            emitters, Array.AsReadOnly(facades.OrderBy(facade => Canonical(facade), StringComparer.Ordinal).ToArray()),
            Array.AsReadOnly(windows.OrderBy(window => window.Id).ToArray()), nightlight)
        { WindowNumbering = data.Windows.Snapshot };
        var entranceData = new EntranceInventoryDocument(map.Context.Descriptor.Id, Array.AsReadOnly(entrances.Values.ToArray()));
        Validate(lightData, entranceData);
        BakeWindowPreparation.ValidateMetadata(new(lightData.Facades, lightData.Windows), data.Windows.Snapshot);
        return new(lightData, entranceData);
    }
    private static string Canonical<T>(T value) => JsonSerializer.Serialize(value, ContractJson.Options);
    internal static void ValidateResident(BakeResidentData resident, MapId map,
        IReadOnlyDictionary<string, ContentModuleInput> modules, IReadOnlyList<BakeService.Producer> producers)
    {
        if (resident.Metadata is null || resident.Metadata.Lighting is null || resident.Metadata.Entrances is null ||
            resident.Metadata.Lighting.Map != map || resident.Metadata.Entrances.Map != map ||
            resident.Windows?.Facades is null || resident.Windows.Windows is null || resident.Coverage is null || resident.Layouts is null || resident.TerrainPoints is null ||
            resident.Directory?.Modules is null || resident.Directory.Auxiliary is null ||
            resident.Directory.Modules.Count != modules.Count || modules.Keys.Any(id => !resident.Directory.Modules.ContainsKey(id)))
            throw new InvalidDataException("Incomplete resident metadata.");
        Validate(resident.Metadata.Lighting, resident.Metadata.Entrances);
        BakeWindowPreparation.ValidateMetadata(resident.Windows, resident.Metadata.Lighting.WindowNumbering);
        BakeWindowPreparation.ValidateMetadata(new(resident.Metadata.Lighting.Facades, resident.Metadata.Lighting.Windows), resident.Metadata.Lighting.WindowNumbering);
        foreach (var pair in resident.Directory.Modules) BakeService.MapBuild.ValidateTiles(pair.Key, pair.Value);
        BakeService.MapBuild.ValidateAuxiliary(resident.Directory.Auxiliary, map, modules, producers);
    }

    private static void Validate(LightInventoryDocument lights, EntranceInventoryDocument doors)
    {
        if (lights.Lamps is null || lights.Facades is null || lights.Windows is null || doors.Entrances is null ||
            lights.Lamps.Count > 100000 || lights.Facades.Count > 100000 || lights.Windows.Count > 100000 || doors.Entrances.Count > 100000)
            throw new InvalidDataException("Invalid resident metadata collection.");
        foreach (var lamp in lights.Lamps)
            if (lamp is null || !VectorMath.Finite(lamp.Position) || !float.IsFinite(lamp.Energy) || !float.IsFinite(lamp.Range) || lamp.Energy < 0 || lamp.Range <= 0 || LightChannelCatalog.Slot(lamp.Channel) < 0)
                throw new InvalidDataException("Invalid lamp declaration.");
        var catalog = lights.Nightlight;
        if (catalog?.Lamps is null || catalog.GridWidth < 0 || catalog.GridHeight < 0 ||
            catalog.Lamps.Count > 0 && catalog.LampTable.Length != catalog.Lamps.Count * 8 ||
            catalog.Lamps.Count == 0 && catalog.LampTable.Length is not (0 or 8) ||
            catalog.LampGrid.Length != (long)catalog.GridWidth * catalog.GridHeight * 4 ||
            lights.WindowNumbering?.WindowCount > 0 && catalog.WindowThresholds.Length != WindowNumberingSnapshot.Capacity ||
            catalog.Lamps.Count > 0 && (catalog.GridWidth == 0 || catalog.GridHeight == 0))
            throw new InvalidDataException("Missing or invalid resident nightlight catalog.");
        foreach (var lamp in catalog.Lamps)
            if (!VectorMath.Finite(lamp.Base) || !float.IsFinite(lamp.HeadHeight) || !float.IsFinite(lamp.Radius) ||
                !float.IsFinite(lamp.Energy) || !float.IsFinite(lamp.ThresholdDeg) || !float.IsFinite(lamp.Keep) ||
                !float.IsFinite(lamp.GlowRadius) || lamp.Radius <= 0 || lamp.HeadHeight < 0 || lamp.GlowRadius < 0 ||
                lamp.Energy < 0 || lamp.Keep < 0 || lamp.Keep >= 1 || lamp.ColorColumn < 0 || lamp.ColorColumn >= PaletteLayout.Capacity ||
                lamp.Slot < 0 || lamp.Slot >= LightChannelCatalog.Count)
                throw new InvalidDataException("Invalid resident lamp record.");
        foreach (var value in catalog.WindowThresholds.Span)
            if (!float.IsFinite(value) || value < 0f || value > 1f)
                throw new InvalidDataException("Invalid baked window threshold.");
        foreach (var value in catalog.LampTable.Span)
            if (!float.IsFinite(value)) throw new InvalidDataException("Invalid resident lamp table.");
        foreach (var value in catalog.LampGrid.Span)
            if (!float.IsFinite(value) || value < 0 || value > catalog.Lamps.Count || value != MathF.Truncate(value))
                throw new InvalidDataException("Invalid resident lamp grid index.");
        // Window ranges, Skip cells and cross-list duplicate IDs are validated together against the prepared table.
        foreach (var item in doors.Entrances)
            if (item?.Entrance is null || string.IsNullOrWhiteSpace(item.Module) || string.IsNullOrWhiteSpace(item.Entrance.Id) || !VectorMath.Finite(item.Entrance.Outside) ||
                !VectorMath.Finite(item.Entrance.Inside) || !VectorMath.Finite(item.Entrance.Center) || !float.IsFinite(item.Entrance.ClearWidth) || item.Entrance.ClearWidth <= 0 ||
                !float.IsFinite(item.Entrance.YawDeg)) throw new InvalidDataException("Invalid entrance declaration.");
    }
}
