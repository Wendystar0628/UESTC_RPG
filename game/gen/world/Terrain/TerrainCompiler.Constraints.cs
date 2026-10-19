using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Water;

namespace Campus.Gen.World;

internal static partial class TerrainCompiler
{
    internal const double AutomaticPadApron = 0.8;
    internal const double AutomaticPadGrade = 6;
    internal const double AutomaticRoadCrossFall = 0.015;

    private static void AddDefaultWater(Builder b)
    {
        foreach (var feature in b.Context.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (b.WrittenWater.Contains(feature.Id)) continue;
            var polygons = WorldFeatureKinds.Parts(feature, "water").SelectMany(p => WorldFeatureGeometry.PolygonsOf(p.Geometry)).ToArray();
            if (polygons.Length == 0) continue;
            if (WaterPoolLayout.IsPool(feature))
            {
                var poolRegion = new TerrainRegion(polygons);
                AddWater(b, null, b.References.SourceFiles(feature.Id), feature.Id, poolRegion,
                    DefaultShoreTop(b, poolRegion, feature.Id) - WaterPoolRules.RimDrop,
                    new TerrainBank(TerrainBankKind.Hard, "1:1"), WaterPoolRules.DeepDepth, 1, [], false);
                continue;
            }
            var defaults = b.Context.Defaults ?? throw new WorldDataException("map.json", "terrainDefaults", "Automatic water constraints require explicit map defaults.");
            var channel = b.References.IsChannelWater(feature);
            var slope = channel ? defaults.ChannelBankGrade : defaults.LakeBankGrade;
            var bank = new TerrainBank(channel ? TerrainBankKind.Hard : TerrainBankKind.Soft,
                "1:" + (1 / slope).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            var region = new TerrainRegion(polygons);
            var underground = b.References.IsUndergroundWater(feature.Id);
            var pending = !underground && channel;
            var shoreTop = underground || pending ? 0 : DefaultShoreTop(b, region, feature.Id);
            AddWater(b, null, b.References.SourceFiles(feature.Id), feature.Id, region, shoreTop - (channel ? 1.2 : 0.5), bank,
                channel ? defaults.ChannelDepth : defaults.LakeDepth, 1d / 7, [], channel);
            if (pending) b.WaterSpecs[^1] = b.WaterSpecs[^1] with { AutoLevelPending = true };
        }
    }

    private static double DefaultShoreTop(Builder b, TerrainRegion region, FeatureId id)
    {
        if (region.Polygons.Length == 0 || region.Polygons[0].Outer.Count == 0)
            throw new WorldDataException(b.References.SourceFiles(id), id.Value, "Water has no shore-top sample.");
        if (b.LandBeforeWater.Nodes.Count == 0) return 0;
        var point = region.Polygons[0].Outer[0];
        return b.LandBeforeWater.Evaluate(point.X, point.Y).Surface.Height;
    }

    private static void AddPads(Builder b, IEnumerable<TerrainPadOp> operations, TerrainProgram prePad)
    {
        foreach (var op in operations)
        {
            if (!b.WrittenPads.Add(op.Feature)) throw new WorldDataException(b.Files[op.Id], op.Id, "Multiple pad operations target one entity.");
            var region = b.References.PadArea(op.Feature, b.Files[op.Id], op.Id);
            AddPad(b, region, op.Feature, op.PadY ?? Median(prePad, region), op.Apron, op.Grade, op.Edge, op);
        }
        foreach (var feature in b.Context.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (b.WrittenPads.Contains(feature.Id)) continue;
            if (b.References.StructuralPadArea(feature) is not { } region) continue;
            AddPad(b, region, feature.Id, Median(prePad, region), AutomaticPadApron, AutomaticPadGrade, null, null);
        }
    }

    private static void AddPad(Builder b, TerrainRegion region, FeatureId feature, double y, double apron, double gradeWidth,
        TerrainPadEdge? edge, TerrainPadOp? op)
    {
        var file = op is null ? b.References.SourceFiles(feature) : b.Files[op.Id]; var id = op?.Id ?? "auto-pad-" + feature.Value;
        b.Pads.Add(feature, (float)y);
        b.PadRegions.Add(feature, region);
        var structure = edge is null ? null : b.References.Structure(new(edge.StructureId), file, id, "edgeBand", "terrace", "retainingWall");
        if (edge is { Kind: TerrainEdgeKind.Terrace } && edge.Tread < Math.Max(2, 1.5 * edge.Riser))
            throw new WorldDataException(file, id, "Terraced retaining structure has insufficient tread depth.");
        var fill = structure is null ? 0.25 : 1d;
        var bounded = TerrainRegion.Expand(region.Bounds, apron + gradeWidth);
        // MapRect stores float coordinates. Its index box must round outwards;
        // the exact signed-distance guard below owns the physical influence limit.
        var support = new MapRect(new(MathF.BitDecrement(bounded.Min.X), MathF.BitDecrement(bounded.Min.Y)),
            new(MathF.BitIncrement(bounded.Max.X), MathF.BitIncrement(bounded.Max.Y)));
        var node = new TerrainNode(id, file, op?.Zone ?? default, TerrainGroup.Pad, op?.Priority ?? 50,
            support, (previous, x, z) =>
            {
                if (op is null && y == 0 && previous.BoundaryWeight is { Height: 0 } && previous.Surface == TerrainDifferential.Constant(0)) return previous;
                var distance = region.Distance(x, z);
                if (distance.Height <= 0) RequireZeroBandCore(b, TerrainDifferential.Constant(y), x, z, file, id);
                if (distance.Height > 0 && InsideOtherPadCore(b, id, x, z)) return previous;
                var beyond = distance - TerrainDifferential.Constant(apron);
                if (beyond.Height > gradeWidth) return previous;
                var target = TerrainDifferential.Constant(y);
                if (distance.Height > 0)
                {
                    var factor = op is null ? previous.BoundaryWeight ?? ZeroBandWeight(b, x, z) : TerrainDifferential.Constant(1);
                    // The outside envelope continues from the apron edge elevation. Resetting it
                    // to PadY at d=apron would create a vertical seam of 3% * apron.
                    var drop = (distance.Height < apron ? distance : TerrainDifferential.Constant(apron)) * 0.03;
                    target -= new TerrainDifferential(drop.Height * factor.Height,
                        drop.Dx * factor.Height + drop.Height * factor.Dx, drop.Dz * factor.Height + drop.Height * factor.Dz);
                }
                var covered = structure is not null && TerrainReferences.NearStructure(structure, new((float)x, (float)z), 0);
                return Constrain(previous, target, beyond, fill, fill, 2, covered);
            }, op is null);
        b.Nodes.Add(node);
        b.PadChecks.Add(new(feature, region, y, apron, gradeWidth, structure, file, id));
        if (structure is not null) b.Diagnostics.Add($"NOT_VERIFIED: pad edge '{edge!.StructureId}' plan reference resolved; generated vertical collision connections require the content/physics acceptance pass.");
    }

    private static double Median(TerrainProgram beforePads, TerrainRegion region)
    {
        var samples = new List<double>();
        for (var z = (int)Math.Ceiling(region.Bounds.Min.Y); z <= (int)Math.Floor(region.Bounds.Max.Y); z++)
            for (var x = (int)Math.Ceiling(region.Bounds.Min.X); x <= (int)Math.Floor(region.Bounds.Max.X); x++)
                if (region.Contains(x, z)) samples.Add(beforePads.Evaluate(x, z).Surface.Height);
        if (samples.Count == 0)
        {
            // An area with no 1 m lattice sample still needs an interior sample, not its possibly-outside centroid.
            var point = InteriorPoint(region);
            samples.Add(beforePads.Evaluate(point.X, point.Y).Surface.Height);
        }
        samples.Sort(); var middle = samples.Count / 2;
        var median = samples.Count % 2 == 0 ? (samples[middle - 1] + samples[middle]) / 2 : samples[middle];
        return Math.Round(median / 0.05, MidpointRounding.ToEven) * 0.05;
    }

    internal static Vector2 InteriorPoint(TerrainRegion region)
    {
        foreach (var polygon in region.Polygons)
        {
            var rings = new[] { polygon.Outer }.Concat(polygon.Holes).ToArray();
            var levels = rings.SelectMany(ring => ring).Select(p => (double)p.Y).Distinct().OrderBy(z => z).ToArray();
            for (var row = 0; row + 1 < levels.Length; row++)
            {
                var z = (levels[row] + levels[row + 1]) / 2; var cuts = new List<double>();
                foreach (var ring in rings)
                    for (var i = 0; i < ring.Count; i++)
                    {
                        var a = ring[i]; var c = ring[(i + 1) % ring.Count];
                        if ((a.Y > z) == (c.Y > z)) continue;
                        cuts.Add(a.X + ((double)c.X - a.X) * (z - a.Y) / (c.Y - a.Y));
                    }
                cuts.Sort();
                for (var i = 0; i + 1 < cuts.Count; i++)
                {
                    var x = (cuts[i] + cuts[i + 1]) / 2;
                    if (region.Contains(x, z)) return new((float)x, (float)z);
                }
            }
        }
        throw new ArgumentException("Pad footprint has no usable interior sample; its holes or dimensions need correction.");
    }

    // Private compile-time builder. Trial profiles are replaced only while Hard banks are
    // being planned; every published node closes over an entry frozen before validation.
    private sealed class RoadEntry(TerrainRoad road, TerrainRoadProfile profile, TerrainCorridorOp? operation,
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

    private static void AddCorridors(Builder b, IEnumerable<TerrainCorridorOp> operations)
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

    private static List<(double From, double To)> BridgeExclusions(Builder b, TerrainRoad road)
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

    private static RoadEntry AddCorridor(Builder b, TerrainRoad road, TerrainCorridorOp? op, string file, TerrainRoadProfile? automaticProfile = null,
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

    internal readonly record struct CorridorContact(TerrainDifferential Distance, double X, double Z, double Jxx, double Jxz, double Jzz);

    internal static TerrainDifferential CorridorOffsetAt(TerrainRoad road, CorridorContact contact, double x, double z)
    {
        var hit = road.Path.Project(x, z);
        if (contact.Distance.Height <= 0) return new(hit.Offset, hit.OffsetDx, hit.OffsetDz);
        // Use the same continuous exterior extension through both source-end planes.
        // Switching to the nearest core point at a terminal station can jump when the
        // actual footprint cap is not exactly perpendicular to the centreline.
        var sign = hit.Offset < 0 ? -1 : hit.Offset > 0 ? 1 : 0;
        var lateral = new TerrainDifferential(hit.Offset * sign, hit.OffsetDx * sign, hit.OffsetDz * sign) - contact.Distance;
        return lateral.Height <= 0 ? TerrainDifferential.Constant(0) : lateral * sign;
    }

    internal static CorridorContact CorridorContactAt(TerrainRoad road, double shoulder, double x, double z)
    {
        // Source centreline endpoints and their rounded paving polygons join at demo
        // centimetre precision. Exterior shoulders retain their authored geometric width.
        var contactOffset = shoulder == 0 ? TerrainMath.RoadCoreJoinTolerance : shoulder;
        TerrainDifferential distance; double tx; double tz; double curvatureScale;
        if (road.Footprint is { } footprint)
        {
            var sourceDistance = footprint.Distance(x, z);
            distance = sourceDistance - TerrainDifferential.Constant(contactOffset);
            if (distance.Height <= 0) return new(distance, x, z, 1, 0, 1);
            var boundary = footprint.NearestBoundary(x, z); tx = boundary.Tx; tz = boundary.Tz;
            curvatureScale = contactOffset / sourceDistance.Height;
        }
        else
        {
            var hit = road.Path.Project(x, z); var width = road.Width / 2 + contactOffset;
            distance = new(hit.Distance - width, hit.DistanceDx, hit.DistanceDz);
            if (distance.Height <= 0) return new(distance, x, z, 1, 0, 1);
            tx = hit.StationDx; tz = hit.StationDz; curvatureScale = width / hit.Distance;
        }
        var sx = x - distance.Height * distance.Dx; var sz = z - distance.Height * distance.Dz;
        if (tx != 0 || tz != 0) return new(distance, sx, sz, tx * tx, tx * tz, tz * tz);
        // At a convex source vertex the offset shoulder is a circular arc. Differentiate its
        // nearest-point projection, not a newly invented centreline end cap.
        return new(distance, sx, sz, curvatureScale * (1 - distance.Dx * distance.Dx),
            -curvatureScale * distance.Dx * distance.Dz, curvatureScale * (1 - distance.Dz * distance.Dz));
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

    private static TerrainDifferential ZeroBandWeight(Builder b, double x, double z)
    {
        // contract-15 09:32 / 09:45: one common 20 m protected band and 10 m Hermite taper for automatic drainage.
        var inside = -b.Campus.Distance(x, z);
        return ZeroBandWeight(inside);
    }

    internal static TerrainDifferential ZeroBandWeight(TerrainDifferential inside)
    {
        if (inside.Height <= 20) return TerrainDifferential.Constant(0);
        if (inside.Height >= 30) return TerrainDifferential.Constant(1);
        var t = (inside.Height - 20) / 10; var derivative = 6 * t * (1 - t) / 10;
        return new(TerrainMath.Hermite(t), inside.Dx * derivative, inside.Dz * derivative);
    }

    private static void ValidateRoadJunctions(Builder b, IReadOnlyList<RoadEntry> roads)
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
}
