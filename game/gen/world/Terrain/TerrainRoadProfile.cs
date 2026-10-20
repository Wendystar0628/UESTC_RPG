using Campus.Contracts;

namespace Campus.Gen.World;

internal sealed class TerrainRoadProfile
{
    internal const double JunctionHeightTolerance = TerrainMath.HeightTolerance;
    internal sealed record Interval(IReadOnlyList<TerrainAnchor> Anchors, string File, string Id);
    internal sealed record Request(TerrainRoad Road, IReadOnlyList<(double From, double To)> Exclusions, double MaximumGrade,
        IReadOnlyList<Interval>? Intervals = null);
    private sealed record NetworkEdge(int A, int B, double Limit)
    {
        public double CorrectionA; public double CorrectionB;
    }
    private sealed record Piece(double S0, double S1, double Y0, double Y1, double M0, double M1, bool Cubic, int[] Support)
    {
        public (double Y, double Grade) At(double station)
        {
            var length = S1 - S0; var t = Math.Max(0, Math.Min(1, (station - S0) / length));
            if (!Cubic) return (Y0 + (Y1 - Y0) * t, (Y1 - Y0) / length);
            var a = 2 * Y0 - 2 * Y1 + length * (M0 + M1);
            var b = -3 * Y0 + 3 * Y1 - length * (2 * M0 + M1);
            var c = length * M0;
            return (((a * t + b) * t + c) * t + Y0, ((3 * a * t + 2 * b) * t + c) / length);
        }
        public IEnumerable<double> CriticalStations()
        {
            yield return S0; yield return S1;
            if (!Cubic) yield break;
            var length = S1 - S0;
            var a = 2 * Y0 - 2 * Y1 + length * (M0 + M1);
            var b = -3 * Y0 + 3 * Y1 - length * (2 * M0 + M1);
            if (a == 0) yield break;
            var t = -b / (3 * a);
            if (t > 0 && t < 1) yield return S0 + t * length;
        }
    }
    private readonly Piece[] _pieces;
    private readonly (double From, double To)[] _exclusions;
    private readonly string? _authoredFile;
    private readonly string? _authoredEntry;
    public double Length { get; }

    // Samples contributing to the existing interpolation pieces, used by the bank fit.
    internal IEnumerable<int> SupportIndices(double from, double to) => _pieces
        .Where(piece => piece.S1 >= from - TerrainMath.HeightTolerance && piece.S0 <= to + TerrainMath.HeightTolerance).SelectMany(piece => piece.Support).Distinct().Order();

    internal sealed record Bound(double Height, string Water);
    internal sealed record Sample(FeatureId Source, int Index, double Station, double X, double Z, int Node, bool Fixed);
    private sealed record NetworkPath(Request Request, double[] Stations, int[] Nodes, bool[] Junctions);

    internal sealed class NetworkFit
    {
        private readonly double[] _targets;
        private readonly bool[] _fixed;
        private readonly NetworkPath[] _paths;
        private readonly Dictionary<FeatureId, NetworkPath> _pathBySource;
        private readonly NetworkEdge[] _edges;
        private readonly Bound?[] _bounds;
        public IReadOnlyList<Sample> Samples { get; }
        public int NodeCount => _targets.Length;
        public IReadOnlyDictionary<FeatureId, TerrainRoadProfile> Profiles { get; private set; } = new Dictionary<FeatureId, TerrainRoadProfile>();

