using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Ground;

internal sealed partial class GroundMesher
{
    private void Cell(int i, int j)
    {
        var c00 = Lattice(i, j);
        var c10 = Lattice(i + 1, j);
        var c11 = Lattice(i + 1, j + 1);
        var c01 = Lattice(i, j + 1);
        // Anti-diagonal 10-01: triangles 00-10-01 and 10-11-01, each with the codes that differ from its first corner.
        Triangle(c00, c10, c01, _code[j * _f.S + i] ^ _code[j * _f.S + i + 1] | _code[j * _f.S + i] ^ _code[(j + 1) * _f.S + i]);
        Triangle(c10, c11, c01, _code[j * _f.S + i + 1] ^ _code[(j + 1) * _f.S + i + 1] | _code[j * _f.S + i + 1] ^ _code[(j + 1) * _f.S + i]);
    }

    private Vert Lattice(int i, int j)
    {
        var vals = new float[TileField.Count];
        Array.Copy(_f.V, (j * _f.S + i) * TileField.Count, vals, 0, TileField.Count);
        return new Vert { X = _f.X(i), Z = _f.Z(j), Vals = vals };
    }

    private void Triangle(Vert a, Vert b, Vert c, int varying) => Polygon([a, b, c], varying);

    private void Polygon(List<Vert> corners, int varying)
    {
        var start = new List<Vert>(corners.Count);
        foreach (var v in corners) start.Add(Copy(v, -1));
        var pieces = new List<List<Vert>> { start };
        for (var t = 0; t < _t.Length; t++)
        {
            if ((varying & _enabled & (1 << t)) == 0) continue;
            var next = new List<List<Vert>>(pieces.Count + 2);
            // Every piece is cut by every contour that crosses its triangle, even one that cannot change its surface:
            // sibling pieces must share every vertex on their common edges, or the seam opens a pinhole.
            foreach (var piece in pieces) Split(piece, t, next);
            pieces = next;
        }
        foreach (var piece in pieces) EmitPiece(piece);
    }

    private static Vert Copy(Vert v, int tag) => new() { X = v.X, Z = v.Z, Vals = v.Vals, Tag = tag };

    private void Split(List<Vert> poly, int t, List<List<Vert>> output)
    {
        var threshold = _t[t];
        var (field, iso) = threshold;
        var inside = 0;
        foreach (var v in poly) if (threshold.Contains(v.Vals[field])) inside++;
        if (inside == 0 || inside == poly.Count)
        {
            output.Add(poly);
            return;
        }
        var a = new List<Vert>();
        var b = new List<Vert>();
        for (var k = 0; k < poly.Count; k++)
        {
            var p = poly[k];
            var q = poly[(k + 1) % poly.Count];
            var pin = threshold.Contains(p.Vals[field]);
            var qin = threshold.Contains(q.Vals[field]);
            var target = pin ? a : b;
            target.Add(Copy(p, p.Tag));
            if (pin != qin)
            {
                var x = Crossing(p, q, field, iso);
                target.Add(Copy(x, t));
                (qin ? a : b).Add(Copy(x, p.Tag));
            }
        }
        // A contour that coincides with an earlier one cuts off an exactly empty sliver; keep the piece whole so its
        // edges keep the tag of the contour that really bounds it. Thin but real slivers are kept on both sides.
        var aa = Clean(a);
        var ba = Clean(b);
        if (aa <= 1e-14 || ba <= 1e-14)
        {
            output.Add(poly);
            return;
        }
        output.Add(a);
        output.Add(b);
    }

    /// <summary>Removes zero-length edges (keeping the later tag); returns the remaining absolute (doubled) area.</summary>
    private static double Clean(List<Vert> poly)
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
    private static bool HasCollinear(List<Vert> poly)
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

    private static double Area(List<Vert> poly)
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
    private static Vert Crossing(Vert p, Vert q, int field, float iso)
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
        return new Vert { X = x, Z = z, Vals = vals };
    }
}
