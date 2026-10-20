using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;

namespace Campus.Systems.Atlas;

public sealed record AtlasMarkerFile(int Version, MapRect AtlasRect, IReadOnlyList<AtlasMarker> Markers)
{
    public float ZoomMin { get; init; }
    public float ZoomMax { get; init; }
    public IReadOnlyList<float> ZoomBands { get; init; } = [];
    public IReadOnlyList<string> Hidden { get; init; } = [];
}

public sealed record AtlasLabelFile(int Version, IReadOnlyList<AtlasRegion> Regions, IReadOnlyList<AtlasRegion> Places);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AtlasMarkerFile))]
[JsonSerializable(typeof(AtlasLabelFile))]
[JsonSerializable(typeof(WaypointFile))]
[JsonSerializable(typeof(AtlasSave))]
internal partial class AtlasJsonContext : JsonSerializerContext
{
    // A generated context binds its options; the shared options may already be frozen by another serializer.
    internal static AtlasJsonContext Contract { get; } = new(new JsonSerializerOptions(ContractJson.Options));
}

internal static class AtlasData
{
    internal static void Load(AtlasService service, IMapContext context)
    {
        var root = context.DataRoot.TrimEnd('/') + "/";
        var markerPath = root + "atlas/markers.json";
        if (!Godot.FileAccess.FileExists(markerPath)) throw new FileNotFoundException("Required markers data is missing.", markerPath);
        var markers = JsonSerializer.Deserialize(Godot.FileAccess.GetFileAsBytes(markerPath), AtlasJsonContext.Contract.AtlasMarkerFile)
            ?? throw new JsonException("Atlas markers are null.");
        var labelPath = root + "atlas/labels.json";
        if (!Godot.FileAccess.FileExists(labelPath)) throw new FileNotFoundException("Required labels data is missing.", labelPath);
        var labels = JsonSerializer.Deserialize(Godot.FileAccess.GetFileAsBytes(labelPath), AtlasJsonContext.Contract.AtlasLabelFile)
            ?? throw new JsonException("Atlas labels are null.");
        var waypointPath = root + "waypoints.json";
        if (!Godot.FileAccess.FileExists(waypointPath)) throw new FileNotFoundException("Required waypoints data is missing.", waypointPath);
        var waypoints = JsonSerializer.Deserialize(Godot.FileAccess.GetFileAsBytes(waypointPath), AtlasJsonContext.Contract.WaypointFile)
            ?? throw new JsonException("Waypoints are null.");
        service.LoadMap(context, markers, labels, waypoints);
    }
}
