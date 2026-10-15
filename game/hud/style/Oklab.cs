using Godot;

namespace Campus.Hud.Style;

/// <summary>OKLab 换算：只给"再现场景"的颜色降彩度用（时间环的天色光谱乘 0.7，UI-VISUAL §2.2、§7.5）。</summary>
public static class Oklab
{
    private static float Lin(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    private static float Enc(float c) => c <= 0.0031308f ? 12.92f * c : 1.055f * MathF.Pow(MathF.Max(c, 0), 1 / 2.4f) - 0.055f;

    public static Color ScaleChroma(Color srgb, float k)
    {
        float r = Lin(srgb.R), g = Lin(srgb.G), b = Lin(srgb.B);
        var l = MathF.Cbrt(0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b);
        var m = MathF.Cbrt(0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b);
        var s = MathF.Cbrt(0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b);
        var L = 0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s;
        var A = (1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s) * k;
        var B = (0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s) * k;
        var l2 = L + 0.3963377774f * A + 0.2158037573f * B;
        var m2 = L - 0.1055613458f * A - 0.0638541728f * B;
        var s2 = L - 0.0894841775f * A - 1.2914855480f * B;
        l2 *= l2 * l2; m2 *= m2 * m2; s2 *= s2 * s2;
        return new Color(
            Math.Clamp(Enc(4.0767416621f * l2 - 3.3077115913f * m2 + 0.2309699292f * s2), 0, 1),
            Math.Clamp(Enc(-1.2684380046f * l2 + 2.6097574011f * m2 - 0.3413193965f * s2), 0, 1),
            Math.Clamp(Enc(-0.0041960863f * l2 - 0.7034186147f * m2 + 1.7076147010f * s2), 0, 1), srgb.A);
    }
}
