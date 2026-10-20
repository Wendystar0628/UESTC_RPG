using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.World;

namespace Campus.Kernel.Maps;

internal sealed record MapContext : IMapContext, IMapInteriorPreparation
{
    public MapContext(MapDescriptor descriptor, IWorldModel world, string dataRoot, BuildInputSnapshot sourceInputs)
    { Descriptor=descriptor;World=world;DataRoot=dataRoot;SourceInputs=sourceInputs; }
    public MapDescriptor Descriptor { get; }
    public IWorldModel World { get; private set; }
    public string DataRoot { get; }
    public BuildInputSnapshot SourceInputs { get; }
    private bool _interiorsPublished;
    public void PublishInteriors(IReadOnlyList<InteriorIndexEntry> entries)
    {
        if (_interiorsPublished) throw new InvalidOperationException("Map interiors have already been published.");
        var prepared = WorldModelSnapshot.WithInteriors(World, entries);
        MapCatalog.ValidateDestinations(Descriptor, SourceInputs.Entries, prepared);
        World = prepared;
        _interiorsPublished = true;
    }
}
