using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>Parsed source data retained between cache lookup and publication, without a compiled evaluator.</summary>
public sealed class WorldModelPreparation
{
    private readonly Func<ITerrainAssetBuilder>? _build;
    private readonly ITerrain? _suppliedTerrain;
    private readonly Func<ITerrain, IReadOnlyList<string>, FidelityFrozenData, WorldModelLoadResult> _complete;
    private readonly Func<ITerrain, FidelityFrozenData> _buildFidelity;
    private readonly Func<IWorldModel, ITerrain, IWorldModel> _withTerrain;
    public TerrainInputSummary SourceInputs { get; }
    public string FidelitySourceStamp { get; }
    internal WorldModelPreparation(TerrainInputSummary sourceInputs, Func<ITerrainAssetBuilder>? build,
        string fidelitySourceStamp, Func<ITerrain, FidelityFrozenData> buildFidelity,
        Func<ITerrain, IReadOnlyList<string>, FidelityFrozenData, WorldModelLoadResult> complete,
        Func<IWorldModel, ITerrain, IWorldModel> withTerrain, ITerrain? suppliedTerrain)
    { SourceInputs = sourceInputs; FidelitySourceStamp = fidelitySourceStamp; _build = build; _buildFidelity = buildFidelity; _complete = complete; _withTerrain = withTerrain; _suppliedTerrain = suppliedTerrain; }
    public ITerrainAssetBuilder CreateAssets() => _build?.Invoke()
        ?? throw new InvalidOperationException("A terrain asset builder requires descriptor-based inputs.");
    public FidelityFrozenData CreateFidelityAssets(ITerrain terrain) => _buildFidelity(terrain);
    public WorldModelLoadResult Complete(TerrainFrozenData data, Func<ITerrain, FidelityFrozenData> resolveFidelity)
    {
        var terrain = new PreparedTerrain(data);
        return _complete(terrain, data.Diagnostics, resolveFidelity(terrain));
    }
    public IWorldModel WithTerrain(IWorldModel world, ITerrain terrain) => _withTerrain(world, terrain);
    internal WorldModelLoadResult BuildForAssets()
    {
        if (_build is null)
        {
            var supplied = _suppliedTerrain ?? throw new InvalidOperationException("An explicit terrain input is required.");
            return _complete(supplied, [], CreateFidelityAssets(supplied));
        }
        var terrain = _build();
        return _complete(terrain, terrain.Diagnostics, CreateFidelityAssets(terrain));
    }
}
