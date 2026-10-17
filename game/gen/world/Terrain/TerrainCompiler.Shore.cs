using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

internal static partial class TerrainCompiler
{
    private sealed record FootwayPlan(WaterShore Shore, IReadOnlyList<TerrainBridgeBanks.RoadSpan> Spans);

    private static void ResolveFootwayBanks(Builder b)
    {
        var entries = b.Roads.Where(entry => entry.Road.Kind is "footway" or "path" or "pedestrian")
            .OrderBy(entry => entry.Road.Source.Value, StringComparer.Ordinal).ToArray();
        var bySource = entries.ToDictionary(entry => entry.Road.Source);
        var roads = entries.Select(entry =>
                new TerrainBridgeBanks.RoadContact(entry.Road.Source,
                    TerrainRegion.Expand(entry.Road.Footprint?.Bounds ?? entry.Road.Path.Bounds,
                        (TerrainSampling.MaximumHeight - TerrainSampling.MinimumHeight) * 3 + entry.Road.Width),
                    entry.Road.Footprint?.Polygons.SelectMany(p => p.Outer).ToArray() ?? entry.Road.Path.Line.Points,
                    (x, z) =>
                    {
                        var contact = CorridorContactAt(entry.Road, 0, x, z);
                        var hit = entry.Road.Path.Project(contact.X, contact.Z);
                        if (entry.Profile.Excluded(hit.Station)) return null;
                        return (contact.Distance, RoadCoreTop(b, entry, contact, x, z));
                    })
                {
                    Explain = (x, z) =>
                    {
                        var core = CorridorContactAt(entry.Road, 0, x, z); var hit = entry.Road.Path.Project(core.X, core.Z);
                        return $"{entry.Road.Source.Value}: core=({core.X:R},{core.Z:R}), segment={hit.Segment}, station={hit.Station:R}, offset={hit.Offset:R}, profile={entry.Profile.At(hit.Station)}, top={RoadCoreTop(b, entry, core, x, z)}";
                    }
                }).ToArray();
        var shores = b.WaterShores.Where(shore => shore.Banks is not null).OrderBy(shore => shore.Id, StringComparer.Ordinal).ToArray();
        var fit = b.RoadFit ?? throw new InvalidOperationException("The shared road-network objective has not been frozen.");
        var initial = fit.Samples.Select(fit.Height).ToArray();
        var levels = shores.ToDictionary(shore => shore.Id, shore => b.Water.First(water => shore.Waters.Contains(water.Feature)).Level);
        // Candidate values depend only on resolved water levels and frozen real core geometry.
        // The finite candidate identities are frozen here; geometry-only values are memoized
        // lazily once for participating roads, and never depend on the current trial profile.
        var radii = entries.ToDictionary(entry => entry.Road.Source, entry => new Lazy<double>(() => RoadCorePathRadius(entry.Road)));
        var candidates = (from shore in shores from entry in entries where entry.Operation is null
            select (Key: (shore.Id, entry.Road.Source), Value: new Lazy<TerrainRoadProfile.Bound>(() => new(
                levels[shore.Id] + 0.3 + 0.015 * radii[entry.Road.Source].Value, string.Join(",", shore.Waters.OrderBy(w => w.Value, StringComparer.Ordinal).Select(w => w.Value))))))
            .ToDictionary(item => item.Key, item => item.Value);
        var maximumIterations = checked(1L + (long)fit.NodeCount * candidates.Count);
        FootwayPlan[]? first = null; FootwayPlan[]? prior = null; FootwayPlan[] final = [];
        var completed = false; var iterations = 0L;
        for (var iteration = 1L; iteration <= maximumIterations; iteration++)
        {
            iterations = iteration;
            // Banks still contain only original bridge contacts here, so CheckLevel/Node.Apply
            // see the same non-road soft field in every pass. Plans are disposable pure data.
            var plans = shores.Select(shore => new FootwayPlan(shore, shore.Banks!.PlanFootways(roads,
                (x, z) => shore.CheckLevel(x, z) ? shore.Node.Apply(b.LandBeforeWater.Evaluate(x, z), x, z).Surface.Height : null))).ToArray();
            if (first is null) { first = plans; ReportInitialFreeboard(b, plans, levels); }
            ReportHardPlanChange(b, prior ?? plans, plans, iteration);
            var raisedBounds = 0;
            foreach (var plan in plans)
            {
                ValidateRealAuthoredAnchorFreeboard(b, plan, bySource, levels[plan.Shore.Id]);
                foreach (var span in plan.Spans)
                {
                    var path = plan.Shore.Banks!.RingPath(span);
                    foreach (var contact in span.Roads)
                    {
                        var entry = bySource[contact.Road];
                        if (entry.Operation is not null)
                        {
                            // Fixed authored Hermite needs its own continuous-extrema proof.
                            var required = levels[plan.Shore.Id] + 0.3 + Math.Abs(entry.Operation.CrossFall) * radii[entry.Road.Source].Value;
                            foreach (var (a, c) in ShoreSupportSegments(path, span.From - TerrainBridgeBanks.TransitionLength, span.To + TerrainBridgeBanks.TransitionLength))
                                foreach (var interval in RoadSurfaceSupportIntervals(entry.Road, a, c))
                                    if (entry.Profile.MinimumHeight(interval.From, interval.To) < required)
                                        throw UnsupportedFreeboard($"Fixed authored Hermite support cannot prove continuous freeboard; source='{entry.Road.Source.Value}', file='{entry.File}', entry='{entry.Operation.Id}', support=[{interval.From:R},{interval.To:R}], conservativeLowerBound={required:R}, water='{plan.Shore.Id}'. A conservative support failure does not establish an authored-content conflict.");
                            continue;
                        }
                        var bound = candidates[(plan.Shore.Id, contact.Road)].Value;
                        foreach (var (a, c) in ShoreSupportSegments(path, span.From - TerrainBridgeBanks.TransitionLength, span.To + TerrainBridgeBanks.TransitionLength))
                            foreach (var index in AutomaticFreeboardSupport(entry.Road, entry.Profile, a, c))
                                if (fit.Require(contact.Road, index, bound)) raisedBounds++;
                    }
                }
            }
            b.Diagnostics.Add($"Road freeboard closure iteration={iteration}, newOrRaisedNodeBounds={raisedBounds}, actualHardSpans={plans.Sum(plan => plan.Spans.Count)}, finiteOuterLimit={maximumIterations}; frozen soft targets/geometry/water/grades retained.");
            if (raisedBounds == 0) { final = plans; completed = true; break; }
            var before = fit.Samples.Select(fit.Height).ToArray();
            var profiles = fit.Solve();
            foreach (var entry in b.Roads.Where(entry => entry.Operation is null)) entry.ReplaceProfile(profiles[entry.Road.Source]);
            ReportProfileChanges(b, fit, before, $"iteration={iteration}");
            prior = plans;
        }
        if (!completed) throw UnsupportedFreeboard("Finite conservative freeboard closure did not converge; no approximate profile/bank snapshot is published.");
        ValidateRoadJunctions(b.Roads);
        ReportProfileChanges(b, fit, initial, "final");
        ReportHardPlanChange(b, first ?? [], final, 0);
        // Snapshot lifetime boundary: final profiles can no longer change, and only the last
        // freshly planned real Hard geometry is committed (never the conservative support).
        foreach (var entry in b.Roads) entry.FreezeProfile();
        foreach (var plan in final)
        {
            plan.Shore.Banks!.CommitFootways(plan.Spans);
            foreach (var span in plan.Spans)
            {
                var a = plan.Shore.Banks.At(span, span.From); var c = plan.Shore.Banks.At(span, span.To); var length = span.To - span.From;
                b.Diagnostics.Add($"Footway Hard bank '{plan.Shore.Id}': ring={span.Polygon}/{span.Ring}, core=({a.X:R},{a.Y:R})->({c.X:R},{c.Y:R}), length={length:R} m, conflict extended 3 m at both ends plus 2 m Soft joins; roads={string.Join(",", span.Roads.Select(r => r.Road.Value))}."
                    + (length > 30 ? " AUTHOR_REVIEW_REQUIRED: continuous Hard segment exceeds 30 m; report independently to Opus without concealing it." : "")
                    + " NOT_VERIFIED: WP-28 0.15 m waterside stone edge and physical bank coverage.");
            }
        }
        foreach (var refresh in b.ShoreEnvelopeRefresh) refresh();
        // Continuous automatic lower bounds above complement, rather than replace, the
        // complete existing 2D/shore/node/zero-band/final-pad/support validation afterwards.
        ValidateCommittedFreeboard(b, final, levels);
        b.Diagnostics.Add($"Road freeboard closure complete: iterations={iterations}, finiteOuterLimit={maximumIterations}; final actual banks committed once; continuous automatic piece/SurfaceAt/IDW convex-support certificate retained.");
        // Terrain nodes retain this builder, but only their frozen RoadEntry profiles are
        // needed after compilation. Do not retain solver targets/corrections/owners in snapshots.
        b.RoadFit = null;
    }

