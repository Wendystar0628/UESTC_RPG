using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;

namespace Campus.Hud.Pages;

/// <summary>
/// 开始页的世界状态（SYS-TITLE）：最近有效存档的 <see cref="SaveMeta"/>，没有存档取默认图的规则默认数据。
/// 开始页期间没有世界实例，片名块只读这里，不读 World.Instance。
/// </summary>
internal readonly record struct TitleWorld(MapId Map, Season Season, double Clock, WeatherKind Weather, SaveMeta? Meta)
{
    public static TitleWorld Resolve(ISaveService saves, IMapService maps)
    {
        var meta = saves.Continue is { } slot ? saves.Slots.FirstOrDefault(info => info.Slot == slot)?.Meta : null;
        if (meta is not null) return new(meta.Map, meta.Season, meta.Clock, meta.Weather, meta);
        var preset = maps.Maps.First(map => map.Id == maps.Default).World.Default;
        return new(maps.Default, preset.Season, preset.Clock,
            preset.Weather ?? throw new InvalidDataException("The map has no default weather."), null);
    }
}
