using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Hud.I18n;
using Campus.Hud.Pages;
using Campus.Hud.Widgets;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Godot;

namespace Campus.Hud;

public partial class HudRoot
{
    private LoadingPage? _loading;
    private EnterRequest? _loadingRequest;
    private bool _loadingCovered;
    private bool _loadingRetiresFrontispiece;
    private Vector2 _loadingDrawSize;
    private readonly List<LoadingLease> _loadingLeases = [];
    private Task _loadingTransition = Task.CompletedTask;
    private Control _frontispiece = null!;
    private bool _frontispieceCovered;
    private Vector2 _frontispieceDrawSize;
    private Task _frontispieceTransition = Task.CompletedTask;

    public bool Active => _loading is not null;
    public bool IsCovered => _loadingCovered && _loading is { } page && CoversViewport(page, _loadingDrawSize);
    bool IAppScreenCoverage.IsCovered => IsCovered || _frontispieceCovered && CoversViewport(_frontispiece, _frontispieceDrawSize);
    public float Progress { get; private set; }

    public IAppLoadingLease Show(EnterRequest? request = null)
    {
        var lease = new LoadingLease(this, request);
        _loadingLeases.Add(lease);
        _loadingRetiresFrontispiece |= request is not null;
        var previous = _loadingTransition;
        _loadingTransition = ShowLoading(previous, lease);
        lease.Covered = _loadingTransition;
        return lease;
    }

    private async Task ShowLoading(Task previous, LoadingLease lease)
    {
        await previous;
        if (lease.Request is not null) _loadingRequest = lease.Request;
        var destination = LoadingDestination(_loadingRequest);
        if (_loading is null)
        {
            _loading = LoadingPage.Create(_kit, _loadingHost, destination);
            _loading.Modulate = Colors.White with { A = 0 };
            SyncHostInput();
            await _loading.Fade(visible: true);
        }
        else _loading.SetDestination(destination);
        Progress = 0;
        _loading.Report(0, 0, 1);
        await FrameAwaiter.ReadyDrawAsync(this, () => _loading is { } page &&
            CoversViewport(page, GetViewport().GetVisibleRect().Size), TimeSpan.FromSeconds(BootTuning.StalledSeconds));
        _loadingDrawSize = GetViewport().GetVisibleRect().Size;
        _loadingCovered = true;
    }

    private void Report(LoadingLease lease, int completed, int total)
    {
        if (!_loadingLeases.Contains(lease)) return;
        lease.Completed = completed; lease.Total = total;
        lease.Progress = Math.Max(lease.Progress, total > 0 ? Math.Clamp((float)completed / total, 0, 1) : 0);
        if (_loading is null || _loadingLeases[^1] != lease) return;
        Progress = lease.Progress;
        _loading.Report(Progress, completed, total);
    }

    private Task ReleaseLoading(LoadingLease lease)
    {
        if (!_loadingLeases.Remove(lease)) return Task.CompletedTask;
        if (_loadingLeases.Count != 0)
        {
            var current = _loadingLeases[^1];
            Report(current, current.Completed, current.Total);
            return Task.CompletedTask;
        }
        var previous = _loadingTransition;
        return _loadingTransition = HideLoading(previous);
    }

    private async Task HideLoading(Task previous)
    {
        await previous;
        if (_loading is null || _loadingLeases.Count != 0) return;
        // 全部准备完成的那一帧必须先呈现；不在这里替流程补满进度。
        await FrameAwaiter.ReadyDrawAsync(this, () => _loading is { } page &&
            CoversViewport(page, GetViewport().GetVisibleRect().Size), TimeSpan.FromSeconds(BootTuning.StalledSeconds));
        if (_loadingLeases.Count != 0) return;
        if (_loadingRetiresFrontispiece) await ((IAppFrontispiecePresentation)this).Hide(0);
        if (_loadingLeases.Count != 0) return;
        _loadingCovered = false;
        await _loading.Fade(visible: false);
        _loading.GetParent().RemoveChild(_loading);
        _loading.QueueFree();
        _loading = null;
        SyncHostInput();
        if (_loadingRequest?.Kind == EnterKind.NewGame) ShowFirstGuide();
        _loadingRequest = null;
        _loadingRetiresFrontispiece = false;
    }

