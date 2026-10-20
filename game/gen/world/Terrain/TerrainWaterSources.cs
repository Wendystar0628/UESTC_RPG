using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Water;
using static Campus.Gen.World.TerrainDryChannels;
using static Campus.Gen.World.TerrainFieldGrading;

namespace Campus.Gen.World;

internal static class TerrainWaterSources
{
    internal sealed record WaterSpec(TerrainOp? Op, string File, FeatureId Id, TerrainRegion Region, double Level,
        TerrainBank Bank, double Depth, double Shelf, TerrainRegion[] Wade, bool Channel)
    {
        public string Name => Op?.Id ?? "auto-water-" + Id.Value;
        public bool AutoLevelPending { get; init; }
    }

    internal static void Channel(TerrainBuildState b, TerrainChannelOp op, string file)
    {
        if (op.Wade.Count != 0 && (op.Dry || b.References.IsUndergroundWater(op.Water!.Value)))
            throw new WorldDataException(file, op.Id, "Wade requires a surface wet channel.");
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
        AddWater(b, op, file, water, region, op.WaterY!.Value, op.Bank, op.Depth, 1d / 7,
            op.Wade.Select(p => new TerrainRegion([p])).ToArray(), true);
    }

    internal static void Lake(TerrainBuildState b, TerrainLakeOp op, string file) =>
        AddWater(b, op, file, op.Water, b.References.Area(op.Water, "water", file, op.Id), op.WaterY, op.Bank,
            op.Bed.MaxDepth, TerrainReferences.Grade(op.Bed.Shelf), op.Wade.Select(p => new TerrainRegion([p])).ToArray(), false);

    private static void AddWater(TerrainBuildState b, TerrainOp? op, string file, FeatureId id, TerrainRegion region, double level,
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
            if (!TerrainUnion.Covers(region, band))
                throw new WorldDataException(file, name, "Wade area extends outside the actual water footprint or into a hole.");
        b.WaterSpecs.Add(new(op, file, id, region, level, bank, depth, shelf, wade, channel));
    }

    internal static void AddDefaultWater(TerrainBuildState b)
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

    internal static double DefaultShoreTop(TerrainBuildState b, TerrainRegion region, FeatureId id)
    {
        if (region.Polygons.Length == 0 || region.Polygons[0].Outer.Count == 0)
            throw new WorldDataException(b.References.SourceFiles(id), id.Value, "Water has no shore-top sample.");
        if (b.LandBeforeWater.Nodes.Count == 0) return 0;
        var point = region.Polygons[0].Outer[0];
        return b.LandBeforeWater.Evaluate(point.X, point.Y).Surface.Height;
    }

    internal static void ValidateWeirs(TerrainBuildState b, IEnumerable<TerrainChannelOp> channels)
    {
        var levels = b.Water.ToDictionary(w => w.Feature, w => w.Level);
        foreach (var channel in channels)
            foreach (var weir in channel.Weirs)
            {
                var file = b.Files[channel.Id];
                if (!levels.TryGetValue(weir.Upstream, out var upper) || !levels.TryGetValue(weir.Downstream, out var lower)
                    || Math.Abs(upper - lower - weir.Drop) > TerrainMath.HeightTolerance)
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
