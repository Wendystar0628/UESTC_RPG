using System.Runtime.InteropServices;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Cinematic;
using Campus.Contracts.Maps;
using Campus.Kernel.Instances;
using Campus.Kernel.Lighting;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Campus.Systems.Nightlight.Cue;
using Campus.Systems.Nightlight.Glow;
using Campus.Systems.Nightlight.Lamps;
using Campus.Systems.Nightlight.Schedule;
using Campus.Systems.Nightlight.Windows;
using Godot;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Nightlight;

/// <summary>
/// 夜景灯光的时刻表节点（WP-39；SYS-TIME §6、MAP-QSH-ADDITIONS §8、ENG-PERF §10.3）。
/// 每帧在 FramePriority.Channels 按钟点、有效太阳高度 h_eff 与季节求 32 个通道的 (L, Δ)，经 WP-03 的
/// ILightChannelSink 写进 light_channels；按图读时刻表、建灯表（lamp_table、lamp_grid）、管窗覆盖（window_override），
/// 写全局参数 shaft。灯具与窗格读 WP-02 的全图清单 ILightInventory，清单的 Rev 变了就重建灯表和光晕；
/// 灯头光晕与窗光晕各挂一个内核实例池（IInstancePoolHost）。注册 id 与 WP-03 的桩相同，合入后自动替掉桩。
/// 同一个节点也是过场"灯光提示"钩子 ILampCueHook 的实现（contract-12 第 4 节），替掉 WP-00 的桩。
/// </summary>
[KernelNode("light-schedule", Owner = "WP-39")]
[KernelService(typeof(ILampCueHook), Owner = "WP-39")]
public partial class NightlightNode : Node, IMapAware, ILampCueHook
{
    private const string ScheduleFile = "lighting-schedule.json";
    private const string ExtrasFile = "cinematics/extras.json";
    private const uint LampSeed = 0x39u;

    // 光柱（ART-STYLE §7.6、ENG-PERF §10.2）：近镜 1.2 → 5.5 m 淡入、顺轴衰减 0.4；整体强度按太阳高度 2°–30° 与天气
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
    private IAppFlow? _flow;
    private ILightInventory? _inventory;
    private IInstancePoolHost? _pools;
    private IShaderMaterialSource? _materials;
    private IMapContext? _map;
    private long _inventoryRev = long.MinValue;
    private Image? _overrideImage;
    private ImageTexture? _overrideTexture;
    private ImageTexture? _tableTexture;
    private ImageTexture? _gridTexture;
    private IReadOnlyList<LampCueDef> _cueDefs = [];
    private LampCue? _cue;
    private long _instance = long.MinValue;

    /// <summary>本图灯表里的灯（只读，给日志、门禁和以后的倒影选灯用）。</summary>
    public IReadOnlyList<LampRecord> Lamps { get; private set; } = [];

    public override void _EnterTree() => ProcessPriority = FramePriority.Channels;

    public override void _Ready()
    {
        _world = Services.Get<IWorldState>();
        _sink = Services.Get<ILightChannelSink>();
        _colors = Services.Get<IColorScriptFrame>();
        _flow = Services.Get<IAppFlow>();
        _inventory = Services.Get<ILightInventory>();
        _pools = Services.Get<IInstancePoolHost>();
        _materials = Services.Get<IShaderMaterialSource>();
        EnsureOverrideTexture();
    }

    public override void _ExitTree()
    {
        _lampGlow.Free();
        _windowGlow.Free();
    }

    public void OnMapUnloading(MapId from)
    {
        EndCue();
        _schedule = null;
        _map = null;
        _cueDefs = [];
        Lamps = [];
        _windows.ClearAll();
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
        _cueDefs = ReadCues(ctx);
        RebuildFromInventory(ctx, schedule);
        _windows.ClearAll();
        EnsureOverrideTexture();
    }

