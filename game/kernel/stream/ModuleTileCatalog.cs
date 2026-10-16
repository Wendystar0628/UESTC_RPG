using Campus.Contracts;
using Campus.Kernel.Lod;

namespace Campus.Kernel.Stream;

public sealed record StreamModule(string Id, IContentModule Module);

/// <summary>Enumerated on a worker; only immutable module IDs survive into the streaming loop.</summary>
public sealed class ModuleTileCatalog
{
    private readonly Dictionary<StreamTile, string[]> _modules;
    private ModuleTileCatalog(Dictionary<StreamTile, string[]> modules) => _modules = modules;
    public IReadOnlyList<string> Modules(StreamTile tile) => _modules.TryGetValue(tile, out var modules) ? modules : [];
    /// <summary>Fixture entry point for modules intentionally sharing one complete context.</summary>
    public static ModuleTileCatalog Build(BuildContext context, IReadOnlyList<StreamModule> modules)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(modules);
        return Build(modules.ToDictionary(module => module.Id, _ => context, StringComparer.Ordinal), modules);
    }
    /// <summary>Production entry point: each module receives the exact context prepared by the bake prepass.</summary>
    public static ModuleTileCatalog Build(IReadOnlyDictionary<string, BuildContext> preparedContexts, IReadOnlyList<StreamModule> modules)
    {
        ArgumentNullException.ThrowIfNull(preparedContexts);
        ArgumentNullException.ThrowIfNull(modules);
        var byTile = new Dictionary<StreamTile, SortedSet<string>>();
        foreach (var module in modules.OrderBy(m => m.Id, StringComparer.Ordinal))
        {
            if (!preparedContexts.TryGetValue(module.Id, out var context) || context is null)
                throw new InvalidOperationException($"Prepared build context is missing for module '{module.Id}'.");
            foreach (var key in module.Module.Tiles(context).Distinct().OrderBy(t => t.X).ThenBy(t => t.Z))
            {
                if (key.X <= -512 || key.X >= 512 || key.Z <= -512 || key.Z >= 512)
                    throw new InvalidDataException($"Module {module.Id} returned an out-of-range tile ({key.X},{key.Z}).");
                var tile = new StreamTile(key, LodLevel.L0);
                while (true)
                {
                    if (!byTile.TryGetValue(tile, out var ids)) byTile.Add(tile, ids = new(StringComparer.Ordinal));
                    ids.Add(module.Id);
                    if (tile.Level == LodLevel.L4) break;
                    tile = tile.Parent;
                }
            }
        }
        return new(byTile.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
    }
}
