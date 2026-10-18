using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Startup;

namespace Campus.Flow;

[KernelService(typeof(IAppFlow), Owner = "WP-49a")]
[KernelService(typeof(IAppBootFlow), Owner = "WP-49a")]
public sealed partial class AppFlowService : IAppBootFlow, IBootParticipant
{
    private BootFlowRequest? _boot;
    private SlotId? _bootSlot;
    private bool _worldPrepared;
    private string? _cinematicOverride;
    private bool _captureMode;
    public bool WillRestoreWorld => _boot is not null && (_boot.LoadFile is not null || _bootSlot.HasValue);
    public string Id => "app-flow";
    public int Order => 100;
    public Task Initialize(BootPhase phase, BootContext context)
    {
        switch (phase)
        {
            case BootPhase.BeforeMap: _captureMode = context.CaptureMode; break;
            case BootPhase.AfterMapWarm: case BootPhase.BeforeReveal: break;
            default: throw new ArgumentOutOfRangeException(nameof(phase));
        }
        return Task.CompletedTask;
    }

    public Task<MapId> PrepareBoot(BootFlowRequest request)
    {
        if (!In(AppState.Boot)) throw new InvalidOperationException("Boot preparation is only accepted during Boot.");
        if (request.LoadFile is not null && request.Slot is not null) throw new ArgumentException("Boot cannot select both a slot and a file.", nameof(request));
        _boot = request; _worldPrepared = false; _preview = null; _livePreview = false; _previewNeedsVeil = false; _cinematicOverride = request.Cinematic; _faultHold = null;
        _bootSlot = request.Slot ?? (request.ShowTitle && !_captureMode && request.LoadFile is null ? saves.Continue : null);
        if (request.Slot is null && _bootSlot is { } automatic && !Readable(Info(automatic))) _bootSlot = null;
        HoldInput();
        var target = request.Map ?? (_bootSlot is { } slot && Readable(Info(slot)) ? Info(slot)!.Meta!.Map : maps.Default);
        return Task.FromResult(target);
    }
    public async Task PrepareWorld()
    {
        if (!In(AppState.Boot) || _boot is null) throw new InvalidOperationException("PrepareBoot must run before PrepareWorld.");
        try { await PrepareWorldCore(); }
        catch (Exception) when (restore.RestoreFaulted) { await HoldRestoreFailure(keepBoot: true); throw; }
    }
    private async Task PrepareWorldCore()
    {
        var request = _boot;
        if (request is null) throw new InvalidOperationException("PrepareBoot must run before PrepareWorld.");
        if (request.LoadFile is { } file)
        {
            var result = await saves.LoadFile(file);
            RequireLoaded(result);
            _preview = null;
        }
        else if (_bootSlot is { } slot)
        {
            var result = await saves.Load(slot, request.ShowTitle ? LoadScope.Preview : LoadScope.Full);
            RequireHealthyWorld();
            if (Loaded(result.Status)) { _preview = request.ShowTitle ? slot : null; _previewNeedsVeil = result.Repairs.Count > 0; }
            else if (request.Slot is not null) RequireLoaded(result);
            else _preview = null; // An unreadable automatic Continue falls back to the map preview.
        }
        RequireHealthyWorld();
        if (request.ShowTitle) { HoldTitleFlow(); camera.SetPose(TitlePose()); }
        // Stage owns no-load world initialization before Travel and supplies desired
        // flow before this method. This branch never reconstructs a default world.
        _worldPrepared = true;
    }
    public Task CompleteBoot()
    {
        RequireHealthyWorld();
        if (!In(AppState.Boot) || _boot is null || !_worldPrepared) throw new InvalidOperationException("Boot world is not prepared.");
        if (_boot.ShowTitle) Change(AppState.Title);
        else { Change(AppState.Playing); ReleaseInput(); }
        return Task.CompletedTask;
    }
}
