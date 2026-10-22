using Campus.Contracts;

namespace Campus.Hud;

/// <summary>
/// 界面层唯一的面状态持有者：小地图、图集页、设置卡、存档页的属主在面真实开着且可交互时上报、不再可交互时撤下，
/// 这里只合并、去重并发布。放在界面根而不在 map/，因为面状态跨地图界面与大页，不属于任何一个界面模块。
/// 上报与发布都在主线程，值不变不发布。
/// </summary>
[KernelService(typeof(IUiSurfaceState))]
[KernelService(typeof(IUiSurfaceSink))]
public sealed class HudSurfaceState : IUiSurfaceState, IUiSurfaceSink
{
    public UiSurface Open { get; private set; }
    public string? SettingsPage { get; private set; }
    public long Rev { get; private set; }
    public event Action? Changed;

    public void Report(UiSurface surface, bool open)
    {
        if (surface is not (UiSurface.Minimap or UiSurface.Atlas or UiSurface.AtlasIndex or UiSurface.SavePage))
            throw new ArgumentOutOfRangeException(nameof(surface), surface, "Report takes exactly one of Minimap, Atlas, AtlasIndex or SavePage.");
        Publish(open ? Open | surface : Open & ~surface, SettingsPage);
    }

    public void ReportSettings(string? page)
    {
        Publish(page is null ? Open & ~UiSurface.SettingsCard : Open | UiSurface.SettingsCard, page);
    }

    private void Publish(UiSurface open, string? settingsPage)
    {
        if (open == Open && settingsPage == SettingsPage) return;
        Open = open;
        SettingsPage = settingsPage;
        Rev++;
        Changed?.Invoke();
    }
}