    private static WorldDataException UnsupportedFreeboard(string detail)
    {
        var error = new WorldDataException("network.json", "automatic-profiles", "UNSUPPORTED: " + detail);
        error.Data["TerrainConstraintKind"] = "unsupported"; return error;
    }

    internal static double RoadCorePathRadius(TerrainRoad road)
    {
        if (road.Footprint is not { } core) return road.Width / 2;
        const double cell = 0.25; var radius = Math.Sqrt(2) * cell / 2; var maximum = 0d; var found = false;
        // Every core point lies in a grid cell whose centre is at most half a diagonal away.
        // Distance-to-path is 1-Lipschitz, so centre distance + that radius covers the ENTIRE
        // real core, including interiors/holes/concave corners; vertex-only tests cannot do so.
        var minX = (long)Math.Floor(core.Bounds.Min.X / cell) - 1; var maxX = (long)Math.Ceiling(core.Bounds.Max.X / cell);
        var minZ = (long)Math.Floor(core.Bounds.Min.Y / cell) - 1; var maxZ = (long)Math.Ceiling(core.Bounds.Max.Y / cell);
        for (var iz = minZ; iz <= maxZ; iz++)
            for (var ix = minX; ix <= maxX; ix++)
            {
                var x = (ix + 0.5) * cell; var z = (iz + 0.5) * cell;
                if (core.Distance(x, z).Height > radius) continue;
                maximum = Math.Max(maximum, road.Path.Project(x, z).Distance + radius); found = true;
            }
        if (!found || !double.IsFinite(maximum)) throw UnsupportedFreeboard($"Real road core '{road.Source.Value}' has no finite coverage radius.");
        return maximum;
    }

    internal static IReadOnlyList<int> AutomaticFreeboardSupport(TerrainRoad road, TerrainRoadProfile profile,
        (double X, double Z) a, (double X, double Z) c)
    {
        var indices = new SortedSet<int>();
        foreach (var interval in RoadSurfaceSupportIntervals(road, a, c))
            foreach (var index in profile.SupportIndices(interval.From, interval.To)) indices.Add(index);
        return indices.ToArray();
    }

    private static IEnumerable<(double From, double To)> RoadSurfaceSupportIntervals(TerrainRoad road,
        (double X, double Z) a, (double X, double Z) c)
    {
        var mx = (a.X + c.X) / 2; var mz = (a.Z + c.Z) / 2;
        var dx = c.X - a.X; var dz = c.Z - a.Z; var radius = Math.Sqrt(dx * dx + dz * dz) / 2;
        var nearest = road.Path.Project(mx, mz).Distance;
        for (var segment = 0; segment + 1 < road.Path.Line.Points.Count; segment++)
        {
            var middle = road.Path.ProjectStationSegment(segment, mx, mz);
            // Both segment-distance and whole-path-distance are 1-Lipschitz. A segment
            // beyond width + 2r cannot enter the existing positive taper-squared support anywhere.
            if (middle.Distance.Height - nearest > road.Width + 2 * radius + 0.0000001) continue;
            var left = road.Path.ProjectStationSegment(segment, a.X, a.Z).Station;
            var right = road.Path.ProjectStationSegment(segment, c.X, c.Z).Station;
            var from = Math.Min(left, right); var to = Math.Max(left, right);
            yield return (from, to);
        }
    }

    private static IEnumerable<((double X, double Z) A, (double X, double Z) C)> ShoreSupportSegments(TerrainPath path, double from, double to)
    {
        // Split at every actual source vertex and at the existing 0.25 m shore lattice.
        // Each emitted piece is straight, so endpoint projections bound its full station range.
        var stations = new SortedSet<double> { from, to };
        var firstTurn = (long)Math.Floor(from / path.Length); var lastTurn = (long)Math.Floor(to / path.Length);
        for (var turn = firstTurn; turn <= lastTurn; turn++)
        {
            var offset = turn * path.Length;
            foreach (var station in path.VertexStations)
                if (station + offset > from && station + offset < to) stations.Add(station + offset);
            for (var station = 0.25; station < path.Length; station += 0.25)
                if (station + offset > from && station + offset < to) stations.Add(station + offset);
        }
        var values = stations.ToArray();
        for (var i = 1; i < values.Length; i++) yield return (Point(values[i - 1]), Point(values[i]));
        (double X, double Z) Point(double station)
        {
            station %= path.Length; if (station < 0) station += path.Length;
            return path.AtDouble(station);
        }
    }

    private static void ValidateRealAuthoredAnchorFreeboard(Builder b, FootwayPlan plan, IReadOnlyDictionary<FeatureId, RoadEntry> entries, double level)
    {
        foreach (var entry in entries.Values.Where(entry => entry.Operation is not null).OrderBy(entry => entry.Road.Source.Value, StringComparer.Ordinal))
            foreach (var anchor in entry.Profile.AuthoredAnchors)
            {
                if (entry.Profile.Excluded(anchor.S)) continue;
                var point = entry.Road.Path.AtDouble(anchor.S);
                // Exact coincidence only. Nearby fixed nodes selected by conservative support
                // never establish a real authored-content conflict.
                if (Math.Abs(plan.Shore.Region.Distance(point.X, point.Z).Height) > 0.0000001) continue;
                var active = plan.Spans.Where(span =>
                {
                    var hit = plan.Shore.Banks!.RingPath(span).Project(point.X, point.Z);
                    if (hit.Distance > 0.0000001) return false;
                    var station = hit.Station; while (station < span.From - TerrainBridgeBanks.TransitionLength) station += span.RingLength;
                    return station >= span.From - TerrainBridgeBanks.TransitionLength && station <= span.To + TerrainBridgeBanks.TransitionLength;
                }).ToArray();
                if (active.Length != 1 || !active[0].Roads.Any(road => road.Road == entry.Road.Source)) continue;
                var stationHit = plan.Shore.Banks!.RingPath(active[0]).Project(point.X, point.Z).Station;
                while (stationHit < active[0].From) stationHit += active[0].RingLength;
                if (stationHit > active[0].To) continue; // Full real Hard core is necessary for this exact proof.
                var actual = RoadCoreTop(b, entry, CorridorContactAt(entry.Road, 0, point.X, point.Z), point.X, point.Z).Height;
                if (actual >= level + 0.3 - 0.000001) continue;
                var error = new WorldDataException(entry.File, entry.Operation!.Id,
                    $"CONTENT_CONFLICT: an authored hard anchor at a real constrained shoreline violates necessary freeboard; source='{entry.Road.Source.Value}', station={anchor.S:R}, world=({point.X:R},{point.Z:R}), anchorY={anchor.Y:R}, actualBankTop={actual:R}, water='{string.Join(",", plan.Shore.Waters.Select(w => w.Value))}', requiredBankTop={level + 0.3:R}. Anchors were not moved.");
                error.Data["TerrainConstraintKind"] = "content-conflict"; throw error;
            }
    }

