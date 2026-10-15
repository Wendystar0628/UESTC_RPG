using Campus.Contracts;
using Campus.Kernel.Materials;
using Campus.Style.Palette;
using Godot;

namespace Campus.Style.Materials;

/// <summary>
/// 卡通材质库（ENG-ARCH §3、ENG-PERF §1.4–§1.6）：按 <see cref="ToonSpec"/> 取白名单着色器的 ShaderMaterial，
/// 分配 7 位材质 ID（1–111；0 未分配，112–127 留给 WP-05 的特殊线类），并持有全局数据纹理 mat_table。
/// 对外的跨包接口是契约里的 IToonMaterialProvider（Get / PaletteId / IsReady，以 Astra 合入的为准），合入后由本类实现；
/// IToonLibrary 只在 WP-04 内部用。调色板纹理归 WP-37（生产方），本类不写全局参数 palette。
/// </summary>
public interface IToonLibrary
{
    PaletteRegistry Palette { get; }

    /// <summary>
    /// 同一"材质类"共用一个材质：ToonSpec 去掉 Lit、Shade、Deep 后相同，就是同一个材质、同一个 ID。
    /// 颜色只走顶点色号，不逐物体建材质。只在主线程调用。
    /// </summary>
    ShaderMaterial Get(ToonRef toon);

    /// <summary>材质 ID（写进 ROUGHNESS 的 7 位值），会按需分配。只在主线程调用。</summary>
    byte MaterialId(ToonRef toon);

    /// <summary>受光色名的色号，打包进顶点 COLOR.r；影的色号 = 受光 + 1。线程安全。</summary>
    byte ColorId(PaletteToken lit);

    /// <summary>已分配的全部材质，供启动预热表使用。</summary>
    IReadOnlyList<ShaderMaterial> Allocated { get; }
}

[KernelService(typeof(IToonLibrary), Owner = "WP-04")]
public sealed class ToonLibrary : IToonLibrary
{
    public const int FirstId = 1;
    public const int LastId = 111;

    private const string ShaderDirectory = "res://style/shaders/";
    private static readonly StringName MatTableGlobal = new(ShaderGlobals.MatTable);
    private static readonly StringName MatIdParameter = new("mat_id");

    private readonly PaletteRegistry _registry;
    private readonly IShaderMaterialSource _source;
    private readonly Dictionary<MaterialClass, (byte Id, ShaderMaterial Material)> _byClass = [];
    private readonly Dictionary<string, Shader> _shaders = new(StringComparer.Ordinal);
    private readonly List<ShaderMaterial> _allocated = [];
    private readonly byte[] _matBytes = new byte[MatTable.ByteCount];
    private readonly Image _matImage;
    private readonly ImageTexture _matTexture;
    private int _nextId = FirstId;

    /// <param name="source">白名单非卡通着色器的材质来源（veg_* 等由它建）。</param>
    /// <param name="registry">WP-01 统一注册的登记表；还没注册时由本类自己加载（同一份 core.json 与 ext）。</param>
    public ToonLibrary(IShaderMaterialSource source, PaletteRegistry? registry = null)
    {
        _source = source;
        _registry = registry ?? PaletteRegistryLoader.Load();
        _matImage = Image.CreateFromData(MatTable.Width, MatTable.Height, false, Image.Format.Rgba8, _matBytes);
        _matTexture = ImageTexture.CreateFromImage(_matImage);
        RenderingServer.GlobalShaderParameterSet(MatTableGlobal, _matTexture);
    }

    public PaletteRegistry Palette => _registry;
    public IReadOnlyList<ShaderMaterial> Allocated => _allocated;

    public ShaderMaterial Get(ToonRef toon) => Resolve(toon).Material;
    public byte MaterialId(ToonRef toon) => Resolve(toon).Id;

    public byte ColorId(PaletteToken lit)
    {
        var entry = _registry.Get(lit);
        return entry.Id;
    }

    private (byte Id, ShaderMaterial Material) Resolve(ToonRef toon)
    {
        var spec = toon.Spec ?? throw new ArgumentException("ToonRef has no spec.", nameof(toon));
        var key = MaterialClass.Of(spec);
        if (_byClass.TryGetValue(key, out var found)) return found;
        _registry.Validate(spec);
        if (_nextId > LastId)
            throw new InvalidOperationException($"Material IDs exhausted ({FirstId}-{LastId}); merge ToonSpec classes before adding more.");
        var id = (byte)_nextId++;
        var material = MaterialFor(spec.Kind);
        material.SetShaderParameter(MatIdParameter, (int)id);
        MatTable.WriteRows(_matBytes, id, spec);
        _matImage.SetData(MatTable.Width, MatTable.Height, false, Image.Format.Rgba8, _matBytes);
        _matTexture.Update(_matImage);
        var result = (id, material);
        _byClass.Add(key, result);
        _allocated.Add(material);
        return result;
    }

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

    /// <summary>材质类：ToonSpec 里决定着色器、明暗档和线表的字段；颜色字段不在内。</summary>
    private readonly record struct MaterialClass(ToonKind Kind, ShadeSteps Steps, float Band, ToonNormal Normal,
        LineClass Line, ToonFx Fx, PaletteToken? Backface, ChannelId? Emissive, float Translucency, float Wet)
    {
        public static MaterialClass Of(ToonSpec s) =>
            new(s.Kind, s.Steps, s.Band, s.Normal, s.Line, s.Fx, s.Backface, s.Emissive, s.Translucency, s.Wet);
    }
}
