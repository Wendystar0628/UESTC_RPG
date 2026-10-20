using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Geometry;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;

namespace Campus.Gen.World;

/// <summary>ART-FIDELITY classification over the current authored world; only build callers invoke this compiler.</summary>
public static class WorldFidelityCompiler
{
    public const float GroundCell = 2f;
    public const float VisibilityReach = 60f;
    public const float WaypointReach = 30f;
    public const float WalkHalfWidth = 10f;
    // Demo-scale samples, independent of mesh tessellation and facade layout rounding.
    private const float SampleStep = 4f;
    private const float CornerBay = 3.6f;
    private static readonly GeometryFactory Factory = new();

    internal static FidelityFrozenData Compile(IReadOnlyDictionary<FeatureId, WorldFeature> features,
        PolygonRings boundary, WorldNetwork network, IInteriorIndex interiors, IOutskirtsModel? outskirts,
        IReadOnlyList<Waypoint> waypoints)
    {
        var ordered = features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal).ToArray();
        var featureZones = new Dictionary<FeatureId, FidelityZone>();
        foreach (var feature in ordered)
            if (Declared(feature) is { } zone) featureZones.Add(feature.Id, zone);
        var buildings = ordered.Where(feature => WorldFeatureGeometry.FacadePolygonsOf(feature, "building").Any()).ToArray();
        var blockers = new STRtree<Polygon>();
        foreach (var feature in buildings)
            foreach (var part in WorldFeatureKinds.Parts(feature, "building"))
                foreach (var rings in WorldFeatureGeometry.PolygonsOf(part.Geometry))
                {
                    var polygon = Polygon(rings);
                    blockers.Insert(polygon.EnvelopeInternal, polygon);
                }
        blockers.Build();

        var walk = new WorldWalkRegions(ordered, boundary, network);
        var samples = new SpatialGrid<Vector2>(VisibilityReach);
        foreach (var road in network.Roads)
        {
            var length = PlanLines.Length(road.Line.Points);
            var count = Math.Max(1, (int)Math.Ceiling(length / SampleStep));
            for (var i = 0; i <= count; i++)
            {
                var point = PlanLines.Sample(road.Line.Points, length * i / count).Point;
                if (walk.At(point.X, point.Y) == WalkClass.Walk) samples.AddPoint(point, point);
            }
        }
        var stands = waypoints.Where(point => point.Stand is not null)
            .Select(point => new Vector2(point.Stand!.Feet.X, point.Stand.Feet.Z)).ToArray();
        foreach (var stand in stands) samples.AddPoint(stand, stand);

        var main = new SpatialGrid<(Vector2 A, Vector2 B)>(WalkHalfWidth * 2);
        foreach (var road in network.Roads.Where(road => road.Props.TryText("walkNetwork") == "main"))
            for (var i = 1; i < road.Line.Points.Count; i++)
            {
                var a = road.Line.Points[i - 1]; var b = road.Line.Points[i];
                main.AddBox(Vector2.Min(a, b) - new Vector2(WalkHalfWidth), Vector2.Max(a, b) + new Vector2(WalkHalfWidth), (a, b));
            }
        bool Corridor(Vector2 point) => main.In(point).Any(line =>
            PlanLines.SegmentDistanceSquared(point, line.A, line.B) <= WalkHalfWidth * WalkHalfWidth);
        var heroPaths = new SpatialGrid<(Vector2 A, Vector2 B, float Radius)>(TileGrid.BaseEdge);
        foreach (var road in network.Roads.Where(road => road.Width > 0 && road.Props.TryText("fidelity") == "K1"))
            for (var i = 1; i < road.Line.Points.Count; i++)
            {
                var a = road.Line.Points[i - 1]; var b = road.Line.Points[i]; var radius = road.Width * .5f;
                heroPaths.AddBox(Vector2.Min(a, b) - new Vector2(radius), Vector2.Max(a, b) + new Vector2(radius), (a, b, radius));
            }

