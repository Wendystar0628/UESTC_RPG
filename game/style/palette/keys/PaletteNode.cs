using System.Globalization;
using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Style.Palette.Keys;

/// <summary>
/// WP-37 colour-script evaluator (SYS-TIME, ENG-PERF). Runs at FramePriority.Palette: reads clock, season and
/// weather from IWorldState, writes the 256×68 palette texture (global sampler <c>palette</c>) and the globals
/// <c>fog</c> and <c>fog_low</c>, and publishes IColorScriptFrame. Also implements IPaletteProbe for the time ring.
/// <c>--inspect=palette</c> shows a debug overlay of the live texture.
/// </summary>
[KernelNode("style.palette", Owner = "WP-37")]
[KernelService(typeof(IColorScriptFrame), Owner = "WP-37")]
[KernelService(typeof(IPaletteProbe), Owner = "WP-37")]
public partial class PaletteNode : Node, IColorScriptFrame, IPaletteProbe
{
    private static readonly StringName _shaderGlobalFog = ShaderGlobals.Fog;
    private static readonly StringName _shaderGlobalFogLow = ShaderGlobals.FogLow;
    private static readonly StringName _shaderGlobalPalette = ShaderGlobals.Palette;

    private const string KeysDir = "res://style/palette/keys/";

    private readonly ColorScript _script;
    private readonly PaletteCatalog _catalog;
    private readonly PaletteComposer _composer;
    private readonly Image _image;
    private readonly ImageTexture _texture;
    private IDisposable? _registration;
    private IWorldState? _world;
    private long _rev;
    private long _instance = long.MinValue;
    private double _lastClock = double.NaN, _lastSeason = double.NaN;
    private int _frames;
    private PaletteInspector? _inspector;

    public ColorScriptSnapshot Current { get; private set; }
    public string CatalogSource { get; }

    public PaletteNode()
    {
        ProcessPriority = FramePriority.Palette;
        _script = new ColorScript(ColorScriptData.Load(Read("autumn.keys.json"), Read("derive.json"), Read("seasons.json"),
            Read("vegetation.json"), Read("weather.json")));
        var source = Services.Get<IPaletteCatalogSource>();
        _catalog = source.Catalog;
        CatalogSource = source.GetType().Name;
        _composer = new PaletteComposer(_script, _catalog);
        foreach (var warning in _composer.Warnings) GD.PushWarning(warning);
        // Default state until the world is read: autumn 17:30, clear (SYS-TIME).
        var first = _script.EvaluateWorld(17.5, (double)Season.Autumn, null, default);
        _composer.ComposeCurrent(first);
        _composer.StepSeasonal(first, full: true);
        _image = Image.CreateFromData(PaletteComposer.Width, PaletteComposer.Height, false, Image.Format.Rgba8, _composer.Pixels);
        _texture = ImageTexture.CreateFromImage(_image);
        // WP-37 produces the palette (ENG-PERF); once published the registry owns its lifetime: never Dispose it here.
        _registration = Services.Get<IGlobalDataTextureRegistry>().RegisterOwned(_shaderGlobalPalette, _texture, _texture.GetRid());
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalPalette, _texture);
        Current = _composer.Snapshot(first, ++_rev);
        WriteGlobals(first);
    }

    public ImageTexture Texture => _texture;
    public PaletteCatalog Catalog => _catalog;
    public byte[] Pixels => _composer.Pixels;
    public double[] SkyStops => _script.Data.SkyStopsDeg;

    private static byte[] Read(string name)
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(KeysDir + name);
        if (bytes.Length == 0) throw new FileNotFoundException($"Missing colour-script data: {KeysDir + name}");
        return bytes;
    }

    public override void _Ready()
    {
        if (AppBoot.Value("inspect") == "palette")
        {
            _inspector = new PaletteInspector(this);
            AddChild(_inspector);
        }
    }

    public override void _Process(double delta)
    {
        _world ??= Services.Get<IWorldState>();
        var clock = _world.Clock;
        var season = _world.SeasonPos;
        var weather = _world.WeatherNow;
        var w = _script.EvaluateWorld(clock, season, _world.Sun.AltDeg, weather);
        _composer.ComposeCurrent(w);
        var jump = double.IsNaN(_lastClock) || Math.Abs(Oklab.HueDiff(_lastClock * 15.0, clock * 15.0)) > 7.5;   // > 0.5 h
        var full = _world.Instance != _instance || season != _lastSeason || jump;
        _composer.StepSeasonal(w, full);
        _image.SetData(PaletteComposer.Width, PaletteComposer.Height, false, Image.Format.Rgba8, _composer.Pixels);
        _texture.Update(_image);
        WriteGlobals(w);
        var snapshot = _composer.Snapshot(w, _rev);
        if (snapshot != Current || _world.Instance != _instance) snapshot = snapshot with { Rev = ++_rev };
        Current = snapshot;
        _instance = _world.Instance;
        _lastClock = clock;
        _lastSeason = season;
        _inspector?.Refresh(w, clock, season);
        if (_inspector is not null && ++_frames == 30) PrintParity(w, clock, season);
    }

    public override void _ExitTree()
    {
        // Stop producing, unbind the global, then hand the texture back to the registry (it releases the native resource).
        SetProcess(false);
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalPalette, default(Variant));
        _registration?.Dispose();
        _registration = null;
    }

    private static void WriteGlobals(WorldFrame w)
    {
        var v = w.Frame.V;
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalFog, new Vector4((float)v[FrameLayout.FogDensity],
            (float)v[FrameLayout.HazeDeg], (float)v[FrameLayout.FogStart], (float)v[FrameLayout.FogSilhouette]));
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalFogLow, new Vector4((float)v[FrameLayout.FogLowTop],
            (float)v[FrameLayout.FogLowDensity], (float)v[FrameLayout.FogLake], 0f));
    }

    public void SampleHorizon(Season season, WeatherVec weather, Span<N.Vector3> srgb24) =>
        _script.SampleHorizon(season, weather, srgb24);

    /// <summary>Hex of a registry column in row 0 (debug and parity output only).</summary>
    public string HexOf(string token, int row = 0)
    {
        if (!_catalog.TryGet(new PaletteToken(token), out var e)) return "-";
        var (col, texelRow) = PaletteLayout.FrameTexel(e.Index, row);
        var i = (texelRow * PaletteComposer.Width + col) * 4;
        var p = _composer.Pixels;
        return string.Create(CultureInfo.InvariantCulture, $"#{p[i]:X2}{p[i + 1]:X2}{p[i + 2]:X2}");
    }

    private void PrintParity(WorldFrame w, double clock, double season)
    {
        string[] tokens = ["sun.light", "shadow.1", "wall.lit", "wall.shade", "stone.beige.shade", "brick.shade", "ginkgo.lit",
            "ginkgo.shade", "grass.lit", "grass.shade", "sky.sun.0", "sky.sun.4", "sky.anti.0", "fog.sun"];
        var parts = string.Join(" ", tokens.Select(t => $"{t}={HexOf(t)}"));
        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"WP37_PALETTE season={season:F2} clock={clock:F3} alt={w.SunAltDeg:F3} r={w.Frame.V[FrameLayout.R]:F4} catalog={CatalogSource} {parts} wall.shade.in={HexOf("wall.lit", 2)}"));
    }
}
