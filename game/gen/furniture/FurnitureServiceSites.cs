using System.Text.Json;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

internal readonly record struct ServiceSite(string Label, Vector2 At, PolygonRings? Outside, Vector2? Front, bool Open = false);

/// <summary>Selects stable gate and building-frontage service candidates.</summary>
internal static class FurnitureServiceSites
{
    /// <summary>The dormitory cluster gates (WP-20's <c>clusterGate</c> features), the open ones first, then by ID; each with its cluster's outline.</summary>
    internal static List<ServiceSite> Gates(IWorldModel world)
    {
        var gates = new List<(bool Open, ServiceSite Site)>();
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == "clusterGate").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Geometry is not PointGeometry point) continue;
            var open = feature.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("open", out var o) && o.ValueKind == JsonValueKind.True;
            PolygonRings? outline = null;
            if (WorldProps.TryText(feature.Props, "cluster") is { } id && world.Features.TryGetValue(new(id), out var cluster))
                outline = cluster.Geometry switch { PolygonGeometry polygon => polygon.Rings, MultiPolygonGeometry { Polygons.Count: > 0 } multi => multi.Polygons[0], _ => null };
            if (outline is null) continue;
            gates.Add((open, new(feature.Id.Value, new(point.Position.X, point.Position.Z), outline, null, open)));
        }
        return gates.OrderBy(g => g.Open ? 0 : 1).Select(g => g.Site).ToList();
    }

    /// <summary>
    /// The buildings of <paramref name="categories"/> in ID order (one site a building): with <paramref name="door"/>, only those with an
    /// open entrance (the first by ID), the site in front of it; otherwise the first open entrance or, without one, the outline point nearest
    /// the network (the side the building is reached from).
    /// </summary>
    internal static IEnumerable<ServiceSite> Buildings(StreetSite site, IReadOnlyList<string> categories, bool door)
    {
        var world = site.World;
        var done = new HashSet<FeatureId>();
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == "building").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (!categories.Contains(WorldProps.TryText(feature.Props, "category") ?? "")) continue;
            var entry = world.Interiors.Get(feature.Id);
            if (entry is not null && !done.Add(entry.Feature)) continue;
            var open = entry?.Entrances.Where(e => e.Open).OrderBy(e => e.Id, StringComparer.Ordinal).FirstOrDefault();
            if (open is not null)
            {
                var plan = new Vector2(open.Center.X, open.Center.Z);
                var facing = Angles.Heading(open.YawDeg);
                yield return new(feature.Id.Value, plan + facing * (door ? (S.BayNear + S.BayFar) * 0.5f : S.BayNear), null, door ? plan : null);
                continue;
            }
            if (door || Frontage(site, feature) is not { } front) continue;
            yield return new(feature.Id.Value, front, null, null);
        }
    }

    /// <summary>The point of a building's outline (every 2 m) nearest a carriageway or a strollable walk, within 30 m; null when none is.</summary>
    private static Vector2? Frontage(StreetSite site, WorldFeature feature)
    {
        IReadOnlyList<PolygonRings> polygons = feature.Geometry switch { PolygonGeometry p => [p.Rings], MultiPolygonGeometry m => m.Polygons, _ => [] };
        if (polygons.Count == 0) return null;
        var min = new Vector2(feature.Bounds.Min.X, feature.Bounds.Min.Z) - new Vector2(30f);
        var max = new Vector2(feature.Bounds.Max.X, feature.Bounds.Max.Z) + new Vector2(30f);
        var lines = site.World.Network.Roads.Where(l => (StreetNetwork.Carriageway(l) || StreetNetwork.Strollable(l)) &&
            l.Line.Points.Any(q => q.X >= min.X && q.X <= max.X && q.Y >= min.Y && q.Y <= max.Y)).OrderBy(l => l.Id.Value, StringComparer.Ordinal).ToList();
        (float D, Vector2 P)? best = null;
        foreach (var rings in polygons)
        {
            var ring = rings.Outer;
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % ring.Count];
                var steps = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(a, b) / 2f));
                for (var k = 0; k < steps; k++)
                {
                    var q = Vector2.Lerp(a, b, (k + 0.5f) / steps);
                    foreach (var line in lines)
                    {
                        var d = MathF.Sqrt(PlanLines.PolylineDistanceSquared(line.Line.Points, q)) - StreetNetwork.HalfWidth(line);
                        if (d <= 30f && (best is null || d < best.Value.D)) best = (d, q);
                    }
                }
            }
        }
        return best?.P;
    }
}
