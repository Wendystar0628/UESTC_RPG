using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Kernel.Lighting;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Post;

/// <summary>
/// The post-processing chain (WP-05, ART-STYLE §7, ENG-PERF §10.2): owns the two CompositorEffects on the active
/// camera (outline at PostSky, finish at PostTransparent), the engine's glow / tonemap / adjustment settings and
/// the runtime LUT. Each frame it copies the colour-script keyframe scalars (IColorScriptFrame), the key light,
/// weather, veil, edge fog and map rect into one <see cref="PostFrame"/> for the render thread, so nothing reads
/// shader globals back. Quality knobs owned here take effect without pipeline compilation (SYS-GRAPHICS §3).
/// </summary>
[KernelNode("post.chain", Owner = "WP-05")]
[QualityResponsive(Owns = [QualityKnob.AntiAliasing, QualityKnob.GlowQuality, QualityKnob.TiltShift,
    QualityKnob.LensFlare, QualityKnob.LightShafts, QualityKnob.Vignette],
    Reads = [QualityKnob.RenderScale])]
public partial class PostChain : Node, IQualityAware, IMapAware
{
    private const float FadeSeconds = 0.5f;            // style-default knobs and glow level changes fade, never cut
    private const float GlowThresholdFloor = 1.0f;     // registered lit colours (peak 1.0) never bloom by day
    private const float GlowLowSunDeg = 6f;            // levels 2–4 above, 3–5 below (ART-STYLE §7.1)
    private const float FlashBloom = 0.1f;             // thunderstorm flash frame (SYS-WEATHER §5)
    private const float SeasonVeilBloom = 0.1f;        // season-change veil (SYS-TIME §5)
    private const float WeatherGroundFogTop = 12f;     // provisional mirror of weather_fog.y (ART-SKYLIGHT §6.6)

    private readonly PostFrameBox _box = new();
    private readonly OutlineEffect _outline = new();
    private readonly FinishEffect _finish = new();
    private readonly Compositor _compositor = new();
    private readonly float[] _glowLevels = new float[7];
    private readonly float[] _glowTargets = new float[7];
    private IColorScriptFrame? _colorScript;
    private IWorldState? _world;
    private IKeyLightState? _key;
    private ICameraRig? _rig;
    private IVeilState? _veil;
    private Camera3D? _camera;
    private Godot.Environment? _environment;
    private Texture3Drd? _lut;
    private Vector4 _mapRect;
    private Rid _palette, _matTable;
    private bool _highGlow = true;
    private bool _bicubicApplied;
    private bool _vignetteOn = true;
    private float _vignetteFade = 1f;
    private bool _tiltOn = true;
    private bool _lensFlareOn = true;
    private bool _lightShaftsOn = true;

    public PostChain()
    {
        Name = "PostChain";
        ProcessPriority = FramePriority.Systems;
        ProcessMode = ProcessModeEnum.Always;   // post keeps running under pause and veils
        _outline.Inputs = _box;
        _finish.Inputs = _box;
        _compositor.CompositorEffects = [_outline, _finish];
    }

    /// <summary>Tilt-shift, lens flare and light shafts: current switch state, for the passes that land later.</summary>
    internal bool TiltShiftOn => _tiltOn;
    internal bool LensFlareOn => _lensFlareOn;
    internal bool LightShaftsOn => _lightShaftsOn;

    public override void _Ready()
    {
        _colorScript = Services.Get<IColorScriptFrame>();
        _world = Services.Get<IWorldState>();
        _key = Services.Get<IKeyLightState>();
        _rig = Services.Get<ICameraRig>();
        _palette = PostDataTextures.Palette;     // writers keep their texture RIDs for the whole run
        _matTable = PostDataTextures.MatTable;
        _veil = Services.Get<IVeilState>();
        if (Services.Get<IMapService>().Context is { } context) OnMapLoaded(context);
        // Startup-only pipeline switches (ENG-PERF §0 rule 10): set before warm-up, never toggled at runtime.
        GetViewport().UseDebanding = true;
    }

    public override void _ExitTree()
    {
        if (_camera is not null && IsInstanceValid(_camera) && _camera.Compositor == _compositor) _camera.Compositor = null;
        if (_environment is not null && _environment.AdjustmentColorCorrection == _lut) _environment.AdjustmentColorCorrection = null;
        _camera = null;
        _environment = null;
        _lut = null;
        // GPU objects belong to the render thread; release them there before the rendering device shuts down.
        RenderingServer.CallOnRenderThread(Callable.From(ReleaseGpuResources));
    }

