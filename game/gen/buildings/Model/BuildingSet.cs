using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Campus.Contracts;

namespace Campus.Gen.Buildings.Model;

/// <summary>Window-number allocation of one building part: window = Base + row × Columns + column.</summary>
public readonly record struct WindowBlock(FeatureId Feature, string Key, int Base, int Columns, int Rows)
{
    public int Count => Columns * Rows;
}

/// <summary>
/// All building plans of one world with their generic window blocks: every building part of every feature, claimed or
/// not, in FeatureId then part-key ordinal order. Plans are cached per world model and seed; Build() calls on any tile
/// agree. The blocks are the layout before claimed replacements; production numbers come from the map's
/// WindowNumberingSnapshot (<see cref="WindowNumbering.Prepare"/>), which equals them while nothing is declared.
/// </summary>
public sealed class BuildingSet
{
    /// <summary>Campus window numbers stay below this (window_override is 256 × 256); outskirts use their own range above it.</summary>
    public const int CampusWindowLimit = 65536;

    private static readonly ConditionalWeakTable<IWorldModel, Dictionary<uint, BuildingSet>> Cache = new();
    private static readonly object Sync = new();

    private BuildingSet(IReadOnlyList<BuildingPlan> plans, IReadOnlyList<WindowBlock> blocks)
    {
        Plans = plans;
        Blocks = blocks;
        var byKey = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < plans.Count; i++) byKey[plans[i].Key] = i;
        _index = byKey;
        WindowCount = blocks.Count == 0 ? 0 : blocks[^1].Base + blocks[^1].Count;
    }

    private readonly Dictionary<string, int> _index;

    /// <summary>Plans sorted by (FeatureId, part key) ordinal.</summary>
    public IReadOnlyList<BuildingPlan> Plans { get; }
    /// <summary>Window blocks, parallel to Plans.</summary>
    public IReadOnlyList<WindowBlock> Blocks { get; }
    public int WindowCount { get; }

    public WindowBlock BlockOf(BuildingPlan plan) => Blocks[_index[plan.Key]];

    public static BuildingSet For(IWorldModel world, uint worldSeed)
    {
        ArgumentNullException.ThrowIfNull(world);
        lock (Sync)
        {
            var bySeed = Cache.GetValue(world, _ => []);
            if (bySeed.TryGetValue(worldSeed, out var found)) return found;
            var created = Create(world, worldSeed);
            bySeed.Add(worldSeed, created);
            return created;
        }
    }

    private static BuildingSet Create(IWorldModel world, uint worldSeed)
    {
        var sources = world.Features.Values
            .OrderBy(f => f.Id.Value, StringComparer.Ordinal)
            .SelectMany(BuildingPlanner.Sources)
            .ToArray();
        var plans = new List<BuildingPlan>(sources.Length);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var src in sources.OrderBy(s => s.Feature.Id.Value, StringComparer.Ordinal).ThenBy(s => s.Key, StringComparer.Ordinal))
        {
            // A source footprint shared by two entities is built once, by the first entity in ordinal order.
            if (!seen.Add(src.Key)) continue;
            var plan = BuildingPlanner.Plan(src, world.Spatial, worldSeed, world.Fidelity);
            if (plan is null) continue;
            plans.Add(plan);
        }
        var (blocks, _) = WindowNumbering.Allocate(plans.Select(p => (p.Id, p.Key, p.TotalColumns, p.Storeys)));
        return new BuildingSet(plans, blocks);
    }
}

/// <summary>
/// The campus window numbering (contract-29). WP-14 owns the one allocation algorithm: grids in ordinal (FeatureId,
/// key) order take contiguous ranges from zero. BuildingsModule.PrepareWindowNumbering runs it once per map, with the
/// zone packs' claimed layouts replacing whole features, and every producer then reads that snapshot from
/// BuildContext.WindowNumbering instead of numbering again.
/// </summary>
public static class WindowNumbering
{
    public const string Version = "window-numbering-v1";

    /// <summary>
    /// The generic layout without claimed replacements: legacy tools, rule fixtures and the provider's default grids
    /// only. Never the numbering of a production Build or window list; those read <see cref="For(BuildContext)"/>.
    /// </summary>
    public static IReadOnlyList<WindowBlock> For(IWorldModel world, uint worldSeed) => BuildingSet.For(world, worldSeed).Blocks;

