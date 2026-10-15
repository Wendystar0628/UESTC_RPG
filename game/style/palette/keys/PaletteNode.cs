using System.Globalization;
using System.Text.Json;
using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Style.Palette.Keys;

/// <summary>
/// WP-37 colour-script evaluator (SYS-TIME §3, ENG-PERF §1.5). Runs at FramePriority.Palette: reads clock, season and
/// weather from IWorldState, writes the 256×64 palette texture (global sampler <c>palette</c>) and the globals
/// <c>fog</c> and <c>fog_low</c>, and publishes IColorScriptFrame. Also implements IPaletteProbe for the time ring.
/// <c>--inspect=palette</c> shows a debug overlay of the live texture.
/// </summary>
[KernelNode("style.palette", Owner = "WP-37")]
[KernelService(typeof(IColorScriptFrame), Owner = "WP-37")]
[KernelService(typeof(IPaletteProbe), Owner = "WP-37")]
public partial class PaletteNode : Node, IColorScriptFrame, IPaletteProbe
{
    private const string KeysDir = "res://style/palette/keys/";
    // Contract-format registry (PaletteRegistryData) published by WP-04; the fallback mirrors its agreed layout.
    private static readonly string[] RegistryPaths = ["res://style/palette/registry.json", "res://style/palette/core.json"];

    private readonly ColorScript _script;
    private readonly PaletteRegistry _registry;
    private readonly PaletteComposer _composer;
    private readonly Image _image;
    private readonly ImageTexture _texture;
    private IWorldState? _world;
    private long _rev;
    private long _instance = long.MinValue;
    private double _lastClock = double.NaN, _lastSeason = double.NaN;
    private int _frames;
    private PaletteInspector? _inspector;

    public ColorScriptSnapshot Current { get; private set; }
    public string RegistrySource { get; }

    public PaletteNode()
    {
        ProcessPriority = FramePriority.Palette;
        _script = new ColorScript(ColorScriptData.Load(Read("autumn.keys.json"), Read("derive.json"), Read("seasons.json"),
            Read("vegetation.json"), Read("weather.json")));
        (_registry, RegistrySource) = LoadRegistry();
        _composer = new PaletteComposer(_script, _registry);
        foreach (var warning in _composer.Warnings) GD.PushWarning(warning);
        // Default state until the world is read: autumn 17:30, clear (SYS-TIME §0).
        var first = _script.EvaluateWorld(17.5, (double)Season.Autumn, default);
        _composer.ComposeCurrent(first);
        _composer.StepSeasonal(first, full: true);
        _image = Image.CreateFromData(PaletteComposer.Width, PaletteComposer.Height, false, Image.Format.Rgba8, _composer.Pixels);
        _texture = ImageTexture.CreateFromImage(_image);
        RenderingServer.GlobalShaderParameterSet(ShaderNames.Palette, _texture);
        Current = _composer.Snapshot(first, ++_rev);
        WriteGlobals(first);
    }

    public ImageTexture Texture => _texture;
    public PaletteRegistry Registry => _registry;
    public byte[] Pixels => _composer.Pixels;
    public double[] SkyStops => _script.Data.SkyStopsDeg;

