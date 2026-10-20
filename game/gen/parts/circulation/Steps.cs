using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation.Geometry;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>
/// Outdoor flight of steps, one flight per part (WLD-TERRAIN). Width is the outer width, Height the rise, Steps the
/// riser count n, Depth = n × tread: the first riser stands on the front edge (−Z), tread k is at k × rise and the top
/// tread [(n−1)·T, n·T] is flush with the upper level, whose pad starts at +Z. The solid sinks 0.1 m below y = 0.
/// Variation: 0 cheek walls with copings and handrails (side rails from 0.6 m rise, a middle rail when the clear width
/// is ≥ 2 m and the rise ≥ 1.05 m), 1 cheek walls only, 2 bare block for steps flanked by walls or terrain.
/// Collision (WLD-TERRAIN, SYS-WALK): three or more risers give one RampCollider through every nosing, flush with
/// the top tread and buried 0.2 m below the lower level; fewer risers give one box per step for auto-step.
/// </summary>
[Part(PartCatalog.Steps)]
public sealed class StepsPart() : CirculationPart<StairParams>(new(300, 200))
{
    private readonly record struct Layout(float W, float H, float D, int N, float R, float T, float Cheek, bool Rails, bool Middle)
    {
        internal float Z(float s) => s - D * 0.5f;
        internal float Clear => W - 2 * Cheek;
        internal float Nosing(float s) => R + s * R / T;
        /// <summary>One- and two-riser stoops get level cheeks; longer flights follow the nosing line after a short pier.</summary>
        internal bool Sloped => N >= 3;
        internal float Knee => Sloped ? Math.Min(S.CheekPier, (N - 1) * T * 0.5f) : 0;
        internal float TreadTop(float s) => Math.Clamp((int)Math.Floor(s / T) + 1, 1, N) * R;

        /// <summary>Cheek top line (underside of the coping) in (z, y), front to back.</summary>
        internal List<Vector2> CheekLine()
        {
            if (!Sloped) return [new(Z(0), H + S.CheekRise), new(Z(D), H + S.CheekRise)];
            var low = Nosing(Knee) + S.CheekRise;
            return [new(Z(0), low), new(Z(Knee), low), new(Z((N - 1) * T), H + S.CheekRise), new(Z(D), H + S.CheekRise)];
        }

        internal List<Vector2> RailLine(float frontInset, float backInset)
        {
            var low = Nosing(Knee) + S.HandrailHeight;
            return [new(Z(frontInset), low), new(Z(Knee), low), new(Z((N - 1) * T), H + S.HandrailHeight), new(Z(D - backInset), H + S.HandrailHeight)];
        }

        /// <summary>Body depth below the cheek line: down past grade at the front pier and past the inner tread corners on the slope.</summary>
        internal float CheekDepth => Sloped ? Math.Max(Nosing(Knee) + S.CheekRise + S.Sink, S.CheekRise + R + 0.15f) : H + S.CheekRise + S.Sink;
    }

    private static Layout Plan(StairParams p)
    {
        if (p.Steps < 1) throw new ArgumentOutOfRangeException(nameof(p), "Steps need at least one riser.");
        var r = p.Height / p.Steps; var t = p.Depth / p.Steps;
        Range(r, S.RiserMinAllowed, 0.2f, "riser height");
        Range(t, 0.25f, 0.6f, "tread depth");
        var cheek = p.Variation == 2 ? 0 : S.CheekWidth;
        Range(p.Width - 2 * cheek, 0.6f, 60f, "clear width");
        var rails = p.Variation == 0 && p.Height >= S.HandrailMinRise && p.Steps >= 3;
        var middle = rails && p.Width - 2 * cheek >= S.MidHandrailMinClear && p.Height >= S.MidHandrailMinRise;
        return new(p.Width, p.Height, p.Depth, p.Steps, r, t, cheek, rails, middle);
    }

    private protected override Box3 BoundsOf(StairParams p)
    {
        var top = p.Variation == 2 ? p.Height : p.Height + S.HandrailHeight + S.HandrailKnob + S.CopingThickness;
        return Centered(p.Width, -S.Sink - 0.2f, top, p.Depth);
    }

