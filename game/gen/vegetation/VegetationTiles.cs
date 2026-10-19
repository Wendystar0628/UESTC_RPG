using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Vegetation;

/// <summary>Conservative tree owner cells from the same authored planting sources, without terrain or geometry generation.</summary>
internal static class VegetationTiles
{
    public static IReadOnlyList<TileKey> Read(BuildContext context)
    {
        var rules = VegetationInputs.Rules(context, VegetationInputs.Species(context));
        if (rules is null) return [];
        var world = context.World;
        var tiles = new HashSet<TileKey>();
        var boundaryMin = world.Boundary.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
        var boundaryMax = world.Boundary.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max);
        foreach (var rule in rules.Roads)
        {
            var reach = MathF.Max(rule.Rows.Max(row => MathF.Abs(row.Offset)), MathF.Abs(rule.Band?.Offset ?? 0f));
            foreach (var road in world.Network.Roads)
            {
                if ((road.Props?.OptionalText("name:zh") ?? road.Props?.OptionalText("name")) != rule.Name) continue;
                var points = road.Line.Points;
                for (var i = 1; i < points.Count; i++)
                    Add(Vector2.Min(points[i - 1], points[i]) - new Vector2(reach), Vector2.Max(points[i - 1], points[i]) + new Vector2(reach));
            }
        }
        var site = new Planting.Site(world, context.WorldSeed);
        foreach (var parcel in site.Green) Add(parcel.Min, parcel.Max);
        foreach (var pit in site.Pits)
        {
            var half = Vector2.Abs(pit.Axis * pit.HalfLength);
            Add(pit.Centre - half, pit.Centre + half);
        }
        foreach (var tree in world.Network.Trees)
        {
            var p = new Vector2(tree.Position.X, tree.Position.Z);
            Add(p, p);
        }
        foreach (var feature in world.Features.Values)
            if (feature.Kind.Value == Planting.DesignTreeKind && feature.Geometry is PointGeometry point)
            {
                var p = new Vector2(point.Position.X, point.Position.Z);
                Add(p, p);
            }
        foreach (var (_, _, p) in Planting.SportWallPoints(world)) Add(p, p);
        return tiles.OrderBy(t => t.X).ThenBy(t => t.Z).ToArray();

        void Add(Vector2 min, Vector2 max)
        {
            min = Vector2.Max(min, boundaryMin);
            max = Vector2.Min(max, boundaryMax);
            if (min.X > max.X || min.Y > max.Y) return;
            var first = TileGrid.At(min);
            var last = TileGrid.At(max);
            for (var z = first.Z; z <= last.Z; z++)
                for (var x = first.X; x <= last.X; x++) tiles.Add(new(x, z));
        }
    }
}
