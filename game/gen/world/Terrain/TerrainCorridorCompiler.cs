using Campus.Contracts;
using Campus.Contracts.Maps;
using static Campus.Gen.World.TerrainCorridorRanges;
using static Campus.Gen.World.TerrainFieldGrading;
using static Campus.Gen.World.TerrainPadRoadComposition;
using static Campus.Gen.World.TerrainRoadCoreGeometry;
using static Campus.Gen.World.TerrainRoadWaterComposition;

namespace Campus.Gen.World;

internal static class TerrainCorridorCompiler
{
    // Private compile-time builder. Trial profiles are replaced only while Hard banks are
    // being planned; every published node closes over an entry frozen before validation.
    internal sealed class RoadEntry(TerrainRoad road, TerrainRoadProfile profile, TerrainCorridorOp? operation,
        string file)
    {
        public TerrainRoad Road { get; } = road;
        public TerrainRoadProfile Profile { get; private set; } = profile;
        public TerrainCorridorOp? Operation { get; } = operation;
        public string File { get; } = file;
        public CorridorPlan? Plan { get; init; }
        public double MaximumCrossFall => Plan is { } plan ? Math.Max(plan.HasGaps ? AutomaticRoadCrossFall : 0, plan.Operations.Max(op => Math.Abs(op.CrossFall))) : Math.Abs(Operation?.CrossFall ?? AutomaticRoadCrossFall);
        private bool _frozen;
        public void ReplaceProfile(TerrainRoadProfile replacement)
        {
            if (_frozen || Operation is not null && Plan is null) throw new InvalidOperationException("A frozen/authored road profile cannot be replaced.");
            Profile = replacement;
        }
        public void FreezeProfile() => _frozen = true;
    }

    internal const double AutomaticRoadCrossFall = 0.015;

    internal static void AddCorridors(TerrainBuildState b, IEnumerable<TerrainCorridorOp> operations)
    {
        var roads = new List<RoadEntry>();
        var plans = CorridorPlans(b, operations);
        PreparePadRoadComposition(b, plans);
        foreach (var plan in plans)
        {
            b.WrittenRoads.Add(plan.Road.Source);
            if (plan.Operations.Length == 1 && plan.Operations[0].Range is null)
            {
                var op = plan.Operations[0];
                roads.Add(AddCorridor(b, plan.Road, op, b.Files[op.Id], exclusionsOverride: plan.Exclusions));
            }
        }
        var ranged = plans.Where(plan => plan.Operations.Any(op => op.Range is not null)).ToArray();
        var automatic = b.References.AutomaticRoads().Where(road => !b.WrittenRoads.Contains(road.Source)).ToArray();
        b.RoadFit = new(automatic.Select(road => new TerrainRoadProfile.Request(road,
            BridgeExclusions(b, road), MaximumRoadGrade(road)))
            .Concat(ranged.Select(plan => new TerrainRoadProfile.Request(plan.Road, plan.Exclusions, MaximumRoadGrade(plan.Road),
                plan.Operations.Select(op => new TerrainRoadProfile.Interval(op.Anchors, b.Files[op.Id], op.Id)).ToArray()))).ToArray(), b.LandBeforeCorridors,
            roads.Select(entry => (entry.Road, entry.Profile)).ToArray(), (x, z) => ZeroBandWeight(b, x, z).Height == 0);
        var profiles = b.RoadFit.Solve();
        foreach (var plan in ranged)
        {
            var op = plan.Operations[0];
            if (plan.HasGaps && b.Context.Defaults is null)
                throw new WorldDataException("map.json", "terrainDefaults", "Automatic road gaps require explicit map defaults.");
            var entry = new RoadEntry(plan.Road, profiles[plan.Road.Source], op, b.Files[op.Id]) { Plan = plan };
            foreach (var interval in plan.Operations) AddCorridor(b, plan.Road, interval, b.Files[interval.Id], sharedEntry: entry);
            if (plan.HasGaps) AddCorridor(b, plan.Road, null, b.References.SourceFiles(plan.Road.Source), sharedEntry: entry);
            roads.Add(entry);
        }
        foreach (var road in automatic)
        {
            if (!b.WrittenRoads.Add(road.Source)) continue;
            var entry = AddCorridor(b, road, null, b.References.SourceFiles(road.Source), profiles[road.Source]); roads.Add(entry);
        }
        ValidateRoadJunctions(b, roads);
        b.Roads.AddRange(roads);
        AddPadCoreComposition(b);
    }

