using Campus.Contracts;
using Campus.Contracts.Maps;
using System.Numerics;

namespace Campus.Gen.World;

internal static partial class TerrainCompiler
{
    private sealed record WaterSpec(TerrainOp? Op, string File, FeatureId Id, TerrainRegion Region, double Level,
        TerrainBank Bank, double Depth, double Shelf, TerrainRegion[] Wade, bool Channel)
    {
        public string Name => Op?.Id ?? "auto-water-" + Id.Value;
        public bool AutoLevelPending { get; init; }
    }
    private sealed record WaterShore(string File, string Id, TerrainRegion Region, TerrainNode Node,
        Func<double, double, bool> CheckLevel, TerrainBridgeBanks? Banks,
        IReadOnlyList<FeatureId> Waters, IReadOnlyList<TerrainBankInterval> OriginalHard);
    private sealed class WaterJoinBudget(double width) { public double Width { get; set; } = width; }

    private static void ResolveUndergroundLevels(Builder b)
    {
        var pipes = b.UndergroundWaterSpecs.OrderBy(spec => spec.Id.Value, StringComparer.Ordinal).ToArray();
        if (pipes.Length == 0) return;
        var ends = pipes.Select(spec => b.References.WaterEndpoints(spec.Id)).ToArray();
        var vertices = pipes.Select(spec => b.References.WaterNodes(spec.Id)).ToArray();
        var surface = b.Water.OrderBy(water => water.Feature.Value, StringComparer.Ordinal).ToArray();
        var surfaceNodes = surface.Select(water => b.References.WaterNodes(water.Feature)).ToArray();
        var weirs = b.Operations.Values.OfType<TerrainChannelOp>().SelectMany(op => op.Weirs).ToArray();
        bool Barrier(FeatureId a, FeatureId c) => weirs.Any(weir => weir.Upstream == a && weir.Downstream == c || weir.Upstream == c && weir.Downstream == a);
        static bool Same(Vector2 a, Vector2 c) => Math.Round(a.X * 1000d, MidpointRounding.ToEven) == Math.Round(c.X * 1000d, MidpointRounding.ToEven)
            && Math.Round(a.Y * 1000d, MidpointRounding.ToEven) == Math.Round(c.Y * 1000d, MidpointRounding.ToEven);
        var groups = Components(pipes.Length, (a, c) => !Barrier(pipes[a].Id, pipes[c].Id)
            && (ends[a].Any(p => vertices[c].Any(q => Same(p, q))) || ends[c].Any(p => vertices[a].Any(q => Same(p, q)))));
        foreach (var group in groups)
        {
            var anchors = new List<(FeatureId Id, double Level)>();
            foreach (var i in group)
            {
                if (pipes[i].Op is not null) anchors.Add((pipes[i].Id, pipes[i].Level));
                for (var j = 0; j < surface.Length; j++)
                {
                    if (Barrier(pipes[i].Id, surface[j].Feature)) continue;
                    var connected = surfaceNodes[j].Count > 0
                        ? ends[i].Any(p => surfaceNodes[j].Any(q => Same(p, q)))
                        : ends[i].Any(p => surface[j].Region.Contains(p.X, p.Y));
                    if (connected) anchors.Add((surface[j].Feature, surface[j].Level));
                }
            }
            var level = anchors.Count == 0 ? -1.2 : anchors.OrderBy(anchor => anchor.Id.Value, StringComparer.Ordinal).First().Level;
            if (anchors.Any(anchor => Math.Abs(anchor.Level - level) > 0.000001))
                throw new WorldDataException("network.json", string.Join(",", group.Select(i => pipes[i].Id.Value)),
                    "Registered underground water connects incompatible explicit/resolved constant levels without a separating weir; no pipe gradient is inferred. Anchors: "
                    + string.Join(", ", anchors.OrderBy(anchor => anchor.Id.Value, StringComparer.Ordinal).Select(anchor => $"{anchor.Id.Value}={anchor.Level:R}")));
            foreach (var i in group)
            {
                var pipe = pipes[i]; b.Water.Add(new(pipe.Id, pipe.Region, level, pipe.Wade, pipe.Channel, Underground: true));
                b.Diagnostics.Add($"Underground water '{pipe.Id.Value}': constant metadata level={level:R}, "
                    + (anchors.Count == 0 ? "isolated legacy base-datum default" : "explicit level or actual registered endpoint connection")
                    + "; no surface carving, bank/open-water generation or inferred pipe slope. Raised terrain does not raise this water metadata.");
            }
        }
    }

