using Campus.Contracts;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Stream;

/// <summary>Owns only materials returned by Create. Toon and reflection materials are borrowed.</summary>
internal sealed class StreamMaterials(IToonMaterialProvider toon, IShaderMaterialSource shaders) : IDisposable
{
    private readonly Dictionary<ShaderId, ShaderMaterial> _owned = [];
    private readonly HashSet<Material> _trackedBorrowed = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<PreparedTile.Draw, Binding> _bindings = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<PreparedTile.Draw, Binding> _reflectionBindings = new(ReferenceEqualityComparer.Instance);
    public Material Resolve(PreparedTile.Draw draw)
    {
        if (_bindings.TryGetValue(draw, out var binding)) return binding.Material;
        Material material;
        if (draw.Shader is not { } shader) material = toon.Get(draw.Material.Spec);
        else if (_owned.TryGetValue(shader, out var existing)) material = existing;
        else
        {
            var created = shaders.Create(shader);
            try { toon.Track(shader, created); _owned.Add(shader, created); material = created; }
            catch { created.Dispose(); throw; }
        }
        toon.TrackLayout(material, new(draw.Format, draw.Instanced, draw.Primitive));
        _bindings.Add(draw, new(material));
        return material;
    }
    public bool IsReady(PreparedTile.Draw draw)
    {
        if (!_bindings.TryGetValue(draw, out var binding)) return false;
        // New requests can invalidate a provider batch, but cannot undo proof for an immutable, already-drawn layout.
        return binding.Ready || (binding.Ready = draw.Shader is { } shader
            ? toon.IsReady(shader, binding.Material) : toon.IsReady(draw.Material.Spec));
    }
    public void TrackReflection(Material borrowed, PreparedTile.Draw draw)
    {
        if (_trackedBorrowed.Add(borrowed)) toon.Track(ShaderId.ReflectFlat, borrowed);
        toon.TrackLayout(borrowed, new(draw.Format, draw.Instanced, draw.Primitive));
        if (!_reflectionBindings.TryGetValue(draw, out var binding) || !ReferenceEquals(binding.Material, borrowed))
            _reflectionBindings[draw] = new(borrowed);
    }
    public bool ReflectionReady(Material borrowed) => toon.IsReady(ShaderId.ReflectFlat, borrowed);
    public bool ReflectionReady(PreparedTile.Draw draw, Material borrowed) =>
        _reflectionBindings.TryGetValue(draw, out var binding) && ReferenceEquals(binding.Material, borrowed) &&
        (binding.Ready || (binding.Ready = ReflectionReady(borrowed)));
    public void Forget(IEnumerable<PreparedTile.Draw> draws)
    {
        foreach (var draw in draws) { _bindings.Remove(draw); _reflectionBindings.Remove(draw); }
    }
    public void Dispose()
    {
        foreach (var material in _owned.OrderBy(pair => pair.Key).Select(pair => pair.Value)) material.Dispose();
        _owned.Clear();
        _trackedBorrowed.Clear();
        _bindings.Clear(); _reflectionBindings.Clear();
    }
    private sealed class Binding(Material material)
    {
        public Material Material { get; } = material;
        public bool Ready;
    }
}
