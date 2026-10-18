using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>UI-VISUAL-COMPONENTS 的布局骨架。只有 Panel 载体持材质，布局节点不占实例参数槽。</summary>
public sealed class UiLayout(UiKit kit)
{
    public int Metric(string name) => kit.Theme.Theme.GetConstant(name, UiThemeBuilder.LayoutMetrics);

    public T Add<T>(Control parent, T child, string? variation = null) where T : Control
    {
        child.MouseFilter = Control.MouseFilterEnum.Ignore;
        if (variation is not null) child.ThemeTypeVariation = variation;
        parent.AddChild(child);
        return child;
    }

    public Control Host(Control parent, string name)
    {
        var host = Add(parent, new Control { Name = name });
        host.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return host;
    }

    /// <summary>全屏留边后宽度最多为高度的 16:9；窄窗保留可用的全部高度。</summary>
    public Control SafeFrame(Control parent)
    {
        var margin = Add(parent, new MarginContainer { Name = "ScreenMargin", Theme = kit.Theme.Theme }, UiThemeBuilder.ScreenMargin);
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var bounds = Add(margin, new Control { Name = "SafeBounds" });
        var safe = Host(bounds, "SafeFrame");
        BindResize(bounds, safe, () =>
        {
            var ratio = (float)Metric("safeAspectWidth") / Metric("safeAspectHeight");
            var fraction = bounds.Size.X > 0 ? Math.Min(1, bounds.Size.Y * ratio / bounds.Size.X) : 1;
            safe.AnchorLeft = (1 - fraction) / 2;
            safe.AnchorRight = (1 + fraction) / 2;
        });
        return safe;
    }

    public VBoxContainer Corner(Control parent, Control.LayoutPreset corner)
    {
        var box = Add(parent, new VBoxContainer(), UiThemeBuilder.Corners);
        box.SetAnchorsAndOffsetsPreset(corner);
        box.GrowHorizontal = corner is Control.LayoutPreset.TopRight or Control.LayoutPreset.BottomRight
            ? Control.GrowDirection.Begin : Control.GrowDirection.End;
        box.GrowVertical = corner is Control.LayoutPreset.BottomLeft or Control.LayoutPreset.BottomRight
            ? Control.GrowDirection.Begin : Control.GrowDirection.End;
        return box;
    }

    public MarginContainer Margin(Control parent, bool page = false) =>
        Add(parent, new MarginContainer(), page ? UiThemeBuilder.PageMargin : UiThemeBuilder.PanelMargin);

