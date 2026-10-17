using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Kernel.Bake;

public sealed record BakedEntrance(string Module, EntranceDecl Entrance);
public sealed record LightInventoryDocument(int Schema, string Abi, MapId Map, string SourceHash,
    IReadOnlyList<LightEmitter> Lamps, IReadOnlyList<WindowFacadeDecl> Facades, IReadOnlyList<WindowDecl> Windows)
{
    public WindowNumberingSnapshot? WindowNumbering { get; init; }
}
public sealed record EntranceInventoryDocument(int Schema, string Abi, MapId Map, string SourceHash,
    IReadOnlyList<BakedEntrance> Entrances);
internal sealed record BakeMapMetadata(LightInventoryDocument Lighting, EntranceInventoryDocument Entrances);

/// <summary>Runtime read-only inventory; map preparation publishes all three immutable tables together.</summary>
public sealed class BakeLightInventory : ILightInventory
{
    private LightInventoryDocument _current = new(1, BakeOptions.Abi, default, "", [], [], []);
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
    internal void Clear() => Publish(new(1, BakeOptions.Abi, default, "", [], [], []));
}

internal static class BakeMapMetadataCodec
{
    public const string LightingFile = "light-inventory.json";
    public const string EntrancesFile = "entrances.json";
    private const int MaximumBytes = 16 * 1024 * 1024;

    public static BakeMapMetadata Read(byte[] lighting, byte[] entrances, MapId map, string sourceHash,
        WindowNumberingSnapshot? expectedNumbering = null)
    {
        if (lighting.Length is <= 0 or > MaximumBytes || entrances.Length is <= 0 or > MaximumBytes)
            throw new InvalidDataException("Boot map metadata exceeds its size bound.");
        LightInventoryDocument lights;
        EntranceInventoryDocument doors;
        try
        {
            lights = JsonSerializer.Deserialize<LightInventoryDocument>(lighting, ContractJson.Options)
                ?? throw new InvalidDataException("Missing lighting inventory.");
            doors = JsonSerializer.Deserialize<EntranceInventoryDocument>(entrances, ContractJson.Options)
                ?? throw new InvalidDataException("Missing entrance inventory.");
        }
        catch (Exception error) when (error is JsonException or ArgumentException or OverflowException)
        {
            throw new InvalidDataException("Malformed boot map metadata or window allocation.", error);
        }
        if (lights.Schema != 1 || doors.Schema != 1 || lights.Abi != BakeOptions.Abi || doors.Abi != BakeOptions.Abi ||
            lights.Map != map || doors.Map != map || lights.SourceHash != sourceHash || doors.SourceHash != sourceHash)
            throw new InvalidDataException("Boot map metadata identity differs from the current map/code/palette inputs.");
        Validate(lights, doors);
        var restored = lights.WindowNumbering;
        if (restored?.Map != expectedNumbering?.Map || restored?.Fingerprint != expectedNumbering?.Fingerprint ||
            (restored is not null && (restored.Map != map || !restored.Allocations.SequenceEqual(expectedNumbering!.Allocations))))
            throw new InvalidDataException("Cached window numbering differs from the prepared map snapshot.");
        BakeWindowPreparation.ValidateMetadata(new(lights.Facades, lights.Windows), expectedNumbering);
        return new(lights with
        {
            Lamps = Array.AsReadOnly(lights.Lamps.ToArray()),
            Facades = Array.AsReadOnly(lights.Facades.Select(facade => facade with
            { SillY = Array.AsReadOnly(facade.SillY.ToArray()), Skip = Array.AsReadOnly(facade.Skip.ToArray()) }).ToArray()),
            Windows = Array.AsReadOnly(lights.Windows.ToArray()),
            WindowNumbering = expectedNumbering
        },
            doors with { Entrances = Array.AsReadOnly(doors.Entrances.ToArray()) });
    }

