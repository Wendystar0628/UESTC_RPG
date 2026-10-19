using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

internal static partial class TerrainCompiler
{
    private static TerrainProgram ComposeProgram(Builder b, IEnumerable<TerrainNode> source)
    {
        var nodes = source.ToArray();
        if (nodes.Length == 0) return new([], (x, z) => ZeroBandWeight(b, x, z));
        var patches = nodes.SelectMany(node => node.IndexPatches ?? [node.Support]).ToArray();
        var support = new MapRect(new(patches.Min(box => box.Min.X), patches.Min(box => box.Min.Y)),
            new(patches.Max(box => box.Max.X), patches.Max(box => box.Max.Y)));
        var channels = b.Water.Where(water => water.Channel && !water.Underground).ToArray();
        var zeroBand = new TerrainNode("compiled-zero-band-core", "overlay/terrain", default, TerrainGroup.ZeroBandCore, 0, support,
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

    private static TerrainDifferential ZeroBandClearance(Builder b, double x, double z) =>
        -b.Campus.Distance(x, z) - TerrainDifferential.Constant(20);

    private static void RequireZeroBandCore(Builder b, TerrainDifferential target, double x, double z, string file, string id)
    {
        if (target.Height == 0) return;
        var bounded = Constrain(new(target, TerrainClass.Flat), TerrainDifferential.Constant(0),
            ZeroBandClearance(b, x, z), 0.5, 0.5, 2).Surface;
        if (Math.Abs(bounded.Height - target.Height) > TerrainMath.HeightTolerance)
            throw new WorldDataException(file, id,
                $"Fixed terrain core cannot meet the protected zero band within the 1:2 soil-grade limit at ({x:R},{z:R}): targetY={target.Height:R}, availableDistance={Math.Max(0, ZeroBandClearance(b, x, z).Height):R}.");
    }

    private sealed record FixedRoadAnchor(double X, double Z, double Y, string File, string Id);
    private sealed record PadRoadComposition(PadCheck Pad, FixedRoadAnchor[] Anchors)
    {
        public double Width => Pad.Apron + Pad.GradeWidth;
    }

    private static void PreparePadRoadComposition(Builder b, IReadOnlyList<CorridorPlan> plans)
    {
        var anchors = plans.SelectMany(plan => plan.Operations.SelectMany(op => op.Anchors
            .Where(anchor => !plan.Exclusions.Any(span => anchor.S >= span.From && anchor.S <= span.To))
            .Select(anchor =>
            {
                var point = plan.Road.Path.AtDouble(anchor.S);
                return new FixedRoadAnchor(point.X, point.Z, anchor.Y, b.Files[op.Id], op.Id);
            }))).ToArray();
        b.RoadAnchors = anchors;
        var conflicts = new List<(PadCheck Pad, FixedRoadAnchor Anchor)>();
        foreach (var pad in b.PadChecks)
        {
            var nearby = new List<FixedRoadAnchor>();
            foreach (var anchor in anchors)
            {
                var distance = pad.Region.Distance(anchor.X, anchor.Z).Height;
                if (distance <= 0 && Math.Abs(anchor.Y - pad.Y) > TerrainMath.HeightTolerance)
                    conflicts.Add((pad, anchor));
                if (distance < pad.Apron + pad.GradeWidth) nearby.Add(anchor);
            }
            b.PadRoadCompositions.Add(pad.Id, new(pad, nearby.ToArray()));
        }
        foreach (var anchor in anchors)
            RequireZeroBandCore(b, TerrainDifferential.Constant(anchor.Y), anchor.X, anchor.Z, anchor.File, anchor.Id);
        if (conflicts.Count > 0)
            throw new WorldDataException(conflicts[0].Anchor.File, conflicts[0].Anchor.Id,
                $"Authored centreline anchors conflict with building cores ({conflicts.Count} fixed conflicts; neither elevation can be moved by grading):\n"
                + string.Join("\n", conflicts.Select(conflict =>
                    $"pad='{conflict.Pad.Feature.Value}' ({conflict.Pad.File} [{conflict.Pad.Id}]), anchor='{conflict.Anchor.Id}' ({conflict.Anchor.File}), point=({conflict.Anchor.X:R},{conflict.Anchor.Z:R}), padY={conflict.Pad.Y:R}, anchorY={conflict.Anchor.Y:R}.")));
    }

    private static void ValidateFinalRoadAnchors(Builder b, TerrainProgram program)
    {
        var conflicts = new List<(FixedRoadAnchor Anchor, double Height)>();
        foreach (var anchor in b.RoadAnchors)
        {
            var height = program.Evaluate(anchor.X, anchor.Z).Surface.Height;
            if (Math.Abs(height - anchor.Y) > TerrainMath.HeightTolerance) conflicts.Add((anchor, height));
        }
        if (conflicts.Count > 0)
            throw new WorldDataException(conflicts[0].Anchor.File, conflicts[0].Anchor.Id,
                $"Final terrain conflicts with {conflicts.Count} authored centreline anchors; fixed values were not moved:\n"
                + string.Join("\n", conflicts.Select(conflict =>
                    $"anchor='{conflict.Anchor.Id}' ({conflict.Anchor.File}), point=({conflict.Anchor.X:R},{conflict.Anchor.Z:R}), anchorY={conflict.Anchor.Y:R}, finalY={conflict.Height:R}, deltaY={conflict.Height - conflict.Anchor.Y:R}.")));
    }

    private static bool InsideOtherPadCore(Builder b, string id, double x, double z)
    {
        foreach (var node in b.LandBeforeCorridors.CandidatesAt(x, z))
            if (node.Group == TerrainGroup.Pad && node.Id != id
                && b.PadRoadCompositions.TryGetValue(node.Id, out var other)
                && other.Pad.Region.Distance(x, z).Height <= 0) return true;
        return false;
    }

    private static void AddPadCoreComposition(Builder b)
    {
        if (b.PadRoadCompositions.Count == 0) return;
        var patches = b.LandBeforeCorridors.Nodes.Where(node => node.Group == TerrainGroup.Pad).Select(node => node.Support).ToArray();
        var support = new MapRect(new(patches.Min(box => box.Min.X), patches.Min(box => box.Min.Y)),
            new(patches.Max(box => box.Max.X), patches.Max(box => box.Max.Y)));
        b.Nodes.Add(new("compiled-pad-cores", "overlay/terrain", default, TerrainGroup.PadCore, 0, support,
            (previous, x, z) =>
            {
                var beforeRoads = b.LandBeforeCorridors.Evaluate(x, z);
                var change = previous.Surface - beforeRoads.Surface;
                if (change == TerrainDifferential.Constant(0)) return previous;
                var weight = TerrainDifferential.Constant(1);
                foreach (var node in b.LandBeforeCorridors.CandidatesAt(x, z))
                {
                    if (node.Group != TerrainGroup.Pad || !b.PadRoadCompositions.TryGetValue(node.Id, out var composition)) continue;
                    var distance = composition.Pad.Region.Distance(x, z);
                    if (distance.Height >= composition.Width) continue;
                    if (distance.Height <= 0)
                    {
                        return WithSurface(previous, beforeRoads.Surface, beforeRoads.CrestDistance, beforeRoads.ToeDistance,
                            beforeRoads.Class == TerrainClass.Transition);
                    }
                    // Reuse the completed pre-road pad field. Only this final composition
                    // owns the mask, so overlapping roads cannot repeatedly erode it.
                    var local = PadRoadWeight(distance, TerrainDifferential.Constant(composition.Width), change);
                    foreach (var anchor in composition.Anchors)
                    {
                        var dx = x - anchor.X; var dz = z - anchor.Z; var radius = Math.Sqrt(dx * dx + dz * dz);
                        var toAnchor = radius == 0 ? TerrainDifferential.Constant(0) : new TerrainDifferential(radius, dx / radius, dz / radius);
                        var anchored = PadRoadWeight(distance, distance + toAnchor, change);
                        if (anchored.Height > local.Height) local = anchored;
                    }
                    if (local.Height < weight.Height) weight = local;
                }
                if (weight.Height == 1) return previous;
                var surface = beforeRoads.Surface + TerrainDifferential.Product(change, weight);
                return WithSurface(previous, surface, beforeRoads.CrestDistance, previous.ToeDistance,
                    beforeRoads.Class == TerrainClass.Transition);
            }, Automatic: true) { IndexPatches = patches });
    }

    private static TerrainDifferential PadRoadWeight(TerrainDifferential distance, TerrainDifferential width, TerrainDifferential change)
    {
        var t = TerrainDifferential.Divide(distance, width);
        if (t.Height <= 0) return TerrainDifferential.Constant(0);
        if (t.Height >= 1) return TerrainDifferential.Constant(1);
        var peak = 1.5; var peakDx = 0d; var peakDz = 0d;
        if (Math.Abs(change.Height) > TerrainMath.HeightTolerance)
        {
            var magnitude = change * (change.Height > 0 ? 1 : -1);
            var limit = TerrainDifferential.Divide(width * 0.5, magnitude);
            // Reuse the existing bounded C1 easing when a full Hermite shoulder would
            // exceed the 1:2 soil budget. An insufficient mean grade still fails the
            // existing composed-surface check; no height or classification is invented.
            peak = Math.Clamp((1 + limit.Height) / 2, 1 + TerrainMath.GradeTolerance, 1.5);
            if (peak > 1 + TerrainMath.GradeTolerance && peak < 1.5)
            {
                peakDx = limit.Dx / 2; peakDz = limit.Dz / 2;
            }
        }
        var (value, derivative) = LimitedEase(t.Height, peak);
        var byPeak = LimitedEasePeakDerivative(t.Height, peak);
        return new(value, derivative * t.Dx + byPeak * peakDx, derivative * t.Dz + byPeak * peakDz);
    }
}
