using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using static Campus.Gen.World.TerrainPadCompiler;

namespace Campus.Gen.World;

internal static class TerrainAcceptance
{
    internal static void ValidateFinalPads(TerrainBuildState b, TerrainProgram program)
    {
        var failures = new List<(PadCheck Pad, string Detail)>();
        foreach (var pad in b.PadChecks.OrderBy(pad => pad.Feature.Value, StringComparer.Ordinal))
            if (new PadSeamChecker(pad, program).Run() is { } detail)
                failures.Add((pad, detail));
        if (failures.Count > 0)
            throw new WorldDataException(failures[0].Pad.File, failures[0].Pad.Id,
                $"Final pad boundary conflicts ({failures.Count} pads; first invalid boundary per pad):\n"
                + string.Join("\n", failures.Select(failure => $"{failure.Pad.Id} ({failure.Pad.File}): {failure.Detail}")));
    }

    /// <summary>Checks final building-edge height against available grading space or an actual structure.</summary>
    internal sealed class PadSeamChecker(PadCheck pad, TerrainProgram program)
    {
        private readonly TerrainRegion exposed = BuildExposed(pad);

        private static TerrainRegion BuildExposed(PadCheck pad)
        {
            try { return TerrainUnion.Build([pad.Region]); }
            catch (ArgumentException error)
            {
                throw new WorldDataException(pad.File, pad.Id,
                    $"Pad '{pad.Feature.Value}' has no valid exposed union boundary; its multipart footprint has inconsistent or non-manifold topology.", error);
            }
        }

        public string? Run()
        {
            foreach (var polygon in exposed.Polygons)
                foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                    for (var i = 0; i < ring.Count; i++)
                        if (CheckEdge(ring, i) is { } detail) return detail;
            return null;
        }

