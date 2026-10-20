using Campus.Contracts;
using Campus.Gen.BakeInput;
using Campus.Kernel.Registry;

namespace Campus.Kernel.Maps;

/// <summary>Capture declarations on the composition thread; resolution remains pure.</summary>
public static class MapClaimCapture
{
    public static MapClaimSnapshot Capture(ServiceRegistry registry, MapId map) => new(registry.Query<IContentModule>(RegistrationKind.Content, map).Select(entry => new MapClaimSource(entry.Id, entry.Owner,
            entry.Map is null, ((IContentModule)registry.Create(entry.Type, map)).Claims)));
}
