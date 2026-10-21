using Campus.Contracts;
using Campus.Contracts.Maps;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera.Walk;

/// <summary>Current-map terrain coverage, authored landing points and motion preparation.</summary>
internal sealed class WalkTerrainPreparation
{
    private ITerrainPreparation? _terrain;
    private IMapContext? _map;
    private Task? _terrainPreparation;
    private Task? _landingPointsPreparation;
    private Task _unloading = Task.CompletedTask;

    public bool LandingPointsPending => _landingPointsPreparation is not null;
    public Exception? Failure { get; private set; }
    public void ResolveService() => _terrain ??= KernelServices.Get<ITerrainPreparation>();
    public void BindMap(IMapContext? map) => _map = map;
    public bool IsPrepared(N.Vector3 feet) => _map is not null && _terrain is not null
        && _terrain.IsPrepared(_map.World, new(feet.X, feet.Z));

    public void PrepareAuthoredLandings(IMapContext map)
    {
        _map = map;
        Failure = null;
        var points = map.Descriptor.Spawns.Where(s => s.Stand is not null)
            .Select(s => new N.Vector2(s.Stand!.Feet.X, s.Stand.Feet.Z)).ToArray();
        _landingPointsPreparation = _terrain!.PreparePoints(map, points);
    }

    public void PrepareSavedHistory(WalkSafePoints history)
    {
        var points = history.Points.Select(p => new N.Vector2(p.Position.X, p.Position.Z)).ToArray();
        var preparation = _terrain!.PreparePoints(_map!, points);
        _landingPointsPreparation = _landingPointsPreparation is { } authored ? Task.WhenAll(authored, preparation) : preparation;
    }

    public bool CompleteLandingPoints()
    {
        if (_landingPointsPreparation is not { } landingPoints) return true;
        if (!landingPoints.IsCompleted) return false;
        try { landingPoints.GetAwaiter().GetResult(); }
        catch (Exception error)
        {
            var failures = landingPoints.Exception;
            Failure = failures is { InnerExceptions.Count: > 1 } ? failures : error;
            throw;
        }
        _landingPointsPreparation = null;
        return true;
    }

    public void Unload()
    {
        _map = null;
        _unloading = Task.WhenAll(new[] { _unloading, _terrainPreparation, _landingPointsPreparation }.OfType<Task>());
        _terrainPreparation = null;
        _landingPointsPreparation = null;
        Failure = null;
    }

    public async Task DrainAsync()
    {
        var draining = _unloading;
        try { await draining; }
        catch (OperationCanceledException) when (draining.IsCanceled) { }
        catch { throw draining.Exception!; }
    }

    public bool TerrainReady(N.Vector3 point, float radius)
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
