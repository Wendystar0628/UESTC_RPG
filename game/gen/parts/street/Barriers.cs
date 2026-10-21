using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Bollard (护柱, WLD-LIFE). Width is the diameter (or the square side), Height the top. Variation 0: stainless round
/// bollard with a dome cap, a flared foot and one dark reflector band. 1: yellow-and-black crash bollard (南门广场,
/// MAP-QSH-SOUTH), one broad black stripe. 2: square stone bollard with a stepped cap. Round bollards stay within 120
/// triangles near (Opus review 10:07): ten segments with smooth normals, the foot and dome rounded by normals, not by bands. A caller's Material (other than the record default) colours the
/// body, so a site's marking paint or stone carries through. Collision: one vertical post, Metal (stone: StonePaving).
/// </summary>
[Part(PartCatalog.Bollard)]
public sealed class BollardPart : IPartDef<ColumnParams>
{
    public BollardPart() => Lods = Levels.Of<ColumnParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<ColumnParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(120, 60);

    public Box3 Bounds(ColumnParams p)
    {
        Validate(p);
        var r = p.Variation == 2 ? p.Width * 0.5f : p.Width * 0.5f + S.BollardFlange;
        return Levels.Padded(new(-r, 0, -r), new(r, p.Height, r));
    }

    public IReadOnlyList<Collider> Collision(ColumnParams p)
    {
        Validate(p);
        var stone = p.Variation == 2;
        var r = stone ? p.Width * 0.5f * 1.414f : p.Width * 0.5f + S.BollardFlange;
        var foot = p.Foot != FootMat.Auto ? p.Foot : stone ? S.StoneFoot : S.MetalFoot;
        return [new PostCollider(Vector2.Zero, r, -S.PostColliderSink, p.Height, SurfaceTag.Prop) { Foot = foot }];
    }

    private PartGeometry Build(ColumnParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        switch (p.Variation)
        {
            case 0:
            case 1:
                {
                    var crash = p.Variation == 1;
                    var body = S.Pole(S.BodyColour(p.Material, crash ? S.SafetyYellow : S.Stainless));
                    var r = p.Width * 0.5f; var f = S.BollardFlange;
                    var dome = r * 0.55f; var shoulder = p.Height - dome;
                    var segments = near ? S.BollardSegments : S.BollardSegmentsFar;
                    // Flared foot (one chamfer band from the flange edge on the ground up to the body), straight body, two-step
                    // dome; smooth lathe normals do the rounding. The profile's closing edge on the axis is left out.
                    var bands = near
                        ? new List<LatheBand>(PartShapes.Creased(new(0, 0), new(r + f, 0), new(r, S.BollardFlareHeight), new(r, shoulder), new(0, shoulder)).Take(3))
                        : new List<LatheBand>(PartShapes.Creased(new(0, 0), new(r, 0), new(r, shoulder), new(0, shoulder)).Take(2));
                    bands.AddRange(PartShapes.Arc(r, shoulder, 0, 90, near ? 2 : 1, dome / r));
                    b.AddLathe(bands, segments, body, Matrix4x4.Identity);
                    // One dark band: the stainless bollard's reflector ring, the crash bollard's black stripe between yellow.
                    var top = crash ? p.Height * S.CrashBandTop : p.Height - S.BollardBandDrop;
                    var y0 = top - (crash ? S.CrashBandHeight : S.BollardBandHeight);
                    b.AddLathe(PartShapes.Creased(new(0, y0), new(r + 0.004f, y0), new(r + 0.004f, top), new(0, top)), segments, S.Pole(crash ? S.HazardBlack : S.DarkSteel), Matrix4x4.Identity);
                    break;
                }
            case 2:
                {
                    var stone = S.Stone(S.BodyColour(p.Material, S.StoneGrey), false);
                    var w = p.Width; var cap = Math.Min(0.06f, p.Height * 0.15f);
                    b.AddBox(new(-w * 0.5f, 0, -w * 0.5f), new(w * 0.5f, p.Height - cap + S.Joint, w * 0.5f), near ? Math.Min(0.02f, w * 0.12f) : 0, stone);
                    var inset = Math.Min(0.015f, w * 0.1f);
                    b.AddBox(new(-w * 0.5f + inset, p.Height - cap, -w * 0.5f + inset), new(w * 0.5f - inset, p.Height, w * 0.5f - inset), near ? Math.Min(0.015f, cap * 0.3f) : 0, stone);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown bollard variation.");
        }
        return b.Finish(Bounds(p));
    }

    private static void Validate(ColumnParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown bollard variation.");
        if (p.Width < 0.08f || p.Height < 0.3f || p.Height < p.Width) throw new ArgumentOutOfRangeException(nameof(p), "A bollard is at least 0.08 m across, 0.3 m tall, and taller than wide.");
    }
}

/// <summary>
/// Stone sphere (石球, WLD-LIFE): a polished stone ball sunk a little into a low round plinth. The ball's diameter is the
/// smallest of Width, Depth and Height (less the plinth); its top is Height. Variation 0: white stone (main crossings). 1: grey
/// stone (南门广场). A caller's Material colours both. The ball sits on the plinth, so its top is that smallest size.
/// Collision: a post as tall as the ball — the contracts have no sphere primitive yet (WLD-LIFE asks for one; requested by game/gen/furniture/).
/// </summary>
[Part(PartCatalog.StoneSphere)]
public sealed class StoneSpherePart : IPartDef<FurnitureParams>
{
    public StoneSpherePart() => Lods = Levels.Of<FurnitureParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<FurnitureParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(320, 90);

