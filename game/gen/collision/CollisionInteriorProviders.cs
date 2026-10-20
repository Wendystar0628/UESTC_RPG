using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.Collision;

/// <summary>Resolves only the frozen per-map provider instances captured by this build context.</summary>
internal static class CollisionInteriorProviders
{
    public static IReadOnlyList<CollisionInterior> Resolve(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var providers = context.InteriorProviders
            ?? throw Failure(context, "The map has no interior provider snapshot.");
        var resolved = new Dictionary<string, CollisionInterior>(StringComparer.Ordinal);
        foreach (var pair in providers.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null)
                throw Failure(context, "An interior binding has an empty provider ID or instance.");
            var features = pair.Value.FeatureIds?.OrderBy(f => f.Value, StringComparer.Ordinal).ToArray()
                ?? throw Failure(context, $"Provider '{pair.Key}' has no feature list.");
            if (features.Length == 0)
                throw Failure(context, $"Provider '{pair.Key}' is not configured: its feature list is empty.");
            if (features.Distinct().Count() != features.Length)
                throw Failure(context, $"Provider '{pair.Key}' has duplicate feature bindings.");
            foreach (var feature in features)
                if (!context.World.Features.ContainsKey(feature))
                    throw Failure(context, $"Provider '{pair.Key}' references missing feature '{feature.Value}'.");
            resolved.Add(pair.Key, new(pair.Key, pair.Value, features));
        }

        // Only indoor permissions require providers; outdoor access stays with its owning site.
        // Catalog bytes are caller-preloaded inputs, not generator file IO.
        if (context.SourceInputs.TryGetValue("interiors.json", out var catalogBytes))
        {
            using var document = JsonDocument.Parse(catalogBytes);
            if (!document.RootElement.TryGetProperty("interiors", out var entries) || entries.ValueKind != JsonValueKind.Array)
                throw Failure(context, "interiors.json has no interiors array.");
            var catalogProviders = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in entries.EnumerateArray())
            {
                var features = entry.GetProperty("featureIds");
                if (features.ValueKind != JsonValueKind.Array || features.GetArrayLength() == 0)
                    throw Failure(context, "An interior catalog entry has no catalog feature bindings.");
                var openParts = InteriorEntranceJson.ParseOpenParts(entry);
                var indoorFeatures = new List<FeatureId>();
                foreach (var featureValue in features.EnumerateArray())
                {
                    var id = featureValue.GetString();
                    if (string.IsNullOrWhiteSpace(id)) throw Failure(context, "An interior catalog entry declares an empty feature ID.");
                    var feature = new FeatureId(id);
                    if (InteriorOpenPart.IndoorFloorsFor(openParts, feature).Count > 0) indoorFeatures.Add(feature);
                }
                if (indoorFeatures.Count == 0) continue;
                var providerId = entry.GetProperty("providerId").GetString();
                if (string.IsNullOrWhiteSpace(providerId)) throw Failure(context, "An interior catalog entry has no provider ID.");
                catalogProviders.Add(providerId);
                if (!resolved.ContainsKey(providerId))
                    throw Failure(context, $"Declared provider '{providerId}' is missing from this map's snapshot.");
                foreach (var feature in indoorFeatures) RequireBinding(providerId, feature);
            }
            foreach (var id in resolved.Keys.Order(StringComparer.Ordinal))
                if (!catalogProviders.Contains(id))
                    throw Failure(context, $"Provider '{id}' is not bound by this map's interior catalog.");
        }

        foreach (var feature in context.World.Features.Keys.OrderBy(f => f.Value, StringComparer.Ordinal))
        {
            var indexed = context.World.Interiors.Get(feature);
            if (indexed is not { Open: true } || !indexed.OpenAreas.Any(area => InteriorOpenPart.IsIndoor(area.Kind))) continue;
            if (string.IsNullOrWhiteSpace(indexed.ProviderId))
                throw Failure(context, $"Open interior '{feature.Value}' has no indexed provider ID.");
            var binding = RequireBinding(indexed.ProviderId, feature);
            if (!binding.Provider.Walkable)
                throw Failure(context, $"Open interior '{feature.Value}' is bound to non-walkable provider '{binding.Id}'.");
        }
        return resolved.Values.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();

        CollisionInterior RequireBinding(string providerId, FeatureId feature)
        {
            if (!resolved.TryGetValue(providerId, out var binding))
                throw Failure(context, $"Open interior '{feature.Value}' requires missing provider '{providerId}'.");
            if (!binding.FeatureIds.Contains(feature))
                throw Failure(context, $"Provider '{providerId}' is not configured for feature '{feature.Value}' in this map.");
            return binding;
        }
    }

    private static InvalidOperationException Failure(BuildContext context, string message) =>
        new($"Collision interior bindings for map '{context.Map.Id.Value}': {message}");
}