    private static void CompileWaterGroups(Builder b)
    {
        ResolveConnectedWaterLevels(b);
        for (var i = 0; i < b.WaterSpecs.Count; i++)
            if (b.WaterSpecs[i].AutoLevelPending)
            {
                var spec = b.WaterSpecs[i];
                b.WaterSpecs[i] = spec with { Level = DefaultShoreTop(b, spec.Region, spec.Id) - 1.2, AutoLevelPending = false };
            }
        var specs = b.WaterSpecs.OrderBy(spec => spec.Id.Value, StringComparer.Ordinal).ToArray();
        var natural = specs.Select(spec => !spec.Channel || b.References.IsNaturalStream(spec.Id)).ToArray();
        var barriers = b.Operations.Values.OfType<TerrainChannelOp>().SelectMany(op => op.Weirs).ToArray();
        bool Barrier(int a, int c) => barriers.Any(weir => weir.Upstream == specs[a].Id && weir.Downstream == specs[c].Id
            || weir.Upstream == specs[c].Id && weir.Downstream == specs[a].Id);
        var connected = new bool[specs.Length, specs.Length];
        for (var i = 0; i < specs.Length; i++)
            for (var j = i + 1; j < specs.Length; j++)
                connected[i, j] = connected[j, i] = specs[i].Region.InteriorOverlaps(specs[j].Region)
                    || Touches(specs[i].Region, specs[j].Region);
        var naturalGroups = Components(specs.Length, (a, c) => natural[a] && natural[c] && connected[a, c] && !Barrier(a, c));
        foreach (var group in naturalGroups)
        {
            var lakes = group.Where(i => !specs[i].Channel).ToArray();
            if (lakes.Length == 0) continue;
            var level = specs[lakes[0]].Level;
            if (lakes.Any(i => Math.Abs(specs[i].Level - level) > 0.000001))
                throw new WorldDataException("overlay/terrain", "water", "Connected natural water reaches lakes with different constant levels and no registered separating weir.");
            foreach (var i in group)
            {
                if (!specs[i].Channel) continue;
                if (specs[i].Op is not null && Math.Abs(specs[i].Level - level) > 0.000001)
                    throw new WorldDataException(specs[i].File, specs[i].Name, "Authored natural-stream level conflicts with its connected lake; a real registered weir and separate water identities are required.");
                specs[i] = specs[i] with { Level = level };
                b.Diagnostics.Add($"Connected natural stream '{specs[i].Id.Value}' uses lake '{specs[lakes[0]].Id.Value}' level {level:R}; its source ID, bank kind and depth remain intact.");
            }
        }
        for (var i = 0; i < specs.Length; i++)
            for (var j = i + 1; j < specs.Length; j++)
                if (connected[i, j] && !Barrier(i, j) && Math.Abs(specs[i].Level - specs[j].Level) > 0.000001)
                    throw new WorldDataException(specs[j].File, specs[j].Name,
                        $"Touching water segments '{specs[i].Id.Value}' and '{specs[j].Id.Value}' have different constant levels without a registered separating weir.");
        var groups = Components(specs.Length, (a, c) => connected[a, c] && !Barrier(a, c) && Math.Abs(specs[a].Level - specs[c].Level) <= 0.000001);
        foreach (var indices in groups)
        {
            var members = indices.Select(i => specs[i]).ToArray();
            // Unioning geometry must not let a lexically earlier automatic member erase an
            // author's zone/zero-band obligation. Validate each declared operation on its own
            // resolved footprint, before any same-level composition can hide its influence.
            foreach (var spec in members.Where(spec => spec.Op is not null))
                b.WaterAuthorValidation.Add(WaterNode(b, spec, spec.Region, null));
            TerrainRegion domain;
            try { domain = members.Length == 1 ? members[0].Region : TerrainUnion.Build(members.Select(spec => spec.Region)); }
            catch (ArgumentException error) { throw new WorldDataException("campus.json", string.Join(",", members.Select(spec => spec.Id.Value)), error.Message, error); }
            var lakes = members.Where(spec => !spec.Channel).ToArray();
            var channels = members.Where(spec => spec.Channel).ToArray();
            RequireSameProfile(lakes); RequireSameProfile(channels);
            var representative = members.OrderBy(spec => spec.Op?.Priority ?? 50).ThenBy(spec => spec.Name, StringComparer.Ordinal).First();
            var bridgeBanks = lakes.Any(lake => lake.Bank.Kind == TerrainBankKind.Soft)
                ? new TerrainBridgeBanks(domain, b.Context.Network.Bridges) : null;
            TerrainNode node;
            Func<double, double, bool> check;
            IReadOnlyList<TerrainBankInterval> originalHard = [];
            if (lakes.Length == 0 || channels.Length == 0)
            {
                var spec = members[0] with { Wade = members.SelectMany(member => member.Wade).ToArray() };
                node = WaterNode(b, spec, domain, bridgeBanks) with
                    { Automatic = members.All(spec => spec.Op is null), HasSeparateAuthorValidation = true };
                if (spec.Bank.Kind == TerrainBankKind.Hard) originalHard = TerrainBankGeometry.WholeRings(domain);
                check = (x, z) => spec.Bank.Kind is TerrainBankKind.Soft or TerrainBankKind.Wetland
                    && (bridgeBanks?.HardWeight(x, z).Height ?? 0) < 1;
            }
            else
            {
                var lakeArea = TerrainUnion.Build(lakes.Select(spec => spec.Region));
                var lake = lakes[0] with { Wade = lakes.SelectMany(spec => spec.Wade).ToArray() };
                var channel = channels[0];
                var blend = new WaterJoinBudget(2);
                var join = new TerrainWaterJoin(domain, lakeArea);
                originalHard = join.HardIntervals(lake.Bank.Kind == TerrainBankKind.Hard, channel.Bank.Kind == TerrainBankKind.Hard);
                var lakeNode = WaterNode(b, lake, domain, bridgeBanks, reserveTransitionGrade: true);
                var channelNode = WaterNode(b, channel, domain, null, reserveTransitionGrade: true);
                var plainLake = WaterNode(b, lake, domain, null, reserveTransitionGrade: true);
                TerrainShoreEnvelope? shoreEnvelope = null;
                var support = new MapRect(System.Numerics.Vector2.Min(lakeNode.Support.Min, channelNode.Support.Min),
                    System.Numerics.Vector2.Max(lakeNode.Support.Max, channelNode.Support.Max));
                node = new(representative.Name, representative.File, representative.Op?.Zone ?? default, TerrainGroup.Water,
                    representative.Op?.Priority ?? 50, support, (previous, x, z) =>
                    {
                        if (domain.Distance(x, z).Height > 0.0000001 && shoreEnvelope is not null)
                        {
                            var baseline = plainLake.Apply(previous, x, z);
                            return WithSurface(previous, shoreEnvelope.Constrain(baseline.Surface, x, z), baseline.CrestDistance, baseline.ToeDistance);
                        }
                        return EvaluateRaw(previous, x, z);
                    }, members.All(spec => spec.Op is null)) { HasSeparateAuthorValidation = true };
                TerrainEvaluation EvaluateRaw(TerrainEvaluation previous, double x, double z)
                {
                        var weight = join.Weight(x, z, blend.Width);
                        if (weight.Height == 1) return lakeNode.Apply(previous, x, z);
                        if (weight.Height == 0) return channelNode.Apply(previous, x, z);
                        var a = channelNode.Apply(previous, x, z); var c = lakeNode.Apply(previous, x, z);
                        var difference = c.Surface - a.Surface;
                        var surface = a.Surface + new TerrainDifferential(difference.Height * weight.Height,
                            difference.Dx * weight.Height + difference.Height * weight.Dx,
                            difference.Dz * weight.Height + difference.Height * weight.Dz);
                        return WithSurface(previous, surface, double.PositiveInfinity, double.PositiveInfinity,
                            a.Class == TerrainClass.Transition || c.Class == TerrainClass.Transition);
                }
                shoreEnvelope = new(domain, (x, z) => EvaluateRaw(b.LandBeforeWater.Evaluate(x, z), x, z).Surface.Height, representative.File, representative.Name,
                    (x, z) => $"join={join.Weight(x, z, blend.Width)}, bank={bridgeBanks?.ProfileAt(x, z)}; "
                        + string.Join("; ", bridgeBanks?.RoadSpans.SelectMany(span => span.Roads).DistinctBy(road => road.Road)
                            .Select(road => road.Explain?.Invoke(x, z)) ?? []));
                b.ShoreEnvelopeRefresh.Add(shoreEnvelope.Refresh);
                ValidateWaterJoin(b, node, domain, join, blend, WaterJoinWidth(b, lake, channel));
                check = (x, z) => join.Weight(x, z, blend.Width).Height > 0
                    && (bridgeBanks?.HardWeight(x, z).Height ?? 0) < 1;
            }
            b.Nodes.Add(node);
            b.WaterShores.Add(new(representative.File, representative.Name, domain, node, check, bridgeBanks,
                members.Select(spec => spec.Id).ToArray(), originalHard));
            foreach (var spec in members)
            {
                b.Water.Add(new(spec.Id, spec.Region, spec.Level, spec.Wade, spec.Channel, bridgeBanks, spec.Level + (spec.Channel ? 1.2 : 0.5)));
                if (spec.Bank.Kind == TerrainBankKind.Hard)
                    b.Diagnostics.Add($"NOT_VERIFIED: generated structural bank coverage for '{spec.Id.Value}' still requires the content/physics acceptance pass.");
            }
            if (members.Length > 1) b.Diagnostics.Add($"Same-level surface union: {string.Join(",", members.Select(spec => spec.Id.Value))}; only exposed union rings are shores, member IDs and original depth profiles retained.");
            if (bridgeBanks?.Contacts.Count > 0)
                b.Diagnostics.Add($"Automatic bridge banks: {bridgeBanks.Contacts.Count} actual crossings on the exposed union shore, ±5 m Hard plus 2 m transitions. NOT_VERIFIED: corresponding WP-28 bank geometry.");
        }
    }

