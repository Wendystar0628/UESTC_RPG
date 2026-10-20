using Campus.Contracts;

namespace Campus.Gen.BakeInput;

/// <summary>Resolves declared claims against this loaded map; package names never imply ownership.</summary>
public sealed record MapClaimSource(string Module, string Owner, bool Global, IReadOnlyList<Claim> Claims);

/// <summary>Detached declarations captured before worker parsing and resolved against the final world.</summary>
public sealed class MapClaimSnapshot
{
    public MapClaimSnapshot(IEnumerable<MapClaimSource> sources) => Sources = Array.AsReadOnly(sources
        .OrderBy(source => source.Module, StringComparer.Ordinal)
        .Select(source => source with { Claims = Array.AsReadOnly(source.Claims.ToArray()) }).ToArray());
    public IReadOnlyList<MapClaimSource> Sources { get; }
    public MapClaimLedger Build(IWorldModel world) => new(world, Sources);
}

public sealed class MapClaimLedger
{
    private static readonly Aspects[] Bits = Enum.GetValues<Aspects>();
    private readonly Dictionary<(FeatureId Feature, Aspects Aspect), string> _modules = [];
    public MapClaimLedger(IWorldModel world, IEnumerable<MapClaimSource> modules)
    {
        foreach (var module in modules.OrderBy(p => p.Module, StringComparer.Ordinal))
            foreach (var claim in module.Claims)
            {
                var id = module.Module;
                var knownMask = Bits.Aggregate((Aspects)0, (mask, bit) => mask | bit);
                if (claim.Aspects == 0 || (claim.Aspects & ~knownMask) != 0) throw new InvalidDataException($"Unknown/empty claim aspects: {id}");
                IEnumerable<FeatureId> features = claim switch
                {
                    FeatureClaim feature when !module.Global && world.Features.ContainsKey(feature.Feature) => [feature.Feature],
                    FeatureClaim => throw new InvalidDataException($"Missing feature or global FeatureClaim: {id}"),
                    AreaClaim area => world.Features.Values.Where(f => f.Zone == area.Zone).Select(f => f.Id).OrderBy(f => f.Value, StringComparer.Ordinal).ToArray(),
                    _ => throw new InvalidDataException($"Unsupported claim kind: {id}")
                };
                foreach (var feature in features)
                    foreach (var bit in Bits.Where(bit => (claim.Aspects & bit) != 0))
                        if (!_modules.TryAdd((feature, bit), module.Module))
                            throw new InvalidDataException($"Conflicting claims: {feature.Value}/{bit}; {_modules[(feature, bit)]} and {module.Module} (owner {module.Owner}).");
            }
    }

    public string? ClaimedBy(FeatureId feature, Aspects aspects)
    {
        string? owner = null;
        foreach (var bit in Bits.Where(bit => (aspects & bit) != 0))
        {
            if (!_modules.TryGetValue((feature, bit), out var found)) return null;
            if (owner is { } existing && existing != found) throw new InvalidOperationException("Composite claim query has different owners; query one aspect at a time.");
            owner = found;
        }
        return owner;
    }
}
