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
        var levels = shores.ToDictionary(shore => shore.Id, shore => b.Water.First(water => shore.Waters.Contains(water.Feature)).Level);
        // Candidate values depend only on resolved water levels and frozen real core geometry.
        // The finite candidate identities are frozen here; geometry-only values are memoized
        // lazily once for participating roads, and never depend on the current trial profile.
        var radii = entries.ToDictionary(entry => entry.Road.Source, entry => new Lazy<double>(() => RoadCorePathRadius(entry.Road)));
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
        ValidateRoadJunctions(b.Roads);
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

    private static TerrainDifferential RoadCoreTop(Builder b, RoadEntry entry, CorridorContact core, double x, double z)
    {
        var longitudinal = entry.Profile.SurfaceAt(entry.Road.Path, x, z, entry.Road.Width, entry.Road.Path.Project(x, z));
        var hit = entry.Road.Path.Project(x, z);
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
            if (Math.Abs(proposed.Surface.Height - previous.Surface.Height) <= TerrainMath.HeightTolerance) return previous;
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
        if (total > 1 + TerrainMath.GradeTolerance || previous.Class != TerrainClass.Transition && (total > 0.5 + TerrainMath.GradeTolerance || transverse > 1d / 3 + TerrainMath.GradeTolerance))
            throw new WorldDataException(file, id, $"Road-water side envelope cannot fit its grade: water='{nearest.Id}', point=({x:R},{z:R}), transverseGrade={transverse:R}, totalGrade={total:R}, roadDistance={roadDistance.Height:R}, waterDistance={waterDistance.Height:R}, previous={previous.Surface}, proposed={proposed.Surface}, final={surface}, class={previous.Class}, shoulder={b.AutomaticRoads[id].Grading?.Width:R}, profile={profile}.")
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
