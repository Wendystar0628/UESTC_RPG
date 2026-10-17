using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;

namespace Campus.Kernel.Registry;

public sealed record RegistryGateReport(string Scope, int Maps, int Services, int Declarations,
    int Entities, int Schemas, int FramePriorities, int QualityKnobs, IReadOnlyList<string> NotImplemented);

/// <summary>Headless declaration checks. Never constructs services, nodes, or geometry.</summary>
public static class RegistryGateRunner
{
    public static RegistryGateReport Run(ServiceRegistry registry, string indexPath, string requestedMap,
        Func<string, byte[]> read, IReadOnlyCollection<Type> compositionInputs)
    {
        var index = JsonSerializer.Deserialize<MapIndex>(read(indexPath), ContractJson.Options)
            ?? throw new InvalidDataException("Map index is null.");
        if (index.Version != 1 || index.Maps.Count == 0 ||
            index.Maps.Select(entry => entry.Id).Distinct().Count() != index.Maps.Count ||
            !index.Maps.Any(entry => entry.Id == index.DefaultMap))
            throw new InvalidDataException("Map index version, unique IDs, or default map is invalid.");
        var mapIds = index.Maps.Select(entry => entry.Id.Value).ToHashSet(StringComparer.Ordinal);
        var requested = string.IsNullOrEmpty(requestedMap) ? index.DefaultMap.Value : requestedMap;
        if (requested != "*" && !mapIds.Contains(requested)) throw new InvalidDataException($"Unknown map: {requested}");
        ValidateDeclarations(registry, mapIds);
        ValidateConstructorGraph(registry, compositionInputs);

        var entityCount = 0;
        var prefixes = new Dictionary<string, MapId>(StringComparer.Ordinal);
        var selected = index.Maps.Where(entry => requested == "*" || entry.Id.Value == requested).ToArray();
        foreach (var entry in selected)
        {
            if (!entry.Dir.StartsWith("res://", StringComparison.Ordinal) || !entry.Dir.EndsWith('/') ||
                entry.Dir.Split('/').Any(part => part is "." or ".."))
                throw new InvalidDataException($"Invalid map data root: {entry.Id}");
            var map = JsonSerializer.Deserialize<MapFile>(read(entry.Dir + "map.json"), ContractJson.Options)
                ?? throw new InvalidDataException($"Map descriptor is null: {entry.Id}");
            var points = JsonSerializer.Deserialize<WaypointFile>(read(entry.Dir + "waypoints.json"), ContractJson.Options)
                ?? throw new InvalidDataException($"Waypoints are null: {entry.Id}");
            if (map.Id != entry.Id || map.Version < 1 || points.Version < 1)
                throw new InvalidDataException($"Map identity or version mismatch: {entry.Id}");
            var rect = map.DataRect;
            if (!Finite(rect.Min) || !Finite(rect.Max) || rect.Max.X <= rect.Min.X || rect.Max.Y <= rect.Min.Y)
                throw new InvalidDataException($"Invalid dataRect: {entry.Id}");
            if (map.Spawns.Select(spawn => spawn.Id).Distinct(StringComparer.Ordinal).Count() != map.Spawns.Count ||
                points.Waypoints.Select(point => point.Id).Distinct(StringComparer.Ordinal).Count() != points.Waypoints.Count)
                throw new InvalidDataException($"Duplicate spawn or waypoint: {entry.Id}");
            var spawn = map.Spawns.FirstOrDefault(item => item.Id == map.DefaultSpawn)?.Fly ??
                points.Waypoints.FirstOrDefault(item => item.Id == map.DefaultSpawn)?.Fly ??
                throw new InvalidDataException($"Default spawn cannot be resolved: {entry.Id}/{map.DefaultSpawn}");
            foreach (var pose in map.Spawns.Select(item => item.Fly).Concat(points.Waypoints.Select(item => item.Fly)).Append(spawn))
                ValidatePose(entry.Id, pose);

            using var campus = JsonDocument.Parse(read(entry.Dir + "campus.json"));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entity in campus.RootElement.GetProperty("entities").EnumerateArray())
            {
                var id = entity.GetProperty("id").GetString();
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new InvalidDataException($"Empty or duplicate entity ID: {entry.Id}/{id}");
                var prefix = id.Split(':', 2)[0];
                if (prefix is "npc" or "item" or "quest") throw new InvalidDataException($"Reserved RPG entity prefix: {id}");
                if (prefixes.TryGetValue(prefix, out var owner) && owner != entry.Id)
                    throw new InvalidDataException($"Entity prefix shared by maps: {prefix} ({owner}, {entry.Id})");
                prefixes[prefix] = entry.Id;
            }
            if (map.Env.ReflectLakes.Select(id => id.Value).Distinct(StringComparer.Ordinal).Count() != map.Env.ReflectLakes.Count ||
                map.Env.ReflectLakes.Any(id => !ids.Contains(id.Value)))
                throw new InvalidDataException($"Reflection lake is duplicate or absent from map entities: {entry.Id}");
            entityCount += ids.Count;
            var effective = registry.ForMap(entry.Id.Value);
            if (effective.Where(item => item.Kind != "service").GroupBy(item => (item.Kind, item.Id, item.Scope)).Any(group => group.Count() > 1))
                throw new InvalidDataException($"Generic/map registrations conflict in map: {entry.Id}");
            using var interiors = JsonDocument.Parse(read(entry.Dir + "interiors.json"));
            foreach (var interior in interiors.RootElement.GetProperty("interiors").EnumerateArray())
            {
                var provider = interior.GetProperty("providerId").GetString();
                if (!effective.Any(item => item.Kind == "interior" && item.Id == provider))
                    throw new InvalidDataException($"Open interior provider is not registered: {entry.Id}/{provider}");
                foreach (var feature in interior.GetProperty("featureIds").EnumerateArray())
                    if (!ids.Contains(feature.GetString()!)) throw new InvalidDataException($"Open interior entity is missing: {entry.Id}/{feature}");
            }
        }
        var priorities = typeof(FramePriority).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral).Select(field => (int)field.GetRawConstantValue()!).ToArray();
        if (priorities.Length != 18 || priorities.Distinct().Count() != priorities.Length ||
            FramePriority.Animate <= FramePriority.Systems || FramePriority.Animate >= FramePriority.Audio)
            throw new InvalidDataException("Frame priority registry is incomplete or ambiguous.");
        var knobs = Enum.GetValues<QualityKnob>();
        if (knobs.Distinct().Count() != knobs.Length || knobs.Select(knob => (int)knob).Where((value, indexValue) => value != indexValue).Any())
            throw new InvalidDataException("Quality knob values must be unique and contiguous.");
        var assembly = typeof(ContractJson).Assembly;
        var schemas = assembly.GetManifestResourceNames().Where(name => name.EndsWith(".schema.json", StringComparison.Ordinal)).ToArray();
        if (!schemas.Any(name => name.EndsWith("map.schema.json", StringComparison.Ordinal)))
            throw new InvalidDataException("Embedded map schema is missing.");
        foreach (var name in schemas)
        {
            using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidDataException($"Embedded schema missing: {name}");
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException($"Schema is not a JSON object: {name}");
        }
        return new("declarations + constructor graph + map data; runtime core reported separately", selected.Length,
            registry.Entries.Count(entry => entry.Kind == "service"), registry.Entries.Count, entityCount,
            schemas.Length, priorities.Length, knobs.Length,
            [
                "WP-27/WP-08: evaluate runtime Claims against each loaded world, including generic FeatureClaim rejection and overlapping claims.",
                "WP-27/WP-08/WP-44: overlay difference reports and fidelity input freshness/coverage.",
                "WP-27/WP-32: selected IQualityService.Knobs values and complete QualityResponsive ownership on runtime consumers.",
                "WP-27/owners: service/node startup, save TypeInfo/ownership, audio buses/emitters, UI theme and reserved-directory checks.",
                "WP-27: provider FeatureIds/door-state equality and generated palette material usage."
            ]);
    }

    public static void ValidateConstructorGraph(ServiceRegistry registry, IReadOnlyCollection<Type> compositionInputs)
    {
        var bindings = new Dictionary<Type, Type>();
        foreach (var entry in registry.Entries.Where(item => item.Kind == "service"))
        {
            var contract = entry.Type.GetCustomAttributes<KernelServiceAttribute>()
                .Single(attribute => attribute.Service.FullName == entry.Id).Service;
            if (!contract.IsAssignableFrom(entry.Type)) throw new InvalidDataException($"Invalid binding: {entry.Id}");
            bindings.Add(contract, entry.Type);
        }
        var done = new HashSet<Type>();
        var path = new HashSet<Type>();
        foreach (var implementation in registry.Entries.Select(entry => entry.Type).Distinct()) Visit(implementation);
        void Visit(Type implementation)
        {
            if (done.Contains(implementation)) return;
            if (!path.Add(implementation)) throw new InvalidDataException($"Circular constructor dependency: {implementation.FullName}");
            var constructor = implementation.GetConstructors().OrderByDescending(item => item.GetParameters().Length).FirstOrDefault()
                ?? throw new InvalidDataException($"No public constructor: {implementation.FullName}");
            foreach (var parameter in constructor.GetParameters())
            {
                if (bindings.TryGetValue(parameter.ParameterType, out var dependency)) Visit(dependency);
                else if (!compositionInputs.Contains(parameter.ParameterType) && !parameter.HasDefaultValue)
                    throw new InvalidDataException($"Unresolved {parameter.ParameterType.FullName} for {implementation.FullName}");
            }
            path.Remove(implementation);
            done.Add(implementation);
        }
    }

    private static void ValidateDeclarations(ServiceRegistry registry, IReadOnlySet<string> mapIds)
    {
        var qualityOwners = new HashSet<QualityKnob>();
        var saveOrders = new HashSet<(RestorePhase, int)>();
        foreach (var entry in registry.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.Owner)) throw new InvalidDataException("Empty registration identity.");
            if (entry.Map is { Length: > 0 } map && !mapIds.Contains(map)) throw new InvalidDataException($"Unknown MapScope: {entry.Id}/{map}");
            var valid = entry.Kind switch
            {
                "service" => true,
                "content" => typeof(IContentModule).IsAssignableFrom(entry.Type),
                "interior" => typeof(IInteriorProvider).IsAssignableFrom(entry.Type),
                "ambient" => typeof(IAmbientEmitterSource).IsAssignableFrom(entry.Type),
                "bake" => typeof(IBakeProducer).IsAssignableFrom(entry.Type),
                "medium" => typeof(IMediumPresenter).IsAssignableFrom(entry.Type),
                "save" => typeof(ISaveSection).IsAssignableFrom(entry.Type),
                "part" => entry.Type.GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IPartDef<>)),
                "node" => IsNode(entry.Type),
                _ => throw new InvalidDataException($"Unknown registration kind: {entry.Kind}")
            };
            if (!valid) throw new InvalidDataException($"Registration type does not implement its contract: {entry.Id}/{entry.Type.FullName}");
            if (entry.Kind == "node" && entry.Type.GetCustomAttribute<QualityResponsiveAttribute>() is { } quality)
            {
                if (!typeof(IQualityAware).IsAssignableFrom(entry.Type)) throw new InvalidDataException($"QualityResponsive node lacks IQualityAware: {entry.Id}");
                foreach (var knob in quality.Owns.Concat(quality.Reads))
                    if (!Enum.IsDefined(knob)) throw new InvalidDataException($"Unknown quality knob: {entry.Id}/{knob}");
                foreach (var knob in quality.Owns)
                    if (!qualityOwners.Add(knob)) throw new InvalidDataException($"Duplicate quality owner: {knob}");
            }
            if (entry.Kind == "save")
            {
                if (entry.Scope is null) throw new InvalidDataException($"Save registration has no scope: {entry.Id}");
                var section = entry.Type.GetCustomAttributes<SaveSectionAttribute>()
                    .Single(attribute => attribute.Id == entry.Id && attribute.Scope == entry.Scope);
                if (section.Version < 1 || !Enum.IsDefined(section.Phase) || !Enum.IsDefined(section.Scope) || !saveOrders.Add((section.Phase, section.Order)))
                    throw new InvalidDataException($"Invalid or duplicate save-section order: {entry.Id}");
            }
        }
    }

    private static bool IsNode(Type type)
    {
        for (Type? current = type; current is not null; current = current.BaseType)
            if (current.FullName == "Godot.Node") return true;
        return false;
    }
    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
    private static void ValidatePose(MapId map, Pose pose)
    {
        if (!float.IsFinite(pose.Position.X) || !float.IsFinite(pose.Position.Y) || !float.IsFinite(pose.Position.Z) ||
            !float.IsFinite(pose.YawDeg) || !float.IsFinite(pose.PitchDeg) ||
            pose.FovDeg is { } fov && (!float.IsFinite(fov) || fov <= 0f || fov >= 180f))
            throw new InvalidDataException($"Invalid pose in map: {map}");
    }
}