    private static byte[] Read(string name)
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(KeysDir + name);
        if (bytes.Length == 0) throw new FileNotFoundException($"Missing colour-script data: {KeysDir + name}");
        return bytes;
    }

    private const string ExtDir = "res://style/palette/ext";

    /// <summary>
    /// Core registry (contract PaletteRegistryData) plus every package extension in <c>style/palette/ext/*.json</c>
    /// (ART-STYLE §8.5; <c>ui.json</c> is the UI token file, not a palette registry). Until WP-01 registers the shared
    /// PaletteRegistry service, this is the evaluator's own loader; the WP-37 fallback replaces a missing core table.
    /// </summary>
    private static (PaletteRegistry, string) LoadRegistry()
    {
        var (core, source) = LoadCore();
        var entries = new List<PaletteEntry>(core.Entries);
        var extFiles = new List<string>();
        if (DirAccess.DirExistsAbsolute(ExtDir))
        {
            foreach (var name in ResourceLoader.ListDirectory(ExtDir).Where(n => n.EndsWith(".json", StringComparison.Ordinal)
                && n != "ui.json").Order(StringComparer.Ordinal))
            {
                var path = ExtDir + "/" + name;
                try
                {
                    var data = JsonSerializer.Deserialize<PaletteRegistryData>(Godot.FileAccess.GetFileAsBytes(path), ContractJson.Options)
                        ?? throw new InvalidDataException("empty registry");
                    if (data.Schema != 1) throw new InvalidDataException($"schema {data.Schema}");
                    entries.AddRange(data.Entries);
                    extFiles.Add(name);
                }
                catch (Exception error) when (error is JsonException or ArgumentException or InvalidDataException or NotSupportedException)
                {
                    GD.PushWarning($"palette: skipped extension {path} ({error.Message}).");
                }
            }
        }
        if (extFiles.Count == 0) return (core, source);
        try
        {
            return (new PaletteRegistry(entries), source + " + ext/" + string.Join(",", extFiles));
        }
        catch (ArgumentException error)
        {
            GD.PushError($"palette: extensions conflict with the core registry ({error.Message}); extensions ignored.");
            return (core, source);
        }
    }

    private static (PaletteRegistry, string) LoadCore()
    {
        foreach (var path in RegistryPaths)
        {
            if (!Godot.FileAccess.FileExists(path)) continue;
            try
            {
                var data = JsonSerializer.Deserialize<PaletteRegistryData>(Godot.FileAccess.GetFileAsBytes(path), ContractJson.Options)
                    ?? throw new InvalidDataException("empty registry");
                return (PaletteRegistry.FromData(data), path);
            }
            catch (Exception error) when (error is JsonException or ArgumentException or InvalidDataException or NotSupportedException)
            {
                GD.PushWarning($"palette: {path} is not a PaletteRegistryData file ({error.Message}); using the WP-37 fallback registry.");
            }
        }
        var fallback = KeysDir + "registry.fallback.json";
        var fallbackData = JsonSerializer.Deserialize<PaletteRegistryData>(Read("registry.fallback.json"), ContractJson.Options)
            ?? throw new InvalidDataException("Fallback palette registry is empty.");
        return (PaletteRegistry.FromData(fallbackData), fallback);
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
        var w = _script.EvaluateWorld(clock, season, weather);
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

    private static void WriteGlobals(WorldFrame w)
    {
        var v = w.Frame.V;
        RenderingServer.GlobalShaderParameterSet(ShaderNames.Fog, new Vector4((float)v[FrameLayout.FogDensity],
            (float)v[FrameLayout.HazeDeg], (float)v[FrameLayout.FogStart], (float)v[FrameLayout.FogSilhouette]));
        RenderingServer.GlobalShaderParameterSet(ShaderNames.FogLow, new Vector4((float)v[FrameLayout.FogLowTop],
            (float)v[FrameLayout.FogLowDensity], (float)v[FrameLayout.FogLake], 0f));
    }

    public void SampleHorizon(Season season, WeatherVec weather, Span<N.Vector3> srgb24) =>
        _script.SampleHorizon(season, weather, srgb24);

    /// <summary>Hex of a registry column in row 0 (debug and parity output only).</summary>
    public string HexOf(string token, int row = 0)
    {
        if (!_registry.TryGet(new PaletteToken(token), out var e)) return "-";
        var i = (row * PaletteComposer.Width + e.Id) * 4;
        var p = _composer.Pixels;
        return string.Create(CultureInfo.InvariantCulture, $"#{p[i]:X2}{p[i + 1]:X2}{p[i + 2]:X2}");
    }

    private void PrintParity(WorldFrame w, double clock, double season)
    {
        string[] tokens = ["sun.light", "shadow.1", "wall.lit", "wall.shade", "stone.beige.shade", "brick.shade", "ginkgo.lit",
            "ginkgo.shade", "grass.lit", "grass.shade", "sky.sun.0", "sky.sun.4", "sky.anti.0", "fog.sun"];
        var parts = string.Join(" ", tokens.Select(t => $"{t}={HexOf(t)}"));
        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"WP37_PALETTE season={season:F2} clock={clock:F3} alt={w.SunAltDeg:F3} r={w.Frame.V[FrameLayout.R]:F4} registry={RegistrySource} {parts} wall.shade.in={HexOf("wall.lit", 2)}"));
    }
}
