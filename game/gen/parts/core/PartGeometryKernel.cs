using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Parts.Core;

/// <summary>Closed, clockwise geometry in metres. All helpers are pure and safe to call on a bake worker.</summary>
public static partial class PartGeometryKernel
{
    /// <summary>24 vertices / 44 triangles with six flat faces and interpolating bevel strips.</summary>
    public static PartGeometry BeveledBox(float width, float height, float depth, float bevel, ToonSpec material)
    {
        GeometryMath.Positive(width, nameof(width)); GeometryMath.Positive(height, nameof(height)); GeometryMath.Positive(depth, nameof(depth));
        ValidateBevel(bevel, Math.Min(width, Math.Min(height, depth)) * 0.5f);
        if (bevel == 0) return Extrude(new PolygonRings([new(-width / 2, -depth / 2), new(width / 2, -depth / 2), new(width / 2, depth / 2), new(-width / 2, depth / 2)]), height, material);
        var writer = new PartShapeWriter(material);
        float[] half = [width * 0.5f, height * 0.5f, depth * 0.5f];
        var lookup = new int[3, 8];
        for (var axis = 0; axis < 3; axis++)
            for (var sign = 0; sign < 2; sign++)
            {
                var corners = new List<int>(4);
                for (var a = 0; a < 2; a++)
                    for (var b = 0; b < 2; b++)
                    {
                        int[] bits = new int[3]; bits[axis] = sign; bits[(axis + 1) % 3] = a; bits[(axis + 2) % 3] = b;
                        float[] p = new float[3]; float[] n = new float[3]; n[axis] = sign == 0 ? -1 : 1;
                        for (var k = 0; k < 3; k++) p[k] = (bits[k] == 0 ? -1 : 1) * (half[k] - (k == axis ? 0 : bevel));
                        var id = writer.Vertex(new(p[0], p[1] + half[1], p[2]), new(n[0], n[1], n[2]), new(a, b));
                        lookup[axis, bits[0] | bits[1] << 1 | bits[2] << 2] = id; corners.Add(id);
                    }
                var outward = Axis(axis, sign == 0 ? -1 : 1);
                writer.Quad(corners[0], corners[1], corners[3], corners[2], outward);
            }
        for (var a = 0; a < 3; a++)
            for (var b = a + 1; b < 3; b++)
                for (var sa = 0; sa < 2; sa++)
                    for (var sb = 0; sb < 2; sb++)
                    {
                        var c = 3 - a - b; var lo = sa << a | sb << b; var hi = lo | 1 << c;
                        var normal = Vector3.Add(Axis(a, sa == 0 ? -1 : 1), Axis(b, sb == 0 ? -1 : 1));
                        writer.Quad(lookup[a, lo], lookup[a, hi], lookup[b, hi], lookup[b, lo], normal);
                    }
        for (var corner = 0; corner < 8; corner++)
            writer.Triangle(lookup[0, corner], lookup[1, corner], lookup[2, corner], new((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
        return writer.Finish();
    }

    /// <summary>Extrudes a footprint in X/Z from baseY; concave rings and disjoint holes are supported.</summary>
    public static PartGeometry Extrude(PolygonRings footprint, float height, ToonSpec material, float baseY = 0) =>
        ExtrudePlane(footprint, height, material, new(0, baseY, 0), Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY);

    /// <summary>Closed wall from X=-width/2 to width/2, Y=0 to height. Openings use local (X,Y) rings.</summary>
    public static PartGeometry WallPanel(float width, float height, float depth, IReadOnlyList<IReadOnlyList<Vector2>> openings, ToonSpec material)
    {
        GeometryMath.Positive(width, nameof(width)); GeometryMath.Positive(height, nameof(height));
        ArgumentNullException.ThrowIfNull(openings);
        return WallPanel(new PolygonRings([new(-width / 2, 0), new(width / 2, 0), new(width / 2, height), new(-width / 2, height)], openings), depth, material);
    }

    /// <summary>Custom wall outline in local X/Y; a door touching the bottom is a notch in Outer, not a touching hole.</summary>
    public static PartGeometry WallPanel(PolygonRings face, float depth, ToonSpec material) =>
        ExtrudePlane(face, depth, material, new(0, 0, -depth / 2), Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);

    /// <summary>Convex, hole-free footprint with single-band chamfers on top/bottom edges. Bevels that collapse the inset are rejected.</summary>
    public static PartGeometry BeveledExtrude(IReadOnlyList<Vector2> footprint, float height, float bevel, ToonSpec material, float baseY = 0)
    {
        GeometryMath.Positive(height, nameof(height)); ValidateBevel(bevel, height * 0.5f);
        if (bevel == 0) return Extrude(new(footprint), height, material, baseY);
        var polygon = PolygonTriangulator.Triangulate(new(footprint));
        var ring = polygon.Vertices;
        for (var i = 0; i < ring.Length; i++)
        {
            var p = ring[(i + ring.Length - 1) % ring.Length]; var c = ring[i]; var n = ring[(i + 1) % ring.Length];
            if (PlanGeometry.Cross(p, c, n) <= 0) throw new ArgumentException("Beveled extrusion currently requires a convex footprint.", nameof(footprint));
        }
        var inset = PlanGeometry.MitreInset(ring, bevel);
        for (var i = 0; i < ring.Length; i++)
            for (var j = 0; j < ring.Length; j++)
                if (PlanGeometry.Cross(ring[i], ring[(i + 1) % ring.Length], inset[j]) <= 0)
                    throw new ArgumentException("Bevel is too large for the footprint.", nameof(bevel));
        var cap = PolygonTriangulator.Triangulate(new(inset));
        // A valid inset must keep correspondence; edge reversals mean the bevel passed the medial axis.
        for (var i = 0; i < ring.Length; i++)
        {
            var j = (i + 1) % ring.Length;
            if (((double)inset[j].X - inset[i].X) * (ring[j].X - ring[i].X) + ((double)inset[j].Y - inset[i].Y) * (ring[j].Y - ring[i].Y) <= 0)
                throw new ArgumentException("Bevel collapses an edge.", nameof(bevel));
        }
        var writer = new PartShapeWriter(material);
        AddCap(writer, cap, new(0, baseY, 0), Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY, 0, -Vector3.UnitY);
        AddCap(writer, cap, new(0, baseY, 0), Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY, height, Vector3.UnitY);
        for (var i = 0; i < ring.Length; i++)
        {
            var j = (i + 1) % ring.Length; var edge = PlanGeometry.RightUnitNormal(ring[i], ring[j]); var normal = new Vector3(edge.X, 0, edge.Y);
            Vector3 P(Vector2 p, float y) => new(p.X, baseY + y, p.Y);
            AddStrip(writer, P(inset[i], 0), P(inset[j], 0), P(ring[j], bevel), P(ring[i], bevel), -Vector3.UnitY, normal, Vector3.Subtract(normal, Vector3.UnitY));
            AddStrip(writer, P(ring[i], bevel), P(ring[j], bevel), P(ring[j], height - bevel), P(ring[i], height - bevel), normal, normal, normal);
            AddStrip(writer, P(ring[i], height - bevel), P(ring[j], height - bevel), P(inset[j], height), P(inset[i], height), normal, Vector3.UnitY, Vector3.Add(normal, Vector3.UnitY));
        }
        return writer.Finish();
    }

    private static PartGeometry ExtrudePlane(PolygonRings rings, float depth, ToonSpec material, Vector3 origin, Vector3 u, Vector3 v, Vector3 axis)
    {
        GeometryMath.Positive(depth, nameof(depth));
        if (!VectorMath.Finite(origin)) throw new ArgumentException("Coordinates must be finite.", nameof(origin));
        var polygon = PolygonTriangulator.Triangulate(rings); var writer = new PartShapeWriter(material);
        AddCap(writer, polygon, origin, u, v, axis, 0, Vector3.Multiply(axis, -1));
        AddCap(writer, polygon, origin, u, v, axis, depth, axis);
        for (var r = 0; r < polygon.RingStarts.Length; r++)
        {
            var start = polygon.RingStarts[r]; var end = r + 1 < polygon.RingStarts.Length ? polygon.RingStarts[r + 1] : polygon.Vertices.Length;
            for (var i = start; i < end; i++)
            {
                var a = polygon.Vertices[i]; var b = polygon.Vertices[i + 1 < end ? i + 1 : start];
                var edge = PlanGeometry.RightUnitNormal(a, b); var normal = Vector3.Add(Vector3.Multiply(u, edge.X), Vector3.Multiply(v, edge.Y));
                var p0 = GeometryMath.Point(a, origin, u, v, axis, 0); var p1 = GeometryMath.Point(b, origin, u, v, axis, 0);
                var p2 = GeometryMath.Point(b, origin, u, v, axis, depth); var p3 = GeometryMath.Point(a, origin, u, v, axis, depth);
                // OpeningAo remains the authored occlusion strength; only reveals reduce visibility.
                var visibility = r == 0 ? 1f : 1f - PartStyleDefaults.OpeningAo;
                AddStrip(writer, p0, p1, p2, p3, normal, normal, normal, visibility);
            }
        }
        return writer.Finish();
    }
    private static void AddCap(PartShapeWriter writer, TriangulatedPolygon polygon, Vector3 origin, Vector3 u, Vector3 v, Vector3 axis, float depth, Vector3 normal)
    {
        var vertices = new int[polygon.Vertices.Length];
        for (var i = 0; i < vertices.Length; i++) vertices[i] = writer.Vertex(GeometryMath.Point(polygon.Vertices[i], origin, u, v, axis, depth), normal, polygon.Vertices[i]);
        for (var i = 0; i < polygon.Indices.Length; i += 3) writer.Triangle(vertices[polygon.Indices[i]], vertices[polygon.Indices[i + 1]], vertices[polygon.Indices[i + 2]], normal);
    }
    private static void AddStrip(PartShapeWriter writer, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 lowerNormal, Vector3 upperNormal, Vector3 outward, float visibility = 1)
    {
        var length = (float)Math.Sqrt(VectorMath.Dot(Vector3.Subtract(b, a), Vector3.Subtract(b, a)));
        var height = (float)Math.Sqrt(VectorMath.Dot(Vector3.Subtract(d, a), Vector3.Subtract(d, a)));
        writer.Quad(writer.Vertex(a, lowerNormal, Vector2.Zero, visibility), writer.Vertex(b, lowerNormal, new(length, 0), visibility),
            writer.Vertex(c, upperNormal, new(length, height), visibility), writer.Vertex(d, upperNormal, new(0, height), visibility), outward);
    }
    private static Vector3 Axis(int axis, float sign) => axis switch { 0 => new(sign, 0, 0), 1 => new(0, sign, 0), 2 => new(0, 0, sign), _ => throw new ArgumentOutOfRangeException(nameof(axis)) };
    private static void ValidateBevel(float bevel, float limit)
    {
        if (!float.IsFinite(bevel) || bevel < 0 || bevel >= limit) throw new ArgumentOutOfRangeException(nameof(bevel), "Bevel must be nonnegative and less than half the smallest dimension.");
    }
}
