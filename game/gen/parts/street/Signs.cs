using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Shuttle stop sign (班车站牌, WLD-LIFE §4.3): a blue board with white lettering on a grey pole. Width and Height are the board
/// width and the overall height. The pole stands inside the board's left edge (seen from −Z) and the board is clamped on it,
/// centred in Z, so both faces (−Z and +Z) carry the lettering clear of the pole. Depth is the footprint and must hold the
/// pole's flange. Lettering is the fixed title "班车" and three timetable rules; stop names wait for the scene-text atlas.
/// Collision: the pole as a post, the board as a thin box (its bottom edge is near head height).
/// </summary>
[Part(PartCatalog.BusStop, Owner = "WP-13")]
public sealed class BusStopPart : IPartDef<SignParams>
{
    public BusStopPart() => Lods = Levels.Of<SignParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2);
    public IReadOnlyDictionary<LodLevel, Func<SignParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(260, 100);

    public Box3 Bounds(SignParams p)
    {
        Validate(p);
        var hz = Math.Max(p.Depth * 0.5f, S.StopBoardDepth * 0.5f + 0.006f);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -hz), new(p.Width * 0.5f, p.Height, hz));
    }

    public IReadOnlyList<Collider> Collision(SignParams p)
    {
        Validate(p);
        var (board, poleX) = Layout(p);
        var foot = p.Foot == FootMat.Auto ? S.MetalFoot : p.Foot;
        var bh = S.StopBoardHeight;
        return
        [
            new PostCollider(new(poleX, 0), S.StopPoleRadius + 0.03f, -S.PostColliderSink, p.Height, SurfaceTag.Prop) { Foot = foot },
            new BoxCollider(Xf.Move(0, board + bh * 0.5f, 0), new(p.Width * 0.5f, bh * 0.5f, S.StopBoardDepth * 0.5f + 0.006f), SurfaceTag.Prop) { Foot = foot },
        ];
    }

    private PartGeometry Build(SignParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var (board, poleX) = Layout(p);
        var rp = S.StopPoleRadius;
        var segments = near ? S.RoundSegmentsSmall : S.RoundSegmentsFar;
        var cap = p.Height - 0.03f;
        // Flared foot as one chamfer band, straight pole, dome cap: 96 triangles near (the stop stays within 400, Opus review 10:07).
        var pole = new List<LatheBand>(near
            ? Shapes.Creased(new(0, 0), new(rp + 0.03f, 0), new(rp, 0.03f), new(rp, cap), new(0, cap)).Take(3)
            : Shapes.Creased(new(0, 0), new(rp, 0), new(rp, cap), new(0, cap)).Take(2));
        pole.AddRange(Shapes.Arc(rp, cap, 0, 90, near ? 2 : 1, 0.03f / rp));
        if (lod <= LodLevel.L1) b.AddLathe(pole, segments, S.Pole(S.GreyMetal), Xf.Move(poleX, 0, 0));
        var bh = S.StopBoardHeight; var bd = S.StopBoardDepth; var e = S.StopBorder;
        var front = -bd * 0.5f; var back = bd * 0.5f;
        b.AddBox(new(-p.Width * 0.5f, board, front), new(p.Width * 0.5f, board + bh, back), near ? 0.01f : 0, S.Casing(S.GreyMetal));
        if (lod > LodLevel.L1) return b.Finish(Bounds(p));
        // Blue faces beside the pole strip (the pole is at +X); face coordinates mirror between front (u = −x) and back (u = +x).
        var blue = S.Board(S.SignBlue);
        var x0 = -p.Width * 0.5f + e; var x1 = poleX - rp - 0.02f;
        var fh = bh - 2 * e; var fy = board + e;
        b.AddBox(new(x0, fy, front - 0.004f), new(x1, fy + fh, front + 0.002f), 0, blue);
        b.AddBox(new(x0, fy, back - 0.002f), new(x1, fy + fh, back + 0.004f), 0, blue);
        var ink = S.Ink(S.PaintWhite);
        var fw = x1 - x0; var cx = (x0 + x1) * 0.5f;
        var titleHeight = Math.Min(fh * 0.32f, fw / (Lettering.Width(S.StopTitle, 1) * 1.15f)); var titleY = fy + fh * 0.74f;
        var ruleWidth = fw * 0.72f; var rule = Math.Max(0.022f, fh * 0.03f);
        foreach (var (z, isBack) in new[] { (front - 0.004f, false), (back + 0.004f, true) })
        {
            var u = isBack ? cx : -cx;
            if (near)
            {
                Lettering.Write(b, S.StopTitle, u, titleY, z, isBack, titleHeight, ink);
                for (var k = 0; k < 3; k++)
                {
                    var y = fy + fh * (0.42f - k * 0.13f);
                    Lettering.Segment(b, new(u - ruleWidth * 0.5f + rule, y), new(u + ruleWidth * 0.5f - rule, y), rule, z, isBack, ink);
                }
            }
            else
            {
                // Beyond the near level the strokes are under 4 px: one light block per label (ART-MODELING §4 "干净" 2).
                var w = Lettering.Width(S.StopTitle, titleHeight) * 0.9f;
                Lettering.Segment(b, new(u - w * 0.5f, titleY), new(u + w * 0.5f, titleY), titleHeight * 0.55f, z, isBack, ink);
            }
        }
        return b.Finish(Bounds(p));
    }

    /// <summary>Board bottom height and the pole's centre X (inside the board's left edge seen from −Z, flange flush with it).</summary>
    private static (float Board, float PoleX) Layout(SignParams p) =>
        (p.Height - S.StopBoardTopGap - S.StopBoardHeight, p.Width * 0.5f - S.StopPoleRadius - 0.03f);

    private static void Validate(SignParams p)
    {
        Levels.Dimensions(p);
        if (p.Width < 0.6f || p.Height < S.StopBoardHeight + S.StopBoardTopGap + 1.4f) throw new ArgumentOutOfRangeException(nameof(p), "A shuttle stop is at least 0.6 m wide and keeps its board near head height.");
        if (p.Depth < 2 * (S.StopPoleRadius + 0.03f)) throw new ArgumentOutOfRangeException(nameof(p), "The footprint must hold the pole's flange.");
    }
}