    private protected override IReadOnlyList<Collider> Colliders(StairParams p)
    {
        var l = Plan(p);
        var foot = Foot(p.Foot, S.OutdoorStepFoot);
        var half = l.Clear * 0.5f;
        var colliders = new List<Collider>();
        if (l.N >= 3)
        {
            // Nosing line y = R + s·R/T; it reaches −0.2 m in front of the first riser.
            var buried = (-S.RampBury - l.R) * l.T / l.R;
            var topNosing = (l.N - 1) * l.T;
            colliders.Add(new RampCollider(new(-half, -S.RampBury, l.Z(buried)), new(half, -S.RampBury, l.Z(buried)),
                new(half, l.H, l.Z(topNosing)), new(-half, l.H, l.Z(topNosing)), S.StepColliderThickness, SurfaceTag.Stair)
            { Foot = foot, Stair = new(l.R, l.T, l.N) });
            colliders.Add(Box(new(-half, l.H - S.StepColliderThickness, l.Z(topNosing)), new(half, l.H, l.Z(l.D)), SurfaceTag.Stair, foot));
        }
        else
            for (var k = 1; k <= l.N; k++)
                colliders.Add(Box(new(-half, -S.Sink, l.Z((k - 1) * l.T)), new(half, k * l.R, l.Z(l.D)), SurfaceTag.Stair, foot));
        if (l.Cheek > 0)
            foreach (var side in new[] { -1f, 1f })
            {
                var x = side * (l.W - l.Cheek) * 0.5f;
                var line = l.CheekLine();
                for (var i = 0; i + 1 < line.Count; i++)
                    colliders.Add(SegmentBox(line[i], line[i + 1], x, l.Cheek, S.CopingThickness, l.CheekDepth, foot));
            }
        return colliders;
    }

    private protected override void Compose(MeshBuilder mesh, StairParams p, Detail detail)
    {
        var l = Plan(p);
        var stone = Pick(p.Material, S.Stone);
        var (ring, chamfer, triangles) = Ring(l, detail);
        var cheeks = l.Cheek > 0 && detail != Detail.Mass;
        // With cheeks the block stops at the cheek bodies' inner faces (a butt joint, no coplanar overlap).
        var inner = cheeks ? l.W * 0.5f - l.Cheek + S.CopingOverhang : l.W * 0.5f;
        mesh.Extrude(stone, ring, chamfer, -inner, inner, Frame.Identity, triangles);
        if (cheeks)
        {
            var line = l.CheekLine();
            var body = new List<Vector2>(line.Count + 2) { new(l.Z(0), -S.Sink) };
            body.AddRange(line);
            body.Add(new(l.Z(l.D), -S.Sink));
            var bevel = detail == Detail.Full ? S.RailBevel : 0;
            foreach (var side in new[] { -1f, 1f })
            {
                // Solid cheek wall down to grade, set 2 cm in under a coping that runs flush with the part's sides.
                var outer = side * l.W * 0.5f;
                var x0 = side < 0 ? outer : outer - l.Cheek;
                mesh.Extrude(stone, body, null, side < 0 ? outer + S.CopingOverhang : side * inner, side < 0 ? -inner : outer - S.CopingOverhang, Frame.Identity);
                mesh.VerticalSweep(S.Trim, Profiles.Rect(-S.CopingThickness, S.Joint, 0, l.Cheek, bevel), line, x0, Frame.Identity);
            }
        }
        if (!l.Rails) return;
        var cheekTop = l.CheekLine();
        foreach (var side in new[] { -1f, 1f })
            RailKit.Handrail(mesh, l.RailLine(0.08f, 0.06f), side * (l.W - l.Cheek) * 0.5f,
                z => RailKit.HeightAt(cheekTop, z) + S.CopingThickness, detail, Frame.Identity);
        if (l.Middle)
            RailKit.Handrail(mesh, l.RailLine(0.25f, 0.3f), 0, z => l.TreadTop(z + l.D * 0.5f), detail, Frame.Identity);
    }

    /// <summary>The shared stepped section (<see cref="StepProfile.Block"/>) with s mapped onto this part's z.</summary>
    private static (List<Vector2> Ring, List<bool> Chamfer, List<int> Triangles) Ring(Layout l, Detail detail)
    {
        var (ring, chamfer, triangles) = StepProfile.Block(l.N, l.H, l.D, detail, S.NosingBevel);
        for (var i = 0; i < ring.Count; i++) ring[i] = new(l.Z(ring[i].X), ring[i].Y);
        return (ring, chamfer, triangles);
    }

    private static BoxCollider Box(Vector3 min, Vector3 max, SurfaceTag tag, FootMat foot) =>
        new(Frame.At((min.X + max.X) * 0.5f, (min.Y + max.Y) * 0.5f, (min.Z + max.Z) * 0.5f).ToMatrix(),
            new((max.X - min.X) * 0.5f, (max.Y - min.Y) * 0.5f, (max.Z - min.Z) * 0.5f), tag)
        { Foot = foot };

    /// <summary>A cheek segment as one rotated box: coping on top, body below, centred on the cheek.</summary>
    private static BoxCollider SegmentBox(Vector2 a, Vector2 b, float x, float width, float above, float below, FootMat foot)
    {
        var along = new Vector3(0, b.Y - a.Y, b.X - a.X);
        var frame = Frame.Along(new(x, (a.Y + b.Y) * 0.5f, (a.X + b.X) * 0.5f), along);
        // Local X runs along the segment, local Y is the perpendicular toward up.
        var centre = frame.Point(new(0, (above - below) * 0.5f, 0));
        var length = (float)Math.Sqrt(VectorMath.Dot(b - a, b - a));
        var placed = frame with { Origin = centre };
        return new(placed.ToMatrix(), new(length * 0.5f, (above + below) * 0.5f, width * 0.5f), SurfaceTag.Prop) { Foot = foot };
    }
}
