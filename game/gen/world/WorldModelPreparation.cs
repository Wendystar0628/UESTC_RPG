using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>Parsed source data retained between cache lookup and publication, without a compiled evaluator.</summary>
public sealed class WorldModelPreparation
{
    private readonly Func<ITerrainAssetBuilder>? _build;
    private readonly Func<ITerrain, IReadOnlyList<string>, WorldModelLoadResult> _complete;
    private readonly Func<IWorldModel, ITerrain, IWorldModel> _withTerrain;
    public TerrainInputSummary SourceInputs { get; }
    internal WorldModelPreparation(TerrainInputSummary sourceInputs, Func<ITerrainAssetBuilder>? build,
        Func<ITerrain, IReadOnlyList<string>, WorldModelLoadResult> complete,
        Func<IWorldModel, ITerrain, IWorldModel> withTerrain)
    { SourceInputs = sourceInputs; _build = build; _complete = complete; _withTerrain = withTerrain; }
    public ITerrainAssetBuilder CreateAssets() => _build?.Invoke()
        ?? throw new InvalidOperationException("A terrain asset builder requires descriptor-based inputs.");
    public WorldModelLoadResult Complete(TerrainFrozenData data) => _complete(new PreparedTerrain(data), data.Diagnostics);
    public IWorldModel WithTerrain(IWorldModel world, ITerrain terrain) => _withTerrain(world, terrain);
    internal WorldModelLoadResult BuildForAssets()
    {
        if (_build is null) return _complete(FlatTerrain.Default, []);
        var terrain = _build();
        return _complete(terrain, terrain.Diagnostics);
    }
}
