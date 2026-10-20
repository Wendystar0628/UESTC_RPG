using System.Numerics;
using Campus.Contracts;
using static Campus.Gen.Ground.GroundSurfaceProfile;
using static Campus.Gen.Ground.GroundContourPolygon;

namespace Campus.Gen.Ground;

/// <summary>Emits surface tops, contour faces and tile skirts; owns exact-query gradient memoization.</summary>
internal sealed class GroundGeometryEmitter
{
    private static readonly Vector3 Up = Vector3.UnitY;

    private readonly TileField _f;

    private readonly GroundLayout _layout;

    private readonly TerrainGrid _grid;

    private readonly GroundSurfaceSink _sink;

    private readonly GroundSourceContext _sourceContext;

    private readonly Dictionary<(float, float, int), Vector2> _gradients = new();

    private readonly GroundSurfaceProfile _surfaces;
    private readonly GroundTilePartition _partition;

    public GroundGeometryEmitter(TileField field, GroundSection section, GroundLayout layout,
        TerrainGrid grid, GroundSurfaceSink sink, GroundSurfaceProfile surfaces, GroundTilePartition partition)
    {
        _f = field;
        _layout = layout;
        _grid = grid;
        _sink = sink;
        _surfaces = surfaces;
        _partition = partition;
        _sourceContext = new(field.Sources, section, partition.FieldTolerance, layout.Paving);
    }

    public void Block(int i0, int j0, int size, int code)
    {
        var surface = _surfaces.Classify(code);
        if (surface == GroundSurface.Water || CoveredLawn(code, surface)) return;
        var ring = new List<Vector3>();
        void Walk(int i, int j)
        {
            if (_partition.Used[j * _f.S + i]) ring.Add(new(_f.X(i), _surfaces.LatticeHeight(surface, i, j), _f.Z(j)));
        }
        for (var k = 0; k < size; k++) Walk(i0 + k, j0);
        for (var k = 0; k < size; k++) Walk(i0 + size, j0 + k);
        for (var k = 0; k < size; k++) Walk(i0 + size - k, j0 + size);
        for (var k = 0; k < size; k++) Walk(i0, j0 + size - k);
        for (var k = 0; k < ring.Count; k++) Skirt(surface, ring[k], ring[(k + 1) % ring.Count]);
        if (ring.Count == 4)
        {
            // Corners in ring order 00, 10, 11, 01: the anti-diagonal 10-01, as the fine cells and the collision height field.
            Top(surface, ring[0], ring[1], ring[3], Up, Up, Up);
            Top(surface, ring[1], ring[2], ring[3], Up, Up, Up);
            return;
        }
        var cx = _f.X(i0) + size * _f.Cell * 0.5f;
        var cz = _f.Z(j0) + size * _f.Cell * 0.5f;
        var cy = 0.25f * (_surfaces.LatticeHeight(surface, i0, j0) + _surfaces.LatticeHeight(surface, i0 + size, j0) +
                          _surfaces.LatticeHeight(surface, i0, j0 + size) + _surfaces.LatticeHeight(surface, i0 + size, j0 + size));
        var centre = new Vector3(cx, cy, cz);
        for (var k = 0; k < ring.Count; k++) Top(surface, centre, ring[k], ring[(k + 1) % ring.Count], Up, Up, Up);
    }

    /// <summary>
    /// Tiles of different levels meet with different vertices (and, on terrain, different heights). Every top edge on
    /// the tile border hangs a short skirt that shades like the surface (normal up, same colour), so a level seam can
    /// never open a gap; the neighbour's skirt covers the view from the other side.
    /// </summary>
    private void Skirt(GroundSurface surface, Vector3 a, Vector3 b)
    {
        if (surface is GroundSurface.Water or GroundSurface.Bevel or GroundSurface.PitBevel) return;
        var x1 = _f.X(_f.N);
        var z1 = _f.Z(_f.N);
        Vector3 outward;
        if (a.X == _f.X0 && b.X == _f.X0) outward = -Vector3.UnitX;
        else if (a.X == x1 && b.X == x1) outward = Vector3.UnitX;
        else if (a.Z == _f.Z0 && b.Z == _f.Z0) outward = -Vector3.UnitZ;
        else if (a.Z == z1 && b.Z == z1) outward = Vector3.UnitZ;
        else return;
        var drop = new Vector3(0f, SkirtDepth, 0f);
        var color = GroundPalette.ColorOf(surface);
        _sink.Triangle(color, VertexFlag.NoShadow, a, b, b - drop, Up, Up, Up, outward, new(surface, GroundFaceRole.Skirt, _sourceContext));
        _sink.Triangle(color, VertexFlag.NoShadow, a, b - drop, a - drop, Up, Up, Up, outward, new(surface, GroundFaceRole.Skirt, _sourceContext));
    }

    private const float SkirtDepth = 0.3f;

