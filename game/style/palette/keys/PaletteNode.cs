using Campus.Contracts.Diagnostics;
using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Renderer;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Style.Palette.Keys;

/// <summary>
/// game/style/palette/keys/ colour-script evaluator (SYS-TIME, ENG-PERF). Runs at FramePriority.Palette: reads clock, season and
/// weather from IWorldState, writes the 256×68 palette texture (global sampler <c>palette</c>) and the globals
/// <c>fog</c> and <c>fog_low</c>, and publishes IColorScriptFrame. Also implements IPaletteProbe for the time ring.
/// <c>--inspect=palette</c> shows a debug overlay of the live texture.
/// </summary>
[KernelNode("style.palette")]
[KernelService(typeof(IColorScriptFrame))]
[KernelService(typeof(IPaletteProbe))]
public partial class PaletteNode : Node, IColorScriptFrame, IPaletteProbe, IMapAware
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
    private bool _worldReset = true;
    private double _lastClock = double.NaN, _lastSeason = double.NaN;
    private float _lastSunAltitude = float.NaN;
    private WeatherVec _lastWeather;
    private WorldFrame _frame;
    private int _seasonalPending;
    private PaletteInspector? _inspector;

    public ColorScriptSnapshot Current { get; private set; }
    public string CatalogSource { get; }

    public PaletteNode()
    {
        ProcessPriority = FramePriority.Palette;
        var resources = KernelServices.Get<IRequiredResources>();
        _script = new ColorScript(ColorScriptData.Load(resources.ReadBytes(KeysDir + "autumn.keys.json"),
            resources.ReadBytes(KeysDir + "derive.json"), resources.ReadBytes(KeysDir + "seasons.json"),
            resources.ReadBytes(KeysDir + "vegetation.json"), resources.ReadBytes(KeysDir + "weather.json")));
        var source = KernelServices.Get<IPaletteCatalogSource>();
        _catalog = source.Catalog;
        CatalogSource = source.GetType().Name;
        _composer = new PaletteComposer(_script, _catalog);
        foreach (var warning in _composer.Warnings) KernelServices.Get<IDiagnosticLog>().Warning("Palette", warning);
        // Default state until the world is read: autumn 17:30, clear (SYS-TIME).
        var first = _frame = _script.EvaluateWorld(17.5, (double)Season.Autumn, null, default);
        _composer.ComposeCurrent(first);
        _composer.StepSeasonal(first, full: true);
        _image = Image.CreateFromData(PaletteComposer.Width, PaletteComposer.Height, false, Image.Format.Rgba8, _composer.Pixels);
        _texture = ImageTexture.CreateFromImage(_image);
        // game/style/palette/keys/ produces the palette (ENG-PERF); once published the registry owns its lifetime: never Dispose it here.
        _registration = KernelServices.Get<IGlobalDataTextureRegistry>().RegisterOwned(_shaderGlobalPalette, _texture, _texture.GetRid());
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalPalette, _texture);
        Current = _composer.Snapshot(first, ++_rev);
        WriteGlobals(first);
    }

    public ImageTexture Texture => _texture;
    public PaletteCatalog Catalog => _catalog;
    public byte[] Pixels => _composer.Pixels;
    public double[] SkyStops => _script.Data.SkyStopsDeg;

    public override void _EnterTree()
    {
        if (_world is not null) _world.InstanceChanged += OnWorldInstanceChanged;
        _worldReset = true;
    }

    public override void _Ready()
    {
        if (AppBoot.Options.Capture.Inspect == "palette")
        {
            _inspector = new PaletteInspector(this);
            AddChild(_inspector);
        }
    }

    public override void _Process(double delta)
    {
        if (_world is null)
        {
            _world = KernelServices.Get<IWorldState>();
            _world.InstanceChanged += OnWorldInstanceChanged;
        }
        var clock = _world.Clock;
        var season = _world.SeasonPos;
        var sunAltitude = _world.Sun.AltDeg;
        var weather = _world.WeatherNow;
        // EvaluateWorld's weather corrections read these five fields; other weather channels do not colour the palette.
        var changed = _worldReset || clock != _lastClock || season != _lastSeason || sunAltitude != _lastSunAltitude ||
            weather.Cloud != _lastWeather.Cloud || weather.Overcast != _lastWeather.Overcast ||
            weather.Precip != _lastWeather.Precip || weather.PrecipKind != _lastWeather.PrecipKind || weather.Wet != _lastWeather.Wet;
        var upload = changed;
        if (changed)
        {
            var w = _frame = _script.EvaluateWorld(clock, season, sunAltitude, weather);
            _composer.ComposeCurrent(w);
            var jump = double.IsNaN(_lastClock) || Math.Abs(Oklab.HueDiff(_lastClock * 15.0, clock * 15.0)) > 7.5;   // > 0.5 h
            var full = _worldReset || season != _lastSeason || jump;
            _composer.StepSeasonal(w, full);
            _seasonalPending = full ? 0 : Math.Max(0, _composer.SeasonalCount - 1);
            WriteGlobals(w);
            var snapshot = _composer.Snapshot(w, _rev);
            if (snapshot != Current || _worldReset) snapshot = snapshot with { Rev = ++_rev };
            Current = snapshot;
            _worldReset = false;
            _lastClock = clock;
            _lastSeason = season;
            _lastSunAltitude = sunAltitude;
            _lastWeather = weather;
            _inspector?.Refresh(w, clock, season);
        }
        else if (_seasonalPending > 0)
        {
            // Finish the existing one-family-per-frame sweep after the last continuous input change.
            _composer.StepSeasonal(_frame, full: false);
            _seasonalPending--;
            upload = true;
        }
        if (upload)
        {
            _image.SetData(PaletteComposer.Width, PaletteComposer.Height, false, Image.Format.Rgba8, _composer.Pixels);
            _texture.Update(_image);
        }
    }

    private void OnWorldInstanceChanged(long _) => _worldReset = true;

    public void OnMapUnloading(MapId from) => _worldReset = true;
    public void OnMapLoaded(IMapContext context) => _worldReset = true;

    public override void _ExitTree()
    {
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
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

    /// <summary>Hex of a registry column in row 0 (live inspector only).</summary>
    public string HexOf(string token, int row = 0)
    {
        if (!_catalog.TryGet(new PaletteToken(token), out var e)) return "-";
        var (col, texelRow) = PaletteLayout.FrameTexel(e.Index, row);
        var i = (texelRow * PaletteComposer.Width + col) * 4;
        var p = _composer.Pixels;
        return string.Create(CultureInfo.InvariantCulture, $"#{p[i]:X2}{p[i + 1]:X2}{p[i + 2]:X2}");
    }
}
