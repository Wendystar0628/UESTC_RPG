using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Campus.Contracts;

public sealed record Registration(RegistrationKind Kind, [property: DataId] string Id, string Owner, [property: DataId] string? Map,
    Type Type, bool IsStub, RegistrationLifetime Lifetime, Type? Contract, string? Scope);

/// <summary>Shared declaration discovery, real-over-stub selection, ordering and constructor path for all hosts.</summary>
public static class RegistrationDiscovery
{
    public static IReadOnlyList<Registration> Discover(IEnumerable<Type> types, Func<Type, string> ownerOf)
    {
        ArgumentNullException.ThrowIfNull(ownerOf);
        var entries = new List<Registration>();
        foreach (var type in types.Distinct().Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ThenBy(type => type.Assembly.FullName, StringComparer.Ordinal))
        foreach (var attribute in type.GetCustomAttributes<RegistrationAttribute>(false))
        {
            var owner = ownerOf(type);
            if (string.IsNullOrWhiteSpace(attribute.Id) || string.IsNullOrWhiteSpace(owner) ||
                !Enum.IsDefined(attribute.Kind) || !Enum.IsDefined(attribute.Lifetime))
                throw new InvalidOperationException($"Invalid registration: {type.FullName}");
            if (attribute.Contract is { } contract && !contract.IsAssignableFrom(type))
                throw new InvalidOperationException($"Invalid service binding: {type.FullName}");
            if (!MatchesKind(type, attribute.Kind)) throw new InvalidOperationException($"Registration contract mismatch: {type.FullName}");
            var map = string.IsNullOrEmpty(attribute.Map) ? type.Assembly.GetCustomAttribute<MapScopeAttribute>()?.Map : attribute.Map;
            entries.Add(new(attribute.Kind, attribute.Id, owner, map, type, attribute.IsStub,
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

    public static bool MatchesKind(Type type, RegistrationKind kind) => kind switch
    {
        RegistrationKind.Service => true,
        RegistrationKind.Content => typeof(IContentModule).IsAssignableFrom(type),
        RegistrationKind.Part => typeof(IPartDef).IsAssignableFrom(type),
        RegistrationKind.Interior => typeof(IInteriorProvider).IsAssignableFrom(type) || typeof(IInteriorProviderFactory).IsAssignableFrom(type),
        RegistrationKind.Ambient => typeof(Campus.Contracts.Audio.IAmbientEmitterSource).IsAssignableFrom(type),
        RegistrationKind.Bake => typeof(Campus.Contracts.Bake.IBakeProducer).IsAssignableFrom(type),
        // Runtime-only contracts are checked by metadata so discovery remains pure .NET.
        RegistrationKind.Medium => type.GetInterfaces().Any(contract => contract.FullName == "Campus.Contracts.IMediumPresenter"),
        RegistrationKind.Save => type.GetInterfaces().Any(contract => contract.FullName == "Campus.Contracts.Save.ISaveSection"),
        RegistrationKind.Node => BaseTypes(type).Any(current => current.FullName == "Godot.Node"),
        _ => throw new InvalidOperationException($"Unknown registration kind: {kind}")
    };

    private static IEnumerable<Type> BaseTypes(Type type)
    {
        for (Type? current = type; current is not null; current = current.BaseType) yield return current;
    }

    public static Registration ResolveInterior(IEnumerable<Registration> entries, string providerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        var matches = entries.Where(entry => entry.Kind == RegistrationKind.Interior &&
            (typeof(IInteriorProvider).IsAssignableFrom(entry.Type) && entry.Id == providerId ||
             typeof(IInteriorProviderFactory).IsAssignableFrom(entry.Type) && providerId.StartsWith(entry.Id, StringComparison.Ordinal))).ToArray();
        if (matches.Length != 1)
            throw new InvalidDataException($"Interior '{providerId}' needs exactly one provider or registered factory; found {matches.Length}.");
        return matches[0];
    }

    public static ConstructorInfo SelectConstructor(Type type) => type.GetConstructors()
        .OrderByDescending(item => item.GetParameters().Length).ThenBy(item => item.ToString(), StringComparer.Ordinal).FirstOrDefault()
        ?? throw new InvalidOperationException($"No public constructor: {type.FullName}");

    public static object Construct(Type type, Func<ParameterInfo, object?> resolve)
    {
        var constructor = SelectConstructor(type);
        var arguments = constructor.GetParameters().Select(resolve).ToArray();
        try { return constructor.Invoke(arguments); }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
}
