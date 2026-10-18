using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

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
                if (cores.Any(value => Math.Abs(value.Top.Height - roadTop.Height) > TerrainMath.HeightTolerance))
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
            var retained = TerrainDifferential.Product(weight, TerrainDifferential.Constant(1) - candidate);
            var combined = retained + candidate;
            var numerator = TerrainDifferential.Product(top ?? originalTop ?? TerrainDifferential.Constant(0), retained) + TerrainDifferential.Product(roadTop, candidate);
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
                var crossing = (samples[i - 1] + samples[i]) / 2;
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
                        && Math.Abs(contact.Top.Height - y) > contact.Distance.Height / 3 + TerrainMath.HeightTolerance;
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
                        && Math.Abs(contact.Top.Height - level.Value) > contact.Distance.Height / 3 + TerrainMath.HeightTolerance) return true;
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