    private static void ReportInitialFreeboard(Builder b, IReadOnlyList<FootwayPlan> plans, IReadOnlyDictionary<string, double> levels)
    {
        foreach (var plan in plans)
        {
            var required = levels[plan.Shore.Id] + 0.3; var own = new Dictionary<FeatureId, (double Top, double X, double Z)>();
            var mixed = (Top: double.PositiveInfinity, X: 0d, Z: 0d);
            foreach (var span in plan.Spans)
                foreach (var (a, c) in ShoreSupportSegments(plan.Shore.Banks!.RingPath(span), span.From - TerrainBridgeBanks.TransitionLength, span.To + TerrainBridgeBanks.TransitionLength))
                    foreach (var point in new[] { a, ((a.X + c.X) / 2, (a.Z + c.Z) / 2), c })
                    {
                        foreach (var road in span.Roads)
                            if (road.Evaluate(point.Item1, point.Item2) is { } value && (!own.TryGetValue(road.Road, out var old) || value.Top.Height < old.Top))
                                own[road.Road] = (value.Top.Height, point.Item1, point.Item2);
                        var original = Math.Max(required, b.LandBeforeWater.Evaluate(point.Item1, point.Item2).Surface.Height);
                        var result = plan.Shore.Banks.ProfileAtPlan(point.Item1, point.Item2, plan.Spans, TerrainDifferential.Constant(original));
                        if (result.Top is { } top && top.Height < mixed.Top) mixed = (top.Height, point.Item1, point.Item2);
                    }
            foreach (var item in own.Where(item => item.Value.Top < required).OrderBy(item => item.Key.Value, StringComparer.Ordinal))
                b.Diagnostics.Add($"Road freeboard initial own deficit: water='{plan.Shore.Id}', source='{item.Key.Value}', world=({item.Value.X:R},{item.Value.Z:R}), ownTop={item.Value.Top:R}, required={required:R}, deficit={required - item.Value.Top:R}.");
            if (mixed.Top < required) b.Diagnostics.Add($"Road freeboard initial mixed deficit: water='{plan.Shore.Id}', world=({mixed.X:R},{mixed.Z:R}), mixedTop={mixed.Top:R}, required={required:R}, deficit={required - mixed.Top:R}.");
        }
    }

    private static void ReportProfileChanges(Builder b, TerrainRoadProfile.NetworkFit fit, IReadOnlyList<double> before, string phase)
    {
        var maximum = 0d; var count = 0; var tiny = 0; var minX = double.PositiveInfinity; var minZ = double.PositiveInfinity;
        var maxX = double.NegativeInfinity; var maxZ = double.NegativeInfinity;
        for (var i = 0; i < fit.Samples.Count; i++)
        {
            var sample = fit.Samples[i]; var after = fit.Height(sample); var change = after - before[i];
            if (change == 0) continue;
            count++; if (Math.Abs(change) < 0.00000001) tiny++;
            maximum = Math.Max(maximum, Math.Abs(change)); minX = Math.Min(minX, sample.X); maxX = Math.Max(maxX, sample.X); minZ = Math.Min(minZ, sample.Z); maxZ = Math.Max(maxZ, sample.Z);
            b.Diagnostics.Add($"Road freeboard sample {phase}: source='{sample.Source.Value}', station={sample.Station:R}, world=({sample.X:R},{sample.Z:R}), before={before[i]:R}, after={after:R}, delta={change:R}, water='{fit.BindingWater(sample)}', lowerBound={fit.LowerBound(sample)?.Height.ToString("R", System.Globalization.CultureInfo.InvariantCulture) ?? "none"}, belowSolverComparison={Math.Abs(change) < 0.00000001}.");
        }
        // Include finite grading support in the bbox, since a changed sample may affect the
        // whole existing road shoulder/support; this bbox deliberately overstates influence.
        var changed = fit.Samples.Where((sample, i) => fit.Height(sample) != before[i]).Select(sample => sample.Source).ToHashSet();
        foreach (var entry in b.Roads.Where(entry => changed.Contains(entry.Road.Source)))
        {
            var node = b.Nodes.Single(value => value.Id == "auto-corridor-" + entry.Road.Feature.Value);
            minX = Math.Min(minX, node.Support.Min.X); minZ = Math.Min(minZ, node.Support.Min.Y); maxX = Math.Max(maxX, node.Support.Max.X); maxZ = Math.Max(maxZ, node.Support.Max.Y);
        }
        b.Diagnostics.Add($"Road freeboard changes {phase}: changedSamples={count}, maxAbsChange={maximum:R}, belowSolverComparisonCount={tiny}, influenceBBox={(count == 0 ? "empty" : $"({minX:R},{minZ:R})-({maxX:R},{maxZ:R})")}.");
    }

    private static void ReportHardPlanChange(Builder b, IReadOnlyList<FootwayPlan> before, IReadOnlyList<FootwayPlan> after, long iteration)
    {
        static string Signature(TerrainBridgeBanks.RoadSpan span) => $"{span.Polygon}/{span.Ring}:[{span.From:R},{span.To:R}]:{string.Join(",", span.Roads.Select(road => road.Road.Value))}";
        foreach (var plan in after)
        {
            var oldSpans = before.FirstOrDefault(value => value.Shore.Id == plan.Shore.Id)?.Spans ?? [];
            var old = oldSpans.Select(Signature).ToArray();
            var current = plan.Spans.Select(Signature).ToArray();
            if (iteration != 0 && old.SequenceEqual(current)) continue;
            bool Overlaps(TerrainBridgeBanks.RoadSpan a, TerrainBridgeBanks.RoadSpan c)
            {
                if (a.Polygon != c.Polygon || a.Ring != c.Ring) return false;
                return new[] { -a.RingLength, 0, a.RingLength }.Any(offset => a.From + offset <= c.To && a.To + offset >= c.From);
            }
            var disappeared = oldSpans.Count(span => !plan.Spans.Any(currentSpan => Overlaps(span, currentSpan)));
            var shortened = plan.Spans.Count(span => oldSpans.Any(oldSpan => Overlaps(oldSpan, span) && span.To - span.From < oldSpan.To - oldSpan.From));
            b.Diagnostics.Add($"Road freeboard Hard spans {(iteration == 0 ? "final" : $"iteration={iteration}")}: water='{plan.Shore.Id}', addedOrResized={current.Except(old).Count()}, removedOrResized={old.Except(current).Count()}, shortened={shortened}, disappeared={disappeared}, before={string.Join(";", old)}, after={string.Join(";", current)}; only actual replanning is exported, never closure support.");
        }
    }