    private static void ResolveConnectedWaterLevels(Builder b)
    {
        var specs = b.WaterSpecs.Concat(b.UndergroundWaterSpecs).OrderBy(spec => spec.Id.Value, StringComparer.Ordinal).ToArray();
        var underground = b.UndergroundWaterSpecs.Select(spec => spec.Id).ToHashSet();
        var nodes = specs.Select(spec => b.References.WaterNodes(spec.Id)).ToArray();
        var ends = specs.Select(spec => b.References.WaterEndpoints(spec.Id)).ToArray();
        var weirs = b.Operations.Values.OfType<TerrainChannelOp>().SelectMany(op => op.Weirs).ToArray();
        static bool Same(Vector2 a, Vector2 c) => Math.Round(a.X * 1000d, MidpointRounding.ToEven) == Math.Round(c.X * 1000d, MidpointRounding.ToEven)
            && Math.Round(a.Y * 1000d, MidpointRounding.ToEven) == Math.Round(c.Y * 1000d, MidpointRounding.ToEven);
        bool Connected(int a, int c)
        {
            if (weirs.Any(w => w.Upstream == specs[a].Id && w.Downstream == specs[c].Id
                || w.Upstream == specs[c].Id && w.Downstream == specs[a].Id)) return false;
            var registeredNodes = ends[a].Any(p => nodes[c].Any(q => Same(p, q))) || ends[c].Any(p => nodes[a].Any(q => Same(p, q)));
            if (registeredNodes) return true;
            if (nodes[c].Count == 0 && ends[a].Any(p => specs[c].Region.Contains(p.X, p.Y))) return true;
            if (nodes[a].Count == 0 && ends[c].Any(p => specs[a].Region.Contains(p.X, p.Y))) return true;
            return !underground.Contains(specs[a].Id) && !underground.Contains(specs[c].Id)
                && (specs[a].Region.InteriorOverlaps(specs[c].Region) || Touches(specs[a].Region, specs[c].Region));
        }
        var resolved = new Dictionary<FeatureId, double>();
        foreach (var group in Components(specs.Length, Connected))
        {
            var anchors = group.Where(i => !specs[i].Channel && !underground.Contains(specs[i].Id) || specs[i].Op is not null).ToArray();
            if (anchors.Length == 0) continue;
            var level = specs[anchors[0]].Level;
            if (anchors.Any(i => Math.Abs(specs[i].Level - level) > 0.000001)
                || group.Any(i => specs[i].Op is not null && Math.Abs(specs[i].Level - level) > 0.000001))
                throw new WorldDataException("network.json", string.Join(",", group.Select(i => specs[i].Id.Value)),
                    "A water component connected through actual source nodes/registered culverts has incompatible authored/lake levels without a separating weir.");
            foreach (var i in group)
            {
                resolved.Add(specs[i].Id, level);
                if (Math.Abs(specs[i].Level - level) > 0.000001)
                    b.Diagnostics.Add($"Connected water '{specs[i].Id.Value}' inherits resolved water '{specs[anchors[0]].Id.Value}' level {level:R} through actual source nodes/registered culverts; its kind/profile/ID are unchanged, no pipe footprint is exposed or unioned into surface shores.");
            }
        }
        for (var i = 0; i < b.WaterSpecs.Count; i++) if (resolved.TryGetValue(b.WaterSpecs[i].Id, out var level)) b.WaterSpecs[i] = b.WaterSpecs[i] with { Level = level, AutoLevelPending = false };
        for (var i = 0; i < b.UndergroundWaterSpecs.Count; i++) if (resolved.TryGetValue(b.UndergroundWaterSpecs[i].Id, out var level)) b.UndergroundWaterSpecs[i] = b.UndergroundWaterSpecs[i] with { Level = level };
    }

