using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Contracts.Maps;
using Campus.Contracts.Water;

namespace Campus.Gen.Water;

/// <summary>
/// All water surfaces of one map, meshed once and handed out per streaming cell. A triangle belongs to the cell that
/// holds its centroid (STD-CONVENTIONS: an element crossing cells belongs to exactly one cell).
/// Cell keys follow the content-module convention: at level L the key is floor(world / (64 &lt;&lt; L)); L4 (0, 0) is the whole map.
/// </summary>
public sealed class WaterSurfaces
{
    private readonly Lazy<WaterMesh[]> _meshes;

    public WaterSurfaces(IWorldModel world, MapDescriptor map, WaterParams? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(map);
        var p = parameters ?? WaterParams.Default;
        Bodies = WaterCatalog.Build(world, map, p);
        _meshes = new(() => Bodies.Select(b => WaterMesher.Build(b, Bodies, p)).ToArray(),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IReadOnlyList<WaterBody> Bodies { get; }
    public IReadOnlyList<WaterMesh> Meshes => _meshes.Value;
    public int TriangleCount => Meshes.Sum(m => m.TriangleCount);

    public IEnumerable<TileKey> Tiles()
    {
        var tiles = new SortedSet<(int X, int Z)>();
        foreach (var mesh in Meshes)
            for (var t = 0; t < mesh.TriangleCount; t++)
            {
                var key = CellOf(mesh, t, LodLevel.L0);
                tiles.Add((key.X, key.Z));
            }
        return tiles.Select(t => new TileKey(t.X, t.Z)).ToArray();
    }

    /// <summary>One bundle with every water triangle of the cell, or null when the cell holds no water.</summary>
    public GeometryBundle? Bundle(TileKey tile, LodLevel lod)
    {
        var whole = lod == LodLevel.L4;
        if (whole && tile != new TileKey(0, 0)) return null;
        var position = new List<float>();
        var uv = new List<float>();
        var uv2 = new List<float>();
        var poolData = new List<float>();
        var poolProfile = new List<float>();
        var poolLanes = new List<float>();
        var index = new List<int>();
        foreach (var mesh in Meshes)
        {
            var remap = new Dictionary<int, int>();
            for (var t = 0; t < mesh.TriangleCount; t++)
            {
                if (!whole && CellOf(mesh, t, lod) != tile) continue;
                for (var k = 0; k < 3; k++)
                {
                    var v = mesh.Index[3 * t + k];
                    if (!remap.TryGetValue(v, out var n))
                    {
                        n = position.Count / 3;
                        remap.Add(v, n);
                        position.Add(mesh.Position[3 * v]);
                        position.Add(mesh.Position[3 * v + 1]);
                        position.Add(mesh.Position[3 * v + 2]);
                        uv.Add(mesh.Body.Kind == WaterKind.Pool ? WaterPoolRules.LineWidth : mesh.Flow[2 * v]);
                        uv.Add(mesh.Flow[2 * v + 1]);
                        uv2.Add(mesh.Shore[v]);
                        uv2.Add(mesh.Body.ReflectSlot);
                        if (mesh.Body.PoolFrame is { } frame)
                        {
                            var local = frame.Coordinates(new(mesh.Position[3 * v], mesh.Position[3 * v + 2]));
                            poolData.AddRange([1f, local.X, local.Y, frame.Length]);
                            poolProfile.AddRange([WaterPoolRules.ShallowDepth, WaterPoolRules.DeepDepth,
                                WaterPoolRules.ShallowFraction, WaterPoolRules.LaneCount(frame.Width)]);
                            poolLanes.AddRange([WaterPoolRules.LaneCenter(0, WaterPoolRules.LaneCount(frame.Width)),
                                WaterPoolRules.LaneWidth, WaterPoolRules.LaneEndBarLength, WaterPoolRules.LaneEndInset]);
                        }
                        else
                        {
                            poolData.AddRange([0f, 0f, 0f, 0f]);
                            poolProfile.AddRange([0f, 0f, 0f, 0f]);
                            poolLanes.AddRange([0f, 0f, 0f, 0f]);
                        }
                    }
                    index.Add(n);
                }
            }
        }
        if (index.Count == 0) return null;
        var count = position.Count / 3;
        GeometryVertexLimits.Require(count);
        var normal = new float[count * 3];
        for (var i = 0; i < count; i++) normal[3 * i + 1] = 1f;
        var flags = new byte[count];
        Array.Fill(flags, VertexFlag.NoShadow);   // no shadow: the water never casts (WLD-WATER)
        var bundle = new GeometryBundle(new ToonRef(WaterPalette.Surface), position.ToArray(), normal, index.ToArray(),
            Uv: uv.ToArray(), Uv2: uv2.ToArray())
        {
            Flags = flags,
            Custom0 = poolData.ToArray(),
            Custom1 = poolProfile.ToArray(),
            Custom2 = poolLanes.ToArray(),
        };
        bundle.ValidateVertexData();
        return bundle;
    }

    private static TileKey CellOf(WaterMesh mesh, int triangle, LodLevel lod)
    {
        var a = mesh.Index[3 * triangle];
        var b = mesh.Index[3 * triangle + 1];
        var c = mesh.Index[3 * triangle + 2];
        var x = (mesh.Position[3 * a] + mesh.Position[3 * b] + mesh.Position[3 * c]) / 3f;
        var z = (mesh.Position[3 * a + 2] + mesh.Position[3 * b + 2] + mesh.Position[3 * c + 2]) / 3f;
        return TileGrid.At(new Vector2(x, z), lod);
    }
}
