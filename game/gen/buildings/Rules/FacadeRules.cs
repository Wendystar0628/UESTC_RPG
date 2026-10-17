using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Buildings.Rules;

/// <summary>Building families the generic generator knows (data key: WorldFeature.Family, else the source category).</summary>
public enum BuildingStyle : byte { Academic, Dormitory, Research, Library, Sports, Service, Canteen, Residential, Utility, Medical, Modern }

public enum RoofEdge : byte
{
    /// <summary>Projecting cornice with a drip lip, then a set-back parapet with coping.</summary>
    Cornice,
    /// <summary>Wall runs up as the parapet; coping cap plus a top-floor colour band.</summary>
    Parapet,
}

/// <summary>Window card types (WLD-INTERIORS), written to the glass spare byte. 0 = unspecified.</summary>
public enum WindowCard : byte
{
    None = 0, Classroom = 1, Lecture = 2, Corridor = 3, Stair = 4, Office = 5, Lab = 6, Clean = 7, Dorm = 8,
    Toilet = 9, Canteen = 10, Shop = 11, Medical = 12, Gym = 13, Pool = 14, Mechanical = 15,
}

/// <summary>
/// Light channel slots (32-slot light_channels texture). Provisional until the contract LightChannelCatalog lands
/// (agreed with WP-39): general 0, dorm 1, teach 2, library 3, lab 4, canteen 5, allnight 7, cafe 8, venue 9, gym 10.
/// </summary>
public static class LightSlots
{
    public const int General = 0;
    public const int Dorm = 1;
    public const int Teach = 2;
    public const int Library = 3;
    public const int Lab = 4;
    public const int Canteen = 5;
    public const int AllNight = 7;
    public const int Cafe = 8;
    public const int Venue = 9;
    public const int Gym = 10;

    public static readonly (int Slot, string Name)[] Declared =
    [
        (General, "window.general"), (Dorm, "window.dorm"), (Teach, "window.teach"), (Library, "window.library"),
        (Lab, "window.lab"), (Canteen, "window.canteen"), (AllNight, "window.allnight"), (Cafe, "window.cafe"),
        (Venue, "window.venue"), (Gym, "window.gym"),
    ];
}

/// <summary>
/// Facade rule set of one family (MAP-QSH-ADDITIONS, ART-MODELING). All lengths in metres.
/// Stylised proportions: deep reveals, thick frames and sills, chunky cornices; human-scale anchors stay real.
/// </summary>
public sealed record FacadeProfile(
    BuildingStyle Style,
    float StoreyHeight,
    float Bay,
    float Glazing,
    float WindowHeight,
    float Sill,
    float GroundSill,
    RoofEdge RoofEdge,
    string Wall,
    string Trim,
    string Plinth,
    string Roof,
    string? Accent,
    int Channel,
    WindowCard Card)
{
    public float Reveal { get; init; } = 0.3f;
    /// <summary>Windows wider than this get two or more light columns (OpeningRules.Grid).</summary>
    public float MullionFrom { get; init; } = 1.35f;
    /// <summary>Windows taller than OpeningRules.TransomFrom get a transom row.</summary>
    public bool Transom { get; init; }
    /// <summary>Pilaster every N piers on long facades; 0 = none.</summary>
    public int PilasterEvery { get; init; }
    public bool AcGrilles { get; init; }
    public bool StringCourses { get; init; } = true;
    /// <summary>Spandrel and lintel cells of window columns take the accent colour (dormitory "窗间板").</summary>
    public bool AccentSpandrels { get; init; }
    /// <summary>The accent takes only the apron under each window (floor to sill, as wide as the window); the lintel stays wall.</summary>
    public bool AccentApron { get; init; }
    public float PlinthProjection { get; init; } = 0.05f;
    /// <summary>Secondary mix for the window cards: (card, weight) by window hash.</summary>
    public (WindowCard Card, int Weight)[] CardMix { get; init; } = [];
    /// <summary>WP-10 part (PartCatalog ID) filling the window openings; doors take <see cref="PartCatalog.Door"/>.</summary>
    public string Window { get; init; } = PartCatalog.WindowCasement;
    /// <summary>Room windows get WP-10 curtains (OpeningPlacement.Curtain picks which, and their state and colour).</summary>
    public bool Curtains { get; init; } = true;
    /// <summary>Colour family of the window and door glass.</summary>
    public string Glass { get; init; } = Families.Glass;
    /// <summary>The facade preset this profile carries (FacadePresets), null for the family's own.</summary>
    public string? Preset { get; init; }
}

