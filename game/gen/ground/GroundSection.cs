using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Ground;

/// <summary>
/// Street cross-section and edge dimensions shared by every ground surface (ART-MODELING 7.8, ART-FIDELITY 3.5).
/// All values are metres. Heights are relative to the carriageway surface, which sits at y = 0.
/// This record is the reusable part that is later promoted to the part kernel for outskirts O0 (WLD-OUTSKIRTS 12.2);
/// keep it free of map data so that a road cross-section can be rebuilt from parameters alone.
/// </summary>
public sealed record GroundSection
{
    /// <summary>Kerb stone top and the flush paving level (sidewalks, plazas, courts).</summary>
    public float PavingY { get; init; } = GroundLevels.Paving;

    /// <summary>Lawn level: 5 cm below paving so that every lawn edge reads as a small step.</summary>
    public float GrassY { get; init; } = GroundLevels.Lawn;

    /// <summary>Carriageway and parking level.</summary>
    public float RoadY { get; init; }

    /// <summary>Kerb stone width measured from the carriageway edge.</summary>
    public float CurbWidth { get; init; } = 0.15f;

    /// <summary>Single-segment 45 degree chamfer on the kerb front edge (R 10-20 mm is baseline B3).</summary>
    public float CurbBevel { get; init; } = 0.015f;

    /// <summary>Granite edging band on the paving side of every paving/lawn boundary.</summary>
    public float EdgingWidth { get; init; } = 0.12f;

    /// <summary>Darker paver border just inside every plaza (ART-MODELING 7.8 edge band at plaza scale).</summary>
    public float PlazaBorder { get; init; } = 0.5f;

    /// <summary>Bank faces at water edges drop to this level; the water surface itself belongs to WP-24.</summary>
    public float BankBottomY { get; init; } = -0.6f;

    /// <summary>Lift of decal geometry (markings) above the surface it lies on.</summary>
    public float DecalLift { get; init; } = GroundLevels.Decal;

    public MarkingSpec Markings { get; init; } = new();

    public PitchSpec Pitch { get; init; } = new();

    public PitSpec Pits { get; init; } = new();

    public LawnTone Lawn { get; init; } = LawnTone.Default;

    public static GroundSection Default { get; } = new();

    /// <summary>Largest field threshold used by the surface classifier; distance fields must be exact up to here.</summary>
    internal float MaxIso => MathF.Max(MathF.Max(CurbWidth, EdgingWidth), Lawn.EdgeBand);
}

/// <summary>
/// Tree pits and planting strips (overlay kind tree-pit): a granite frame of the authored thickness round a sunken soil
/// face. The ground carves the pit out of whatever paving or lawn surrounds it: paving stops at the frame's outer face
/// and nothing lies over the soil. Heights are measured from the paving surface (the reference every pit stands on):
/// a single pit's frame stands <see cref="PitRimAbovePaving"/> above it, a planting strip's kerb
/// <see cref="StripRimAbovePaving"/>, and the soil lies <see cref="SoilBelowRim"/> below the frame top.
/// </summary>
public sealed record PitSpec
{
    /// <summary>
    /// Reference of the authored heights when <see cref="UseAuthoredHeights"/> is on: null = the paving surface
    /// (<see cref="GroundSection.PavingY"/>); 0 would measure from the carriageway level of the terrain.
    /// </summary>
    public float? Datum { get; init; }

    /// <summary>
    /// False (design heights): every pit and strip takes the heights below and the record's <c>height</c> and
    /// <c>soilAbove</c> are not shown. True: the record's own heights are drawn, from <see cref="Datum"/>.
    /// </summary>
    public bool UseAuthoredHeights { get; init; }

    /// <summary>Frame top above the paving: a single pit, and a planting strip's kerb (m).</summary>
    public float PitRimAbovePaving { get; init; } = 0.20f;
    public float StripRimAbovePaving { get; init; } = 0.15f;

    /// <summary>A shape whose bounding box is at most this wide both ways is a single pit; a longer one is a planting strip (m).</summary>
    public float CompactSide { get; init; } = 4f;