    private static void ValidateCommittedFreeboard(Builder b, IReadOnlyList<FootwayPlan> plans, IReadOnlyDictionary<string, double> levels)
    {
        foreach (var plan in plans)
            foreach (var span in plan.Spans)
                foreach (var (a, c) in ShoreSupportSegments(plan.Shore.Banks!.RingPath(span), span.From - TerrainBridgeBanks.TransitionLength, span.To + TerrainBridgeBanks.TransitionLength))
                    foreach (var point in new[] { a, ((a.X + c.X) / 2, (a.Z + c.Z) / 2), c })
                    {
                        // Runs the final real composition (and its original hard rejection),
                        // including all contributor derivatives and Hard/Soft transitions.
                        _ = plan.Shore.Node.Apply(b.LandBeforeWater.Evaluate(point.Item1, point.Item2), point.Item1, point.Item2);
                        var required = levels[plan.Shore.Id] + 0.3;
                        var top = plan.Shore.Banks!.ProfileAt(point.Item1, point.Item2, TerrainDifferential.Constant(required)).Top;
                        if (top is { } actual && actual.Height < required - 0.000001)
                            throw UnsupportedFreeboard($"Final committed bank is below freeboard at ({point.Item1:R},{point.Item2:R}), water='{plan.Shore.Id}', top={actual.Height:R}, required={required:R}.");
                    }
    }

    private static TerrainDifferential RoadCoreTop(Builder b, RoadEntry entry, CorridorContact core, double x, double z)
    {
        var longitudinal = entry.Profile.SurfaceAt(entry.Road.Path, x, z, entry.Road.Width, entry.Road.Path.Project(x, z));
        var cross = entry.Operation is { } op ? TerrainDifferential.Constant(op.CrossFall) : ZeroBandWeight(b, x, z) * 0.015;
        var lateral = CorridorOffsetAt(entry.Road, core, x, z);
        var offset = IsMotorRoad(entry.Road) ? lateral * (lateral.Height > 0 ? -1 : lateral.Height < 0 ? 1 : 0) : lateral;
        return new(longitudinal.Height + cross.Height * offset.Height,
            longitudinal.Dx + cross.Height * offset.Dx + cross.Dx * offset.Height,
            longitudinal.Dz + cross.Height * offset.Dz + cross.Dz * offset.Height);
    }

    private static IReadOnlyList<ResolvedTerrainBank> ExportBanks(Builder b)
    {
        var result = new List<ResolvedTerrainBank>();
        foreach (var shore in b.WaterShores.OrderBy(shore => shore.Id, StringComparer.Ordinal))
        {
            var intervals = shore.OriginalHard.ToList();
            if (shore.Banks is { } bank)
            {
                intervals.AddRange(bank.Contacts.Select(contact => new TerrainBankInterval(contact.Polygon, contact.Ring,
                    contact.ShoreStation - TerrainBridgeBanks.HardHalfLength, contact.ShoreStation + TerrainBridgeBanks.HardHalfLength,
                    contact.RingLength, [], [contact.Bridge])));
                intervals.AddRange(bank.RoadSpans.Select(span => new TerrainBankInterval(span.Polygon, span.Ring, span.From, span.To,
                    span.RingLength, span.Roads.Select(road => road.Road).ToArray(), [])));
            }
            var index = 0;
            foreach (var interval in TerrainBankGeometry.Merge(intervals))
            {
                var id = $"{shore.Waters.OrderBy(water => water.Value, StringComparer.Ordinal).First().Value}:bank:{interval.Polygon}:{interval.Ring}:{index++}";
                var span = TerrainBankGeometry.Create(id, shore.Waters, shore.Region, interval);
                result.Add(new(span, shore.Node, b.LandBeforeWater));
            }
        }
        return result;
    }

    private static TerrainEvaluation ConstrainAutomaticRoadAtWater(Builder b, TerrainRoad road, string file, string id,
        TerrainEvaluation previous, TerrainEvaluation proposed, double x, double z)
    {
        if (b.Water.Count == 0 || proposed.Surface == previous.Surface) return proposed;
        var roadDistance = CorridorContactAt(road, 0, x, z).Distance;
        if (roadDistance.Height < 0) return proposed; // The road core is fixed; incompatible core/shore contact fails validation.
        WaterShore? nearest = null; var waterDistance = new TerrainDifferential(double.PositiveInfinity, 0, 0);
        // The compiled union owns the exposed shore. Member polygons retain identity only;
        // their internal edges must never become grading constraints or projection tangents.
        foreach (var water in b.WaterShores)
        {
            var distance = water.Region.Distance(x, z);
            if (distance.Height < waterDistance.Height) { nearest = water; waterDistance = distance; }
        }
        if (nearest is null) return proposed;
        if (roadDistance.Height == 0 && waterDistance.Height <= 0)
        {
            if (Math.Abs(proposed.Surface.Height - previous.Surface.Height) <= 0.000001) return previous;
            throw new WorldDataException(file, id, $"Fixed road core and water '{nearest.Id}' meet without room for a shoulder/slope at ({x:R},{z:R}).");
        }
        if (waterDistance.Height <= 0) return previous; // Signed interior: no road-side fill can reappear inside water.
        var weight = WaterSideWeight(waterDistance, roadDistance);
        var delta = proposed.Surface - previous.Surface;
        var surface = previous.Surface + new TerrainDifferential(delta.Height * weight.Height,
            delta.Dx * weight.Height + delta.Height * weight.Dx,
            delta.Dz * weight.Height + delta.Height * weight.Dz);
        var profile = default(RoadBoundaryProfile);
        if (b.AutomaticRoads[id].Grading?.FixedBoundaryProfile == true)
            surface = FixedBoundaryRoadSurface(b, b.AutomaticRoads[id], nearest, waterDistance, roadDistance, previous.Surface, proposed.Surface, x, z, out profile);
        if (surface == previous.Surface) return previous;
        var transverse = Math.Abs(surface.Dx * roadDistance.Dx + surface.Dz * roadDistance.Dz);
        var total = Math.Sqrt(surface.Dx * surface.Dx + surface.Dz * surface.Dz);
        if (total > 1.000001 || previous.Class != TerrainClass.Transition && (total > 0.500001 || transverse > 1d / 3 + 0.000001))
            throw new WorldDataException(file, id, $"Road-water side envelope cannot fit its grade: water='{nearest.Id}', point=({x:R},{z:R}), transverseGrade={transverse:R}, totalGrade={total:R}, roadDistance={roadDistance.Height:R}, waterDistance={waterDistance.Height:R}, previous={previous.Surface}, proposed={proposed.Surface}, final={surface}, class={previous.Class}, shoulder={b.AutomaticRoads[id].Grading!.Width:R}, profile={profile}.")
            { CanNarrowAutomaticRoad = true };
        return WithSurface(previous, surface, proposed.CrestDistance, Math.Min(proposed.ToeDistance, waterDistance.Height));
    }

    private readonly record struct RoadBoundaryProfile(double X, double Z, double Tx, double Tz,
        TerrainDifferential RawBank, TerrainDifferential Bank, TerrainDifferential Core);