    private void ReleaseGpuResources()
    {
        _outline.ReleaseGpuResources();
        _finish.ReleaseGpuResources();
    }

    public void ApplyQuality(QualityView view, in QualityChange change)
    {
        if (change.Has(QualityKnob.AntiAliasing))
        {
            // Under-veil class: applied while the veil is fully covering (or at startup); rebuilds buffers only.
            GetViewport().ScreenSpaceAA = (int)view[QualityKnob.AntiAliasing] switch
            {
                0 => Viewport.ScreenSpaceAAEnum.Smaa,
                1 => Viewport.ScreenSpaceAAEnum.Fxaa,
                var v => throw new ArgumentOutOfRangeException(nameof(view), v, "AntiAliasing knob"),
            };
        }
        if (change.Has(QualityKnob.GlowQuality))
        {
            _highGlow = (int)view[QualityKnob.GlowQuality] switch
            {
                0 => true,
                1 => false,
                var v => throw new ArgumentOutOfRangeException(nameof(view), v, "GlowQuality knob"),
            };
            _bicubicApplied = false;   // switched in the frame the level fade finishes
            if (change.Veiled) SnapGlowLevels();
        }
        if (change.Has(QualityKnob.Vignette))
        {
            _vignetteOn = OnOff(view[QualityKnob.Vignette], "Vignette");
            if (change.Veiled || change.Origin == ApplyOrigin.Startup) _vignetteFade = _vignetteOn ? 1f : 0f;
        }
        if (change.Has(QualityKnob.TiltShift))
        {
            _tiltOn = (int)view[QualityKnob.TiltShift] switch
            {
                0 or 1 => true,        // 1/2 or 1/4 resolution blur
                2 => false,
                var v => throw new ArgumentOutOfRangeException(nameof(view), v, "TiltShift knob"),
            };
        }
        if (change.Has(QualityKnob.LensFlare)) _lensFlareOn = OnOff(view[QualityKnob.LensFlare], "LensFlare");
        if (change.Has(QualityKnob.LightShafts)) _lightShaftsOn = OnOff(view[QualityKnob.LightShafts], "LightShafts");
    }

    private static bool OnOff(float value, string knob) => (int)value switch
    {
        0 => false,
        1 => true,
        var v => throw new ArgumentOutOfRangeException(nameof(value), v, knob + " knob"),
    };

    public void OnMapUnloading(MapId from) { }

    public void OnMapLoaded(IMapContext ctx)
    {
        var rect = ctx.Descriptor.DataRect;
        _mapRect = new Vector4(rect.Min.X, rect.Min.Y, 1f / (rect.Max.X - rect.Min.X), 1f / (rect.Max.Y - rect.Min.Y));
    }

    public override void _Process(double delta)
    {
        if (_colorScript is null || _world is null || _key is null || _rig is null || _veil is null) return;
        var dt = (float)Math.Min(delta, 0.25);
        AttachToCamera();
        var environment = GetViewport().FindWorld3D()?.Environment;
        if (environment != _environment) ConfigureEnvironment(environment);

        var cs = _colorScript.Current;
        var weather = _world.WeatherNow;
        var keyDir = _key.KeyDir;   // WP-03's key light, same value as the key_dir / key_strength globals

        var step = dt / FadeSeconds;
        _vignetteFade = Mathf.MoveToward(_vignetteFade, _vignetteOn ? 1f : 0f, step);

        var frame = new PostFrame
        {
            LineDarken = cs.LineDarkness,
            TentMix = 0f,
            Flash = Mathf.Clamp(weather.Lightning, 0f, 1f),
            FarRain = 0f,
            KeyDir = new Vector3(keyDir.X, keyDir.Y, keyDir.Z),
            KeyStrength = _key.KeyStrength,
            RimColor = Colors.White,
            RimStrength = 0f,
            DebugView = 0,
            Fog = new Vector4(cs.Fog.X, cs.Fog.Y, cs.Fog.Z, cs.Fog.W),
            FogLow = new Vector4(cs.FogLow.X, cs.FogLow.Y, cs.FogLow.Z, cs.FogLow.W),
            WeatherFog = new Vector4(weather.Fog > 0f ? weather.Fog : 1f, WeatherGroundFogTop, weather.GroundFog, 0f),
            MapRect = _mapRect,
            Medium = new Vector4(0f, 0f, 0f, 1f),
            MediumColor = Colors.White,
            EdgeFog = _rig.EdgeFog,
            Veil = _veil.Opacity,
            Palette = _palette,
            MatTable = _matTable,
            TiltAmount = 0f,
            VignetteK = cs.Vignette * _vignetteFade,
            VignetteTint = new Vector3(cs.VignetteTint.X, cs.VignetteTint.Y, cs.VignetteTint.Z),
            CdlSlope = new Vector3(cs.CdlSlope.X, cs.CdlSlope.Y, cs.CdlSlope.Z),
            CdlOffset = new Vector3(cs.CdlOffset.X, cs.CdlOffset.Y, cs.CdlOffset.Z),
            CdlPower = new Vector3(cs.CdlPower.X, cs.CdlPower.Y, cs.CdlPower.Z),
            CdlSaturation = cs.CdlSaturation,
        };
        _box.Write(frame);

        if (_environment is null) return;
        var bloom = cs.Glow + FlashBloom * frame.Flash
            + (_veil.Reason == VeilReason.Season ? SeasonVeilBloom * _veil.Opacity : 0f);
        _environment.GlowBloom = Mathf.Max(bloom, 0f);
        _environment.GlowIntensity = cs.GlowIntensity;
        _environment.GlowHdrThreshold = Mathf.Max(cs.GlowHdrThreshold, GlowThresholdFloor);
        _environment.GlowHdrScale = cs.GlowHdrScale;
        _environment.TonemapExposure = Mathf.Pow(2f, cs.Exposure);
        UpdateGlowLevels(_world.Sun.AltDeg, step);
        if (_lut is null && _finish.LutTexture.IsValid)
        {
            _lut = new Texture3Drd { TextureRdRid = _finish.LutTexture };
            _environment.AdjustmentColorCorrection = _lut;
        }
    }

