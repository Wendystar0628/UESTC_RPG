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

    private static bool SameWaterNode(Vector2 a, Vector2 c) =>
        Math.Round(a.X * 1000d, MidpointRounding.ToEven) == Math.Round(c.X * 1000d, MidpointRounding.ToEven)
        && Math.Round(a.Y * 1000d, MidpointRounding.ToEven) == Math.Round(c.Y * 1000d, MidpointRounding.ToEven);

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
        var groups = Components(pipes.Length, (a, c) => !Barrier(pipes[a].Id, pipes[c].Id)
            && (ends[a].Any(p => vertices[c].Any(q => SameWaterNode(p, q))) || ends[c].Any(p => vertices[a].Any(q => SameWaterNode(p, q)))));
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
                        ? ends[i].Any(p => surfaceNodes[j].Any(q => SameWaterNode(p, q)))
                        : ends[i].Any(p => surface[j].Region.Contains(p.X, p.Y));
                    if (connected) anchors.Add((surface[j].Feature, surface[j].Level));
                }
            }
            var level = anchors.Count == 0 ? -1.2 : anchors.OrderBy(anchor => anchor.Id.Value, StringComparer.Ordinal).First().Level;
            if (anchors.Any(anchor => Math.Abs(anchor.Level - level) > TerrainMath.HeightTolerance))
                throw new WorldDataException(b.References.SourceFiles(group.Select(i => pipes[i].Id).Concat(anchors.Select(anchor => anchor.Id)).ToArray()), string.Join(",", group.Select(i => pipes[i].Id.Value)),
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
        var registeredBanks = ReadRegisteredBanks(b);
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
            if (lakes.Any(i => Math.Abs(specs[i].Level - level) > TerrainMath.HeightTolerance))
                throw new WorldDataException("overlay/terrain", "water", "Connected natural water reaches lakes with different constant levels and no registered separating weir.");
            foreach (var i in group)
            {
                if (!specs[i].Channel) continue;
                if (specs[i].Op is not null && Math.Abs(specs[i].Level - level) > TerrainMath.HeightTolerance)
                    throw new WorldDataException(specs[i].File, specs[i].Name, "Authored natural-stream level conflicts with its connected lake; a real registered weir and separate water identities are required.");
                specs[i] = specs[i] with { Level = level };
                b.Diagnostics.Add($"Connected natural stream '{specs[i].Id.Value}' uses lake '{specs[lakes[0]].Id.Value}' level {level:R}; its source ID, bank kind and depth remain intact.");
            }
        }
        for (var i = 0; i < specs.Length; i++)
            for (var j = i + 1; j < specs.Length; j++)
                if (connected[i, j] && !Barrier(i, j) && Math.Abs(specs[i].Level - specs[j].Level) > TerrainMath.HeightTolerance)
                    throw new WorldDataException(specs[j].File, specs[j].Name,
                        $"Touching water segments '{specs[i].Id.Value}' and '{specs[j].Id.Value}' have different constant levels without a registered separating weir.");
        var groups = Components(specs.Length, (a, c) => connected[a, c] && !Barrier(a, c) && Math.Abs(specs[a].Level - specs[c].Level) <= TerrainMath.HeightTolerance);
        foreach (var indices in groups)
        {
            var members = indices.Select(i => specs[i]).ToArray();
            // Unioning geometry must not let a lexically earlier automatic member erase an
            // author's zone-extent obligation. Validate each declared operation on its own
            // resolved footprint, before any same-level composition can hide its influence.
            foreach (var spec in members.Where(spec => spec.Op is not null))
                b.WaterAuthorValidation.Add(WaterNode(b, spec, spec.Region, null));
            TerrainRegion domain;
            try { domain = members.Length == 1 ? members[0].Region : TerrainUnion.Build(members.Select(spec => spec.Region)); }
            catch (ArgumentException error) { throw new WorldDataException(string.Join(",", members.Select(spec => spec.File).Distinct(StringComparer.Ordinal)), string.Join(",", members.Select(spec => spec.Id.Value)), error.Message, error); }
            var representative = members.OrderBy(spec => spec.Op?.Priority ?? 50).ThenBy(spec => spec.Name, StringComparer.Ordinal).First();
            var registeredHard = RegisteredBankIntervals(registeredBanks, members, domain);
            var bridgeBanks = members.Any(spec => spec.Bank.Kind is TerrainBankKind.Soft or TerrainBankKind.Wetland)
                ? new TerrainBridgeBanks(domain, b.Context.Network.Bridges, registeredHard, id => b.References.SourceFiles(id)) : null;
            var (node, check, originalHard) = ComposeWaterProfiles(b, members, domain, bridgeBanks, representative);
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

    private static (TerrainNode Node, Func<double, double, bool> Check, IReadOnlyList<TerrainBankInterval> Hard)
        ComposeWaterProfiles(Builder b, WaterSpec[] members, TerrainRegion domain, TerrainBridgeBanks? banks, WaterSpec representative)
    {
        // Defaults supply the uncovered water. Natural lake profiles retain their existing
        // precedence over automatic channels; explicit operations then use priority/ordinal ID.
        // Equal adjacent profiles share a mask, but never move an operation across another one.
        var ordered = members.Where(spec => spec.Op is null).OrderBy(spec => !spec.Channel)
            .ThenBy(spec => spec.Name, StringComparer.Ordinal)
            .Concat(members.Where(spec => spec.Op is not null).OrderBy(spec => spec.Op!.Priority)
                .ThenBy(spec => spec.Name, StringComparer.Ordinal)).ToArray();
        var profiles = new List<(WaterSpec Spec, List<TerrainRegion> Areas)>();
        foreach (var spec in ordered)
        {
            if (profiles.Count > 0 && SameWaterProfile(profiles[^1].Spec, spec)) profiles[^1].Areas.Add(spec.Region);
            else profiles.Add((spec, [spec.Region]));
        }
        var wade = members.SelectMany(spec => spec.Wade).ToArray();
        var nodes = profiles.Select(profile => WaterNode(b, profile.Spec with { Wade = wade }, domain, banks,
            reserveTransitionGrade: profiles.Count > 1)).ToArray();
        var joins = profiles.Skip(1).Select(profile => new TerrainWaterJoin(domain, TerrainUnion.Build(profile.Areas))).ToArray();
        var blend = new WaterJoinBudget(2);
        var support = new MapRect(nodes.Select(node => node.Support.Min).Aggregate(Vector2.Min),
            nodes.Select(node => node.Support.Max).Aggregate(Vector2.Max));
        TerrainShoreEnvelope? envelope = null;
        TerrainEvaluation Evaluate(TerrainEvaluation previous, double x, double z, int index)
        {
            if (index == 0) return nodes[0].Apply(previous, x, z);
            var weight = joins[index - 1].Weight(x, z, blend.Width);
            if (weight.Height == 1) return nodes[index].Apply(previous, x, z);
            var a = Evaluate(previous, x, z, index - 1);
            if (weight.Height == 0) return a;
            var c = nodes[index].Apply(previous, x, z);
            var surface = a.Surface + TerrainDifferential.Product(c.Surface - a.Surface, weight);
            return WithSurface(previous, surface, double.PositiveInfinity, double.PositiveInfinity,
                a.Class == TerrainClass.Transition || c.Class == TerrainClass.Transition);
        }
        var node = new TerrainNode(representative.Name, representative.File, representative.Op?.Zone ?? default,
            TerrainGroup.Water, representative.Op?.Priority ?? 50, support, (previous, x, z) =>
            {
                var value = Evaluate(previous, x, z, nodes.Length - 1);
                return domain.Distance(x, z).Height > 0 && envelope is not null
                    ? WithSurface(previous, envelope.Constrain(value.Surface, x, z), value.CrestDistance, value.ToeDistance)
                    : value;
            }, members.All(spec => spec.Op is null)) { HasSeparateAuthorValidation = true };
        if (joins.Length > 0)
        {
            envelope = new(domain, (x, z) => Evaluate(b.LandBeforeWater.Evaluate(x, z), x, z, nodes.Length - 1).Surface.Height,
                representative.File, representative.Name);
            b.ShoreEnvelopeRefresh.Add(envelope.Refresh);
            var suggested = profiles.SelectMany(a => profiles.Select(c => WaterJoinWidth(b, a.Spec, c.Spec))).Max();
            ResolveWaterJoinWidth(b, node, domain, joins, blend, suggested, envelope.Refresh);
        }
        bool Soft(WaterSpec spec) => spec.Bank.Kind is TerrainBankKind.Soft or TerrainBankKind.Wetland;
        bool Check(double x, double z)
        {
            var soft = Soft(profiles[0].Spec) ? 1d : 0d;
            for (var i = 1; i < profiles.Count; i++)
            {
                var weight = joins[i - 1].Weight(x, z, blend.Width).Height;
                soft = soft * (1 - weight) + (Soft(profiles[i].Spec) ? weight : 0);
            }
            return soft > 0 && (banks?.HardWeight(x, z).Height ?? 0) < 1;
        }
        IReadOnlyList<TerrainBankInterval> hard = profiles[0].Spec.Bank.Kind == TerrainBankKind.Hard
            ? TerrainBankGeometry.WholeRings(domain) : [];
        for (var i = 1; i < profiles.Count; i++)
            hard = profiles[i].Spec.Bank.Kind == TerrainBankKind.Hard
                ? TerrainBankGeometry.Merge(hard.Concat(joins[i - 1].HardIntervals(true, false)))
                : IntersectHardIntervals(hard, joins[i - 1].HardIntervals(false, true));
        return (node, Check, hard);
    }

    private static IReadOnlyList<TerrainBankInterval> IntersectHardIntervals(IReadOnlyList<TerrainBankInterval> a,
        IReadOnlyList<TerrainBankInterval> c)
    {
        var result = new List<TerrainBankInterval>();
        foreach (var left in a)
            foreach (var right in c.Where(right => right.Polygon == left.Polygon && right.Ring == left.Ring))
                // Merged intervals can wrap once around the ring's zero station.
                foreach (var shift in new[] { -left.Length, 0, left.Length })
                {
                    var from = Math.Max(left.From, right.From + shift); var to = Math.Min(left.To, right.To + shift);
                    if (to > from) result.Add(left with { From = from, To = to });
                }
        return TerrainBankGeometry.Merge(result);
    }

    private static void ResolveConnectedWaterLevels(Builder b)
    {
        var specs = b.WaterSpecs.Concat(b.UndergroundWaterSpecs).OrderBy(spec => spec.Id.Value, StringComparer.Ordinal).ToArray();
        var underground = b.UndergroundWaterSpecs.Select(spec => spec.Id).ToHashSet();
        var nodes = specs.Select(spec => b.References.WaterNodes(spec.Id)).ToArray();
        var ends = specs.Select(spec => b.References.WaterEndpoints(spec.Id)).ToArray();
        var weirs = b.Operations.Values.OfType<TerrainChannelOp>().SelectMany(op => op.Weirs).ToArray();
        bool Connected(int a, int c)
        {
            if (weirs.Any(w => w.Upstream == specs[a].Id && w.Downstream == specs[c].Id
                || w.Upstream == specs[c].Id && w.Downstream == specs[a].Id)) return false;
            var registeredNodes = ends[a].Any(p => nodes[c].Any(q => SameWaterNode(p, q))) || ends[c].Any(p => nodes[a].Any(q => SameWaterNode(p, q)));
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
            if (anchors.Any(i => Math.Abs(specs[i].Level - level) > TerrainMath.HeightTolerance)
                || group.Any(i => specs[i].Op is not null && Math.Abs(specs[i].Level - level) > TerrainMath.HeightTolerance))
                throw new WorldDataException(string.Join(",", group.Select(i => specs[i].File).Distinct(StringComparer.Ordinal)), string.Join(",", group.Select(i => specs[i].Id.Value)),
                    "A water component connected through actual source nodes/registered culverts has incompatible authored/lake levels without a separating weir.");
            foreach (var i in group)
            {
                resolved.Add(specs[i].Id, level);
                if (Math.Abs(specs[i].Level - level) > TerrainMath.HeightTolerance)
                    b.Diagnostics.Add($"Connected water '{specs[i].Id.Value}' inherits resolved water '{specs[anchors[0]].Id.Value}' level {level:R} through actual source nodes/registered culverts; its kind/profile/ID are unchanged, no pipe footprint is exposed or unioned into surface shores.");
            }
        }
        for (var i = 0; i < b.WaterSpecs.Count; i++) if (resolved.TryGetValue(b.WaterSpecs[i].Id, out var level)) b.WaterSpecs[i] = b.WaterSpecs[i] with { Level = level, AutoLevelPending = false };
        for (var i = 0; i < b.UndergroundWaterSpecs.Count; i++) if (resolved.TryGetValue(b.UndergroundWaterSpecs[i].Id, out var level)) b.UndergroundWaterSpecs[i] = b.UndergroundWaterSpecs[i] with { Level = level };
    }

    private static void ResolveWaterJoinWidth(Builder b, TerrainNode node, TerrainRegion domain, TerrainWaterJoin[] joins,
        WaterJoinBudget budget, double suggestedWidth, Action refreshShore)
    {
        var dx = (double)domain.Bounds.Max.X - domain.Bounds.Min.X;
        var dz = (double)domain.Bounds.Max.Y - domain.Bounds.Min.Y;
        var maximumWidth = Math.Max(2, Math.Sqrt(dx * dx + dz * dz));
        while (true)
        {
            (double X, double Z, double Grade)? failure = null;
            var rect = b.Context.DataRect;
            for (var z = Math.Max(Math.Ceiling(node.Support.Min.Y), Math.Ceiling(rect.Min.Y)); z <= Math.Min(node.Support.Max.Y, rect.Max.Y) && failure is null; z++)
                for (var x = Math.Max(Math.Ceiling(node.Support.Min.X), Math.Ceiling(rect.Min.X)); x <= Math.Min(node.Support.Max.X, rect.Max.X); x++)
                {
                    if (!joins.Any(join => join.Weight(x, z, budget.Width).Height is > 0 and < 1)) continue;
                    var sample = node.Apply(b.LandBeforeWater.Evaluate(x, z), x, z);
                    var grade = Math.Sqrt(sample.Surface.Dx * sample.Surface.Dx + sample.Surface.Dz * sample.Surface.Dz);
                    if (grade <= (sample.Class == TerrainClass.Transition ? 1 : 0.5) + TerrainMath.GradeTolerance) continue;
                    failure = (x, z, grade); break;
                }
            if (failure is null) return;
            if (budget.Width >= maximumWidth) throw new WorldDataException(node.File, node.Id,
                $"Original water depth profiles cannot fit the available geometric transition extent at ({failure.Value.X:R},{failure.Value.Z:R}), grade={failure.Value.Grade:R}; provide an explicit bed transition.");
            budget.Width = Math.Min(maximumWidth, Math.Max(suggestedWidth, budget.Width * 2));
            refreshShore();
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
    private static bool SameWaterProfile(WaterSpec a, WaterSpec c) => a.Bank.Kind == c.Bank.Kind
        && TerrainReferences.Grade(a.Bank.Slope!) == TerrainReferences.Grade(c.Bank.Slope!)
        && a.Depth == c.Depth && a.Shelf == c.Shelf;
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