    Task IAppFrontispiecePresentation.Show()
    {
        var previous = _frontispieceTransition;
        return _frontispieceTransition = ShowFrontispiece(previous);
    }

    private async Task ShowFrontispiece(Task previous)
    {
        await previous;
        _frontispiece.Modulate = Colors.White;
        _frontispiece.Visible = true;
        await FrameAwaiter.ReadyDrawAsync(this, () => CoversViewport(_frontispiece, GetViewport().GetVisibleRect().Size),
            TimeSpan.FromSeconds(BootTuning.StalledSeconds));
        _frontispieceDrawSize = GetViewport().GetVisibleRect().Size;
        _frontispieceCovered = true;
    }

    Task IAppFrontispiecePresentation.Hide(float seconds)
    {
        var previous = _frontispieceTransition;
        return _frontispieceTransition = HideFrontispiece(previous, seconds);
    }

    private async Task HideFrontispiece(Task previous, float seconds)
    {
        await previous;
        if (!_frontispiece.Visible) return;
        _frontispieceCovered = false;
        var duration = _motion.Fade(seconds);
        if (duration > 0)
        {
            var tween = _frontispiece.CreateTween();
            tween.TweenProperty(_frontispiece, "modulate:a", 0f, duration);
            await ToSignal(tween, Tween.SignalName.Finished);
        }
        _frontispiece.Visible = false;
    }

    private bool CoversViewport(Control layer, Vector2 drawnSize)
    {
        if (!IsInsideTree() || !Visible || !layer.IsVisibleInTree() || layer.Modulate.A < 1f || layer.SelfModulate.A < 1f) return false;
        var viewport = GetViewport().GetVisibleRect();
        var rect = layer.GetGlobalRect();
        return drawnSize == viewport.Size && viewport.Size.X > 0 && viewport.Size.Y > 0 &&
            rect.Position.X <= viewport.Position.X && rect.Position.Y <= viewport.Position.Y &&
            rect.End.X >= viewport.End.X && rect.End.Y >= viewport.End.Y;
    }

    private void OnCoverageDrawn()
    {
        if (!RenderingServer.RenderLoopEnabled) return;
        var size = GetViewport().GetVisibleRect().Size;
        if (_loadingCovered && _loading is { } page && CoversViewport(page, size)) _loadingDrawSize = size;
        if (_frontispieceCovered && CoversViewport(_frontispiece, size)) _frontispieceDrawSize = size;
    }

    private sealed class LoadingLease(HudRoot owner, EnterRequest? request) : IAppLoadingLease
    {
        private Task? _release;
        public EnterRequest? Request { get; } = request;
        public Task Covered { get; set; } = Task.CompletedTask;
        public int Completed { get; set; }
        public int Total { get; set; } = 1;
        public float Progress { get; set; }
        public void Report(int completed, int total) => owner.Report(this, completed, total);
        public Task Hide() => _release ??= owner.ReleaseLoading(this);
        public ValueTask DisposeAsync() => new(Hide());
    }

    private string LoadingDestination(EnterRequest? request)
    {
        var maps = _dependencies.Maps;
        var descriptor = request?.Kind == EnterKind.NewGame
            ? maps.Maps.FirstOrDefault(map => map.Id == maps.Default)
            : maps.Context?.Descriptor;
        var preset = request?.Kind == EnterKind.NewGame ? descriptor?.World.Default : null;
        var season = preset?.Season ?? _dependencies.WorldState.SeasonTarget;
        var clock = preset?.Clock ?? _dependencies.WorldState.Clock;
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
            var saves = _dependencies.Saves;
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
