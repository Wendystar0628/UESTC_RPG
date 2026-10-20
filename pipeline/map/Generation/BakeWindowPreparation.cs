using Campus.Contracts;

namespace Campus.Pipeline.Map;

/// <summary>Pure, map-filtered declaration collection. Only the registered provider allocates window IDs.</summary>
public static class BakeWindowPreparation
{
    public sealed record Result(WindowNumberingSnapshot Snapshot, WindowMetadata Metadata);

    public static Result Prepare(BuildContext prepass, IReadOnlyDictionary<string, ContentModuleInput> modules)
    {
        var ordered = modules.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
        var declarations = new SortedDictionary<string, ClaimedWindowLayout>(StringComparer.Ordinal);
        var origins = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (id, module) in ordered)
        {
            if (module.Value is not IWindowMetadataSource source) continue;
            var layouts = source.DescribeWindowLayouts(prepass.World, prepass.WorldSeed)
                ?? throw new InvalidDataException($"Window layouts are null: {id}.");
            foreach (var layout in layouts)
            {
                if (layout is null || string.IsNullOrWhiteSpace(layout.Feature.Value) ||
                    !prepass.World.Features.ContainsKey(layout.Feature))
                    throw new InvalidDataException($"Window layout references an absent feature: {id}/{layout?.Feature.Value}.");
                var owner = prepass.ClaimedBy(layout.Feature, Aspects.Facade);
                if (owner is null || owner != id)
                    throw new InvalidDataException($"Window facade owner mismatch: {id}/{layout.Feature.Value}; registered={id}, claimed={owner ?? "none"}.");
                if (layout.Grids is null) throw new InvalidDataException($"Window grids are null: {id}/{layout.Feature.Value}.");
                var grids = new SortedDictionary<string, WindowGridSpec>(StringComparer.Ordinal);
                foreach (var grid in layout.Grids)
                {
                    if (grid is null || string.IsNullOrWhiteSpace(grid.Key) || grid.Columns <= 0 || grid.Rows <= 0 ||
                        (long)grid.Columns * grid.Rows > WindowNumberingSnapshot.Capacity)
                        throw new InvalidDataException($"Invalid window dimensions/key: {id}/{layout.Feature.Value}/{grid?.Key}.");
                    if (!grids.TryAdd(grid.Key, new(grid.Key, grid.Columns, grid.Rows)))
                        throw new InvalidDataException($"Duplicate window grid: {id}/{layout.Feature.Value}/{grid.Key}.");
                }
                if (!declarations.TryAdd(layout.Feature.Value, new(layout.Feature, Array.AsReadOnly(grids.Values.ToArray()))))
                    throw new InvalidDataException($"Duplicate complete window layout: {layout.Feature.Value}; modules {origins[layout.Feature.Value]} and {id}.");
                origins.Add(layout.Feature.Value, id);
            }
        }
        var missing = prepass.World.Features.Keys.Select(feature => new { Feature = feature, Owner = prepass.ClaimedBy(feature, Aspects.Facade) })
            .Where(item => item.Owner is not null && !declarations.ContainsKey(item.Feature.Value))
            .OrderBy(item => item.Owner!, StringComparer.Ordinal).ThenBy(item => item.Feature.Value, StringComparer.Ordinal).ToArray();
        if (missing.Length != 0)
            throw new InvalidDataException($"Missing window layouts for {missing.Length} facade claims: " +
                string.Join("; ", missing.Select(item => $"owner {item.Owner!}, feature {item.Feature.Value}")) +
                ". Declare Grids=[] explicitly only for an authored windowless feature.");

        var providers = ordered.Where(pair => pair.Value.Value is IWindowNumberingProvider).ToArray();
        // Windowless maps publish their explicit empty prepass allocation. A real building needs its provider.
        var requiresProvider = declarations.Count != 0 || ordered.Any(pair => pair.Value.Value is IWindowMetadataSource) ||
            prepass.World.Features.Values.Any(feature => feature.Kind.Value == "building" || feature.Parts.Any(part => part.Height.HasValue || part.Storeys.HasValue));
        if (providers.Length == 0 && !requiresProvider) return new(prepass.WindowNumbering, new([], []));
        if (providers.Length != 1)
            throw new InvalidDataException($"Map {prepass.Map.Id.Value} needs exactly one window numbering provider; found {providers.Length}: {string.Join(", ", providers.Select(pair => pair.Key))}.");

        var provider = providers[0];
        // The provider fingerprints its resolved allocation; the frozen context carries no per-product identity.
        var snapshot = ((IWindowNumberingProvider)provider.Value.Value).PrepareWindowNumbering(
            Context(prepass, provider.Value.CodeHash, prepass.WindowNumbering), Array.AsReadOnly(declarations.Values.ToArray()))
            ?? throw new InvalidDataException($"Window provider {provider.Key} returned no snapshot.");
        if (snapshot.Map != prepass.Map.Id) throw new InvalidDataException("Window provider returned another map's snapshot.");
        foreach (var allocation in snapshot.Allocations)
            if (!prepass.World.Features.ContainsKey(allocation.Feature))
                throw new InvalidDataException($"Window allocation references absent feature {allocation.Feature.Value}.");
        foreach (var layout in declarations.Values)
        {
            var allocated = snapshot.Allocations.Where(item => item.Feature == layout.Feature).ToArray();
            if (allocated.Length != layout.Grids.Count || layout.Grids.Any(grid => !allocated.Any(item =>
                item.Key == grid.Key && item.Columns == grid.Columns && item.Rows == grid.Rows)))
                throw new InvalidDataException($"Window provider did not replace the complete declared layout: {layout.Feature.Value}.");
        }

