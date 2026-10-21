using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Gen.World;

namespace Campus.Kernel.Bake;

/// <summary>Canonical terrain blocks for runtime samples and the finite offline query closure.</summary>
internal static class TerrainBlockCoverage
{
    internal static TileKey Block(Vector2 point) => TileGrid.At(point, LodLevel.L2);

    internal static TileKey[] Points(IReadOnlyList<Vector2> points)
    {
        var blocks = new HashSet<TileKey>();
        foreach (var point in points)
        {
            var grid = TerrainSampling.QueryGrid(point);
            for (var z = 0; z < grid.Height; z++)
                for (var x = 0; x < grid.Width; x++)
                    blocks.Add(Block(grid.Origin + new Vector2(x * grid.Step.X, z * grid.Step.Y)));
        }
        return blocks.OrderBy(block => block.X).ThenBy(block => block.Z).ToArray();
    }

    internal static IEnumerable<TileKey> Region(MapRect region)
    {
        if (!VectorMath.Finite(region.Min) || !VectorMath.Finite(region.Max)
            || region.Min.X > region.Max.X || region.Min.Y > region.Max.Y)
            throw new ArgumentOutOfRangeException(nameof(region));
        var edge = TerrainSampling.QueryGrid(region.Max);
        var min = Block(region.Min);
        var max = Block(edge.Origin + new Vector2((edge.Width - 1) * edge.Step.X, (edge.Height - 1) * edge.Step.Y));
        for (var z = min.Z; z <= max.Z; z++)
            for (var x = min.X; x <= max.X; x++) yield return new(x, z);
    }

    internal static MapRect OfflineDomain(IMapContext context)
    {
        var map = context.Descriptor;
        var reach = new Vector2(map.Roam.MaxRadius);
        var min = Vector2.Min(map.DataRect.Min, Vector2.Min(new(map.Bounds.Min.X, map.Bounds.Min.Z), map.Center - reach));
        var max = Vector2.Max(map.DataRect.Max, Vector2.Max(new(map.Bounds.Max.X, map.Bounds.Max.Z), map.Center + reach));
        foreach (var point in MapCatalog.ReviewPoses(map, context.SourceInputs.Entries).Values) Include(point.Position);
        foreach (var spawn in map.Spawns)
            if (spawn.Stand is { } stand) Include(stand.Feet);
        if (map.Title is { } title) Include(title.T0.Position);
        if (context.SourceInputs.Entries.TryGetValue("waypoints.json", out var waypointBytes))
        {
            var waypoints = JsonSerializer.Deserialize<WaypointFile>(waypointBytes.Span, ContractJson.Options)
                ?? throw new InvalidDataException("Waypoint file is null.");
            foreach (var point in waypoints.Waypoints)
                if (point.Stand is { } stand) Include(stand.Feet);
        }
        if (context.SourceInputs.Entries.TryGetValue("atlas/markers.json", out var atlasBytes))
        {
            using var atlas = JsonDocument.Parse(atlasBytes);
            foreach (var marker in atlas.RootElement.GetProperty("markers").EnumerateArray())
            {
                if (!marker.TryGetProperty("teleport", out var value) || value.ValueKind == JsonValueKind.Null) continue;
                var teleport = value.Deserialize<TeleportSpec>(ContractJson.Options)
                    ?? throw new InvalidDataException("Atlas teleport is null.");
                Include(teleport.Fly.Position);
                if (teleport.Stand is { } stand) Include(stand.Feet);
            }
        }
        foreach (var feature in context.World.Features.Keys.OrderBy(id => id.Value, StringComparer.Ordinal))
            if (context.World.Interiors.Get(feature) is { Open: true } interior)
                foreach (var door in interior.Entrances.Where(door => door.Open)) Include(door.Outside);
        // Arrival collision coverage remains required around every reachable or authored endpoint.
        var probe = new Vector2(WalkRules.ArrivalCollisionRadius);
        return new(min - probe, max + probe);

        void Include(Vector3 position)
        {
            var point = new Vector2(position.X, position.Z);
            if (!VectorMath.Finite(point)) throw new InvalidDataException("Authored terrain query position is not finite.");
            min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }
    }
}
