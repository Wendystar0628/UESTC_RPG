// C# adaptation of Mapbox earcut v3.0.2, without the optional z-order acceleration.
// Source: https://github.com/mapbox/earcut/tree/v3.0.2 ; ISC license in Earcut.LICENSE.
// Copyright (c) 2024, Mapbox. The linked-list, bridge and recovery algorithms retain upstream semantics.
using System.Numerics;

namespace Campus.Gen.Parts.Core;

internal static class Earcut
{
    private sealed class Node(int index, double x, double y)
    {
        public readonly int Index = index;
        public readonly double X = x, Y = y;
        public Node Prev = null!, Next = null!;
    }

    internal static int[] Triangulate(Vector2[] points, int[] starts)
    {
        Node outer = Ring(points, 0, starts.Length > 1 ? starts[1] : points.Length);
        var holes = new List<Node>();
        for (var i = 1; i < starts.Length; i++)
        {
            var node = Ring(points, starts[i], i + 1 < starts.Length ? starts[i + 1] : points.Length);
            var left = node; var sentinel = node;
            do { if (node.X < left.X || node.X == left.X && node.Y < left.Y) left = node; node = node.Next; } while (node != sentinel);
            holes.Add(left);
        }
        holes.Sort((a, b) => { var c = a.X.CompareTo(b.X); if (c == 0) c = a.Y.CompareTo(b.Y); return c == 0 ? a.Index.CompareTo(b.Index) : c; });
        foreach (var hole in holes)
        {
            var bridge = Bridge(hole, outer) ?? throw new ArgumentException("Cannot connect polygon hole.");
            var reverse = Split(bridge, hole);
            Filter(reverse, reverse.Next);
            outer = Filter(bridge, bridge.Next);
        }
        var triangles = new List<int>();
        Cut(outer, triangles, 0);
        return triangles.ToArray();
    }

