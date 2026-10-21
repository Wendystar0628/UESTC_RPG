using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof;

/// <summary>
/// Equipment fence (设备围挡): dark perforated aluminium boards between light posts, with a light cap rail
/// (MAP-QSH-ADDITIONS: a clean dark band, the plant only shows above it). Width runs along X, Depth is the
/// board thickness, PostSpacing the post pitch. Variation ≥ 1 adds an access door with a handle in the middle.
/// <see cref="Along"/> builds a whole enclosure ring with mitred boards and corner posts.
/// Collision is a barrier wall for enclosures on the ground; on roofs the fence is visual only and placers leave it out.
/// </summary>
[Part(PartCatalog.EquipmentFence)]
public sealed class EquipmentFencePart : IPartDef<RailingParams>
{
    private const float CapOverhang = 0.015f;

    public EquipmentFencePart() => Lods = Levels.Of<RailingParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3);
    public IReadOnlyDictionary<LodLevel, Func<RailingParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(120, 30);

    public Box3 Bounds(RailingParams p)
    {
        Validate(p);
        var half = Math.Max(p.Depth * 0.5f + CapOverhang, (p.Depth + 0.02f) * 0.5f);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -half), new(p.Width * 0.5f, p.Height, half));
    }

    public IReadOnlyList<Collider> Collision(RailingParams p)
    {
        Validate(p);
        return [new WallCollider(new(-p.Width * 0.5f, 0), new(p.Width * 0.5f, 0), p.Depth * 0.5f, 0, Math.Max(p.Height, WalkRules.BarrierHeight), SurfaceTag.Barrier) { Foot = p.Foot }];
    }

    private PartGeometry Build(RailingParams p, LodLevel lod) =>
        Along([new(-p.Width * 0.5f, 0), new(p.Width * 0.5f, 0)], false, p, lod, Bounds(p));

    /// <summary>The fence along an X/Z polyline; walk an enclosure counterclockwise seen from above so its outside is on the right.</summary>
    public static PartGeometry Along(IReadOnlyList<Vector2> path, bool closed, RailingParams p, LodLevel lod, Box3? bounds = null)
    {
        Validate(p); ArgumentNullException.ThrowIfNull(path);
        var half = p.Depth * 0.5f; var cap = Math.Min(RoofStyle.FenceCap, p.Height * 0.1f);
        var board = PartMaterials.Board(RoofStyle.FenceSteel);
        var light = PartMaterials.Metal(RoofStyle.Aluminium);
        var builder = new PartBuilder();
        if (lod <= LodLevel.L1)
        {
            var inset = Math.Min(RoofStyle.FenceBoardInset, half * 0.4f);
            builder.Add(PartSweeps.Sweep(PartShapes.Rect(-half + inset, half - inset, 0, p.Height - cap + 0.003f), path, board, closed));
        }
        else builder.Add(PartSweeps.Sweep(PartShapes.Rect(-half, half, 0, p.Height - cap + 0.003f), path, board, closed));
        if (lod <= LodLevel.L2)
            builder.Add(PartSweeps.Sweep(PartShapes.Rect(-half - CapOverhang, half + CapOverhang, p.Height - cap, p.Height), path, light, closed));
        if (lod == LodLevel.L0)
        {
            var post = new Vector3(RoofStyle.FencePost, p.Height - cap + 0.003f, p.Depth + 0.02f);
            foreach (var (at, along) in Posts(path, closed, p.PostSpacing))
                builder.AddCentredBox(post, Xf.Move(0, post.Y * 0.5f, 0).Then(Xf.AlongX(along, at)), light);
            if (p.Variation >= 1 && !closed)
            {
                // Access door: the bay nearest the middle of the first run gets a door head and a handle.
                var a = path[0]; var b = path[1];
                var along = new Vector3(b.X - a.X, 0, b.Y - a.Y);
                var length = (float)Math.Sqrt((double)along.X * along.X + (double)along.Z * along.Z);
                var bays = Math.Max(1, (int)Math.Round(length / Math.Max(p.PostSpacing, 0.3f)));
                var bay = length / bays;
                var centre = -length * 0.5f + bay * (bays / 2 + 0.5f);
                var frame = Xf.AlongX(along, new((a.X + b.X) * 0.5f, 0, (a.Y + b.Y) * 0.5f));
                var head = p.Height - cap - Math.Min(0.15f, p.Height * 0.1f);
                builder.AddCentredBox(new(bay - RoofStyle.FencePost + 0.006f, 0.05f, p.Depth + 0.02f), Xf.Move(centre, head, 0).Then(frame), light);
                builder.AddCentredBox(new(0.12f, 0.035f, p.Depth + 0.03f), Xf.Move(centre + bay * 0.3f, Math.Min(1.05f, p.Height * 0.55f), 0).Then(frame), PartMaterials.Metal(RoofStyle.Steel), 0, VertexFlag.NoShadow);
            }
        }
        var measured = builder.Measure();
        return builder.Finish(bounds ?? Levels.Padded(measured.Min, measured.Max));
    }

    private static IEnumerable<(Vector3 At, Vector3 Along)> Posts(IReadOnlyList<Vector2> path, bool closed, float spacing)
    {
        var segments = closed ? path.Count : path.Count - 1;
        for (var s = 0; s < segments; s++)
        {
            var a = path[s]; var b = path[(s + 1) % path.Count];
            var along = new Vector3(b.X - a.X, 0, b.Y - a.Y);
            var length = (float)Math.Sqrt((double)along.X * along.X + (double)along.Z * along.Z);
            var count = Math.Max(1, (int)Math.Round(length / Math.Max(spacing, 0.3f)));
            var edge = Math.Min(RoofStyle.FencePost * 0.5f, length * 0.25f) / length;
            // A shared vertex gets its post from the segment that starts there; a closed ring's last vertex is the first.
            var first = s == 0 ? 0 : 1;
            var last = closed && s == segments - 1 ? count - 1 : count;
            for (var i = first; i <= last; i++)
            {
                var t = (float)i / count;
                if (!closed) t = Math.Clamp(t, edge, 1 - edge);
                yield return (new(a.X + (b.X - a.X) * t, 0, a.Y + (b.Y - a.Y) * t), along);
            }
        }
    }

    private static void Validate(RailingParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        Levels.Positive(p.PostSpacing, nameof(p.PostSpacing));
        if (p.Depth < 0.03f || p.Height < 0.5f) throw new ArgumentOutOfRangeException(nameof(p), "A fence board is at least 0.03 m thick and 0.5 m high.");
    }
}

