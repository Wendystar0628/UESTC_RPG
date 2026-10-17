namespace Campus.Gen.Interior.Shell.Cards;

/// <summary>
/// Content of one window card (WLD-INTERIORS): the room the shader paints behind a pane. Everything is chosen per
/// window by hash from these ranges, so a facade reads "rooms of the same kind, not copies". Lengths in metres,
/// luminances are multipliers on the shader's room tone (1 = the card's reference tone), chances in [0, 1].
/// The card never corresponds to a real room (D-028).
/// </summary>
public sealed record WindowCardSpec(WindowCardKind Kind)
{
    /// <summary>Relative weights of the three parallax depths 3, 4.5 and 6 m (WLD-INTERIORS).</summary>
    public float Depth3 { get; init; }
    public float Depth45 { get; init; }
    public float Depth6 { get; init; }
    /// <summary>Chance that a partition wall meets the pane, so it shows two rooms side by side.</summary>
    public float Partition { get; init; }
    /// <summary>Distance from the pane edge to each side wall: narrow rooms (dorm, office) show their side walls.</summary>
    public float SideExtra { get; init; }
    /// <summary>Floor below the pane's bottom edge (sill height above the floor).</summary>
    public float SillDrop { get; init; } = 0.9f;
    /// <summary>Ceiling above the pane's top edge.</summary>
    public float CeilingRise { get; init; } = 0.4f;
    public CardFurniture Near { get; init; }
    /// <summary>Distance of the near silhouette plane from the glass.</summary>
    public float NearDepth { get; init; } = 1.5f;
    public CardFurniture Far { get; init; }
    public CardSide Side { get; init; }
    public CardScreen Screen { get; init; }
    public float ScreenChance { get; init; }
    /// <summary>Coverage range of the screen (meaning per <see cref="CardScreen"/>).</summary>
    public float CoverMin { get; init; }
    public float CoverMax { get; init; }
    /// <summary>Unlit room brightness in daylight relative to the glass tone (rooms read darker than the wall outside).</summary>
    public float DayLum { get; init; } = 1f;
    /// <summary>Lit room brightness relative to the window lamp colour.</summary>
    public float NightGain { get; init; } = 1f;
    public CardCeiling Ceiling { get; init; }
    public CardAccent Accent { get; init; }
    /// <summary>
    /// Share of cool-white lamps for this card. Data only for now: the lamp's warm/cool pick stays the shared hash in
    /// lamp.gdshaderinc (fx_glow and ScheduleMath must change with it, see the 51b contract request).
    /// </summary>
    public float CoolShare { get; init; } = 1f / 3f;
    /// <summary>First layer and layer count in the future Texture2DArray detail version (WLD-INTERIORS, about 84 layers).</summary>
    public int LayerBase { get; init; }
    public int LayerCount { get; init; }
}