    private static TerrainDifferential FixedBoundaryRoadSurface(Builder b, RoadEntry entry, WaterShore water,
        TerrainDifferential waterDistance, TerrainDifferential roadDistance, TerrainDifferential previous,
        TerrainDifferential proposed, double x, double z, out RoadBoundaryProfile profile)
    {
        var bank = water.Region.NearestBoundary(x, z);
        var atBank = b.LandBeforeCorridors.Evaluate(bank.X, bank.Z).Surface;
        var bankTangent = atBank.Dx * bank.Tx + atBank.Dz * bank.Tz;
        var a = new TerrainDifferential(atBank.Height, bankTangent * bank.Tx, bankTangent * bank.Tz);
        var c = RoadCoreTop(b, entry, CorridorContactAt(entry.Road, 0, x, z), x, z);
        profile = new(bank.X, bank.Z, bank.Tx, bank.Tz, atBank, a, c);
        var gap = waterDistance + roadDistance; var change = c - a;
        var t = waterDistance.Height / gap.Height;
        var tx = (waterDistance.Dx * gap.Height - waterDistance.Height * gap.Dx) / (gap.Height * gap.Height);
        var tz = (waterDistance.Dz * gap.Height - waterDistance.Height * gap.Dz) / (gap.Height * gap.Height);
        var peak = 1.5; var px = 0d; var pz = 0d;
        if (Math.Abs(change.Height) > 0.000000001)
        {
            var magnitude = Math.Abs(change.Height); var sign = change.Height > 0 ? 1 : -1;
            var limit = gap.Height / (3 * magnitude);
            if (limit <= 1) throw new WorldDataException(entry.File, "auto-corridor-" + entry.Road.Feature.Value,
                $"Fixed water and road-core heights require a mean side grade above 1:3; neither height can be silently moved. point=({x:R},{z:R}), gap={gap.Height:R}, bank={a}, core={c}, previous={previous}, proposed={proposed}, profile={profile}.");
            peak = Math.Min(1.5, (1 + limit) / 2);
            if (peak < 1.5)
            {
                px = (gap.Dx / magnitude - gap.Height * sign * change.Dx / (magnitude * magnitude)) / 6;
                pz = (gap.Dz / magnitude - gap.Height * sign * change.Dz / (magnitude * magnitude)) / 6;
            }
        }
        var (value, derivative) = LimitedEase(t, peak); var dp = LimitedEasePeakDerivative(t, peak);
        var result = a + new TerrainDifferential(change.Height * value,
            change.Dx * value + change.Height * (derivative * tx + dp * px),
            change.Dz * value + change.Height * (derivative * tz + dp * pz));
        // This is a restriction of the original finite grading envelope. It cannot reverse a
        // fill into a cut, or introduce a new influence where the original operation was identity.
        if (proposed.Height >= previous.Height)
            return result.Height <= previous.Height ? previous : result.Height >= proposed.Height ? proposed : result;
        return result.Height >= previous.Height ? previous : result.Height <= proposed.Height ? proposed : result;
    }

    private static double LimitedEasePeakDerivative(double t, double peak)
    {
        if (t <= 0 || t >= 1 || peak >= 1.5) return 0;
        var end = 1 - 1 / peak;
        if (t < end) return t * t / (2 * end) - t * t / (2 * end * end * peak);
        if (t > 1 - end) return -(1 - t) * (1 - t) / (2 * end) + (1 - t) * (1 - t) / (2 * end * end * peak);
        return t - 0.5;
    }

    internal static TerrainDifferential WaterSideWeight(TerrainDifferential water, TerrainDifferential road)
    {
        if (road.Height < 0) throw new ArgumentException("Water-side weight is only defined outside the fixed road core.");
        if (water.Height <= 0)
        {
            if (road.Height == 0) throw new ArgumentException("Coincident road and water boundaries have no interpolation interval.");
            return TerrainDifferential.Constant(0);
        }
        var denominator = water.Height + road.Height;
        if (!(denominator > 0) || !double.IsFinite(denominator)) throw new ArgumentException("Invalid road-water boundary interval.");
        var t = water.Height / denominator; var derivative = 6 * t * (1 - t);
        return new(TerrainMath.Hermite(t), derivative * (water.Dx * road.Height - water.Height * road.Dx) / (denominator * denominator),
            derivative * (water.Dz * road.Height - water.Height * road.Dz) / (denominator * denominator));
    }
}

/// <summary>Actual bridge/ordered-shore intersections. No names, nearest-road guesses or map-specific IDs.</summary>
internal sealed class TerrainBridgeBanks
{
    internal sealed record Contact(FeatureId Bridge, int Polygon, int Ring, double ShoreStation, double RingLength, Vector2 Position);
    internal sealed record RoadContact(FeatureId Road, MapRect Bounds, IReadOnlyList<Vector2> CriticalPoints,
        Func<double, double, (TerrainDifferential Distance, TerrainDifferential Top)?> Evaluate)
    { public Func<double, double, string>? Explain { get; init; } }
    internal sealed record RoadSpan(int Polygon, int Ring, double From, double To, double RingLength,
        IReadOnlyList<RoadContact> Roads);
    private readonly (int Polygon, int Ring, TerrainPath Path)[] _rings;
    private RoadSpan[] _roadSpans = [];
    private bool _footwaysCommitted;
    public IReadOnlyList<Contact> Contacts { get; }
    public IReadOnlyList<RoadSpan> RoadSpans => _roadSpans;
    internal const double HardHalfLength = 5; // Opus contract-15, 2026-10-16 11:14.
    internal const double TransitionLength = 2; // Opus contract-15, 2026-10-16 11:49.
    internal const double FootwayExtension = 3; // Opus 2026-10-16 13:39.

    public TerrainBridgeBanks(TerrainRegion region, IReadOnlyList<NetworkLine> bridges)
    {
        var rings = new List<(int Polygon, int Ring, TerrainPath Path)>();
        for (var polygon = 0; polygon < region.Polygons.Length; polygon++)
        {
            var shape = region.Polygons[polygon]; var index = 0;
            foreach (var ring in new[] { shape.Outer }.Concat(shape.Holes))
                rings.Add((polygon, index++, new(new(ring.Append(ring[0]).ToArray()))));
        }
        _rings = rings.ToArray(); var contacts = new List<Contact>();
        foreach (var bridge in bridges.OrderBy(bridge => bridge.Id.Value, StringComparer.Ordinal))
        {
            var path = new TerrainPath(bridge.Line);
            foreach (var ring in _rings)
                foreach (var hit in ring.Path.Intersections(path))
                {
                    // A tangential touch is not an abutment. Resolve the two sides on the
                    // registered bridge itself; centimetre source precision is a numerical probe,
                    // never a search radius for an unrelated bridge.
                    var before = hit.OtherStation > 0 ? path.At(Math.Max(0, hit.OtherStation - 0.01)) : (Vector2?)null;
                    var after = hit.OtherStation < path.Length ? path.At(Math.Min(path.Length, hit.OtherStation + 0.01)) : (Vector2?)null;
                    var beforeInside = before is { } a && region.Contains(a.X, a.Y);
                    var afterInside = after is { } c && region.Contains(c.X, c.Y);
                    if (beforeInside == afterInside) continue;
                    var station = hit.Station == ring.Path.Length ? 0 : hit.Station;
                    if (contacts.Any(contact => contact.Bridge == bridge.Id && contact.Polygon == ring.Polygon
                        && contact.Ring == ring.Ring && Math.Abs(contact.ShoreStation - station) < 0.000001)) continue;
                    contacts.Add(new(bridge.Id, ring.Polygon, ring.Ring, station, ring.Path.Length, new((float)hit.X, (float)hit.Z)));
                }
        }
        Contacts = contacts.OrderBy(contact => contact.Polygon).ThenBy(contact => contact.Ring)
            .ThenBy(contact => contact.ShoreStation).ThenBy(contact => contact.Bridge.Value, StringComparer.Ordinal).ToArray();
    }

