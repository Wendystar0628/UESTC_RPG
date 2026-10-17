using Campus.Contracts;

namespace Campus.Gen.Geometry;

/// <summary>Creates caller-owned writable output from a frozen shared surface.</summary>
public static class GeometryPublication
{
    /// <param name="hasLodChannel">Retains a source's null versus explicitly empty LOD channel.</param>
    public static GeometryBundle Publish(GeometryBuffer source, bool hasLodChannel = false)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new(source.Material, source.Position.ToArray(), source.Normal.ToArray(), source.Index.ToArray(),
            source.Ao?.ToArray(), source.Uv?.ToArray(), source.Uv2?.ToArray(),
            source.Lods.Count == 0 && !hasLodChannel ? null : source.Lods.Select(lod => new MeshLod(lod.Error, lod.Index.ToArray())).ToArray(), source.Variant)
        {
            Shader = source.Shader,
            IgnoreOcclusionCulling = source.IgnoreOcclusionCulling,
            Reflect = source.Reflect,
            ColorIndices = source.ColorIndices?.ToArray(),
            DecalBaseColorIndices = source.DecalBaseColorIndices?.ToArray(),
            Flags = source.Flags?.ToArray(),
            Reserved = source.Reserved?.ToArray(),
            Custom0 = source.Custom0?.ToArray(),
            Custom1 = source.Custom1?.ToArray(),
            Custom2 = source.Custom2?.ToArray(),
        };
    }

    internal static GeometryBundle Copy(GeometryBundle source) => source with
    {
        Position = source.Position.ToArray(),
        Normal = source.Normal.ToArray(),
        Index = source.Index.ToArray(),
        Ao = source.Ao?.ToArray(),
        Uv = source.Uv?.ToArray(),
        Uv2 = source.Uv2?.ToArray(),
        ColorIndices = source.ColorIndices?.ToArray(),
        DecalBaseColorIndices = source.DecalBaseColorIndices?.ToArray(),
        Flags = source.Flags?.ToArray(),
        Reserved = source.Reserved?.ToArray(),
        Custom0 = source.Custom0?.ToArray(),
        Custom1 = source.Custom1?.ToArray(),
        Custom2 = source.Custom2?.ToArray(),
        Lods = source.Lods?.Select(lod => new MeshLod(lod.Error, lod.Index.ToArray())).ToArray(),
    };
}
