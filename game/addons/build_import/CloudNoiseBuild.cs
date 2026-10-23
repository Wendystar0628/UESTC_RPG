using Campus.Contracts;
using Campus.Systems.Sky;
using Godot;

namespace Campus.Addons.BuildImport;

/// <summary>
/// Builds the fixed tileable cloud texture (ENG-PERF, SYS-WEATHER) before launch.
/// IntegerNoise and the existing dome/fbm arithmetic preserve the established cloud field;
/// Godot owns mip generation and binary resource serialization.
/// </summary>
[Tool]
public partial class CloudNoiseBuild : RefCounted
{
    private const int Size = CloudNoise.Size;
    private const int CoverageSteps = CloudNoise.CoverageSteps;
    // cloud_noise 的团块频率（每个平铺多少格）。着色器里一个平铺 = 24 km。
    private const int NoiseBodyFreq = 14;      // 云体：格约 1.7 km
    private const int NoiseDetailFreq = 24;    // 圆团块：着色器按 ×1.5（格约 0.67 km）与 ×4（格约 0.25 km）两个尺度取

    private sealed record Result(byte[] Rgba, float[] CoverageThreshold);

    public Error Build()
    {
        var result = Generate(StableHash.Hash32(0u, "sky", "cloud_noise"));
        using var image = Image.CreateFromData(Size, Size, false, Image.Format.Rgba8, result.Rgba);
        var error = image.GenerateMipmaps();
        if (error != Error.Ok) return error;
        image.SetMeta(CloudNoise.ThresholdMetadata, result.CoverageThreshold);
        return ResourceSaver.Save(image, CloudNoise.ResourcePath);
    }

    // The GDScript owner releases this managed RefCounted before the editor shuts down.
    public void Release() => Dispose();

    private static Result Generate(uint seed)
    {
        var r = new float[Size * Size];
        var g = new float[Size * Size];
        var b = new float[Size * Size];
        var a = new float[Size * Size];
        for (var y = 0; y < Size; y++)
        {
            var v = (y + 0.5f) / Size;
            for (var x = 0; x < Size; x++)
            {
                var u = (x + 0.5f) / Size;
                var i = y * Size + x;
                // R：云体。大团块（越大越高），按簇的低频场分布；轮廓 s·(1 − d²/r²)，斜率有界，着色器按它估安全步长
                r[i] = Domes(u, v, NoiseBodyFreq, seed ^ 0x1001u, 0.30f, 0.75f, 0.40f, 1.0f, 0.60f, true)
                    * ScalarMath.Lerp(0.30f, 1.15f, Fbm(u, v, 3, 2, seed ^ 0x3003u));
                // G：层云盖的胞状起伏；B：圆团块（着色器在两个尺度上取它，叠出花椰菜顶与塔肩）
                g[i] = Fbm(u, v, 8, 4, seed ^ 0x4004u);
                b[i] = Domes(u, v, NoiseDetailFreq, seed ^ 0x5005u, 0.45f, 0.95f, 0.55f, 1.0f, 1f, false);
                a[i] = Fbm(u, v, 2, 4, seed ^ 0x6006u);
            }
        }
        Normalize(r);
        Normalize(g);
        Normalize(b);
        Normalize(a);
        var rgba = new byte[Size * Size * 4];
        var histogram = new int[256];
        for (var i = 0; i < r.Length; i++)
        {
            var rb = ToByte(r[i]);
            histogram[rb]++;
            rgba[i * 4] = rb;
            rgba[i * 4 + 1] = ToByte(g[i]);
            rgba[i * 4 + 2] = ToByte(b[i]);
            rgba[i * 4 + 3] = ToByte(a[i]);
        }
        return new Result(rgba, Thresholds(histogram, r.Length));
    }

    private static float[] Thresholds(int[] histogram, int total)
    {
        var table = new float[CoverageSteps];
        table[0] = 1f;
        for (var step = 1; step < CoverageSteps; step++)
        {
            var target = (long)total * step / (CoverageSteps - 1);
            long above = 0;
            var value = 255;
            while (value > 0 && above + histogram[value] < target)
            {
                above += histogram[value];
                value--;
            }
            // 阈值放在这一档的下沿：密度大于它的像素约占 step %
            table[step] = Math.Clamp((value - 0.5f) / 255f, 0f, 1f);
        }
        return table;
    }

    // 一层"圆顶"：每格至多一个特征点（按 presence 的概率出现），半径与强度按哈希取；sizeLinked 时强度随半径走（大团块更高）。
    // 轮廓 = s · (1 − d²/r²)，取各点的最大值，所以团块相接处起褶。半径不超过一格，3×3 邻域就够。
    private static float Domes(float u, float v, int freq, uint salt, float rMin, float rMax, float sMin, float sMax,
        float presence, bool sizeLinked)
    {
        var x = u * freq;
        var y = v * freq;
        var cx = (int)MathF.Floor(x);
        var cy = (int)MathF.Floor(y);
        var best = 0f;
        for (var dj = -1; dj <= 1; dj++)
        {
            for (var di = -1; di <= 1; di++)
            {
                var ix = cx + di;
                var iy = cy + dj;
                var wx = (uint)(((ix % freq) + freq) % freq);
                var wy = (uint)(((iy % freq) + freq) % freq);
                var h = IntegerNoise.Hash3D(wx, wy, salt);
                if (IntegerNoise.Low24(IntegerNoise.Hash3D(h, 0x54u, salt)) > presence) continue;
                var px = ix + IntegerNoise.Low24(h);
                var py = iy + IntegerNoise.Low24(IntegerNoise.Hash3D(h, 0x51u, salt));
                var size = IntegerNoise.Low24(IntegerNoise.Hash3D(h, 0x52u, salt));
                var radius = ScalarMath.Lerp(rMin, rMax, size);
                var strength = ScalarMath.Lerp(sMin, sMax, sizeLinked ? size : IntegerNoise.Low24(IntegerNoise.Hash3D(h, 0x53u, salt)));
                var dx = x - px;
                var dy = y - py;
                var q = (dx * dx + dy * dy) / (radius * radius);
                if (q >= 1f) continue;
                var value = strength * (1f - q);
                if (value > best) best = value;
            }
        }
        return best;
    }

    private static float Fbm(float u, float v, int freq, int octaves, uint salt)
    {
        var sum = 0f;
        var amp = 1f;
        var norm = 0f;
        for (var o = 0; o < octaves; o++)
        {
            sum += amp * IntegerNoise.TileableValue2D(u, v, freq << o, salt + (uint)o * 0x9E37u);
            norm += amp;
            amp *= 0.5f;
        }
        return sum / norm;
    }

    private static void Normalize(float[] values)
    {
        var lo = float.MaxValue;
        var hi = float.MinValue;
        foreach (var value in values)
        {
            lo = Math.Min(lo, value);
            hi = Math.Max(hi, value);
        }
        var span = Math.Max(hi - lo, 1e-6f);
        for (var i = 0; i < values.Length; i++) values[i] = (values[i] - lo) / span;
    }

    private static byte ToByte(float v) => (byte)Math.Clamp((int)MathF.Round(v * 255f), 0, 255);
}