    public static byte[] Encode<T>(T value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, ContractJson.Options);
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("Boot map metadata exceeds 16 MiB.");
        return bytes;
    }

    public static BakeMapMetadata Build(BakeService.MapBuild map)
    {
        var data = map.Data.Value;
        var lamps = new SortedDictionary<string, LightEmitter>(StringComparer.Ordinal);
        var facades = new List<WindowFacadeDecl>(data.Windows.Metadata.Facades);
        var windows = new List<WindowDecl>(data.Windows.Metadata.Windows);
        var entrances = new SortedDictionary<string, BakedEntrance>(StringComparer.Ordinal);
        BuildContext Context(string codeHash) => new()
        {
            World = map.Context.World,
            Map = map.Context.Descriptor,
            WorldSeed = data.Seed,
            Catalog = map.Catalog,
            ClaimedBy = map.Claims.Value.ClaimedBy,
            SourceInputs = data.Inputs,
            DataHash = data.Hash,
            CodeHash = codeHash,
            InteriorProviders = data.InteriorProviders,
            WindowNumbering = data.Windows.Snapshot,
            GlobalInputs = map.GlobalInputs,
            GroundMaskContributors = map.GroundMaskContributors,
            PartLods = map.PartLods
        };
        void Collect(string id, ContentOutput output, bool collectLegacyWindows = true, FeatureId? feature = null)
        {
            map.Collect(id, "metadata", output.Diagnostics, feature);
            foreach (var lamp in output.Emitters ?? []) lamps.TryAdd(Canonical(lamp), lamp);
            if (collectLegacyWindows)
            {
                facades.AddRange(output.Windows);
                windows.AddRange(output.LooseWindows);
            }
            foreach (var entrance in output.Entrances)
            {
                var item = new BakedEntrance(id, entrance);
                entrances.TryAdd(Canonical(item), item);
            }
        }
        foreach (var (id, module) in map.Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var context = Context(module.CodeHash);
            foreach (var tile in module.Value.Tiles(context).Distinct().OrderBy(tile => tile.X).ThenBy(tile => tile.Z))
            {
                if (!map.Active) throw new OperationCanceledException("Map unloaded while preparing resident metadata.");
                // Windows were collected once by the pure two-phase metadata pass. This existing geometry walk
                // collects diagnostics and lamps/entrances, whose contracts have no separate metadata capability yet.
                Collect(id, module.Value.Build(context, tile, LodLevel.L0), module.Value is not IWindowMetadataSource);
            }
        }
        foreach (var binding in data.InteriorBindings.OrderBy(binding => binding.ProviderId, StringComparer.Ordinal))
        {
            var id = "interior." + binding.ProviderId;
            var interior = map.Interiors[id];
            var context = Context(interior.CodeHash);
            foreach (var feature in binding.FeatureIds.OrderBy(feature => feature.Value, StringComparer.Ordinal))
            {
                var layout = BakeService.ValidatedLayout(interior.Value, binding, context, feature);
                Collect(id, ContentOutput.Empty with { Entrances = layout.Entrances });
                foreach (var storey in layout.OpenStoreys.Order())
                {
                    if (!map.Active) throw new OperationCanceledException("Map unloaded while preparing interior metadata.");
                    Collect(id, interior.Value.BuildStorey(context, feature, storey, InteriorDetail.Full), feature: feature);
                }
            }
        }
        var hash = SourceHash(map);
        var lightData = new LightInventoryDocument(1, BakeOptions.Abi, map.Context.Descriptor.Id, hash,
            lamps.Values.ToArray(), facades.OrderBy(facade => Canonical(facade), StringComparer.Ordinal).ToArray(), windows.OrderBy(window => window.Id).ToArray())
        { WindowNumbering = data.Windows.Snapshot };
        var entranceData = new EntranceInventoryDocument(1, BakeOptions.Abi, map.Context.Descriptor.Id, hash, entrances.Values.ToArray());
        // The disk reader is also the final producer validator; no separate relaxed generation path.
        return Read(Encode(lightData), Encode(entranceData), map.Context.Descriptor.Id, hash, data.Windows.Snapshot);
    }

    public static string SourceHash(BakeService.MapBuild map) => BakeCacheStore.Hash(Encode(new
    {
        data = map.Data.Value.Hash,
        modules = map.Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new { id = pair.Key, code = pair.Value.CodeHash }).ToArray(),
        interiors = map.Data.Value.InteriorBindings.OrderBy(binding => binding.ProviderId, StringComparer.Ordinal).Select(binding => new
        {
            id = binding.ProviderId,
            code = map.Interiors["interior." + binding.ProviderId].CodeHash,
            features = binding.FeatureIds.OrderBy(feature => feature.Value, StringComparer.Ordinal).ToArray(),
            floors = binding.OpenFloors.Order().ToArray()
        }).ToArray()
    }));

    private static string Canonical<T>(T value) => JsonSerializer.Serialize(value, ContractJson.Options);
    private static void Validate(LightInventoryDocument lights, EntranceInventoryDocument doors)
    {
        if (lights.Lamps is null || lights.Facades is null || lights.Windows is null || doors.Entrances is null ||
            lights.Lamps.Count > 100000 || lights.Facades.Count > 100000 || lights.Windows.Count > 100000 || doors.Entrances.Count > 100000)
            throw new InvalidDataException("Invalid resident metadata collection.");
        foreach (var lamp in lights.Lamps)
            if (lamp is null || !Finite(lamp.Position) || !float.IsFinite(lamp.Energy) || !float.IsFinite(lamp.Range) || lamp.Energy < 0 || lamp.Range <= 0 || LightChannelCatalog.Slot(lamp.Channel) < 0)
                throw new InvalidDataException("Invalid lamp declaration.");
        // Window ranges, Skip cells and cross-list duplicate IDs are validated together against the prepared table.
        foreach (var item in doors.Entrances)
            if (item?.Entrance is null || string.IsNullOrWhiteSpace(item.Module) || string.IsNullOrWhiteSpace(item.Entrance.Id) || !Finite(item.Entrance.Outside) ||
                !Finite(item.Entrance.Inside) || !Finite(item.Entrance.Center) || !float.IsFinite(item.Entrance.ClearWidth) || item.Entrance.ClearWidth <= 0 ||
                !float.IsFinite(item.Entrance.YawDeg)) throw new InvalidDataException("Invalid entrance declaration.");
    }
    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