    private static void ValidateWaterJoin(Builder b, TerrainNode node, TerrainRegion domain, TerrainWaterJoin join, WaterJoinBudget budget, double suggestedWidth)
    {
        // Resolve the geometric (non-orthogonal shore normals) budget before the road graph
        // samples its base field. The 2 m interval, endpoint levels and both depths stay fixed.
        var roads = b.References.AutomaticRoads().ToArray();
        var probes = new List<(double X, double Z)>();
        foreach (var polygon in domain.Polygons)
            foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                for (var i = 0; i < ring.Count; i++)
                {
                    var a = ring[i]; var c = ring[(i + 1) % ring.Count]; var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
                    var samples = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dz * dz)));
                    for (var j = 0; j <= samples; j++)
                    {
                        var x = a.X + dx * j / samples; var z = a.Y + dz * j / samples;
                        foreach (var road in roads)
                        {
                            var bounds = road.Footprint?.Bounds ?? road.Path.Bounds;
                            var reach = (TerrainSampling.MaximumHeight - TerrainSampling.MinimumHeight) * 3 + road.Width + 2 * TerrainBridgeBanks.TransitionLength;
                            if (x < bounds.Min.X - reach || x > bounds.Max.X + reach || z < bounds.Min.Y - reach || z > bounds.Max.Y + reach) continue;
                            var core = CorridorContactAt(road, 0, x, z);
                            if (core.Distance.Height <= 0) continue;
                            for (var part = 1; part < 4; part++)
                                probes.Add((x + (core.X - x) * part / 4, z + (core.Z - z) * part / 4));
                        }
                    }
                }
        var lower = 2d; double? upper = null;
        var dxDomain = (double)domain.Bounds.Max.X - domain.Bounds.Min.X; var dzDomain = (double)domain.Bounds.Max.Y - domain.Bounds.Min.Y;
        var maximumWidth = Math.Max(2, Math.Sqrt(dxDomain * dxDomain + dzDomain * dzDomain));
        while (true)
        {
            (double X, double Z, double Grade)? failure = null;
            long checkedBlendSamples = 0, interiorBedSamples = 0;
            var minX = double.PositiveInfinity; var maxX = double.NegativeInfinity;
            var minZ = double.PositiveInfinity; var maxZ = double.NegativeInfinity;
            var bedMinX = double.PositiveInfinity; var bedMaxX = double.NegativeInfinity;
            var bedMinZ = double.PositiveInfinity; var bedMaxZ = double.NegativeInfinity;
            (double X, double Z, double Grade, double Dx, double Dz)? maxSoil = null, maxTransition = null;
            foreach (var p in probes) if (!Check(p.X, p.Z)) break;
            var rect = b.Context.DataRect;
            for (var z = Math.Max(Math.Ceiling(node.Support.Min.Y), Math.Ceiling(rect.Min.Y)); z <= Math.Min(node.Support.Max.Y, rect.Max.Y) && failure is null; z++)
                for (var x = Math.Max(Math.Ceiling(node.Support.Min.X), Math.Ceiling(rect.Min.X)); x <= Math.Min(node.Support.Max.X, rect.Max.X); x++)
                    if (!Check(x, z)) break;
            if (failure is null)
            {
                upper = budget.Width;
                if (upper.Value - lower <= 0.01)
                {
                    var checkedBounds = checkedBlendSamples == 0 ? "none" : $"x=[{minX:R},{maxX:R}], z=[{minZ:R},{maxZ:R}]";
                    var bedBounds = interiorBedSamples == 0 ? "none" : $"x=[{bedMinX:R},{bedMaxX:R}], z=[{bedMinZ:R},{bedMaxZ:R}]";
                    var soilMaximum = maxSoil is { } soil ? $"{soil.Grade:R} at=({soil.X:R},{soil.Z:R}), gradient=({soil.Dx:R},{soil.Dz:R})" : "none (no non-Transition sample)";
                    var transitionMaximum = maxTransition is { } transition ? $"{transition.Grade:R} at=({transition.X:R},{transition.Z:R}), gradient=({transition.Dx:R},{transition.Dz:R})" : "none (no Transition sample)";
                    b.Diagnostics.Add($"Water interior join '{node.Id}': validated width={upper.Value:R} m (1 cm search bracket), source bounds={domain.Bounds}; "
                        + $"final successful iteration, 1 m lattice + fractional shore-road probes (sampled evidence, not an analytic global maximum; counts include repeated checks): "
                        + $"checkedBlendSamples(0<w<1)={checkedBlendSamples}, checkedBlendBounds={checkedBounds}; "
                        + $"interiorBedSamples(0<w<1, dDomain<0)={interiorBedSamples}, interiorBedBounds={bedBounds}; "
                        + $"maxSoilGrade(non-Transition, 0<w<1)={soilMaximum}; maxTransitionGrade(0<w<1)={transitionMaximum}; "
                        + "exposed shore remains exactly 2 m, levels and terminal depths unchanged.");
                    return;
                }
                budget.Width = (lower + upper.Value) / 2;
            }
            else
            {
                lower = budget.Width;
                if (upper is not null) budget.Width = (lower + upper.Value) / 2;
                else
                {
                    if (budget.Width >= maximumWidth) throw new WorldDataException(node.File, node.Id,
                        $"Original water depth profiles cannot fit the available geometric transition extent at ({failure.Value.X:R},{failure.Value.Z:R}), grade={failure.Value.Grade:R}; provide an explicit bed transition.");
                    budget.Width = Math.Min(maximumWidth, Math.Max(suggestedWidth, budget.Width * 2));
                }
            }

            bool Check(double x, double z)
            {
                var w = join.Weight(x, z, budget.Width).Height;
                if (w <= 0 || w >= 1) return true;
                var sample = node.Apply(b.LandBeforeWater.Evaluate(x, z), x, z);
                var grade = Math.Sqrt(sample.Surface.Dx * sample.Surface.Dx + sample.Surface.Dz * sample.Surface.Dz);
                checkedBlendSamples++;
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minZ = Math.Min(minZ, z); maxZ = Math.Max(maxZ, z);
                // Only strictly internal water samples describe bed coverage; exterior
                // bank/road probes still contribute to the unchanged two-dimensional gate.
                if (domain.Distance(x, z).Height < 0)
                {
                    interiorBedSamples++;
                    bedMinX = Math.Min(bedMinX, x); bedMaxX = Math.Max(bedMaxX, x);
                    bedMinZ = Math.Min(bedMinZ, z); bedMaxZ = Math.Max(bedMaxZ, z);
                }
                if (sample.Class == TerrainClass.Transition)
                {
                    if (maxTransition is null || grade > maxTransition.Value.Grade)
                        maxTransition = (x, z, grade, sample.Surface.Dx, sample.Surface.Dz);
                }
                else if (maxSoil is null || grade > maxSoil.Value.Grade)
                    maxSoil = (x, z, grade, sample.Surface.Dx, sample.Surface.Dz);
                if (grade <= (sample.Class == TerrainClass.Transition ? 1 : 0.5) + 0.000001) return true;
                failure = (x, z, grade); return false;
            }
        }
    }

    internal static (double Value, double Derivative) LimitedEase(double t, double peak)
    {
        if (t <= 0) return (0, 0); if (t >= 1) return (1, 0);
        peak = Math.Min(peak, 1.5);
        if (!(peak > 1)) throw new ArgumentOutOfRangeException(nameof(peak), "A C1 blend needs a slope budget above its mean slope.");
        var end = 1 - 1 / peak;
        if (t < end) return (peak * t * t / (2 * end), peak * t / end);
        if (t > 1 - end) return (1 - peak * (1 - t) * (1 - t) / (2 * end), peak * (1 - t) / end);
        return (peak * (t - end / 2), peak);
    }

    private static double WaterJoinWidth(Builder b, WaterSpec lake, WaterSpec channel)
    {
        if (lake.Bank.Kind != TerrainBankKind.Soft || channel.Bank.Kind != TerrainBankKind.Hard || lake.Wade.Length != 0) return 2;
        var top = Math.Max(DefaultShoreTop(b, channel.Region, channel.Id), channel.Level + 0.3);
        var grade = TerrainReferences.Grade(channel.Bank.Slope!); var foot = Math.Max(channel.Level - channel.Depth, channel.Level - 0.8);
        var drop = top - foot; var normalGrade = Math.Sqrt(1 - 0.5 * 0.5); var footDistance = drop / normalGrade;
        var shallowEnd = 0.25 / 0.15;
        var stations = new[] { 0d, 0.25, shallowEnd, shallowEnd + (lake.Depth - 0.3) / lake.Shelf,
            footDistance, footDistance + (foot - (channel.Level - channel.Depth)) / grade };
        var maximumDifference = 0d;
        foreach (var d in stations.Where(d => d >= 0))
        {
            var soft = lake.Level - SoftDepth(TerrainDifferential.Constant(d), lake.Depth, lake.Shelf, false).Height;
            var hard = d <= footDistance ? top - d * normalGrade : Math.Max(channel.Level - channel.Depth, foot - (d - footDistance) * grade);
            maximumDifference = Math.Max(maximumDifference, Math.Abs(soft - hard));
        }
        var baseGrade = Math.Max(grade, Math.Max(lake.Shelf, 0.15));
        if (maximumDifference == 0 || baseGrade >= 0.5) return 2;
        // Triangle inequality includes the non-orthogonal gradient term; unlike a Pythagorean
        // budget it does not assume the bed normal and blend direction are perpendicular.
        return Math.Max(2, 1.5 * maximumDifference / (0.5 - baseGrade));
    }
    private static void RequireSameProfile(WaterSpec[] specs)
    {
        if (specs.Length < 2) return;
        var first = specs[0];
        if (specs.Any(spec => spec.Bank.Kind != first.Bank.Kind
            || Math.Abs(TerrainReferences.Grade(spec.Bank.Slope!) - TerrainReferences.Grade(first.Bank.Slope!)) > 0.000001
            || Math.Abs(spec.Depth - first.Depth) > 0.000001 || Math.Abs(spec.Shelf - first.Shelf) > 0.000001))
            throw new WorldDataException("overlay/terrain", "water", "Overlapping same-level members of one profile class have different explicit bank/depth rules; provide an authored split instead of silently choosing one.");
    }
    private static bool Touches(TerrainRegion a, TerrainRegion c) => a.Polygons.SelectMany(p => p.Outer).Any(p => c.Distance(p.X, p.Y).Height == 0)
        || c.Polygons.SelectMany(p => p.Outer).Any(p => a.Distance(p.X, p.Y).Height == 0);
    private static IReadOnlyList<int[]> Components(int count, Func<int, int, bool> edge)
    {
        var visited = new bool[count]; var groups = new List<int[]>();
        for (var start = 0; start < count; start++)
        {
            if (visited[start]) continue;
            var queue = new Queue<int>(); var group = new List<int>(); queue.Enqueue(start); visited[start] = true;
            while (queue.TryDequeue(out var item))
            {
                group.Add(item);
                for (var i = 0; i < count; i++)
                    if (!visited[i] && edge(item, i)) { visited[i] = true; queue.Enqueue(i); }
            }
            groups.Add(group.OrderBy(i => i).ToArray());
        }
        return groups;
    }
}

