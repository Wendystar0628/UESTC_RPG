using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>Illuminance grade of a lamp marker (MAP-QSH-ADDITIONS): the channel, energy and pool radius follow it.</summary>
public enum LampGrade
{
    /// <summary>Main axis and squares: early first-lamp group (lamp.first), energy 1.</summary>
    One,
    /// <summary>Secondary roads and entrances: ordinary lamp threshold (lamp.street), energy 0.75.</summary>
    Two,
    /// <summary>Landscape: half off from 1:00 to 5:30 (lamp.landscape), energy 0.5.</summary>
    Three,
}

/// <summary>
/// Street lamp (路灯, WLD-LIFE). Height is the top of the head; Width is the disc diameter (variations 0, 2) or the outreach
/// from the pole axis to the tip of the LED head (variation 1); Depth is unused beyond the base. Variation 0: white slim pole with
/// a disc head on the first-lamp group (main roads, ginkgo avenue). 1: grey single-arm LED head over −Z on the street group
/// (secondary roads). 2: the disc lamp on the street group (main lamps in grade-two areas, MAP-QSH-ADDITIONS).
/// No OmniLight3D: the lens is self-lit on its channel and placers hand <see cref="Emitter"/> to the lamp inventory (contract-12).
/// </summary>
[Part(PartCatalog.StreetLamp, Owner = "WP-13")]
public sealed class StreetLampPart : IPartDef<LampParams>
{
    public StreetLampPart() => Lods = Levels.Of<LampParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2);
    public IReadOnlyDictionary<LodLevel, Func<LampParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(640, 170);

    public Box3 Bounds(LampParams p)
    {
        Validate(p);
        if (p.Variation != 1)
        {
            var r = Math.Max(DiscRadius(p), S.LampFlangeRadius);
            return Levels.Padded(new(-r, 0, -r), new(r, p.Height, r));
        }
        var arm = Arm(p);
        var x = Math.Max(S.LampFlangeRadius + 0.02f, S.ArmHeadWidth * 0.5f);
        return Levels.Padded(new(-x, 0, arm.FrontZ), new(x, p.Height, S.LampFlangeRadius + 0.02f));
    }

    public IReadOnlyList<Collider> Collision(LampParams p)
    {
        Validate(p);
        return [new PostCollider(Vector2.Zero, S.LampSleeveRadius + 0.02f, -S.PostColliderSink, p.Height, SurfaceTag.Prop) { Foot = Foot(p) }];
    }

    /// <summary>The schedule group a variation's lens and default marker follow.</summary>
    public static LampGrade DefaultGrade(LampParams p) => p.Variation == 0 ? LampGrade.One : LampGrade.Two;

    /// <summary>
    /// Lamp-inventory marker for a placed lamp (contract-12): just under the lens, warm, with the grade's channel, energy and pool.
    /// <paramref name="at"/> is the placement matrix (row vectors, part-local to world).
    /// </summary>
    public static LightEmitter Emitter(LampParams p, Matrix4x4 at, LampGrade? grade = null)
    {
        Validate(p);
        Vector3 lens;
        if (p.Variation != 1) lens = new(0, p.Height - S.DiscHeadThickness - S.DiscLensDrop - S.MarkerDrop, 0);
        else
        {
            var arm = Arm(p);
            lens = Xf.Point(arm.Head, new(0, -S.ArmHeadThickness * 0.5f - S.MarkerDrop, 0));
        }
        var (channel, energy, range) = Marker(grade ?? DefaultGrade(p));
        return new(channel, Xf.Point(at, lens), S.LampWarm, energy, range);
    }

    internal static (ChannelId Channel, float Energy, float Range) Marker(LampGrade grade) => grade switch
    {
        LampGrade.One => (S.FirstGroup, S.GradeOneEnergy, S.GradeOneRange),
        LampGrade.Two => (S.StreetGroup, S.GradeTwoEnergy, S.GradeTwoRange),
        LampGrade.Three => (S.Landscape, S.GradeThreeEnergy, S.GradeTwoRange * 0.6f),
        _ => throw new ArgumentOutOfRangeException(nameof(grade), grade, "Unknown lamp grade."),
    };

    private PartGeometry Build(LampParams p, LodLevel lod)
    {
        Validate(p);
        var builder = new PartBuilder();
        var segments = lod switch { LodLevel.L0 => S.RoundSegments, LodLevel.L1 => S.RoundSegmentsFar, _ => 6 };
        var lens = S.Lens(S.Diffuser, Marker(DefaultGrade(p)).Channel);
        if (p.Variation != 1) BuildDisc(builder, p, lod, segments, lens);
        else BuildArm(builder, p, lod, segments, lens);
        return builder.Finish(Bounds(p));
    }

    private static void BuildDisc(PartBuilder b, LampParams p, LodLevel lod, int segments, ToonSpec lens)
    {
        var near = lod == LodLevel.L0;
        var white = S.Pole(S.PaintWhite);
        var r = DiscRadius(p);
        var yb = p.Height - S.DiscHeadThickness;
        var rimTop = yb + S.DiscRimHeight;
        var chamfer = near ? S.DiscChamfer : 0;
        if (lod <= LodLevel.L1) AddFoot(b, p, near, segments, white);
        // Pole, slightly tapered, into the head.
        var poleFrom = lod <= LodLevel.L1 ? S.LampSleeveHeight - 0.01f : 0f;
        b.AddLathe(PartShapes.Creased(new(0, poleFrom), new(S.LampPoleBaseRadius, poleFrom), new(S.LampPoleTopRadius, yb + 0.05f), new(0, yb + 0.05f)), segments, white, Matrix4x4.Identity);
        // Disc head: flat underside, chamfered rim, shallow smooth dome.
        var head = new List<LatheBand> { LatheBand.Flat(0, yb, r - chamfer, yb) };
        if (near) head.Add(LatheBand.Flat(r - chamfer, yb, r, yb + chamfer));
        head.Add(new(r, yb + chamfer, r, rimTop, new(1, 0), new(1, 0)));
        head.AddRange(PartShapes.Arc(r, rimTop, 0, 90, near ? 4 : 2, (p.Height - rimTop) / r));
        b.AddLathe(head, segments, white, Matrix4x4.Identity);
        if (lod > LodLevel.L1) return;
        // Diffuser: the self-lit disc under the head, proud by the lens drop.
        var rl = r - S.DiscLensInset; var yl = yb - S.DiscLensDrop;
        b.AddLathe(near
            ? PartShapes.Creased(new(0, yl), new(rl - 0.008f, yl), new(rl, yl + 0.008f), new(rl, yb + S.Joint), new(0, yb + S.Joint))
            : PartShapes.Creased(new(0, yl), new(rl, yl), new(rl, yb + S.Joint), new(0, yb + S.Joint)), segments, lens, Matrix4x4.Identity, VertexFlag.NoShadow);
    }

    private static void BuildArm(PartBuilder b, LampParams p, LodLevel lod, int segments, ToonSpec lens)
    {
        var near = lod == LodLevel.L0;
        var grey = S.Pole(S.GreyMetal);
        var arm = Arm(p);
        if (lod <= LodLevel.L1) AddFoot(b, p, near, segments, grey);
        var poleFrom = lod <= LodLevel.L1 ? S.LampSleeveHeight - 0.01f : 0f;
        var poleTop = arm.Joint.Y + 0.14f;
        b.AddLathe(PartShapes.Creased(new(0, poleFrom), new(S.LampPoleBaseRadius, poleFrom), new(S.LampPoleTopRadius, poleTop), new(0, poleTop)), segments, grey, Matrix4x4.Identity);
        if (near)
        {
            // Rounded pole cap and the arm clamp.
            var cap = new List<LatheBand> { LatheBand.Flat(0, poleTop - 0.02f, S.LampPoleTopRadius + 0.012f, poleTop - 0.02f) };
            cap.Add(new(S.LampPoleTopRadius + 0.012f, poleTop - 0.02f, S.LampPoleTopRadius + 0.012f, poleTop, new(1, 0), new(1, 0)));
            cap.AddRange(PartShapes.Arc(S.LampPoleTopRadius + 0.012f, poleTop, 0, 90, 2, 0.45f));
            b.AddLathe(cap, segments, grey, Matrix4x4.Identity);
            b.AddLathe(PartShapes.Creased(new(0, arm.Joint.Y - 0.08f), new(S.LampPoleTopRadius + 0.02f, arm.Joint.Y - 0.08f), new(S.LampPoleTopRadius + 0.02f, arm.Joint.Y + 0.08f), new(0, arm.Joint.Y + 0.08f)),
                segments, S.Pole(S.DarkSteel), Matrix4x4.Identity);
        }
        if (lod <= LodLevel.L1)
            b.AddPipe(arm.Joint, arm.Tip, S.ArmRadius, near ? S.RoundSegmentsSmall : 6, grey);
        b.AddCentredBox(new(S.ArmHeadWidth, S.ArmHeadThickness, S.ArmHeadLength), arm.Head, S.Casing(S.GreyMetal), near ? 0.018f : 0);
        if (lod <= LodLevel.L1)
            b.AddCentredBox(new(S.ArmHeadWidth - 0.08f, 0.014f, S.ArmHeadLength - 0.12f), Xf.Move(0, -S.ArmHeadThickness * 0.5f - 0.003f, 0).Then(arm.Head), lens, near ? 0.004f : 0, VertexFlag.NoShadow);
    }

    /// <summary>Base flange, sleeve and its accent ring: the pole's contact with the ground (ART-MODELING item 3).</summary>
    private static void AddFoot(PartBuilder b, LampParams p, bool near, int segments, ToonSpec body)
    {
        var rs = S.LampSleeveRadius; var hs = S.LampSleeveHeight; var c = near ? 0.03f : 0;
        b.AddLathe(near
            ? PartShapes.Creased(new(0, 0), new(rs, 0), new(rs, hs - c), new(rs - c, hs), new(0, hs))
            : PartShapes.Creased(new(0, 0), new(rs, 0), new(rs, hs), new(0, hs)), segments, body, Matrix4x4.Identity);
        if (!near) return;
        var accent = p.Variation == 1 ? S.DarkSteel : S.GreyMetal;
        var small = S.RoundSegmentsSmall;
        var rf = S.LampFlangeRadius; var hf = S.LampFlangeHeight;
        b.AddLathe(PartShapes.Creased(new(0, 0), new(rf, 0), new(rf, hf - 0.01f), new(rf - 0.01f, hf), new(0, hf)), small, S.Casing(accent), Matrix4x4.Identity, VertexFlag.NoShadow);
        var y0 = hs - c - 0.03f - S.LampRingHeight;
        b.AddLathe(PartShapes.Creased(new(0, y0), new(rs + 0.006f, y0), new(rs + 0.006f, y0 + S.LampRingHeight), new(0, y0 + S.LampRingHeight)), small, S.Pole(accent), Matrix4x4.Identity, VertexFlag.NoShadow);
    }

    private static float DiscRadius(LampParams p) => Math.Max(p.Width, S.DiscMinDiameter) * 0.5f;

    /// <summary>Single-arm geometry: the joint on the pole, the arm tip inside the head, the head frame (−Z front tilted up).</summary>
    private static (Vector3 Joint, Vector3 Tip, Matrix4x4 Head, float FrontZ) Arm(LampParams p)
    {
        var rise = S.ArmRiseDeg * (float.Pi / 180f);
        var c = (float)DetMath.Cos(rise); var s = (float)DetMath.Sin(rise);
        var reach = Math.Max(p.Width - S.ArmHeadLength, S.ArmMinReach);
        var half = S.ArmHeadLength * 0.5f;
        var centreZ = -(reach + half * c);
        var centreY = p.Height - S.ArmHeadThickness * 0.5f * c - half * s - 0.004f;
        // Head frame: local −Z is the outward, rising arm direction; rows are the images of local X, Y, Z.
        var head = new Matrix4x4(1, 0, 0, 0, 0, c, s, 0, 0, -s, c, 0, 0, centreY, centreZ, 1);
        var jointY = centreY - (reach + half * c) * s / c;
        var tip = Xf.Point(head, new(0, 0, half - 0.06f));
        var frontZ = centreZ - half * c - S.ArmHeadThickness * 0.5f * s;
        return (new(0, jointY, 0), tip, head, frontZ);
    }

    private static Campus.Contracts.Audio.FootMat Foot(LampParams p) => p.Foot == Campus.Contracts.Audio.FootMat.Auto ? S.MetalFoot : p.Foot;

    private static void Validate(LampParams p)
    {
        Levels.Dimensions(p);
        if (p.Height < 2.5f) throw new ArgumentOutOfRangeException(nameof(p), "A street lamp is at least 2.5 m tall.");
        if (p.Variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown street-lamp variation.");
    }
}

