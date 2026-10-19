using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

/// <summary>应用进出共用的全屏加载页；只显示流程报告的完成比例，不补间进度。</summary>
public partial class LoadingPage : Control
{
    private UiKit _kit = null!;
    private LoadingProgress _bar = null!;
    private Label _destination = null!;

    public static LoadingPage Create(UiKit kit, Control parent, string destination)
    {
        var page = new LoadingPage { _kit = kit, Name = "LoadingPage", MouseFilter = MouseFilterEnum.Stop };
        parent.AddChild(page);
        page.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var background = new ColorRect { Color = kit.Colors.Resolve("loading.background"), MouseFilter = MouseFilterEnum.Ignore };
        page.AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var origin = kit.Tokens.V2("metrics", "loading", "titleOrigin");
        var titleMargin = kit.Layout.Add(page, new MarginContainer(), UiThemeBuilder.LoadingTitleMargin);
        titleMargin.AnchorRight = (origin.X + kit.Tokens.Get("metrics", "base", "centralFree")[0].GetSingle()) /
            kit.Tokens.F("metrics", "base", "width");
        var title = kit.Layout.Stack(titleMargin, groups: false);
        TitleMenu.TitleName(kit, title);
        kit.Text(title, UiKit.T("title.english"), "mono-caps", "over.title.text", wrap: false);

        var widthFraction = kit.Tokens.F("metrics", "loading", "barWidthFraction");
        var footer = kit.Layout.Add(page, new MarginContainer
        {
            AnchorLeft = (1 - widthFraction) / 2, AnchorRight = (1 + widthFraction) / 2,
            AnchorTop = 1, AnchorBottom = 1, GrowHorizontal = GrowDirection.Both, GrowVertical = GrowDirection.Begin
        }, UiThemeBuilder.LoadingFooterMargin);
        var row = kit.Layout.Stack(footer, groups: false);
        var diameter = kit.Tokens.F("metrics", "via", "core");
        page._bar = new LoadingProgress
        {
            Name = "PreparationProgress", Kit = kit, ShowPercentage = false, MinValue = 0, MaxValue = 1, Step = 0,
            MouseFilter = MouseFilterEnum.Ignore, ThemeTypeVariation = UiThemeBuilder.LoadingProgressType,
            CustomMinimumSize = new Vector2(0, diameter), AccessibilityLive = AccessibilityServer.AccessibilityLiveMode.Polite
        };
        row.AddChild(page._bar);
        page._destination = kit.Text(row, destination, "data", "over.title.text", align: HorizontalAlignment.Center);
        return page;
    }

    public void Report(float progress, int completed, int total)
    {
        _bar.Value = progress;
        _bar.AccessibilityName = _destination.Text;
        _bar.AccessibilityDescription = UiKit.T("loading.progress", ("completed", completed.ToString()), ("total", total.ToString()));
        _bar.QueueRedraw();
    }

    public void SetDestination(string destination) => _destination.Text = destination;

    public async Task Fade(bool visible)
    {
        var duration = _kit.Motion.Fade(_kit.Tokens.Ms("loading", visible ? "in" : "out"));
        if (duration <= 0) { Modulate = Colors.White with { A = visible ? 1 : 0 }; return; }
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", visible ? 1f : 0f, duration);
        await ToSignal(tween, Tween.SignalName.Finished);
    }
}

/// <summary>引擎进度控件保留读屏数值，轨道与前端过孔只读固定色名和尺寸令牌。</summary>
public partial class LoadingProgress : ProgressBar
{
    public UiKit Kit { get; init; } = null!;

    public override void _Draw()
    {
        var y = Size.Y / 2;
        var width = Kit.Tokens.F("metrics", "line", "hairPhys");
        var start = new Vector2(0, y);
        var end = new Vector2(Size.X, y);
        var head = new Vector2(Size.X * (float)Value, y);
        DrawLine(start, end, Kit.Colors.Theme("light", "line.hair"), width);
        DrawLine(start, head, Kit.Colors.Resolve("blue.400"), width);
        DrawCircle(head, Kit.Tokens.F("metrics", "via", "core") / 2, Kit.Colors.Resolve("blue.400"));
    }
}
