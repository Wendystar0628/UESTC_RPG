using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Water;
using static Campus.Gen.World.TerrainFieldGrading;
using static Campus.Gen.World.TerrainWaterSources;

namespace Campus.Gen.World;

internal static class TerrainWaterProfiles
{
    internal static TerrainNode WaterNode(TerrainBuildState b, WaterSpec spec, TerrainRegion region, TerrainBridgeBanks? bridgeBanks,
        bool reserveTransitionGrade = false)
    {
        var (op, file, id, _, level, bank, depth, shelf, wade, _) = spec;
        var name = op?.Id ?? "auto-water-" + id.Value;
        if (WaterPoolLayout.IsPool(b.Context.Features[id])) return PoolNode(b, spec, region);
        var grade = TerrainReferences.Grade(bank.Slope!);
        var softProfile = bank.Kind is TerrainBankKind.Soft or TerrainBankKind.Wetland
            ? new SoftBankProfile(b, spec, region, bridgeBanks, reserveTransitionGrade, name, grade) : null;
        var zone = op?.Zone ?? default; var priority = op?.Priority ?? 50;
        var support = TerrainRegion.Expand(region.Bounds, (b.RangeBound + Math.Abs(level) + depth) / grade + 4);
        var node = new TerrainNode(name, file, zone, TerrainGroup.Water, priority, support, (previous, x, z) =>
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
                    return softProfile!.Evaluate(previous, d, x, z);
                case TerrainBankKind.Hard:
                {
                    // contract-15 09:38: preserve the land; the covered 1:1 transition is entirely waterward.
                    if (d.Height >= 0) return WithSurface(previous, RaiseBankTop(previous.Surface, d, level + 0.3), d.Height, double.PositiveInfinity);
                    target = HardWaterSurface(b, region, d, previous.Surface, x, z, level, depth, grade, file, name, reserveTransitionGrade: reserveTransitionGrade);
                    target = WadeSurface(target, -d, level, depth, shelf, wade, x, z);
                    return WithSurface(previous, target, d.Height, double.PositiveInfinity, true);
                }
                case TerrainBankKind.Steps: throw new WorldDataException(file, name, "Missing real stepped-bank profile.");
                default: throw new WorldDataException(file, name, "A wet water segment cannot use a swale bank.");
            }
        }, op is null);
        if (spec.Channel) return node;
        return node with
        {
            Apply = (previous, x, z) =>
            {
                var value = node.Apply(previous, x, z);
                if (region.Contains(x, z)) RequireZeroBandCore(b, value.Surface, x, z, file, name);
                return value;
            }
        };
    }

    private static TerrainNode PoolNode(TerrainBuildState b, WaterSpec spec, TerrainRegion region)
    {
        var (op, file, id, _, level, _, _, _, _, _) = spec;
        var name = op?.Id ?? "auto-water-" + id.Value;
        var frame = WaterPoolLayout.Frame(b.Context.Features[id]);
        return new TerrainNode(name, file, op?.Zone ?? default, TerrainGroup.Water, op?.Priority ?? 50,
            region.Bounds, (previous, x, z) =>
            {
                if (!region.Contains(x, z)) return previous;
                var along = frame.Coordinates(new((float)x, (float)z)).X;
                return WithSurface(previous, TerrainDifferential.Constant(level - WaterPoolRules.DepthAt(along, frame.Length)),
                    double.PositiveInfinity, double.PositiveInfinity, true);
            }, op is null);
    }

    private sealed class SoftBankProfile(TerrainBuildState b, WaterSpec spec, TerrainRegion region,
        TerrainBridgeBanks? bridgeBanks, bool reserveTransitionGrade, string name, double grade)
    {
        public TerrainEvaluation Evaluate(TerrainEvaluation previous, TerrainDifferential d, double x, double z)
        {
            var (_, file, _, _, level, bank, depth, shelf, wade, _) = spec;
            TerrainDifferential target;
            if (d.Height <= 0)
            {
                var inward = -d;
                var bed = SoftDepth(inward, depth, shelf, false, bank.Kind == TerrainBankKind.Wetland ? grade : 0.15);
                target = TerrainDifferential.Constant(level) - bed;
                target = WadeSurface(target, inward, level, depth, shelf, wade, x, z);
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
                    if (d.Height < 0) target = WadeSurface(target, -d, level, depth, shelf, wade, x, z);
                    return WithSurface(previous, target, d.Height, -d.Height, d.Height < 0);
                }
            }
            return WithSurface(previous, target, double.PositiveInfinity, -d.Height);
        }
    }

    private static TerrainDifferential WadeSurface(TerrainDifferential bed, TerrainDifferential inward,
        double level, double depth, double shelf, TerrainRegion[] wade, double x, double z)
    {
        foreach (var band in wade)
        {
            var distance = band.Distance(x, z);
            var shallow = TerrainDifferential.Constant(level) - SoftDepth(inward, depth, shelf, true);
            // Only the bed envelope extends beyond the registration; WaterAt keeps permission local.
            if (distance.Height > 0) shallow -= distance * (1d / 6);
            if (shallow.Height > bed.Height) bed = shallow;
        }
        return bed;
    }

    private static TerrainDifferential HardWaterSurface(TerrainBuildState b, TerrainRegion region, TerrainDifferential distance,
        TerrainDifferential landSurface, double x, double z, double level, double depth, double grade, string file, string name,
        TerrainDifferential? roadTop = null, bool reserveTransitionGrade = false,
        TerrainBridgeBanks? bridgeBanks = null, TerrainDifferential? originalBankTop = null)
    {
        if (roadTop is { } declaredTop && declaredTop.Height < level + 0.3 - TerrainMath.HeightTolerance)
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
        if (roadTop is not null && top.Dx * top.Dx + top.Dz * top.Dz > Math.Pow(0.5 + TerrainMath.GradeTolerance, 2))
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

    internal static TerrainDifferential SoftDepth(TerrainDifferential distance, double maximum, double shelf, bool wade, double ordinaryGrade = 0.15)
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
}
