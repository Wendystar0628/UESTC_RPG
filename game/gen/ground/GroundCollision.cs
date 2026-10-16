using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Gen.Ground;

/// <summary>
/// Walking collision for the ground (SYS-WALK 13): one 65 x 65 height field per 64 m cell, 1 m spacing, heights in cm,
/// sampled from the same lattice and the same surface rules as the visible L0 mesh; plus the 0.5 m footstep grid
/// (128 x 128 bytes, SYS-AUDIO 9.3). Row-major with x fastest: index = z * 65 + x (HeightMapShape3D map_data order).
/// Triangulation diagonal of each 1 m cell: the anti-diagonal (1, 0)-(0, 1) (<see cref="CellSplit"/>), as measured on Godot
/// 4.7.2's Jolt height-field shape and the same diagonal the L0 mesh uses.
/// </summary>
public static class GroundCollision
{
    public const int Samples = 65;
    public const int FootCells = 128;

    public static HeightFieldCollider Build(BuildContext ctx, TileKey tile, GroundSection? section = null, IGroundTerrain? terrain = null)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        section ??= GroundSection.Default;
        var layout = GroundLayout.Of(ctx.World);
        var extent = GroundLod.Tile(tile, LodLevel.L0, ctx.Map.DataRect);
        var grid = TerrainGrid.For(terrain ?? FlatTerrain.Instance, tile, LodLevel.L0, extent);
        var field = TileField.Build(layout, extent, section, grid, LawnNoise.Seed(ctx));
        var mesher = new GroundMesher(field, section, LodLevel.L0, layout, grid, new MeshSink());
        var step = field.N / (Samples - 1);          // lattice points per metre (4 at 0.25 m)
        var cm = new short[Samples * Samples];
        for (var j = 0; j < Samples; j++)
            for (var i = 0; i < Samples; i++)
                cm[j * Samples + i] = (short)MathF.Round(mesher.WalkHeightAt(i * step, j * step) * 100f);
        var foot = new byte[FootCells * FootCells];
        var half = field.N / FootCells;               // lattice points per 0.5 m (2 at 0.25 m)
        for (var j = 0; j < FootCells; j++)
            for (var i = 0; i < FootCells; i++)
            {
                // Cell centre: (i + 0.5) * 0.5 m lies on the 0.25 m lattice.
                var surface = mesher.SurfaceAt(i * half + half / 2, j * half + half / 2);
                foot[j * FootCells + i] = (byte)GroundPalette.FootOf(surface);
            }
        return new HeightFieldCollider(tile, cm, SurfaceTag.Ground)
        {
            Foot = Contracts.Audio.FootMat.Grass,
            FootMatHalfMeter = foot
        };
    }

    /// <summary>
    /// Solid tree pits of one tile: a pit whose frame stands at least <see cref="PitSpec.SolidHeight"/> above the paving
    /// and whose outline is a rectangle is a box from the terrain to the frame top (the top can be stood on: SYS-WALK 13,
    /// "花池"), stone underfoot. A pit belongs to the tile that holds its centre. Lower frames and planting strips are not
    /// solid: the walking height field passes over them at paving level.
    /// </summary>
    public static IReadOnlyList<Collider> PitBoxes(BuildContext ctx, TileKey tile, GroundSection? section = null, IGroundTerrain? terrain = null)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        section ??= GroundSection.Default;
        terrain ??= FlatTerrain.Instance;
        var extent = GroundLod.Tile(tile, LodLevel.L0, ctx.Map.DataRect);
        var min = new Vector2(extent.X0, extent.Z0);
        var max = min + new Vector2(extent.Size);
        var boxes = new List<Collider>();
        foreach (var shape in GroundLayout.Of(ctx.World).Near(min, max))
        {
            if (shape.Kind != SurfaceKind.Pit || PitBox(shape, section, terrain) is not { } found) continue;
            var centre = new Vector2(found.At.M41, found.At.M43);
            if (centre.X >= min.X && centre.X < max.X && centre.Y >= min.Y && centre.Y < max.Y) boxes.Add(found);
        }
        return boxes;
    }

    /// <summary>The box of one rectangular pit that is tall enough to block, else null.</summary>
    internal static BoxCollider? PitBox(GroundShape shape, GroundSection section, IGroundTerrain terrain)
    {
        if (shape.Kind != SurfaceKind.Pit || shape.Rings is not { Length: 1 } rings || rings[0].Length != 4) return null;
        var c = rings[0];
        Vector2[] edges = [c[1] - c[0], c[2] - c[1], c[3] - c[2], c[0] - c[3]];
        var length = new float[4];
        for (var k = 0; k < 4; k++)
        {
            length[k] = edges[k].Length();
            if (!(length[k] > 0.2f)) return null;
        }
        var tolerance = section.Pits.RectangleTolerance;
        for (var k = 0; k < 4; k++)
            if (MathF.Abs(Vector2.Dot(edges[k] / length[k], edges[(k + 1) % 4] / length[(k + 1) % 4])) > tolerance) return null;
        var first = (length[0] + length[2]) * 0.5f;
        var second = (length[1] + length[3]) * 0.5f;
        var centre = (c[0] + c[1] + c[2] + c[3]) * 0.25f;
        var ground = terrain.HeightAt(centre.X, centre.Y);
        var top = ground + shape.PitHeights(section).Rim;
        if (top - (ground + section.PavingY) < section.Pits.SolidHeight) return null;
        var u = (edges[0] - edges[2]) / (2f * first);   // unit vector along the first pair of edges
        var half = new Vector3(first * 0.5f, (top - ground) * 0.5f, second * 0.5f);
        // Local x runs along u and local z across it; rows are the images of the local axes (a proper rotation about y).
        var at = new Matrix4x4(u.X, 0f, u.Y, 0f, 0f, 1f, 0f, 0f, -u.Y, 0f, u.X, 0f, centre.X, ground + half.Y, centre.Y, 1f);
        return new BoxCollider(at, half, SurfaceTag.Prop) { Foot = FootMat.StonePaving };
    }

    /// <summary>Height of the collision surface at a world point, interpolated on the same (anti-)diagonal split.</summary>
    public static float SampleHeight(HeightFieldCollider collider, float x, float z)
    {
        ArgumentNullException.ThrowIfNull(collider);
        var lx = x - collider.Tile.X * GroundLod.TileSize;
        var lz = z - collider.Tile.Z * GroundLod.TileSize;
        var i = Math.Clamp((int)MathF.Floor(lx), 0, Samples - 2);
        var j = Math.Clamp((int)MathF.Floor(lz), 0, Samples - 2);
        var fx = Math.Clamp(lx - i, 0f, 1f);
        var fz = Math.Clamp(lz - j, 0f, 1f);
        float H(int a, int b) => collider.HeightsCm[(j + b) * Samples + i + a] * 0.01f;
        return CellSplit.Lerp(H(0, 0), H(1, 0), H(0, 1), H(1, 1), fx, fz);
    }
}
