using Campus.Contracts;
using Campus.Contracts.Maps;
using static Campus.Gen.World.TerrainCorridorCompiler;
using static Campus.Gen.World.TerrainRoadCoreGeometry;
using static Campus.Gen.World.TerrainWaterNetwork;

namespace Campus.Gen.World;

internal static class TerrainFootwayBankClosure
{
    internal sealed record FootwayPlan(WaterShore Shore, IReadOnlyList<TerrainBridgeBanks.RoadSpan> Spans);

    internal static void ResolveFootwayBanks(TerrainBuildState b)
    {
        var entries = b.Roads.Where(entry => entry.Road.Kind is "footway" or "path" or "pedestrian")
            .OrderBy(entry => entry.Road.Source.Value, StringComparer.Ordinal).ToArray();
        var bySource = entries.ToDictionary(entry => entry.Road.Source);
        var roads = RoadContacts(b, entries);
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
        CommitBanks(b, final);
        foreach (var refresh in b.ShoreEnvelopeRefresh) refresh();
        b.Diagnostics.Add($"Road freeboard closure complete: iterations={iterations}; final banks committed.");
        // Terrain nodes retain this builder, but only their frozen RoadEntry profiles are
        // needed after compilation. Do not retain solver targets/corrections/owners in snapshots.
        b.RoadFit = null;
    }
    private static TerrainBridgeBanks.RoadContact[] RoadContacts(TerrainBuildState b, RoadEntry[] entries)
    {
        return entries.Select(entry =>
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
    }

    private static void CommitBanks(TerrainBuildState b, FootwayPlan[] final)
    {
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
    }

}
