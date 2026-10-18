using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Diorama;

/// <summary>
/// The far ring (WLD-DIORAMA far layer, §6): one continuous height field round the whole horizon, low hills everywhere
/// and the snow mountains rising out of them at their bearings, visual height = true elevation × visualScale (§6.1).
/// Columns run along the bearing (finer across the mountains), rows from the front foot (closer, low) to the crest (the
/// silhouette); a skirt drops from the foot far below the fog sea. Smooth normals come from the field, so the lit and
/// shaded slopes and the spurs follow the live sun; snow potential (height share of the tallest peak, plus gullies) and
/// the crest flag go to UV for the shader's snow line and rim (§6.2–§6.4).
/// </summary>
public static class FarRing
{
    private const float MountainMarginDeg = 3f;     // fine columns this far beyond a mountain's outer peak
    private const float MassifShare = 0.58f;        // the shoulder under a mountain's peaks, share of its height
    private const float PeakExponent = 1.12f;       // flank concavity of a peak (1 = straight, >1 sharper)
    private const float HillNoise = 0.22f, HillNoisePeriodDeg = 2.6f, HillDetailPeriodDeg = 0.9f;
    private const float RidgeNoise = 0.07f, RidgeNoisePeriodDeg = 0.7f, RidgeDetail = 0.035f, RidgeDetailPeriodDeg = 0.3f;
    private const float SlopeExponent = 1.5f;       // row height = crest × w^1.5: gentle foot, steep upper slope
    private const float SpurAmp = 0.07f, SpurPeriodMountainDeg = 2.1f, SpurPeriodHillDeg = 5f;
    private const float GullySnow = 0.12f;          // extra snow potential in the gullies between spurs
    private const float FootBlurDeg = 5f;           // profile averaging width at the foot (degrees either side)

    public static GeometryBundle Build(FarRingData far, SnowData snow, Vector2 centre, uint seed)
    {
        var columns = Columns(far);
        var rows = far.Rows;
        var mesh = new MeshWriter(DioramaVertex.KindFar);
        var maxPeak = far.Mountains.Count == 0 ? HillHeight(far, 0f) : far.Mountains.Max(m => MountainHeight(far, m));
        var uv2 = new Vector2(DioramaVertex.PackSeasons(snow.Visibility), DioramaVertex.PackSeasons(snow.Snowline));
        var n = columns.Count;
        // Grid (column, row) with row 0 = foot ... rows - 1 = crest; then one skirt vertex per column.
        var position = new Vector3[n, rows];
        var potential = new float[n, rows];
        var hillSeed = StableHash.Hash32(seed, "diorama.far", "hills");
        var ridgeSeed = StableHash.Hash32(seed, "diorama.far", "ridge");
        var spurSeed = StableHash.Hash32(seed, "diorama.far", "spur");
        var crests = new float[n];
        var weights = new float[n];
        for (var i = 0; i < n; i++) (crests[i], weights[i]) = Crest(far, columns[i], hillSeed, ridgeSeed);
        for (var i = 0; i < n; i++)
        {
            var az = columns[i];
            var b = az * (MathF.PI / 180f);
            var dir = new Vector3(MathF.Sin(b), 0f, -MathF.Cos(b));
            var spurPeriod = SpurPeriodHillDeg + (SpurPeriodMountainDeg - SpurPeriodHillDeg) * weights[i];
            var spur = DioramaNoise.Value(az / spurPeriod, spurSeed, (int)MathF.Round(360f / spurPeriod));
            for (var r = 0; r < rows; r++)
            {
                var w = r / (float)(rows - 1);
                var bell = 4f * w * (1f - w);
                // The crest's detail belongs to the ridge: lower down the slope the profile is a wider and wider average, so
                // ridges converge at the peaks and the foot is broad instead of the peaks' flanks running straight down.
                var crest = Blurred(columns, crests, i, FootBlurDeg * (1f - w));
                var y = crest * MathF.Pow(w, SlopeExponent) * (1f + SpurAmp * bell * spur);
                var radius = far.Radius - far.Depth * (1f - w);
                position[i, r] = new Vector3(centre.X, 0f, centre.Y) + dir * radius + Vector3.UnitY * y;
                potential[i, r] = Math.Clamp(y / maxPeak + GullySnow * bell * MathF.Max(0f, -spur), 0f, 1f);
            }
        }
        var index = new int[n, rows + 1];
        for (var i = 0; i < n; i++)
        {
            for (var r = 0; r < rows; r++)
            {
                var normal = Normal(position, i, r, n, rows, centre);
                index[i, r] = mesh.Vertex(position[i, r], normal, new(potential[i, r], r == rows - 1 ? 1f : 0f), uv2);
            }
            var foot = position[i, 0];
            var inward = Vector3.Normalize(new Vector3(centre.X - foot.X, 0f, centre.Y - foot.Z));
            index[i, rows] = mesh.Vertex(new(foot.X, far.SkirtY, foot.Z), inward, new(0f, 0f), uv2);
        }
        for (var i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            mesh.Quad(index[i, rows], index[j, rows], index[j, 0], index[i, 0]);
            for (var r = 0; r < rows - 1; r++)
                mesh.Quad(index[i, r], index[j, r], index[j, r + 1], index[i, r + 1]);
        }
        return mesh.Bundle(DioramaPalette.Far);
    }

    /// <summary>Mean crest over ±radius degrees (box window over the non-uniform columns, wrapping round the ring).</summary>
    private static float Blurred(List<float> columns, float[] crests, int i, float radius)
    {
        if (radius <= 0.01f) return crests[i];
        var n = columns.Count;
        var sum = crests[i];
        var count = 1;
        for (var k = 1; k < n; k++)
        {
            var j = (i + k) % n;
            if (MathF.Abs(Delta(columns[j], columns[i])) > radius) break;
            sum += crests[j]; count++;
        }
        for (var k = 1; k < n; k++)
        {
            var j = (i - k + n) % n;
            if (MathF.Abs(Delta(columns[j], columns[i])) > radius) break;
            sum += crests[j]; count++;
        }
        return sum / count;
    }

