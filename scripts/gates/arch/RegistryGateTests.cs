using Campus.Contracts;
using Campus.Kernel.Registry;
using GdUnit4;

namespace Campus.Arch.Tests;

[TestSuite]
public sealed class RegistryGateTests
{
    [TestCase]
    public void RealWorldReplacementDoesNotConstructOrCastTheD0Fallback()
    {
        var registry = new ServiceRegistry();
        registry.Discover([typeof(ServiceRegistry).Assembly, typeof(ReplacementWorld).Assembly]);
        var chosen = registry.Entries.Single(entry => entry.Kind == "service" && entry.Id == typeof(IWorldModel).FullName);
        if (chosen.Type != typeof(ReplacementWorld) || chosen.IsStub) throw new InvalidOperationException("Real world replacement was not selected.");
        RegistryGateRunner.ValidateConstructorGraph(registry, [typeof(IRuleset), typeof(IRenderHost)]);
        if (ReplacementWorld.Constructed) throw new InvalidOperationException("Declaration validation constructed the selected world implementation.");
    }

    [KernelService(typeof(IWorldModel), Owner = "WP-08")]
    public sealed class ReplacementWorld : IWorldModel
    {
        public static bool Constructed { get; private set; }
        public ReplacementWorld()
        {
            Constructed = true;
            throw new InvalidOperationException("Runtime initialization is intentionally unavailable in this declaration-only fixture.");
        }
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => throw new NotSupportedException();
        public PolygonRings Boundary => throw new NotSupportedException();
        public WorldNetwork Network => throw new NotSupportedException();
        public ISpatialIndex Spatial => throw new NotSupportedException();
        public IInteriorIndex Interiors => throw new NotSupportedException();
        public IOutskirtsModel? Outskirts => throw new NotSupportedException();
        public IFidelityModel? Fidelity => throw new NotSupportedException();
    }
}
