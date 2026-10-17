using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Two-post signpost (双柱指路牌, MAP-QSH-ADDITIONS): two dark grey square posts with caps and base plates, carrying three to
/// five arrow boards (variation 0, 1, 2) that point alternately to the viewer's right and left, each with a white chevron printed at
/// its tip on both faces. Width × Height × Depth is the overall sign; the boards span both posts and reach the side they point to.
/// The place names (white, with pinyin) wait for the scene-text atlas; the boards stay clean until then. Collision: the two posts and a
/// thin box over the boards, Metal.
/// </summary>
[Part(PartCatalog.Signpost, Owner = "WP-13")]
public sealed class SignpostPart : IPartDef<SignParams>
{
    public SignpostPart() => Lods = Levels.Of<SignParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2);
    public IReadOnlyDictionary<LodLevel, Func<SignParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(440, 140);

    public Box3 Bounds(SignParams p)
    {
        Validate(p);
        var hz = Math.Max(p.Depth * 0.5f, S.SignpostPost);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -hz), new(p.Width * 0.5f, p.Height, hz));
    }

    public IReadOnlyList<Collider> Collision(SignParams p)
    {
        var k = Layout(p);
        var foot = p.Foot == FootMat.Auto ? S.MetalFoot : p.Foot;
        var r = S.SignpostPost * 0.71f;
        return
        [
            new PostCollider(new(-k.PostX, 0), r, -S.PostColliderSink, p.Height, SurfaceTag.Prop) { Foot = foot },
            new PostCollider(new(k.PostX, 0), r, -S.PostColliderSink, p.Height, SurfaceTag.Prop) { Foot = foot },
            new BoxCollider(Xf.Move(0, (k.Bottom + k.Top) * 0.5f, S.SignpostPost * 0.5f + 0.002f + S.SignpostBoardDepth * 0.5f), new(p.Width * 0.5f, (k.Top - k.Bottom) * 0.5f, S.SignpostBoardDepth * 0.5f + 0.01f), SurfaceTag.Prop) { Foot = foot },
        ];
    }

    private PartGeometry Build(SignParams p, LodLevel lod)
    {
        var k = Layout(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var dark = S.Casing(S.DarkSteel);
        var post = S.SignpostPost; var hp = post * 0.5f;
        foreach (var x in new[] { -k.PostX, k.PostX })
        {
            b.AddBox(new(x - hp, 0, -hp), new(x + hp, p.Height - 0.04f + S.Joint, hp), near ? 0.012f : 0, dark);
            if (lod > LodLevel.L1) continue;
            // Cap and base plate unbevelled: five boards stay within the budget (the convex edge line stands in, ART-STYLE).
            b.AddBox(new(x - hp - 0.012f, p.Height - 0.04f, -hp - 0.012f), new(x + hp + 0.012f, p.Height, hp + 0.012f), 0, dark);
            if (near) b.AddBox(new(x - hp - 0.04f, 0, -hp - 0.04f), new(x + hp + 0.04f, 0.02f, hp + 0.04f), 0, dark, VertexFlag.NoShadow);
        }
        var bd = S.SignpostBoardDepth;
        var white = S.Ink(S.PaintWhite);
        for (var i = 0; i < k.Boards; i++)
        {
            // Boards from the top down; the first points to the viewer's right (−X), then they alternate.
            var y1 = k.Top - i * (S.SignpostBoardHeight + S.SignpostBoardGap); var y0 = y1 - S.SignpostBoardHeight;
            var sign = i % 2 == 0 ? -1f : 1f;
            var tip = sign * p.Width * 0.5f; var tail = tip - sign * k.Length;
            var neck = tip - sign * S.SignpostTip;
            Vector2[] outline = [new(tail, y0), new(neck, y0), new(tip, (y0 + y1) * 0.5f), new(neck, y1), new(tail, y1)];
            b.AddPlate(outline, hp + 0.002f, hp + 0.002f + bd, near ? 0.008f : 0, S.Casing(S.DarkSteel));
            if (!near) continue;
            // The chevron at the tip, both faces (u is the viewer's right: −x on the front, +x on the back).
            var h = S.SignpostBoardHeight * 0.3f; var cx = neck - sign * 0.05f; var cy = (y0 + y1) * 0.5f;
            foreach (var (z, back) in new[] { (hp + 0.002f, false), (hp + 0.002f + bd, true) })
            {
                var u = back ? 1f : -1f;
                Lettering.Segment(b, new(u * (cx - sign * h * 0.5f), cy + h), new(u * cx, cy), h * 0.32f, z, back, white);
                Lettering.Segment(b, new(u * cx, cy), new(u * (cx - sign * h * 0.5f), cy - h), h * 0.32f, z, back, white);
            }
        }
        return b.Finish(Bounds(p));
    }

    private readonly record struct SignpostLayout(float PostX, int Boards, float Length, float Top, float Bottom);

    private static SignpostLayout Layout(SignParams p)
    {
        Validate(p);
        var boards = 3 + p.Variation;
        var length = Math.Min(S.SignpostBoardLength, p.Width - 0.2f);
        var top = p.Height - 0.16f;
        var bottom = top - boards * S.SignpostBoardHeight - (boards - 1) * S.SignpostBoardGap;
        // The posts stand inside every board: within the overlap of a right- and a left-pointing board.
        var postX = Math.Min(0.16f, length - p.Width * 0.5f - S.SignpostPost);
        return new(postX, boards, length, top, bottom);
    }

    private static void Validate(SignParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown signpost variation (3–5 boards).");
        if (p.Width < 0.8f || p.Height < 2f) throw new ArgumentOutOfRangeException(nameof(p), "A signpost is at least 0.8 m wide and 2 m tall.");
        if (Math.Min(S.SignpostBoardLength, p.Width - 0.2f) - p.Width * 0.5f - S.SignpostPost < 0.06f) throw new ArgumentOutOfRangeException(nameof(p), "The boards must overlap round the two posts.");
    }
}

