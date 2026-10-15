using Campus.Contracts;

namespace Campus.Kernel.Stubs.Foundation;

/// <summary>Explicit classifications supplied by the existing water catalog; unknown is never guessed from an ID.</summary>
public enum FlatTerrainWaterKind : byte { Unknown, Lake, Channel }

/// <summary>Stage-0 adapter from known water categories to an immutable contract-only flat terrain snapshot.</summary>
public static class FlatTerrainStub
{
    public static FlatTerrain FromWaterKinds(IReadOnlyDictionary<FeatureId, FlatTerrainWaterKind> waterKinds)
    {
        ArgumentNullException.ThrowIfNull(waterKinds);
        var levels = new Dictionary<FeatureId, float>();
        foreach (var pair in waterKinds.OrderBy(p => p.Key.Value, StringComparer.Ordinal))
        {
            float? level = pair.Value switch
            {
                FlatTerrainWaterKind.Unknown => null,
                FlatTerrainWaterKind.Lake => -0.5f,
                FlatTerrainWaterKind.Channel => -0.6f,
                _ => throw new ArgumentOutOfRangeException(nameof(waterKinds), "Unknown water classification.")
            };
            if (level is float y) levels.Add(pair.Key, y);
        }
        // The WP-24 visual lake lift remains a mesh offset; it does not alter the logical constant water level.
        return new(levels);
    }
}
