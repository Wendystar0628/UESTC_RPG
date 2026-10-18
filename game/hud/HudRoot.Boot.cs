using Campus.Contracts.Startup;
using Campus.Hud.Pages;
using Godot;

namespace Campus.Hud;

public partial class HudRoot
{
    public string Id => "hud";
    public int Order => 0;

    /// <summary>启动参数在 BeforeMap 才到：_Ready 时先按默认搭好，这里把 --ui、--ui-scale、--ui-gallery、--ui-export-theme 和交给动效、主题时钟的参数补上。</summary>
    public Task Initialize(BootPhase phase, BootContext context)
    {
        if (phase != BootPhase.BeforeMap) return Task.CompletedTask;
        _args = HudArguments.From(context);
        _motion.Apply(_args);
        if (_args.ExportTheme is { } exportPath)
        {
            var error = _theme.Export(exportPath);
            GD.Print($"UI theme exported: {exportPath} ({error})");
            GetTree().Quit(error == Error.Ok ? 0 : 2);
            return Task.CompletedTask;
        }
        _clock.Apply(_args);
        _hidden = _args.UiHidden;
        _root.Visible = !_hidden;
        ApplyScale();
        if (_args.UiGallery is { } gallery) CallDeferred(MethodName.OpenGallery, gallery);
        return Task.CompletedTask;
    }

    /// <summary>--ui-gallery=&lt;页名&gt;：启动时直接打开一页，供截图与评审（UI-VISUAL；完整的组件画廊归 35c）。</summary>
    private void OpenGallery(string page)
    {
        switch (page)
        {
            case "settings": OpenSettings(SettingsPage.General, fromTitle: false); break;
            case "graphics": OpenSettings(SettingsPage.Graphics, fromTitle: false); break;
            case "sound": OpenSettings(SettingsPage.Sound, fromTitle: false); break;
            case "keys": OpenSettings(SettingsPage.Keys, fromTitle: false); break;
            case "rebind": OpenSettings(SettingsPage.Rebind, fromTitle: false); break;
            case "licenses": OpenSettings(SettingsPage.Licenses, fromTitle: false); break;
            case "saves": OpenSaves(fromTitle: true); break;
            case "title": _clean = _galleryTitle = true; _title.Open(); break;
            case "hud": break;
            case "census": _ = Census(3); break;
            default: throw new ArgumentException($"Unknown --ui-gallery page: {page}");
        }
    }

    /// <summary>
    /// --ui-gallery=census：数界面占着实例参数槽的件数（<see cref="UiKit.Holders"/>）。先在漫游中数一次，再经 IAppFlow 回到开始页
    /// （没有 IAppFlow 时像 --ui-gallery=title 一样把开始页压在画面上），然后把设置卡、存档页各开关 <paramref name="cycles"/> 次，
    /// 每步记一行日志，最后走正常退出。
    /// </summary>
    private async Task Census(int cycles)
    {
        try
        {
            for (var i = 0; i < 600 && !Playing; i++) await NextFrame();
            for (var i = 0; i < 10; i++) await NextFrame();
            CensusLine("playing");
            if (_flow is not null) await _flow.ReturnToTitle();
            else { _clean = _galleryTitle = true; _title.Open(); }
            for (var i = 0; i < 10; i++) await NextFrame();
            CensusLine($"title ({_flow?.State.ToString() ?? "no flow"})");
            for (var i = 1; i <= cycles; i++)
            {
                OpenSettings(SettingsPage.Graphics, fromTitle: true);
                await NextFrame();
                CensusLine($"settings open #{i}");
                CloseSettings();
                OpenSaves(fromTitle: true);
                await NextFrame();
                CensusLine($"saves open #{i}");
                CloseSaves();
                await NextFrame();
                CensusLine($"closed #{i}");
            }
        }
        catch (Exception error) { GD.PushError($"UI census failed: {error}"); }
        RequestQuit();
    }

    private SignalAwaiter NextFrame() => ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

    private void CensusLine(string step) => GD.Print($"UI census - {step}: {_kit.Holders(_root)} items hold shader instance slots");
}
