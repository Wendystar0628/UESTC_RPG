namespace Campus.Style.Shaders.Cards;

/// <summary>
/// The 15 window cards and the lobby card (WLD-INTERIORS). This is the single place to tune what a closed
/// building shows behind its glass; the shader only interprets these numbers (window_cards table, WindowCardTable).
/// Reading cues by day: curtains or blinds catching daylight, ceiling fittings, a side wall or partition. By night the
/// lit rooms differ by depth, curtain state, fittings and silhouettes, never one uniform sheet (SYS-TIME decides
/// which windows are lit).
/// </summary>
public static class WindowCardCatalog
{
    /// <summary>Layers per card in the detail version's Texture2DArray: 2 back walls, 2 silhouettes, 1 screen.</summary>
    public const int LayersPerCard = 5;
    public const int LobbyLayers = 6;

    /// <summary>All cards ordered by kind; index i is kind i + 1.</summary>
    public static IReadOnlyList<WindowCardSpec> All { get; } = Build();

    public static WindowCardSpec Get(WindowCardKind kind) => kind switch
    {
        >= WindowCardKind.Classroom and <= WindowCardKind.Lobby => All[(int)kind - 1],
        WindowCardKind.None => throw new ArgumentOutOfRangeException(nameof(kind), "Card 0 is 'unspecified'; the shader picks Office."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static WindowCardSpec[] Build()
    {
        var cards = new[]
        {
            CreateClassroom(),
            CreateLecture(),
            CreateCorridor(),
            CreateStair(),
            CreateOffice(),
            CreateLab(),
            CreateClean(),
            CreateDorm(),
            CreateToilet(),
            CreateCanteen(),
            CreateShop(),
            CreateMedical(),
            CreateGym(),
            CreatePool(),
            CreateMechanical(),
            CreateLobby(),
        };
        for (var i = 0; i < cards.Length; i++)
        {
            var kind = (WindowCardKind)(i + 1);
            if (cards[i].Kind != kind) throw new InvalidOperationException($"Window card {i} is {cards[i].Kind}, expected {kind}.");
            cards[i] = cards[i] with
            {
                LayerBase = i * LayersPerCard,
                LayerCount = kind == WindowCardKind.Lobby ? LobbyLayers : LayersPerCard,
            };
        }
        return cards;
    }

    // W-CLS: deep, wide room; strips across the ceiling, blackboard on the side wall, desk rows, curtains bunched.
    private static WindowCardSpec CreateClassroom() => new(WindowCardKind.Classroom)
    {
        Depth45 = 0.3f, Depth6 = 0.7f, SideExtra = 3.2f, Near = CardFurniture.Desks, NearDepth = 2.2f,
        Far = CardFurniture.Shelf, Side = CardSide.Board, Screen = CardScreen.SideCurtain, ScreenChance = 0.75f,
        CoverMin = 0.12f, CoverMax = 0.32f, DayLum = 1f, NightGain = 1f, Ceiling = CardCeiling.Strips, CoolShare = 0.75f,
    };

    // W-LEC: lecture hall; tiered desks, heavier curtains, panel ceiling.
    private static WindowCardSpec CreateLecture() => new(WindowCardKind.Lecture)
    {
        Depth6 = 1f, SideExtra = 3.6f, SillDrop = 1.1f, CeilingRise = 0.9f, Near = CardFurniture.Desks, NearDepth = 1.8f,
        Side = CardSide.Board, Screen = CardScreen.SideCurtain, ScreenChance = 0.85f, CoverMin = 0.2f, CoverMax = 0.55f,
        DayLum = 0.9f, NightGain = 0.95f, Ceiling = CardCeiling.Panels, CoolShare = 0.6f,
    };

    // W-COR: corridor end window looking down the corridor; door rhythm on both sides, exit sign at the far end.
    private static WindowCardSpec CreateCorridor() => new(WindowCardKind.Corridor)
    {
        Depth6 = 1f, SideExtra = 0.25f, Side = CardSide.Doors, Ceiling = CardCeiling.Downlights,
        Accent = CardAccent.Exit, DayLum = 0.92f, NightGain = 0.9f, CoolShare = 0.85f,
    };

    // W-STR: stair flight crossing the window, landing lamp, exit sign.
    private static WindowCardSpec CreateStair() => new(WindowCardKind.Stair)
    {
        Depth3 = 1f, SideExtra = 0.3f, SillDrop = 1.2f, Near = CardFurniture.Stair, NearDepth = 1.1f,
        Ceiling = CardCeiling.Single, Accent = CardAccent.Exit, DayLum = 0.95f, NightGain = 0.85f, CoolShare = 0.9f,
    };

    // W-OFF: narrow room, desk and shelves, venetian blinds at varied heights, partitions between offices.
    private static WindowCardSpec CreateOffice() => new(WindowCardKind.Office)
    {
        Depth3 = 0.45f, Depth45 = 0.55f, Partition = 0.3f, SideExtra = 0.45f, Near = CardFurniture.Desks, NearDepth = 1.4f,
        Far = CardFurniture.Shelf, Side = CardSide.Shelf, Screen = CardScreen.Venetian, ScreenChance = 0.6f,
        CoverMin = 0.15f, CoverMax = 0.7f, DayLum = 1f, NightGain = 0.95f, Ceiling = CardCeiling.Panels, CoolShare = 0.6f,
    };

    // W-LAB: benches with reagent shelves, roller blinds, cool panels.
    private static WindowCardSpec CreateLab() => new(WindowCardKind.Lab)
    {
        Depth3 = 0.2f, Depth45 = 0.5f, Depth6 = 0.3f, Partition = 0.2f, SideExtra = 0.9f, Near = CardFurniture.Bench,
        NearDepth = 1.7f, Far = CardFurniture.Shelf, Screen = CardScreen.Roller, ScreenChance = 0.5f, CoverMin = 0.1f,
        CoverMax = 0.45f, DayLum = 1.02f, NightGain = 1f, Ceiling = CardCeiling.Panels, CoolShare = 0.85f,
    };

    // W-CLEAN: clean room, amber light (keep to two or three panes campus-wide via the facade mix).
    private static WindowCardSpec CreateClean() => new(WindowCardKind.Clean)
    {
        Depth3 = 0.5f, Depth45 = 0.5f, SideExtra = 0.9f, Near = CardFurniture.Equip, NearDepth = 1.6f,
        Far = CardFurniture.Equip, Ceiling = CardCeiling.Panels, Accent = CardAccent.Amber, DayLum = 0.95f,
        NightGain = 0.9f, CoolShare = 0f,
    };

    // W-DORM: 4-person room, loft beds along the side walls, wardrobes at the back, curtains anywhere from open
    // two-thirds drawn, bed curtains glowing at night.
    private static WindowCardSpec CreateDorm() => new(WindowCardKind.Dorm)
    {
        Depth3 = 0.35f, Depth45 = 0.65f, SideExtra = 0.22f, SillDrop = 1f, Far = CardFurniture.Shelf, Side = CardSide.Bunk,
        Screen = CardScreen.SideCurtain, ScreenChance = 0.75f, CoverMin = 0.1f, CoverMax = 0.6f, DayLum = 0.95f,
        NightGain = 1f, Ceiling = CardCeiling.Single, Accent = CardAccent.BedCurtain, CoolShare = 0.35f,
    };

    // W-WC: toilet and boiler room; frosted lower panes, cubicle partitions, one cool fitting.
    private static WindowCardSpec CreateToilet() => new(WindowCardKind.Toilet)
    {
        Depth3 = 1f, SideExtra = 0.4f, SillDrop = 1.4f, Near = CardFurniture.Stall, NearDepth = 1.2f,
        Screen = CardScreen.Frosted, ScreenChance = 1f, CoverMin = 0.55f, CoverMax = 0.72f, DayLum = 1f,
        NightGain = 0.9f, Ceiling = CardCeiling.Single, CoolShare = 1f,
    };

    // W-CAN: canteen hall; tables, serving counter with warm light boxes.
    private static WindowCardSpec CreateCanteen() => new(WindowCardKind.Canteen)
    {
        Depth6 = 1f, SideExtra = 4f, SillDrop = 0.8f, CeilingRise = 1f, Near = CardFurniture.Tables, NearDepth = 2.6f,
        Far = CardFurniture.Counter, Ceiling = CardCeiling.Downlights, Accent = CardAccent.LightBox, DayLum = 1f,
        NightGain = 1.05f, CoolShare = 0.15f,
    };

    // W-SHOP: shelves and counter, bright by day too.
    private static WindowCardSpec CreateShop() => new(WindowCardKind.Shop)
    {
        Depth45 = 0.5f, Depth6 = 0.5f, SideExtra = 1f, SillDrop = 0.4f, Near = CardFurniture.Shelf, NearDepth = 1.6f,
        Far = CardFurniture.Counter, Side = CardSide.Shelf, DayLum = 1.1f, NightGain = 1.1f, Ceiling = CardCeiling.Panels,
        CoolShare = 0.7f,
    };

    // W-MED: clinic rooms; blinds mostly lowered, cabinets, partitions.
    private static WindowCardSpec CreateMedical() => new(WindowCardKind.Medical)
    {
        Depth45 = 1f, Partition = 0.4f, SideExtra = 0.45f, Far = CardFurniture.Shelf, Screen = CardScreen.Venetian,
        ScreenChance = 0.8f, CoverMin = 0.3f, CoverMax = 0.8f, DayLum = 0.98f, NightGain = 0.95f,
        Ceiling = CardCeiling.Panels, CoolShare = 0.9f,
    };

    // W-GYM: high windows into a hall; trusses and high-bay lamps, the far wall a long way off.
    private static WindowCardSpec CreateGym() => new(WindowCardKind.Gym)
    {
        Depth6 = 1f, SideExtra = 8f, SillDrop = 3f, CeilingRise = 2.5f, Ceiling = CardCeiling.Truss, DayLum = 0.88f,
        NightGain = 1f, CoolShare = 0.8f,
    };

    // W-POOL: hall with the water glowing on the floor.
    private static WindowCardSpec CreatePool() => new(WindowCardKind.Pool)
    {
        Depth6 = 1f, SideExtra = 8f, SillDrop = 1.5f, CeilingRise = 2.5f, Ceiling = CardCeiling.Truss,
        Accent = CardAccent.Water, DayLum = 0.92f, NightGain = 1f, CoolShare = 0.9f,
    };

    // W-MECH: plant room; equipment cabinets, a pipe run, louvred blinds, indicator dots at night.
    private static WindowCardSpec CreateMechanical() => new(WindowCardKind.Mechanical)
    {
        Depth3 = 1f, SideExtra = 0.5f, Near = CardFurniture.Equip, NearDepth = 1.1f, Far = CardFurniture.Equip,
        Screen = CardScreen.Venetian, ScreenChance = 0.4f, CoverMin = 0.3f, CoverMax = 0.6f, DayLum = 0.75f,
        NightGain = 0.8f, Ceiling = CardCeiling.Single, Accent = CardAccent.Indicator, CoolShare = 1f,
    };

    // Lobby card (door glass of closed entrances): deeper hall, turnstiles with standby green lamps, floor
    // index board, elevator doors; one step darker than an open lobby (WLD-INTERIORS).
    private static WindowCardSpec CreateLobby() => new(WindowCardKind.Lobby)
    {
        Depth6 = 1f, SideExtra = 1.6f, SillDrop = 0.02f, CeilingRise = 0.8f, Near = CardFurniture.Turnstile,
        NearDepth = 2.4f, Far = CardFurniture.Elevator, Side = CardSide.Shelf, Ceiling = CardCeiling.Downlights,
        Accent = CardAccent.Lobby, DayLum = 0.85f, NightGain = 0.8f, CoolShare = 0.7f,
    };
}
