using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.West.Framework;
using L = Campus.Gen.Zones.West.WestTune.Look;
using M = Campus.Gen.Zones.West.WestTune.Motto;

namespace Campus.Gen.Zones.West.Motto;

/// <summary>
/// 校训石 (MAP-QSH-WEST): one cream-white landscape stone lying on its side, about 1.75 m × 6.6 m. 19b (Opus reviews
/// 21:28 and 19b): a few big flat planes with a low, gently tilted top and one flat dressed face, not a crumpled box and not
/// a smooth dome (MottoStone); it no longer stands straight on the paving: it lies in an oval bed of river pebbles held by a low granite kerb, with a crescent of low
/// clipped planting round its back and ends. The front stays open, so the face still reads from P10 and from Q1's 3 m spot.
/// The face is left blank until the scene text atlas carries the motto (contract-26). The low lawn lamp beside it is WP-39's
/// (lighting-schedule.json).
/// </summary>
public sealed class MottoStoneSite : WestSite
{
    /// <summary>The stone's frame: origin at its plan centre, local +Z toward its face's bearing.</summary>
    internal static PlanFrame Frame => PlanMath.Facing(M.At, M.Facing);

    /// <summary>Half axes of the kerb's centreline (local x along the stone, z across it).</summary>
    internal static Vector2 Kerb => new(M.Length * 0.5f + M.BedMarginX, M.Depth * 0.5f + M.BedMarginZ);

    public override string Id => "19a.motto";

    public override void Build(SiteBuilder b)
    {
        var f = Frame;
        var y = b.Ground(M.At);
        var around = b.Detail switch { SiteDetail.Full => 48, SiteDetail.Plain => 32, _ => 16 };
        // The bed: a low dome of pebbles inside the kerb; the stone sinks into it so its foot has a soft line, not a cut.
        var bed = Kerb - new Vector2(M.KerbWidth * 0.5f);
        Vector2[] dome = [new(1f, 0f), new(0.97f, M.PebbleCrown * 0.45f), new(0.82f, M.PebbleCrown * 0.8f), new(0.5f, M.PebbleCrown * 0.97f), new(0f, M.PebbleCrown)];
        b.Add(Solids.Lathe(dome, around, L.Pebble, bed.X, bed.Y), f.At(0, y - 0.04f, 0));
        b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(M.KerbWidth, M.KerbHeight + 0.06f, b.Detail == SiteDetail.Mass ? 0f : 0.03f),
            Oval(f, Kerb, around, -180f, 180f, closed: true), L.Granite, closed: true, baseY: y - 0.06f), Matrix4x4.Identity);
        // A landscape stone of a few big flat planes, low tilted top, one flat dressed face (MottoStone).
        b.Add(MottoStone.Build(L.CreamStone), f.At(0, y - 0.03f, 0));
        if (b.Detail == SiteDetail.Mass) return;
        // Planting crescent round the back and both ends, open toward the face.
        var hedge = Kerb + new Vector2(M.KerbWidth * 0.5f + M.HedgeGap + M.HedgeWidth * 0.5f);
        b.Add(PartGeometryKernel.Sweep(HedgeProfile(), Oval(f, hedge, around, -90f - M.HedgeSweep, -90f + M.HedgeSweep, closed: false), L.Hedge, baseY: y - 0.05f), Matrix4x4.Identity);
        // Text hook: the stone face stays blank until the scene text atlas (contract-26) arrives. Then add on the dressed face
        // (MottoStone.OnFace / FaceNormal, between heights 0.35 and 1.3, Look.Red lacquer) two lines "求实求真" / "大气大为" laid out by CharacterSize, CharacterPitch, LineGap.
    }

    /// <summary>
    /// World plan points of an oval in the stone's frame, angles in degrees from local +X toward local +Z (the face side);
    /// −90 is straight behind the stone. A closed oval leaves out its repeated end point.
    /// </summary>
    internal static Vector2[] Oval(PlanFrame f, Vector2 half, int segments, float from, float to, bool closed)
    {
        var count = closed ? segments : Math.Max(2, (int)MathF.Ceiling(segments * (to - from) / 360f) + 1);
        var points = new Vector2[count];
        for (var i = 0; i < count; i++)
        {
            var t = closed ? from + 360f * i / segments : from + (to - from) * i / (count - 1);
            var a = t * (MathF.PI / 180f);
            points[i] = f.P(half.X * DetMath.Cos(a), half.Y * DetMath.Sin(a));
        }
        // Sweep wants a counter-clockwise path in world plan for an outward-right lateral; either way the section is symmetric.
        return points;
    }

    /// <summary>A rounded, clipped hedge section (smooth normals), its foot just under the paving.</summary>
    private static SweepProfile HedgeProfile()
    {
        float w = M.HedgeWidth * 0.5f, h = M.HedgeHeight + 0.05f;
        Vector2[] p = [new(-w, 0), new(w, 0), new(w, h * 0.55f), new(w * 0.64f, h * 0.92f), new(0, h), new(-w * 0.64f, h * 0.92f), new(-w, h * 0.55f)];
        Vector2[] n = [new(-1, 0), new(1, 0), new(0.94f, 0.34f), new(0.6f, 0.8f), new(0, 1), new(-0.6f, 0.8f), new(-0.94f, 0.34f)];
        return new(p, [.. n.Select(v => v / MathF.Sqrt(v.X * v.X + v.Y * v.Y))]);
    }

    /// <summary>One box with a top (SYS-WALK): the stone is about 2 m high, not climbable.</summary>
    public override void Collide(SiteColliders c)
    {
        var y = c.Ground(M.At.X, M.At.Y);
        c.Box(Frame.At(0, y + M.Height * 0.5f, 0), new(M.Length * 0.5f, M.Height * 0.5f, M.Depth * 0.5f), SurfaceTag.Prop, FootMat.StonePaving);
    }

    /// <summary>Q1 (walk, K1): 3 m in front of the face, eye height, looking at the characters and the stone's foot.</summary>
    public override IReadOnlyList<InspectionProbe> Probes
    {
        get
        {
            var out1 = PlanMath.Heading(M.Facing);
            var eye = M.At + out1 * (M.Depth * 0.5f + 3f);
            return [Probe("Q1", M.Entity, new(eye.X, 1.62f, eye.Y), -out1, -8f, 3f)];
        }
    }
}
