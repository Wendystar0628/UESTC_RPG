using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.Ground;

/// <summary>Channels of the two ground masks (ENG-PERF 1.5, SYS-WEATHER 7, WLD-VEGETATION 9.5).</summary>
public enum MaskChannel : byte
{
    /// <summary>ground_mask R: ginkgo leaf potential (painted from canopies, WP-23).</summary>
    GinkgoLeaves = 0,
    /// <summary>ground_mask G: other leaves and petals (WP-23).</summary>
    OtherLeaves = 1,
    /// <summary>ground_mask B: snow potential: lawn 1, paths 0.5, carriageway 0.2, under eaves 0 (WP-15).</summary>
    Snow = 2,
    /// <summary>ground_mask A: maple leaf potential, a subset of OtherLeaves (WP-23).</summary>
    MapleLeaves = 3,
    /// <summary>ground_mask2 R: puddle potential (WP-15 with WP-45b).</summary>
    Puddle = 4,
    /// <summary>ground_mask2 G: building shelter, 0 under eaves, canopies and covered walks (WP-15 with WP-45b).</summary>
    Shelter = 5,
    /// <summary>ground_mask2 B: canopy shelter, multiplied by leaf amount at run time (WP-23).</summary>
    Canopy = 6,
    /// <summary>ground_mask2 A: reserved, always 0.</summary>
    Reserved2 = 7
}

/// <summary>
/// CPU-side bytes of ground_mask and ground_mask2 (RGBA8, uncompressed, rows along +z), both
/// covering the map's data rectangle (map_rect). The Godot side uploads these unchanged; the same bytes serve as the
/// read-only CPU copy for IGroundCover and footsteps (SYS-AUDIO 21.2). Writes go through the channel methods only.
/// </summary>
public sealed class GroundMaskSet
{
    public GroundMaskSet(MapRect rect, float metersPerPixel)
    {
        ArgumentNullException.ThrowIfNull(rect);
        if (!(metersPerPixel > 0f)) throw new ArgumentOutOfRangeException(nameof(metersPerPixel));
        Rect = rect;
        MetersPerPixel = metersPerPixel;
        Width = (int)MathF.Round((rect.Max.X - rect.Min.X) / metersPerPixel);
        Height = (int)MathF.Round((rect.Max.Y - rect.Min.Y) / metersPerPixel);
        if (Width < 1 || Height < 1) throw new ArgumentException("Empty mask rectangle.", nameof(rect));
        Mask1 = new byte[Width * Height * 4];
        Mask2 = new byte[Width * Height * 4];
    }

    public MapRect Rect { get; }
    public float MetersPerPixel { get; }
    public int Width { get; }
    public int Height { get; }
    /// <summary>ground_mask, RGBA8, row-major from Rect.Min (row = z).</summary>
    public byte[] Mask1 { get; }
    /// <summary>ground_mask2, RGBA8, same layout as Mask1.</summary>
    public byte[] Mask2 { get; }

    /// <summary>World centre of pixel (px, pz).</summary>
    public Vector2 Centre(int px, int pz) =>
        new(Rect.Min.X + (px + 0.5f) * MetersPerPixel, Rect.Min.Y + (pz + 0.5f) * MetersPerPixel);

    public byte Read(MaskChannel channel, int px, int pz)
    {
        var (buffer, offset) = Slot(channel, px, pz);
        return buffer[offset];
    }

    public void Write(MaskChannel channel, int px, int pz, byte value)
    {
        if (channel is MaskChannel.Reserved2)
            throw new ArgumentOutOfRangeException(nameof(channel), channel, "Reserved channels stay zero.");
        var (buffer, offset) = Slot(channel, px, pz);
        buffer[offset] = value;
    }

    /// <summary>Keeps the larger of the stored and the new value (canopies and leaf potentials accumulate this way).</summary>
    public void WriteMax(MaskChannel channel, int px, int pz, byte value)
    {
        if (Read(channel, px, pz) < value) Write(channel, px, pz, value);
    }

    /// <summary>Keeps the smaller value (shelters only ever lower exposure).</summary>
    public void WriteMin(MaskChannel channel, int px, int pz, byte value)
    {
        if (Read(channel, px, pz) > value) Write(channel, px, pz, value);
    }

