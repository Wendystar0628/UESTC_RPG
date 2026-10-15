using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Stubs.Foundation;

public sealed record D0CampusData(
    GeoFrame Geo,
    Vector2 Center,
    Box3 Bounds,
    TileKey TileMin,
    TileKey TileMax,
    IReadOnlyDictionary<FeatureId, WorldFeature> Features,
    ContentOutput Content,
    IReadOnlyList<Collider> Colliders)
{
    public PolygonRings Boundary { get; init; } = new([]);
    public Box3 GroundBounds { get; init; }
    public int SourceFeatureCount { get; init; }
    public int SourceEntityCount { get; init; }
}

/// <summary>
/// Pure-data D0 adapter for the existing campus export. Call on a worker thread, then mount Content on the main thread.
/// Building volumes are deliberately bounding boxes; this is a development stage, not the production world parser.
/// </summary>
public static class D0CampusLoader
{
    public static D0CampusData FromBytes(ReadOnlyMemory<byte> campusBytes,
        IReadOnlySet<FeatureId>? openFeatureIds = null)
    {
        using var document = JsonDocument.Parse(campusBytes);
        var root = document.RootElement;
        var meta = root.GetProperty("meta");
        var origin = meta.GetProperty("origin");
        if (origin.GetArrayLength() != 2) throw new JsonException("campus.meta.origin must contain longitude and latitude.");
        var originLon = origin[0].GetDouble();
        var originLat = origin[1].GetDouble();
        if (!double.IsFinite(originLon) || !double.IsFinite(originLat)) throw new JsonException("Non-finite campus origin.");
        var crs = Text(meta, "crs") ?? throw new JsonException("Missing campus.meta.crs.");
        if (!crs.StartsWith("EPSG:", StringComparison.Ordinal) ||
            !int.TryParse(crs.AsSpan(5), NumberStyles.None, CultureInfo.InvariantCulture, out var epsg))
            throw new JsonException("campus.meta.crs must be an EPSG code.");
        var boundaryBounds = ReadBounds(meta.GetProperty("bounds"));
        var geo = new GeoFrame(originLon, originLat, epsg, boundaryBounds.Min, boundaryBounds.Max);
        var boundaryRings = ReadRings(root.GetProperty("boundary"));
        if (boundaryRings.Count == 0 || boundaryRings[0].Count < 3) throw new JsonException("Campus boundary needs an outer ring.");
        var boundary = new PolygonRings(Array.AsReadOnly(boundaryRings[0].ToArray()),
            Array.AsReadOnly(boundaryRings.Skip(1).Select(ring => (IReadOnlyList<Vector2>)Array.AsReadOnly(ring.ToArray())).ToArray()));
        var center = BoundaryCenter(root.GetProperty("boundary"), (boundaryBounds.Min + boundaryBounds.Max) * 0.5f);

        // The D0 stage is square and covers at least 150 m beyond the source boundary.
        var groundCenter = (boundaryBounds.Min + boundaryBounds.Max) * 0.5f;
        var boundarySize = boundaryBounds.Max - boundaryBounds.Min;
        var groundHalf = MathF.Max(2500f, MathF.Max(boundarySize.X, boundarySize.Y) + 300f) * 0.5f;
        var groundMin = groundCenter - new Vector2(groundHalf);
        var groundMax = groundCenter + new Vector2(groundHalf);
        var groundBounds = new Box3(new(groundMin.X, -0.25f, groundMin.Y), new(groundMax.X, 0f, groundMax.Y));
        // No outskirts are present in this adapter. These are the documented fallback roam heights.
        var bounds = new Box3(new(groundMin.X, -28f, groundMin.Y), new(groundMax.X, 2000f, groundMax.Y));
        var tileMin = new TileKey((int)MathF.Floor(groundMin.X / 64f), (int)MathF.Floor(groundMin.Y / 64f));
        var tileMax = new TileKey((int)MathF.Ceiling(groundMax.X / 64f) - 1, (int)MathF.Ceiling(groundMax.Y / 64f) - 1);

        var sourceFeatures = root.GetProperty("features").EnumerateArray()
            .OrderBy(feature => Text(feature, "id"), StringComparer.Ordinal).ToArray();
        var entities = root.GetProperty("entities").EnumerateArray()
            .OrderBy(entity => Text(entity, "id"), StringComparer.Ordinal).ToArray();
        var bySourceId = sourceFeatures.ToDictionary(feature => RequiredText(feature, "id"), StringComparer.Ordinal);
        var ownersBySource = new Dictionary<string, List<FeatureId>>(StringComparer.Ordinal);
        var byEntityId = new Dictionary<string, List<JsonElement>>(StringComparer.Ordinal);
        foreach (var source in sourceFeatures)
        {
            var entityId = Text(source, "entityId");
            if (entityId is null) continue;
            if (!byEntityId.TryGetValue(entityId, out var group)) byEntityId.Add(entityId, group = []);
            group.Add(source);
        }

        var features = new Dictionary<FeatureId, WorldFeature>();
        foreach (var entity in entities)
        {
            var id = new FeatureId(RequiredText(entity, "id"));
            var members = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
            if (byEntityId.TryGetValue(id.Value, out var directMembers))
                foreach (var member in directMembers) members[RequiredText(member, "id")] = member;
            if (entity.TryGetProperty("featureIds", out var links))
                foreach (var link in links.EnumerateArray())
                {
                    var key = link.GetString();
                    if (key is not null && bySourceId.TryGetValue(key, out var member)) members[key] = member;
                }
            foreach (var key in members.Keys)
            {
                if (!ownersBySource.TryGetValue(key, out var owners)) ownersBySource.Add(key, owners = []);
                owners.Add(id);
            }

            var plan = ReadBounds(entity.GetProperty("bounds"));
            var height = Number(entity, "height") ?? 0f;
            var levels = Integer(entity, "levels");
            var kind = Text(entity, "kind") ?? "poi";
            foreach (var member in members.Values)
            {
                height = MathF.Max(height, Number(member, "height") ?? 0f);
                if (Text(member, "kind") == "building") kind = "building";
                levels = Math.Max(levels ?? 0, Integer(member, "levels") ?? 0);
            }
            if (kind == "building" && height <= 0f) height = MathF.Max(1, levels ?? 1) * 3.5f;
            OverlayGeometry entityGeometry = members.Count == 0
                ? new PointGeometry(EntityPosition(entity, (plan.Min + plan.Max) * 0.5f))
                : new PolygonGeometry(Rectangle(plan.Min, plan.Max));
            var world = new WorldFeature(id, FeatureSource.Osm, new(kind), entityGeometry)
            {
                Parts = Array.AsReadOnly(members.Values.Select(member => new WorldFeaturePart(
                    RequiredText(member, "id"), SourceGeometry(member), Number(member, "height"), Integer(member, "levels"))
                { Props = member.Clone() }).ToArray()),
                Name = Text(entity, "name"),
                Height = height,
                Storeys = levels is > 0 ? levels : null,
                Bounds = new(new(plan.Min.X, 0f, plan.Min.Y), new(plan.Max.X, height, plan.Max.Y)),
                Props = entity.Clone()
            };
            if (!features.TryAdd(id, world)) throw new JsonException($"Duplicate entity ID: {id.Value}.");
        }

        var ground = new MeshBatch("d0.ground");
        var water = new MeshBatch("d0.water");
        var road = new MeshBatch("d0.road");
        var building = new MeshBatch("d0.building");
        ground.Quad(new(groundMin.X, 0f, groundMin.Y), new(groundMax.X, 0f, groundMin.Y),
            new(groundMax.X, 0f, groundMax.Y), new(groundMin.X, 0f, groundMax.Y), Vector3.UnitY);
        var colliders = new List<Collider>
        {
            new SlabCollider(Rectangle(groundMin, groundMax).Outer, 0f, 0.25f, SurfaceTag.Ground) { Foot = FootMat.Grass }
        };

        foreach (var source in sourceFeatures)
        {
            var kind = Text(source, "kind");
            if (!source.TryGetProperty("polygons", out var polygons) || polygons.ValueKind != JsonValueKind.Array) continue;
            var sourceId = RequiredText(source, "id");
            var isOpen = openFeatureIds is not null && ownersBySource.TryGetValue(sourceId, out var owners) && owners.Any(openFeatureIds.Contains);
            foreach (var polygon in polygons.EnumerateArray())
            {
                var rings = ReadRings(polygon);
                if (rings.Count == 0 || rings[0].Count < 3) continue;
                switch (kind)
                {
                    case "building":
                        if (isOpen) break;
                        var box = RingBounds(rings[0]);
                        var height = Number(source, "height") ?? 0f;
                        if (height <= 0f) height = MathF.Max(1, Integer(source, "levels") ?? 1) * 3.5f;
                        if (box.Max.X <= box.Min.X || box.Max.Y <= box.Min.Y) break;
                        AddBox(building, box.Min, box.Max, height);
                        AddShell(colliders, box.Min, box.Max, height);
                        break;
                    case "water": Fill(water, rings, 0.025f); break;
                    case "road": Fill(road, rings, 0.055f); break;
                    case "parking": Fill(road, rings, 0.035f); break;
                    case "sport": Fill(road, rings, 0.04f); break;
                    case "green":
                    case "poi":
                        break; // Existing green ground is the D0 surface; point POIs get no invented mass.
                    default:
                        throw new JsonException($"Unsupported campus feature kind: {kind ?? "<missing>"}.");
                }
            }
        }
        var meshes = new List<GeometryBundle>();
        meshes.AddRange(ground.Finish());
        meshes.AddRange(water.Finish());
        meshes.AddRange(road.Finish());
        meshes.AddRange(building.Finish());
        return new(geo, center, bounds, tileMin, tileMax,
            new ReadOnlyDictionary<FeatureId, WorldFeature>(features), new(meshes.AsReadOnly(), []), colliders.AsReadOnly())
        {
            Boundary = boundary,
            GroundBounds = groundBounds,
            SourceFeatureCount = sourceFeatures.Length,
            SourceEntityCount = entities.Length
        };
    }

