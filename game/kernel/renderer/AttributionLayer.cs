using Campus.Contracts;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;
using NVector3 = System.Numerics.Vector3;

namespace Campus.Kernel.Renderer;

/// <summary>The single OSM credit layer. UI styling may update Credit/theme and the reserved right-hand space.</summary>
[KernelNode("attribution")]
[KernelService(typeof(AttributionLayer))]
public partial class AttributionLayer : CanvasLayer
{
    private readonly NVector3[] _horizon = new NVector3[24];
    private IWorldState? _world;
    private IPaletteProbe? _palette;
    private double _elapsed = .1;
    public LinkButton Credit { get; } = new()
    {
        Name = "OpenStreetMapCredit",
        Text = "© OpenStreetMap contributors",
        Underline = LinkButton.UnderlineMode.Never
    };
    public bool UseLightOnLetterbox { get; set; }

    public override void _Ready()
    {
        Layer = 11;
        ProcessPriority = FramePriority.Hud;
        _world = Services.Get<IWorldState>();
        _palette = Services.Get<IPaletteProbe>();
        AddChild(Credit);
        Credit.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        Credit.OffsetLeft = -215; Credit.OffsetRight = -12;
        Credit.OffsetTop = -27; Credit.OffsetBottom = -12;
        Credit.AddThemeFontSizeOverride("font_size", 11);
        foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
            Credit.AddThemeColorOverride(state, Colors.White);
        Credit.Pressed += () => OS.ShellOpen("https://www.openstreetmap.org/copyright");
    }
    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_elapsed < .1 || _world is null || _palette is null) return;
        _elapsed %= .1;
        _palette.SampleHorizon(_world.SeasonTarget, _world.WeatherNow, _horizon);
        var clock = (_world.Clock % 24 + 24) % 24;
        var from = (int)clock;
        var horizon = NVector3.Lerp(_horizon[from], _horizon[(from + 1) % 24], (float)(clock - from));
        var color = UseLightOnLetterbox ? Colors.White : new Color(1f - horizon.X, 1f - horizon.Y, 1f - horizon.Z);
        color.A = .35f;
        Credit.Modulate = color;
    }
    public void ReserveRight(float pixels)
    {
        if (!float.IsFinite(pixels) || pixels < 0) throw new ArgumentOutOfRangeException(nameof(pixels));
        Credit.OffsetLeft = -215 - pixels;
        Credit.OffsetRight = -12 - pixels;
    }
}
