using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Gen.Interior;

/// <summary>Static collision handoff for the generated plan; automatic door leaves are never baked here.</summary>
public static class InteriorPlanCollision
{
    public static IReadOnlyList<Collider> BuildStorey(InteriorPlan plan, int storey)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (!plan.Storeys.Any(s => s.Index == storey)) return [];
        var result = new List<Collider>();
        foreach (InteriorSlab slab in plan.Slabs.Where(s => s.Storey == storey))
            result.Add(Slab(slab));
        foreach (InteriorWallSpan wall in plan.Walls.Where(w => w.Storey == storey))
        {
            (SurfaceTag tag, FootMat foot) = wall.Role switch
            {
                InteriorWallRole.Exterior or InteriorWallRole.Partition => (SurfaceTag.Wall, FootMat.Concrete),
                InteriorWallRole.LockedDoor => (SurfaceTag.DoorClosed, wall.LeafKind switch
                {
                    null or DoorLeafKind.RoomDoor => FootMat.WoodDoor,
                    DoorLeafKind.FireDoor => FootMat.Metal,
                    DoorLeafKind.GlassSwing => FootMat.Glass,
                    _ => throw new ArgumentOutOfRangeException(nameof(plan), "Unknown door leaf kind.")
                }),
                InteriorWallRole.WindowGlass => (SurfaceTag.Wall, FootMat.Glass),
                InteriorWallRole.StairBarrier => (SurfaceTag.Barrier, FootMat.Metal),
                InteriorWallRole.Seal => (SurfaceTag.DoorClosed, wall.Closure switch
                {
                    SealKind.GlassPartition => FootMat.Glass,
                    SealKind.ShelfWall => FootMat.WoodDoor,
                    SealKind.RollerShutter => FootMat.Shutter,
                    SealKind.IronGate or SealKind.FireDoor or SealKind.FoldingGate => FootMat.Metal,
                    _ => throw new ArgumentOutOfRangeException(nameof(plan), "Unknown seal kind.")
                }),
                _ => throw new ArgumentOutOfRangeException(nameof(plan), "Unknown interior wall role.")
            };
            result.Add(new WallCollider(wall.A, wall.B, wall.Thickness * 0.5f, wall.BottomY, wall.TopY, tag) { Foot = foot });
        }
        foreach (InteriorStair stair in plan.Stairs.Where(s => s.LowerStorey == storey))
        {
            result.Add(stair.UpFlight);
            result.Add(stair.ReturnFlight);
            result.Add(Slab(stair.MidLanding));
        }
        return result.ToArray();
    }

    private static SlabCollider Slab(InteriorSlab slab) => new(slab.Ring, slab.TopY, slab.Thickness, SurfaceTag.Ground)
    { Foot = FootMat.Terrazzo };
}