        public NetworkFit(IReadOnlyList<Request> requests, TerrainProgram terrain,
            IReadOnlyList<(TerrainRoad Road, TerrainRoadProfile Profile)> authored, Func<double, double, bool> protectedZeroBand)
        {
            var ownership = SourceOwnership(requests, authored);
            var locked = LockedHeights(requests, authored);
            var keys = new Dictionary<(long X, long Z), int>(); var values = new List<double>(); var fixedValues = new List<bool>();
            var paths = new List<NetworkPath>();
            var edges = new List<NetworkEdge>();
            foreach (var request in requests.OrderBy(r => r.Road.Source.Value, StringComparer.Ordinal))
            {
                var path = request.Road.Path;
                var (sampled, junctions) = SampleStations(request, ownership);
                var nodes = new int[sampled.Length];
                var fixedJunctions = new bool[sampled.Length];
                for (var i = 0; i < sampled.Length; i++)
                {
                    var p = path.At(sampled[i]); var key = Key(p.X, p.Y);
                    fixedJunctions[i] = ownership.TryGetValue(key, out var owners) && owners.Count > 1;
                    var isZero = protectedZeroBand(p.X, p.Y); var isAuthored = locked.TryGetValue(key, out var authorY);
                    var interval = request.Intervals?.FirstOrDefault(value => sampled[i] >= value.Anchors[0].S && sampled[i] <= value.Anchors[^1].S);
                    if (interval is not null)
                    {
                        var anchors = interval.Anchors; var right = 1;
                        while (right + 1 < anchors.Count && anchors[right].S < sampled[i]) right++;
                        var a = anchors[right - 1]; var c = anchors[right];
                        var fixedY = a.Y + (c.Y - a.Y) * ((sampled[i] - a.S) / (c.S - a.S));
                        if (isAuthored && Math.Abs(authorY - fixedY) > JunctionHeightTolerance)
                            throw new WorldDataException(interval.File, interval.Id, "Authored interval conflicts with another fixed road at a shared node.");
                        authorY = fixedY; isAuthored = true;
                    }
                    if (isZero && isAuthored && Math.Abs(authorY) > TerrainMath.HeightTolerance)
                        throw new WorldDataException(interval?.File ?? "world", interval?.Id ?? "corridor", "An authored shared road node conflicts with the protected zero band.");
                    if (!keys.TryGetValue(key, out var node))
                    {
                        node = values.Count; keys.Add(key, node);
                        values.Add(isAuthored ? authorY : isZero ? 0 : terrain.Evaluate(p.X, p.Y).Surface.Height);
                        fixedValues.Add(isZero || isAuthored);

                    }
                    else if (isAuthored)
                    {
                        if (fixedValues[node] && Math.Abs(values[node] - authorY) > JunctionHeightTolerance)
                            throw new WorldDataException(interval?.File ?? "world", interval?.Id ?? "corridor",
                                "Fixed corridor intervals disagree at a shared network node.");
                        values[node] = authorY; fixedValues[node] = true;
                    }
                    nodes[i] = node;
                    if (i == 0 || request.Exclusions.Any(e => (sampled[i - 1] + sampled[i]) / 2 > e.From && (sampled[i - 1] + sampled[i]) / 2 < e.To)) continue;
                    var midpoint = (sampled[i - 1] + sampled[i]) / 2;
                    // Include the 2.5 m rounding half-width so rounding cannot import a steeper
                    // exterior derivative into the prescribed 10 m junction zone.
                    var grade = junctions.Any(s => Math.Abs(midpoint - s) < 12.5) ? Math.Min(0.03, request.MaximumGrade) : request.MaximumGrade;
                    // Mixed-source final rounding preserves the fixed author anchors. Leave room
                    // for its one-sided Hermite derivative peak only in the automatically fitted gaps.
                    if (request.Intervals is { Count: > 0 } ranges && !ranges.Any(value => midpoint >= value.Anchors[0].S && midpoint <= value.Anchors[^1].S))
                        grade *= 0.75;
                    edges.Add(new(nodes[i - 1], node, grade * (sampled[i] - sampled[i - 1])));
                }
                paths.Add(new(request, sampled, nodes, fixedJunctions));
            }
            _targets = values.ToArray(); _fixed = fixedValues.ToArray(); _paths = paths.ToArray();
            _pathBySource = _paths.ToDictionary(path => path.Request.Road.Source);
            _edges = edges.ToArray(); _bounds = new Bound?[_targets.Length];
            Samples = _paths.SelectMany(item => item.Stations.Select((station, index) =>
            {
                var p = item.Request.Road.Path.At(station); var node = item.Nodes[index];
                return new Sample(item.Request.Road.Source, index, station, p.X, p.Y, node, _fixed[node]);
            })).ToArray();
        }

        private static (long X, long Z) Key(double x, double z) =>
            ((long)Math.Round(x * 1000, MidpointRounding.ToEven), (long)Math.Round(z * 1000, MidpointRounding.ToEven));