    public override void _Process(double delta)
    {
        if (_schedule is null || _world is null || _sink is null) return;
        // 后台烘焙补完、换图后清单 Rev 加 1：重建灯表与光晕（与清单的顺序无关）
        if (_map is not null && _inventory is not null && _inventory.Rev != _inventoryRev) RebuildFromInventory(_map, _schedule);
        if (_world.Instance != _instance)
        {
            _instance = _world.Instance;
            EndCue();
        }
        if (_cue is not null && !CueAllowed()) EndCue();
        var weather = _world.WeatherNow;
        var gain = _colors?.Current.LampPoolGain ?? 1f;
        var input = new ScheduleInput(_world.Clock, _world.Sun.AltDeg, weather.Overcast, weather.Precip, gain, delta, _world.Instance);
        _schedule.Evaluate(input, _channels);
        _sink.Submit(_channels);
        RenderingServer.GlobalShaderParameterSet(ShaderNames.Shaft,
            new Vector4(ShaftStrength(_world.Sun.AltDeg, weather), ShaftNear, ShaftFar, ShaftAxisFalloff));
        if (_windows.TakeUpload(delta) && _overrideImage is not null && _overrideTexture is not null)
        {
            _overrideImage.SetData(WindowOverrides.Side, WindowOverrides.Side, false, Image.Format.Rgba8, _windows.Data);
            _overrideTexture.Update(_overrideImage);
        }
    }

    // ------------------------------------------------------------------
    // 过场"灯光提示"钩子 ILampCueHook（SYS-TITLE §3.3）：WP-49a 的 CinematicPlayer 调用；只在 Entering 生效，
    // 只改灯光通道纹理（lamp.cue、sys.cue 两个槽位），撤销后各值与不覆盖时完全相同。
    // Seek 在 FramePriority.Camera 调，早于本节点的 Channels，所以同一帧就按新前沿求值；钩子自己不计时。
    // ------------------------------------------------------------------

    bool ILampCueHook.Begin(string cueRef, float targetSunAltDeg) => BeginCue(cueRef, targetSunAltDeg);
    void ILampCueHook.Seek(double timelineSeconds) => SeekCue(timelineSeconds);
    void ILampCueHook.End() => EndCue();

    /// <summary>开始覆盖：cueRef 是 extras.json 里 lampCue 的 ref；targetSunDeg 是过场终点钟点的有效太阳高度（判"本该亮着"的灯）。</summary>
    public bool BeginCue(string cueRef, float targetSunDeg)
    {
        if (_schedule is null || !CueAllowed()) return false;
        var def = _cueDefs.FirstOrDefault(d => d.Ref == cueRef);
        if (def is null) return false;
        _cue = new LampCue(def, targetSunDeg);
        _schedule.Cue = _cue.At(double.NegativeInfinity);
        return true;
    }

    /// <summary>每帧由播放器传入时间轴秒数，推进前沿。</summary>
    public void SeekCue(double timelineSeconds)
    {
        if (_schedule is null || _cue is null) return;
        if (!CueAllowed()) { EndCue(); return; }
        _schedule.Cue = _cue.At(timelineSeconds);
    }

    /// <summary>只有进入过场（Entering）时覆盖生效；别的状态一律不覆盖。</summary>
    private bool CueAllowed()
    {
        if (_flow is null) return false;
        return _flow.State switch
        {
            AppState.Entering => true,
            AppState.Boot or AppState.Title or AppState.Playing or AppState.Leaving or AppState.Quitting => false,
            _ => throw new InvalidOperationException($"Unhandled AppState {_flow.State} for the lamp cue."),
        };
    }

    /// <summary>撤销覆盖（到点、跳过、交还控制、换实例都走这里）。</summary>
    public void EndCue()
    {
        _cue = null;
        if (_schedule is not null) _schedule.Cue = null;
    }

    // ------------------------------------------------------------------

