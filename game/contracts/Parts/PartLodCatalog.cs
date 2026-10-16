namespace Campus.Contracts.Parts;

/// <summary>
/// Immutable per-map capabilities captured from explicitly identified part definitions.
/// Does not retain definitions, builders or caller-owned collections. An empty catalog is an explicit
/// registration result; absence of a catalog is represented by BuildContext.PartLods being null.
/// </summary>
public sealed class PartLodCatalog
{
    private readonly Dictionary<string, IReadOnlyList<LodLevel>> _supported = new(StringComparer.Ordinal);

    public PartLodCatalog(IEnumerable<KeyValuePair<PartId, IPartDef>> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var entries = definitions.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal).ToArray();
        // Reject identity errors before reading any definition's capabilities.
        string? previous = null;
        foreach (var pair in entries)
        {
            ValidateId(pair.Key);
            if (StringComparer.Ordinal.Equals(previous, pair.Key.Value))
                throw new ArgumentException($"Duplicate part ID: '{pair.Key.Value}'.", nameof(definitions));
            if (pair.Value is null)
                throw new ArgumentException($"Part '{pair.Key.Value}' has no definition.", nameof(definitions));
            previous = pair.Key.Value;
        }
        foreach (var pair in entries)
        {
            try { _supported.Add(pair.Key.Value, FreezeLods(pair.Value.SupportedLods)); }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException)
            { throw new ArgumentException($"Invalid LOD capabilities for part '{pair.Key.Value}': {error.Message}", nameof(definitions), error); }
        }
    }

    /// <summary>Returns the frozen numeric-order list; an unknown ID is an error, not a missing optional capability.</summary>
    public IReadOnlyList<LodLevel> SupportedLods(PartId id)
    {
        ValidateId(id);
        return _supported.TryGetValue(id.Value, out var levels) ? levels :
            throw new KeyNotFoundException($"No registered part LOD capabilities for '{id.Value}'.");
    }

    /// <summary>False means a known part lacks this known LOD. Unknown IDs and undefined LOD values throw.</summary>
    public bool Supports(PartId id, LodLevel lod)
    {
        var levels = SupportedLods(id);
        ValidateLevel(lod);
        return levels.Contains(lod);
    }

    // Shared with IPartDef<TParams>'s default implementation. No registry, cache or reflected Lods access.
    internal static IReadOnlyList<LodLevel> FreezeLods(IEnumerable<LodLevel> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var levels = source.ToArray();
        if (levels.Length == 0) throw new ArgumentException("A part must declare at least one LOD.", nameof(source));
        foreach (var level in levels) ValidateLevel(level);
        Array.Sort(levels, (left, right) => ((byte)left).CompareTo((byte)right));
        for (var i = 1; i < levels.Length; i++)
            if (levels[i] == levels[i - 1]) throw new ArgumentException("A part's LOD declarations must be unique.", nameof(source));
        return Array.AsReadOnly(levels);
    }

    private static void ValidateId(PartId id)
    {
        if (string.IsNullOrWhiteSpace(id.Value)) throw new ArgumentException("Part ID must be non-empty.", nameof(id));
    }

    private static void ValidateLevel(LodLevel lod)
    {
        switch (lod)
        {
            case LodLevel.L0:
            case LodLevel.L1:
            case LodLevel.L2:
            case LodLevel.L3:
            case LodLevel.L4:
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown part LOD.");
        }
    }
}
