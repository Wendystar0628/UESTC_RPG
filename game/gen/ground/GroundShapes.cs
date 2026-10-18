using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Ground;

/// <summary>Source surface classes in field order. Each class gets one signed distance field per tile.</summary>
internal enum SurfaceKind : byte { Water, Carriageway, Parking, Plaza, Walk, Court, Turf, Track, Building, Pit }

/// <summary>
/// One tree pit or planting strip as authored (<c>kind</c> tree-pit): the outline is the outer face of its stone frame.
/// <c>kerb</c> is the frame's thickness. <c>height</c> and <c>soilAbove</c> are the authored frame top and soil top; the
/// ground shows them only when <see cref="PitSpec.UseAuthoredHeights"/> is on, otherwise it draws the design heights of
/// <see cref="PitSpec"/>. Null means "not given".
/// </summary>
internal sealed record PitProfile(float? Rim, float? Soil, float? Kerb)
{
    /// <summary>
    /// Resolved (frame top, soil top, frame thickness) above the carriageway level. <paramref name="compact"/> is a
    /// single pit (a box), as against a long planting strip: they have different design heights.
    /// </summary>
    public (float Rim, float Soil, float Kerb) Resolve(GroundSection section, bool compact)
    {
        var spec = section.Pits;
        var datum = spec.Datum ?? section.PavingY;
        var rim = spec.UseAuthoredHeights && Rim is { } given
            ? datum + given
            : section.PavingY + (compact ? spec.PitRimAbovePaving : spec.StripRimAbovePaving);
        var soil = spec.UseAuthoredHeights && Soil is { } s ? datum + s : rim - spec.SoilBelowRim;
        soil = MathF.Min(soil, rim - spec.MinSoilBelowRim);
        var kerb = Math.Clamp(Kerb ?? spec.DefaultKerb, spec.MinKerb, spec.MaxKerb);
        return (rim, soil, kerb);
    }
}

/// <summary>One source shape: a polygon with holes, or a centre line with a half width (a chain of capsules).</summary>
internal sealed class GroundShape
{
    public required SurfaceKind Kind { get; init; }
    public required FeatureId Id { get; init; }
    /// <summary>Original part identity, when the source was a feature part. Set before publishing the layout.</summary>
    public string? PartSourceId { get; set; }
    /// <summary>Original centre-line order, not an inferred polygon axis or a selected paving phase.</summary>
    public IReadOnlyList<Vector2>? DirectionPath { get; set; }
    public Vector2[][]? Rings { get; init; }
    public Vector2[]? Line { get; init; }
    public float HalfWidth { get; init; }
    public required Vector2 Min { get; init; }
    public required Vector2 Max { get; init; }
    /// <summary>Turf only: the record names a sport (a football pitch), so its lines are drawn; an untagged pitch is plain grass.</summary>
    public bool Marked { get; init; }
    /// <summary>Pit only: the frame and soil heights of the pit.</summary>
    public PitProfile? Pit { get; init; }

    /// <summary>Pit only: (frame top, soil top, frame thickness) above the carriageway level, for this outline.</summary>
    public (float Rim, float Soil, float Kerb) PitHeights(GroundSection section) =>
        Pit!.Resolve(section, Max.X - Min.X <= section.Pits.CompactSide && Max.Y - Min.Y <= section.Pits.CompactSide);
}

/// <summary>A centre line of a carriageway or footpath from the road network; drives markings.</summary>
internal sealed record RoadLine(FeatureId Id, Vector2[] Points, float Width, bool Carriageway, string Type);

internal enum GroundFaceRole : byte { Top, Vertical, Skirt }

/// <summary>CPU input at the mesher/batching boundary. Raw sources are queried at the face's actual position.</summary>
internal readonly record struct GroundFaceInput(GroundSurface Surface, GroundFaceRole Role, GroundSourceContext Context)
{
    public IEnumerable<GroundShape> SourcesAt(Vector2 position) => Context.At(Surface, position);

    /// <summary>
    /// Paving frame of a granite plaza top or a paver walk top at this position, or null. Only the paved body is
    /// patterned (not curbs, edging, vertical faces, skirts or borders). Plazas: the first bound source covering the
    /// point (stable source order) gives the frame; unbound plazas overlapping it are laid in the same grid, so the field
    /// has no seam along the triangulation. Walks: where several overlap (junctions), the one the point lies deepest in.
    /// </summary>
    public PavingFrame? PavingAt(Vector2 position)
    {
        if (Role != GroundFaceRole.Top) return null;
        var paving = Context.Paving;
        if (Surface == GroundSurface.Granite && paving.HasPlazas)
        {
            foreach (var shape in SourcesAt(position))
                if (paving.FrameOf(shape) is { } frame) return frame;
            return null;
        }
        if (Surface != GroundSurface.Paver || !paving.HasWalks) return null;
        GroundShape? deepest = null;
        var depth = float.MaxValue;
        foreach (var shape in SourcesAt(position))
        {
            var d = Sdf.Shape(shape, position, 64f);
            if (d < depth) (deepest, depth) = (shape, d);
        }
        return deepest is null ? null : paving.WalkFrameAt(deepest, position);
    }
}

