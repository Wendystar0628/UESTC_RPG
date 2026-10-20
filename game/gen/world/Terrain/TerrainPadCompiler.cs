using Campus.Contracts;
using Campus.Contracts.Maps;
using static Campus.Gen.World.TerrainFieldGrading;
using static Campus.Gen.World.TerrainGeometry;
using static Campus.Gen.World.TerrainPadRoadComposition;

namespace Campus.Gen.World;

internal static class TerrainPadCompiler
{
    internal sealed record PadCheck(FeatureId Feature, TerrainRegion Region, double Y,
        double Apron, double GradeWidth, WorldFeature? Structure, string File, string Id);

    internal const double AutomaticPadApron = 0.8;

    internal const double AutomaticPadGrade = 6;

    internal static void AddPads(TerrainBuildState b, IEnumerable<TerrainPadOp> operations, TerrainProgram prePad)
    {
        foreach (var op in operations)
        {
            if (!b.WrittenPads.Add(op.Feature)) throw new WorldDataException(b.Files[op.Id], op.Id, "Multiple pad operations target one entity.");
            var region = b.References.PadArea(op.Feature, b.Files[op.Id], op.Id);
            AddPad(b, region, op.Feature, op.PadY ?? Median(prePad, region), op.Apron, op.Grade, op.Edge, op);
        }
        foreach (var feature in b.Context.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (b.WrittenPads.Contains(feature.Id)) continue;
            if (b.References.StructuralPadArea(feature) is not { } region) continue;
            AddPad(b, region, feature.Id, Median(prePad, region), AutomaticPadApron, AutomaticPadGrade, null, null);
        }
    }

    private static void AddPad(TerrainBuildState b, TerrainRegion region, FeatureId feature, double y, double apron, double gradeWidth,
        TerrainPadEdge? edge, TerrainPadOp? op)
    {
        var file = op is null ? b.References.SourceFiles(feature) : b.Files[op.Id]; var id = op?.Id ?? "auto-pad-" + feature.Value;
        b.Pads.Add(feature, (float)y);
        b.PadRegions.Add(feature, region);
        var structure = edge is null ? null : b.References.Structure(new(edge.StructureId), file, id, "edgeBand", "terrace", "retainingWall");
        if (edge is { Kind: TerrainEdgeKind.Terrace } && edge.Tread < Math.Max(2, 1.5 * edge.Riser))
            throw new WorldDataException(file, id, "Terraced retaining structure has insufficient tread depth.");
        var fill = structure is null ? 0.25 : 1d;
        var bounded = TerrainRegion.Expand(region.Bounds, apron + gradeWidth);
        // MapRect stores float coordinates. Its index box must round outwards;
        // the exact signed-distance guard below owns the physical influence limit.
        var support = new MapRect(new(MathF.BitDecrement(bounded.Min.X), MathF.BitDecrement(bounded.Min.Y)),
            new(MathF.BitIncrement(bounded.Max.X), MathF.BitIncrement(bounded.Max.Y)));
        var node = new TerrainNode(id, file, op?.Zone ?? default, TerrainGroup.Pad, op?.Priority ?? 50,
            support, (previous, x, z) =>
            {
                if (op is null && y == 0 && previous.BoundaryWeight is { Height: 0 } && previous.Surface == TerrainDifferential.Constant(0)) return previous;
                var distance = region.Distance(x, z);
                if (distance.Height <= 0) RequireZeroBandCore(b, TerrainDifferential.Constant(y), x, z, file, id);
                if (distance.Height > 0 && InsideOtherPadCore(b, id, x, z)) return previous;
                var beyond = distance - TerrainDifferential.Constant(apron);
                if (beyond.Height > gradeWidth) return previous;
                var target = TerrainDifferential.Constant(y);
                if (distance.Height > 0)
                {
                    var factor = op is null ? previous.BoundaryWeight ?? ZeroBandWeight(b, x, z) : TerrainDifferential.Constant(1);
                    // The outside envelope continues from the apron edge elevation. Resetting it
                    // to PadY at d=apron would create a vertical seam of 3% * apron.
                    var drop = (distance.Height < apron ? distance : TerrainDifferential.Constant(apron)) * 0.03;
                    target -= new TerrainDifferential(drop.Height * factor.Height,
                        drop.Dx * factor.Height + drop.Height * factor.Dx, drop.Dz * factor.Height + drop.Height * factor.Dz);
                }
                var covered = structure is not null && TerrainReferences.NearStructure(structure, new((float)x, (float)z), 0);
                return Constrain(previous, target, beyond, fill, fill, 2, covered);
            }, op is null);
        b.Nodes.Add(node);
        b.PadChecks.Add(new(feature, region, y, apron, gradeWidth, structure, file, id));
        if (structure is not null) b.Diagnostics.Add($"NOT_VERIFIED: pad edge '{edge!.StructureId}' plan reference resolved; generated vertical collision connections require the content/physics acceptance pass.");
    }

    private static double Median(TerrainProgram beforePads, TerrainRegion region)
    {
        var samples = new List<double>();
        for (var z = (int)Math.Ceiling(region.Bounds.Min.Y); z <= (int)Math.Floor(region.Bounds.Max.Y); z++)
            for (var x = (int)Math.Ceiling(region.Bounds.Min.X); x <= (int)Math.Floor(region.Bounds.Max.X); x++)
                if (region.Contains(x, z)) samples.Add(beforePads.Evaluate(x, z).Surface.Height);
        if (samples.Count == 0)
        {
            // An area with no 1 m lattice sample still needs an interior sample, not its possibly-outside centroid.
            var point = InteriorPoint(region);
            samples.Add(beforePads.Evaluate(point.X, point.Y).Surface.Height);
        }
        samples.Sort(); var middle = samples.Count / 2;
        var median = samples.Count % 2 == 0 ? (samples[middle - 1] + samples[middle]) / 2 : samples[middle];
        return Math.Round(median / 0.05, MidpointRounding.ToEven) * 0.05;
    }
}