    /// <summary>The build's authoritative snapshot, checked against its map; never recomputed or looked up elsewhere.</summary>
    public static WindowNumberingSnapshot For(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var snapshot = ctx.WindowNumbering ?? throw new InvalidOperationException(
            "BuildContext.WindowNumbering is missing: the kernel prepares it once per map through IWindowNumberingProvider before any window output.");
        if (ctx.Map is null || snapshot.Map != ctx.Map.Id)
            throw new InvalidOperationException($"The window numbering belongs to map {snapshot.Map.Value}, not {ctx.Map?.Id.Value ?? "(none)"}.");
        return snapshot;
    }

    /// <summary>A generic plan's block in the snapshot; its grid must still be the plan's (TotalColumns × Storeys).</summary>
    public static WindowBlock BlockOf(WindowNumberingSnapshot snapshot, BuildingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(plan);
        var a = snapshot.Get(plan.Id, plan.Key);
        if (a.Columns != plan.TotalColumns || a.Rows != plan.Storeys)
            throw new InvalidOperationException($"Window block {plan.Id.Value} / {plan.Key} is {a.Columns} × {a.Rows}, the plan {plan.TotalColumns} × {plan.Storeys}.");
        return new WindowBlock(a.Feature, a.Key, a.BaseId, a.Columns, a.Rows);
    }

    /// <summary>
    /// The map-wide allocation (IWindowNumberingProvider). Generic buildings keep their source grids and the shared-source
    /// rule of <see cref="BuildingSet"/>; a claimed layout removes every generic block of its feature and adds its own
    /// grids (none for Grids = []). Everything is then numbered by <see cref="Allocate"/>. Any invalid declaration, a
    /// footprint numbered twice or more than <see cref="WindowNumberingSnapshot.Capacity"/> windows throws: no table at all.
    /// </summary>
    public static WindowNumberingSnapshot Prepare(BuildContext prepass, IReadOnlyList<ClaimedWindowLayout> validatedLayouts)
    {
        ArgumentNullException.ThrowIfNull(prepass);
        ArgumentNullException.ThrowIfNull(validatedLayouts);
        ArgumentNullException.ThrowIfNull(prepass.Map);
        var world = prepass.World;
        var generic = BuildingSet.For(world, prepass.WorldSeed);
        // Copied and checked again: one complete layout per facade-claimed feature of this world.
        var declared = new SortedDictionary<string, ClaimedWindowLayout>(StringComparer.Ordinal);
        foreach (var layout in validatedLayouts)
        {
            if (layout?.Grids is null) throw new ArgumentException("A claimed window layout needs its grid list.", nameof(validatedLayouts));
            var id = layout.Feature.Value;
            if (string.IsNullOrWhiteSpace(id) || !world.Features.ContainsKey(layout.Feature))
                throw new ArgumentException($"Window layout for '{id}': no such feature on this map.", nameof(validatedLayouts));
            if (prepass.ClaimedBy(layout.Feature, Aspects.Facade) is null)
                throw new ArgumentException($"Window layout for {id}: its facade is not claimed, so its generic windows stay.", nameof(validatedLayouts));
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var copy = new List<WindowGridSpec>(layout.Grids.Count);
            foreach (var grid in layout.Grids)
            {
                if (grid is null || string.IsNullOrWhiteSpace(grid.Key) || grid.Columns <= 0 || grid.Rows <= 0)
                    throw new ArgumentException($"Window layout for {id}: every grid needs a key and positive columns and rows.", nameof(validatedLayouts));
                if (!keys.Add(grid.Key)) throw new ArgumentException($"Window layout for {id}: grid '{grid.Key}' is declared twice.", nameof(validatedLayouts));
                copy.Add(grid);
            }
            copy.Sort((a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));
            if (!declared.TryAdd(id, new ClaimedWindowLayout(layout.Feature, copy.AsReadOnly())))
                throw new ArgumentException($"Feature {id} has more than one window layout.", nameof(validatedLayouts));
        }
        // A footprint shared by several features is numbered once, by the first one (BuildingSet), and stays with it: a
        // claimed feature may keep such a part of another feature's building generic (the Liren connector beside the
        // A/B blocks), but a grid that renumbers that very footprint would number one facade twice.
        var numberedBy = generic.Blocks.ToDictionary(b => b.Key, b => b.Feature.Value, StringComparer.Ordinal);
        foreach (var (id, layout) in declared)
            foreach (var grid in layout.Grids)
                if (numberedBy.TryGetValue(grid.Key, out var holder) && holder != id && !declared.ContainsKey(holder))
                    throw new InvalidOperationException(
                        $"Grid {grid.Key} of claimed {id} (facade {prepass.ClaimedBy(layout.Feature, Aspects.Facade)!.Value.Value}) renumbers a footprint " +
                        $"numbered by {holder} (facade {prepass.ClaimedBy(new FeatureId(holder), Aspects.Facade)?.Value ?? "WP-14"}); settle the source's owner first.");
        var grids = generic.Blocks.Where(b => !declared.ContainsKey(b.Feature.Value)).Select(b => (b.Feature, b.Key, b.Columns, b.Rows))
            .Concat(declared.Values.SelectMany(l => l.Grids.Select(g => (l.Feature, g.Key, g.Columns, g.Rows))))
            .OrderBy(g => g.Feature.Value, StringComparer.Ordinal).ThenBy(g => g.Key, StringComparer.Ordinal).ToArray();
        var total = grids.Sum(g => (long)g.Columns * g.Rows);
        if (total > WindowNumberingSnapshot.Capacity)
            throw new InvalidOperationException($"The campus needs {total} window IDs, {total - WindowNumberingSnapshot.Capacity} over the " +
                $"{WindowNumberingSnapshot.Capacity}-ID window_override; largest grids: " +
                string.Join(", ", grids.OrderByDescending(g => (long)g.Columns * g.Rows).Take(5).Select(g => $"{g.Feature.Value} / {g.Key} {(long)g.Columns * g.Rows}")) + ".");
        var table = Allocate(grids).Blocks.Select(b => new WindowAllocation(b.Feature, b.Key, b.Base, b.Columns, b.Rows)).ToArray();
        return new WindowNumberingSnapshot(prepass.Map.Id, Fingerprint(prepass, declared.Values, table), table);
    }

