using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Maps;

internal sealed record MapContext(MapDescriptor Descriptor, IWorldModel World, string DataRoot, string BakeRoot) : IMapContext;
