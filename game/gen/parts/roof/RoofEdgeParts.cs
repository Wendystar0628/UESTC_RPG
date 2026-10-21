using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof;

/// <summary>
/// Parapet coping (女儿墙压顶): a chamfered slab with a drip lip under the outer edge.
/// Width runs along X, Depth across the wall (−Z is the outer face), Height is the slab thickness;
/// the drips hang below the origin, which sits on the parapet top. Colours come from <see cref="BoxPartParams.Material"/>
/// (the host's trim colour); the material class is the building trim class.
/// <see cref="Along"/> sweeps the same section round a parapet ring with mitred corners.
/// </summary>
[Part(PartCatalog.ParapetCap)]
public sealed class ParapetCapPart : IPartDef<TrimParams>
{
    public ParapetCapPart() => Lods = Levels.Of<TrimParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3);
    public IReadOnlyDictionary<LodLevel, Func<TrimParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(64, 12);

    public Box3 Bounds(TrimParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, -RoofStyle.CopingDripDepth, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    private PartGeometry Build(TrimParams p, LodLevel lod) =>
        Along([new(-p.Width * 0.5f, 0), new(p.Width * 0.5f, 0)], false, p, lod, Bounds(p));

    /// <summary>
    /// Coping along an X/Z polyline (lateral positive is the right of travel; walk a parapet ring counterclockwise
    /// seen from above, i.e. so that the right side is outside). Bounds are the caller's or measured from the path.
    /// </summary>
    public static PartGeometry Along(IReadOnlyList<Vector2> path, bool closed, TrimParams p, LodLevel lod, Box3? bounds = null)
    {
        Validate(p); ArgumentNullException.ThrowIfNull(path);
        var near = lod == LodLevel.L0;
        var material = PartMaterials.Trim(p.Material);
        var half = p.Depth * 0.5f;
        var builder = new PartBuilder();
        builder.Add(PartSweeps.Sweep(PartShapes.BevelRect(p.Depth, p.Height, near ? RoofStyle.CopingBevel : 0, 0, 0), path, material, closed));
        if (near)
        {
            // Drip lip under the outer edge only: deep enough to cast a clean line on the wall below (ART-MODELING).
            // It sits 2 mm behind the outer face and runs up into the slab, so no edge is shared with the chamfer.
            var d = RoofStyle.CopingDripDepth; var w = Math.Min(RoofStyle.CopingDripWidth, half * 0.4f);
            builder.Add(PartSweeps.Sweep(PartShapes.Rect(half - w, half - 0.002f, -d, RoofStyle.CopingBevel + 0.01f), path, material, closed));
        }
        var measured = builder.Measure();
        return builder.Finish(bounds ?? Levels.Padded(measured.Min, measured.Max));
    }

    private static void Validate(TrimParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
    }
}

/// <summary>
/// Lightning strip (避雷带): one continuous galvanised rod on short supports along the parapet top.
/// Width is the run length along X, Height the rod top above the coping, Depth the rod diameter
/// (raised to the prop minimum section, ART-FIDELITY). Variation 0 puts one support near each end;
/// Variation ≥ 1 (precision zone K1) puts one every metre.
/// </summary>
[Part(PartCatalog.LightningStrip)]
public sealed class LightningStripPart : IPartDef<TrimParams>
{
    public LightningStripPart() => Lods = Levels.Of<TrimParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<TrimParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(64 + 12 * 16, 24);

    public Box3 Bounds(TrimParams p)
    {
        var (rod, height) = Size(p);
        return Levels.Padded(new(-p.Width * 0.5f, -0.003f, -rod * 0.5f), new(p.Width * 0.5f, height, rod * 0.5f));
    }

    private PartGeometry Build(TrimParams p, LodLevel lod) =>
        Along([new(-p.Width * 0.5f, 0), new(p.Width * 0.5f, 0)], false, p, lod, Bounds(p));

    /// <summary>The strip along an X/Z polyline (a parapet ring). Supports follow the same spacing rule per segment.</summary>
    public static PartGeometry Along(IReadOnlyList<Vector2> path, bool closed, TrimParams p, LodLevel lod, Box3? bounds = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        var (rod, height) = Size(p);
        var near = lod == LodLevel.L0;
        var metal = PartMaterials.Metal(RoofStyle.Steel);
        var builder = new PartBuilder();
        builder.Add(PartSweeps.Sweep(PartShapes.Circle(rod * 0.5f, near ? RoofStyle.RoundSegments : RoofStyle.RoundSegmentsFar),
            path, metal, closed, height - rod * 0.5f), VertexFlag.NoShadow);
        if (near)
        {
            var w = RoofStyle.StripSupportWidth * 0.5f; var top = height - rod * 0.5f;
            foreach (var at in Supports(path, closed, p.Variation >= 1))
                builder.AddBox(new(at.X - w, -0.003f, at.Y - w), new(at.X + w, top, at.Y + w), 0, metal, VertexFlag.NoShadow);
        }
        var measured = builder.Measure();
        return builder.Finish(bounds ?? Levels.Padded(measured.Min, measured.Max));
    }