/// <summary>
/// Entrance lamp fixture (门口灯, WLD-INTERIORS): a wall lantern with a light back plate, a dark housing and frosted lenses on
/// the front (−Z) and the underside. Origin is the bottom centre; the back face lies on +Z (the wall). Visual only: it hangs
/// above head height on the wall, whose collider stands in for it. Door lamps are lamp fixtures in the lamp table on the
/// first-lamp group (on at dusk, on all night, never half off); no window or interior channel (WP-39, max-104).
/// </summary>
[Part(PartCatalog.EntranceLamp, Owner = "WP-13")]
public sealed class EntranceLampPart : IPartDef<LampParams>
{
    public EntranceLampPart() => Lods = Levels.Of<LampParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<LampParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(240, 60);

    public Box3 Bounds(LampParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, -0.008f, -p.Depth * 0.5f - 0.008f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    /// <summary>Lamp-inventory marker in front of and below the lantern: first-lamp group, grade-two energy (entrances, MAP-QSH-ADDITIONS).</summary>
    public static LightEmitter Emitter(LampParams p, Matrix4x4 at)
    {
        Validate(p);
        var y0 = p.Height * (1 - S.DoorLampHousingRatio) * 0.5f;
        return new(S.FirstGroup, Xf.Point(at, new(0, y0 - S.MarkerDrop, -p.Depth * 0.5f - 0.04f)), S.LampWarm, S.GradeTwoEnergy, S.DoorLampRange);
    }

    private PartGeometry Build(LampParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var back = p.Depth * 0.5f; var plate = S.DoorLampBackPlate;
        b.AddBox(new(-p.Width * 0.5f, 0, back - plate), new(p.Width * 0.5f, p.Height, back), near ? 0.006f : 0, S.Casing(S.GreyMetal), VertexFlag.NoShadow);
        var w = p.Width * S.DoorLampHousingRatio; var h = p.Height * S.DoorLampHousingRatio;
        var y0 = (p.Height - h) * 0.5f; var front = -p.Depth * 0.5f;
        b.AddBox(new(-w * 0.5f, y0, front), new(w * 0.5f, y0 + h, back - plate + S.Joint), near ? 0.012f : 0, S.Casing(S.DarkSteel), VertexFlag.NoShadow);
        var lens = S.Lens(S.Diffuser, S.FirstGroup);
        var lw = w * S.DoorLampLensRatio; var lh = h * S.DoorLampLensRatio;
        b.AddBox(new(-lw * 0.5f, y0 + (h - lh) * 0.5f, front - 0.006f), new(lw * 0.5f, y0 + (h + lh) * 0.5f, front + 0.004f), near ? 0.003f : 0, lens, VertexFlag.NoShadow);
        if (near)
            b.AddBox(new(-lw * 0.5f, y0 - 0.006f, front + 0.02f), new(lw * 0.5f, y0 + 0.004f, front + 0.02f + (p.Depth - plate) * 0.5f), 0.003f, lens, VertexFlag.NoShadow);
        return b.Finish(Bounds(p));
    }

    private static void Validate(LampParams p)
    {
        Levels.Dimensions(p);
        if (p.Width < 0.12f || p.Height < 0.12f || p.Depth < 0.06f) throw new ArgumentOutOfRangeException(nameof(p), "An entrance lamp is at least 0.12 × 0.12 × 0.06 m.");
    }
}
