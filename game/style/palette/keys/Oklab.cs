using System.Globalization;

namespace Campus.Style.Palette.Keys;

/// <summary>OKLab colour, double precision (SYS-TIME §3.1). Mirrors pipeline/colorscript/oklab.py.</summary>
public readonly record struct Lab(double L, double A, double B)
{
    public static Lab operator +(Lab x, Lab y) => new(x.L + y.L, x.A + y.A, x.B + y.B);
    public static Lab operator *(Lab x, double k) => new(x.L * k, x.A * k, x.B * k);
    public double Chroma => Math.Sqrt(A * A + B * B);
    public double Hue => Chroma > 1e-9 ? Mod360(Math.Atan2(B, A) * 180.0 / Math.PI) : 0.0;

    public static double Mod360(double h)
    {
        var r = h % 360.0;
        return r < 0 ? r + 360.0 : r;
    }
}

/// <summary>sRGB / OKLab / OKLCH conversions (Björn Ottosson's OKLab, public domain matrices).</summary>
public static class Oklab
{
    public static double SrgbToLinear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    public static double LinearToSrgb(double c) => c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1.0 / 2.4) - 0.055;

    public static Lab FromLinear(double r, double g, double b)
    {
        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);
        return new(0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
            1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
            0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
    }

    public static (double R, double G, double B) ToLinear(Lab c)
    {
        var l = c.L + 0.3963377774 * c.A + 0.2158037573 * c.B;
        var m = c.L - 0.1055613458 * c.A - 0.0638541728 * c.B;
        var s = c.L - 0.0894841775 * c.A - 1.2914855480 * c.B;
        l = l * l * l; m = m * m * m; s = s * s * s;
        return (4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
            -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
            -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s);
    }

    public static Lab FromSrgb(double r, double g, double b) => FromLinear(SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b));

    /// <summary>Parses the data-file colour notation (a hash followed by six hex digits).</summary>
    public static Lab Parse(string text)
    {
        var t = text.Trim();
        if (t.Length != 7 || t[0] != '#') throw new FormatException($"Colour must be #RRGGBB: '{text}'.");
        var v = int.Parse(t.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return FromSrgb(((v >> 16) & 255) / 255.0, ((v >> 8) & 255) / 255.0, (v & 255) / 255.0);
    }

    public static Lab FromLch(double l, double c, double h)
    {
        var r = h * Math.PI / 180.0;
        return new(l, c * Math.Cos(r), c * Math.Sin(r));
    }

    public static double HueDiff(double from, double to)
    {
        var d = (to - from) % 360.0;
        if (d < 0) d += 360.0;
        return d > 180.0 ? d - 360.0 : d;
    }

    public static double Lerp(double a, double b, double t) => a + (b - a) * t;
    public static Lab Lerp(Lab p, Lab q, double t) => new(Lerp(p.L, q.L, t), Lerp(p.A, q.A, t), Lerp(p.B, q.B, t));

    public static Lab LerpLch(Lab p, Lab q, double t)
    {
        var c = Lerp(p.Chroma, q.Chroma, t);
        var h = p.Hue + HueDiff(p.Hue, q.Hue) * t;
        return FromLch(Lerp(p.L, q.L, t), c, h);
    }

    /// <summary>GLSL smoothstep, reversed edges allowed.</summary>
    public static double Smoothstep(double e0, double e1, double x)
    {
        if (e0 == e1) return x < e0 ? 0.0 : 1.0;
        var t = Math.Clamp((x - e0) / (e1 - e0), 0.0, 1.0);
        return t * t * (3.0 - 2.0 * t);
    }

    public static double Smootherstep(double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return t * t * t * (t * (6.0 * t - 15.0) + 10.0);
    }

    private static bool InGamut((double R, double G, double B) c) =>
        c.R >= -1e-7 && c.R <= 1.0000001 && c.G >= -1e-7 && c.G <= 1.0000001 && c.B >= -1e-7 && c.B <= 1.0000001;

    /// <summary>Out-of-gamut colours lose chroma only, never lightness (ART-STYLE §3.2); in-gamut colours come back unchanged.</summary>
    public static Lab GamutMap(Lab c)
    {
        if (InGamut(ToLinear(c))) return c;
        var l = Math.Clamp(c.L, 0.0, 1.0);
        var hue = c.Hue;
        double lo = 0, hi = c.Chroma;
        for (var i = 0; i < 32; i++)
        {
            var mid = 0.5 * (lo + hi);
            if (InGamut(ToLinear(FromLch(l, mid, hue)))) lo = mid; else hi = mid;
        }
        return FromLch(l, lo, hue);
    }

    public static (double R, double G, double B) ToSrgbClipped(Lab c)
    {
        var lin = ToLinear(GamutMap(c));
        return (LinearToSrgb(Math.Clamp(lin.R, 0, 1)), LinearToSrgb(Math.Clamp(lin.G, 0, 1)), LinearToSrgb(Math.Clamp(lin.B, 0, 1)));
    }

    public static byte ToByte(double c) => (byte)Math.Floor(Math.Clamp(c, 0.0, 1.0) * 255.0 + 0.5);

    public static string ToHex(Lab c)
    {
        var (r, g, b) = ToSrgbClipped(c);
        return string.Create(CultureInfo.InvariantCulture, $"#{ToByte(r):X2}{ToByte(g):X2}{ToByte(b):X2}");
    }
}
