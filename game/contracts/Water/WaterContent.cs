using System.Numerics;

namespace Campus.Contracts.Water;

/// <summary>Generated water surface data used to sample and select planar reflections.</summary>
public sealed record WaterReflectionSurface(
    FeatureId Id,
    PolygonRings Rings,
    float SurfaceY,
    int ReflectSlot,
    Vector2 Min,
    Vector2 Max,
    float Area);

/// <summary>Optional water metadata capability of a registered content module.</summary>
public interface IWaterContentSource : IContentModule
{
    IReadOnlyList<WaterReflectionSurface> GetWaterBodies(BuildContext context);
}
