using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Interior;

/// <summary>Caller-owned material class, per-vertex colour and flags for shared interior geometry.</summary>
public readonly record struct InteriorPaint(ToonSpec Material, string Family, byte Flags = VertexFlag.Interior);

internal static class InteriorPaintGeometry
{
    public static void Tri(this GeometrySink mesh, in InteriorPaint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
        => mesh.Tri(paint, a, b, c, normal, normal, normal);
    public static void Tri(this GeometrySink mesh, in InteriorPaint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc)
    {
        if (Vector3.Cross(b - a, c - a).LengthSquared() <= 1e-14f) return;
        var colour = mesh.ColorIndex(new(paint.Family + ".lit"));
        mesh.Tri(paint.Material, new(a, na, ColorIndex: colour, Flags: paint.Flags),
            new(b, nb, ColorIndex: colour, Flags: paint.Flags), new(c, nc, ColorIndex: colour, Flags: paint.Flags));
    }
    public static void Quad(this GeometrySink mesh, in InteriorPaint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    { mesh.Tri(paint, a, b, c, normal); mesh.Tri(paint, a, c, d, normal); }
    public static void Add(this GeometrySink mesh, in InteriorPaint paint, PartGeometry geometry, Matrix4x4 at)
    {
        foreach (var surface in geometry.Meshes)
        {
            Vector3 P(int i) => Vector3.Transform(new(surface.Position[3*i],surface.Position[3*i+1],surface.Position[3*i+2]),at);
            Vector3 N(int i) => Vector3.TransformNormal(new(surface.Normal[3*i],surface.Normal[3*i+1],surface.Normal[3*i+2]),at);
            for (var t=0; t<surface.Index.Length; t+=3)
            {
                int a=surface.Index[t], b=surface.Index[t+1], c=surface.Index[t+2];
                mesh.Tri(paint, P(a), P(b), P(c), N(a), N(b), N(c));
            }
        }
    }
}
