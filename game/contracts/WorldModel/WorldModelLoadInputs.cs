using System.Collections.ObjectModel;
using Campus.Contracts.Maps;

namespace Campus.Contracts;

/// <summary>Explicit, preloaded world-building inputs. No IO, registry or runtime service is consulted.</summary>
public sealed record WorldModelLoadInputs(MapDescriptor Descriptor,
    IReadOnlyDictionary<FeatureId, WpId>? ClaimedBy = null)
{
    private readonly IReadOnlyDictionary<ZoneId, IReadOnlyList<PolygonRings>>? _zoneBoundaries;

    /// <summary>Optional authoritative zone polygons for ownership and influence-extent acceptance; not a height-evaluation prerequisite.
    /// Null means unavailable; an empty dictionary means supplied with no zones. Snapshotting does not assert validation.</summary>
    public IReadOnlyDictionary<ZoneId, IReadOnlyList<PolygonRings>>? ZoneBoundaries
    {
        get => _zoneBoundaries;
        init => _zoneBoundaries = SnapshotZones(value);
    }

    private static IReadOnlyDictionary<ZoneId, IReadOnlyList<PolygonRings>>? SnapshotZones(
        IReadOnlyDictionary<ZoneId, IReadOnlyList<PolygonRings>>? source)
    {
        if (source is null) return null;
        var copy = new SortedDictionary<ZoneId, IReadOnlyList<PolygonRings>>(
            Comparer<ZoneId>.Create((left, right) => StringComparer.Ordinal.Compare(left.Value, right.Value)));
        foreach (var (zone, polygons) in source)
        {
            ArgumentNullException.ThrowIfNull(polygons);
            copy.Add(zone, Array.AsReadOnly(polygons.Select(SnapshotPolygon).ToArray()));
        }
        return new ReadOnlyDictionary<ZoneId, IReadOnlyList<PolygonRings>>(copy);
    }

    private static PolygonRings SnapshotPolygon(PolygonRings polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        ArgumentNullException.ThrowIfNull(polygon.Outer);
        ArgumentNullException.ThrowIfNull(polygon.Holes);
        var holes = polygon.Holes.Select(ring =>
        {
            ArgumentNullException.ThrowIfNull(ring);
            return (IReadOnlyList<System.Numerics.Vector2>)Array.AsReadOnly(ring.ToArray());
        }).ToArray();
        return new(Array.AsReadOnly(polygon.Outer.ToArray()), Array.AsReadOnly(holes));
    }

    public MapDescriptor Descriptor { get; } = Descriptor ?? throw new ArgumentNullException(nameof(Descriptor));
    // Copy the resolved map claims ledger once. Later mutations of the caller's dictionary cannot affect a load.
    public IReadOnlyDictionary<FeatureId, WpId>? ClaimedBy { get; } = ClaimedBy is null ? null :
        new ReadOnlyDictionary<FeatureId, WpId>(ClaimedBy.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value));
}
