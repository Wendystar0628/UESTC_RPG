using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

internal static partial class TerrainCompiler
{
    private sealed record PadCheck(FeatureId Feature, TerrainRegion Region, double Y,
        double Apron, double GradeWidth, WorldFeature? Structure, string File, string Id);

    private sealed class Builder(TerrainCompileContext context)
    {
        public TerrainCompileContext Context { get; } = context;
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

    public static CompiledTerrain Compile(TerrainCompileContext context, IReadOnlyList<TerrainSource> sources)
    {
        var (builder, program) = Construct(context, sources);
        return Complete(builder, program, zonesValidated: false);
    }

    private static (Builder Builder, TerrainProgram Program) Construct(TerrainCompileContext context, IReadOnlyList<TerrainSource> sources)
    {
        var builder = new Builder(context);
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
                builder.RangeBound += op switch
                {
                    TerrainMoundOp m => m.Height,
                    TerrainBasinOp b => b.Height,
                    TerrainChannelOp c => c.Depth,
                    _ => 0
                };
            }
        }
        builder.RangeBound += TerrainSampling.MaximumHeight - TerrainSampling.MinimumHeight;
        if (!double.IsFinite(builder.RangeBound)) throw new WorldDataException("overlay/terrain", "range", "Terrain operation amplitudes overflow.");
        var ordered = builder.Operations.Values.OrderBy(Group).ThenBy(op => op.Priority).ThenBy(op => op.Id, StringComparer.Ordinal).ToArray();
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
            ValidateFinalPads(builder, program);
        }
        catch (WorldDataException error)
        {
            error.Data["TerrainDiagnostics"] = builder.Diagnostics.ToArray();
            throw;
        }
        return (builder, program);
    }

    private static CompiledTerrain Complete(Builder builder, TerrainProgram program, bool zonesValidated)
    {
        if (!zonesValidated) builder.Diagnostics.Add("NOT_VERIFIED: terrain is evaluated, but operation zone membership and cross-zone influence extents have not been accepted by offline validation with authoritative zone polygons.");
        builder.Diagnostics.Add($"Terrain compiled: authored={builder.Operations.Count}, nodes={program.Nodes.Count}, pads={builder.Pads.Count}, waterSegments={builder.Water.Count}.");
        return new(builder.Context.DataRect, program, builder.Pads, builder.Water, builder.Diagnostics, zonesValidated, ExportBanks(builder));
    }

    private static TerrainGroup Group(TerrainOp op) => op switch
    {
        TerrainPlatformOp or TerrainMoundOp or TerrainBasinOp or TerrainTerraceOp => TerrainGroup.Regional,
        TerrainSlopeOp s => s.Geometry is PolygonGeometry ? TerrainGroup.Regional : TerrainGroup.Linear,
        TerrainChannelOp => TerrainGroup.Linear,
        TerrainLakeOp => TerrainGroup.Water,
        TerrainPadOp => TerrainGroup.Pad,
        TerrainCorridorOp => TerrainGroup.Corridor,
        _ => throw new ArgumentException("Unknown terrain operation.")
    };

    private static MapRect ConstraintBounds(Builder b, MapRect core, double fill, double cut, double fillet, double crossFall = 0)
    {
        var effectiveGrade = Math.Min(fill, cut) - Math.Abs(crossFall);
        if (effectiveGrade <= 0) throw new ArgumentException("Constraint cross-fall must be smaller than both exterior grades for finite support.");
        var diameter = Math.Abs(core.Max.X - core.Min.X) + Math.Abs(core.Max.Y - core.Min.Y);
        return TerrainRegion.Expand(core, (b.RangeBound + TerrainSampling.MaximumHeight - TerrainSampling.MinimumHeight + Math.Abs(crossFall) * diameter) / effectiveGrade + 2 * fillet);
    }

    private static TerrainEvaluation WithSurface(TerrainEvaluation previous, TerrainDifferential result, double crest, double toe, bool coveredTransition = false)
    {
        if (result == previous.Surface) return previous;
        var grade = Math.Sqrt(result.Dx * result.Dx + result.Dz * result.Dz);
        // Later shallow grading can blend into a covered bank without removing its structural
        // plan mask. An infinitesimal road/soil blend must not relabel a 1:1 bank as bare soil.
        var kind = (coveredTransition || previous.Class == TerrainClass.Transition) && grade > 0.5
            ? TerrainClass.Transition : grade >= 1d / 6 ? TerrainClass.Slope : TerrainClass.Flat;
        return previous with { Surface = result, Class = kind, CrestDistance = crest, ToeDistance = toe };
    }

    private static TerrainEvaluation Constrain(TerrainEvaluation previous, TerrainDifferential target,
        TerrainDifferential distance, double fill, double cut, double fillet, bool coveredTransition = false)
    {
        if (distance.Height <= 0) return WithSurface(previous, target, distance.Height, double.PositiveInfinity, coveredTransition);
        var rounded = TerrainMath.FilletDistance(distance, fillet);
        var raising = previous.Surface.Height < target.Height;
        var grade = raising ? fill : cut;
        var envelope = raising ? target - rounded * fill : target + rounded * cut;
        var result = raising ? TerrainMath.SmoothMaximum(previous.Surface, envelope, grade * fillet)
            : TerrainMath.SmoothMinimum(previous.Surface, envelope, grade * fillet);
        // Foot blending is one-sided and bounded by the crown target; composing both smooth clamps would move the plateau.
        if (raising && result.Height > target.Height || !raising && result.Height < target.Height) result = target;
        var toe = Math.Abs(target.Height - previous.Surface.Height) / grade + 1.5 * fillet - distance.Height;
        return WithSurface(previous, result, distance.Height, toe, coveredTransition);
    }

    private static void Platform(Builder b, TerrainPlatformOp op, string file)
    {
        var region = new TerrainRegion([op.Rings]); var fill = TerrainReferences.Grade(op.Side.Fill); var cut = TerrainReferences.Grade(op.Side.Cut);
        b.Nodes.Add(new(op.Id, file, op.Zone, TerrainGroup.Regional, op.Priority, ConstraintBounds(b, region.Bounds, fill, cut, op.Fillet),
            (previous, x, z) =>
            {
                var distance = region.Distance(x, z); var target = TerrainDifferential.Constant(op.Y);
                if (distance.Height <= 0) RequireZeroBandCore(b, target, x, z, file, op.Id);
                return Constrain(previous, target, distance, fill, cut, op.Fillet);
            }));
    }

    private static void Mound(Builder b, TerrainShape shape, double amplitude, TerrainOp op, string file)
    {
        var (center, radii, yaw) = shape switch
        {
            TerrainCircle circle => (circle.Center, new Vector2((float)circle.Radius), 0d),
            TerrainEllipse ellipse => (ellipse.Center, ellipse.Radii, (ellipse.YawDeg - 90) * (Math.PI / 180)),
            _ => throw new WorldDataException(file, op.Id, "Only circle and ellipse mound/basin shapes are supported.")
        };
        if (!VectorMath.Finite(radii) || radii.X <= 0 || radii.Y <= 0)
            throw new WorldDataException(file, op.Id, "Terrain shape radius cannot be represented in world coordinates.");
        if (1.5 * Math.Abs(amplitude) / Math.Min(radii.X, radii.Y) > 0.5)
            throw new WorldDataException(file, op.Id, "Mound/basin profile exceeds the 1:2 soil-grade limit before composition.");
        var c = Math.Cos(yaw); var s = Math.Sin(yaw);
        var ex = Math.Sqrt(radii.X * (double)radii.X * c * c + radii.Y * (double)radii.Y * s * s);
        var ez = Math.Sqrt(radii.X * (double)radii.X * s * s + radii.Y * (double)radii.Y * c * c);
        var bounds = new MapRect(new((float)(center.X - ex), (float)(center.Y - ez)), new((float)(center.X + ex), (float)(center.Y + ez)));
        b.Nodes.Add(new(op.Id, file, op.Zone, TerrainGroup.Regional, op.Priority, bounds, (previous, x, z) =>
        {
            var contribution = TerrainMath.EllipticalMound(x, z, center, radii, yaw, amplitude);
            return WithSurface(previous, previous.Surface + contribution, double.PositiveInfinity, double.PositiveInfinity);
        }));
    }

    private static void Slope(Builder b, TerrainSlopeOp op, string file)
    {
        var fill = TerrainReferences.Grade(op.Side.Fill); var cut = TerrainReferences.Grade(op.Side.Cut);
        switch (op.Geometry)
        {
            case PolygonGeometry polygon:
            {
                var region = new TerrainRegion([polygon.Rings]); var from = op.From!.Value; var to = op.To!.Value;
                var dx = (double)to.X - from.X; var dz = (double)to.Y - from.Y; var length = Math.Sqrt(dx * dx + dz * dz);
                var tx = dx / length; var tz = dz / length;
                b.Nodes.Add(new(op.Id, file, op.Zone, TerrainGroup.Regional, op.Priority, ConstraintBounds(b, region.Bounds, fill, cut, op.Fillet, op.CrossFall), (previous, x, z) =>
                {
                    var along = ((x - from.X) * tx + (z - from.Y) * tz) / length;
                    var t = Math.Max(0, Math.Min(1, along)); var derivative = along > 0 && along < 1 ? (op.Y1 - op.Y0) / length : 0;
                    var offset = -(x - from.X) * tz + (z - from.Y) * tx;
                    var target = new TerrainDifferential(op.Y0 + (op.Y1 - op.Y0) * t + op.CrossFall * offset,
                        derivative * tx - op.CrossFall * tz, derivative * tz + op.CrossFall * tx);
                    var distance = region.Distance(x, z);
                    if (distance.Height <= 0) RequireZeroBandCore(b, target, x, z, file, op.Id);
                    return Constrain(previous, target, distance, fill, cut, op.Fillet);
                }));
                break;
            }
            case PolylineGeometry polyline:
            {
                var path = new TerrainPath(polyline.Line); var half = op.Width!.Value / 2;
                b.Nodes.Add(new(op.Id, file, op.Zone, TerrainGroup.Linear, op.Priority, ConstraintBounds(b, TerrainRegion.Expand(path.Bounds, half), fill, cut, op.Fillet, op.CrossFall), (previous, x, z) =>
                {
                    var hit = path.Project(x, z); var gradient = (op.Y1 - op.Y0) / path.Length;
                    var target = new TerrainDifferential(op.Y0 + gradient * hit.Station + op.CrossFall * hit.Offset,
                        gradient * hit.StationDx + op.CrossFall * hit.OffsetDx, gradient * hit.StationDz + op.CrossFall * hit.OffsetDz);
                    var distance = new TerrainDifferential(hit.Distance - half, hit.DistanceDx, hit.DistanceDz);
                    if (distance.Height <= 0) RequireZeroBandCore(b, target, x, z, file, op.Id);
                    return Constrain(previous, target, distance, fill, cut, op.Fillet);
                }));
                break;
            }
            default: throw new WorldDataException(file, op.Id, "Unsupported slope geometry.");
        }
    }

    private static void Terrace(Builder b, TerrainTerraceOp op, string file)
    {
        var path = new TerrainPath(op.Front);
        foreach (var station in op.StairAt) if (station > path.Length) throw new WorldDataException(file, op.Id, "Terrace stairAt is outside the real front length.");
        var length = Math.Sqrt((double)op.Inward.X * op.Inward.X + (double)op.Inward.Y * op.Inward.Y);
        var nx = op.Inward.X / length; var nz = op.Inward.Y / length;
        var depth = op.Tiers * op.Tread; var rise = op.Tiers * op.Riser;
        if (op.Riser / op.Tread > 0.5 || op.FrontY + rise > TerrainSampling.MaximumHeight)
            throw new WorldDataException(file, op.Id, "Terrace mean slope or upper elevation exceeds the terrain limits.");
        var ring = op.Front.Points.Concat(op.Front.Points.Reverse().Select(p => new Vector2((float)(p.X + nx * depth), (float)(p.Y + nz * depth)))).ToArray();
        var region = new TerrainRegion([new(ring)]);
        b.Nodes.Add(new(op.Id, file, op.Zone, TerrainGroup.Regional, op.Priority, ConstraintBounds(b, region.Bounds, 0.5, 0.5, 2), (previous, x, z) =>
        {
            var hit = path.Project(x, z); var front = path.At(hit.Station);
            var along = (x - front.X) * nx + (z - front.Y) * nz; var t = Math.Max(0, Math.Min(depth, along));
            var slope = along > 0 && along < depth ? rise / depth : 0;
            var a = path.Line.Points[hit.Segment]; var c = path.Line.Points[hit.Segment + 1];
            var segmentX = (double)c.X - a.X; var segmentZ = (double)c.Y - a.Y;
            var segmentLength = Math.Sqrt(segmentX * segmentX + segmentZ * segmentZ);
            var alongTangent = nx * segmentX / segmentLength + nz * segmentZ / segmentLength;
            var target = new TerrainDifferential(op.FrontY + rise * t / depth - 0.10,
                slope * (nx - alongTangent * hit.StationDx), slope * (nz - alongTangent * hit.StationDz));
            var distance = region.Distance(x, z);
            if (distance.Height <= 0) RequireZeroBandCore(b, target, x, z, file, op.Id);
            return Constrain(previous, target, distance, 0.5, 0.5, 2);
        }));
    }
}