/// <summary>
/// Shared-bicycle bay (共享单车停放点, WLD-LIFE, D-186): a white painted frame on the ground, Width × Depth, with a short slot
/// tick every <see cref="StreetStyle.BaySlot"/> along its back line, and at its back left corner a low standing sign: a dark post and
/// a white panel with the electronic-blue band (for the bay's number) and a blue bicycle icon, both faces. Height is the sign's
/// height. The number waits for WP-55's bay data and the scene-text atlas. Collision: the sign only, a thin box, Metal; the painted
/// frame is ground (WLD-LIFE). Variation 0 is the open bay; a roofed shelter (自行车棚) is a later variation.
/// </summary>
[Part(PartCatalog.BicycleShelter, Owner = "WP-13")]
public sealed class BicycleBayPart : IPartDef<RoofParams>
{
    public BicycleBayPart() => Lods = Levels.Of<RoofParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<RoofParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(300, 100);

    public Box3 Bounds(RoofParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f + 0.22f));
    }

    public IReadOnlyList<Collider> Collision(RoofParams p)
    {
        var (x, z) = SignAt(p);
        return [new BoxCollider(Xf.Move(x, p.Height * 0.5f, z), new(S.BaySignWidth * 0.5f, p.Height * 0.5f, 0.05f), SurfaceTag.Prop)
            { Foot = p.Foot == FootMat.Auto ? S.MetalFoot : p.Foot }];
    }

    private PartGeometry Build(RoofParams p, LodLevel lod)
    {
        var (sx, sz) = SignAt(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        // The painted frame, a few millimetres proud of the ground, out of the shadow pass.
        var paint = Lettering.Print(S.Board(S.GroundPaint));
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f; var l = S.BayLine; const float y = 0.004f;
        void Line(float x0, float z0, float x1, float z1) =>
            b.Add(Shapes.FlatQuad(new(x0, y, z0), new(x0, y, z1), new(x1, y, z1), new(x1, y, z0), Vector3.UnitY, paint), VertexFlag.NoShadow);
        Line(-hx, -hz, hx, -hz + l); Line(-hx, hz - l, hx, hz);
        Line(-hx, -hz + l, -hx + l, hz - l); Line(hx - l, -hz + l, hx, hz - l);
        if (near)
            for (var x = -hx + S.BaySlot; x < hx - S.BaySlot * 0.5f; x += S.BaySlot)
                Line(x - l * 0.5f, hz - l - 0.35f, x + l * 0.5f, hz - l);
        // The sign: post, panel, blue band and the icon on both faces.
        var dark = S.Casing(S.DarkSteel);
        var w = S.BaySignWidth; var panel = S.BaySignPanel; var y0 = p.Height - panel;
        b.AddBox(new(sx - 0.03f, 0, sz - 0.03f), new(sx + 0.03f, y0 + 0.05f, sz + 0.03f), near ? 0.008f : 0, dark);
        if (near) b.AddBox(new(sx - 0.08f, 0, sz - 0.08f), new(sx + 0.08f, 0.02f, sz + 0.08f), 0.006f, dark, VertexFlag.NoShadow);
        const float pd = 0.03f;
        b.AddBox(new(sx - w * 0.5f, y0, sz - pd * 0.5f), new(sx + w * 0.5f, p.Height, sz + pd * 0.5f), near ? 0.008f : 0, S.Casing(S.GreyMetal));
        var e = 0.02f; var band = panel * 0.3f;
        foreach (var (z, back) in new[] { (sz - pd * 0.5f, false), (sz + pd * 0.5f, true) })
        {
            var out_ = back ? 1f : -1f;
            b.AddBox(new(sx - w * 0.5f + e, y0 + e, Math.Min(z, z + out_ * 0.004f)), new(sx + w * 0.5f - e, p.Height - e, Math.Max(z, z + out_ * 0.004f)), 0, S.Board(S.PaintWhite), VertexFlag.NoShadow);
            var z2 = z + out_ * 0.004f;
            b.AddBox(new(sx - w * 0.5f + e, p.Height - e - band, Math.Min(z2, z2 + out_ * 0.002f)), new(sx + w * 0.5f - e, p.Height - e, Math.Max(z2, z2 + out_ * 0.002f)), 0, S.Board(S.BikeBlue), VertexFlag.NoShadow);
            if (near) Lettering.Bicycle(b, back ? sx : -sx, y0 + e + (panel - 2 * e - band) * 0.5f, w * 0.72f, z2, back, S.Ink(S.BikeBlue));
        }
        return b.Finish(Bounds(p));
    }

    /// <summary>The sign stands behind the back line at the bay's left end (seen from the aisle at −Z, its left is +X).</summary>
    private static (float X, float Z) SignAt(RoofParams p)
    {
        Validate(p);
        return (p.Width * 0.5f - S.BaySignWidth * 0.5f - 0.1f, p.Depth * 0.5f + 0.12f);
    }

    private static void Validate(RoofParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation != 0) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Only the open bay (variation 0) is built on the mainline.");
        if (p.Width < 1.8f || p.Depth < 1.6f || p.Height < 0.9f || p.Height > 2.6f) throw new ArgumentOutOfRangeException(nameof(p), "A bay is at least 1.8 × 1.6 m with a 0.9–2.6 m sign.");
    }
}

