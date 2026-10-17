namespace Campus.Contracts;

/// <summary>One complete generated source in world coordinates, assembled on demand from its production geometry and colliders.</summary>
/// <param name="SourceId">Stable and unique within the module; auxiliary structures have their own source IDs.</param>
/// <param name="Feature">The actual owning world feature, or null for procedural sources without a world feature.</param>
/// <param name="Output">Only this source's actual visible output, including placed parts and instances.</param>
/// <param name="Colliders">Only this source's actual collision primitives.</param>
/// <param name="OccupiedContours">Directed material contours: each outer minus its own holes, then unioned with the other contours and the output's XZ projection.</param>
public sealed record ContentSourceTrace(string SourceId, FeatureId? Feature, ContentOutput Output,
    IReadOnlyList<Collider> Colliders, IReadOnlyList<PolygonRings> OccupiedContours);

/// <summary>Enumerates complete sources across the map without splitting their geometry at tile boundaries.</summary>
public interface IContentSourceTraceProvider
{
    IEnumerable<ContentSourceTrace> TraceSources(BuildContext context, LodLevel level);
}
