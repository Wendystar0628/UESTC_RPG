using System.Numerics;
using Campus.Contracts;
using static Campus.Contracts.ScalarMath;

namespace Campus.Gen.Diorama;

/// <summary>
/// One city ring of the backdrop (WLD-DIORAMA, O3): a few rows of simple building masses standing on a circle round
/// the campus centre, low blocks and residential towers by compass sector, their feet sunk below the fog sea. The faces are
/// real planes, so the lit and shaded sides follow the live sun (the "slope layer", §5); haze, night windows and weather
/// are the shader's. Back and bottom faces are left out: the camera never leaves the ring (parallax k &lt; 1, roam ≤ 3 km).
/// </summary>
public static class CityRing
{
    // Building masses (m): residential towers, low blocks; gaps between buildings; rooftop machine rooms on towers.
    private const float TowerWidthMin = 22f, TowerWidthMax = 40f, TowerDepthMin = 15f, TowerDepthMax = 24f;
    private const float BlockWidthMin = 38f, BlockWidthMax = 104f, BlockDepthMin = 24f, BlockDepthMax = 58f;
    private const float TowerGapMin = 9f, TowerGapMax = 30f, BlockGapMin = 14f, BlockGapMax = 46f;
    private const float CrownShare = 0.55f, CrownHeightMin = 3.5f, CrownHeightMax = 7f;
    private const int ClusterMin = 2, ClusterMax = 6;
    private const float YawJitterDeg = 9f;
    private const float SectorBlendDeg = 7f;

