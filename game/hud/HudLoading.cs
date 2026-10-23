using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Hud.Pages;
using Campus.Hud.Widgets;
using Godot;
using System.Threading;

namespace Campus.Hud;

/// <summary>Serializes loading leases and tracks their actual viewport coverage.</summary>
internal sealed class HudLoading : IAppLoadingPresentation
{
    private readonly CanvasLayer _owner;
    private readonly HudRootDependencies _dependencies;
    private readonly HudView _view;
    private readonly Action _syncInput;
    private CancellationTokenSource _lifetime = null!;

    public HudLoading(CanvasLayer owner, HudRootDependencies dependencies, HudView view, Action syncInput)
    {
        _owner = owner;
        _dependencies = dependencies;
        _view = view;
        _syncInput = syncInput;
    }

    public void Enter()
    {
        _lifetime = new CancellationTokenSource();
        RenderingServer.Singleton.FramePostDraw += OnCoverageDrawn;
    }

    public void Exit()
    {
        RenderingServer.Singleton.FramePostDraw -= OnCoverageDrawn;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _loadingCovered = false;
        _loadingLeases.Clear();
        _loadingTransition = Task.CompletedTask;
        if (_loading is { } page)
        {
            page.GetParent().RemoveChild(page);
            page.QueueFree();
            _loading = null;
        }
        _loadingRequest = null;
        Progress = 0;
    }

    private LoadingPage? _loading;
    private EnterRequest? _loadingRequest;
    private bool _loadingCovered;
    private Vector2 _loadingDrawSize;
    private readonly List<LoadingLease> _loadingLeases = [];
    private Task _loadingTransition = Task.CompletedTask;

    public bool Active => _loading is not null;
    public bool IsCovered => _loadingCovered && _loading is { } page && CoversViewport(_owner, page, _loadingDrawSize);
    public float Progress { get; private set; }

    public IAppLoadingLease Show(EnterRequest? request = null)
    {
        var lease = new LoadingLease(this, request);
        _loadingLeases.Add(lease);
        var previous = _loadingTransition;
        _loadingTransition = ShowLoading(previous, lease, _lifetime.Token);
        lease.Covered = _loadingTransition;
        return lease;
    }

    private async Task ShowLoading(Task previous, LoadingLease lease, CancellationToken lifetime)
    {
        await previous.WaitAsync(lifetime);
        lifetime.ThrowIfCancellationRequested();
        if (lease.Request is not null) _loadingRequest = lease.Request;
        var destination = LoadingDestination(_loadingRequest);
        if (_loading is null)
        {
            _loading = LoadingPage.Create(_view.Kit, _view.LoadingHost, destination);
            _loading.Modulate = Colors.White with { A = 0 };
            _syncInput();
            await _loading.Fade(visible: true, lifetime);
            lifetime.ThrowIfCancellationRequested();
        }
        else _loading.SetDestination(destination);
        Progress = 0;
        _loading.Report(0, 0, 1);
        await FrameAwaiter.ReadyDrawAsync(_owner, () => _loading is { } page &&
            CoversViewport(_owner, page, _owner.GetViewport().GetVisibleRect().Size), TimeSpan.FromSeconds(BootTuning.StalledSeconds), lifetime);
        lifetime.ThrowIfCancellationRequested();
        _loadingDrawSize = _owner.GetViewport().GetVisibleRect().Size;
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
        return _loadingTransition = HideLoading(previous, _lifetime.Token);
    }

    private async Task HideLoading(Task previous, CancellationToken lifetime)
    {
        await previous.WaitAsync(lifetime);
        lifetime.ThrowIfCancellationRequested();
        if (_loading is null || _loadingLeases.Count != 0) return;
        // 全部准备完成的那一帧必须先呈现；不在这里替流程补满进度。
        await FrameAwaiter.ReadyDrawAsync(_owner, () => _loading is { } page &&
            CoversViewport(_owner, page, _owner.GetViewport().GetVisibleRect().Size), TimeSpan.FromSeconds(BootTuning.StalledSeconds), lifetime);
        lifetime.ThrowIfCancellationRequested();
        if (_loadingLeases.Count != 0) return;
        _loadingCovered = false;
        await _loading.Fade(visible: false, lifetime);
        lifetime.ThrowIfCancellationRequested();
        _loading.GetParent().RemoveChild(_loading);
        _loading.QueueFree();
        _loading = null;
        _syncInput();
        _loadingRequest = null;
    }

    /// <summary>The layer is fully opaque, spans the visible viewport, and was drawn at the current viewport size.</summary>
    internal static bool CoversViewport(CanvasLayer owner, Control layer, Vector2 drawnSize)
    {
        if (!owner.IsInsideTree() || !owner.Visible || !layer.IsVisibleInTree() || layer.Modulate.A < 1f || layer.SelfModulate.A < 1f) return false;
        var viewport = owner.GetViewport().GetVisibleRect();
        var rect = layer.GetGlobalRect();
        return drawnSize == viewport.Size && viewport.Size.X > 0 && viewport.Size.Y > 0 &&
            rect.Position.X <= viewport.Position.X && rect.Position.Y <= viewport.Position.Y &&
            rect.End.X >= viewport.End.X && rect.End.Y >= viewport.End.Y;
    }

    private void OnCoverageDrawn()
    {
        if (!RenderingServer.RenderLoopEnabled) return;
        var size = _owner.GetViewport().GetVisibleRect().Size;
        if (_loadingCovered && _loading is { } page && CoversViewport(_owner, page, size)) _loadingDrawSize = size;
    }

    private sealed class LoadingLease(HudLoading owner, EnterRequest? request) : IAppLoadingLease
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
        var weather = preset?.Weather ?? _dependencies.WorldState.Weather.Kind;
        var place = descriptor is null ? "" : Campus.Hud.I18n.Tr.Name(descriptor.Name);
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
                weather = meta.Weather;
                place = SaveMetaText.Place(meta, maps);
            }
        }
        return UiKit.T("loading.destination", ("action", UiKit.T(action)), ("place", place),
            ("moment", SaveMetaText.Moment(season, clock, weather)));
    }
}
