namespace Campus.Gen.Water;

/// <summary>
/// Tunable constants of the water generator, kept in one place for review (WF-FLOW §4).
/// Shader-side look parameters live at the top of game/style/shaders/water.gdshader.
/// </summary>
public sealed record WaterParams
{
    public static WaterParams Default { get; } = new();

    /// <summary>Grid step of the surface mesh in metres. Must divide <see cref="TileSize"/>.</summary>
    public float MeshStep { get; init; } = 3.2f;

    /// <summary>Streaming cell edge (CORE-CONVENTIONS §1.3: 64 m quadtree cells).</summary>
    public float TileSize { get; init; } = 64f;

    /// <summary>
    /// Normal water level below the bank top (m), per water body, until ITerrain.WaterY (contract-15, D-180) lands:
    /// lakes and ponds have soft banks (bank − 0.5 m); channels run between revetments with railings (bank − 0.6 m).
    /// Per-segment channel levels and the steps at weirs, sluices and culvert mouths come from MAP-QSH-TERRAIN.
    /// </summary>
    public float LakeDrop { get; init; } = 0.5f;

    /// <summary>See <see cref="LakeDrop"/>.</summary>
    public float ChannelDrop { get; init; } = 0.6f;

    /// <summary>Lakes sit this much above channels so overlapping OSM polygons never z-fight.</summary>
    public float LakeLift { get; init; } = 0.004f;

    /// <summary>Shore distances are clamped here; bands end at 9 m (WLD-WATER §1.1).</summary>
    public float SdfClamp { get; init; } = 24f;

    /// <summary>An edge is not shore when the water continues past it (inlets, joints of OSM river pieces).</summary>
    public float OpenEdgeProbe { get; init; } = 0.75f;

    /// <summary>Other bodies closer than this contribute their shore edges to the distance field.</summary>
    public float NeighbourReach { get; init; } = 30f;

    /// <summary>Isoperimetric ratio P²/(4πA) above which a water body counts as a channel.</summary>
    public float ChannelElongation { get; init; } = 5f;

    /// <summary>Set flow speed of all channels (WLD-WATER §4).</summary>
    public float FlowSpeed { get; init; } = 0.45f;

    /// <summary>Grid lines closer than this fraction of a step to a ring vertex or band edge are skipped.</summary>
    public float SliverFraction { get; init; } = 0.15f;

    /// <summary>Screen-area estimate samples per reflection candidate lake.</summary>
    public int AreaSamples { get; init; } = 48;
}
