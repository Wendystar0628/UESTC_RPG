using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using static Campus.Gen.World.TerrainFieldGrading;

namespace Campus.Gen.World;

internal static class TerrainLandOperations
{
    internal static void Platform(TerrainBuildState b, TerrainPlatformOp op, string file)
    {
        var region = new TerrainRegion([op.Rings]); var fill = TerrainReferences.Grade(op.Side.Fill); var cut = TerrainReferences.Grade(op.Side.Cut);
        b.Nodes.Add(new(op.Id, file, op.Zone, TerrainGroup.Regional, op.Priority, ConstraintBounds(b, region.Bounds, fill, cut, op.Fillet),
            (previous, x, z) =>
            {
                var distance = region.Distance(x, z); var target = TerrainDifferential.Constant(op.Y);
                if (distance.Height <= 0) RequireZeroBandCore(b, target, x, z, file, op.Id);
                return Constrain(previous, target, distance, fill, cut, op.Fillet);
            }));
    }

    internal static void Mound(TerrainBuildState b, TerrainShape shape, double amplitude, TerrainOp op, string file)
    {
        var (center, radii, yaw) = shape switch
        {
            TerrainCircle circle => (circle.Center, new Vector2((float)circle.Radius), 0d),
            TerrainEllipse ellipse => (ellipse.Center, ellipse.Radii, (ellipse.YawDeg - 90) * (Math.PI / 180)),
            _ => throw new WorldDataException(file, op.Id, "Only circle and ellipse mound/basin shapes are supported.")
        };
        if (!VectorMath.Finite(radii) || radii.X <= 0 || radii.Y <= 0)
            throw new WorldDataException(file, op.Id, "Terrain shape radius cannot be represented in world coordinates.");
        if (1.5 * Math.Abs(amplitude) / Math.Min(radii.X, radii.Y) > 0.5)
            throw new WorldDataException(file, op.Id, "Mound/basin profile exceeds the 1:2 soil-grade limit before composition.");
        var c = Math.Cos(yaw); var s = Math.Sin(yaw);
        var ex = Math.Sqrt(radii.X * (double)radii.X * c * c + radii.Y * (double)radii.Y * s * s);
        var ez = Math.Sqrt(radii.X * (double)radii.X * s * s + radii.Y * (double)radii.Y * c * c);
        var bounds = new MapRect(new((float)(center.X - ex), (float)(center.Y - ez)), new((float)(center.X + ex), (float)(center.Y + ez)));
        b.Nodes.Add(new(op.Id, file, op.Zone, TerrainGroup.Regional, op.Priority, bounds, (previous, x, z) =>
        {
            var contribution = TerrainMath.EllipticalMound(x, z, center, radii, yaw, amplitude);
            return WithSurface(previous, previous.Surface + contribution, double.PositiveInfinity, double.PositiveInfinity);
        }));
    }