    /// <summary>Paints a disc that fades linearly from <paramref name="centreValue"/> at the centre to 0 at the rim.</summary>
    public void PaintCone(MaskChannel channel, Vector2 centre, float radius, byte centreValue)
    {
        if (!(radius > 0f)) return;
        var (x0, z0) = Pixel(centre - new Vector2(radius));
        var (x1, z1) = Pixel(centre + new Vector2(radius));
        for (var pz = Math.Max(z0, 0); pz <= Math.Min(z1, Height - 1); pz++)
            for (var px = Math.Max(x0, 0); px <= Math.Min(x1, Width - 1); px++)
            {
                var d = Vector2.Distance(Centre(px, pz), centre);
                if (d >= radius) continue;
                WriteMax(channel, px, pz, (byte)MathF.Round(centreValue * (1f - d / radius)));
            }
    }

    /// <summary>Bilinear CPU sample in [0, 1]; outside the rectangle returns 0 (the shader's "no data").</summary>
    public float Sample(MaskChannel channel, float x, float z)
    {
        var u = (x - Rect.Min.X) / MetersPerPixel - 0.5f;
        var v = (z - Rect.Min.Y) / MetersPerPixel - 0.5f;
        if (u < -0.5f || v < -0.5f || u > Width - 0.5f || v > Height - 0.5f) return 0f;
        var i = (int)MathF.Floor(u);
        var j = (int)MathF.Floor(v);
        var fu = u - i;
        var fv = v - j;
        float At(int a, int b) => Read(channel, Math.Clamp(a, 0, Width - 1), Math.Clamp(b, 0, Height - 1)) / 255f;
        var top = At(i, j) + fu * (At(i + 1, j) - At(i, j));
        var bottom = At(i, j + 1) + fu * (At(i + 1, j + 1) - At(i, j + 1));
        return top + fv * (bottom - top);
    }

    public (int X, int Z) Pixel(Vector2 p) =>
        ((int)MathF.Floor((p.X - Rect.Min.X) / MetersPerPixel), (int)MathF.Floor((p.Y - Rect.Min.Y) / MetersPerPixel));

    private (byte[] Buffer, int Offset) Slot(MaskChannel channel, int px, int pz)
    {
        if ((uint)px >= (uint)Width || (uint)pz >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(px));
        var index = (pz * Width + px) * 4;
        return channel switch
        {
            MaskChannel.GinkgoLeaves or MaskChannel.OtherLeaves or MaskChannel.Snow or MaskChannel.MapleLeaves => (Mask1, index + (int)channel),
            MaskChannel.Puddle or MaskChannel.Shelter or MaskChannel.Canopy or MaskChannel.Reserved2 => (Mask2, index + (int)channel - 4),
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Unknown mask channel.")
        };
    }
}

/// <summary>Builds the ground masks from the same surfaces as the ground mesh.</summary>
public static class GroundMasks
{
    /// <summary>Mask resolution (P0 decides; 1 m keeps lawn and path edges readable, 2 m saves about 39 MB).</summary>
    public const float DefaultMetersPerPixel = 1f;
    /// <summary>Eave and canopy overhang counted as sheltered around a building footprint.</summary>
    public const float EaveOverhang = 0.6f;

