namespace Campus.Contracts;

/// <summary>Surface lifts above ITerrain.HeightAt/PadY, in metres; these are not absolute elevations.</summary>
public static class GroundLevels
{
    public const float Paving = .15f;
    public const float Lawn = .10f;
    public const float Decal = .004f;
}
