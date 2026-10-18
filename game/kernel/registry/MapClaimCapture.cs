using Campus.Contracts;
using Campus.Pipeline.Map;

namespace Campus.Kernel.Registry;

/// <summary>Capture declarations on the composition thread; resolution remains pure.</summary>
public static class MapClaimCapture
{
    public static MapClaimSnapshot Capture(ServiceRegistry registry, MapId map) => new(registry.ForMap(map.Value)
        .Where(entry => entry.Kind == "content").Select(entry => new MapClaimSource(entry.Id, new WpId(entry.Owner),
            entry.Map is null, ((IContentModule)registry.Create(entry.Type)).Claims)));
}
