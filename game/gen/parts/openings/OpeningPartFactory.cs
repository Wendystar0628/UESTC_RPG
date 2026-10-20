using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Openings;

/// <summary>Builds registered opening parts by their catalog ID.</summary>
public static class OpeningPartFactory
{

    private static readonly CasementWindowPart Casement = new();
    private static readonly RibbonWindowPart Ribbon = new();
    private static readonly SlitWindowPart Slit = new();
    private static readonly HighWindowPart High = new();
    private static readonly ObservationWindowPart Observation = new();
    private static readonly ServiceWindowPart Service = new();
    private static readonly CurtainWallPart CurtainWall = new();
    private static readonly EntranceDoorPart Entrance = new();
    private static readonly WoodDoorPart Wood = new();
    private static readonly FireDoorPart Fire = new();
    private static readonly GlassDoorPart GlassDoor = new();
    private static readonly ScreenDoorPart Screen = new();
    private static readonly StallDoorPart Stall = new();
    private static readonly AutomaticDoorFramePart AutoFrame = new();
    private static readonly AutomaticDoorLeafPart AutoLeaf = new();
    private static readonly DoorCloserPart Closer = new();
    private static readonly DoorStopPart Stop = new();
    private static readonly DoorVisionPanelPart Vision = new();
    private static readonly RollerShutterPart Roller = new();
    private static readonly LouvrePart Louvre = new();
    private static readonly CurtainPart Curtain = new();
    private static readonly PvcCurtainPart Pvc = new();
    private static readonly InteriorCardPart Card = new();

    /// <summary>Geometry of any WP-10 part by catalog ID; call on a bake worker.</summary>
    public static PartGeometry Build(PartId part, object parameters, LodLevel lod) => (part.Value, parameters) switch
    {
        (PartCatalog.WindowCasement, WindowParams p) => Casement.Lods[lod](p),
        (PartCatalog.WindowRibbon, WindowParams p) => Ribbon.Lods[lod](p),
        (PartCatalog.WindowSlit, WindowParams p) => Slit.Lods[lod](p),
        (PartCatalog.WindowHigh, WindowParams p) => High.Lods[lod](p),
        (PartCatalog.WindowObservation, WindowParams p) => Observation.Lods[lod](p),
        (PartCatalog.WindowService, WindowParams p) => Service.Lods[lod](p),
        (PartCatalog.CurtainWall, WindowParams p) => CurtainWall.Lods[lod](p),
        (PartCatalog.Door, DoorParams p) => Entrance.Lods[lod](p),
        (PartCatalog.DoorWood, DoorParams p) => Wood.Lods[lod](p),
        (PartCatalog.DoorFire, DoorParams p) => Fire.Lods[lod](p),
        (PartCatalog.DoorGlass, DoorParams p) => GlassDoor.Lods[lod](p),
        (PartCatalog.DoorScreen, DoorParams p) => Screen.Lods[lod](p),
        (PartCatalog.DoorStall, DoorParams p) => Stall.Lods[lod](p),
        (PartCatalog.AutomaticDoorFrame, DoorParams p) => AutoFrame.Lods[lod](p),
        (PartCatalog.AutomaticDoorLeaf, DoorParams p) => AutoLeaf.Lods[lod](p),
        (PartCatalog.DoorCloser, EquipmentParams p) => Closer.Lods[lod](p),
        (PartCatalog.DoorStop, EquipmentParams p) => Stop.Lods[lod](p),
        (PartCatalog.DoorVisionPanel, WindowParams p) => Vision.Lods[lod](p),
        (PartCatalog.DoorRoller, DoorParams p) => Roller.Lods[lod](p),
        (PartCatalog.Louvre, PanelParams p) => Louvre.Lods[lod](p),
        (PartCatalog.Curtain, PanelParams p) => Curtain.Lods[lod](p),
        (PartCatalog.PvcCurtain, PanelParams p) => Pvc.Lods[lod](p),
        (PartCatalog.InteriorCard, PanelParams p) => Card.Lods[lod](p),
        _ => throw new ArgumentException($"Not a WP-10 part, or the parameters do not match: {part.Value}.", nameof(part)),
    };
}
