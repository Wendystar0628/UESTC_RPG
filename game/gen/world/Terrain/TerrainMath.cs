using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>A scalar field value and its analytical world X/Z derivatives.</summary>
public readonly record struct TerrainDifferential(double Height, double Dx, double Dz)
{
    public static TerrainDifferential Constant(double height) => new(height, 0, 0);
    public static TerrainDifferential operator +(TerrainDifferential a, TerrainDifferential b) => new(a.Height + b.Height, a.Dx + b.Dx, a.Dz + b.Dz);
    public static TerrainDifferential operator -(TerrainDifferential a, TerrainDifferential b) => new(a.Height - b.Height, a.Dx - b.Dx, a.Dz - b.Dz);
    public static TerrainDifferential operator -(TerrainDifferential value) => new(-value.Height, -value.Dx, -value.Dz);
    public static TerrainDifferential operator *(TerrainDifferential value, double scale) => new(value.Height * scale, value.Dx * scale, value.Dz * scale);
}

/// <summary>Format-independent scalar primitives. This class does not choose map layout or operation order.</summary>
public static class TerrainMath
{
    public static double Hermite(double t)
    {
        RequireFinite(t, nameof(t));
        if (t <= 0) return 0;
        if (t >= 1) return 1;
        return t * t * (3 - 2 * t);
    }

    public static TerrainDifferential SmoothMinimum(TerrainDifferential a, TerrainDifferential b, double width)
    {
        RequireFinite(width, nameof(width));
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (width == 0 || Math.Abs(a.Height - b.Height) >= width) return a.Height <= b.Height ? a : b;
        var h = 1 - Math.Abs(a.Height - b.Height) / width;
        var weightA = 0.5 + (b.Height - a.Height) / (2 * width);
        var weightB = 1 - weightA;
        return new(Math.Min(a.Height, b.Height) - width * h * h / 4,
            weightA * a.Dx + weightB * b.Dx, weightA * a.Dz + weightB * b.Dz);
    }

    public static TerrainDifferential SmoothMaximum(TerrainDifferential a, TerrainDifferential b, double width) =>
        -SmoothMinimum(-a, -b, width);

    /// <summary>Rounded exterior distance, d²/(2f) up to f and d-f/2 after f.</summary>
    public static TerrainDifferential FilletDistance(TerrainDifferential distance, double fillet)
    {
        RequireFinite(fillet, nameof(fillet));
        if (fillet <= 0) throw new ArgumentOutOfRangeException(nameof(fillet));
        if (distance.Height <= 0) return TerrainDifferential.Constant(0);
        if (distance.Height >= fillet) return distance - TerrainDifferential.Constant(fillet / 2);
        var scale = distance.Height / fillet;
        return new(distance.Height * distance.Height / (2 * fillet), distance.Dx * scale, distance.Dz * scale);
    }

    /// <summary>Constraint clamp only; placement of the separately defined foot smoothing is a compiler decision.</summary>
    public static TerrainDifferential ClampToEnvelope(TerrainDifferential previous, TerrainDifferential target,
        TerrainDifferential exteriorDistance, double fillGrade, double cutGrade, double fillet)
    {
        RequireFinite(fillGrade, nameof(fillGrade)); RequireFinite(cutGrade, nameof(cutGrade));
        if (fillGrade <= 0 || cutGrade <= 0) throw new ArgumentOutOfRangeException(nameof(fillGrade));
        if (exteriorDistance.Height <= 0) return target;
        var rounded = FilletDistance(exteriorDistance, fillet);
        var lower = target - rounded * fillGrade;
        var upper = target + rounded * cutGrade;
        if (previous.Height < lower.Height) return lower;
        return previous.Height > upper.Height ? upper : previous;
    }

