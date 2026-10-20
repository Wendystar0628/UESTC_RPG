using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Geometry;
using static Campus.Gen.Vegetation.Planting;
using static Campus.Gen.Vegetation.TreePlacement;

namespace Campus.Gen.Vegetation;

/// <summary>Reserves explicit sport-group tree walls and resolves their shared rows.</summary>
internal static class SportTreeWalls
{
    // MAP-QSH-VEGETATION assigns tennis walls to the existing deciduous metasequoia mothers.
    // Height, crown and variation remain the species/TreeForms defaults.
    internal const string SportWallSpecies = "metaseq";
    internal const string SportWallKey = ":wall:";
    internal const float WallGeometryTolerance = 0.01f;

    internal sealed record SportWallGroup(WorldFeature Feature, Vector2 Gate, float GateGap, float Pitch,
        float Offset, float SharedBelow);

    internal sealed record SportWallEdge(SportWallGroup Group, int Index, Vector2 Start, Vector2 Axis,
        Vector2 Normal, float Length, bool HasGate)
    {
        public Vector2 End => Start + Axis * Length;
    }

    /// <summary>Explicit sport groups supply the outline and door; no court regrouping or world-axis envelope.</summary>
    internal static void PlantSportWalls(PlantingSite site, SpeciesTable species, List<PlantedTree> trees)
    {
        foreach (var (_, key, point) in SportWallPoints(site.World))
            Try(site, species.Get(SportWallSpecies), point, key, wash: false, PlantSource.SportWall, trees, crownClear: false);
    }

    internal static List<SportWallEdge> Edges(IWorldModel world)
    {
        var edges = new List<SportWallEdge>();
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == "sportGroup")
                     .OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Geometry is not PolygonGeometry polygon || feature.Props is not { } props ||
                !props.TryGetProperty("members", out var members) || members.ValueKind != JsonValueKind.Array ||
                members.GetArrayLength() == 0) continue;
            var gate = props.GetProperty("gate");
            var ring = polygon.Rings.Outer;
            var group = new SportWallGroup(feature, new(gate[0].GetSingle(), gate[1].GetSingle()),
                props.GetProperty("gateGap").GetSingle(), props.GetProperty("treePitch").GetSingle(),
                props.GetProperty("treeOffset").GetSingle(), props.GetProperty("sharedGapBelow").GetSingle());
            if (group.Pitch <= 0 || group.Offset < 0 || group.GateGap < 0 || group.SharedBelow <= 0)
                throw new InvalidDataException($"sportGroup {feature.Id.Value}: invalid tree wall dimensions.");
            var count = ring.Count > 1 && ring[0] == ring[^1] ? ring.Count - 1 : ring.Count;
            if (count < 3) continue;
            var gateEdge = Enumerable.Range(0, count).MinBy(i =>
                PlanLines.Project(new[] { ring[i], ring[(i + 1) % count] }, group.Gate).Distance);
            var winding = PlanRings.SignedArea(ring) > 0 ? 1f : -1f;
            for (var i = 0; i < count; i++)
            {
                var delta = ring[(i + 1) % count] - ring[i];
                if (delta.LengthSquared() < 1e-6f) continue;
                var axis = Vector2.Normalize(delta);
                edges.Add(new(group, i, ring[i], axis, new(axis.Y * winding, -axis.X * winding), delta.Length(), i == gateEdge));
            }
        }

        return edges;
    }

    // Intersection along the outward normal measures the actual opposing edges, including their slight rotation.
    internal static float Gap(SportWallEdge edge, SportWallEdge other, Vector2 point) =>
        Vector2.Dot(other.Start - point, other.Normal) / Vector2.Dot(edge.Normal, other.Normal);

    internal static bool AtGate(SportWallEdge edge, Vector2 point) => edge.HasGate &&
        MathF.Abs(Vector2.Dot(point - edge.Group.Gate, edge.Axis)) <= edge.Group.GateGap * 0.5f;

}
