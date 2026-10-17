using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

internal static partial class TerrainCompiler
{
    internal const double PadGradeLimit = 0.25;
    internal const double PadGradeComparisonTolerance = 0.000001;
    internal const double PadGradeComparisonLimit = PadGradeLimit + PadGradeComparisonTolerance;
    internal const double PadGradeSquaredComparisonLimit = PadGradeComparisonLimit * PadGradeComparisonLimit;

    internal static bool PadSquaredGradeExceedsComparison(double squaredGrade) =>
        !double.IsFinite(squaredGrade) || squaredGrade > PadGradeSquaredComparisonLimit;

    private static void ValidateProgram(Builder b, TerrainProgram program)
    {
        ValidatePadOverlaps(b);
        ValidateFinalPads(b, program);
        ValidateShoreSamples(b, program);
        var minimum = 0d; var maximum = 0d; var maximumGrade = 0d; long count = 0;
        var rect = b.Context.DataRect;
        foreach (var tile in program.AffectedTiles())
        {
            var startX = Math.Max((double)tile.X * 64, Math.Ceiling(rect.Min.X));
            var startZ = Math.Max((double)tile.Z * 64, Math.Ceiling(rect.Min.Y));
            var endX = Math.Min(((double)tile.X + 1) * 64 - 1, Math.Floor(rect.Max.X));
            var endZ = Math.Min(((double)tile.Z + 1) * 64 - 1, Math.Floor(rect.Max.Y));
            for (var z = startZ; z <= endZ; z++)
                for (var x = startX; x <= endX; x++)
                {
                    var sample = program.Evaluate(x, z); var h = sample.Surface.Height;
                    var grade = Math.Sqrt(sample.Surface.Dx * sample.Surface.Dx + sample.Surface.Dz * sample.Surface.Dz);
                    if (h < TerrainSampling.MinimumHeight || h > TerrainSampling.MaximumHeight)
                        throw new WorldDataException("overlay/terrain", "height", $"Unclamped height {h:R} at ({x}, {z}) is outside [-5,10].");
                    if (grade > 1.000001 || grade > 0.500001 && sample.Class != TerrainClass.Transition)
                        throw new WorldDataException("overlay/terrain", "grade", $"Final composed grade {grade:R} at ({x}, {z}) exceeds its terrain class limit. {program.Explain(x, z)}");
                    var protectedBand = sample.BoundaryWeight?.Height == 0;
                    var channel = b.Water.Any(w => w.Channel && !w.Underground && w.Region.Contains(x, z));
                    if (protectedBand && Math.Abs(h) > 0.000001 && !channel)
                        throw new WorldDataException("overlay/terrain", "zeroBand", $"Height {h:R} at ({x}, {z}) changes the protected campus zero band/outside area. {program.Explain(x, z)}");
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
            var x0 = Math.Max(Math.Ceiling(node.Support.Min.X), Math.Ceiling(rect.Min.X));
            var z0 = Math.Max(Math.Ceiling(node.Support.Min.Y), Math.Ceiling(rect.Min.Y));
            var x1 = Math.Min(Math.Floor(node.Support.Max.X), Math.Floor(rect.Max.X));
            var z1 = Math.Min(Math.Floor(node.Support.Max.Y), Math.Floor(rect.Max.Y));
            for (var z = z0; z <= z1; z++)
                for (var x = x0; x <= x1; x++)
                {
                    var zeroBand = b.Campus.Distance(x, z).Height >= -20;
                    var outsideZone = zone is not null && !zone.Contains(x, z);
                    if (!zeroBand && !outsideZone) continue;
                    var before = preceding(x, z); var after = node.Apply(before, x, z);
                    if (Math.Abs(before.Surface.Height - after.Surface.Height) <= 0.000001) continue;
                    if (zeroBand && Math.Abs(after.Surface.Height) > 0.000001 && !b.Water.Any(w => w.Channel && !w.Underground && w.Region.Contains(x, z)))
                        throw new WorldDataException(node.File, node.Id, $"Authored operation changes the protected zero band at ({x}, {z}); a later operation cannot hide this violation.");
                    if (outsideZone)
                        throw new WorldDataException(node.File, node.Id, $"Terrain influence (including its slope/fillet) leaves authoritative zone '{node.Zone.Value}' at ({x}, {z}).");
                }
        }
    }

    private static void ValidateShoreSamples(Builder b, TerrainProgram program)
    {
        foreach (var water in b.WaterShores)
        {
            foreach (var polygon in water.Region.Polygons)
                foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                    for (var i = 0; i < ring.Count; i++)
                    {
                        var a = ring[i]; var c = ring[(i + 1) % ring.Count];
                        var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
                        var steps = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dz * dz)));
                        for (var j = 0; j <= steps; j++)
                        {
                            var x = a.X + dx * j / steps; var z = a.Y + dz * j / steps;
                            var h = program.Evaluate(x, z).Surface.Height;
                            var expected = water.Node.Apply(b.LandBeforeWater.Evaluate(x, z), x, z).Surface.Height;
                            if (water.CheckLevel(x, z) && Math.Abs(h - expected) > 0.02)
                                throw new WorldDataException(water.File, water.Id,
                                    $"Shoreline at ({x:R}, {z:R}) differs from its fixed bank profile by {h - expected:R} m after all constraints (expected={expected:R}). {program.Explain(x, z)}");
                            ValidateWaterApproach(b, program, x, z);
                        }
                    }
        }
    }

    private static void ValidateWaterApproach(Builder b, TerrainProgram program, double x, double z)
    {
        foreach (var node in program.CandidatesAt(x, z))
        {
            if (!b.AutomaticRoads.TryGetValue(node.Id, out var entry)) continue;
            var core = CorridorContactAt(entry.Road, 0, x, z);
            if (core.Distance.Height <= 0 || entry.Profile.Excluded(entry.Road.Path.Project(core.X, core.Z).Station)) continue;
            // A gap narrower than the 1 m terrain lattice still has to be feasible. Probe its
            // actual shortest shoreline-to-core section, including the Hermite midpoint, rather
            // than relying on a lucky global raster sample landing inside the gap.
            for (var part = 1; part < 4; part++)
            {
                var px = x + (core.X - x) * part / 4; var pz = z + (core.Z - z) * part / 4;
                var sample = program.Evaluate(px, pz);
                var grade = Math.Sqrt(sample.Surface.Dx * sample.Surface.Dx + sample.Surface.Dz * sample.Surface.Dz);
                if (grade > 1.000001 || grade > 0.500001 && sample.Class != TerrainClass.Transition)
                    throw new WorldDataException(entry.File, node.Id, $"Narrow road-water gap exceeds its terrain grade at ({px:R},{pz:R}): {grade:R}, surface={sample.Surface}, class={sample.Class}. {program.Explain(px, pz)}") { CanNarrowAutomaticRoad = true };
            }
        }
    }

    private static void ValidateFinalPads(Builder b, TerrainProgram program)
    {
        long rayCount = 0, totalRayCandidates = 0, totalRayEvents = 0;
        var maximumRayCandidates = 0; var maximumRayEvents = 0;
        var largestRay = "";
        foreach (var pad in b.PadChecks.OrderBy(pad => pad.Feature.Value, StringComparer.Ordinal))
        {
            TerrainRegion exposed;
            try { exposed = TerrainUnion.Build([pad.Region]); }
            catch (ArgumentException error)
            {
                throw new WorldDataException(pad.File, pad.Id,
                    $"Pad '{pad.Feature.Value}' has no valid exposed union boundary; its multipart footprint has inconsistent or non-manifold topology.", error);
            }
            var reach = pad.Apron + pad.GradeWidth;
            foreach (var polygon in exposed.Polygons)
                foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                    for (var i = 0; i < ring.Count; i++)
                    {
                        var a = ring[i]; var c = ring[(i + 1) % ring.Count];
                        var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
                        var edgeNormal = EdgeNormal(i);
                        var before = EdgeNormal((i + ring.Count - 1) % ring.Count);
                        var after = EdgeNormal((i + 1) % ring.Count);
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
                            var distance = exposed.Distance(x, z);
                            // A multipart footprint may cover another part's ring. Only the
                            // boundary of the actual filled region is a building/ground seam.
                            if (Math.Abs(distance.Height) > 0.000001) continue;
                            var ground = program.Evaluate(x, z).Surface.Height;
                            var difference = Math.Abs(ground - pad.Y);
                            if ((difference > 1.2 || difference / PadGradeLimit > pad.GradeWidth) && !Covered(x, z))
                                throw new WorldDataException(pad.File, pad.Id,
                                    $"Pad needs an actual edge/terrace structure after all corridors: feature='{pad.Feature.Value}', boundary=({x:R},{z:R}), padY={pad.Y:R}, groundY={ground:R}, ground difference={difference:R} m, available grade width={pad.GradeWidth:R} m. Contributors: {program.Explain(x, z)}");
                            var t = j / (double)steps;
                            ProbeSeam(x, z, startOffset.X + t * (endOffset.X - startOffset.X), startOffset.Z + t * (endOffset.Z - startOffset.Z));
                        }
                        // Convex corners have a curved offset boundary between their two edge
                        // normals. Sample that arc too; testing only edge offsets misses it.
                        if (before.X * edgeNormal.Z - before.Z * edgeNormal.X > 0)
                            ProbeCorner(a.X, a.Y, before.X, before.Z, edgeNormal.X, edgeNormal.Z);

                        (double X, double Z) EdgeNormal(int edge)
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
                    }
            (double X, double Z) OffsetCorner((double X, double Z) a, (double X, double Z) c, (double X, double Z) edge)
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
            void ProbeSeam(double x, double z, double nx, double nz)
            {
                var normalLength = Math.Sqrt(nx * nx + nz * nz);
                if (normalLength == 0 || !double.IsFinite(normalLength) || reach * normalLength > int.MaxValue / 2)
                    throw new WorldDataException(pad.File, pad.Id, "Pad offset direction is non-finite or cannot be represented by the validation lattice.");
                PadRayTrace trace;
                try { trace = TracePadRayBoundary(exposed, pad.Node.Support, x, z, nx, nz, reach); }
                catch (ArgumentException error)
                {
                    throw new WorldDataException(pad.File, pad.Id,
                        $"Pad '{pad.Feature.Value}' has an invalid geometric ray: origin=({x:R},{z:R}), direction=({nx:R},{nz:R}).", error);
                }
                rayCount++; totalRayCandidates += trace.Candidates; totalRayEvents += trace.Events;
                maximumRayCandidates = Math.Max(maximumRayCandidates, trace.Candidates);
                if (trace.Events > maximumRayEvents)
                {
                    maximumRayEvents = trace.Events;
                    largestRay = $"feature='{pad.Feature.Value}', source='{pad.File}'/'{pad.Id}', origin=({x:R},{z:R}), direction=({nx:R},{nz:R})";
                }
                var limit = trace.End;
                if (exposed.Distance(x + nx * limit, z + nz * limit).Height < pad.Apron)
                {
                    Fail("has no real apron/ground section before another footprint edge", limit, Sample(limit));
                }
                var apronLow = 0d; var apronParameter = limit;
                while ((apronParameter - apronLow) * normalLength > 0.000001)
                {
                    var mid = (apronLow + apronParameter) / 2;
                    if (exposed.Distance(x + nx * mid, z + nz * mid).Height < pad.Apron) apronLow = mid;
                    else apronParameter = mid;
                }
                var available = limit - apronParameter;
                var sectionLength = available * normalLength;
                if (!double.IsFinite(sectionLength) || sectionLength > int.MaxValue / 2)
                    throw new WorldDataException(pad.File, pad.Id, "Pad offset section cannot be represented by the validation lattice.");
                var count = Math.Max(1, (int)Math.Ceiling(sectionLength));
                var samples = new List<(double D, TerrainDifferential Actual, TerrainDifferential Without)>();
                // Midpoints supplement the established 1 m validation lattice, especially
                // where a corridor enters or exits the final grading strip between vertices.
                for (var i = 0; i <= 2 * count; i++)
                {
                    var d = apronParameter + available * i / (2 * count);
                    samples.Add(Sample(d));
                }
                // Probe the actual inside limit as well as the boundary. A floating-point
                // endpoint just outside a finite pad must not disguise an unfinished cut.
                if (available > 0.000001) samples.Insert(samples.Count - 1, Sample(limit - 0.000001));
                var lastChanged = samples.FindLastIndex(sample => !Same(sample.Actual, sample.Without));
                if (lastChanged == samples.Count - 1)
                {
                    var end = samples[^1];
                    // Even a real wall cannot authorize a hard-clipped pad envelope.
                    Fail("does not reach the same final ground without this pad within its available grade width", end.D, end);
                }
                if (lastChanged < 0) return; // The final corridors already replace this entire section.
                var changed = samples[lastChanged]; var returned = samples[lastChanged + 1];
                // Both final evaluations use the identical solved road/water program. Only
                // this exact pad node is omitted; no new profiles or defaults are constructed.
                while (returned.D - changed.D > 0.000001)
                {
                    var mid = Sample((changed.D + returned.D) / 2);
                    if (Same(mid.Actual, mid.Without)) returned = mid; else changed = mid;
                }
                samples.RemoveRange(lastChanged + 1, samples.Count - lastChanged - 1);
                samples.Add(changed); samples.Add(returned);
                var apronHeight = samples[0].Actual.Height;
                for (var i = 0; i < samples.Count; i++)
                {
                    var sample = samples[i]; var width = (sample.D - apronParameter) * normalLength;
                    var difference = Math.Abs(sample.Actual.Height - apronHeight);
                    if ((difference > 1.2 || difference > PadGradeLimit * width + 0.000001) && !CoveredHeightSection(i))
                        Fail("exceeds its 1.2 m height / 1:4 available-width limit after all corridors", sample.D, sample);
                    var sdf = exposed.Distance(x + nx * sample.D, z + nz * sample.D).Height;
                    if (sdf >= pad.Apron && sdf <= reach && !Same(sample.Actual, sample.Without))
                    {
                        var squaredGrade = sample.Actual.Dx * sample.Actual.Dx + sample.Actual.Dz * sample.Actual.Dz;
                        if (!double.IsFinite(squaredGrade)) Fail("has a non-finite two-dimensional final grade", sample.D, sample);
                        if (PadSquaredGradeExceedsComparison(squaredGrade) && !Covered(x + nx * sample.D, z + nz * sample.D))
                            Fail($"exceeds its 1:4 two-dimensional final grade (squaredGrade={squaredGrade:R})", sample.D, sample);
                    }
                    var grade = Math.Abs(sample.Actual.Dx * nx + sample.Actual.Dz * nz) / normalLength;
                    if (grade > PadGradeComparisonLimit && !Covered(x + nx * sample.D, z + nz * sample.D))
                        Fail($"exceeds its 1:4 final section grade ({grade:R})", sample.D, sample);
                    if (i == 0) continue;
                    var previous = samples[i - 1]; var interval = (sample.D - previous.D) * normalLength;
                    if (interval == 0) continue;
                    var secant = Math.Abs(sample.Actual.Height - previous.Actual.Height) / interval;
                    if (secant > PadGradeComparisonLimit && !(Covered(x + nx * previous.D, z + nz * previous.D)
                        && Covered(x + nx * sample.D, z + nz * sample.D)
                        && Covered(x + nx * (previous.D + sample.D) / 2, z + nz * (previous.D + sample.D) / 2)))
                        Fail($"has a final grading/end connection jump or section steeper than 1:4 ({secant:R})", sample.D, sample);
                }

                bool CoveredHeightSection(int endIndex)
                {
                    var changedSection = false;
                    for (var j = 1; j <= endIndex; j++)
                    {
                        var a = samples[j - 1]; var c = samples[j];
                        if (Math.Abs(c.Actual.Height - a.Actual.Height) <= 0.000001) continue;
                        changedSection = true;
                        if (!(Covered(x + nx * a.D, z + nz * a.D) && Covered(x + nx * c.D, z + nz * c.D)
                            && Covered(x + nx * (a.D + c.D) / 2, z + nz * (a.D + c.D) / 2))) return false;
                    }
                    return changedSection;
                }
                (double D, TerrainDifferential Actual, TerrainDifferential Without) Sample(double d)
                {
                    var sx = x + nx * d; var sz = z + nz * d;
                    return (d, program.Evaluate(sx, sz).Surface, program.EvaluateExcept(sx, sz, pad.Node).Surface);
                }
                void Fail(string reason, double d, (double D, TerrainDifferential Actual, TerrainDifferential Without) sample)
                {
                    var sx = x + nx * d; var sz = z + nz * d;
                    throw new WorldDataException(pad.File, pad.Id,
                        $"Pad final grading after all corridors {reason}: feature='{pad.Feature.Value}', sectionOrigin=({x:R},{z:R}), rayDirection=({nx:R},{nz:R}), point=({sx:R},{sz:R}), rayParameter={d:R}, distanceBeyondApron={exposed.Distance(sx, sz).Height - pad.Apron:R}, declaredGradeWidth={pad.GradeWidth:R}, rawSquaredGrade={sample.Actual.Dx * sample.Actual.Dx + sample.Actual.Dz * sample.Actual.Dz:R}, mathematicalGradeLimit={PadGradeLimit:R}, gradeComparisonTolerance={PadGradeComparisonTolerance:R}, rayCandidates={trace.Candidates}, rayEvents={trace.Events}, actual={sample.Actual}, sameProgramWithoutPad={sample.Without}. Contributors: {program.Explain(sx, sz)}");
                }
            }

            bool Covered(double x, double z) => pad.Structure is { } structure
                && TerrainReferences.NearStructure(structure, new((float)x, (float)z), 0);
            static bool Same(TerrainDifferential actual, TerrainDifferential without) => Math.Abs(actual.Height - without.Height) <= 0.000001
                && Math.Abs(actual.Dx - without.Dx) <= 0.000001 && Math.Abs(actual.Dz - without.Dz) <= 0.000001;

            void ProbeCorner(double x, double z, double ax, double az, double cx, double cz)
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
        }
        b.Diagnostics.Add($"Final pad geometric rays: rays={rayCount}, totalCandidates={totalRayCandidates}, totalEvents={totalRayEvents}, maxCandidatesPerRay={maximumRayCandidates}, maxEventsPerRay={maximumRayEvents}; largestEventRay={largestRay}.");
    }

    internal readonly record struct PadRayTrace(double End, int Candidates, int Events);
    private readonly record struct PadRayPiece(double Start, double End, double A, double B, double C);

    internal static PadRayTrace TracePadRayBoundary(TerrainRegion region, MapRect support,
        double x, double z, double nx, double nz, double cap)
    {
        var end = double.PositiveInfinity;
        if (nx > 0) end = Math.Min(end, (support.Max.X - x) / nx);
        if (nx < 0) end = Math.Min(end, (support.Min.X - x) / nx);
        if (nz > 0) end = Math.Min(end, (support.Max.Y - z) / nz);
        if (nz < 0) end = Math.Min(end, (support.Min.Y - z) / nz);
        if (!double.IsFinite(end) || end <= 0) throw new ArgumentException("Pad ray has no finite outward support interval.");
        var pieces = new List<PadRayPiece>(); var events = new SortedSet<double> { 0, end };
        var ex = x + nx * end; var ez = z + nz * end;
        foreach (var polygon in region.Polygons)
            foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                for (var i = 0; i < ring.Count; i++)
                {
                    var a = ring[i]; var c = ring[(i + 1) % ring.Count];
                    if (Math.Max(a.X, c.X) < Math.Min(x, ex) - cap || Math.Min(a.X, c.X) > Math.Max(x, ex) + cap
                        || Math.Max(a.Y, c.Y) < Math.Min(z, ez) - cap || Math.Min(a.Y, c.Y) > Math.Max(z, ez) + cap) continue;
                    // All coefficients use local source-edge/ray differences, never squared
                    // world coordinates. Projection changes partition each segment into at most
                    // three valid squared-distance quadratics (endpoint / line / endpoint).
                    var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
                    var ax = x - a.X; var az = z - a.Y; var length2 = dx * dx + dz * dz;
                    var u0 = (ax * dx + az * dz) / length2; var ud = (nx * dx + nz * dz) / length2;
                    var cuts = new SortedSet<double> { 0, end };
                    if (ud != 0)
                    {
                        var first = -u0 / ud; var last = (1 - u0) / ud;
                        if (first > 0 && first < end) cuts.Add(first);
                        if (last > 0 && last < end) cuts.Add(last);
                    }
                    var bounds = cuts.ToArray();
                    for (var j = 0; j + 1 < bounds.Length; j++)
                    {
                        var low = bounds[j]; var high = bounds[j + 1]; var mid = low + (high - low) / 2;
                        if (mid == low || mid == high) continue;
                        var u = u0 + ud * mid;
                        if (u <= 0) AddPiece(new(low, high, nx * nx + nz * nz, 2 * (ax * nx + az * nz), ax * ax + az * az));
                        else if (u >= 1)
                        {
                            var cx = x - c.X; var cz = z - c.Y;
                            AddPiece(new(low, high, nx * nx + nz * nz, 2 * (cx * nx + cz * nz), cx * cx + cz * cz));
                        }
                        else
                        {
                            var cross0 = dx * az - dz * ax; var crossD = dx * nz - dz * nx;
                            AddPiece(new(low, high, crossD * crossD / length2, 2 * cross0 * crossD / length2, cross0 * cross0 / length2));
                        }
                    }
                }
        for (var i = 0; i < pieces.Count; i++)
            for (var j = i + 1; j < pieces.Count; j++)
            {
                var a = pieces[i]; var c = pieces[j]; var low = Math.Max(a.Start, c.Start); var high = Math.Min(a.End, c.End);
                if (high > low) AddRoots(a.A - c.A, a.B - c.B, a.C - c.C, low, high);
            }
        var ordered = events.ToArray(); var exterior = false;
        for (var i = 0; i + 1 < ordered.Length; i++)
        {
            var low = ordered[i]; var high = ordered[i + 1]; var mid = low + (high - low) / 2;
            if (mid == low || mid == high) continue;
            var lx = x + nx * low; var lz = z + nz * low;
            var mx = x + nx * mid; var mz = z + nz * mid;
            var hx = x + nx * high; var hz = z + nz * high;
            // Distinct parameters may still name the same representable world point.
            // Such an interval cannot supply an independent containment-side sample.
            if (mx == lx && mz == lz || mx == hx && mz == hz) continue;
            var nearest = double.PositiveInfinity; var slope = 0d;
            // Equal candidates retain the first source edge / projection-piece order.
            foreach (var piece in pieces)
            {
                if (mid < piece.Start || mid > piece.End) continue;
                var squared = (piece.A * mid + piece.B) * mid + piece.C;
                if (squared >= nearest) continue;
                nearest = squared; slope = 2 * piece.A * mid + piece.B;
            }
            if (!exterior)
            {
                var real = region.Distance(mx, mz).Height;
                if (real > 0) exterior = true;
                else if (slope > 0) return new(low, pieces.Count, ordered.Length);
                else continue; // An interpolated source point may start a few ULPs inside.
            }
            if (nearest >= cap * cap) return new(low, pieces.Count, ordered.Length);
            if (slope < 0) return new(low, pieces.Count, ordered.Length);
            // Positive -> positive nearest-edge changes and exact plateaus continue.
            // The first interval turning back towards another wall ends the outward strip.
        }
        return new(end, pieces.Count, ordered.Length);

        void AddPiece(PadRayPiece piece)
        {
            if (!double.IsFinite(piece.A) || !double.IsFinite(piece.B) || !double.IsFinite(piece.C))
                throw new ArgumentException("Pad ray has a non-finite local distance quadratic.");
            var minimum = Math.Min(Value(piece, piece.Start), Value(piece, piece.End));
            if (piece.A > 0)
            {
                var vertex = -piece.B / (2 * piece.A);
                if (vertex > piece.Start && vertex < piece.End) minimum = Math.Min(minimum, Value(piece, vertex));
            }
            if (minimum > cap * cap) return; // Cannot be nearest before the fixed SDF cap.
            pieces.Add(piece); events.Add(piece.Start); events.Add(piece.End);
            if (piece.A > 0)
            {
                var vertex = -piece.B / (2 * piece.A);
                if (vertex > piece.Start && vertex < piece.End) events.Add(vertex);
            }
            AddRoots(piece.A, piece.B, piece.C - cap * cap, piece.Start, piece.End);
        }
        static double Value(PadRayPiece piece, double parameter) => (piece.A * parameter + piece.B) * parameter + piece.C;
        void AddRoots(double a, double b, double c, double low, double high)
        {
            if (a == 0) { if (b != 0) Add(-c / b); return; }
            var discriminant = b * b - 4 * a * c;
            if (discriminant < 0) return;
            var root = Math.Sqrt(discriminant);
            var q = -0.5 * (b + (b < 0 ? -root : root));
            if (q == 0) Add(-b / (2 * a));
            else { Add(q / a); Add(c / q); }
            void Add(double value) { if (double.IsFinite(value) && value >= low && value <= high) events.Add(value); }
        }
    }

    private static void ValidatePadOverlaps(Builder b)
    {
        var pads = b.PadRegions.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal).ToArray();
        for (var i = 0; i < pads.Length; i++)
            for (var j = i + 1; j < pads.Length; j++)
            {
                if (Math.Abs(b.Pads[pads[i].Key] - b.Pads[pads[j].Key]) <= 0.15) continue;
                var a = pads[i].Value; var c = pads[j].Value;
                if (a.Bounds.Max.X <= c.Bounds.Min.X || c.Bounds.Max.X <= a.Bounds.Min.X || a.Bounds.Max.Y <= c.Bounds.Min.Y || c.Bounds.Max.Y <= a.Bounds.Min.Y) continue;
                var overlap = a.InteriorOverlaps(c);
                if (overlap) throw new WorldDataException("overlay/terrain", "pad", $"Overlapping pads '{pads[i].Key.Value}' and '{pads[j].Key.Value}' differ by more than 0.15 m.");
            }
    }
}