    private static IReadOnlyList<LampCueDef> ReadCues(IMapContext ctx)
    {
        var path = ctx.DataRoot + ExtrasFile;
        if (!Godot.FileAccess.FileExists(path)) return [];
        var extras = JsonSerializer.Deserialize<CinematicExtrasFile>(Godot.FileAccess.GetFileAsBytes(path), ContractJson.Options);
        if (extras is null) return [];
        return extras.Refs.Values.OfType<LampCueDef>().OrderBy(d => d.Ref, StringComparer.Ordinal).ToArray();
    }

    private void RebuildFromInventory(IMapContext ctx, ScheduleEvaluator schedule)
    {
        // 全图的灯具标记与窗格声明：WP-02 汇总各内容模块 Build() 的输出（含外围）。清单是桩或空时灯表为空，
        // 光斑、灯头光晕、窗光晕都不画，地面读到的是"无灯"。
        _inventoryRev = _inventory?.Rev ?? 0;
        IReadOnlyList<LightEmitter> emitters = _inventory?.Lamps ?? [];
        var palette = TryPalette();
        var lamps = LampSetBuilder.Build(emitters, schedule,
            token => palette is not null && palette.TryGet(token, out var entry) ? entry.Index.Value : null,
            (x, z) => ctx.World.Spatial.GroundY(x, z), LampSeed ^ ctx.Descriptor.Seed, _cueDefs, out var report);
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
        RenderingServer.GlobalShaderParameterSet(ShaderNames.LampTable, _tableTexture);
        RenderingServer.GlobalShaderParameterSet(ShaderNames.LampGrid, _gridTexture);

        var windowSet = GlowSets.Windows(_inventory?.Facades ?? [], _inventory?.Windows ?? [], out var windowReport);
        if (_pools is not null && _materials is not null)
        {
            ShaderMaterial Material() => _materials.Create(ShaderId.FxGlow);
            _lampGlow.Upload(_pools, Material, GlowSets.Lamps(lamps));
            _windowGlow.Upload(_pools, Material, windowSet);
        }
        GD.Print($"nightlight: map {ctx.Descriptor.Id.Value} rev={_inventoryRev} lamps={report.Accepted} cue={report.CueLamps} grid={size.Width}x{size.Height} " +
            $"overflowCells={overflow} skipped(channel={report.UnknownChannel}, notLamp={report.NotLamp}, color={report.UnknownColor}, dup={report.Duplicates}) " +
            $"windows={windowReport.Windows} skipped(channel={windowReport.UnknownChannel}, facade={windowReport.BadFacade})");
    }

    // 完整色表（旧 0–255 + ext-v2 bank 1）：灯色号可以是 256–511。
    private static PaletteCatalog? TryPalette()
    {
        try { return Services.Get<IPaletteCatalogSource>().Catalog; }
        catch (InvalidOperationException) { return null; }
    }

    private void EnsureOverrideTexture()
    {
        if (_overrideTexture is not null) return;
        _overrideImage = Image.CreateFromData(WindowOverrides.Side, WindowOverrides.Side, false, Image.Format.Rgba8, _windows.Data);
        _overrideTexture = ImageTexture.CreateFromImage(_overrideImage);
        RenderingServer.GlobalShaderParameterSet(ShaderNames.WindowOverride, _overrideTexture);
    }

    /// <summary>光柱的整体强度：太阳 2°–30° 之间才有；雨中的白天不画（太阳雨由 WP-45 的判定另算）；雾天 ×1.5。</summary>
    private static float ShaftStrength(float sunAltDeg, WeatherVec w)
    {
        var sun = ScheduleMath.SmoothStep(2f, 6f, sunAltDeg) * (1f - ScheduleMath.SmoothStep(24f, 30f, sunAltDeg));
        var rain = ScheduleMath.SmoothStep(0.05f, 0.2f, w.Precip * (1f - w.PrecipKind)) * ScheduleMath.SmoothStep(0.4f, 0.6f, w.Overcast);
        var fog = 1f + 0.5f * ScheduleMath.SmoothStep(1.2f, 2.5f, w.Fog);
        return sun * (1f - rain) * fog;
    }
}
