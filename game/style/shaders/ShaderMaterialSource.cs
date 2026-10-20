using Campus.Contracts;
using Campus.Kernel.Materials;
using Godot;

namespace Campus.Style.Materials;

/// <summary>
/// 白名单着色器的材质来源（contract-3；ENG-PERF）。每次返回一个新的 ShaderMaterial，
/// 调用方只写 uniform，不换着色器、不改 render_mode 和 #define。卡通系仍经 ToonSpec 走材质库。
/// 着色器文件归各自的包，这里只建材质。
/// </summary>
[KernelService(typeof(IShaderMaterialSource), Owner = "WP-04")]
public sealed class ShaderMaterialSource : IShaderMaterialSource
{
    private readonly Dictionary<ShaderId, Shader> _shaders = [];

    public ShaderMaterial Create(ShaderId shader) => new() { Shader = Load(shader) };

    private Shader Load(ShaderId shader)
    {
        if (_shaders.TryGetValue(shader, out var cached)) return cached;
        var path = "res://" + MaterialShaders.FileOf(shader);
        if (!ResourceLoader.Exists(path, "Shader"))
            throw new FileNotFoundException($"Whitelisted shader '{shader}' is not available at '{path}'. The shader registry must reference a delivered resource.", path);
        var loaded = ResourceLoader.Load<Shader>(path)
            ?? throw new InvalidOperationException($"Failed to load whitelisted shader '{shader}' from '{path}'.");
        _shaders.Add(shader, loaded);
        return loaded;
    }
}
