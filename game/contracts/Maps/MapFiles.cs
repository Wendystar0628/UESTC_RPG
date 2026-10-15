using System.Numerics;

namespace Campus.Contracts.Maps;

public sealed record MapIndexEntry(MapId Id, string Dir);
public sealed record MapIndex(int Version, MapId DefaultMap, IReadOnlyList<MapIndexEntry> Maps);

// The authored map file excludes values derived from the campus and bake data.
public sealed record MapFile(
    MapId Id, LocalizedName Name, MapKind Kind, int Version, uint Seed,
    MapRect DataRect, RoamLimits Roam, string DefaultSpawn,
    IReadOnlyList<MapSpawn> Spawns, MapWorldRules World, MapEnvironment Env,
    IReadOnlyDictionary<string, string> Cinematics, IReadOnlyList<MapPortal> Portals)
{
    public MapTitle? Title { get; init; }
    public MapDescriptor Complete(GeoFrame geo, Vector2 center, Box3 bounds, TileKey tileMin, TileKey tileMax) =>
        new(Id, Name, Kind, Version, Seed, geo, center, bounds, DataRect, Roam,
            tileMin, tileMax, DefaultSpawn, Spawns, World, Env, Cinematics, Portals)
        { Title = Title };
}
