using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Circulation.Geometry;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>Finish of steps swept along a path.</summary>
public enum StepsFinish
{
    /// <summary>Stone steps: riser 0.08–0.2 m, tread 0.25–0.6 m, chamfered nosings.</summary>
    Stone,
    /// <summary>Grass seat tiers (草阶): riser 0.2–0.45 m, tread 0.8–2.0 m, concrete edge with a grass mat on each tier.</summary>
    GrassSeats,
}

/// <summary>
/// Steps or grass seat tiers swept along a plan polyline, open or closed: the time-plaza ring, curved flights, the lake
/// and moon-viewing seat terraces and the sunken lawn (MAP-QSH-TERRAIN; WLD-TERRAIN).
/// The line is the foot of the first riser at height baseY; the steps climb toward (dz, −dx) of the line's direction,
/// the WP-09 sweep's positive lateral. A loop with positive Σ(x·z′ − x′·z) (x = cos θ, z = sin θ with θ rising) climbs
/// outward, as the time-plaza ring does; reverse the points to climb inward. Tread k sits at k·riser over [(k−1)·tread, k·tread]; the top tread is flush with the upper level.
/// Not a catalogue part (a path has no BoxPartParams); zone packages call it directly, like <see cref="TerraceEdge"/>.
/// </summary>
public static class StepsAlong
{
    public static PartGeometry Build(IReadOnlyList<Vector2> footLine, bool closed, float baseY, int risers, float riser, float tread,
        StepsFinish finish, LodLevel lod)
    {
        Check(footLine, closed, risers, riser, tread, finish);
        var detail = DetailOf(lod);
        var path = Path(footLine, baseY);
        var mesh = new MeshBuilder();
        var rise = risers * riser; var run = risers * tread;
        var seats = finish == StepsFinish.GrassSeats;
        var (section, chamfer, triangles) = StepProfile.Block(risers, rise, run, detail, seats ? S.SeatNosingBevel : S.NosingBevel);
        mesh.SweepPath(seats ? S.Trim : S.Stone, section, chamfer, triangles, path, closed, Frame.Identity);
        if (seats && detail != Detail.Mass)
            for (var k = 1; k <= risers; k++)
            {
                // A grass mat on each tier behind a concrete edge, tucked a joint into the next riser.
                var back = k < risers ? k * tread + S.Joint : run;
                var mat = new List<Vector2>
                {
                    new((k - 1) * tread + S.SeatEdge, k * riser - S.Joint), new(back, k * riser - S.Joint),
                    new(back, k * riser + S.SeatGrass), new((k - 1) * tread + S.SeatEdge, k * riser + S.SeatGrass),
                };
                mesh.SweepPath(S.Grass, mat, null, null, path, closed, Frame.Identity);
            }
        return mesh.Finish(Bounds(footLine, baseY, rise, run, seats));
    }

    /// <summary>
    /// Stone flights of three or more risers get one RampCollider per path segment through every nosing (flush at the top,
    /// buried 0.2 m), plus a box under the top tread; shorter flights and seat tiers get one stacked box per tier and
    /// segment. Rotated boxes and ramps follow each straight segment; joins on curves leave only sub-tread wedges.
    /// </summary>
    public static IReadOnlyList<Collider> Collide(IReadOnlyList<Vector2> footLine, bool closed, float baseY, int risers, float riser, float tread,
        StepsFinish finish, FootMat foot = FootMat.Auto)
    {
        Check(footLine, closed, risers, riser, tread, finish);
        var feet = foot == FootMat.Auto ? finish == StepsFinish.Stone ? S.OutdoorStepFoot : FootMat.Grass : foot;
        var rise = risers * riser; var run = risers * tread;
        var colliders = new List<Collider>();
        var segments = closed ? footLine.Count : footLine.Count - 1;
        for (var s = 0; s < segments; s++)
        {
            var a = footLine[s]; var b = footLine[(s + 1) % footLine.Count];
            var d = b - a; var length = (float)Math.Sqrt(VectorMath.Dot(d, d));
            var lateral = new Vector2(d.Y / length, -d.X / length);
            Vector3 At(Vector2 p, float s2, float y) => new(p.X + lateral.X * s2, baseY + y, p.Y + lateral.Y * s2);
            if (finish == StepsFinish.Stone && risers >= 3)
            {
                var buried = (-S.RampBury - riser) * tread / riser;
                var topNosing = run - tread;
                colliders.Add(new RampCollider(At(a, buried, -S.RampBury), At(b, buried, -S.RampBury), At(b, topNosing, rise), At(a, topNosing, rise),
                    S.StepColliderThickness, SurfaceTag.Stair)
                { Foot = feet, Stair = new(riser, tread, risers) });
                colliders.Add(SegmentBox(a, d, lateral, length, topNosing, run, baseY + rise - S.StepColliderThickness, baseY + rise, feet));
            }
            else
                for (var k = 1; k <= risers; k++)
                    colliders.Add(SegmentBox(a, d, lateral, length, (k - 1) * tread, run, baseY - S.Sink, baseY + k * riser, feet));
        }
        return colliders;
    }

