using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Stubs;

/// <summary>D0 has no baked atlas bytes. The UI uses its explicit plain-fill fallback.</summary>
[KernelService(typeof(IAtlasBaseSource), Owner = "WP-00", IsStub = true)]
public sealed class AtlasBaseSourceStub(IMapService maps) : IAtlasBaseSource
{
    public long Rev => maps.Rev;
    public MapId Map => maps.Current;
    public bool Ready => false;
    public AtlasBaseSnapshot? Snapshot => null;
}