    public static GroundMaskSet Build(BuildContext ctx, GroundSection? section = null,
        float metersPerPixel = DefaultMetersPerPixel)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        section ??= GroundSection.Default;
        var rect = ctx.Map.DataRect;
        var set = new GroundMaskSet(rect, metersPerPixel);
        var layout = GroundLayout.Of(ctx.World);
        PaintSurfaces(set, layout, section);
        PaintBuildings(set, layout);
        var grid = new GroundMaskGrid(rect, set.Width, set.Height, set.MetersPerPixel);
        foreach (var contributor in ctx.GroundMaskContributors)
            contributor.Paint(ctx, in grid, set.Mask1, set.Mask2);
        return set;
    }

    /// <summary>
    /// Snow potential, puddle potential along the gutter, and default unsheltered exposure.
    /// </summary>
    private static void PaintSurfaces(GroundMaskSet set, GroundLayout layout, GroundSection section)
    {
        var step = set.MetersPerPixel;
        var tileCells = (int)(TileGrid.BaseEdge / step);
        if (tileCells * step != TileGrid.BaseEdge) throw new ArgumentOutOfRangeException(nameof(set), "Mask pixels must tile 64 m.");
        // Sample pixel centres: a lattice offset by half a pixel, laid out in 64 m tiles.
        var first = TileGrid.At(set.Rect.Min);
        var tx0 = first.X;
        var tz0 = first.Z;
        var tx1 = (int)MathF.Ceiling(set.Rect.Max.X / TileGrid.BaseEdge);
        var tz1 = (int)MathF.Ceiling(set.Rect.Max.Y / TileGrid.BaseEdge);
        for (var tz = tz0; tz < tz1; tz++)
            for (var tx = tx0; tx < tx1; tx++)
            {
                var tile = new GroundTile(tx * TileGrid.BaseEdge + step * 0.5f, tz * TileGrid.BaseEdge + step * 0.5f,
                    TileGrid.BaseEdge, step);
                // These are plan-view potentials. No height field is generated for an unused RF texture.
                var grid = TerrainGrid.FlatPlan(tile);
                var field = TileField.Build(layout, tile, section, grid);
                var mesher = new GroundMesher(field, section, LodLevel.L0, layout, grid, new MeshSink());
                for (var j = 0; j < tileCells; j++)
                    for (var i = 0; i < tileCells; i++)
                    {
                        var centre = new Vector2(field.X(i), field.Z(j));
                        var (px, pz) = set.Pixel(centre);
                        if ((uint)px >= (uint)set.Width || (uint)pz >= (uint)set.Height) continue;
                        var surface = mesher.SurfaceAt(i, j);
                        set.Write(MaskChannel.Snow, px, pz, SnowPotential(surface));
                        set.Write(MaskChannel.Shelter, px, pz, 255);
                        // Gutter: the metre of asphalt along a kerb holds water first.
                        var a = field.Get(i, j, TileField.A);
                        if (surface == GroundSurface.Asphalt && a > -1f) set.Write(MaskChannel.Puddle, px, pz, 140);
                    }
            }
    }

    private static byte SnowPotential(GroundSurface surface) => surface switch
    {
        // The soil of a pit holds snow like a lawn does (leaves and petals pile there too); its frame is stone.
        GroundSurface.Grass or GroundSurface.GrassLight or GroundSurface.GrassDark or GroundSurface.Turf or GroundSurface.PitSoil => 255,
        GroundSurface.Asphalt or GroundSurface.Parking => 51,
        GroundSurface.Bevel or GroundSurface.Curb or GroundSurface.Edging or GroundSurface.Track or GroundSurface.Court or
            GroundSurface.Granite or GroundSurface.PlazaBorder or GroundSurface.Paver or GroundSurface.PitRim or GroundSurface.PitBevel => 128,
        GroundSurface.Water => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown ground surface.")
    };

    /// <summary>
    /// Building footprints plus the eave overhang: no snow and no rain underneath. Footprints are the layout's building
    /// shapes, one per source polygon with its own holes, so every part of a multi-part building is covered and an
    /// inner courtyard stays open.
    /// </summary>
    private static void PaintBuildings(GroundMaskSet set, GroundLayout layout)
    {
        foreach (var shape in layout.Shapes)
        {
            if (shape.Kind != SurfaceKind.Building) continue;
            var min = shape.Min - new Vector2(EaveOverhang + 1f);
            var max = shape.Max + new Vector2(EaveOverhang + 1f);
            var (x0, z0) = set.Pixel(min);
            var (x1, z1) = set.Pixel(max);
            for (var pz = Math.Max(z0, 0); pz <= Math.Min(z1, set.Height - 1); pz++)
                for (var px = Math.Max(x0, 0); px <= Math.Min(x1, set.Width - 1); px++)
                {
                    if (Sdf.Shape(shape, set.Centre(px, pz), EaveOverhang + 1f) >= EaveOverhang) continue;
                    set.Write(MaskChannel.Shelter, px, pz, 0);
                    set.Write(MaskChannel.Snow, px, pz, 0);
                    set.Write(MaskChannel.Puddle, px, pz, 0);
                }
        }
    }

}
