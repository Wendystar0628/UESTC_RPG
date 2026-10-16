using Campus.Contracts.Atlas;

namespace Campus.Systems.Atlas;

public sealed class AllUnlockedPolicy : IUnlockPolicy
{
    public string Id => "all-unlocked";
    public UnlockState Initial(AtlasMarker marker) => UnlockState.Unlocked;
    public UnlockState OnVisit(AtlasMarker marker, UnlockState now) => UnlockState.Unlocked;
}

public sealed class ProximityUnlockPolicy : IUnlockPolicy
{
    public string Id => "proximity";
    // Kiosks are discoverable destinations; their district posts are revealed on a visit.
    public UnlockState Initial(AtlasMarker marker) => marker.Kind switch
    {
        MarkerKind.Anchor => marker.Form == AnchorForm.Kiosk ? UnlockState.Known : UnlockState.Hidden,
        MarkerKind.Viewpoint or MarkerKind.Interior or MarkerKind.Route or MarkerKind.Landmark
            or MarkerKind.Facility or MarkerKind.Custom or MarkerKind.Quest => UnlockState.Unlocked,
        _ => throw new ArgumentOutOfRangeException(nameof(marker)),
    };
    public UnlockState OnVisit(AtlasMarker marker, UnlockState now) => UnlockState.Unlocked;
}

internal static class AtlasPolicies
{
    internal static IUnlockPolicy Create(string id) => id switch
    {
        "all-unlocked" => new AllUnlockedPolicy(),
        "proximity" => new ProximityUnlockPolicy(),
        _ => throw new NotSupportedException($"Unknown Atlas unlock policy: {id}"),
    };

    internal static bool IsSupported(string? id) => id is "all-unlocked" or "proximity";
}
