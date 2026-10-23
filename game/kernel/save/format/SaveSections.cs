using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Save;
using Campus.Kernel.Maps;
using Campus.Kernel.World;

namespace Campus.Kernel.Save.Format;

public sealed record SaveSectionRegistration(SaveSectionAttribute Attribute, ISaveSection Section);
public sealed record PreparedSection(string Key, SaveSectionRegistration Registration, MapId Map,
    object? Dto, [property: DataVersion] int FromVersion, string? Repair)
{
    public Exception? Error { get; init; }
}
public sealed record PreparedSave(SaveDocument Document, IReadOnlyList<PreparedSection> Sections);
public sealed record CapturedSection(string Key, SaveSectionRegistration Registration, object Dto);

/// <summary>Strict current-version decoding is separate from main-thread Capture, CreateDefault and Restore.</summary>
public sealed class SaveSections
{
    public IReadOnlyList<SaveSectionRegistration> Registered { get; }

    public SaveSections(IEnumerable<SaveSectionRegistration> registrations)
    {
        var items = registrations.OrderBy(r => r.Attribute.Phase).ThenBy(r => r.Attribute.Order)
            .ThenBy(r => r.Attribute.Id, StringComparer.Ordinal).ToArray();
        foreach (var item in items)
        {
            if (!SaveContainer.IsId(item.Attribute.Id) || item.Attribute.Version < 1 ||
                !Enum.IsDefined(item.Attribute.Phase) || !Enum.IsDefined(item.Attribute.Scope))
                throw new ArgumentException("Invalid save section declaration.");
        }
        if (items.GroupBy(r => (r.Attribute.Scope, r.Attribute.Id)).Any(g => g.Count() != 1) ||
            items.GroupBy(r => (r.Attribute.Phase, r.Attribute.Order)).Any(g => g.Count() != 1))
            throw new ArgumentException("Duplicate save section identity or restore order.");
        Registered = items;
    }

    public PreparedSave Prepare(SaveDocument document)
    {
        // Inspect every known envelope before repairing anything: a future section makes the whole slot read-only.
        foreach (var (key, raw) in document.Sections)
            if (Find(key) is { } registration && raw.Version > registration.Attribute.Version)
                throw new SaveFormatException("Save section comes from a newer version: " + key)
                { IsNewer = true, Metadata = document.Meta };
        var maps = document.Sections.Keys.Where(k => k.StartsWith("map:", StringComparison.Ordinal))
            .Select(k => new MapId(k[4..k.IndexOf('/')])).Append(document.Meta.Map).Distinct().OrderBy(m => m.Value, StringComparer.Ordinal).ToArray();
        var prepared = new List<PreparedSection>();
        foreach (var registration in Registered)
        {
            var attribute = registration.Attribute;
            foreach (var map in attribute.Scope == SaveScope.Global ? [document.Meta.Map] : maps)
            {
                var key = Key(attribute, map);
                if (!document.Sections.TryGetValue(key, out var raw))
                { prepared.Add(new(key, registration, map, null, 0, null)); continue; }
                try
                {
                    using var envelope = SaveContainer.ParseJson(raw.Envelope);
                    var data = envelope.RootElement.GetProperty("data");
                    if (raw.Version != attribute.Version) throw new SaveFormatException("Section version mismatch: " + key);
                    ValidateData(data, registration.Section.TypeInfo.Type);
                    var dto = JsonSerializer.Deserialize(data, registration.Section.TypeInfo);
                    if (dto is null) throw new JsonException("Null section.");
                    prepared.Add(new(key, registration, map, dto, raw.Version, null));
                }
                catch (Exception error) when (error is JsonException or SaveFormatException or NotSupportedException or ArgumentException or InvalidOperationException or OverflowException or KeyNotFoundException or FormatException)
                {
                    prepared.Add(PrepareInvalidSection(key, registration, map, raw.Version, error));
                }
            }
        }
        return new(document, prepared);
    }

