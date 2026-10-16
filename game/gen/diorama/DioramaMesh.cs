using System.Numerics;
using Campus.Contracts;

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
    public const byte FlagNoShadow = 8;

    /// <summary>Four 0–1 values as 6-bit fields, spring in the lowest bits (≤ 2^24, exact in fp32).</summary>
    public static float PackSeasons(IReadOnlyList<float> four)
    {
        ArgumentNullException.ThrowIfNull(four);
        if (four.Count != 4) throw new ArgumentException("Four seasons expected.", nameof(four));
        var word = 0;
        for (var i = 0; i < 4; i++) word |= (int)MathF.Round(Math.Clamp(four[i], 0f, 1f) * 63f) << (6 * i);
        return word;
    }

    public static float UnpackSeason(float word, int season) => (((int)word >> (6 * season)) & 63) / 63f;
}

/// <summary>Growable vertex and index lists for one diorama surface.</summary>
internal sealed class MeshWriter(byte kind)
{
    private readonly List<float> _position = [];
    private readonly List<float> _normal = [];
    private readonly List<float> _uv = [];
    private readonly List<float> _uv2 = [];
    private readonly List<byte> _ao = [];
    private readonly List<int> _index = [];

    public int VertexCount => _position.Count / 3;
    public int TriangleCount => _index.Count / 3;

    public int Vertex(Vector3 p, Vector3 n, Vector2 uv, Vector2 uv2, byte ao = 255)
    {
        _position.Add(p.X); _position.Add(p.Y); _position.Add(p.Z);
        _normal.Add(n.X); _normal.Add(n.Y); _normal.Add(n.Z);
        _uv.Add(uv.X); _uv.Add(uv.Y);
        _uv2.Add(uv2.X); _uv2.Add(uv2.Y);
        _ao.Add(ao);
        return VertexCount - 1;
    }

    public void Triangle(int a, int b, int c)
    {
        _index.Add(a); _index.Add(b); _index.Add(c);
    }

    /// <summary>Quad a-b-c-d in order; both sides draw (cull_disabled), so winding only matters for consistency.</summary>
    public void Quad(int a, int b, int c, int d)
    {
        Triangle(a, b, c);
        Triangle(a, c, d);
    }

    public GeometryBundle Bundle(ToonSpec spec)
    {
        if (VertexCount > 65536) throw new InvalidOperationException($"Diorama surface kind {kind} has {VertexCount} vertices; the limit is 65 536 (ENG-PERF §5).");
        var count = VertexCount;
        var flags = new byte[count];
        Array.Fill(flags, DioramaVertex.FlagNoShadow);
        var reserved = new byte[count];
        Array.Fill(reserved, kind);
        var ao = new float[count];
        for (var i = 0; i < count; i++) ao[i] = _ao[i] / 255f;
        var bundle = new GeometryBundle(new ToonRef(spec), _position.ToArray(), _normal.ToArray(), _index.ToArray(),
            Ao: ao, Uv: _uv.ToArray(), Uv2: _uv2.ToArray())
        {
            Shader = ShaderId.DioramaFar,
            IgnoreOcclusionCulling = true,
            Flags = flags,
            Reserved = reserved,
        };
        bundle.ValidateVertexData();
        return bundle;
    }
}
