using System.Collections.ObjectModel;
using Campus.Contracts.Maps;

namespace Campus.Contracts;

/// <summary>Explicit, preloaded world-building inputs. No IO, registry or runtime service is consulted.</summary>
public sealed record WorldModelLoadInputs(MapDescriptor Descriptor, WalkBakeData? Walk = null,
    IReadOnlyDictionary<FeatureId, WpId>? ClaimedBy = null)
{
    public MapDescriptor Descriptor { get; } = Descriptor ?? throw new ArgumentNullException(nameof(Descriptor));
    public WalkBakeData? Walk { get; } = Walk;
    // Copy the resolved map claims ledger once. Later mutations of the caller's dictionary cannot affect a load.
    public IReadOnlyDictionary<FeatureId, WpId>? ClaimedBy { get; } = ClaimedBy is null ? null :
        new ReadOnlyDictionary<FeatureId, WpId>(ClaimedBy.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value));
}