/// <summary>Continuous extension of the prescribed exposed-shore profile into the water interior.</summary>
internal sealed class TerrainWaterJoin
{
    private readonly record struct Contribution(TerrainDifferential Distance, TerrainDifferential Weight);
    private sealed record Transition(Vector2 A, Vector2 B, double Start, double End, double RunLength);
    private readonly TerrainRingIndex? _lakeEdges;
    private readonly TerrainRingIndex? _channelEdges;
    private readonly Transition[] _transitions;
    private readonly TerrainRegion _domain;
    private readonly TerrainRegion _lakeArea;

    public TerrainWaterJoin(TerrainRegion domain, TerrainRegion lakeArea)
    {
        _domain = domain; _lakeArea = lakeArea;
        var lake = new List<(Vector2 A, Vector2 B)>(); var channel = new List<(Vector2 A, Vector2 B)>();
        var transitions = new List<Transition>();
        foreach (var polygon in domain.Polygons)
            foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
            {
                var count = ring.Count; var labels = new bool[count];
                for (var i = 0; i < count; i++)
                {
                    var a = ring[i]; var c = ring[(i + 1) % count];
                    // A source boundary midpoint can round to the exterior in the integer
                    // containment predicate. Millimetre topology still identifies that same
                    // exposed edge as lake shore; it must not become an invented Hard segment.
                    labels[i] = lakeArea.Distance(((double)a.X + c.X) / 2, ((double)a.Y + c.Y) / 2).Height <= 0.001;
                    if (labels[i]) lake.Add((a, c));
                }
                if (!labels.Any(value => value))
                { for (var i = 0; i < count; i++) channel.Add((ring[i], ring[(i + 1) % count])); continue; }
                for (var start = 0; start < count; start++)
                {
                    if (labels[start] || !labels[(start + count - 1) % count]) continue;
                    var run = new List<(Vector2 A, Vector2 B, double Start, double End)>(); var station = 0d; var index = start;
                    do
                    {
                        var a = ring[index]; var c = ring[(index + 1) % count];
                        var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y; var end = station + Math.Sqrt(dx * dx + dz * dz);
                        run.Add((a, c, station, end)); station = end; index = (index + 1) % count;
                    } while (!labels[index]);
                    foreach (var edge in run)
                    {
                        var cuts = new[] { edge.Start, edge.End, TerrainBridgeBanks.TransitionLength, station - TerrainBridgeBanks.TransitionLength }
                            .Where(s => s >= edge.Start && s <= edge.End).Distinct().OrderBy(s => s).ToArray();
                        for (var i = 0; i + 1 < cuts.Length; i++)
                        {
                            var a = At(cuts[i]); var c = At(cuts[i + 1]);
                            if (a == c) continue;
                            if (cuts[i] >= TerrainBridgeBanks.TransitionLength && cuts[i + 1] <= station - TerrainBridgeBanks.TransitionLength) channel.Add((a, c));
                            else transitions.Add(new(a, c, cuts[i], cuts[i + 1], station));
                        }
                        Vector2 At(double s)
                        {
                            if (s == edge.Start) return edge.A; if (s == edge.End) return edge.B;
                            var t = (s - edge.Start) / (edge.End - edge.Start);
                            return new((float)(edge.A.X + ((double)edge.B.X - edge.A.X) * t), (float)(edge.A.Y + ((double)edge.B.Y - edge.A.Y) * t));
                        }
                    }
                }
            }
        _lakeEdges = lake.Count == 0 ? null : new(lake); _channelEdges = channel.Count == 0 ? null : new(channel);
        _transitions = transitions.ToArray();
    }

