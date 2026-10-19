using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Tests;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.RelateNG;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace Campus.Gen.World.Tests;

/// <summary>Minimal target reader for Q-045, not a production loader. Migrate the read/derive logic and delete both Q-045 files in Q-046.</summary>
internal static class Q045WorldFormatReadback
{
    internal sealed record Target(GeoFrame Geo, PolygonRings Boundary, IReadOnlyDictionary<FeatureId, WorldFeature> Features, WorldNetwork Network);
    private static readonly GeometryFactory Factory = new();

    internal static Target Read(string directory)
    {
        using var geoDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "geo.json")));
        var geo = geoDoc.RootElement;
        var origin = geo.GetProperty("origin"); var bounds = geo.GetProperty("bounds");
        var frame = new GeoFrame(origin[0].GetDouble(), origin[1].GetDouble(),
            int.Parse(geo.GetProperty("crs").GetString()!.Split(':')[^1], CultureInfo.InvariantCulture),
            new(bounds[0].GetSingle(), bounds[1].GetSingle()), new(bounds[2].GetSingle(), bounds[3].GetSingle()));
        var boundary = Value<PolygonRings>(geo.GetProperty("boundary"));
        var prefix = geo.GetProperty("entityPrefix").GetString()!;
        var features = new Dictionary<FeatureId, WorldFeature>();
        var roads = new List<NetworkLine>(); var waterways = new List<NetworkLine>(); var trees = new List<NetworkTree>();
        foreach (var zone in Q045WorldFormatConversion.Zones)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, zone + ".json")));
            foreach (var raw in doc.RootElement.GetProperty("features").EnumerateArray())
            {
                var id = new FeatureId(Text(raw, "id")!); var kind = new DesignKind(Text(raw, "kind")!);
                var parts = new List<WorldFeaturePart>();
                if (raw.TryGetProperty("parts", out var members))
                    foreach (var member in members.EnumerateArray())
                    {
                        var partId = Text(member, "id")!;
                        Polyline? line = member.TryGetProperty("line", out var nodes) ? new(Value<Vector2[]>(nodes)) : null;
                        var width = Number(member, "width") ?? 0;
                        var geometry = member.TryGetProperty("geometry", out var shape) ? Value<OverlayGeometry>(shape)
                            : Buffer(line ?? throw new InvalidOperationException("Missing part geometry: " + partId), width, boundary)
                                ?? throw new InvalidOperationException("Empty buffered part: " + partId);
                        var part = new WorldFeaturePart(partId, geometry, Number(member, "height"), Integer(member, "storeys"))
                        {
                            Props = Props(member),
                            FacadeGeometry = member.TryGetProperty("facadePolygons", out var memberFacade) ? WorldModelLoader.Geometry(Value<PolygonRings[]>(memberFacade)) : null
                        };
                        parts.Add(part);
                        if (line is not null) AddLine(new(new(OsmFeatureIds.FromPart(prefix, partId).Source), line, width) { Props = part.Props }, kind.Value);
                    }
                var combined = parts.Count == 0 ? Value<OverlayGeometry>(raw.GetProperty("geometry"))
                    : WorldModelLoader.Geometry(parts.SelectMany(part => WorldFeatureGeometry.PolygonsOf(part.Geometry)).ToArray());
                var height = Number(raw, "height");
                var feature = new WorldFeature(id, FeatureSource.Design, kind, combined)
                {
                    Parts = parts.ToArray(), Zone = new(zone), Name = Text(raw, "name"), Height = height, Storeys = Integer(raw, "storeys"),
                    Family = Text(raw, "family") is { } family ? new(family) : null,
                    Facade = Text(raw, "facade") is { } facadePreset ? new(facadePreset) : null,
                    Props = Props(raw), Bounds = WorldModelLoader.BoundsOf(combined, height ?? 0)
                };
                if (!features.TryAdd(id, feature)) throw new InvalidOperationException("Duplicate world feature: " + id.Value);
                var sourceLine = OsmFeatureIds.TryFromSource(prefix, id.Value, out _);
                var designRoad = kind.Value == "road" && feature.TryText("highway") is "footway" or "path" or "pedestrian";
                // Source network lines include area=yes boundaries; only authored additions exclude those from new centerlines.
                if (parts.Count == 0 && combined is PolylineGeometry polyline && (sourceLine || designRoad && feature.TryText("area") != "yes"))
                    AddLine(new(id, polyline.Line, feature.TryNumber("width") ?? 0) { Props = feature.Props }, kind.Value);
            }
            trees.AddRange(doc.RootElement.GetProperty("trees").EnumerateArray().Select(Value<NetworkTree>));
        }
        // Original source lists are ordered by numeric way/node identity, followed by ordinal design additions.
        var orderedRoads = Order(roads, line => line.Id).ToArray();
        var orderedWater = Order(waterways, line => line.Id).ToArray();
        var network = new WorldNetwork(orderedRoads, orderedWater,
            orderedRoads.Where(line => line.Props.TryText("bridge") == "yes").ToArray(),
            orderedWater.Where(line => line.Props.TryText("tunnel") == "culvert").ToArray(), Order(trees, tree => tree.Id).ToArray());
        return new(frame, boundary, features, network);

        void AddLine(NetworkLine line, string kind)
        {
            if (kind == "road" || line.Props.TryText("highway") is not null) roads.Add(line);
            else if (kind is "water" or "weir" || line.Props.TryText("waterway") is not null) waterways.Add(line);
            else throw new InvalidOperationException("Unclassified network line: " + line.Id.Value);
        }
        IEnumerable<T> Order<T>(IEnumerable<T> items, Func<T, FeatureId> id) => items
            .OrderBy(item => OsmFeatureIds.TryFromSource(prefix, id(item).Value, out _) ? 0 : 1)
            .ThenBy(item => OsmFeatureIds.TryFromSource(prefix, id(item).Value, out _) ? long.Parse(id(item).Value.Split(':')[^1], CultureInfo.InvariantCulture) : 0)
            .ThenBy(item => id(item).Value, StringComparer.Ordinal);
    }

    internal static void Compare(WorldModelLoadResult original, Target target)
    {
        Require(original.Geo == target.Geo, "geo frame");
        Require(SameGeometry(new PolygonGeometry(original.Boundary), new PolygonGeometry(target.Boundary)), "boundary");
        var networkIds = original.World.Network.Roads.Concat(original.World.Network.Waterways).Select(line => line.Id).ToHashSet();
        Require(target.Features.Keys.Where(id => !original.World.Features.ContainsKey(id)).All(networkIds.Contains), "unexpected added feature");
        foreach (var before in original.World.Features.Values)
        {
            Require(target.Features.TryGetValue(before.Id, out var after), before.Id.Value + " missing");
            var label = before.Id.Value;
            Require(before.Kind == after!.Kind && before.Name == after.Name && before.Family == after.Family && before.Facade == after.Facade, label + " identity/style");
            Require(Near(before.Height, after.Height) && before.Storeys == after.Storeys, label + " height/storeys");
            Require(SameGeometry(before.Geometry, after.Geometry), label + " geometry");
            Require(before.Parts.Count == after.Parts.Count, label + " part count");
            CheckProps(before.Props, after.Props, label);
            for (var i = 0; i < before.Parts.Count; i++)
            {
                var a = before.Parts[i]; var b = after.Parts[i];
                Require(a.SourceId == b.SourceId && Near(a.Height, b.Height) && a.Storeys == b.Storeys, label + " part identity/height");
                Require(SameGeometry(a.Geometry, b.Geometry), label + "/" + a.SourceId + " geometry");
                Require((a.FacadeGeometry is null) == (b.FacadeGeometry is null) &&
                    (a.FacadeGeometry is null || SameGeometry(a.FacadeGeometry, b.FacadeGeometry)), label + " facadePolygons");
                CheckProps(a.Props, b.Props, label + "/" + a.SourceId);
            }
        }
        Lines(original.World.Network.Roads, target.Network.Roads, "roads");
        Lines(original.World.Network.Waterways, target.Network.Waterways, "waterways");
        Lines(original.World.Network.Bridges, target.Network.Bridges, "bridges");
        Lines(original.World.Network.Culverts, target.Network.Culverts, "culverts");
        var sourceTrees = original.World.Network.Trees;
        Require(sourceTrees.Count == target.Network.Trees.Count, "trees count");
        for (var i = 0; i < sourceTrees.Count; i++)
        {
            var a = sourceTrees[i]; var b = target.Network.Trees[i];
            Require(a.Id == b.Id && a.Species == b.Species && Vector3.Distance(a.Position, b.Position) <= TestTolerance.Position, "tree " + a.Id.Value);
        }
        Console.WriteLine($"Q-045 readback passed: {original.World.Features.Count} features, {target.Network.Roads.Count} roads, {target.Network.Waterways.Count} waterways, {sourceTrees.Count} trees.");
    }

    private static void Lines(IReadOnlyList<NetworkLine> before, IReadOnlyList<NetworkLine> after, string label)
    {
        Require(before.Count == after.Count, label + " count");
        for (var i = 0; i < before.Count; i++)
        {
            var a = before[i]; var b = after[i];
            Require(a.Id == b.Id && Near(a.Width, b.Width), label + " order/id/width " + a.Id.Value);
            Require(a.Line.Points.Count == b.Line.Points.Count && a.Line.Points.Zip(b.Line.Points)
                .All(pair => Vector2.Distance(pair.First, pair.Second) <= TestTolerance.Position), label + " nodes " + a.Id.Value);
            CheckProps(a.Props, b.Props, label + "/" + a.Id.Value);
        }
    }

    private static void CheckProps(JsonElement? before, JsonElement? after, string label)
    {
        var expected = Q045WorldFormatConversion.Slim(before);
        if (expected is null) return;
        Require(after is { ValueKind: JsonValueKind.Object }, label + " props missing");
        foreach (var property in expected)
            Require(after!.Value.TryGetProperty(property.Key, out var value) && JsonElement.DeepEquals(value,
                JsonSerializer.SerializeToElement(property.Value, ContractJson.Options)), label + " props." + property.Key);
    }

    internal static OverlayGeometry? Buffer(Polyline line, float width, PolygonRings boundary)
    {
        var alignment = Factory.CreateLineString(line.Points.Select(point => new Coordinate(point.X, point.Y)).ToArray());
        var parameters = new BufferParameters { EndCapStyle = EndCapStyle.Flat, JoinStyle = JoinStyle.Mitre };
        var clipped = BufferOp.Buffer(alignment, width / 2d, parameters).Intersection(ToNts(new PolygonGeometry(boundary)));
        var polygons = Polygons(clipped).Select(polygon => new PolygonRings(Ring(polygon.ExteriorRing),
            Enumerable.Range(0, polygon.NumInteriorRings).Select(index => Ring(polygon.GetInteriorRingN(index))).ToArray())).ToArray();
        return polygons.Length == 0 ? null : WorldModelLoader.Geometry(polygons);
    }

    internal static bool SameGeometry(OverlayGeometry a, OverlayGeometry? b)
    {
        if (b is null) return false;
        if (a is PointGeometry ap && b is PointGeometry bp) return Vector3.Distance(ap.Position, bp.Position) <= TestTolerance.Position;
        if (a is PolylineGeometry al && b is PolylineGeometry bl)
            return al.Line.Points.Count == bl.Line.Points.Count && al.Line.Points.Zip(bl.Line.Points).All(pair => Vector2.Distance(pair.First, pair.Second) <= TestTolerance.Position);
        if (WorldFeatureGeometry.PolygonsOf(a).Count == 0 || WorldFeatureGeometry.PolygonsOf(b).Count == 0) return false;
        var ga = ToNts(a); var gb = ToNts(b); var tolerance = TestTolerance.Position;
        return Relate(ga.Buffer(tolerance), gb, RelatePredicate.Covers()) && Relate(gb.Buffer(tolerance), ga, RelatePredicate.Covers())
            && Relate(ga.Boundary.Buffer(tolerance), gb.Boundary, RelatePredicate.Covers())
            && Relate(gb.Boundary.Buffer(tolerance), ga.Boundary, RelatePredicate.Covers());
    }

    internal static bool Inside(PolygonRings boundary, Vector2 point) => Relate(ToNts(new PolygonGeometry(boundary)),
        Factory.CreatePoint(new Coordinate(point.X, point.Y)), RelatePredicate.Covers());
    internal static bool Intersects(OverlayGeometry geometry, PolygonRings boundary) => Relate(ToNts(geometry),
        ToNts(new PolygonGeometry(boundary)), RelatePredicate.Intersects());

    // RelateNG handles invalid input topology without changing the original area or boundary used by the 2 cm comparison.
    private static bool Relate(NtsGeometry a, NtsGeometry b, TopologyPredicate predicate) => RelateNG.Relate(a, b, predicate);

    private static NtsGeometry ToNts(OverlayGeometry geometry) => geometry switch
    {
        PointGeometry p => Factory.CreatePoint(new Coordinate(p.Position.X, p.Position.Z)),
        PolylineGeometry p => Factory.CreateLineString(p.Line.Points.Select(point => new Coordinate(point.X, point.Y)).ToArray()),
        PolygonGeometry p => Polygon(p.Rings),
        MultiPolygonGeometry p => Factory.CreateMultiPolygon(p.Polygons.Select(Polygon).ToArray()),
        _ => throw new ArgumentException("Unknown geometry.")
    };
    private static Polygon Polygon(PolygonRings polygon) => Factory.CreatePolygon(Closed(polygon.Outer), polygon.Holes.Select(Closed).ToArray());
    private static LinearRing Closed(IReadOnlyList<Vector2> points) => Factory.CreateLinearRing(points.Select(point => new Coordinate(point.X, point.Y)).Append(new(points[0].X, points[0].Y)).ToArray());
    private static IReadOnlyList<Vector2> Ring(LineString ring) => ring.Coordinates.Take(ring.NumPoints - 1).Select(point => new Vector2((float)point.X, (float)point.Y)).ToArray();
    private static IEnumerable<Polygon> Polygons(NtsGeometry geometry)
    {
        if (geometry.IsEmpty) yield break;
        if (geometry is Polygon polygon) { yield return polygon; yield break; }
        if (geometry is GeometryCollection collection)
            for (var i = 0; i < collection.NumGeometries; i++) foreach (var item in Polygons(collection.GetGeometryN(i))) yield return item;
    }
    private static T Value<T>(JsonElement element) => element.Deserialize<T>(ContractJson.Options)!;
    private static JsonElement? Props(JsonElement element) => element.TryGetProperty("props", out var value) ? value.Clone() : null;
    private static string? Text(JsonElement element, string key) => element.TryGetProperty(key, out var value) ? value.GetString() : null;
    private static float? Number(JsonElement element, string key) => element.TryGetProperty(key, out var value) ? value.GetSingle() : null;
    private static int? Integer(JsonElement element, string key) => element.TryGetProperty(key, out var value) ? value.GetInt32() : null;
    private static bool Near(float? a, float? b) => a is null || b is null ? a is null && b is null : MathF.Abs(a.Value - b.Value) <= TestTolerance.Position;
    private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException("Q-045 world differs: " + label); }
}
