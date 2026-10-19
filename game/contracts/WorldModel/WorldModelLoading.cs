using System.Numerics;
using Campus.Contracts.Maps;

namespace Campus.Contracts;

/// <summary>Complete parsed world and metadata for the kernel map adapter. Boundary has the world model as its single source.</summary>
public sealed record WorldModelLoadResult(IWorldModel World, GeoFrame Geo, Vector2 Center,
    Box3 Bounds, TileKey TileMin, TileKey TileMax)
{
    public PolygonRings Boundary => World.Boundary;
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public interface IWorldModelLoader
{
    /// <summary>
    /// Files are preloaded by the kernel adapter, keyed by forward-slash paths relative to this map's DataRoot.
    /// Implementations parse only the supplied bytes; no path access or engine resource loading occurs here.
    /// </summary>
    WorldModelLoadResult Load(MapId map, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
        WorldModelLoadInputs inputs);
}