        private static Dictionary<(long X, long Z), HashSet<FeatureId>> SourceOwnership(IReadOnlyList<Request> requests,
            IReadOnlyList<(TerrainRoad Road, TerrainRoadProfile Profile)> authored)
        {
            var ownership = new Dictionary<(long X, long Z), HashSet<FeatureId>>();
            foreach (var road in requests.Select(r => r.Road).Concat(authored.Select(r => r.Road)).OrderBy(r => r.Source.Value, StringComparer.Ordinal))
                foreach (var p in road.Path.Line.Points)
                {
                    var key = Key(p.X, p.Y);
                    if (!ownership.TryGetValue(key, out var owners)) ownership.Add(key, owners = []);
                    owners.Add(road.Source);
                }
            return ownership;
        }

        private static Dictionary<(long X, long Z), double> LockedHeights(IReadOnlyList<Request> requests,
            IReadOnlyList<(TerrainRoad Road, TerrainRoadProfile Profile)> authored)
        {
            var locked = new Dictionary<(long X, long Z), double>();
            var firstLockedSource = new Dictionary<(long X, long Z), (TerrainRoad Road, TerrainRoadProfile Profile, double X, double Z, double Height)>();
            foreach (var entry in authored)
                foreach (var p in entry.Road.Path.Line.Points)
                {
                    var value = entry.Profile.At(entry.Road.Path.Project(p.X, p.Y).Station).Y; var key = Key(p.X, p.Y);
                    if (locked.TryGetValue(key, out var prior) && Math.Abs(prior - value) > JunctionHeightTolerance)
                    {
                        var first = firstLockedSource[key];
                        throw new WorldDataException(entry.Profile._authoredFile ?? "world", entry.Profile._authoredEntry ?? "corridor",
                            $"Authored road profiles disagree at a shared source node: world=({p.X:R},{p.Y:R}), millimetreKey=({key.X},{key.Z}); "
                            + $"firstSource='{first.Road.Source.Value}', firstWorld=({first.X:R},{first.Z:R}), firstHeight={first.Height:R}, firstFile='{first.Profile._authoredFile ?? "<automatic>"}', firstEntry='{first.Profile._authoredEntry ?? "<automatic>"}'; "
                            + $"secondSource='{entry.Road.Source.Value}', secondHeight={value:R}, secondFile='{entry.Profile._authoredFile ?? "<automatic>"}', secondEntry='{entry.Profile._authoredEntry ?? "<automatic>"}', deltaSecondMinusFirst={value - first.Height:R}, comparedLockedHeight={prior:R}, deltaSecondMinusLocked={value - prior:R}.");
                    }
                    locked[key] = value;
                    firstLockedSource.TryAdd(key, (entry.Road, entry.Profile, p.X, p.Y, value));
                }
            foreach (var request in requests)
                foreach (var interval in request.Intervals ?? [])
                    foreach (var anchor in interval.Anchors)
                    {
                        var p = request.Road.Path.AtDouble(anchor.S); var key = Key(p.X, p.Z);
                        if (locked.TryGetValue(key, out var prior) && Math.Abs(prior - anchor.Y) > JunctionHeightTolerance)
                            throw new WorldDataException(interval.File, interval.Id, "Fixed corridor anchors disagree at a shared source-road node.");
                        locked[key] = anchor.Y;
                    }
            return locked;
        }

        private static (double[] Stations, List<double> Junctions) SampleStations(Request request,
            IReadOnlyDictionary<(long X, long Z), HashSet<FeatureId>> ownership)
        {
            var path = request.Road.Path; var stations = new SortedSet<double> { 0, path.Length };
            for (var s = 5d; s < path.Length; s += 5) stations.Add(s);
            var junctions = new List<double>(); var accumulated = 0d;
            for (var i = 0; i < path.Line.Points.Count; i++)
            {
                if (i > 0)
                {
                    var a = path.Line.Points[i - 1]; var c = path.Line.Points[i];
                    var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
                    accumulated += Math.Sqrt(dx * dx + dz * dz);
                }
                stations.Add(accumulated);
                if (ownership[Key(path.Line.Points[i].X, path.Line.Points[i].Y)].Count > 1) junctions.Add(accumulated);
            }
            foreach (var s in junctions)
                foreach (var offset in new[] { -12.5, -10, 10, 12.5 }) stations.Add(Math.Max(0, Math.Min(path.Length, s + offset)));
            foreach (var e in request.Exclusions) { stations.Add(e.From); stations.Add(e.To); }
            foreach (var interval in request.Intervals ?? [])
                foreach (var anchor in interval.Anchors) stations.Add(anchor.S);
            return (stations.ToArray(), junctions);
        }

