using Campus.Hud.I18n;
using Campus.Hud.Motion;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// 组件工厂：面板、过孔、文字、图标、线、按钮。每件都挂 ui_panel 的对应 kind，并写 instance uniform。
/// 常驻件（pin = true）登记在这里，设置"常驻件：随主题"时统一改写 pin_night（UI-VISUAL §3.6、§11.6）。
/// </summary>
public sealed class UiKit
{
    public const string IconDir = "res://assets/hud/icons/";

    private readonly Dictionary<string, Texture2D> _icons = new(StringComparer.Ordinal);
    private readonly List<WeakReference<CanvasItem>> _pinned = [];
    private bool _residentFollows;

    public UiKit(UiTokens tokens, UiColors colors, UiMaterials materials, UiThemeBuilder theme, MotionPolicy motion)
    {
        Tokens = tokens;
        Colors = colors;
        Materials = materials;
        Theme = theme;
        Motion = motion;
    }

    public UiTokens Tokens { get; }
    public UiColors Colors { get; }
    public UiMaterials Materials { get; }
    public UiThemeBuilder Theme { get; }
    public MotionPolicy Motion { get; }

    public static string T(string key) => Tr.T(key);
    public static string T(string key, params (string Name, string Value)[] args) => Tr.T(key, args);

    public Texture2D Icon(string name)
    {
        if (_icons.TryGetValue(name, out var texture)) return texture;
        texture = GD.Load<Texture2D>(IconDir + name + ".svg") ?? throw new FileNotFoundException("Missing UI icon.", name);
        _icons[name] = texture;
        return texture;
    }

    /// <summary>写 instance uniform；常驻件按当前设置决定是否固定深屏。</summary>
    public void Apply(CanvasItem item, UiFx fx, bool remember = true)
    {
        if (fx.Pin && remember) Remember(item);
        if (!Materials.Available) return;
        (fx with { Pin = fx.Pin && !_residentFollows }).Write(item, Tokens);
    }

    private void Remember(CanvasItem item)
    {
        // 常驻件随进出开始页重建，顺手剔掉已经释放的旧件
        for (var i = _pinned.Count - 1; i >= 0; i--)
        {
            if (!_pinned[i].TryGetTarget(out var existing) || !GodotObject.IsInstanceValid(existing)) _pinned.RemoveAt(i);
            else if (existing == item) return;
        }
        _pinned.Add(new WeakReference<CanvasItem>(item));
    }

    /// <summary>"常驻件：深屏 / 随主题"（UI-NAV §4.6）。</summary>
    public void SetResidentFollows(bool follows)
    {
        if (_residentFollows == follows) return;
        _residentFollows = follows;
        if (!Materials.Available) return;
        _pinned.RemoveAll(weak => !weak.TryGetTarget(out var item) || !GodotObject.IsInstanceValid(item));
        foreach (var weak in _pinned)
            if (weak.TryGetTarget(out var item)) item.SetInstanceShaderParameter(UiFx.PPin, follows ? 0f : 1f);
    }

    public bool ResidentFollows => _residentFollows;

    // ---------------- 件 ----------------
    public Panel Box(Control parent, Rect2 rect, UiFx fx)
    {
        var panel = new Panel
        {
            Position = rect.Position,
            Size = rect.Size,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ThemeTypeVariation = Theme.PanelType(fx.Margin),
        };
        if (Materials.Panel is { } m) panel.Material = m;
        parent.AddChild(panel);
        Apply(panel, fx);
        return panel;
    }

    public Panel Dot(Control parent, Vector2 center, float diameter, UiFx fx) =>
        Box(parent, new Rect2(center - new Vector2(diameter, diameter) / 2, new Vector2(diameter, diameter)), fx with { Flags = fx.Flags | UiFlags.Circle });

    public Label Text(Control parent, string text, string typeRole, string colorRole, Vector2 position, bool pin = false,
        float width = 0, HorizontalAlignment align = HorizontalAlignment.Left, bool wrap = false)
    {
        var label = new Label
        {
            Text = Theme.Upper(typeRole) ? text.ToUpperInvariant() : text,
            ThemeTypeVariation = UiThemeBuilder.TextType(typeRole),
            Position = position,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = align,
        };
        if (wrap) { label.AutowrapMode = TextServer.AutowrapMode.WordSmart; label.CustomMinimumSize = new Vector2(width, 0); }
        if (Materials.Text is { } m) label.Material = m;
        parent.AddChild(label);
        if (width > 0) label.Size = new Vector2(width, label.GetCombinedMinimumSize().Y);
        Apply(label, new UiFx(Role: colorRole, Pin: pin));
        return label;
    }

