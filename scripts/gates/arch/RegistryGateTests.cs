using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Kernel.Registry;
using Campus.Gates;
using GdUnit4;
using Godot;

namespace Campus.Arch.Tests;

[TestSuite]
public sealed class RegistryGateTests
{
    [TestCase]
    [TestCategory("Contract")]
    public void DeclarationValidationDoesNotConstructRegisteredImplementations()
    {
        var registry = new ServiceRegistry();
        registry.Discover(typeof(ServiceRegistry).Assembly.GetTypes().Concat([typeof(ReplacementWorld), typeof(InteriorFactory)]),
            _ => "fixture");
        var chosen = registry.Entries.Single(entry => entry.Kind == RegistrationKind.Service && entry.Id == typeof(IWorldModel).FullName);
        if (chosen.Type != typeof(ReplacementWorld) || chosen.IsStub) throw new InvalidOperationException("Real world replacement was not selected.");
        RegistryGateRunner.ValidateConstructorGraph(registry,
            [typeof(IAppExit), typeof(ServiceRegistry), typeof(Camera3D),
                typeof(WorldEnvironment), typeof(Godot.Environment), typeof(CanvasLayer)]);
        var factory = RegistrationDiscovery.ResolveInterior(registry.Entries, "fixture.family.first");
        if (factory.Type != typeof(InteriorFactory)) throw new InvalidOperationException("Factory prefix registration was not selected.");
        if (ReplacementWorld.Constructed || InteriorFactory.Constructed)
            throw new InvalidOperationException("Declaration validation constructed a registered implementation.");
    }

    [InteriorProviderFactory("fixture.family.")]
    public sealed class InteriorFactory : IInteriorProviderFactory
    {
        public static bool Constructed { get; private set; }
        public InteriorFactory()
        {
            Constructed = true;
            throw new InvalidOperationException("Factory construction is unavailable in this declaration-only fixture.");
        }
        public IInteriorProvider Create(string providerId) => throw new NotSupportedException();
    }

    [KernelService(typeof(IWorldModel))]
    public sealed class ReplacementWorld : IWorldModel
    {
        public static bool Constructed { get; private set; }
        public ReplacementWorld()
        {
            Constructed = true;
            throw new InvalidOperationException("Runtime initialization is intentionally unavailable in this declaration-only fixture.");
        }
        public ITerrain Terrain => FlatTerrain.Default;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => throw new NotSupportedException();
        public IReadOnlyDictionary<string, TerrainOp> TerrainOperations { get; } =
            new System.Collections.ObjectModel.ReadOnlyDictionary<string, TerrainOp>(new Dictionary<string, TerrainOp>(StringComparer.Ordinal));
        public PolygonRings Boundary => throw new NotSupportedException();
        public WorldNetwork Network => throw new NotSupportedException();
        public ISpatialIndex Spatial => throw new NotSupportedException();
        public IInteriorIndex Interiors => throw new NotSupportedException();
        public IOutskirtsModel? Outskirts => throw new NotSupportedException();
        public IFidelityModel Fidelity => throw new NotSupportedException();
    }
}