    public static GeometryBundle Build(BackdropData data, CityRingData ring, int ringIndex, Vector2 centre, uint seed)
    {
        var mesh = new MeshWriter(DioramaVertex.KindCity);
        for (var row = 0; row < ring.Rows; row++)
        {
            var rowRadius = ring.Rows == 1 ? ring.Radius : ring.Radius - ring.Depth * 0.5f + ring.Depth * row / (ring.Rows - 1);
            var haze = Math.Clamp(ring.Haze + ring.RowHaze * (row - (ring.Rows - 1) * 0.5f), 0f, 1f);
            var rng = new Rng(StableHash.Hash32(seed, "diorama.city", ring.Id, row.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            var start = rng.NextFloat() * 360f;
            var az = start;
            var cluster = 0;
            var clusterHeight = 0f;
            while (az < start + 360f)
            {
                var bearing = Angles.WrapDegrees(az);
                var fill = Fill(data.Sectors, bearing, ringIndex);
                var tower = rng.NextFloat() < fill.TowerShare;
                var width = tower ? Lerp(TowerWidthMin, TowerWidthMax, rng.NextFloat()) : Lerp(BlockWidthMin, BlockWidthMax, rng.NextFloat());
                var depth = tower ? Lerp(TowerDepthMin, TowerDepthMax, rng.NextFloat()) : Lerp(BlockDepthMin, BlockDepthMax, rng.NextFloat());
                var gap = tower ? Lerp(TowerGapMin, TowerGapMax, rng.NextFloat()) : Lerp(BlockGapMin, BlockGapMax, rng.NextFloat());
                var present = rng.NextFloat() < fill.Fill;
                float height;
                if (tower)
                {
                    if (cluster <= 0)
                    {
                        cluster = ClusterMin + rng.NextInt(ClusterMax - ClusterMin + 1);
                        clusterHeight = Lerp(fill.High.Min, fill.High.Max, rng.NextFloat());
                    }
                    cluster--;
                    height = clusterHeight * Lerp(0.93f, 1.07f, rng.NextFloat());
                }
                else
                {
                    cluster = 0;
                    height = Lerp(fill.Low.Min, fill.Low.Max, rng.NextFloat());
                }
                var radial = rowRadius + (rng.NextFloat() - 0.5f) * (ring.Rows > 1 ? ring.Depth / ring.Rows : 60f);
                var yaw = (rng.NextFloat() * 2f - 1f) * YawJitterDeg;
                var crown = tower && rng.NextFloat() < CrownShare;
                var crownW = width * Lerp(0.28f, 0.42f, rng.NextFloat());
                var crownD = depth * Lerp(0.4f, 0.6f, rng.NextFloat());
                var crownH = Lerp(CrownHeightMin, CrownHeightMax, rng.NextFloat());
                var crownShift = (rng.NextFloat() - 0.5f) * (width - crownW) * 0.7f;
                var buildingSeed = (byte)(1 + rng.NextInt(255));
                var centreAz = bearing + (width * 0.5f) / radial * (180f / MathF.PI);
                if (present)
                {
                    var frame = Frame(centre, centreAz, radial, yaw);
                    Box(mesh, frame, 0f, width, depth, ring.BaseY, height, ring.Parallax, haze, buildingSeed);
                    if (crown)
                        Box(mesh, frame, crownShift, crownW, crownD, height, height + crownH, ring.Parallax, haze, 0);
                }
                az += (width + gap) / radial * (180f / MathF.PI);
            }
        }
        return mesh.Bundle(DioramaPalette.City);
    }

    private readonly record struct BoxFrame(Vector3 Centre, Vector3 Side, Vector3 Inward);

    private static BoxFrame Frame(Vector2 centre, float bearing, float radius, float yawDeg)
    {
        var heading = Angles.Heading(bearing);
        var dir = new Vector3(heading.X, 0f, heading.Y);
        var c = new Vector3(centre.X, 0f, centre.Y) + dir * radius;
        var y = yawDeg * (MathF.PI / 180f);
        var inward = -dir;
        var side = new Vector3(-inward.Z, 0f, inward.X);
        var cy = MathF.Cos(y);
        var sy = MathF.Sin(y);
        var inwardR = Vector3.Normalize(inward * cy + side * sy);
        var sideR = Vector3.Normalize(side * cy - inward * sy);
        return new(c, sideR, inwardR);
    }

    /// <summary>Front, two sides and roof of one mass; shift moves it along the side axis.</summary>
    private static void Box(MeshWriter mesh, BoxFrame f, float shift, float width, float depth, float y0, float y1, float k, float haze, byte seed)
    {
        var c = f.Centre + f.Side * shift;
        var hw = width * 0.5f;
        var hd = depth * 0.5f;
        var fl = c + f.Inward * hd - f.Side * hw;   // front-left
        var fr = c + f.Inward * hd + f.Side * hw;   // front-right
        var bl = c - f.Inward * hd - f.Side * hw;   // back-left
        var br = c - f.Inward * hd + f.Side * hw;   // back-right
        var uv2 = new Vector2(k, haze);
        Wall(mesh, fl, fr, f.Inward, y0, y1, width, uv2, seed);
        Wall(mesh, bl, fl, -f.Side, y0, y1, depth, uv2, seed);
        Wall(mesh, fr, br, f.Side, y0, y1, depth, uv2, seed);
        var up = Vector3.UnitY;
        var a = mesh.Vertex(At(fl, y1), up, new(0f, y1), uv2, seed);
        var b = mesh.Vertex(At(fr, y1), up, new(width, y1), uv2, seed);
        var cc = mesh.Vertex(At(br, y1), up, new(width, y1), uv2, seed);
        var d = mesh.Vertex(At(bl, y1), up, new(0f, y1), uv2, seed);
        mesh.Quad(a, b, cc, d);
    }

    private static void Wall(MeshWriter mesh, Vector3 left, Vector3 right, Vector3 normal, float y0, float y1, float length, Vector2 uv2, byte seed)
    {
        var a = mesh.Vertex(At(left, y0), normal, new(0f, y0), uv2, seed);
        var b = mesh.Vertex(At(right, y0), normal, new(length, y0), uv2, seed);
        var c = mesh.Vertex(At(right, y1), normal, new(length, y1), uv2, seed);
        var d = mesh.Vertex(At(left, y1), normal, new(0f, y1), uv2, seed);
        mesh.Quad(a, b, c, d);
    }

    private static Vector3 At(Vector3 p, float y) => new(p.X, y, p.Z);

    /// <summary>Sector parameters at a bearing, blended over a few degrees across sector edges so no seam shows.</summary>
    public static SectorFill Fill(IReadOnlyList<SectorData> sectors, float bearing, int ringIndex)
    {
        float total = 0f, fill = 0f, tower = 0f, lowMin = 0f, lowMax = 0f, highMin = 0f, highMax = 0f;
        foreach (var s in sectors)
        {
            var span = Angles.WrapDegrees(s.To - s.From);
            var into = Angles.WrapDegrees(bearing - s.From);
            var outside = into <= span ? 0f : MathF.Min(into - span, 360f - into);
            var w = 1f - Smoothstep(0f, SectorBlendDeg, outside);
            if (w <= 0f) continue;
            var f = ringIndex == 0 ? s.Near : s.Mid;
            total += w;
            fill += w * f.Fill; tower += w * f.TowerShare;
            lowMin += w * f.Low.Min; lowMax += w * f.Low.Max; highMin += w * f.High.Min; highMax += w * f.High.Max;
        }
        if (total <= 0f) return new(0f, 0f, (1f, 1f), (1f, 1f));
        var inv = 1f / total;
        return new(fill * inv, tower * inv, (lowMin * inv, lowMax * inv), (highMin * inv, highMax * inv));
    }
}
