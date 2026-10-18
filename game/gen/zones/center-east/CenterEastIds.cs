using Campus.Gen.World;

namespace Campus.Gen.Zones.CenterEast;

/// <summary>Zone identities; each OSM member is declared once.</summary>
public static class CenterEastIds
{
    private const string Prefix = "qsh";
    private static readonly OsmFeatureIds Relation11043355 = OsmFeatureIds.Relation(Prefix, 11043355);
    private static readonly OsmFeatureIds Relation11043352 = OsmFeatureIds.Relation(Prefix, 11043352);
    private static readonly OsmFeatureIds Relation11043353 = OsmFeatureIds.Relation(Prefix, 11043353);
    private static readonly OsmFeatureIds Way797290169 = OsmFeatureIds.Way(Prefix, 797290169);
    private static readonly OsmFeatureIds Way797290174 = OsmFeatureIds.Way(Prefix, 797290174);
    private static readonly OsmFeatureIds Way797290183 = OsmFeatureIds.Way(Prefix, 797290183);
    private static readonly OsmFeatureIds Way797290182 = OsmFeatureIds.Way(Prefix, 797290182);
    private static readonly OsmFeatureIds Relation11043356 = OsmFeatureIds.Relation(Prefix, 11043356);
    private static readonly OsmFeatureIds Relation11127418 = OsmFeatureIds.Relation(Prefix, 11127418);
    private static readonly OsmFeatureIds Relation11127417 = OsmFeatureIds.Relation(Prefix, 11127417);
    private static readonly OsmFeatureIds Way806848385 = OsmFeatureIds.Way(Prefix, 806848385);
    private static readonly OsmFeatureIds Way806848386 = OsmFeatureIds.Way(Prefix, 806848386);
    private static readonly OsmFeatureIds Way806848387 = OsmFeatureIds.Way(Prefix, 806848387);
    private static readonly OsmFeatureIds Way806848388 = OsmFeatureIds.Way(Prefix, 806848388);
    private static readonly OsmFeatureIds Way806848375 = OsmFeatureIds.Way(Prefix, 806848375);
    private static readonly OsmFeatureIds Way806848378 = OsmFeatureIds.Way(Prefix, 806848378);
    private static readonly OsmFeatureIds Way806848379 = OsmFeatureIds.Way(Prefix, 806848379);
    private static readonly OsmFeatureIds Way806848381 = OsmFeatureIds.Way(Prefix, 806848381);
    private static readonly OsmFeatureIds Way806848376 = OsmFeatureIds.Way(Prefix, 806848376);
    private static readonly OsmFeatureIds Way806848377 = OsmFeatureIds.Way(Prefix, 806848377);
    private static readonly OsmFeatureIds Way806848380 = OsmFeatureIds.Way(Prefix, 806848380);
    private static readonly OsmFeatureIds Node5261487375 = OsmFeatureIds.Node(Prefix, 5261487375);
    private static readonly OsmFeatureIds Node7878394896 = OsmFeatureIds.Node(Prefix, 7878394896);
    private static readonly OsmFeatureIds Node7878386482 = OsmFeatureIds.Node(Prefix, 7878386482);
    private static readonly OsmFeatureIds Relation11583698 = OsmFeatureIds.Relation(Prefix, 11583698);
    private static readonly OsmFeatureIds Way883187548 = OsmFeatureIds.Way(Prefix, 883187548);
    private static readonly OsmFeatureIds Way1431090636 = OsmFeatureIds.Way(Prefix, 1431090636);
    private static readonly OsmFeatureIds Way1431090637 = OsmFeatureIds.Way(Prefix, 1431090637);
    private static readonly OsmFeatureIds Way1431090638 = OsmFeatureIds.Way(Prefix, 1431090638);
    private static readonly OsmFeatureIds Way1431090639 = OsmFeatureIds.Way(Prefix, 1431090639);
    private static readonly OsmFeatureIds Way1431090640 = OsmFeatureIds.Way(Prefix, 1431090640);

    public static class Pinxue
    {
        public static readonly string Complex = Relation11043355.Entity;
        public static readonly string WingA = Relation11043352.Entity;
        public static readonly string WingB = Relation11043353.Entity;
        public static readonly string WingAMain = Relation11043352.Part;
        public static readonly string ArcLobby = Way797290169.Part;
        public static readonly string FrameBar = Way797290174.Part;
        public static readonly string BridgeAC = Way797290183.Part;
        public static readonly string BridgeBC = Way797290182.Part;
        public static readonly string Forecourt = Relation11043356.Source;
        public const string FountainId = "design:center-east-pinxue-fountain";
        public const string TreePitsId = "design:center-east-pinxue-tree-pits";
        public const string RoundBedId = "design:center-east-pinxue-round-bed";
        public const string RampWallNorthId = "design:center-east-pinxue-ramp-seatwall-north";
        public const string RampWallSouthId = "design:center-east-pinxue-ramp-seatwall-south";
        public const string ArcEndId = "design:center-east-pinxue-arc-end";
    }

