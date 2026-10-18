using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

internal static partial class TerrainCompiler
{
    private sealed class PadRayStats
    {
        private long count, totalCandidates, totalEvents;
        private int maximumCandidates, maximumEvents;
        private string largest = "";

        public void Record(PadRayTrace trace, PadCheck pad, PadRay ray)
        {
            count++; totalCandidates += trace.Candidates; totalEvents += trace.Events;
            maximumCandidates = Math.Max(maximumCandidates, trace.Candidates);
            if (trace.Events <= maximumEvents) return;
            maximumEvents = trace.Events;
            largest = $"feature='{pad.Feature.Value}', source='{pad.File}'/'{pad.Id}', origin=({ray.X:R},{ray.Z:R}), direction=({ray.Nx:R},{ray.Nz:R})";
        }

        public string Summary() =>
            $"Final pad geometric rays: rays={count}, totalCandidates={totalCandidates}, totalEvents={totalEvents}, maxCandidatesPerRay={maximumCandidates}, maxEventsPerRay={maximumEvents}; largestEventRay={largest}.";
    }

    private readonly record struct PadRay(double X, double Z, double Nx, double Nz, double Length)
    {
        public (double X, double Z) At(double d) => (X + Nx * d, Z + Nz * d);
    }

    private readonly record struct PadSample(double D, TerrainDifferential Actual, TerrainDifferential Without);

    /// <summary>One pad's final-grading check: walks the exposed footprint boundary and probes the outward ground strip.</summary>
    private sealed class PadSeamChecker(PadCheck pad, TerrainProgram program, PadRayStats stats)
    {
        private readonly TerrainRegion exposed = BuildExposed(pad);
        private readonly double reach = pad.Apron + pad.GradeWidth;

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

        // ---- boundary walk ----

        private void CheckEdge(IReadOnlyList<Vector2> ring, int i)
        {
            var a = ring[i]; var c = ring[(i + 1) % ring.Count];
            var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
            var edgeNormal = EdgeNormal(ring, i);
            var before = EdgeNormal(ring, (i + ring.Count - 1) % ring.Count);
            var after = EdgeNormal(ring, (i + 1) % ring.Count);
            var startOffset = OffsetCorner(before, edgeNormal, edgeNormal);
            var endOffset = OffsetCorner(edgeNormal, after, edgeNormal);
            var outerDx = dx + reach * (endOffset.X - startOffset.X);
            var outerDz = dz + reach * (endOffset.Z - startOffset.Z);
            var contourLength = Math.Max(Math.Sqrt(dx * dx + dz * dz), Math.Sqrt(outerDx * outerDx + outerDz * outerDz));
            if (!double.IsFinite(contourLength) || contourLength >= int.MaxValue)
                throw new WorldDataException(pad.File, pad.Id, "Pad offset contour cannot be represented by the validation lattice.");
            var steps = Math.Max(1, (int)Math.Ceiling(contourLength));
            for (var j = 0; j <= steps; j++)
            {
                var x = a.X + dx * j / steps; var z = a.Y + dz * j / steps;
                // A multipart footprint may cover another part's ring. Only the
                // boundary of the actual filled region is a building/ground seam.
                if (Math.Abs(exposed.Distance(x, z).Height) > 0.000001) continue;
                CheckBoundaryHeight(x, z);
                var t = j / (double)steps;
                ProbeSeam(x, z, startOffset.X + t * (endOffset.X - startOffset.X), startOffset.Z + t * (endOffset.Z - startOffset.Z));
            }
            // Convex corners have a curved offset boundary between their two edge
            // normals. Sample that arc too; testing only edge offsets misses it.
            if (before.X * edgeNormal.Z - before.Z * edgeNormal.X > 0)
                ProbeCorner(a.X, a.Y, before.X, before.Z, edgeNormal.X, edgeNormal.Z);
        }

