using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Round wall clock (外墙圆钟, ART-AMBIENCE §2): a white face in a grey aluminium bezel on a dark drum, twelve black ticks and two
/// black hands with a centre cap, facing −Z with its back on +Z (the wall). The face's diameter is the smaller of Width and Height
/// (1.2–1.8 m on the two stair towers, smaller for the catalogue default); Depth is the drum. The hands are drawn at 17:30, the default
/// state; the world clock turning them is the runtime's business (the part is static geometry, the same at every hour and season). It
/// does not glow at night (ART-AMBIENCE §2: dark, or a faint ring light behind it that the placer's lamp table supplies). Visual only:
/// it hangs on a stair tower, far out of reach.
/// </summary>
[Part(PartCatalog.WallClock, Owner = "WP-13")]
public sealed class WallClockPart : IPartDef<SignParams>
{
    public WallClockPart() => Lods = Levels.Of<SignParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<SignParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(700, 220);

    public Box3 Bounds(SignParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    private PartGeometry Build(SignParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var segments = near ? S.ClockSegments : S.RoundSegments;
        var b = new PartBuilder();
        var r = Math.Min(p.Width, p.Height) * 0.5f; var d = p.Depth;
        var cy = p.Height * 0.5f;
        var bezelW = Math.Max(r * S.ClockBezelRatio, 0.015f);
        // The lathe's axis points out of the wall: local Y runs from the wall (y = 0, on +Z) to the face's side (y = d, towards −Z).
        var at = Xf.RotX(-90f).Then(Xf.Move(0, cy, d * 0.5f));
        var faceTop = d - S.ClockFaceSink;
        b.AddLathe(PartShapes.Creased(new(0, 0), new(r - S.ClockDrumInset, 0), new(r - S.ClockDrumInset, faceTop - 0.006f), new(0, faceTop - 0.006f)), segments, S.Casing(S.DarkSteel), at);
        var c = near ? Math.Min(0.012f, bezelW * 0.4f) : 0f;
        var ringFoot = faceTop - 0.012f;
        b.AddLathe(near
            ? PartShapes.Loop(new(r - bezelW, ringFoot), new(r, ringFoot), new(r, d - c), new(r - c, d), new(r - bezelW, d))
            : PartShapes.Loop(new(r - bezelW, ringFoot), new(r, ringFoot), new(r, d), new(r - bezelW, d)), segments, S.Pole(S.GreyMetal), at);
        var rf = r - bezelW;
        b.AddLathe(PartShapes.Creased(new(0, ringFoot), new(rf + 0.004f, ringFoot), new(rf + 0.004f, faceTop), new(0, faceTop)), segments, S.Board(S.PaintWhite), at, VertexFlag.NoShadow);
        // Everything below stands on the face plane, seen from −Z: u (the viewer's right) is −X, 12 o'clock is +Y.
        var faceZ = -d * 0.5f + S.ClockFaceSink;
        var ink = S.Board(S.HazardBlack);
        if (near)
            for (var i = 0; i < 12; i++)
            {
                var major = i % 3 == 0;
                var len = rf * (major ? S.ClockMajorLen : S.ClockMinorLen);
                var width = Math.Max(rf * (major ? S.ClockMajorWidth : S.ClockMinorWidth), 0.006f);
                Stroke(b, i * 30f, rf * S.ClockTickRing - len * 0.5f, len, width, cy, faceZ, S.ClockTickThick, ink);
            }
        // Hands at 17:30: the hour hand 5.5/12 of the way round, the minute hand at the half hour. Each reaches its length forward
        // and a short tail behind the centre.
        var tail = rf * S.ClockTail;
        var hourReach = rf * S.ClockHourLen; var minuteReach = rf * S.ClockMinuteLen;
        Stroke(b, 165f, (hourReach - tail) * 0.5f, hourReach + tail, Math.Max(rf * S.ClockHourWidth, 0.01f), cy, faceZ - S.ClockTickThick, S.ClockHandThick, ink);
        Stroke(b, 180f, (minuteReach - tail) * 0.5f, minuteReach + tail, Math.Max(rf * S.ClockMinuteWidth, 0.008f), cy, faceZ - S.ClockTickThick - S.ClockHandThick, S.ClockHandThick, ink);
        b.AddDisc(0, cy, faceZ - S.ClockTickThick - 2 * S.ClockHandThick - 0.004f, faceZ + S.Joint, Math.Max(rf * 0.045f, 0.012f), near ? 10 : 6, ink, VertexFlag.NoShadow);
        return b.Finish(Bounds(p));
    }

    /// <summary>
    /// A bar on the face plane: <paramref name="phi"/> degrees clockwise from 12 o'clock, its middle <paramref name="mid"/> from the
    /// centre along that direction, <paramref name="length"/> long, <paramref name="thick"/> deep with its back sunk a joint into the
    /// face plane <paramref name="faceZ"/>. Out of the shadow pass.
    /// </summary>
    private static void Stroke(PartBuilder b, float phi, float mid, float length, float width, float cy, float faceZ, float thick, ToonSpec material)
    {
        var a = phi * Angles.DegreesToRadians;
        // Seen from the front, clockwise runs from +Y towards the viewer's right, which is -X.
        var dx = -MathF.Sin(a); var dy = MathF.Cos(a);
        var at = new Matrix4x4(dx, dy, 0, 0, -dy, dx, 0, 0, 0, 0, 1, 0, dx * mid, cy + dy * mid, faceZ - thick * 0.5f + S.Joint * 0.5f, 1);
        b.AddCentredBox(new(length, width, thick + S.Joint), at, material, 0, VertexFlag.NoShadow);
    }

    private static void Validate(SignParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation != 0) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown wall-clock variation.");
        if (Math.Min(p.Width, p.Height) < 0.3f || p.Depth < 0.06f) throw new ArgumentOutOfRangeException(nameof(p), "A wall clock is at least 0.3 m across and 6 cm deep.");
    }
}
