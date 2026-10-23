using Campus.Kernel.Diagnostics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Renderer;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Sky;

/// <summary>
/// game/systems/sky/ all-day sky (ART-SKYLIGHT; SYS-WEATHER). Owns the Sky resource and its material,
/// the global texture cloud_noise and the globals stars and cloud (ENG-PERF).
/// Inputs: IWorldState (world time, anim time, weather vector) and IColorScriptFrame (star visibility, cloud cover);
/// colours come from the palette texture inside the shader. Nothing is read back from shader globals.
/// </summary>
[KernelNode("systems.sky")]
public partial class SkyNode : Node, IMapAware, IViewContentReadiness, IRegistryExitParticipant
{
    private static readonly StringName _shaderGlobalCloud = ShaderGlobals.Cloud;
    private static readonly StringName _shaderGlobalCloudNoise = ShaderGlobals.CloudNoise;
    private static readonly StringName _shaderGlobalStars = ShaderGlobals.Stars;

    private static readonly StringName CloudCoverParam = new("cloud_cover");
    private static readonly StringName CloudThrParam = new("cloud_thr");

    private IWorldState? _world;
    private IColorScriptFrame? _script;
    private ShaderMaterial? _material;
    private Godot.Sky? _sky;
    private Godot.Environment? _environment;
    private IDisposable? _noiseRegistration;
    private ImageTexture? _noiseTexture;
    private float[]? _thresholds;
    private SkyMapData? _map;
    private float _sentCover = -1f;
    private float _sentThr = -1f;
    private bool _exiting;
    public Exception? Failure { get; private set; }
    public bool IsReady(Pose pose) => Failure is null && _map is not null && _material is not null &&
        _environment is not null && _noiseTexture is not null && _thresholds is not null;

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Systems;
        try { Initialize(); }
        catch (Exception error) { RecordFailure(error); }
    }

    private void Initialize()
    {
        _world = KernelServices.Get<IWorldState>();
        _script = KernelServices.Get<IColorScriptFrame>();
        _material = CreateMaterial();
        _sky = new Godot.Sky
        {
            SkyMaterial = _material,
            RadianceSize = Godot.Sky.RadianceSizeEnum.Size32,
            ProcessMode = Godot.Sky.ProcessModeEnum.Incremental,
        };
        UploadNoise();
        TryAttach();
    }

    public void OnMapUnloading(MapId from) => _map = null;

    public void OnMapLoaded(IMapContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var path = ctx.DataRoot + SkyMapData.FileName;
        var bytes = KernelServices.Get<IRequiredResources>().ReadBytes(path);
        _map = SkyMapData.Parse(bytes);
    }

    public override void _Process(double delta)
    {
        if (_exiting || Failure is not null) return;
        try { UpdateContent(); }
        catch (Exception error) { RecordFailure(error); }
    }

    // The map runtime owns readiness failures; do not also send the same cause to application shutdown.
    private void RecordFailure([FailureHandler("ENG-ARCH")] Exception error)
    {
        Failure = RetainedFailure.Append(Failure, error);
    }

    private void UpdateContent()
    {
        if (_world is null || _script is null || _map is null) return;
        if (_environment is null) TryAttach();

        var weather = _world.WeatherNow;
        var frame = _script.Current;

        var drift = Drift(_world.WorldHours, _world.AnimTime, _map.CloudDrift);
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalCloud,
            new Vector4(drift.X, drift.Y, Clamp01(weather.CloudType), Clamp01(weather.Cb)));

        // 星空：色彩脚本给的可见度，再按阴度与雾淡出；被云挡住的部分在着色器里逐像素处理（ART-SKYLIGHT）
        var stars = Clamp01(frame.Stars) * (1f - Clamp01(weather.Overcast))
            * (1f - Mathf.SmoothStep(1f, SkyTuning.StarsFogHide, weather.Fog));
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalStars, stars);

        if (_material is null || _thresholds is null) return;
        var cover = Clamp01(frame.CloudCover);
        // 色彩脚本的云量是"看上去占天空多少"；近地平线的云因为透视会挤在一起，所以换算到云底平面上的覆盖率时取凸映射
        var thr = CloudNoise.ThresholdFor(_thresholds, MathF.Pow(cover, SkyTuning.PlaneCoverageGamma));
        if (Math.Abs(cover - _sentCover) > SkyTuning.UniformEpsilon)
        {
            _material.SetShaderParameter(CloudCoverParam, cover);
            _sentCover = cover;
        }
        if (Math.Abs(thr - _sentThr) > SkyTuning.UniformEpsilon)
        {
            _material.SetShaderParameter(CloudThrParam, thr);
            _sentThr = thr;
        }
    }

    /// <summary>
    /// cloud.xy = f(world time) + g(anim time), wrapped so float precision holds over long saves.
    /// World time only grows, so clouds never run backwards; fast flow turns f into a time-lapse (ART-SKYLIGHT).
    /// </summary>
    public static Vector2 Drift(double worldHours, double animTime, CloudDrift drift)
    {
        ArgumentNullException.ThrowIfNull(drift);
        var az = drift.TowardDeg * Math.PI / 180.0;
        var distance = drift.SpeedMps * worldHours * 3600.0;
        // 噪声坐标 = 世界坐标 + 漂移，所以漂移取反，云才朝 towardDeg 走（方向约定见 CORE-INDEX：(sin A, ·, −cos A)）
        var x = -Math.Sin(az) * distance;
        var z = Math.Cos(az) * distance;
        var theta = 2.0 * Math.PI * animTime / WorldTime.AnimLoopSeconds;
        x += SkyTuning.AnimLoopRadiusMeters * Math.Cos(theta);
        z += SkyTuning.AnimLoopRadiusMeters * Math.Sin(theta);
        return new Vector2((float)Wrap(x), (float)Wrap(z));
    }

    private static double Wrap(double v)
    {
        var w = v % SkyTuning.DriftWrapMeters;
        return w < 0 ? w + SkyTuning.DriftWrapMeters : w;
    }

    private ShaderMaterial CreateMaterial() => KernelServices.Get<IShaderMaterialSource>().Create(ShaderId.Sky);

    private void TryAttach()
    {
        if (_sky is null) return;
        var environment = GetViewport()?.FindWorld3D()?.Environment;
        if (environment is null) return;
        // 字段边界（Opus 定案）：本节点只写 BackgroundMode 和 Sky。环境光源、反射源、雾、Glow、Tonemap、
        // Adjustment 等归 game/kernel/post/ / game/kernel/renderer/，一概不碰（ENG-BUDGET"Environment 与 Sky"）。
        environment.Sky = _sky;
        environment.BackgroundMode = Godot.Environment.BGMode.Sky;
        _environment = environment;
    }

    private void UploadNoise()
    {
        // The resource loader owns the cached Image; only the new texture transfers to the registry.
        var image = KernelServices.Get<IRequiredResources>().Load<Image>(CloudNoise.ResourcePath);
        if (image.GetWidth() != CloudNoise.Size || image.GetHeight() != CloudNoise.Size ||
            image.GetFormat() != Image.Format.Rgba8 ||
            image.GetMipmapCount() != System.Numerics.BitOperations.Log2((uint)CloudNoise.Size) ||
            !image.HasMeta(CloudNoise.ThresholdMetadata))
            throw new InvalidDataException("The required cloud noise resource has an invalid image layout or no coverage table.");
        using var metadata = image.GetMeta(CloudNoise.ThresholdMetadata);
        if (metadata.VariantType != Variant.Type.PackedFloat32Array)
            throw new InvalidDataException("The required cloud noise coverage table is not PackedFloat32Array.");
        var thresholds = metadata.AsFloat32Array();
        if (thresholds.Length != CloudNoise.CoverageSteps ||
            Array.Exists(thresholds, value => !float.IsFinite(value) || value < 0f || value > 1f))
            throw new InvalidDataException("The required cloud noise coverage table is invalid.");
        var texture = ImageTexture.CreateFromImage(image);
        try
        {
            _noiseRegistration = KernelServices.Get<IGlobalDataTextureRegistry>()
                .RegisterOwned(ShaderGlobals.CloudNoise, texture, texture.GetRid());
        }
        catch
        {
            texture.Dispose();
            throw;
        }
        _noiseTexture = texture;
        _thresholds = thresholds;
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalCloudNoise, texture);
    }

    public void PrepareForExit()
    {
        _exiting = true;
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalCloudNoise, default(Variant));
        _noiseRegistration?.Dispose();
        _noiseRegistration = null;
        _noiseTexture = null;
    }

    public Task DrainForExitAsync() => Task.CompletedTask;

    public override void _ExitTree() => PrepareForExit();

    private static float Clamp01(float v) => float.IsFinite(v) ? Math.Clamp(v, 0f, 1f) : 0f;
}
