using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Save;

namespace Campus.Kernel.Registry;

public sealed record Registration(string Kind, string Id, string Owner, string? Map, Type Type, bool IsStub)
{
    public SaveScope? Scope { get; init; }
}

/// <summary>Stable discovery with explicit fallback replacement and constructor injection.</summary>
public sealed class ServiceRegistry
{
    private readonly Dictionary<Type, Type> _serviceTypes = [];
    private readonly Dictionary<Type, object> _instances = [];
    private readonly HashSet<Type> _constructing = [];
    private readonly List<Registration> _entries = [];
    public IReadOnlyList<Registration> Entries => _entries;

    public void Add<T>(T instance) where T : class => _instances[typeof(T)] = instance;
    public T Get<T>() where T : class => (T)Get(typeof(T));
    public object Get(Type service)
    {
        if (_instances.TryGetValue(service, out var existing)) return existing;
        if (!_serviceTypes.TryGetValue(service, out var implementation))
            throw new InvalidOperationException($"Service is not registered: {service.FullName}");
        var instance = Create(implementation);
        _instances[service] = instance;
        return instance;
    }

    public object Create(Type implementation)
    {
        if (_instances.TryGetValue(implementation, out var existing)) return existing;
        if (!_constructing.Add(implementation))
            throw new InvalidOperationException($"Circular service dependency: {implementation.FullName}");
        try
        {
            var constructor = implementation.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault()
                ?? throw new InvalidOperationException($"No public constructor: {implementation.FullName}");
            var args = constructor.GetParameters().Select(p =>
                _instances.ContainsKey(p.ParameterType) || _serviceTypes.ContainsKey(p.ParameterType)
                    ? Get(p.ParameterType)
                    : p.HasDefaultValue ? p.DefaultValue
                    : throw new InvalidOperationException($"Unresolved {p.ParameterType.Name} for {implementation.Name}")).ToArray();
            var instance = constructor.Invoke(args);
            _instances[implementation] = instance;
            return instance;
        }
        finally { _constructing.Remove(implementation); }
    }

    /// <summary>Forget a constructed instance and every alias to it. Its owner must dispose/free it first.</summary>
    public void Release(Type implementation)
    {
        ArgumentNullException.ThrowIfNull(implementation);
        if (!_instances.TryGetValue(implementation, out var instance)) return;
        var aliases = _instances.Where(pair => ReferenceEquals(pair.Value, instance)).Select(pair => pair.Key).ToArray();
        foreach (var alias in aliases) _instances.Remove(alias);
    }

    public void Discover(IEnumerable<Assembly> assemblies)
    {
        var types = assemblies.Distinct().OrderBy(a => a.FullName, StringComparer.Ordinal)
            .SelectMany(a => a.GetTypes()).Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters)
            .OrderBy(t => t.FullName, StringComparer.Ordinal).ToArray();
        var services = types.SelectMany(t => t.GetCustomAttributes<KernelServiceAttribute>()
            .Select(a => (Type: t, Attribute: a))).GroupBy(x => x.Attribute.Service);
        foreach (var group in services.OrderBy(g => g.Key.FullName, StringComparer.Ordinal))
        {
            var actual = group.Where(x => !x.Attribute.IsStub).ToArray();
            var chosen = actual.Length > 0 ? actual : group.ToArray();
            if (chosen.Length != 1) throw new InvalidOperationException($"Duplicate service: {group.Key.FullName}");
            var item = chosen[0];
            if (!group.Key.IsAssignableFrom(item.Type)) throw new InvalidOperationException($"Invalid service binding: {item.Type.FullName}");
            _serviceTypes.Add(group.Key, item.Type);
            _entries.Add(new("service", group.Key.FullName!, item.Attribute.Owner, null, item.Type, item.Attribute.IsStub));
        }
        foreach (var type in types)
        {
            AddAttribute(type, type.GetCustomAttribute<ContentModuleAttribute>(), "content");
            AddAttribute(type, type.GetCustomAttribute<PartAttribute>(), "part");
            AddAttribute(type, type.GetCustomAttribute<InteriorProviderAttribute>(), "interior");
            AddAttribute(type, type.GetCustomAttribute<Campus.Contracts.Audio.AmbientEmitterAttribute>(), "ambient");
            AddAttribute(type, type.GetCustomAttribute<MediumPresenterAttribute>(), "medium");
            AddAttribute(type, type.GetCustomAttribute<KernelNodeAttribute>(), "node");
            AddAttribute(type, type.GetCustomAttribute<BakeProducerAttribute>(), "bake");
            foreach (var section in type.GetCustomAttributes<SaveSectionAttribute>())
                AddAttribute(type, section, "save");
        }
        foreach (var group in _entries.Where(e => e.Kind != "service").GroupBy(e => (e.Kind, e.Map, e.Id, e.Scope)).ToArray())
        {
            var real = group.Where(e => !e.IsStub).ToArray();
            var selected = real.Length > 0 ? real : group.ToArray();
            if (selected.Length > 1) throw new InvalidOperationException($"Duplicate registration: {group.Key}");
            foreach (var dropped in group.Where(e => e != selected[0]).ToArray()) _entries.Remove(dropped);
        }
        _entries.Sort((a, b) => StringComparer.Ordinal.Compare(
            a.Kind + ":" + a.Id + ":" + a.Map + ":" + a.Scope,
            b.Kind + ":" + b.Id + ":" + b.Map + ":" + b.Scope));
    }

    private void AddAttribute(Type type, Attribute? attribute, string kind)
    {
        if (attribute is null) return;
        if (attribute is BakeProducerAttribute && !typeof(IBakeProducer).IsAssignableFrom(type))
            throw new InvalidOperationException($"Bake producer does not implement IBakeProducer: {type.FullName}");
        var metadata = attribute.GetType();
        var id = metadata.GetProperty("Id")?.GetValue(attribute) as string
            ?? throw new InvalidOperationException($"Registration has no Id: {type.FullName}");
        var owner = metadata.GetProperty("Owner")?.GetValue(attribute) as string ?? "";
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(owner))
            throw new InvalidOperationException($"Registration has empty identity: {type.FullName}");
        var map = metadata.GetProperty("Map")?.GetValue(attribute) as string;
        if (string.IsNullOrEmpty(map)) map = type.Assembly.GetCustomAttribute<MapScopeAttribute>()?.Map;
        var stub = metadata.GetProperty("IsStub")?.GetValue(attribute) is true ||
            type.Assembly.GetName().Name == "Campus.Gen.Parts.Stubs" ||
            type.Namespace?.StartsWith("Campus.Kernel.Stubs", StringComparison.Ordinal) == true;
        _entries.Add(new(kind, id, owner, map, type, stub)
        {
            Scope = attribute is SaveSectionAttribute section ? section.Scope : null
        });
    }

    public void InstantiateServices()
    {
        foreach (var service in _serviceTypes.Keys.OrderBy(t => t.FullName, StringComparer.Ordinal)) Get(service);
    }

    public IReadOnlyList<Registration> ForMap(string map) => _entries
        .Where(e => string.IsNullOrEmpty(e.Map) || e.Map == map).ToArray();

    public void ValidateMap(string map)
    {
        foreach (var entry in ForMap(map).Where(e => e.Kind == "content"))
        {
            var module = (IContentModule)Create(entry.Type);
            if (string.IsNullOrEmpty(entry.Map) && module.Claims.Any(c => c is FeatureClaim))
                throw new InvalidOperationException($"Global module cannot claim a map feature: {entry.Id}");
        }
    }
}
