namespace Campus.Style.Shaders.Cards;

/// <summary>
/// Window card type (WLD-INTERIORS). The numeric value is the VF-B glass window-type byte (COLOR's fourth byte)
/// and the row of the window_cards table. 1–15 are the contract's card list; 16 is the door lobby card of closed
/// entrances (WLD-INTERIORS), proposed to VF-B in the 51b contract request.
/// </summary>
public enum WindowCardKind : byte
{
    None = 0,
    Classroom = 1,
    Lecture = 2,
    Corridor = 3,
    Stair = 4,
    Office = 5,
    Lab = 6,
    Clean = 7,
    Dorm = 8,
    Toilet = 9,
    Canteen = 10,
    Shop = 11,
    Medical = 12,
    Gym = 13,
    Pool = 14,
    Mechanical = 15,
    Lobby = 16,
}

/// <summary>Silhouette drawn on a plane parallel to the window (near layer) or on the back wall (far layer).</summary>
public enum CardFurniture : byte
{
    None = 0,
    /// <summary>Rows of desks with chair backs (classroom, lecture hall).</summary>
    Desks = 1,
    /// <summary>Lab benches with a reagent shelf above.</summary>
    Bench = 2,
    /// <summary>Tall cabinets or shelves with shelf lines (office, wardrobe, archive).</summary>
    Shelf = 3,
    /// <summary>Loft bed over desk ("上床下桌"), steel frame, bed curtain.</summary>
    Bunk = 4,
    /// <summary>Cubicle partitions with a gap below (toilet).</summary>
    Stall = 5,
    /// <summary>Joined canteen tables and stools.</summary>
    Tables = 6,
    /// <summary>Serving counter with a light box strip above (canteen, shop).</summary>
    Counter = 7,
    /// <summary>Equipment cabinets and a horizontal pipe run (plant room, clean room).</summary>
    Equip = 8,
    /// <summary>A stair flight crossing the window with its handrail.</summary>
    Stair = 9,
    /// <summary>Face-recognition turnstiles (lobby card).</summary>
    Turnstile = 10,
    /// <summary>A pair of elevator doors in a stone surround with a floor indicator (lobby card).</summary>
    Elevator = 11,
}

/// <summary>What the side walls carry.</summary>
public enum CardSide : byte
{
    None = 0,
    /// <summary>Sliding blackboard with projection screen (classroom side wall, WLD-INTERIORS).</summary>
    Board = 1,
    /// <summary>Loft beds along both side walls (4-person dorm).</summary>
    Bunk = 2,
    /// <summary>Door rhythm along a corridor.</summary>
    Doors = 3,
    /// <summary>Shelving along the walls.</summary>
    Shelf = 4,
}

/// <summary>What sits on the glass plane.</summary>
public enum CardScreen : byte
{
    None = 0,
    /// <summary>Fabric curtains bunched at one or both sides; coverage = drawn fraction of the width.</summary>
    SideCurtain = 1,
    /// <summary>Venetian blinds lowered from the head; coverage = lowered fraction of the height.</summary>
    Venetian = 2,
    /// <summary>Frosted film on the lower part; coverage = frosted fraction of the height.</summary>
    Frosted = 3,
    /// <summary>Opaque roller blind lowered from the head; coverage = lowered fraction of the height.</summary>
    Roller = 4,
}

public enum CardCeiling : byte
{
    None = 0,
    /// <summary>Fluorescent strips running away from the window.</summary>
    Strips = 1,
    /// <summary>600 mm mineral-fibre grid with recessed panel lights.</summary>
    Panels = 2,
    /// <summary>One central fitting.</summary>
    Single = 3,
    /// <summary>A row of downlights along the depth.</summary>
    Downlights = 4,
    /// <summary>Exposed roof trusses with high-bay lamps (gym, pool).</summary>
    Truss = 5,
}

/// <summary>A small signature element; most also keep a lit room off "dead black" at night (WLD-INTERIORS).</summary>
public enum CardAccent : byte
{
    None = 0,
    /// <summary>"安全出口" green sign over the far door, glowing all night.</summary>
    Exit = 1,
    /// <summary>Clean-room amber light (only a few panes campus-wide).</summary>
    Amber = 2,
    /// <summary>Coloured bed curtains glowing warm at night.</summary>
    BedCurtain = 3,
    /// <summary>Warm light boxes over the serving windows.</summary>
    LightBox = 4,
    /// <summary>Pool water glow on the floor.</summary>
    Water = 5,
    /// <summary>Equipment indicator dots at night.</summary>
    Indicator = 6,
    /// <summary>Lobby card: turnstile standby green lamps, floor index board, elevator indicator.</summary>
    Lobby = 7,
}
