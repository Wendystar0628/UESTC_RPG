using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>
/// Designed terrain as the ground reads it (D-180): a deterministic height function sampled on a world grid of int16
/// centimetres. This mirrors the part of the planned contract <c>ITerrain</c> (contract-15) that the ground uses;
/// when the contract lands, an adapter over <c>IWorldModel.Terrain</c> replaces <see cref="FlatTerrain"/> and
/// nothing else changes. Grid spacing per level: L0 1 m, L1 2 m, L2 4 m, L3 8 m (65 x 65 per tile), L4 32 m
/// (81 x 81 for the whole map).
/// </summary>
public interface IGroundTerrain
{
    /// <summary>No terrain operation touches this L0 tile: the flat fast path applies.</summary>
    bool IsFlat(TileKey tile);

    /// <summary>Exact height; equals the int16 grid on 1 m grid points.</summary>
    float HeightAt(float x, float z);

    /// <summary>Fills the tile's grid in centimetres (65 x 65; 81 x 81 at level 4).</summary>
    void FillTile(TileKey tile, int level, Span<short> cm);
}

public sealed class FlatTerrain : IGroundTerrain
{
    public static FlatTerrain Instance { get; } = new();
    private FlatTerrain() { }
    public bool IsFlat(TileKey tile) => true;
    public float HeightAt(float x, float z) => 0f;
    public void FillTile(TileKey tile, int level, Span<short> cm) => cm.Clear();
}

/// <summary>
/// One tile's terrain samples, interpolated on the grid triangles with the anti-diagonal (1,0)-(0,1) (<see cref="CellSplit"/>),
/// the same split the collision height field uses; every ground vertex takes its base height from here ("draping").
/// </summary>
internal sealed class TerrainGrid
{
    private readonly short[] _cm;

    private TerrainGrid(float x0, float z0, float spacing, int count, short[] cm, bool flat)
    {
        X0 = x0;
        Z0 = z0;
        Spacing = spacing;
        Count = count;
        _cm = cm;
        IsFlat = flat;
    }

    public float X0 { get; }
    public float Z0 { get; }
    public float Spacing { get; }
    public int Count { get; }
    public bool IsFlat { get; }

    public static TerrainGrid For(IGroundTerrain terrain, TileKey tile, LodLevel lod, GroundTile extent)
    {
        var level = lod switch
        {
            LodLevel.L0 => 0,
            LodLevel.L1 => 1,
            LodLevel.L2 => 2,
            LodLevel.L3 => 3,
            LodLevel.L4 => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown level of detail.")
        };
        var count = level == 4 ? 81 : 65;
        var spacing = extent.Size / (count - 1);
        var cm = new short[count * count];
        if (level == 0 && terrain.IsFlat(tile)) return new(extent.X0, extent.Z0, spacing, count, cm, true);
        terrain.FillTile(tile, level, cm);
        var flat = true;
        foreach (var v in cm)
            if (v != 0) { flat = false; break; }
        return new(extent.X0, extent.Z0, spacing, count, cm, flat);
    }

    /// <summary>Height at a world point inside (or on the edge of) the tile.</summary>
    public float At(float x, float z)
    {
        if (IsFlat) return 0f;
        var u = (x - X0) / Spacing;
        var w = (z - Z0) / Spacing;
        var i = Math.Clamp((int)MathF.Floor(u), 0, Count - 2);
        var j = Math.Clamp((int)MathF.Floor(w), 0, Count - 2);
        var fu = Math.Clamp(u - i, 0f, 1f);
        var fw = Math.Clamp(w - j, 0f, 1f);
        float H(int a, int b) => _cm[(j + b) * Count + i + a] * 0.01f;
        return CellSplit.Lerp(H(0, 0), H(1, 0), H(0, 1), H(1, 1), fu, fw);
    }
}

/// <summary>
/// Shading normal class of a ground triangle (D-180): paving, roads, plazas and gentle lawn slopes shade as level
/// ground (normal up); designed earth slopes from 1:6 use flat faceted normals with their own vertices.
/// </summary>
public enum GroundShading : byte { Up, Faceted }

public static class GroundShadingRule
{
    /// <summary>Rise over run at or above which a lawn triangle is shaded as a slope (1:6).</summary>
    public const float FacetSlope = 1f / 6f;

    public static GroundShading For(GroundSurface surface, float slope) => surface switch
    {
        GroundSurface.Grass or GroundSurface.GrassLight or GroundSurface.GrassDark or GroundSurface.Turf =>
            slope >= FacetSlope ? GroundShading.Faceted : GroundShading.Up,
        GroundSurface.Water or GroundSurface.Asphalt or GroundSurface.Parking or GroundSurface.Bevel or GroundSurface.Curb or
            GroundSurface.Edging or GroundSurface.Track or GroundSurface.Court or GroundSurface.Granite or
            GroundSurface.PlazaBorder or GroundSurface.Paver or GroundSurface.PitRim or GroundSurface.PitBevel or GroundSurface.PitSoil => GroundShading.Up,
        _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown ground surface.")
    };
}