    private static Node Ring(Vector2[] points, int start, int end)
    {
        Node? last = null;
        for (var i = start; i < end; i++)
        {
            var p = new Node(i, points[i].X, points[i].Y);
            if (last is null) { p.Prev = p; p.Next = p; }
            else { p.Next = last.Next; p.Prev = last; last.Next.Prev = p; last.Next = p; }
            last = p;
        }
        return last ?? throw new ArgumentException("Empty polygon ring.");
    }
    private static Node Filter(Node start, Node? end = null)
    {
        end ??= start;
        var p = start;
        bool again;
        do
        {
            again = false;
            if (Equal(p, p.Next) || Area(p.Prev, p, p.Next) == 0)
            {
                Remove(p); p = end = p.Prev;
                if (p == p.Next) break;
                again = true;
            }
            else p = p.Next;
        } while (again || p != end);
        return end;
    }
    private static void Cut(Node ear, List<int> triangles, int pass)
    {
        var stop = ear;
        while (ear.Prev != ear.Next)
        {
            var previous = ear.Prev; var next = ear.Next;
            if (IsEar(ear))
            {
                triangles.Add(previous.Index); triangles.Add(ear.Index); triangles.Add(next.Index);
                Remove(ear); ear = stop = next.Next; continue;
            }
            ear = next;
            if (ear != stop) continue;
            if (pass == 0) Cut(Filter(ear), triangles, 1);
            else if (pass == 1) Cut(Cure(Filter(ear), triangles), triangles, 2);
            else SplitCut(ear, triangles);
            break;
        }
    }
    private static bool IsEar(Node ear)
    {
        var a = ear.Prev; var b = ear; var c = ear.Next;
        if (Area(a, b, c) >= 0) return false;
        var p = c.Next;
        while (p != a)
        {
            if (!Equal(p, a) && InTriangle(a.X, a.Y, b.X, b.Y, c.X, c.Y, p.X, p.Y) && Area(p.Prev, p, p.Next) >= 0) return false;
            p = p.Next;
        }
        return true;
    }
    private static Node Cure(Node start, List<int> triangles)
    {
        var p = start;
        do
        {
            var a = p.Prev; var b = p.Next.Next;
            if (!Equal(a, b) && Intersects(a, p, p.Next, b) && Local(a, b) && Local(b, a))
            {
                triangles.Add(a.Index); triangles.Add(p.Index); triangles.Add(b.Index);
                Remove(p); Remove(p.Next); p = start = b;
            }
            p = p.Next;
        } while (p != start);
        return Filter(p);
    }
    private static void SplitCut(Node start, List<int> triangles)
    {
        var a = start;
        do
        {
            var b = a.Next.Next;
            while (b != a.Prev)
            {
                if (a.Index != b.Index && Diagonal(a, b))
                {
                    var c = Split(a, b);
                    Cut(Filter(a, a.Next), triangles, 0); Cut(Filter(c, c.Next), triangles, 0); return;
                }
                b = b.Next;
            }
            a = a.Next;
        } while (a != start);
        throw new ArgumentException("Polygon cannot be triangulated.");
    }
    private static Node? Bridge(Node hole, Node outer)
    {
        var p = outer; var hx = hole.X; var hy = hole.Y; var qx = double.NegativeInfinity;
        Node? m = null;
        do
        {
            if (hy <= p.Y && hy >= p.Next.Y && p.Next.Y != p.Y)
            {
                var x = p.X + (hy - p.Y) * (p.Next.X - p.X) / (p.Next.Y - p.Y);
                if (x <= hx && x > qx) { qx = x; m = p.X < p.Next.X ? p : p.Next; if (x == hx) return m; }
            }
            p = p.Next;
        } while (p != outer);
        if (m is null) return null;
        var stop = m; var mx = m.X; var my = m.Y; var tanMin = double.PositiveInfinity;
        p = m;
        do
        {
            if (hx >= p.X && p.X >= mx && hx != p.X && InTriangle(hy < my ? hx : qx, hy, mx, my, hy < my ? qx : hx, hy, p.X, p.Y))
            {
                var tan = Math.Abs(hy - p.Y) / (hx - p.X);
                if (Local(p, hole) && (tan < tanMin || tan == tanMin && (p.X > m.X || p.X == m.X && Area(m.Prev, m, p.Prev) < 0 && Area(p.Next, m, m.Next) < 0)))
                { m = p; tanMin = tan; }
            }
            p = p.Next;
        } while (p != stop);
        return m;
    }
    private static bool InTriangle(double ax, double ay, double bx, double by, double cx, double cy, double px, double py) =>
        (cx - px) * (ay - py) >= (ax - px) * (cy - py) && (ax - px) * (by - py) >= (bx - px) * (ay - py) && (bx - px) * (cy - py) >= (cx - px) * (by - py);
    private static double Area(Node p, Node q, Node r) => (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
    private static bool Equal(Node a, Node b) => a.X == b.X && a.Y == b.Y;
    private static bool OnSegment(Node p, Node q, Node r) => q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) && q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y);
    private static bool Intersects(Node a, Node b, Node c, Node d)
    {
        var o1 = Math.Sign(Area(a, b, c)); var o2 = Math.Sign(Area(a, b, d));
        var o3 = Math.Sign(Area(c, d, a)); var o4 = Math.Sign(Area(c, d, b));
        return o1 != o2 && o3 != o4 || o1 == 0 && OnSegment(a, c, b) || o2 == 0 && OnSegment(a, d, b) || o3 == 0 && OnSegment(c, a, d) || o4 == 0 && OnSegment(c, b, d);
    }
    private static bool Local(Node a, Node b) => Area(a.Prev, a, a.Next) < 0 ?
        Area(a, b, a.Next) >= 0 && Area(a, a.Prev, b) >= 0 : Area(a, b, a.Prev) < 0 || Area(a, a.Next, b) < 0;
    private static bool Diagonal(Node a, Node b)
    {
        if (a.Next.Index == b.Index || a.Prev.Index == b.Index) return false;
        var p = a;
        do
        {
            if (p.Index != a.Index && p.Next.Index != a.Index && p.Index != b.Index && p.Next.Index != b.Index && Intersects(p, p.Next, a, b)) return false;
            p = p.Next;
        } while (p != a);
        bool inside = false;
        double px = (a.X + b.X) * 0.5, py = (a.Y + b.Y) * 0.5;
        do
        {
            if ((p.Y > py) != (p.Next.Y > py) && p.Next.Y != p.Y && px < (p.Next.X - p.X) * (py - p.Y) / (p.Next.Y - p.Y) + p.X) inside = !inside;
            p = p.Next;
        } while (p != a);
        return Local(a, b) && Local(b, a) && inside && (Area(a.Prev, a, b.Prev) != 0 || Area(a, b.Prev, b) != 0) || Equal(a, b) && Area(a.Prev, a, a.Next) > 0 && Area(b.Prev, b, b.Next) > 0;
    }
    private static Node Split(Node a, Node b)
    {
        var a2 = new Node(a.Index, a.X, a.Y); var b2 = new Node(b.Index, b.X, b.Y);
        var an = a.Next; var bp = b.Prev;
        a.Next = b; b.Prev = a; a2.Next = an; an.Prev = a2; b2.Next = a2; a2.Prev = b2; bp.Next = b2; b2.Prev = bp;
        return b2;
    }
    private static void Remove(Node p) { p.Next.Prev = p.Prev; p.Prev.Next = p.Next; }
}
