using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Parts.Core;

namespace Campus.Kernel.Bake;

/// <summary>
/// Load-time validation of the effective, post-overlay world. One canonical source polygon is one
/// comparison object; aggregate entity geometry is not an additional footprint when Parts exist.
/// Claims are diagnostic only. This class does not load data, mutate the world or cache results.
/// </summary>
public static class BuildingOverlapValidator
{
    public const double MaximumIntersectionOverUnion = 0.9;

    /// <summary>
    /// Throws one stable report containing every different-source pair whose area IoU is strictly
    /// greater than 0.9. Conflicting aliases of one source fail before comparisons. Invoke once for
    /// each effective world before any cache hit or geometry generation can bypass validation.
    /// </summary>
    public static void Validate(IWorldModel world, Func<FeatureId, Aspects, WpId?>? claimedBy = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        var footprints = Collect(world, claimedBy);
        var failures = new List<string>();
        for (var a = 0; a < footprints.Count; a++)
            for (var b = a + 1; b < footprints.Count; b++)
            {
                var left = footprints[a];
                var right = footprints[b];
                if (StringComparer.Ordinal.Equals(left.Source, right.Source) || !left.Shape.Bounds.Overlaps(right.Shape.Bounds)) continue;
                var measurement = Measure(left.Shape, right.Shape);
                if (measurement.IoU <= MaximumIntersectionOverUnion) continue;
                failures.Add($"A: {Describe(left)}\nB: {Describe(right)}\n" +
                    $"areaA={Number(measurement.AreaA)} m2; areaB={Number(measurement.AreaB)} m2; " +
                    $"intersection={Number(measurement.Intersection)} m2; union={Number(measurement.Union)} m2; " +
                    $"IoU={Number(measurement.IoU)} > {Number(MaximumIntersectionOverUnion)}");
            }
        if (failures.Count != 0)
            throw new InvalidDataException($"Building footprint overlap: {failures.Count.ToString(CultureInfo.InvariantCulture)} pair(s).\n" +
                string.Join("\n\n", failures));
    }