    private static bool IsMotorRoad(TerrainRoad road) => road.Kind is "motorway" or "trunk" or "primary" or "secondary" or "tertiary" or "residential" or "service";

    private static double MaximumRoadGrade(TerrainRoad road) => IsMotorRoad(road) ? 0.06 : 1d / 12;

    internal static List<(double From, double To)> BridgeExclusions(TerrainBuildState b, TerrainRoad road)
    {
        const double join = 0.05;
        var exclusions = new List<(double From, double To)>();
        foreach (var bridge in b.Context.Network.Bridges.OrderBy(bridge => bridge.Id.Value, StringComparer.Ordinal))
        {
            var deck = b.References.BridgeArea(bridge.Id);
            var bounds = TerrainRegion.Expand(deck?.Bounds ?? new TerrainPath(bridge.Line).Bounds,
                join + (deck is null ? bridge.Width / 2d : 0));
            if (road.Path.Bounds.Max.X < bounds.Min.X || road.Path.Bounds.Min.X > bounds.Max.X
                || road.Path.Bounds.Max.Y < bounds.Min.Y || road.Path.Bounds.Min.Y > bounds.Max.Y) continue;
            exclusions.AddRange(TerrainUnion.CoveredPathIntervals(road.Path, deck, bridge, join));
        }
        return exclusions;
    }

