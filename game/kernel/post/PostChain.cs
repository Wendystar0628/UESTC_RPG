using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Startup;
using Campus.Kernel.Renderer;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Post;

/// <summary>
/// The post-processing chain (ART-STYLE-LINES, ART-STYLE, ENG-PERF): owns the two CompositorEffects on the active
/// camera (outline at PostSky, finish at PostTransparent), the engine's glow / tonemap / adjustment settings and
/// the runtime LUT. Each frame it copies the colour-script keyframe scalars (IColorScriptFrame), the key light,
/// weather, edge fog and map rect into one <see cref="PostFrame"/> for the render thread, so nothing reads
/// shader globals back. Quality knobs owned here take effect without pipeline compilation (SYS-GRAPHICS).
/// Glow runs at the standard level; the finish pass applies the highlight shoulder and vignette.
/// </summary>
[KernelNode("post.chain")]
[QualityResponsive(Order = 2, Owns = [QualityKnob.AntiAliasing], Reads = [QualityKnob.RenderScale])]
public partial class PostChain : Node, IQualityAware, IMapAware, IViewContentReadiness
{
    private const float FadeSeconds = 0.5f;            // glow level changes fade, never cut
    private const int GlowLevelCount = 2;              // standard glow: two consecutive levels
    private const float GlowThresholdFloor = 1.0f;     // registered lit colours (peak 1.0) never bloom by day
    private const float GlowLowSunDeg = 6f;            // glow starts at level 2 above, level 3 below (ART-STYLE)
    private const float FlashBloom = 0.1f;             // thunderstorm flash frame (SYS-WEATHER)

    private readonly PostFrameBox _box = new();
    private readonly OutlineEffect _outline = new();
    private readonly FinishEffect _finish = new();
    private readonly Compositor _compositor = new();
    private readonly float[] _glowLevels = new float[7];
    private readonly float[] _glowTargets = new float[7];
    private IColorScriptFrame? _colorScript;
    private IWorldState? _world;
    private IKeyLightState? _key;
    private IWeatherRenderState? _weather;
    private IMediumRenderState? _medium;
    private IGlobalDataTextureRegistry? _textures;
    private ICameraRig? _rig;
    private Camera3D? _camera;
    private Godot.Environment? _environment;
    private Texture3Drd? _lut;
    private Vector4 _mapRect;

    public Exception? Failure => _outline.Failure ?? _finish.Failure;
    public bool IsReady(Pose pose) => _outline.Rendered && _finish.Rendered && _lut is not null;

    public PostChain()
    {
        Name = "PostChain";
        ProcessPriority = FramePriority.Systems;
        ProcessMode = ProcessModeEnum.Always;   // post keeps running under pause and loading screens
        _outline.Inputs = _box;
        _finish.Inputs = _box;
        _compositor.CompositorEffects = [_outline, _finish];
    }

    public override void _Ready()
    {
        _colorScript = KernelServices.Get<IColorScriptFrame>();
        _world = KernelServices.Get<IWorldState>();
        _key = KernelServices.Get<IKeyLightState>();
        _weather = KernelServices.Get<IWeatherRenderState>();
        _medium = KernelServices.Get<IMediumRenderState>();
        _textures = KernelServices.Get<IGlobalDataTextureRegistry>();
        _rig = KernelServices.Get<ICameraRig>();
        if (KernelServices.Get<IMapService>().Context is { } context) OnMapLoaded(context);
        // Startup-only pipeline switches (ENG-PERF rule 10): set before warm-up, never toggled at runtime.
        GetViewport().UseDebanding = true;
        RenderingServer.EnvironmentGlowSetUseBicubicUpscale(false);   // standard glow upscales bilinearly
    }