    private static IEnumerable<Vector2> Supports(IReadOnlyList<Vector2> path, bool closed, bool everyMetre)
    {
        var segments = closed ? path.Count : path.Count - 1;
        for (var s = 0; s < segments; s++)
        {
            var a = path[s]; var b = path[(s + 1) % path.Count];
            var length = (float)Math.Sqrt(((double)b.X - a.X) * (b.X - a.X) + ((double)b.Y - a.Y) * (b.Y - a.Y));
            var inset = Math.Min(RoofStyle.StripSupportInset, length * 0.25f);
            var count = everyMetre ? Math.Max(2, (int)Math.Floor((length - 2 * inset) / RoofStyle.StripSupportSpacing) + 1) : 2;
            for (var i = 0; i < count; i++)
            {
                var t = (inset + (length - 2 * inset) * i / (count - 1)) / length;
                yield return new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
            }
        }
    }

    private static (float Rod, float Height) Size(TrimParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        var rod = Math.Max(p.Depth, RoofStyle.StripMinRod);
        // Catalogue defaults describe the bare rod; keep the documented 0.15 m standoff in that case.
        var height = Math.Max(p.Height, Math.Max(RoofStyle.StripDefaultHeight, rod + 0.04f));
        return (rod, height);
    }
}

/// <summary>
/// Rainwater downpipe (落水管): round pipe with a rain hopper at the top, one clamp per storey and a kicked shoe at the foot.
/// Width is the pipe diameter, Height the run from the ground to the hopper top, Depth the footprint across the wall:
/// the wall plane is at +Depth/2, the pipe axis at the origin, the shoe kicks out towards −Z.
/// Variation ≥ 1 is the dark condensate drain beside a unit grille: no hopper.
/// Colours come from <see cref="BoxPartParams.Material"/> (main wall or dark grey, ART-FIDELITY).
/// </summary>
[Part(PartCatalog.Downpipe)]
public sealed class DownpipePart : IPartDef<ColumnParams>
{
    public DownpipePart() => Lods = Levels.Of<ColumnParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<ColumnParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(360, 64);

    public Box3 Bounds(ColumnParams p)
    {
        Validate(p);
        var r = p.Width * 0.5f; var hopper = RoofStyle.DownpipeHopper * 0.5f;
        var side = Math.Max(r + 0.012f, hopper);
        return Levels.Padded(new(-side, 0, -RoofStyle.DownpipeShoeKick - r), new(side, p.Height, Math.Max(p.Depth * 0.5f + 0.003f, r + 0.012f)));
    }

    private PartGeometry Build(ColumnParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var r = p.Width * 0.5f; var wall = p.Depth * 0.5f;
        var metal = PartMaterials.Pipe(p.Material);
        var segments = near ? RoofStyle.RoundSegments : RoofStyle.RoundSegmentsFar;
        var hopper = p.Variation < 1;
        var hopperH = Math.Min(0.25f, p.Height * 0.2f); var hopperD = Math.Min(0.18f, Math.Max(r * 2 + 0.04f, wall * 2));
        var top = hopper ? p.Height - hopperH + 0.01f : p.Height;
        var rise = Math.Min(RoofStyle.DownpipeShoeRise, top * 0.4f);
        var builder = new PartBuilder();
        // Pipe and shoe as one mitred sweep, drawn from the top down in a local frame (local X = down, local Z = world Z).
        Vector2[] path = [new(0, 0), new(top - rise, 0), new(top - (r + 0.01f), -RoofStyle.DownpipeShoeKick)];
        builder.Add(PartSweeps.Sweep(PartShapes.Circle(r, segments), path, metal), Xf.XDown.Then(Xf.Move(0, top, 0)));
        // Rain hopper against the wall (condensate drains have none).
        var hw = RoofStyle.DownpipeHopper * 0.5f;
        if (hopper) builder.AddBox(new(-hw, p.Height - hopperH, wall - hopperD), new(hw, p.Height, wall), near ? 0.015f : 0, metal);
        if (near)
        {
            for (var y = Math.Min(1.5f, top * 0.5f); y < top - 0.4f; y += RoofStyle.DownpipeClampPitch)
            {
                builder.AddColumn(0, y, y + RoofStyle.DownpipeClampHeight, 0, r + 0.012f, segments, metal, VertexFlag.NoShadow);
                if (wall - r > 0.01f)
                    builder.AddBox(new(-0.015f, y, r - 0.005f), new(0.015f, y + RoofStyle.DownpipeClampHeight, wall + 0.003f), 0, metal, VertexFlag.NoShadow);
            }
        }
        return builder.Finish(Bounds(p));
    }

    private static void Validate(ColumnParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        if (p.Height < 1f) throw new ArgumentOutOfRangeException(nameof(p.Height), "A downpipe run must be at least 1 m.");
    }
}
