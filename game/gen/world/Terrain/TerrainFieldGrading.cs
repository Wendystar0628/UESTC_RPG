using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

internal static class TerrainFieldGrading
{
    internal static double SourceRangeBound(IEnumerable<TerrainOp> operations) =>
        TerrainSampling.MaximumHeight - TerrainSampling.MinimumHeight + operations.Sum(op => op switch
        { TerrainMoundOp mound => mound.Height, TerrainBasinOp basin => basin.Height, TerrainChannelOp channel => channel.Depth, _ => 0d });

    internal static double CorridorReach(double rangeBound, double width, double crossMagnitude) =>
        width + (rangeBound + TerrainSampling.MaximumHeight - TerrainSampling.MinimumHeight + crossMagnitude * width) / (1d / 3 - crossMagnitude) + 4;

    internal static MapRect ConstraintBounds(TerrainBuildState b, MapRect core, double fill, double cut, double fillet, double crossFall = 0)
    {
        return ConstraintBounds(b.RangeBound, core, fill, cut, fillet, crossFall);
    }

    internal static MapRect ConstraintBounds(double rangeBound, MapRect core, double fill, double cut, double fillet, double crossFall = 0)
    {
        var effectiveGrade = Math.Min(fill, cut) - Math.Abs(crossFall);
        if (effectiveGrade <= 0) throw new ArgumentException("Constraint cross-fall must be smaller than both exterior grades for finite support.");
        var diameter = Math.Abs(core.Max.X - core.Min.X) + Math.Abs(core.Max.Y - core.Min.Y);
        return TerrainRegion.Expand(core, (rangeBound + TerrainSampling.MaximumHeight - TerrainSampling.MinimumHeight + Math.Abs(crossFall) * diameter) / effectiveGrade + 2 * fillet);
    }

    internal static TerrainEvaluation WithSurface(TerrainEvaluation previous, TerrainDifferential result, double crest, double toe, bool coveredTransition = false)
    {
        if (result == previous.Surface) return previous;
        var grade = Math.Sqrt(result.Dx * result.Dx + result.Dz * result.Dz);
        // Later shallow grading can blend into a covered bank without removing its structural
        // plan mask. An infinitesimal road/soil blend must not relabel a 1:1 bank as bare soil.
        var kind = (coveredTransition || previous.Class == TerrainClass.Transition) && grade > 0.5
            ? TerrainClass.Transition : grade >= 1d / 6 ? TerrainClass.Slope : TerrainClass.Flat;
        return previous with { Surface = result, Class = kind, CrestDistance = crest, ToeDistance = toe };
    }

    internal static TerrainEvaluation Constrain(TerrainEvaluation previous, TerrainDifferential target,
        TerrainDifferential distance, double fill, double cut, double fillet, bool coveredTransition = false)
    {
        if (distance.Height <= 0) return WithSurface(previous, target, distance.Height, double.PositiveInfinity, coveredTransition);
        var rounded = TerrainMath.FilletDistance(distance, fillet);
        var raising = previous.Surface.Height < target.Height;
        var grade = raising ? fill : cut;
        var envelope = raising ? target - rounded * fill : target + rounded * cut;
        var result = raising ? TerrainMath.SmoothMaximum(previous.Surface, envelope, grade * fillet)
            : TerrainMath.SmoothMinimum(previous.Surface, envelope, grade * fillet);
        // Foot blending is one-sided and bounded by the crown target; composing both smooth clamps would move the plateau.
        if (raising && result.Height > target.Height || !raising && result.Height < target.Height) result = target;
        var toe = Math.Abs(target.Height - previous.Surface.Height) / grade + 1.5 * fillet - distance.Height;
        return WithSurface(previous, result, distance.Height, toe, coveredTransition);
    }

    internal static TerrainProgram ComposeProgram(TerrainBuildState b, IEnumerable<TerrainNode> source)
    {
        var nodes = source.ToArray();
        if (nodes.Length == 0) return new([], (x, z) => ZeroBandWeight(b, x, z));
        var patches = nodes.SelectMany(node => node.IndexPatches ?? [node.Support]).ToArray();
        var support = new MapRect(new(patches.Min(box => box.Min.X), patches.Min(box => box.Min.Y)),
            new(patches.Max(box => box.Max.X), patches.Max(box => box.Max.Y)));
        var channels = b.Water.Where(water => water.Channel && !water.Underground).ToArray();
        var zeroBand = new TerrainNode("compiled-zero-band-core", "world", default, TerrainGroup.ZeroBandCore, 0, support,
            (previous, x, z) =>
            {
                if (previous.Surface == TerrainDifferential.Constant(0)) return previous;
                var bounded = Constrain(previous, TerrainDifferential.Constant(0), ZeroBandClearance(b, x, z), 0.5, 0.5, 2);
                return bounded.Surface == previous.Surface || channels.Any(water => water.Region.Contains(x, z)) ? previous : bounded;
            },
            Automatic: true) { IndexPatches = patches };
        // As with pad cores, one final bounded envelope owns the priority. It trims
        // earthworks before the protected edge without repeatedly multiplying falloffs.
        return new(nodes.Append(zeroBand), (x, z) => ZeroBandWeight(b, x, z));
    }

    private static TerrainDifferential ZeroBandClearance(TerrainBuildState b, double x, double z) =>
        -b.Campus.Distance(x, z) - TerrainDifferential.Constant(20);

    internal static void RequireZeroBandCore(TerrainBuildState b, TerrainDifferential target, double x, double z, string file, string id)
    {
        if (target.Height == 0) return;
        var bounded = Constrain(new(target, TerrainClass.Flat), TerrainDifferential.Constant(0),
            ZeroBandClearance(b, x, z), 0.5, 0.5, 2).Surface;
        if (Math.Abs(bounded.Height - target.Height) > TerrainMath.HeightTolerance)
            throw new WorldDataException(file, id,
                $"Fixed terrain core cannot meet the protected zero band within the 1:2 soil-grade limit at ({x:R},{z:R}): targetY={target.Height:R}, availableDistance={Math.Max(0, ZeroBandClearance(b, x, z).Height):R}.");
    }

    internal static TerrainDifferential ZeroBandWeight(TerrainBuildState b, double x, double z)
    {
        // contract-15 09:32 / 09:45: one common 20 m protected band and 10 m Hermite taper for automatic drainage.
        var inside = -b.Campus.Distance(x, z);
        return ZeroBandWeight(inside);
    }

    internal static TerrainDifferential ZeroBandWeight(TerrainDifferential inside)
    {
        if (inside.Height <= 20) return TerrainDifferential.Constant(0);
        if (inside.Height >= 30) return TerrainDifferential.Constant(1);
        var t = (inside.Height - 20) / 10; var derivative = 6 * t * (1 - t) / 10;
        return new(TerrainMath.Hermite(t), inside.Dx * derivative, inside.Dz * derivative);
    }
}
