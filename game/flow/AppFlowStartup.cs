using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Contracts.Save;

namespace Campus.Flow;

internal sealed class AppFlowStartup(IMapService maps, AppFlowContinuation continuation, AppFlowResources resources)
{
    private BootFlowRequest? _request;
    private readonly TaskCompletionSource _mapReady = new();
    private readonly TaskCompletionSource _backgroundReady = new();
    private Task? _preparation;
    internal Task MapReady => _mapReady.Task;
    internal Task BackgroundReady => _backgroundReady.Task;
    internal bool CaptureMode { get; private set; }
    internal SlotId? WriteSlot { get; private set; }
    internal BootFlowRequest Request => _request ?? throw new InvalidOperationException("PrepareBoot must run before entry.");
    internal bool ShowTitle => Request.ShowTitle;
    internal uint? Seed => Request.Seed;
    internal void Fail(Exception error) { _mapReady.TrySetException(error); _backgroundReady.TrySetException(error); }
    internal void Cancel() { _mapReady.TrySetCanceled(); _backgroundReady.TrySetCanceled(); }
    internal Task Initialize(BootPhase phase, BootContext context)
    {
        switch (phase)
        {
            case BootPhase.BeforeMap: CaptureMode = context.CaptureMode; WriteSlot = context.Run.Save.WriteSlot; break;
            case BootPhase.AfterMapWarm: case BootPhase.BeforeReveal: break;
            default: throw new ArgumentOutOfRangeException(nameof(phase));
        }
        return Task.CompletedTask;
    }
    internal MapId Select(BootFlowRequest request)
    {
        if (request.LoadFile is not null && request.Slot is not null)
            throw new ArgumentException("Boot cannot select both a slot and a file.", nameof(request));
        _request = request;
        resources.HoldInput();
        if (request.ShowTitle) continuation.RetainTitlePrefetch();
        return request.Map ?? (request.Slot is { } slot && AppFlowContinuation.Readable(continuation.Info(slot))
            ? continuation.Info(slot)!.Meta!.Map : maps.Default);
    }
    internal Task PrepareBackground(Task mapPreparation, Task globalPreparation)
    {
        if (_preparation is not null) throw new InvalidOperationException("Background preparation can start only once.");
        return _preparation = Prepare();
        async Task Prepare()
        {
            var map = Map();
            var combined = Task.WhenAll(map, Global());
            try { await combined; _backgroundReady.TrySetResult(); }
            catch (Exception error)
            {
                var failure = (Exception?)combined.Exception ?? error;
                Fail(failure);
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }
        async Task Global()
        {
            try { await globalPreparation; }
            catch (Exception error) { Fail(error); throw; }
        }
        async Task Map()
        {
            try
            {
                await mapPreparation;
                continuation.RefreshTitlePrefetch();
                _mapReady.TrySetResult();
            }
            catch (Exception error) { Fail(error); throw; }
        }
    }
}