        public bool Require(FeatureId source, int sampleIndex, Bound bound)
        {
            var path = _pathBySource[source]; var node = path.Nodes[sampleIndex];
            if (_bounds[node] is { } old && old.Height >= bound.Height) return false;
            if (_fixed[node]) return false; // Actual bank checks own fixed-height conflicts.
            _bounds[node] = bound; return true;
        }

        public IReadOnlyDictionary<FeatureId, TerrainRoadProfile> Solve()
        {
            // The original targets are sampled exactly once, before any road-derived Hard bank.
            // Every solve starts from that frozen objective and resets all Dykstra corrections.
            foreach (var edge in _edges) { edge.CorrectionA = 0; edge.CorrectionB = 0; }
            var lowerCorrections = new double[_targets.Length];
            // Dykstra projections solve the unweighted least-squares fit to the sampled base field
            // under all graph-edge grade constraints. Every shared world node has one variable;
            // explicit author/zero-band nodes are eliminated fixed variables, not soft samples.
            var heights = _targets.ToArray(); var converged = false;
            for (var iteration = 0; iteration < 20000; iteration++)
            {
                var change = 0d;
                foreach (var edge in _edges)
                {
                    var oldA = heights[edge.A]; var oldB = heights[edge.B];
                    var a = _fixed[edge.A] ? oldA : oldA + edge.CorrectionA;
                    var c = _fixed[edge.B] ? oldB : oldB + edge.CorrectionB;
                    var difference = c - a; var excess = Math.Abs(difference) - edge.Limit;
                    var nextA = a; var nextB = c;
                    if (excess > TerrainMath.HeightTolerance)
                    {
                        var sign = difference < 0 ? -1 : 1;
                        if (_fixed[edge.A] && _fixed[edge.B])
                            throw new WorldDataException("world", "automatic-profiles", "Fixed road nodes cannot satisfy their longitudinal grade; provide a feasible authored route/profile.");
                        if (_fixed[edge.A]) nextB -= sign * excess;
                        else if (_fixed[edge.B]) nextA += sign * excess;
                        else { nextA += sign * excess / 2; nextB -= sign * excess / 2; }
                    }
                    edge.CorrectionA = _fixed[edge.A] ? 0 : a - nextA;
                    edge.CorrectionB = _fixed[edge.B] ? 0 : c - nextB;
                    heights[edge.A] = nextA; heights[edge.B] = nextB;
                    change = Math.Max(change, Math.Max(Math.Abs(nextA - oldA), Math.Abs(nextB - oldB)));
                }
                for (var node = 0; node < heights.Length; node++)
                {
                    if (_fixed[node] || _bounds[node] is not { } bound) continue;
                    var old = heights[node]; var corrected = old + lowerCorrections[node];
                    var next = Math.Max(corrected, bound.Height);
                    lowerCorrections[node] = corrected - next; heights[node] = next;
                    change = Math.Max(change, Math.Abs(next - old));
                }
                if (change <= TerrainMath.HeightTolerance / 2 && _edges.All(e => Math.Abs(heights[e.B] - heights[e.A]) <= e.Limit + TerrainMath.HeightTolerance)
                    && Enumerable.Range(0, heights.Length).All(node => _bounds[node] is not { } bound || heights[node] >= bound.Height - TerrainMath.HeightTolerance))
                { converged = true; break; }
            }
            if (!converged)
            {
                var constrained = _bounds.Any(bound => bound is not null);
                var error = new WorldDataException("world", "automatic-profiles",
                    (constrained ? "UNSUPPORTED: conservative freeboard bounds and fixed graph constraints could not be proved feasible. " : "")
                    + "Deterministic road-network smoothing did not converge; no approximate invalid profile is published.");
                if (constrained) error.Data["TerrainConstraintKind"] = "unsupported";
                throw error;
            }
            var result = new Dictionary<FeatureId, TerrainRoadProfile>();
            foreach (var item in _paths)
            {
                var anchors = item.Stations.Select((s, i) => new TerrainAnchor(s, heights[item.Nodes[i]])).ToArray();
                if (item.Request.Intervals is { Count: > 0 } intervals)
                {
                    var profile = new TerrainRoadProfile(anchors, item.Request.Road.Path.Length, item.Request.Exclusions,
                        item.Request.MaximumGrade, intervals[0].File, intervals[0].Id, preserveBounds: true);
                    result.Add(item.Request.Road.Source, profile);
                }
                else result.Add(item.Request.Road.Source, new(anchors, item.Request.Road.Path.Length, item.Request.Exclusions, item.Junctions));
            }
            Profiles = result; return result;
        }
    }