/// <summary>
/// Read-only view of the existing tile candidates. A query can yield zero or several sources; neither is a unique
/// authored material/direction. T03 resolves its binding before constructing a profile or metre UVs.
/// </summary>
internal sealed class GroundSourceContext(IReadOnlyList<GroundShape> candidates, GroundSection section, float tolerance,
    GroundPaving? paving = null)
{
    public GroundPaving Paving { get; } = paving ?? GroundPaving.Empty;

    public IEnumerable<GroundShape> At(GroundSurface surface, Vector2 position)
    {
        var margin = surface is GroundSurface.Curb or GroundSurface.Bevel ? section.CurbWidth : 0f;
        margin += tolerance;
        foreach (var shape in candidates)
        {
            if (!Matches(surface, shape.Kind) || position.X < shape.Min.X - margin || position.X > shape.Max.X + margin ||
                position.Y < shape.Min.Y - margin || position.Y > shape.Max.Y + margin) continue;
            if (Sdf.Shape(shape, position, margin + 1f) <= margin) yield return shape;
        }
    }

    private static bool Matches(GroundSurface surface, SurfaceKind kind) => surface switch
    {
        GroundSurface.Granite or GroundSurface.PlazaBorder => kind == SurfaceKind.Plaza,
        GroundSurface.Paver => kind == SurfaceKind.Walk,
        GroundSurface.Asphalt => kind == SurfaceKind.Carriageway,
        GroundSurface.Parking => kind == SurfaceKind.Parking,
        GroundSurface.Curb or GroundSurface.Bevel => kind is SurfaceKind.Carriageway or SurfaceKind.Parking,
        GroundSurface.Edging => kind is SurfaceKind.Plaza or SurfaceKind.Walk or SurfaceKind.Track or SurfaceKind.Turf or SurfaceKind.Court,
        GroundSurface.Track => kind == SurfaceKind.Track,
        GroundSurface.Turf => kind == SurfaceKind.Turf,
        GroundSurface.Court => kind == SurfaceKind.Court,
        GroundSurface.PitRim or GroundSurface.PitBevel or GroundSurface.PitSoil => kind == SurfaceKind.Pit,
        GroundSurface.Water => kind == SurfaceKind.Water,
        GroundSurface.Grass or GroundSurface.GrassLight or GroundSurface.GrassDark => false,
        _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown ground surface.")
    };
}

/// <summary>Maps world-model kinds and OSM-style road types to ground surface classes.</summary>
internal static class GroundClassifier
{
    // Default widths when the data has none (same table as the dataset builder that produced campus.json).
    private static float DefaultWidth(string type) => type switch
    {
        "footway" => 2.5f,
        "path" => 2f,
        "steps" => 2.5f,
        "cycleway" => 3f,
        "service" => 5f,
        "residential" => 6f,
        "tertiary" => 8f,
        "secondary" => 10f,
        "pedestrian" => 5f,
        _ => 4f
    };

    public static bool IsCarriageway(string type) => type switch
    {
        "service" or "residential" or "tertiary" or "secondary" or "primary" or "unclassified" or "living_street" or
            "construction" or "road" or "trunk" => true,
        _ => false
    };

    /// <summary>Surface class of a road with the given OSM highway type.</summary>
    public static SurfaceKind RoadKind(string type) => type switch
    {
        "pedestrian" => SurfaceKind.Plaza,
        "footway" or "path" or "cycleway" or "steps" or "track" or "bridleway" or "corridor" => SurfaceKind.Walk,
        _ => IsCarriageway(type) ? SurfaceKind.Carriageway : SurfaceKind.Walk
    };

    public static string RoadType(JsonElement? props) =>
        WorldProps.TryText(props, "roadType") ?? WorldProps.TryText(props, "highway") ?? "";

    /// <summary>
    /// Surface class of one source record from its kind and its own props; null for kinds that leave the lawn untouched
    /// (POIs, green areas, the frame of a stadium). Buildings return <see cref="SurfaceKind.Building"/>: a footprint, not
    /// paving. <paramref name="entity"/> is the owning entity's record, which carries the OSM tags (<c>sourceTags</c>)
    /// that a part's own record does not.
    /// </summary>
    public static SurfaceKind? KindOf(string kind, JsonElement? props, JsonElement? entity = null)
    {
        switch (kind)
        {
            case "road":
                return RoadKind(RoadType(props));
            case "parking":
                return SurfaceKind.Parking;
            case "water":
                return SurfaceKind.Water;
            case "building":
                return SurfaceKind.Building;
            case "sport":
                return WorldProps.TryText(props, "sport") switch
                {
                    "running" => SurfaceKind.Track,
                    "athletics" when Leisure(props, entity) == "track" => SurfaceKind.Track,
                    "soccer" => SurfaceKind.Turf,
                    "swimming" => SurfaceKind.Water,
                    null or "" => UntaggedSport(Leisure(props, entity)),
                    _ => SurfaceKind.Court
                };
            default:
                return null;
        }
    }

    /// <summary>
    /// A sport record without a sport tag is read from its leisure tag: a track is a running track, and a stadium or a
    /// sports centre only encloses the pitches, tracks and pools mapped inside it, so it has no surface of its own
    /// (null: the lawn or whatever lies beneath shows). A pitch that names no sport is open turf, not a court: a court
    /// needs a sport to say what is marked on it. Anything else untagged stays a court.
    /// </summary>
    private static SurfaceKind? UntaggedSport(string? leisure) => leisure switch
    {
        "track" => SurfaceKind.Track,
        "stadium" or "sports_centre" => null,
        "pitch" => SurfaceKind.Turf,
        _ => SurfaceKind.Court
    };

    private static string? Leisure(JsonElement? props, JsonElement? entity) =>
        WorldProps.TryText(props, "leisure") ?? WorldProps.TryText(props, "sourceTags", "leisure") ?? WorldProps.TryText(entity, "sourceTags", "leisure");

    public static float Width(JsonElement? props, string type, float given) =>
        given > 0f && float.IsFinite(given) ? given : WorldProps.TryNumber(props, "width") is { } w && w > 0f ? w : DefaultWidth(type);

}