    public TerrainDifferential Weight(double x, double z, double width)
    {
        var near = ShoreWeight(x, z, width); var domainDistance = _domain.Distance(x, z);
        if (domainDistance.Height >= 0) return near;
        var distance = _lakeArea.Distance(x, z);
        TerrainDifferential interior;
        if (distance.Height <= 0) interior = TerrainDifferential.Constant(1);
        else if (distance.Height >= width) interior = TerrainDifferential.Constant(0);
        else
        {
            var t = distance.Height / width; var slope = -6 * t * (1 - t) / width;
            interior = new(1 - TerrainMath.Hermite(t), distance.Dx * slope, distance.Dz * slope);
        }
        // The existing minimum 2 m fillet connects the fixed boundary extension to the
        // interior field. Widening the interior's horizontal profile must not also preserve
        // its narrow boundary pattern deeper and deeper into the bed.
        const double normalFillet = 2;
        var inward = -domainDistance; if (inward.Height >= normalFillet) return interior;
        var ratio = inward.Height / normalFillet; var derivative = 6 * ratio * (1 - ratio) / normalFillet;
        var depthWeight = new TerrainDifferential(TerrainMath.Hermite(ratio), derivative * inward.Dx, derivative * inward.Dz);
        return near + Product(interior - near, depthWeight);
    }

    public IReadOnlyList<TerrainBankInterval> HardIntervals(bool lakeHard, bool channelHard)
    {
        if (lakeHard && channelHard) return TerrainBankGeometry.WholeRings(_domain);
        var result = new List<TerrainBankInterval>();
        if (!lakeHard && !channelHard) return result;
        foreach (var item in TerrainBankGeometry.Rings(_domain))
        {
            var points = item.Path.Line.Points; var count = points.Count - 1; var stations = new double[count + 1]; var lake = new bool[count];
            for (var i = 0; i < count; i++)
            {
                var a = points[i]; var c = points[i + 1]; var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
                stations[i + 1] = stations[i] + Math.Sqrt(dx * dx + dz * dz);
                lake[i] = _lakeArea.Distance(((double)a.X + c.X) / 2, ((double)a.Y + c.Y) / 2).Height <= 0.001;
            }
            if (lake.All(value => value == lake[0]))
            {
                if (lake[0] ? lakeHard : channelHard) result.Add(new(item.Polygon, item.Ring, 0, item.Path.Length, item.Path.Length, [], []));
                continue;
            }
            for (var start = 0; start < count; start++)
            {
                if (lake[start] == lake[(start + count - 1) % count] || !(lake[start] ? lakeHard : channelHard)) continue;
                var length = 0d; var edge = start;
                do { length += stations[edge + 1] - stations[edge]; edge = (edge + 1) % count; } while (lake[edge] == lake[start]);
                var from = stations[start]; var to = from + length;
                if (!lake[start]) { from += TerrainBridgeBanks.TransitionLength; to -= TerrainBridgeBanks.TransitionLength; }
                if (to > from) result.Add(new(item.Polygon, item.Ring, from, to, item.Path.Length, [], []));
            }
        }
        return result;
    }

