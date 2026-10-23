using System.Buffers.Binary;
using System.Collections.Concurrent;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Adapts immutable local prototypes and world-space instance streams to the existing single-surface cache ABI.</summary>
/// <remarks>
/// Every input stream remains independent, including 64 m sub-blocks inside one L2 tile.
/// There is no tile-origin translation or shadow-proxy parameter in this adapter/codec.
/// Prototype buffers are read directly and packed once per prototype reference in this adapter's map lifetime.
/// </remarks>
public sealed class BakeInstanceAdapter(PaletteCatalog catalog)
{
    private readonly ConcurrentDictionary<GeometryPrototype, Lazy<BakedSurface[]>> _prototypes = new(ReferenceEqualityComparer.Instance);


    public TileBundle PackTile(BakeKey key, TileKey tile, LodLevel level, Box3 tileBounds,
        IReadOnlyList<BakedSurface> surfaces, IReadOnlyList<GeometryInstanceStream> streams)
    {
        var instances = Pack(streams);
        var bounds = tileBounds;
        foreach (var stream in streams)
            bounds = new(System.Numerics.Vector3.Min(bounds.Min, stream.Bounds.Min), System.Numerics.Vector3.Max(bounds.Max, stream.Bounds.Max));
        return BakeGeometryBuilder.Assemble(key, tile, level, bounds, surfaces, instances, streams);
    }

    public IReadOnlyList<BakedMultiMesh> Pack(IReadOnlyList<GeometryInstanceStream> streams)
    {
        ArgumentNullException.ThrowIfNull(streams);
        var output = new List<BakedMultiMesh>();
        foreach (var stream in streams)
        {
            ArgumentNullException.ThrowIfNull(stream);
            var surfaces = _prototypes.GetOrAdd(stream.Prototype, prototype => new(() => PackPrototype(prototype),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
            var bytes = new byte[checked(stream.Data.Length * sizeof(float))];
            var values = stream.Data.Span;
            for (var i = 0; i < values.Length; i++)
                BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * sizeof(float)), values[i]);
            // BakedMultiMesh makes its own immutable copy. N surfaces therefore mean N stored/uploaded streams.
            foreach (var surface in surfaces) output.Add(new(surface, stream.Count, bytes, stream.Bounds));
        }
        return output.AsReadOnly();
    }

    private BakedSurface[] PackPrototype(GeometryPrototype prototype)
    {
        var result = new BakedSurface[prototype.Surfaces.Count];
        for (var slot = 0; slot < result.Length; slot++)
        {
            var source = prototype.Surfaces[slot];
            var label = $"{prototype.Id}/surface[{slot}]";
            if (source.Variant is not null)
                throw new NotSupportedException($"Instance prototype {label} has Variant state; this adapter does not encode per-instance variant handling.");
            if (source.Reflect)
                throw new NotSupportedException($"Instance prototype {label} requests reflection; the current MultiMesh consumer does not support reflection proxies.");
            result[slot] = BakeSurfacePacker.Pack(source, prototype.Bounds, catalog);
        }
        return result;
    }
}