public static class FacadeRules
{
    // Dormitory variants by cluster: warm white with pink-sand spandrels, or warm yellow with brown spandrels.
    private static readonly FacadeProfile DormA = new(BuildingStyle.Dormitory, 3.3f, 3.6f, 0.5f, 1.7f, 1.0f, 1.0f, RoofEdge.Parapet,
        Families.WallWarmWhite, Families.WallGreyWhite, Families.BaseWarmBrown, Families.RoofGrey, Families.PanelPinkSand, LightSlots.Dorm, WindowCard.Dorm)
    { AcGrilles = true, AccentSpandrels = true, Transom = true, MullionFrom = 1.2f };
    private static readonly FacadeProfile DormB = DormA with { Wall = Families.WallDormYellow, Trim = Families.WallWarmWhite, Accent = Families.PanelBrown };

    private static readonly FacadeProfile Academic = new(BuildingStyle.Academic, 3.9f, 4.2f, 0.56f, 2.0f, 0.9f, 1.0f, RoofEdge.Cornice,
        Families.WallGreyWhite, Families.StoneBeige, Families.RoofGrey, Families.RoofGrey, null, LightSlots.Teach, WindowCard.Classroom)
    { PilasterEvery = 3, Transom = true, CardMix = [(WindowCard.Classroom, 8), (WindowCard.Office, 1), (WindowCard.Lecture, 1)] };

    private static readonly FacadeProfile Research = new(BuildingStyle.Research, 4.2f, 3.9f, 0.62f, 2.1f, 0.85f, 1.0f, RoofEdge.Cornice,
        Families.StoneBeige, Families.WallGreyWhite, Families.RoofGrey, Families.RoofGrey, null, LightSlots.Lab, WindowCard.Lab)
    { PilasterEvery = 2, CardMix = [(WindowCard.Lab, 7), (WindowCard.Office, 3)] };

    private static readonly FacadeProfile Library = new(BuildingStyle.Library, 4.5f, 3.0f, 0.45f, 2.6f, 0.8f, 0.9f, RoofEdge.Cornice,
        Families.StoneBeige, Families.WallGreyWhite, Families.RoofGrey, Families.RoofGrey, null, LightSlots.Library, WindowCard.Office)
    { PilasterEvery = 2 };

    private static readonly FacadeProfile Sports = new(BuildingStyle.Sports, 8f, 4.5f, 0.78f, 1.8f, 0.6f, 0.6f, RoofEdge.Cornice,
        Families.WallGreyWhite, Families.WallWarmWhite, Families.BaseWarmBrown, Families.RoofSilverMetal, null, LightSlots.Gym, WindowCard.Gym)
    { Window = PartCatalog.WindowRibbon, Curtains = false };

    private static readonly FacadeProfile Service = new(BuildingStyle.Service, 3.6f, 3.8f, 0.6f, 1.9f, 0.9f, 1.0f, RoofEdge.Parapet,
        Families.WallGreyWhite, Families.WallWarmWhite, Families.RoofGrey, Families.RoofGrey, null, LightSlots.General, WindowCard.Office)
    { CardMix = [(WindowCard.Office, 6), (WindowCard.Mechanical, 2), (WindowCard.Shop, 2)] };

    private static readonly FacadeProfile Canteen = new(BuildingStyle.Canteen, 4.5f, 4.2f, 0.7f, 2.4f, 0.6f, 0.75f, RoofEdge.Cornice,
        Families.TileCanteen, Families.WallWarmWhite, Families.BaseWarmBrown, Families.RoofGrey, null, LightSlots.Canteen, WindowCard.Canteen)
    { Window = PartCatalog.WindowRibbon };

    private static readonly FacadeProfile Residential = new(BuildingStyle.Residential, 3.0f, 3.4f, 0.45f, 1.6f, 0.95f, 1.0f, RoofEdge.Parapet,
        Families.WallCreamCoffee, Families.WallWarmWhite, Families.BaseWarmBrown, Families.RoofGrey, Families.BandTerracotta, LightSlots.Dorm, WindowCard.Dorm)
    { AcGrilles = true, AccentSpandrels = true, MullionFrom = 1.2f };

