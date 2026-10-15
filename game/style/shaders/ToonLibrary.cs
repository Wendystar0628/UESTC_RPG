using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Style.Materials;

/// <summary>
/// 卡通材质库（ENG-ARCH §3、ENG-PERF §1.4–§1.6），实现跨包的 <see cref="IToonMaterialProvider"/>：
/// 按 <see cref="ToonSpec"/> 取白名单着色器的材质，分配 7 位材质 ID（1–111；0 未分配，112–127 留给 WP-05 的特殊线类），
/// 生产全局数据纹理 mat_table（经 <see cref="IGlobalDataTextureRegistry"/> 登记，交出释放权）。调色板纹理归 WP-37。
/// </summary>
/// <remarks>
/// 预热：每个材质记着调用方实际登记过的绘制布局（<see cref="TrackLayout"/>）和已有绘制证据的布局。
/// <see cref="CapturePendingWarm"/> 交出还缺证据的材质与布局的快照；待预热的集合一变（新布局、影响管线的画质项变化、
/// 一批完成），修订号就加一，旧批交回来一律拒收。<see cref="IsReady(ToonSpec)"/> 只认已预热：至少登记过一个布局、
/// 且全部布局都由 WP-01 在全遮下实际画过；只建了材质、只登记了布局或只等了时间都不算。
/// </remarks>
[KernelService(typeof(IToonMaterialProvider), Owner = "WP-04")]
public sealed class ToonLibrary : IToonMaterialProvider, IDisposable
{
    public const int FirstId = 1;
    public const int LastId = 111;

    private const string ShaderDirectory = "res://style/shaders/";
    private static readonly StringName MatTableGlobal = new(ShaderGlobals.MatTable);
    private static readonly StringName MatIdParameter = new("mat_id");

    private readonly object _gate = new();
    private readonly PaletteRegistry _registry;
    private readonly IShaderMaterialSource _source;
    private readonly IQualityService? _quality;
    private readonly Dictionary<MaterialClass, ToonEntry> _byClass = [];
    private readonly Dictionary<Material, WarmState> _states = new(ReferenceEqualityComparer.Instance);
    private readonly List<WarmState> _order = [];   // 分配与登记的先后：批内顺序不依赖字典遍历
    private readonly Dictionary<string, Shader> _shaders = new(StringComparer.Ordinal);
    private readonly byte[] _matBytes = new byte[MatTable.ByteCount];
    private readonly Image _matImage;
    private readonly ImageTexture _matTexture;
    private IDisposable? _registration;
    private ToonWarmBatch? _issued;
    private long _revision = 1;
    private int _nextId = FirstId;
    private bool _disposed;

    /// <param name="palettes">色名登记表来源（WP-04 的正式实现，无 GPU）。</param>
    /// <param name="source">白名单非卡通着色器的材质来源（veg_* 等由它建）。</param>
    /// <param name="textures">全局数据纹理的登记处；mat_table 登记后释放权归它。</param>
    /// <param name="quality">画质服务：影响管线的项一变，已有的预热证据作废。</param>
    public ToonLibrary(IPaletteRegistrySource palettes, IShaderMaterialSource source, IGlobalDataTextureRegistry textures,
        IQualityService? quality = null)
    {
        ArgumentNullException.ThrowIfNull(palettes);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(textures);
        _registry = palettes.Registry;
        _source = source;
        _matImage = Image.CreateFromData(MatTable.Width, MatTable.Height, false, Image.Format.Rgba8, _matBytes);
        _matTexture = ImageTexture.CreateFromImage(_matImage);
        // 登记成功后纹理的释放归登记处：本类此后只 Update，不 Dispose、不 FreeRid。
        _registration = textures.RegisterOwned(ShaderGlobals.MatTable, _matTexture, _matTexture.GetRid());
        RenderingServer.GlobalShaderParameterSet(MatTableGlobal, _matTexture);
        _quality = quality;
        if (_quality is not null) _quality.Changed += OnQualityChanged;
    }

    // ---------------------------------------------------------------------
    // 材质与色号
    // ---------------------------------------------------------------------

    /// <summary>
    /// 同一"材质类"共用一个材质：ToonSpec 去掉 Lit、Shade、Deep 后相同，就是同一个材质、同一个 ID。
    /// 颜色只走顶点色号，不逐物体建材质。头一次遇到的材质类要分配 ID、改写 mat_table，只能在主线程。
    /// </summary>
    public Material Get(ToonSpec spec) => Resolve(spec).Material;