    internal static void Slope(TerrainBuildState b, TerrainSlopeOp op, string file)
    {
        var fill = TerrainReferences.Grade(op.Side.Fill); var cut = TerrainReferences.Grade(op.Side.Cut);
        switch (op.Geometry)
        {
            case PolygonGeometry polygon:
            {
                var region = new TerrainRegion([polygon.Rings]); var from = op.From!.Value; var to = op.To!.Value;
                var dx = (double)to.X - from.X; var dz = (double)to.Y - from.Y; var length = Math.Sqrt(dx * dx + dz * dz);
                var tx = dx / length; var tz = dz / length;
                b.Nodes.Add(new(op.Id, file, op.Zone, TerrainGroup.Regional, op.Priority, ConstraintBounds(b, region.Bounds, fill, cut, op.Fillet, op.CrossFall), (previous, x, z) =>
                {
                    var along = ((x - from.X) * tx + (z - from.Y) * tz) / length;
                    var t = Math.Max(0, Math.Min(1, along)); var derivative = along > 0 && along < 1 ? (op.Y1 - op.Y0) / length : 0;
                    var offset = -(x - from.X) * tz + (z - from.Y) * tx;
                    var target = new TerrainDifferential(op.Y0 + (op.Y1 - op.Y0) * t + op.CrossFall * offset,
                        derivative * tx - op.CrossFall * tz, derivative * tz + op.CrossFall * tx);
                    var distance = region.Distance(x, z);
                    if (distance.Height <= 0) RequireZeroBandCore(b, target, x, z, file, op.Id);
                    return Constrain(previous, target, distance, fill, cut, op.Fillet);
                }));
                break;
            }
            case PolylineGeometry polyline:
            {
                var path = new TerrainPath(polyline.Line); var half = op.Width!.Value / 2;
                b.Nodes.Add(new(op.Id, file, op.Zone, TerrainGroup.Linear, op.Priority, ConstraintBounds(b, TerrainRegion.Expand(path.Bounds, half), fill, cut, op.Fillet, op.CrossFall), (previous, x, z) =>
                {
                    var hit = path.Project(x, z); var gradient = (op.Y1 - op.Y0) / path.Length;
                    var target = new TerrainDifferential(op.Y0 + gradient * hit.Station + op.CrossFall * hit.Offset,
                        gradient * hit.StationDx + op.CrossFall * hit.OffsetDx, gradient * hit.StationDz + op.CrossFall * hit.OffsetDz);
                    var distance = new TerrainDifferential(hit.Distance - half, hit.DistanceDx, hit.DistanceDz);
                    if (distance.Height <= 0) RequireZeroBandCore(b, target, x, z, file, op.Id);
                    return Constrain(previous, target, distance, fill, cut, op.Fillet);
                }));
                break;
            }
            default: throw new WorldDataException(file, op.Id, "Unsupported slope geometry.");
        }
    }

    internal static void Terrace(TerrainBuildState b, TerrainTerraceOp op, string file)
    {
        var path = new TerrainPath(op.Front);
        foreach (var station in op.StairAt) if (station > path.Length) throw new WorldDataException(file, op.Id, "Terrace stairAt is outside the real front length.");
        var length = Math.Sqrt((double)op.Inward.X * op.Inward.X + (double)op.Inward.Y * op.Inward.Y);
        var nx = op.Inward.X / length; var nz = op.Inward.Y / length;
        var depth = op.Tiers * op.Tread; var rise = op.Tiers * op.Riser;
        if (op.Riser / op.Tread > 0.5 || op.FrontY + rise > TerrainSampling.MaximumHeight)
            throw new WorldDataException(file, op.Id, "Terrace mean slope or upper elevation exceeds the terrain limits.");
        var ring = op.Front.Points.Concat(op.Front.Points.Reverse().Select(p => new Vector2((float)(p.X + nx * depth), (float)(p.Y + nz * depth)))).ToArray();
        var region = new TerrainRegion([new(ring)]);
        b.Nodes.Add(new(op.Id, file, op.Zone, TerrainGroup.Regional, op.Priority, ConstraintBounds(b, region.Bounds, 0.5, 0.5, 2), (previous, x, z) =>
        {
            var hit = path.Project(x, z); var front = path.At(hit.Station);
            var along = (x - front.X) * nx + (z - front.Y) * nz; var t = Math.Max(0, Math.Min(depth, along));
            var slope = along > 0 && along < depth ? rise / depth : 0;
            var a = path.Line.Points[hit.Segment]; var c = path.Line.Points[hit.Segment + 1];
            var segmentX = (double)c.X - a.X; var segmentZ = (double)c.Y - a.Y;
            var segmentLength = Math.Sqrt(segmentX * segmentX + segmentZ * segmentZ);
            var alongTangent = nx * segmentX / segmentLength + nz * segmentZ / segmentLength;
            var target = new TerrainDifferential(op.FrontY + rise * t / depth - 0.10,
                slope * (nx - alongTangent * hit.StationDx), slope * (nz - alongTangent * hit.StationDz));
            var distance = region.Distance(x, z);
            if (distance.Height <= 0) RequireZeroBandCore(b, target, x, z, file, op.Id);
            return Constrain(previous, target, distance, 0.5, 0.5, 2);
        }));
    }
}
