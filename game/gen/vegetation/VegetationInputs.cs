using Campus.Contracts;

namespace Campus.Gen.Vegetation;

/// <summary>
/// game/gen/vegetation/'s read entry at the build-input boundary (contract-32). The species table is a global input relative to
/// game/data, frozen once by the kernel's asset manifest and shared by every map (<see cref="BuildContext.GlobalInputs"/>);
/// a map's planting rules are a map input relative to its DataRoot, i.e. game/data/maps/&lt;map&gt;/vegetation/planting.json
/// (<see cref="BuildContext.SourceInputs"/>). The kernel fingerprints these frozen bytes per product; this class
/// never performs IO. A map without a planting file has no trees.
/// </summary>
public static class VegetationInputs
{
    /// <summary>Global key: game/data/vegetation/species.json.</summary>
    public const string SpeciesKey = "vegetation/species.json";

    /// <summary>Map key, relative to the map's DataRoot.</summary>
    public const string PlantingKey = "vegetation/planting.json";

    public static SpeciesTable Species(BuildContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return SpeciesTable.Parse(ctx.GlobalInputs.Require(SpeciesKey).Span);
    }

    /// <summary>The map's planting rules, or null when the map has none.</summary>
    public static PlantingRules? Rules(BuildContext ctx, SpeciesTable species)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return ctx.SourceInputs.TryGetValue(PlantingKey, out var bytes) ? PlantingRules.Parse(bytes.Span, species) : null;
    }

    /// <summary>The whole map's layout from the context's inputs, world model and world seed (empty without planting rules).</summary>
    public static (PlantingLayout Layout, PlantingRules? Rules) Layout(BuildContext ctx)
    {
        var species = Species(ctx);
        var rules = Rules(ctx, species);
        return (rules is null ? new PlantingLayout([]) : Planting.Build(ctx.World, species, rules, ctx.WorldSeed), rules);
    }
}
