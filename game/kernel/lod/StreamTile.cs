using System.Numerics;
using Campus.Contracts;
using Campus.Kernel.Frame;

namespace Campus.Kernel.Lod;

/// <summary>Coordinates are indices in the selected level's 64 * 2^level metre grid.</summary>
public readonly record struct StreamTile(TileKey Tile, LodLevel Level) : IComparable<StreamTile>
{
    public int Edge => TileGrid.Edge(Level);
    public Vector2 Min => TileGrid.Origin(Tile, Level);
    public Vector2 Max => Min + new Vector2(Edge);
    public static StreamTile Root => new(new(0, 0), LodLevel.L4);
    public int CompareTo(StreamTile other)
    {
        var level = other.Level.CompareTo(Level); // Coarse first; stable tie-breaks across runs.
        if (level != 0) return level;
        var x = Tile.X.CompareTo(other.Tile.X);
        return x != 0 ? x : Tile.Z.CompareTo(other.Tile.Z);
    }
    public StreamTile Parent => Level switch
    {
        LodLevel.L0 or LodLevel.L1 or LodLevel.L2 => new(new(FloorHalf(Tile.X), FloorHalf(Tile.Z)), Level + 1),
        LodLevel.L3 => Root,
        LodLevel.L4 => throw new InvalidOperationException("The map root has no parent."),
        _ => throw new ArgumentOutOfRangeException(nameof(Level))
    };
    public static int FloorHalf(int value) => value >> 1;
}

/// <summary>Central tuning for the first semantic LOD pass; no quality-file reads.</summary>
public sealed record StreamSettings
{
    public float ReplanSeconds { get; init; } = .1f;
    public float FadeSeconds { get; init; } = RenderFade.Seconds;
    public double MountMilliseconds { get; init; } = KernelBudgets.StreamMountMilliseconds;
    public int MaximumPreparations { get; init; } = KernelBudgets.StreamMaximumPreparations;
    public const float DefaultBaseErrorPixels = 1.5f;
    public const float MergeDistanceScale = 1.1f;
    public float BaseErrorPixels { get; init; } = DefaultBaseErrorPixels;
    public float ReferenceFov { get; init; } = 60;
    public float[] Distances { get; init; } = [40, 150, 400, 800];
    public float[] PrefetchSeconds { get; init; } = [.5f, 1.5f, 3];
}

/// <summary>The two speed gates have independent recovery dwell timers.</summary>
public sealed class LodSpeedPolicy
{
    private bool _skipL0, _skipL1;
    private float _l0Recovery, _l1Recovery;
    public LodLevel Minimum { get; private set; }
    public void Reset() { _skipL0 = _skipL1 = false; _l0Recovery = _l1Recovery = 0; Minimum = LodLevel.L0; }
    public void Step(float speed, float delta, bool exempt)
    {
        if (exempt) { Reset(); return; }
        Update(ref _skipL0, ref _l0Recovery, speed, 15, 10, delta);
        Update(ref _skipL1, ref _l1Recovery, speed, 45, 35, delta);
        Minimum = _skipL1 ? LodLevel.L2 : _skipL0 ? LodLevel.L1 : LodLevel.L0;
    }
    private static void Update(ref bool blocked, ref float recovery, float speed, float enter, float leave, float delta)
    {
        if (speed > enter) { blocked = true; recovery = 0; }
        else if (blocked && speed < leave)
        {
            recovery += Math.Max(0, delta);
            if (recovery >= .3f) { blocked = false; recovery = 0; }
        }
        else recovery = 0;
    }
}
