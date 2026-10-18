using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Road-name sign (路名牌, MAP-QSH-ADDITIONS-VOCAB §2): a dark grey board with a white rule round its edge, a row of character
/// blocks and a row of pinyin strips (the road names wait for the scene-text atlas, so the blocks stand in for the text). Width is the
/// board; the footprint depth holds the plate and what carries it. Variation 0: a bare plate (Height is its height), the back on +Z
/// with two clamp tabs for a post or a wall; visual only, the front (−Z) printed. Variation 1: the two-post sign of a main junction
/// (Height is the overall height, the plate near the top), both faces printed; collision is the two posts and the plate, Metal.
/// </summary>
[Part(PartCatalog.RoadNameSign, Owner = "WP-13")]
public sealed class RoadNameSignPart : IPartDef<SignParams>
{
    public RoadNameSignPart() => Lods = Levels.Of<SignParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<SignParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(500, 160);

    public Box3 Bounds(SignParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f - S.InkProud), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f + (p.Variation == 1 ? S.InkProud : 0f)));
    }

    public IReadOnlyList<Collider> Collision(SignParams p)
    {
        var k = Layout(p);
        if (p.Variation == 0) return [];
        var foot = StreetShapes.Foot(p.Foot, S.MetalFoot);
        var r = S.RoadPost * 0.71f + 0.01f;
        return
        [
            new PostCollider(new(-k.PostX, k.PostZ), r, -S.PostColliderSink, p.Height, SurfaceTag.Prop) { Foot = foot },
            new PostCollider(new(k.PostX, k.PostZ), r, -S.PostColliderSink, p.Height, SurfaceTag.Prop) { Foot = foot },
            new BoxCollider(Xf.Move(0, (k.Bottom + k.Top) * 0.5f, (k.Front + k.Back) * 0.5f), new(p.Width * 0.5f, (k.Top - k.Bottom) * 0.5f, S.RoadPlateThickness * 0.5f + 0.01f), SurfaceTag.Prop) { Foot = foot },
        ];
    }

    private PartGeometry Build(SignParams p, LodLevel lod)
    {
        var k = Layout(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var dark = S.Casing(S.DarkSteel);
        b.AddBox(new(-p.Width * 0.5f, k.Bottom, k.Front), new(p.Width * 0.5f, k.Top, k.Back), near ? 0.008f : 0, dark);
        if (p.Variation == 0)
        {
            // Two clamp tabs on the back for a post or a wall.
            if (near)
                foreach (var x in new[] { -p.Width * 0.3f, p.Width * 0.3f })
                    b.AddBox(new(x - 0.025f, k.Bottom + (k.Top - k.Bottom) * 0.2f, k.Back - S.Joint), new(x + 0.025f, k.Top - (k.Top - k.Bottom) * 0.2f, p.Depth * 0.5f), 0.004f, S.Casing(S.GreyMetal), VertexFlag.NoShadow);
        }
        else
        {
            var post = S.RoadPost; var hp = post * 0.5f;
            foreach (var x in new[] { -k.PostX, k.PostX })
            {
                b.AddBox(new(x - hp, 0, k.PostZ - hp), new(x + hp, p.Height - 0.03f + S.Joint, k.PostZ + hp), near ? 0.012f : 0, dark);
                if (!near) continue;
                b.AddBox(new(x - hp - 0.012f, p.Height - 0.03f, k.PostZ - hp - 0.012f), new(x + hp + 0.012f, p.Height, k.PostZ + hp + 0.012f), 0.008f, dark);
                b.AddBox(new(x - hp - 0.025f, 0, k.PostZ - hp - 0.025f), new(x + hp + 0.025f, 0.02f, k.PostZ + hp + 0.025f), 0.006f, dark, VertexFlag.NoShadow);
            }
        }
        Face(b, p, k, near, back: false);
        if (p.Variation == 1) Face(b, p, k, near, back: true);
        return b.Finish(Bounds(p));
    }

    /// <summary>White rule, four character blocks, three pinyin strips (one merged bar beyond the near level).</summary>
    private static void Face(PartBuilder b, SignParams p, Plate k, bool near, bool back)
    {
        var ink = S.Ink(S.PaintWhite);
        var w = p.Width; var h = k.Top - k.Bottom; var cy = (k.Bottom + k.Top) * 0.5f;
        var z = back ? k.Back : k.Front;
        var charH = h * 0.36f;
        var span = w * 0.66f;
        var total = S.RoadChars * charH + (S.RoadChars - 1) * charH * 0.3f;
        var scale = Math.Min(1f, span / total);
        charH *= scale;
        var gap = charH * 0.3f;
        var width = S.RoadChars * charH + (S.RoadChars - 1) * gap;
        var rowY = cy + h * 0.1f;
        if (!near)
        {
            StreetShapes.Ink(b, -width * 0.5f, rowY - charH * 0.5f, width * 0.5f, rowY + charH * 0.5f, z, back, ink);
            return;
        }
        var e = S.RoadInset; var r = S.RoadRule;
        var x0 = -w * 0.5f + e; var x1 = w * 0.5f - e;
        var y0 = k.Bottom + e; var y1 = k.Top - e;
        StreetShapes.Ink(b, x0, y1 - r, x1, y1, z, back, ink);
        StreetShapes.Ink(b, x0, y0, x1, y0 + r, z, back, ink);
        StreetShapes.Ink(b, x0, y0 + r - S.Joint, x0 + r, y1 - r + S.Joint, z, back, ink);
        StreetShapes.Ink(b, x1 - r, y0 + r - S.Joint, x1, y1 - r + S.Joint, z, back, ink);
        for (var i = 0; i < S.RoadChars; i++)
        {
            var cx0 = -width * 0.5f + i * (charH + gap);
            StreetShapes.Ink(b, cx0, rowY - charH * 0.5f, cx0 + charH, rowY + charH * 0.5f, z, back, ink);
        }
        // Pinyin: three syllables of unequal length, centred under the characters.
        float[] syllables = [0.3f, 0.2f, 0.34f];
        var pinyinH = charH * 0.3f; var py = cy - h * 0.24f;
        var run = (syllables.Sum() + (syllables.Length - 1) * 0.08f) * width;
        var sx = -run * 0.5f;
        foreach (var s in syllables)
        {
            StreetShapes.Ink(b, sx, py - pinyinH * 0.5f, sx + s * width, py + pinyinH * 0.5f, z, back, ink);
            sx += (s + 0.08f) * width;
        }
    }

    private readonly record struct Plate(float Bottom, float Top, float Front, float Back, float PostX, float PostZ);

    private static Plate Layout(SignParams p)
    {
        Validate(p);
        var front = -p.Depth * 0.5f; var back = front + S.RoadPlateThickness;
        var postX = p.Width * 0.5f - S.RoadPostInset;
        var postZ = back - S.Joint + S.RoadPost * 0.5f;
        if (p.Variation == 0) return new(0, p.Height, front, back, postX, postZ);
        var top = p.Height - S.RoadPlateTopGap;
        return new(top - S.RoadPlateHeight, top, front, back, postX, postZ);
    }

    private static void Validate(SignParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown road-name sign variation.");
        if (p.Width < 0.6f || p.Depth < 0.05f) throw new ArgumentOutOfRangeException(nameof(p), "A road-name sign is at least 0.6 m wide and 5 cm deep.");
        if (p.Variation == 0 && (p.Height < 0.15f || p.Height > 0.8f)) throw new ArgumentOutOfRangeException(nameof(p), "A bare road-name plate is 0.15–0.8 m tall.");
        if (p.Variation == 1 && (p.Height < 1.6f || p.Width < 1f || p.Depth < 0.14f)) throw new ArgumentOutOfRangeException(nameof(p), "A two-post road-name sign is at least 1.6 m tall, 1 m wide and 14 cm deep.");
    }
}

