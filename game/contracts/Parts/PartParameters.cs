using Campus.Contracts.Audio;

namespace Campus.Contracts.Parts;

/// <summary>Metres. Origin is the footprint centre at ground level; Y is up and depth runs along Z.</summary>
public abstract record BoxPartParams(
    float Width,
    float Height,
    float Depth)
{
    public ToonSpec Material { get; init; } = new(new("wall.lit"), new("wall.shade"));
    public FootMat Foot { get; init; } = FootMat.Auto;
    public float Bevel { get; init; } = 0.02f;
    public int Variation { get; init; }
    public float Wear { get; init; }
}

public sealed record BoxParams(float Width = 1f, float Height = 1f, float Depth = 1f)
    : BoxPartParams(Width, Height, Depth);
public sealed record ColumnParams(float Width = 0.4f, float Height = 3f, float Depth = 0.4f)
    : BoxPartParams(Width, Height, Depth);
public sealed record PanelParams(float Width = 2f, float Height = 2.8f, float Depth = 0.12f)
    : BoxPartParams(Width, Height, Depth);
public sealed record TrimParams(float Width = 3f, float Height = 0.12f, float Depth = 0.3f)
    : BoxPartParams(Width, Height, Depth);
public sealed record WindowParams(float Width = 1.8f, float Height = 1.5f, float Depth = 0.2f,
    int Columns = 2,
    int Rows = 1)
    : BoxPartParams(Width, Height, Depth);
public sealed record DoorParams(float Width = 0.9f, float Height = WalkRules.DefaultDoorHeight, float Depth = 0.12f,
    float OpenAngleDeg = 0f,
    int Leaves = 1)
    : BoxPartParams(Width, Height, Depth);
public sealed record RailingParams(float Width = 3f, float Height = 1.1f, float Depth = 0.12f,
    float PostSpacing = 1f)
    : BoxPartParams(Width, Height, Depth);
public sealed record StairParams(float Width = 1.8f, float Height = 1.5f, float Depth = 2.7f,
    int Steps = 10)
    : BoxPartParams(Width, Height, Depth);
public sealed record RampParams(float Width = 1.8f, float Height = 0.3f, float Depth = 3.6f)
    : BoxPartParams(Width, Height, Depth);
public sealed record BalconyParams(float Width = 3.6f, float Height = 1.2f, float Depth = 1.2f)
    : BoxPartParams(Width, Height, Depth);
public sealed record RoofParams(float Width = 4f, float Height = 0.3f, float Depth = 2f,
    float SlopeDeg = 0f)
    : BoxPartParams(Width, Height, Depth);
public sealed record EquipmentParams(float Width = 0.8f, float Height = 1.2f, float Depth = 0.5f)
    : BoxPartParams(Width, Height, Depth)
{
    public SceneTextRun? PrintedText { get; init; }
    public PaletteIndex? PrintedTextBaseColor { get; init; }
}
public sealed record FurnitureParams(float Width = 1.8f, float Height = 0.8f, float Depth = 0.6f)
    : BoxPartParams(Width, Height, Depth);
public sealed record LampParams(float Width = 0.5f, float Height = 4f, float Depth = 0.5f)
    : BoxPartParams(Width, Height, Depth);
public sealed record SignParams(float Width = 1.2f, float Height = 1.8f, float Depth = 0.12f)
    : BoxPartParams(Width, Height, Depth)
{
    public SceneTextRun? PrintedText { get; init; }
    public PaletteIndex? PrintedTextBaseColor { get; init; }
}
public sealed record VehicleParams(float Width = 1.8f, float Height = 1.5f, float Depth = 4.2f)
    : BoxPartParams(Width, Height, Depth);
