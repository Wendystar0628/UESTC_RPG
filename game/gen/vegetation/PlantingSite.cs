using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.World;
using static Campus.Gen.Vegetation.Planting;

namespace Campus.Gen.Vegetation;

internal sealed record PlantingParcel(string Id, PolygonRings Rings, Vector2 Min, Vector2 Max, float Area);

/// <summary>
/// How grass meets a piece of pavement (WLD-VEGETATION): a walk or path under 3 m wide takes a soft edge the grass may
/// creep over; steps keep the 1 m setback of an entrance; every other pavement (plazas, wide walks) keeps a clean straight edge.
/// </summary>
internal enum PlantingPaveEdge : byte { Clean, Soft, Steps }

/// <summary>One piece of hard pavement: an area, or a centre line with its half width. Min and Max box it (without the clearance).</summary>
internal sealed record PlantingPave(PolygonRings? Rings, IReadOnlyList<Vector2>? Line, float Half, Vector2 Min, Vector2 Max, PlantingPaveEdge Edge);

/// <summary>
/// A tree pit: an area (a square pit or a planting strip; Axis along its long side, HalfLength and HalfWidth its half
/// extents), or a disc round Centre (a round bed). Key is the feature ID, with the polygon's index for a multi-polygon.
/// PerPit and Species come from the feature's props; SoilAbove is the soil surface above the ground (m).
/// </summary>
internal sealed record PlantingPit(string Key, PolygonRings? Rings, Vector2 Centre, float Radius, Vector2 Axis, float HalfLength, float HalfWidth,
    int PerPit, string? Species, float SoilAbove)
{
    public bool Holds(Vector2 p) => Rings is { } rings ? PlanGeometry.Contains(rings, p) : Vector2.Distance(p, Centre) <= Radius;
}

/// <summary>The world as the planter sees it: blocking outlines and roads in a coarse grid, green parcels, occupied crowns.</summary>
internal sealed class PlantingSite
{
    private const float Cell = 32f;
    private readonly SpatialGrid<PlantingParcel> _blocking = new(Cell);
    private readonly SpatialGrid<NetworkLine> _roads = new(Cell);
    private readonly SpatialGrid<PlantingPave> _pavement = new(Cell);
    private readonly List<PlantingPit> _pits = [];
    private readonly SpatialGrid<(Vector2 P, float R)> _occupied = new(Cell);
    private readonly float _windMin;
    private readonly float _windSpan;

    public PlantingSite(IWorldModel world, uint seed)
    {
        World = world;
        Seed = seed;
        var green = new List<PlantingParcel>();
        // The effective network (WP-08) publishes a design walk as a network line under its feature's ID and keeps the feature's
        // polyline: line and polyline are one walk. The network is the effective source, so the walk is filed once, as the line.
        var published = world.Network.Roads.Select(r => r.Id.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            foreach (var kind in Blocking)
                foreach (var rings in Parcels(feature, kind))
                    _blocking.AddBox(rings.Min, rings.Max, rings);
            foreach (var rings in Parcels(feature, "green")) green.Add(rings);
            // The effective world's hard pavement: the road areas and design walks of the features (the network's own
            // centre lines are roads above), and the tree pits that may stand on it.
            if (!(IsWalk(feature) && published.Contains(feature.Id.Value)))
                foreach (var (props, geometry) in RoadSources(feature)) IndexPavement(props, geometry);
            if (feature.Kind.Value == TreePitKind) AddPits(feature);
        }
        Green = green;
        foreach (var road in world.Network.Roads.OrderBy(r => r.Id.Value, StringComparer.Ordinal))
        {
            if (road.Line.Points.Count < 2) continue;
            // A design walk is pavement, not a road: it takes the trunks that stand on it (tree pits excepted) and not the ones
            // beside it, and a tree on it still counts as planted. Only the walk's own line is read, at the network's width.
            if (world.Features.TryGetValue(road.Id, out var walk) && IsWalk(walk) && road.Props?.OptionalText("highway") is { } type && Paved.Contains(type))
            {
                IndexPavement(road.Props, new PolylineGeometry(road.Line), road.Width);
                continue;
            }
            var min = road.Line.Points.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - new Vector2(road.Width * 0.5f);
            var max = road.Line.Points.Aggregate(new Vector2(float.MinValue), Vector2.Max) + new Vector2(road.Width * 0.5f);
            _roads.AddBox(min, max, road);
        }
        var outer = world.Boundary.Outer;
        _windMin = outer.Count > 0 ? outer.Min(p => p.X) : -1000f;
        _windSpan = MathF.Max(1f, (outer.Count > 0 ? outer.Max(p => p.X) : 1000f) - _windMin);
    }

