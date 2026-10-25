using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Tests;
using System.Numerics;

namespace Campus.Gen.Sites.Tests;

/// <summary>Picks the cells a whole-zone determinism test compares: the few with the most placed items.</summary>
internal static class ZoneTestCells
{
    public static TileKey[] Heaviest(ZoneModuleBase module, BuildContext ctx, LodLevel lod = LodLevel.L0) =>
        [.. Items(module, ctx, lod).GroupBy(item => TileGrid.At(item.Anchor, lod))
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.X).ThenBy(group => group.Key.Z)
            .Take(TestSamples.HeaviestCells).Select(group => group.Key)];

    public static IReadOnlyList<SiteItem> Items(ZoneModuleBase module, BuildContext ctx, LodLevel lod = LodLevel.L0)
    {
        var tiles = lod == LodLevel.L4 ? [new TileKey(0, 0)] : module.Tiles(ctx)
            .Select(tile => TileGrid.At(TileGrid.Origin(tile), lod)).Distinct()
            .OrderBy(tile => tile.X).ThenBy(tile => tile.Z).ToArray();
        return tiles.SelectMany(tile => module.BuildItems(ctx, tile, lod)).ToArray();
    }

    public static IReadOnlyList<Collider> Colliders(ZoneModuleBase module, BuildContext ctx) =>
        module.Tiles(ctx).SelectMany(tile => module.Collide(ctx, tile)).ToArray();

    /// <summary>VF-B numbered cells, excluding clear glass without a window-card grid.</summary>
    public static (HashSet<int> Ids, int UnnumberedVertices, int NumberedMeshes) WindowIds(IEnumerable<GeometryBundle> meshes)
    {
        var ids = new HashSet<int>();
        var unnumbered = 0;
        var numberedMeshes = 0;
        foreach (var mesh in meshes.Where(mesh => (mesh.Material.Spec.Kind is ToonKind.Glass or ToonKind.GlassNear) && mesh.Uv2 is not null))
        {
            mesh.ValidateVertexData();
            var numbered = false;
            for (var vertex = 0; vertex < mesh.Uv2!.Length / 2; vertex++)
            {
                if (WindowId(mesh, vertex) is not { } id) { unnumbered++; continue; }
                numbered = true;
                ids.Add(id);
            }
            if (numbered) numberedMeshes++;
        }
        return (ids, unnumbered, numberedMeshes);
    }

    public static int? WindowId(GeometryBundle mesh, int vertex)
    {
        var columns = (int)MathF.Floor(MathF.Round(mesh.Uv2![2 * vertex + 1]) / LightChannelCatalog.Count);
        return columns == 0 ? null : (int)MathF.Round(mesh.Uv2[2 * vertex])
            + (int)MathF.Floor(mesh.Uv![2 * vertex + 1]) * columns + (int)MathF.Floor(mesh.Uv[2 * vertex]);
    }

    /// <summary>Compares source partitioning at the shared stage before GeometryBatch.Merge.</summary>
    public static bool SameTriangles(IEnumerable<GeometryBundle> source, IEnumerable<GeometryBundle> tiled)
    {
        var expected = Triangles(source);
        foreach (var triangle in Triangles(tiled))
        {
            if (!expected.TryGetValue(triangle.Key, out var count) || count != triangle.Value) return false;
            expected.Remove(triangle.Key);
        }
        return expected.Count == 0;

        static Dictionary<(ToonRef Material, Vector3 A, Vector3 B, Vector3 C, Vector3 NA, Vector3 NB, Vector3 NC), int> Triangles(IEnumerable<GeometryBundle> meshes)
        {
            var result = new Dictionary<(ToonRef, Vector3, Vector3, Vector3, Vector3, Vector3, Vector3), int>();
            foreach (var mesh in meshes)
                for (var index = 0; index < mesh.Index.Length; index += 3)
                {
                    Vector3 Position(int vertex) => new(mesh.Position[vertex * 3], mesh.Position[vertex * 3 + 1], mesh.Position[vertex * 3 + 2]);
                    Vector3 Normal(int vertex) => new(mesh.Normal[vertex * 3], mesh.Normal[vertex * 3 + 1], mesh.Normal[vertex * 3 + 2]);
                    var a = mesh.Index[index]; var b = mesh.Index[index + 1]; var c = mesh.Index[index + 2];
                    var triangle = (mesh.Material, Position(a), Position(b), Position(c), Normal(a), Normal(b), Normal(c));
                    result[triangle] = result.GetValueOrDefault(triangle) + 1;
                }
            return result;
        }
    }
}
