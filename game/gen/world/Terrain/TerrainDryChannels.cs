using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Geometry;
using static Campus.Gen.World.TerrainFieldGrading;
using static Campus.Gen.World.TerrainRoadCoreGeometry;

namespace Campus.Gen.World;

internal static class TerrainDryChannels
{
    internal static void DryChannel(TerrainBuildState b, TerrainChannelOp op, string file)
    {
        var path = new TerrainPath(op.Line!); var half = op.Width!.Value / 2;
        var inner = TerrainReferences.Grade(op.Bank.Inner!);
        var outer = op.Bank.Outer is null ? (double?)null : TerrainReferences.Grade(op.Bank.Outer);
        if (inner > 0.5 || outer is > 0.5) throw new WorldDataException(file, op.Id, "Dry swale banks exceed the 1:2 soil-grade limit.");
        var radius = half + op.Depth / inner;
        var (end, terminal, underpassRoads) = ResolveCrossings(b, op, file, path, radius);
        var prefix = ComposeProgram(b, b.Nodes);
        var reach = radius + (outer is { } g ? b.RangeBound / g : 0);
        var support = TerrainRegion.Expand(path.Bounds, reach);
        var recoveryEnd = end;
        b.Nodes.Add(new(op.Id, file, op.Zone, TerrainGroup.Linear, op.Priority, support, (previous, x, z) =>
        {
            // An explicitly resolved underpass is below the actual road core. It must not
            // excavate the surface used as the road's default profile target.
            if (underpassRoads.Any(road => CorridorContactAt(road, 0, x, z).Distance.Height <= 0)) return previous;
            var hit = path.Project(x, z);
            if (hit.Station > recoveryEnd || hit.Distance > reach) return previous;
            var center = path.At(hit.Station); var centerValue = prefix.Evaluate(center.X, center.Y).Surface;
            var baseHeight = new TerrainDifferential(centerValue.Height,
                (centerValue.Dx * hit.StationDx + centerValue.Dz * hit.StationDz) * hit.StationDx,
                (centerValue.Dx * hit.StationDx + centerValue.Dz * hit.StationDz) * hit.StationDz);
            var depth = TerrainDifferential.Constant(op.Depth);
            var beyondBottom = new TerrainDifferential(hit.Distance - half, hit.DistanceDx, hit.DistanceDz);
            TerrainDifferential surface; double crest; double toe;
            if (beyondBottom.Height <= 0) { surface = baseHeight - depth; crest = double.PositiveInfinity; toe = half - hit.Distance; }
            else
            {
                surface = baseHeight - depth + beyondBottom * inner; crest = beyondBottom.Height - depth.Height / inner; toe = -beyondBottom.Height;
                if (surface.Height > baseHeight.Height)
                {
                    if (outer is not { } outerGrade) return previous;
                    var exteriorDistance = beyondBottom - depth * (1 / inner);
                    surface = baseHeight - exteriorDistance * outerGrade;
                    if (surface.Height <= previous.Surface.Height) return previous;
                    crest = exteriorDistance.Height; toe = double.PositiveInfinity;
                }
            }
            if (terminal is { } cap)
            {
                var distance = EndCapDistance(x, z, cap.Center, cap.Tx, cap.Tz, cap.Radius);
                if (distance.Height <= 0) return previous;
                if (distance.Height < TerrainCrossingRules.EndBeforeRecoveryMetres)
                {
                    // Root engineering ruling: 2 m is horizontal distance to the terminating arc.
                    // Intersect the original section with the shallower recovery envelope; do not
                    // differentiate sqrt(radius^2-offset^2), whose flank derivative is unbounded.
                    var envelope = previous.Surface - distance * (op.Depth / TerrainCrossingRules.EndBeforeRecoveryMetres);
                    if (surface.Height < envelope.Height) surface = envelope;
                    if (surface.Height > previous.Surface.Height)
                    {
                        var factor = distance * (1 / TerrainCrossingRules.EndBeforeRecoveryMetres); var difference = surface - previous.Surface;
                        surface = previous.Surface + new TerrainDifferential(difference.Height * factor.Height,
                            difference.Dx * factor.Height + difference.Height * factor.Dx, difference.Dz * factor.Height + difference.Height * factor.Dz);
                    }
                }
            }
            return WithSurface(previous, surface, crest, toe);
        }));
    }

