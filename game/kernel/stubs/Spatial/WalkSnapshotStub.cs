using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Stubs;

/// <summary>No physical publisher is installed. Default positions/shapes are undefined behind Absent/Pending.</summary>
[KernelService(typeof(IWalkSnapshot), Owner = "WP-00", IsStub = true)]
public sealed class WalkSnapshotStub(IWorldState world, IMapService maps, IViewControl view) : IWalkSnapshot
{
    public WalkSnapshot Previous => Current;
    public WalkSnapshot Current
    {
        get
        {
            ViewMode mode = view.Mode;
            var presence = mode switch
            {
                ViewMode.Fly => WalkPresence.Absent,
                ViewMode.Walk => WalkPresence.Pending,
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            };
            return new WalkSnapshot
            {
                Stamp = new(maps.Current, maps.Rev, world.Instance, 0, 0),
                Mode = mode,
                Presence = presence,
                Hold = WalkHold.CollisionNotReady | WalkHold.WaterNotReady,
                Support = null,
                Water = null,
                NextFoot = null
            };
        }
    }
    public float Fraction => 1;
}