        private void CheckBoundaryHeight(double x, double z)
        {
            var ground = program.Evaluate(x, z).Surface.Height;
            var difference = Math.Abs(ground - pad.Y);
            if ((difference > 1.2 || difference / PadGradeLimit > pad.GradeWidth) && !Covered(x, z))
                throw new WorldDataException(pad.File, pad.Id,
                    $"Pad needs an actual edge/terrace structure after all corridors: feature='{pad.Feature.Value}', boundary=({x:R},{z:R}), padY={pad.Y:R}, groundY={ground:R}, ground difference={difference:R} m, available grade width={pad.GradeWidth:R} m. Contributors: {program.Explain(x, z)}");
        }

        private (double X, double Z) EdgeNormal(IReadOnlyList<Vector2> ring, int edge)
        {
            var p = ring[edge]; var q = ring[(edge + 1) % ring.Count];
            var ex = (double)q.X - p.X; var ez = (double)q.Y - p.Y;
            var length = Math.Sqrt(ex * ex + ez * ez);
            if (!double.IsFinite(length) || length == 0)
                throw new WorldDataException(pad.File, pad.Id, "Pad has a degenerate boundary edge.");
            // TerrainRegion canonicalizes outer CCW / holes CW. Its right
            // normal points into ground for both; the ray trace uses the union.
            return (ez / length, -ex / length);
        }

        private (double X, double Z) OffsetCorner((double X, double Z) a, (double X, double Z) c, (double X, double Z) edge)
        {
            if (a.X * c.Z - a.Z * c.X >= 0) return edge;
            // At a concave corner the true offset contour is the intersection of
            // the two offset lines, not a circle about the original vertex.
            var denominator = 1 + a.X * c.X + a.Z * c.Z;
            if (denominator <= 0) throw new WorldDataException(pad.File, pad.Id, "Pad has a degenerate concave offset corner.");
            var result = ((a.X + c.X) / denominator, (a.Z + c.Z) / denominator);
            if (!double.IsFinite(result.Item1) || !double.IsFinite(result.Item2))
                throw new WorldDataException(pad.File, pad.Id, "Pad has a non-finite concave offset corner.");
            return result;
        }

        private void ProbeCorner(double x, double z, double ax, double az, double cx, double cz)
        {
            if (ax == 0 && az == 0 || cx == 0 && cz == 0)
                throw new WorldDataException(pad.File, pad.Id, "Pad has a degenerate convex offset corner.");
            var chordX = reach * (cx - ax); var chordZ = reach * (cz - az);
            if (chordX * chordX + chordZ * chordZ <= 1) return;
            var nx = ax + cx; var nz = az + cz; var length = Math.Sqrt(nx * nx + nz * nz);
            if (length == 0) throw new WorldDataException(pad.File, pad.Id, "Pad has an unresolved convex offset arc.");
            nx /= length; nz /= length;
            ProbeSeam(x, z, nx, nz);
            ProbeCorner(x, z, ax, az, nx, nz);
            ProbeCorner(x, z, nx, nz, cx, cz);
        }

        // ---- one outward ray ----

        private void ProbeSeam(double x, double z, double nx, double nz)
        {
            var ray = MakeRay(x, z, nx, nz);
            var trace = Trace(ray);
            stats.Record(trace, pad, ray);
            var limit = trace.End;
            var (lx, lz) = ray.At(limit);
            if (exposed.Distance(lx, lz).Height < pad.Apron)
                Fail(ray, trace, "has no real apron/ground section before another footprint edge", limit, Sample(ray, limit));
            var apronParameter = FindApronStart(ray, limit);
            var samples = SampleSection(ray, apronParameter, limit);
            if (!TrimToChangedSection(ray, trace, samples)) return; // The final corridors already replace this entire section.
            CheckSamples(ray, trace, samples, apronParameter);
        }

        private PadRay MakeRay(double x, double z, double nx, double nz)
        {
            var normalLength = Math.Sqrt(nx * nx + nz * nz);
            if (normalLength == 0 || !double.IsFinite(normalLength) || reach * normalLength > int.MaxValue / 2)
                throw new WorldDataException(pad.File, pad.Id, "Pad offset direction is non-finite or cannot be represented by the validation lattice.");
            return new(x, z, nx, nz, normalLength);
        }

