using System.Reflection;
using Campus.Contracts;

namespace Campus.Pipeline.Map;

public sealed record GenerationRegistration(string Kind, string Id, string Owner, string? Map, Type Type, bool IsStub);

/// <summary>Existing generator registration and map selection rules, shared by the engine and offline hosts.</summary>
public static class RegistrationDiscovery
{
    public static IReadOnlyList<GenerationRegistration> Discover(IEnumerable<Assembly> assemblies)
    {
        var entries = new List<GenerationRegistration>();
        foreach (var type in assemblies.Distinct().OrderBy(assembly => assembly.FullName, StringComparer.Ordinal)
            .SelectMany(assembly => assembly.GetTypes()).Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters)
            .OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            if (type.GetCustomAttribute<ContentModuleAttribute>() is { } content)
                Add(type, "content", content.Id, content.Owner, content.Map);
            if (type.GetCustomAttribute<PartAttribute>() is { } part)
                Add(type, "part", part.Id, part.Owner, null);
            if (type.GetCustomAttribute<InteriorProviderAttribute>() is { } interior)
                Add(type, "interior", interior.Id, interior.Owner, interior.Map);
        }
        return Array.AsReadOnly(Select(entries, entry => (entry.Kind, entry.Map, entry.Id), entry => entry.IsStub)
            .OrderBy(entry => entry.Kind + ":" + entry.Id + ":" + entry.Map, StringComparer.Ordinal).ToArray());

        void Add(Type type, string kind, string id, string owner, string? map)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(owner))
                throw new InvalidOperationException($"Registration has empty identity: {type.FullName}");
            if (string.IsNullOrEmpty(map)) map = type.Assembly.GetCustomAttribute<MapScopeAttribute>()?.Map;
            var stub = type.Namespace?.StartsWith("Campus.Gen.Parts.Stubs", StringComparison.Ordinal) == true ||
                type.Namespace?.StartsWith("Campus.Kernel.Stubs", StringComparison.Ordinal) == true;
            entries.Add(new(kind, id, owner, map, type, stub));
        }
    }

    public static IReadOnlyList<T> Select<T, TKey>(IEnumerable<T> entries, Func<T, TKey> identity, Func<T, bool> isStub)
    {
        var selected = new List<T>();
        foreach (var group in entries.GroupBy(identity))
        {
            var real = group.Where(entry => !isStub(entry)).ToArray();
            var actual = real.Length > 0 ? real : group.ToArray();
            if (actual.Length != 1) throw new InvalidOperationException($"Duplicate registration: {group.Key}");
            selected.Add(actual[0]);
        }
        return selected.AsReadOnly();
    }

    public static IReadOnlyList<T> ForMap<T>(IEnumerable<T> entries, Func<T, string?> scope, string map) =>
        entries.Where(entry => string.IsNullOrEmpty(scope(entry)) || scope(entry) == map).ToArray();
}
