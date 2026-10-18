using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Parts.Roof;

/// <summary>Shared LOD table and bounds helpers.</summary>
internal static class Levels
{
    /// <summary>One entry per listed level; levels left out are not drawn (far levels may be omitted, ART-MODELING).</summary>
    public static IReadOnlyDictionary<LodLevel, Func<T, PartGeometry>> Of<T>(Func<T, LodLevel, PartGeometry> build, params LodLevel[] levels)
    {
        var table = new SortedDictionary<LodLevel, Func<T, PartGeometry>>();
        foreach (var level in levels) table.Add(level, p => build(p, level));
        return new ReadOnlyDictionary<LodLevel, Func<T, PartGeometry>>(table);
    }

    public static Box3 Padded(Vector3 min, Vector3 max)
    {
        var pad = RoofStyle.BoundsPad;
        return new(new(min.X - pad, min.Y - pad, min.Z - pad), new(max.X + pad, max.Y + pad, max.Z + pad));
    }

    public static void Positive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name, "A dimension must be finite and positive.");
    }
}