    /// <summary>色名的色号（受光色名打包进顶点 COLOR.r；影 = 受光 + 1）。登记表不可变，线程安全。</summary>
    public byte PaletteId(PaletteToken token) => _registry.Get(token).Id;

    /// <summary>材质 ID（写进 ROUGHNESS 的 7 位值），会按需分配。</summary>
    public byte MaterialId(ToonSpec spec) => Resolve(spec).Id;

    public bool IsReady(ToonSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        lock (_gate)
        {
            PruneDisposed();
            return !_disposed && _byClass.TryGetValue(MaterialClass.Of(spec), out var entry)
                && _states.TryGetValue(entry.Material, out var state) && state.Ready;
        }
    }

    public void Track(ShaderId shader, Material material)
    {
        ArgumentNullException.ThrowIfNull(material);
        if (!Enum.IsDefined(shader)) throw new ArgumentOutOfRangeException(nameof(shader), shader, "Unknown ShaderId.");
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            PruneDisposed();
            if (!GodotObject.IsInstanceValid(material)) throw new ObjectDisposedException(nameof(material));
            if (_states.TryGetValue(material, out var state))
            {
                if (state.Shader == shader) return;
                throw new ArgumentException(state.Spec is not null
                    ? "This material is a toon material from Get(ToonSpec); its readiness is tracked by spec."
                    : $"This material is already tracked as {state.Shader}, not {shader}.", nameof(material));
            }
            var added = new WarmState(material, null, shader);
            Add(added);
            if (added.Pending) Advance();   // CanvasItem 登记即待预热；Spatial 要等登记布局
        }
    }

    public void TrackLayout(Material material, WarmDrawLayout layout)
    {
        ArgumentNullException.ThrowIfNull(material);
        if (!Enum.IsDefined(layout.Primitive))
            throw new ArgumentOutOfRangeException(nameof(layout), layout.Primitive, "Unknown primitive.");
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            PruneDisposed();
            if (!GodotObject.IsInstanceValid(material)) throw new ObjectDisposedException(nameof(material));
            if (!_states.TryGetValue(material, out var state))
                throw new InvalidOperationException("TrackLayout needs a material returned by Get(ToonSpec) or registered with Track(ShaderId, Material).");
            if (state.Requested.Contains(layout)) return;
            state.Requested.Add(layout);
            state.Order.Add(layout);
            if (!state.Warmed.Contains(layout)) Advance();
        }
    }

    public bool IsReady(ShaderId shader, Material material)
    {
        ArgumentNullException.ThrowIfNull(material);
        lock (_gate)
        {
            PruneDisposed();
            return !_disposed && _states.TryGetValue(material, out var state) && state.Shader == shader && state.Ready;
        }
    }

    // ---------------------------------------------------------------------
    // 预热批
    // ---------------------------------------------------------------------

    public ToonWarmBatch CapturePendingWarm()
    {
        lock (_gate)
        {
            PruneDisposed();
            if (_issued is not null && _issued.Revision == _revision) return _issued;
            var items = new List<ToonWarmMaterial>();
            foreach (var state in _order)
            {
                if (!state.Pending) continue;
                items.Add(new ToonWarmMaterial(state.Spec, state.Material, state.Shader, state.PendingLayouts()));
            }
            _issued = new ToonWarmBatch(_revision, items.AsReadOnly());
            return _issued;
        }
    }

    public bool CompleteWarm(ToonWarmBatch batch)
    {
        if (batch is null) return false;
        lock (_gate)
        {
            PruneDisposed();
            // 只收本类发出的、仍是当前修订号的那一批；修订号变了（新布局、画质变化）证据作废，下一帧重取。
            if (_disposed || !ReferenceEquals(batch, _issued) || batch.Revision != _revision) return false;
            foreach (var item in batch.Materials)
            {
                var state = _states[item.Material];
                state.Drawn = true;
                foreach (var layout in item.Layouts) state.Warmed.Add(layout);
            }
            _issued = null;
            if (batch.Materials.Count > 0) Advance();
            return true;
        }
    }

    // ---------------------------------------------------------------------
    // 内部
    // ---------------------------------------------------------------------

    private ToonEntry Resolve(ToonSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var key = MaterialClass.Of(spec);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            PruneDisposed();
            if (_byClass.TryGetValue(key, out var found)) return found;
            if (OS.GetThreadCallerId() != OS.GetMainThreadId())
                throw new InvalidOperationException("A new toon material class must be allocated on the main thread (mat_table update).");
            _registry.Validate(spec);
            if (_nextId > LastId)
                throw new InvalidOperationException($"Material IDs exhausted ({FirstId}-{LastId}); merge ToonSpec classes before adding more.");
            var id = (byte)_nextId++;
            var material = MaterialFor(spec.Kind);
            material.SetShaderParameter(MatIdParameter, (int)id);
            MatTable.WriteRows(_matBytes, id, spec);
            _matImage.SetData(MatTable.Width, MatTable.Height, false, Image.Format.Rgba8, _matBytes);
            _matTexture.Update(_matImage);
            var entry = new ToonEntry(id, material);
            _byClass.Add(key, entry);
            Add(new WarmState(material, spec, null));
            return entry;
        }
    }

    private void Add(WarmState state)
    {
        _states.Add(state.Material, state);
        _order.Add(state);
    }

    // 借用材质由 owner 卸图时释放。只查原生有效性，不再读取已释放实例的 Shader。
    // 清退后旧批整批失效，存活材质的旧批证据也不能被部分接受。
    private void PruneDisposed()
    {
        var removed = false;
        for (var i = _order.Count - 1; i >= 0; i--)
        {
            var state = _order[i];
            if (GodotObject.IsInstanceValid(state.Material)) continue;
            _states.Remove(state.Material);
            _order.RemoveAt(i);
            if (state.Spec is not null) _byClass.Remove(MaterialClass.Of(state.Spec));
            removed = true;
        }
        if (removed) Advance();
    }

    // 待预热的集合变了：换修订号，已发出的批作废
    private void Advance()
    {
        _revision++;
        _issued = null;
    }

    private void OnQualityChanged(QualityChange change)
    {
        var knobs = Enum.GetValues<QualityKnob>().Where(change.Has);
        if (!knobs.Any(AffectsPipelines)) return;
        lock (_gate)
        {
            if (_disposed) return;
            PruneDisposed();
            var any = false;
            foreach (var state in _order)
            {
                any |= state.Warmed.Count > 0 || state.Drawn;
                state.Reset();
            }
            if (any || _issued is not null) Advance();
        }
    }

    /// <summary>
    /// 会换掉渲染管线变体的画质项（多重采样改渲染通道、软阴影档改特化常量）：这些一变，已画过的布局要重新预热。
    /// 只影响距离、密度、后期或下次启动的项不作废证据。新项只在末尾追加；这里穷举，没登记的值直接报错（ENG-ONBOARD §3.2）。
    /// </summary>
    private static bool AffectsPipelines(QualityKnob knob) => knob switch
    {
        QualityKnob.AntiAliasing or QualityKnob.ShadowQuality => true,
        QualityKnob.DisplayMode or QualityKnob.WindowSize or QualityKnob.VSync or QualityKnob.FrameCap or QualityKnob.Vrr
            or QualityKnob.RenderScale or QualityKnob.FsrSharpness or QualityKnob.Anisotropy
            or QualityKnob.ShadowDistance or QualityKnob.SmallCasters
            or QualityKnob.GlowQuality or QualityKnob.TiltShift or QualityKnob.LensFlare or QualityKnob.LightShafts
            or QualityKnob.DetailScale or QualityKnob.MeshLodThreshold
            or QualityKnob.GeometricLeaves or QualityKnob.FoliageDensity or QualityKnob.GrassRadius
            or QualityKnob.ReflectionInterval or QualityKnob.ParticleDensity
            or QualityKnob.Adaptive or QualityKnob.GraphicsApi or QualityKnob.GpuIndex
            or QualityKnob.Vignette => false,
        _ => throw new ArgumentOutOfRangeException(nameof(knob), knob, "Unclassified QualityKnob.")
    };

    private ShaderMaterial MaterialFor(ToonKind kind)
    {
        // 新 Kind 只在末尾追加；这里穷举，没有登记的值直接报错（ENG-ONBOARD §3.2）。
        // 卡通系（toon_*）由本类建；植被与荷（veg_*）的着色器归 WP-23、WP-38，经 IShaderMaterialSource 建。
        // 主线版：归属的包还没交付时退到同类的已有着色器，交付后自动接上，不改调用方。
        return kind switch
        {
            ToonKind.Static => Toon("toon_static", "toon_static"),
            ToonKind.Glass => Toon("toon_glass", "toon_static"),
            ToonKind.GlassNear => Toon("toon_glass_near", "toon_glass"),
            ToonKind.Cut => Toon("toon_cut", "toon_static"),
            ToonKind.VegTrunk => Whitelisted(ShaderId.VegTrunk),
            ToonKind.VegCard => Whitelisted(ShaderId.VegCard),
            ToonKind.VegImpostor => Whitelisted(ShaderId.VegImpostor),
            ToonKind.VegGrass => Whitelisted(ShaderId.VegGrass),
            ToonKind.Lotus => Whitelisted(ShaderId.VegLotus),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown ToonKind.")
        };
    }

    private ShaderMaterial Whitelisted(ShaderId id) =>
        ShaderMaterialSource.Exists(id) ? _source.Create(id) : Toon("toon_static", "toon_static");

    private ShaderMaterial Toon(string file, string fallback) => new()
    {
        Shader = LoadShader(file) ?? LoadShader(fallback)
            ?? throw new FileNotFoundException($"Whitelisted shader missing: {ShaderDirectory}{fallback}.gdshader")
    };

    private Shader? LoadShader(string file)
    {
        if (_shaders.TryGetValue(file, out var cached)) return cached;
        var path = ShaderDirectory + file + ".gdshader";
        if (!ResourceLoader.Exists(path, "Shader")) return null;
        var shader = ResourceLoader.Load<Shader>(path);
        _shaders.Add(file, shader);
        return shader;
    }

    /// <summary>
    /// 退出时先停更新、清掉全局绑定，再注销登记；纹理由登记处在最后一个租约结束后释放。
    /// 本类建的材质与 Image 在此释放；Track 借用的材质和登记处持有的纹理不在此释放。
    /// </summary>
    public void Dispose()
    {
        Material[] owned;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _issued = null;
            owned = _byClass.Values.Select(entry => entry.Material).Distinct<Material>(ReferenceEqualityComparer.Instance).ToArray();
            _byClass.Clear();
            _states.Clear();
            _order.Clear();
            _shaders.Clear();
        }
        if (_quality is not null) _quality.Changed -= OnQualityChanged;
        try
        {
            RenderingServer.GlobalShaderParameterSet(MatTableGlobal, default(Variant));
        }
        finally
        {
            try { _registration?.Dispose(); }
            finally
            {
                _registration = null;
                foreach (var material in owned)
                    if (GodotObject.IsInstanceValid(material)) material.Dispose();
                if (GodotObject.IsInstanceValid(_matImage)) _matImage.Dispose();
            }
        }
    }

    private sealed record ToonEntry(byte Id, Material Material);

    /// <summary>
    /// 一个材质的预热状态：调用方登记的布局（按登记先后）与已有绘制证据的布局。
    /// Spatial 按布局算；CanvasItem 按实际画布绘制算。其他模式没有对应预热路径，保持不就绪。
    /// </summary>
    private sealed class WarmState(Material material, ToonSpec? spec, ShaderId? shader)
    {
        public Material Material { get; } = material;
        public ToonSpec? Spec { get; } = spec;          // 卡通材质：分配这个材质类的那份 ToonSpec
        public ShaderId? Shader { get; } = shader;      // 非卡通材质：Track 时给的白名单着色器
        private Godot.Shader.Mode? Mode { get; } = material switch
        {
            ShaderMaterial { Shader: { } s } => s.GetMode(),
            _ => null
        };
        public HashSet<WarmDrawLayout> Requested { get; } = [];
        public List<WarmDrawLayout> Order { get; } = [];
        public HashSet<WarmDrawLayout> Warmed { get; } = [];
        public bool Drawn { get; set; }

        public bool Ready => Mode switch
        {
            Godot.Shader.Mode.Spatial => Order.Count > 0 && Order.All(Warmed.Contains),
            Godot.Shader.Mode.CanvasItem => Drawn,
            _ => false
        };

        public bool Pending => Mode switch
        {
            Godot.Shader.Mode.Spatial => Order.Any(layout => !Warmed.Contains(layout)),
            Godot.Shader.Mode.CanvasItem => !Drawn,
            _ => false
        };

        public WarmDrawLayout[] PendingLayouts() =>
            Mode == Godot.Shader.Mode.Spatial ? Order.Where(layout => !Warmed.Contains(layout)).ToArray() : [];

        public void Reset()
        {
            Warmed.Clear();
            Drawn = false;
        }
    }

    /// <summary>材质类：ToonSpec 里决定着色器、明暗档和线表的字段；颜色字段不在内。</summary>
    private readonly record struct MaterialClass(ToonKind Kind, ShadeSteps Steps, float Band, ToonNormal Normal,
        LineClass Line, ToonFx Fx, PaletteToken? Backface, ChannelId? Emissive, float Translucency, float Wet)
    {
        public static MaterialClass Of(ToonSpec s) =>
            new(s.Kind, s.Steps, s.Band, s.Normal, s.Line, s.Fx, s.Backface, s.Emissive, s.Translucency, s.Wet);
    }
}