    private static string RequiredText(JsonElement value, string property) =>
        Text(value, property) is { Length: > 0 } text ? text : throw new JsonException($"Missing {property}.");
    private static string? Text(JsonElement value, string property) =>
        value.TryGetProperty(property, out var element) && element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    private static float? Number(JsonElement value, string property)
    {
        if (!value.TryGetProperty(property, out var element) || element.ValueKind == JsonValueKind.Null) return null;
        var number = element.GetSingle();
        return float.IsFinite(number) ? number : throw new JsonException($"Non-finite {property}.");
    }
    private static int? Integer(JsonElement value, string property)
    {
        if (!value.TryGetProperty(property, out var element) || element.ValueKind == JsonValueKind.Null) return null;
        // JSON exports may spell an integer as 2.0 or 2e0. GetInt32 only accepts the integer lexical form.
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDecimal(out var number) ||
            number != decimal.Truncate(number) || number < int.MinValue || number > int.MaxValue)
            throw new JsonException($"{property} must be a finite whole number in the Int32 range.");
        return decimal.ToInt32(number);
    }
    private static Vector2 Point(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 2) throw new JsonException("Invalid X/Z point.");
        var point = new Vector2(value[0].GetSingle(), value[1].GetSingle());
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) throw new JsonException("Non-finite X/Z point.");
        return point;
    }
    private static (Vector2 Min, Vector2 Max) ReadBounds(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 4) throw new JsonException("Invalid X/Z bounds.");
        var min = new Vector2(value[0].GetSingle(), value[1].GetSingle());
        var max = new Vector2(value[2].GetSingle(), value[3].GetSingle());
        if (!float.IsFinite(min.X) || !float.IsFinite(min.Y) || !float.IsFinite(max.X) || !float.IsFinite(max.Y) || min.X > max.X || min.Y > max.Y)
            throw new JsonException("Invalid finite or ordered X/Z bounds.");
        return (min, max);
    }
    private static PolygonRings Rectangle(Vector2 min, Vector2 max) =>
        new([min, new(max.X, min.Y), max, new(min.X, max.Y)]);
    private static Vector3 EntityPosition(JsonElement entity, Vector2 fallback)
    {
        if (!entity.TryGetProperty("anchor", out var anchor) || anchor.ValueKind == JsonValueKind.Null)
            return new(fallback.X, 0f, fallback.Y);
        if (anchor.ValueKind != JsonValueKind.Array || anchor.GetArrayLength() is not (2 or 3))
            throw new JsonException("Entity anchor must contain X/Z or X/Y/Z.");
        var position = anchor.GetArrayLength() == 3
            ? new Vector3(anchor[0].GetSingle(), anchor[1].GetSingle(), anchor[2].GetSingle())
            : new Vector3(anchor[0].GetSingle(), 0f, anchor[1].GetSingle());
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
            throw new JsonException("Entity anchor must be finite.");
        return position;
    }
    private static OverlayGeometry SourceGeometry(JsonElement source)
    {
        var polygons = new List<PolygonRings>();
        foreach (var polygon in source.GetProperty("polygons").EnumerateArray())
        {
            var rings = ReadRings(polygon);
            if (rings.Count == 0 || rings[0].Count < 3) throw new JsonException("Source polygon needs a valid outer ring.");
            polygons.Add(new(Array.AsReadOnly(rings[0].ToArray()),
                Array.AsReadOnly(rings.Skip(1).Select(ring => (IReadOnlyList<Vector2>)Array.AsReadOnly(ring.ToArray())).ToArray())));
        }
        return polygons.Count switch
        {
            0 => throw new JsonException("Source feature needs polygon geometry."),
            1 => new PolygonGeometry(polygons[0]),
            _ => new MultiPolygonGeometry(polygons.AsReadOnly())
        };
    }
    private static List<IReadOnlyList<Vector2>> ReadRings(JsonElement polygon)
    {
        var result = new List<IReadOnlyList<Vector2>>();
        foreach (var source in polygon.EnumerateArray())
        {
            var points = new List<Vector2>();
            foreach (var value in source.EnumerateArray())
            {
                var point = Point(value);
                if (points.Count == 0 || points[^1] != point) points.Add(point);
            }
            if (points.Count > 1 && points[0] == points[^1]) points.RemoveAt(points.Count - 1);
            result.Add(points);
        }
        return result;
    }
    private static (Vector2 Min, Vector2 Max) RingBounds(IReadOnlyList<Vector2> ring)
    {
        var min = ring[0];
        var max = ring[0];
        foreach (var point in ring) { min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
        return (min, max);
    }
    private static Vector2 BoundaryCenter(JsonElement boundary, Vector2 fallback)
    {
        double areaSum = 0d, xSum = 0d, zSum = 0d;
        var rings = ReadRings(boundary);
        for (var r = 0; r < rings.Count; r++)
        {
            var ring = rings[r];
            double twiceArea = 0d, x = 0d, z = 0d;
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Count];
                var cross = (double)a.X * b.Y - (double)b.X * a.Y;
                twiceArea += cross;
                x += (a.X + b.X) * cross;
                z += (a.Y + b.Y) * cross;
            }
            if (Math.Abs(twiceArea) < 1e-8) continue;
            var weight = Math.Abs(twiceArea) * (r == 0 ? 1d : -1d);
            xSum += x / (3d * twiceArea) * weight;
            zSum += z / (3d * twiceArea) * weight;
            areaSum += weight;
        }
        return areaSum > 0d ? new((float)(xSum / areaSum), (float)(zSum / areaSum)) : fallback;
    }

    // Even-odd scan bands handle concave outlines and holes without filling lake islands or road courtyards.
    private static void Fill(MeshBatch mesh, IReadOnlyList<IReadOnlyList<Vector2>> rings, float y)
    {
        var edges = new List<(Vector2 A, Vector2 B)>();
        var rows = new SortedSet<float>();
        foreach (var ring in rings)
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Count];
                rows.Add(a.Y);
                if (a.Y != b.Y) edges.Add((a, b));
            }
        var heights = rows.ToArray();
        for (var i = 0; i + 1 < heights.Length; i++)
        {
            var lo = heights[i];
            var hi = heights[i + 1];
            var middle = ((double)lo + hi) * 0.5d;
            var active = edges.Where(edge => Math.Min(edge.A.Y, edge.B.Y) < middle && Math.Max(edge.A.Y, edge.B.Y) > middle)
                .OrderBy(edge => AtZ(edge, middle)).ThenBy(edge => edge.A.X).ThenBy(edge => edge.B.X).ToArray();
            if (active.Length % 2 != 0) throw new JsonException("Campus polygon has an odd scan-line intersection count.");
            for (var j = 0; j < active.Length; j += 2)
            {
                var left = active[j];
                var right = active[j + 1];
                var a = new Vector3((float)AtZ(left, lo), y, lo);
                var b = new Vector3((float)AtZ(right, lo), y, lo);
                var c = new Vector3((float)AtZ(right, hi), y, hi);
                var d = new Vector3((float)AtZ(left, hi), y, hi);
                mesh.Quad(a, b, c, d, Vector3.UnitY);
            }
        }
    }
    private static double AtZ((Vector2 A, Vector2 B) edge, double z) =>
        edge.A.X + (z - edge.A.Y) * (edge.B.X - edge.A.X) / (edge.B.Y - edge.A.Y);

    private static void AddBox(MeshBatch mesh, Vector2 min, Vector2 max, float height)
    {
        var a = new Vector3(min.X, 0f, min.Y); var b = new Vector3(max.X, 0f, min.Y);
        var c = new Vector3(max.X, 0f, max.Y); var d = new Vector3(min.X, 0f, max.Y);
        var up = new Vector3(0f, height, 0f);
        mesh.Quad(a, b, b + up, a + up, -Vector3.UnitZ);
        mesh.Quad(c, d, d + up, c + up, Vector3.UnitZ);
        mesh.Quad(d, a, a + up, d + up, -Vector3.UnitX);
        mesh.Quad(b, c, c + up, b + up, Vector3.UnitX);
        mesh.Quad(a + up, b + up, c + up, d + up, Vector3.UnitY);
        mesh.Quad(d, c, b, a, -Vector3.UnitY);
    }
    private static void AddShell(List<Collider> colliders, Vector2 min, Vector2 max, float height)
    {
        var ring = Rectangle(min, max).Outer;
        for (var i = 0; i < ring.Count; i++)
            colliders.Add(new WallCollider(ring[i], ring[(i + 1) % ring.Count], 0.15f, 0f, height, SurfaceTag.Wall) { Foot = FootMat.Concrete });
    }

    private sealed class MeshBatch(string token)
    {
        private readonly ToonRef _material = new(new(new(token), new(token + ".shade")));
        private readonly List<GeometryBundle> _completed = [];
        private readonly List<float> _position = [];
        private readonly List<float> _normal = [];
        private readonly List<int> _index = [];
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            if (_position.Count / 3 + 4 > 60000) Flush();
            var first = _position.Count / 3;
            Vertex(a, normal); Vertex(b, normal); Vertex(c, normal); Vertex(d, normal);
            Triangle(first, first + 1, first + 2, a, b, c);
            Triangle(first, first + 2, first + 3, a, c, d);
        }
        private void Vertex(Vector3 point, Vector3 normal)
        {
            _position.Add(point.X); _position.Add(point.Y); _position.Add(point.Z);
            _normal.Add(normal.X); _normal.Add(normal.Y); _normal.Add(normal.Z);
        }
        private void Triangle(int a, int b, int c, Vector3 va, Vector3 vb, Vector3 vc)
        {
            if (Vector3.Cross(vb - va, vc - va).LengthSquared() <= 1e-12f) return;
            _index.Add(a); _index.Add(b); _index.Add(c);
        }
        private void Flush()
        {
            if (_index.Count > 0) _completed.Add(new(_material, _position.ToArray(), _normal.ToArray(), _index.ToArray()));
            _position.Clear(); _normal.Clear(); _index.Clear();
        }
        public IReadOnlyList<GeometryBundle> Finish() { Flush(); return _completed; }
    }
}
