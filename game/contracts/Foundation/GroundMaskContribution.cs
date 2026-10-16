using Campus.Contracts.Maps;

namespace Campus.Contracts;

[Flags]
public enum GroundMaskChannels : byte { None = 0, GinkgoLeaves = 1, OtherLeaves = 2, CanopyShelter = 4 }

/// <summary>RGBA8 row-major grid; texel centres are Rect.Min + (x+.5,z+.5)*MetersPerPixel.</summary>
public readonly record struct GroundMaskGrid(MapRect Rect, int Width, int Height, float MetersPerPixel)
{
    public int ByteCount => checked(Width * Height * 4);
    public void Validate(int mask1Bytes, int mask2Bytes)
    {
        ArgumentNullException.ThrowIfNull(Rect);
        if (Width <= 0 || Height <= 0 || !float.IsFinite(MetersPerPixel) || MetersPerPixel <= 0 ||
            !float.IsFinite(Rect.Min.X) || !float.IsFinite(Rect.Min.Y) || !float.IsFinite(Rect.Max.X) || !float.IsFinite(Rect.Max.Y))
            throw new ArgumentException("Mask layout must have positive finite dimensions.");
        // Preserve the existing WP-15 / WP-23 grid rounding rule, including non-integral extents.
        if (Width != MathF.Round((Rect.Max.X - Rect.Min.X) / MetersPerPixel) ||
            Height != MathF.Round((Rect.Max.Y - Rect.Min.Y) / MetersPerPixel) ||
            mask1Bytes != ByteCount || mask2Bytes != ByteCount)
            throw new ArgumentException("Mask extents, dimensions and RGBA8 buffer lengths disagree.");
    }
}

/// <summary>Optional capability of an already discovered IContentModule, filtered for the current map before worker builds.</summary>
/// <remarks>Claims are exclusive: duplicate channel owners fail before painting. No new registration attribute is needed.</remarks>
public interface IGroundMaskContributor
{
    string Id { get; }
    GroundMaskChannels Channels { get; }
    /// <summary>Pure CPU contribution. Only declared channels may change: Mask1.R/G or Mask2.B; never retain spans.</summary>
    /// <remarks>WP-15 skips its legacy tree fallback for claimed channels. A contributor may overwrite its own channel;
    /// global max-merging is not implied. Existing WP-23 corridor reductions therefore remain representable.</remarks>
    void Paint(BuildContext context, in GroundMaskGrid grid, Span<byte> mask1, Span<byte> mask2);
}
