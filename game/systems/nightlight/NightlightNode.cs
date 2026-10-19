using System.Runtime.InteropServices;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Instances;
using Campus.Kernel.Lighting;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Campus.Systems.Nightlight.Glow;
using Campus.Systems.Nightlight.Lamps;
using Campus.Systems.Nightlight.Schedule;
using Campus.Systems.Nightlight.Windows;
using Godot;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Nightlight;

/// <summary>
/// 夜景灯光的时刻表节点（WP-39；SYS-TIME、MAP-QSH-ADDITIONS、ENG-PERF）。
/// 每帧在 FramePriority.Channels 按钟点、有效太阳高度 h_eff 与季节求 32 个通道的 (L, Δ)，经 WP-03 的
/// ILightChannelSink 写进 light_channels；按图读时刻表、建灯表与倒影索引（lamp_table、lamp_grid、refl_lamps）、管窗覆盖（window_override），
/// 写全局参数 shaft。灯具与窗格读 WP-02 的全图清单 ILightInventory，清单的 Rev 变了就重建灯表和光晕；
/// 灯头光晕与窗光晕各挂一个内核实例池（IInstancePoolHost）。注册 id 与 WP-03 的桩相同，合入后自动替掉桩。
/// </summary>
[KernelNode("light-schedule", Owner = "WP-39")]
public partial class NightlightNode : Node, IMapAware, IViewContentReadiness
{
    private static readonly StringName _shaderGlobalLampGrid = ShaderGlobals.LampGrid;
    private static readonly StringName _shaderGlobalLampTable = ShaderGlobals.LampTable;
    private static readonly StringName _shaderGlobalShaft = ShaderGlobals.Shaft;
    private static readonly StringName _shaderGlobalWindowOverride = ShaderGlobals.WindowOverride;

    private const string ScheduleFile = "lighting-schedule.json";
    private const uint LampSeed = 0x39u;

    // 光柱（ART-STYLE、ENG-PERF）：近镜 1.2 → 5.5 m 淡入、顺轴衰减 0.4；整体强度按太阳高度 2°–30° 与天气
    private const float ShaftNear = 1.2f;
    private const float ShaftFar = 5.5f;
    private const float ShaftAxisFalloff = 0.4f;

    private readonly N.Vector2[] _channels = new N.Vector2[LightChannelCatalog.Count];
    private readonly WindowOverrides _windows = new();
    private readonly GlowMount _lampGlow = new(GlowKind.Lamp, "nightlight.glow.lamp");
    private readonly GlowMount _windowGlow = new(GlowKind.Window, "nightlight.glow.window");
    private ScheduleEvaluator? _schedule;
    private IWorldState? _world;
    private ILightChannelSink? _sink;
    private IColorScriptFrame? _colors;
    private ILightInventory? _inventory;
    private IInstancePoolHost? _pools;
    private IShaderMaterialSource? _materials;
    private IMapContext? _map;
    private ITerrainPreparation? _terrain;
    private Task? _groundPreparation;
    private long _preparingRevision = long.MinValue;
    private long _inventoryRev = long.MinValue;
    private Image? _overrideImage;
    private ImageTexture? _overrideTexture;
    private ImageTexture? _tableTexture;
    private ImageTexture? _gridTexture;

