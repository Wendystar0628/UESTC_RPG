using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Zones.Core.Library;

namespace Campus.Gen.Zones.Core.Framework;

/// <summary>
/// One built object or group of the core zone: the time plaza, the sundial, later the library shell and each
/// surrounding block, the museum and the West Lake structures. A piece is a pure function of the world model and
/// its own style constants; it never reads files or world state (ENG-ARCH).
/// <para>
/// Each piece lives in exactly one 64 m L0 cell (its <see cref="Anchor"/>), so a level-l tile is
/// floor(anchor / (64 · 2^l)) and L4 is the whole-map tile (0, 0). Geometry may extend past the cell; the baker
/// merges by tile, not by clipping.
/// </para>
/// </summary>
public interface ICorePiece
{
    /// <summary>Stable lowercase identifier, used for ordering, seeds and test output.</summary>
    string Id { get; }

    /// <summary>Plan point that decides the piece's tile at every level.</summary>
    Vector2 Anchor { get; }

    /// <summary>Features and aspects this piece generates instead of the generic modules (SYS-MAPS).</summary>
    IReadOnlyList<Claim> Claims { get; }

    /// <summary>Visible geometry, placed catalogue parts and light emitters at one level.</summary>
    void Build(CoreBuild build, LodLevel lod);

    /// <summary>Collision primitives with the same parameters as the geometry (SYS-WALK); Foot always filled.</summary>
    IReadOnlyList<Collider> Collide();

    /// <summary>Close-range inspection probes of this piece (ART-FIDELITY); most pieces have none.</summary>
    IReadOnlyList<InspectionProbe> Probes => [];
}

/// <summary>Output of one tile build: shared mesh sink plus placed parts and emitters, in piece order.</summary>
public sealed class CoreBuild(PaletteCatalog palette)
{
    public CoreMesh Mesh { get; } = new(palette);
    public List<PlacedPart> Parts { get; } = [];
    public List<LightEmitter> Emitters { get; } = [];
    /// <summary>Bundles a piece writes itself (VF-B glass, landmark parts kept as their own instances), after the shared sink's.</summary>
    public List<GeometryBundle> Bundles { get; } = [];
    /// <summary>The registry the shared sink colours with, for pieces that write their own bundles.</summary>
    public PaletteCatalog Palette { get; } = palette;
    /// <summary>The map's window allocation (BuildContext.WindowNumbering, map already checked); null only in fixtures.</summary>
    public WindowNumberingSnapshot? WindowNumbering { get; init; }

    /// <summary>
    /// The map's part LOD capabilities (BuildContext.PartLods). Null only where no part is placed: <see cref="Finish"/> cannot
    /// tell which levels a part builds without it, and does not assume.
    /// </summary>
    public PartLodCatalog? PartLods { get; init; }

    private readonly List<GlassBatch> _glass = [];

    /// <summary>
    /// One building's glass writer into the tile's shared batch for <paramref name="spec"/> (16d-2): every glazed building
    /// of a cell shares one bundle per glass material, so the library and the museum in one L3 cell cost one window-glass
    /// draw call. Each building keeps its own window base, grid width and channel per vertex.
    /// </summary>
    public GlassSink Glass(ToonSpec spec, int? windowBase, int columns, ChannelId channel)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var batch = _glass.Find(b => b.Spec == spec);
        if (batch is null)
        {
            batch = new GlassBatch(spec, Palette, windowBase is not null);
            _glass.Add(batch);
        }
        return new GlassSink(batch, windowBase, columns, channel);
    }

    /// <summary>
    /// The tile's output at <paramref name="lod"/>: the shared sink's bundles, then the shared glass batches in first-use order,
    /// then the bundles pieces wrote themselves. Placed parts the baker cannot build at that level are left out (small street
    /// furniture such as the bench registers L0 and L1 only), so a piece may place its dressing without checking each part.
    /// A placed part the catalog does not know, or any placed part with no catalog, is an error, not a part to pass through.
    /// </summary>
    public ContentOutput Finish(LodLevel lod)
    {
        var meshes = Mesh.Finish();
        var glass = _glass.SelectMany(b => b.Finish()).ToArray();
        if (glass.Length > 0 || Bundles.Count > 0) meshes = [.. meshes, .. glass, .. Bundles];
        var parts = BuildableAt(lod);
        if (meshes.Count == 0 && parts.Length == 0 && Emitters.Count == 0) return ContentOutput.Empty;
        return new ContentOutput(meshes, parts, Emitters.Count == 0 ? null : Emitters.ToArray());
    }

    private PlacedPart[] BuildableAt(LodLevel lod)
    {
        if (Parts.Count == 0) return [];
        var catalog = PartLods ?? throw new InvalidOperationException("Placed parts need BuildContext.PartLods to know which levels the baker can build.");
        return Parts.Where(p => catalog.Supports(p.Part, lod)).ToArray();
    }
}

/// <summary>Tile arithmetic shared by every piece (IContentModule contract on Tiles and Build).</summary>
public static class CoreTiles
{
    public const float L0Size = 64f;

    public static TileKey Of(Vector2 p, LodLevel lod)
    {
        if (lod == LodLevel.L4) return new TileKey(0, 0);
        var size = L0Size * (1 << (int)lod);
        return new TileKey((int)MathF.Floor(p.X / size), (int)MathF.Floor(p.Y / size));
    }
}