    public override void _ExitTree()
    {
        if (_camera is not null && IsInstanceValid(_camera) && _camera.Compositor == _compositor) _camera.Compositor = null;
        if (_environment is not null && _environment.AdjustmentColorCorrection == _lut) _environment.AdjustmentColorCorrection = null;
        _camera = null;
        _environment = null;
        _lut = null;
        _box.Close();   // cancel input no callback took; callbacks already running keep their own batches
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
            // Loading class: applied while the loading screen is fully covering (or at startup); rebuilds buffers only.
            GetViewport().ScreenSpaceAA = (int)view[QualityKnob.AntiAliasing] switch
            {
                0 => Viewport.ScreenSpaceAAEnum.Smaa,
                1 => Viewport.ScreenSpaceAAEnum.Fxaa,
                var v => throw new ArgumentOutOfRangeException(nameof(view), v, "AntiAliasing knob"),
            };
        }
    }

    public void OnMapUnloading(MapId from) { }

    public void OnMapLoaded(IMapContext ctx)
    {
        var rect = ctx.Descriptor.DataRect;
        _mapRect = new Vector4(rect.Min.X, rect.Min.Y, 1f / (rect.Max.X - rect.Min.X), 1f / (rect.Max.Y - rect.Min.Y));
    }

    public override void _Process(double delta)
    {
        if (_colorScript is null || _world is null || _key is null || _weather is null || _medium is null
            || _textures is null || _rig is null) return;
        var dt = (float)Math.Min(delta, 0.25);
        AttachToCamera();
        var environment = GetViewport().FindWorld3D()?.Environment;
        if (environment != _environment) ConfigureEnvironment(environment);

        var cs = _colorScript.Current;
        var weather = _world.WeatherNow;
        // Published render state: the same values written to key_dir / key_strength, weather_fog, medium and
        // medium_color this frame (IKeyLightState, IWeatherRenderState, IMediumRenderState); never read back.
        var keyDir = _key.KeyDir;
        var weatherFog = _weather.WeatherFog;
        var medium = _medium.Medium;
        var mediumColor = _medium.MediumColor;

        var step = dt / FadeSeconds;

        var frame = new PostFrame
        {
            LineDarken = cs.LineDarkness,
            Flash = Mathf.Clamp(weather.Lightning, 0f, 1f),
            KeyDir = new Vector3(keyDir.X, keyDir.Y, keyDir.Z),
            KeyStrength = _key.KeyStrength,
            Fog = new Vector4(cs.Fog.X, cs.Fog.Y, cs.Fog.Z, cs.Fog.W),
            FogLow = new Vector4(cs.FogLow.X, cs.FogLow.Y, cs.FogLow.Z, cs.FogLow.W),
            SeasonAtmosphere = _world.SeasonAtmosphere,
            WeatherFog = new Vector4(weatherFog.X, weatherFog.Y, weatherFog.Z, weatherFog.W),
            MapRect = _mapRect,
            Medium = new Vector4(medium.X, medium.Y, medium.Z, medium.W),
            MediumColor = new Color(mediumColor.X, mediumColor.Y, mediumColor.Z),
            EdgeFog = _rig.EdgeFog,
            SunDir = new Vector3(_world.Sun.Dir.X, _world.Sun.Dir.Y, _world.Sun.Dir.Z),     // same source as sun_dir / moon_dir
            MoonDir = new Vector3(_world.Moon.Dir.X, _world.Moon.Dir.Y, _world.Moon.Dir.Z),
            VignetteK = cs.Vignette,
            VignetteTint = new Vector3(cs.VignetteTint.X, cs.VignetteTint.Y, cs.VignetteTint.Z),
            CdlSlope = new Vector3(cs.CdlSlope.X, cs.CdlSlope.Y, cs.CdlSlope.Z),
            CdlOffset = new Vector3(cs.CdlOffset.X, cs.CdlOffset.Y, cs.CdlOffset.Z),
            CdlPower = new Vector3(cs.CdlPower.X, cs.CdlPower.Y, cs.CdlPower.Z),
            CdlSaturation = cs.CdlSaturation,
        };
        // Publish the scalars and their texture leases as one render-thread input.
        _box.Publish(frame, _textures.TryAcquire(ShaderGlobals.Palette, out var palette) ? palette : null,
            _textures.TryAcquire(ShaderGlobals.MatTable, out var matTable) ? matTable : null);

        if (_environment is null) return;
        var bloom = cs.Glow + WorldTimeTuning.SeasonGlowBoost * frame.SeasonAtmosphere + FlashBloom * frame.Flash;
        _environment.GlowBloom = Mathf.Max(bloom, 0f);
        _environment.GlowIntensity = cs.GlowIntensity;
        _environment.GlowHdrThreshold = Mathf.Max(cs.GlowHdrThreshold, GlowThresholdFloor);
        _environment.GlowHdrScale = cs.GlowHdrScale;
        _environment.TonemapExposure = Mathf.Pow(2f, cs.Exposure)
            * (1f + WorldTimeTuning.SeasonBrightnessBoost * frame.SeasonAtmosphere);
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
        // Fixed for the whole run (ART-STYLE): Linear tonemap keeps palette colours, Adjustment carries the
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

    // Glow levels start at 2 by day and at 3 once the sun is below 6°; standard glow keeps two levels from there.
    // Changes fade over 0.5 s (SetGlowLevel).
    private void TargetGlowLevels(float sunAltDeg)
    {
        var first = sunAltDeg >= GlowLowSunDeg ? 1 : 2;   // 0-based index of level 2 or level 3
        for (var i = 0; i < _glowTargets.Length; i++) _glowTargets[i] = i >= first && i < first + GlowLevelCount ? 1f : 0f;
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
    }

    private void UpdateGlowLevels(float sunAltDeg, float step)
    {
        if (_environment is null) return;
        TargetGlowLevels(sunAltDeg);
        for (var i = 0; i < _glowLevels.Length; i++)
        {
            var next = Mathf.MoveToward(_glowLevels[i], _glowTargets[i], step);
            if (next == _glowLevels[i]) continue;
            _glowLevels[i] = next;
            _environment.SetGlowLevel(i, next);
        }
    }
}
