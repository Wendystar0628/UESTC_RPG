using Campus.Contracts;

namespace Campus.Gen.Zones.South.Framework;

/// <summary>
/// Concatenates bundles (position, normal, index, AO, UV) that share a material, in input order, up to 60,000 vertices per
/// bundle. Per-vertex window data (UV2, colour ids, flags, the spare byte) is carried along, so numbered panes of one material
/// merge into one bundle and keep their window IDs; bundles with custom channels, encoding-2 colours or LODs pass through untouched.
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
            if (!Mergeable(mesh)) { output.Add(mesh); continue; }
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

    private static bool Mergeable(GeometryBundle m) =>
        m.Ao is not null && m.Uv is not null && m.ColorIndices is null && m.DecalBaseColorIndices is null &&
        m.Custom0 is null && m.Custom1 is null && m.Custom2 is null && m.Lods is null;

    private static bool Same(GeometryBundle a, GeometryBundle b) =>
        a.Material == b.Material && a.Shader == b.Shader && a.Variant == b.Variant &&
        a.IgnoreOcclusionCulling == b.IgnoreOcclusionCulling && a.Reflect == b.Reflect &&
        (a.Uv2 is null) == (b.Uv2 is null) && (a.ColorIds is null) == (b.ColorIds is null) &&
        (a.Flags is null) == (b.Flags is null) && (a.Reserved is null) == (b.Reserved is null);

    private static GeometryBundle Concat(List<GeometryBundle> parts)
    {
        if (parts.Count == 1) return parts[0];
        var first = parts[0];
        var vertices = parts.Sum(p => p.Position.Length / 3);
        var position = new float[vertices * 3];
        var normal = new float[vertices * 3];
        var ao = new float[vertices];
        var uv = new float[vertices * 2];
        var uv2 = first.Uv2 is null ? null : new float[vertices * 2];
        var colorIds = first.ColorIds is null ? null : new byte[vertices];
        var flags = first.Flags is null ? null : new byte[vertices];
        var reserved = first.Reserved is null ? null : new byte[vertices];
        var index = new int[parts.Sum(p => p.Index.Length)];
        int v = 0, i = 0;
        foreach (var p in parts)
        {
            var n = p.Position.Length / 3;
            Array.Copy(p.Position, 0, position, v * 3, n * 3);
            Array.Copy(p.Normal, 0, normal, v * 3, n * 3);
            Array.Copy(p.Ao!, 0, ao, v, n);
            Array.Copy(p.Uv!, 0, uv, v * 2, n * 2);
            if (uv2 is not null) Array.Copy(p.Uv2!, 0, uv2, v * 2, n * 2);
            if (colorIds is not null) Array.Copy(p.ColorIds!, 0, colorIds, v, n);
            if (flags is not null) Array.Copy(p.Flags!, 0, flags, v, n);
            if (reserved is not null) Array.Copy(p.Reserved!, 0, reserved, v, n);
            for (var k = 0; k < p.Index.Length; k++) index[i + k] = p.Index[k] + v;
            v += n;
            i += p.Index.Length;
        }
        return first with
        {
            Position = position,
            Normal = normal,
            Index = index,
            Ao = ao,
            Uv = uv,
            Uv2 = uv2,
            ColorIds = colorIds,
            Flags = flags,
            Reserved = reserved,
        };
    }
}
