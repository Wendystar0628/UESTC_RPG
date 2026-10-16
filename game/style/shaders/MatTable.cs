using Campus.Contracts;

namespace Campus.Style.Materials;

/// <summary>
/// 全局数据纹理 mat_table（ENG-PERF §1.5）：128×4 RGBA8、最近邻，按材质 ID 取列。WP-04 持有，WP-05 共读。
/// <list type="bullet">
/// <item>行 0 线表（WP-05 的布局）：R 线宽系数（值/255×2）、G 线色染色强度、B 标志位（1 擦除、2 只描外轮廓、4 内部软线、8 不描）、A 小件淡出终点（值×2 m）</item>
/// <item>行 1：R 明暗档（ShadeSteps 原值）、G half_level、B 过渡带（两阶与三阶存固定半宽×1000，两阶半与软物存 fwidth 倍数×50）、A 湿润响应</item>
/// <item>行 2：R 透光 k_t、G 受光侧暖边 k_rim、B 闪点系数、A 自发光通道（灯光通道槽位 + 1；0 = 不自发光）</item>
/// <item>行 3：R 解析法线（0 不用、1 球、2 柱）、G 倒角提亮的受光幅度、B 影里幅度（值/255×0.25 L）、A 硬卡通高光强度（值/255）</item>
/// </list>
/// toon.gdshaderinc 的 toon_mat() 按同一布局解码。
/// </summary>
public static class MatTable
{
    public const int Width = 128;
    public const int Height = 4;

    // 可调初值（ART-STYLE §2.1、§2.6、§5.3；ENG-PERF §2）
    public const float HalfLevel = 0.8f;          // 两阶半的半受光档
    public const float BandTwoHalf = 1.0f;        // 两阶半：fwidth × 1
    public const float BandSoft = 2.2f;           // 软物：fwidth × 2.2
    public const float RimDefault = 0.2f;         // 受光侧暖边
    public const float RimMetal = 0.35f;          // 金属（带各向异性亮带的材质）
    public const float BevelLitL = 0.05f;         // 倒角带受光提亮（OKLab L）
    public const float BevelShadeL = 0.02f;       // 倒角带影里提亮
    public const float SpecMetal = 1.0f;          // 硬卡通高光强度：金属件（带各向异性亮带的材质，max-110 110a）
    public const float LineWidth = 1.0f;
    public const float LineTint = 0.22f;
    public const float FineFadeEndMeters = 60f;   // 小件线淡出的终点（ART-STYLE §4.1）
    public const int LastEmissiveSlot = 29;       // 可作自发光的最后一个灯光通道槽位：30 sys.cue、31 sys.poolGain 不是亮度

    public const int ByteCount = Width * Height * 4;

    /// <summary>把一个材质的 4 行写进 RGBA8 字节数组（逐字节写，不经浮点颜色换算）。</summary>
    public static void WriteRows(byte[] bytes, byte id, ToonSpec spec)
    {
        if (bytes.Length != ByteCount) throw new ArgumentException("mat_table buffer must be 128×4 RGBA8.", nameof(bytes));
        Put(bytes, id, 0, Row0(spec));
        Put(bytes, id, 1, Row1(spec));
        Put(bytes, id, 2, Row2(spec));
        Put(bytes, id, 3, Row3(spec));
    }

    private static void Put(byte[] bytes, int x, int y, (int R, int G, int B, int A) v)
    {
        var i = (y * Width + x) * 4;
        bytes[i] = (byte)v.R;
        bytes[i + 1] = (byte)v.G;
        bytes[i + 2] = (byte)v.B;
        bytes[i + 3] = (byte)v.A;
    }

    private static (int, int, int, int) Row0(ToonSpec spec)
    {
        var (flags, fade) = spec.Line switch
        {
            LineClass.Building => (0, 0f),
            LineClass.Tree => (spec.Kind == ToonKind.VegCard ? 2 : 4, 0f),   // 插片树冠只描外轮廓，云团树冠内部软线
            LineClass.Fine => (0, FineFadeEndMeters),
            LineClass.None => (8, 0f),
            _ => throw new ArgumentOutOfRangeException(nameof(spec), spec.Line, "Unknown LineClass.")
        };
        return Bytes(Byte(LineWidth / 2f), Byte(LineTint), flags, Math.Clamp((int)MathF.Round(fade / 2f), 0, 255));
    }

    private static (int, int, int, int) Row1(ToonSpec spec)
    {
        var (half, band) = spec.Steps switch
        {
            ShadeSteps.Two => (1f, spec.Band * 1000f),
            ShadeSteps.Three => (1f, spec.Band * 1000f),
            ShadeSteps.TwoHalf => (HalfLevel, BandTwoHalf * 50f),
            ShadeSteps.Soft => (HalfLevel, BandSoft * 50f),
            _ => throw new ArgumentOutOfRangeException(nameof(spec), spec.Steps, "Unknown ShadeSteps.")
        };
        return Bytes((int)spec.Steps, Byte(half), Math.Clamp((int)MathF.Round(band), 0, 255), Byte(spec.Wet));
    }

    private static (int, int, int, int) Row2(ToonSpec spec)
    {
        var rim = spec.Fx.HasFlag(ToonFx.Rim) ? (spec.Fx.HasFlag(ToonFx.AnisoBand) ? RimMetal : RimDefault) : 0f;
        return Bytes(Byte(spec.Translucency), Byte(rim), 0, EmissiveByte(spec.Emissive));
    }

    /// <summary>
    /// 自发光通道：槽位 + 1，0 表示不自发光。只收灯光通道的窗灯与灯具槽位（ENG-PERF §10.3）；
    /// sys.cue、sys.poolGain 不是亮度，未登记的通道名直接报错。
    /// </summary>
    public static int EmissiveByte(ChannelId? channel)
    {
        if (channel is not { } id) return 0;
        var slot = LightChannelCatalog.Slot(id);
        if (slot < 0) throw new ArgumentException($"Emissive channel '{id.Value}' is not in LightChannelCatalog.", nameof(channel));
        if (slot > LastEmissiveSlot)
            throw new ArgumentException($"Emissive channel '{id.Value}' (slot {slot}) is a system slot, not a light level.", nameof(channel));
        return slot + 1;
    }

    private static (int, int, int, int) Row3(ToonSpec spec)
    {
        var normal = spec.Normal switch
        {
            ToonNormal.Flat or ToonNormal.Smooth or ToonNormal.Spherical => 0,
            ToonNormal.AnalyticSphere => 1,
            ToonNormal.AnalyticCylinder => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(spec), spec.Normal, "Unknown ToonNormal.")
        };
        var bevel = spec.Fx.HasFlag(ToonFx.BevelLift);
        var gloss = spec.Fx.HasFlag(ToonFx.AnisoBand) ? SpecMetal : 0f;
        return Bytes(normal, bevel ? Byte(BevelLitL / 0.25f) : 0, bevel ? Byte(BevelShadeL / 0.25f) : 0, Byte(gloss));
    }

    private static int Byte(float unit) => Math.Clamp((int)MathF.Round(unit * 255f), 0, 255);
    private static (int, int, int, int) Bytes(int r, int g, int b, int a) => (r, g, b, a);
}
