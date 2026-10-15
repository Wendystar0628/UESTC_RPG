using Godot;

namespace Campus.Hud.Style;

/// <summary>ui_panel 的 flags 位（与着色器里的 F_* 一一对应）。</summary>
public static class UiFlags
{
    public const int Highlight = 1, Ambient = 2, Float = 4, Dashed = 8, Primary = 16, SelectedIris = 32,
        Via = 64, ViaOn = 128, Glow = 256, Circle = 512, CardHover = 1024, Wave = 2048, RowBar = 4096,
        HoverSelected = 8192, Solid = 16384;
}

/// <summary>
/// 一个控件的 instance uniform（UI-VISUAL §11.5）：底色、边、内容色、flags、切角、外扩、悬停、焦点、流光、量化、常驻固定深屏、附加参数。
/// 色只写色名（ui.json 的主题令牌），下标由 <see cref="UiTokens.Role"/> 换算。
/// </summary>
public record struct UiFx(
    string? Fill = null, string? Line = null, string Role = "text.1", int Flags = 0, float Chamfer = 0, int Margin = 6,
    float Hover = 0, float Focus = 0, float Trace = -1, float Reveal = 1, bool Pin = false, Vector4 Extra = default)
{
    public static readonly StringName PFill = "fill_role", PLine = "line_role", PRole = "role", PFlags = "flags",
        PChamfer = "chamfer", PMargin = "margin", PHover = "hover", PFocus = "focus", PTrace = "trace", PReveal = "reveal",
        PPin = "pin_night", PExtra = "extra";

    public readonly void Write(CanvasItem item, UiTokens tokens)
    {
        item.SetInstanceShaderParameter(PFill, Fill is null ? -1 : tokens.Role(Fill));
        item.SetInstanceShaderParameter(PLine, Line is null ? -1 : tokens.Role(Line));
        item.SetInstanceShaderParameter(PRole, tokens.Role(Role));
        item.SetInstanceShaderParameter(PFlags, Flags);
        item.SetInstanceShaderParameter(PChamfer, Chamfer);
        item.SetInstanceShaderParameter(PMargin, (float)Margin);
        item.SetInstanceShaderParameter(PHover, Hover);
        item.SetInstanceShaderParameter(PFocus, Focus);
        item.SetInstanceShaderParameter(PTrace, Trace);
        item.SetInstanceShaderParameter(PReveal, Reveal);
        item.SetInstanceShaderParameter(PPin, Pin ? 1f : 0f);
        item.SetInstanceShaderParameter(PExtra, Extra);
    }
}
