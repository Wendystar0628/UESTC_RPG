using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Contracts.Vegetation;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;

namespace Campus.Gen.Vegetation;

/// <summary>
/// The clump prototypes of veg_grass and the runtime data of the grass pools (WLD-VEGETATION). A clump is a tuft of
/// tapered, curved blades, each two quads and a tip (5 vertices, 3 triangles), rooted in a small disc round the clump foot and
/// leaning outward. Per vertex: UV = (side −1 / 0 / +1, height fraction t); CUSTOM1 = (blade root, blade rank 0–1);
/// CUSTOM2 = (offset of the vertex from the blade's centre line, blade phase). veg_grass sways the centre line, thins the blades
/// by rank with distance, keeps them at least a pixel wide and sinks them into the lawn before the near radius ends.
/// The material is the lawn's own (grass.* pair, lawn wetness) so the blades shade like the ground under them; no outline,
/// no shadow. Pool data reuses <see cref="VegetationMotherData"/>: one entry per style, keyed <see cref="KeyPrefix"/> + style,
/// with a single prototype.
/// </summary>
public static class GrassClumps
{
    /// <summary>Key prefix of the grass pools in <see cref="VegetationModule.PrepareGrass"/> (the runtime splits on it).</summary>
    public const string KeyPrefix = "grass/";

    /// <summary>Sway reach (m) added to the clump bounds.</summary>
    public const float SwayPad = 0.1f;

    private sealed record Form(int Blades, float RootRadius, float MinHeight, float MaxHeight, float Width, float MinLean, float MaxLean);

    private static Form FormOf(GrassStyle style) => style switch
    {
        // Trimmed lawn: short, upright blades. Transition green: taller, looser blades that bow.
        GrassStyle.Short => new(13, 0.11f, 0.075f, 0.14f, 0.011f, 0.15f, 0.45f),
        GrassStyle.Tall => new(9, 0.16f, 0.26f, 0.52f, 0.016f, 0.3f, 0.75f),
        _ => throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown grass style.")
    };

    /// <summary>Horizontal reach of a clump's blades from its foot at scale 1 (m): root disc plus the longest lean.</summary>
    public static float Reach(GrassStyle style)
    {
        var f = FormOf(style);
        return f.RootRadius + f.MaxHeight * f.MaxLean * 0.6f;
    }

    public static ToonSpec Spec() => LawnAppearance.Spec() with
    {
        Kind = ToonKind.VegGrass,
        Line = LineClass.None
    };

    private static readonly Lazy<GeometryPrototype>[] Prototypes =
        Enum.GetValues<GrassStyle>().Select(s => new Lazy<GeometryPrototype>(() => Create(s), LazyThreadSafetyMode.ExecutionAndPublication)).ToArray();

    public static GeometryPrototype Prototype(GrassStyle style) => Prototypes[(int)style].Value;

    /// <summary>One pool entry per style present, records in clump order, bounds per clump.</summary>
    public static IReadOnlyList<VegetationMotherData> Mothers(IReadOnlyList<GrassClump> clumps)
    {
        ArgumentNullException.ThrowIfNull(clumps);
        var mothers = new List<VegetationMotherData>();
        foreach (var style in Enum.GetValues<GrassStyle>())
        {
            var mine = clumps.Where(c => c.Style == style).ToArray();
            if (mine.Length == 0) continue;
            var prototype = Prototype(style);
            var records = new float[mine.Length * InstanceStreamLayout.FloatsPerInstance];
            var bounds = new Box3[mine.Length];
            for (var i = 0; i < mine.Length; i++)
            {
                var c = mine[i];
                var at = Matrix4x4.CreateScale(c.Scale) * Matrix4x4.CreateRotationY(c.Yaw) * Matrix4x4.CreateTranslation(c.Position);
                InstanceStreamLayout.Write(records.AsSpan(i * InstanceStreamLayout.FloatsPerInstance, InstanceStreamLayout.FloatsPerInstance), at, c.Custom);
                bounds[i] = VegetationPrototypes.WorldBounds(prototype.Bounds, at);
            }
            mothers.Add(VegetationMotherData.TakeOwnership(KeyPrefix + Name(style), records, [prototype], bounds,
                GrassPoolIndexBuilder.Build(bounds)));
        }
        return mothers;
    }

