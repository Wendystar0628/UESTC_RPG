using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Openings;

/// <summary>
/// One gallery entry: a part with review parameters, the wall opening it sits in (sill height above the floor) and,
/// optionally, a covering placed just in front of the glass. Glazed entries get wall-block glass from <see cref="OpeningGlass"/>.
/// </summary>
public sealed record OpeningSample(string Id, string Label, PartId Part, object Params, float Width, float Height, float Sill, bool Glazed)
{
    public PartId? Covering { get; init; }
    public object? CoveringParams { get; init; }
}

/// <summary>Review set for the part gallery and the WP-10 sandbox: every opening part once, exterior row first.</summary>
public static class OpeningGallery
{
    public static IReadOnlyList<OpeningSample> Samples() => Array.AsReadOnly<OpeningSample>(
    [
        new("casement", "平开窗", new(PartCatalog.WindowCasement), new WindowParams(1.8f, 1.6f, 0.07f, 2, 2), 1.8f, 1.6f, 0.9f, true),
        new("casement-curtain", "平开窗 + 窗帘半开", new(PartCatalog.WindowCasement), new WindowParams(1.8f, 1.6f, 0.07f, 2, 1), 1.8f, 1.6f, 0.9f, true)
        {
            Covering = new(PartCatalog.Curtain),
            CoveringParams = new PanelParams(1.74f, 1.58f, 0.016f) { Variation = 1, Material = OpeningStyle.Fabric },
        },
        new("ribbon", "带形窗", new(PartCatalog.WindowRibbon), new WindowParams(3.6f, 1.5f, 0.07f, 3, 1), 3.6f, 1.5f, 1.0f, true),
        new("slit", "竖缝窗", new(PartCatalog.WindowSlit), new WindowParams(0.6f, 2.4f, 0.09f, 1, 3), 0.6f, 2.4f, 0.5f, true),
        new("curtain-wall", "幕墙", new(PartCatalog.CurtainWall), new WindowParams(3.6f, 3.2f, 0.2f, 3, 2), 3.6f, 3.2f, 0.3f, true),
        new("door-closed", "门（关）", new(PartCatalog.Door), new DoorParams(1.8f, 2.7f, 0.12f, 0f, 2), 1.8f, 2.7f, 0f, false),
        new("door-open", "门（固定敞开）", new(PartCatalog.Door), new DoorParams(1.8f, 2.4f, 0.12f, 90f, 2), 1.8f, 2.4f, 0f, false),
        new("roller", "卷帘（半落）", new(PartCatalog.DoorRoller), new DoorParams(3f, 2.8f, 0.15f, 45f), 3f, 2.8f, 0f, false),
        new("louvre", "百叶", new(PartCatalog.Louvre), new PanelParams(1.2f, 0.8f, 0.08f), 1.2f, 0.8f, 1.6f, false),
        new("blind", "平开窗 + 室内百叶", new(PartCatalog.WindowCasement), new WindowParams(1.8f, 1.6f, 0.07f, 2, 1), 1.8f, 1.6f, 0.9f, true)
        {
            Covering = new(PartCatalog.Louvre),
            CoveringParams = new PanelParams(1.74f, 1.58f, 0.02f) { Variation = 2 },
        },
        new("door-wood", "木门", new(PartCatalog.DoorWood), new DoorParams(0.9f, 2.1f, 0.2f), 0.9f, 2.1f, 0f, false),
        new("door-fire", "防火门", new(PartCatalog.DoorFire), new DoorParams(1.0f, 2.1f, 0.2f), 1.0f, 2.1f, 0f, false),
        new("door-glass", "玻璃地弹门", new(PartCatalog.DoorGlass), new DoorParams(1.8f, 2.4f, 0.12f, 0f, 2), 1.8f, 2.4f, 0f, false),
        new("door-screen", "屏蔽门", new(PartCatalog.DoorScreen), new DoorParams(1.2f, 2.2f, 0.2f), 1.2f, 2.2f, 0f, false),
        new("door-stall", "隔间门", new(PartCatalog.DoorStall), new DoorParams(0.7f, 1.9f, 0.04f), 0.7f, 1.9f, 0f, false),
        new("window-high", "高窗", new(PartCatalog.WindowHigh), new WindowParams(1.8f, 0.6f, 0.07f, 2, 1), 1.8f, 0.6f, 2.1f, true),
        new("window-observation", "观察窗", new(PartCatalog.WindowObservation), new WindowParams(0.6f, 0.8f, 0.06f, 1, 1), 0.6f, 0.8f, 1.1f, true),
        new("window-service", "服务窗口", new(PartCatalog.WindowService), new WindowParams(1.2f, 1.0f, 0.3f, 2, 1), 1.2f, 1.0f, 0.9f, true),
        new("auto-door", "自动门（框 + 扇 + 闭门器）", new(PartCatalog.AutomaticDoorFrame), new DoorParams(1.0f, 2.1f, 0.2f) { Variation = 1 }, 1.0f, 2.1f, 0f, false),
        new("pvc", "PVC 门帘", new(PartCatalog.PvcCurtain), new PanelParams(1.2f, 2.2f, 0.04f), 1.2f, 2.2f, 0f, false),
    ]);

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
