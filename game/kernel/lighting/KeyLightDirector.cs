using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Lighting;

/// <summary>
/// The only DirectionalLight3D in the scene ("KeyLight"), shared by the sun and the moon
/// (ART-SKYLIGHT 3.1, ENG-PERF 10.1). Runs in the key-light step after WorldState and writes
/// key_dir, key_strength, shadow_strength, retreat_y and overcast_dir. Its colour and energy stay
/// fixed: the key colour and intensity reach the toon materials only through the palette and globals.
/// </summary>
[KernelNode("key-light")]
[QualityResponsive(Order = 1, Owns = [QualityKnob.ShadowQuality, QualityKnob.ShadowDistance])]
public partial class KeyLightDirector : Node, IQualityAware
{
    private static readonly StringName _shaderGlobalKeyDir = ShaderGlobals.KeyDir;
    private static readonly StringName _shaderGlobalKeyStrength = ShaderGlobals.KeyStrength;
    private static readonly StringName _shaderGlobalOvercastDir = ShaderGlobals.OvercastDir;
    private static readonly StringName _shaderGlobalRetreatY = ShaderGlobals.RetreatY;
    private static readonly StringName _shaderGlobalShadowStrength = ShaderGlobals.ShadowStrength;

    /// <summary>
    /// PCSS light size, 0 = PCSS off. Compared in the D0 scene at 0 / 0.1 / 0.2 / 0.45° under the toon
    /// threshold: any PCSS turns edges grainy (0.45°) or lumpy (0.1–0.2°), and long low-sun shadows
    /// from far casters vanish. Fixed at startup (specialization constant), never changed at runtime.
    /// </summary>
    public const float AngularDistanceDeg = 0f;
    /// <summary>Chosen against canopy acne where crown spheres intersect (0.1 / 2.5 left light slivers).</summary>
    public const float ShadowBiasValue = 0.05f;
    public const float ShadowNormalBiasValue = 4f;

    private readonly KeyLightModel _model = new();
    private IWorldState? _world;
    private IKeyLightPublisher? _state;
    private DirectionalLight3D? _light;
    private ShadowSettings _settings = ShadowSettings.From(1, 1600f);
    private N.Vector3 _writtenDir = new(float.NaN);
    private N.Vector3 _writtenOvercast = new(float.NaN);

    public KeyLightFrame Last { get; private set; }

    public override void _EnterTree()
    {
        ProcessPriority = FramePriority.KeyLight;
        if (_world is not null) _world.InstanceChanged += OnWorldInstanceChanged;
        _model.Reset();
    }

    public override void _Ready()
    {
        _world = Services.Get<IWorldState>();
        _world.InstanceChanged += OnWorldInstanceChanged;
        _state = Services.Get<IKeyLightPublisher>();
        EnsureLight();
        Step();
    }

    public override void _Process(double delta) => Step();

    public override void _ExitTree()
    {
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
    }

    private void OnWorldInstanceChanged(long _) => _model.Reset();

    public void ApplyQuality(QualityView view, in QualityChange change)
    {
        ArgumentNullException.ThrowIfNull(view);
        _settings = ShadowSettings.From(view);
        var light = EnsureLight();
        RenderingServer.DirectionalShadowAtlasSetSize(_settings.AtlasSize, true);
        light.DirectionalShadowMode = _settings.Splits == 4
            ? DirectionalLight3D.ShadowMode.Parallel4Splits
            : DirectionalLight3D.ShadowMode.Parallel2Splits;
        light.DirectionalShadowMaxDistance = _settings.MaxDistance;
        light.DirectionalShadowSplit1 = _settings.Split1;
        if (_settings.Splits == 4)
        {
            light.DirectionalShadowSplit2 = _settings.Split2;
            light.DirectionalShadowSplit3 = _settings.Split3;
        }
        light.ShadowCasterMask = Last.ShadowPass ? _settings.CasterMask : 0u;
    }

    private DirectionalLight3D EnsureLight()
    {
        if (_light is not null) return _light;
        _light = new DirectionalLight3D
        {
            Name = "KeyLight",
            ShadowEnabled = true,
            LightAngularDistance = AngularDistanceDeg,
            ShadowBias = ShadowBiasValue,
            ShadowNormalBias = ShadowNormalBiasValue,
            ShadowReverseCullFace = true,
            ShadowBlur = 1f,
            ShadowOpacity = 1f,
            DirectionalShadowBlendSplits = true,
            DirectionalShadowFadeStart = 0.85f,
            SkyMode = DirectionalLight3D.SkyModeEnum.LightOnly,
            LightEnergy = 1f,
            ShadowCasterMask = _settings.CasterMask,
        };
        AddChild(_light);
        return _light;
    }

    private void Step()
    {
        if (_world is null || _light is null) return;
        var frame = _model.Step(KeyLightInput.From(_world));
        Last = frame;
        _state?.Publish(frame);

        if (frame.LightDir != _writtenDir)
        {
            _writtenDir = frame.LightDir;
            var dir = frame.LightDir.ToGodot();
            var up = Mathf.Abs(dir.Y) > 0.999f ? Vector3.Back : Vector3.Up;
            _light.GlobalTransform = new Transform3D(Basis.LookingAt(-dir, up), Vector3.Zero);
            RenderingServer.GlobalShaderParameterSet(_shaderGlobalKeyDir, dir);
        }
        if (frame.OvercastDir != _writtenOvercast)
        {
            _writtenOvercast = frame.OvercastDir;
            RenderingServer.GlobalShaderParameterSet(_shaderGlobalOvercastDir, frame.OvercastDir.ToGodot());
        }
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalKeyStrength, frame.KeyStrength);
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalShadowStrength, frame.ShadowStrength);
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalRetreatY, frame.RetreatY);

        // Idle pass: opacity 0 skips shadow sampling, mask 0 culls every caster (neither is a
        // specialization constant). Restoring happens in a frame whose shadow_strength is still 0.
        var opacity = frame.ShadowPass ? 1f : 0f;
        if (_light.ShadowOpacity != opacity) _light.ShadowOpacity = opacity;
        var mask = frame.ShadowPass ? _settings.CasterMask : 0u;
        if (_light.ShadowCasterMask != mask) _light.ShadowCasterMask = mask;
    }
}
