namespace Campus.Contracts.Parts;

public sealed record PartCatalogEntry(PartId Id, WpId Owner, Type ParameterType, object Defaults);

/// <summary>D0 candidate shared vocabulary. Defaults are provisional bounding volumes until each owner supplies its geometry.</summary>
public static class PartCatalog
{
    public const string Box = "core.box";
    public const string Column = "core.column";
    public const string WallPanel = "core.wall.panel";
    public const string Trim = "core.trim";
    public const string Slab = "core.slab";
    public const string WindowCasement = "opening.window.casement";
    public const string WindowRibbon = "opening.window.ribbon";
    public const string WindowSlit = "opening.window.slit";
    public const string WindowHigh = "opening.window.high";
    public const string WindowObservation = "opening.window.observation";
    public const string WindowService = "opening.window.service";
    public const string CurtainWall = "opening.curtain-wall";
    public const string Door = "opening.door";
    public const string DoorWood = "opening.door.wood";
    public const string DoorFire = "opening.door.fire";
    public const string DoorGlass = "opening.door.glass";
    public const string DoorRoller = "opening.door.roller";
    public const string DoorScreen = "opening.door.screen";
    public const string DoorStall = "opening.door.stall";
    public const string AutomaticDoorFrame = "opening.door.automatic-frame";
    public const string AutomaticDoorLeaf = "opening.door.automatic-leaf";
    public const string DoorCloser = "opening.door.closer";
    public const string DoorStop = "opening.door.stop";
    public const string DoorVisionPanel = "opening.door.vision-panel";
    public const string Louvre = "opening.louvre";
    public const string Curtain = "opening.curtain";
    public const string PvcCurtain = "opening.curtain.pvc";
    public const string InteriorCard = "opening.interior-card";
    public const string Railing = "circulation.railing";
    public const string Handrail = "circulation.handrail";
    public const string StairStraight = "circulation.stair.straight";
    public const string StairDouble = "circulation.stair.double";
    public const string StairOpen = "circulation.stair.open";
    public const string Steps = "circulation.steps";
    public const string Ramp = "circulation.ramp";
    public const string Balcony = "circulation.balcony";
    public const string DryingRail = "circulation.drying-rail";
    public const string Planter = "circulation.planter";
    public const string IronGate = "circulation.gate.iron";
    public const string FoldingGate = "circulation.gate.folding";
    public const string RaisedFloor = "circulation.floor.raised";
    public const string TieredFloor = "circulation.floor.tiered";
    public const string Platform = "circulation.platform";
    public const string ElevatorDoor = "circulation.elevator-door";
    public const string ParapetCap = "roof.parapet-cap";
    public const string LightningStrip = "roof.lightning-strip";
    public const string Skylight = "roof.skylight";
    public const string WaterTank = "roof.water-tank";
    public const string HeatPump = "roof.heat-pump";
    public const string AirConditioner = "roof.air-conditioner";
    public const string EquipmentGrille = "roof.equipment-grille";
    public const string Canopy = "roof.canopy";
    public const string Downpipe = "roof.downpipe";
    public const string Antenna = "roof.antenna";
    public const string TestTower = "roof.test-tower";
    public const string SolarPanel = "roof.solar-panel";
    public const string EquipmentFence = "roof.equipment-fence";
    public const string Hydrant = "interior.mep.hydrant";
    public const string Extinguisher = "interior.mep.extinguisher";
    public const string IndoorAirConditioner = "interior.mep.air-conditioner";
    public const string CeilingFan = "interior.mep.ceiling-fan";
    public const string WallFan = "interior.mep.wall-fan";
    public const string SecurityCamera = "interior.mep.camera";
    public const string IndoorSpeaker = "interior.mep.speaker";
    public const string SwitchPanel = "interior.mep.switch-panel";
    public const string CableTray = "interior.mep.cable-tray";
    public const string Boiler = "interior.mep.boiler";
    public const string WaterDispenser = "interior.mep.water-dispenser";
    public const string CardReader = "interior.mep.card-reader";
    public const string Turnstile = "interior.mep.turnstile";
    public const string Switchgear = "interior.mep.switchgear";
    public const string ExitSign = "interior.mep.exit-sign";
    public const string RoomSign = "interior.mep.room-sign";
    public const string DirectorySign = "interior.mep.directory-sign";
    public const string Pennant = "interior.mep.pennant";
    public const string Banner = "interior.mep.banner";
    public const string StreetLamp = "street.lamp";
    public const string Bench = "street.bench";
    public const string BinFour = "street.bin-four";
    public const string Bollard = "street.bollard";
    public const string StoneSphere = "street.stone-sphere";
    public const string BusStop = "street.bus-stop";
    public const string ParkedCar = "street.car";
    public const string ParkedBicycle = "street.bicycle";
    public const string DeliveryLocker = "street.delivery-locker";
    public const string VendingMachine = "street.vending-machine";
    public const string Signpost = "street.signpost";
    public const string DrinkingFountain = "street.drinking-fountain";
    public const string AtlasKiosk = "street.atlas-kiosk";
    public const string AtlasPost = "street.atlas-post";
    public const string EntranceMat = "street.entrance-mat";
    public const string OpenSign = "street.open-sign";
    public const string EntranceLamp = "street.entrance-lamp";
    public const string RoadNameSign = "street.road-name-sign";
    public const string StreetSpeaker = "street.speaker";
    public const string ConvexMirror = "street.convex-mirror";
    public const string BicycleShelter = "street.bicycle-shelter";
    public const string UtilityPole = "street.utility-pole";
    public const string StreetFence = "street.fence";
    public const string Drain = "street.drain";
    public const string CurbRamp = "street.curb-ramp";
    public const string WallClock = "street.wall-clock";
    public const string NoticeBoard = "street.notice-board";