    public static class Research
    {
        public const string KeAHallId = "design:center-east-ke-a-lens-hall";
        public const string KeBHallId = "design:center-east-ke-b-lens-hall";
        public const string KeCHallId = "design:center-east-ke-c-lens-hall";

        public static readonly string KeA = Relation11127418.Entity;
        public static readonly string KeB = Relation11127417.Entity;
        public static readonly string KeC = Way806848385.Entity;
        public static readonly string KeAMain = Way806848386.Part;
        public static readonly string KeAWest = Way806848387.Part;
        public static readonly string KeAHall = Way806848388.Part;
        public static readonly string KeBMain = Way806848375.Part;
        public static readonly string KeBMiddle = Way806848378.Part;
        public static readonly string KeBWest = Way806848379.Part;
        public static readonly string KeBHall = Way806848381.Part;
        public static readonly string KeCBar = Way806848376.Part;
        public static readonly string KeCLink = Way806848377.Part;
        public static readonly string KeCHall = Way806848380.Part;
        public const string KeARoofId = "design:center-east-ke-a-roof";
        public const string KeBRoofId = "design:center-east-ke-b-roof";
        public const string KeCGardenId = "design:center-east-ke-c-roof-garden";
    }

    public static class Monuments
    {
        public static readonly string Lin = Node5261487375.Entity;
        public static readonly string Maxwell = Node7878394896.Entity;
        public static readonly string Stele = Node7878386482.Entity;
        public const string StoneId = "design:center-east-maxwell-equation-stone";
        public const string ApronId = "design:center-east-maxwell-apron";
        public static readonly string Plaza = Relation11583698.Source;
    }

    public static class Wenke
    {
        public static readonly string Entity = Way883187548.Entity;
        public static readonly string SourceOutline = Way883187548.Part;
        public static readonly string Tower = Way1431090636.Part;
        public static readonly string South = Way1431090637.Part;
        public static readonly string Hub = Way1431090638.Part;
        public static readonly string North = Way1431090639.Part;
        public static readonly string West = Way1431090640.Part;
        public const string AtriumRoofId = "design:center-east-wenke-atrium-roof";
        public const string ForecourtId = "design:center-east-wenke-forecourt";
        public const string WalkId = "design:center-east-wenke-walk";
    }

    public static class Friendship
    {
        public const string TreeId = "design:center-east-wenke-friendship-tree";
        public const string StoneId = "design:center-east-wenke-friendship-stone";
    }
    public static class Layout
    {
        public const string PinxueFrameEdge = "design:center-east-pinxue-frame-edge";
        public const string PinxueBridgeAcEdge = "design:center-east-pinxue-bridge-ac-edge";
        public const string PinxueBridgeBcEdge = "design:center-east-pinxue-bridge-bc-edge";
        public const string PinxueArcChord = "design:center-east-pinxue-arc-chord";
        public const string KeAHallBack = "design:center-east-ke-a-hall-back";
        public const string KeBHallBack = "design:center-east-ke-b-hall-back";
        public const string KeCHallBack = "design:center-east-ke-c-hall-back";
        public const string KeAStair1 = "design:center-east-ke-a-stair-1";
        public const string KeAStair2 = "design:center-east-ke-a-stair-2";
        public const string KeAStair3 = "design:center-east-ke-a-stair-3";
        public const string KeAStair4 = "design:center-east-ke-a-stair-4";
        public const string KeAPlant1 = "design:center-east-ke-a-plant-1";
        public const string KeADoor1 = "design:center-east-ke-a-door-1";
        public const string KeBStair1 = "design:center-east-ke-b-stair-1";
        public const string KeBStair2 = "design:center-east-ke-b-stair-2";
        public const string KeBStair3 = "design:center-east-ke-b-stair-3";
        public const string KeBStair4 = "design:center-east-ke-b-stair-4";
        public const string KeBPlant1 = "design:center-east-ke-b-plant-1";
        public const string KeBDoor1 = "design:center-east-ke-b-door-1";
        public const string KeCStair1 = "design:center-east-ke-c-stair-1";
        public const string KeCStair2 = "design:center-east-ke-c-stair-2";
        public const string KeCPlant1 = "design:center-east-ke-c-plant-1";
        public const string LinPlacement = "design:center-east-lin-placement";
    }
}