        var facades = new List<WindowFacadeDecl>();
        var windows = new List<WindowDecl>();
        foreach (var (id, module) in ordered)
        {
            if (module.Value is not IWindowMetadataSource source) continue;
            var metadata = source.DescribeWindows(Context(prepass, module.CodeHash, snapshot))
                ?? throw new InvalidDataException($"Window metadata is null: {id}.");
            if (metadata.Facades is null || metadata.Windows is null) throw new InvalidDataException($"Window metadata lists are null: {id}.");
            foreach (var facade in metadata.Facades)
            {
                if (facade?.SillY is null || facade.Skip is null) throw new InvalidDataException($"Window facade lists are null: {id}.");
                facades.Add(facade with { SillY = Array.AsReadOnly(facade.SillY.ToArray()), Skip = Array.AsReadOnly(facade.Skip.ToArray()) });
            }
            windows.AddRange(metadata.Windows);
        }
        var result = new WindowMetadata(Array.AsReadOnly(facades.ToArray()), Array.AsReadOnly(windows.ToArray()));
        ValidateMetadata(result, snapshot);
        return new(snapshot, result);
    }

    public static BuildContext Context(BuildContext input, string codeHash, WindowNumberingSnapshot snapshot, IWorldModel? world = null) => new()
    {
        World = world ?? input.World,
        Map = input.Map,
        WorldSeed = input.WorldSeed,
        Catalog = input.Catalog,
        ClaimedBy = input.ClaimedBy,
        SourceInputs = input.SourceInputs,
        InteriorProviders = input.InteriorProviders,
        GlobalInputs = input.GlobalInputs,
        GroundMaskContributors = input.GroundMaskContributors,
        PartLods = input.PartLods,
        CodeHash = codeHash,
        WindowNumbering = snapshot
    };

    /// <summary>Validate logical cells, including duplicates between regular facades and individual windows.</summary>
    public static void ValidateMetadata(WindowMetadata metadata, WindowNumberingSnapshot snapshot)
    {
        var blocks = snapshot.Allocations.ToDictionary(item => item.BaseId);
        var seen = new HashSet<int>();
        foreach (var facade in metadata.Facades)
        {
            if (facade is null || facade.BaseId < 0 || facade.TotalColumns <= 0 || facade.Columns <= 0 || facade.Rows <= 0 ||
                facade.ColumnStart < 0 || (long)facade.ColumnStart + facade.Columns > facade.TotalColumns ||
                facade.SillY is null || facade.SillY.Count != facade.Rows || facade.Skip is null ||
                (long)facade.Rows * facade.Columns > 1_000_000 ||
                facade.SillY.Any(y => !float.IsFinite(y)) || !VectorMath.Finite(facade.Origin) || !VectorMath.Finite(facade.Right) || !VectorMath.Finite(facade.Up) ||
                !Positive(facade.Pitch) || !Positive(facade.Width) || !Positive(facade.Height) || LightChannelCatalog.Slot(facade.Channel) < 0)
                throw new InvalidDataException("Invalid window facade metadata.");
            var skip = facade.Skip.ToHashSet();
            if (skip.Count != facade.Skip.Count || skip.Any(cell => cell < 0 || (long)cell >= (long)facade.Columns * facade.Rows))
                throw new InvalidDataException("Invalid/duplicate facade Skip cell.");
            var block = Block(facade.BaseId, facade.TotalColumns);
            if (block is { } allocation && facade.Rows > allocation.Rows)
                throw new InvalidDataException("Window facade extends beyond its allocated rows.");
            for (var row = 0; row < facade.Rows; row++)
                for (var column = 0; column < facade.Columns; column++)
                    if (!skip.Contains(row * facade.Columns + column))
                        Add((long)facade.BaseId + (long)row * facade.TotalColumns + facade.ColumnStart + column, block);
        }
        foreach (var window in metadata.Windows)
        {
            if (window is null || window.Id < 0 || window.BaseId < 0 || window.TotalColumns <= 0 || !VectorMath.Finite(window.Center) ||
                !VectorMath.Finite(window.Right) || !VectorMath.Finite(window.Up) || !Positive(window.Width) || !Positive(window.Height) || LightChannelCatalog.Slot(window.Channel) < 0)
                throw new InvalidDataException("Invalid individual window metadata.");
            Add(window.Id, Block(window.BaseId, window.TotalColumns));
            if (window.Id < window.BaseId) throw new InvalidDataException("Window ID is below its declared base.");
        }

        WindowAllocation? Block(int baseId, int columns)
        {
            if (baseId >= WindowNumberingSnapshot.Capacity) return null; // Existing outskirts ID domain.
            if (!blocks.TryGetValue(baseId, out var allocation) || allocation.Columns != columns)
                throw new InvalidDataException($"Window BaseId/TotalColumns does not match the current allocation: {baseId}/{columns}.");
            return allocation;
        }
        void Add(long id, WindowAllocation? block)
        {
            if (id < 0 || id > int.MaxValue || (block is { } allocation && (id < allocation.BaseId || id >= (long)allocation.BaseId + allocation.Count)))
                throw new InvalidDataException($"Window ID is outside its current allocation: {id}.");
            if (!seen.Add((int)id)) throw new InvalidDataException($"Duplicate logical window ID: {id}.");
        }
    }

    private static bool Positive(float value) => float.IsFinite(value) && value > 0;
}