    private static BoxCollider SegmentBox(Vector2 a, Vector2 d, Vector2 lateral, float length, float s0, float s1, float y0, float y1, FootMat foot)
    {
        var mid = (s0 + s1) * 0.5f;
        var centre = new Vector3(a.X + d.X * 0.5f + lateral.X * mid, (y0 + y1) * 0.5f, a.Y + d.Y * 0.5f + lateral.Y * mid);
        // Local X runs along the segment, local Y is up; local Z = X × Y is ∓lateral, so the half depth is symmetric anyway.
        var frame = Frame.Along(centre, new(d.X, 0, d.Y));
        return new(frame.ToMatrix(), new(length * 0.5f, (y1 - y0) * 0.5f, (s1 - s0) * 0.5f), SurfaceTag.Stair) { Foot = foot };
    }

    internal static Detail DetailOf(LodLevel lod) => lod switch
    {
        LodLevel.L0 => Detail.Full,
        LodLevel.L1 => Detail.Plain,
        LodLevel.L2 => Detail.Coarse,
        LodLevel.L3 or LodLevel.L4 => Detail.Mass,
        _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown LOD level."),
    };

    internal static IReadOnlyList<Vector3> Path(IReadOnlyList<Vector2> line, float y)
    {
        var path = new Vector3[line.Count];
        for (var i = 0; i < line.Count; i++) path[i] = new(line[i].X, y, line[i].Y);
        return path;
    }

    private static void Check(IReadOnlyList<Vector2> line, bool closed, int risers, float riser, float tread, StepsFinish finish)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (line.Count < (closed ? 3 : 2)) throw new ArgumentException("Steps need a foot line of two points, or three for a loop.", nameof(line));
        if (risers < 1 || risers > 40) throw new ArgumentOutOfRangeException(nameof(risers));
        var (rMin, rMax, tMin, tMax) = finish switch
        {
            StepsFinish.Stone => (0.08f, 0.2f, 0.25f, 0.6f),
            StepsFinish.GrassSeats => (0.2f, 0.45f, 0.8f, 2.0f),
            _ => throw new ArgumentOutOfRangeException(nameof(finish), finish, "Unknown steps finish."),
        };
        if (!(riser >= rMin && riser <= rMax)) throw new ArgumentOutOfRangeException(nameof(riser), $"Riser {riser} m is outside {rMin}–{rMax} m.");
        if (!(tread >= tMin && tread <= tMax)) throw new ArgumentOutOfRangeException(nameof(tread), $"Tread {tread} m is outside {tMin}–{tMax} m.");
    }

    private static Box3 Bounds(IReadOnlyList<Vector2> line, float baseY, float rise, float run, bool seats)
    {
        // Mitred joins reach at most miterLimit (4) × the section's lateral extent beyond the line.
        var reach = run * 4 + 0.01f;
        float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
        foreach (var p in line) { minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X); minZ = Math.Min(minZ, p.Y); maxZ = Math.Max(maxZ, p.Y); }
        return new(new(minX - reach, baseY - S.Sink, minZ - reach), new(maxX + reach, baseY + rise + (seats ? S.SeatGrass : 0), maxZ + reach));
    }
}