    public IReadOnlyDictionary<string, RawSaveSection> Serialize(IReadOnlyList<CapturedSection> captures,
        IReadOnlyDictionary<string, RawSaveSection> previous, MapId? expectedMap = null, WorldSaveMoment? moment = null)
    {
        var output = new SortedDictionary<string, RawSaveSection>(StringComparer.Ordinal);
        foreach (var (key, raw) in previous.OrderBy(p => p.Key, StringComparer.Ordinal))
            if (Find(key) is null) output.Add(key, raw);
        foreach (var capture in captures)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject(); writer.WriteNumber("v", capture.Registration.Attribute.Version); writer.WritePropertyName("data");
                JsonSerializer.Serialize(writer, capture.Dto, capture.Registration.Section.TypeInfo);
                writer.WriteEndObject();
            }
            output.Add(capture.Key, new(capture.Registration.Attribute.Version, stream.ToArray()));
        }
        ValidateSnapshotTimeline(output, expectedMap, moment);
        AlignCurrentMapVisit(output);
        return output;
    }

    private static PreparedSection PrepareInvalidSection(string key, SaveSectionRegistration registration,
        MapId map, int version, [FailureHandler("SYS-SAVE-FORMAT")] Exception error)
    {
        if (IsCore(registration.Attribute))
            throw new SaveFormatException("Invalid core section " + key, error);
        return new(key, registration, map, null, version, "Replaced invalid section: " + key) { Error = error };
    }

    internal SaveSectionRegistration? ForPayload<T>() => Registered.SingleOrDefault(r => r.Section.TypeInfo.Type == typeof(T));

    internal bool TryRaw<T>(IReadOnlyDictionary<string, RawSaveSection> sections, MapId map, out RawSaveSection raw)
    {
        var registration = ForPayload<T>();
        if (registration is not null && sections.TryGetValue(Key(registration.Attribute, map), out var found))
        { raw = found; return true; }
        raw = null!; return false;
    }

    private void AlignCurrentMapVisit(IDictionary<string, RawSaveSection> sections)
    {
        var mapsRegistration = ForPayload<MapsSave>();
        var viewRegistration = ForPayload<ViewSaveData>();
        if (mapsRegistration is null || viewRegistration is null) return;
        var mapsKey = Key(mapsRegistration.Attribute, default);
        var viewKey = Key(viewRegistration.Attribute, default);
        if (!sections.TryGetValue(mapsKey, out var maps) || !sections.TryGetValue(viewKey, out var view)) return;
        // The current visit and metadata describe the immutable view snapshot, including forced landings.
        var mapEnvelope = JsonNode.Parse(maps.Envelope) ?? throw new JsonException("Missing maps envelope.");
        var viewEnvelope = JsonNode.Parse(view.Envelope) ?? throw new JsonException("Missing view envelope.");
        var data = mapEnvelope["data"] ?? throw new JsonException("Missing maps data.");
        var current = data["current"]?.GetValue<string>() ?? throw new JsonException("Missing current map.");
        var pose = viewEnvelope["data"]?["pose"] ?? throw new JsonException("Missing view pose.");
        var mode = viewEnvelope["data"]?["mode"] ?? throw new JsonException("Missing view mode.");
        var visits = data["visits"]?.AsArray() ?? throw new JsonException("Missing map visits.");
        var visit = visits.FirstOrDefault(v => v?["map"]?.GetValue<string>() == current)
            ?? throw new JsonException("Current map has no visit.");
        visit["lastPose"] = pose.DeepClone(); visit["mode"] = mode.DeepClone();
        sections[mapsKey] = new(maps.Version, Encoding.UTF8.GetBytes(mapEnvelope.ToJsonString()));
    }

    private void ValidateSnapshotTimeline(IReadOnlyDictionary<string, RawSaveSection> sections, MapId? expectedMap, WorldSaveMoment? moment)
    {
        if (expectedMap is { } map && TryRaw<MapsSave>(sections, map, out var maps))
        {
            using var json = SaveContainer.ParseJson(maps.Envelope);
            if (json.RootElement.GetProperty("data").GetProperty("current").GetString() != map.Value)
                throw new SaveFormatException("Map changed during snapshot; the generation was not written.");
        }
        if (!TryRaw<WorldSaveV1>(sections, expectedMap ?? default, out var world))
        {
            if (moment.HasValue) throw new SaveFormatException("The world snapshot section is missing.");
            return;
        }
        using var worldJson = SaveContainer.ParseJson(world.Envelope);
        var worldData = worldJson.RootElement.GetProperty("data");
        if (moment is { } expected)
        {
            if (worldData.GetProperty("worldHours").GetDouble() != expected.WorldHours ||
                worldData.GetProperty("clockOffset").GetDouble() != expected.ClockOffset ||
                !Enum.TryParse<Season>(worldData.GetProperty("seasonTarget").GetString(), true, out var season) || season != expected.Season ||
                worldData.GetProperty("flowRate").GetInt32() != expected.FlowRate)
                throw new SaveFormatException("World section disagrees with the shared capture moment.");
        }
    }

    public static string Key(SaveSectionAttribute attribute, MapId map) => attribute.Scope switch
    {
        SaveScope.Global => attribute.Id,
        SaveScope.PerMap => "map:" + map.Value + "/" + attribute.Id,
        _ => throw new ArgumentOutOfRangeException(nameof(attribute))
    };

    private SaveSectionRegistration? Find(string key)
    {
        var perMap = key.StartsWith("map:", StringComparison.Ordinal);
        var id = perMap ? key[(key.IndexOf('/') + 1)..] : key;
        return Registered.FirstOrDefault(r => r.Attribute.Id == id && r.Attribute.Scope == (perMap ? SaveScope.PerMap : SaveScope.Global));
    }

    private static bool IsCore(SaveSectionAttribute attribute) => attribute.Phase == RestorePhase.World;

    private static void ValidateData(JsonElement data, Type payloadType)
    {
        ValidateFinite(data);
        if (payloadType == typeof(WorldSaveV1))
        {
            foreach (var field in new[] { "flowRate", "flowRateBeforePause" })
                if (data.TryGetProperty(field, out var flow) && !FlowRates.IsValid(flow.GetInt32()))
                    throw new JsonException("Invalid world flow rate.");
            if (data.TryGetProperty("worldHours", out var hours) && hours.GetDouble() < 0)
                throw new JsonException("Negative world time.");
            if (data.TryGetProperty("worldHours", out hours) && data.TryGetProperty("clockOffset", out var offset) &&
                !double.IsFinite(hours.GetDouble() + offset.GetDouble())) throw new JsonException("World clock overflow.");
            if (data.TryGetProperty("flowRateBeforePause", out var before) && before.GetInt32() == 0)
                throw new JsonException("Paused restore rate cannot be zero.");
            if (data.TryGetProperty("animTime", out var anim) && (anim.GetDouble() < 0 || anim.GetDouble() >= WorldTime.AnimLoopSeconds))
                throw new JsonException("Invalid animation time.");
            if (data.TryGetProperty("seasonPos", out var position) && data.TryGetProperty("seasonTarget", out var target) &&
                (!Enum.TryParse<Season>(target.GetString(), true, out var season) || position.GetDouble() != (int)season))
                throw new JsonException("Season position is not settled at its target.");
        }
    }

    private static void ValidateFinite(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Number:
                if (!value.TryGetDouble(out var number) || !double.IsFinite(number)) throw new JsonException("Non-finite number.");
                break;
            case JsonValueKind.Object:
                foreach (var property in value.EnumerateObject()) ValidateFinite(property.Value);
                break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray()) ValidateFinite(item);
                break;
        }
    }
}
