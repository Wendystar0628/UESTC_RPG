using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.World;

internal static partial class TerrainCompiler
{
    private static void ValidateFinalPads(Builder b, TerrainProgram program)
    {
        foreach (var pad in b.PadChecks.OrderBy(pad => pad.Feature.Value, StringComparer.Ordinal))
            new PadSeamChecker(pad, program).Run();
    }

    /// <summary>Checks final building-edge height against available grading space or an actual structure.</summary>
    private sealed class PadSeamChecker(PadCheck pad, TerrainProgram program)
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

        public void Run()
        {
            foreach (var polygon in exposed.Polygons)
                foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                    for (var i = 0; i < ring.Count; i++) CheckEdge(ring, i);
        }

        private void CheckEdge(IReadOnlyList<Vector2> ring, int i)
        {
            var a = ring[i]; var c = ring[(i + 1) % ring.Count];
            var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
            var steps = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dz * dz)));
            for (var j = 0; j <= steps; j++)
                CheckBoundaryHeight(a.X + dx * j / steps, a.Y + dz * j / steps);
        }

        private void CheckBoundaryHeight(double x, double z)
        {
            var ground = program.Evaluate(x, z).Surface.Height;
            var difference = Math.Abs(ground - pad.Y);
            if ((difference > 1.2 + 2 * TerrainSampling.Centimetre || difference > 0.25 * pad.GradeWidth + 2 * TerrainSampling.Centimetre) && !Covered(x, z))
                throw new WorldDataException(pad.File, pad.Id,
                    $"Pad needs an actual edge/terrace structure after all corridors: feature='{pad.Feature.Value}', boundary=({x:R},{z:R}), padY={pad.Y:R}, groundY={ground:R}, ground difference={difference:R} m, available grade width={pad.GradeWidth:R} m. Contributors: {program.Explain(x, z)}");
        }

        private bool Covered(double x, double z) => pad.Structure is { } structure
            && TerrainReferences.NearStructure(structure, new((float)x, (float)z), 0);
    }
}