    /// <summary>本图灯表里的灯（只读，光斑、光晕与倒影索引共用）。</summary>
    public IReadOnlyList<LampRecord> Lamps { get; private set; } = [];
    public bool IsReady(Pose pose) => _map is not null && _inventory is not null && _inventoryRev == _inventory.Rev;
    public string? Error => _groundPreparation?.Exception?.GetBaseException().Message;

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
        _terrain = Services.Get<ITerrainPreparation>();
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
        _groundPreparation = null; _preparingRevision = long.MinValue;
        _lampGlow.Free();
        _windowGlow.Free();
        ClearReflectionLamps();
    }

    public void OnMapUnloading(MapId from)
    {
        _schedule = null;
        _map = null;
        _groundPreparation = null; _preparingRevision = long.MinValue;
        Lamps = [];
        ClearReflectionLamps();
        _windows.Bind(null);
        _inventoryRev = long.MinValue;
        _lampGlow.Free();
        _windowGlow.Free();
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var bytes = Godot.FileAccess.GetFileAsBytes(ctx.DataRoot + ScheduleFile);
        if (bytes.Length == 0) throw new InvalidDataException($"Missing {ScheduleFile} for map {ctx.Descriptor.Id.Value}.");
        var file = JsonSerializer.Deserialize<LightingScheduleFile>(bytes, ContractJson.Options)
            ?? throw new InvalidDataException($"{ScheduleFile} is empty.");
        if (file.Map != ctx.Descriptor.Id)
            throw new InvalidDataException($"{ScheduleFile} belongs to map {file.Map.Value}, not {ctx.Descriptor.Id.Value}.");
        var schedule = new ScheduleEvaluator(file);
        for (var slot = 0; slot < LightChannelCatalog.Count; slot++)
            if (schedule.Spec(slot).Id != LightChannelCatalog.Id(slot))
                throw new InvalidDataException($"{ScheduleFile} slot {slot} is '{schedule.Spec(slot).Id.Value}', catalog says '{LightChannelCatalog.Id(slot).Value}'.");
        _schedule = schedule;
        _map = ctx;
        RebuildFromInventory(ctx, schedule);
        _windows.ClearAll();
        EnsureOverrideTexture();
    }

    public override void _Process(double delta)
    {
        if (_schedule is null || _world is null || _sink is null) return;
        // 后台烘焙补完、换图后清单 Rev 加 1：重建灯表与光晕（与清单的顺序无关）
        if (_map is not null && _inventory is not null && _inventory.Rev != _inventoryRev) RebuildFromInventory(_map, _schedule);
        var weather = _world.WeatherNow;
        var gain = _colors?.Current.LampPoolGain ?? 1f;
        var input = new ScheduleInput(_world.Clock, _world.Sun.AltDeg, weather.Overcast, weather.Precip, gain, delta);
        _schedule.Evaluate(input, _channels);
        _sink.Submit(_channels);
        UpdateReflectionLamps();
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalShaft,
            new Vector4(ShaftStrength(_world.Sun.AltDeg, weather), ShaftNear, ShaftFar, ShaftAxisFalloff));
        if (_windows.TakeUpload(delta) && _overrideImage is not null && _overrideTexture is not null)
        {
            _overrideImage.SetData(WindowOverrides.Side, WindowOverrides.Side, false, Image.Format.Rgba8, _windows.Data);
            _overrideTexture.Update(_overrideImage);
        }
    }

    private void OnWorldInstanceChanged(long _)
    {
        _schedule?.Reset();
        _windows.ClearAll();
    }

    private void RebuildFromInventory(IMapContext ctx, ScheduleEvaluator schedule)
    {
        // WP-02 在地图准备阶段一起发布清单和窗号；只借用同一版数据，不重新分配窗号或复制库存。
        var inventory = _inventory ?? throw new InvalidOperationException("The lighting inventory is not ready.");
        var revision = inventory.Rev;
        var numbering = inventory.WindowNumbering ?? throw new InvalidDataException("The lighting inventory has no prepared window numbering.");
        if (numbering.Map != ctx.Descriptor.Id)
            throw new InvalidDataException("The lighting inventory belongs to a different map.");
        var emitters = inventory.Lamps;
        if (_groundPreparation is { } pending)
        {
            if (!pending.IsCompleted) return;
            pending.GetAwaiter().GetResult();
        }
        if (_preparingRevision != revision)
        {
            var points = emitters.Select(e => new N.Vector2(e.Position.X, e.Position.Z)).Distinct().ToArray();
            _preparingRevision = revision;
            _groundPreparation = (_terrain ??= Services.Get<ITerrainPreparation>()).PreparePoints(ctx, "nightlight.lamp-bases", points);
            if (!_groundPreparation.IsCompleted) return;
            _groundPreparation.GetAwaiter().GetResult();
        }
        var facades = inventory.Facades;
        var windows = inventory.Windows;
        _windows.Bind(numbering);
        var palette = Services.Get<IPaletteCatalogSource>().Catalog;
        var lamps = LampSetBuilder.Build(emitters, schedule,
            token => palette.TryGet(token, out var entry) ? entry.Index.Value : null,
            (x, z) => ctx.World.Spatial.GroundY(x, z), LampSeed ^ ctx.Descriptor.Seed, out var report);
        Lamps = lamps;
        var rect = ctx.Descriptor.DataRect;
        var table = LampTable.PackTable(lamps);
        var grid = LampTable.PackGrid(lamps, rect, out var size, out var overflow);
        var tableImage = Image.CreateFromData(Math.Max(lamps.Count, 1), 2, false, Image.Format.Rgbaf, MemoryMarshal.AsBytes(table.AsSpan()));
        var gridImage = Image.CreateFromData(size.Width, size.Height, false, Image.Format.Rgbaf, MemoryMarshal.AsBytes(grid.AsSpan()));
        if (_tableTexture is null) _tableTexture = ImageTexture.CreateFromImage(tableImage);
        else _tableTexture.SetImage(tableImage);
        if (_gridTexture is null) _gridTexture = ImageTexture.CreateFromImage(gridImage);
        else _gridTexture.SetImage(gridImage);
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalLampTable, _tableTexture);
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalLampGrid, _gridTexture);
        var windowSet = GlowSets.Windows(facades, windows, out var windowReport);
        // 倒影只借用同一次窗光晕构建的缓冲，不再次枚举、复制整份窗清单。
        _reflectionWindows = windowSet;
        _reflectionWaterfrontBuildings ??= WaterfrontWindows.Buildings(ctx.World, numbering);
        _reflectionWindowIndices = WaterfrontWindows.Indices(numbering, _reflectionWaterfrontBuildings, windowSet);
        UpdateReflectionLamps();
        if (_pools is not null && _materials is not null)
        {
            ShaderMaterial Material() => _materials.Create(ShaderId.FxGlow);
            _lampGlow.Upload(_pools, Material, GlowSets.Lamps(lamps));
            _windowGlow.Upload(_pools, Material, windowSet);
        }
        _inventoryRev = revision;
        GD.Print($"nightlight: map {ctx.Descriptor.Id.Value} rev={_inventoryRev} lamps={report.Accepted} grid={size.Width}x{size.Height} " +
            $"overflowCells={overflow} skipped(channel={report.UnknownChannel}, notLamp={report.NotLamp}, color={report.UnknownColor}, dup={report.Duplicates}) " +
            $"windows={windowReport.Windows} skipped(channel={windowReport.UnknownChannel}, facade={windowReport.BadFacade})");
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