    /// <summary>
    /// Emits one top-surface triangle with the D-180 shading rule: level-ground normals except on designed earth
    /// slopes (1:6 and steeper), which get the triangle's own flat normal on their own vertices.
    /// </summary>
    private void Top(GroundSurface surface, Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc)
    {
        if (!_grid.IsFlat && surface != GroundSurface.Bevel)
        {
            var n = Vector3.Cross(b - a, c - a);
            if (n.Y < 0f) n = -n;
            if (n.LengthSquared() > 1e-14f)
            {
                n = Vector3.Normalize(n);
                var slope = MathF.Sqrt(n.X * n.X + n.Z * n.Z) / MathF.Max(n.Y, 1e-6f);
                if (GroundShadingRule.For(surface, slope) == GroundShading.Faceted) na = nb = nc = n;
            }
        }
        _sink.Triangle(GroundPalette.ColorOf(surface), VertexFlag.NoShadow, a, b, c, na, nb, nc, Up,
            new(surface, GroundFaceRole.Top, _sourceContext));
    }

    public void EmitPiece(List<GroundContourVertex> poly)
    {
        var code = _surfaces.CodeOf(Mean(poly));
        var surface = _surfaces.Classify(code);
        if (CoveredLawn(code, surface)) return;
        if (surface != GroundSurface.Water)
        {
            var points = new Vector3[poly.Count];
            var normals = new Vector3[poly.Count];
            for (var k = 0; k < poly.Count; k++)
            {
                points[k] = Point(poly[k], surface);
                normals[k] = SurfaceNormal(surface, poly[k]);
            }
            for (var k = 0; k < poly.Count; k++) Skirt(surface, points[k], points[(k + 1) % poly.Count]);
            if (poly.Count == 3 || (_grid.IsFlat && !HasCollinear(poly)))
            {
                for (var k = 1; k + 1 < poly.Count; k++)
                    Top(surface, points[0], points[k], points[k + 1], normals[0], normals[k], normals[k + 1]);
            }
            else
            {
                // Fan from the centroid so that every outline edge is a triangle edge. A fan from a vertex drops the
                // degenerate triangles of collinear outline runs, which leaves T-junctions (pinholes) against the
                // neighbour's vertices, and on sloping ground makes vertical slivers.
                var mean = Mean(poly);
                var c = new GroundContourVertex { X = poly.Average(v => v.X), Z = poly.Average(v => v.Z), Vals = mean };
                var pc = Point(c, surface);
                var nc = SurfaceNormal(surface, c);
                for (var k = 0; k < poly.Count; k++)
                {
                    var next = (k + 1) % poly.Count;
                    Top(surface, pc, points[k], points[next], nc, normals[k], normals[next]);
                }
            }
        }
        var ccw = Area(poly) > 0;
        for (var k = 0; k < poly.Count; k++)
        {
            var u = poly[k];
            if (u.Tag < 0) continue;
            var w = poly[(k + 1) % poly.Count];
            // Outward edge normal in the xz plane (positive shoelace area: interior on the left of u -> w in x-z axes).
            var ex = (float)(w.X - u.X);
            var ez = (float)(w.Z - u.Z);
            var outward = ccw ? new Vector3(ez, 0f, -ex) : new Vector3(-ez, 0f, ex);
            if (outward.LengthSquared() < 1e-14f) continue;
            Face(u, w, u.Tag, code, surface, Vector3.Normalize(outward));
        }
    }

    /// <summary>Emits the vertical face along a contour edge when this side is higher than the other.</summary>
    private void Face(GroundContourVertex u, GroundContourVertex w, int t, int code, GroundSurface surface, Vector3 outward)
    {
        if (surface == GroundSurface.Water) return;
        var mid = new float[TileField.Count];
        for (var f = 0; f < mid.Length; f++) mid[f] = 0.5f * (u.Vals[f] + w.Vals[f]);
        var other = (_surfaces.CodeOf(mid) & ~(1 << t)) | (~code & (1 << t));
        var neighbour = _surfaces.Classify(other);
        var hu = _surfaces.HeightOf(surface, u.Vals);
        var hw = _surfaces.HeightOf(surface, w.Vals);
        var lu = _surfaces.HeightOf(neighbour, u.Vals);
        var lw = _surfaces.HeightOf(neighbour, w.Vals);
        if (hu - lu < 1e-4f || hw - lw < 1e-4f) return;
        var field = _surfaces.Thresholds[t].Field;
        // Outward from this (higher) side: up the field gradient when this side is inside (field below iso).
        var sign = (code & (1 << t)) != 0 ? 1f : -1f;
        var nu = FaceNormal(u, field, sign, outward);
        var nw = FaceNormal(w, field, sign, outward);
        // Water banks are stone revetment whatever lies above them; other steps take the upper surface's face colour.
        var color = neighbour == GroundSurface.Water ? GroundColor.Curb : GroundPalette.FaceColorOf(surface);
        var pu0 = new Vector3((float)u.X, lu, (float)u.Z);
        var pu1 = new Vector3((float)u.X, hu, (float)u.Z);
        var pw0 = new Vector3((float)w.X, lw, (float)w.Z);
        var pw1 = new Vector3((float)w.X, hw, (float)w.Z);
        _sink.Triangle(color, VertexFlag.NoShadow, pu0, pw0, pw1, nu, nw, nw, outward, new(surface, GroundFaceRole.Vertical, _sourceContext));
        _sink.Triangle(color, VertexFlag.NoShadow, pu0, pw1, pu1, nu, nw, nu, outward, new(surface, GroundFaceRole.Vertical, _sourceContext));
    }

