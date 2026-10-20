using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Gates;

/// <summary>Captures formal runtime save sections and compares their persisted state.</summary>
internal static class CoreRuntimeSnapshot
{
    public static string Capture()
    {
        var map = KernelServices.Get<IMapService>();
        var moment = KernelServices.Get<IWorldRestore>().GetSaveMoment();
        var entries = KernelServices.Services.Query<ISaveSection>(RegistrationKind.Save)
            .OrderBy(entry => entry.Id, StringComparer.Ordinal).ThenBy(entry => entry.Scope).ToArray();
        if (!new[] { "world", "maps", "weather", "view", "walk" }.All(id => entries.Any(entry => entry.Id == id)))
            throw new InvalidOperationException("Formal core save sections are missing.");
        using var bytes = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bytes))
        {
            writer.WriteStartObject();
            foreach (var entry in entries)
            {
                var attribute = entry.Type.GetCustomAttributes<SaveSectionAttribute>()
                    .Single(item => item.Id == entry.Id && item.Scope.ToString() == entry.Scope);
                var section = (ISaveSection)KernelServices.Services.Create(entry.Type);
                foreach (var sectionMap in attribute.Scope == SaveScope.Global ? [map.Current]
                    : map.Visits.Select(visit => visit.Map).Append(map.Current).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal).ToArray())
                {
                    writer.WritePropertyName(attribute.Scope == SaveScope.Global ? entry.Id : "map:" + sectionMap.Value + "/" + entry.Id);
                    var context = new CaptureContext(SaveReason.Test, sectionMap, false) { Moment = moment };
                    JsonSerializer.Serialize(writer, section.Capture(in context), section.TypeInfo);
                }
            }
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    // Landing may settle any spatial position. Other values, numeric spelling and order remain exact.
    public static bool Matches(string saved, string restored) =>
        MatchCoreNode(JsonNode.Parse(saved), JsonNode.Parse(restored));

    private static bool MatchCoreNode(JsonNode? saved, JsonNode? restored)
    {
        if (saved is JsonObject before && restored is JsonObject after)
        {
            if (before.Count != after.Count) return false;
            using var properties = after.GetEnumerator();
            foreach (var property in before)
            {
                if (!properties.MoveNext() || property.Key != properties.Current.Key) return false;
                if (property.Key == "position")
                {
                    if (!MatchCorePosition(property.Value as JsonArray, properties.Current.Value as JsonArray)) return false;
                }
                else if (!MatchCoreNode(property.Value, properties.Current.Value)) return false;
            }
            return true;
        }
        if (saved is JsonArray beforeItems && restored is JsonArray afterItems)
            return beforeItems.Count == afterItems.Count
                && beforeItems.Zip(afterItems).All(pair => MatchCoreNode(pair.First, pair.Second));
        return saved?.ToJsonString(ContractJson.Options) == restored?.ToJsonString(ContractJson.Options);
    }

    private static bool MatchCorePosition(JsonArray? saved, JsonArray? restored)
    {
        const double positionTolerance = .02;
        if (saved is null || restored is null || saved.Count != 3 || restored.Count != 3) return false;
        double distanceSquared = 0;
        for (var i = 0; i < 3; i++)
        {
            if (saved[i] is not JsonValue savedValue || restored[i] is not JsonValue restoredValue
                || !savedValue.TryGetValue<double>(out var a) || !restoredValue.TryGetValue<double>(out var b)) return false;
            if (!double.IsFinite(a) || !double.IsFinite(b)) return false;
            var delta = a - b;
            distanceSquared += delta * delta;
        }
        return distanceSquared <= positionTolerance * positionTolerance;
    }
}