    private static readonly FacadeProfile Utility = new(BuildingStyle.Utility, 4.0f, 4.6f, 0.38f, 1.2f, 1.6f, 1.6f, RoofEdge.Parapet,
        Families.WallGreyWhite, Families.WallWarmWhite, Families.RoofGrey, Families.RoofGrey, null, LightSlots.General, WindowCard.Mechanical)
    { StringCourses = false, Window = PartCatalog.WindowHigh, Curtains = false };

    private static readonly FacadeProfile Medical = Academic with
    {
        Style = BuildingStyle.Medical,
        Wall = Families.WallWarmWhite,
        Trim = Families.WallGreyWhite,
        Channel = LightSlots.AllNight,
        Card = WindowCard.Medical,
        CardMix = [(WindowCard.Medical, 8), (WindowCard.Office, 2)],
        PilasterEvery = 0,
    };

    private static readonly FacadeProfile Modern = Research with
    {
        Style = BuildingStyle.Modern,
        Wall = Families.WallGreyWhite,
        Trim = Families.WallWarmWhite,
        Glazing = 0.72f,
        WindowHeight = 2.3f,
        Channel = LightSlots.Teach,
        Card = WindowCard.Office,
        PilasterEvery = 0,
        CardMix = [(WindowCard.Classroom, 6), (WindowCard.Office, 4)],
        Window = PartCatalog.CurtainWall,
        Curtains = false,
    };

    /// <summary>
    /// Family from WorldFeature.Family (open vocabulary), else from the source category: the part's own record
    /// first (a building part under a poi entity), then the entity's.
    /// </summary>
    public static BuildingStyle StyleOf(BuildingFamily? familyKey, JsonElement? partProps, JsonElement? featureProps)
    {
        if (familyKey is { Value: { Length: > 0 } family })
        {
            var byFamily = family switch
            {
                "academic" or "teaching" or "office" or "administration" => BuildingStyle.Academic,
                "dormitory" or "dorm" => BuildingStyle.Dormitory,
                "research" or "lab" => BuildingStyle.Research,
                "library" => BuildingStyle.Library,
                "sports" or "venue" or "gym" => BuildingStyle.Sports,
                "service" or "shop" => BuildingStyle.Service,
                "canteen" or "dining" => BuildingStyle.Canteen,
                "residential" => BuildingStyle.Residential,
                "utility" => BuildingStyle.Utility,
                "medical" => BuildingStyle.Medical,
                "modern" => BuildingStyle.Modern,
                _ => (BuildingStyle?)null,
            };
            if (byFamily is { } known) return known;
        }
        var category = Category(partProps) ?? Category(featureProps);
        return category switch
        {
            "residence" => BuildingStyle.Dormitory,
            "teaching" or "administration" => BuildingStyle.Academic,
            "research" => BuildingStyle.Research,
            "library" => BuildingStyle.Library,
            "sport" => BuildingStyle.Sports,
            "dining" => BuildingStyle.Canteen,
            "service" => BuildingStyle.Service,
            "medical" => BuildingStyle.Medical,
            _ => BuildingStyle.Utility,
        };
    }

    public static string? Category(JsonElement? props)
    {
        if (props is not { ValueKind: JsonValueKind.Object } p) return null;
        return p.TryGetProperty("category", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
    }

    /// <summary>
    /// Profile for a building. clusterHash groups neighbouring buildings (same 160 m cell) so that
    /// families differ between groups, never inside one building (ART-MODELING).
    /// </summary>
    public static FacadeProfile ProfileFor(BuildingStyle style, uint clusterHash) => style switch
    {
        BuildingStyle.Dormitory => (clusterHash & 1u) == 0u ? DormA : DormB,
        BuildingStyle.Academic => Academic,
        BuildingStyle.Research => Research,
        BuildingStyle.Library => Library,
        BuildingStyle.Sports => Sports,
        BuildingStyle.Service => Service,
        BuildingStyle.Canteen => Canteen,
        BuildingStyle.Residential => Residential,
        BuildingStyle.Utility => Utility,
        BuildingStyle.Medical => Medical,
        BuildingStyle.Modern => Modern,
        _ => throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown building style."),
    };
}
