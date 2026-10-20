using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.Buildings.Model;

/// <summary>
/// The campus window numbering. WP-14 preserves persisted addresses and appends newly required ranges. BuildingsModule.PrepareWindowNumbering runs it once per map, with the
/// zone packs' claimed layouts replacing whole features, and every producer then reads that snapshot from
/// BuildContext.WindowNumbering instead of numbering again.
/// </summary>
public static class WindowNumbering
{
    public const string Version = "window-numbering-v2";

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
        WindowAllocation a;
        try { a = snapshot.Get(plan.Id, plan.Key); }
        catch (KeyNotFoundException)
        {
            throw new InvalidOperationException(
                $"The authoritative window numbering for map {snapshot.Map.Value} has no block for generic building {plan.Id.Value} / {plan.Key}. " +
                "Prepare and publish the complete map allocation through IWindowNumberingProvider before building windows.");
        }
        if (a.Columns != plan.TotalColumns || a.Rows != plan.Storeys)
            throw new InvalidOperationException($"Window block {plan.Id.Value} / {plan.Key} is {a.Columns} × {a.Rows}, the plan {plan.TotalColumns} × {plan.Storeys}.");
        return new WindowBlock(a.Feature, a.Key, a.BaseId, a.Columns, a.Rows);
    }

    /// <summary>
    /// The map-wide allocation (IWindowNumberingProvider). Generic buildings keep their source grids and the shared-source
    /// rule of <see cref="BuildingSet"/>; a claimed layout removes every generic block of its feature and adds its own
    /// grids (none for Grids = []). Existing ranges stay pinned; newly required ranges append through <see cref="Append"/>. Any invalid declaration, a
    /// footprint numbered twice or more than <see cref="WindowNumberingSnapshot.Capacity"/> windows throws: no table at all.
    /// </summary>
    public static WindowNumberingSnapshot Prepare(BuildContext prepass, IReadOnlyList<ClaimedWindowLayout> validatedLayouts)
    {
        ArgumentNullException.ThrowIfNull(prepass);
        ArgumentNullException.ThrowIfNull(validatedLayouts);
        ArgumentNullException.ThrowIfNull(prepass.Map);
        var world = prepass.World;
        var generic = BuildingSet.For(prepass);
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
                        $"Grid {grid.Key} of claimed {id} (facade {prepass.ClaimedBy(layout.Feature, Aspects.Facade)!}) renumbers a footprint " +
                        $"numbered by {holder} (facade {prepass.ClaimedBy(new FeatureId(holder), Aspects.Facade) ?? "buildings"}); settle the source's owner first.");
        var grids = generic.Blocks.Where(b => !declared.ContainsKey(b.Feature.Value)).Select(b => (b.Feature, b.Key, b.Columns, b.Rows))
            .Concat(declared.Values.SelectMany(l => l.Grids.Select(g => (l.Feature, g.Key, g.Columns, g.Rows))))
            .OrderBy(g => g.Feature.Value, StringComparer.Ordinal).ThenBy(g => g.Key, StringComparer.Ordinal).ToArray();
        var total = grids.Sum(g => (long)g.Columns * g.Rows);
        if (total > WindowNumberingSnapshot.Capacity)
            throw new InvalidOperationException($"The campus needs {total} window IDs, {total - WindowNumberingSnapshot.Capacity} over the " +
                $"{WindowNumberingSnapshot.Capacity}-ID window_override; largest grids: " +
                string.Join(", ", grids.OrderByDescending(g => (long)g.Columns * g.Rows).Take(5).Select(g => $"{g.Feature.Value} / {g.Key} {(long)g.Columns * g.Rows}")) + ".");
        if (!prepass.SourceInputs.TryGetValue(WindowNumberingRegistry.FileName, out var bytes) || bytes.IsEmpty)
            throw new InvalidDataException($"Map {prepass.Map.Id.Value}: persistent {WindowNumberingRegistry.FileName} is missing or empty in BuildContext.SourceInputs.");
        var registry = JsonSerializer.Deserialize<WindowNumberingRegistry>(bytes.Span, ContractJson.Options)
            ?? throw new InvalidDataException("The window-numbering registry is empty.");
        if (registry.Map != prepass.Map.Id) throw new InvalidDataException("The window registry belongs to another map.");
        var previous = new WindowNumberingSnapshot(registry.Map, new string('0', 64), registry.Allocations, registry.NextId);
        var table = Append(previous, grids, out var nextId);
        return new WindowNumberingSnapshot(prepass.Map.Id, Fingerprint(prepass, declared.Values, table), table, nextId);
    }

    /// <summary>Existing addresses are pinned; new or enlarged grids append. Removed ranges are never reused.</summary>
    public static WindowAllocation[] Append(WindowNumberingSnapshot previous,
        IEnumerable<(FeatureId Feature, string Key, int Columns, int Rows)> grids, out int nextId)
    {
        var old = previous.Allocations.ToDictionary(a => (a.Feature, a.Key));
        var table = new List<WindowAllocation>();
        var next = previous.NextId;
        foreach (var (feature, key, columns, rows) in grids.OrderBy(g => g.Feature.Value, StringComparer.Ordinal).ThenBy(g => g.Key, StringComparer.Ordinal))
        {
            if (columns <= 0 || rows <= 0) throw new InvalidDataException("A window grid needs positive dimensions.");
            var count = checked(columns * rows);
            int start;
            if (old.TryGetValue((feature, key), out var before) && count <= before.Count) start = before.BaseId;
            else
            {
                if (count > WindowNumberingSnapshot.Capacity - next)
                    throw new InvalidOperationException($"Window address capacity exhausted by {feature.Value} / {key}; retired ranges are not reused.");
                start = next;
                next = checked(next + count);
            }
            table.Add(new(feature, key, start, columns, rows));
        }
        nextId = next;
        return table.ToArray();
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
    /// Lowercase SHA-256 over the resolved table: version, map, seed, every declaration (including empty ones)
    /// and allocation row. Unrelated map inputs are excluded. Strings are UTF-8 with an int32 length,
    /// integers little-endian.
    /// </summary>
    private static string Fingerprint(BuildContext prepass, IEnumerable<ClaimedWindowLayout> declared, IReadOnlyList<WindowAllocation> table)
    {
        var buffer = new List<byte>(64 * (table.Count + 8));
        Text(Version); Text(prepass.Map.Id.Value); Int((int)prepass.WorldSeed);
        var layouts = declared.ToArray();
        Int(layouts.Length);
        foreach (var layout in layouts)
        {
            Text(layout.Feature.Value);
            Text(prepass.ClaimedBy(layout.Feature, Aspects.Facade) ?? "");
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
