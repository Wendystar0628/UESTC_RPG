using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>Small shared pieces of the street-set completion parts (poles, boards, mirror, clock, drain, fence).</summary>
internal static class StreetShapes
{
    public static FootMat Foot(FootMat given, FootMat styleFoot) => given == FootMat.Auto ? styleFoot : given;

    /// <summary>Self-lit-free sky-reflecting glass (notice-board pane, convex mirror): the glass class of ART-STYLE.</summary>
    public static ToonSpec Glazing() => new(S.Lit(S.CarGlass), S.Shade(S.CarGlass)) { Kind = ToonKind.Glass, Fx = ToonFx.GlassStreak | ToonFx.Glare, Wet = 0f };

    /// <summary>
    /// A raised print block (letter stand-in, rule, poster) on a face looking along −Z (<paramref name="back"/> false) or +Z: a box
    /// <see cref="StreetStyle.InkProud"/> proud of the face plane <paramref name="zFace"/>, sunk a joint into it. Out of the shadow pass.
    /// </summary>
    public static void Ink(PartBuilder b, float x0, float y0, float x1, float y1, float zFace, bool back, ToonSpec material, float proud = S.InkProud)
    {
        var (lo, hi) = back ? (zFace - S.Joint, zFace + proud) : (zFace - proud, zFace + S.Joint);
        b.AddBox(new(x0, y0, lo), new(x1, y1, hi), 0, material, VertexFlag.NoShadow);
    }
}