    private static (double End, (Vector2 Center, double Tx, double Tz, double Radius)? Terminal,
        List<TerrainRoad> UnderpassRoads) ResolveCrossings(TerrainBuildState b, TerrainChannelOp op,
        string file, TerrainPath path, double radius)
    {
        var end = path.Length;
        (Vector2 Center, double Tx, double Tz, double Radius)? terminal = null;
        if (op.Crossings.OfType<TerrainEndBeforeCrossing>().Count() > 1)
            throw new WorldDataException(file, op.Id, "A dry channel can have only one terminal endBefore road.");
        var underpasses = new List<(TerrainUnderpassCrossing Input, double Station)>();
        var underpassRoads = new List<TerrainRoad>();
        foreach (var crossing in op.Crossings)
        {
            var road = b.References.Road(crossing.Road, file, op.Id);
            switch (crossing)
            {
                case TerrainUnderpassCrossing underpass:
                {
                    var candidates = path.Intersections(road.Path, PlanGeometry.Tolerance).OrderBy(p => (p.X - underpass.At.X) * (p.X - underpass.At.X)
                        + (p.Z - underpass.At.Y) * (p.Z - underpass.At.Y)).ThenBy(p => p.Station).ToArray();
                    var dx = candidates.Length == 0 ? double.PositiveInfinity : candidates[0].X - underpass.At.X;
                    var dz = candidates.Length == 0 ? double.PositiveInfinity : candidates[0].Z - underpass.At.Y;
                    if (candidates.Length == 0 || Math.Sqrt(dx * dx + dz * dz) > TerrainCrossingRules.IntersectionToleranceMetres)
                        throw new WorldDataException(file, op.Id, $"Crossing '{crossing.Road.Value}' has no real centreline intersection within 1 m of at=({underpass.At.X:R},{underpass.At.Y:R}); nearestActualIntersection={(candidates.Length == 0 ? "none" : $"({candidates[0].X:R},{candidates[0].Z:R})")}, distance={Math.Sqrt(dx * dx + dz * dz):R}.");
                    underpasses.Add((underpass, candidates[0].Station)); underpassRoads.Add(road); break;
                }
                case TerrainEndBeforeCrossing:
                {
                    // The whole circle, hence its forward semicircle, stays 1 m beyond the actual
                    // road edge even at an oblique intersection. The centre is another radius away.
                    var centreStation = FindRoadClearanceEntry(path, road.Path,
                        road.Width / 2 + TerrainCrossingRules.EndBeforeRoadEdgeClearanceMetres + radius, file, op.Id);
                    end = centreStation + radius;
                    if (end > path.Length || end <= TerrainCrossingRules.EndBeforeRecoveryMetres)
                        throw new WorldDataException(file, op.Id, "Source channel has insufficient actual length for its complete semicircular cap and 2 m recovery.");
                    var center = path.At(centreStation); var segment = path.Project(center.X, center.Y).Segment;
                    var a = path.Line.Points[segment]; var c = path.Line.Points[segment + 1];
                    var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y; var length = Math.Sqrt(dx * dx + dz * dz);
                    terminal = (center, dx / length, dz / length, radius);
                    break;
                }
                default: throw new WorldDataException(file, op.Id, "Unknown dry-channel crossing type.");
            }
        }
        if (underpasses.Any(p => p.Station >= end)) throw new WorldDataException(file, op.Id, "An underpass lies beyond the declared end-before termination.");
        return (end, terminal, underpassRoads);
    }

    internal static TerrainDifferential EndCapDistance(double x, double z, Vector2 center, double tx, double tz, double radius)
    {
        var dx = x - center.X; var dz = z - center.Y; var along = dx * tx + dz * tz; var lateral = -dx * tz + dz * tx;
        if (along >= 0)
        {
            var distance = Math.Sqrt(dx * dx + dz * dz);
            return distance == 0 ? TerrainDifferential.Constant(radius) : new(radius - distance, -dx / distance, -dz / distance);
        }
        // Behind the diameter, the closest terminal-arc point is an endpoint of that diameter.
        var across = radius - Math.Abs(lateral); var length = Math.Sqrt(along * along + across * across);
        if (length == 0) return TerrainDifferential.Constant(0);
        var sign = lateral > 0 ? 1d : lateral < 0 ? -1d : 0d;
        return new(length, (along * tx + across * sign * tz) / length,
            (along * tz - across * sign * tx) / length);
    }

    private static double FindRoadClearanceEntry(TerrainPath path, TerrainPath road, double radius, string file, string op)
    {
        if (road.Project(path.Line.Points[0].X, path.Line.Points[0].Y).Distance <= radius)
            throw new WorldDataException(file, op, "End-before channel begins inside the road-clearance band.");
        var candidates = new List<double>(); var station = 0d;
        for (var i = 0; i < path.Line.Points.Count - 1; i++)
        {
            var a = path.Line.Points[i]; var b = path.Line.Points[i + 1];
            var dx = (double)b.X - a.X; var dz = (double)b.Y - a.Y; var length = Math.Sqrt(dx * dx + dz * dz);
            for (var j = 0; j < road.Line.Points.Count - 1; j++)
            {
                var c = road.Line.Points[j]; var d = road.Line.Points[j + 1];
                var rx = (double)d.X - c.X; var rz = (double)d.Y - c.Y; var roadLength = Math.Sqrt(rx * rx + rz * rz);
                var tx = rx / roadLength; var tz = rz / roadLength;
                var offset = -tz * (a.X - c.X) + tx * (a.Y - c.Y); var rate = -tz * dx + tx * dz;
                if (rate != 0)
                    foreach (var side in new[] { -radius, radius })
                    {
                        var t = (side - offset) / rate;
                        var along = tx * (a.X + t * dx - c.X) + tz * (a.Y + t * dz - c.Y);
                        if (t >= 0 && t <= 1 && along >= 0 && along <= roadLength) candidates.Add(station + t * length);
                    }
                foreach (var end in new[] { c, d })
                {
                    var qx = (double)a.X - end.X; var qz = (double)a.Y - end.Y;
                    var aa = dx * dx + dz * dz; var bb = 2 * (qx * dx + qz * dz); var cc = qx * qx + qz * qz - radius * radius;
                    var discriminant = bb * bb - 4 * aa * cc;
                    if (discriminant < 0) continue;
                    var root = Math.Sqrt(discriminant);
                    foreach (var t in new[] { (-bb - root) / (2 * aa), (-bb + root) / (2 * aa) })
                        if (t >= 0 && t <= 1) candidates.Add(station + t * length);
                }
            }
            station += length;
        }
        foreach (var candidate in candidates.OrderBy(value => value))
        {
            var point = path.At(candidate);
            if (Math.Abs(road.Project(point.X, point.Y).Distance - radius) <= 0.001) return candidate;
        }
        throw new WorldDataException(file, op, "End-before channel does not reach the actual named road's clearance boundary; endpoint extension is not inferred.");
    }
}
