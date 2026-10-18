using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Collision;

/// <summary>Queries the current map's frozen water geometry, never its visible water tiles or collision residency.</summary>
[KernelService(typeof(IWaterTraversalQuery), Owner = "WP-08")]
public sealed partial class WaterTraversalQuery(IMapService maps) : IWaterTraversalQuery
{
    private IMapContext? _context;
    private WaterTraversalCatalog? _catalog;
    private WaterQueryStatus _catalogStatus;
    private float[] _cuts = [];
    private long _revision;
    public MapId Map => maps.Current;
    public long DataRevision { get { EnsureCatalog(); return _revision; } }

    public WaterQueryResult Sample(in WaterQuery query)
    {
        if (!VectorMath.Finite(query.Position)) return Invalid("water.query.nonfinite");
        var status = EnsureCatalog();
        if (status != WaterQueryStatus.Ready) return new(status, diagnosticCode: "water.catalog.unavailable");
        try { return SampleCore(query.Position, query.Space); }
        catch (ArgumentException) { return Invalid("water.sample.invalid"); }
        catch (InvalidOperationException) { return Invalid("water.sample.invalid"); }
    }

    private WaterQueryStatus EnsureCatalog()
    {
        var context = maps.Context;
        if (context is null)
        {
            if (_context is not null) _revision++;
            _context = null; _catalog = null; _cuts = [];
            return _catalogStatus = WaterQueryStatus.NotReady;
        }
        if (ReferenceEquals(_context, context)) return _catalogStatus;
        _context = context; _catalog = null; _cuts = []; _revision++;
        if (context.World.Terrain is not ITerrainWaterSource source) return _catalogStatus = WaterQueryStatus.NotReady;
        try
        {
            _catalog = new(context.World, source);
            _cuts = new float[_catalog.TraceCapacity];
            return _catalogStatus = WaterQueryStatus.Ready;
        }
        catch (JsonException) { return _catalogStatus = WaterQueryStatus.Invalid; }
        catch (ArgumentException) { return _catalogStatus = WaterQueryStatus.Invalid; }
        catch (InvalidOperationException) { return _catalogStatus = WaterQueryStatus.Invalid; }
        catch (FormatException) { return _catalogStatus = WaterQueryStatus.Invalid; }
    }

    private WaterQueryResult SampleCore(Vector3 position, NavSpace? space)
    {
        var point = new Vector2(position.X, position.Z);
        foreach (var volume in _catalog!.Volumes)
        {
            if (!WaterTraversalCatalog.SameSpace(space, volume.Space) ||
                !WaterTraversalCatalog.Contains(volume.Polygons, point)) continue;
            if (volume.MinY is { } min && position.Y < min || volume.MaxY is { } max && position.Y > max) continue;
            var result = SampleVolume(volume, position);
            if (result.Status != WaterQueryStatus.NoWater) return result;
        }
        return new(WaterQueryStatus.NoWater);
    }

    private WaterQueryResult SampleVolume(WaterVolume volume, Vector3 position)
    {
        var terrain = _context!.World.Terrain;
        var level = terrain.WaterY(volume.Water);
        if (level is null) return new(WaterQueryStatus.NotReady, diagnosticCode: "water.level.missing");
        var bottom = volume.BottomKind switch
        {
            WaterBottomKind.Terrain => terrain.HeightAt(position.X, position.Z),
            WaterBottomKind.Plane => volume.PlaneY!.Value,
            _ => throw new InvalidOperationException("Unknown water bottom kind.")
        };
        if (!float.IsFinite(level.Value) || !float.IsFinite(bottom)) return Invalid("water.height.nonfinite");
        var min = volume.MinY ?? bottom; var max = volume.MaxY ?? level.Value;
        if (bottom < min || level > max || volume.MinY.HasValue && bottom > level)
            return Invalid("water.volume.heights");
        // Dry terrain inside a footprint is not a water column, and water never extends beneath its bed.
        if (bottom >= level || position.Y < bottom || position.Y > level || position.Y < min || position.Y > max)
            return new(WaterQueryStatus.NoWater);
        var point = new Vector2(position.X, position.Z);
        var access = WaterTraversalCatalog.Contains(volume.WadeAreas, point) ? WaterAccess.WadeOnly : WaterAccess.Forbidden;
        string? areaId = null;
        foreach (var area in _catalog!.Areas)
        {
            if (area.VolumeId != volume.Id || !WaterTraversalCatalog.Contains(area.Polygons, point)) continue;
            if (areaId is not null && area.Access != access) return Invalid("water.permission.overlap");
            access = area.Access; areaId = area.Id;
        }
        return new(WaterQueryStatus.Ready, new WaterSample(position, volume.Water, volume.Id, areaId,
            volume.Space, level.Value, bottom, volume.BottomKind, min, max, access));
    }