    private TerrainDifferential ShoreWeight(double x, double z, double width)
    {
        var capacity = _transitions.Length + 2;
        Span<Contribution> values = capacity <= 64 ? stackalloc Contribution[capacity] : new Contribution[capacity];
        var count = 0;
        if (_lakeEdges is not null) values[count++] = Constant(_lakeEdges.Nearest(x, z), 1, x, z);
        if (_channelEdges is not null) values[count++] = Constant(_channelEdges.Nearest(x, z), 0, x, z);
        foreach (var edge in _transitions)
        {
            var dx = (double)edge.B.X - edge.A.X; var dz = (double)edge.B.Y - edge.A.Y;
            var lengthSquared = dx * dx + dz * dz; var length = Math.Sqrt(lengthSquared);
            var raw = ((x - edge.A.X) * dx + (z - edge.A.Y) * dz) / lengthSquared;
            var t = Math.Max(0, Math.Min(1, raw)); var px = edge.A.X + dx * t; var pz = edge.A.Y + dz * t;
            var qx = x - px; var qz = z - pz; var distance = Math.Sqrt(qx * qx + qz * qz);
            var station = edge.Start + (edge.End - edge.Start) * t;
            var (left, leftSlope) = EndWeight(station); var (right, rightSlope) = EndWeight(edge.RunLength - station);
            var weight = 1 - (1 - left) * (1 - right);
            var along = leftSlope * (1 - right) - rightSlope * (1 - left);
            var scale = raw > 0 && raw < 1 ? (edge.End - edge.Start) / length : 0;
            values[count++] = new(distance == 0 ? TerrainDifferential.Constant(0) : new(distance, qx / distance, qz / distance),
                new(weight, along * scale * dx / length, along * scale * dz / length));
        }
        var closest = 0;
        for (var i = 1; i < count; i++) if (values[i].Distance.Height < values[closest].Distance.Height) closest = i;
        var minimum = values[closest].Distance;
        if (minimum.Height <= 0.0000001) return values[closest].Weight; // Floating projection limit, not a geometric band.
        var denominator = TerrainDifferential.Constant(0); var numerator = TerrainDifferential.Constant(0);
        for (var i = 0; i < count; i++)
        {
            var distance = values[i].Distance;
            var taper = TerrainDifferential.Constant(1) - (distance - minimum) * (1 / width);
            if (taper.Height <= 0) continue;
            // Normalized inverse-square coordinates remain finite near a shore and equal its
            // prescribed boundary value exactly. Compact support gives exact terminal profiles.
            var ratio = Divide(minimum, distance); var weight = Product(Product(taper, taper), Product(ratio, ratio));
            numerator += Product(weight, values[i].Weight); denominator += weight;
        }
        return Divide(numerator, denominator);
    }

    private static Contribution Constant(TerrainRingIndex.Hit hit, double weight, double x, double z)
    {
        var distance = Math.Sqrt(hit.Squared);
        return new(distance == 0 ? TerrainDifferential.Constant(0) : new(distance, (x - hit.X) / distance, (z - hit.Z) / distance), TerrainDifferential.Constant(weight));
    }
    private static (double Value, double Slope) EndWeight(double station)
    {
        if (station <= 0) return (1, 0); if (station >= TerrainBridgeBanks.TransitionLength) return (0, 0);
        var t = station / TerrainBridgeBanks.TransitionLength;
        return (1 - TerrainMath.Hermite(t), -6 * t * (1 - t) / TerrainBridgeBanks.TransitionLength);
    }
    private static TerrainDifferential Product(TerrainDifferential a, TerrainDifferential c) => new(a.Height * c.Height,
        a.Dx * c.Height + a.Height * c.Dx, a.Dz * c.Height + a.Height * c.Dz);
    private static TerrainDifferential Divide(TerrainDifferential a, TerrainDifferential c) => new(a.Height / c.Height,
        (a.Dx * c.Height - a.Height * c.Dx) / (c.Height * c.Height), (a.Dz * c.Height - a.Height * c.Dz) / (c.Height * c.Height));
}

/// <summary>Simultaneous fixed shore-height constraints on land, with exact 1:2 Lipschitz cones.</summary>
internal sealed class TerrainShoreEnvelope
{
    private const double Grade = 0.5;
    private sealed record Edge(double Ax, double Az, double Bx, double Bz, double Ay, double By, int Id);
    private sealed class Node
    {
        public required double MinX, MaxX, MinZ, MaxZ, MinY, MaxY;
        public Edge[]? Edges;
        public Node? Left, Right;
    }
    private readonly TerrainRegion _domain;
    private readonly Func<double, double, double> _shoreHeight;
    private readonly string _file, _id;
    private readonly Func<double, double, string>? _explain;
    private Node _root = null!;

    public TerrainShoreEnvelope(TerrainRegion domain, Func<double, double, double> shoreHeight, string file, string id,
        Func<double, double, string>? explain = null)
    { _domain = domain; _shoreHeight = shoreHeight; _file = file; _id = id; _explain = explain; Refresh(); }

