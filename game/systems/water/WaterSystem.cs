using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Content;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Water;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Systems.Water;

/// <summary>
/// Godot side of the water system (WLD-WATER, ENG-PERF): the planar reflection of the lake that fills most
/// of the screen. A SubViewport with its own World3D (no shadow-casting light there), a mirrored camera, and every
/// N frames one render; reflect_tex, reflect_vp and reflect_lake are written for water.gdshader. The reflection is a
/// style floor: it never switches off, only its refresh interval follows the quality setting (2 / 3 / 4 frames).
/// Water surfaces themselves are static content supplied by the map's water module.
/// </summary>
[KernelNode("systems.water")]
[KernelService(typeof(IReflectionWorld))]
[QualityResponsive(Owns = [QualityKnob.ReflectionInterval], Reads = [QualityKnob.RenderScale])]
public partial class WaterSystem : Node, IMapAware, IQualityAware, IReflectionWorld
{
    private static readonly StringName ReflectTex = ShaderGlobals.ReflectTex;
    private static readonly StringName ReflectVp = ShaderGlobals.ReflectVp;
    private static readonly StringName ReflectLake = ShaderGlobals.ReflectLake;

    private static readonly StringName WaterYParam = "water_y";

    private readonly IMediumState? _medium;
    private readonly IWorldState? _world;
    private readonly IShaderMaterialSource? _materials;
    private readonly IMapContentSource? _content;
    private readonly IBakeContextSource? _builds;
    private readonly IMeasuredViewportRegistry? _measurements;
    private IDisposable? _viewportMeasurement;
    private ShaderMaterial? _proxyMaterial;
    private SubViewport? _viewport;
    private Camera3D? _mirror;
    private Godot.Environment? _environment;
    private Godot.Environment? _sourceEnvironment;
    private ReflectionLakes _lakes = new([]);
    private int _interval = 4;
    private long _frame;
    private int _lake = -1;
    private readonly N.Plane[] _renderFrustum = new N.Plane[6];
    private readonly N.Plane[] _demandFrustum = new N.Plane[6];
    private bool _available;

    public WaterSystem() { }

    public WaterSystem(IMediumState medium, IWorldState world, IShaderMaterialSource materials,
        IMapContentSource content, IBakeContextSource builds, IMeasuredViewportRegistry measurements) : this()
    {
        _medium = medium;
        _world = world;
        _materials = materials;
        _content = content;
        _builds = builds;
        _measurements = measurements;
    }

    /// <summary>Lake number that currently has the planar reflection, or -1.</summary>
    public int ReflectedLake => _lake;

    /// <summary>Reflection World3D for the REFLECT proxies (mounted by the kernel; see the game/systems/water/ contract request).</summary>
    public World3D? ReflectionWorld => _viewport?.FindWorld3D();

    /// <summary>
    /// The reflect_flat override for every proxy in the reflection world. Its water_y follows the reflected lake,
    /// so fragments under the water plane are dropped (4.7 has no oblique near plane, ENG-PERF).
    /// </summary>
    public ShaderMaterial? ReflectionProxyMaterial => _proxyMaterial;

    public Rid Scenario => _available ? ReflectionWorld?.Scenario ?? default : default;
    public Material ProxyMaterial => Scenario.IsValid && ReflectionProxyMaterial is { } material ? material :
        throw new InvalidOperationException("The reflection world is unavailable; check Scenario before reading ProxyMaterial.");
    public event Action? Changed;

