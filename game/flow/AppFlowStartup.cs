using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Startup;

namespace Campus.Flow;

internal sealed class AppFlowStartup(ISaveService saves, IMapService maps, AppFlowContinuation continuation,
    AppFlowResources resources, IAppTitlePresentation titlePresentation, AppFlowRecovery recovery)
{
    private BootFlowRequest? _request;
    private SlotId? _slot;
    private bool _worldPrepared;
    private readonly TaskCompletionSource _worldReady = new();
    private readonly TaskCompletionSource _mapReady = new();
    internal Task MapReady => _mapReady.Task;
    internal Task WorldReady => _worldReady.Task;
    internal void MapAvailable() => _mapReady.TrySetResult();
    internal void MarkPrepared() => _worldPrepared = true;
    internal bool CaptureMode { get; private set; }
    internal bool HasRequest => _request is not null;
    internal bool ShowTitle => _request is { ShowTitle: true };
    internal uint? Seed => _request?.Seed;
    internal bool WillRestoreWorld => _request is not null && (_request.LoadFile is not null || _slot.HasValue);
    internal void Fail(Exception error) { _mapReady.TrySetException(error); _worldReady.TrySetException(error); }
    internal void Cancel() { _mapReady.TrySetCanceled(); _worldReady.TrySetCanceled(); }
    internal void Complete() => _worldReady.TrySetResult();

    internal Task Initialize(BootPhase phase, BootContext context)
    {
        switch (phase)
        {
            case BootPhase.BeforeMap: CaptureMode = context.CaptureMode; break;
            case BootPhase.AfterMapWarm: case BootPhase.BeforeReveal: break;
            default: throw new ArgumentOutOfRangeException(nameof(phase));
        }
        return Task.CompletedTask;
    }
    internal MapId Select(BootFlowRequest request)
    {
        if (request.LoadFile is not null && request.Slot is not null)
            throw new ArgumentException("Boot cannot select both a slot and a file.", nameof(request));
        _request = request; _worldPrepared = false;
        continuation.ResetPreview(); recovery.ResetHold();
        _slot = request.Slot ?? (request.ShowTitle && !CaptureMode && request.LoadFile is null ? saves.Continue : null);
        if (request.Slot is null && _slot is { } automatic && !AppFlowContinuation.Readable(continuation.Info(automatic))) _slot = null;
        resources.HoldInput();
        return request.Map ?? (_slot is { } slot && AppFlowContinuation.Readable(continuation.Info(slot))
            ? continuation.Info(slot)!.Meta!.Map : maps.Default);
    }
    internal async Task PrepareWorld(Func<bool> entryTaken)
    {
        var request = _request ?? throw new InvalidOperationException("PrepareBoot must run before PrepareWorld.");
        titlePresentation.SetVisible(request.ShowTitle);
        await continuation.LoadBootWorld(request, _slot);
        continuation.RequireHealthyWorld();
        if (entryTaken()) { _worldPrepared = true; return; }
        if (request.ShowTitle) { resources.HoldTitleFlow(); await continuation.PositionBootTitle(entryTaken); }
        // The stage initialized no-load worlds before map preparation and supplied their desired flow.
        _worldPrepared = true;
    }
    internal void RecoverAsTitle()
    {
        _request = (_request ?? throw new InvalidOperationException("PrepareBoot must run before PrepareWorld.")) with { ShowTitle = true };
        _worldPrepared = true;
    }
    internal void RequirePrepared()
    {
        if (_request is null || !_worldPrepared) throw new InvalidOperationException("Boot world is not prepared.");
    }
}
