using Campus.Contracts;
using Godot;

namespace Campus.Hud.Style;

/// <summary>
/// 经 IShaderMaterialSource 向 Campus.Style 的材质库申请 ui_panel、ui_screen（UI-VISUAL），只写 uniform：色表 pal_day / pal_night、
/// 数值令牌、kind / mode。逐控件的效果一律写 instance uniform（见 <see cref="UiFx"/>）。
/// </summary>
public sealed class UiMaterials
{
    public const int KindPanel = 0, KindLine = 1, KindIcon = 2, KindText = 3;

    private readonly ShaderMaterial[] _panel = new ShaderMaterial[4];

    public UiMaterials(UiTokens tokens, UiColors colors, IShaderMaterialSource source)
    {
        var day = colors.RoleTable(tokens, "light");
        var night = colors.RoleTable(tokens, "dark");
        for (var kind = 0; kind < _panel.Length; kind++)
        {
            var m = source.Create(ShaderId.UiPanel);
            m.SetShaderParameter("kind", kind);
            m.SetShaderParameter("pal_day", day);
            m.SetShaderParameter("pal_night", night);
            WritePanelNumbers(m, tokens);
            _panel[kind] = m;
        }
        Reveal = source.Create(ShaderId.UiScreen);
        Frost = source.Create(ShaderId.UiScreen);
        foreach (var (m, mode) in new[] { (Reveal, 0), (Frost, 1) })
        {
            m.SetShaderParameter("mode", mode);
            var front = tokens.S("shader", "screenFront");
            m.SetShaderParameter("front_day", colors.Theme("light", front));
            m.SetShaderParameter("front_night", colors.Theme("dark", front));
            m.SetShaderParameter("front_delta", tokens.F("motion", "limits", "frontDeltaMax"));
            m.SetShaderParameter("fade_band", tokens.F("motion", "durations", "scan", "fadeBand"));
            m.SetShaderParameter("grid_geo", new Vector2(tokens.F("motion", "durations", "grid", "cell"), tokens.F("motion", "durations", "grid", "alpha")));
            m.SetShaderParameter("grid_wave", tokens.F("motion", "durations", "grid", "lastStart"));
            m.SetShaderParameter("frost_lod", tokens.F("metrics", "material", "frost", "lod"));
            m.SetShaderParameter("corner_minor", tokens.F("metrics", "shape", "cornerMinor"));
            m.SetShaderParameter("front_width", tokens.F("themeSwitch", "frontWidth"));
        }
    }

    public ShaderMaterial Panel => _panel[KindPanel];
    public ShaderMaterial Line => _panel[KindLine];
    public ShaderMaterial Icon => _panel[KindIcon];
    public ShaderMaterial Text => _panel[KindText];
    public ShaderMaterial Reveal { get; }
    public ShaderMaterial Frost { get; }

    /// <summary>是不是这里发出的界面材质（ui_panel 四种 kind 与 ui_screen 两种 mode）。</summary>
    public bool Owns(Material? material) =>
        material is not null && (Array.IndexOf(_panel, material) >= 0 || material == Reveal || material == Frost);

    private static void WritePanelNumbers(ShaderMaterial m, UiTokens t)
    {
        m.SetShaderParameter("corner_minor", t.F("metrics", "shape", "cornerMinor"));
        m.SetShaderParameter("focus_geo", new Vector4(t.F("metrics", "focus", "offset"), t.F("metrics", "focus", "tightenFrom"),
            t.F("metrics", "focus", "irisPhys"), t.F("metrics", "focus", "irisMax")));
        m.SetShaderParameter("glow_geo", new Vector2(t.F("metrics", "glow", "radius"), t.F("metrics", "glow", "max")));
        m.SetShaderParameter("ambient_geo", t.Shadow("shadow.ambient", "light"));
        m.SetShaderParameter("float_geo_day", t.Shadow("shadow.float", "light"));
        m.SetShaderParameter("float_geo_night", t.Shadow("shadow.float", "dark"));
        m.SetShaderParameter("contact_geo", t.Shadow("shadow.float.near", "light"));
        m.SetShaderParameter("dash_geo", t.V2("metrics", "line", "dash"));
        m.SetShaderParameter("trace_geo", new Vector2(t.F("motion", "durations", "trace", "head"), t.F("motion", "durations", "trace", "tail")));
        m.SetShaderParameter("front_width", t.F("themeSwitch", "frontWidth"));
        m.SetShaderParameter("front_delta", t.F("themeSwitch", "frontDelta"));
    }
}