    private void AttachToCamera()
    {
        var camera = GetViewport().GetCamera3D();
        if (camera == _camera) return;
        if (_camera is not null && IsInstanceValid(_camera) && _camera.Compositor == _compositor) _camera.Compositor = null;
        _camera = camera;
        if (_camera is not null) _camera.Compositor = _compositor;
    }

    private void ConfigureEnvironment(Godot.Environment? environment)
    {
        _environment = environment;
        if (environment is null) return;
        // Fixed for the whole run (ART-STYLE §7.2, §7.7): Linear tonemap keeps palette colours, Adjustment carries the
        // LUT with brightness / contrast / saturation neutral, fog lives in the materials, glow always on (Screen).
        environment.TonemapMode = Godot.Environment.ToneMapper.Linear;
        environment.FogEnabled = false;
        environment.VolumetricFogEnabled = false;
        environment.GlowEnabled = true;
        environment.GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Screen;
        environment.AdjustmentEnabled = true;
        environment.AdjustmentBrightness = 1f;
        environment.AdjustmentContrast = 1f;
        environment.AdjustmentSaturation = 1f;
        if (_lut is not null) environment.AdjustmentColorCorrection = _lut;
        SnapGlowLevels();
    }

    // Glow levels: 2–4 by day, 3–5 once the sun is below 6°; "standard" quality drops the top level. Changes fade
    // over 0.5 s (SetGlowLevel), and the bicubic upscale switch follows when the fade has finished (ENG-PERF §10.2).
    private void TargetGlowLevels(float sunAltDeg)
    {
        var first = sunAltDeg >= GlowLowSunDeg ? 1 : 2;   // 0-based index of level 2 or level 3
        var count = _highGlow ? 3 : 2;
        for (var i = 0; i < _glowTargets.Length; i++) _glowTargets[i] = i >= first && i < first + count ? 1f : 0f;
    }

    private void SnapGlowLevels()
    {
        if (_world is null || _environment is null) return;
        TargetGlowLevels(_world.Sun.AltDeg);
        for (var i = 0; i < _glowLevels.Length; i++)
        {
            _glowLevels[i] = _glowTargets[i];
            _environment.SetGlowLevel(i, _glowLevels[i]);
        }
        RenderingServer.EnvironmentGlowSetUseBicubicUpscale(_highGlow);
        _bicubicApplied = true;
    }

    private void UpdateGlowLevels(float sunAltDeg, float step)
    {
        if (_environment is null) return;
        TargetGlowLevels(sunAltDeg);
        var settled = true;
        for (var i = 0; i < _glowLevels.Length; i++)
        {
            var next = Mathf.MoveToward(_glowLevels[i], _glowTargets[i], step);
            if (next != _glowLevels[i])
            {
                _glowLevels[i] = next;
                _environment.SetGlowLevel(i, next);
            }
            settled &= next == _glowTargets[i];
        }
        if (settled && !_bicubicApplied)
        {
            RenderingServer.EnvironmentGlowSetUseBicubicUpscale(_highGlow);
            _bicubicApplied = true;
        }
    }
}
