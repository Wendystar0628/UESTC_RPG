using Campus.Contracts;

namespace Campus.Gen.Parts.Core;

/// <summary>Documented initial values from ART-MODELING §§2, 5–7; final tuning belongs to the golden-sample review.</summary>
public static class PartStyleDefaults
{
    public const float WallCornerBevel = 0.03f;
    public const float TrimEdgeBevel = 0.015f;
    public const float MinimumReadablePixels = 2;
    public const float MinimumRepeatedPeriodPixels = 4;
    public const int MaximumIdenticalBays = 4;
    public const float ColorDeltaL = 4;
    public const float ColorDeltaA = 3;
    public const float ColorDeltaB = 3;
    public const float CorniceSmall = 0.3f;
    public const float CorniceMedium = 0.6f;
    public const float CorniceLarge = 1;
    public const float JointOverlap = 0.005f;
    public const float OpeningAo = 0.2f;
    public const LineClass BuildingLine = LineClass.Building;
    public const LineClass TrimLine = LineClass.Fine;

    public static SweepProfile CorniceProfile(float depth = CorniceMedium, float height = 0.5f) =>
        PartGeometryKernel.BeveledRectangleProfile(depth, height, TrimEdgeBevel);
}