/// <summary>
/// Kerb ramp (缘石坡道, 13c2b): a dropped kerb in front of a footway's kerb, rising from the carriageway at its front (−Z) to the kerb
/// top at its back (+Z), with flared wings at both ends and a yellow tactile warning strip printed across its foot. Width runs along
/// the kerb, Height is the kerb's rise, Depth the run. Collision: one walkable ramp, Ground, Concrete. The part ID waits for its
/// catalogue entry (contract request): <see cref="StreetStyle.KerbRampId"/>.
/// </summary>
[Part(StreetStyle.KerbRampId, Owner = "WP-13")]
public sealed class KerbRampPart : IPartDef<RampParams>
{
    public KerbRampPart() => Lods = Levels.Of<RampParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<RampParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(120, 40);

    public Box3 Bounds(RampParams p)
    {
        Validate(p);
        var hx = p.Width * 0.5f + S.KerbRampFlare;
        return Levels.Padded(new(-hx, 0, -p.Depth * 0.5f), new(hx, p.Height + 0.004f, p.Depth * 0.5f));
    }

    public IReadOnlyList<Collider> Collision(RampParams p)
    {
        Validate(p);
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f;
        return [new RampCollider(new(-hx, 0, -hz), new(hx, 0, -hz), new(hx, p.Height, hz), new(-hx, p.Height, hz), 0.1f, SurfaceTag.Ground)
            { Foot = p.Foot == FootMat.Auto ? FootMat.Concrete : p.Foot }];
    }

    private PartGeometry Build(RampParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f; var h = p.Height;
        var concrete = S.Stone(new(S.Lit(S.KerbConcrete), S.Shade(S.KerbConcrete)), false);
        const float lip = 0.012f;
        b.AddProfile([new(-hz, 0), new(hz, 0), new(hz, h), new(-hz, lip)], -hx, hx, 0, concrete);
        var f = S.KerbRampFlare;
        foreach (var side in new[] { -1f, 1f })
            b.Add(Shapes.Tetra(new(side * (hx - S.Joint), 0, -hz), new(side * (hx - S.Joint), 0, hz), new(side * (hx - S.Joint), h, hz), new(side * (hx + f), 0, hz), concrete));
        if (!near) return b.Finish(Bounds(p));
        // The tactile strip across the foot, on the slope, 3 mm proud.
        var slope = (h - lip) / p.Depth;
        var n = Xf.Unit(new(0, 1, -slope));
        Vector3 On(float x, float z) => new Vector3(x, lip + (z + hz) * slope, z) + n * 0.003f;
        var z0 = -hz + 0.05f; var z1 = z0 + S.TactileDepth;
        b.Add(Shapes.FlatQuad(On(-hx + 0.08f, z0), On(-hx + 0.08f, z1), On(hx - 0.08f, z1), On(hx - 0.08f, z0), n, Lettering.Print(S.Board(S.TactileYellow))), VertexFlag.NoShadow);
        return b.Finish(Bounds(p));
    }

    private static void Validate(RampParams p)
    {
        Levels.Dimensions(p);
        if (p.Width < 0.9f || p.Height > 0.2f || p.Depth < p.Height * 6f) throw new ArgumentOutOfRangeException(nameof(p), "A kerb ramp is at least 0.9 m wide, rises at most 0.2 m, no steeper than 1:6.");
    }
}