    public static IReadOnlyList<PartCatalogEntry> Entries { get; } = Array.AsReadOnly<PartCatalogEntry>(
    [
        new(new(Box), new("WP-09"), typeof(BoxParams), new BoxParams(1f, 1f, 1f)),
        new(new(Column), new("WP-09"), typeof(ColumnParams), new ColumnParams(0.4f, 3f, 0.4f)),
        new(new(WallPanel), new("WP-09"), typeof(PanelParams), new PanelParams(3.6f, 3f, 0.2f)),
        new(new(Trim), new("WP-09"), typeof(TrimParams), new TrimParams(3.6f, 0.12f, 0.3f)),
        new(new(Slab), new("WP-09"), typeof(PanelParams), new PanelParams(3.6f, 0.2f, 3.6f)),
        new(new(WindowCasement), new("WP-10"), typeof(WindowParams), new WindowParams(1.8f, 1.5f, 0.07f)),
        new(new(WindowRibbon), new("WP-10"), typeof(WindowParams), new WindowParams(3.6f, 1.5f, 0.07f, Columns: 3, Rows: 1)),
        new(new(WindowSlit), new("WP-10"), typeof(WindowParams), new WindowParams(0.6f, 2.4f, 0.09f, Columns: 1, Rows: 3)),
        new(new(WindowHigh), new("WP-10"), typeof(WindowParams), new WindowParams(1.8f, 0.6f, 0.07f)),
        new(new(WindowObservation), new("WP-10"), typeof(WindowParams), new WindowParams(0.6f, 0.8f, 0.06f, Columns: 1, Rows: 1)),
        new(new(WindowService), new("WP-10"), typeof(WindowParams), new WindowParams(1.2f, 1f, 0.3f)),
        new(new(CurtainWall), new("WP-10"), typeof(WindowParams), new WindowParams(3.6f, 3.2f, 0.2f, Columns: 3, Rows: 1)),
        new(new(Door), new("WP-10"), typeof(DoorParams), new DoorParams(0.9f, 2.1f, 0.12f)),
        new(new(DoorWood), new("WP-10"), typeof(DoorParams), new DoorParams(0.9f, 2.1f, 0.2f)),
        new(new(DoorFire), new("WP-10"), typeof(DoorParams), new DoorParams(1f, 2.1f, 0.2f)),
        new(new(DoorGlass), new("WP-10"), typeof(DoorParams), new DoorParams(1.8f, 2.4f, 0.12f, OpenAngleDeg: 0f, Leaves: 2)),
        new(new(DoorRoller), new("WP-10"), typeof(DoorParams), new DoorParams(3f, 2.8f, 0.15f)),
        new(new(DoorScreen), new("WP-10"), typeof(DoorParams), new DoorParams(1.2f, 2.2f, 0.2f)),
        new(new(DoorStall), new("WP-10"), typeof(DoorParams), new DoorParams(0.7f, 1.9f, 0.04f)),
        new(new(AutomaticDoorFrame), new("WP-10"), typeof(DoorParams), new DoorParams(1f, 2.1f, 0.2f)),
        new(new(AutomaticDoorLeaf), new("WP-10"), typeof(DoorParams), new DoorParams(0.87f, 2.03f, 0.045f)),
        new(new(DoorCloser), new("WP-10"), typeof(EquipmentParams), new EquipmentParams(0.3f, 0.07f, 0.06f)),
        new(new(DoorStop), new("WP-10"), typeof(EquipmentParams), new EquipmentParams(0.08f, 0.06f, 0.08f)),
        new(new(DoorVisionPanel), new("WP-10"), typeof(WindowParams), new WindowParams(0.2f, 0.6f, 0.03f)),
        new(new(Louvre), new("WP-10"), typeof(PanelParams), new PanelParams(1.2f, 0.8f, 0.08f)),
        new(new(Curtain), new("WP-10"), typeof(PanelParams), new PanelParams(1.8f, 2.2f, 0.016f)),
        new(new(PvcCurtain), new("WP-10"), typeof(PanelParams), new PanelParams(1.2f, 2.2f, 0.04f)),
        new(new(InteriorCard), new("WP-10"), typeof(PanelParams), new PanelParams(1.8f, 1.5f, 0.02f)),
        new(new(Railing), new("WP-11"), typeof(RailingParams), new RailingParams(3f, 1.1f, 0.12f, PostSpacing: 1.8f)),
        new(new(Handrail), new("WP-11"), typeof(RailingParams), new RailingParams(3f, 0.08f, 0.08f)),
        new(new(StairStraight), new("WP-11"), typeof(StairParams), new StairParams(1.8f, 1.5f, 2.7f)),
        new(new(StairDouble), new("WP-11"), typeof(StairParams), new StairParams(3.1f, 3.3f, 4.58f, Steps: 11)),
        new(new(StairOpen), new("WP-11"), typeof(StairParams), new StairParams(1.8f, 1.5f, 2.7f)),
        new(new(Steps), new("WP-11"), typeof(StairParams), new StairParams(3.6f, 0.45f, 0.9f, Steps: 3)),
        new(new(Ramp), new("WP-11"), typeof(RampParams), new RampParams(1.8f, 0.3f, 3.6f)),
        new(new(Balcony), new("WP-11"), typeof(BalconyParams), new BalconyParams(3.6f, 1.2f, 1.2f)),
        new(new(DryingRail), new("WP-11"), typeof(RailingParams), new RailingParams(2f, 0.08f, 0.08f)),
        new(new(Planter), new("WP-11"), typeof(FurnitureParams), new FurnitureParams(1.2f, 0.6f, 0.5f)),
        new(new(IronGate), new("WP-11"), typeof(DoorParams), new DoorParams(1.8f, 2.1f, 0.12f)),
        new(new(FoldingGate), new("WP-11"), typeof(DoorParams), new DoorParams(3.6f, 1.6f, 0.3f)),
        new(new(RaisedFloor), new("WP-11"), typeof(PanelParams), new PanelParams(3.6f, 0.2f, 3.6f)),
        new(new(TieredFloor), new("WP-11"), typeof(StairParams), new StairParams(6f, 1.5f, 2.7f)),
        new(new(Platform), new("WP-11"), typeof(PanelParams), new PanelParams(3.6f, 0.3f, 1.8f)),
        new(new(ElevatorDoor), new("WP-11"), typeof(DoorParams), new DoorParams(1.2f, 2.2f, 0.1f)),
        new(new(ParapetCap), new("WP-12"), typeof(TrimParams), new TrimParams(3.6f, 0.15f, 0.4f)),
        new(new(LightningStrip), new("WP-12"), typeof(TrimParams), new TrimParams(3.6f, 0.15f, 0.08f)),
        new(new(Skylight), new("WP-12"), typeof(RoofParams), new RoofParams(2.4f, 0.5f, 1.8f)),
        new(new(WaterTank), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(2f, 2.5f, 2f)),
        new(new(HeatPump), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(1.6f, 1.5f, 0.9f)),
        new(new(AirConditioner), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(0.64f, 0.48f, 0.26f)),
        new(new(EquipmentGrille), new("WP-12"), typeof(PanelParams), new PanelParams(0.8f, 0.64f, 0.36f)),
        new(new(Canopy), new("WP-12"), typeof(RoofParams), new RoofParams(3.6f, 0.3f, 1.8f)),
        new(new(Downpipe), new("WP-12"), typeof(ColumnParams), new ColumnParams(0.12f, 3f, 0.16f)),
        new(new(Antenna), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(1.4f, 2.2f, 1.4f)),
        new(new(TestTower), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(2f, 6f, 2f)),
        new(new(SolarPanel), new("WP-12"), typeof(PanelParams), new PanelParams(1.1f, 0.08f, 1.8f)),
        new(new(EquipmentFence), new("WP-12"), typeof(RailingParams), new RailingParams(3f, 1.8f, 0.06f, PostSpacing: 1.3f)),
        new(new(Hydrant), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(0.7f, 1f, 0.2f)),
        new(new(Extinguisher), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(0.18f, 0.55f, 0.18f)),
        new(new(IndoorAirConditioner), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(0.9f, 0.3f, 0.2f)),
        new(new(CeilingFan), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(1.2f, 0.4f, 1.2f)),
        new(new(WallFan), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(0.4f, 0.5f, 0.3f)),
        new(new(SecurityCamera), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(0.12f, 0.12f, 0.25f)),
        new(new(IndoorSpeaker), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(0.25f, 0.35f, 0.2f)),
        new(new(SwitchPanel), new("WP-12"), typeof(PanelParams), new PanelParams(0.09f, 0.09f, 0.02f)),
        new(new(CableTray), new("WP-12"), typeof(TrimParams), new TrimParams(3f, 0.1f, 0.3f)),
        new(new(Boiler), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(0.7f, 1.2f, 0.45f)),
        new(new(WaterDispenser), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(0.35f, 1.1f, 0.35f)),
        new(new(CardReader), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(0.12f, 0.18f, 0.04f)),
        new(new(Turnstile), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(0.25f, 1f, 1.2f)),
        new(new(Switchgear), new("WP-12"), typeof(EquipmentParams), new EquipmentParams(0.8f, 1.8f, 0.6f)),
        new(new(ExitSign), new("WP-12"), typeof(SignParams), new SignParams(0.4f, 0.15f, 0.04f)),
        new(new(RoomSign), new("WP-12"), typeof(SignParams), new SignParams(0.3f, 0.12f, 0.02f)),
        new(new(DirectorySign), new("WP-12"), typeof(SignParams), new SignParams(0.6f, 1f, 0.04f)),
        new(new(Pennant), new("WP-12"), typeof(SignParams), new SignParams(0.5f, 0.8f, 0.02f)),
        new(new(Banner), new("WP-12"), typeof(SignParams), new SignParams(3f, 0.6f, 0.02f)),
        new(new(StreetLamp), new("WP-13"), typeof(LampParams), new LampParams(0.6f, 4.5f, 0.6f)),
        new(new(Bench), new("WP-13"), typeof(FurnitureParams), new FurnitureParams(1.8f, 0.8f, 0.6f)),
        new(new(BinFour), new("WP-13"), typeof(FurnitureParams), new FurnitureParams(2f, 1f, 0.55f)),
        new(new(Bollard), new("WP-13"), typeof(ColumnParams), new ColumnParams(0.18f, 0.8f, 0.18f)),
        new(new(StoneSphere), new("WP-13"), typeof(FurnitureParams), new FurnitureParams(0.5f, 0.5f, 0.5f)),
        new(new(BusStop), new("WP-13"), typeof(SignParams), new SignParams(1.2f, 2.4f, 0.18f)),
        new(new(ParkedCar), new("WP-13"), typeof(VehicleParams), new VehicleParams(1.8f, 1.5f, 4.2f)),
        new(new(ParkedBicycle), new("WP-13"), typeof(VehicleParams), new VehicleParams(0.6f, 1.1f, 1.8f)),
        new(new(DeliveryLocker), new("WP-13"), typeof(EquipmentParams), new EquipmentParams(2.4f, 2f, 0.6f)),
        new(new(VendingMachine), new("WP-13"), typeof(EquipmentParams), new EquipmentParams(1f, 1.85f, 0.8f)),
        new(new(Signpost), new("WP-13"), typeof(SignParams), new SignParams(1.2f, 2.2f, 0.12f)),
        new(new(DrinkingFountain), new("WP-13"), typeof(EquipmentParams), new EquipmentParams(0.6f, 0.95f, 0.5f)),
        new(new(AtlasKiosk), new("WP-13"), typeof(SignParams), new SignParams(2.2f, 2.4f, 0.5f)),
        new(new(AtlasPost), new("WP-13"), typeof(SignParams), new SignParams(0.3f, 1f, 0.3f)),
        new(new(EntranceMat), new("WP-13"), typeof(PanelParams), new PanelParams(1.5f, 0.03f, 0.9f)),
        new(new(OpenSign), new("WP-13"), typeof(SignParams), new SignParams(0.3f, 0.15f, 0.02f)),
        new(new(EntranceLamp), new("WP-13"), typeof(LampParams), new LampParams(0.25f, 0.25f, 0.15f)),
        new(new(RoadNameSign), new("WP-13"), typeof(SignParams), new SignParams(1f, 0.3f, 0.06f)),
        new(new(StreetSpeaker), new("WP-13"), typeof(EquipmentParams), new EquipmentParams(0.4f, 0.3f, 0.4f)),
        new(new(ConvexMirror), new("WP-13"), typeof(SignParams), new SignParams(0.6f, 2.2f, 0.2f)),
        new(new(BicycleShelter), new("WP-13"), typeof(RoofParams), new RoofParams(6f, 2.4f, 3f)),
        new(new(UtilityPole), new("WP-13"), typeof(ColumnParams), new ColumnParams(0.25f, 6f, 0.25f)),
        new(new(StreetFence), new("WP-13"), typeof(RailingParams), new RailingParams(3f, 1.5f, 0.12f)),
        new(new(Drain), new("WP-13"), typeof(TrimParams), new TrimParams(2f, 0.12f, 0.3f)),
        new(new(CurbRamp), new("WP-13"), typeof(RampParams), new RampParams(1.5f, 0.15f, 1.05f)),
        new(new(WallClock), new("WP-13"), typeof(SignParams), new SignParams(0.5f, 0.5f, 0.08f)),
        new(new(NoticeBoard), new("WP-13"), typeof(SignParams), new SignParams(1.8f, 2f, 0.15f)),
    ]);

    public static PartCatalogEntry? Find(PartId id) => Entries.FirstOrDefault(entry => entry.Id == id);
}