/// <summary>
/// Green open sign (绿色开放牌, WLD-INTERIORS §4, WLD-LIFE §4.11): a white face with a green band carrying "开放" and a green
/// upward arrow (the arrow is self-lit on the signal channel, like the gate arrows it matches). Variation 0: wall plate — Width ×
/// Height × Depth is the plate, origin at its bottom centre, back on +Z (the wall), visual only. Variation 1: free-standing
/// sign by the door — Width is the panel, Height the overall height, Depth the base; collision is one thin box, Metal.
/// Building name and open range need the scene-text atlas; the mainline sign carries the fixed title only.
/// </summary>
[Part(PartCatalog.OpenSign, Owner = "WP-13")]
public sealed class OpenSignPart : IPartDef<SignParams>
{
    private const float PanelDepth = 0.03f;

    public OpenSignPart() => Lods = Levels.Of<SignParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<SignParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(220, 80);

    public Box3 Bounds(SignParams p)
    {
        Validate(p);
        var hz = p.Variation == 0 ? p.Depth * 0.5f : Math.Max(p.Depth * 0.5f, PanelDepth * 0.5f + S.SignPost);
        var front = p.Variation == 0 ? -p.Depth * 0.5f : -Math.Max(p.Depth * 0.5f, PanelDepth * 0.5f);
        return Levels.Padded(new(-p.Width * 0.5f, 0, front - 0.009f), new(p.Width * 0.5f, p.Height, hz));
    }

    public IReadOnlyList<Collider> Collision(SignParams p)
    {
        Validate(p);
        if (p.Variation == 0) return [];
        return [new BoxCollider(Xf.Move(0, p.Height * 0.5f, 0), new(p.Width * 0.5f, p.Height * 0.5f, Math.Max(p.Depth * 0.5f, 0.05f)), SurfaceTag.Prop)
            { Foot = p.Foot == FootMat.Auto ? S.MetalFoot : p.Foot }];
    }