    public void Recolor(CanvasItem item, string colorRole, bool pin = false) => Apply(item, new UiFx(Role: colorRole, Pin: pin));

    public TextureRect IconRect(Control parent, string icon, Rect2 rect, string colorRole, bool pin = false)
    {
        var t = new TextureRect
        {
            Texture = Icon(icon),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Position = rect.Position,
            Size = rect.Size,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        if (Materials.Icon is { } m) t.Material = m;
        parent.AddChild(t);
        Apply(t, new UiFx(Role: colorRole, Pin: pin, Extra: new Vector4(1, 0, 0, 0)));
        return t;
    }

    /// <summary>1 物理像素的线（Line2D 只作载体，宽度 3 逻辑像素留给抗锯齿）；extra = (线宽, 虚线实长, 空长, 0)。</summary>
    public Line2D Line(Control parent, Vector2[] points, string colorRole, bool pin = false, Vector4? extra = null, int flags = 0, float width = 3)
    {
        var line = new Line2D
        {
            Points = points,
            Width = width,
            TextureMode = Line2D.LineTextureMode.Stretch,
            JointMode = Line2D.LineJointMode.Sharp,
            Antialiased = false,
            DefaultColor = Godot.Colors.White,
        };
        if (Materials.Line is { } m) line.Material = m;
        parent.AddChild(line);
        Apply(line, new UiFx(Role: colorRole, Pin: pin, Flags: flags, Extra: extra ?? new Vector4(1, 0, 0, 0)));
        return line;
    }

    /// <summary>M7 示波器（UI-VISUAL §9.3）：extra = (振幅 ≤ 0.4, 周期数, 相速, 线宽)。</summary>
    public ColorRect Wave(Control parent, Rect2 rect, string colorRole, bool pin, Vector4 extra)
    {
        var wave = new ColorRect { Position = rect.Position, Size = rect.Size, Color = Godot.Colors.White, MouseFilter = Control.MouseFilterEnum.Ignore };
        if (Materials.Line is { } m) wave.Material = m;
        else wave.Visible = false;
        parent.AddChild(wave);
        Apply(wave, new UiFx(Role: colorRole, Pin: pin, Flags: UiFlags.Wave, Extra: extra));
        return wave;
    }

    public TechButton Button(Control parent, ButtonStyle style, string text, Rect2 rect, string fontRole = "body", int padX = 16,
        string? icon = null, int iconMax = 24, bool pin = false, int padY = 4, HorizontalAlignment align = HorizontalAlignment.Center,
        HorizontalAlignment iconAlign = HorizontalAlignment.Left, Action? pressed = null)
    {
        var button = TechButton.Create(this, style, text, icon, fontRole, padX, padY, iconMax, pin);
        button.Position = rect.Position;
        button.Size = rect.Size;
        button.Alignment = align;
        button.IconAlignment = iconAlign;
        if (pressed is not null) button.Pressed += pressed;
        parent.AddChild(button);
        if (pin) Remember(button);
        return button;
    }

    public float Width(Label label) => label.GetCombinedMinimumSize().X;

    /// <summary>
    /// 子树里占着实例参数槽的件数：挂着界面材质、没在排队释放的 CanvasItem（含滚动条这类内部子节点）。
    /// 引擎在材质挂上时就从全局参数池分配槽，隐藏、摘材质、清参数、移出场景树都不归还，只有释放节点才归还；
    /// 所以隐藏的大页不留件，打开时再建（UI-VISUAL §11.5）。
    /// </summary>
    public int Holders(Node root)
    {
        if (root.IsQueuedForDeletion()) return 0;
        var count = root is CanvasItem item && Materials.Owns(item.Material) ? 1 : 0;
        foreach (var child in root.GetChildren(includeInternal: true)) count += Holders(child);
        return count;
    }
}
