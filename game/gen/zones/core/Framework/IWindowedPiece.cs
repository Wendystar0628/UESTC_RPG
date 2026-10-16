using Campus.Contracts;

namespace Campus.Gen.Zones.Core.Framework;

/// <summary>
/// A piece that builds a claimed facade with its own window grid (IWindowMetadataSource). Both members read the piece's
/// immutable opening layout, the same one its glass is built from; neither builds geometry.
/// </summary>
public interface IWindowedPiece
{
    /// <summary>The complete grid set of the piece's facade feature; sizes only, no base.</summary>
    ClaimedWindowLayout WindowLayout { get; }

    /// <summary>One declaration per logical window cell, numbered from the map's allocation.</summary>
    IReadOnlyList<WindowDecl> Windows(WindowNumberingSnapshot numbering);
}