    /// <summary>Column bearings, 0 ≤ b &lt; 360 ascending: coarse steps, fine across the mountains.</summary>
    public static List<float> Columns(FarRingData far)
    {
        var spans = far.Mountains.Select(m =>
        {
            var reach = m.Peaks.Max(p => MathF.Abs(p.OffsetDeg) + p.HalfWidthDeg) + MountainMarginDeg;
            return (From: m.Azimuth - reach, To: m.Azimuth + reach);
        }).ToArray();
        var list = new List<float>();
        var az = 0f;
        while (az < 360f - 1e-3f)
        {
            list.Add(az);
            var fine = spans.Any(s => InSpan(az, s.From, s.To));
            az += fine ? far.FineStepDeg : far.StepDeg;
        }
        return list;

        static bool InSpan(float az, float from, float to)
        {
            var span = to - from;
            var into = CityRing.Wrap(az - from);
            return into <= span;
        }
    }

    public static float HillHeight(FarRingData far, float az)
    {
        var keys = far.Hills.OrderBy(k => CityRing.Wrap(k.Azimuth)).ToArray();
        var b = CityRing.Wrap(az);
        var i = Array.FindLastIndex(keys, k => CityRing.Wrap(k.Azimuth) <= b);
        var a = keys[i < 0 ? keys.Length - 1 : i];
        var c = keys[(i + 1) % keys.Length];
        var span = CityRing.Wrap(c.Azimuth - a.Azimuth);
        var t = span <= 0f ? 0f : CityRing.Wrap(b - a.Azimuth) / span;
        var s = t * t * (3f - 2f * t);
        var elev = a.ElevationDeg + (c.ElevationDeg - a.ElevationDeg) * s;
        return far.Radius * MathF.Tan(elev * (MathF.PI / 180f));
    }

    public static float MountainHeight(FarRingData far, MountainData m) =>
        far.Radius * MathF.Tan(m.TrueElevationDeg * far.VisualScale * (MathF.PI / 180f));

    /// <summary>Silhouette height at a bearing and how much of it is snow mountain (0 hills … 1 mountain).</summary>
    public static (float Height, float MountainWeight) Crest(FarRingData far, float az, uint hillSeed, uint ridgeSeed)
    {
        var hill = HillHeight(far, az);
        var wrapHill = (int)MathF.Round(360f / HillNoisePeriodDeg);
        var wrapDetail = (int)MathF.Round(360f / HillDetailPeriodDeg);
        hill *= 1f + HillNoise * (0.7f * DioramaNoise.Value(az / HillNoisePeriodDeg, hillSeed, wrapHill)
            + 0.3f * DioramaNoise.Value(az / HillDetailPeriodDeg, hillSeed ^ 0x5bd1e995u, wrapDetail));
        var mountain = 0f;
        foreach (var m in far.Mountains)
        {
            var h = MountainHeight(far, m);
            var reach = m.Peaks.Max(p => MathF.Abs(p.OffsetDeg) + p.HalfWidthDeg);
            var dm = MathF.Abs(Delta(az, m.Azimuth));
            if (dm > reach + MountainMarginDeg) continue;
            var shoulder = dm < reach ? MassifShare * h * Bump(dm / reach) : 0f;
            var peaks = 0f;
            foreach (var p in m.Peaks)
            {
                var d = MathF.Abs(Delta(az, m.Azimuth + p.OffsetDeg));
                if (d >= p.HalfWidthDeg) continue;
                peaks = MathF.Max(peaks, p.Height * h * MathF.Pow(1f - d / p.HalfWidthDeg, PeakExponent));
            }
            mountain = MathF.Max(mountain, MathF.Max(shoulder, peaks));
        }
        if (mountain > 0f)
            mountain *= 1f + RidgeNoise * DioramaNoise.Value(az / RidgeNoisePeriodDeg, ridgeSeed, (int)MathF.Round(360f / RidgeNoisePeriodDeg))
                + RidgeDetail * DioramaNoise.Value(az / RidgeDetailPeriodDeg, ridgeSeed ^ 0x27d4eb2fu, (int)MathF.Round(360f / RidgeDetailPeriodDeg));
        var height = MathF.Max(hill, mountain);
        var weight = mountain > hill ? Math.Clamp((mountain - hill) / MathF.Max(hill, 1f), 0f, 1f) : 0f;
        return (height, weight);
    }

    private static float Bump(float t)
    {
        var x = Math.Clamp(1f - t, 0f, 1f);
        return x * x * (3f - 2f * x);
    }

    /// <summary>Signed bearing difference a − b in −180…180.</summary>
    private static float Delta(float a, float b)
    {
        var d = CityRing.Wrap(a - b);
        return d > 180f ? d - 360f : d;
    }

    private static Vector3 Normal(Vector3[,] p, int i, int r, int n, int rows, Vector2 centre)
    {
        var along = p[(i + 1) % n, r] - p[(i - 1 + n) % n, r];
        var up = p[i, Math.Min(r + 1, rows - 1)] - p[i, Math.Max(r - 1, 0)];
        var normal = Vector3.Cross(along, up);
        var inward = new Vector3(centre.X - p[i, r].X, 0f, centre.Y - p[i, r].Z);
        if (Vector3.Dot(normal, inward) < 0f) normal = -normal;
        var len = normal.Length();
        return len > 1e-6f ? normal / len : Vector3.Normalize(inward);
    }
}
