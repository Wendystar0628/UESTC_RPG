using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

internal static partial class TerrainCompiler
{
    private static void Channel(Builder b, TerrainChannelOp op, string file)
    {
        if (op.Wade.Count != 0)
            throw new WorldDataException(file, op.Id, "Unsupported channel Wade: wading profiles are not implemented by this terrain compiler.");
        if (op.Dry) { DryChannel(b, op, file); return; }
        var water = op.Water!.Value;
        var region = b.References.Area(water, "water", file, op.Id);
        var (path, width) = op.Line is { } explicitLine ? (new TerrainPath(explicitLine), op.Width!.Value)
            : b.References.WaterLine(water, file, op.Id);
        foreach (var weir in op.Weirs) if (weir.S > path.Length) throw new WorldDataException(file, op.Id, "Weir station exceeds the actual channel length.");
        if (b.References.IsUndergroundWater(water))
        {
            AddWater(b, op, file, water, region, op.WaterY!.Value, op.Bank, op.Depth, 1d / 7, [], true);
            return;
        }
        var sideGrade = TerrainReferences.Grade(op.Bank.Slope!);
        b.Nodes.Add(new(op.Id, file, op.Zone, TerrainGroup.Linear, op.Priority,
            ConstraintBounds(b, TerrainRegion.Expand(path.Bounds, width / 2), sideGrade, sideGrade, 2), (previous, x, z) =>
            {
                if (region.Distance(x, z).Height >= 0) return previous;
                var hit = path.Project(x, z);
                return Constrain(previous, TerrainDifferential.Constant(op.WaterY!.Value - op.Depth),
                    new(hit.Distance - width / 2, hit.DistanceDx, hit.DistanceDz), sideGrade, sideGrade, 2, op.Bank.Kind is TerrainBankKind.Hard or TerrainBankKind.Steps);
            }, WaterBed: true));
        AddWater(b, op, file, water, region, op.WaterY!.Value, op.Bank, op.Depth, 1d / 7, [], true);
    }

    private static void Lake(Builder b, TerrainLakeOp op, string file) =>
        AddWater(b, op, file, op.Water, b.References.Area(op.Water, "water", file, op.Id), op.WaterY, op.Bank,
            op.Bed.MaxDepth, TerrainReferences.Grade(op.Bed.Shelf), op.Wade.Select(p => new TerrainRegion([p])).ToArray(), false);

    private static void AddWater(Builder b, TerrainOp? op, string file, FeatureId id, TerrainRegion region, double level,
        TerrainBank bank, double depth, double shelf, TerrainRegion[] wade, bool channel)
    {
        var name = op?.Id ?? "auto-water-" + id.Value;
        if (!b.WrittenWater.Add(id)) throw new WorldDataException(file, name, $"Multiple terrain operations define water segment '{id.Value}'.");
        if (b.References.IsUndergroundWater(id))
        {
            b.UndergroundWaterSpecs.Add(new(op, file, id, region, level, bank, depth, shelf, wade, channel));
            return;
        }
        if (level - depth < -4.5) throw new WorldDataException(file, name, "Water bed would lie below -4.5 m.");
        var grade = TerrainReferences.Grade(bank.Slope!);
        if (bank.Kind == TerrainBankKind.Steps)
            throw new WorldDataException(file, name, "Steps shore requires the real stair nosing profile and submerged landing; the current input has no structural profile reference. It cannot be replaced by a generic ramp.");
        if (grade > 1) throw new WorldDataException(file, name, "Water profile exceeds the maximum 1:1 terrain grade.");
        foreach (var band in wade)
            foreach (var polygon in band.Polygons)
                foreach (var p in polygon.Outer)
                    if (!region.Contains(p.X, p.Y)) throw new WorldDataException(file, name, "Wade area extends outside the actual water footprint or into a hole.");
        b.WaterSpecs.Add(new(op, file, id, region, level, bank, depth, shelf, wade, channel));
    }

