namespace Campus.Contracts.Water;

/// <summary>Shared outdoor pool dimensions and the authored two-band depth profile.</summary>
public static class WaterPoolRules
{
    public const float RimDrop = 0.15f;
    public const float ShallowDepth = 1.2f;
    public const float DeepDepth = 1.8f;
    public const float ShallowFraction = 1f / 3f;
    public const float LaneWidth = 2.5f;
    public const float LineWidth = 0.25f;
    public const float LaneEndBarLength = 1f;
    public const float LaneEndInset = 2f;

    public static float DepthAt(float along, float length) =>
        along < length * ShallowFraction ? ShallowDepth : DeepDepth;

    public static int LaneCount(float width) => Math.Max(1, (int)MathF.Floor(width / LaneWidth));

    public static float LaneCenter(int lane, int count) => (lane - (count - 1) * 0.5f) * LaneWidth;
}
