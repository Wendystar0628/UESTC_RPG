using System.Numerics;

namespace Campus.Contracts.Vegetation;

/// <summary>Authored grass-area categories; exclusions take precedence when placement is evaluated.</summary>
public enum GrassCategory : byte { Short = 0, Tall = 1, Wildflower = 2, Ornamental = 3, Exclude = 4 }

/// <summary>
/// One grass-area feature in world X/Z, retaining holes and its overlay zone. SetbackMeters is an optional
/// inward clearance from its boundary, or an outward clearance for Exclude. Null leaves the existing placement rule in charge.
/// This is plan input, not a sampled height, placement result or grass prototype.
/// </summary>
public sealed class GrassAreaData
{
    public GrassAreaData(FeatureId id, ZoneId? zone, GrassCategory category,
        IReadOnlyList<PolygonRings> polygons, float? setbackMeters)
    {
        Id = id;
        Zone = zone;
        Category = category;
        Polygons = Array.AsReadOnly(polygons.Select(p => new PolygonRings(
            Array.AsReadOnly(p.Outer.ToArray()),
            Array.AsReadOnly(p.Holes.Select(h => (IReadOnlyList<Vector2>)Array.AsReadOnly(h.ToArray())).ToArray()))).ToArray());
        SetbackMeters = setbackMeters;
    }

    public FeatureId Id { get; }
    public ZoneId? Zone { get; }
    public GrassCategory Category { get; }
    public IReadOnlyList<PolygonRings> Polygons { get; }
    public float? SetbackMeters { get; }
}

/// <summary>Frozen grass scopes from one prepared map context. Contains no generated grass instances or colour field.</summary>
public sealed class GrassRuntimeData(MapId map, IReadOnlyList<GrassAreaData> areas)
{
    public MapId Map { get; } = map;
    public IReadOnlyList<GrassAreaData> Areas { get; } = Array.AsReadOnly(areas.ToArray());
}

/// <summary>One occupied grass cell, in first-record order; Start/Count address the index's member channel.</summary>
public readonly record struct GrassPoolCell(int X, int Z, int Start, int Count, Box3 Bounds);

/// <summary>Frozen grass selection directory and its quality-independent capacity envelope.</summary>
public sealed class GrassPoolIndex
{
    public const float CellSize = 8f;
    public const float Reach = 24f;

    public GrassPoolIndex(IReadOnlyList<GrassPoolCell> cells, ReadOnlyMemory<int> members, int capacity)
        : this(cells, members.ToArray(), capacity) { }

    private GrassPoolIndex(IReadOnlyList<GrassPoolCell> cells, int[] members, int capacity)
    {
        Cells = Array.AsReadOnly(cells.ToArray());
        Members = members;
        Capacity = capacity;
    }

    /// <summary>Takes exclusive ownership of members; the small cell directory still takes a snapshot.</summary>
    public static GrassPoolIndex TakeOwnership(IReadOnlyList<GrassPoolCell> cells, int[] members, int capacity)
    {
        ArgumentNullException.ThrowIfNull(members);
        return new(cells, members, capacity);
    }

    public IReadOnlyList<GrassPoolCell> Cells { get; }
    public ReadOnlyMemory<int> Members { get; }
    public int Capacity { get; }
}
