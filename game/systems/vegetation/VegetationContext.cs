using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;

namespace Campus.Systems.Vegetation;

/// <summary>The resident species table consumes the formal prepared map context.</summary>
internal static class VegetationContext
{
    public static BuildContext Resolve(IMapContext map, string moduleId, IBakeContextSource source)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(source);
        return source.GetPreparedContext(map, moduleId);
    }
}
