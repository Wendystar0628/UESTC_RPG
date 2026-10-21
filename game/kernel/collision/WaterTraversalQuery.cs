using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Collision;

/// <summary>Queries frozen natural water geometry independently of visible tiles or collision residency.</summary>
[KernelService(typeof(IWaterTraversalQuery))]
public sealed class WaterTraversalQuery(IMapService maps) : IWaterTraversalQuery
{
    private IMapContext? _context;
    private WaterTraversalCatalog? _catalog;
    private WaterQueryStatus _catalogStatus;

    public WaterQueryResult Sample(Vector2 point)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) return Invalid("water.query.nonfinite");
        var status = EnsureCatalog();
        if (status != WaterQueryStatus.Ready) return new(status, diagnosticCode: "water.catalog.unavailable");
        return SampleCore(point);
    }

    private WaterQueryStatus EnsureCatalog()
    {
        var context = maps.Context;
        if (context is null)
        {
            _context = null; _catalog = null;
            return _catalogStatus = WaterQueryStatus.NotReady;
        }
        if (ReferenceEquals(_context, context)) return _catalogStatus;
        if (context.World.Terrain is not ITerrainWaterSource source)
        {
            _context = context; _catalog = null;
            return _catalogStatus = WaterQueryStatus.NotReady;
        }
        var catalog = new WaterTraversalCatalog(context.World, source);
        _catalog = catalog; _context = context;
        return _catalogStatus = WaterQueryStatus.Ready;
    }

    private WaterQueryResult SampleCore(Vector2 point)
    {
        var terrain = _context!.World.Terrain;
        foreach (var water in _catalog!.Waters)
        {
            if (!WaterTraversalCatalog.Contains(water.Polygons, point)) continue;
            var level = terrain.WaterY(water.Water);
            if (level is null) return new(WaterQueryStatus.NotReady, diagnosticCode: "water.level.missing");
            var bottom = terrain.HeightAt(point.X, point.Y);
            if (!float.IsFinite(level.Value) || !float.IsFinite(bottom)) return Invalid("water.height.nonfinite");
            if (bottom >= level.Value) continue;
            return new(WaterQueryStatus.Ready, new WaterSample(level.Value, bottom,
                WaterTraversalCatalog.Contains(_catalog.SwimAreas, point)));
        }
        return new(WaterQueryStatus.NoWater);
    }

    private static WaterQueryResult Invalid(string code) => new(WaterQueryStatus.Invalid, diagnosticCode: code);
}
