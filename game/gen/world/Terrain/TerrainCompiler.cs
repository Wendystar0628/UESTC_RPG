using Campus.Contracts;
using Campus.Contracts.Maps;
using static Campus.Gen.World.TerrainAcceptance;
using static Campus.Gen.World.TerrainBankRegistration;
using static Campus.Gen.World.TerrainCorridorCompiler;
using static Campus.Gen.World.TerrainFieldGrading;
using static Campus.Gen.World.TerrainFootwayBankClosure;
using static Campus.Gen.World.TerrainLandOperations;
using static Campus.Gen.World.TerrainPadCompiler;
using static Campus.Gen.World.TerrainPadRoadComposition;
using static Campus.Gen.World.TerrainRoadWaterComposition;
using static Campus.Gen.World.TerrainWaterNetwork;
using static Campus.Gen.World.TerrainWaterSources;

namespace Campus.Gen.World;

internal static class TerrainCompiler
{
    public static CompiledTerrain Compile(TerrainCompileContext context, IReadOnlyList<TerrainSource> sources)
    {
        var (builder, program) = Construct(context, sources);
        return Complete(builder, program, zonesValidated: false);
    }

    internal static (TerrainBuildState TerrainBuildState, TerrainProgram Program) Construct(TerrainCompileContext context, IReadOnlyList<TerrainSource> sources)
    {
        var builder = new TerrainBuildState(context);
        foreach (var source in sources.OrderBy(s => s.File, StringComparer.Ordinal))
        {
            TerrainOverlayJson.Validate(source.Data);
            if (source.Data.Map is { } map && map != context.Map) throw new WorldDataException(source.File, "map", "Terrain overlay belongs to another map.");
            if (source.File.Split('/')[^1] != source.Data.Zone.Value + ".terrain.json")
                throw new WorldDataException(source.File, "zone", "Terrain filename and declared zone disagree.");
            foreach (var op in source.Data.Ops)
            {
                if (context.Zones is not null && !context.Zones.ContainsKey(op.Zone))
                    throw new WorldDataException(source.File, op.Id, $"Terrain zone '{op.Zone.Value}' does not exist in the supplied authoritative zone map.");
                if (!builder.Operations.TryAdd(op.Id, op)) throw new WorldDataException(source.File, op.Id, $"Duplicate terrain ID, already in {builder.Files[op.Id]}.");
                builder.Files.Add(op.Id, source.File);
            }
        }
        builder.RangeBound = SourceRangeBound(builder.Operations.Values);
        if (!double.IsFinite(builder.RangeBound)) throw new WorldDataException("world", "range", "Terrain operation amplitudes overflow.");
        builder.SourceInputs = TerrainSourceInputs.Prepare(context, sources);
        var ordered = builder.Operations.Values.OrderBy(Group).ThenBy(op => op.Priority).ThenBy(op => op.Id, StringComparer.Ordinal).ToArray();
        CompileOperations(builder, ordered);
        builder.LandBeforeWater = ComposeProgram(builder, builder.Nodes.Where(node => (int)node.Group < (int)TerrainGroup.Water && !node.WaterBed));
        AddDefaultWater(builder);
        CompileWaterGroups(builder);
        ResolveUndergroundLevels(builder);
        ValidateWeirs(builder, ordered.OfType<TerrainChannelOp>());
        var prePad = ComposeProgram(builder, builder.Nodes);
        AddPads(builder, ordered.OfType<TerrainPadOp>(), prePad);
        builder.LandBeforeCorridors = ComposeProgram(builder, builder.Nodes);
        try
        {
            AddCorridors(builder, ordered.OfType<TerrainCorridorOp>());
            ResolveFootwayBanks(builder);
            PrepareRoadWaterComposition(builder);
        }
        catch (WorldDataException error)
        {
            error.Data["TerrainDiagnostics"] = builder.Diagnostics.ToArray();
            throw;
        }
        var program = ComposeProgram(builder, builder.Nodes);
        try
        {
            ValidateFinalRoadAnchors(builder, program);
            ValidatePadOverlaps(builder);
        }
        catch (WorldDataException error)
        {
            error.Data["TerrainDiagnostics"] = builder.Diagnostics.ToArray();
            throw;
        }
        return (builder, program);
    }

    private static void CompileOperations(TerrainBuildState builder, TerrainOp[] ordered)
    {
        foreach (var op in ordered)
        {
            var file = builder.Files[op.Id];
            try
            {
                switch (op)
                {
                    case TerrainPlatformOp p: Platform(builder, p, file); break;
                    case TerrainMoundOp m: Mound(builder, m.Shape, m.Height, m, file); break;
                    case TerrainBasinOp b: Mound(builder, b.Shape, -b.Height, b, file); break;
                    case TerrainSlopeOp s: Slope(builder, s, file); break;
                    case TerrainTerraceOp t: Terrace(builder, t, file); break;
                    case TerrainChannelOp c: Channel(builder, c, file); break;
                    case TerrainLakeOp l: Lake(builder, l, file); break;
                    case TerrainPadOp or TerrainCorridorOp: break; // Depend on the completed pre-pad field.
                    default: throw new WorldDataException(file, op.Id, "Unsupported terrain operation.");
                }
            }
            catch (ArgumentException error) { throw new WorldDataException(file, op.Id, error.Message, error); }
        }
    }

    private static CompiledTerrain Complete(TerrainBuildState builder, TerrainProgram program, bool zonesValidated)
    {
        if (!zonesValidated) builder.Diagnostics.Add("NOT_VERIFIED: terrain is evaluated, but operation zone membership and cross-zone influence extents have not been accepted by offline validation with authoritative zone polygons.");
        builder.Diagnostics.Add($"Terrain compiled: authored={builder.Operations.Count}, nodes={program.Nodes.Count}, pads={builder.Pads.Count}, waterSegments={builder.Water.Count}.");
        return new(builder.SourceInputs, builder.Context.DataRect, program, builder.Pads, builder.Water, builder.Diagnostics, zonesValidated, ExportBanks(builder));
    }

    internal static TerrainGroup Group(TerrainOp op) => op switch
    {
        TerrainPlatformOp or TerrainMoundOp or TerrainBasinOp or TerrainTerraceOp => TerrainGroup.Regional,
        TerrainSlopeOp s => s.Geometry is PolygonGeometry ? TerrainGroup.Regional : TerrainGroup.Linear,
        TerrainChannelOp => TerrainGroup.Linear,
        TerrainLakeOp => TerrainGroup.Water,
        TerrainPadOp => TerrainGroup.Pad,
        TerrainCorridorOp => TerrainGroup.Corridor,
        _ => throw new ArgumentException("Unknown terrain operation.")
    };

    /// <summary>Offline acceptance of the constructed field and each authored influence extent.</summary>
    internal static CompiledTerrain CompileAndValidate(TerrainCompileContext context, IReadOnlyList<TerrainSource> sources)
    {
        var (builder, program) = Construct(context, sources);
        try
        {
            ValidateFinalPads(builder, program);
            ValidateSourceRoadWaterCrossings(builder);
            ValidateProgram(builder, program);
        }
        catch (WorldDataException error)
        {
            error.Data["TerrainDiagnostics"] = builder.Diagnostics.ToArray();
            throw;
        }
        var zonesValidated = context.Zones is not null && builder.Operations.Values.All(op => context.Zones.ContainsKey(op.Zone));
        return Complete(builder, program, zonesValidated);
    }
}
