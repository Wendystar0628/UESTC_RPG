namespace Campus.Gen.Interior.Shell.Cards;

/// <summary>
/// Packs <see cref="WindowCardCatalog"/> into the window_cards global data texture that toon_glass reads by the VF-B
/// window-type byte (WLD-INTERIORS; the global and its uploader are in the 51b contract request). Static data:
/// uploaded once, independent of map, time, season and weather. RGBA32F, <see cref="Width"/> × <see cref="Rows"/>,
/// row = card kind, row 0 unused (unspecified glass falls back to Office in the shader):
/// <list type="bullet">
/// <item>x0 (cumulative weight of 3 m, cumulative weight of 3 + 4.5 m, partition chance, side wall offset m)</item>
/// <item>x1 (near furniture, near plane depth m, far furniture, side walls)</item>
/// <item>x2 (screen, screen chance, coverage min, coverage max)</item>
/// <item>x3 (day luminance, night gain, ceiling, accent)</item>
/// <item>x4 (sill drop m, ceiling rise m, cool share, layer base)</item>
/// <item>x5 (layer count, 0, 0, <see cref="Tag"/>): a default or unset texture never carries the tag</item>
/// </list>
/// Enum fields are stored as their numeric values; the shader's constants mirror the enums in WindowCardKinds.cs.
/// </summary>
public static class WindowCardTable
{
    public const int Width = 6;
    public const int Rows = 32;
    public const float Tag = 51f;

    public static float[] Pack() => Pack(WindowCardCatalog.All);

    public static float[] Pack(IReadOnlyList<WindowCardSpec> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        var data = new float[Width * Rows * 4];
        var seen = new bool[Rows];
        foreach (var c in cards)
        {
            var row = (int)c.Kind;
            if (row is <= 0 or >= Rows) throw new ArgumentOutOfRangeException(nameof(cards), $"Card kind {c.Kind} has no table row.");
            if (seen[row]) throw new ArgumentException($"Card kind {c.Kind} appears twice.", nameof(cards));
            seen[row] = true;
            Validate(c);
            var total = c.Depth3 + c.Depth45 + c.Depth6;
            Put(data, row, 0, c.Depth3 / total, (c.Depth3 + c.Depth45) / total, c.Partition, c.SideExtra);
            Put(data, row, 1, (float)c.Near, c.NearDepth, (float)c.Far, (float)c.Side);
            Put(data, row, 2, (float)c.Screen, c.ScreenChance, c.CoverMin, c.CoverMax);
            Put(data, row, 3, c.DayLum, c.NightGain, (float)c.Ceiling, (float)c.Accent);
            Put(data, row, 4, c.SillDrop, c.CeilingRise, c.CoolShare, c.LayerBase);
            Put(data, row, 5, c.LayerCount, 0f, 0f, Tag);
        }
        return data;
    }

    private static void Validate(WindowCardSpec c)
    {
        float[] values = [c.Depth3, c.Depth45, c.Depth6, c.Partition, c.SideExtra, c.SillDrop, c.CeilingRise, c.NearDepth,
            c.ScreenChance, c.CoverMin, c.CoverMax, c.DayLum, c.NightGain, c.CoolShare];
        if (values.Any(v => !float.IsFinite(v) || v < 0f)) throw new ArgumentException($"{c.Kind}: values must be finite and non-negative.");
        if (c.Depth3 + c.Depth45 + c.Depth6 <= 0f) throw new ArgumentException($"{c.Kind}: no parallax depth has weight.");
        if (c.Partition > 1f || c.ScreenChance > 1f || c.CoolShare > 1f || c.CoverMin > c.CoverMax || c.CoverMax > 1f)
            throw new ArgumentException($"{c.Kind}: chances and coverages must lie in [0, 1] with min ≤ max.");
        if (c.Screen == CardScreen.None && c.ScreenChance > 0f) throw new ArgumentException($"{c.Kind}: screen chance without a screen.");
        if (c.Near != CardFurniture.None && (c.NearDepth < 0.5f || c.NearDepth > 3f))
            throw new ArgumentException($"{c.Kind}: the near plane must sit 0.5–3 m behind the glass (shallowest room is 3 m).");
        if (c.SillDrop > 3.5f || c.CeilingRise > 3f) throw new ArgumentException($"{c.Kind}: room height out of range.");
        if (c.DayLum > 1.5f || c.NightGain > 1.5f) throw new ArgumentException($"{c.Kind}: luminance out of range.");
    }

    private static void Put(float[] data, int row, int x, float r, float g, float b, float a)
    {
        var i = (row * Width + x) * 4;
        data[i] = r;
        data[i + 1] = g;
        data[i + 2] = b;
        data[i + 3] = a;
    }
}