        var areas = new SpatialGrid<(PolygonRings Rings, FidelityZone Zone)>(TileGrid.BaseEdge);
        foreach (var feature in ordered)
        {
            var declared = featureZones.TryGetValue(feature.Id, out var authored) ? authored : (FidelityZone?)null;
            foreach (var rings in WorldFeatureGeometry.PolygonsOf(feature.Geometry))
            {
                var zone = declared ?? (feature.Kind.Value == "building"
                    ? interiors.Get(feature.Id) is { Open: true } ? FidelityZone.K2 : FidelityZone.K4
                    : feature.Props.TryText("walkNetwork") == "main" ? FidelityZone.K2 : (FidelityZone?)null);
                if (zone is { } value) Area(rings, value);
            }
        }
        foreach (var block in outskirts?.Blocks ?? []) Area(new(block.Footprint), block.Band switch
        {
            OutskirtsBand.O0 => FidelityZone.K3,
            OutskirtsBand.O1 or OutskirtsBand.O2 => FidelityZone.K5,
            _ => throw new InvalidDataException("Unknown outskirts fidelity band.")
        });

        var facades = new List<FacadeSegment>(); var roofs = new List<RoofZone>();
        foreach (var feature in buildings)
        {
            var hero = featureZones.TryGetValue(feature.Id, out var authored) && authored == FidelityZone.K1;
            roofs.Add(new(feature.Id, hero ? FidelityZone.K1 : FidelityZone.K4));
            var index = 0;
            foreach (var polygon in WorldFeatureGeometry.FacadePolygonsOf(feature, "building"))
                foreach (var (source, outer) in new[] { (polygon.Outer, true) }.Concat(polygon.Holes.Select(hole => (hole, false))))
                {
                    var ring = PlanGeometry.OpenRing(source, outer);
                    var edges = new List<FacadeSegment[]>();
                    for (var edge = 0; edge < ring.Count; edge++)
                    {
                        var a = ring[edge]; var b = ring[(edge + 1) % ring.Count];
                        var count = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(a, b) / SampleStep));
                        var normal = Vector2.Normalize(new Vector2(b.Y - a.Y, a.X - b.X));
                        var segments = new FacadeSegment[count];
                        for (var part = 0; part < count; part++)
                        {
                            var start = Vector2.Lerp(a, b, part / (float)count);
                            var end = Vector2.Lerp(a, b, (part + 1) / (float)count);
                            var center = (start + end) * .5f;
                            var nearStand = stands.Any(stand => Vector2.DistanceSquared(stand, center) <= WaypointReach * WaypointReach);
                            var zone = hero || nearStand ? FidelityZone.K1 : Visible(center, normal) ? FidelityZone.K3 : FidelityZone.K4;
                            var ground = zone == FidelityZone.K1 ? FidelityZone.K1 : Corridor(center + normal) ? FidelityZone.K2 : zone;
                            segments[part] = new(feature.Id, index++, start, end, zone, ground);
                        }
                        edges.Add(segments);
                    }
                    // One ordinary bay around a visible-to-back corner; no camera-dependent promotion.
                    var visibleEnds = edges.Select(edge => (Start: edge[0].Zone <= FidelityZone.K3, End: edge[^1].Zone <= FidelityZone.K3)).ToArray();
                    for (var edge = 0; edge < edges.Count; edge++)
                    {
                        var current = edges[edge];
                        if (visibleEnds[(edge + edges.Count - 1) % edges.Count].End)
                            Extend(current, fromStart: true);
                        if (visibleEnds[(edge + 1) % edges.Count].Start)
                            Extend(current, fromStart: false);
                    }
                    facades.AddRange(edges.SelectMany(edge => edge));
                }
        }
        var extent = boundary.Outer.Concat(outskirts?.Blocks.SelectMany(block => block.Footprint) ?? []).ToArray();
        var min = new Vector2(MathF.Floor(extent.Min(p => p.X) / GroundCell) * GroundCell,
            MathF.Floor(extent.Min(p => p.Y) / GroundCell) * GroundCell);
        var width = Math.Max(1, (int)MathF.Ceiling((extent.Max(p => p.X) - min.X) / GroundCell));
        var height = Math.Max(1, (int)MathF.Ceiling((extent.Max(p => p.Y) - min.Y) / GroundCell));
        var groundCells = new byte[checked(width * height)];
        for (var row = 0; row < height; row++)
            for (var column = 0; column < width; column++)
            {
                var point = min + new Vector2((column + .5f) * GroundCell, (row + .5f) * GroundCell);
                var zone = walk.At(point.X, point.Y) switch
                {
                    WalkClass.Outside => FidelityZone.K5,
                    WalkClass.Forbidden or WalkClass.Water => FidelityZone.K4,
                    WalkClass.Wade => FidelityZone.K2,
                    WalkClass.Walk => Corridor(point) ? FidelityZone.K2 : FidelityZone.K3,
                    _ => throw new InvalidDataException("Unknown walk classification.")
                };
                var areasHere = areas.In(point);
                foreach (var area in areasHere)
                    if (area.Zone == FidelityZone.K4 && zone != FidelityZone.K5 && PlanGeometry.Contains(area.Rings, point)) zone = FidelityZone.K4;
                foreach (var area in areasHere)
                    if (area.Zone < zone && PlanGeometry.Contains(area.Rings, point)) zone = area.Zone;
                if (heroPaths.In(point).Any(line => PlanLines.SegmentDistanceSquared(point, line.A, line.B) <= line.Radius * line.Radius)) zone = FidelityZone.K1;
                if (zone != FidelityZone.K5 && stands.Any(stand => Vector2.DistanceSquared(stand, point) <= WaypointReach * WaypointReach)) zone = FidelityZone.K1;
                groundCells[row * width + column] = (byte)zone;
            }
        var data = new FidelityFrozenData(min, width, height, groundCells, facades.ToArray(), roofs.ToArray(), featureZones, "");
        return data with { InputHash = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(data, ContractJson.Options))) };

        void Area(PolygonRings rings, FidelityZone zone) => areas.AddBox(
            new(rings.Outer.Min(p => p.X), rings.Outer.Min(p => p.Y)),
            new(rings.Outer.Max(p => p.X), rings.Outer.Max(p => p.Y)), (rings, zone));

        bool Visible(Vector2 point, Vector2 normal)
        {
            var start = point + normal * .5f;
            foreach (var source in samples.CellsAround(point).SelectMany(cell => cell))
            {
                var delta = source - point;
                if (delta.LengthSquared() > VisibilityReach * VisibilityReach || Vector2.Dot(delta, normal) <= 0) continue;
                var ray = Factory.CreateLineString([new(start.X, start.Y), new(source.X, source.Y)]);
                if (blockers.Query(ray.EnvelopeInternal).All(blocker => !blocker.Intersects(ray))) return true;
            }
            return false;
        }
    }

    private static FidelityZone? Declared(WorldFeature feature) => feature.Props.TryText("fidelity") switch
    {
        null => null,
        "K1" => FidelityZone.K1,
        "K2" => FidelityZone.K2,
        var zone => throw new InvalidDataException($"Invalid authored fidelity '{zone}' on '{feature.Id.Value}'.")
    };

    private static void Extend(FacadeSegment[] segments, bool fromStart)
    {
        var distance = 0f;
        for (var i = 0; i < segments.Length && distance < CornerBay; i++)
        {
            var index = fromStart ? i : segments.Length - 1 - i;
            var segment = segments[index];
            if (segment.Zone == FidelityZone.K4)
                segments[index] = segment with { Zone = FidelityZone.K3,
                    GroundBand = segment.GroundBand == FidelityZone.K4 ? FidelityZone.K3 : segment.GroundBand };
            distance += Vector2.Distance(segment.A, segment.B);
        }
    }

    private static Polygon Polygon(PolygonRings rings) => Factory.CreatePolygon(Ring(rings.Outer), rings.Holes.Select(Ring).ToArray());
    private static LinearRing Ring(IReadOnlyList<Vector2> ring) => Factory.CreateLinearRing(
        ring.Select(point => new Coordinate(point.X, point.Y)).Append(new(ring[0].X, ring[0].Y)).ToArray());
}