    public (double AlongDistance, Contact? Contact) NearestContact(double x, double z)
    {
        var closestRing = -1; var nearest = double.PositiveInfinity; var station = 0d;
        for (var i = 0; i < _rings.Length; i++)
        {
            var hit = _rings[i].Path.Project(x, z);
            if (hit.Distance >= nearest) continue;
            closestRing = i; nearest = hit.Distance; station = hit.Station;
        }
        if (closestRing < 0) return (double.PositiveInfinity, null);
        var ring = _rings[closestRing]; Contact? contact = null; var along = double.PositiveInfinity;
        foreach (var candidate in Contacts)
        {
            if (candidate.Polygon != ring.Polygon || candidate.Ring != ring.Ring) continue;
            var distance = Math.Abs(station - candidate.ShoreStation);
            distance = Math.Min(distance, candidate.RingLength - distance);
            if (distance < along) { along = distance; contact = candidate; }
        }
        return (along, contact);
    }

    public TerrainDifferential HardWeight(double x, double z)
        => ProfileAt(x, z).Weight;

    public (TerrainDifferential Weight, TerrainDifferential? Top) ProfileAt(double x, double z, TerrainDifferential? originalTop = null)
        => ProfileAtPlan(x, z, _roadSpans, originalTop);

    internal (TerrainDifferential Weight, TerrainDifferential? Top) ProfileAtPlan(double x, double z,
        IReadOnlyList<RoadSpan> spans, TerrainDifferential? originalTop = null)
    {
        var ringIndex = -1; var nearest = double.PositiveInfinity; var hit = default(TerrainPathHit);
        for (var i = 0; i < _rings.Length; i++)
        {
            var candidate = _rings[i].Path.Project(x, z);
            if (candidate.Distance >= nearest) continue;
            ringIndex = i; nearest = candidate.Distance; hit = candidate;
        }
        if (ringIndex < 0) return (TerrainDifferential.Constant(0), null);
        var ring = _rings[ringIndex]; var shortest = double.PositiveInfinity; var signed = 0d;
        foreach (var contact in Contacts)
        {
            if (contact.Polygon != ring.Polygon || contact.Ring != ring.Ring) continue;
            var delta = hit.Station - contact.ShoreStation;
            if (delta > contact.RingLength / 2) delta -= contact.RingLength;
            if (delta < -contact.RingLength / 2) delta += contact.RingLength;
            if (Math.Abs(delta) < shortest) { shortest = Math.Abs(delta); signed = delta; }
        }
        var weight = Taper(shortest - HardHalfLength, signed < 0 ? -1 : 1);
        TerrainDifferential? top = null;
        foreach (var span in spans)
        {
            if (span.Polygon != ring.Polygon || span.Ring != ring.Ring) continue;
            var station = hit.Station;
            while (station < span.From - TransitionLength) station += span.RingLength;
            while (station > span.To + TransitionLength && station - span.RingLength >= span.From - TransitionLength) station -= span.RingLength;
            var outside = station < span.From ? span.From - station : station > span.To ? station - span.To : 0;
            var candidate = Taper(outside, station < span.From ? -1 : 1);
            if (candidate.Height == 0) continue;
            var contributors = new List<(FeatureId Road, TerrainDifferential Distance, TerrainDifferential Top)>();
            foreach (var road in span.Roads.OrderBy(road => road.Road.Value, StringComparer.Ordinal))
            {
                var contact = EvaluateRoadContact(road, x - hit.Distance * hit.DistanceDx, z - hit.Distance * hit.DistanceDz);
                if (contact is { } value) contributors.Add((road.Road, AlongShore(value.Distance), AlongShore(value.Top)));
            }
            if (contributors.Count == 0) continue;
            var cores = contributors.Where(value => value.Distance.Height == 0).ToArray();
            TerrainDifferential roadTop;
            if (cores.Length > 0)
            {
                // A fixed core is never averaged away. Equal-height cores retain the first
                // ordinal road's derivative; incompatible shared-core heights remain an error.
                roadTop = cores[0].Top;
                if (cores.Any(value => Math.Abs(value.Top.Height - roadTop.Height) > 0.0001))
                    throw new WorldDataException("network.json", string.Join(",", cores.Select(value => value.Road.Value)),
                        $"Coincident road cores have incompatible bank tops at ({x:R},{z:R}): "
                        + string.Join("; ", cores.Select(value => $"{value.Road.Value}={value.Top.Height:R}")));
            }
            else if (contributors.Count == 1) roadTop = contributors[0].Top;
            else
            {
                var scale = contributors.Min(value => value.Distance.Height);
                var influences = contributors.Select(value =>
                {
                    var ratio = scale / value.Distance.Height;
                    return ratio * ratio;
                }).ToArray();
                var sum = 0d; var height = 0d;
                for (var i = 0; i < contributors.Count; i++)
                {
                    sum += influences[i]; height += influences[i] * contributors[i].Top.Height;
                }
                height /= sum;
                var dx = 0d; var dz = 0d;
                for (var i = 0; i < contributors.Count; i++)
                {
                    var value = contributors[i]; var influence = influences[i];
                    // The common scale² cancels in the normalized quotient, including its
                    // derivative. Every individual inverse-distance derivative remains here.
                    var distanceTerm = -2 * influence * ((value.Top.Height - height) / value.Distance.Height);
                    dx += influence * value.Top.Dx + distanceTerm * value.Distance.Dx;
                    dz += influence * value.Top.Dz + distanceTerm * value.Distance.Dz;
                }
                roadTop = new(height, dx / sum, dz / sum);
                if (!Finite(roadTop)) throw new WorldDataException("network.json", string.Join(",", contributors.Select(value => value.Road.Value)),
                    $"Road bank composition is non-finite at ({x:R},{z:R}).");
            }
            // Smooth union of overlapping transition bands. Choosing the largest weight and
            // switching its road would jump in height when the two frozen road profiles differ.
            var retained = Product(weight, TerrainDifferential.Constant(1) - candidate);
            var combined = retained + candidate;
            var numerator = Product(top ?? originalTop ?? TerrainDifferential.Constant(0), retained) + Product(roadTop, candidate);
            top = new(numerator.Height / combined.Height,
                (numerator.Dx * combined.Height - numerator.Height * combined.Dx) / (combined.Height * combined.Height),
                (numerator.Dz * combined.Height - numerator.Height * combined.Dz) / (combined.Height * combined.Height));
            weight = combined;
        }
        return (weight, top);

        TerrainDifferential Taper(double outside, double direction)
        {
            if (outside <= 0) return TerrainDifferential.Constant(1);
            if (outside >= TransitionLength) return TerrainDifferential.Constant(0);
            var t = outside / TransitionLength; var slope = -6 * t * (1 - t) / TransitionLength * direction;
            return new(1 - TerrainMath.Hermite(t), slope * hit.StationDx, slope * hit.StationDz);
        }
        TerrainDifferential AlongShore(TerrainDifferential value)
        {
            var tangent = value.Dx * hit.StationDx + value.Dz * hit.StationDz;
            return new(value.Height, tangent * hit.StationDx, tangent * hit.StationDz);
        }
        static TerrainDifferential Product(TerrainDifferential a, TerrainDifferential c) =>
            new(a.Height * c.Height, a.Dx * c.Height + a.Height * c.Dx, a.Dz * c.Height + a.Height * c.Dz);
    }

