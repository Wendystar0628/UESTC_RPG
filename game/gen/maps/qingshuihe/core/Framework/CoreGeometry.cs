using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Maps.Qingshuihe.Core.Framework;

/// <summary>
/// Material classes of the core zone. Colours vary per vertex (palette IDs); classes stay few so a piece costs a
/// handful of draw calls (ENG-PERF). The non-colour fields of each class live in <see cref="CoreStyle"/>.
/// </summary>
public enum CoreMaterial : byte
{
    /// <summary>Granite plinths, stone tiers and steps' companions: bevel lift, mottle.</summary>
    Stone,
    /// <summary>Flush inlays in paving (bands, emblem, hour marks): decal, no shadow, no outline.</summary>
    Inlay,
    /// <summary>Poles, rings, rods and bronze: rim and anisotropic band, fine line.</summary>
    Metal,
    /// <summary>Thin cloth (flags): translucent, fine line.</summary>
    Cloth,
    /// <summary>Building walls (library shell and later blocks): bevel lift, building line.</summary>
    Wall,
    /// <summary>Copings, string courses, sills and plinth bands: bevel lift, fine line.</summary>
    Trim,
    /// <summary>Flat roofs and terrace decks: mottle, high wet response.</summary>
    Roof,
    /// <summary>Sheet-metal roofs and eaves: rim and anisotropic band, building line.</summary>
    Sheet,
    /// <summary>Lawn on built terraces: the ground lawn's response (WP-15), no outline on the turf.</summary>
    Lawn,
    /// <summary>Clipped hedges: smooth normals, soft steps, the tree line class.</summary>
    Foliage,
}

/// <summary>How one face is painted: material class, colour family (palette name without .lit/.shade) and VF-A flags.</summary>
public readonly record struct Paint(CoreMaterial Material, string Family, byte Flags = 0);

/// <summary>Core paint and catalogue-part mapping into the shared ordinary geometry stream.</summary>
public static class CoreGeometry
{
    public static void Tri(this GeometrySink mesh, in Paint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 normal) =>
        mesh.Tri(paint, a, b, c, normal, normal, normal);

    public static void Tri(this GeometrySink mesh, in Paint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc)
    {
        if (Vector3.Cross(b - a, c - a).LengthSquared() <= 1e-14f) return;
        var spec = CoreStyle.Spec(paint.Material, CoreStyle.DefaultFamily(paint.Material));
        var colour = mesh.ColorIndex(new(paint.Family + ".lit"));
        mesh.Tri(spec, new(a, na, ColorIndex: colour, Flags: paint.Flags),
            new(b, nb, ColorIndex: colour, Flags: paint.Flags), new(c, nc, ColorIndex: colour, Flags: paint.Flags));
    }

    public static void Quad(this GeometrySink mesh, in Paint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal) =>
        mesh.Quad(paint, a, b, c, d, normal, normal, normal, normal);

    public static void Quad(this GeometrySink mesh, in Paint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd)
    {
        mesh.Tri(paint, a, b, c, na, nb, nc);
        mesh.Tri(paint, a, c, d, na, nc, nd);
    }

    public static void Add(this GeometrySink mesh, in Paint paint, PartGeometry geometry, Matrix4x4 at)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        foreach (var surface in geometry.Meshes)
            for (var t = 0; t < surface.Index.Length; t += 3)
            {
                var (a, na) = Point(surface, surface.Index[t], at);
                var (b, nb) = Point(surface, surface.Index[t + 1], at);
                var (c, nc) = Point(surface, surface.Index[t + 2], at);
                mesh.Tri(paint, a, b, c, na, nb, nc);
            }
    }

    public static void Add(this GeometrySink mesh, PartGeometry geometry, Matrix4x4 at, Func<ToonSpec, Paint> paintOf)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(paintOf);
        foreach (var surface in geometry.Meshes)
        {
            var paint = paintOf(surface.Material.Spec);
            for (var t = 0; t < surface.Index.Length; t += 3)
            {
                var flags = (byte)(paint.Flags | ((surface.Flags?[surface.Index[t]] ?? 0) & (VertexFlag.Decal | VertexFlag.NoShadow)));
                var (a, na) = Point(surface, surface.Index[t], at);
                var (b, nb) = Point(surface, surface.Index[t + 1], at);
                var (c, nc) = Point(surface, surface.Index[t + 2], at);
                mesh.Tri(paint with { Flags = flags }, a, b, c, na, nb, nc);
            }
        }
    }

    private static (Vector3 P, Vector3 N) Point(GeometryBundle surface, int i, Matrix4x4 at) =>
        (Vector3.Transform(new(surface.Position[3 * i], surface.Position[3 * i + 1], surface.Position[3 * i + 2]), at),
         Vector3.TransformNormal(new(surface.Normal[3 * i], surface.Normal[3 * i + 1], surface.Normal[3 * i + 2]), at));
}
