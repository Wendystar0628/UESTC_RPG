using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Circulation;

/// <summary>Clear dimensions and registered materials of an open-ended glazed barrel canopy.</summary>
public sealed record ArchedCanopyParams(float Span, float StraightHeight, float Rise, float Length,
    float RibPitch, ToonSpec Glass, ToonSpec Frame)
{
    public float GlassThickness { get; init; } = CirculationStyle.FaceInset;
    public float RibWidth { get; init; } = CirculationStyle.PostSize;
    public float Grade { get; init; }
    public int Segments { get; init; } = 16;
}

/// <summary>Glazed arch shell, vertical sides and capped steel ribs; the same shell also serves a long shopping arcade.</summary>
public static class ArchedCanopy
{
    public static PartGeometry Build(ArchedCanopyParams p, LodLevel lod)
    {
        if (!(p.Length > 0 && p.RibPitch > 0 && p.StraightHeight >= 0)) throw new ArgumentOutOfRangeException(nameof(p));
        var near = lod is LodLevel.L0 or LodLevel.L1;
        var samples = ArcSection.Sample(p.Span, p.Rise, p.Segments);
        var b = new PartBuilder();
        var glassProfile = Strip(samples, p.StraightHeight, p.GlassThickness);
        b.Add(PartSolids.WallPanel(new(glassProfile), p.Length, p.Glass));
        if (p.StraightHeight > 0)
            foreach (var side in new[] { -1f, 1f })
                b.Add(PartSolids.BeveledBox(p.GlassThickness, p.StraightHeight, p.Length, 0, p.Glass),
                    Xf.Move(side * p.Span * .5f, 0, 0));
        var count = Math.Max(1, (int)MathF.Ceiling(p.Length / p.RibPitch));
        var ribProfile = Strip(samples, p.StraightHeight, p.RibWidth);
        for (var k = 0; k <= count; k++)
        {
            if (!near && k != 0 && k != count && k % 4 != 0) continue;
            var z = -p.Length * .5f + p.Length * k / count;
            b.Add(PartSolids.WallPanel(new(ribProfile), p.RibWidth, p.Frame), Xf.Move(0, 0, z));
            if (p.StraightHeight > 0)
                foreach (var side in new[] { -1f, 1f })
                    b.Add(PartSolids.BeveledBox(p.RibWidth, p.StraightHeight, p.RibWidth,
                            near ? CirculationStyle.PostBevel : 0, p.Frame), Xf.Move(side * p.Span * .5f, 0, z));
        }
        foreach (var side in new[] { -1f, 1f })
            b.Add(PartSolids.BeveledBox(p.RibWidth, p.RibWidth, p.Length,
                    near ? CirculationStyle.RailBevel : 0, p.Frame), Xf.Move(side * p.Span * .5f, p.StraightHeight, 0));
        var measure = b.Measure();
        var geometry = b.Finish(new(measure.Min, measure.Max));
        return p.Grade == 0 ? geometry : Incline(geometry, p.Grade);
    }

    private static Vector2[] Strip(IReadOnlyList<Vector2> samples, float baseY, float thickness) =>
        [.. samples.Select(v => v + new Vector2(0, baseY)),
            .. samples.Reverse().Select(v => v + new Vector2(0, baseY + thickness))];

    /// <summary>Vertical rise along z; inverse-transpose normals preserve the glass and roof shading.</summary>
    private static PartGeometry Incline(PartGeometry geometry, float grade) => WithGrade(geometry, new(0, grade));

    /// <summary>Applies a vertical plane to a solid, with inverse-transpose normals; used by joined shelter roof strips.</summary>
    public static PartGeometry WithGrade(PartGeometry geometry, Vector2 gradient, float offset = 0)
    {
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        var meshes = geometry.Meshes.Select(mesh =>
        {
            var positions = mesh.Position.ToArray(); var normals = mesh.Normal.ToArray();
            for (var i = 0; i < positions.Length; i += 3)
            {
                positions[i + 1] += gradient.X * positions[i] + gradient.Y * positions[i + 2] + offset;
                var point = new Vector3(positions[i], positions[i + 1], positions[i + 2]);
                min = Vector3.Min(min, point); max = Vector3.Max(max, point);
                var normal = Vector3.Normalize(new(normals[i] - gradient.X * normals[i + 1], normals[i + 1],
                    normals[i + 2] - gradient.Y * normals[i + 1]));
                normals[i] = normal.X; normals[i + 1] = normal.Y; normals[i + 2] = normal.Z;
            }
            return mesh with { Position = positions, Normal = normals };
        }).ToArray();
        return new(meshes, new(min, max));
    }
}
