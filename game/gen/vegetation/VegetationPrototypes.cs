using System.Collections.Concurrent;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Campus.Contracts;

namespace Campus.Gen.Vegetation;

/// <summary>
/// The pools a tree can be drawn in (WLD-VEGETATION, ENG-PERF): the three near levels (≤ 150 m), the far
/// pool (150–400 m) and the ShadowsOnly proxy of the near trees.
/// </summary>
public enum VegPool : byte { L0Plus = 0, L0 = 1, L1 = 2, Far = 3, Shadow = 4 }

/// <summary>
/// Frozen prototypes of the vegetation pools and the per-tree instance record. One prototype per mother model (species ×
/// variant) and pool, built once per palette and shared by every tile stream and by the runtime pools: the near levels are
/// the mother models (wood surface, then crown surface); the far pool's mainline stand-in is <see cref="MotherModels.FarStandIn"/>
/// (the L2 stand-in decided in WLD-VEGETATION replaces it in the detail version); the shadow proxy is
/// <see cref="MotherModels.ShadowProxy"/>. The instance record is the 3×4 transform (uniform scale, yaw, trunk foot) plus
/// INSTANCE_CUSTOM (<see cref="PlantedTree.Custom"/>, §11.2).
/// </summary>
public static class VegetationPrototypes
{
    public const string Owner = "WP-23";

    /// <summary>Crown sway and card flutter reach (m) around the model bounds (veg_* wind, WLD-VEGETATION CustomAabb).</summary>
    public const float SwayPad = 0.6f;

    private static readonly ConditionalWeakTable<PaletteCatalog, ConcurrentDictionary<(SpeciesInfo, int, VegPool), Lazy<GeometryPrototype>>> Cache = new();

    /// <summary>Prototype ID: vegetation/{species}/{variant}/{pool}.</summary>
    public static string Id(SpeciesInfo species, int variant, VegPool pool)
    {
        ArgumentNullException.ThrowIfNull(species);
        return string.Join('/', "vegetation", species.Id, variant.ToString(CultureInfo.InvariantCulture), Name(pool));
    }

    /// <summary>Species ID, variant and pool of a prototype ID made by <see cref="Id"/>.</summary>
    public static (string Species, int Variant, VegPool Pool) Parse(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        var parts = id.Split('/');
        if (parts.Length != 4 || parts[0] != "vegetation" || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var variant))
            throw new FormatException($"Not a vegetation prototype ID: {id}");
        var pool = parts[3] switch
        {
            "l0p" => VegPool.L0Plus,
            "l0" => VegPool.L0,
            "l1" => VegPool.L1,
            "far" => VegPool.Far,
            "shadow" => VegPool.Shadow,
            _ => throw new FormatException($"Unknown vegetation pool in {id}")
        };
        return (parts[1], variant, pool);
    }

    public static GeometryPrototype Get(SpeciesInfo species, int variant, VegPool pool, PaletteCatalog palette)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(palette);
        _ = Name(pool);
        var table = Cache.GetValue(palette, _ => new());
        return table.GetOrAdd((species, variant, pool), key => new Lazy<GeometryPrototype>(
            () => Create(key.Item1, key.Item2, key.Item3, palette), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    /// <summary>Local → world: uniform scale, yaw about the trunk, then the trunk foot (row-vector order).</summary>
    public static Matrix4x4 Transform(PlantedTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);
        return Matrix4x4.CreateScale(tree.Scale) * DetMath.RotationY(tree.Yaw) * Matrix4x4.CreateTranslation(tree.Position);
    }

    /// <summary>Writes one 16-float instance record (<see cref="InstanceStreamLayout"/>).</summary>
    public static void Write(Span<float> record, PlantedTree tree) => InstanceStreamLayout.Write(record, Transform(tree), tree.Custom);

    /// <summary>World bounds of a local box under an affine transform (the eight corners).</summary>
    public static Box3 WorldBounds(Box3 local, Matrix4x4 at)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        for (var i = 0; i < 8; i++)
        {
            var corner = new Vector3((i & 1) == 0 ? local.Min.X : local.Max.X, (i & 2) == 0 ? local.Min.Y : local.Max.Y,
                (i & 4) == 0 ? local.Min.Z : local.Max.Z);
            var p = Vector3.Transform(corner, at);
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        return new Box3(min, max);
    }

    public static Box3 Union(Box3 a, Box3 b) => new(Vector3.Min(a.Min, b.Min), Vector3.Max(a.Max, b.Max));

    public static int Triangles(GeometryPrototype prototype)
    {
        ArgumentNullException.ThrowIfNull(prototype);
        return prototype.Surfaces.Sum(s => s.Index.Length / 3);
    }

    private static string Name(VegPool pool) => pool switch
    {
        VegPool.L0Plus => "l0p",
        VegPool.L0 => "l0",
        VegPool.L1 => "l1",
        VegPool.Far => "far",
        VegPool.Shadow => "shadow",
        _ => throw new ArgumentOutOfRangeException(nameof(pool), pool, "Unknown vegetation pool.")
    };

    private static GeometryPrototype Create(SpeciesInfo species, int variant, VegPool pool, PaletteCatalog palette)
    {
        var id = Id(species, variant, pool);
        switch (pool)
        {
            case VegPool.Shadow:
                var proxy = MotherModels.ShadowProxy(species, variant, palette);
                return new GeometryPrototype(id, [GeometryBuffer.Freeze(proxy)], Pad(BoundsOf(proxy.Position)));
            case VegPool.Far:
                // The far stand-in (MotherModels.FarStandIn, 23c2c): a five-sided trunk and the L1 merged masses at icosphere
                // level 0, tagged so veg_card / veg_trunk cross-fade it against the near pool's L1; stemless balls have no wood.
                var l1 = Get(species, variant, VegPool.L1, palette);
                var (farWood, farCrown) = MotherModels.FarStandIn(species, variant, palette);
                var far = new List<GeometryBuffer>(2);
                if (farWood.Index.Length > 0) far.Add(GeometryBuffer.Freeze(farWood));
                far.Add(GeometryBuffer.Freeze(farCrown));
                return new GeometryPrototype(id, far, l1.Bounds);
            case VegPool.L0Plus:
            case VegPool.L0:
            case VegPool.L1:
                var level = pool switch { VegPool.L0Plus => VegLevel.L0Plus, VegPool.L0 => VegLevel.L0, _ => VegLevel.L1 };
                var model = MotherModels.Build(species, variant, level, palette);
                var surfaces = new List<GeometryBuffer>(2);
                // A pool surface must draw triangles; a level without wood or crown simply has one surface.
                if (model.Wood.Index.Length > 0) surfaces.Add(GeometryBuffer.Freeze(model.Wood));
                if (model.Crown.Index.Length > 0) surfaces.Add(GeometryBuffer.Freeze(model.Crown));
                return new GeometryPrototype(id, surfaces, Pad(model.Bounds));
            default:
                throw new ArgumentOutOfRangeException(nameof(pool), pool, "Unknown vegetation pool.");
        }
    }

    private static Box3 BoundsOf(float[] position)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        for (var i = 0; i < position.Length; i += 3)
        {
            var p = new Vector3(position[i], position[i + 1], position[i + 2]);
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        return position.Length == 0 ? new Box3(Vector3.Zero, Vector3.Zero) : new Box3(min, max);
    }

    private static Box3 Pad(Box3 box) => new(box.Min - new Vector3(SwayPad), box.Max + new Vector3(SwayPad));
}
