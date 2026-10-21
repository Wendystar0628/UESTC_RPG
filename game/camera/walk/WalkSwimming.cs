using Campus.Contracts.Diagnostics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera.Walk;

/// <summary>Water readiness, swimming hysteresis and semantic movement-region checks.</summary>
internal sealed class WalkSwimming(WalkBodyCollision collisionBody, Action notReady, Action requestExit)
{
    private readonly Action _notReady = notReady;
    private readonly Action _requestExit = requestExit;
    private IWaterTraversalQuery? _water;
    private bool _swimming;
    public bool Active => _swimming;
    public void ResolveService() => _water ??= KernelServices.Get<IWaterTraversalQuery>();
    public void Reset() => _swimming = false;

    public bool Update(WaterQueryResult result, N.Vector3 feet)
    {
        bool swimming = result.Sample is { } sample && WalkMath.SwimmingAt(sample, feet.Y, _swimming);
        bool changed = swimming != _swimming;
        if (changed) _swimming = swimming;
        return changed;
    }

    public bool ReadWater(N.Vector3 point, out WaterQueryResult result)
    {
        result = _water!.Sample(new(point.X, point.Z));
        switch (result.Status)
        {
            case WaterQueryStatus.Ready:
            case WaterQueryStatus.NoWater:
                return true;
            case WaterQueryStatus.NotReady:
                _notReady();
                return false;
            case WaterQueryStatus.Invalid:
                KernelServices.Get<IDiagnosticLog>().Error("", $"invalid water query: {result.DiagnosticCode}");
                _requestExit();
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(result));
        }
    }

    public Vector3 LimitRegions(Vector3 motion, N.Vector3 feet, IMapContext map, ICollisionWorld collision)
    {
        if (RegionAllowsMotion(motion, feet, map, collision)) return motion;
        var alongZ = new Vector3(0, 0, motion.Z);
        if (RegionAllowsMotion(alongZ, feet, map, collision)) return alongZ;
        var alongX = new Vector3(motion.X, 0, 0);
        return RegionAllowsMotion(alongX, feet, map, collision) ? alongX : Vector3.Zero;
    }

    private bool RegionAllowsMotion(Vector3 motion, N.Vector3 feet, IMapContext map, ICollisionWorld collision)
    {
        float radius = collisionBody.Radius;
        int count = Math.Max(1, (int)MathF.Ceiling(motion.Length() / radius));
        for (int i = 1; i <= count; i++)
        {
            var center = feet + (motion * ((float)i / count)).ToNumerics();
            if (!RegionAllowsPoint(center, map, collision)
                || !RegionAllowsPoint(center + N.Vector3.UnitX * radius, map, collision)
                || !RegionAllowsPoint(center - N.Vector3.UnitX * radius, map, collision)
                || !RegionAllowsPoint(center + N.Vector3.UnitZ * radius, map, collision)
                || !RegionAllowsPoint(center - N.Vector3.UnitZ * radius, map, collision)) return false;
        }
        return true;
    }

    private bool RegionAllowsPoint(N.Vector3 point, IMapContext map, ICollisionWorld collision)
    {
        if (!ReadWater(point, out var water)) return false;
        var spatial = map!.World.Spatial;
        var classification = spatial.WalkClassAt(point.X, point.Z);
        if (classification != WalkClass.Water) return WalkMath.CanWalk(classification);
        if (water.Sample is { CanSwim: true }) return true;
        if (!collision!.Ready(point, collisionBody.Radius)) return false;
        // A dry bridge is allowed only when the native floor, rather than airborne feet, is above water.
        var support = collision.SupportY(point.X, point.Z, point.Y + WalkRules.StepHeight);
        return support is { } hit && float.IsFinite(hit.Y)
            && !WalkSafetyQueries.InWater(spatial, point with { Y = hit.Y });
    }
}
