using System.Reflection;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Registry;
using N = System.Numerics;

namespace Campus.Kernel.Bake;

/// <summary>Expands registered parts and packs surfaces on a bake worker; never touches the scene tree.</summary>
internal sealed class BakeGeometryBuilder
{
    private readonly PaletteCatalog _catalog;
    private readonly IReadOnlyDictionary<string, Func<object, LodLevel, PartGeometry>> _parts;
    private readonly BakeInstanceAdapter _instances;

    public BakeGeometryBuilder(PaletteCatalog catalog, ServiceRegistry registry, MapId? map = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog;
        _instances = new BakeInstanceAdapter(catalog);
        var parts = new Dictionary<string, Func<object, LodLevel, PartGeometry>>(StringComparer.Ordinal);
        foreach (var item in (map is { } mapId ? registry.ForMap(mapId.Value) : registry.Entries)
            .Where(r => r.Kind == "part").OrderBy(r => r.Id, StringComparer.Ordinal))
        {
            var part = registry.Create(item.Type);
            var api = item.Type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>));
            var factory = typeof(BakeGeometryBuilder).GetMethod(nameof(PartBuilder), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(api.GetGenericArguments()[0]);
            parts.Add(item.Id, (Func<object, LodLevel, PartGeometry>)factory.Invoke(null, [part])!);
        }
        _parts = parts;
    }

    public TileBundle Build(BakeKey key, BuildContext context, IContentModule module, Action<IReadOnlyList<BuildDiagnostic>> collect)
    {
        var request = BakeRequestKeys.Parse(key);
        if (request.Collision) throw new InvalidOperationException("Collision requests must use a collision producer.");
        RequireCatalog(context);
        var output = module.Build(context, request.Tile, request.Level);
        return BuildOutput(key, context, output, request.Tile, request.Level, collect);
    }

    public TileBundle BuildOutput(BakeKey key, BuildContext context, ContentOutput output, TileKey tile, LodLevel level,
        Action<IReadOnlyList<BuildDiagnostic>> collect)
    {
        RequireCatalog(context);
        collect(output.Diagnostics);
        var meshes = new List<GeometryBundle>(output.Meshes);
        foreach (var part in output.Parts)
        {
            if (!_parts.TryGetValue(part.Part.Value, out var build)) throw new InvalidDataException($"Unknown part: {part.Part.Value}");
            var geometry = build(part.Params, level);
            foreach (var mesh in geometry.Meshes) meshes.Add(Transform(mesh, part));
        }
        var bounds = Bounds(context, tile, level, meshes);
        var surfaces = meshes.Where(m => m.Position.Length > 0 && m.Index.Length > 0).Select(m => Pack(m, bounds)).ToArray();
        var batched = PackedSurfaceBatcher.Merge(surfaces);
        return _instances.PackTile(key, tile, level, bounds, batched, output.Instances);
    }

    internal static TileBundle Assemble(BakeKey key, TileKey tile, LodLevel level, Box3 bounds,
        IReadOnlyList<BakedSurface> surfaces, IReadOnlyList<BakedMultiMesh> instances, IReadOnlyList<GeometryInstanceStream> streams)
    {
        var bundle = new TileBundle(key, tile, level, bounds, surfaces, instances);
        var streamGroups = new List<object>();
        var firstMultiMesh = 0;
        foreach (var stream in streams)
        {
            streamGroups.Add(new
            {
                stream.Id,
                prototype = stream.Prototype.Id,
                firstMultiMesh,
                surfaceCount = stream.Prototype.Surfaces.Count,
                logicalInstances = stream.Count,
                bounds = stream.Bounds
            });
            firstMultiMesh = checked(firstMultiMesh + stream.Prototype.Surfaces.Count);
        }
        var statistics = JsonSerializer.Serialize(new
        {
            module = key.Module,
            vertices = bundle.Stats.Vertices,
            triangles = bundle.Stats.Triangles,
            surfaces = bundle.Stats.Surfaces,
            bytes = bundle.Stats.Bytes,
            instances = bundle.Stats.Instances,
            logicalInstances = streams.Sum(stream => (long)stream.Count),
            multiMeshes = instances.Count,
            instanceBufferBytes = instances.Sum(instance => (long)instance.InstanceData.Length),
            submittedTriangles = surfaces.Sum(Triangles) + instances.Sum(instance => Triangles(instance.Surface) * instance.InstanceCount),
            instanceStreams = streamGroups,
            bounds,
            pipelines = surfaces.Select(surface => (Surface: surface, Instanced: false))
                .Concat(instances.Select(instance => (Surface: instance.Surface, Instanced: true)))
                .Select(item => new
                {
                    item.Surface.Material.Spec.Kind,
                    item.Surface.Material.Spec.Fx,
                    item.Surface.Format,
                    item.Surface.Primitive,
                    item.Surface.Shader,
                    item.Surface.Reflect,
                    item.Instanced
                }).Distinct()
                .OrderBy(p => p.Kind).ThenBy(p => p.Fx).ThenBy(p => p.Format).ThenBy(p => p.Shader).ThenBy(p => p.Reflect).ThenBy(p => p.Instanced).ToArray()
        }, ContractJson.Options);
        return new(key, tile, level, bounds, surfaces, instances, statistics);
    }

    private static long Triangles(BakedSurface surface)
    {
        var count = surface.IndexCount == 0 ? surface.VertexCount : surface.IndexCount;
        return surface.Primitive switch
        {
            BakedPrimitive.Points or BakedPrimitive.Lines or BakedPrimitive.LineStrip => 0,
            BakedPrimitive.Triangles => count / 3,
            BakedPrimitive.TriangleStrip => Math.Max(0, count - 2),
            _ => throw new InvalidDataException("Unknown packed primitive.")
        };
    }

    private static Func<object, LodLevel, PartGeometry> PartBuilder<T>(IPartDef<T> part) where T : notnull =>
        (parameters, level) => part.Lods.TryGetValue(level, out var build)
            ? build(parameters is T typed ? typed : throw new ArgumentException($"Part parameter type must be {typeof(T).Name}."))
            : throw new InvalidDataException($"Part {part.GetType().Name} has no {level} builder.");

    private void RequireCatalog(BuildContext context)
    {
        if (!ReferenceEquals(context.Catalog, _catalog))
            throw new InvalidOperationException("Geometry generation and packing require the same catalog snapshot.");
    }

    private GeometryBundle Transform(GeometryBundle mesh, PlacedPart part)
    {
        mesh.ValidateVertexData();
        if (!N.Matrix4x4.Invert(part.At, out var inverse)) throw new InvalidDataException("Part transform is singular.");
        var normalMatrix = N.Matrix4x4.Transpose(inverse);
        var positions = new float[mesh.Position.Length]; var normals = new float[mesh.Normal.Length];
        for (var i = 0; i < positions.Length; i += 3)
        {
            var position = N.Vector3.Transform(new(mesh.Position[i], mesh.Position[i + 1], mesh.Position[i + 2]), part.At);
            var normal = N.Vector3.Normalize(N.Vector3.TransformNormal(new(mesh.Normal[i], mesh.Normal[i + 1], mesh.Normal[i + 2]), normalMatrix));
            positions[i] = position.X; positions[i + 1] = position.Y; positions[i + 2] = position.Z;
            normals[i] = normal.X; normals[i + 1] = normal.Y; normals[i + 2] = normal.Z;
        }
        var tinted = BakeSurfacePacker.ApplyTint(mesh, part, _catalog);
        return tinted with
        {
            Position = positions,
            Normal = normals,
            Variant = part.Variant ?? mesh.Variant,
            IgnoreOcclusionCulling = part.IgnoreOcclusionCulling || mesh.IgnoreOcclusionCulling,
            Custom0 = TransformCustom(mesh.Custom0, part.At),
            Custom1 = TransformCustom(mesh.Custom1, part.At),
            Custom2 = TransformCustom(mesh.Custom2, part.At)
        };
    }

    private static float[]? TransformCustom(float[]? values, N.Matrix4x4 at)
    {
        if (values is null) return null;
        var result = (float[])values.Clone();
        for (var i = 0; i < result.Length; i += 4)
        {
            var point = N.Vector3.Transform(new(values[i], values[i + 1], values[i + 2]), at);
            result[i] = point.X; result[i + 1] = point.Y; result[i + 2] = point.Z;
        }
        return result;
    }

    private Box3 Bounds(BuildContext context, TileKey tile, LodLevel level, IEnumerable<GeometryBundle> meshes)
    {
        var size = BakeRequestKeys.BaseTileMetres << (int)level;
        var minimum = level == LodLevel.L4 ? context.Map.Bounds.Min : new N.Vector3(tile.X * size, context.Map.Bounds.Min.Y, tile.Z * size);
        var maximum = level == LodLevel.L4 ? context.Map.Bounds.Max : minimum + new N.Vector3(size, context.Map.Bounds.Size.Y, size);
        var hasGeometry = false;
        foreach (var mesh in meshes)
        {
            mesh.ValidateVertexData();
            if (mesh.Position.Length == 0 || mesh.Index.Length == 0) continue;
            if (!hasGeometry)
            {
                minimum.Y = float.PositiveInfinity;
                maximum.Y = float.NegativeInfinity;
                hasGeometry = true;
            }
            for (var i = 0; i < mesh.Position.Length; i += 3)
            {
                var p = new N.Vector3(mesh.Position[i], mesh.Position[i + 1], mesh.Position[i + 2]);
                minimum = N.Vector3.Min(minimum, p); maximum = N.Vector3.Max(maximum, p);
            }
        }
        // Keep the grid's XZ coverage, but quantize Y over actual geometry instead of the map's flight limits.
        // All surfaces share exactly one non-degenerate box, including parts crossing a grid edge.
        maximum = N.Vector3.Max(maximum, minimum + new N.Vector3(0.001f));
        return new(minimum, maximum);
    }

    private BakedSurface Pack(GeometryBundle mesh, Box3 bounds) => BakeSurfacePacker.Pack(mesh, bounds, _catalog);
}