    private static string Name(GrassStyle style) => style == GrassStyle.Short ? "short" : "tall";

    private static GeometryPrototype Create(GrassStyle style)
    {
        var f = FormOf(style);
        var position = new List<float>();
        var uv = new List<float>();
        var c1 = new List<float>();
        var c2 = new List<float>();
        var index = new List<int>();
        var salt = StableHash.Hash32(0u, "grass-clump", Name(style));
        for (var b = 0; b < f.Blades; b++)
        {
            float H(int k) => IntegerNoise.High24(IntegerNoise.Hash2D((uint)b, (uint)k, salt));
            // Roots spread evenly over the disc (golden angle), blades lean outward with some scatter.
            var ra = b * 2.3999632f;
            var rr = f.RootRadius * MathF.Sqrt((b + 0.5f) / f.Blades);
            var root = new Vector3(MathF.Cos(ra) * rr, 0f, MathF.Sin(ra) * rr);
            var la = ra + (H(1) - 0.5f) * 1.6f;
            var lean = new Vector3(MathF.Cos(la), 0f, MathF.Sin(la));
            var height = f.MinHeight + (f.MaxHeight - f.MinHeight) * H(2);
            var bow = height * (f.MinLean + (f.MaxLean - f.MinLean) * H(3));
            // The flat side faces roughly across the lean, turned a little so the clump reads from every side.
            var fa = la + MathF.PI * 0.5f + (H(4) - 0.5f) * 0.8f;
            var side = new Vector3(MathF.Cos(fa), 0f, MathF.Sin(fa)) * f.Width * (0.8f + 0.4f * H(5));
            var rank = (b + 0.5f) / f.Blades;
            var phase = H(6);
            Vector3 Centre(float t) => root + new Vector3(0f, height * t, 0f) + lean * (bow * t * t);
            var first = position.Count / 3;
            void Vertex(Vector3 c, Vector3 off, float s, float t)
            {
                var p = c + off;
                position.AddRange([p.X, p.Y, p.Z]);
                uv.AddRange([s, t]);
                c1.AddRange([root.X, root.Y, root.Z, rank]);
                c2.AddRange([off.X, off.Y, off.Z, phase]);
            }
            var mid = 0.55f;
            Vertex(Centre(0f), -side, -1f, 0f);
            Vertex(Centre(0f), side, 1f, 0f);
            Vertex(Centre(mid), -side * 0.72f, -1f, mid);
            Vertex(Centre(mid), side * 0.72f, 1f, mid);
            Vertex(Centre(1f), Vector3.Zero, 0f, 1f);
            index.AddRange([first, first + 1, first + 2, first + 1, first + 3, first + 2, first + 2, first + 3, first + 4]);
        }
        var count = position.Count / 3;
        GeometryVertexLimits.Require(count);
        var normal = Enumerable.Range(0, count).SelectMany(_ => new[] { 0f, 1f, 0f }).ToArray();
        var bundle = new GeometryBundle(new ToonRef(Spec()), [.. position], normal, [.. index], Enumerable.Repeat(1f, count).ToArray(), [.. uv])
        {
            ColorIndices = Enumerable.Repeat(LawnAppearance.Grass, count).ToArray(),
            Flags = Enumerable.Repeat(VertexFlag.NoShadow, count).ToArray(),
            Custom1 = [.. c1],
            Custom2 = [.. c2]
        };
        bundle.ValidateVertexData();
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        for (var i = 0; i < position.Count; i += 3)
        {
            var p = new Vector3(position[i], position[i + 1], position[i + 2]);
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        var pad = new Vector3(SwayPad, 0.05f, SwayPad);
        return new GeometryPrototype(KeyPrefix + Name(style), [GeometryBuffer.Freeze(bundle)], new Box3(min - pad, max + pad));
    }
}
