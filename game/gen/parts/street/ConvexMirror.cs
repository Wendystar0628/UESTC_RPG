using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Convex corner mirror (弯道凸面镜, WLD-AMBIENCE): a low-saturation orange pole carrying a round mirror near its top, the mirror
/// a shallow glass dome in an orange frame ring on a dark back plate, facing −Z. Width is the mirror's diameter (0.6–0.8 m), Height the
/// pole's top (3.6–4 m on the street), Depth the footprint that holds the pole's foot behind (+Z) and the mirror in front of it. The
/// mirror is never a live reflection: it takes the glass class, which paints the sky gradient, an opposite-side silhouette and a diagonal
/// streak (ART-STYLE). Collision: the pole as a post, Metal; the mirror is out of reach.
/// </summary>
[Part(PartCatalog.ConvexMirror)]
public sealed class ConvexMirrorPart : IPartDef<SignParams>
{
    public ConvexMirrorPart() => Lods = Levels.Of<SignParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2);
    public IReadOnlyDictionary<LodLevel, Func<SignParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(1100, 380);

    public Box3 Bounds(SignParams p)
    {
        Validate(p);
        return Levels.Padded(new(-Math.Max(p.Width * 0.5f, S.MirrorFlange), 0, -p.Depth * 0.5f), new(Math.Max(p.Width * 0.5f, S.MirrorFlange), p.Height, p.Depth * 0.5f));
    }

    public IReadOnlyList<Collider> Collision(SignParams p)
    {
        Validate(p);
        return [new PostCollider(new(0, PoleZ(p)), S.MirrorFlange, -S.PostColliderSink, p.Height, SurfaceTag.Prop) { Foot = StreetShapes.Foot(p.Foot, S.MetalFoot) }];
    }

    private PartGeometry Build(SignParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var segments = lod switch { LodLevel.L0 => S.RoundSegments, LodLevel.L1 => S.RoundSegmentsSmall, _ => 6 };
        var b = new PartBuilder();
        var orange = S.Pole(S.MirrorOrange);
        var rp = S.MirrorPole; var poleZ = PoleZ(p);
        // Pole: flared foot, straight shaft, dome cap.
        var pole = new List<LatheBand>(near
            ? PartShapes.Creased(new(0, 0), new(S.MirrorFlange, 0), new(S.MirrorFlange, 0.02f), new(rp, 0.045f), new(rp, p.Height - 0.03f), new(0, p.Height - 0.03f)).Take(4)
            : PartShapes.Creased(new(0, 0), new(rp, 0), new(rp, p.Height - 0.03f), new(0, p.Height - 0.03f)).Take(2));
        pole.AddRange(PartShapes.Arc(rp, p.Height - 0.03f, 0, 90, near ? 2 : 1, 0.03f / rp));
        b.AddLathe(pole, segments, orange, Xf.Move(0, 0, poleZ));
        var r = p.Width * 0.5f;
        var yc = MirrorY(p);
        var zBase = poleZ - rp + S.Joint;
        var at = Xf.RotX(-90f).Then(Xf.Move(0, yc, zBase));
        // Back plate and frame ring; the lathe axis (local Y) points out of the pole's front, towards −Z.
        b.AddLathe(PartShapes.Creased(new(0, 0), new(r - 0.01f, 0), new(r - 0.01f, S.MirrorBack), new(0, S.MirrorBack)), segments, S.Casing(S.DarkSteel), at);
        var ring = S.MirrorRing; var depth = S.MirrorRingDepth; var c = near ? 0.012f : 0f;
        b.AddLathe(near
            ? PartShapes.Loop(new(r - ring, 0.01f), new(r, 0.01f), new(r, depth - c), new(r - c, depth), new(r - ring, depth))
            : PartShapes.Loop(new(r - ring, 0.01f), new(r, 0.01f), new(r, depth), new(r - ring, depth)), segments, orange, at);
        // The glass dome, sunk under the ring's inner edge so no gap shows.
        var rd = r - ring + 0.006f; var baseY = S.MirrorBack + 0.005f; var rise = depth + S.MirrorDomeRise - baseY;
        var dome = new List<LatheBand> { LatheBand.Flat(0, baseY, rd, baseY) };
        dome.AddRange(PartShapes.Arc(rd, baseY, 0, 90, near ? 4 : 2, rise / rd));
        b.AddLathe(dome, segments, StreetShapes.Glazing(), at, VertexFlag.NoShadow);
        if (!near) return b.Finish(Bounds(p));
        // Two clamp bands where the mirror meets the pole.
        foreach (var dy in new[] { -(r + 0.05f), r + 0.025f })
            b.AddLathe(PartShapes.Loop(new(rp - 0.005f, yc + dy - 0.02f), new(rp + 0.012f, yc + dy - 0.02f), new(rp + 0.012f, yc + dy + 0.02f), new(rp - 0.005f, yc + dy + 0.02f)),
                S.RoundSegmentsSmall, S.Pole(S.DarkSteel), Xf.Move(0, 0, poleZ), VertexFlag.NoShadow);
        return b.Finish(Bounds(p));
    }

    /// <summary>The pole stands against the back of the footprint, its foot flange just inside it.</summary>
    private static float PoleZ(SignParams p) => p.Depth * 0.5f - S.MirrorFlange - 0.005f;

    /// <summary>The mirror's centre height: its top edge a hand under the pole's cap.</summary>
    private static float MirrorY(SignParams p) => p.Height - p.Width * 0.5f - S.MirrorTopGap;

    private static void Validate(SignParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation != 0) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown convex-mirror variation.");
        if (p.Width < 0.5f || p.Width > 1f) throw new ArgumentOutOfRangeException(nameof(p), "A convex mirror is 0.5–1 m across.");
        if (p.Depth < 0.2f) throw new ArgumentOutOfRangeException(nameof(p), "The footprint must hold the pole's foot and the mirror's projection (0.2 m).");
        if (MirrorY(p) - p.Width * 0.5f < 1.5f) throw new ArgumentOutOfRangeException(nameof(p), "The mirror's lower edge hangs at least 1.5 m up.");
    }
}
