using Campus.Gen.World;
using Campus.Gen.Parts.Street;
using Campus.Contracts.Bake;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.Boundary;

/// <summary>
/// WP-30 campus boundary and gates (WLD-OUTSKIRTS, MAP-QSH-ADDITIONS): the sites listed in
/// <see cref="BoundarySites.All"/>, filed into 64 m cells. 30a built the north and west gates and the waterside plaza inside
/// the north gate; 30b adds 西北门, 西二门 and 东南口, and the fence all the way round with its side gates and river grates. Every closure collider
/// is tagged <see cref="SurfaceTag.Boundary"/>. Pure .NET: reads the world model and the terrain only.
/// </summary>

[ContentModule("zone.boundary")]
public sealed class BoundaryModule : ZoneModuleBase
{
    public override BakeInputDependencies BakeInputs { get; } = new(BakeInputKinds.World | BakeInputKinds.Windows, [], []);

    public BoundaryModule() : this(BoundarySites.All) { }

    internal BoundaryModule(IReadOnlyList<ZoneSite> sites) : base(sites) { }
}

/// <summary>The boundary package's sites in a fixed order; later sub-tickets append theirs after these.</summary>
public static class BoundarySites
{
    public static IReadOnlyList<ZoneSite> All { get; } = Array.AsReadOnly<ZoneSite>(
    [
        // 30a (north, west) and 30b (northwest, west2, southeast)
        .. BoundaryTune.Gates.Select(g => (ZoneSite)new Gates.GateSite(g)),
        new Waterfront.NorthPlazaSite(),
        // 30b: the fence all the way round, side gates, river grates
        new Ring.RingFenceSite(),
    ]);
}
