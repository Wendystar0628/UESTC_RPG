using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

/// <summary>Pure parsing of one authored interiors.json entrance into absolute world coordinates.</summary>
public static class InteriorEntranceJson
{
    private static readonly InteriorEntranceJsonContext Context = new(new JsonSerializerOptions(ContractJson.Options)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        AllowDuplicateProperties = false,
        NumberHandling = JsonNumberHandling.Strict
    });

    /// <summary>
    /// Parse the optional closed entrance register without creating providers or opening buildings.
    /// Validates IDs across both registers and rejects claims on an open-catalog feature.
    /// The caller must prove every returned feature is an existing building before publishing.
    /// </summary>
    public static IReadOnlyDictionary<FeatureId, IReadOnlyList<EntranceDecl>> ParseClosedEntrances(
        JsonElement catalog, ITerrain? terrain = null)
    {
        if (!catalog.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var value) || value != 3)
            throw new JsonException("Unsupported interior catalog version.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var openFeatures = new HashSet<FeatureId>();
        var register = JsonSerializer.Deserialize(catalog, Context.InteriorEntranceRegister)
            ?? throw new JsonException("An interior catalog must be an object.");
        foreach (var interior in register.Interiors)
        {
            if (interior is null) throw new JsonException("An interior must be an object.");
            var features = interior.FeatureIds.Select(value => new FeatureId(Text(value, "featureIds"))).ToHashSet();
            openFeatures.UnionWith(features);
            foreach (var entrance in interior.Entrances)
            {
                if (entrance is null) throw new JsonException("An entrance must be an object.");
                var id = Text(entrance.Id, "id");
                if (!ids.Add(id)) throw new JsonException($"Duplicate entrance ID: '{id}'.", id, null, null);
                if (!features.Contains(new(Text(entrance.FeatureId, "featureId"))))
                    throw new JsonException($"Entrance '{id}' belongs to a feature outside its interior.", id, null, null);
            }
        }
        var result = new Dictionary<FeatureId, List<EntranceDecl>>();
        var index = 0;
        foreach (var raw in register.ClosedEntrances)
        {
            var path = $"closedEntrances[{index++}]";
            try
            {
                // Read identity first so malformed door data still reports its authored ID.
                var identity = JsonSerializer.Deserialize(raw, Context.InteriorEntranceIdentity)
                    ?? throw new JsonException("A closed entrance must be an object.");
                var id = Text(identity.Id, "id");
                path = id;
                if (!ids.Add(id)) throw new JsonException($"Duplicate entrance ID: '{id}'.");
                var feature = new FeatureId(Text(identity.FeatureId, "featureId"));
                if (openFeatures.Contains(feature))
                    throw new JsonException($"Closed entrance '{id}' claims open interior feature '{feature.Value}'.");
                var entrance = Parse(raw, feature, terrain);
                if (entrance.Open) throw new JsonException($"Closed entrance '{id}' must have open=false.");
                if (!result.TryGetValue(feature, out var list)) result.Add(feature, list = []);
                list.Add(entrance);
            }
            catch (JsonException error) { throw new JsonException(error.Message, path, null, null, error); }
        }
        return new System.Collections.ObjectModel.ReadOnlyDictionary<FeatureId, IReadOnlyList<EntranceDecl>>(
            result.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<EntranceDecl>)pair.Value.AsReadOnly()));
    }

    /// <summary>
    /// Missing heightRef and "absolute" preserve authored coordinates without querying terrain.
    /// "pad" adds terrain.PadY(feature) once to all three Y coordinates and any known headY.
    /// Missing or zero headY remains unknown (zero); a resolved known headY must not collide with that sentinel.
    /// The result contains no height reference. Call only on raw catalog records, never on a resolved EntranceDecl.
    /// </summary>
    /// <remarks>
    /// The caller proves feature ownership and supplies the intended map's terrain. This parser checks that the
    /// record names the same feature, but does not prove terrain readiness or substitute a flat implementation.
    /// An explicitly supplied terrain may legitimately return zero. No IO, registry or engine dependency.
    /// </remarks>
    public static EntranceDecl Parse(JsonElement entrance, FeatureId feature, ITerrain? terrain = null)
    {
        var data = JsonSerializer.Deserialize(entrance, Context.InteriorEntranceRecord)
            ?? throw new JsonException("An entrance must be an object.");
        switch (data.Position)
        {
            case EntrancePosition.Unspecified:
            case EntrancePosition.Front:
            case EntrancePosition.Rear:
                break;
            default:
                throw new JsonException($"Unknown entrance position '{data.Position}'.");
        }
        var id = Text(data.Id, "id");
        var featureId = Text(data.FeatureId, "featureId");
        if (!StringComparer.Ordinal.Equals(featureId, feature.Value))
            throw new JsonException($"Entrance '{id}' belongs to '{featureId}', not '{feature.Value}'.");
        var outside = data.OutsideLanding;
        var inside = data.InsideLanding;
        var center = data.DoorCenter;
        var width = Number(data.ClearWidth, "clearWidth");
        var yaw = Number(data.FacingDeg, "facingDeg");
        var headY = Number(data.HeadY, "headY");
        if (width < 1.1f) throw new JsonException($"Entrance '{id}' clearWidth must be at least 1.1 m.");
        var heightRef = Text(data.HeightRef, "heightRef");
        switch (heightRef)
        {
            case "absolute":
                break;
            case "pad":
                if (terrain is null)
                    throw new JsonException($"Entrance '{id}' on '{featureId}' requires explicit terrain for heightRef 'pad'.");
                float padY;
                try { padY = terrain.PadY(feature); }
                catch (Exception error) when (error is ArgumentException or KeyNotFoundException or NotSupportedException or InvalidOperationException)
                {
                    throw new JsonException($"Cannot resolve pad height for entrance '{id}' on '{featureId}'.", error);
                }
                if (!float.IsFinite(padY))
                    throw new JsonException($"Pad height for entrance '{id}' on '{featureId}' must be finite.");
                outside = Raise(outside, padY, id, "outsideLanding");
                inside = Raise(inside, padY, id, "insideLanding");
                center = Raise(center, padY, id, "doorCenter");
                if (headY != 0)
                {
                    headY += padY;
                    if (!float.IsFinite(headY) || headY == 0)
                        throw new JsonException($"Entrance '{id}' resolved headY must be finite and nonzero; zero is reserved for unknown.");
                }
                break;
            default:
                throw new JsonException($"Unknown heightRef '{heightRef}' for entrance '{id}'; expected 'absolute' or 'pad'.");
        }
        return new(id, outside, inside, width, data.Open) { Center = center, YawDeg = yaw, HeadY = headY, Position = data.Position };
    }

    /// <summary>Read the only open-range format. The world owner validates each SourceId before provider publication.</summary>
    public static IReadOnlyList<InteriorOpenPart> ParseOpenParts(JsonElement interior)
    {
        var features = interior.GetProperty("featureIds").EnumerateArray().Select(item => new FeatureId(item.GetString()!)).ToHashSet();
        var result = new List<InteriorOpenPart>();
        var seen = new HashSet<(FeatureId, string, InteriorOpenPartKind)>();
        foreach (var part in interior.GetProperty("openParts").EnumerateArray())
        {
            var feature = new FeatureId(Text(part.GetProperty("featureId").GetString(), "featureId"));
            var kind = part.GetProperty("kind").Deserialize<InteriorOpenPartKind>(ContractJson.Options);
            var source = kind switch
            {
                InteriorOpenPartKind.IndoorFloor => part.GetProperty("sourceId").GetString() ?? throw new JsonException("Missing sourceId."),
                InteriorOpenPartKind.OutdoorHighPlace => Text(part.GetProperty("areaId").GetString(), "areaId"),
                _ => throw new JsonException("Unknown open-part kind.")
            };
            var floors = kind switch
            {
                InteriorOpenPartKind.IndoorFloor => part.GetProperty("floors").EnumerateArray().Select(item => item.GetInt32()).Order().ToArray(),
                InteriorOpenPartKind.OutdoorHighPlace when !part.TryGetProperty("floors", out _) && !string.IsNullOrWhiteSpace(source) => [],
                InteriorOpenPartKind.OutdoorHighPlace => throw new JsonException("Outdoor high places reference an area ID without floors."),
                _ => throw new JsonException("Unknown open-part kind.")
            };
            if (!features.Contains(feature) || !seen.Add((feature, source, kind)) ||
                InteriorOpenPart.IsIndoor(kind) && floors.Length == 0 ||
                floors.Distinct().Count() != floors.Length || floors.Any(floor => floor < short.MinValue || floor > short.MaxValue))
                throw new JsonException($"Invalid or duplicate open part: {feature.Value}/{source}.");
            result.Add(new(feature, source, Array.AsReadOnly(floors), kind));
        }
        if (features.Any(feature => !result.Any(part => part.FeatureId == feature)))
            throw new JsonException("Every open feature needs an explicit open part.");
        return result.AsReadOnly();
    }

    private static string Text(string? value, string name) => value is { Length: > 0 }
        ? value : throw new JsonException($"Entrance {name} must be a nonempty string.");

    private static float Number(float value, string name) => float.IsFinite(value) ? value
        : throw new JsonException($"Entrance {name} must be a finite number.");

    private static Vector3 Raise(Vector3 point, float padY, string id, string name)
    {
        point.Y += padY;
        if (!float.IsFinite(point.Y)) throw new JsonException($"Entrance '{id}' resolved {name}.Y must be finite.");
        return point;
    }
}

internal sealed record InteriorEntranceRecord(string Id, string FeatureId, Vector3 DoorCenter,
    float FacingDeg, Vector3 OutsideLanding, Vector3 InsideLanding, float ClearWidth, bool Open,
    string HeightRef = "absolute", float HeadY = 0, EntrancePosition Position = EntrancePosition.Unspecified);

// These register projections read ownership/identity only; provider metadata and open-door geometry
// are validated by their owning parser. Closed-door geometry uses the strict record above.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
internal sealed record InteriorEntranceRegister
{
    public required IReadOnlyList<InteriorOpenEntranceRegister> Interiors { get; init; }
    public IReadOnlyList<JsonElement> ClosedEntrances { get; set; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
internal sealed record InteriorOpenEntranceRegister(IReadOnlyList<string> FeatureIds,
    IReadOnlyList<InteriorEntranceIdentity> Entrances);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
internal sealed record InteriorEntranceIdentity(string Id, string? FeatureId = null);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(InteriorEntranceRecord))]
[JsonSerializable(typeof(InteriorEntranceRegister))]
[JsonSerializable(typeof(InteriorEntranceIdentity))]
internal partial class InteriorEntranceJsonContext : JsonSerializerContext { }
