using Campus.Contracts.Maps;

namespace Campus.Contracts.Content;

/// <summary>Map-filtered content producers captured by the prepared map; callers consume only declared capabilities.</summary>
public interface IMapContentSource
{
    /// <summary>Returns the registered producer for this exact prepared, active map context. Unknown or stale maps fail.</summary>
    T GetModule<T>(IMapContext context, string moduleId) where T : class, IContentModule;
}
