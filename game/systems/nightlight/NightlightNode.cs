using System.Runtime.InteropServices;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Bake;
using Campus.Contracts.Startup;
using Campus.Kernel.Instances;
using Campus.Kernel.Lighting;
using Campus.Kernel.Renderer;
using Campus.Systems.Nightlight.Glow;
using Campus.Systems.Nightlight.Schedule;
using Campus.Systems.Nightlight.Lamps;
using Campus.Systems.Nightlight.Windows;
using Godot;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Systems.Nightlight;

/// <summary>Dynamic 32-channel lighting and resident lamp textures; large glow buffers belong to stream tiles.</summary>
[KernelNode("light-schedule")]
public partial class NightlightNode : Node, IMapAware, IViewContentReadiness, IViewCapturePreparation, IStreamTileParticipant
{
    private static readonly StringName _shaderGlobalLampGrid = ShaderGlobals.LampGrid;
    private static readonly StringName _shaderGlobalLampTable = ShaderGlobals.LampTable;
    private static readonly StringName _shaderGlobalWindowThresholds = ShaderGlobals.WindowThresholds;
    private static readonly StringName _shaderGlobalWindowOverride = ShaderGlobals.WindowOverride;

    private readonly N.Vector2[] _channels = new N.Vector2[LightChannelCatalog.Count];
    private readonly WindowOverrides _windows = new();
    private ScheduleEvaluator? _schedule;
    private IWorldState? _world;
    private ILightChannelSink? _sink;
    private IColorScriptFrame? _colors;
    private ILightInventory? _inventory;
    private GlowTileMounts? _glows;
    private readonly NightlightReflections _reflections = new();
    private ICameraRig? _camera;
    private Pose? _appliedPose;
    private long _appliedGlowRevision = -1;
    private ReadOnlyMemory<float> _thresholds;
    private ImageTexture? _thresholdTexture;
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
        if (Failure is not null || _windows.Dirty || _map is null || _inventory is null || _inventoryRev != _inventory.Rev || _appliedPose != pose ||
            _tableTexture is null || _gridTexture is null || _overrideTexture is null || _thresholdTexture is null || !_reflections.Ready || _glows is null || !_glows.Ready || _appliedGlowRevision != _glows.Revision) return false;
        return true;
    }
    public StreamMountKind Kind => StreamMountKind.Auxiliary;
    public Exception? Failure => _glows?.Failure;

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
    }

    public override void _Ready()
    {
        _world = KernelServices.Get<IWorldState>();
        _world.InstanceChanged += OnWorldInstanceChanged;
        _sink = KernelServices.Get<ILightChannelSink>();
        _colors = KernelServices.Get<IColorScriptFrame>();
        _inventory = KernelServices.Get<ILightInventory>();
        _glows = new(KernelServices.Get<IInstancePoolHost>(), KernelServices.Get<IShaderMaterialSource>());
        _camera = KernelServices.Get<ICameraRig>();
    }

    public override void _ExitTree()
    {
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        _windows.Bind(null);
        _inventoryRev = long.MinValue;
        _glows?.Dispose();
        _glows = null;
        _reflections.Dispose();
        FreeTextures();
    }

    public void OnMapUnloading(MapId from)
    {
        _schedule = null;
        _map = null;
        Lamps = [];
        _reflections.Dispose();
        _windows.Bind(null);
        _inventoryRev = long.MinValue;
        _glows?.Bind(null);
        FreeTextures();
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var bytes = Godot.FileAccess.GetFileAsBytes(ctx.DataRoot + NightlightBakeProducer.ScheduleFile);
        if (bytes.Length == 0) throw new InvalidDataException("Missing map lighting schedule.");
        _schedule = NightlightBakeProducer.ReadSchedule(bytes, ctx.Descriptor.Id);
        _glows!.Bind(ctx.Descriptor.Id);
        _map = ctx;
        BindInventory(ctx);
        _windows.ClearAll();
    }

    public override void _Process(double delta)
    {
        if (_map is not null && _inventory is not null && _inventory.Rev != _inventoryRev) BindInventory(_map);
        if (_camera is { } camera) ApplyLighting(camera.Pose, delta);
    }

    private void ApplyLighting(Pose pose, double delta)
    {
        if (_schedule is null || _world is null || _sink is null || _map is null ||
            _inventory is null || _inventoryRev != _inventory.Rev) return;
        var weather = _world.WeatherNow;
        var gain = _colors?.Current.LampPoolGain ?? 1f;
        _schedule.Evaluate(new ScheduleInput(_world.Clock, _world.Sun.AltDeg, weather.Overcast, weather.Precip, gain, delta), _channels);
        _sink.Submit(_channels);
        if (_glows is { } glows)
        {
            _reflections.Update(Lamps, glows.ReflectionTiles, _channels, _windows.Data, _thresholds.Span, pose);
            _appliedGlowRevision = glows.Revision;
        }
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
        if (catalog.WindowThresholds.Length != WindowNumberingSnapshot.Capacity)
            throw new InvalidDataException("Missing baked window thresholds.");
        _thresholds = catalog.WindowThresholds;
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
        using var thresholds = Image.CreateFromData(WindowOverrides.Side, WindowOverrides.Side, false,
            Image.Format.Rf, MemoryMarshal.AsBytes(_thresholds.Span));
        if (_thresholdTexture is null) _thresholdTexture = ImageTexture.CreateFromImage(thresholds);
        else _thresholdTexture.SetImage(thresholds);
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalWindowThresholds, _thresholdTexture);
        EnsureOverrideTexture();
        _reflections.Clear();
        _inventoryRev = inventory.Rev;
        _appliedPose = null;
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

    private void FreeTextures()
    {
        _overrideImage?.Dispose();
        _overrideTexture?.Dispose();
        _tableTexture?.Dispose();
        _gridTexture?.Dispose();
        _thresholdTexture?.Dispose();
        _overrideImage = null;
        _overrideTexture = null;
        _tableTexture = null;
        _gridTexture = null;
        _thresholdTexture = null;
        _thresholds = default;
        _appliedPose = null;
    }

    public bool CanMount(BakeKey key) => _glows?.CanMount(key) ?? false;
    public StreamTileNeed Demand(in BakeTileEntry entry, in StreamDemandView view, bool resident) =>
        _glows?.Demand(entry, view, resident) ?? StreamTileNeed.None;
    public IStreamMountWork CreateMount(BakeKey key, object product, BakePriority priority, long revision) =>
        (_glows ?? throw new InvalidOperationException("Nightlight glow mounting is not ready.")).CreateMount(key, product, priority, revision);
    public bool IsReady(BakeKey key) => _glows?.IsReady(key) ?? false;
    public void Unmount(BakeKey key) => _glows?.Unmount(key);
}
