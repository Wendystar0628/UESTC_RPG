using System.Runtime.CompilerServices;
using Campus.Contracts;

namespace Campus.Gen.Buildings.Model;

/// <summary>Window-number allocation of one building part: window = Base + row × Columns + column.</summary>
public readonly record struct WindowBlock(FeatureId Feature, string Key, int Base, int Columns, int Rows)
{
    public int Count => Columns * Rows;
}

/// <summary>
/// All building plans of one world, with campus window numbers. Every building part of every feature takes part
/// in the numbering (claimed or not) in FeatureId then part-key ordinal order, so zone packs that reuse these
/// facade rules get the same numbers. Plans are cached per world model and seed; Build() calls on any tile agree.
/// </summary>
public sealed class BuildingSet
{
    /// <summary>Campus window numbers stay below this (window_override is 256 × 256); outskirts use their own range above it.</summary>
    public const int CampusWindowLimit = 65536;

    private static readonly ConditionalWeakTable<IWorldModel, Dictionary<uint, BuildingSet>> Cache = new();
    private static readonly object Sync = new();

    private BuildingSet(IReadOnlyList<BuildingPlan> plans, IReadOnlyList<WindowBlock> blocks)
    {
        Plans = plans;
        Blocks = blocks;
        var byKey = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < plans.Count; i++) byKey[plans[i].Key] = i;
        _index = byKey;
        WindowCount = blocks.Count == 0 ? 0 : blocks[^1].Base + blocks[^1].Count;
    }

    private readonly Dictionary<string, int> _index;

    /// <summary>Plans sorted by (FeatureId, part key) ordinal.</summary>
    public IReadOnlyList<BuildingPlan> Plans { get; }
    /// <summary>Window blocks, parallel to Plans.</summary>
    public IReadOnlyList<WindowBlock> Blocks { get; }
    public int WindowCount { get; }

    public WindowBlock BlockOf(BuildingPlan plan) => Blocks[_index[plan.Key]];

    public static BuildingSet For(IWorldModel world, uint worldSeed)
    {
        ArgumentNullException.ThrowIfNull(world);
        lock (Sync)
        {
            var bySeed = Cache.GetValue(world, _ => []);
            if (bySeed.TryGetValue(worldSeed, out var found)) return found;
            var created = Create(world, worldSeed);
            bySeed.Add(worldSeed, created);
            return created;
        }
    }

    private static BuildingSet Create(IWorldModel world, uint worldSeed)
    {
        var sources = world.Features.Values
            .OrderBy(f => f.Id.Value, StringComparer.Ordinal)
            .SelectMany(BuildingPlanner.Sources)
            .ToArray();
        var plans = new List<BuildingPlan>(sources.Length);
        var blocks = new List<WindowBlock>(sources.Length);
        var next = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var src in sources.OrderBy(s => s.Feature.Id.Value, StringComparer.Ordinal).ThenBy(s => s.Key, StringComparer.Ordinal))
        {
            // A source footprint shared by two entities is built once, by the first entity in ordinal order.
            if (!seen.Add(src.Key)) continue;
            var plan = BuildingPlanner.Plan(src, world.Spatial, worldSeed, world.Fidelity);
            if (plan is null) continue;
            plans.Add(plan);
            blocks.Add(new WindowBlock(plan.Id, plan.Key, next, plan.TotalColumns, plan.Storeys));
            next += plan.TotalColumns * plan.Storeys;
        }
        return new BuildingSet(plans, blocks);
    }
}

/// <summary>Public window-number lookup for WP-39 (window overrides) and zone packs reusing the facade rules.</summary>
public static class WindowNumbering
{
    public static IReadOnlyList<WindowBlock> For(IWorldModel world, uint worldSeed) => BuildingSet.For(world, worldSeed).Blocks;
}