/// <summary>
/// Entrance canopy (雨棚): host-coloured slab inside a white aluminium edge band on three sides; the band rises
/// a little above the slab and hangs below it as a drip. Width runs along the wall (X), Depth is the projection
/// (wall at +Z, front at −Z), Height the edge band height. SlopeDeg (0–15) drops the front about the wall line.
/// Variation ≥ 1 adds two tie rods to the wall.
/// </summary>
[Part(PartCatalog.Canopy)]
public sealed class CanopyPart : IPartDef<RoofParams>
{
    private const float MaxSlope = 15f;

    public CanopyPart() => Lods = Levels.Of<RoofParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3);
    public IReadOnlyDictionary<LodLevel, Func<RoofParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(200, 90);

    public Box3 Bounds(RoofParams p)
    {
        Validate(p);
        var tilt = Tilt(p);
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (var x in new[] { -p.Width * 0.5f, p.Width * 0.5f })
            foreach (var y in new[] { 0f, p.Height })
                foreach (var z in new[] { -p.Depth * 0.5f, p.Depth * 0.5f })
                {
                    var q = Xf.Point(tilt, new(x, y, z));
                    min = Vector3.Min(min, q); max = Vector3.Max(max, q);
                }
        // Tie rods start inside the slab and end on the wall above it.
        if (p.Variation >= 1) max = Vector3.Max(max, new(max.X, p.Height + RoofStyle.CanopyRodRise, p.Depth * 0.5f));
        return Levels.Padded(min, max);
    }

    private PartGeometry Build(RoofParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var tilt = Tilt(p);
        var host = PartMaterials.Trim(p.Material);
        var builder = new PartBuilder();
        if (lod >= LodLevel.L2)
        {
            builder.AddCentredBox(new(p.Width, p.Height, p.Depth), Xf.Move(0, p.Height * 0.5f, 0).Then(tilt), host);
            return builder.Finish(Bounds(p));
        }
        var band = Math.Min(0.05f, Math.Min(p.Width, p.Depth) * 0.1f);
        var rise = Math.Min(RoofStyle.CanopyFasciaProud, p.Height * 0.2f);
        var slab = Math.Min(RoofStyle.CanopySlab, p.Height * 0.6f);
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f;
        // Edge band on three sides; the right of travel is outside, so the section sits inside the path.
        Vector2[] edge = [new(-hx, hz), new(-hx, -hz), new(hx, -hz), new(hx, hz)];
        var white = PartMaterials.Trim(RoofStyle.CasingWhite);
        builder.Add(PartSweeps.Sweep(PartShapes.BevelRect(band, p.Height, near ? 0.015f : 0, -band * 0.5f, 0), edge, white), tilt);
        var slabPiece = PartShapes.Box(new(-hx + band - 0.004f, p.Height - rise - slab, -hz + band - 0.004f), new(hx - band + 0.004f, p.Height - rise, hz), 0, host, out var slabAt);
        builder.Add(slabPiece, slabAt.Then(tilt));
        if (p.Variation >= 1)
        {
            var metal = PartMaterials.Pipe(PartMaterials.Metal(RoofStyle.Steel));
            var segments = near ? RoofStyle.RoundSegments : RoofStyle.RoundSegmentsFar;
            foreach (var side in new[] { -1f, 1f })
            {
                var x = side * Math.Max(hx - 0.35f, hx * 0.5f);
                var low = Xf.Point(tilt, new(x, p.Height - rise - 0.02f, -hz + band + 0.08f));
                builder.AddPipe(low, new(x, p.Height + RoofStyle.CanopyRodRise - 0.08f, hz - 0.02f), RoofStyle.CanopyRod * 0.5f, segments, metal, VertexFlag.NoShadow);
                if (near)
                    builder.AddBox(new(x - 0.06f, p.Height + RoofStyle.CanopyRodRise - 0.16f, hz - 0.02f), new(x + 0.06f, p.Height + RoofStyle.CanopyRodRise, hz), 0, metal, VertexFlag.NoShadow);
            }
        }
        return builder.Finish(Bounds(p));
    }

    /// <summary>Rotation about the top of the wall line that lowers the front by SlopeDeg.</summary>
    private static Matrix4x4 Tilt(RoofParams p)
    {
        var slope = Math.Clamp(p.SlopeDeg, 0, MaxSlope);
        if (slope == 0) return Matrix4x4.Identity;
        return Xf.Move(0, -p.Height, -p.Depth * 0.5f).Then(Xf.RotX(-slope)).Then(Xf.Move(0, p.Height, p.Depth * 0.5f));
    }

    private static void Validate(RoofParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        if (p.Width < 0.6f || p.Depth < 0.4f || p.Height < 0.12f) throw new ArgumentOutOfRangeException(nameof(p), "A canopy is at least 0.6 × 0.12 × 0.4 m.");
    }
}

