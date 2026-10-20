using Campus.Contracts;
using Campus.Contracts.Maps;
using static Campus.Gen.World.TerrainCorridorCompiler;
using static Campus.Gen.World.TerrainPadCompiler;
using static Campus.Gen.World.TerrainPadRoadComposition;
using static Campus.Gen.World.TerrainRoadWaterComposition;
using static Campus.Gen.World.TerrainWaterNetwork;
using static Campus.Gen.World.TerrainWaterSources;

namespace Campus.Gen.World;

/// <summary>Per-compilation state shared by the ordered terrain stages and retained frozen evaluators.</summary>
internal sealed class TerrainBuildState(TerrainCompileContext context)
{
    public TerrainCompileContext Context { get; } = context;
    public TerrainInputSummary SourceInputs { get; set; } = null!;
    public TerrainReferences References { get; } = new(context);
    public TerrainRegion Campus { get; } = new([context.Boundary]);
    public List<TerrainNode> Nodes { get; } = [];
    public List<TerrainWaterRegion> Water { get; } = [];
    public List<WaterSpec> WaterSpecs { get; } = [];
    public List<WaterSpec> UndergroundWaterSpecs { get; } = [];
    public List<WaterShore> WaterShores { get; } = [];
    public List<TerrainNode> WaterAuthorValidation { get; } = [];
    public Dictionary<FeatureId, float> Pads { get; } = [];
    public Dictionary<FeatureId, TerrainRegion> PadRegions { get; } = [];
    public List<PadCheck> PadChecks { get; } = [];
    public Dictionary<string, PadRoadComposition> PadRoadCompositions { get; } = new(StringComparer.Ordinal);
    public FixedRoadAnchor[] RoadAnchors { get; set; } = [];
    public List<string> Diagnostics { get; } = [];
    public double RangeBound { get; set; }
    public Dictionary<string, TerrainOp> Operations { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Files { get; } = new(StringComparer.Ordinal);
    public HashSet<FeatureId> WrittenWater { get; } = [];
    public HashSet<FeatureId> WrittenPads { get; } = [];
    public HashSet<FeatureId> WrittenRoads { get; } = [];
    public TerrainProgram LandBeforeWater { get; set; } = new([]);
    public TerrainProgram LandBeforeCorridors { get; set; } = new([]);
    public List<RoadWaterComposition> RoadWaterCompositions { get; } = [];
    public List<RoadEntry> Roads { get; } = [];
    public TerrainRoadProfile.NetworkFit? RoadFit { get; set; }
    public List<Action> ShoreEnvelopeRefresh { get; } = [];
}
