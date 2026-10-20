using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using NetTopologySuite.Geometries;

namespace Campus.Kernel.Bake;

/// <summary>
/// Load-time validation of the effective, post-overlay world. One canonical source polygon is one
/// comparison object; aggregate entity geometry is not an additional footprint when Parts exist.
/// Claims are diagnostic only. This class does not load data, mutate the world or cache results.
/// </summary>
public static class BuildingOverlapValidator
{
    public const double MaximumIntersectionOverUnion = 0.9;
    private static readonly GeometryFactory Factory = new();

    /// <summary>
    /// Throws one stable report containing every different-source pair whose area IoU is strictly
    /// greater than 0.9. Conflicting aliases of one source fail before comparisons. Invoke once for
    /// each effective world before any cache hit or geometry generation can bypass validation.
    /// </summary>
    public static void Validate(IWorldModel world, Func<FeatureId, Aspects, string?>? claimedBy = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        var footprints = Collect(world, claimedBy);
        var failures = new List<string>();
        for (var a = 0; a < footprints.Count; a++)
            for (var b = a + 1; b < footprints.Count; b++)
            {
                var left = footprints[a];
                var right = footprints[b];
                if (StringComparer.Ordinal.Equals(left.Source, right.Source) || !left.Shape.Bounds.Intersects(right.Shape.Bounds)) continue;
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

    private static List<Footprint> Collect(IWorldModel world, Func<FeatureId, Aspects, string?>? claimedBy)
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

        void Add(WorldFeature feature, string id, WorldGeometry geometry)
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
                throw new InvalidDataException($"source={Quote(id)}; {reference}; invalid source geometry.", e);
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

    private static bool SameNonArea(WorldGeometry a, WorldGeometry b) => (a, b) switch
    {
        (PointGeometry x, PointGeometry y) => x.Position == y.Position,
        (PolylineGeometry x, PolylineGeometry y) => x.Line.Points.SequenceEqual(y.Line.Points) || x.Line.Points.SequenceEqual(y.Line.Points.Reverse()),
        _ => false
    };

    private static string DescribeReference(WorldFeature feature, Func<FeatureId, Aspects, string?>? claimedBy) =>
        $"entity={Quote(feature.Id.Value)}, name={Quote(feature.Name ?? "")}, " +
        $"owner={{massing:{OwnerValue(claimedBy, feature.Id, Aspects.Massing)},facade:{OwnerValue(claimedBy, feature.Id, Aspects.Facade)},roof:{OwnerValue(claimedBy, feature.Id, Aspects.Roof)}}}";

    // The resolver reports actual claims. Absence is not assigned to a guessed generic owner.
    private static string OwnerValue(Func<FeatureId, Aspects, string?>? resolver, FeatureId feature, Aspects aspect) =>
        resolver is null ? "(unavailable)" : resolver(feature, aspect) is { } owner ? Quote(owner) : "(unclaimed)";

    private static string Describe(Footprint footprint) =>
        $"source={Quote(footprint.Source)}; polygon={footprint.PolygonIndex.ToString(CultureInfo.InvariantCulture)}; " +
        $"references=[{string.Join("; ", footprint.References)}]";

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";

    // Normalise ring starts, orientation, redundant straight vertices and hole order before comparing
    // aliases. Polygon positions in a MultiPolygon remain the supplied source indices.
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
        var polygon = Factory.CreatePolygon(ToRing(canonical[0]), canonical.Skip(1).Select(ToRing).ToArray());
        if (!double.IsFinite(polygon.Area) || polygon.Area <= 0) throw new ArgumentException("Polygon area must be finite and positive.");
        return new(canonical, polygon);
    }

    // Promote the supplied float X/Z metres once; area/IoU stay in double and never round back to float.
    // Unlike terrain union, overlap validation must measure the supplied footprint without snapping it.
    private static LinearRing ToRing(Vector2[] ring)
    {
        var coordinates = new Coordinate[ring.Length + 1];
        for (var i = 0; i < ring.Length; i++) coordinates[i] = new((double)ring[i].X, (double)ring[i].Y);
        coordinates[^1] = coordinates[0].Copy();
        return Factory.CreateLinearRing(coordinates);
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
        var intersection = a.Bounds.Intersects(b.Bounds) ? a.Geometry.Intersection(b.Geometry).Area : 0;
        // Clamp only arithmetic drift to mathematical area bounds; the rejection threshold stays 0.9.
        var common = Math.Clamp(intersection, 0, Math.Min(a.Area, b.Area));
        var union = a.Area + b.Area - common;
        return new(a.Area, b.Area, common, union, common / union);
    }

    internal readonly record struct AreaMeasurement(double AreaA, double AreaB, double Intersection, double Union, double IoU);
    private sealed record PolygonShape(Vector2[][] Rings, Polygon Geometry)
    {
        public double Area { get; } = Geometry.Area;
        public Envelope Bounds { get; } = Geometry.EnvelopeInternal;
    }
    private sealed record Footprint(string Source, int PolygonIndex, PolygonShape Shape, string[] References);
    private sealed class Source(PolygonShape[] polygons, WorldGeometry geometry, string reference)
    {
        public PolygonShape[] Polygons { get; } = polygons;
        public WorldGeometry Geometry { get; } = geometry;
        public SortedSet<string> References { get; } = new(StringComparer.Ordinal) { reference };
        public SortedSet<string> Conflicts { get; } = new(StringComparer.Ordinal);
    }
}
