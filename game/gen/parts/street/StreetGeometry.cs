using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Parts.Street;

/// <summary>Shared LOD table, bounds and validation helpers.</summary>
public static class Levels
{
    /// <summary>One entry per listed level; levels left out are not drawn (ART-MODELING: L2 keeps identity pieces only).</summary>
    public static IReadOnlyDictionary<LodLevel, Func<T, PartGeometry>> Of<T>(Func<T, LodLevel, PartGeometry> build, params LodLevel[] levels)
    {
        var table = new SortedDictionary<LodLevel, Func<T, PartGeometry>>();
        foreach (var level in levels) table.Add(level, p => build(p, level));
        return new ReadOnlyDictionary<LodLevel, Func<T, PartGeometry>>(table);
    }

    public static Box3 Padded(Vector3 min, Vector3 max)
    {
        var pad = StreetStyle.BoundsPad;
        return new(new(min.X - pad, min.Y - pad, min.Z - pad), new(max.X + pad, max.Y + pad, max.Z + pad));
    }

    public static void Positive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name, "A dimension must be finite and positive.");
    }

    public static void Dimensions(Campus.Contracts.Parts.BoxPartParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Positive(p.Width, nameof(p.Width)); Positive(p.Height, nameof(p.Height)); Positive(p.Depth, nameof(p.Depth));
    }
}
