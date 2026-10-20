using System.Text;
using System.Reflection;
using Campus.Contracts;
using Campus.Gen.World;
using Campus.Gen.BakeInput;

namespace Campus.Tests;

/// <summary>Real authored maps through the production catalogue parser and input capture.</summary>
internal static class RealMapFixture
{
    internal static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> WindowInputs(MapId map,
        WindowNumberingSnapshot? previous = null) => new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            [WindowNumberingRegistry.FileName] = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
                previous is null ? new WindowNumberingRegistry(map, 0, []) : WindowNumberingRegistry.FromSnapshot(previous), ContractJson.Options)
        };

    public static MapCatalogEntry Entry(MapId? map = null)
    {
        var index = MapCatalog.ReadIndex(File.ReadAllBytes(RepositoryFixture.FilePath("game", "data", "maps", "index.json")));
        var id = map ?? index.DefaultMap;
        var entry = index.Maps.SingleOrDefault(entry => entry.Id == id)
            ?? throw new ArgumentException("The requested map is not in the authored catalogue.", nameof(map));
        var source = new DiskWorldInputSource(ResourceDirectory(entry.Dir));
        return MapCatalog.ReadEntry(entry, Required(source, "map.json").Span,
            Required(source, "world/geo.json"), (source.Read("waypoints.json") ?? ReadOnlyMemory<byte>.Empty).Span);
    }

    public static string DataDirectory(MapId? map = null) => ResourceDirectory(Entry(map).DataRoot);

    public static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ReadFiles(MapId? map = null)
    {
        var entry = Entry(map);
        return WorldInputFiles.ReadLoadInput(entry.Descriptor, new DiskWorldInputSource(ResourceDirectory(entry.DataRoot)));
    }

    /// <summary>Mutable text inputs for tests that intentionally edit the captured production data.</summary>
    public static Dictionary<string, string> ReadTextFiles(MapId? map = null) =>
        ReadFiles(map).ToDictionary(pair => pair.Key, pair => Encoding.UTF8.GetString(pair.Value.Span), StringComparer.Ordinal);

    private static readonly Lazy<WorldModelLoadResult> DefaultLoaded = new(() => LoadFresh(null, null));

    /// <summary>Default map with default options is loaded once per process and shared read-only; anything else loads fresh.</summary>
    public static WorldModelLoadResult Load(MapId? map = null, WorldLoadOptions? options = null) =>
        map is null && options is null ? DefaultLoaded.Value : LoadFresh(map, options);

    private static WorldModelLoadResult LoadFresh(MapId? map, WorldLoadOptions? options)
    {
        var entry = Entry(map);
        var files = WorldInputFiles.ReadLoadInput(entry.Descriptor, new DiskWorldInputSource(ResourceDirectory(entry.DataRoot)));
        return LoadPrepared(files, entry.Descriptor.Id, options);
    }

    /// <summary>Load intentional edits to real map inputs with the registered production claims.</summary>
    public static WorldModelLoadResult LoadPrepared(IReadOnlyDictionary<string, string> files,
        MapId? map = null, WorldLoadOptions? options = null) =>
        LoadPrepared(files.ToDictionary(pair => pair.Key,
            pair => (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes(pair.Value), StringComparer.Ordinal), map, options);

    public static WorldModelLoadResult LoadPrepared(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
        MapId? map = null, WorldLoadOptions? options = null)
    {
        files = BuildInputSnapshot.Freeze(files, "fixture").Entries;
        var entry = MapCatalog.ReadEntry(Entry(map), files);
        var id = entry.Descriptor.Id;
        options ??= new();
        var registrations = RegistrationDiscovery.Discover(
            new[] { "Campus.Gen", "Campus.Gen.Maps.Qingshuihe" }.Select(Assembly.Load), _ => "fixture");
        var claims = new MapClaimSnapshot(RegistrationDiscovery.Query<IContentModule>(registrations, RegistrationKind.Content, id)
            .Select(entry => new MapClaimSource(entry.Id, entry.Owner, entry.Map is null,
                ((IContentModule)RegistrationDiscovery.Construct(entry.Type, parameter => parameter.HasDefaultValue ? parameter.DefaultValue : throw new InvalidOperationException("Fixture registration has an unresolved dependency."))).Claims)));
        var loaded = WorldModelLoader.LoadWorld(new(entry.Descriptor), files, options);
        _ = claims.Build(loaded.World);
        if (files.TryGetValue("interiors.json", out var catalog))
        {
            var selected = InteriorAssembly.ResolveRegistrations(catalog, RegistrationDiscovery.ForMap(registrations,id.Value));
            var providers = selected.ToDictionary(binding => binding.ProviderId, binding =>
            {
                var instance = RegistrationDiscovery.Construct(binding.Registration.Type, parameter => parameter.HasDefaultValue
                    ? parameter.DefaultValue : throw new InvalidOperationException("Fixture interior registration has an unresolved dependency."));
                return instance is IInteriorProviderFactory factory ? factory.Create(binding.ProviderId) : (IInteriorProvider)instance;
            }, StringComparer.Ordinal);
            var bindings = InteriorAssembly.ConfigureForWorld(catalog,providers,loaded.World);
            var context = new BuildContext { World=loaded.World, Map=entry.Descriptor, WorldSeed=entry.Descriptor.Seed,
                Catalog=Campus.Contracts.Tests.PaletteFixture.Catalog, InteriorProviders=providers, SourceInputs=files,
                CodeHash="fixture-interior-preparation", WindowNumbering=new(entry.Descriptor.Id,new string('0',64),[]) };
            var layouts = bindings.SelectMany(binding => binding.FeatureIds.Select(feature =>
                InteriorAssembly.ValidatedLayout(providers[binding.ProviderId],binding,context,feature))).ToArray();
            loaded = loaded with { World = WorldModelSnapshot.WithInteriors(loaded.World, InteriorAssembly.IndexEntries(layouts,bindings,providers)) };
        }
        MapCatalog.ValidateDestinations(entry.Descriptor, files, loaded.World);
        return loaded;
    }

    private static string ResourceDirectory(string resource)
    {
        MapCatalog.ValidateRoot(resource);
        return RepositoryFixture.FilePath("game", resource["res://".Length..].Replace('/', Path.DirectorySeparatorChar));
    }

    private static ReadOnlyMemory<byte> Required(DiskWorldInputSource source, string path) =>
        source.Read(path) is { IsEmpty: false } bytes ? bytes : throw new IOException($"Missing or empty map resource: {path}.");

    private sealed class DiskWorldInputSource(string directory) : IWorldInputSource
    {
        public ReadOnlyMemory<byte>? Read(string relativePath)
        {
            try { return File.ReadAllBytes(Path.Combine(directory, relativePath)); }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }

        public IEnumerable<string> Files(string relativeDirectory)
        {
            var path = Path.Combine(directory, relativeDirectory);
            return Directory.Exists(path) ? Directory.EnumerateFiles(path).Select(file => Path.GetFileName(file)!) : [];
        }

        public IEnumerable<string> Directories(string relativeDirectory)
        {
            var path = Path.Combine(directory, relativeDirectory);
            return Directory.Exists(path) ? Directory.EnumerateDirectories(path).Select(child => Path.GetFileName(child)!) : [];
        }
    }
}
