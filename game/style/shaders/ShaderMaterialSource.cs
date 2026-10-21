using Campus.Contracts;
using Godot;

namespace Campus.Style.Materials;

/// <summary>
/// 白名单着色器的材质来源（contract-3；ENG-PERF）。每次返回一个新的 ShaderMaterial，
/// 调用方只写 uniform，不换着色器、不改 render_mode 和 #define。卡通系仍经 ToonSpec 走材质库。
/// 着色器文件归各自的包，这里只建材质。
/// </summary>
[KernelService(typeof(IShaderMaterialSource))]
public sealed class ShaderMaterialSource(IRequiredResources resources) : IShaderMaterialSource
{
    private readonly Dictionary<ShaderId, Shader> _shaders = [];

    public ShaderMaterial Create(ShaderId shader) => new() { Shader = Load(shader) };

    private Shader Load(ShaderId shader)
    {
        if (_shaders.TryGetValue(shader, out var cached)) return cached;
        var path = "res://" + MaterialShaders.FileOf(shader);
        var loaded = resources.Load<Shader>(path);
        _shaders.Add(shader, loaded);
        return loaded;
    }
}
