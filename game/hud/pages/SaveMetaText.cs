using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

/// <summary>
/// 存档摘要文字的唯一一份：地点、世界时刻、游玩时长、保存时刻，以及开始页继续行的整行摘要（SYS-TITLE、SYS-SAVE）。
/// 存档页、开始页与加载界面都从这里取，不各自拼写。
/// </summary>
internal static class SaveMetaText
{
    /// <summary>地点名：有地点翻译键用它，查不到时统一退回存档所在图的图名。</summary>
    public static string Place(SaveMeta meta, IMapService maps) =>
        meta.PlaceKey is { } key && Campus.Hud.I18n.Tr.Has(key) ? UiKit.T(key) : Campus.Hud.I18n.Tr.Name(Map(meta.Map, maps).Name);

    /// <summary>"季 · 钟点 · 天气"，片名块、存档卡与加载界面共用。</summary>
    public static string Moment(Season season, double clock, WeatherKind weather) =>
        $"{UiKit.T("hud.season." + TimeCard.SeasonCode(season).ToLowerInvariant())} · {WorldTime.FormatClock(clock)} · " +
        UiKit.T("weather.cell." + WeatherStrip.CellKey(WeatherStrip.CellOf(weather)));

    public static string Moment(SaveMeta meta) => Moment(meta.Season, meta.Clock, meta.Weather);

    /// <summary>游玩时长 HH:MM；秒对玩家没有意义，不写。</summary>
    public static string PlayTime(double seconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return $"{(int)t.TotalHours:00}:{t.Minutes:00}";
    }

    /// <summary>保存时刻按本地时间写成"年-月-日 时:分"；界面不读系统时钟，所以不写今天、昨天这类相对日。</summary>
    public static string SavedAt(long unix) =>
        unix <= 0 ? "—" : DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    /// <summary>开始页继续行：季 · 钟点 · 天气 · 地点 · 时长（SYS-TITLE）。</summary>
    public static string Summary(SaveMeta meta, IMapService maps) =>
        $"{Moment(meta)} · {Place(meta, maps)} · {PlayTime(meta.PlaySeconds)}";

    private static MapDescriptor Map(MapId id, IMapService maps) =>
        maps.Maps.FirstOrDefault(map => map.Id == id) ?? throw new InvalidDataException($"Save refers to unknown map {id.Value}.");
}
