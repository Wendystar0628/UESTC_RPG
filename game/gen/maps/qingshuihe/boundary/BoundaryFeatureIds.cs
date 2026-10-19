using Campus.Gen.World;

namespace Campus.Gen.Maps.Qingshuihe.Boundary;

/// <summary>Map identities owned by the campus boundary package.</summary>
public static class BoundaryFeatureIds
{
    public const string NorthBankContour = "design:boundary-north-bank-contour";
    public const string NorthForecourt = "design:boundary-north-forecourt";
    public const string NorthPlaza = "design:boundary-north-a05-plaza";
    public static readonly WorldFeatureIds NorthGateRoad = WorldFeatureIds.Way("qsh", 723349795);
    public static readonly WorldFeatureIds WestGateRoad = WorldFeatureIds.Way("qsh", 544161581);
    public static readonly WorldFeatureIds NorthwestGateRoad = WorldFeatureIds.Way("qsh", 563777250);
    public static readonly WorldFeatureIds West2GateRoad = WorldFeatureIds.Way("qsh", 544277137);
    public static readonly WorldFeatureIds SoutheastGateRoad = WorldFeatureIds.Way("qsh", 988857948);
    public static readonly WorldFeatureIds NorthTipRoad = WorldFeatureIds.Way("qsh", 723349816);
}
