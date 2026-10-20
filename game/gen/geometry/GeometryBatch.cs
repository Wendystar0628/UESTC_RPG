using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Geometry;

/// <summary>Groups ordinary surfaces for the shared accumulator; custom vertex formats and LODs remain intact.</summary>
public static class GeometryBatch
{
    public static IReadOnlyList<GeometryBundle> Merge(IEnumerable<GeometryBundle> meshes)
    {
        ArgumentNullException.ThrowIfNull(meshes);
        var output = new List<GeometryBundle>();
        var streams = new List<(GeometryBundle Key, GeometrySink Sink)>();
        foreach (var mesh in meshes)
        {
            if (mesh.Custom0 is not null || mesh.Custom1 is not null || mesh.Custom2 is not null || mesh.Lods is not null)
            {
                output.Add(GeometryPublication.Copy(mesh));
                continue;
            }
            var slot = streams.FindIndex(s => s.Key.Shader == mesh.Shader && s.Key.Variant == mesh.Variant &&
                s.Key.IgnoreOcclusionCulling == mesh.IgnoreOcclusionCulling && s.Key.Reflect == mesh.Reflect);
            if (slot < 0)
            {
                slot = streams.Count;
                streams.Add((mesh, new GeometrySink()));
            }
            streams[slot].Sink.Append(mesh with
            { Shader = null, Variant = null, Reflect = false, IgnoreOcclusionCulling = false }, Matrix4x4.Identity);
        }
        foreach (var (key, sink) in streams)
            output.AddRange(sink.Finish().Select(mesh => mesh with
            {
                Shader = key.Shader,
                Variant = key.Variant,
                Reflect = key.Reflect,
                IgnoreOcclusionCulling = key.IgnoreOcclusionCulling,
            }));
        return output;
    }
}