    /// <summary>Soil below the frame top, and the least it may lie below it (m).</summary>
    public float SoilBelowRim { get; init; } = 0.05f;
    public float MinSoilBelowRim { get; init; } = 0.03f;

    /// <summary>Frame thickness when the record gives none, and the range it is held to (the soil must stay a face, the bevels must fit).</summary>
    public float DefaultKerb { get; init; } = 0.15f;
    public float MinKerb { get; init; } = 0.06f;
    public float MaxKerb { get; init; } = 0.6f;
}

/// <summary>Road marking dimensions (ART-AMBIENCE 6, GB 5768 proportions, stylised).</summary>
public sealed record MarkingSpec
{
    /// <summary>Carriageways at least this wide get a dashed centre line.</summary>
    public float CentreLineMinWidth { get; init; } = 5.8f;
    public float CentreLineWidth { get; init; } = 0.15f;
    public float DashLength { get; init; } = 2f;
    public float DashGap { get; init; } = 4f;

    /// <summary>Zebra stripes run parallel to the carriageway; the crossing is this long along the road.</summary>
    public float ZebraLength { get; init; } = 4f;
    public float ZebraStripe { get; init; } = 0.45f;
    public float ZebraGap { get; init; } = 0.6f;

    /// <summary>Stripes stop this far inside the carriageway edge.</summary>
    public float ZebraMargin { get; init; } = 0.35f;

    /// <summary>Junction arms of carriageways at least this wide get a crossing.</summary>
    public float ZebraJunctionMinWidth { get; init; } = 5.8f;

    /// <summary>Extra setback of a junction crossing beyond the widest arm's half width.</summary>
    public float ZebraSetback { get; init; } = 2.5f;
}

/// <summary>
/// Football pitch markings: the proportions of the Laws of the Game for a 105 x 68 m pitch, scaled to the size of the
/// pitch they are drawn on (lengths along it by its length, widths across it by its width, circles by the mean).
/// </summary>
public sealed record PitchSpec
{
    /// <summary>Line width; the Laws allow up to 0.12 m, drawn a little wider like the road markings so it reads from the air.</summary>
    public float LineWidth { get; init; } = 0.15f;

    /// <summary>Gap between the edging band along the pitch boundary and the outer line.</summary>
    public float EdgeClear { get; init; } = 0.03f;

    /// <summary>Pitches narrower than this are left unmarked, as are those outside the proportions below.</summary>
    public float MinShortSide { get; init; } = 20f;
    public float MinAspect { get; init; } = 1.1f;
    public float MaxAspect { get; init; } = 2.5f;

    /// <summary>The reference pitch the dimensions below belong to.</summary>
    public float RefLength { get; init; } = 105f;
    public float RefWidth { get; init; } = 68f;
    public float PenaltyDepth { get; init; } = 16.5f;
    public float PenaltyWidth { get; init; } = 40.32f;
    public float GoalDepth { get; init; } = 5.5f;
    public float GoalWidth { get; init; } = 18.32f;
    public float SpotDistance { get; init; } = 11f;
    public float CircleRadius { get; init; } = 9.15f;
    public float CornerRadius { get; init; } = 1f;

    /// <summary>Side of the square that stands for each spot (kick-off, penalties).</summary>
    public float SpotSize { get; init; } = 0.3f;

    /// <summary>Chord length of the circles and arcs.</summary>
    public float Chord { get; init; } = 1f;
}

/// <summary>
/// Tile extents and lattice resolution per level of detail. The tile key of a level indexes cells of 64 &lt;&lt; lod metres
/// (floor(world / (64 &lt;&lt; lod))); L4 is a single tile (0, 0) covering the map's data rectangle.
/// Every tile is sampled with a power-of-two number of cells.
/// </summary>
public static class GroundLod
{
    /// <summary>
    /// Fixed vertical range of every ground tile's position quantisation, metres (D-180): the packer uses
    /// [MinY, MaxY] for y in all ground tiles so that neighbouring tiles quantise heights with the same step.
    /// </summary>
    public const float MinY = -5f, MaxY = 10f;

