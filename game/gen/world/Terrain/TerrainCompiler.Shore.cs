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
                        var pointHit = entry.Road.Path.Project(x, z);
                        if (entry.Profile.Excluded(pointHit.Station)) return null;
                        var contact = CorridorContactAt(entry.Road, 0, x, z);
                        var hit = entry.Road.Path.Project(contact.X, contact.Z);
                        if (entry.Profile.Excluded(hit.Station)) return null;
                        return (contact.Distance, RoadCoreTop(b, entry, contact, pointHit, x, z));
                    })
                {
                    Explain = (x, z) =>
                    {
                        var pointHit = entry.Road.Path.Project(x, z);
                        var core = CorridorContactAt(entry.Road, 0, x, z); var hit = entry.Road.Path.Project(core.X, core.Z);
                        if (entry.Profile.Excluded(pointHit.Station) || entry.Profile.Excluded(hit.Station))
                            return $"{entry.Road.Source.Value}: structural interval has no ground-road bank contact; pointStation={pointHit.Station:R}, coreStation={hit.Station:R}.";
                        return $"{entry.Road.Source.Value}: core=({core.X:R},{core.Z:R}), segment={hit.Segment}, station={hit.Station:R}, offset={hit.Offset:R}, profile={entry.Profile.At(hit.Station)}, top={RoadCoreTop(b, entry, core, pointHit, x, z)}";
                    }
                }).ToArray();
        var shores = b.WaterShores.Where(shore => shore.Banks is not null).OrderBy(shore => shore.Id, StringComparer.Ordinal).ToArray();
        var fit = b.RoadFit ?? throw new InvalidOperationException("The shared road-network objective has not been frozen.");
        var levels = shores.ToDictionary(shore => shore.Id, shore => b.Water.First(water => shore.Waters.Contains(water.Feature)).Level);
        // Candidate values depend only on resolved water levels and frozen real core geometry.
        // The finite candidate identities are frozen here; geometry-only values are memoized
        // lazily once for participating roads, and never depend on the current trial profile.
        var radii = entries.ToDictionary(entry => entry.Road.Source, entry => new Lazy<double>(() => RoadCorePathRadius(entry.Road, b.References.SourceFiles(entry.Road.Source))));
        var candidates = (from shore in shores from entry in entries where entry.Operation is null || entry.Plan is not null
            select (Key: (shore.Id, entry.Road.Source), Value: new Lazy<TerrainRoadProfile.Bound>(() => new(
                levels[shore.Id] + 0.3 + entry.MaximumCrossFall * radii[entry.Road.Source].Value, string.Join(",", shore.Waters.OrderBy(w => w.Value, StringComparer.Ordinal).Select(w => w.Value))))))
            .ToDictionary(item => item.Key, item => item.Value);
        var maximumIterations = checked(1L + (long)fit.NodeCount * candidates.Count);
        FootwayPlan[] final = [];
        var completed = false; var iterations = 0L;
        for (var iteration = 1L; iteration <= maximumIterations; iteration++)
        {
            iterations = iteration;
            // Banks still contain only original bridge contacts here, so CheckLevel/Node.Apply
            // see the same non-road soft field in every pass. Plans are disposable pure data.
            var plans = shores.Select(shore => new FootwayPlan(shore, shore.Banks!.PlanFootways(roads,
                (x, z) => shore.CheckLevel(x, z) ? shore.Node.Apply(b.LandBeforeWater.Evaluate(x, z), x, z).Surface.Height : null))).ToArray();
            var raisedBounds = 0;
            foreach (var plan in plans)
            {
                foreach (var span in plan.Spans)
                {
                    var path = plan.Shore.Banks!.RingPath(span);
                    foreach (var contact in span.Roads)
                    {
                        var entry = bySource[contact.Road];
                        if (entry.Operation is not null && entry.Plan is null) continue;
                        var bound = candidates[(plan.Shore.Id, contact.Road)].Value;
                        foreach (var (a, c) in ShoreSupportSegments(path, span.From - TerrainBridgeBanks.TransitionLength, span.To + TerrainBridgeBanks.TransitionLength))
                            foreach (var index in AutomaticFreeboardSupport(entry.Road, entry.Profile, a, c))
                                if (fit.Require(contact.Road, index, bound)) raisedBounds++;
                    }
                }
            }
            b.Diagnostics.Add($"Road freeboard closure iteration={iteration}, newOrRaisedNodeBounds={raisedBounds}, actualHardSpans={plans.Sum(plan => plan.Spans.Count)}, finiteOuterLimit={maximumIterations}; frozen soft targets/geometry/water/grades retained.");
            if (raisedBounds == 0) { final = plans; completed = true; break; }
            var profiles = fit.Solve();
            foreach (var entry in b.Roads.Where(entry => entry.Operation is null || entry.Plan is not null)) entry.ReplaceProfile(profiles[entry.Road.Source]);
        }
        if (!completed) throw UnsupportedFreeboard("Finite conservative freeboard closure did not converge; no approximate profile/bank snapshot is published.");
        ValidateRoadJunctions(b, b.Roads);
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
        b.Diagnostics.Add($"Road freeboard closure complete: iterations={iterations}; final banks committed.");
        // Terrain nodes retain this builder, but only their frozen RoadEntry profiles are
        // needed after compilation. Do not retain solver targets/corrections/owners in snapshots.
        b.RoadFit = null;
    }

    private static WorldDataException UnsupportedFreeboard(string detail, string file = "world")
    {
        var error = new WorldDataException(file, "automatic-profiles", "UNSUPPORTED: " + detail);
        error.Data["TerrainConstraintKind"] = "unsupported"; return error;
    }

    internal static double RoadCorePathRadius(TerrainRoad road, string file = "world")
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
        if (!found || !double.IsFinite(maximum)) throw UnsupportedFreeboard($"Real road core '{road.Source.Value}' has no finite coverage radius.", file);
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
            if (middle.Distance.Height - nearest > road.Width + 2 * radius + TerrainMath.HeightTolerance) continue;
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

    private static TerrainDifferential RoadCoreTop(Builder b, RoadEntry entry, CorridorContact core, TerrainPathHit hit, double x, double z)
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

    private static IReadOnlyList<ResolvedTerrainBank> ExportBanks(Builder b)
    {
        var result = new List<ResolvedTerrainBank>();
        foreach (var shore in b.WaterShores.OrderBy(shore => shore.Id, StringComparer.Ordinal))
        {
            var intervals = shore.OriginalHard.ToList();
            if (shore.Banks is { } bank)
            {
                intervals.AddRange(bank.RegisteredHard);
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

    private sealed record RoadWaterComposition(WaterShore Shore, TerrainShoreEnvelope Envelope);

    private static void PrepareRoadWaterComposition(Builder b)
    {
        ValidateFixedRoadWaterAnchors(b);
        if (b.Roads.Count == 0) return;
        foreach (var shore in b.WaterShores.OrderBy(shore => shore.Id, StringComparer.Ordinal))
            b.RoadWaterCompositions.Add(new(shore, new(shore.Region,
                (x, z) => b.LandBeforeCorridors.Evaluate(x, z).Surface.Height, shore.File, shore.Id, 1d / 3)));
    }

    private static void ValidateFixedRoadWaterAnchors(Builder b)
    {
        foreach (var anchor in b.RoadAnchors)
            foreach (var shore in b.WaterShores)
            {
                if (shore.Region.Distance(anchor.X, anchor.Z).Height > 0) continue;
                var bed = b.LandBeforeCorridors.Evaluate(anchor.X, anchor.Z).Surface.Height;
                if (Math.Abs(anchor.Y - bed) > TerrainMath.HeightTolerance)
                    throw new WorldDataException(anchor.File, anchor.Id,
                        $"Authored centreline anchor conflicts with fixed water core '{shore.Id}' at ({anchor.X:R},{anchor.Z:R}): anchorY={anchor.Y:R}, waterBedY={bed:R}, deltaY={bed - anchor.Y:R}; a real bridge/underpass exclusion is required.");
            }
    }

    private static void ValidateSourceRoadWaterCrossings(Builder b)
    {
        foreach (var entry in b.Roads)
            foreach (var shore in b.WaterShores)
            {
                var road = entry.Road;
                if (road.Path.Bounds.Max.X < shore.Region.Bounds.Min.X || road.Path.Bounds.Min.X > shore.Region.Bounds.Max.X
                    || road.Path.Bounds.Max.Y < shore.Region.Bounds.Min.Y || road.Path.Bounds.Min.Y > shore.Region.Bounds.Max.Y) continue;
                // Split only at real source vertices, shore intersections and structural
                // interval ends. Containment is constant on each resulting straight span.
                var stations = new SortedSet<double>(road.Path.VertexStations);
                foreach (var polygon in shore.Region.Polygons)
                    foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                        foreach (var hit in road.Path.Intersections(new(new(ring.Append(ring[0]).ToArray())))) stations.Add(hit.Station);
                var exclusions = entry.Plan?.Exclusions ?? BridgeExclusions(b, road)
                    .Concat(entry.Operation?.Steps.Select(step => (From: step.S0, To: step.S1)) ?? []).ToArray();
                foreach (var span in exclusions)
                {
                    stations.Add(Math.Clamp(span.From, 0, road.Path.Length));
                    stations.Add(Math.Clamp(span.To, 0, road.Path.Length));
                }
                var values = stations.ToArray();
                for (var i = 1; i < values.Length; i++)
                {
                    var station = (values[i - 1] + values[i]) / 2;
                    if (entry.Profile.Excluded(station)) continue;
                    var point = road.Path.AtDouble(station);
                    if (shore.Region.Distance(point.X, point.Z).Height >= -TerrainMath.HeightTolerance) continue;
                    var op = entry.Plan?.At(station) ?? entry.Operation;
                    var file = op is null ? entry.File : b.Files[op.Id];
                    throw new WorldDataException(file, op?.Id ?? "auto-corridor-" + road.Feature.Value,
                        $"Source road centreline crosses registered surface-water core '{shore.Id}' without a real bridge/underpass exclusion: road='{road.Source.Value}', station={station:R}, point=({point.X:R},{point.Z:R}). Pavement-edge grading cannot authorize a crossing.");
                }
            }
    }

    private static TerrainEvaluation ConstrainRoadAtWater(Builder b,
        TerrainEvaluation previous, TerrainEvaluation proposed, double x, double z)
    {
        if (proposed.Surface == previous.Surface) return proposed;
        var surface = proposed.Surface; var toe = proposed.ToeDistance;
        foreach (var composition in b.RoadWaterCompositions)
        {
            var distance = composition.Shore.Region.Distance(x, z).Height;
            // The union's real bed and shoreline outrank every corridor edge and shoulder.
            // Source-centreline crossings are content acceptance, checked offline; they
            // never authorize road-side fill or an invented bridge in the published field.
            if (distance <= 0) return previous;
            // Intersect with fixed-shore slope cones instead of multiplying competing masks.
            // Min/max preserves the original field's grade and finite support: inherited banks
            // can remain, but a road cannot create a steeper connection or reverse fill/cut.
            surface = composition.Envelope.Restrict(previous.Surface, surface, x, z);
            toe = Math.Min(toe, distance);
        }
        return WithSurface(previous, surface, proposed.CrestDistance, toe);
    }

    private static double LimitedEasePeakDerivative(double t, double peak)
    {
        if (t <= 0 || t >= 1 || peak >= 1.5) return 0;
        var end = 1 - 1 / peak;
        if (t < end) return t * t / (2 * end) - t * t / (2 * end * end * peak);
        if (t > 1 - end) return -(1 - t) * (1 - t) / (2 * end) + (1 - t) * (1 - t) / (2 * end * end * peak);
        return t - 0.5;
    }

}
