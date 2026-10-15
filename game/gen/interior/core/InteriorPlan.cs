using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Interior;

/// <summary>Dimensions for one rectangular double-loaded dormitory wing, not an entire bent building.</summary>
public sealed record DormitoryPlanOptions
{
    public FacadeGridOptions Facade { get; init; } = new();
    public float CorridorClearWidth { get; init; } = 2.3f;
    public float ExteriorWallThickness { get; init; } = 0.25f;
    public float PartitionThickness { get; init; } = 0.2f;
    public float SlabThickness { get; init; } = 0.15f;
    public float DoorClearWidth { get; init; } = 1.1f;
    public float DoorHeight { get; init; } = 2.1f;
    public float StairFlightWidth { get; init; } = 1.45f;
    public float StairWellWidth { get; init; } = 0.2f;
    public float StairLandingDepth { get; init; } = 1.5f;
    public float StairTreadDepth { get; init; } = 0.28f;
    public float MaximumRiserHeight { get; init; } = 0.165f;
    public ChannelId LightChannel { get; init; } = new("interior.main");
}

/// <summary>
/// An explicit opening decision by the owning provider. FacadeBayIndex comes from the shared grid;
/// omitted rooms remain locked. Type is a furniture-template key, such as dorm.four or service.hot-water.
/// </summary>
public sealed record InteriorRoomUse(int Storey, int FacadeBayIndex, string Type, bool Open,
    ZoneKind Kind = ZoneKind.Room, DoorLeafKind DoorKind = DoorLeafKind.RoomDoor);

/// <summary>
/// The caller supplies an authored rectangular wing and its absolute storey heights.
/// An irregular building must be split into wings by its provider; bounding-box substitution is rejected.
/// Entrances and seals remain authoritative caller data. No registration or file IO occurs here.
/// </summary>
public sealed record InteriorPlanRequest(FeatureId Feature, string WingId, uint WorldSeed,
    PolygonRings Footprint, IReadOnlyList<StoreyInfo> Storeys,
    IReadOnlyList<EntranceDecl> Entrances, IReadOnlyList<InteriorRoomUse> Rooms,
    IReadOnlyList<SealOp> Seals)
{
    public DormitoryPlanOptions Options { get; init; } = new();
    /// <summary>Reserve disjoint non-negative ranges when composing several generated wings.</summary>
    public int ZoneIdBase { get; init; }
}

public enum InteriorWallRole : byte { Exterior, Partition, LockedDoor, WindowGlass, StairBarrier, Seal }
public sealed record InteriorWallSpan(int Storey, Vector2 A, Vector2 B, float Thickness,
    float BottomY, float TopY, InteriorWallRole Role)
{
    public SealKind? Closure { get; init; }
    public DoorLeafKind? LeafKind { get; init; }
}
public sealed record InteriorSlab(int Storey, IReadOnlyList<Vector2> Ring, float TopY, float Thickness);
public sealed record InteriorRoom(int Storey, int ZoneId, int FacadeBayIndex, string Type,
    IReadOnlyList<Vector2> ClearRing, bool Open);
public sealed record InteriorWindow(int Storey, int FacadeBayIndex, int ZoneId, FacadeWindowKind Kind);
/// <summary>Two flights share a mid-landing; the upper landing is part of the next storey's floor slab.</summary>
public sealed record InteriorStair(int LowerStorey, int UpperStorey, int LowerZone, int UpperZone,
    RampCollider UpFlight, RampCollider ReturnFlight, InteriorSlab MidLanding,
    IReadOnlyList<Vector2> FloorOpening);

/// <summary>
/// Data handoff to shell/selected providers. Constructing InteriorLayout, rendering and registration
/// remain downstream responsibilities. All spatial outputs are quantized to 1 mm.
/// </summary>
public sealed record InteriorPlan(FeatureId Feature, string WingId, FacadeGrid Facade,
    IReadOnlyList<StoreyInfo> Storeys, IReadOnlyList<ZoneVolume> Zones,
    IReadOnlyList<PortalDecl> Portals, IReadOnlyList<DoorLeafDecl> Doors,
    IReadOnlyList<EntranceDecl> Entrances, IReadOnlyList<InteriorRoom> Rooms,
    IReadOnlyList<InteriorWindow> Windows, IReadOnlyList<InteriorWallSpan> Walls,
    IReadOnlyList<InteriorSlab> Slabs, IReadOnlyList<InteriorStair> Stairs, uint PlanHash);