/// <summary>
/// Ridge skylight (采光顶): host-coloured curb ring with a glazed gable on top, aluminium ridge, eaves bars and rafters.
/// The ridge runs along X. SlopeDeg 0 takes the default pitch; pitches stay under 35° (outline crease band) and the
/// rise is capped so the whole skylight fits in Height.
/// </summary>
[Part(PartCatalog.Skylight)]
public sealed class SkylightPart : IPartDef<RoofParams>
{
    public SkylightPart() => Lods = Levels.Of<RoofParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3);
    public IReadOnlyDictionary<LodLevel, Func<RoofParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(260, 60);

    public Box3 Bounds(RoofParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    private PartGeometry Build(RoofParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var curb = Math.Min(RoofStyle.SkylightCurb, Math.Min(p.Width, p.Depth) * 0.2f);
        var hx = p.Width * 0.5f - curb * 0.5f; var a = p.Depth * 0.5f - curb * 0.5f;
        var slope = p.SlopeDeg > 0 ? Math.Min(p.SlopeDeg, RoofStyle.SkylightMaxSlopeDeg) : RoofStyle.SkylightDefaultSlopeDeg;
        var rise = Math.Min(a * MathF.Tan(slope * Angles.DegreesToRadians), p.Height - 0.16f);
        var curbH = p.Height - rise - 0.045f;
        var baseY = curbH - 0.003f;
        var builder = new PartBuilder();
        Vector2[] ring = [new(-hx, -a), new(hx, -a), new(hx, a), new(-hx, a)];
        builder.Add(PartSweeps.Sweep(PartShapes.BevelRect(curb, curbH, near ? 0.015f : 0, 0, 0), ring, PartMaterials.Trim(p.Material), true));
        Vector2[] ridge = [new(-hx, 0), new(hx, 0)];
        builder.Add(PartSweeps.Sweep(new([new(-a, 0), new(a, 0), new(0, rise)]), ridge, PartMaterials.Glazing(), false, baseY));
        if (lod <= LodLevel.L1)
        {
            var frame = PartMaterials.Metal(RoofStyle.Aluminium);
            Vector2[] bar = [new(-hx - 0.01f, 0), new(hx + 0.01f, 0)];
            builder.Add(PartSweeps.Sweep(PartShapes.Rect(-0.035f, 0.035f, rise - 0.02f, rise + 0.04f), bar, frame, false, baseY));
            if (near)
            {
                builder.Add(PartSweeps.Sweep(PartShapes.Rect(a - 0.05f, a + 0.02f, -0.005f, 0.04f), bar, frame, false, baseY), VertexFlag.NoShadow);
                builder.Add(PartSweeps.Sweep(PartShapes.Rect(-a - 0.02f, -a + 0.05f, -0.005f, 0.04f), bar, frame, false, baseY), VertexFlag.NoShadow);
                var length = (float)Math.Sqrt((double)a * a + (double)rise * rise);
                var angle = MathF.Atan2(rise, a) * Angles.RadiansToDegrees;
                var count = Math.Max(1, (int)Math.Round(2 * hx / RoofStyle.SkylightBarPitch));
                var size = new Vector3(RoofStyle.SkylightFrame, 0.04f, length);
                for (var k = 0; k <= count; k++)
                {
                    var x = -hx + 2 * hx * k / count;
                    // Sit each rafter on the glass: centre of the slope plus half a bar along the outward normal, 3 mm embedded.
                    var lift = 0.02f - 0.003f;
                    builder.AddCentredBox(size, Xf.RotX(-angle).Then(Xf.Move(x, baseY + rise * 0.5f + a / length * lift, -a * 0.5f - rise / length * lift)), frame, 0, VertexFlag.NoShadow);
                    builder.AddCentredBox(size, Xf.RotX(angle).Then(Xf.Move(x, baseY + rise * 0.5f + a / length * lift, a * 0.5f + rise / length * lift)), frame, 0, VertexFlag.NoShadow);
                }
            }
        }
        return builder.Finish(Bounds(p));
    }

    private static void Validate(RoofParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        if (p.Width < 0.8f || p.Depth < 0.8f || p.Height < 0.3f) throw new ArgumentOutOfRangeException(nameof(p), "A skylight is at least 0.8 × 0.3 × 0.8 m.");
    }
}