    private static RoadEntry AddCorridor(TerrainBuildState b, TerrainRoad road, TerrainCorridorOp? op, string file, TerrainRoadProfile? automaticProfile = null,
        RoadEntry? sharedEntry = null, IReadOnlyList<(double From, double To)>? exclusionsOverride = null)
    {
        var id = op?.Id ?? "auto-corridor-" + road.Feature.Value;
        var exclusions = exclusionsOverride ?? BridgeExclusions(b, road);
        var motor = IsMotorRoad(road);
        var maximumGrade = MaximumRoadGrade(road);
        var profile = sharedEntry?.Profile ?? automaticProfile ?? (op is null
            ? throw new InvalidOperationException("Automatic roads require the completed shared network profile solve.")
            : new TerrainRoadProfile(op.Anchors, road.Path.Length, exclusions, maximumGrade, file, id));
        var shoulder = op?.Shoulder ?? b.Context.Defaults?.CorridorShoulder
            ?? throw new WorldDataException("map.json", "terrainDefaults", "Automatic road constraints require explicit map defaults.");
        var width = road.Width / 2 + (sharedEntry?.Plan is { } planWidth ? Math.Max(shoulder, planWidth.Operations.Max(operation => operation.Shoulder)) : shoulder);
        var crossMagnitude = sharedEntry?.MaximumCrossFall ?? Math.Abs(op?.CrossFall ?? AutomaticRoadCrossFall);
        var reach = CorridorReach(b.RangeBound, width, crossMagnitude);
        var support = TerrainRegion.Expand(road.Path.Bounds, reach);
        var patches = PathPatches(road.Path, reach);
        var entry = sharedEntry ?? new RoadEntry(road, profile, op, file);
        TerrainEvaluation Apply(TerrainEvaluation previous, double x, double z, bool pavement)
        {
            if (op is null && previous.BoundaryWeight is { Height: 0 } && previous.Surface == TerrainDifferential.Constant(0)) return previous;
            var pointHit = road.Path.Project(x, z);
            // SurfaceAt evaluates this projection, not the separately projected footprint
            // contact. A structural interval has no ground surface or grading contribution.
            if (entry.Profile.Excluded(pointHit.Station)) return previous;
            if (entry.Plan is { } plan && plan.At(pointHit.Station)?.Id != op?.Id) return previous;
            // Pavement is composed after every road's shoulders and earthworks. A later
            // road's falloff can therefore never cut through an earlier road surface.
            if ((CorridorContactAt(road, 0, x, z).Distance.Height <= 0) != pavement) return previous;
            var parameters = entry.Plan?.Parameters(pointHit, shoulder);
            var widthAt = parameters?.Shoulder ?? TerrainDifferential.Constant(shoulder);
            var contact = CorridorContactAt(road, widthAt.Height, x, z);
            contact = contact with { Distance = contact.Distance - new TerrainDifferential(0, widthAt.Dx, widthAt.Dz) };
            var hit = road.Path.Project(contact.X, contact.Z);
            if (entry.Profile.Excluded(hit.Station)) return previous;
            var longitudinal = entry.Profile.SurfaceAt(road.Path, x, z, road.Width, pointHit);
            var cross = parameters?.Cross ?? TerrainDifferential.Constant(op?.CrossFall ?? AutomaticRoadCrossFall);
            if (op is null)
            {
                cross = TerrainDifferential.Product(previous.BoundaryWeight ?? ZeroBandWeight(b, x, z), cross);
            }
            var lateral = CorridorOffsetAt(road, contact, x, z);
            var offset = motor ? lateral * (lateral.Height > 0 ? -1 : lateral.Height < 0 ? 1 : 0) : lateral;
            var target = new TerrainDifferential(longitudinal.Height + cross.Height * offset.Height,
                longitudinal.Dx + cross.Height * offset.Dx + cross.Dx * offset.Height,
                longitudinal.Dz + cross.Height * offset.Dz + cross.Dz * offset.Height);
            if (pavement) RequireZeroBandCore(b, target, x, z, file, id);
            var proposed = Constrain(previous, target, contact.Distance, 1d / 3, 1d / 3, 2);
            return ConstrainRoadAtWater(b, previous, proposed, x, z);
        }
        b.Nodes.Add(new(id, file, op?.Zone ?? default, TerrainGroup.CorridorGrading, op?.Priority ?? 50, support,
            (previous, x, z) => Apply(previous, x, z, false), op is null) { IndexPatches = patches });
        var coreReach = (road.Footprint is null ? road.Width / 2 : 0) + TerrainMath.RoadCoreJoinTolerance;
        var coreSupport = TerrainRegion.Expand(road.Footprint?.Bounds ?? road.Path.Bounds, coreReach);
        b.Nodes.Add(new(id, file, op?.Zone ?? default, TerrainGroup.Corridor, op?.Priority ?? 50, coreSupport,
            (previous, x, z) => Apply(previous, x, z, true), op is null)
        {
            IndexPatches = road.Footprint is null ? PathPatches(road.Path, coreReach) : null,
            ExplainPoint = (x, z) =>
            {
                var hit = road.Path.Project(x, z); var center = road.Path.At(hit.Station);
                var contact = CorridorContactAt(road, shoulder, x, z); var sx = contact.X; var sz = contact.Z;
                var oldShoulder = b.LandBeforeCorridors.Evaluate(sx, sz).Surface.Height;
                var shoulderHit = road.Path.Project(sx, sz);
                var shoulderOffset = motor ? -Math.Abs(shoulderHit.Offset) : shoulderHit.Offset;
                var edgeTarget = entry.Profile.At(hit.Station).Y + (op?.CrossFall ?? ZeroBandWeight(b, sx, sz).Height * AutomaticRoadCrossFall) * shoulderOffset;
                return $"roadProjection=({center.X:R},{center.Y:R}), s={hit.Station:R}/{road.Path.Length:R}, distance={hit.Distance:R}, signedOffset={hit.Offset:R}, halfWidth+shoulder={width:R}, coreDistance={contact.Distance.Height:R}, shoulderPoint=({sx:R},{sz:R}), originalShoulderH={oldShoulder:R}, targetShoulderH={edgeTarget:R}, supportReach={reach:R}, sourceFootprintContains={road.Footprint?.Contains(x,z)}";
            }
        });
        if (op is not null && !motor) b.Diagnostics.Add($"NOT_VERIFIED: corridor '{op.Id}' obeys the 1:12 paved-path ceiling; the stricter 5% main-network test needs actual walkable main-region data.");
        return entry;
    }

    private static MapRect[] PathPatches(TerrainPath path, double reach)
    {
        var patches = new MapRect[path.Line.Points.Count - 1];
        for (var i = 0; i < patches.Length; i++)
        {
            var a = path.Line.Points[i]; var c = path.Line.Points[i + 1];
            patches[i] = TerrainRegion.Expand(new(new(Math.Min(a.X, c.X), Math.Min(a.Y, c.Y)),
                new(Math.Max(a.X, c.X), Math.Max(a.Y, c.Y))), reach);
        }
        return patches;
    }

