using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;

namespace Campus.Kernel.Stubs;

[KernelService(typeof(IAppFlow), Owner = "WP-00", IsStub = true)]
[KernelService(typeof(IAppBootFlow), Owner = "WP-00", IsStub = true)]
public sealed class AppFlowStub(ICameraRig camera, IVeilService veil, ISaveService saves,
    IMapService maps, IWorldControl world) : IAppFlow, IAppBootFlow
{
    private BootFlowRequest? _bootRequest;
    private IDisposable? _bootInput;
    public AppState State { get; private set; } = AppState.Boot;
    public long Rev { get; private set; }
    public event Action<AppState, AppState>? Changed;
    public EnterPath? Path => State switch
    {
        AppState.Entering => EnterPath.Veil,
        AppState.Boot or AppState.Title or AppState.Playing or AppState.Leaving or AppState.Quitting => null,
        _ => throw new InvalidOperationException("Unknown application state."),
    };
    public bool Skippable => false;

    public Task<MapId> PrepareBoot(BootFlowRequest request)
    {
        if (!IsState(AppState.Boot)) throw new InvalidOperationException("Startup has already completed.");
        _bootRequest = request;
        _bootInput ??= camera.LockInput(InputLockReason.Cinematic);
        return Task.FromResult(request.Map ?? maps.Default);
    }

    public Task PrepareWorld()
    {
        if (_bootRequest is null) throw new InvalidOperationException("PrepareBoot must run first.");
        // The D0 save service has no readable slots. World and map defaults were set by bootstrap.
        return Task.CompletedTask;
    }

    public Task CompleteBoot()
    {
        if (_bootRequest is null) throw new InvalidOperationException("PrepareBoot must run first.");
        // D0 intentionally retains the direct --title=0 path; WP-49a supplies the real title lifecycle.
        Change(AppState.Playing);
        _bootInput?.Dispose();
        _bootInput = null;
        return Task.CompletedTask;
    }

    // Boot calls this after the map, world, registry, and viewport are ready.
    public async Task Initialize()
    {
        if (!IsState(AppState.Boot)) return;
        using var input = camera.LockInput(InputLockReason.Cinematic);
        await veil.Cover(VeilReason.Boot, 0);
        await veil.Reveal(VeilReason.Boot, 0.3f);
        Change(AppState.Playing);
    }

    public async Task<EnterResult> Enter(EnterRequest r)
    {
        if (!IsState(AppState.Title)) return EnterResult.Rejected;
        if (maps.Context is not { } context) return EnterResult.Failed;
        Change(AppState.Entering);
        var result = EnterResult.Failed;
        await new CinematicPlayerStub(camera, veil).ReachEnd(async () =>
        {
            var slot = r.Slot ?? saves.Continue ?? saves.Slots[0].Slot;
            var loaded = r.Kind switch
            {
                EnterKind.NewGame => await saves.NewGame(slot),
                EnterKind.Continue or EnterKind.LoadSlot => await saves.Load(slot),
                _ => throw new ArgumentOutOfRangeException(nameof(r)),
            };
            switch (loaded.Status)
            {
                case LoadStatus.Loaded:
                case LoadStatus.Recovered:
                    break;
                case LoadStatus.Empty:
                case LoadStatus.Corrupt:
                case LoadStatus.NewerBuild:
                case LoadStatus.Failed:
                    return;
                default: throw new ArgumentOutOfRangeException(nameof(loaded.Status));
            }
            switch (r.Kind)
            {
                case EnterKind.NewGame:
                    world.ResetDefault();
                    var arrived = await maps.Travel(new(context.Descriptor.Id,
                        Cause: MapTravelCause.NewGame)).Done;
                    result = arrived switch
                    {
                        MapTravelResult.Arrived => EnterResult.Entered,
                        MapTravelResult.Rejected or MapTravelResult.Failed => EnterResult.Failed,
                        _ => throw new ArgumentOutOfRangeException(nameof(arrived)),
                    };
                    break;
                case EnterKind.Continue:
                case EnterKind.LoadSlot:
                    await saves.CompleteResume();
                    result = EnterResult.Entered;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(r));
            }
        });
        Change(result switch
        {
            EnterResult.Entered => AppState.Playing,
            EnterResult.Rejected or EnterResult.Failed => AppState.Title,
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        });
        return result;
    }
    public async Task ReturnToTitle()
    {
        if (!IsState(AppState.Playing)) return;
        Change(AppState.Leaving);
        var saved = await saves.FlushForQuit(SaveReason.ToTitle, TimeSpan.FromSeconds(5));
        if (!CanLeave(saved.Status)) { Change(AppState.Playing); return; }
        await new CinematicPlayerStub(camera, veil).ReachEnd(() =>
        {
            if (maps.Context?.Descriptor.Title is { } title) camera.SetPose(title.T0);
            return Task.CompletedTask;
        });
        Change(AppState.Title);
    }
    public void Skip() { }
    public async Task Quit()
    {
        Change(AppState.Quitting);
        var saved = await saves.FlushForQuit(SaveReason.Quit, TimeSpan.FromSeconds(5));
        if (!CanLeave(saved.Status)) Change(AppState.Playing);
    }
    private static bool CanLeave(SaveStatus status) => status switch
    {
        SaveStatus.Saved or SaveStatus.Disabled or SaveStatus.Deferred => true,
        SaveStatus.Failed => false,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };
    private bool IsState(AppState expected) => State switch
    {
        AppState.Boot or AppState.Title or AppState.Entering or AppState.Playing or AppState.Leaving or AppState.Quitting => State == expected,
        _ => throw new InvalidOperationException("Unknown application state."),
    };
    private void Change(AppState next)
    {
        if (State == next) return;
        var previous = State; State = next; Rev++; Changed?.Invoke(previous, next);
    }
}

// No timeline is played in D0; the same endpoint work runs underneath the shared veil.
public sealed class CinematicPlayerStub(ICameraRig camera, IVeilService veil)
{
    public async Task ReachEnd(Func<Task> endpoint)
    {
        using var input = camera.LockInput(InputLockReason.Cinematic);
        await veil.Cover(VeilReason.Cinematic, 0);
        try { await endpoint(); }
        finally { await veil.Reveal(VeilReason.Cinematic, 0); }
    }
}
