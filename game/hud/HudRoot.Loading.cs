using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Hud.I18n;
using Campus.Hud.Pages;
using Campus.Hud.Widgets;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Hud;

public partial class HudRoot
{
    private LoadingPage? _loading;
    private EnterRequest? _loadingRequest;
    private bool _loadingCovered;

    public bool Active => _loading is not null;
    public bool IsCovered => _loadingCovered && _loading is { } page && page.IsVisibleInTree();
    public float Progress { get; private set; }

    public async Task Show(EnterRequest? request = null)
    {
        _loadingRequest = request;
        var destination = LoadingDestination(request);
        if (_loading is not null)
        {
            _loading.SetDestination(destination);
            return;
        }
        Progress = 0;
        _loading = LoadingPage.Create(_kit, _loadingHost, destination);
        _loading.Modulate = Colors.White with { A = 0 };
        _wait.Visible = false;
        SyncHostInput();
        await _loading.Fade(visible: true);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        _loadingCovered = true;
    }

    public void Report(int completed, int total)
    {
        if (_loading is null) return;
        Progress = Math.Max(Progress, total > 0 ? Math.Clamp((float)completed / total, 0, 1) : 0);
        _loading.Report(Progress, completed, total);
    }

    async Task IAppLoadingPresentation.Hide()
    {
        if (_loading is null) return;
        // 全部准备完成的那一帧必须先呈现；不在这里替流程补满进度。
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        _loadingCovered = false;
        await _loading.Fade(visible: false);
        _loading.GetParent().RemoveChild(_loading);
        _loading.QueueFree();
        _loading = null;
        SyncHostInput();
        if (_loadingRequest?.Kind == EnterKind.NewGame) ShowFirstGuide();
        _loadingRequest = null;
    }

    private static string LoadingDestination(EnterRequest? request)
    {
        var maps = Services.Get<IMapService>();
        var descriptor = request?.Kind == EnterKind.NewGame
            ? maps.Maps.FirstOrDefault(map => map.Id == maps.Default)
            : maps.Context?.Descriptor;
        var preset = request?.Kind == EnterKind.NewGame ? descriptor?.World.Default : null;
        var season = preset?.Season ?? Services.Get<IWorldState>().SeasonTarget;
        var clock = preset?.Clock ?? Services.Get<IWorldState>().Clock;
        var place = descriptor is null ? "" : Localized(descriptor.Name);
        var action = request?.Kind switch
        {
            null => "loading.title",
            EnterKind.NewGame => "title.new",
            EnterKind.Continue => "title.continue",
            EnterKind.LoadSlot => "saves.load",
            _ => throw new InvalidOperationException("Unknown entry kind.")
        };
        if (request?.Kind is EnterKind.Continue or EnterKind.LoadSlot)
        {
            var saves = Services.Get<ISaveService>();
            var slot = request.Slot ?? saves.Continue;
            if (saves.Slots.FirstOrDefault(item => item.Slot == slot)?.Meta is { } meta)
            {
                season = meta.Season;
                clock = meta.Clock;
                var map = maps.Maps.FirstOrDefault(item => item.Id == meta.Map);
                place = meta.PlaceKey is { } key && Campus.Hud.I18n.Tr.Has(key) ? UiKit.T(key) : map is null ? place : Localized(map.Name);
            }
        }
        return UiKit.T("loading.destination", ("action", UiKit.T(action)), ("place", place),
            ("season", UiKit.T("hud.season." + TimeCard.SeasonCode(season).ToLowerInvariant())), ("clock", WorldTime.FormatClock(clock)));
    }

    private static string Localized(LocalizedName name) =>
        TranslationServer.GetLocale().StartsWith("zh", StringComparison.OrdinalIgnoreCase) || name.En.Length == 0 ? name.Zh : name.En;

    private void ShowFirstGuide()
    {
        if (_settings.FirstGuideSeen || _hidden) return;
        _settings.MarkFirstGuideSeen();
        var label = _kit.Text(_hintHost, UiKit.T("hud.first_guide"), "body", "over.title.text", align: HorizontalAlignment.Center);
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        label.GrowHorizontal = Control.GrowDirection.Both;
        label.GrowVertical = Control.GrowDirection.Begin;
        label.OffsetTop = label.OffsetBottom = -_kit.Tokens.Size("toastBottom");
        label.ResetSize();
        label.Modulate = Colors.White with { A = 0 };
        var tween = label.CreateTween();
        tween.TweenProperty(label, "modulate:a", 1f, _motion.Fade(_kit.Tokens.Ms("loading", "in")));
        tween.TweenInterval(_kit.Tokens.Ms("firstGuide", "hold"));
        tween.TweenProperty(label, "modulate:a", 0f, _motion.Fade(_kit.Tokens.Ms("loading", "out")));
        tween.TweenCallback(Callable.From(label.QueueFree));
    }
}
