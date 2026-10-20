using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Diorama;

/// <summary>
/// Vertex layout of diorama_far.gdshader (VF-B: VF-A + fp32 UV2, the same format as water; no new vertex format):
/// COLOR = palette lit index (shade = lit + 1), AO byte = per-kind detail, flags 8 (never casts), reserved byte = kind.
/// <list type="bullet">
/// <item>Fog sea (kind 0): UV = (distance outward from the inner edge m, 0); UV2 = (0, 0).</item>
/// <item>City ring (kind 1): UV = (metres along the face, metres above y = 0); UV2 = (parallax k, haze 0–1); AO = building seed.</item>
/// <item>Far ring (kind 2, parallax 1 horizontally and vertically): UV = (snow potential 0–1, ridge edge 0/1);
/// UV2 = (seasonal visibility word, seasonal snowline word), each four 6-bit seasons packed spring-low (exact in fp32).</item>
/// </list>
/// The shader reads the kind from the reserved byte; nothing here depends on the map.
/// </summary>
public static class DioramaVertex
{
    public const byte KindFogSea = 0;
    public const byte KindCity = 1;
    public const byte KindFar = 2;

    /// <summary>Largest value stored in one 6-bit season field.</summary>
    public const int SeasonQuantizationMax = 63;

    /// <summary>Four 0–1 values as 6-bit fields, spring in the lowest bits (≤ 2^24, exact in fp32).</summary>
    public static float PackSeasons(IReadOnlyList<float> four)
    {
        ArgumentNullException.ThrowIfNull(four);
        if (four.Count != 4) throw new ArgumentException("Four seasons expected.", nameof(four));
        var word = 0;
        for (var i = 0; i < 4; i++) word |= (int)MathF.Round(Math.Clamp(four[i], 0f, 1f) * SeasonQuantizationMax) << (6 * i);
        return word;
    }

    public static float UnpackSeason(float word, int season) => (((int)word >> (6 * season)) & SeasonQuantizationMax) / (float)SeasonQuantizationMax;
}

/// <summary>Indexed diorama construction with shader metadata applied after shared surface publication.</summary>
internal sealed class MeshWriter(byte kind)
{
    private readonly List<GeometryVertex> _vertices = [];
    private readonly List<int> _index = [];
    public int VertexCount => _vertices.Count;
    public int TriangleCount => _index.Count / 3;

    public int Vertex(Vector3 p, Vector3 n, Vector2 uv, Vector2 uv2, byte ao = 255)
    {
        _vertices.Add(new(p, n, uv, uv2, ao / 255f, Flags: VertexFlag.NoShadow, Reserved: kind));
        return VertexCount - 1;
    }

    public void Triangle(int a, int b, int c)
    { _index.Add(a); _index.Add(b); _index.Add(c); }

    public void Quad(int a, int b, int c, int d)
    { Triangle(a, b, c); Triangle(a, c, d); }

    public IReadOnlyList<GeometryBundle> Bundles(ToonSpec spec)
    {
        var sink = new GeometrySink(withUv2: true);
        sink.Polygon(spec, _vertices, _index);
        return sink.Finish().Select(bundle => bundle with
        {
            Shader = ShaderId.DioramaFar,
            IgnoreOcclusionCulling = true,
        }).ToArray();
    }
}
