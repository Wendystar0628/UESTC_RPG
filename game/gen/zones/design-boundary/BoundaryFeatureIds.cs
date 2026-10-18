using Campus.Gen.World;

namespace Campus.Gen.Zones.DesignBoundary;

/// <summary>Map identities owned by the campus boundary package.</summary>
public static class BoundaryFeatureIds
{
    public const string NorthBankContour = "design:boundary-north-bank-contour";
    public const string NorthForecourt = "design:boundary-north-forecourt";
    public const string NorthPlaza = "design:boundary-north-a05-plaza";
    public static readonly OsmFeatureIds NorthGateRoad = OsmFeatureIds.Way("qsh", 723349795);
    public static readonly OsmFeatureIds WestGateRoad = OsmFeatureIds.Way("qsh", 544161581);
    public static readonly OsmFeatureIds NorthwestGateRoad = OsmFeatureIds.Way("qsh", 563777250);
    public static readonly OsmFeatureIds West2GateRoad = OsmFeatureIds.Way("qsh", 544277137);
    public static readonly OsmFeatureIds SoutheastGateRoad = OsmFeatureIds.Way("qsh", 988857948);
    public static readonly OsmFeatureIds NorthTipRoad = OsmFeatureIds.Way("qsh", 723349816);
}