    public override void _EnterTree()
    {
        if (_world is not null) _world.InstanceChanged += OnWorldInstanceChanged;
        _lake = -1;
    }

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Systems;
        _viewport = new SubViewport
        {
            Name = "WaterReflection",
            OwnWorld3D = true,
            Msaa3D = Viewport.Msaa.Disabled,
            ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled,
            PositionalShadowAtlasSize = 0,
            UseOcclusionCulling = false,
            // Transparent where no proxy is drawn: water.gdshader puts its analytic sky there, so planar and
            // analytic reflections share one sky and the reflection world needs no sky pass.
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
            Size = ReflectionSize(),
        };
        _mirror = new Camera3D { Name = "MirrorCamera", Current = true };
        _environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.ClearColor,
            AmbientLightSource = Godot.Environment.AmbientSource.Disabled,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
            GlowEnabled = false,
            FogEnabled = false,
        };
        _proxyMaterial = _materials?.Create(ShaderId.ReflectFlat);
        _viewport.AddChild(_mirror);
        _viewport.AddChild(new WorldEnvironment { Name = "ReflectionSky", Environment = _environment });
        AddChild(_viewport);
        RegisterMeasurement();
        RenderingServer.GlobalShaderParameterSet(ReflectTex, _viewport.GetTexture());
        RenderingServer.GlobalShaderParameterSet(ReflectLake, -1);
        GetWindow().SizeChanged += OnSizeChanged;
        _available = _proxyMaterial is not null;
        if (_available) Changed?.Invoke();
    }

    public override void _ExitTree()
    {
        UnregisterMeasurement();
        // Consumers synchronously release their proxy RIDs while the old world and material still exist.
        _available = false;
        Changed?.Invoke();
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        GetWindow().SizeChanged -= OnSizeChanged;
        _lake = -1;
        RenderingServer.GlobalShaderParameterSet(ReflectLake, -1);
    }

    public void OnMapUnloading(MapId from)
    {
        UnregisterMeasurement();
        if (_viewport is not null) _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        _lakes = new([]);
        _lake = -1;
        RenderingServer.GlobalShaderParameterSet(ReflectLake, -1);
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var content = _content ?? throw new InvalidOperationException("Map content source is unavailable.");
        var builds = _builds ?? throw new InvalidOperationException("Prepared build context source is unavailable.");
        var module = content.GetModule<IWaterContentSource>(ctx, "water");
        var bodies = module.GetWaterBodies(builds.GetPreparedContext(ctx, "water"));
        _lakes = ReflectionLakes.FromBodies(bodies, ReflectionLakes.AreaSamples);
        _lake = -1;
        _frame = 0;
        RegisterMeasurement();
    }

    private void RegisterMeasurement()
    {
        if (_viewportMeasurement is null && _viewport is not null)
            _viewportMeasurement = _measurements?.Register(_viewport.GetViewportRid(), "倒影");
    }

    private void UnregisterMeasurement()
    {
        _viewportMeasurement?.Dispose();
        _viewportMeasurement = null;
    }

    private void OnWorldInstanceChanged(long _)
    {
        _lake = -1;
        _frame = 0;
        RenderingServer.GlobalShaderParameterSet(ReflectLake, -1);
    }

    /// <remarks>The six planes are reusable scratch: consume the returned view before the next query.
    /// Near remains the source's world-space ground loading envelope; reflection leaves its XZ coverage unchanged.</remarks>
    public bool TryGetDemand(in StreamDemandView source, out StreamDemandView reflected)
    {
        reflected = default;
        if (!TrySelect(source.Eye.Position, source.Frustum.Span, out _, out var h)) return false;
        if (source.Frustum.Length != _demandFrustum.Length)
            throw new ArgumentException("A reflection demand requires six frustum planes.", nameof(source));
        for (var i = 0; i < _demandFrustum.Length; i++)
            _demandFrustum[i] = ReflectionLakes.ReflectPlane(source.Frustum.Span[i], h);
        var eye = source.Eye with
        {
            Position = ReflectionLakes.ReflectPoint(source.Eye.Position, h),
            PitchDeg = -source.Eye.PitchDeg
        };
        reflected = new(eye, _demandFrustum, source.Near);
        return true;
    }

    private bool TrySelect(N.Vector3 eye, ReadOnlySpan<N.Plane> frustum, out int slot, out float waterY)
    {
        slot = -1;
        waterY = float.NegativeInfinity;
        if (!Scenario.IsValid) return false;
        // Demand is conservative for every source view. The current body's medium cannot reject a
        // destination or prediction, and demand queries must not sample unprepared destination terrain.
        slot = _lakes.Select(eye, frustum, _lake);
        if (slot < 0) return false;
        waterY = _lakes.WaterY(slot);
        return true;
    }

    public void ApplyQuality(QualityView view, in QualityChange change)
    {
        ArgumentNullException.ThrowIfNull(view);
        // Style floor: every 2–4 frames, never off (SYS-GRAPHICS); anything outside clamps to the worst value.
        var n = (int)MathF.Round(view[QualityKnob.ReflectionInterval]);
        _interval = n is >= 2 and <= 4 ? n : 4;
        if (change.Has(QualityKnob.RenderScale)) OnSizeChanged();
    }

    public override void _Process(double delta)
    {
        if (_viewport is null || _mirror is null) return;
        var drawRequested = UpdateReflection(_viewport, _mirror);
        if (_viewportMeasurement is not null)
            _measurements!.ReportDraw(_viewport.GetViewportRid(), Engine.GetProcessFrames(), drawRequested);
    }

    private bool UpdateReflection(SubViewport viewport, Camera3D mirror)
    {
        // Keep the existing live rendering policy; this does not gate other source views' demand.
        var kind = _medium?.Current.Kind ?? MediumKind.Air;
        if (kind is MediumKind.Underwater or MediumKind.Underground)
        {
            viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            return false;
        }
        // The mirror must match the camera that renders this frame exactly, including its projection,
        // so it follows the viewport's current Camera3D rather than the rig's pose.
        var main = GetViewport().GetCamera3D();
        if (main is null || _lakes.Lakes.Count == 0) return false;
        if (_frame++ % _interval != 0) return false;

        var eye = main.GlobalPosition;
        var transform = main.GlobalTransform;
        var cameraTransform = main.GetCameraTransform();
        var projection = main.GetCameraProjection();
        // Get the existing camera's six planes without allocating an engine Array or a second camera.
        CameraFrustum.Fill(cameraTransform, projection, _renderFrustum);
        if (!TrySelect(new(eye.X, eye.Y, eye.Z), _renderFrustum, out var slot, out var h))
        {
            viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            return false;
        }
        if (slot != _lake)
        {
            _lake = slot;
            RenderingServer.GlobalShaderParameterSet(ReflectLake, slot);
            _proxyMaterial?.SetShaderParameter(WaterYParam, h);
        }
        SyncEnvironment();
        var position = ReflectionLakes.ReflectPoint(new(eye.X, eye.Y, eye.Z), h);
        var f = -transform.Basis.Z;
        var u = transform.Basis.Y;
        var forward = ReflectionLakes.ReflectDirection(new(f.X, f.Y, f.Z));
        var up = ReflectionLakes.ReflectDirection(new(u.X, u.Y, u.Z));
        mirror.Fov = main.Fov;
        mirror.Near = main.Near;
        mirror.Far = main.Far;
        mirror.KeepAspect = main.KeepAspect;
        mirror.CullMask = main.CullMask;
        mirror.LookAtFromPosition(new(position.X, position.Y, position.Z),
            new(position.X + forward.X, position.Y + forward.Y, position.Z + forward.Z), new(up.X, up.Y, up.Z));
        var vp = mirror.GetCameraProjection() * new Projection(mirror.GlobalTransform.AffineInverse());
        RenderingServer.GlobalShaderParameterSet(ReflectVp, vp);
        viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        return true;
    }

    /// <summary>The reflection uses the main world's tone mapping (ENG-PERF); sky, ambient, glow and fog stay off.</summary>
    private void SyncEnvironment()
    {
        var source = GetViewport().FindWorld3D()?.Environment;
        if (source is null || _environment is null || ReferenceEquals(source, _sourceEnvironment)) return;
        _sourceEnvironment = source;
        _environment.TonemapMode = source.TonemapMode;
        _environment.TonemapExposure = source.TonemapExposure;
        _environment.TonemapWhite = source.TonemapWhite;
    }

    private Vector2I ReflectionSize()
    {
        // Window pixels are independent of UI canvas scaling; quality owns the applied 3D scale.
        var size = GetWindow().Size;
        var scale = GetViewport().Scaling3DScale / 4f;
        return new(Math.Max(1, (int)(size.X * scale)), Math.Max(1, (int)(size.Y * scale)));
    }

    private void OnSizeChanged()
    {
        if (_viewport is not null) _viewport.Size = ReflectionSize();
    }
}
