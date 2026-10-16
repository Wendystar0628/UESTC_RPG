using Campus.Contracts;

namespace Campus.Gen.Zones.CenterEast.Framework;

/// <summary>
/// Concatenates plain bundles (position, normal, index, AO, UV) that share a material, in input order, up to 60,000
/// vertices per bundle. Bundles carrying window IDs, flags, custom channels or LODs pass through untouched.
/// </summary>
internal static class MeshMerge
{
    private const int MaxVertices = 60000;

    public static IReadOnlyList<GeometryBundle> Merge(IEnumerable<GeometryBundle> meshes)
    {
        var output = new List<GeometryBundle>();
        var open = new List<(GeometryBundle Key, List<GeometryBundle> Parts, int Vertices)>();
        foreach (var mesh in meshes)
        {
            if (!Plain(mesh)) { output.Add(mesh); continue; }
            var vertices = mesh.Position.Length / 3;
            var index = open.FindIndex(o => Same(o.Key, mesh));
            if (index >= 0 && open[index].Vertices + vertices > MaxVertices)
            {
                output.Add(Concat(open[index].Parts));
                open.RemoveAt(index);
                index = -1;
            }
            if (index < 0) { open.Add((mesh, [mesh], vertices)); continue; }
            var slot = open[index];
            slot.Parts.Add(mesh);
            open[index] = (slot.Key, slot.Parts, slot.Vertices + vertices);
        }
        foreach (var slot in open) output.Add(Concat(slot.Parts));
        return output;
    }

    private static bool Plain(GeometryBundle m) =>
        m.Ao is not null && m.Uv is not null && m.Uv2 is null && m.ColorIds is null && m.Flags is null && m.Reserved is null &&
        m.Custom0 is null && m.Custom1 is null && m.Custom2 is null && m.Lods is null;

    private static bool Same(GeometryBundle a, GeometryBundle b) =>
        a.Material == b.Material && a.Shader == b.Shader && a.Variant == b.Variant &&
        a.IgnoreOcclusionCulling == b.IgnoreOcclusionCulling && a.Reflect == b.Reflect;

    private static GeometryBundle Concat(List<GeometryBundle> parts)
    {
        if (parts.Count == 1) return parts[0];
        var vertices = parts.Sum(p => p.Position.Length / 3);
        var position = new float[vertices * 3];
        var normal = new float[vertices * 3];
        var ao = new float[vertices];
        var uv = new float[vertices * 2];
        var index = new int[parts.Sum(p => p.Index.Length)];
        int v = 0, i = 0;
        foreach (var p in parts)
        {
            var n = p.Position.Length / 3;
            Array.Copy(p.Position, 0, position, v * 3, n * 3);
            Array.Copy(p.Normal, 0, normal, v * 3, n * 3);
            Array.Copy(p.Ao!, 0, ao, v, n);
            Array.Copy(p.Uv!, 0, uv, v * 2, n * 2);
            for (var k = 0; k < p.Index.Length; k++) index[i + k] = p.Index[k] + v;
            v += n;
            i += p.Index.Length;
        }
        var first = parts[0];
        return first with { Position = position, Normal = normal, Index = index, Ao = ao, Uv = uv };
    }
}
