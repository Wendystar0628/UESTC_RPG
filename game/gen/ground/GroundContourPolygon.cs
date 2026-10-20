using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>Contour polygon field interpolation, canonical crossings and outline measurements.</summary>
internal static class GroundContourPolygon
{
    internal static GroundContourVertex Copy(GroundContourVertex v, int tag) => new() { X = v.X, Z = v.Z, Vals = v.Vals, Tag = tag };

    /// <summary>Removes zero-length edges (keeping the later tag); returns the remaining absolute (doubled) area.</summary>
    internal static double Clean(List<GroundContourVertex> poly)
    {
        for (var k = poly.Count - 1; k >= 0 && poly.Count > 1; k--)
        {
            var next = poly[(k + 1) % poly.Count];
            if (poly[k].X == next.X && poly[k].Z == next.Z)
            {
                next.Tag = next.Tag >= 0 ? next.Tag : poly[k].Tag;
                poly.RemoveAt(k);
            }
        }
        if (poly.Count < 3) return 0d;
        return Math.Abs(Area(poly));
    }

    /// <summary>True when three consecutive outline vertices are collinear (a lattice point or crossing mid-edge).</summary>
    internal static bool HasCollinear(List<GroundContourVertex> poly)
    {
        for (var k = 0; k < poly.Count; k++)
        {
            var a = poly[(k + poly.Count - 1) % poly.Count];
            var b = poly[k];
            var c = poly[(k + 1) % poly.Count];
            var cross = (b.X - a.X) * (c.Z - b.Z) - (b.Z - a.Z) * (c.X - b.X);
            if (Math.Abs(cross) < 1e-12) return true;
        }
        return false;
    }

    internal static double Area(List<GroundContourVertex> poly)
    {
        double area = 0;
        for (var k = 0; k < poly.Count; k++)
        {
            var p = poly[k];
            var q = poly[(k + 1) % poly.Count];
            area += p.X * q.Z - q.X * p.Z;
        }
        return area;
    }

    /// <summary>
    /// Crossing on edge pq, computed in a canonical order so both triangles sharing the edge agree exactly.
    /// Fields that coincide with the cut field on this edge (A = Car where no parking is near, P = Plaza where only a
    /// plaza is near) are set to the same exact value, so a later cut on them produces an exactly empty sliver.
    /// </summary>
    internal static GroundContourVertex Crossing(GroundContourVertex p, GroundContourVertex q, int field, float iso)
    {
        if (q.X < p.X || (q.X == p.X && q.Z < p.Z)) (p, q) = (q, p);
        var t = (iso - (double)p.Vals[field]) / ((double)q.Vals[field] - p.Vals[field]);
        if (t <= 0d) t = 0d;
        if (t >= 1d) t = 1d;
        var vals = new float[TileField.Count];
        for (var f = 0; f < vals.Length; f++)
        {
            if (p.Vals[f] == p.Vals[field] && q.Vals[f] == q.Vals[field]) vals[f] = iso;
            else vals[f] = t == 0d ? p.Vals[f] : t == 1d ? q.Vals[f] : (float)(p.Vals[f] + t * ((double)q.Vals[f] - p.Vals[f]));
        }
        vals[field] = iso;
        var x = t == 0d ? p.X : t == 1d ? q.X : p.X + t * (q.X - p.X);
        var z = t == 0d ? p.Z : t == 1d ? q.Z : p.Z + t * (q.Z - p.Z);
        return new GroundContourVertex { X = x, Z = z, Vals = vals };
    }

    // ---------------------------------------------------------------- emission

    internal static float[] Mean(List<GroundContourVertex> poly)
    {
        var mean = new float[TileField.Count];
        foreach (var v in poly)
            for (var f = 0; f < mean.Length; f++) mean[f] += v.Vals[f];
        for (var f = 0; f < mean.Length; f++) mean[f] /= poly.Count;
        return mean;
    }
}
