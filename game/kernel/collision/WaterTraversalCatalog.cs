using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Kernel.Collision;

/// <summary>Authoring operations are ordinary overlay features; the map loader remains their only reader.</summary>
internal sealed class WaterTraversalCatalog
{
    public WaterVolume[] Volumes { get; }
    public WaterArea[] Areas { get; }
    public WaterExitDecl[] Exits { get; }
    public int TraceCapacity { get; }

    public WaterTraversalCatalog(IWorldModel world, ITerrainWaterSource source)
    {
        var volumes = new List<WaterVolume>();
        foreach (var water in source.Waters)
            volumes.Add(new(TerrainWaterIdentity.VolumeId(water.Water), water.Water,
                water.Polygons, null, null, null, WaterBottomKind.Terrain, null, water.WadeAreas));
        var areas = new List<WaterArea>();
        var exits = new List<WaterExitDecl>();
        foreach (var feature in world.Features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Kind.Value is not ("water-volume" or "swim-area" or "water-exit")) continue;
            var props = feature.Props is { ValueKind: JsonValueKind.Object } value
                ? value : throw new JsonException($"Water declaration '{feature.Id.Value}' needs props.");
            switch (feature.Kind.Value)
            {
                case "water-volume":
                    if (feature.Id.Value.StartsWith(TerrainWaterIdentity.VolumePrefix, StringComparison.Ordinal))
                        throw new JsonException("An authored water volume uses the reserved natural-volume prefix.");
                    var bottom = Read<WaterBottomKind>(props, "bottomKind");
                    _ = bottom switch
                    {
                        WaterBottomKind.Terrain or WaterBottomKind.Plane => 0,
                        _ => throw new JsonException("Unknown water bottom kind.")
                    };
                    var min = Number(props, "minY"); var max = Number(props, "maxY");
                    if (min >= max) throw new JsonException("Water volume minY must be below maxY.");
                    var plane = bottom switch
                    {
                        WaterBottomKind.Terrain => (float?)null,
                        WaterBottomKind.Plane => Number(props, "bottomY"),
                        _ => throw new JsonException("Unknown water bottom kind.")
                    };
                    if (plane is { } y && (y < min || y > max)) throw new JsonException("Pool bed lies outside its volume.");
                    volumes.Add(new(feature.Id.Value, new(Text(props, "water")), Polygons(feature.Geometry),
                        Space(props, "space"), min, max, bottom, plane, []));
                    break;
                case "swim-area":
                    var access = Read<WaterAccess>(props, "access");
                    _ = access switch
                    {
                        WaterAccess.Forbidden or WaterAccess.WadeOnly or WaterAccess.SurfaceSwim => 0,
                        _ => throw new JsonException("Unknown water access.")
                    };
                    areas.Add(new(feature.Id.Value, Text(props, "volumeId"), Polygons(feature.Geometry), access));
                    break;
                case "water-exit":
                    if (feature.Geometry is not PointGeometry point) throw new JsonException("A water exit needs a dry-feet point.");
                    var kind = Read<WaterExitKind>(props, "kind");
                    _ = kind switch
                    {
                        WaterExitKind.SoftBank or WaterExitKind.SubmergedSteps or WaterExitKind.PoolSteps => 0,
                        _ => throw new JsonException("Unknown water exit kind.")
                    };
                    var approachData = Required(props, "approach");
                    var approach = new Box3(Read<Vector3>(approachData, "min"), Read<Vector3>(approachData, "max"));
                    if (!VectorMath.Finite(approach.Min) || !VectorMath.Finite(approach.Max) ||
                        approach.Min.X >= approach.Max.X || approach.Min.Y >= approach.Max.Y || approach.Min.Z >= approach.Max.Z)
                        throw new JsonException("Exit approach must be a finite nonempty box.");
                    var width = Number(props, "clearWidth");
                    if (width <= 0 || !VectorMath.Finite(point.Position)) throw new JsonException("Exit geometry is invalid.");
                    exits.Add(new(feature.Id.Value, Text(props, "volumeId"), kind, approach,
                        new(point.Position, Number(props, "dryYawDeg"), Number(props, "dryPitchDeg")),
                        Space(props, "drySpace"), width));
                    break;
            }
        }
        Volumes = volumes.OrderBy(volume => volume.MinY.HasValue ? 0 : 1)
            .ThenBy(volume => volume.Id, StringComparer.Ordinal).ToArray();
        Areas = areas.ToArray(); Exits = exits.ToArray();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var volume in Volumes)
        {
            if (string.IsNullOrWhiteSpace(volume.Water.Value) || !ids.Add(volume.Id))
                throw new JsonException("Water volumes need unique IDs and a water feature ID.");
        }
        foreach (var area in Areas)
            if (!ids.Contains(area.VolumeId)) throw new JsonException($"Unknown swim-area volume '{area.VolumeId}'.");
        foreach (var exit in Exits)
            if (!ids.Contains(exit.VolumeId)) throw new JsonException($"Unknown exit volume '{exit.VolumeId}'.");
        TraceCapacity = 2;
        foreach (var volume in Volumes)
        {
            TraceCapacity += 4;
            foreach (var polygon in volume.Polygons)
            {
                TraceCapacity += polygon.Outer.Count;
                foreach (var hole in polygon.Holes) TraceCapacity += hole.Count;
            }
        }
    }

    public WaterVolume? Volume(string id)
    {
        foreach (var volume in Volumes) if (volume.Id == id) return volume;
        return null;
    }

    public static bool Contains(IReadOnlyList<PolygonRings> polygons, Vector2 point)
    {
        for (int i = 0; i < polygons.Count; i++)
        {
            var polygon = polygons[i];
            if (!PlanGeometry.PointInRing(point, polygon.Outer)) continue;
            bool hole = false;
            for (int j = 0; j < polygon.Holes.Count; j++)
                if (PlanGeometry.PointInRing(point, polygon.Holes[j])) { hole = true; break; }
            if (!hole) return true;
        }
        return false;
    }

    public static bool SameSpace(NavSpace? query, NavSpace? volume) =>
        query is null || query == volume || (query.Value.Building is null && volume is null);

    private static PolygonRings[] Polygons(OverlayGeometry geometry)
    {
        IEnumerable<PolygonRings> values = geometry switch
        {
            PolygonGeometry polygon => [polygon.Rings],
            MultiPolygonGeometry multi => multi.Polygons,
            _ => throw new JsonException("Water volumes and swim areas need polygon geometry.")
        };
        var result = values.Select(polygon => new PolygonRings(PlanGeometry.OpenRing(polygon.Outer),
            polygon.Holes.Select(ring => PlanGeometry.OpenRing(ring, false)).ToArray())).ToArray();
        if (result.Length == 0) throw new JsonException("A water declaration needs a nonempty polygon.");
        return result;
    }

    private static JsonElement Required(JsonElement props, string key) => props.TryGetProperty(key, out var value)
        ? value : throw new JsonException($"Missing water property '{key}'.");
    private static T Read<T>(JsonElement props, string key) =>
        Required(props, key).Deserialize<T>(ContractJson.Options)!;
    private static string Text(JsonElement props, string key)
    {
        var value = Required(props, key).GetString();
        return !string.IsNullOrWhiteSpace(value) && value == value.Trim() ? value
            : throw new JsonException($"Invalid water identifier '{key}'.");
    }
    private static float Number(JsonElement props, string key)
    {
        var value = Required(props, key).GetSingle();
        return float.IsFinite(value) ? value : throw new JsonException($"Non-finite water property '{key}'.");
    }
    private static NavSpace? Space(JsonElement props, string key)
    {
        var value = Required(props, key);
        if (value.ValueKind == JsonValueKind.Null) return null;
        return new(new FeatureId(Text(value, "building")), Required(value, "floor").GetInt16());
    }
}

internal sealed record WaterVolume(string Id, FeatureId Water, IReadOnlyList<PolygonRings> Polygons,
    NavSpace? Space, float? MinY, float? MaxY, WaterBottomKind BottomKind, float? PlaneY,
    IReadOnlyList<PolygonRings> WadeAreas);
internal sealed record WaterArea(string Id, string VolumeId, IReadOnlyList<PolygonRings> Polygons, WaterAccess Access);