    public IWorldModel World { get; }
    public uint Seed { get; }
    public List<PlantingParcel> Green { get; }

    /// <summary>The trees that gave way to pavement so far, in planting order.</summary>
    public List<AvoidedTree> Avoided { get; } = [];

    /// <summary>The tree pits of the overlays, in feature-ID order.</summary>
    public IReadOnlyList<PlantingPit> Pits => _pits;

    /// <summary>WLD-VEGETATION z: 0 at the west edge of the campus, 1 at the east edge.</summary>
    public float WindPos(float x) => Math.Clamp((x - _windMin) / _windSpan, 0f, 1f);

    public bool Blocked(Vector2 p, float radius)
    {
        foreach (var parcel in _blocking.In(p))
        {
            if (p.X < parcel.Min.X - radius || p.X > parcel.Max.X + radius || p.Y < parcel.Min.Y - radius || p.Y > parcel.Max.Y + radius) continue;
            if (PlanGeometry.Contains(parcel.Rings, p) || Edge(parcel.Rings, p) < radius) return true;
        }
        return false;
    }

    public bool OnRoad(Vector2 p, float clearance)
    {
        foreach (var road in _roads.In(p))
        {
            var r = road.Width * 0.5f + clearance;
            if (PlanLines.Project(road.Line.Points, p).Distance < r) return true;
        }
        return false;
    }

    /// <summary>
    /// <see cref="OnRoad(Vector2, float)"/> with a clearance per kind of network line: carriageways (kerbed), walks and paths
    /// under <see cref="GrassField.SoftEdgeWidth"/> (soft, may be negative), steps, and every other paved line (clean).
    /// </summary>
    public bool OnRoad(Vector2 p, float carriageway, float clean, float soft, float steps)
    {
        foreach (var road in _roads.In(p))
        {
            var type = road.Props?.OptionalText("highway");
            var clearance = type is null || !Paved.Contains(type) ? carriageway
                : type == "steps" ? steps : road.Width < GrassField.SoftEdgeWidth ? soft : clean;
            if (PlanLines.Project(road.Line.Points, p).Distance < road.Width * 0.5f + clearance) return true;
        }
        return false;
    }

    /// <summary>True when a trunk at <paramref name="p"/> stands on hard pavement or within <see cref="TrunkClearance"/> of it, and not in a tree pit.</summary>
    public bool OnPavement(Vector2 p) => !InPit(p) && IsPaved(p, TrunkClearance);

    public bool InPit(Vector2 p) => _pits.Any(pit => pit.Holds(p));

    /// <summary>True when <paramref name="p"/> lies on hard pavement or within <paramref name="clearance"/> of it (tree pits included).</summary>
    public bool IsPaved(Vector2 p, float clearance) => IsPaved(p, clearance, clearance, clearance);

    /// <summary>
    /// <see cref="IsPaved(Vector2, float)"/> with a clearance per kind of edge (<see cref="PlantingPaveEdge"/>); a soft clearance may be
    /// negative (a centre line's distance below its half width), so grass can reach over the edge of a narrow walk.
    /// </summary>
    public bool IsPaved(Vector2 p, float clean, float soft, float steps)
    {
        foreach (var pave in _pavement.In(p))
        {
            var clearance = pave.Edge switch { PlantingPaveEdge.Soft => soft, PlantingPaveEdge.Steps => steps, _ => clean };
            if (pave.Rings is { } rings)
            {
                if (p.X < pave.Min.X - clearance || p.X > pave.Max.X + clearance ||
                    p.Y < pave.Min.Y - clearance || p.Y > pave.Max.Y + clearance) continue;
                if (PlanGeometry.Contains(rings, p) || Edge(rings, p) < clearance) return true;
            }
            else
            {
                var r = pave.Half + clearance;
                if (PlanLines.Project(pave.Line!, p).Distance < r) return true;
            }
        }
        return false;
    }

