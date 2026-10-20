using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Buildings.Rules;

namespace Campus.Gen.Buildings.Mesh;

/// <summary>
/// One vertex: position, normal, UV (metres along the facade, or window pane coordinates on glass) and occlusion.
/// Occlusion is 0 on open faces and grows on narrow recesses; the bundle stores visibility Ao = 1 - occlusion
/// (contract: missing Ao means 1, the toon threshold push is 0.3 × (1 - Ao)).
/// </summary>
public readonly record struct V(Vector3 P, Vector3 N, Vector2 Uv, float Ao = 0f);

/// <summary>
/// Per-face attributes written into the VF-A COLOR bytes and the VF-B UV2. A <see cref="WallPattern"/> on a wall face puts it
/// in the patterned wall class: UV2 is then (gate, K) (default (0, 0) = no pattern drawn), and only the main wall panels,
/// whose UV is metres along the wall and up, set gate 1.
/// </summary>
public readonly record struct Face(MaterialKind Kind, string Family, byte Flags = 0, byte Spare = 0, Vector2 Uv2 = default,
    WallPattern Pattern = WallPattern.None);

/// <summary>
/// Glass data a placed part surface takes over from the building (WLD-INTERIORS): its pane UVs in [0, 1) move
/// into window cell <see cref="Cell"/> and UV2 becomes the building's (BaseId, channel slot + 32 × TotalColumns).
/// </summary>
public readonly record struct PartGlass(Vector2 Cell, Vector2 Uv2);

/// <summary>Building-face and placed-glass mappings into the shared geometry stream.</summary>
public static class BuildingGeometry
{
    public static void Quad(this GeometrySink sink, in Face face, in V a, in V b, in V c, in V d) =>
        sink.Quad(Spec(face), Vertex(sink, face, a), Vertex(sink, face, b), Vertex(sink, face, c), Vertex(sink, face, d));

    public static void Tri(this GeometrySink sink, in Face face, in V a, in V b, in V c) =>
        sink.Tri(Spec(face), Vertex(sink, face, a), Vertex(sink, face, b), Vertex(sink, face, c));

    public static void Polygon(this GeometrySink sink, in Face face, IReadOnlyList<V> vertices, IReadOnlyList<int> indices)
    {
        var paint = face;
        sink.Polygon(Spec(paint), vertices.Select(v => Vertex(sink, paint, v)).ToArray(), indices);
    }

    public static void Append(this GeometrySink sink, GeometryBundle part, in Matrix4x4 at, PartGlass? glass = null, float? behind = null)
    {
        var indices = part.Index;
        if (behind is { } cut)
        {
            var visible = new List<int>();
            for (var t = 0; t < indices.Length; t += 3)
                if (part.Position[3 * indices[t] + 2] < cut || part.Position[3 * indices[t + 1] + 2] < cut || part.Position[3 * indices[t + 2] + 2] < cut)
                { visible.Add(indices[t]); visible.Add(indices[t + 1]); visible.Add(indices[t + 2]); }
            indices = [.. visible];
        }
        if (indices.Length == 0) return;
        var mesh = part with { Index = indices };
        if (glass is { } pane && part.Material.Spec.Kind == ToonKind.Glass)
        {
            var count = part.Position.Length / 3;
            var uv = new float[count * 2]; var uv2 = new float[count * 2];
            for (var i = 0; i < count; i++)
            {
                uv[2 * i] = (part.Uv?[2 * i] ?? 0) + pane.Cell.X;
                uv[2 * i + 1] = (part.Uv?[2 * i + 1] ?? 0) + pane.Cell.Y;
                uv2[2 * i] = pane.Uv2.X; uv2[2 * i + 1] = pane.Uv2.Y;
            }
            mesh = mesh with { Uv = uv, Uv2 = uv2 };
        }
        sink.Append(mesh, at);
    }

    private static ToonSpec Spec(in Face face) => Materials.Spec(face.Kind, Materials.DefaultFamily(face.Kind), face.Pattern);
    private static GeometryVertex Vertex(GeometrySink sink, in Face face, in V vertex) =>
        new(vertex.P, vertex.N, vertex.Uv, Spec(face).Kind == ToonKind.Glass || face.Pattern != WallPattern.None || face.Uv2 != Vector2.Zero ? face.Uv2 : null,
            1f - Math.Clamp(vertex.Ao, 0f, 1f), sink.ColorIndex(Materials.Lit(face.Family)), face.Flags, face.Spare);
}
