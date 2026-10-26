using Campus.Contracts;
using Campus.Kernel.Diagnostics;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Stream;

/// <summary>Owns only materials returned by Create. Toon and reflection materials are borrowed.</summary>
internal sealed class StreamMaterials(IToonMaterialProvider toon, IShaderMaterialSource shaders) : IDisposable
{
    private readonly Dictionary<ShaderId, ShaderMaterial> _owned = [];
    private readonly Dictionary<PreparedTile.Draw, Material> _bindings = new(ReferenceEqualityComparer.Instance);
    internal bool IsEmpty => _owned.Count == 0 && _bindings.Count == 0;
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
            catch (Exception failure)
            {
                try { created.Dispose(); }
                catch (Exception cleanup)
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(RetainedFailure.Append(failure, cleanup)).Throw();
                    throw;
                }
                throw;
            }
        }
        _bindings.Add(draw, material);
        return material;
    }
    public void Forget(IEnumerable<PreparedTile.Draw> draws)
    {
        foreach (var draw in draws) _bindings.Remove(draw);
    }
    internal bool ReleaseOne()
    {
        foreach (var (shader, material) in _owned)
        { material.Dispose(); _owned.Remove(shader); return false; }
        _bindings.Clear(); return true;
    }
    public void Dispose()
    {
        List<Exception>? failures = null;
        foreach (var (shader, material) in _owned.OrderBy(pair => pair.Key).ToArray())
            try { material.Dispose(); _owned.Remove(shader); }
            catch (Exception error) { (failures ??= []).Add(error); }
        _bindings.Clear();
        if (failures is not null) throw new AggregateException("Stream material release failed.", failures);
    }
}
