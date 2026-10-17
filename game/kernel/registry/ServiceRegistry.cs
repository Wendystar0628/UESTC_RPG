using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Save;
using Campus.Pipeline.Map;

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

    /// <summary>
    /// Creates one fresh, map-owned instance of an interior provider selected by ForMap.
    /// Call on the main thread before worker publication; configure once, then keep the instance for that map's jobs.
    /// This is not a general transient-service API and never adds the provider to the global instance cache.
    /// </summary>
    public IInteriorProvider CreateInteriorProvider(Type implementation, MapId map)
    {
        ArgumentNullException.ThrowIfNull(implementation);
        if (string.IsNullOrWhiteSpace(map.Value)) throw new ArgumentException("A map ID is required.", nameof(map));
        if (!implementation.IsClass || implementation.IsAbstract || implementation.ContainsGenericParameters ||
            !typeof(IInteriorProvider).IsAssignableFrom(implementation) ||
            !ForMap(map.Value).Any(entry => entry.Kind == "interior" && entry.Type == implementation))
            throw new InvalidOperationException($"Interior provider {implementation.FullName} is not registered for map {map.Value}.");
        if (typeof(Godot.GodotObject).IsAssignableFrom(implementation) ||
            implementation.IsDefined(typeof(KernelNodeAttribute), false) ||
            implementation.IsDefined(typeof(KernelServiceAttribute), false))
            throw new InvalidOperationException($"Map-owned interior creation cannot construct an engine node or service: {implementation.FullName}.");
        if (!_constructing.Add(implementation))
            throw new InvalidOperationException($"Circular interior provider dependency: {implementation.FullName}");
        try
        {
            var constructor = implementation.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault()
                ?? throw new InvalidOperationException($"No public constructor: {implementation.FullName}");
            var args = constructor.GetParameters().Select(parameter =>
            {
                if (typeof(IInteriorProvider).IsAssignableFrom(parameter.ParameterType))
                    throw new InvalidOperationException($"Interior provider {implementation.Name} must read other providers through BuildContext.InteriorProviders, not constructor injection.");
                var dependency = _instances.ContainsKey(parameter.ParameterType) || _serviceTypes.ContainsKey(parameter.ParameterType)
                    ? Get(parameter.ParameterType)
                    : parameter.HasDefaultValue ? parameter.DefaultValue
                    : throw new InvalidOperationException($"Unresolved {parameter.ParameterType.Name} for {implementation.Name}");
                if (dependency is IInteriorProvider)
                    throw new InvalidOperationException($"Interior provider {implementation.Name} cannot capture a globally cached interior dependency.");
                return dependency;
            }).ToArray();
            return (IInteriorProvider)constructor.Invoke(args);
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
        var capturedAssemblies = assemblies.Distinct().ToArray();
        var types = capturedAssemblies.OrderBy(a => a.FullName, StringComparer.Ordinal)
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
        foreach (var entry in RegistrationDiscovery.Discover(capturedAssemblies))
            _entries.Add(new(entry.Kind, entry.Id, entry.Owner, entry.Map, entry.Type, entry.IsStub));
        foreach (var type in types)
        {
            AddAttribute(type, type.GetCustomAttribute<Campus.Contracts.Audio.AmbientEmitterAttribute>(), "ambient");
            AddAttribute(type, type.GetCustomAttribute<MediumPresenterAttribute>(), "medium");
            AddAttribute(type, type.GetCustomAttribute<KernelNodeAttribute>(), "node");
            AddAttribute(type, type.GetCustomAttribute<BakeProducerAttribute>(), "bake");
            foreach (var section in type.GetCustomAttributes<SaveSectionAttribute>())
                AddAttribute(type, section, "save");
        }
        var selected = RegistrationDiscovery.Select(_entries.Where(entry => entry.Kind != "service"),
            entry => (entry.Kind, entry.Map, entry.Id, entry.Scope), entry => entry.IsStub);
        _entries.RemoveAll(entry => entry.Kind != "service");
        _entries.AddRange(selected);
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

    public IReadOnlyList<Registration> ForMap(string map) => RegistrationDiscovery.ForMap(_entries, entry => entry.Map, map);

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
