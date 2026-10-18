using Campus.Contracts;
using Campus.Hud.I18n;
using Campus.Hud.Motion;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// 组件工厂：面板、过孔、文字、图标、线、按钮。每件都挂 ui_panel 的对应 kind，并写 instance uniform。
/// 常驻件（pin = true）登记在这里，设置"常驻件：随主题"时统一改写 pin_night（UI-VISUAL）。
/// </summary>
public sealed class UiKit
{
    public const string IconDir = "res://assets/hud/icons/";

    private readonly Dictionary<string, Texture2D> _icons = new(StringComparer.Ordinal);
    private readonly List<WeakReference<CanvasItem>> _pinned = [];
    private bool _residentFollows;

    public UiKit(UiTokens tokens, UiColors colors, UiMaterials materials, UiThemeBuilder theme, MotionPolicy motion, IUiTheme themeState)
    {
        Tokens = tokens;
        Colors = colors;
        Materials = materials;
        Theme = theme;
        Motion = motion;
        ThemeState = themeState;
        Layout = new UiLayout(this);
    }

    public UiTokens Tokens { get; }
    public UiColors Colors { get; }
    public UiMaterials Materials { get; }
    public UiThemeBuilder Theme { get; }
    public MotionPolicy Motion { get; }
    public IUiTheme ThemeState { get; }
    public UiLayout Layout { get; }

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

    /// <summary>"常驻件：深屏 / 随主题"（UI-NAV）。</summary>
    public void SetResidentFollows(bool follows)
    {
        if (_residentFollows == follows) return;
        _residentFollows = follows;
        _pinned.RemoveAll(weak => !weak.TryGetTarget(out var item) || !GodotObject.IsInstanceValid(item));
        foreach (var weak in _pinned)
            if (weak.TryGetTarget(out var item)) item.SetInstanceShaderParameter(UiFx.PPin, follows ? 0f : 1f);
    }

    public bool ResidentFollows => _residentFollows;

    // ---------------- 件 ----------------
    /// <summary>容器里的面板载体；布局容器本身不继承这份材质。</summary>
    public PanelContainer Panel(Control parent, UiFx fx)
    {
        var panel = new PanelContainer
        {
            ThemeTypeVariation = UiThemeBuilder.PanelContainerType,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = Materials.Panel,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        parent.AddChild(panel);
        Apply(panel, fx);
        return panel;
    }

    public Panel Box(Control parent, UiFx fx)
    {
        var panel = new Panel
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ThemeTypeVariation = Theme.PanelType(fx.Margin),
            Material = Materials.Panel,
        };
        parent.AddChild(panel);
        Apply(panel, fx);
        return panel;
    }

    public Label Text(Control parent, string text, string typeRole = "body", string colorRole = "text.1", bool pin = false,
        bool wrap = true, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label
        {
            Text = Theme.Upper(typeRole) ? text.ToUpperInvariant() : text,
            ThemeTypeVariation = UiThemeBuilder.TextType(typeRole),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = align,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
            TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Material = Materials.Text,
        };
        void FitText()
        {
            // 自动换行的 Label 需要有限宽度；行高由当前字体得出，不能在首轮容器排版时塌成零。
            var font = label.GetThemeFont("font");
            var fontSize = label.GetThemeFontSize("font_size");
            label.CustomMinimumSize = new Vector2(label.CustomMinimumSize.X, MathF.Ceiling(font.GetHeight(fontSize)));
            if (label.AutowrapMode != TextServer.AutowrapMode.Off && parent.Size.X > 0)
                label.CustomMaximumSize = new Vector2(parent.Size.X, -1);
        }
        label.ThemeChanged += FitText;
        parent.AddChild(label);
        UiLayout.BindResize(parent, label, FitText);
        Apply(label, new UiFx(Role: colorRole, Pin: pin));
        return label;
    }

    public TextureRect IconRect(Control parent, string icon, string colorRole = "text.1", bool pin = false)
    {
        var edge = Layout.Metric("icon");
        var rect = new TextureRect
        {
            Texture = Icon(icon),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(edge, edge),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = Materials.Icon,
        };
        parent.AddChild(rect);
        Apply(rect, new UiFx(Role: colorRole, Pin: pin, Extra: new Vector4(1, 0, 0, 0)));
        return rect;
    }

    public TechButton Button(Control parent, ButtonStyle style, string text, string fontRole = "body", string? icon = null,
        bool pin = false, Action? pressed = null, HorizontalAlignment align = HorizontalAlignment.Center)
    {
        var button = TechButton.Create(this, style, text, icon, fontRole, pin);
        button.Alignment = align;
        if (pressed is not null) button.Pressed += pressed;
        parent.AddChild(button);
        if (pin) Remember(button);
        return button;
    }

    /// <summary>时间环内部的圆形绘制载体；不参与页面布局。</summary>
    public Panel Dot(Control parent, Vector2 center, float diameter, UiFx fx)
    {
        var panel = Box(parent, fx with { Flags = fx.Flags | UiFlags.Circle });
        panel.Position = center - Vector2.One * diameter / 2;
        panel.Size = Vector2.One * diameter;
        return panel;
    }

    public void Recolor(CanvasItem item, string colorRole, bool pin = false) => Apply(item, new UiFx(Role: colorRole, Pin: pin));

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
        line.Material = Materials.Line;
        parent.AddChild(line);
        Apply(line, new UiFx(Role: colorRole, Pin: pin, Flags: flags, Extra: extra ?? new Vector4(1, 0, 0, 0)));
        return line;
    }

    /// <summary>
    /// 子树里占着实例参数槽的件数：挂着界面材质、没在排队释放的 CanvasItem（含滚动条这类内部子节点）。
    /// 引擎在材质挂上时就从全局参数池分配槽，隐藏、摘材质、清参数、移出场景树都不归还，只有释放节点才归还；
    /// 所以隐藏的大页不留件，打开时再建（UI-VISUAL）。
    /// </summary>
    public int Holders(Node root)
    {
        if (root.IsQueuedForDeletion()) return 0;
        var count = root is CanvasItem item && Materials.Owns(item.Material) ? 1 : 0;
        foreach (var child in root.GetChildren(includeInternal: true)) count += Holders(child);
        return count;
    }
}
