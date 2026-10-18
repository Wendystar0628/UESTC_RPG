using System.Numerics;

namespace Campus.Systems.Nightlight.Schedule;

/// <summary>时刻表用到的纯函数：与着色器一侧（lamp_heff、窗灯 smoothstep）同一写法，G2 用同一组输入比对两边。</summary>
public static class ScheduleMath
{
    /// <summary>有效太阳高度 h_eff = h − 6° × 阴度 − 4° × 降水（SYS-WEATHER、SYS-TIME）。</summary>
    public static float EffectiveSun(float sunAltDeg, float overcast, float precip, EffectiveSunSpec spec) =>
        sunAltDeg - spec.OvercastDeg * Math.Clamp(overcast, 0f, 1f) - spec.PrecipDeg * Math.Clamp(precip, 0f, 1f);

    /// <summary>按美术钟点求关键帧曲线：线性插值，最后一个键与第一个键跨 24 h 衔接；只有一个键时为常数。</summary>
    public static float Curve(IReadOnlyList<Vector2> keys, double clock)
    {
        if (keys.Count == 0) return 0f;
        if (keys.Count == 1) return keys[0].Y;
        var c = (float)(((clock % 24.0) + 24.0) % 24.0);
        var last = keys[^1];
        var first = keys[0];
        if (c < first.X || c >= last.X)
        {
            var span = first.X + 24f - last.X;
            var x = c >= last.X ? c - last.X : c + 24f - last.X;
            return span <= 0f ? first.Y : last.Y + (first.Y - last.Y) * (x / span);
        }
        for (var i = 0; i < keys.Count - 1; i++)
        {
            var a = keys[i];
            var b = keys[i + 1];
            if (c >= a.X && c < b.X)
            {
                var span = b.X - a.X;
                return span <= 0f ? b.Y : a.Y + (b.Y - a.Y) * ((c - a.X) / span);
            }
        }
        return last.Y;
    }

    /// <summary>把 32 位哈希映射到 [0, 1)，取高 24 位，与着色器的 hash01 一致。</summary>
    public static float Unit(uint hash) => (hash >> 8) / 16777216f;

    /// <summary>与 lamp.gdshaderinc 的 lamp_hash_u 同一常数（窗阈值、窗冷暖）；toon、fx_glow 都 include 它，G2 比对。</summary>
    public static uint ShaderHash(uint x)
    {
        unchecked
        {
            x ^= x >> 16;
            x *= 2146121005u;
            x ^= x >> 15;
            x *= 2221713035u;
            x ^= x >> 16;
            return x;
        }
    }

    /// <summary>
    /// 窗的亮灯阈值，与 lamp.gdshaderinc 的 lamp_window_threshold 一致：
    /// 同一层相邻 3 个开间阈值相近（灯按房间成团亮灭），fract(均匀 + 独立量) 仍是均匀分布；整栋楼再加 ±0.25 的偏置。
    /// </summary>
    public static float WindowThreshold(int windowId, int baseId, int columns)
    {
        unchecked
        {
            var local = Math.Max(windowId - baseId, 0);
            var c = Math.Max(columns, 1);
            var row = local / c;
            var col = local - row * c;
            var seg = (uint)baseId * 131u + (uint)row * 17u + (uint)(col / 3);
            var t = Unit(ShaderHash((uint)windowId * 2u + 1u)) * 0.3f + Unit(ShaderHash(seg * 2u + 11u));
            t -= MathF.Floor(t);
            var building = Unit(ShaderHash((uint)baseId * 2u + 7u));
            return Math.Clamp(t + 0.5f * (building - 0.5f), 0f, 1f);
        }
    }
}
