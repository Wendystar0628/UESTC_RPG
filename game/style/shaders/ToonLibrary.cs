using Campus.Contracts;
using Campus.Style.Shaders.Cards;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Style.Materials;

/// <summary>
/// 卡通材质库（ENG-ARCH、ENG-PERF），实现跨包的 <see cref="IToonMaterialProvider"/>：
/// 按 <see cref="ToonSpec"/> 取白名单着色器的材质，分配 7 位材质 ID（1–111；0 未分配，112–127 留给 WP-05 的特殊线类），
/// 生产全局数据纹理 mat_table 与静态 window_cards（经 <see cref="IGlobalDataTextureRegistry"/> 登记，交出释放权）。调色板纹理归 WP-37。
/// </summary>
[KernelService(typeof(IToonMaterialProvider), Owner = "WP-04")]
public sealed class ToonLibrary : IToonMaterialProvider, IDisposable
{
    public const int FirstId = 1;
    public const int LastId = 111;

    private static readonly StringName MatTableGlobal = new(ShaderGlobals.MatTable);
    private static readonly StringName WindowCardsGlobal = new(ShaderGlobals.WindowCards);
    private static readonly StringName ClearGlassParameter = new("clear_glass");
    private static readonly StringName MatIdParameter = new("mat_id");
    private static readonly StringName PatternLayerParameter = new("pattern_layer");
    private static readonly StringName PatternPeriodParameter = new("pattern_period");
    private static readonly StringName PatternJointParameter = new("pattern_joint");

    private readonly object _gate = new();
    private readonly PaletteCatalog _catalog;
    private readonly IShaderMaterialSource _source;
    private readonly Dictionary<MaterialClass, ToonEntry> _byClass = [];
    private SceneTextTextures? _sceneTextTextures;
    private readonly byte[] _matBytes = new byte[MatTable.ByteCount];
    private readonly Image _matImage;
    private readonly ImageTexture _matTexture;
    private IDisposable? _registration;
    private IDisposable? _windowCardsRegistration;
    private int _nextId = FirstId;
    private bool _disposed;

    /// <param name="palettes">完整色表来源（WP-04 的正式实现，无 GPU）：ToonSpec 按它校验，含 256–511 的扩展色。</param>
    /// <param name="source">白名单着色器的材质来源。</param>
    /// <param name="textures">全局数据纹理的登记处；mat_table 与 window_cards 登记后释放权归它。</param>
    public ToonLibrary(IPaletteCatalogSource palettes, IShaderMaterialSource source, IGlobalDataTextureRegistry textures)
    {
        ArgumentNullException.ThrowIfNull(palettes);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(textures);
        if (OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Toon data textures must be created on the main thread.");
        _catalog = palettes.Catalog;
        _source = source;
        Image? matImage = null;
        ImageTexture? matTexture = null;
        ImageTexture? cardTexture = null;
        var matBound = false;
        var cardsBound = false;
        try
        {
            matImage = Image.CreateFromData(MatTable.Width, MatTable.Height, false, Image.Format.Rgba8, _matBytes);
            matTexture = ImageTexture.CreateFromImage(matImage);
            _registration = textures.RegisterOwned(ShaderGlobals.MatTable, matTexture, matTexture.GetRid());
            RenderingServer.GlobalShaderParameterSet(MatTableGlobal, matTexture);
            matBound = true;

            // 静态表只上传一次。Image 是本地临时资源；登记成功后 GPU 纹理的释放权归 registry。
            var packed = WindowCardTable.Pack();
            var bytes = System.Runtime.InteropServices.MemoryMarshal.AsBytes(packed.AsSpan()).ToArray();
            using var cardImage = Image.CreateFromData(WindowCardTable.Width, WindowCardTable.Rows, false, Image.Format.Rgbaf, bytes);
            cardTexture = ImageTexture.CreateFromImage(cardImage);
            _windowCardsRegistration = textures.RegisterOwned(ShaderGlobals.WindowCards, cardTexture, cardTexture.GetRid());
            RenderingServer.GlobalShaderParameterSet(WindowCardsGlobal, cardTexture);
            cardsBound = true;
            _matImage = matImage;
            _matTexture = matTexture;
        }
        catch
        {
            // 失败前已交权的纹理只注销；未交权的纹理由本地释放，不能 double free。
            try
            {
                if (cardsBound) RenderingServer.GlobalShaderParameterSet(WindowCardsGlobal, default(Variant));
                if (matBound) RenderingServer.GlobalShaderParameterSet(MatTableGlobal, default(Variant));
            }
            finally
            {
                if (_windowCardsRegistration is not null) _windowCardsRegistration.Dispose();
                else cardTexture?.Dispose();
                _registration?.Dispose();
                if (_registration is null) matTexture?.Dispose();
                matImage?.Dispose();
            }
            throw;
        }
    }

    // ---------------------------------------------------------------------
    // 材质与色号
    // ---------------------------------------------------------------------

    /// <summary>
    /// 同一"材质类"共用一个材质：ToonSpec 去掉 Lit、Shade、Deep 后相同，就是同一个材质、同一个 ID。
    /// 颜色只走顶点色号，不逐物体建材质。头一次遇到的材质类要分配 ID、改写 mat_table，只能在主线程。
    /// </summary>
    public Material Get(ToonSpec spec) => Resolve(spec).Material;

    /// <summary>材质 ID（写进 ROUGHNESS 的 7 位值），会按需分配。</summary>
    public byte MaterialId(ToonSpec spec) => Resolve(spec).Id;

    /// <summary>Caller-owned material for one declared shader pipeline combination.</summary>
    public Material CreatePipelineMaterial(ToonKind kind)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return MaterialFor(kind);
    }

