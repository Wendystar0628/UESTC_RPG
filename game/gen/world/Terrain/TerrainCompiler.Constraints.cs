using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

internal static partial class TerrainCompiler
{
    private static void AddDefaultWater(Builder b)
    {
        foreach (var feature in b.Context.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (b.WrittenWater.Contains(feature.Id)) continue;
            var polygons = WorldFeatureKinds.Parts(feature, "water").SelectMany(p => WorldFeatureGeometry.PolygonsOf(p.Geometry)).ToArray();
            if (polygons.Length == 0) continue;
            var defaults = b.Context.Defaults ?? throw new WorldDataException("map.json", "terrainDefaults", "Automatic water constraints require explicit map defaults.");
            var channel = b.References.IsChannelWater(feature);
            var slope = channel ? defaults.ChannelBankGrade : defaults.LakeBankGrade;
            var bank = new TerrainBank(channel ? TerrainBankKind.Hard : TerrainBankKind.Soft,
                "1:" + (1 / slope).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            var region = new TerrainRegion(polygons);
            var underground = b.References.IsUndergroundWater(feature.Id);
            var pending = !underground && channel;
            var shoreTop = underground || pending ? 0 : DefaultShoreTop(b, region, feature.Id);
            AddWater(b, null, "map.json", feature.Id, region, shoreTop - (channel ? 1.2 : 0.5), bank,
                channel ? defaults.ChannelDepth : defaults.LakeDepth, 1d / 7, [], channel);
            if (pending) b.WaterSpecs[^1] = b.WaterSpecs[^1] with { AutoLevelPending = true };
        }
    }

    private static double DefaultShoreTop(Builder b, TerrainRegion region, FeatureId id)
    {
        if (region.Polygons.Length == 0 || region.Polygons[0].Outer.Count == 0)
            throw new WorldDataException("campus.json", id.Value, "Water has no shore-top sample.");
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
            var polygons = WorldFeatureKinds.Parts(feature, "building").SelectMany(p => WorldFeatureGeometry.PolygonsOf(p.Geometry)).ToArray();
            if (polygons.Length == 0) continue;
            var region = new TerrainRegion(polygons);
            AddPad(b, region, feature.Id, TouchesZeroBand(b, region) ? 0 : Median(prePad, region), 0.8, 6, null, null);
        }
    }

