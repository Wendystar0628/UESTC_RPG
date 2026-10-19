using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Vegetation;

public static partial class Planting
{
    // MAP-QSH-VEGETATION assigns tennis walls to the existing deciduous metasequoia mothers.
    // Height, crown and variation remain the species/TreeForms defaults.
    private const string SportWallSpecies = "metaseq";
    private const string SportWallKey = ":wall:";
    private const float WallGeometryTolerance = 0.01f;

    private sealed record SportWallGroup(WorldFeature Feature, Vector2 Gate, float GateGap, float Pitch,
        float Offset, float SharedBelow);

    private sealed record SportWallEdge(SportWallGroup Group, int Index, Vector2 Start, Vector2 Axis,
        Vector2 Normal, float Length, bool HasGate)
    {
        public Vector2 End => Start + Axis * Length;
    }

    /// <summary>Explicit sport groups supply the outline and door; no court regrouping or world-axis envelope.</summary>
    private static void PlantSportWalls(Site site, SpeciesTable species, List<PlantedTree> trees)
    {
        foreach (var (_, key, point) in SportWallPoints(site.World))
            Try(site, species.Get(SportWallSpecies), point, key, wash: false, PlantSource.SportWall, trees, crownClear: false);
    }

    /// <summary>Actual wall trunk candidates shared by planting and bake terrain dependencies.</summary>
    public static IEnumerable<(FeatureId Feature, string Key, Vector2 Position)> SportWallPoints(IWorldModel world)
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
            var winding = PlanGeometry.SignedArea(ring) > 0 ? 1f : -1f;
            for (var i = 0; i < count; i++)
            {
                var delta = ring[(i + 1) % count] - ring[i];
                if (delta.LengthSquared() < 1e-6f) continue;
                var axis = Vector2.Normalize(delta);
                edges.Add(new(group, i, ring[i], axis, new(axis.Y * winding, -axis.X * winding), delta.Length(), i == gateEdge));
            }
        }

        var occupied = new List<Vector2>();
        foreach (var edge in edges)
        {
            var neighbours = new List<(SportWallEdge Edge, float From, float To)>();
            foreach (var other in edges)
            {
                if (other.Group == edge.Group || Vector2.Dot(edge.Normal, other.Normal) > -0.995f) continue;
                var from = MathF.Max(0, MathF.Min(Vector2.Dot(other.Start - edge.Start, edge.Axis), Vector2.Dot(other.End - edge.Start, edge.Axis)));
                var to = MathF.Min(edge.Length, MathF.Max(Vector2.Dot(other.Start - edge.Start, edge.Axis), Vector2.Dot(other.End - edge.Start, edge.Axis)));
                if (to <= from) continue;
                var middle = edge.Start + edge.Axis * ((from + to) * 0.5f);
                var gap = Gap(edge, other, middle);
                if (gap > 0 && gap < MathF.Min(edge.Group.SharedBelow, other.Group.SharedBelow) - WallGeometryTolerance) neighbours.Add((other, from, to));
            }
            var cuts = neighbours.SelectMany(n => new[] { n.From, n.To }).Append(0).Append(edge.Length).Distinct().Order().ToArray();
            for (var span = 0; span < cuts.Length - 1; span++)
            {
                var from = cuts[span];
                var to = cuts[span + 1];
                var middle = (from + to) * 0.5f;
                var neighbour = neighbours.Where(n => middle >= n.From && middle <= n.To)
                    .OrderBy(n => Gap(edge, n.Edge, edge.Start + edge.Axis * middle)).Select(n => n.Edge).FirstOrDefault();
                // One owner generates a shared row. A wide gap keeps each group's own offset row.
                if (neighbour is not null && StringComparer.Ordinal.Compare(edge.Group.Feature.Id.Value, neighbour.Group.Feature.Id.Value) > 0) continue;
                for (var index = (int)MathF.Ceiling(from / edge.Group.Pitch); index * edge.Group.Pitch < to; index++)
                {
                    var distance = index * edge.Group.Pitch;
                    var onEdge = edge.Start + edge.Axis * distance;
                    var offset = neighbour is null ? edge.Group.Offset : Gap(edge, neighbour, onEdge) * 0.5f;
                    var point = onEdge + edge.Normal * offset;
                    if (AtGate(edge, point) || neighbour is not null && AtGate(neighbour, point)) continue;
                    if (!PlanGeometry.Contains(world.Boundary, point) || occupied.Any(p => Vector2.Distance(p, point) < SameTree)) continue;
                    var key = edge.Group.Feature.Id.Value + SportWallKey + edge.Index.ToString(CultureInfo.InvariantCulture) + ":" + index.ToString(CultureInfo.InvariantCulture);
                    occupied.Add(point);
                    yield return (edge.Group.Feature.Id, key, point);
                }
            }
        }
    }

    /// <summary>Largest possible wall crown radius, including existing variant and scale variation.</summary>
    public static float SportWallReach(SpeciesTable species)
    {
        var info = species.Get(SportWallSpecies);
        return Enumerable.Range(0, TreeForms.Variants).Max(variant =>
            TreeForms.Get(info.Form).CrownRadius * TreeForms.HeightOf(info, variant) *
            (TreeScaleMinimum + TreeScaleRange) * TreeForms.CrownScale(variant));
    }

    // Intersection along the outward normal measures the actual opposing edges, including their slight rotation.
    private static float Gap(SportWallEdge edge, SportWallEdge other, Vector2 point) =>
        Vector2.Dot(other.Start - point, other.Normal) / Vector2.Dot(edge.Normal, other.Normal);

    private static bool AtGate(SportWallEdge edge, Vector2 point) => edge.HasGate &&
        MathF.Abs(Vector2.Dot(point - edge.Group.Gate, edge.Axis)) <= edge.Group.GateGap * 0.5f;

    internal static string? SportWallFeature(string key)
    {
        var marker = key.LastIndexOf(SportWallKey, StringComparison.Ordinal);
        return marker < 0 ? null : key[..marker];
    }
}
