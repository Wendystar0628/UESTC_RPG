using System.Reflection;
using Campus.Contracts;

namespace Campus.Pipeline.Map;

public sealed record Registration(RegistrationKind Kind, string Id, string Owner, string? Map,
    Type Type, bool IsStub, RegistrationLifetime Lifetime, Type? Contract, string? Scope);

/// <summary>One declaration discovery, fallback selection, ordering and constructor path for all hosts.</summary>
public static class RegistrationDiscovery
{
    public static IReadOnlyList<Registration> Discover(IEnumerable<Assembly> assemblies)
    {
        var entries = new List<Registration>();
        foreach (var type in assemblies.Distinct().OrderBy(assembly => assembly.FullName, StringComparer.Ordinal)
            .SelectMany(assembly => assembly.GetTypes()).Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters)
            .OrderBy(type => type.FullName, StringComparer.Ordinal))
        foreach (var attribute in type.GetCustomAttributes<RegistrationAttribute>(false))
        {
            if (string.IsNullOrWhiteSpace(attribute.Id) || string.IsNullOrWhiteSpace(attribute.Owner) ||
                !Enum.IsDefined(attribute.Kind) || !Enum.IsDefined(attribute.Lifetime))
                throw new InvalidOperationException($"Invalid registration: {type.FullName}");
            if (attribute.Contract is { } contract && !contract.IsAssignableFrom(type))
                throw new InvalidOperationException($"Invalid service binding: {type.FullName}");
            var valid = attribute.Kind switch
            {
                RegistrationKind.Content => typeof(IContentModule).IsAssignableFrom(type),
                RegistrationKind.Part => typeof(IPartDef).IsAssignableFrom(type),
                RegistrationKind.Interior => typeof(IInteriorProvider).IsAssignableFrom(type),
                RegistrationKind.Ambient => typeof(Campus.Contracts.Audio.IAmbientEmitterSource).IsAssignableFrom(type),
                RegistrationKind.Bake => typeof(Campus.Contracts.Bake.IBakeProducer).IsAssignableFrom(type),
                RegistrationKind.Service or RegistrationKind.Node or RegistrationKind.Medium or RegistrationKind.Save => true,
                _ => throw new InvalidOperationException($"Unknown registration kind: {attribute.Kind}")
            };
            if (!valid) throw new InvalidOperationException($"Registration contract mismatch: {type.FullName}");
            var map = string.IsNullOrEmpty(attribute.Map) ? type.Assembly.GetCustomAttribute<MapScopeAttribute>()?.Map : attribute.Map;
            entries.Add(new(attribute.Kind, attribute.Id, attribute.Owner, map, type, attribute.IsStub,
                attribute.Lifetime, attribute.Contract, attribute.IdentityScope));
        }
        var selected = new List<Registration>();
        foreach (var group in entries.GroupBy(entry => (entry.Kind, entry.Map, entry.Id, entry.Scope)))
        {
            var real = group.Where(entry => !entry.IsStub).ToArray();
            var chosen = real.Length > 0 ? real : group.ToArray();
            if (chosen.Length != 1) throw new InvalidOperationException($"Duplicate registration: {group.Key}");
            selected.Add(chosen[0]);
        }
        foreach (var group in selected.GroupBy(entry => entry.Type))
            if (group.Select(entry => entry.Lifetime).Distinct().Count() != 1)
                throw new InvalidOperationException($"Conflicting lifetimes: {group.Key.FullName}");
        return Array.AsReadOnly(Order(selected).ToArray());
    }

    public static IEnumerable<Registration> Order(IEnumerable<Registration> entries) => entries
        .OrderBy(entry => entry.Id, StringComparer.Ordinal).ThenBy(entry => entry.Kind)
        .ThenBy(entry => entry.Map, StringComparer.Ordinal).ThenBy(entry => entry.Scope, StringComparer.Ordinal);

    public static IReadOnlyList<Registration> ForMap(IEnumerable<Registration> entries, string map) =>
        entries.Where(entry => entry.Map is null || entry.Map == map).ToArray();

    public static IReadOnlyList<Registration> Query<T>(IEnumerable<Registration> entries, RegistrationKind kind, MapId? map = null)
        where T : class => Order(entries.Where(entry => entry.Kind == kind && typeof(T).IsAssignableFrom(entry.Contract ?? entry.Type) &&
            (map is null || entry.Map is null || entry.Map == map.Value.Value))).ToArray();

    public static object Construct(Type type, Func<ParameterInfo, object?> resolve)
    {
        var constructor = type.GetConstructors().OrderByDescending(item => item.GetParameters().Length)
            .ThenBy(item => item.ToString(), StringComparer.Ordinal).FirstOrDefault()
            ?? throw new InvalidOperationException($"No public constructor: {type.FullName}");
        return constructor.Invoke(constructor.GetParameters().Select(resolve).ToArray());
    }
}
