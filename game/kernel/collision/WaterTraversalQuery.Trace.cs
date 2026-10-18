using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Collision;

public sealed partial class WaterTraversalQuery
{
    // Polygon/height boundaries are exact cuts. Only the varying terrain bed needs short probes and refinement.
    private const float BedProbeStep = .25f;

    public WaterQueryResult Trace(Vector3 from, Vector3 to, NavSpace? space = null)
    {
        if (!VectorMath.Finite(from) || !VectorMath.Finite(to) || !VectorMath.Finite(to - from))
            return Invalid("water.trace.nonfinite");
        var status = EnsureCatalog();
        if (status != WaterQueryStatus.Ready) return new(status, diagnosticCode: "water.catalog.unavailable");
        try { return TraceCore(from, to, space); }
        catch (ArgumentException) { return Invalid("water.trace.invalid"); }
        catch (InvalidOperationException) { return Invalid("water.trace.invalid"); }
    }

    private WaterQueryResult TraceCore(Vector3 from, Vector3 to, NavSpace? space)
    {
        var initial = SampleCore(from, space);
        if (initial.Status != WaterQueryStatus.NoWater) return TraceResult(initial, 0);
        if (from == to) return initial;
        var a = new Vector2(from.X, from.Z); var b = new Vector2(to.X, to.Z);
        int count = 0;
        _cuts[count++] = 0; _cuts[count++] = 1;
        foreach (var volume in _catalog!.Volumes)
        {
            if (!WaterTraversalCatalog.SameSpace(space, volume.Space)) continue;
            for (int polygonIndex = 0; polygonIndex < volume.Polygons.Count; polygonIndex++)
            {
                var polygon = volume.Polygons[polygonIndex];
                RingCuts(polygon.Outer);
                for (int holeIndex = 0; holeIndex < polygon.Holes.Count; holeIndex++) RingCuts(polygon.Holes[holeIndex]);
            }
            HeightCut(volume.MinY); HeightCut(volume.MaxY); HeightCut(volume.PlaneY);
            HeightCut(_context!.World.Terrain.WaterY(volume.Water));
        }
        Array.Sort(_cuts, 0, count);
        var length = Vector3.Distance(from, to);
        if (!float.IsFinite(length)) return Invalid("water.trace.length");
        for (int i = 1; i < count; i++)
        {
            float start = _cuts[i - 1], end = _cuts[i];
            if (end <= start) continue;
            double needed = Math.Ceiling((double)length * (end - start) / BedProbeStep);
            if (needed > int.MaxValue) return Invalid("water.trace.length");
            int probes = Math.Max(1, (int)needed);
            float previous = start;
            for (int step = 0; step < probes; step++)
            {
                // Always sample each interval's interior: a hole boundary itself is excluded from water.
                float middle = start + (end - start) * ((step + .5f) / probes);
                var result = SampleCore(Vector3.Lerp(from, to, middle), space);
                if (result.Status != WaterQueryStatus.NoWater) return Refine(previous, middle, result);
                float next = start + (end - start) * ((step + 1f) / probes);
                result = SampleCore(Vector3.Lerp(from, to, next), space);
                if (result.Status != WaterQueryStatus.NoWater) return Refine(middle, next, result);
                previous = next;
            }
        }
        return new(WaterQueryStatus.NoWater);

        void RingCuts(IReadOnlyList<Vector2> ring)
        {
            for (int edge = 0; edge < ring.Count; edge++)
                if (PlanLines.SegmentHit(a, b, ring[edge], ring[(edge + 1) % ring.Count]) is { } intersection)
                    _cuts[count++] = intersection.T;
        }
        void HeightCut(float? height)
        {
            if (height is not { } value || !float.IsFinite(value) || to.Y == from.Y) return;
            float t = (value - from.Y) / (to.Y - from.Y);
            if (t > 0 && t < 1) _cuts[count++] = t;
        }
        WaterQueryResult Refine(float dry, float wet, WaterQueryResult result)
        {
            if (result.Status != WaterQueryStatus.Ready) return result;
            while ((wet - dry) * length > TerrainSampling.Centimetre)
            {
                float mid = dry + (wet - dry) * .5f;
                if (mid == dry || mid == wet) break;
                var check = SampleCore(Vector3.Lerp(from, to, mid), space);
                switch (check.Status)
                {
                    case WaterQueryStatus.NoWater: dry = mid; break;
                    case WaterQueryStatus.Ready: wet = mid; result = check; break;
                    case WaterQueryStatus.NotReady:
                    case WaterQueryStatus.Invalid: return check;
                    default: throw new InvalidOperationException("Unknown water query status.");
                }
            }
            return TraceResult(result, wet);
        }
    }

    private static WaterQueryResult TraceResult(WaterQueryResult result, float t) => result.Status switch
    {
        WaterQueryStatus.Ready => new(WaterQueryStatus.Ready, result.Sample, t),
        WaterQueryStatus.NoWater or WaterQueryStatus.NotReady or WaterQueryStatus.Invalid => result,
        _ => throw new InvalidOperationException("Unknown water query status.")
    };
}