    public bool Spaced(Vector2 p, float radius) => !_occupied.AnyAround(p, o => Vector2.Distance(p, o.P) < radius + o.R + CrownGap);

    public void Occupy(Vector2 p, float radius) => _occupied.AddPoint(p, (p, radius));

    /// <summary>Plan envelope of the features named <paramref name="name"/> (all parts), or null when the map has none.</summary>
    public (Vector2 Min, Vector2 Max)? Envelope(string name)
    {
        var boxes = World.Features.Values.Where(f => f.Name == name)
            .Select(f => (new Vector2(f.Bounds.Min.X, f.Bounds.Min.Z), new Vector2(f.Bounds.Max.X, f.Bounds.Max.Z))).ToList();
        if (boxes.Count == 0) return null;
        return (boxes.Aggregate(new Vector2(float.MaxValue), (a, b) => Vector2.Min(a, b.Item1)),
            boxes.Aggregate(new Vector2(float.MinValue), (a, b) => Vector2.Max(a, b.Item2)));
    }

    /// <summary>The road sources of a feature: its parts of kind road, else its own geometry; each with the props that carry the road type.</summary>
    private static IEnumerable<(JsonElement? Props, WorldGeometry Geometry)> RoadSources(WorldFeature feature)
    {
        if (feature.Parts.Count == 0)
        {
            if (feature.Kind.Value == "road") yield return (feature.Props, feature.Geometry);
            yield break;
        }
        foreach (var part in Parts(feature, "road")) yield return (part.Props ?? feature.Props, part.Geometry);
    }

    /// <summary>A feature that is just a polyline of its own (a design walk): the form in which the network line of its ID is also filed.</summary>
    private static bool IsWalk(WorldFeature feature) => feature.Parts.Count == 0 && feature.Geometry is PolylineGeometry;

    private void IndexPavement(JsonElement? props, WorldGeometry geometry, float width = 0f)
    {
        var type = props?.OptionalText("highway");
        if (type is null || !Paved.Contains(type)) return;
        var steps = type == "steps" ? PlantingPaveEdge.Steps : PlantingPaveEdge.Clean;
        switch (geometry)
        {
            case PolygonGeometry polygon:
                IndexPavement(polygon.Rings, steps);
                break;
            case MultiPolygonGeometry multi:
                foreach (var rings in multi.Polygons) IndexPavement(rings, steps);
                break;
            case PolylineGeometry { Line.Points: { Count: >= 2 } points }:
                var full = width > 0f ? width : PavedWidth(props, type);
                var half = full * 0.5f;
                var edge = steps == PlantingPaveEdge.Steps ? steps : full < GrassField.SoftEdgeWidth ? PlantingPaveEdge.Soft : PlantingPaveEdge.Clean;
                var min = points.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - new Vector2(half);
                var max = points.Aggregate(new Vector2(float.MinValue), Vector2.Max) + new Vector2(half);
                _pavement.AddBox(min, max, new PlantingPave(null, points, half, min, max, edge));
                break;
        }
    }

