using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Zones.Common;

namespace Campus.Gen.Zones.North;

/// <summary>Only building-kind parts close cluster fences; other kinds keep their own footprints.</summary>
internal static class NorthBuildings
{
    internal static IEnumerable<IReadOnlyList<Vector2>> Rings(WorldFeature feature)
    {
        if (feature.Parts.Count > 0)
        {
            var parts = feature.Parts.Where(part => (WorldProps.TryText(part.Props, "kind") ?? feature.Kind.Value) == "building").ToArray();
            if (parts.Length == 0) yield break;
            feature = feature with { Parts = parts };
        }
        else if (feature.Kind.Value != "building") yield break;
        foreach (var ring in feature.Outers())
            yield return PlanGeometry.DeduplicateRing(ring, 0.05f * 0.05f);
    }
}