    public static IReadOnlyDictionary<FeatureId, TerrainRoadProfile> SmoothNetwork(IReadOnlyList<Request> requests,
        TerrainProgram terrain, IReadOnlyList<(TerrainRoad Road, TerrainRoadProfile Profile)> authored,
        Func<double, double, bool> protectedZeroBand) => new NetworkFit(requests, terrain, authored, protectedZeroBand).Solve();

    private TerrainRoadProfile(IReadOnlyList<TerrainAnchor> anchors, double length,
        IReadOnlyList<(double From, double To)> exclusions, IReadOnlyList<bool> junctions)
    {
        Length = length; _exclusions = exclusions.ToArray(); var pieces = new List<Piece>();
        var slopes = Enumerable.Range(0, anchors.Count - 1).Select(i => (anchors[i + 1].Y - anchors[i].Y) / (anchors[i + 1].S - anchors[i].S)).ToArray();
        var radii = new double[anchors.Count];
        for (var i = 1; i + 1 < anchors.Count; i++)
            if (!junctions[i] && !TouchesExclusion(anchors[i - 1].S, anchors[i + 1].S))
                radii[i] = Math.Min(2.5, Math.Min(anchors[i].S - anchors[i - 1].S, anchors[i + 1].S - anchors[i].S) / 2);
        for (var i = 0; i + 1 < anchors.Count; i++)
        {
            var a = anchors[i]; var c = anchors[i + 1]; var start = a.S + radii[i]; var end = c.S - radii[i + 1];
            if (end > start) pieces.Add(new(start, end, a.Y + slopes[i] * radii[i], c.Y - slopes[i] * radii[i + 1], slopes[i], slopes[i], false, [i, i + 1]));
            if (radii[i + 1] > 0)
            {
                var radius = radii[i + 1];
                pieces.Add(new(c.S - radius, c.S + radius, c.Y - slopes[i] * radius, c.Y + slopes[i + 1] * radius, slopes[i], slopes[i + 1], true, [i, i + 1, i + 2]));
            }
        }
        _pieces = pieces.ToArray();
    }

    public TerrainRoadProfile(IReadOnlyList<TerrainAnchor> anchors, double actualLength,
        IReadOnlyList<(double From, double To)> exclusions, double maximumGrade, string file, string op, bool preserveBounds = false)
    {
        _authoredFile = file; _authoredEntry = op;
        // Source plan coordinates and authored station values are centimetre precision; never infer a missing endpoint.
        if (anchors.Count < 2 || anchors[0].S != 0 || Math.Abs(anchors[^1].S - actualLength) > 0.01)
            throw new WorldDataException(file, op, $"Corridor anchors must cover s=0 through actual centreline length {actualLength:R} m (1 cm comparison tolerance).");
        // The declared terminal station denotes the real source endpoint under the existing
        // centimetre comparison tolerance. Preserve its exact authored Y at that endpoint.
        anchors = anchors.Select((anchor, index) => index == anchors.Count - 1 ? new TerrainAnchor(actualLength, anchor.Y) : anchor).ToArray();
        if (anchors[^2].S >= actualLength)
            throw new WorldDataException(file, op, "The final interior anchor must precede the actual source endpoint.");
        Length = actualLength; _exclusions = exclusions.OrderBy(e => e.From).ThenBy(e => e.To).ToArray();
        var slopes = new double[anchors.Count - 1];
        for (var i = 0; i < slopes.Length; i++) slopes[i] = (anchors[i + 1].Y - anchors[i].Y) / (anchors[i + 1].S - anchors[i].S);
        var pieces = new List<Piece>();
        for (var i = 0; i < slopes.Length; i++)
        {
            var a = anchors[i]; var c = anchors[i + 1]; var slope = slopes[i];
            var leftWidth = i > 0 && !TouchesExclusion(anchors[i - 1].S, c.S) ? Math.Min(2.5, (c.S - a.S) / 2) : 0;
            var rightWidth = i + 1 < slopes.Length && !TouchesExclusion(a.S, anchors[i + 2].S) ? Math.Min(2.5, (c.S - a.S) / 2) : 0;
            int[] support = preserveBounds ? [i, i + 1] : [];
            if (leftWidth > 0) pieces.Add(new(a.S, a.S + leftWidth, a.Y, a.Y + slope * leftWidth, RoundedGrade(slopes[i - 1], slope), slope, true, support));
            if (c.S - rightWidth > a.S + leftWidth) pieces.Add(new(a.S + leftWidth, c.S - rightWidth,
                a.Y + slope * leftWidth, c.Y - slope * rightWidth, slope, slope, false, support));
            if (rightWidth > 0) pieces.Add(new(c.S - rightWidth, c.S, c.Y - slope * rightWidth, c.Y, slope, RoundedGrade(slope, slopes[i + 1]), true, support));
        }
        _pieces = pieces.ToArray();
        foreach (var piece in _pieces)
            foreach (var station in piece.CriticalStations().Concat(_exclusions.SelectMany(e => new[] { e.From, e.To })).Where(s => s >= piece.S0 && s <= piece.S1))
                if (!_exclusions.Any(e => station >= e.From && station <= e.To) && Math.Abs(piece.At(station).Grade) > maximumGrade + TerrainMath.GradeTolerance)
                    throw new WorldDataException(file, op, $"Fixed corridor anchors cannot be smoothed within grade {maximumGrade:R}; station={station:R}, grade={piece.At(station).Grade:R}. Anchors were not moved.");

        // A harmonic joint keeps each mixed-source piece between its two samples, so the
        // existing automatic freeboard solver can raise gap samples without moving anchors.
        double RoundedGrade(double a, double c) => !preserveBounds ? Joint(a, c) : a * c <= 0 ? 0 : 2 * a * c / (a + c);
    }