    private ToonEntry Resolve(ToonSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        // 图样的形状与层号在命中缓存之前校验：没登记的层不会借用已有的无图样材质。
        SurfacePatternLayers.Validate(spec);
        var key = MaterialClass.Of(spec);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_byClass.TryGetValue(key, out var found))
            {
                if (GodotObject.IsInstanceValid(found.Material)) return found;
                _byClass.Remove(key);
            }
            if (OS.GetThreadCallerId() != OS.GetMainThreadId())
                throw new InvalidOperationException("A new toon material class must be allocated on the main thread (mat_table update).");
            _catalog.Validate(spec);
            if (_nextId > LastId)
                throw new InvalidOperationException($"Material IDs exhausted ({FirstId}-{LastId}); merge ToonSpec classes before adding more.");
            var id = (byte)_nextId++;
            var material = MaterialFor(spec.Kind);
            material.SetShaderParameter(MatIdParameter, (int)id);
            if (spec.SurfacePattern is { } pattern)
            {
                // 只有普通 Static（toon_static）能带图样，契约校验已拦住其余 Kind；参数随材质类，不随铺向与相位。
                material.SetShaderParameter(PatternLayerParameter, (int)pattern.Layer);
                material.SetShaderParameter(PatternPeriodParameter, new Vector2(pattern.PeriodMeters.X, pattern.PeriodMeters.Y));
                material.SetShaderParameter(PatternJointParameter, new Vector2(pattern.JointMeters.X, pattern.JointMeters.Y));
            }
            MatTable.WriteRows(_matBytes, id, spec);
            _matImage.SetData(MatTable.Width, MatTable.Height, false, Image.Format.Rgba8, _matBytes);
            _matTexture.Update(_matImage);
            var entry = new ToonEntry(id, material);
            _byClass.Add(key, entry);
            return entry;
        }
    }

    private ShaderMaterial MaterialFor(ToonKind kind)
    {
        var material = _source.Create(MaterialShaders.ShaderFor(kind));
        if (kind == ToonKind.GlassNear) material.SetShaderParameter(ClearGlassParameter, true);
        if (kind == ToonKind.SceneText)
        {
            _sceneTextTextures ??= new SceneTextTextures();
            _sceneTextTextures.Bind(material);
        }
        return material;
    }

    /// <summary>
    /// 退出时先停更新、清掉全局绑定，再注销登记；纹理由登记处在最后一个租约结束后释放。
    /// 本类缓存的材质与 Image 在此释放；预编译调用方持有的材质和登记处持有的纹理不在此释放。
    /// </summary>
    public void Dispose()
    {
        Material[] owned;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            owned = _byClass.Values.Select(entry => entry.Material).Distinct<Material>(ReferenceEqualityComparer.Instance).ToArray();
            _byClass.Clear();
        }
        try
        {
            try { RenderingServer.GlobalShaderParameterSet(WindowCardsGlobal, default(Variant)); }
            finally { RenderingServer.GlobalShaderParameterSet(MatTableGlobal, default(Variant)); }
        }
        finally
        {
            try
            {
                try { _windowCardsRegistration?.Dispose(); }
                finally { _registration?.Dispose(); }
            }
            finally
            {
                _registration = null;
                _windowCardsRegistration = null;
                foreach (var material in owned)
                    if (GodotObject.IsInstanceValid(material)) material.Dispose();
                _sceneTextTextures?.Dispose();
                if (GodotObject.IsInstanceValid(_matImage)) _matImage.Dispose();
            }
        }
    }

    private sealed record ToonEntry(byte Id, Material Material);

    /// <summary>材质类：ToonSpec 里决定着色器、明暗档和线表的字段；颜色字段不在内。</summary>
    private readonly record struct MaterialClass(ToonKind Kind, ShadeSteps Steps, float Band, ToonNormal Normal,
        LineClass Line, ToonFx Fx, PaletteToken? Backface, ChannelId? Emissive, float Translucency, float Wet,
        SurfacePatternSpec? SurfacePattern)
    {
        public static MaterialClass Of(ToonSpec s) =>
            new(s.Kind, s.Steps, s.Band, s.Normal, s.Line, s.Fx, s.Backface, s.Emissive, s.Translucency, s.Wet, s.SurfacePattern);
    }
}
