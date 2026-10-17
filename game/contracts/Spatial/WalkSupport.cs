using System.Numerics;

namespace Campus.Contracts;

/// <summary>Immutable exact support surfaces, prepared from the same geometry as walking collision.</summary>
public interface IWalkSupportSource
{
    float? SupportY(float x, float z, float yHint);
}

/// <summary>One ground tile's original collision field and physical tops, including contact materials and holes.</summary>
public sealed record GroundSupportTileData(HeightFieldCollider Base, IReadOnlyList<Collider> Tops);

/// <summary>Exact startup geometry. Ground retains tile ownership; other module and part tops retain their actual layers.</summary>
public sealed record WalkSupportData(IReadOnlyList<GroundSupportTileData> Ground, IReadOnlyList<Collider> Other)
{
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Ground);
        ArgumentNullException.ThrowIfNull(Other);
        var tiles = new HashSet<TileKey>();
        foreach (var tile in Ground)
        {
            ArgumentNullException.ThrowIfNull(tile);
            ArgumentNullException.ThrowIfNull(tile.Base);
            ArgumentNullException.ThrowIfNull(tile.Tops);
            if (!tiles.Add(tile.Base.Tile)) throw new ArgumentException("Ground support tiles must be unique.");
            ValidateCollider(tile.Base);
            foreach (var top in tile.Tops)
            {
                if (top is HeightFieldCollider) throw new ArgumentException("Ground top geometry must be separate from its base field.");
                ValidateCollider(top);
            }
        }
        foreach (var collider in Other) ValidateCollider(collider);
    }

    private static void ValidateCollider(Collider collider)
    {
        ArgumentNullException.ThrowIfNull(collider);
        if (collider.Tag is not (SurfaceTag.Ground or SurfaceTag.Stair))
            throw new ArgumentException("Walking support geometry must be Ground or Stair.");
        var valid = collider switch
        {
            HeightFieldCollider field => field.HeightsCm is { Length: TerrainSampling.TileSamples * TerrainSampling.TileSamples }
                && (field.FootMatHalfMeter is null || field.FootMatHalfMeter.Length == 128 * 128),
            SlabCollider slab => slab.Ring is { Count: >= 3 } && slab.Ring.All(p => float.IsFinite(p.X) && float.IsFinite(p.Y))
                && float.IsFinite(slab.Y) && float.IsFinite(slab.Thick) && slab.Thick > 0f,
            RampCollider ramp => Finite(ramp.P0) && Finite(ramp.P1) && Finite(ramp.P2) && Finite(ramp.P3)
                && float.IsFinite(ramp.Thick) && ramp.Thick > 0f,
            BoxCollider box => Finite(box.Half) && box.Half.X > 0 && box.Half.Y > 0 && box.Half.Z > 0
                && Finite(Vector3.Transform(Vector3.Zero, box.At)) && Finite(Vector3.Transform(box.Half, box.At))
                && Finite(Vector3.Transform(-box.Half, box.At)),
            _ => false
        };
        if (!valid) throw new ArgumentException("Walking support collider geometry is invalid.");
    }

    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