    internal static void ValidateRoadJunctions(TerrainBuildState b, IReadOnlyList<RoadEntry> roads)
    {
        var nodes = new Dictionary<(long X, long Z), List<(RoadEntry Road, double Station)>>();
        foreach (var road in roads)
            foreach (var p in road.Road.Path.Line.Points)
            {
                var key = ((long)Math.Round(p.X * 1000d), (long)Math.Round(p.Y * 1000d));
                var station = road.Road.Path.Project(p.X, p.Y).Station;
                if (!nodes.TryGetValue(key, out var list)) nodes[key] = list = [];
                if (!list.Any(entry => entry.Road.Road.Source == road.Road.Source)) list.Add((road, station));
            }
        foreach (var node in nodes.OrderBy(pair => pair.Key.X).ThenBy(pair => pair.Key.Z))
        {
            if (node.Value.Count < 2) continue;
            var connected = node.Value.Where(entry => !entry.Road.Profile.Excluded(entry.Station)).ToArray();
            if (connected.Length < 2) continue;
            var first = connected[0].Road.Profile.At(connected[0].Station).Y;
            foreach (var (road, station) in connected)
            {
                if (road.Profile.Excluded(station)) continue;
                var height = road.Profile.At(station).Y;
                if (!double.IsFinite(first) || !double.IsFinite(height) || Math.Abs(height - first) > TerrainRoadProfile.JunctionHeightTolerance)
                    throw new WorldDataException(road.File, road.Operation?.Id ?? road.Road.Feature.Value, "Road centreline junction has inconsistent elevations.");
                var maximumGrade = MaximumRoadGrade(road.Road);
                (double Station, double Height, double Grade)? comfortExceeded = null;
                for (var delta = -10; delta <= 10; delta++)
                {
                    var sample = Math.Max(0, Math.Min(road.Profile.Length, station + delta));
                    if (road.Profile.Excluded(sample)) continue;
                    var point = road.Profile.At(sample);
                    if (!double.IsFinite(point.Y) || !double.IsFinite(point.Grade) || Math.Abs(point.Grade) > maximumGrade + TerrainMath.GradeTolerance)
                        throw new WorldDataException(road.File, road.Operation?.Id ?? road.Road.Feature.Value,
                            $"Road profile is non-finite or exceeds hard grade {maximumGrade:R} within 10 m of a junction: node=({node.Key.X},{node.Key.Z}) mm, station={station:R}, sample={sample:R}, height={point.Y:R}, grade={point.Grade:R}.");
                    if (Math.Abs(point.Grade) > 0.03 + TerrainMath.GradeTolerance
                        && (comfortExceeded is null || Math.Abs(point.Grade) > Math.Abs(comfortExceeded.Value.Grade)))
                        comfortExceeded = (sample, point.Y, point.Grade);
                }
                // The 3% junction buffer is a comfort target; the road-class ceiling above
                // remains blocking. Keep the authored profile and report its worst existing sample.
                if (comfortExceeded is { } issue)
                {
                    var diagnostic = $"Road junction exceeds 3% comfort target within 10 m (non-blocking content issue): file='{road.File}', entry='{road.Operation?.Id ?? road.Road.Feature.Value}', source='{road.Road.Source.Value}', node=({node.Key.X},{node.Key.Z}) mm, station={station:R}, sample={issue.Station:R}, height={issue.Height:R}, grade={issue.Grade:R}, hardMaximumGrade={maximumGrade:R}.";
                    if (!b.Diagnostics.Contains(diagnostic, StringComparer.Ordinal)) b.Diagnostics.Add(diagnostic);
                }
            }
        }
    }

    internal static TerrainDifferential RoadCoreTop(TerrainBuildState b, RoadEntry entry, CorridorContact core, TerrainPathHit hit, double x, double z)
    {
        var longitudinal = entry.Profile.SurfaceAt(entry.Road.Path, x, z, entry.Road.Width, hit);
        var cross = entry.Plan?.Parameters(hit, b.Context.Defaults?.CorridorShoulder ?? entry.Operation!.Shoulder).Cross
            ?? (entry.Operation is { } op ? TerrainDifferential.Constant(op.CrossFall) : ZeroBandWeight(b, x, z) * 0.015);
        if (entry.Plan is { } plan && plan.At(hit.Station) is null) cross = TerrainDifferential.Product(cross, ZeroBandWeight(b, x, z));
        var lateral = CorridorOffsetAt(entry.Road, core, x, z);
        var offset = IsMotorRoad(entry.Road) ? lateral * (lateral.Height > 0 ? -1 : lateral.Height < 0 ? 1 : 0) : lateral;
        return new(longitudinal.Height + cross.Height * offset.Height,
            longitudinal.Dx + cross.Height * offset.Dx + cross.Dx * offset.Height,
            longitudinal.Dz + cross.Height * offset.Dz + cross.Dz * offset.Height);
    }
}
