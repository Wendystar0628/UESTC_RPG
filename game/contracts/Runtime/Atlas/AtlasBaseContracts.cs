using System.Numerics;
using Campus.Contracts.Maps;

namespace Campus.Contracts.Atlas;

/// <summary>Pure CPU atlas bridge. Ready means four real SDF textures are available, not that labels/glyphs/regions are ready.</summary>
public interface IAtlasBaseSource
{
    long Rev { get; }
    MapId Map { get; }
    bool Ready { get; }
    AtlasBaseSnapshot? Snapshot { get; }
}
