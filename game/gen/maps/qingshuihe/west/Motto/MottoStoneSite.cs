using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.West.WestTune.Look;
using M = Campus.Gen.Maps.Qingshuihe.West.WestTune.Motto;

namespace Campus.Gen.Maps.Qingshuihe.West.Motto;

/// <summary>
/// 校训石 (MAP-QSH-WEST): one cream-white landscape stone lying on its side, about 1.75 m × 6.6 m. 19b (Opus reviews
/// 21:28 and 19b): a few big flat planes with a low, gently tilted top and one flat dressed face, not a crumpled box and not
/// a smooth dome (MottoStone); it no longer stands straight on the paving: it lies in an oval bed of river pebbles held by a low granite kerb, with a crescent of low
/// clipped planting round its back and ends. The front stays open, so the face still reads from P10 and from Q1's 3 m spot.
/// The face uses the shared scene-text atlas. The low lawn lamp beside it is WP-39's
/// (lighting-schedule.json).
/// </summary>
public sealed class MottoStoneSite : ZoneSite
{
    /// <summary>The stone's plan centre: its node's anchor in the world model.</summary>
    internal static Vector2 At(IWorldModel world) =>
        world.Feature(M.Entity).Point();

    /// <summary>Half axes of the kerb's centreline (local x along the stone, z across it).</summary>
    internal static Vector2 Kerb => new(M.Length * 0.5f + M.BedMarginX, M.Depth * 0.5f + M.BedMarginZ);

    public override string Id => "19a.motto";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world)
    {
        var half = Kerb + new Vector2(M.KerbWidth * 0.5f + M.HedgeGap + M.HedgeWidth);
        return [SiteRanges.Bounds(PlanMath.Facing(At(world), M.Facing).Rect(-half.X, -half.Y, half.X, half.Y))];
    }

    public override void Build(SiteBuilder b)
    {
        var at = At(b.Context.World);
        var f = PlanMath.Facing(at, M.Facing);   // origin at the stone's plan centre, local +Z toward its face's bearing
        var y = b.Ground(at);
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
        var tilt = System.Numerics.Matrix4x4.CreateRotationX(-MathF.Atan(.16f));
        foreach (var (text, lineY) in new[] { ("求实求真", 1.3f), ("大气大为", 1.3f - M.LineGap) })
        {
            var point = MottoStone.OnFace(0, lineY);
            Framework.WestLettering.Write(b, text, M.CharacterSize, L.Red, L.CreamStone,
                tilt * f.At(point.X, y - .03f + point.Y, point.Z + .015f));
        }
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
            points[i] = f.P(half.X * MathF.Cos(a), half.Y * MathF.Sin(a));
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
        var at = At(c.Context.World);
        var y = c.Ground(at.X, at.Y);
        c.Box(PlanMath.Facing(at, M.Facing).At(0, y + M.Height * 0.5f, 0), new(M.Length * 0.5f, M.Height * 0.5f, M.Depth * 0.5f), SurfaceTag.Prop, FootMat.StonePaving);
    }

}