    private static TerrainNode WaterNode(Builder b, WaterSpec spec, TerrainRegion region, TerrainBridgeBanks? bridgeBanks,
        bool reserveTransitionGrade = false)
    {
        var (op, file, id, _, level, bank, depth, shelf, wade, _) = spec;
        var name = op?.Id ?? "auto-water-" + id.Value;
        var grade = TerrainReferences.Grade(bank.Slope!);
        var zone = op?.Zone ?? default; var priority = op?.Priority ?? 50;
        var support = TerrainRegion.Expand(region.Bounds, (b.RangeBound + Math.Abs(level) + depth) / grade + 4);
        return new(name, file, zone, TerrainGroup.Water, priority, support, (previous, x, z) =>
        {
            if (bank.Kind == TerrainBankKind.Hard && previous.Surface.Height >= level + 0.3 && !region.Contains(x, z)) return previous;
            if (bank.Kind is TerrainBankKind.Soft or TerrainBankKind.Wetland && (bridgeBanks?.RoadSpans.Count ?? 0) == 0
                && level + 0.3 <= 0 && previous.Surface == TerrainDifferential.Constant(0))
            {
                var dx = Math.Max(0, Math.Max(region.Bounds.Min.X - x, x - region.Bounds.Max.X));
                var dz = Math.Max(0, Math.Max(region.Bounds.Min.Y - z, z - region.Bounds.Max.Y));
                var reach = Math.Max(0, -level / grade);
                if (dx * dx + dz * dz > reach * reach) return previous;
            }
            var d = region.Distance(x, z);
            TerrainDifferential target;
            switch (bank.Kind)
            {
                case TerrainBankKind.Soft:
                case TerrainBankKind.Wetland:
                    if (d.Height <= 0)
                    {
                        var inward = -d;
                        var bed = SoftDepth(inward, depth, shelf, false, bank.Kind == TerrainBankKind.Wetland ? grade : 0.15);
                        target = TerrainDifferential.Constant(level) - bed;
                        foreach (var band in wade)
                        {
                            var toBand = band.Distance(x, z);
                            var shallow = TerrainDifferential.Constant(level) - SoftDepth(inward, depth, shelf, true);
                            if (toBand.Height <= 0) target = shallow;
                            else
                            {
                                // Continue the shallow band into its surrounding bed with the prescribed 1:6 slope.
                                var envelope = shallow - toBand * (1d / 6);
                                if (envelope.Height > target.Height) target = envelope;
                            }
                        }
                    }
                    else target = SoftOuterBank(previous.Surface, d, level, grade);
                    if (bridgeBanks is not null)
                    {
                        TerrainDifferential? originalTop = null;
                        if (bridgeBanks.RoadSpans.Count > 0)
                        {
                            var shore = region.NearestBoundary(x, z); var value = b.LandBeforeWater.Evaluate(shore.X, shore.Z).Surface;
                            if (value.Height < level + 0.3) value = TerrainDifferential.Constant(level + 0.3);
                            var along = value.Dx * shore.Tx + value.Dz * shore.Tz;
                            originalTop = new(value.Height, along * shore.Tx, along * shore.Tz);
                        }
                        var resolved = bridgeBanks.ProfileAt(x, z, originalTop);
                        var weight = resolved.Weight;
                        if (weight.Height > 0)
                        {
                            var hard = HardWaterSurface(b, region, d, previous.Surface, x, z, level, depth, grade, file, name, resolved.Top, reserveTransitionGrade,
                                bridgeBanks, originalTop);
                            var delta = hard - target;
                            target += new TerrainDifferential(delta.Height * weight.Height,
                                delta.Dx * weight.Height + delta.Height * weight.Dx,
                                delta.Dz * weight.Height + delta.Height * weight.Dz);
                            return WithSurface(previous, target, d.Height, -d.Height, d.Height < 0);
                        }
                    }
                    return WithSurface(previous, target, double.PositiveInfinity, -d.Height);
                case TerrainBankKind.Hard:
                {
                    // contract-15 09:38: preserve the land; the covered 1:1 transition is entirely waterward.
                    if (d.Height >= 0) return WithSurface(previous, RaiseBankTop(previous.Surface, d, level + 0.3), d.Height, double.PositiveInfinity);
                    target = HardWaterSurface(b, region, d, previous.Surface, x, z, level, depth, grade, file, name, reserveTransitionGrade: reserveTransitionGrade);
                    return WithSurface(previous, target, d.Height, double.PositiveInfinity, true);
                }
                case TerrainBankKind.Steps: throw new WorldDataException(file, name, "Missing real stepped-bank profile.");
                default: throw new WorldDataException(file, name, "A wet water segment cannot use a swale bank.");
            }
        }, op is null);
    }

