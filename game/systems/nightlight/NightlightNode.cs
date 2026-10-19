using System.Runtime.InteropServices;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Bake;
using Campus.Contracts.Startup;
using Campus.Kernel.Instances;
using Campus.Kernel.Lighting;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Campus.Systems.Nightlight.Glow;
using Campus.Systems.Nightlight.Schedule;
using Campus.Systems.Nightlight.Windows;
using Godot;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Nightlight;

/// <summary>Dynamic 32-channel lighting and resident lamp textures; large glow buffers belong to stream tiles.</summary>
[KernelNode("light-schedule", Owner = "WP-39")]
public partial class NightlightNode : Node, IMapAware, IViewContentReadiness, IViewCapturePreparation, IStreamTileParticipant
{
    private static readonly StringName _shaderGlobalLampGrid = ShaderGlobals.LampGrid;
    private static readonly StringName _shaderGlobalLampTable = ShaderGlobals.LampTable;
    private static readonly StringName _shaderGlobalShaft = ShaderGlobals.Shaft;
    private static readonly StringName _shaderGlobalWindowOverride = ShaderGlobals.WindowOverride;

    // 光柱（ART-STYLE、ENG-PERF）：近镜 1.2 → 5.5 m 淡入、顺轴衰减 0.4；整体强度按太阳高度 2°–30° 与天气
    private const float ShaftNear = 1.2f;
    private const float ShaftFar = 5.5f;
    private const float ShaftAxisFalloff = 0.4f;

    private readonly N.Vector2[] _channels = new N.Vector2[LightChannelCatalog.Count];
    private readonly WindowOverrides _windows = new();
    private ScheduleEvaluator? _schedule;
    private IWorldState? _world;
    private ILightChannelSink? _sink;
    private IColorScriptFrame? _colors;
    private ILightInventory? _inventory;
    private IInstancePoolHost? _pools;
    private IShaderMaterialSource? _materials;
    private ShaderMaterial? _lampMaterial;
    private ShaderMaterial? _windowMaterial;
    private IMapContext? _map;
    private long _inventoryRev = long.MinValue;
    private Image? _overrideImage;
    private ImageTexture? _overrideTexture;
    private ImageTexture? _tableTexture;
    private ImageTexture? _gridTexture;

    /// <summary>本图灯表里的灯（只读，光斑、光晕与倒影索引共用）。</summary>
    public IReadOnlyList<LampRecord> Lamps { get; private set; } = [];
    public bool IsReady(Pose pose)
    {
        if (Error is not null || _windows.Dirty || _map is null || _inventory is null || _inventoryRev != _inventory.Rev || _appliedPose != pose ||
            _tableTexture is null || _gridTexture is null || _overrideTexture is null || _reflectionTexture is null) return false;
        foreach (var tile in _mounted.Values) if (!tile.Mount.IsReady) return false;
        return true;
    }
    public string? Error { get; private set; }

    public void PrepareCapture(Pose pose)
    {
        _schedule?.Reset();
        ApplyLighting(pose, 0d);
    }

    public override void _EnterTree()
    {
        ProcessPriority = FramePriority.Channels;
        _inventoryRev = long.MinValue;
        if (_world is not null) _world.InstanceChanged += OnWorldInstanceChanged;
        OnWorldInstanceChanged(0);
        if (_overrideTexture is not null) EnsureOverrideTexture();
        if (_reflectionTexture is not null) ClearReflectionLamps();
    }

    public override void _Ready()
    {
        _world = Services.Get<IWorldState>();
        _world.InstanceChanged += OnWorldInstanceChanged;
        _sink = Services.Get<ILightChannelSink>();
        _colors = Services.Get<IColorScriptFrame>();
        _inventory = Services.Get<ILightInventory>();
        _pools = Services.Get<IInstancePoolHost>();
        _materials = Services.Get<IShaderMaterialSource>();
        _reflectionCamera = Services.Get<ICameraRig>();
        EnsureOverrideTexture();
        ClearReflectionLamps();
    }

    public override void _ExitTree()
    {
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        _windows.Bind(null);
        _inventoryRev = long.MinValue;
        FreeTiles();
        _lampMaterial?.Dispose();
        _windowMaterial?.Dispose();
        _lampMaterial = null;
        _windowMaterial = null;
        ClearReflectionLamps();
    }

    public void OnMapUnloading(MapId from)
    {
        _schedule = null;
        _map = null;
        Lamps = [];
        ClearReflectionLamps();
        _windows.Bind(null);
        _inventoryRev = long.MinValue;
        FreeTiles();
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var bytes = Godot.FileAccess.GetFileAsBytes(ctx.DataRoot + NightlightBakeProducer.ScheduleFile);
        if (bytes.Length == 0) throw new InvalidDataException("Missing map lighting schedule.");
        _schedule = NightlightBakeProducer.ReadSchedule(bytes, ctx.Descriptor.Id);
        _map = ctx;
        Error = null;
        BindInventory(ctx);
        _windows.ClearAll();
        EnsureOverrideTexture();
    }

