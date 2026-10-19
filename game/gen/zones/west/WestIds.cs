using Campus.Gen.World;

namespace Campus.Gen.Zones.West;

/// <summary>West zone map identities and authored layout selections.</summary>
public static class WestIds
{
    public static readonly WorldFeatureIds Node11020008575 = WorldFeatureIds.Node("qsh", 11020008575);
    public static readonly WorldFeatureIds Node11361252903 = WorldFeatureIds.Node("qsh", 11361252903);
    public static readonly WorldFeatureIds Node6784744448 = WorldFeatureIds.Node("qsh", 6784744448);
    public static readonly WorldFeatureIds Relation10807930 = WorldFeatureIds.Relation("qsh", 10807930);
    public static readonly WorldFeatureIds Relation11031411 = WorldFeatureIds.Relation("qsh", 11031411);
    public static readonly WorldFeatureIds Relation11032065 = WorldFeatureIds.Relation("qsh", 11032065);
    public static readonly WorldFeatureIds Relation11334252 = WorldFeatureIds.Relation("qsh", 11334252);
    public static readonly WorldFeatureIds Relation11334253 = WorldFeatureIds.Relation("qsh", 11334253);
    public static readonly WorldFeatureIds Relation11334255 = WorldFeatureIds.Relation("qsh", 11334255);
    public static readonly WorldFeatureIds Relation11665641 = WorldFeatureIds.Relation("qsh", 11665641);
    public static readonly WorldFeatureIds Relation14153711 = WorldFeatureIds.Relation("qsh", 14153711);
    public static readonly WorldFeatureIds Relation8426842 = WorldFeatureIds.Relation("qsh", 8426842);
    public static readonly WorldFeatureIds Way217855160 = WorldFeatureIds.Way("qsh", 217855160);
    public static readonly WorldFeatureIds Way217855313 = WorldFeatureIds.Way("qsh", 217855313);
    public static readonly WorldFeatureIds Way544161580 = WorldFeatureIds.Way("qsh", 544161580);
    public static readonly WorldFeatureIds Way552816780 = WorldFeatureIds.Way("qsh", 552816780);
    public static readonly WorldFeatureIds Way552816781 = WorldFeatureIds.Way("qsh", 552816781);
    public static readonly WorldFeatureIds Way570145228 = WorldFeatureIds.Way("qsh", 570145228);
    public static readonly WorldFeatureIds Way603368216 = WorldFeatureIds.Way("qsh", 603368216);
    public static readonly WorldFeatureIds Way604832425 = WorldFeatureIds.Way("qsh", 604832425);
    public static readonly WorldFeatureIds Way692240860 = WorldFeatureIds.Way("qsh", 692240860);
    public static readonly WorldFeatureIds Way779496151 = WorldFeatureIds.Way("qsh", 779496151);
    public static readonly WorldFeatureIds Way779496154 = WorldFeatureIds.Way("qsh", 779496154);
    public static readonly WorldFeatureIds Way779496166 = WorldFeatureIds.Way("qsh", 779496166);
    public static readonly WorldFeatureIds Way779496167 = WorldFeatureIds.Way("qsh", 779496167);
    public static readonly WorldFeatureIds Way779496168 = WorldFeatureIds.Way("qsh", 779496168);
    public static readonly WorldFeatureIds Way779496169 = WorldFeatureIds.Way("qsh", 779496169);
    public static readonly WorldFeatureIds Way795677482 = WorldFeatureIds.Way("qsh", 795677482);
    public static readonly WorldFeatureIds Way795677483 = WorldFeatureIds.Way("qsh", 795677483);
    public static readonly WorldFeatureIds Way950008613 = WorldFeatureIds.Way("qsh", 950008613);

    public const string TerrainPrefix = "design:terrain:west-";
    public const string A10Plaza = "design:west-a10-plaza";
    public const string BoardwalkRoute = "design:west-boardwalk-route";
    public const string FourDoorA = "design:west-four-door-a";
    public const string FourDoorB = "design:west-four-door-b";
    public const string FourDoorC = "design:west-four-door-c";
    public const string FourFanLawn = "design:terrain:west-four-fan-lawn";
    public const string FourSeatArc1 = "design:west-four-seat-arc-1";
    public const string FourSeatArc2 = "design:west-four-seat-arc-2";
    public const string FourSeatArc3 = "design:west-four-seat-arc-3";
    public const string HongguangNorth = "design:terrain:west-hongguang-north";
    public const string InnovationDoor = "design:west-innovation-door";
    public const string InnovationShaft1 = "design:west-innovation-shaft-1";
    public const string InnovationShaft2 = "design:west-innovation-shaft-2";
    public const string InnovationShaft3 = "design:west-innovation-shaft-3";
    public const string InnovationShaft4 = "design:west-innovation-shaft-4";
    public const string Levee = "design:terrain:west-levee";
    public const string LeveeLakeMouth = "design:west-levee-lake-mouth";
    public const string LeveeStoneBridge = "design:west-levee-stone-bridge";
    public const string ShelterBack = "design:west-shelter-back";
    public const string SixBridgeBEnd = "design:west-six-bridge-b-end";
    public const string SixBridgeBStart = "design:west-six-bridge-b-start";
    public const string SixBridgeCEnd = "design:west-six-bridge-c-end";
    public const string SixBridgeCStart = "design:west-six-bridge-c-start";
    public const string SixEastFace = "design:west-six-east-face";
    public const string SixForecourt = "design:west-six-forecourt";
    public const string SixPlant1 = "design:west-six-plant-1";
    public const string SixPlant2 = "design:west-six-plant-2";
    public const string SixPlant3 = "design:west-six-plant-3";
    public const string SixShutDoor1 = "design:west-six-shut-door-1";
    public const string SixShutDoor2 = "design:west-six-shut-door-2";
    public const string SixShutDoor3 = "design:west-six-shut-door-3";
    public const string SixShutFace1 = "design:west-six-shut-face-1";
    public const string SixShutFace2 = "design:west-six-shut-face-2";
    public const string SixShutFace3 = "design:west-six-shut-face-3";
    public const string WetlandBeach = "design:west-wetland-beach";
    public const string WetlandLow = "design:terrain:west-wetland-low";
    public const string WetlandStream = "design:terrain:west-wetland-stream";
    public const string WetlandWade = "design:west-wetland-wade";

    public static IReadOnlyList<string> SixShutFaces { get; } = [SixShutFace1, SixShutFace2, SixShutFace3];
    public static IReadOnlyList<(string Start, string End)> SixBridges { get; } =
        [(SixBridgeBStart, SixBridgeBEnd), (SixBridgeCStart, SixBridgeCEnd)];
}
