using Campus.Contracts;
using N = System.Numerics;

namespace Campus.Camera.Walk;

public partial class WalkController
{
    private bool TerrainReady(N.Vector3 point, float radius)
    {
        if (_terrainPreparation is { } pending)
        {
            if (!pending.IsCompleted) return false;
            pending.GetAwaiter().GetResult(); _terrainPreparation = null;
        }
        var center = new N.Vector2(point.X, point.Z);
        var min = center - new N.Vector2(radius);
        var max = center + new N.Vector2(radius);
        var terrain = _terrain!;
        var world = _map!.World;
        // Sparse landing/path tables may contain the corners without the intervening motion cells.
        bool prepared = true;
        for (int z = (int)MathF.Floor(min.Y); prepared && z <= (int)MathF.Ceiling(max.Y); z++)
            for (int x = (int)MathF.Floor(min.X); x <= (int)MathF.Ceiling(max.X); x++)
                if (!terrain.IsPrepared(world, new(x, z))) { prepared = false; break; }
        if (prepared) return true;
        _terrainPreparation = terrain.PrepareRegion(_map, new(min, max));
        if (!_terrainPreparation.IsCompleted) return false;
        _terrainPreparation.GetAwaiter().GetResult(); _terrainPreparation = null;
        return true;
    }
}