    private static void AddPad(Builder b, TerrainRegion region, FeatureId feature, double y, double apron, double gradeWidth,
        TerrainPadEdge? edge, TerrainPadOp? op)
    {
        var file = op is null ? "campus.json" : b.Files[op.Id]; var id = op?.Id ?? "auto-pad-" + feature.Value;
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
        b.PadChecks.Add(new(feature, region, y, gradeWidth, structure, file, id));
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

    private sealed class AutomaticShoulder(double width)
    {
        public double Width { get; private set; } = width;
        public bool FixedBoundaryProfile { get; set; }
        public bool Shrink()
        {
            if (Width == 0) return false;
            Width = Width / 2 < 0.001 ? 0 : Width / 2;
            return true;
        }
    }
    // Private compile-time builder. Trial profiles are replaced only while Hard banks are
    // being planned; every published node closes over an entry frozen before validation.
    private sealed class RoadEntry(TerrainRoad road, TerrainRoadProfile profile, TerrainCorridorOp? operation,
        string file, AutomaticShoulder? grading)
    {
        public TerrainRoad Road { get; } = road;
        public TerrainRoadProfile Profile { get; private set; } = profile;
        public TerrainCorridorOp? Operation { get; } = operation;
        public string File { get; } = file;
        public AutomaticShoulder? Grading { get; } = grading;
        public CorridorPlan? Plan { get; init; }
        public double MaximumCrossFall => Plan is { } plan ? Math.Max(plan.HasGaps ? 0.015 : 0, plan.Operations.Max(op => Math.Abs(op.CrossFall))) : Math.Abs(Operation?.CrossFall ?? 0.015);
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
            BridgeExclusions(b, road), IsMotorRoad(road) ? 0.06 : 1d / 12))
            .Concat(ranged.Select(plan => new TerrainRoadProfile.Request(plan.Road, plan.Exclusions, IsMotorRoad(plan.Road) ? 0.06 : 1d / 12,
                plan.Operations.Select(op => new TerrainRoadProfile.Interval(op.Anchors, b.Files[op.Id], op.Id)).ToArray()))).ToArray(), b.LandBeforeCorridors,
            roads.Select(entry => (entry.Road, entry.Profile)).ToArray(), (x, z) => ZeroBandWeight(b, x, z).Height == 0);
        var profiles = b.RoadFit.Solve();
        foreach (var plan in ranged)
        {
            var op = plan.Operations[0];
            if (plan.HasGaps && b.Context.Defaults is null)
                throw new WorldDataException("map.json", "terrainDefaults", "Automatic road gaps require explicit map defaults.");
            var entry = new RoadEntry(plan.Road, profiles[plan.Road.Source], op, b.Files[op.Id], null) { Plan = plan };
            foreach (var interval in plan.Operations) AddCorridor(b, plan.Road, interval, b.Files[interval.Id], sharedEntry: entry);
            if (plan.HasGaps)
            {
                AddCorridor(b, plan.Road, null, "network.json", sharedEntry: entry);
                b.AutomaticRoads.Add("auto-corridor-" + plan.Road.Feature.Value, entry);
            }
            roads.Add(entry);
        }
        foreach (var road in automatic)
        {
            if (!b.WrittenRoads.Add(road.Source)) continue;
            var entry = AddCorridor(b, road, null, "network.json", profiles[road.Source]); roads.Add(entry);
            b.AutomaticRoads.Add("auto-corridor-" + road.Feature.Value, entry);
        }
        ValidateRoadJunctions(roads);
        b.Roads.AddRange(roads);
    }

    private static bool IsMotorRoad(TerrainRoad road) => road.Kind is "motorway" or "trunk" or "primary" or "secondary" or "tertiary" or "residential" or "service";

    private static List<(double From, double To)> BridgeExclusions(Builder b, TerrainRoad road)
    {
        var exclusions = new List<(double From, double To)>();
        foreach (var bridge in b.Context.Network.Bridges)
        {
            var stations = bridge.Line.Points.Select(p => road.Path.Project(p.X, p.Y)).ToArray();
            if (stations.Length >= 2 && stations.All(hit => hit.Distance <= 0.02)) exclusions.Add((stations.Min(hit => hit.Station), stations.Max(hit => hit.Station)));
        }
        return exclusions;
    }

    private static RoadEntry AddCorridor(Builder b, TerrainRoad road, TerrainCorridorOp? op, string file, TerrainRoadProfile? automaticProfile = null,
        RoadEntry? sharedEntry = null, IReadOnlyList<(double From, double To)>? exclusionsOverride = null)
    {
        var id = op?.Id ?? "auto-corridor-" + road.Feature.Value;
        var exclusions = exclusionsOverride ?? BridgeExclusions(b, road);
        var motor = IsMotorRoad(road);
        var maximumGrade = motor ? 0.06 : 1d / 12;
        var profile = sharedEntry?.Profile ?? automaticProfile ?? (op is null
            ? throw new InvalidOperationException("Automatic roads require the completed shared network profile solve.")
            : new TerrainRoadProfile(op.Anchors, road.Path.Length, exclusions, maximumGrade, file, id));
        var shoulder = op?.Shoulder ?? b.Context.Defaults?.CorridorShoulder
            ?? throw new WorldDataException("map.json", "terrainDefaults", "Automatic road constraints require explicit map defaults.");
        var grading = op is null ? sharedEntry?.Grading ?? new AutomaticShoulder(shoulder) : null;
        var width = road.Width / 2 + (sharedEntry?.Plan is { } planWidth ? Math.Max(shoulder, planWidth.Operations.Max(operation => operation.Shoulder)) : shoulder);
        var crossMagnitude = sharedEntry?.MaximumCrossFall ?? Math.Abs(op?.CrossFall ?? 0.015);
        var reach = width + (b.RangeBound + TerrainSampling.MaximumHeight - TerrainSampling.MinimumHeight + crossMagnitude * width) / (1d / 3 - crossMagnitude) + 4;
        var support = TerrainRegion.Expand(road.Path.Bounds, reach);
        var patches = PathPatches(road.Path, reach);
        var entry = sharedEntry ?? new RoadEntry(road, profile, op, file, grading);
        b.Nodes.Add(new(id, file, op?.Zone ?? default, TerrainGroup.Corridor, op?.Priority ?? 50, support, (previous, x, z) =>
        {
            if (op is null && previous.BoundaryWeight is { Height: 0 } && previous.Surface == TerrainDifferential.Constant(0)) return previous;
            var pointHit = road.Path.Project(x, z);
            if (entry.Plan is { } plan && plan.At(pointHit.Station)?.Id != op?.Id) return previous;
            var parameters = entry.Plan?.Parameters(pointHit, shoulder);
            var widthAt = parameters?.Shoulder ?? TerrainDifferential.Constant(grading?.Width ?? shoulder);
            var contact = CorridorContactAt(road, widthAt.Height, x, z);
            contact = contact with { Distance = contact.Distance - new TerrainDifferential(0, widthAt.Dx, widthAt.Dz) };
            var hit = road.Path.Project(contact.X, contact.Z);
            if (entry.Profile.Excluded(hit.Station)) return previous;
            var longitudinal = entry.Profile.SurfaceAt(road.Path, x, z, road.Width, road.Path.Project(x, z));
            var cross = parameters?.Cross ?? TerrainDifferential.Constant(op?.CrossFall ?? 0.015);
            if (op is null)
            {
                cross = TerrainDifferential.Product(previous.BoundaryWeight ?? ZeroBandWeight(b, x, z), cross);
            }
            var lateral = CorridorOffsetAt(road, contact, x, z);
            var offset = motor ? lateral * (lateral.Height > 0 ? -1 : lateral.Height < 0 ? 1 : 0) : lateral;
            var target = new TerrainDifferential(longitudinal.Height + cross.Height * offset.Height,
                longitudinal.Dx + cross.Height * offset.Dx + cross.Dx * offset.Height,
                longitudinal.Dz + cross.Height * offset.Dz + cross.Dz * offset.Height);
            var proposed = Constrain(previous, target, contact.Distance, 1d / 3, 1d / 3, 2);
            if (Math.Abs(proposed.Surface.Height - previous.Surface.Height) > TerrainMath.HeightTolerance
                && (op is not null || CorridorContactAt(road, 0, x, z).Distance.Height < 0))
            {
                foreach (var water in b.WaterShores)
                    if (water.Region.Distance(x, z).Height < -TerrainMath.HeightTolerance)
                        throw new WorldDataException(file, id,
                            $"Road core/author constraint changes registered surface-water bed '{water.Id}' without a real bridge/underpass exclusion at ({x:R},{z:R}): previousH={previous.Surface.Height:R}, proposedH={proposed.Surface.Height:R}. Smoothing does not authorize filling a channel or inventing a bridge.");
            }
            return op is null ? ConstrainAutomaticRoadAtWater(b, road, file, id, previous, proposed, x, z) : proposed;
        }, op is null)
        {
            IndexPatches = patches,
            ExplainPoint = (x, z) =>
            {
                var hit = road.Path.Project(x, z); var center = road.Path.At(hit.Station);
                var contact = CorridorContactAt(road, grading?.Width ?? shoulder, x, z); var sx = contact.X; var sz = contact.Z;
                var oldShoulder = b.LandBeforeCorridors.Evaluate(sx, sz).Surface.Height;
                var shoulderHit = road.Path.Project(sx, sz);
                var shoulderOffset = motor ? -Math.Abs(shoulderHit.Offset) : shoulderHit.Offset;
                var edgeTarget = entry.Profile.At(hit.Station).Y + (op?.CrossFall ?? ZeroBandWeight(b, sx, sz).Height * 0.015) * shoulderOffset;
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
        TerrainDifferential distance; double tx; double tz; double curvatureScale;
        if (road.Footprint is { } footprint)
        {
            var sourceDistance = footprint.Distance(x, z);
            distance = sourceDistance - TerrainDifferential.Constant(shoulder);
            if (distance.Height <= 0) return new(distance, x, z, 1, 0, 1);
            var boundary = footprint.NearestBoundary(x, z); tx = boundary.Tx; tz = boundary.Tz;
            curvatureScale = shoulder / sourceDistance.Height;
        }
        else
        {
            var hit = road.Path.Project(x, z); var width = road.Width / 2 + shoulder;
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

    private static bool TouchesZeroBand(Builder b, TerrainRegion region)
    {
        foreach (var polygon in region.Polygons)
            for (var i = 0; i < polygon.Outer.Count; i++)
            {
                var a = polygon.Outer[i]; var c = polygon.Outer[(i + 1) % polygon.Outer.Count];
                var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
                var count = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dz * dz)));
                for (var j = 0; j <= count; j++)
                    if (-b.Campus.Distance(a.X + dx * j / count, a.Y + dz * j / count).Height <= 20) return true;
            }
        return false;
    }

    private static void ValidateRoadJunctions(IReadOnlyList<RoadEntry> roads)
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
                if (Math.Abs(road.Profile.At(station).Y - first) > TerrainRoadProfile.JunctionHeightTolerance)
                    throw new WorldDataException(road.File, road.Operation?.Id ?? road.Road.Feature.Value, "Road centreline junction has inconsistent elevations.");
                for (var delta = -10; delta <= 10; delta++)
                {
                    var sample = Math.Max(0, Math.Min(road.Profile.Length, station + delta));
                    if (!road.Profile.Excluded(sample) && Math.Abs(road.Profile.At(sample).Grade) > 0.03 + TerrainMath.GradeTolerance)
                        throw new WorldDataException(road.File, road.Operation?.Id ?? road.Road.Feature.Value,
                            $"Road profile exceeds 3% within 10 m of a junction: node=({node.Key.X},{node.Key.Z}) mm, station={station:R}, sample={sample:R}, height={road.Profile.At(sample).Y:R}, grade={road.Profile.At(sample).Grade:R}.");
                }
            }
        }
    }
}
