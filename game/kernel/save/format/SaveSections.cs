using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Save;

namespace Campus.Kernel.Save.Format;

public sealed record SaveSectionRegistration(SaveSectionAttribute Attribute, ISaveSection Section);
public sealed record PreparedSection(string Key, SaveSectionRegistration Registration, MapId Map,
    object? Dto, int FromVersion, string? Repair);
public sealed record PreparedSave(SaveDocument Document, IReadOnlyList<PreparedSection> Sections);
public sealed record CapturedSection(string Key, SaveSectionRegistration Registration, object Dto);

/// <summary>Pure decoding/migration is separate from main-thread Capture, CreateDefault and Restore.</summary>
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
        // A newer known segment makes the entire slot read-only even when some other segment is corrupt.
        foreach (var (key, raw) in document.Sections)
        {
            var registration = Find(key);
            if (registration is not null && raw.Version > registration.Attribute.Version)
                throw new SaveFormatException("Newer section version: " + key, newerBuild: true);
        }
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
                    object? dto;
                    if (raw.Version == attribute.Version)
                    {
                        ValidateData(data, attribute, map);
                        dto = JsonSerializer.Deserialize(data, registration.Section.TypeInfo);
                    }
                    else
                    {
                        var migrated = JsonNode.Parse(data.GetRawText()) ?? throw new JsonException("Null section.");
                        for (var version = raw.Version; version < attribute.Version; version++)
                            migrated = registration.Section.Migrate(version, migrated) ?? throw new JsonException("Null migration result.");
                        using var checkedJson = SaveContainer.ParseJson(Encoding.UTF8.GetBytes(migrated.ToJsonString()));
                        ValidateData(checkedJson.RootElement, attribute, map);
                        dto = JsonSerializer.Deserialize(checkedJson.RootElement, registration.Section.TypeInfo);
                    }
                    if (dto is null) throw new JsonException("Null section.");
                    prepared.Add(new(key, registration, map, dto, raw.Version, null));
                }
                catch (Exception error) when (error is JsonException or SaveFormatException or NotSupportedException or ArgumentException or InvalidOperationException or OverflowException or KeyNotFoundException or FormatException)
                {
                    if (IsCore(attribute)) throw new SaveFormatException("Invalid core section " + key + ": " + error.Message);
                    prepared.Add(new(key, registration, map, null, raw.Version, "Replaced invalid section: " + key));
                }
            }
        }
        try { ValidatePreparedTimeline(prepared, document.Meta.Map); }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        { throw new SaveFormatException("Invalid core timeline: " + error.Message); }
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

    private static void AlignCurrentMapVisit(IDictionary<string, RawSaveSection> sections)
    {
        if (!sections.TryGetValue("maps", out var maps) || !sections.TryGetValue("view", out var view)) return;
        // The current visit and metadata are summaries of this exact immutable view snapshot, including forced landings.
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
        if (sections.TryGetValue("world", out var world))
        {
            var worldData = JsonNode.Parse(world.Envelope)?["data"];
            if (worldData?["worldHours"] is { } hours) visit["leftAtHours"] = hours.DeepClone();
        }
        if (sections.TryGetValue("map:" + current + "/weather", out var weather))
        {
            var weatherData = JsonNode.Parse(weather.Envelope)?["data"];
            if (weatherData?["epoch"] is { } epoch) visit["weatherEpoch"] = epoch.DeepClone();
        }
        sections["maps"] = new(maps.Version, Encoding.UTF8.GetBytes(mapEnvelope.ToJsonString()));
    }

    private static void ValidatePreparedTimeline(IReadOnlyList<PreparedSection> prepared, MapId currentMap)
    {
        var world = prepared.FirstOrDefault(p => p.Registration.Attribute.Id == "world" && p.Registration.Attribute.Scope == SaveScope.Global);
        var weather = prepared.FirstOrDefault(p => p.Map == currentMap && p.Registration.Attribute.Id == "weather" && p.Registration.Attribute.Scope == SaveScope.PerMap);
        if (world?.Dto is null || weather?.Dto is null) return; // A missing segment is explicitly defaulted on the main thread.
        var worldBytes = JsonSerializer.SerializeToUtf8Bytes(world.Dto, world.Registration.Section.TypeInfo);
        var weatherBytes = JsonSerializer.SerializeToUtf8Bytes(weather.Dto, weather.Registration.Section.TypeInfo);
        using var worldJson = SaveContainer.ParseJson(worldBytes);
        using var weatherJson = SaveContainer.ParseJson(weatherBytes);
        CheckWeatherMoment(worldJson.RootElement, weatherJson.RootElement);
    }

    private static void ValidateSnapshotTimeline(IReadOnlyDictionary<string, RawSaveSection> sections, MapId? expectedMap, WorldSaveMoment? moment)
    {
        if (expectedMap is { } map && sections.TryGetValue("maps", out var maps))
        {
            using var json = SaveContainer.ParseJson(maps.Envelope);
            if (json.RootElement.GetProperty("data").GetProperty("current").GetString() != map.Value)
                throw new SaveFormatException("Map changed during snapshot; the generation was not written.");
        }
        if (!sections.TryGetValue("world", out var world))
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
        if (expectedMap is { } current && sections.TryGetValue("map:" + current.Value + "/weather", out var weather))
        {
            using var weatherJson = SaveContainer.ParseJson(weather.Envelope);
            CheckWeatherMoment(worldData, weatherJson.RootElement.GetProperty("data"));
        }
    }

    private static void CheckWeatherMoment(JsonElement world, JsonElement weather)
    {
        if (world.GetProperty("worldHours").GetDouble() != weather.GetProperty("leftAtHours").GetDouble() ||
            world.GetProperty("clockOffset").GetDouble() != weather.GetProperty("clockOffset").GetDouble() ||
            !string.Equals(world.GetProperty("seasonTarget").GetString(), weather.GetProperty("season").GetString(), StringComparison.OrdinalIgnoreCase))
            throw new SaveFormatException("World and current-map weather describe different moments.");
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

    private static void ValidateData(JsonElement data, SaveSectionAttribute attribute, MapId map)
    {
        ValidateNumbersAndArrays(data, attribute.Id == "weather" ? 256 : int.MaxValue);
        if (attribute.Id == "world")
        {
            foreach (var field in new[] { "flowRate", "flowRateBeforePause" })
                if (data.TryGetProperty(field, out var flow) && flow.GetInt32() is not (0 or 1 or 10 or 60 or 360 or 1440))
                    throw new JsonException("Invalid world flow rate.");
            if (data.TryGetProperty("worldHours", out var hours) && hours.GetDouble() < 0)
                throw new JsonException("Negative world time.");
            if (data.TryGetProperty("worldHours", out hours) && data.TryGetProperty("clockOffset", out var offset) &&
                !double.IsFinite(hours.GetDouble() + offset.GetDouble())) throw new JsonException("World clock overflow.");
            if (data.TryGetProperty("flowRateBeforePause", out var before) && before.GetInt32() == 0)
                throw new JsonException("Paused restore rate cannot be zero.");
            if (data.TryGetProperty("animTime", out var anim) && (anim.GetDouble() < 0 || anim.GetDouble() >= 480))
                throw new JsonException("Invalid animation time.");
            if (data.TryGetProperty("seasonPos", out var position) && data.TryGetProperty("seasonTarget", out var target) &&
                (!Enum.TryParse<Season>(target.GetString(), true, out var season) || position.GetDouble() != (int)season))
                throw new JsonException("Season position is not settled at its target.");
        }
        if (attribute.Id == "atlas" && data.TryGetProperty("pins", out var pins) && pins.GetArrayLength() > 50)
            throw new JsonException("More than fifty custom pins.");
        if (attribute.Id == "weather" && attribute.Scope == SaveScope.PerMap) ValidateWeather(data, map);
    }

    private static void ValidateWeather(JsonElement data, MapId map)
    {
        if (data.GetProperty("map").GetString() != map.Value) throw new JsonException("Weather map does not match its section key.");
        foreach (var field in new[] { "segment", "attempt", "leftAtHours", "quietUntilHours" })
            if (data.TryGetProperty(field, out var value) && value.GetDouble() < 0) throw new JsonException("Negative weather counter: " + field);
        foreach (var field in new[] { "wet", "puddle", "snowAdd", "wetOverride", "pinIntensity" })
            CheckUnit(data, field);
        CheckUnit(data.GetProperty("selection"), "intensity");
        var queue = data.GetProperty("queue");
        if (queue.GetArrayLength() == 0) throw new JsonException("Weather queue is empty.");
        var from = data.GetProperty("queueFromHours").GetDouble();
        var left = data.GetProperty("leftAtHours").GetDouble();
        if (from > left) throw new JsonException("Weather queue starts after the saved time.");
        var end = from;
        foreach (var item in queue.EnumerateArray())
        {
            var minutes = item.GetProperty("minutes").GetDouble();
            if (minutes <= 0 || minutes > 1e6) throw new JsonException("Invalid weather duration.");
            CheckUnit(item, "intensity"); end += minutes / 60;
        }
        if (!double.IsFinite(end) || end < left + 24 - 1e-8) throw new JsonException("Weather forecast does not cover twenty-four hours.");
        var hasKind = Present(data, "pinKind");
        if (hasKind != Present(data, "pinFromHours") || hasKind != Present(data, "pinToHours") || !hasKind && Present(data, "pinIntensity"))
            throw new JsonException("Incomplete weather pin.");
        if (hasKind && data.GetProperty("pinToHours").GetDouble() <= data.GetProperty("pinFromHours").GetDouble())
            throw new JsonException("Invalid weather pin interval.");
    }
    private static bool Present(JsonElement data, string field) => data.TryGetProperty(field, out var value) && value.ValueKind != JsonValueKind.Null;
    private static void CheckUnit(JsonElement data, string field)
    {
        if (Present(data, field) && (data.GetProperty(field).GetDouble() < 0 || data.GetProperty(field).GetDouble() > 1))
            throw new JsonException("Weather amount outside [0,1]: " + field);
    }

    private static void ValidateNumbersAndArrays(JsonElement value, int arrayLimit)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Number:
                if (!value.TryGetDouble(out var number) || !double.IsFinite(number)) throw new JsonException("Non-finite number.");
                break;
            case JsonValueKind.Object:
                foreach (var property in value.EnumerateObject()) ValidateNumbersAndArrays(property.Value, arrayLimit);
                break;
            case JsonValueKind.Array:
                if (value.GetArrayLength() > arrayLimit) throw new JsonException("Array exceeds section limit.");
                foreach (var item in value.EnumerateArray()) ValidateNumbersAndArrays(item, arrayLimit);
                break;
        }
    }
}

