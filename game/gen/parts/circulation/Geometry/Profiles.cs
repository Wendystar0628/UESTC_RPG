using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Circulation.Geometry;

/// <summary>Sweep sections in (lateral, up). Principal faces get flat normals; chamfer bands interpolate between them.</summary>
internal static class Profiles
{
    /// <summary>Rectangle lat0..lat1 × up0..up1 with an optional single chamfer on every corner.</summary>
    internal static SweepProfile Rect(float lat0, float lat1, float up0, float up1, float bevel)
    {
        if (!(lat1 > lat0) || !(up1 > up0)) throw new ArgumentException("A section needs positive extent.");
        var b = bevel > 0 && bevel < Math.Min(lat1 - lat0, up1 - up0) * 0.45f ? bevel : 0;
        if (b == 0)
            return new([new(lat0, up0), new(lat1, up0), new(lat1, up1), new(lat0, up1)]);
        return new(
            [new(lat0 + b, up0), new(lat1 - b, up0), new(lat1, up0 + b), new(lat1, up1 - b),
             new(lat1 - b, up1), new(lat0 + b, up1), new(lat0, up1 - b), new(lat0, up0 + b)],
            [new(0, -1), new(0, -1), new(1, 0), new(1, 0), new(0, 1), new(0, 1), new(-1, 0), new(-1, 0)]);
    }

    /// <summary>Round tube section centred on the path; smooth radial normals (ART-MODELING §7.6).</summary>
    internal static SweepProfile Circle(float radius, int segments)
    {
        if (!(radius > 0) || segments < 6) throw new ArgumentException("A tube needs a positive radius and at least six sides.");
        var points = new Vector2[segments]; var normals = new Vector2[segments];
        for (var i = 0; i < segments; i++)
        {
            var angle = 2 * Math.PI * i / segments;
            var c = (float)DetMath.Cos(angle); var s = (float)DetMath.Sin(angle);
            var length = (float)Math.Sqrt((double)c * c + (double)s * s);
            normals[i] = new(c / length, s / length);
            points[i] = new(radius * c, radius * s);
        }
        return new(points, normals);
    }

    /// <summary>Straight horizontal tube or bar along a kernel path (X/Z plane) at height baseY.</summary>
    internal static PartGeometry Bar(SweepProfile profile, IReadOnlyList<Vector2> xzPath, ToonSpec material, float baseY) =>
        PartGeometryKernel.Sweep(profile, xzPath, material, baseY: baseY);
}
