using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Sports;

/// <summary>Authored sports identities and frames; base surfaces remain with the ground producer.</summary>
internal static class SportsLayout
{
    internal static IEnumerable<WorldFeature> Courts(IWorldModel world) => world.Features.Values
        .Where(f => f.Kind.Value == "sport" && f.Props.TryText("sport") is "basketball" or "tennis")
        .OrderBy(f => f.Id.Value, StringComparer.Ordinal);

    internal static Vector2 Axis(WorldFeature feature)
    {
        var endpoints = feature.RequiredProps().GetProperty("rectAxis");
        return endpoints[1].Pair() - endpoints[0].Pair();
    }

    internal static IEnumerable<(Vector2 A, Vector2 B)> Edges(IReadOnlyList<Vector2> ring)
    {
        for (var i = 0; i < ring.Count; i++) yield return (ring[i], ring[(i + 1) % ring.Count]);
    }

    internal static (float Low, float High) FenceGround(IWorldModel world, Vector2 a, Vector2 b)
    {
        var normal = PlanGeometry.RightUnitNormal(a, b) * Campus.Gen.Parts.Sport.SportsStyle.FenceGroundBand;
        var heights = new[] { a - normal, a + normal, b - normal, b + normal, (a + b) * 0.5f - normal, (a + b) * 0.5f + normal }
            .Select(p => world.Terrain.HeightAt(p.X, p.Y)).ToArray();
        return (heights.Min(), heights.Max());
    }

    internal static IEnumerable<(WorldFeaturePart Part, PolygonRings Rings)> TrainingParts(IWorldModel world) =>
        world.Feature(CenterEastIds.Sports.Training.Entity).Parts.SelectMany(part =>
            WorldFeatureGeometry.PolygonsOf(WorldFeatureGeometry.FacadeGeometryOf(part)).Select(rings => (part, rings)));
}