    private static TerrainDifferential HardWaterSurface(Builder b, TerrainRegion region, TerrainDifferential distance,
        TerrainDifferential landSurface, double x, double z, double level, double depth, double grade, string file, string name,
        TerrainDifferential? roadTop = null, bool reserveTransitionGrade = false,
        TerrainBridgeBanks? bridgeBanks = null, TerrainDifferential? originalBankTop = null)
    {
        if (roadTop is { } declaredTop && declaredTop.Height < level + 0.3 - 0.000001)
            throw new WorldDataException(file, name,
                $"Resolved footway bank top {declaredTop.Height:R} is below the fixed water level plus 0.3 m freeboard at ({x:R},{z:R}): waterY={level:R}, requiredBankTop={level + 0.3:R}, finalMixedTop={declaredTop}, originalBankTop={originalBankTop}, deficit={level + 0.3 - declaredTop.Height:R}. "
                + (bridgeBanks?.ExplainProfileAt(x, z) ?? "No resolved footway span is available for this profile."));
        if (distance.Height >= 0) return roadTop is { } upper
            ? Constrain(new(landSurface, TerrainClass.Flat), upper, distance, 1d / 3, 1d / 3, 2).Surface
            : RaiseBankTop(landSurface, distance, level + 0.3);
        var shore = region.NearestBoundary(x, z);
        var land = roadTop ?? b.LandBeforeWater.Evaluate(shore.X, shore.Z).Surface;
        if (land.Height < level + 0.3) land = TerrainDifferential.Constant(level + 0.3);
        var tangentGrade = land.Dx * shore.Tx + land.Dz * shore.Tz;
        var top = new TerrainDifferential(land.Height, tangentGrade * shore.Tx, tangentGrade * shore.Tz);
        var foot = Math.Max(level - depth, level - 0.8); var inward = -distance;
        var drop = top - TerrainDifferential.Constant(foot);
        // A sloping road top contributes an independent along-shore derivative. Reserve the
        // existing 1:2 land-grade budget for it, leaving sqrt(1-(1/2)^2) waterward so the full
        // vector remains within 1:1. This changes no endpoint/depth and adds no design slope.
        var normalGrade = roadTop is null && !reserveTransitionGrade ? 1 : Math.Sqrt(1 - 0.5 * 0.5);
        if (roadTop is not null && top.Dx * top.Dx + top.Dz * top.Dz > 0.25 + 0.000001)
            throw new WorldDataException(file, name, "Resolved road bank top exceeds the available along-shore grade budget.");
        var footDistance = drop * (1 / normalGrade);
        if (inward.Height < footDistance.Height) return top - inward * normalGrade;
        var target = TerrainDifferential.Constant(foot) - (inward - footDistance) * grade;
        return target.Height < level - depth ? TerrainDifferential.Constant(level - depth) : target;
    }

    private static TerrainDifferential RaiseBankTop(TerrainDifferential existing, TerrainDifferential distance, double top)
    {
        if (existing.Height >= top) return existing;
        var envelope = TerrainDifferential.Constant(top) - TerrainMath.FilletDistance(distance, 2) * 0.25;
        var raised = TerrainMath.SmoothMaximum(existing, envelope, 0.5);
        return raised.Height > top ? TerrainDifferential.Constant(top) : raised;
    }

    private static TerrainDifferential SoftOuterBank(TerrainDifferential existing, TerrainDifferential distance, double level, double grade)
    {
        var ramp = TerrainDifferential.Constant(level) + distance * grade;
        var original = ramp.Height < existing.Height ? ramp : existing;
        var top = level + 0.3; // Opus 12:50: freeboard is at the landward bank top, not at the waterline.
        if (original.Height >= top) return original;
        var dropDistance = 0.3 / grade;
        var topDistance = dropDistance < 1 ? Math.Sqrt(4 * dropDistance) : dropDistance + 1;
        TerrainDifferential lower;
        if (distance.Height <= topDistance)
            lower = TerrainDifferential.Constant(top) - TerrainMath.FilletDistance(TerrainDifferential.Constant(topDistance) - distance, 2) * grade;
        else lower = TerrainDifferential.Constant(top) - TerrainMath.FilletDistance(distance - TerrainDifferential.Constant(topDistance), 2) * 0.25;
        var raised = TerrainMath.SmoothMaximum(original, lower, 0.5);
        if (raised.Height > top) raised = TerrainDifferential.Constant(top);
        if (raised.Height > ramp.Height) raised = ramp;
        return raised.Height > original.Height ? raised : original;
    }