        private PadRayTrace Trace(PadRay ray)
        {
            try { return TracePadRayBoundary(exposed, pad.Node.Support, ray.X, ray.Z, ray.Nx, ray.Nz, reach); }
            catch (ArgumentException error)
            {
                throw new WorldDataException(pad.File, pad.Id,
                    $"Pad '{pad.Feature.Value}' has an invalid geometric ray: origin=({ray.X:R},{ray.Z:R}), direction=({ray.Nx:R},{ray.Nz:R}).", error);
            }
        }

        private double FindApronStart(PadRay ray, double limit)
        {
            var low = 0d; var high = limit;
            while ((high - low) * ray.Length > 0.000001)
            {
                var mid = (low + high) / 2;
                var (mx, mz) = ray.At(mid);
                if (exposed.Distance(mx, mz).Height < pad.Apron) low = mid;
                else high = mid;
            }
            return high;
        }

        private List<PadSample> SampleSection(PadRay ray, double apronParameter, double limit)
        {
            var available = limit - apronParameter;
            var sectionLength = available * ray.Length;
            if (!double.IsFinite(sectionLength) || sectionLength > int.MaxValue / 2)
                throw new WorldDataException(pad.File, pad.Id, "Pad offset section cannot be represented by the validation lattice.");
            var count = Math.Max(1, (int)Math.Ceiling(sectionLength));
            var samples = new List<PadSample>();
            // Midpoints supplement the established 1 m validation lattice, especially
            // where a corridor enters or exits the final grading strip between vertices.
            for (var i = 0; i <= 2 * count; i++)
                samples.Add(Sample(ray, apronParameter + available * i / (2 * count)));
            // Probe the actual inside limit as well as the boundary. A floating-point
            // endpoint just outside a finite pad must not disguise an unfinished cut.
            if (available > 0.000001) samples.Insert(samples.Count - 1, Sample(ray, limit - 0.000001));
            return samples;
        }

        /// <summary>Cuts the samples after the last place this pad still changes the ground; false when it changes nothing.</summary>
        private bool TrimToChangedSection(PadRay ray, PadRayTrace trace, List<PadSample> samples)
        {
            var lastChanged = samples.FindLastIndex(sample => !Same(sample.Actual, sample.Without));
            if (lastChanged == samples.Count - 1)
            {
                var end = samples[^1];
                // Even a real wall cannot authorize a hard-clipped pad envelope.
                Fail(ray, trace, "does not reach the same final ground without this pad within its available grade width", end.D, end);
            }
            if (lastChanged < 0) return false;
            var changed = samples[lastChanged]; var returned = samples[lastChanged + 1];
            // Both final evaluations use the identical solved road/water program. Only
            // this exact pad node is omitted; no new profiles or defaults are constructed.
            while (returned.D - changed.D > 0.000001)
            {
                var mid = Sample(ray, (changed.D + returned.D) / 2);
                if (Same(mid.Actual, mid.Without)) returned = mid; else changed = mid;
            }
            samples.RemoveRange(lastChanged + 1, samples.Count - lastChanged - 1);
            samples.Add(changed); samples.Add(returned);
            return true;
        }

        private void CheckSamples(PadRay ray, PadRayTrace trace, List<PadSample> samples, double apronParameter)
        {
            var apronHeight = samples[0].Actual.Height;
            for (var i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                var (px, pz) = ray.At(sample.D);
                var width = (sample.D - apronParameter) * ray.Length;
                var difference = Math.Abs(sample.Actual.Height - apronHeight);
                if ((difference > 1.2 || difference > PadGradeLimit * width + 0.000001) && !CoveredHeightSection(ray, samples, i))
                    Fail(ray, trace, "exceeds its 1.2 m height / 1:4 available-width limit after all corridors", sample.D, sample);
                CheckPlanarGrade(ray, trace, sample);
                var grade = Math.Abs(sample.Actual.Dx * ray.Nx + sample.Actual.Dz * ray.Nz) / ray.Length;
                if (grade > PadGradeComparisonLimit && !Covered(px, pz))
                    Fail(ray, trace, $"exceeds its 1:4 final section grade ({grade:R})", sample.D, sample);
                if (i == 0) continue;
                var previous = samples[i - 1]; var interval = (sample.D - previous.D) * ray.Length;
                if (interval == 0) continue;
                var secant = Math.Abs(sample.Actual.Height - previous.Actual.Height) / interval;
                if (secant > PadGradeComparisonLimit && !CoveredSpan(ray, previous.D, sample.D))
                    Fail(ray, trace, $"has a final grading/end connection jump or section steeper than 1:4 ({secant:R})", sample.D, sample);
            }
        }

