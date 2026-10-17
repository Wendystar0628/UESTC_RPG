using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Gen.Water;
using Campus.Kernel.Materials;
using Godot;

namespace Campus.Systems.Water;

/// <summary>
/// Godot side of the water system (WLD-WATER, ENG-PERF): the planar reflection of the lake that fills most
/// of the screen. A SubViewport with its own World3D (no shadow-casting light there), a mirrored camera, and every
/// N frames one render; reflect_tex, reflect_vp and reflect_lake are written for water.gdshader. The reflection is a
/// style floor: it never switches off, only its refresh interval follows the quality setting (2 / 3 / 4 frames).
/// Water surfaces themselves are static content (Campus.Gen.Water.WaterModule).
/// </summary>
[KernelNode("systems.water", Owner = "WP-24")]
[QualityResponsive(Owns = [QualityKnob.ReflectionInterval])]
public partial class WaterSystem : Node, IMapAware, IQualityAware
{
    private static readonly StringName ReflectTex = ShaderGlobals.ReflectTex;
    private static readonly StringName ReflectVp = ShaderGlobals.ReflectVp;
    private static readonly StringName ReflectLake = ShaderGlobals.ReflectLake;

    private static readonly StringName WaterYParam = "water_y";

    private readonly IMediumState? _medium;
    private readonly IWorldState? _world;
    private readonly IShaderMaterialSource? _materials;
    private ShaderMaterial? _proxyMaterial;
    private SubViewport? _viewport;
    private Camera3D? _mirror;
    private Godot.Environment? _environment;
    private Godot.Environment? _sourceEnvironment;
    private ReflectionLakes _lakes = new([]);
    private int _interval = 4;
    private long _frame;
    private int _lake = -1;
    private long _instance = -1;
    private Camera3D? _frustum;
    private readonly Func<System.Numerics.Vector3, bool> _inFrustum;

    public WaterSystem() => _inFrustum = InFrustum;

    public WaterSystem(IMediumState medium, IWorldState world, IShaderMaterialSource materials) : this()
    {
        _medium = medium;
        _world = world;
        _materials = materials;
    }

    /// <summary>Lake number that currently has the planar reflection, or -1.</summary>
    public int ReflectedLake => _lake;

    /// <summary>Reflection World3D for the REFLECT proxies (mounted by the kernel; see the WP-24 contract request).</summary>
    public World3D? ReflectionWorld => _viewport?.FindWorld3D();

    /// <summary>
    /// The reflect_flat override for every proxy in the reflection world. Its water_y follows the reflected lake,
    /// so fragments under the water plane are dropped (4.7 has no oblique near plane, ENG-PERF).
    /// </summary>
    public ShaderMaterial? ReflectionProxyMaterial => _proxyMaterial;

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
        RenderingServer.GlobalShaderParameterSet(ReflectTex, _viewport.GetTexture());
        RenderingServer.GlobalShaderParameterSet(ReflectLake, -1);
        GetViewport().SizeChanged += OnSizeChanged;
    }

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= OnSizeChanged;
        RenderingServer.GlobalShaderParameterSet(ReflectLake, -1);
    }

    public void OnMapUnloading(MapId from)
    {
        _lakes = new([]);
        _lake = -1;
        RenderingServer.GlobalShaderParameterSet(ReflectLake, -1);
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var bodies = WaterCatalog.Build(ctx.World, ctx.Descriptor);
        _lakes = ReflectionLakes.FromBodies(bodies, WaterParams.Default.AreaSamples);
        _lake = -1;
        _frame = 0;
    }

    public void ApplyQuality(QualityView view, in QualityChange change)
    {
        ArgumentNullException.ThrowIfNull(view);
        // Style floor: every 2–4 frames, never off (SYS-GRAPHICS); anything outside clamps to the worst value.
        var n = (int)MathF.Round(view[QualityKnob.ReflectionInterval]);
        _interval = n is >= 2 and <= 4 ? n : 4;
    }

    public override void _Process(double delta)
    {
        if (_viewport is null || _mirror is null) return;
        if (_world is not null && _world.Instance != _instance)
        {
            _instance = _world.Instance;
            _lakes.Reset();
        }
        // Underground and underwater the reflection is invisible: stop rendering it (SYS-VIEW). Not a quality option.
        var kind = _medium?.Current.Kind ?? MediumKind.Air;
        if (kind is MediumKind.Underwater or MediumKind.Underground)
        {
            _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            return;
        }
        // The mirror must match the camera that renders this frame exactly, including its projection,
        // so it follows the viewport's current Camera3D rather than the rig's pose.
        var main = GetViewport().GetCamera3D();
        if (main is null || _lakes.Lakes.Count == 0) return;
        if (_frame++ % _interval != 0) return;

        var eye = main.GlobalPosition;
        _frustum = main;
        var slot = _lakes.Select(new(eye.X, eye.Y, eye.Z), _inFrustum);
        var h = _lakes.WaterY(slot);
        if (slot != _lake)
        {
            _lake = slot;
            RenderingServer.GlobalShaderParameterSet(ReflectLake, slot);
            if (slot >= 0) _proxyMaterial?.SetShaderParameter(WaterYParam, h);
        }
        if (slot < 0) return;
        SyncEnvironment();
        var t = main.GlobalTransform;
        var position = new Vector3(eye.X, 2f * h - eye.Y, eye.Z);
        var forward = -t.Basis.Z;
        var up = t.Basis.Y;
        forward.Y = -forward.Y;
        up.Y = -up.Y;
        _mirror.Fov = main.Fov;
        _mirror.Near = main.Near;
        _mirror.Far = main.Far;
        _mirror.KeepAspect = main.KeepAspect;
        _mirror.CullMask = main.CullMask;
        _mirror.LookAtFromPosition(position, position + forward, up);
        var vp = _mirror.GetCameraProjection() * new Projection(_mirror.GlobalTransform.AffineInverse());
        RenderingServer.GlobalShaderParameterSet(ReflectVp, vp);
        _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
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

    private bool InFrustum(System.Numerics.Vector3 p) => _frustum?.IsPositionInFrustum(new(p.X, p.Y, p.Z)) ?? false;

    private Vector2I ReflectionSize()
    {
        var size = GetViewport().GetVisibleRect().Size;
        return new(Math.Max(1, (int)(size.X / 4f)), Math.Max(1, (int)(size.Y / 4f)));
    }

    private void OnSizeChanged()
    {
        if (_viewport is not null) _viewport.Size = ReflectionSize();
    }
}