/// <summary>
/// Notice board (宣传栏 and 木檐公告板, WLD-LIFE-STREET, ART-AMBIENCE §4): a grey aluminium frame round a glass case of flat posters
/// (low-saturation colours, a title bar and two text bars each; the posters' own text waits for the scene-text atlas), the case
/// standing on two dark posts with its sill 0.8 m up. Width × Height × Depth is the whole board; the case sits at the back (+Z) and is
/// <see cref="StreetStyle.NoticeCaseDepth"/> deep. Variation 0: the plain case (校内主路、宿舍区和食堂门口). Variation 1: with a small
/// warm-wood eave sloping forward over the top, which uses the rest of the depth. Collision: one box over the footprint, Metal.
/// </summary>
[Part(PartCatalog.NoticeBoard, Owner = "WP-13")]
public sealed class NoticeBoardPart : IPartDef<SignParams>
{
    public NoticeBoardPart() => Lods = Levels.Of<SignParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<SignParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(900, 300);

    public Box3 Bounds(SignParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    public IReadOnlyList<Collider> Collision(SignParams p)
    {
        Validate(p);
        return [new BoxCollider(Xf.Move(0, p.Height * 0.5f, 0), new(p.Width * 0.5f, p.Height * 0.5f, p.Depth * 0.5f), SurfaceTag.Prop) { Foot = StreetShapes.Foot(p.Foot, S.MetalFoot) }];
    }

    private PartGeometry Build(SignParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var hx = p.Width * 0.5f; var f = S.NoticeFrame;
        var eave = p.Variation == 1;
        var zB = p.Depth * 0.5f; var zF = zB - S.NoticeCaseDepth;
        var yB = S.NoticeSill; var yT = eave ? p.Height - S.NoticeEaveGap : p.Height;
        var alu = S.Casing(S.GreyMetal); var dark = S.Casing(S.DarkSteel);
        var bevel = near ? 0.01f : 0;
        // Frame: top and bottom bars full width, side bars between them (a joint into both).
        b.AddBox(new(-hx, yT - f, zF), new(hx, yT, zB), bevel, alu);
        b.AddBox(new(-hx, yB, zF), new(hx, yB + f, zB), bevel, alu);
        b.AddBox(new(-hx, yB + f - S.Joint, zF), new(-hx + f, yT - f + S.Joint, zB), bevel, alu);
        b.AddBox(new(hx - f, yB + f - S.Joint, zF), new(hx, yT - f + S.Joint, zB), bevel, alu);
        // Legs under the case, behind its plane; a base plate each near.
        var legX = Math.Min(0.22f, hx - 0.12f);
        foreach (var sx in new[] { -1f, 1f })
        {
            var x = sx * (hx - legX);
            b.AddBox(new(x - S.NoticePost * 0.5f, 0, zB - S.NoticePost), new(x + S.NoticePost * 0.5f, yB + f, zB), near ? 0.01f : 0, dark);
            if (near) b.AddBox(new(x - 0.07f, 0, zB - S.NoticePost - 0.03f), new(x + 0.07f, 0.02f, zB), 0.006f, dark, VertexFlag.NoShadow);
        }
        // Inside the case: the pale back panel, the posters on it, the glass pane in front.
        var ix0 = -hx + f; var ix1 = hx - f; var iy0 = yB + f; var iy1 = yT - f;
        var panelFront = zB - S.NoticePanel;
        b.AddBox(new(ix0 - S.Joint, iy0 - S.Joint, panelFront), new(ix1 + S.Joint, iy1 + S.Joint, zB), 0, S.Board(S.PosterBoard), VertexFlag.NoShadow);
        Posters(b, p, ix0, ix1, iy0, iy1, panelFront, near);
        b.AddBox(new(ix0, iy0, zF + S.NoticeGlassInset), new(ix1, iy1, zF + S.NoticeGlassInset + S.NoticeGlassThick), 0, StreetShapes.Glazing(), VertexFlag.NoShadow);
        if (eave) Eave(b, p, near);
        return b.Finish(Bounds(p));
    }

    /// <summary>A grid of posters: two rows when the case is tall enough, a column per ~0.55 m; heights and colours vary with the cell.</summary>
    private static void Posters(PartBuilder b, SignParams p, float x0, float x1, float y0, float y1, float z, bool near)
    {
        var rows = y1 - y0 >= 0.9f ? 2 : 1;
        var cols = Math.Max(2, (int)MathF.Round((x1 - x0) / 0.55f));
        var cw = (x1 - x0) / cols; var ch = (y1 - y0) / rows;
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
            {
                var i = r * cols + c;
                var w = cw * 0.8f; var h = ch * (0.84f - 0.07f * (i % 3));
                var cx = x0 + (c + 0.5f) * cw; var cy = y1 - (r + 0.5f) * ch;
                var left = cx - w * 0.5f; var top = cy + h * 0.5f;
                var sheet = S.Board(S.Posters[(i + 2 * p.Variation) % S.Posters.Length]);
                StreetShapes.Ink(b, left, cy - h * 0.5f, left + w, top, z, false, sheet, 0.004f);
                if (!near) continue;
                // Title bar across the head, then two text bars; the head colour alternates between the street blue and dark steel.
                var head = S.Board(i % 2 == 0 ? S.SignBlue : S.DarkSteel);
                StreetShapes.Ink(b, left + w * 0.08f, top - h * 0.24f, left + w * 0.92f, top - h * 0.08f, z - 0.004f, false, head, 0.002f);
                var text = S.Board(S.GreyMetal);
                StreetShapes.Ink(b, left + w * 0.12f, top - h * 0.52f, left + w * 0.88f, top - h * 0.46f, z - 0.004f, false, text, 0.002f);
                StreetShapes.Ink(b, left + w * 0.12f, top - h * 0.7f, left + w * 0.7f, top - h * 0.64f, z - 0.004f, false, text, 0.002f);
            }
    }

    /// <summary>Warm-wood eave: a plank sloping down to the front over the whole width, sitting on the case's top bar.</summary>
    private static void Eave(PartBuilder b, SignParams p, bool near)
    {
        var hx = p.Width * 0.5f; var h = p.Height; var d = p.Depth * 0.5f;
        var wood = S.Casing(S.Wood);
        // Side profile (z, y): back top, front top, front bottom, back bottom; a convex quad thicker at the back.
        Vector2[] profile = [new(d, h), new(-d, h - S.NoticeEaveDrop), new(-d, h - S.NoticeEaveDrop - S.NoticeEaveThick), new(d, h - S.NoticeEaveGap)];
        b.AddProfile(profile, -hx, hx, near ? 0.008f : 0, wood);
        if (!near) return;
        // A fascia board along the front edge.
        b.AddBox(new(-hx, h - S.NoticeEaveDrop - S.NoticeEaveThick - 0.035f, -d), new(hx, h - S.NoticeEaveDrop - S.NoticeEaveThick + S.Joint, -d + 0.03f), 0.006f, wood);
    }

    private static void Validate(SignParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown notice-board variation.");
        if (p.Width < 0.8f || p.Height < 1.5f || p.Depth < S.NoticeCaseDepth + 0.03f) throw new ArgumentOutOfRangeException(nameof(p), "A notice board is at least 0.8 × 1.5 m and its depth holds the case.");
        if (p.Variation == 1 && p.Depth < S.NoticeCaseDepth + 0.1f) throw new ArgumentOutOfRangeException(nameof(p), "A board with an eave needs 10 cm of depth beyond the case.");
    }
}