    private static TerrainDifferential SoftDepth(TerrainDifferential distance, double maximum, double shelf, bool wade, double ordinaryGrade = 0.15)
    {
        // contract-15 07:10: these are explicit design constants, not an epsilon used to hide a shore mismatch.
        var shoreGrade = wade ? 0.08 : ordinaryGrade;
        TerrainDifferential depth;
        if (distance.Height <= 0.25) depth = distance * ((0.05 + shoreGrade * 0.25) / 0.25);
        else
        {
            var shelfStart = wade ? 3 : (0.3 - 0.05) / shoreGrade;
            depth = distance.Height <= shelfStart ? TerrainDifferential.Constant(0.05) + distance * shoreGrade
                : TerrainDifferential.Constant(0.05 + shoreGrade * shelfStart) + (distance - TerrainDifferential.Constant(shelfStart)) * shelf;
        }
        return depth.Height >= maximum ? TerrainDifferential.Constant(maximum) : depth;
    }

    private static void DryChannel(Builder b, TerrainChannelOp op, string file)
    {
        var path = new TerrainPath(op.Line!); var half = op.Width!.Value / 2;
        var inner = TerrainReferences.Grade(op.Bank.Inner!);
        var outer = op.Bank.Outer is null ? (double?)null : TerrainReferences.Grade(op.Bank.Outer);
        if (inner > 0.5 || outer is > 0.5) throw new WorldDataException(file, op.Id, "Dry swale banks exceed the 1:2 soil-grade limit.");
        var radius = half + op.Depth / inner;
        var end = path.Length;
        (Vector2 Center, double Tx, double Tz, double Radius)? terminal = null;
        if (op.Crossings.OfType<TerrainEndBeforeCrossing>().Count() > 1)
            throw new WorldDataException(file, op.Id, "A dry channel can have only one terminal endBefore road.");
        var underpasses = new List<(TerrainUnderpassCrossing Input, double Station)>();
        var underpassRoads = new List<TerrainRoad>();
        foreach (var crossing in op.Crossings)
        {
            var road = b.References.Road(crossing.Road, file, op.Id);
            switch (crossing)
            {
                case TerrainUnderpassCrossing underpass:
                {
                    var candidates = path.Intersections(road.Path).OrderBy(p => (p.X - underpass.At.X) * (p.X - underpass.At.X)
                        + (p.Z - underpass.At.Y) * (p.Z - underpass.At.Y)).ThenBy(p => p.Station).ToArray();
                    var dx = candidates.Length == 0 ? double.PositiveInfinity : candidates[0].X - underpass.At.X;
                    var dz = candidates.Length == 0 ? double.PositiveInfinity : candidates[0].Z - underpass.At.Y;
                    if (candidates.Length == 0 || Math.Sqrt(dx * dx + dz * dz) > TerrainCrossingRules.IntersectionToleranceMetres)
                        throw new WorldDataException(file, op.Id, $"Crossing '{crossing.Road.Value}' has no real centreline intersection within 1 m of at=({underpass.At.X:R},{underpass.At.Y:R}); nearestActualIntersection={(candidates.Length == 0 ? "none" : $"({candidates[0].X:R},{candidates[0].Z:R})")}, distance={Math.Sqrt(dx * dx + dz * dz):R}.");
                    underpasses.Add((underpass, candidates[0].Station)); underpassRoads.Add(road); break;
                }
                case TerrainEndBeforeCrossing:
                {
                    // The whole circle, hence its forward semicircle, stays 1 m beyond the actual
                    // road edge even at an oblique intersection. The centre is another radius away.
                    var centreStation = FindRoadClearanceEntry(path, road.Path,
                        road.Width / 2 + TerrainCrossingRules.EndBeforeRoadEdgeClearanceMetres + radius, file, op.Id);
                    end = centreStation + radius;
                    if (end > path.Length || end <= TerrainCrossingRules.EndBeforeRecoveryMetres)
                        throw new WorldDataException(file, op.Id, "Source channel has insufficient actual length for its complete semicircular cap and 2 m recovery.");
                    var center = path.At(centreStation); var segment = path.Project(center.X, center.Y).Segment;
                    var a = path.Line.Points[segment]; var c = path.Line.Points[segment + 1];
                    var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y; var length = Math.Sqrt(dx * dx + dz * dz);
                    terminal = (center, dx / length, dz / length, radius);
                    break;
                }
                default: throw new WorldDataException(file, op.Id, "Unknown dry-channel crossing type.");
            }
        }
        if (underpasses.Any(p => p.Station >= end)) throw new WorldDataException(file, op.Id, "An underpass lies beyond the declared end-before termination.");
        var prefix = new TerrainProgram(b.Nodes);
        var reach = radius + (outer is { } g ? b.RangeBound / g : 0);
        var support = TerrainRegion.Expand(path.Bounds, reach);
        var recoveryEnd = end;
        b.Nodes.Add(new(op.Id, file, op.Zone, TerrainGroup.Linear, op.Priority, support, (previous, x, z) =>
        {
            // An explicitly resolved underpass is below the actual road core. It must not
            // excavate the surface used as the road's default profile target.
            if (underpassRoads.Any(road => CorridorContactAt(road, 0, x, z).Distance.Height <= 0)) return previous;
            var hit = path.Project(x, z);
            if (hit.Station > recoveryEnd || hit.Distance > reach) return previous;
            var center = path.At(hit.Station); var centerValue = prefix.Evaluate(center.X, center.Y).Surface;
            var baseHeight = new TerrainDifferential(centerValue.Height,
                (centerValue.Dx * hit.StationDx + centerValue.Dz * hit.StationDz) * hit.StationDx,
                (centerValue.Dx * hit.StationDx + centerValue.Dz * hit.StationDz) * hit.StationDz);
            var depth = TerrainDifferential.Constant(op.Depth);
            var beyondBottom = new TerrainDifferential(hit.Distance - half, hit.DistanceDx, hit.DistanceDz);
            TerrainDifferential surface; double crest; double toe;
            if (beyondBottom.Height <= 0) { surface = baseHeight - depth; crest = double.PositiveInfinity; toe = half - hit.Distance; }
            else
            {
                surface = baseHeight - depth + beyondBottom * inner; crest = beyondBottom.Height - depth.Height / inner; toe = -beyondBottom.Height;
                if (surface.Height > baseHeight.Height)
                {
                    if (outer is not { } outerGrade) return previous;
                    var exteriorDistance = beyondBottom - depth * (1 / inner);
                    surface = baseHeight - exteriorDistance * outerGrade;
                    if (surface.Height <= previous.Surface.Height) return previous;
                    crest = exteriorDistance.Height; toe = double.PositiveInfinity;
                }
            }
            if (terminal is { } cap)
            {
                var distance = EndCapDistance(x, z, cap.Center, cap.Tx, cap.Tz, cap.Radius);
                if (distance.Height <= 0) return previous;
                if (distance.Height < TerrainCrossingRules.EndBeforeRecoveryMetres)
                {
                    // Root engineering ruling: 2 m is horizontal distance to the terminating arc.
                    // Intersect the original section with the shallower recovery envelope; do not
                    // differentiate sqrt(radius^2-offset^2), whose flank derivative is unbounded.
                    var envelope = previous.Surface - distance * (op.Depth / TerrainCrossingRules.EndBeforeRecoveryMetres);
                    if (surface.Height < envelope.Height) surface = envelope;
                    if (surface.Height > previous.Surface.Height)
                    {
                        var factor = distance * (1 / TerrainCrossingRules.EndBeforeRecoveryMetres); var difference = surface - previous.Surface;
                        surface = previous.Surface + new TerrainDifferential(difference.Height * factor.Height,
                            difference.Dx * factor.Height + difference.Height * factor.Dx, difference.Dz * factor.Height + difference.Height * factor.Dz);
                    }
                }
            }
            return WithSurface(previous, surface, crest, toe);
        }));
    }

