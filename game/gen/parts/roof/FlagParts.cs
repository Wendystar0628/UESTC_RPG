using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof;

/// <summary>Cloth, bar and mark pieces shared by the flag parts.</summary>
internal static class FlagKit
{
    /// <summary>A bar between two points on the cloth plane (x, y), <paramref name="thick"/> deep centred on z = 0, out of the shadow pass.</summary>
    public static void Stroke(PartBuilder b, Vector2 a, Vector2 c, float width, float thick, ToonSpec material)
    {
        var d = c - a; var length = d.Length();
        if (length < 1e-4f) return;
        var dir = d / length;
        var at = new Matrix4x4(dir.X, dir.Y, 0, 0, -dir.Y, dir.X, 0, 0, 0, 0, 1, 0, (a.X + c.X) * 0.5f, (a.Y + c.Y) * 0.5f, 0, 1);
        b.AddCentredBox(new(length + width * 0.5f, width, thick), at, material, 0, VertexFlag.NoShadow);
    }

    /// <summary>
    /// The original round mark (ART-STYLE: a thin outer ring round an octagon and a cross of circuit traces; never the real school
    /// badge) as one-colour strokes through the cloth, centred on (cx, cy), <paramref name="r"/> the ring's radius.
    /// </summary>
    public static void Mark(PartBuilder b, float cx, float cy, float r, float thick, ToonSpec material)
    {
        var stroke = Math.Max(r * 0.09f, 0.008f);
        Vector2 On(float radius, int i, int n, float phase = 0f)
        {
            var a = phase + MathF.PI * 2 * i / n;
            return new(cx + radius * MathF.Cos(a), cy + radius * MathF.Sin(a));
        }
        for (var i = 0; i < 12; i++) Stroke(b, On(r, i, 12), On(r, i + 1, 12), stroke, thick, material);
        for (var i = 0; i < 8; i++) Stroke(b, On(r * 0.62f, i, 8, MathF.PI / 8), On(r * 0.62f, i + 1, 8, MathF.PI / 8), stroke, thick, material);
        Stroke(b, new(cx, cy - r * 0.3f), new(cx, cy + r * 0.3f), stroke, thick, material);
        Stroke(b, new(cx, cy), new(cx + r * 0.3f, cy), stroke, thick, material);
    }
}

/// <summary>
/// Lamp-pole flag (灯杆旗, and the autumn 校庆道旗, WLD-LIFE-STREET, ART-FIDELITY-DETAIL): a flat cloth hanging from a crossbar, a
/// weighted rod in its hem, the mark of the school in one colour on both faces and no lettering. Width × Height is the cloth with its
/// bar; Depth is the cloth's thickness. Variation 0: electronic blue, the lamp-pole flag. 1: low-saturation red, the anniversary pennant.
/// 2: blue with a lighter band near the hem and no mark. The pole is the host's; the bar is what its bracket holds. Visual only: flags
/// never block the way and never come with the weather (they show in autumn by the season mask).
/// </summary>
[Part(PartCatalog.Pennant, Owner = "WP-12")]
public sealed class PennantPart : IPartDef<SignParams>
{
    public PennantPart() => Lods = Levels.Of<SignParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<SignParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(700, 140);

    public Box3 Bounds(SignParams p)
    {
        Validate(p);
        var hz = Math.Max(p.Depth * 0.5f, RoofStyle.FlagBarRadius + 0.005f);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -hz), new(p.Width * 0.5f, p.Height, hz));
    }

    private PartGeometry Build(SignParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var hx = p.Width * 0.5f; var h = p.Height; var d = p.Depth * 0.5f;
        var cloth = RoofStyle.Board(p.Variation == 1 ? RoofStyle.FlagRed : RoofStyle.FlagBlue);
        var steel = RoofStyle.Pipe(RoofStyle.Metal(RoofStyle.Aluminium));
        var rb = RoofStyle.FlagBarRadius; var seg = near ? RoofStyle.RoundSegments : RoofStyle.RoundSegmentsFar;
        var barY = h - rb - 0.005f;
        // Crossbar with round end caps, two short hanger tabs, the cloth under it and the hem rod.
        b.AddPipe(new(-hx + 0.01f, barY, 0), new(hx - 0.01f, barY, 0), rb, seg, steel);
        var top = barY - rb * 0.5f; var hem = RoofStyle.FlagHem;
        b.AddBox(new(-hx + 0.012f, hem, -d), new(hx - 0.012f, top, d), near ? 0.002f : 0, cloth);
        b.AddPipe(new(-hx + 0.008f, hem, 0), new(hx - 0.008f, hem, 0), RoofStyle.FlagHemRadius, seg, steel);
        if (!near) return b.Finish(Bounds(p));
        foreach (var x in new[] { -hx * 0.7f, hx * 0.7f })
            b.AddBox(new(x - 0.012f, top - 0.04f, -d - 0.002f), new(x + 0.012f, barY + 0.008f, d + 0.002f), 0, RoofStyle.Metal(RoofStyle.Aluminium), VertexFlag.NoShadow);
        var white = RoofStyle.Board(RoofStyle.CasingWhite);
        var span = top - hem;
        if (p.Variation == 2)
        {
            // A lighter band across the cloth, a hand above the hem rod.
            b.AddBox(new(-hx + 0.012f, hem + span * 0.18f, -d - 0.001f), new(hx - 0.012f, hem + span * 0.3f, d + 0.001f), 0, RoofStyle.Board(RoofStyle.FlagBlueLight), VertexFlag.NoShadow);
            return b.Finish(Bounds(p));
        }
        var r = Math.Min(p.Width * 0.5f - 0.05f, span * 0.28f);
        FlagKit.Mark(b, 0, hem + span * 0.58f, r, p.Depth + 0.004f, white);
        return b.Finish(Bounds(p));
    }

    private static void Validate(SignParams p)
    {
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        if (p.Variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown pennant variation.");
        if (p.Width < 0.3f || p.Height < 0.5f || p.Depth < 0.01f || p.Depth > 0.05f) throw new ArgumentOutOfRangeException(nameof(p), "A pennant is at least 0.3 × 0.5 m of cloth, 1–5 cm thick.");
    }
}

