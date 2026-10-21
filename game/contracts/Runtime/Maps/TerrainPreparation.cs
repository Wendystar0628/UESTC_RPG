using System.Numerics;
using Campus.Contracts.Maps;

namespace Campus.Contracts;

/// <summary>Explicit asynchronous preparation; readers only query after their requested region or points are ready.</summary>
public interface ITerrainPreparation
{
    bool IsPrepared(IWorldModel world, Vector2 point);
    Task PrepareRegion(IMapContext context, MapRect region, CancellationToken cancellation = default);
    Task PreparePoints(IMapContext context, IReadOnlyList<Vector2> points, CancellationToken cancellation = default);
}
