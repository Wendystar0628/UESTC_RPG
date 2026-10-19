using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Bake;
using Campus.Systems.Nightlight.Glow;
using Campus.Systems.Nightlight.Lamps;
using Campus.Systems.Nightlight.Schedule;
using Campus.Systems.Nightlight.Windows;

namespace Campus.Systems.Nightlight;

/// <summary>Resident lamp numbering and independently baked lamp/window owner tiles.</summary>
[BakeProducer("nightlight", Owner = "WP-39")]
public sealed class NightlightBakeProducer : IBakeProducer, IBakeContentConsumer, INightlightCatalogBuilder
{
    internal const string ScheduleFile = "lighting-schedule.json";
    private const uint LampSeed = 0x39u;
    private Lazy<IReadOnlyList<PreparedContentModule>>? _modules;
    private LightInventoryDocument? _lighting;
    private Lazy<IReadOnlySet<FeatureId>>? _waterfront;

    public NightlightCatalog BuildCatalog(BuildContext context, IReadOnlyList<LightEmitter> emitters)
    {
        if (!context.SourceInputs.TryGetValue(ScheduleFile, out var bytes))
            throw new InvalidDataException($"Missing prepared {ScheduleFile}.");
        var schedule = ReadSchedule(bytes.Span, context.Map.Id);
        var lamps = Array.AsReadOnly(LampSetBuilder.Build(emitters, schedule,
            token => context.Catalog.TryGet(token, out var entry) ? entry.Index.Value : null,
            context.World.Terrain.HeightAt, LampSeed ^ context.Map.Seed, out _).ToArray());
        var table = LampTable.PackTable(lamps);
        var grid = LampTable.PackGrid(lamps, context.Map.DataRect, out var size, out _);
        return new(lamps, table, grid, size.Width, size.Height);
    }

    internal static ScheduleEvaluator ReadSchedule(ReadOnlySpan<byte> bytes, MapId map)
    {
        var file = JsonSerializer.Deserialize<LightingScheduleFile>(bytes, ContractJson.Options)
            ?? throw new InvalidDataException($"{ScheduleFile} is empty.");
        if (file.Map != map) throw new InvalidDataException("Lighting schedule belongs to a different map.");
        var schedule = new ScheduleEvaluator(file);
        for (var slot = 0; slot < LightChannelCatalog.Count; slot++)
            if (schedule.Spec(slot).Id != LightChannelCatalog.Id(slot))
                throw new InvalidDataException($"Lighting schedule slot {slot} differs from the channel catalog.");
        return schedule;
    }

    public void Prepare(Lazy<IReadOnlyList<PreparedContentModule>> modules, LightInventoryDocument lighting)
    {
        _modules = modules;
        _lighting = lighting;
        _waterfront = new(() =>
        {
            var sources = modules.Value;
            if (sources.Count == 0) throw new InvalidDataException("Nightlight needs its prepared content source world.");
            var numbering = lighting.WindowNumbering ?? throw new InvalidDataException("Missing prepared window numbering.");
            return WaterfrontWindows.Buildings(sources[0].Context.World, numbering);
        });
    }

    public bool CanBuild(BakeKey key) => key.Module is BakeModuleIds.NightlightLamps or BakeModuleIds.NightlightWindows;

    public IReadOnlyList<BakeTileEntry> Tiles(BuildContext context)
    {
        // Use formal prepared source inputs, never regenerate a context from the producer's code hash.
        var sources = (_modules ?? throw new InvalidOperationException("Nightlight sources are not prepared.")).Value;
        if (sources.Count > 0) context = sources[0].Context;
        var lighting = RequireLighting(context);
        var lampBounds = new Dictionary<TileKey, Box3>();
        foreach (var lamp in lighting.Nightlight.Lamps)
        {
            var tile = TileGrid.At(lamp.Base);
            var radius = lamp.Radius * LampTable.MaxPoolGain;
            var glow = new Vector3(lamp.GlowRadius * 2.5f);
            var bounds = new Box3(Vector3.Min(lamp.Base - new Vector3(radius), lamp.Head - glow),
                Vector3.Max(lamp.Base + new Vector3(radius), lamp.Head + glow));
            lampBounds[tile] = lampBounds.TryGetValue(tile, out var previous) ? Union(previous, bounds) : bounds;
        }
        var entries = new List<BakeTileEntry>();
        foreach (var pair in lampBounds.OrderBy(p => p.Key.X).ThenBy(p => p.Key.Z))
            entries.Add(new(Key(context.Map.Id, BakeModuleIds.NightlightLamps, pair.Key), pair.Value));
        foreach (var tile in WindowCells.Candidates(lighting.Facades, lighting.Windows))
        {
            if (WindowCells.Bounds(lighting.Facades, lighting.Windows, tile) is { } bounds)
                entries.Add(new(Key(context.Map.Id, BakeModuleIds.NightlightWindows, tile), bounds));
        }
        return Array.AsReadOnly(entries.ToArray());
    }

    public object Build(BakeKey key, BuildContext context)
    {
        var lighting = RequireLighting(context);
        if (!CanBuild(key) || key.Map != context.Map.Id) throw new ArgumentException("Nightlight key does not match its map.");
        var request = BakeRequestKeys.Parse(key);
        if (request.Collision || request.Level != LodLevel.L0) throw new ArgumentException("Nightlight requires an L0 owner tile.");
        if (key.Module == BakeModuleIds.NightlightLamps)
        {
            var ids = Enumerable.Range(0, lighting.Nightlight.Lamps.Count)
                .Where(i => TileGrid.At(lighting.Nightlight.Lamps[i].Base) == request.Tile).ToArray();
            return new NightlightTileContent(key, request.Tile, Array.AsReadOnly(ids), GlowSets.Lamps(lighting.Nightlight.Lamps, ids), []);
        }
        var windows = GlowSets.Windows(lighting.Facades, lighting.Windows, request.Tile, out _);
        var numbering = lighting.WindowNumbering ?? throw new InvalidDataException("Missing prepared window numbering.");
        var waterfront = (_waterfront ?? throw new InvalidOperationException("Nightlight waterfront inputs are not prepared.")).Value;
        return new NightlightTileContent(key, request.Tile, [], windows, WaterfrontWindows.Indices(numbering, waterfront, windows));
    }

    private LightInventoryDocument RequireLighting(BuildContext context)
    {
        var lighting = _lighting ?? throw new InvalidOperationException("Nightlight lighting inputs are not prepared.");
        if (lighting.Map != context.Map.Id) throw new InvalidDataException("Nightlight inventory belongs to a different map.");
        return lighting;
    }

    private static BakeKey Key(MapId map, string module, TileKey tile) => BakeRequestKeys.Tile(map, module, tile, LodLevel.L0);
    private static Box3 Union(Box3 a, Box3 b) => new(Vector3.Min(a.Min, b.Min), Vector3.Max(a.Max, b.Max));
}