    public WaterQueryStatus ExitsNear(in WaterQuery query, float radius, Span<WaterExitDecl> buffer, out int written)
    {
        written = 0;
        if (!VectorMath.Finite(query.Position) || !float.IsFinite(radius) || radius < 0) return WaterQueryStatus.Invalid;
        var status = EnsureCatalog();
        if (status != WaterQueryStatus.Ready) return status;
        try { return ExitsNearCore(query, radius, buffer, out written); }
        catch (ArgumentException) { written = 0; buffer.Clear(); return WaterQueryStatus.Invalid; }
        catch (InvalidOperationException) { written = 0; buffer.Clear(); return WaterQueryStatus.Invalid; }
    }

    private WaterQueryStatus ExitsNearCore(in WaterQuery query, float radius, Span<WaterExitDecl> buffer, out int written)
    {
        written = 0;
        var point = new Vector2(query.Position.X, query.Position.Z);
        foreach (var volume in _catalog!.Volumes)
        {
            if (!WaterTraversalCatalog.SameSpace(query.Space, volume.Space) ||
                !WaterTraversalCatalog.Contains(volume.Polygons, point)) continue;
            if (volume.MinY is { } min && query.Position.Y < min || volume.MaxY is { } max && query.Position.Y > max) continue;
            var level = _context!.World.Terrain.WaterY(volume.Water);
            if (level is null) return WaterQueryStatus.NotReady;
            if (!float.IsFinite(level.Value)) return WaterQueryStatus.Invalid;
        }
        // Resolve every nearby level before writing: an unavailable result never publishes a partial exit set.
        foreach (var exit in _catalog!.Exits)
        {
            var volume = _catalog.Volume(exit.VolumeId)!;
            if (!WaterTraversalCatalog.SameSpace(query.Space, volume.Space)) continue;
            var distance = ExitDistanceSquared(exit, query.Position);
            if (distance > (double)radius * radius) continue;
            var level = _context!.World.Terrain.WaterY(volume.Water);
            if (level is null || !float.IsFinite(level.Value))
                return level is null ? WaterQueryStatus.NotReady : WaterQueryStatus.Invalid;
        }
        // Insertion into the caller's span retains nearest exits without per-step lists or sorting allocations.
        foreach (var exit in _catalog.Exits)
        {
            var volume = _catalog.Volume(exit.VolumeId)!;
            if (!WaterTraversalCatalog.SameSpace(query.Space, volume.Space)) continue;
            var distance = ExitDistanceSquared(exit, query.Position);
            if (distance > (double)radius * radius) continue;
            int index = 0;
            while (index < written)
            {
                var other = ExitDistanceSquared(buffer[index], query.Position);
                if (distance < other || distance == other && string.CompareOrdinal(exit.Id, buffer[index].Id) < 0) break;
                index++;
            }
            if (index >= buffer.Length) continue;
            var last = Math.Min(written, buffer.Length - 1);
            for (int i = last; i > index; i--) buffer[i] = buffer[i - 1];
            buffer[index] = exit;
            if (written < buffer.Length) written++;
        }
        return WaterQueryStatus.Ready;
    }

    private static double ExitDistanceSquared(WaterExitDecl exit, Vector3 point)
    {
        var nearest = Vector3.Clamp(point, exit.Approach.Min, exit.Approach.Max);
        double x = (double)nearest.X - point.X, y = (double)nearest.Y - point.Y, z = (double)nearest.Z - point.Z;
        return x * x + y * y + z * z;
    }
    private static WaterQueryResult Invalid(string code) => new(WaterQueryStatus.Invalid, diagnosticCode: code);
}