    public (double Y, double Grade) At(double station)
    {
        var low = 0; var high = _pieces.Length - 1;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (station <= _pieces[middle].S1) high = middle; else low = middle + 1;
        }
        return _pieces[low].At(station);
    }

    internal static double ResolveStation(double station, double declaredEnd, double actualLength, string file, string op)
    {
        if (station == declaredEnd && Math.Abs(declaredEnd - actualLength) <= 0.01) return actualLength;
        if (!double.IsFinite(station) || station < 0 || station > actualLength)
            throw new WorldDataException(file, op, $"Station {station:R} lies outside actual centreline length {actualLength:R} m; only the declared terminal anchor has the 1 cm endpoint comparison tolerance.");
        return station;
    }

    public TerrainDifferential SurfaceAt(TerrainPath path, double x, double z, double roadWidth, TerrainPathHit nearest)
    {
        if (nearest.Distance <= 0.0000001)
        {
            var fixedPoint = At(nearest.Station);
            return new(fixedPoint.Y, fixedPoint.Grade * nearest.StationDx, fixedPoint.Grade * nearest.StationDz);
        }
        var minimum = new TerrainDifferential(nearest.Distance, nearest.DistanceDx, nearest.DistanceDz);
        var sum = TerrainDifferential.Constant(0); var weighted = TerrainDifferential.Constant(0);
        for (var i = 0; i + 1 < path.Line.Points.Count; i++)
        {
            var projection = path.ProjectStationSegment(i, x, z);
            if (Excluded(projection.Station)) continue;
            var taper = TerrainDifferential.Constant(1) - (projection.Distance - minimum) * (1 / roadWidth);
            if (taper.Height <= 0) continue;
            var ratio = TerrainDifferential.Divide(minimum, projection.Distance);
            var weight = TerrainDifferential.Product(TerrainDifferential.Product(taper, taper), TerrainDifferential.Product(ratio, ratio));
            var value = At(projection.Station);
            weighted += TerrainDifferential.Product(weight, new(value.Y, value.Grade * projection.Dx, value.Grade * projection.Dz)); sum += weight;
        }
        if (sum.Height == 0) throw new InvalidOperationException($"A road surface has no non-structural profile segment at ({x:R},{z:R}), nearest station={nearest.Station:R}/{Length:R}, exclusions={string.Join(';', _exclusions)}.");
        return TerrainDifferential.Divide(weighted, sum);

    }
    public bool Excluded(double station) => _exclusions.Any(e => station >= e.From - 0.0000001 && station <= e.To + 0.0000001);
    private bool TouchesExclusion(double a, double b) => _exclusions.Any(e => e.From < b && e.To > a);
    private static double Joint(double a, double b) => a * b <= 0 ? 0 : (a + b) / 2;
}