    public override void _Process(double delta)
    {
        if (_map is not null && _inventory is not null && _inventory.Rev != _inventoryRev) BindInventory(_map);
        if (_reflectionCamera is { } camera) ApplyLighting(camera.Pose, delta);
    }

    private void ApplyLighting(Pose pose, double delta)
    {
        if (_schedule is null || _world is null || _sink is null) return;
        var weather = _world.WeatherNow;
        var gain = _colors?.Current.LampPoolGain ?? 1f;
        _schedule.Evaluate(new ScheduleInput(_world.Clock, _world.Sun.AltDeg, weather.Overcast, weather.Precip, gain, delta), _channels);
        _sink.Submit(_channels);
        UpdateReflectionLamps(pose);
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalShaft,
            new Vector4(ShaftStrength(_world.Sun.AltDeg, weather), ShaftNear, ShaftFar, ShaftAxisFalloff));
        if (_windows.TakeUpload(delta) && _overrideImage is not null && _overrideTexture is not null)
        {
            _overrideImage.SetData(WindowOverrides.Side, WindowOverrides.Side, false, Image.Format.Rgba8, _windows.Data);
            _overrideTexture.Update(_overrideImage);
        }
        _appliedPose = pose;
    }

    private void OnWorldInstanceChanged(long _)
    {
        _schedule?.Reset();
        _windows.ClearAll();
        _appliedPose = null;
    }

    private void BindInventory(IMapContext ctx)
    {
        var inventory = _inventory ?? throw new InvalidOperationException("The lighting inventory is not ready.");
        var numbering = inventory.WindowNumbering ?? throw new InvalidDataException("Missing prepared window numbering.");
        if (numbering.Map != ctx.Descriptor.Id) throw new InvalidDataException("The lighting inventory belongs to a different map.");
        _windows.Bind(numbering);
        var catalog = inventory.Nightlight;
        Lamps = catalog.Lamps;
        var tableImage = Image.CreateFromData(Math.Max(Lamps.Count, 1), 2, false, Image.Format.Rgbaf,
            MemoryMarshal.AsBytes(catalog.LampTable.Span));
        var gridImage = Image.CreateFromData(catalog.GridWidth, catalog.GridHeight, false, Image.Format.Rgbaf,
            MemoryMarshal.AsBytes(catalog.LampGrid.Span));
        if (_tableTexture is null) _tableTexture = ImageTexture.CreateFromImage(tableImage);
        else _tableTexture.SetImage(tableImage);
        if (_gridTexture is null) _gridTexture = ImageTexture.CreateFromImage(gridImage);
        else _gridTexture.SetImage(gridImage);
        tableImage.Dispose();
        gridImage.Dispose();
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalLampTable, _tableTexture);
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalLampGrid, _gridTexture);
        _inventoryRev = inventory.Rev;
        _appliedPose = null;
    }

    private ShaderMaterial GlowMaterial(GlowKind kind)
    {
        var materials = _materials ?? throw new InvalidOperationException("Nightlight material source is not ready.");
        return kind switch
        {
            GlowKind.Lamp => _lampMaterial ??= materials.Create(ShaderId.FxGlow),
            GlowKind.Window => _windowMaterial ??= materials.Create(ShaderId.FxGlow),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), "Only lamp and window glow tiles are mounted.")
        };
    }

    private void EnsureOverrideTexture()
    {
        if (_overrideTexture is null)
        {
            _overrideImage = Image.CreateFromData(WindowOverrides.Side, WindowOverrides.Side, false, Image.Format.Rgba8, _windows.Data);
            _overrideTexture = ImageTexture.CreateFromImage(_overrideImage);
        }
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalWindowOverride, _overrideTexture);
    }

    /// <summary>光柱的整体强度：太阳 2°–30° 之间才有；雨中的白天不画（太阳雨由 WP-45 的判定另算）；雾天 ×1.5。</summary>
    private static float ShaftStrength(float sunAltDeg, WeatherVec w)
    {
        var sun = ScalarMath.Smoothstep(2f, 6f, sunAltDeg) * (1f - ScalarMath.Smoothstep(24f, 30f, sunAltDeg));
        var rain = ScalarMath.Smoothstep(0.05f, 0.2f, w.Precip * (1f - w.PrecipKind)) * ScalarMath.Smoothstep(0.4f, 0.6f, w.Overcast);
        var fog = 1f + 0.5f * ScalarMath.Smoothstep(1.2f, 2.5f, w.Fog);
        return sun * (1f - rain) * fog;
    }
}
