using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.World;

namespace Campus.Gen.Sites;

public static partial class FeatureReads
{

    /// <summary>
    /// Reads a non-rendered contourSegment: a two-point polyline with props.parent, optional sourceId and contour="facade".
    /// Endpoints are ordered: follow the parent's outer ring counter-clockwise in (x,z) from the first to the second,
    /// including both matched vertices. This selects the same arc after a ring rotation or reversal, even when it is the longer arc.
    /// The default contour is physical geometry; facade uses the current source facade geometry (as Outers does).
    /// </summary>
    public static Vector2[] ContourSegment(this IWorldModel world, string selectionId)
    {
        if (!world.Features.TryGetValue(new(selectionId), out var selection))
            throw ContourError(selectionId, "Selection is missing from the world model.");
        if (selection.Kind.Value != "contourSegment" || selection.Geometry is not PolylineGeometry { Line.Points.Count: 2 } line)
            throw ContourError(selectionId, "Selection requires kind contourSegment and exactly two ordered polyline endpoints.");
        if (selection.Props is not { ValueKind: JsonValueKind.Object } props)
            throw ContourError(selectionId, "Selection requires props.parent.");
        var parentId = ContourText(props, "parent", selectionId)
            ?? throw ContourError(selectionId, "Selection requires props.parent.");
        if (!world.Features.TryGetValue(new(parentId), out var parent))
            throw ContourError(selectionId, $"Parent {parentId} is missing from the world model.");
        var sourceId = ContourText(props, "sourceId", selectionId);
        var contour = ContourText(props, "contour", selectionId) ?? "physical";
        if (contour is not ("physical" or "facade"))
            throw ContourError(selectionId, "props.contour must be physical or facade.");
        IReadOnlyList<IReadOnlyList<Vector2>> rings;
        if (sourceId is not null)
        {
            var part = parent.Parts.FirstOrDefault(p => p.SourceId == sourceId)
                ?? throw ContourError(selectionId, $"Parent {parentId} has no source part {sourceId}.");
            rings = ContourOuters(contour == "facade" ? WorldFeatureGeometry.FacadeGeometryOf(part) : part.Geometry, selectionId);
        }
        else rings = contour == "facade" ? parent.Outers() : ContourOuters(parent.Geometry, selectionId);

        Vector2[]? result = null;
        foreach (var input in rings)
        {
            IReadOnlyList<Vector2> ring;
            try { ring = PlanGeometry.OpenRing(input); }
            catch (ArgumentException error) { throw ContourError(selectionId, $"Parent {parentId} has an invalid contour: {error.Message}"); }
            var a = Match(ring, line.Line.Points[0], selectionId);
            var b = Match(ring, line.Line.Points[1], selectionId);
            if (a < 0 || b < 0) continue;
            if (a == b) throw ContourError(selectionId, "Both endpoints match the same contour vertex.");
            if (result is not null) throw ContourError(selectionId, "Endpoints match more than one outer ring; specify the source part.");
            var count = (b - a + ring.Count) % ring.Count + 1;
            result = Enumerable.Range(0, count).Select(i => ring[(a + i) % ring.Count]).ToArray();
        }
        return result ?? throw ContourError(selectionId, $"Both endpoints must match vertices on one parent contour within {WorldFeatureGeometry.SelectionEndpointTolerance} m.");
    }

    /// <summary>
    /// Frame on the selected arc's endpoint chord. Origin is the first endpoint unless originAtEnd is true;
    /// +X follows first-to-last unless forward is false. The two choices are independent of the selected CCW arc.
    /// </summary>
    public static PlanFrame ContourFrame(this IWorldModel world, string selectionId, bool originAtEnd = false, bool forward = true)
    {
        var segment = world.ContourSegment(selectionId);
        return PlanFrame.Along(originAtEnd ? segment[^1] : segment[0], forward ? segment[^1] - segment[0] : segment[0] - segment[^1]);
    }

    private static int Match(IReadOnlyList<Vector2> ring, Vector2 endpoint, string selectionId)
    {
        var best = -1;
        var distance = float.PositiveInfinity;
        var tied = false;
        for (var i = 0; i < ring.Count; i++)
        {
            var d = Vector2.DistanceSquared(endpoint, ring[i]);
            if (d < distance) { distance = d; best = i; tied = false; }
            else if (d == distance) tied = true;
        }
        if (tied && distance <= WorldFeatureGeometry.SelectionEndpointTolerance * WorldFeatureGeometry.SelectionEndpointTolerance)
            throw ContourError(selectionId, "An endpoint is equally close to multiple contour vertices.");
        return distance <= WorldFeatureGeometry.SelectionEndpointTolerance * WorldFeatureGeometry.SelectionEndpointTolerance ? best : -1;
    }

    private static IReadOnlyList<IReadOnlyList<Vector2>> ContourOuters(WorldGeometry geometry, string selectionId) => geometry switch
    {
        PolygonGeometry polygon => [polygon.Rings.Outer],
        MultiPolygonGeometry multi => multi.Polygons.Select(p => p.Outer).ToArray(),
        _ => throw ContourError(selectionId, "The selected parent contour is not a polygon."),
    };

    private static string? ContourText(JsonElement props, string key, string selectionId)
    {
        if (!props.TryGetProperty(key, out _)) return null;
        var value = WorldProps.TryText(props, key);
        return !string.IsNullOrWhiteSpace(value) ? value : throw ContourError(selectionId, $"props.{key} must be a nonempty string.");
    }

    private static WorldDataException ContourError(string selectionId, string message) => new("world model", selectionId, message);
}