        private void CheckPlanarGrade(PadRay ray, PadRayTrace trace, PadSample sample)
        {
            var (px, pz) = ray.At(sample.D);
            var sdf = exposed.Distance(px, pz).Height;
            if (sdf < pad.Apron || sdf > reach || Same(sample.Actual, sample.Without)) return;
            var squaredGrade = sample.Actual.Dx * sample.Actual.Dx + sample.Actual.Dz * sample.Actual.Dz;
            if (!double.IsFinite(squaredGrade)) Fail(ray, trace, "has a non-finite two-dimensional final grade", sample.D, sample);
            if (PadSquaredGradeExceedsComparison(squaredGrade) && !Covered(px, pz))
                Fail(ray, trace, $"exceeds its 1:4 two-dimensional final grade (squaredGrade={squaredGrade:R})", sample.D, sample);
        }

        // ---- helpers ----

        private bool CoveredHeightSection(PadRay ray, List<PadSample> samples, int endIndex)
        {
            var changedSection = false;
            for (var j = 1; j <= endIndex; j++)
            {
                var a = samples[j - 1]; var c = samples[j];
                if (Math.Abs(c.Actual.Height - a.Actual.Height) <= 0.000001) continue;
                changedSection = true;
                if (!CoveredSpan(ray, a.D, c.D)) return false;
            }
            return changedSection;
        }

        private bool CoveredSpan(PadRay ray, double d0, double d1)
        {
            var (x0, z0) = ray.At(d0); var (x1, z1) = ray.At(d1); var (xm, zm) = ray.At((d0 + d1) / 2);
            return Covered(x0, z0) && Covered(x1, z1) && Covered(xm, zm);
        }

        private bool Covered(double x, double z) => pad.Structure is { } structure
            && TerrainReferences.NearStructure(structure, new((float)x, (float)z), 0);

        private static bool Same(TerrainDifferential actual, TerrainDifferential without) => Math.Abs(actual.Height - without.Height) <= 0.000001
            && Math.Abs(actual.Dx - without.Dx) <= 0.000001 && Math.Abs(actual.Dz - without.Dz) <= 0.000001;

        private PadSample Sample(PadRay ray, double d)
        {
            var (sx, sz) = ray.At(d);
            return new(d, program.Evaluate(sx, sz).Surface, program.EvaluateExcept(sx, sz, pad.Node).Surface);
        }

        private void Fail(PadRay ray, PadRayTrace trace, string reason, double d, PadSample sample)
        {
            var (sx, sz) = ray.At(d);
            throw new WorldDataException(pad.File, pad.Id,
                $"Pad final grading after all corridors {reason}: feature='{pad.Feature.Value}', sectionOrigin=({ray.X:R},{ray.Z:R}), rayDirection=({ray.Nx:R},{ray.Nz:R}), point=({sx:R},{sz:R}), rayParameter={d:R}, distanceBeyondApron={exposed.Distance(sx, sz).Height - pad.Apron:R}, declaredGradeWidth={pad.GradeWidth:R}, rawSquaredGrade={sample.Actual.Dx * sample.Actual.Dx + sample.Actual.Dz * sample.Actual.Dz:R}, mathematicalGradeLimit={PadGradeLimit:R}, gradeComparisonTolerance={PadGradeComparisonTolerance:R}, rayCandidates={trace.Candidates}, rayEvents={trace.Events}, actual={sample.Actual}, sameProgramWithoutPad={sample.Without}. Contributors: {program.Explain(sx, sz)}");
        }
    }
}