    private void IndexPavement(PolygonRings rings, PlantingPaveEdge edge)
    {
        if (rings.Outer.Count < 3) return;
        var min = rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
        var max = rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max);
        _pavement.AddBox(min, max, new PlantingPave(rings, null, 0f, min, max, edge));
    }

    /// <summary>Width of a paved line as WP-15 paves it (GroundClassifier): its own width, else the type's default.</summary>
    private static float PavedWidth(JsonElement? props, string type) =>
        props?.OptionalNumber("width") is { } width and > 0f ? width : type switch
        {
            "path" => 2f,
            "cycleway" => 3f,
            "pedestrian" => 5f,
            _ => 2.5f
        };

    /// <summary>A tree pit feature (design addition): square pits are areas; a round bed is a point with a radius, else half its size.</summary>
    private void AddPits(WorldFeature feature)
    {
        var perPit = feature.Props?.OptionalNumber("perPit") is { } n and >= 0f ? (int)MathF.Round(n) : 1;
        if (feature.Props?.OptionalText("plant") is { } plant && plant != "tree") perPit = 0;
        var species = feature.Props?.OptionalText("species");
        var soil = feature.Props?.OptionalNumber("soilAbove") is { } s and >= 0f ? s : 0.3f;
        var id = feature.Id.Value;
        switch (feature.Geometry)
        {
            case PolygonGeometry polygon:
                AddPit(id, polygon.Rings);
                break;
            case MultiPolygonGeometry multi:
                for (var i = 0; i < multi.Polygons.Count; i++) AddPit(id + ":" + i.ToString(CultureInfo.InvariantCulture), multi.Polygons[i]);
                break;
            case PointGeometry point:
                var radius = feature.Props?.OptionalNumber("radius") is { } r and > 0f ? r : feature.Props?.OptionalNumber("size") is { } size and > 0f ? size * 0.5f : 0.9f;
                _pits.Add(new(id, null, new Vector2(point.Position.X, point.Position.Z), radius, Vector2.UnitX, 0f, 0f, perPit, species, soil));
                break;
        }

        void AddPit(string key, PolygonRings rings)
        {
            var outer = rings.Outer;
            if (outer.Count < 3) return;
            // The long side gives the axis (pits are squares or strips); the extents are measured across it.
            var e0 = outer[1] - outer[0];
            var e1 = outer[2] - outer[1];
            var axis = Vector2.Normalize(e0.LengthSquared() >= e1.LengthSquared() ? e0 : e1);
            var across = new Vector2(-axis.Y, axis.X);
            var centre = PlanGeometry.Centroid(outer);
            float a0 = float.MaxValue, a1 = float.MinValue, b0 = float.MaxValue, b1 = float.MinValue;
            foreach (var q in outer)
            {
                var d = q - centre;
                (a0, a1) = (MathF.Min(a0, Vector2.Dot(d, axis)), MathF.Max(a1, Vector2.Dot(d, axis)));
                (b0, b1) = (MathF.Min(b0, Vector2.Dot(d, across)), MathF.Max(b1, Vector2.Dot(d, across)));
            }
            _pits.Add(new(key, rings, centre, 0f, axis, (a1 - a0) * 0.5f, (b1 - b0) * 0.5f, perPit, species, soil));
        }
    }

    private static IEnumerable<PlantingParcel> Parcels(WorldFeature feature, string kind)
    {
        foreach (var part in Parts(feature, kind))
        {
            var polygons = WorldFeatureGeometry.PolygonsOf(WorldFeatureGeometry.FacadeGeometryOf(part));
            foreach (var (rings, index) in polygons.Select((rings, index) => (rings, index)))
            {
                if (rings.Outer.Count < 3) continue;
                var min = rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
                var max = rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max);
                var area = MathF.Abs((float)PlanGeometry.SignedArea(rings.Outer)) - rings.Holes.Sum(h => MathF.Abs((float)PlanGeometry.SignedArea(h)));
                var id = feature.Id.Value + ":" + part.SourceId + ":" + index.ToString(CultureInfo.InvariantCulture);
                yield return new PlantingParcel(id, rings, min, max, area);
            }
        }
    }

    /// <summary>The parts of a feature of one kind (a part's own kind wins over its feature's), as the world model groups them.</summary>
    public static IEnumerable<WorldFeaturePart> Parts(WorldFeature feature, string kind)
    {
        if (feature.Parts.Count == 0)
        {
            if (feature.Kind.Value == kind && feature.Geometry is not (PointGeometry or PolylineGeometry))
                yield return new("", feature.Geometry, feature.Height, feature.Storeys);
            yield break;
        }
        foreach (var part in feature.Parts)
        {
            var partKind = part.Props?.OptionalText("kind") ?? feature.Kind.Value;
            if (partKind == kind) yield return part;
        }
    }
    internal static float Edge(PolygonRings rings, Vector2 p) =>
        rings.Holes.Aggregate(PlanGeometry.DistanceToRing(p, rings.Outer), (d, h) => MathF.Min(d, PlanGeometry.DistanceToRing(p, h)));
}