    /// <summary>World rectangle and cell size of one tile at the given level.</summary>
    public static GroundTile Tile(Contracts.TileKey tile, Contracts.LodLevel lod, Contracts.Maps.MapRect map)
    {
        ArgumentNullException.ThrowIfNull(map);
        switch (lod)
        {
            case Contracts.LodLevel.L0:
            case Contracts.LodLevel.L1:
            case Contracts.LodLevel.L2:
            case Contracts.LodLevel.L3:
                var size = (float)TileGrid.Edge(lod);
                var cells = lod == Contracts.LodLevel.L0 ? 256 : 128;
                return new(tile.X * size, tile.Z * size, size, size / cells);
            case Contracts.LodLevel.L4:
                if (tile.X != 0 || tile.Z != 0) throw new ArgumentOutOfRangeException(nameof(tile), tile, "L4 has the single tile (0, 0).");
                var extent = MathF.Max(map.Max.X - map.Min.X, map.Max.Y - map.Min.Y);
                return new(map.Min.X, map.Min.Y, extent, extent / 256f);
            default:
                throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown level of detail.");
        }
    }

    /// <summary>Kerb chamfer is only resolved where the chamfer is wider than a fraction of a pixel.</summary>
    public static bool HasBevel(Contracts.LodLevel lod) => lod switch
    {
        Contracts.LodLevel.L0 or Contracts.LodLevel.L1 => true,
        Contracts.LodLevel.L2 or Contracts.LodLevel.L3 or Contracts.LodLevel.L4 => false,
        _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown level of detail.")
    };

    /// <summary>Kerb, edging and lawn-edge bands are dropped where they are thinner than a pixel (L2 and coarser).</summary>
    public static bool HasBands(Contracts.LodLevel lod) => lod switch
    {
        Contracts.LodLevel.L0 or Contracts.LodLevel.L1 => true,
        Contracts.LodLevel.L2 or Contracts.LodLevel.L3 or Contracts.LodLevel.L4 => false,
        _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown level of detail.")
    };

    /// <summary>
    /// Lattice on which lawn tone contours are resolved (metres); 0 means no tones at this level. Tone patches are
    /// broad, so a coarse contour is enough and keeps lawns cheap; beyond L2 fog carries the distance.
    /// </summary>
    public static float ToneCell(Contracts.LodLevel lod) => lod switch
    {
        Contracts.LodLevel.L0 => 2f,
        Contracts.LodLevel.L1 => 4f,
        Contracts.LodLevel.L2 => 8f,
        Contracts.LodLevel.L3 or Contracts.LodLevel.L4 => 0f,
        _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown level of detail.")
    };

    /// <summary>
    /// Height steps modelled per level: 2 = full cross-section (kerb 0.15 m, lawn 5 cm below paving); 1 = kerb only,
    /// lawns flush with paving; 0 = one continuous surface (steps are sub-pixel beyond 150 m). Water banks always.
    /// </summary>
    public static int Steps(Contracts.LodLevel lod) => lod switch
    {
        Contracts.LodLevel.L0 => 2,
        Contracts.LodLevel.L1 => 1,
        Contracts.LodLevel.L2 or Contracts.LodLevel.L3 or Contracts.LodLevel.L4 => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown level of detail.")
    };

    public static bool HasMarkings(Contracts.LodLevel lod) => lod switch
    {
        Contracts.LodLevel.L0 or Contracts.LodLevel.L1 => true,
        Contracts.LodLevel.L2 or Contracts.LodLevel.L3 or Contracts.LodLevel.L4 => false,
        _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown level of detail.")
    };
}

/// <summary>One ground tile: world origin (x, z), side length and lattice cell size, all in metres.</summary>
public readonly record struct GroundTile(float X0, float Z0, float Size, float Cell)
{
    public int Cells => (int)MathF.Round(Size / Cell);
}