    private static List<Footprint> Collect(IWorldModel world, Func<FeatureId, Aspects, WpId?>? claimedBy)
    {
        var sources = new SortedDictionary<string, Source>(StringComparer.Ordinal);
        var errors = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Parts.Count == 0)
            {
                if (feature.Kind.Value == "building") Add(feature, feature.Id.Value, feature.Geometry);
                continue;
            }
            foreach (var part in feature.Parts.OrderBy(p => p.SourceId, StringComparer.Ordinal))
            {
                // This is the same source-kind rule as WP-14 BuildingPlanner.Sources.
                var kind = part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var value) &&
                    value.ValueKind == JsonValueKind.String ? value.GetString() : feature.Kind.Value;
                if (kind == "building") Add(feature, part.SourceId, part.Geometry);
            }
        }
        foreach (var (id, source) in sources)
            foreach (var conflict in source.Conflicts)
                errors.Add($"source={Quote(id)}; {conflict}; references=[{string.Join("; ", source.References)}]");
        if (errors.Count != 0)
            throw new InvalidDataException("Invalid building source geometry:\n" + string.Join("\n", errors));
        var result = new List<Footprint>();
        foreach (var (id, source) in sources)
            for (var i = 0; i < source.Polygons.Length; i++)
                result.Add(new(id, i, source.Polygons[i], source.References.ToArray()));
        return result;

        void Add(WorldFeature feature, string id, OverlayGeometry geometry)
        {
            var reference = DescribeReference(feature, claimedBy);
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add($"Empty source ID: {reference}.");
                return;
            }
            PolygonShape[] polygons;
            try
            {
                polygons = geometry switch
                {
                    PolygonGeometry p => [Prepare(p.Rings)],
                    MultiPolygonGeometry m when m.Polygons.Count > 0 => m.Polygons.Select(Prepare).ToArray(),
                    PointGeometry or PolylineGeometry => [], // Non-area features never acquire an envelope footprint.
                    _ => throw new ArgumentException("Unsupported or empty polygon geometry.")
                };
            }
            catch (ArgumentException e)
            {
                errors.Add($"source={Quote(id)}; {reference}; {e.Message.Replace('\r', ' ').Replace('\n', ' ')}");
                return;
            }
            if (!sources.TryGetValue(id, out var source))
            {
                sources.Add(id, new(polygons, geometry, reference));
                return;
            }
            source.References.Add(reference);
            var conflict = source.Polygons.Length != polygons.Length
                ? $"polygon count differs ({source.Polygons.Length.ToString(CultureInfo.InvariantCulture)} versus {polygons.Length.ToString(CultureInfo.InvariantCulture)})"
                : null;
            if (conflict is null && polygons.Length == 0 && !SameNonArea(source.Geometry, geometry))
                conflict = "non-area geometry differs";
            if (conflict is null)
                for (var i = 0; i < polygons.Length; i++)
                    if (!SamePolygon(source.Polygons[i], polygons[i]))
                    { conflict = $"geometry differs at polygon={i.ToString(CultureInfo.InvariantCulture)}"; break; }
            if (conflict is not null) source.Conflicts.Add(conflict);
        }
    }

    private static bool SameNonArea(OverlayGeometry a, OverlayGeometry b) => (a, b) switch
    {
        (PointGeometry x, PointGeometry y) => x.Position == y.Position,
        (PolylineGeometry x, PolylineGeometry y) => x.Line.Points.SequenceEqual(y.Line.Points) || x.Line.Points.SequenceEqual(y.Line.Points.Reverse()),
        _ => false
    };

    private static string DescribeReference(WorldFeature feature, Func<FeatureId, Aspects, WpId?>? claimedBy) =>
        $"entity={Quote(feature.Id.Value)}, name={Quote(feature.Name ?? "")}, " +
        $"owner={{massing:{OwnerValue(claimedBy, feature.Id, Aspects.Massing)},facade:{OwnerValue(claimedBy, feature.Id, Aspects.Facade)},roof:{OwnerValue(claimedBy, feature.Id, Aspects.Roof)}}}";

    // The resolver reports actual claims. Absence is not assigned to a guessed generic owner.
    private static string OwnerValue(Func<FeatureId, Aspects, WpId?>? resolver, FeatureId feature, Aspects aspect) =>
        resolver is null ? "(unavailable)" : resolver(feature, aspect) is { } owner ? Quote(owner.Value) : "(unclaimed)";

    private static string Describe(Footprint footprint) =>
        $"source={Quote(footprint.Source)}; polygon={footprint.PolygonIndex.ToString(CultureInfo.InvariantCulture)}; " +
        $"references=[{string.Join("; ", footprint.References)}]";

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";

    // Normalise ring starts, orientation, redundant straight vertices and hole order before comparing
    // aliases or triangulating. Polygon positions in a MultiPolygon remain the supplied source indices.
    private static PolygonShape Prepare(PolygonRings rings)
    {
        var initial = PolygonTriangulator.Triangulate(rings);
        var canonical = new Vector2[initial.RingStarts.Length][];
        for (var r = 0; r < canonical.Length; r++)
        {
            var start = initial.RingStarts[r];
            var end = r + 1 == canonical.Length ? initial.Vertices.Length : initial.RingStarts[r + 1];
            var first = start;
            for (var i = start + 1; i < end; i++)
                if (Compare(initial.Vertices[i], initial.Vertices[first]) < 0) first = i;
            var ring = new Vector2[end - start];
            for (var i = 0; i < ring.Length; i++) ring[i] = initial.Vertices[start + (first - start + i) % ring.Length];
            canonical[r] = ring;
        }
        Array.Sort(canonical, 1, canonical.Length - 1, Comparer<Vector2[]>.Create(CompareRings));
        var triangulated = PolygonTriangulator.Triangulate(new(canonical[0], canonical.Skip(1).Cast<IReadOnlyList<Vector2>>().ToArray()));
        var triangles = new Triangle[triangulated.Indices.Length / 3];
        var area = new Sum();
        for (var i = 0; i < triangles.Length; i++)
        {
            var t = new Triangle(new(triangulated.Vertices[triangulated.Indices[3 * i]]),
                new(triangulated.Vertices[triangulated.Indices[3 * i + 1]]), new(triangulated.Vertices[triangulated.Indices[3 * i + 2]]));
            triangles[i] = t;
            area.Add(Cross(t.A, t.B, t.C) * 0.5);
        }
        if (!double.IsFinite(area.Value) || area.Value <= 0) throw new ArgumentException("Polygon area must be finite and positive.");
        return new(canonical, triangles, area.Value, BoundsOf(triangulated.Vertices));
    }

    private static int Compare(Vector2 a, Vector2 b)
    {
        var x = a.X.CompareTo(b.X);
        return x != 0 ? x : a.Y.CompareTo(b.Y);
    }

    private static int CompareRings(Vector2[] a, Vector2[] b)
    {
        for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            var comparison = Compare(a[i], b[i]);
            if (comparison != 0) return comparison;
        }
        return a.Length.CompareTo(b.Length);
    }

    private static bool SamePolygon(PolygonShape a, PolygonShape b) =>
        a.Rings.Length == b.Rings.Length && a.Rings.Zip(b.Rings).All(pair => pair.First.AsSpan().SequenceEqual(pair.Second));

    // Internal for focused pure .NET regression tests; not a second public validation/numbering API.
    internal static AreaMeasurement Measure(PolygonRings a, PolygonRings b) => Measure(Prepare(a), Prepare(b));

    private static AreaMeasurement Measure(PolygonShape a, PolygonShape b)
    {
        var intersection = new Sum();
        if (a.Bounds.Overlaps(b.Bounds))
            foreach (var left in a.Triangles)
                foreach (var right in b.Triangles)
                    if (left.Bounds.Overlaps(right.Bounds)) intersection.Add(Intersection(left, right));
        // Clipping/summation can exceed an area by a few ulps. Clamp only to mathematical bounds;
        // do not shift the 0.9 rejection threshold or buffer the supplied footprints.
        var common = Math.Clamp(intersection.Value, 0, Math.Min(a.Area, b.Area));
        var union = a.Area + b.Area - common;
        return new(a.Area, b.Area, common, union, common / union);
    }

    private static double Intersection(Triangle subject, Triangle clip)
    {
        Span<Point> input = stackalloc Point[12];
        Span<Point> output = stackalloc Point[12];
        input[0] = subject.A; input[1] = subject.B; input[2] = subject.C;
        var count = 3;
        for (var edge = 0; edge < 3; edge++)
        {
            var a = edge == 0 ? clip.A : edge == 1 ? clip.B : clip.C;
            var b = edge == 0 ? clip.B : edge == 1 ? clip.C : clip.A;
            var nextCount = 0;
            var previous = input[count - 1];
            var previousDistance = Cross(a, b, previous);
            for (var i = 0; i < count; i++)
            {
                var current = input[i];
                var distance = Cross(a, b, current);
                if ((previousDistance >= 0) != (distance >= 0))
                {
                    var t = previousDistance / (previousDistance - distance);
                    var crossing = previousDistance == 0 ? previous : distance == 0 ? current :
                        new Point(previous.X + t * (current.X - previous.X), previous.Y + t * (current.Y - previous.Y));
                    Append(output, ref nextCount, crossing);
                }
                if (distance >= 0) Append(output, ref nextCount, current);
                previous = current;
                previousDistance = distance;
            }
            if (nextCount > 1 && output[0] == output[nextCount - 1]) nextCount--;
            if (nextCount < 3) return 0;
            var temporary = input; input = output; output = temporary;
            count = nextCount;
        }
        var area = new Sum();
        for (var i = 1; i + 1 < count; i++) area.Add(Cross(input[0], input[i], input[i + 1]) * 0.5);
        return Math.Max(0, area.Value);
    }

    private static void Append(Span<Point> points, ref int count, Point point)
    {
        if (count == 0 || points[count - 1] != point) points[count++] = point;
    }

    private static double Cross(Point a, Point b, Point c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    private static Bounds BoundsOf(Vector2[] vertices) => new(vertices.Min(p => (double)p.X), vertices.Min(p => (double)p.Y),
        vertices.Max(p => (double)p.X), vertices.Max(p => (double)p.Y));

    private readonly record struct Point(double X, double Y)
    {
        public Point(Vector2 point) : this(point.X, point.Y) { }
    }
    private readonly record struct Bounds(double MinX, double MinY, double MaxX, double MaxY)
    {
        public bool Overlaps(Bounds other) => MinX < other.MaxX && other.MinX < MaxX && MinY < other.MaxY && other.MinY < MaxY;
    }
    private readonly record struct Triangle(Point A, Point B, Point C)
    {
        public Bounds Bounds { get; } = new(Math.Min(A.X, Math.Min(B.X, C.X)), Math.Min(A.Y, Math.Min(B.Y, C.Y)),
            Math.Max(A.X, Math.Max(B.X, C.X)), Math.Max(A.Y, Math.Max(B.Y, C.Y)));
    }
    internal readonly record struct AreaMeasurement(double AreaA, double AreaB, double Intersection, double Union, double IoU);
    private sealed record PolygonShape(Vector2[][] Rings, Triangle[] Triangles, double Area, Bounds Bounds);
    private sealed record Footprint(string Source, int PolygonIndex, PolygonShape Shape, string[] References);
    private sealed class Source(PolygonShape[] polygons, OverlayGeometry geometry, string reference)
    {
        public PolygonShape[] Polygons { get; } = polygons;
        public OverlayGeometry Geometry { get; } = geometry;
        public SortedSet<string> References { get; } = new(StringComparer.Ordinal) { reference };
        public SortedSet<string> Conflicts { get; } = new(StringComparer.Ordinal);
    }
    private struct Sum
    {
        public double Value;
        private double _correction;
        public void Add(double value)
        {
            var corrected = value - _correction;
            var next = Value + corrected;
            _correction = (next - Value) - corrected;
            Value = next;
        }
    }
}