    internal static TerrainDifferential EndCapDistance(double x, double z, Vector2 center, double tx, double tz, double radius)
    {
        var dx = x - center.X; var dz = z - center.Y; var along = dx * tx + dz * tz; var lateral = -dx * tz + dz * tx;
        if (along >= 0)
        {
            var distance = Math.Sqrt(dx * dx + dz * dz);
            return distance == 0 ? TerrainDifferential.Constant(radius) : new(radius - distance, -dx / distance, -dz / distance);
        }
        // Behind the diameter, the closest terminal-arc point is an endpoint of that diameter.
        var across = radius - Math.Abs(lateral); var length = Math.Sqrt(along * along + across * across);
        if (length == 0) return TerrainDifferential.Constant(0);
        var sign = lateral > 0 ? 1d : lateral < 0 ? -1d : 0d;
        return new(length, (along * tx + across * sign * tz) / length,
            (along * tz - across * sign * tx) / length);
    }

    private static double FindRoadClearanceEntry(TerrainPath path, TerrainPath road, double radius, string file, string op)
    {
        if (road.Project(path.Line.Points[0].X, path.Line.Points[0].Y).Distance <= radius)
            throw new WorldDataException(file, op, "End-before channel begins inside the road-clearance band.");
        var candidates = new List<double>(); var station = 0d;
        for (var i = 0; i < path.Line.Points.Count - 1; i++)
        {
            var a = path.Line.Points[i]; var b = path.Line.Points[i + 1];
            var dx = (double)b.X - a.X; var dz = (double)b.Y - a.Y; var length = Math.Sqrt(dx * dx + dz * dz);
            for (var j = 0; j < road.Line.Points.Count - 1; j++)
            {
                var c = road.Line.Points[j]; var d = road.Line.Points[j + 1];
                var rx = (double)d.X - c.X; var rz = (double)d.Y - c.Y; var roadLength = Math.Sqrt(rx * rx + rz * rz);
                var tx = rx / roadLength; var tz = rz / roadLength;
                var offset = -tz * (a.X - c.X) + tx * (a.Y - c.Y); var rate = -tz * dx + tx * dz;
                if (rate != 0)
                    foreach (var side in new[] { -radius, radius })
                    {
                        var t = (side - offset) / rate;
                        var along = tx * (a.X + t * dx - c.X) + tz * (a.Y + t * dz - c.Y);
                        if (t >= 0 && t <= 1 && along >= 0 && along <= roadLength) candidates.Add(station + t * length);
                    }
                foreach (var end in new[] { c, d })
                {
                    var qx = (double)a.X - end.X; var qz = (double)a.Y - end.Y;
                    var aa = dx * dx + dz * dz; var bb = 2 * (qx * dx + qz * dz); var cc = qx * qx + qz * qz - radius * radius;
                    var discriminant = bb * bb - 4 * aa * cc;
                    if (discriminant < 0) continue;
                    var root = Math.Sqrt(discriminant);
                    foreach (var t in new[] { (-bb - root) / (2 * aa), (-bb + root) / (2 * aa) })
                        if (t >= 0 && t <= 1) candidates.Add(station + t * length);
                }
            }
            station += length;
        }
        foreach (var candidate in candidates.OrderBy(value => value))
        {
            var point = path.At(candidate);
            if (Math.Abs(road.Project(point.X, point.Y).Distance - radius) <= 0.001) return candidate;
        }
        throw new WorldDataException(file, op, "End-before channel does not reach the actual named road's clearance boundary; endpoint extension is not inferred.");
    }

