using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;

namespace Campus.Systems.Vegetation;

/// <summary>Workers consume the same prepared map version as the formal bake service.</summary>
internal static class VegetationContext
{
    public static Func<BuildContext> Resolve(IMapContext map, string moduleId, IBakeContextSource source)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(source);
        var prepared = source.GetPreparedContext(map, moduleId);
        return () => prepared;
    }
}