    private Vector3 Point(GroundContourVertex v, GroundSurface surface) =>
        new((float)v.X, _surfaces.HeightOf(surface, v.Vals), (float)v.Z);

    private Vector3 SurfaceNormal(GroundSurface surface, GroundContourVertex v) => surface switch
    {
        GroundSurface.Bevel => BevelNormal(v),
        GroundSurface.PitBevel => PitBevelNormal(v),
        _ => Up
    };

    /// <summary>Normal of a pit frame's chamfer: tilted outward on the outer top edge, toward the soil on the inner one.</summary>
    private Vector3 PitBevelNormal(GroundContourVertex v)
    {
        var g = Gradient(v, TileField.Pit);
        if (g == Vector2.Zero) return Up;
        var inner = v.Vals[TileField.Core] < -v.Vals[TileField.Pit];
        var sign = inner ? -1f : 1f;
        return Vector3.Normalize(Up + new Vector3(sign * g.X, 0f, sign * g.Y));
    }

    private Vector3 BevelNormal(GroundContourVertex v)
    {
        var g = Gradient(v, TileField.A);
        if (g == Vector2.Zero) return Up;
        return Vector3.Normalize(Up + new Vector3(-g.X, 0f, -g.Y));
    }

    /// <summary>Smooth face normal from the field gradient; falls back to the edge normal where the gradient disagrees.</summary>
    private Vector3 FaceNormal(GroundContourVertex v, int field, float sign, Vector3 outward)
    {
        var g = Gradient(v, field) * sign;
        var n = new Vector3(g.X, 0f, g.Y);
        return Vector3.Dot(n, outward) > 0.5f ? n : outward;
    }

    /// <summary>Unit horizontal gradient of a field at the vertex, from exact point queries (seam consistent).</summary>
    private Vector2 Gradient(GroundContourVertex v, int field)
    {
        var x = (float)v.X;
        var z = (float)v.Z;
        if (_gradients.TryGetValue((x, z, field), out var cached)) return cached;
        const float h = 0.02f;
        var limit = _f.Limit;
        var p = new Vector2(x, z);
        var shapes = _layout.Near(p - new Vector2(limit + h), p + new Vector2(limit + h));
        var gx = Value(field, shapes, new(x + h, z), limit) - Value(field, shapes, new(x - h, z), limit);
        var gz = Value(field, shapes, new(x, z + h), limit) - Value(field, shapes, new(x, z - h), limit);
        var g = new Vector2(gx, gz);
        g = g.LengthSquared() > 1e-12f ? Vector2.Normalize(g) : Vector2.Zero;
        _gradients.Add((x, z, field), g);
        return g;
    }

    private static float Value(int field, List<GroundShape> shapes, Vector2 p, float limit)
    {
        float Of(SurfaceKind kind)
        {
            var best = limit;
            foreach (var shape in shapes)
                if (shape.Kind == kind) best = MathF.Min(best, Sdf.Shape(shape, p, limit));
            return best;
        }
        return field switch
        {
            TileField.W => Of(SurfaceKind.Water),
            TileField.A => MathF.Min(Of(SurfaceKind.Carriageway), Of(SurfaceKind.Parking)),
            TileField.Car => Of(SurfaceKind.Carriageway),
            TileField.P => MathF.Min(MathF.Min(MathF.Min(Of(SurfaceKind.Track), Of(SurfaceKind.Turf)),
                MathF.Min(Of(SurfaceKind.Court), Of(SurfaceKind.Plaza))), Of(SurfaceKind.Walk)),
            TileField.Track => Of(SurfaceKind.Track),
            TileField.Turf => Of(SurfaceKind.Turf),
            TileField.Court => Of(SurfaceKind.Court),
            TileField.Plaza => Of(SurfaceKind.Plaza),
            TileField.Building => Of(SurfaceKind.Building),
            // Core is Pit plus the frame thickness of the nearest pit: the same gradient.
            TileField.Pit or TileField.Core => Of(SurfaceKind.Pit),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown field.")
        };
    }
}
