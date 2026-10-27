using Campus.Contracts;
using Campus.Contracts.Maps;
using Godot;

namespace Campus.Kernel.Registry;

/// <summary>Declaration-owned global/map/transient instances; supplied instances remain owned by their caller.</summary>
public sealed class ServiceRegistry
{
    private readonly Dictionary<Type, object> _supplied = [];
    private readonly Dictionary<(Type Type, MapId? Map), object> _instances = [];
    private readonly HashSet<(Type Type, MapId? Map)> _constructing = [];
    private readonly Dictionary<(Type Factory, string Id, MapId Map), IInteriorProvider> _interiors = [];
    private readonly List<(object Instance, MapId? Map)> _created = [];
    private IReadOnlyList<Registration> _entries = [];
    private bool _closing;
    private bool _releasingOwned;
    private bool _disposed;
    private Task? _mapPreparationSeal;
    private Task? _shutdown;
    private readonly HashSet<MapId> _releasingMaps = [];
    public IReadOnlyList<Registration> Entries => _entries;
    public IReadOnlyList<object> Created => _created.Select(item => item.Instance).ToArray();

    public void Add<T>(T instance) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_closing) throw new InvalidOperationException("Registry is shutting down.");
        _supplied[typeof(T)] = instance;
    }
    public T Get<T>() where T : class => (T)Get(typeof(T));
    public object Get(Type service, MapId? map = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_supplied.TryGetValue(service, out var supplied)) return supplied;
        var entry = ForScope(map).SingleOrDefault(item => item.Kind == RegistrationKind.Service && item.Contract == service)
            ?? throw new InvalidOperationException($"Service is not registered: {service.FullName}");
        return Create(entry.Type, map);
    }

    public object Create(Type implementation, MapId? map = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (map is { } requested && string.IsNullOrWhiteSpace(requested.Value)) throw new ArgumentException("A map ID is required.", nameof(map));
        if (_supplied.TryGetValue(implementation, out var supplied)) return supplied;
        var entries = ForScope(map).Where(entry => entry.Type == implementation).ToArray();
        if (entries.Length == 0) throw new InvalidOperationException($"Type is not registered: {implementation.FullName}");
        // A supplied service binding replaces the implementation for all of its registrations.
        foreach (var entry in entries)
            if (entry.Contract is { } contract && _supplied.TryGetValue(contract, out supplied)) return supplied;
        var lifetime = entries[0].Lifetime;
        MapId? scope = lifetime switch
        {
            RegistrationLifetime.Global => null,
            RegistrationLifetime.Map => map ?? throw new InvalidOperationException($"Map scope required: {implementation.FullName}"),
            RegistrationLifetime.Transient => map,
            _ => throw new InvalidOperationException($"Unknown lifetime: {lifetime}")
        };
        var key = (implementation, scope);
        if (lifetime != RegistrationLifetime.Transient && _instances.TryGetValue(key, out var existing)) return existing;
        // ExitTree may still resolve existing bindings, but teardown must never construct replacements.
        if (_closing) throw new InvalidOperationException($"Registry is shutting down: {implementation.FullName}");
        if (map is { } releasing && _releasingMaps.Contains(releasing)) throw new InvalidOperationException($"Map scope is releasing: {releasing.Value}");
        if (!_constructing.Add(key)) throw new InvalidOperationException($"Circular dependency: {implementation.FullName}");
        try
        {
            var instance = RegistrationDiscovery.Construct(implementation, parameter =>
            {
                if (typeof(IInteriorProvider).IsAssignableFrom(parameter.ParameterType))
                    throw new InvalidOperationException($"Interior providers are read through BuildContext: {implementation.FullName}");
                if (_supplied.ContainsKey(parameter.ParameterType) || ForScope(scope).Any(entry => entry.Contract == parameter.ParameterType))
                    return Get(parameter.ParameterType, scope);
                if (parameter.HasDefaultValue) return parameter.DefaultValue;
                throw new InvalidOperationException($"Unresolved {parameter.ParameterType.Name} for {implementation.Name}");
            });
            _created.Add((instance, scope));
            if (lifetime != RegistrationLifetime.Transient) _instances.Add(key, instance);
            return instance;
        }
        finally { _constructing.Remove(key); }
    }

    /// <summary>Factory products join the same map-owned cache and reverse-release sequence as every registration.</summary>
    public IInteriorProvider CreateInterior(Type factoryType, string providerId, MapId map)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var registration = RegistrationDiscovery.ResolveInterior(ForMap(map.Value), providerId);
        if (registration.Type != factoryType || !typeof(IInteriorProviderFactory).IsAssignableFrom(registration.Type))
            throw new InvalidOperationException($"Interior factory is not registered for '{providerId}'.");
        if (registration.Lifetime != RegistrationLifetime.Map)
            throw new InvalidOperationException("An interior factory must declare map lifetime.");
        var key = (factoryType, providerId, map);
        if (_interiors.TryGetValue(key, out var existing)) return existing;
        if (_closing || _releasingMaps.Contains(map)) throw new InvalidOperationException("Map scope is closing.");
        var factory = (IInteriorProviderFactory)Create(factoryType, map);
        var provider = factory.Create(providerId) ?? throw new InvalidOperationException("Interior factory returned null.");
        if (_created.Any(item => ReferenceEquals(item.Instance, provider)))
            throw new InvalidOperationException("Interior factory must create a fresh map-owned provider instance.");
        _interiors.Add(key, provider);
        _created.Add((provider, map));
        return provider;
    }

    public void Discover(IEnumerable<Type> types, Func<Type, string> ownerOf)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_closing) throw new InvalidOperationException("Registry is shutting down.");
        if (_entries.Count > 0 || _created.Count > 0) throw new InvalidOperationException("Registry discovery runs once.");
        _entries = RegistrationDiscovery.Discover(types, ownerOf);
    }

    public IReadOnlyList<Registration> ForMap(string map) => RegistrationDiscovery.ForMap(_entries, map);
    public IReadOnlyList<Registration> Query<T>(RegistrationKind kind, MapId? map = null) where T : class =>
        RegistrationDiscovery.Query<T>(_entries, kind, map);
    private IEnumerable<Registration> ForScope(MapId? map) => _entries.Where(entry => entry.Map is null || map is { } id && entry.Map == id.Value);

    public void InstantiateServices()
    {
        foreach (var entry in Query<object>(RegistrationKind.Service).Where(entry => entry.Map is null && entry.Lifetime == RegistrationLifetime.Global))
            Get(entry.Contract!);
    }

    public IReadOnlyList<T> RuntimeInstances<T>(MapId? map = null) where T : class => ForScope(map)
        .Where(entry => entry.Kind is RegistrationKind.Node or RegistrationKind.Service or RegistrationKind.Save)
        .Where(entry => typeof(T).IsAssignableFrom(entry.Type))
        // A node's lifecycle identity is its node ID, even when service aliases sort before it.
        .GroupBy(entry => entry.Type)
        .Select(group => group.OrderBy(entry => entry.Kind == RegistrationKind.Node ? 0 : 1)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal).First())
        .OrderBy(entry => entry.Id, StringComparer.Ordinal)
        .Select(entry => entry.Kind == RegistrationKind.Service ? Get(entry.Contract!, map) : Create(entry.Type, map))
        .OfType<T>().Distinct((IEqualityComparer<T>)ReferenceEqualityComparer.Instance).ToArray();

    public IReadOnlyList<T> CreatedInstances<T>(MapId? map = null) where T : class => _created
        .Where(item => item.Map is null || item.Map == map).Select(item => item.Instance)
        .OfType<T>().ToArray();

    public async Task ReleaseMapAsync(MapId map)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_releasingOwned) throw new InvalidOperationException("Registry owned-object release has started.");
        if (!_releasingMaps.Add(map)) throw new InvalidOperationException($"Map scope is already releasing: {map.Value}");
        try { await ReleaseAsync(item => item.Map == map); }
        finally { _releasingMaps.Remove(map); }
    }

    /// <summary>Seal registration and construction, stop owned map producers, then observe their submitted data preparation.</summary>
    public Task SealMapPreparationAsync()
    {
        _closing = true;
        return _mapPreparationSeal ??= SealMapPreparation();
    }

    private async Task SealMapPreparation()
    {
        // Supplied bindings belong to their caller; teardown never resolves or constructs an absent service.
        var maps = CreatedInstances<IMapService>().SingleOrDefault();
        var lifetime = CreatedInstances<IMapLifetime>().SingleOrDefault();
        var failures = new List<Exception>();
        try { maps?.StopNewWork(); } catch (Exception error) { failures.Add(error); }
        try { lifetime?.StopNewWork(); } catch (Exception error) { failures.Add(error); }
        if (maps is not null)
            try { await maps.DrainAsync(); } catch (Exception error) { failures.Add(error); }
        if (failures.Count > 0) throw new AggregateException("Map preparation stop failed.", failures);
    }

    /// <summary>Stop map/background work, exit nodes with bindings intact, then release owned objects and scene children.</summary>
    public Task ShutdownAsync(Node? scene = null, Action<string>? trace = null)
    {
        return _shutdown ??= Shutdown(scene, trace);
    }

    private async Task Shutdown(Node? scene, Action<string>? trace)
    {
        _closing = true;
        var failures = new List<Exception>();
        trace?.Invoke("shutdown-map");
        try { await SealMapPreparationAsync(); } catch (Exception error) { failures.Add(error); }
        try
        {
            var lifetime = CreatedInstances<IMapLifetime>().SingleOrDefault();
            if (lifetime is not null) await lifetime.ShutdownAsync();
        }
        catch (Exception error) { failures.Add(error); }
        var streamsDrained = true;
        foreach (var stream in CreatedInstances<IMapStreamState>())
            try { streamsDrained &= stream.IsUnloaded; }
            catch (Exception error) { failures.Add(error); streamsDrained = false; }
        if (!streamsDrained)
        {
            if (failures.Count > 0) throw new AggregateException("Map resources remain owned after shutdown failed.", failures);
            throw new InvalidOperationException("The map stream must finish draining before registry teardown.");
        }
        // Runtime drain owns map release; global teardown must not reenter that phase.
        _releasingOwned = true;
        var participants = _created.AsEnumerable().Reverse().Select(item => item.Instance).OfType<IRegistryExitParticipant>().ToArray();
        foreach (var participant in participants)
            try { participant.PrepareForExit(); } catch (Exception error) { failures.Add(error); }
        foreach (var participant in participants)
            try { await participant.DrainForExitAsync(); } catch (Exception error) { failures.Add(error); }
        trace?.Invoke("shutdown-nodes");
        Node[] roots = [];
        if (scene is not null)
        {
            var children = scene.GetChildren(includeInternal: true);
            using (var nativeChildren = (Godot.Collections.Array)children)
                roots = children.AsEnumerable().Reverse().ToArray();
        }
        var sceneOwned = roots.Where(node => !_created.Any(item => ReferenceEquals(item.Instance, node))).ToArray();
        // Scene-owned containers and their descendants must exit before any service binding is removed.
        ExitNodes(roots, failures);
        try { await ReleaseAsync(_ => true, trace); } catch (Exception error) { failures.Add(error); }
        foreach (var node in sceneOwned)
            try { FreeNode(node, trace); } catch (Exception error) { failures.Add(error); }
        _supplied.Clear();
        _disposed = true;
        if (failures.Count > 0) throw new AggregateException("Registry shutdown failed.", failures);
    }

    private async Task ReleaseAsync(Func<(object Instance, MapId? Map), bool> select, Action<string>? trace = null)
    {
        var failures = new List<Exception>();
        var owned = _created.Where(select).Reverse().ToArray();
        ExitNodes(owned.Select(item => item.Instance).OfType<Node>(), failures);
        foreach (var item in owned)
        {
            Forget(item.Instance);
            try
            {
                if (item.Instance is Node node) FreeNode(node, trace);
                else if (item.Instance is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync();
                else if (item.Instance is IDisposable disposable) disposable.Dispose();
            }
            catch (Exception error) { failures.Add(error); }
        }
        if (failures.Count > 0) throw new AggregateException("Registered object release failed.", failures);
    }

    private void Forget(object instance)
    {
        _created.RemoveAll(item => ReferenceEquals(item.Instance, instance));
        foreach (var key in _interiors.Where(pair => ReferenceEquals(pair.Value, instance)).Select(pair => pair.Key).ToArray()) _interiors.Remove(key);
        foreach (var alias in _supplied.Where(pair => ReferenceEquals(pair.Value, instance)).Select(pair => pair.Key).ToArray()) _supplied.Remove(alias);
        foreach (var key in _instances.Where(pair => ReferenceEquals(pair.Value, instance)).Select(pair => pair.Key).ToArray()) _instances.Remove(key);
    }

    private static void FreeNode(Node node, Action<string>? trace)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        node.SetProcess(false); node.SetPhysicsProcess(false);
        node.GetParent()?.RemoveChild(node);
        trace?.Invoke("node:" + node.GetType().Name);
        node.Free();
    }

    private static void ExitNodes(IEnumerable<Node> nodes, [FailureHandler("ENG-ARCH")] List<Exception> failures)
    {
        foreach (var node in nodes)
            try
            {
                if (!GodotObject.IsInstanceValid(node)) continue;
                node.SetProcess(false); node.SetPhysicsProcess(false);
                node.GetParent()?.RemoveChild(node);
            }
            catch (Exception error) { failures.Add(error); }
    }
}
