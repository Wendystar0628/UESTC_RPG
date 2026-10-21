using System.Text.Json;
using Campus.Contracts;
using Godot;

namespace Campus.Hud.Style;

/// <summary>
/// 界面的非色令牌：读 res://hud/style/ui_metrics.json（UI-VISUAL）。尺寸、字号、切角、线宽、影与辉光几何、
/// 动效曲线与时长都只从这里取；色值只在 ui.json，经 <see cref="UiColors"/> 取。
/// </summary>
public sealed class UiTokens
{
    public const string Path = "res://hud/style/ui_metrics.json";

    private readonly JsonElement _root;

    public UiTokens(JsonElement root)
    {
        _root = root;
        var roles = new List<string>();
        foreach (var role in Get("shader", "panelRoles").EnumerateArray()) roles.Add(role.GetString()!);
        PanelRoles = roles.AsReadOnly();
    }

    public static UiTokens Load(IRequiredResources resources)
    {
        var bytes = resources.ReadBytes(Path);
        using var document = JsonDocument.Parse(bytes);
        return new UiTokens(document.RootElement.Clone());
    }

    /// <summary>ui_panel 色表的顺序（= 着色器里的 R_* 常量）。</summary>
    public IReadOnlyList<string> PanelRoles { get; }

    public int Role(string token)
    {
        for (var i = 0; i < PanelRoles.Count; i++)
            if (PanelRoles[i] == token) return i;
        throw new KeyNotFoundException($"UI role is not in ui_metrics.json shader.panelRoles: {token}");
    }

    public JsonElement Get(params string[] path)
    {
        var element = _root;
        foreach (var key in path)
            if (!element.TryGetProperty(key, out element)) throw new KeyNotFoundException("ui_metrics.json: " + string.Join('.', path));
        return element;
    }

    public float F(params string[] path) => Get(path).GetSingle();
    public int I(params string[] path) => Get(path).GetInt32();
    public string S(params string[] path) => Get(path).GetString()!;

    public Vector2 V2(params string[] path)
    {
        var e = Get(path);
        return new Vector2(e[0].GetSingle(), e[1].GetSingle());
    }

    public bool TryGet(out JsonElement value, params string[] path)
    {
        value = _root;
        foreach (var key in path)
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(key, out value)) return false;
        return true;
    }

    // ---- 常用的几项，按名字取，免得调用方拼路径 ----
    public float Chamfer(string kind) => F("metrics", "shape", "chamfer", kind);
    public float Space(string name) => F("metrics", "space", name);
    public float Size(string name) => F("metrics", "size", name);
    public float Ms(params string[] path) => F(["motion", "durations", .. path]) / 1000f;

    /// <summary>shadow.* 的几何（x, y, blur）；该主题不画时为 0。</summary>
    public Vector4 Shadow(string token, string theme)
    {
        var e = Get("metrics", "shadow", token, theme);
        return e.ValueKind == JsonValueKind.Null
            ? Vector4.Zero
            : new Vector4(e.GetProperty("x").GetSingle(), e.GetProperty("y").GetSingle(), e.GetProperty("blur").GetSingle(), 0);
    }

    /// <summary>贝塞尔曲线（x1, y1, x2, y2），名字取 Enter、Exit、Standard。</summary>
    public Vector4 Curve(string name)
    {
        var e = Get("motion", "curves", name);
        return new Vector4(e[0].GetSingle(), e[1].GetSingle(), e[2].GetSingle(), e[3].GetSingle());
    }
}
