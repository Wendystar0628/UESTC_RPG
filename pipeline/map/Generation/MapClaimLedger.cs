using Campus.Contracts;
using System.Collections.ObjectModel;

namespace Campus.Pipeline.Map;

/// <summary>Resolves declared claims against this loaded map; package names never imply ownership.</summary>
public sealed record MapClaimSource(string Module, WpId Owner, bool Global, IReadOnlyList<Claim> Claims);

/// <summary>Detached declarations captured before worker parsing; overlay exceptions never invent an owner.</summary>
public sealed class MapClaimSnapshot
{
    public MapClaimSnapshot(IEnumerable<MapClaimSource> sources) => Sources = Array.AsReadOnly(sources
        .OrderBy(source => source.Module, StringComparer.Ordinal)
        .Select(source => source with { Claims = Array.AsReadOnly(source.Claims.ToArray()) }).ToArray());
    public IReadOnlyList<MapClaimSource> Sources { get; }
    public bool RequiresAreaResolution => Sources.Any(source => source.Claims.Any(claim => claim is AreaClaim));
    public MapClaimLedger Build(IWorldModel world) => new(world, Sources);
    public IReadOnlyDictionary<FeatureId, WpId> OverlayOwners(IWorldModel? baseline = null)
    {
        var candidates = new Dictionary<FeatureId, HashSet<WpId>>();
        foreach (var source in Sources)
            foreach (var claim in source.Claims)
            {
                IEnumerable<FeatureId> features = claim switch
                {
                    FeatureClaim feature when !source.Global => [feature.Feature],
                    FeatureClaim => throw new InvalidDataException($"Global FeatureClaim: {source.Module}"),
                    AreaClaim area when baseline is not null => baseline.Features.Values.Where(f => f.Zone == area.Zone).Select(f => f.Id),
                    AreaClaim => [],
                    _ => throw new InvalidDataException("Unknown claim kind.")
                };
                foreach (var feature in features)
                {
                    if (!candidates.TryGetValue(feature, out var owners)) candidates[feature] = owners = [];
                    owners.Add(source.Owner);
                }
            }
        return new ReadOnlyDictionary<FeatureId, WpId>(candidates.Where(pair => pair.Value.Count == 1)
            .OrderBy(pair => pair.Key.Value, StringComparer.Ordinal).ToDictionary(pair => pair.Key, pair => pair.Value.Single()));
    }
}

public sealed class MapClaimLedger
{
    private static readonly Aspects[] Bits = Enum.GetValues<Aspects>();
    private readonly Dictionary<(FeatureId Feature, Aspects Aspect), WpId> _owners = [];
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
                        if (!_owners.TryAdd((feature, bit), module.Owner))
                            throw new InvalidDataException($"Conflicting claims: {feature.Value}/{bit}; {_owners[(feature, bit)].Value} and {module.Owner.Value} ({id}).");
            }
    }

    public WpId? ClaimedBy(FeatureId feature, Aspects aspects)
    {
        WpId? owner = null;
        foreach (var bit in Bits.Where(bit => (aspects & bit) != 0))
        {
            if (!_owners.TryGetValue((feature, bit), out var found)) return null;
            if (owner is { } existing && existing != found) throw new InvalidOperationException("Composite claim query has different owners; query one aspect at a time.");
            owner = found;
        }
        return owner;
    }
}
