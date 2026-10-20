using Campus.Contracts.Startup;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud;

/// <summary>Retains the control tree and scale; reentry reuses its theme, materials, and injected clock.</summary>
internal sealed class HudView
{
    private readonly CanvasLayer _owner;
    private readonly HudSettings _settings;
    private float _scaleDebounce = -1;
    private float? _scale;
    public UiKit Kit { get; }
    public Control Root { get; }
    public Control LoadingHost { get; }
    public Control Frontispiece { get; }
    public Control BackgroundHost { get; }
    public Control SafeFrame { get; }
    public Control Corners { get; }
    public Control MinimapSlot { get; }
    public Control PageHost { get; }
    public Control DialogHost { get; }
    public Control HintHost { get; }
    public VBoxContainer TopLeft { get; }
    public VBoxContainer TopRight { get; }
    public VBoxContainer BottomLeft { get; }
    public VBoxContainer BottomRight { get; }

    public HudView(CanvasLayer owner, HudRootDependencies dependencies)
    {
        _owner = owner;
        _settings = dependencies.Settings;
        var tokens = UiTokens.Load();
        var colors = UiColors.Load(dependencies.PaletteCatalog.Catalog);
        var materials = new UiMaterials(tokens, colors, dependencies.ShaderMaterials);
        var theme = new UiThemeBuilder(tokens, colors);
        Kit = new UiKit(tokens, colors, materials, theme, dependencies.Motion, dependencies.ThemeClock);
        owner.AddChild(dependencies.ThemeClock);
        Root = CreateLayer("Root");
        LoadingHost = CreateLayer("LoadingHost");
        Frontispiece = new ColorRect { Name = "Frontispiece", Color = colors.Resolve("loading.background"),
            Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        owner.AddChild(Frontispiece);
        owner.MoveChild(Frontispiece, 0);
        Frontispiece.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        BackgroundHost = Kit.Layout.Host(Root, "BackgroundHost");
        SafeFrame = Kit.Layout.SafeFrame(Root);
        Corners = Kit.Layout.Host(SafeFrame, "CornerHost");
        // Tree order remains the Tab reading order.
        TopLeft = Kit.Layout.Corner(Corners, Control.LayoutPreset.TopLeft);
        TopRight = Kit.Layout.Corner(Corners, Control.LayoutPreset.TopRight);
        BottomLeft = Kit.Layout.Corner(Corners, Control.LayoutPreset.BottomLeft);
        BottomRight = Kit.Layout.Corner(Corners, Control.LayoutPreset.BottomRight);
        MinimapSlot = Kit.Layout.Add(TopLeft, new Control
        {
            Name = "MinimapSlot", CustomMinimumSize = tokens.V2("metrics", "size", "minimap")
        });
        PageHost = Kit.Layout.Host(SafeFrame, "PageHost");
        DialogHost = Kit.Layout.Host(SafeFrame, "DialogHost");
        HintHost = Kit.Layout.Host(SafeFrame, "HintHost");
        UiLayout.SetInteractive(BackgroundHost, false);
        UiLayout.SetInteractive(HintHost, false);
    }

    private Control CreateLayer(string name)
    {
        var control = new Control { Name = name, Theme = Kit.Theme.Theme,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps };
        _owner.AddChild(control);
        control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return control;
    }

    public void Enter()
    {
        Root.Resized += Layout;
        SafeFrame.Resized += Layout;
        _settings.Changed += OnSettingsChanged;
        Kit.SetResidentFollows(_settings.Resident == ResidentTheme.Follow);
        Layout();
    }

    public void Exit()
    {
        Root.Resized -= Layout;
        SafeFrame.Resized -= Layout;
        _settings.Changed -= OnSettingsChanged;
    }

    public void Configure(UiBootOptions args)
    {
        Root.Visible = args.Visible != false;
        _scale = args.Scale;
        ApplyScale();
    }

    public void Tick(double delta)
    {
        _settings.Tick(delta);
        if (_scaleDebounce < 0 || (_scaleDebounce += (float)delta) < 0.2f) return;
        _scaleDebounce = -1;
        ApplyScale();
    }

    private void OnSettingsChanged()
    {
        Kit.SetResidentFollows(_settings.Resident == ResidentTheme.Follow);
        _scaleDebounce = 0;
    }

    public void Layout()
    {
        ApplyScale();
        foreach (var corner in new[] { TopLeft, TopRight, BottomLeft, BottomRight })
            corner.CustomMaximumSize = new Vector2(SafeFrame.Size.X / 2, SafeFrame.Size.Y);
    }

    private void ApplyScale()
    {
        var window = DisplayServer.WindowGetSize();
        if (window.X <= 0 || window.Y <= 0) return;
        var s = Math.Min(window.X / 1920f, window.Y / 1080f);
        var lo = 0.85f / s;
        var hi = Math.Min(window.X / 1280f, window.Y / 720f) / s;
        _owner.GetTree().Root.ContentScaleFactor = Math.Clamp(_scale ?? _settings.UiScale, lo, Math.Max(lo, hi));
    }
}
