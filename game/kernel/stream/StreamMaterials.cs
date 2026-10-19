using Campus.Contracts;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Stream;

/// <summary>Owns only materials returned by Create. Toon and reflection materials are borrowed.</summary>
internal sealed class StreamMaterials(IToonMaterialProvider toon, IShaderMaterialSource shaders) : IDisposable
{
    private readonly Dictionary<ShaderId, ShaderMaterial> _owned = [];
    private readonly Dictionary<PreparedTile.Draw, Material> _bindings = new(ReferenceEqualityComparer.Instance);
    public Material Resolve(PreparedTile.Draw draw)
    {
        if (_bindings.TryGetValue(draw, out var binding)) return binding;
        Material material;
        if (draw.Shader is not { } shader) material = toon.Get(draw.Material.Spec);
        else if (_owned.TryGetValue(shader, out var existing)) material = existing;
        else
        {
            var created = shaders.Create(shader);
            try { _owned.Add(shader, created); material = created; }
            catch { created.Dispose(); throw; }
        }
        _bindings.Add(draw, material);
        return material;
    }
    public void Forget(IEnumerable<PreparedTile.Draw> draws)
    {
        foreach (var draw in draws) _bindings.Remove(draw);
    }
    public void Dispose()
    {
        foreach (var material in _owned.OrderBy(pair => pair.Key).Select(pair => pair.Value)) material.Dispose();
        _owned.Clear();
        _bindings.Clear();
    }
}
