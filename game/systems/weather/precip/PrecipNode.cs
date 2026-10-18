using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Kernel.Instances;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Weather.Precip;

/// <summary>
/// 降水与湿润的运行时节点（WP-45b）：在 FramePriority.Motion 那一步
/// ① 把天气向量换算成天气类全局参数 weather_a、weather_b、weather_fog（contract-5 第 5 条）；
/// ② 求降水池这一帧的可见实例数与 instance uniform（PrecipModel），写进内核的动态实例池；
/// 切图时重建 precip_occ（主线版是常量纹理）并重设降水池的 CustomAabb。
/// 负责画面设置的"降水与粒子密度"（SYS-GRAPHICS）。
/// 降水池由内核建（ENG-ARCH，IInstancePoolHost）：启动时就挂上、整块写一次实例缓冲，运行时只改可见数与 instance uniform。
/// </summary>
[KernelNode("systems.weather.precip", Owner = "WP-45b")]
[QualityResponsive(Owns = [QualityKnob.ParticleDensity])]
public partial class PrecipNode : Node, IQualityAware, IMapAware
{
    private static readonly StringName _shaderInstancePrecipD = InstanceShaderParameters.PrecipD;
    private static readonly StringName _shaderInstancePrecipK = InstanceShaderParameters.PrecipK;
    private static readonly StringName _shaderInstancePrecipV = InstanceShaderParameters.PrecipV;
    private static readonly StringName _shaderGlobalPrecipOcc = ShaderGlobals.PrecipOcc;
    private static readonly StringName _shaderGlobalWeatherA = ShaderGlobals.WeatherA;
    private static readonly StringName _shaderGlobalWeatherB = ShaderGlobals.WeatherB;
    private static readonly StringName _shaderGlobalWeatherFog = ShaderGlobals.WeatherFog;

    /// <summary>降水池在 ENG-PERF 图层登记里的第 6 层（粒子）：相机可见、不投影、不进倒影世界。</summary>
    private const uint PoolLayers = 1u << 5;

    private readonly PrecipModel _model = new();
    private IWorldState? _world;
    private IWindField? _wind;
    private ICameraRig? _camera;
    private ShaderMaterial? _material;
    private IInstancePool? _pool;
    private float _density = 1f;
    private int _stepRate = 12;
    private Image? _occImage;
    private ImageTexture? _occ;

    /// <summary>本帧的降水池输出（可见数与 instance uniform 的值）。</summary>
    public PrecipFrame Frame { get; private set; }

    /// <summary>本图降水池的 CustomAabb（可达范围"中心 ± 3 km"）；没有地图时罩住原点 ± 3 km。</summary>
    public Box3 PoolBounds { get; private set; } = PrecipModel.PoolBounds(System.Numerics.Vector2.Zero, 3000f, -100f, 2000f);

    public override void _EnterTree() => ProcessPriority = FramePriority.Motion;

    public override void _Ready()
    {
        _world = Services.Get<IWorldState>();
        _wind = Services.Get<IWindField>();
        _camera = Services.Get<ICameraRig>();
        _material = Services.Get<IShaderMaterialSource>().Create(ShaderId.Precip);
        var (position, uv, index) = PrecipModel.BuildQuad();
        _pool = Services.Get<IInstancePoolHost>().Mount(new InstancePoolSpec(
            "precip", "WP-45b", position, uv, index, _material,
            PrecipTuning.Capacity, ToAabb(PoolBounds), PoolLayers));
        _pool.WriteBuffer(PrecipModel.BuildBuffer());
    }

    public override void _ExitTree()
    {
        _pool?.Free();
        _pool = null;
    }

    public override void _Process(double delta)
    {
        if (_world is null || _wind is null || _camera is null || _pool is null) return;
        var w = _world.WeatherNow;
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalWeatherA, ToGodot(WeatherGlobalsMap.A(w)));
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalWeatherB, ToGodot(WeatherGlobalsMap.B(w)));
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalWeatherFog, ToGodot(WeatherGlobalsMap.Fog(w)));
        var eye = _camera.Pose.Position;
        var gust = _wind.Sample(eye.X, eye.Z, _world.AnimTime).Gust;
        Frame = _model.Step(w, _world.Instance, _world.Step12, gust, _density, _stepRate);
        _pool.VisibleCount = Frame.VisibleCount;
        _pool.SetParameter(_shaderInstancePrecipD, ToGodot(Frame.D));
        _pool.SetParameter(_shaderInstancePrecipV, ToGodot(Frame.V));
        _pool.SetParameter(_shaderInstancePrecipK, ToGodot(Frame.K));
    }

    public void ApplyQuality(QualityView view, in QualityChange change)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (change.Has(QualityKnob.ParticleDensity)) _density = view[QualityKnob.ParticleDensity];
        _stepRate = view.StepRate;
    }

    public void OnMapUnloading(MapId from)
    {
        // 纹理在下一张图的 OnMapLoaded 里原地更新（尺寸不变用 Update），这里不释放，免得材质失效。
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var d = ctx.Descriptor;
        PoolBounds = PrecipModel.PoolBounds(d.Center, d.Roam.MaxRadius, d.Roam.FloorY, d.Roam.CeilY);
        _pool?.SetAabb(ToAabb(PoolBounds));
        // 主线版：precip_occ 是常量顶面（遮蔽高度图的烘焙在后置细节里，ENG-WP）；全图纹理按 map_rect 采样，尺寸不影响取值。
        var size = PrecipTuning.OccConstantSize;
        _occImage ??= Image.CreateEmpty(size, size, false, Image.Format.Rh);
        _occImage.Fill(new Color(PrecipTuning.OccConstantTop, 0f, 0f, 1f));
        if (_occ is null)
        {
            _occ = ImageTexture.CreateFromImage(_occImage);
            RenderingServer.GlobalShaderParameterSet(_shaderGlobalPrecipOcc, _occ);
        }
        else
        {
            _occ.Update(_occImage);
        }
    }

    private static Vector4 ToGodot(System.Numerics.Vector4 v) => new(v.X, v.Y, v.Z, v.W);

    private static Aabb ToAabb(Box3 box) => new(box.Min.ToGodot(), (box.Max - box.Min).ToGodot());
}