    private static void ValidateWeirs(Builder b, IEnumerable<TerrainChannelOp> channels)
    {
        var levels = b.Water.ToDictionary(w => w.Feature, w => w.Level);
        foreach (var channel in channels)
            foreach (var weir in channel.Weirs)
            {
                var file = b.Files[channel.Id];
                if (!levels.TryGetValue(weir.Upstream, out var upper) || !levels.TryGetValue(weir.Downstream, out var lower)
                    || Math.Abs(upper - lower - weir.Drop) > 0.0001)
                    throw new WorldDataException(file, channel.Id, "Weir drop does not match two actual constant water-level segments.");
                var kind = weir.Kind switch { TerrainWeirKind.Weir => "weir", TerrainWeirKind.Gate => "gate", TerrainWeirKind.Culvert => "culvert", _ => throw new ArgumentException("Unknown weir kind.") };
                var structure = b.References.Structure(weir.Structure, file, channel.Id, kind);
                var path = channel.Line is { } line ? new TerrainPath(line) : b.References.WaterLine(channel.Water!.Value, file, channel.Id).Path;
                if (!TerrainReferences.NearStructure(structure, path.At(weir.S), 0.02))
                    throw new WorldDataException(file, channel.Id, "Weir station is not on its named structural footprint/alignment.");
                foreach (var water in new[] { weir.Upstream, weir.Downstream })
                    if (!TerrainReferences.TouchesRegion(structure, b.Water.Single(w => w.Feature == water).Region, 0.02))
                        throw new WorldDataException(file, channel.Id, "Weir structure is not connected to both named water segments.");
            }
    }
}