/// <summary>
/// Banner (横幅): a long flat cloth between a top rod and a hem rod and a plain band for the
/// slogan, which waits for the scene-text atlas (short simplified-Chinese lines in the placer's hands). Width × Height is the cloth with
/// its rods; Depth the cloth's thickness. Variation 0: white cloth, blue edge stripes and a blue band. 1: blue cloth with a white band.
/// 2: low-saturation red cloth with a white band. Visual only; a seasonal summer piece (WLD-LIFE-FAUNA), so no collision.
/// </summary>
[Part(PartCatalog.Banner, Owner = "WP-12")]
public sealed class BannerPart : IPartDef<SignParams>
{
    public BannerPart() => Lods = Levels.Of<SignParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<SignParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(420, 130);

    public Box3 Bounds(SignParams p)
    {
        Validate(p);
        var hz = Math.Max(p.Depth * 0.5f + 0.003f, RoofStyle.FlagBarRadius + 0.005f);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -hz), new(p.Width * 0.5f, p.Height, hz));
    }

    private PartGeometry Build(SignParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var hx = p.Width * 0.5f; var h = p.Height; var d = p.Depth * 0.5f;
        var v = p.Variation;
        var cloth = RoofStyle.Board(v switch { 0 => RoofStyle.CasingWhite, 1 => RoofStyle.FlagBlue, _ => RoofStyle.FlagRed });
        var accent = RoofStyle.Board(v == 0 ? RoofStyle.FlagBlue : RoofStyle.CasingWhite);
        var steel = RoofStyle.Pipe(RoofStyle.Metal(RoofStyle.Aluminium));
        var rb = RoofStyle.FlagBarRadius; var seg = near ? RoofStyle.RoundSegments : RoofStyle.RoundSegmentsFar;
        var top = h - 2 * rb; var bottom = 2 * rb;
        b.AddPipe(new(-hx, h - rb, 0), new(hx, h - rb, 0), rb, seg, steel);
        b.AddPipe(new(-hx, rb, 0), new(hx, rb, 0), rb, seg, steel);
        b.AddBox(new(-hx + 0.02f, bottom - RoofStyle.FlagOverlap, -d), new(hx - 0.02f, top + RoofStyle.FlagOverlap, d), near ? 0.002f : 0, cloth);
        // The slogan band through the cloth (both faces), and for the white cloth the blue edge stripes along top and bottom.
        var bandH = (top - bottom) * 0.42f; var cy = (top + bottom) * 0.5f;
        b.AddBox(new(-hx * 0.78f, cy - bandH * 0.5f, -d - 0.002f), new(hx * 0.78f, cy + bandH * 0.5f, d + 0.002f), 0, accent, VertexFlag.NoShadow);
        if (!near) return b.Finish(Bounds(p));
        if (v == 0)
        {
            var stripe = (top - bottom) * 0.07f;
            b.AddBox(new(-hx + 0.02f, top - stripe, -d - 0.002f), new(hx - 0.02f, top, d + 0.002f), 0, accent, VertexFlag.NoShadow);
            b.AddBox(new(-hx + 0.02f, bottom, -d - 0.002f), new(hx - 0.02f, bottom + stripe, d + 0.002f), 0, accent, VertexFlag.NoShadow);
        }
        return b.Finish(Bounds(p));
    }

    private static void Validate(SignParams p)
    {
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        if (p.Variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown banner variation.");
        if (p.Width < 1f || p.Height < 0.3f || p.Depth < 0.005f || p.Depth > 0.05f) throw new ArgumentOutOfRangeException(nameof(p), "A banner is at least 1 × 0.3 m of cloth, 0.5–5 cm thick.");
    }
}