    /// <summary>
    /// The one numbering rule: grids in the given order take contiguous half-open ranges from zero (blank bays and doors
    /// included). Returns the blocks and the total, counted in 64 bits; a base beyond int range throws.
    /// </summary>
    public static (WindowBlock[] Blocks, long Total) Allocate(IEnumerable<(FeatureId Feature, string Key, int Columns, int Rows)> grids)
    {
        var blocks = new List<WindowBlock>();
        long next = 0;
        foreach (var (feature, key, columns, rows) in grids)
        {
            blocks.Add(new WindowBlock(feature, key, checked((int)next), columns, rows));
            next += (long)columns * rows;
        }
        return ([.. blocks], next);
    }

    /// <summary>
    /// Lowercase SHA-256 over the resolved input and the table: version, map, seed, the caller's input and code hashes,
    /// every declaration (owner, grids, empty ones too) and every allocation row. Strings are UTF-8 with an int32 length,
    /// integers little-endian.
    /// </summary>
    private static string Fingerprint(BuildContext prepass, IEnumerable<ClaimedWindowLayout> declared, IReadOnlyList<WindowAllocation> table)
    {
        var buffer = new List<byte>(64 * (table.Count + 8));
        Text(Version); Text(prepass.Map.Id.Value); Int((int)prepass.WorldSeed); Text(prepass.DataHash); Text(prepass.CodeHash);
        var layouts = declared.ToArray();
        Int(layouts.Length);
        foreach (var layout in layouts)
        {
            Text(layout.Feature.Value);
            Text(prepass.ClaimedBy(layout.Feature, Aspects.Facade)?.Value ?? "");
            Int(layout.Grids.Count);
            foreach (var grid in layout.Grids) { Text(grid.Key); Int(grid.Columns); Int(grid.Rows); }
        }
        Int(table.Count);
        foreach (var a in table) { Text(a.Feature.Value); Text(a.Key); Int(a.BaseId); Int(a.Columns); Int(a.Rows); }
        return Convert.ToHexStringLower(SHA256.HashData([.. buffer]));

        void Int(int value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            buffer.AddRange(bytes);
        }

        void Text(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Int(bytes.Length);
            buffer.AddRange(bytes);
        }
    }
}