    /// <summary>Signed amplitude: positive mound, negative basin. yawRadians is an explicit mathematical rotation.</summary>
    public static TerrainDifferential EllipticalMound(double x, double z, Vector2 center, Vector2 radii,
        double yawRadians, double amplitude)
    {
        RequireFinite(x, nameof(x)); RequireFinite(z, nameof(z)); RequireFinite(yawRadians, nameof(yawRadians)); RequireFinite(amplitude, nameof(amplitude));
        if (!float.IsFinite(center.X) || !float.IsFinite(center.Y) || !float.IsFinite(radii.X) || !float.IsFinite(radii.Y) || radii.X <= 0 || radii.Y <= 0)
            throw new ArgumentException("Ellipse centre and positive radii must be finite.");
        var c = DetMath.Cos(yawRadians); var s = DetMath.Sin(yawRadians);
        if (!double.IsFinite(c) || !double.IsFinite(s)) throw new ArgumentOutOfRangeException(nameof(yawRadians));
        var dx = x - center.X; var dz = z - center.Y;
        var u = (c * dx + s * dz) / radii.X;
        var v = (-s * dx + c * dz) / radii.Y;
        var radius = Math.Sqrt(u * u + v * v);
        if (radius >= 1) return TerrainDifferential.Constant(0);
        if (radius == 0) return TerrainDifferential.Constant(amplitude);
        var height = amplitude * (1 - Hermite(radius));
        var derivative = -6 * amplitude * (1 - radius);
        return new(height, derivative * (u * c / radii.X - v * s / radii.Y),
            derivative * (u * s / radii.X + v * c / radii.Y));
    }

    public static short QuantizeCentimetres(double height)
    {
        RequireFinite(height, nameof(height));
        if (height < TerrainSampling.MinimumHeight || height > TerrainSampling.MaximumHeight)
            throw new ArgumentOutOfRangeException(nameof(height), "Terrain height is outside the authored range before clamping.");
        return checked((short)Math.Round(height * 100, MidpointRounding.ToEven));
    }

    /// <summary>Tests the millimetre-quantized ring with integer crossings; its boundary belongs to the ring.</summary>
    public static bool PointInRing(double x, double z, IReadOnlyList<Vector2> ring)
    {
        if (ring.Count < 3) throw new ArgumentException("A ring requires at least three vertices.", nameof(ring));
        var px = Millimetres(x); var pz = Millimetres(z); var inside = false;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i]; var b = ring[(i + 1) % ring.Count];
            var ax = Millimetres(a.X); var az = Millimetres(a.Y);
            var bx = Millimetres(b.X); var bz = Millimetres(b.Y);
            var cross = (Int128)(bx - ax) * (pz - az) - (Int128)(bz - az) * (px - ax);
            if (cross == 0 && px >= Math.Min(ax, bx) && px <= Math.Max(ax, bx) && pz >= Math.Min(az, bz) && pz <= Math.Max(az, bz)) return true;
            if ((az > pz) != (bz > pz) && ((cross > 0) == (bz > az))) inside = !inside;
        }
        return inside;
    }

    public static bool Contains(double x, double z, PolygonRings polygon)
    {
        if (!PointInRing(x, z, polygon.Outer)) return false;
        foreach (var hole in polygon.Holes) if (PointInRing(x, z, hole)) return false;
        return true;
    }

    /// <summary>Explicit diagonal parameter until ground mesh and collision agree on the shared convention.</summary>
    public static double TriangleSurface(short h00, short h10, short h01, short h11, double u, double v, bool minToMaxDiagonal)
    {
        RequireFinite(u, nameof(u)); RequireFinite(v, nameof(v));
        if (u < 0 || u > 1 || v < 0 || v > 1) throw new ArgumentOutOfRangeException(nameof(u));
        if (h00 == TerrainSampling.HoleCm || h10 == TerrainSampling.HoleCm || h01 == TerrainSampling.HoleCm || h11 == TerrainSampling.HoleCm)
            throw new ArgumentException("A terrain hole has no surface to drape onto.");
        double centimetres;
        if (minToMaxDiagonal)
            centimetres = u >= v ? h00 + (h10 - h00) * u + (h11 - h10) * v : h00 + (h11 - h01) * u + (h01 - h00) * v;
        else
            centimetres = u + v <= 1 ? h00 + (h10 - h00) * u + (h01 - h00) * v : h11 + (h01 - h11) * (1 - u) + (h10 - h11) * (1 - v);
        return centimetres * 0.01;
    }

    private static long Millimetres(double coordinate)
    {
        RequireFinite(coordinate, nameof(coordinate));
        var value = Math.Round(coordinate * 1000, MidpointRounding.ToEven);
        if (value < long.MinValue / 4d || value > long.MaxValue / 4d) throw new ArgumentOutOfRangeException(nameof(coordinate));
        return checked((long)value);
    }

    internal static void RequireFinite(double value, string name)
    { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(name, "Value must be finite."); }
}