    public Box3 Bounds(FurnitureParams p)
    {
        var r = Radius(p);
        return Levels.Padded(new(-r, 0, -r), new(r, Top(p), r));
    }

    public IReadOnlyList<Collider> Collision(FurnitureParams p)
    {
        var r = Radius(p);
        return [new PostCollider(Vector2.Zero, r, -S.PostColliderSink, Top(p), SurfaceTag.Prop) { Foot = p.Foot == FootMat.Auto ? S.StoneFoot : p.Foot }];
    }

    private PartGeometry Build(FurnitureParams p, LodLevel lod)
    {
        var r = Radius(p);
        var near = lod == LodLevel.L0;
        var colour = S.BodyColour(p.Material, p.Variation == 1 ? S.StoneGrey : S.StoneWhite);
        var b = new PartBuilder();
        var centre = Top(p) - r;
        b.AddLathe([.. PartShapes.Arc(r, centre, -90, 90, near ? 8 : 4)], near ? S.RoundSegments : S.RoundSegmentsFar, S.Stone(colour, true), Matrix4x4.Identity);
        // The plinth is a separate stone material, so the ball meets it with a material line, not a seam (ART-MODELING item 3).
        var rp = r * S.SpherePlinthRatio; var hp = S.SpherePlinthHeight;
        b.AddLathe(near
            ? PartShapes.Creased(new(0, 0), new(rp, 0), new(rp, hp - 0.01f), new(rp - 0.01f, hp), new(0, hp))
            : PartShapes.Creased(new(0, 0), new(rp, 0), new(rp, hp), new(0, hp)), near ? S.RoundSegmentsSmall : S.RoundSegmentsFar, S.Stone(colour, false), Matrix4x4.Identity, VertexFlag.NoShadow);
        return b.Finish(Bounds(p));
    }

    private static float Top(FurnitureParams p) => Math.Min(p.Height, Math.Min(p.Width, p.Depth));

    private static float Radius(FurnitureParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown stone-sphere variation.");
        var d = Math.Min(p.Height, Math.Min(p.Width, p.Depth));
        if (d < 0.2f) throw new ArgumentOutOfRangeException(nameof(p), "A stone sphere is at least 0.2 m across.");
        return (d - S.SpherePlinthHeight + S.SphereSink) * 0.5f;
    }
}
