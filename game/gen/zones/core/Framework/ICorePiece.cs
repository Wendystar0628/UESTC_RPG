using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.Core.Framework;

/// <summary>
/// One built object or group of the core zone: the time plaza, the sundial, later the library shell and each
/// surrounding block, the museum and the West Lake structures. A piece is a pure function of the world model and
/// its own style constants; it never reads files or world state (ENG-ARCH §2).
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

    /// <summary>Features and aspects this piece generates instead of the generic modules (SYS-MAPS §7).</summary>
    IReadOnlyList<Claim> Claims { get; }

    /// <summary>Visible geometry, placed catalogue parts and light emitters at one level.</summary>
    void Build(CoreBuild build, LodLevel lod);

    /// <summary>Collision primitives with the same parameters as the geometry (SYS-WALK §13); Foot always filled.</summary>
    IReadOnlyList<Collider> Collide();

    /// <summary>Close-range inspection probes of this piece (ART-FIDELITY §8); most pieces have none.</summary>
    IReadOnlyList<InspectionProbe> Probes => [];
}

/// <summary>Output of one tile build: shared mesh sink plus placed parts and emitters, in piece order.</summary>
public sealed class CoreBuild(PaletteRegistry? palette)
{
    public CoreMesh Mesh { get; } = new(palette);
    public List<PlacedPart> Parts { get; } = [];
    public List<LightEmitter> Emitters { get; } = [];

    public ContentOutput Finish()
    {
        var meshes = Mesh.Finish();
        if (meshes.Count == 0 && Parts.Count == 0 && Emitters.Count == 0) return ContentOutput.Empty;
        return new ContentOutput(meshes, Parts.ToArray(), Emitters.Count == 0 ? null : Emitters.ToArray());
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
