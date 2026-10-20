using Campus.Contracts;

namespace Campus.Gen.Buildings.Model;

/// <summary>Window-number allocation of one building part: window = Base + row × Columns + column.</summary>
public readonly record struct WindowBlock(FeatureId Feature, string Key, int Base, int Columns, int Rows)
{
    public int Count => Columns * Rows;
}

/// <summary>
/// All building plans of one world with their generic window blocks: every building part of every feature, claimed or
/// not, in FeatureId then part-key ordinal order. Plans are memoized by their build context; Build() calls on any tile
/// agree. The blocks are the layout before claimed replacements; production numbers come from the map's
/// WindowNumberingSnapshot (<see cref="WindowNumbering.Prepare"/>), which also preserves the persistent registry's addresses.
/// </summary>
public sealed class BuildingSet
{
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

    public static BuildingSet For(BuildContext context) =>
        context.Memo(typeof(BuildingSet), () => Create(context.World, context.WorldSeed));

    private static BuildingSet Create(IWorldModel world, uint worldSeed)
    {
        var sources = world.Features.Values
            .OrderBy(f => f.Id.Value, StringComparer.Ordinal)
            .SelectMany(BuildingPlanner.Sources)
            .ToArray();
        var plans = new List<BuildingPlan>(sources.Length);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var src in sources.OrderBy(s => s.Feature.Id.Value, StringComparer.Ordinal).ThenBy(s => s.Key, StringComparer.Ordinal))
        {
            // A source footprint shared by two entities is built once, by the first entity in ordinal order.
            if (!seen.Add(src.Key)) continue;
            var plan = BuildingPlanner.Plan(src, world, worldSeed);
            if (plan is null) continue;
            plans.Add(plan);
        }
        var (blocks, _) = WindowNumbering.Allocate(plans.Select(p => (p.Id, p.Key, p.TotalColumns, p.Storeys)));
        return new BuildingSet(plans, blocks);
    }
}