public static class SaveMigrations
{
    /// <summary>Apply build-generated immutable ID mappings without I/O or service access.</summary>
    public static JsonNode RemapIds(JsonNode data, IReadOnlyDictionary<string, string> mapping)
    {
        var result = data.DeepClone();
        Rewrite(result, mapping);
        return result;
    }

    private static void Rewrite(JsonNode node, IReadOnlyDictionary<string, string> mapping)
    {
        if (node is JsonObject obj)
        {
            var renamed = new List<KeyValuePair<string, JsonNode?>>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (key, child) in obj.ToArray())
            {
                var newKey = mapping.TryGetValue(key, out var replacementKey) ? replacementKey : key;
                if (!keys.Add(newKey)) throw new JsonException("ID remap would merge two object keys.");
                JsonNode? rewritten = child;
                if (child is JsonValue value && value.TryGetValue<string>(out var id) && mapping.TryGetValue(id, out var replacement))
                    rewritten = JsonValue.Create(replacement);
                else if (child is not null) Rewrite(child, mapping);
                renamed.Add(new(newKey, rewritten));
            }
            obj.Clear();
            foreach (var (key, child) in renamed) obj.Add(key, child);
        }
        else if (node is JsonArray array)
            for (var i = 0; i < array.Count; i++)
            {
                if (array[i] is JsonValue value && value.TryGetValue<string>(out var id) && mapping.TryGetValue(id, out var replacement))
                    array[i] = replacement;
                else if (array[i] is { } child) Rewrite(child, mapping);
            }
    }
}
