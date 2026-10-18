using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Street fence (道路护栏, ART-AMBIENCE §6, MAP-QSH-ADDITIONS-VOCAB §5), a run along X: Width is its length, Height its top, Depth its
/// thickness, PostSpacing the distance between posts. Variation 0: the pipe pedestrian rail (外围管式步道栏) of round posts, two
/// horizontal tubes and thin uprights, grey aluminium. 1: the see-through boundary railing (校界透空栏杆), dense dark uprights between
/// two rails and light stone piers every <c>PostSpacing</c>. 2: the warm-white W-beam guardrail (波形护栏) on grey posts, for slopes
/// and car parks. The uprights go beyond the near level into the rails (ART-MODELING: they fade first). Collision: one box over the
/// run, Metal.
/// </summary>
[Part(PartCatalog.StreetFence, Owner = "WP-13")]
public sealed class StreetFencePart : IPartDef<RailingParams>
{
    public StreetFencePart() => Lods = Levels.Of<RailingParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<RailingParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(1100, 260);

    public Box3 Bounds(RailingParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    public IReadOnlyList<Collider> Collision(RailingParams p)
    {
        Validate(p);
        return [new BoxCollider(Xf.Move(0, p.Height * 0.5f, 0), new(p.Width * 0.5f, p.Height * 0.5f, Math.Max(p.Depth * 0.5f, S.FenceColliderHalf)), SurfaceTag.Prop)
            { Foot = StreetShapes.Foot(p.Foot, S.MetalFoot) }];
    }

    private PartGeometry Build(RailingParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        switch (p.Variation)
        {
            case 0: Pipe(b, p, near); break;
            case 1: Bars(b, p, near); break;
            default: Beam(b, p, near); break;
        }
        return b.Finish(Bounds(p));
    }

    /// <summary>Post centres, evenly spread so the end posts stand <paramref name="inset"/> inside the run and no gap exceeds the spacing.</summary>
    private static float[] Posts(RailingParams p, float inset)
    {
        var span = p.Width - 2 * inset;
        var count = Math.Max(1, (int)MathF.Ceiling(span / p.PostSpacing));
        var xs = new float[count + 1];
        for (var i = 0; i <= count; i++) xs[i] = -p.Width * 0.5f + inset + span * i / count;
        return xs;
    }

    private static void Pipe(PartBuilder b, RailingParams p, bool near)
    {
        var rp = S.FencePostRadius; var segments = near ? S.RoundSegmentsSmall : S.RoundSegmentsFar;
        var metal = S.Pole(S.GreyMetal);
        var xs = Posts(p, rp);
        foreach (var x in xs)
        {
            var bands = new List<LatheBand>(PartShapes.Creased(new(0, 0), new(rp, 0), new(rp, p.Height - rp), new(0, p.Height - rp)).Take(2));
            bands.AddRange(PartShapes.Arc(rp, p.Height - rp, 0, 90, near ? 2 : 1));
            b.AddLathe(bands, segments, metal, Xf.Move(x, 0, 0));
        }
        var rail = S.FenceRail;
        foreach (var y in new[] { p.Height - rp - rail, p.Height * 0.5f })
            b.AddPipe(new(xs[0], y, 0), new(xs[^1], y, 0), rail, near ? 8 : 6, metal);
        if (!near) return;
        // Uprights between the rails, clear of the posts.
        var pitch = S.FenceUprightPitch; var r = S.FenceUpright;
        var start = xs[0] + rp + pitch * 0.5f; var end = xs[^1] - rp - pitch * 0.5f;
        var n = Math.Max(0, (int)MathF.Floor((end - start) / pitch));
        for (var i = 0; i <= n; i++)
        {
            var x = start + (end - start) * i / Math.Max(1, n);
            if (xs.Any(px => MathF.Abs(px - x) < rp + r + 0.02f)) continue;
            b.AddPipe(new(x, 0.1f, 0), new(x, p.Height - rp - rail, 0), r, 5, metal, VertexFlag.NoShadow);
        }
    }

    private static void Bars(PartBuilder b, RailingParams p, bool near)
    {
        var pier = Math.Min(p.Depth - 0.03f, S.FencePier);
        var dark = S.Casing(S.DarkSteel);
        var stone = S.Stone(S.BodyColour(p.Material, S.StoneWhite), false);
        var xs = Posts(p, pier * 0.5f + 0.015f);
        foreach (var x in xs)
        {
            b.AddBox(new(x - pier * 0.5f, 0, -pier * 0.5f), new(x + pier * 0.5f, p.Height - S.FencePierCap + S.Joint, pier * 0.5f), near ? 0.012f : 0, stone);
            b.AddBox(new(x - pier * 0.5f - 0.015f, p.Height - S.FencePierCap, -pier * 0.5f - 0.015f), new(x + pier * 0.5f + 0.015f, p.Height, pier * 0.5f + 0.015f), near ? 0.01f : 0, stone);
        }
        // Two rails run the whole length between the piers; the dense uprights stand between them (not in the far levels).
        var rail = S.FenceBarRail; var top = p.Height - S.FencePierCap - 0.04f;
        b.AddBox(new(xs[0], top - rail, -S.FenceBar * 0.7f), new(xs[^1], top, S.FenceBar * 0.7f), near ? 0.004f : 0, dark);
        b.AddBox(new(xs[0], 0.1f, -S.FenceBar * 0.7f), new(xs[^1], 0.1f + rail, S.FenceBar * 0.7f), near ? 0.004f : 0, dark);
        if (!near) return;
        var thin = S.Metal(S.DarkSteel);
        for (var i = 0; i + 1 < xs.Length; i++)
        {
            var start = xs[i] + pier * 0.5f - S.Joint; var end = xs[i + 1] - pier * 0.5f + S.Joint;
            var n = Math.Max(1, (int)MathF.Floor((end - start) / S.FenceBarPitch));
            var pitch = (end - start) / n;
            for (var k = 0; k < n; k++)
            {
                var x = start + (k + 0.5f) * pitch;
                b.AddBox(new(x - S.FenceBar * 0.5f, 0.1f + rail - S.Joint, -S.FenceBar * 0.5f), new(x + S.FenceBar * 0.5f, top - rail + S.Joint, S.FenceBar * 0.5f), 0, thin, VertexFlag.NoShadow);
            }
        }
    }

    /// <summary>W-beam: two flanges and a recessed web as three stacked boards, on posts behind them.</summary>
    private static void Beam(PartBuilder b, RailingParams p, bool near)
    {
        var white = S.Casing(S.PaintWhite); var grey = S.Casing(S.GreyMetal);
        var xs = Posts(p, S.GuardPost * 0.5f);
        var back = p.Depth * 0.5f; var t = S.GuardBeamThick;
        foreach (var x in xs)
            b.AddBox(new(x - S.GuardPost * 0.5f, 0, back - S.GuardPostDepth), new(x + S.GuardPost * 0.5f, p.Height - 0.03f, back), near ? 0.008f : 0, grey);
        // The beam stands in front of the posts, between the top of the post and 0.3 m under it.
        var h = S.GuardBeamHeight; var y0 = p.Height - h;
        var zf = back - S.GuardPostDepth - t;
        var x0 = xs[0] - S.GuardPost * 0.5f; var x1 = xs[^1] + S.GuardPost * 0.5f;
        b.AddBox(new(x0, y0, zf), new(x1, y0 + h * 0.36f, zf + t), near ? 0.008f : 0, white);
        b.AddBox(new(x0, y0 + h * 0.36f - S.Joint, zf + S.GuardRecess), new(x1, y0 + h * 0.64f + S.Joint, zf + t + S.GuardRecess), 0, white);
        b.AddBox(new(x0, y0 + h * 0.64f, zf), new(x1, p.Height, zf + t), near ? 0.008f : 0, white);
    }

    private static void Validate(RailingParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown street-fence variation.");
        if (!float.IsFinite(p.PostSpacing) || p.PostSpacing < 0.5f) throw new ArgumentOutOfRangeException(nameof(p), "Fence posts are at least 0.5 m apart.");
        if (p.Width < 1f || p.Height < 0.5f || p.Depth < 0.08f) throw new ArgumentOutOfRangeException(nameof(p), "A fence run is at least 1 m long, 0.5 m high and 8 cm thick.");
        if (p.Variation == 1 && (p.Height < 1.6f || p.Depth < 0.15f)) throw new ArgumentOutOfRangeException(nameof(p), "A boundary railing is at least 1.6 m high with piers 15 cm deep.");
        if (p.Variation == 2 && (p.Height > 1.2f || p.Height < 0.6f || p.Depth < S.GuardPostDepth + S.GuardBeamThick + S.GuardRecess)) throw new ArgumentOutOfRangeException(nameof(p), "A guardrail is 0.6–1.2 m high and deep enough for post and beam.");
    }
}