    public void Refresh()
    {
        var edges = new List<Edge>();
        foreach (var polygon in _domain.Polygons)
            foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                for (var i = 0; i < ring.Count; i++)
                {
                    var a = ring[i]; var c = ring[(i + 1) % ring.Count];
                    var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
                    var count = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dz * dz) / 0.25));
                    var x0 = (double)a.X; var z0 = (double)a.Y; var y0 = _shoreHeight(x0, z0);
                    for (var j = 1; j <= count; j++)
                    {
                        var x1 = a.X + dx * j / count; var z1 = a.Y + dz * j / count; var y1 = _shoreHeight(x1, z1);
                        Add(x0, z0, x1, z1, y0, y1, 0); x0 = x1; z0 = z1; y0 = y1;
                    }
                }
        _root = Build(edges.ToArray());

        void Add(double ax, double az, double bx, double bz, double ay, double by, int depth)
        {
            var mx = (ax + bx) / 2; var mz = (az + bz) / 2; var my = _shoreHeight(mx, mz);
            var error = Math.Max(Math.Abs(my - (ay + by) / 2), Math.Max(
                Math.Abs(_shoreHeight((3 * ax + bx) / 4, (3 * az + bz) / 4) - (3 * ay + by) / 4),
                Math.Abs(_shoreHeight((ax + 3 * bx) / 4, (az + 3 * bz) / 4) - (ay + 3 * by) / 4)));
            // These are vector boundary constraints, not a sampled height field. Adaptive
            // chords resolve the prescribed curve to 1e-8 m, far below its 2 cm acceptance.
            if (error > 0.00000001)
            {
                if (depth >= 24) throw new WorldDataException(_file, _id,
                    $"Shore height is discontinuous or cannot be resolved by the deterministic boundary envelope: a=({ax:R},{az:R},{ay:R}), b=({bx:R},{bz:R},{by:R}), mid={my:R}, error={error:R}. A: {_explain?.Invoke(ax, az)} B: {_explain?.Invoke(bx, bz)}");
                Add(ax, az, mx, mz, ay, my, depth + 1); Add(mx, mz, bx, bz, my, by, depth + 1); return;
            }
            edges.Add(new(ax, az, bx, bz, ay, by, edges.Count));
        }
    }

    public TerrainDifferential Constrain(TerrainDifferential baseline, double x, double z)
    {
        var lower = new TerrainDifferential(double.NegativeInfinity, 0, 0);
        var upper = new TerrainDifferential(double.PositiveInfinity, 0, 0);
        Visit(_root);
        if (lower.Height > upper.Height + 0.000001)
            throw new WorldDataException(_file, _id, $"Fixed exposed shore heights cannot coexist within the 1:2 land-grade limit at ({x:R},{z:R}); lower={lower.Height:R}, upper={upper.Height:R}.");
        var result = baseline.Height > upper.Height ? upper : baseline;
        return result.Height < lower.Height ? lower : result;

        void Visit(Node node)
        {
            var distance = Distance(node, x, z);
            if (node.MaxY - Grade * distance <= lower.Height && node.MinY + Grade * distance >= upper.Height) return;
            if (node.Edges is { } edges)
            {
                foreach (var edge in edges)
                {
                    var a = Bound(edge, x, z, false); var c = Bound(edge, x, z, true);
                    if (a.Height > lower.Height) lower = a;
                    if (c.Height < upper.Height) upper = c;
                }
                return;
            }
            if (Distance(node.Left!, x, z) <= Distance(node.Right!, x, z)) { Visit(node.Left!); Visit(node.Right!); }
            else { Visit(node.Right!); Visit(node.Left!); }
        }
    }

    private static TerrainDifferential Bound(Edge edge, double x, double z, bool upper)
    {
        var dx = edge.Bx - edge.Ax; var dz = edge.Bz - edge.Az; var length = Math.Sqrt(dx * dx + dz * dz);
        var tx = dx / length; var tz = dz / length; var slope = (edge.By - edge.Ay) / length;
        var along = (x - edge.Ax) * tx + (z - edge.Az) * tz;
        var across = Math.Abs(-(x - edge.Ax) * tz + (z - edge.Az) * tx);
        var station = along;
        if (Math.Abs(slope) < Grade)
            station += (upper ? -1 : 1) * slope * across / Math.Sqrt(Grade * Grade - slope * slope);
        else station = upper ? slope > 0 ? 0 : length : slope > 0 ? length : 0;
        station = Math.Max(0, Math.Min(length, station));
        var qx = x - (edge.Ax + tx * station); var qz = z - (edge.Az + tz * station);
        var distance = Math.Sqrt(qx * qx + qz * qz); var sign = upper ? 1 : -1;
        var height = edge.Ay + slope * station + sign * Grade * distance;
        return distance <= 0.0000000001 ? new(height, slope * tx, slope * tz)
            : new(height, sign * Grade * qx / distance, sign * Grade * qz / distance);
    }
    private static Node Build(Edge[] edges)
    {
        var node = new Node { MinX = edges.Min(e => Math.Min(e.Ax, e.Bx)), MaxX = edges.Max(e => Math.Max(e.Ax, e.Bx)),
            MinZ = edges.Min(e => Math.Min(e.Az, e.Bz)), MaxZ = edges.Max(e => Math.Max(e.Az, e.Bz)),
            MinY = edges.Min(e => Math.Min(e.Ay, e.By)), MaxY = edges.Max(e => Math.Max(e.Ay, e.By)) };
        if (edges.Length <= 8) { node.Edges = edges.OrderBy(e => e.Id).ToArray(); return node; }
        var splitX = node.MaxX - node.MinX >= node.MaxZ - node.MinZ;
        var sorted = edges.OrderBy(e => splitX ? e.Ax + e.Bx : e.Az + e.Bz).ThenBy(e => e.Id).ToArray();
        var middle = sorted.Length / 2; node.Left = Build(sorted[..middle]); node.Right = Build(sorted[middle..]); return node;
    }
    private static double Distance(Node node, double x, double z)
    {
        var dx = Math.Max(0, Math.Max(node.MinX - x, x - node.MaxX)); var dz = Math.Max(0, Math.Max(node.MinZ - z, z - node.MaxZ));
        return Math.Sqrt(dx * dx + dz * dz);
    }
}