        private string? CheckEdge(IReadOnlyList<Vector2> ring, int i)
        {
            var a = ring[i]; var c = ring[(i + 1) % ring.Count];
            var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
            var steps = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dz * dz)));
            for (var j = 0; j <= steps; j++)
                if (CheckBoundaryHeight(a.X + dx * j / steps, a.Y + dz * j / steps) is { } detail) return detail;
            return null;
        }

        private string? CheckBoundaryHeight(double x, double z)
        {
            var ground = program.Evaluate(x, z).Surface.Height;
            var difference = Math.Abs(ground - pad.Y);
            if ((difference > 1.2 + 2 * TerrainSampling.Centimetre || difference > 0.25 * pad.GradeWidth + 2 * TerrainSampling.Centimetre) && !Covered(x, z))
                return $"Pad needs an actual edge/terrace structure after all corridors: feature='{pad.Feature.Value}', boundary=({x:R},{z:R}), padY={pad.Y:R}, groundY={ground:R}, ground difference={difference:R} m, available grade width={pad.GradeWidth:R} m. Contributors: {program.Explain(x, z)}";
            return null;
        }

        private bool Covered(double x, double z) => pad.Structure is { } structure
            && TerrainReferences.NearStructure(structure, new((float)x, (float)z), 0);
    }

    internal static void ValidateProgram(TerrainBuildState b, TerrainProgram program)
    {
        var minimum = 0d; var maximum = 0d; var maximumGrade = 0d; long count = 0;
        var rect = b.Context.DataRect;
        foreach (var tile in program.AffectedTiles())
        {
            var startX = Math.Max((double)tile.X * TileGrid.BaseEdge, Math.Ceiling(rect.Min.X));
            var startZ = Math.Max((double)tile.Z * TileGrid.BaseEdge, Math.Ceiling(rect.Min.Y));
            var endX = Math.Min(((double)tile.X + 1) * TileGrid.BaseEdge - 1, Math.Floor(rect.Max.X));
            var endZ = Math.Min(((double)tile.Z + 1) * TileGrid.BaseEdge - 1, Math.Floor(rect.Max.Y));
            for (var z = startZ; z <= endZ; z++)
                for (var x = startX; x <= endX; x++)
                {
                    var sample = program.Evaluate(x, z); var h = sample.Surface.Height;
                    var grade = Math.Sqrt(sample.Surface.Dx * sample.Surface.Dx + sample.Surface.Dz * sample.Surface.Dz);
                    if (h < TerrainSampling.MinimumHeight || h > TerrainSampling.MaximumHeight)
                        throw new WorldDataException("world", "height", $"Unclamped height {h:R} at ({x}, {z}) is outside [-5,10].");
                    if (grade > 1 + TerrainMath.GradeTolerance || grade > 0.5 + TerrainMath.GradeTolerance && sample.Class != TerrainClass.Transition)
                        throw new WorldDataException("world", "grade", $"Final composed grade {grade:R} at ({x}, {z}) exceeds its terrain class limit. {program.Explain(x, z)}");
                    var protectedBand = sample.BoundaryWeight?.Height == 0;
                    var channel = b.Water.Any(w => w.Channel && !w.Underground && w.Region.Contains(x, z));
                    if (protectedBand && Math.Abs(h) > TerrainMath.HeightTolerance && !channel)
                        throw new WorldDataException("world", "zeroBand", $"Height {h:R} at ({x}, {z}) changes the protected campus zero band/outside area. {program.Explain(x, z)}");
                    minimum = Math.Min(minimum, h); maximum = Math.Max(maximum, h); maximumGrade = Math.Max(maximumGrade, grade); count++;
                }
        }
        for (var i = 0; i < program.Nodes.Count; i++)
        {
            var node = program.Nodes[i]; if (node.Automatic || node.HasSeparateAuthorValidation) continue;
            var index = i;
            ValidateAuthored(node, (x, z) => program.Evaluate(x, z, index));
        }
        foreach (var node in b.WaterAuthorValidation.OrderBy(node => node.Priority).ThenBy(node => node.Id, StringComparer.Ordinal))
            ValidateAuthored(node, (x, z) => b.LandBeforeWater.Evaluate(x, z));
        b.Diagnostics.Add($"Terrain 1 m validation: samples={count}, height=[{minimum:R},{maximum:R}], maxGrade={maximumGrade:R}; centimetre quantization has not hidden out-of-range values.");

        void ValidateAuthored(TerrainNode node, Func<double, double, TerrainEvaluation> preceding)
        {
            var zone = b.Context.Zones is { } zones && zones.TryGetValue(node.Zone, out var polygons) ? new TerrainRegion(polygons) : null;
            if (zone is null) return;
            var x0 = Math.Max(Math.Ceiling(node.Support.Min.X), Math.Ceiling(rect.Min.X));
            var z0 = Math.Max(Math.Ceiling(node.Support.Min.Y), Math.Ceiling(rect.Min.Y));
            var x1 = Math.Min(Math.Floor(node.Support.Max.X), Math.Floor(rect.Max.X));
            var z1 = Math.Min(Math.Floor(node.Support.Max.Y), Math.Floor(rect.Max.Y));
            for (var z = z0; z <= z1; z++)
                for (var x = x0; x <= x1; x++)
                {
                    if (zone.Contains(x, z)) continue;
                    var before = preceding(x, z); var after = node.Apply(before, x, z);
                    if (Math.Abs(before.Surface.Height - after.Surface.Height) <= TerrainMath.HeightTolerance) continue;
                    throw new WorldDataException(node.File, node.Id, $"Terrain influence (including its slope/fillet) leaves authoritative zone '{node.Zone.Value}' at ({x}, {z}).");
                }
        }
    }

    internal static void ValidatePadOverlaps(TerrainBuildState b)
    {
        var pads = b.PadRegions.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal).ToArray();
        for (var i = 0; i < pads.Length; i++)
            for (var j = i + 1; j < pads.Length; j++)
            {
                if (Math.Abs(b.Pads[pads[i].Key] - b.Pads[pads[j].Key]) <= 0.15) continue;
                var a = pads[i].Value; var c = pads[j].Value;
                if (a.Bounds.Max.X <= c.Bounds.Min.X || c.Bounds.Max.X <= a.Bounds.Min.X || a.Bounds.Max.Y <= c.Bounds.Min.Y || c.Bounds.Max.Y <= a.Bounds.Min.Y) continue;
                var overlap = a.InteriorOverlaps(c);
                if (overlap) throw new WorldDataException("world", "pad", $"Overlapping pads '{pads[i].Key.Value}' and '{pads[j].Key.Value}' differ by more than 0.15 m.");
            }
    }
}