    public string ExplainProfileAt(double x, double z)
    {
        var ringIndex = -1; var nearest = double.PositiveInfinity; var hit = default(TerrainPathHit);
        for (var i = 0; i < _rings.Length; i++)
        {
            var candidate = _rings[i].Path.Project(x, z);
            if (candidate.Distance >= nearest) continue;
            ringIndex = i; nearest = candidate.Distance; hit = candidate;
        }
        if (ringIndex < 0) return "No shore ring resolves at the failing point.";
        var ring = _rings[ringIndex];
        var sx = x - hit.Distance * hit.DistanceDx; var sz = z - hit.Distance * hit.DistanceDz;
        var details = new List<string>();
        foreach (var span in _roadSpans)
        {
            if (span.Polygon != ring.Polygon || span.Ring != ring.Ring) continue;
            var station = hit.Station;
            while (station < span.From - TransitionLength) station += span.RingLength;
            while (station > span.To + TransitionLength && station - span.RingLength >= span.From - TransitionLength) station -= span.RingLength;
            var outside = station < span.From ? span.From - station : station > span.To ? station - span.To : 0;
            var taper = outside <= 0 ? 1 : outside >= TransitionLength ? 0 : 1 - TerrainMath.Hermite(outside / TransitionLength);
            if (taper == 0) continue;
            var contributors = new List<(FeatureId Road, double CoreDistance, double IdwDistance, double Top)>();
            foreach (var road in span.Roads.OrderBy(road => road.Road.Value, StringComparer.Ordinal))
            {
                if (road.Evaluate(sx, sz) is not { } value) continue;
                contributors.Add((road.Road, value.Distance.Height, Math.Max(0, value.Distance.Height), value.Top.Height));
            }
            if (contributors.Count == 0) continue;
            var firstCore = contributors.FindIndex(value => value.IdwDistance == 0);
            var influences = new double[contributors.Count];
            if (firstCore >= 0) influences[firstCore] = 1;
            else
            {
                var scale = contributors.Min(value => value.IdwDistance);
                for (var i = 0; i < contributors.Count; i++)
                {
                    var ratio = scale / contributors[i].IdwDistance;
                    influences[i] = ratio * ratio;
                }
            }
            var sum = influences.Sum(); var top = 0d;
            for (var i = 0; i < contributors.Count; i++) top += influences[i] * contributors[i].Top / sum;
            details.Add($"span=[{span.From:R},{span.To:R}], alignedStation={station:R}, transitionWeight={taper:R}, blend={(firstCore >= 0 ? "fixed-core" : "inverse-distance-squared")}, spanTop={top:R}; "
                + string.Join("; ", contributors.Select((value, i) => $"road='{value.Road.Value}', shoreCoreDistance={value.CoreDistance:R}, idwDistance={value.IdwDistance:R}, ownTop={value.Top:R}, normalizedWeight={influences[i] / sum:R}")));
        }
        return $"shore=({sx:R},{sz:R}), ring={ring.Polygon}/{ring.Ring}, shoreStation={hit.Station:R}; " + string.Join(" | ", details);
    }

    private static bool Finite(TerrainDifferential value) =>
        double.IsFinite(value.Height) && double.IsFinite(value.Dx) && double.IsFinite(value.Dz);

    private static (TerrainDifferential Distance, TerrainDifferential Top)? EvaluateRoadContact(RoadContact road, double x, double z)
    {
        if (road.Evaluate(x, z) is not { } value) return null;
        if (!Finite(value.Distance) || !Finite(value.Top))
            throw new WorldDataException("network.json", road.Road.Value, $"Road bank contact is non-finite at ({x:R},{z:R}).");
        return (value.Distance.Height < 0 ? TerrainDifferential.Constant(0) : value.Distance, value.Top);
    }

    public void ResolveFootways(IReadOnlyList<RoadContact> roads, Func<double, double, double?> softLevel)
        => CommitFootways(PlanFootways(roads, softLevel));

    // Planning never mutates this object, which is already referenced by the soft base field.
    // The compiler may replan with a new trial profile; only the final real plan is committed.
    internal IReadOnlyList<RoadSpan> PlanFootways(IReadOnlyList<RoadContact> roads, Func<double, double, double?> softLevel)
    {
        if (_footwaysCommitted) throw new InvalidOperationException("Road banks are frozen for this terrain snapshot.");
        var spans = new List<RoadSpan>();
        foreach (var ring in _rings)
        {
            var candidates = roads.Where(road => road.Bounds.Min.X <= ring.Path.Bounds.Max.X && road.Bounds.Max.X >= ring.Path.Bounds.Min.X
                && road.Bounds.Min.Y <= ring.Path.Bounds.Max.Y && road.Bounds.Max.Y >= ring.Path.Bounds.Min.Y)
                .OrderBy(road => road.Road.Value, StringComparer.Ordinal).ToArray();
            if (candidates.Length == 0) continue;
            var stations = new SortedSet<double> { 0, ring.Path.Length };
            for (var s = 0.25; s < ring.Path.Length; s += 0.25) stations.Add(s);
            foreach (var p in ring.Path.Line.Points) stations.Add(ring.Path.Project(p.X, p.Y).Station);
            foreach (var road in candidates)
                foreach (var p in road.CriticalPoints) stations.Add(ring.Path.Project(p.X, p.Y).Station);
            var samples = stations.ToArray(); var conflicts = samples.Select(Conflict).ToArray();
            var intervals = new List<(double From, double To)>(); double? from = conflicts[0] ? 0 : null;
            for (var i = 1; i < samples.Length; i++)
            {
                if (conflicts[i] == conflicts[i - 1]) continue;
                var low = samples[i - 1]; var high = samples[i];
                for (var step = 0; step < 32 && high - low > 0.000001; step++)
                {
                    var middle = (low + high) / 2;
                    if (Conflict(middle) == conflicts[i - 1]) low = middle; else high = middle;
                }
                var crossing = (low + high) / 2;
                if (conflicts[i]) from = crossing;
                else { intervals.Add((from!.Value, crossing)); from = null; }
            }
            if (from is { } tail) intervals.Add((tail, ring.Path.Length));
            if (intervals.Count == 0) continue;
            if (intervals.Count > 1 && intervals[0].From == 0 && intervals[^1].To == ring.Path.Length)
            {
                var wrapped = (intervals[^1].From, intervals[0].To + ring.Path.Length);
                intervals.RemoveAt(intervals.Count - 1); intervals.RemoveAt(0); intervals.Add(wrapped);
            }
            var extended = intervals.Select(value => (From: value.From - FootwayExtension, To: value.To + FootwayExtension)).OrderBy(value => value.From).ToList();
            var merged = new List<(double From, double To)>();
            foreach (var interval in extended)
            {
                if (merged.Count > 0 && interval.From <= merged[^1].To)
                    merged[^1] = (merged[^1].From, Math.Max(merged[^1].To, interval.To));
                else merged.Add(interval);
            }
            if (merged.Count > 1 && merged[^1].To >= merged[0].From + ring.Path.Length)
            {
                var combined = (merged[^1].From, merged[0].To + ring.Path.Length);
                merged.RemoveAt(merged.Count - 1); merged.RemoveAt(0); merged.Add(combined);
            }
            foreach (var interval in merged.OrderBy(value => value.From))
            {
                var start = interval.From; var end = interval.To;
                if (end - start >= ring.Path.Length) { start = 0; end = ring.Path.Length; }
                var contributors = candidates.Where(road => samples.Any(station =>
                {
                    var unwrapped = station < start ? station + ring.Path.Length : station;
                    if (unwrapped < start || unwrapped > end) return false;
                    var p = ring.Path.At(station); var level = softLevel(p.X, p.Y);
                    return level is { } y && EvaluateRoadContact(road, p.X, p.Y) is { } contact
                        && Math.Abs(contact.Top.Height - y) > contact.Distance.Height / 3 + 0.000001;
                })).ToArray();
                if (contributors.Length == 0) throw new InvalidOperationException("A resolved Hard interval lost its actual source road.");
                spans.Add(new(ring.Polygon, ring.Ring, start, end, ring.Path.Length, contributors));
            }

            bool Conflict(double station)
            {
                var p = ring.Path.At(station); var level = softLevel(p.X, p.Y);
                if (level is null) return false;
                foreach (var road in candidates)
                    if (EvaluateRoadContact(road, p.X, p.Y) is { } contact && contact.Distance.Height >= 0
                        && Math.Abs(contact.Top.Height - level.Value) > contact.Distance.Height / 3 + 0.000001) return true;
                return false;
            }
        }
        return spans.OrderBy(span => span.Polygon).ThenBy(span => span.Ring).ThenBy(span => span.From).ToArray();
    }

