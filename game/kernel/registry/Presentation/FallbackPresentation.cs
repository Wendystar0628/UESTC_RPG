using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>No reflection world exists until WP-24 supplies its registered implementation.</summary>
[KernelService(typeof(IReflectionWorld), Owner = "WP-00", IsStub = true)]
public sealed class FallbackReflectionWorld : IReflectionWorld
{
    public Rid Scenario => default;
    public Material ProxyMaterial => throw new InvalidOperationException("The reflection world is unavailable; check Scenario before reading ProxyMaterial.");
    public event Action? Changed { add { } remove { } }
}