    private PartGeometry Build(SignParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        float y0, ph, front;
        if (p.Variation == 0)
        {
            y0 = 0; ph = p.Height; front = -p.Depth * 0.5f;
            b.AddBox(new(-p.Width * 0.5f, 0, front), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f), near ? Math.Min(0.006f, p.Depth * 0.2f) : 0, S.Casing(S.GreyMetal), VertexFlag.NoShadow);
        }
        else
        {
            ph = p.Height * S.SignPanelRatio; y0 = p.Height - ph; front = -PanelDepth * 0.5f;
            var steel = S.Casing(S.GreyMetal);
            b.AddBox(new(-p.Width * 0.5f, y0, front), new(p.Width * 0.5f, p.Height, PanelDepth * 0.5f), near ? 0.008f : 0, steel);
            var post = S.SignPost; var pz = PanelDepth * 0.5f + post * 0.5f - S.Joint;
            b.AddBox(new(-post * 0.5f, S.SignBaseHeight - S.Joint, pz - post * 0.5f), new(post * 0.5f, y0 + ph * 0.5f, pz + post * 0.5f), near ? 0.006f : 0, steel);
            b.AddBox(new(-p.Width * 0.35f, 0, -p.Depth * 0.5f), new(p.Width * 0.35f, S.SignBaseHeight, p.Depth * 0.5f), near ? 0.008f : 0, S.Casing(S.DarkSteel), VertexFlag.NoShadow);
        }
        // Face: white, the green band across the top, the title in the band, the arrow on the white.
        var inset = Math.Min(S.SignFaceInset, Math.Min(p.Width, ph) * 0.08f);
        var fw = p.Width - 2 * inset; var fh = ph - 2 * inset;
        var fy = y0 + inset;
        b.AddBox(new(-fw * 0.5f, fy, front - 0.003f), new(fw * 0.5f, fy + fh, front + 0.001f), 0, S.Board(S.PaintWhite), VertexFlag.NoShadow);
        var band = fh * S.SignBandRatio;
        b.AddBox(new(-fw * 0.5f, fy + fh - band, front - 0.0045f), new(fw * 0.5f, fy + fh, front - 0.002f), 0, S.Board(S.SignGreen), VertexFlag.NoShadow);
        var arrowSize = Math.Min((fh - band) * 0.66f, fw * 0.3f);
        var arrowY = fy + (fh - band) * 0.5f;
        if (near)
        {
            Lettering.Write(b, S.OpenTitle, 0, fy + fh - band * 0.5f, front - 0.0045f, false, band * 0.6f, S.Ink(S.PaintWhite));
            Lettering.ArrowUp(b, 0, arrowY, arrowSize, front - 0.003f, false, S.Lens(S.SignGreen, S.Signal));
        }
        else
        {
            // Under 4 px the arrow reads as one green mark.
            Lettering.Segment(b, new(0, arrowY - arrowSize * 0.5f), new(0, arrowY + arrowSize * 0.5f), arrowSize * 0.3f, front - 0.003f, false, S.Lens(S.SignGreen, S.Signal));
        }
        return b.Finish(Bounds(p));
    }

    private static void Validate(SignParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown open-sign variation.");
        if (p.Width < 0.15f) throw new ArgumentOutOfRangeException(nameof(p), "An open sign is at least 0.15 m wide.");
        if (p.Variation == 0 && (p.Height < 0.1f || p.Depth < 0.01f)) throw new ArgumentOutOfRangeException(nameof(p), "A wall plate is at least 0.1 m tall and 1 cm thick.");
        if (p.Variation == 1 && (p.Height < 0.9f || p.Depth < 0.15f)) throw new ArgumentOutOfRangeException(nameof(p), "A standing sign is at least 0.9 m tall on a 0.15 m base.");
    }
}

/// <summary>
/// Entrance mat (地垫, WLD-INTERIORS §4): a dark rubber mat in a slightly lighter dark border, lying flat (Height is its
/// thickness). Walkable: no collider; its RubberMat footstep comes from the floor or ground material grid (WLD-LIFE §5).
/// </summary>
[Part(PartCatalog.EntranceMat, Owner = "WP-13")]
public sealed class EntranceMatPart : IPartDef<PanelParams>
{
    public EntranceMatPart() => Lods = Levels.Of<PanelParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<PanelParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(100, 20);

    public Box3 Bounds(PanelParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    private PartGeometry Build(PanelParams p, LodLevel lod)
    {
        Validate(p);
        var b = new PartBuilder();
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f;
        if (lod != LodLevel.L0)
        {
            b.AddBox(new(-hx, 0, -hz), new(hx, p.Height, hz), 0, S.Mat(S.StoneDark), VertexFlag.NoShadow);
            return b.Finish(Bounds(p));
        }
        var rim = Math.Min(S.MatBorder, Math.Min(hx, hz) * 0.2f);
        var lip = Math.Min(0.006f, p.Height * 0.3f);
        b.AddBox(new(-hx, 0, -hz), new(hx, p.Height - lip, hz), Math.Min(0.006f, (p.Height - lip) * 0.4f), S.Mat(S.DarkSteel), VertexFlag.NoShadow);
        b.AddBox(new(-hx + rim, lip, -hz + rim), new(hx - rim, p.Height, hz - rim), Math.Min(0.004f, p.Height * 0.2f), S.Mat(S.StoneDark), VertexFlag.NoShadow);
        return b.Finish(Bounds(p));
    }

    private static void Validate(PanelParams p)
    {
        Levels.Dimensions(p);
        if (p.Width < 0.4f || p.Depth < 0.3f || p.Height > 0.05f || p.Height < 0.01f) throw new ArgumentOutOfRangeException(nameof(p), "An entrance mat is at least 0.4 × 0.3 m and 1–5 cm thick.");
    }
}