    internal void CommitFootways(IReadOnlyList<RoadSpan> spans)
    {
        if (_footwaysCommitted) throw new InvalidOperationException("Road bank spans are committed once per terrain snapshot.");
        _roadSpans = spans.ToArray(); _footwaysCommitted = true;
    }

    internal TerrainPath RingPath(RoadSpan span) => _rings.Single(ring => ring.Polygon == span.Polygon && ring.Ring == span.Ring).Path;

    public Vector2 At(RoadSpan span, double station)
    {
        var path = _rings.Single(ring => ring.Polygon == span.Polygon && ring.Ring == span.Ring).Path;
        station %= path.Length; if (station < 0) station += path.Length;
        return path.At(station);
    }
}

internal static class TerrainBankGeometry
{
    public static IEnumerable<(int Polygon, int Ring, TerrainPath Path)> Rings(TerrainRegion region)
    {
        for (var polygon = 0; polygon < region.Polygons.Length; polygon++)
        {
            var index = 0;
            foreach (var ring in new[] { region.Polygons[polygon].Outer }.Concat(region.Polygons[polygon].Holes))
                yield return (polygon, index++, new TerrainPath(new(ring.Append(ring[0]).ToArray())));
        }
    }

    public static IReadOnlyList<TerrainBankInterval> WholeRings(TerrainRegion region) => Rings(region)
        .Select(ring => new TerrainBankInterval(ring.Polygon, ring.Ring, 0, ring.Path.Length, ring.Path.Length, [], [])).ToArray();

    public static IReadOnlyList<TerrainBankInterval> Merge(IEnumerable<TerrainBankInterval> intervals)
    {
        var result = new List<TerrainBankInterval>();
        foreach (var group in intervals.GroupBy(span => (span.Polygon, span.Ring)).OrderBy(group => group.Key.Polygon).ThenBy(group => group.Key.Ring))
        {
            var merged = new List<TerrainBankInterval>();
            foreach (var interval in group.Select(Normalize).OrderBy(span => span.From).ThenBy(span => span.To))
            {
                if (merged.Count > 0 && interval.From <= merged[^1].To) merged[^1] = Combine(merged[^1], interval, interval.To);
                else merged.Add(interval);
            }
            while (merged.Count > 1 && merged[^1].To >= merged[0].From + merged[0].Length)
            {
                var combined = Combine(merged[^1], merged[0], merged[0].To + merged[0].Length);
                merged.RemoveAt(merged.Count - 1); merged.RemoveAt(0); merged.Add(combined);
            }
            result.AddRange(merged.Select(Normalize).OrderBy(span => span.From));
        }
        return result;

        static TerrainBankInterval Normalize(TerrainBankInterval value)
        {
            var extent = value.To - value.From;
            if (extent >= value.Length) return value with { From = 0, To = value.Length };
            var from = value.From % value.Length; if (from < 0) from += value.Length;
            return value with { From = from, To = from + extent };
        }
        static TerrainBankInterval Combine(TerrainBankInterval a, TerrainBankInterval c, double end) => a with
        {
            To = Math.Max(a.To, end),
            Roads = a.Roads.Concat(c.Roads).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal).ToArray(),
            Bridges = a.Bridges.Concat(c.Bridges).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal).ToArray()
        };
    }

    public static TerrainBankSpan Create(string id, IReadOnlyList<FeatureId> waters, TerrainRegion region, TerrainBankInterval interval)
    {
        var path = Rings(region).Single(ring => ring.Polygon == interval.Polygon && ring.Ring == interval.Ring).Path;
        if (interval.To - interval.From >= path.Length)
            return new(id, waters, interval.Roads, interval.Bridges, path.Line, 0, path.Length, 0, TerrainBankKind.Hard);
        var transition = TerrainBridgeBanks.TransitionLength;
        if (interval.To - interval.From + 2 * transition >= path.Length)
            throw new WorldDataException("overlay/terrain", id,
                "A partial Hard ring has overlapping end transitions around its entire seam; author an explicit complete bank or split before exporting structural geometry.");
        var start = interval.From - transition; var end = interval.To + transition;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var stations = new SortedSet<double> { start, interval.From, interval.To, end };
            var cumulative = 0d;
            for (var i = 0; i < path.Line.Points.Count; i++)
            {
                if (i > 0)
                {
                    var a = path.Line.Points[i - 1]; var c = path.Line.Points[i]; var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
                    cumulative += Math.Sqrt(dx * dx + dz * dz);
                }
                for (var turn = (int)Math.Floor(start / path.Length); turn <= (int)Math.Ceiling(end / path.Length); turn++)
                {
                    var candidate = cumulative + turn * path.Length;
                    if (candidate > start && candidate < end) stations.Add(candidate);
                }
            }
            var points = new List<(Vector2 Point, bool From, bool To)>();
            foreach (var station in stations)
            {
                var wrapped = station % path.Length; if (wrapped < 0) wrapped += path.Length;
                var point = path.At(wrapped); var from = station == interval.From; var to = station == interval.To;
                if (points.Count > 0 && points[^1].Point == point)
                    points[^1] = (point, points[^1].From || from, points[^1].To || to);
                else points.Add((point, from, to));
            }
            var coreFrom = 0d; var coreTo = 0d; var length = 0d;
            for (var i = 0; i < points.Count; i++)
            {
                if (i > 0)
                {
                    var dx = (double)points[i].Point.X - points[i - 1].Point.X; var dz = (double)points[i].Point.Y - points[i - 1].Point.Y;
                    length += Math.Sqrt(dx * dx + dz * dz);
                }
                if (points[i].From) coreFrom = length;
                if (points[i].To) coreTo = length;
            }
            if (coreFrom >= transition && coreTo <= length - transition)
                return new(id, waters, interval.Roads, interval.Bridges, new(points.Select(point => point.Point).ToArray()),
                    coreFrom, coreTo, transition, TerrainBankKind.Hard);
            // Only compensate final Vector2 rounding: an exported 2 m margin must not become
            // 1.99999 m and violate the shared exact-station contract. The field is unchanged.
            var padding = points.Max(point => 4 * (Math.Abs((double)MathF.BitIncrement(point.Point.X) - point.Point.X)
                + Math.Abs((double)MathF.BitIncrement(point.Point.Y) - point.Point.Y)));
            if (coreFrom < transition) start -= transition - coreFrom + padding;
            if (coreTo > length - transition) end += transition - (length - coreTo) + padding;
        }
        throw new WorldDataException("overlay/terrain", id, "Final shore coordinates cannot retain both exact 2 m transition margins.");
    }
}