    public VBoxContainer Stack(Control parent, bool groups = true) =>
        Add(parent, new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }, groups ? UiThemeBuilder.Groups : UiThemeBuilder.Rows);

    public HBoxContainer Row(Control parent) =>
        Add(parent, new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }, UiThemeBuilder.Row);

    public HFlowContainer Flow(Control parent, bool tabs = false) =>
        Add(parent, new HFlowContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }, tabs ? UiThemeBuilder.Tabs : UiThemeBuilder.Actions);

    public ScrollContainer Scroll(Control parent)
    {
        // 滚动区域是交互节点，要接滚轮；其内的排版节点仍一律 Ignore。
        var scroll = new ScrollContainer
        {
            MouseFilter = Control.MouseFilterEnum.Pass,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            FollowFocus = true,
        };
        parent.AddChild(scroll);
        var bar = scroll.GetVScrollBar();
        bar.ThemeTypeVariation = "ScrollThin";
        bar.Material = kit.Materials.Text;
        kit.Recolor(bar, "line.scroll");
        return scroll;
    }

    /// <summary>页外壳可做位移动效；页内 Header / Tabs / Body / Footer 由容器管理。</summary>
    public UiPageFrame Page(Control parent, string title, string widthMetric = "settingsWidth", bool centered = false)
    {
        var shell = Host(parent, "PageShell");
        FitWidth(parent, shell, widthMetric, centered);
        var panel = kit.Panel(shell, new UiFx(Fill: "surface.panel", Line: "line.panel", Chamfer: kit.Tokens.Chamfer("page"), Flags: UiFlags.Ambient));
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var stack = Stack(Margin(panel, page: true));
        var header = Row(stack);
        kit.Text(header, title, "h2");
        var tabs = Flow(stack, tabs: true);
        var scroll = Scroll(stack);
        var body = Stack(scroll);
        var footer = Flow(stack);
        return new UiPageFrame(shell, panel, header, tabs, scroll, body, footer);
    }

    public VBoxContainer SettingGroup(Control parent, string title)
    {
        var group = Stack(parent, groups: false);
        kit.Text(group, title, "mono-caps", "text.2");
        return group;
    }

    /// <summary>标签按剩余空间换行，编辑器使用内容最小尺寸；不再固定标签宽。</summary>
    public HBoxContainer SettingRow(Control parent, string label, string? description = null)
    {
        var row = Row(parent);
        var text = Stack(row, groups: false);
        text.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        kit.Text(text, label);
        if (!string.IsNullOrEmpty(description)) kit.Text(text, description, "small", "text.2");
        return row;
    }

    public UiCardGrid Cards(Control parent)
    {
        var grid = Add(parent, new UiCardGrid { CardMinimumWidth = Metric("cardMinWidth"), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }, UiThemeBuilder.CardGrid);
        return grid;
    }

    public VBoxContainer Card(Control parent, bool pin = false) =>
        Stack(Margin(kit.Panel(parent, new UiFx(Fill: "surface.panel", Line: "line.panel", Chamfer: kit.Tokens.Chamfer("card"), Pin: pin))));

    public UiPageFrame Sidebar(Control parent, string title) => Page(parent, title, "sidebarWidth");

    public VBoxContainer FloatingCard(Control parent, Control.LayoutPreset corner = Control.LayoutPreset.BottomRight, bool pin = false)
    {
        var anchor = Corner(parent, corner);
        BindResize(parent, anchor, () =>
        {
            var width = Math.Max(0, Math.Min(Metric("floatingCardWidth"), parent.Size.X));
            anchor.CustomMinimumSize = new Vector2(width, 0);
            anchor.CustomMaximumSize = new Vector2(width, -1);
        });
        return Card(anchor, pin);
    }

    public TechButton ListRow(Control parent, string text, string? icon = null, Action? pressed = null) =>
        kit.Button(parent, ButtonStyle.Item, text, icon: icon, pressed: pressed, align: HorizontalAlignment.Left);

    public UiPageFrame Dialog(Control parent, string title)
    {
        var shell = Host(parent, "DialogShell");
        var center = Add(shell, new CenterContainer());
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var panel = kit.Panel(center, new UiFx(Fill: "surface.panel", Line: "line.panel", Chamfer: kit.Tokens.Chamfer("page"), Flags: UiFlags.Float));
        panel.CustomMinimumSize = new Vector2(Metric("dialogWidth"), 0);
        var stack = Stack(Margin(panel, page: true));
        var header = Row(stack);
        kit.Text(header, title, "h2");
        var tabs = Flow(stack, tabs: true);
        var scroll = Scroll(stack);
        scroll.VerticalScrollMode = ScrollContainer.ScrollMode.MaximizeFirst;
        var body = Stack(scroll);
        var footer = Flow(stack);
        void FitDialog()
        {
            panel.CustomMaximumSize = parent.Size;
            // 页头和操作栏留在正文外，正文的自然高度有上限。
            var padding = 2 * kit.Theme.Theme.GetConstant("margin_top", UiThemeBuilder.PageMargin);
            var gaps = 3 * stack.GetThemeConstant("separation");
            var reserved = padding + gaps + header.GetCombinedMinimumSize().Y + tabs.GetCombinedMinimumSize().Y + footer.GetCombinedMinimumSize().Y;
            scroll.CustomMaximumSize = new Vector2(-1, Math.Max(0, Math.Min(Metric("dialogBodyMaxHeight"), parent.Size.Y - reserved)));
        }
        BindResize(parent, shell, FitDialog);
        header.MinimumSizeChanged += FitDialog;
        tabs.MinimumSizeChanged += FitDialog;
        footer.MinimumSizeChanged += FitDialog;
        return new UiPageFrame(shell, panel, header, tabs, scroll, body, footer);
    }

    public static void SetInteractive(Control root, bool enabled)
    {
        root.FocusBehaviorRecursive = enabled ? Control.FocusBehaviorRecursiveEnum.Inherited : Control.FocusBehaviorRecursiveEnum.Disabled;
        root.MouseBehaviorRecursive = enabled ? Control.MouseBehaviorRecursiveEnum.Inherited : Control.MouseBehaviorRecursiveEnum.Disabled;
    }

    private void FitWidth(Control parent, Control shell, string metric, bool centered)
    {
        BindResize(parent, shell, () =>
        {
            var fraction = parent.Size.X > 0 ? Math.Min(1, Metric(metric) / parent.Size.X) : 1;
            shell.AnchorLeft = centered ? (1 - fraction) / 2 : 0;
            shell.AnchorRight = shell.AnchorLeft + fraction;
        });
    }

    private static void BindResize(Control source, Control lifetime, Action layout)
    {
        source.Resized += layout;
        lifetime.Ready += layout;
        lifetime.TreeExiting += () => source.Resized -= layout;
        layout();
    }
}

public sealed record UiPageFrame(Control Shell, PanelContainer Panel, HBoxContainer Header, HFlowContainer Tabs,
    ScrollContainer Scroll, VBoxContainer Body, HFlowContainer Footer);

/// <summary>卡片宽度按可用空间决定列数；窄窗自动换成单列，Tab 顺序沿树的阅读顺序。</summary>
public partial class UiCardGrid : GridContainer
{
    public float CardMinimumWidth { get; set; }

    public override void _Notification(int what)
    {
        if (what != NotificationResized && what != NotificationThemeChanged) return;
        var gap = GetThemeConstant("h_separation");
        var columns = Math.Max(1, (int)MathF.Floor((Size.X + gap) / Math.Max(1, CardMinimumWidth + gap)));
        if (Columns != columns) Columns = columns;
    }
}
