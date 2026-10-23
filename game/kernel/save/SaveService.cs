using System.Collections.Concurrent;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;
using Campus.Kernel.Maps;
using Campus.Kernel.World;
using Campus.Kernel.Diagnostics;

namespace Campus.Kernel.Save;

[KernelService(typeof(SaveService))]
[KernelService(typeof(ISaveService))]
public sealed class SaveService : ISaveService, IDisposable, IAsyncDisposable
{
    private readonly ConcurrentQueue<Action> _frames = new();
    private readonly object _frameGate = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly CancellationToken _lifetime;
    private readonly List<Task<SaveResult>> _writes = [];
    private Task? _initialization, _disposal;
    private readonly SemaphoreSlim _operation = new(1, 1);
    private readonly Dictionary<SlotId, SlotSnapshot> _slots = [];
    private SaveRuntimeOptions _options = null!;
    private SaveRuntimeBindings _runtime = null!;
    private SaveSections _sections = null!;
    private SaveRepository? _repository;
    private IReadOnlyDictionary<string, RawSaveSection> _unknown = new Dictionary<string, RawSaveSection>();
    private SaveRestoreOperation? _restore;
    private Task<SaveResult>? _writing;
    private SaveWriteOperation? _writingOperation;
    private Task<SaveResult>? _flush;
    private SaveWriteOperation? _flushOperation;
    private SaveReason? _request;
    private double _requestAge, _interval, _playSeconds;
    private ulong _line;
    private bool _newLine, _initialized, _stopping, _authorizedOverwrite;
    private int _frame;
    private byte[]? _thumbnail;
    private RulesetId _defaultRuleset;
    private IReadOnlyList<SlotInfo> _slotInfos = SaveSlots.Auto.Select(s => new SlotInfo(s, SlotKind.Auto, SlotHealth.Empty, null, 0)).ToArray();
    public SaveService() => _lifetime = _stop.Token;

    public bool Enabled => _initialized && _repository?.Enabled == true;
    public IReadOnlyList<SlotInfo> Slots => _slotInfos;
    public SlotId? Continue => _slotInfos.Where(s => s.Health == SlotHealth.Ok)
        .OrderByDescending(s => s.Meta!.SavedAtUnix).ThenBy(s => s.Slot.Value, StringComparer.Ordinal).Select(s => (SlotId?)s.Slot).FirstOrDefault();
    public SlotId? Active { get; private set; }
    public bool Busy => _writing is { IsCompleted: false };
    public SaveState State { get; private set; } = new(null, null, null, null, ReadOnlyReason.None);
    public event Action? SlotsChanged;

    public Task Initialize(SaveRuntimeOptions options, SaveRuntimeBindings bindings, SaveSections sections)
    {
        ObjectDisposedException.ThrowIf(_stopping, this);
        if (_initialization is not null) throw new InvalidOperationException("Save service was initialized twice.");
        _options = options; _runtime = bindings; _sections = sections;
        _defaultRuleset = bindings.Ruleset.Current.Id;
        _restore = new(bindings, PublishRestoreFault, PublishRestoreSuccess);
        return _initialization = InitializeRepository(options, sections);
    }
    private async Task InitializeRepository(SaveRuntimeOptions options, SaveSections sections)
    {
        using var data = await SaveInitialization.Prepare(options, sections, _lifetime).ConfigureAwait(false);
        await OnFrame(() =>
        {
            _repository = data.TakeRepository();
            foreach (var slot in data.Slots) _slots[slot.Info.Slot] = slot;
            _initialized = true;
            Publish(null);
        }).ConfigureAwait(false);
    }

    /// <summary>Called only by the save node, after StreamMount. Queued callbacks are snapshot/restoration boundaries.</summary>
    public void Tick(double delta)
    {
        var callbacks = _frames.Count;
        for (var i = 0; i < callbacks && _frames.TryDequeue(out var action); i++) action();
        if (!_initialized || _stopping) return;
        _frame++;
        var dt = Math.Clamp(double.IsFinite(delta) ? delta : 0, 0, .1);
        if (_request.HasValue) _requestAge += dt;
        if (Playing && !Restoring && !RestoreFaulted)
        {
            _playSeconds += dt; _interval += dt;
            if (_flush is { IsCompleted: true }) { _flush = null; _flushOperation = null; }
            if (_interval >= 300 && _request is null) Request(SaveReason.Interval);
        }
        if (_options.SaveAtFrame is { } at && _frame >= at && !_saveAtDone && Safe)
        { _saveAtDone = true; _ = SaveNow(SaveReason.Test); }
        if (_request is { } reason && !Busy && (Safe || _requestAge >= 60) && !Restoring &&
            Playing && _runtime.World.Instance != 0)
        {
            var forced = !Safe;
            _request = null; _requestAge = 0;
            _ = StartSave(reason, forced);
        }
    }
    private bool _saveAtDone;
    private bool Playing => _runtime.AppFlow.IsPlaying;
    private bool MapTransitioning => (_runtime.AppFlow.Activity & AppActivity.MapTransition) != 0;

    private bool Safe
    {
        get
        {
            if (RestoreFaulted || Restoring || _runtime.World.Instance == 0 || !Playing || _runtime.Loading.Active || MapTransitioning) return false;
            foreach (var registration in _sections.Registered) if (!registration.Section.IsSettled) return false;
            return true;
        }
    }

    /// <summary>Timed saves are the 5-minute Interval (and the gate's Test); other reasons never schedule a save (SYS-SAVE-FLOW).</summary>
    public void Request(SaveReason reason)
    {
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason));
        if (reason is SaveReason.Quit or SaveReason.ToTitle) throw new ArgumentException("Use FlushForQuit.", nameof(reason));
        if (reason is not (SaveReason.Interval or SaveReason.Test)) return;
        if (!_initialized || !Enabled || Active is null || _runtime.World.Instance == 0 || Restoring || RestoreFaulted ||
            !Playing || MapTransitioning) return;
        if (_request is null) _requestAge = 0;
        _request = reason;
    }

    public Task<SaveResult> SaveNow(SaveReason reason)
    {
        if (reason is not (SaveReason.NewGame or SaveReason.Test)) throw new ArgumentOutOfRangeException(nameof(reason));
        if (_initialized && _runtime.World.Instance == 0)
            return Task.FromResult(new SaveResult(SaveStatus.Deferred, Active, 0,
                RestoreFaulted ? "save.error.restore_world" : null));
        return StartSave(reason, forced: true);
    }

    private Task<SaveResult> StartSave(SaveReason reason, bool forced)
    {
        ObjectDisposedException.ThrowIf(_stopping, this);
        var operation = new SaveWriteOperation();
        var task = SaveOperation(operation, reason, forced);
        _writes.RemoveAll(write => write.IsCompletedSuccessfully);
        _writes.Add(task);
        _writingOperation = operation;
        _writing = task;
        return task;
    }

    private async Task<SaveResult> SaveOperation(SaveWriteOperation operation, SaveReason reason, bool forced)
    {
        await _operation.WaitAsync(_lifetime).ConfigureAwait(false);
        try
        {
            if (await OnFrame(() => RestoreFaulted).ConfigureAwait(false))
                return new(SaveStatus.Failed, Active, 0, "save.error.restore_world");
            var disabled = await OnFrame(() => !Enabled || Active is null || _runtime.World.Instance == 0 || Restoring).ConfigureAwait(false);
            if (disabled) return new(Enabled ? SaveStatus.Deferred : SaveStatus.Disabled, Active, 0);
            var active = Active!.Value;
            var current = await Task.Run(() => _repository!.Scan(active)).ConfigureAwait(false);
            await OnFrame(() => _slots[active] = current).ConfigureAwait(false);
            if (current.Info.Health == SlotHealth.Newer)
                return new(SaveStatus.Disabled, active, 0, "save.error.newer_slot");
            if (!_authorizedOverwrite && current.Info.Health == SlotHealth.Corrupt)
                return new(SaveStatus.Disabled, active, 0, "save.error.read_only_slot");
            byte[]? thumbnail = _thumbnail;
            if (reason is SaveReason.Quit or SaveReason.ToTitle or SaveReason.NewGame)
            {
                thumbnail = await operation.CaptureThumbnail(_runtime.Diagnostics, async () =>
                {
                    var capture = await OnFrame(_runtime.CaptureThumbnail).ConfigureAwait(false);
                    return await capture.ConfigureAwait(false);
                }, thumbnail).ConfigureAwait(false);
            }
            var snapshot = await OnFrame(() => Capture(reason, forced, checked(current.MaxGeneration + 1), thumbnail)).ConfigureAwait(false);
            if (snapshot is null) return new(SaveStatus.Deferred, active, 0);
            SlotSnapshot saved;
            try
            {
                var document = await Task.Run(() =>
                {
                    var raw = _sections.Serialize(snapshot.Sections, snapshot.Unknown, snapshot.Meta.Map, snapshot.Moment);
                    var meta = SettledMetadata(snapshot.Meta, raw);
                    return new SaveDocument(meta, raw, snapshot.Thumbnail);
                }).ConfigureAwait(false);
                var finalMeta = await OnFrame(() =>
                {
                    var region = _runtime.Maps.Current == document.Meta.Map
                        ? _runtime.Atlas.RegionAt(document.Meta.Pose.Position.X, document.Meta.Pose.Position.Z) : null;
                    return document.Meta with { Region = region, PlaceKey = region is null ? null : "place." + region };
                }).ConfigureAwait(false);
                document = document with { Meta = finalMeta };
                var replaceUnusable = _authorizedOverwrite;
                operation.MarkSnapshotDetached();
                saved = await Task.Run(() => operation.Commit(_repository!, document, replaceUnusable: replaceUnusable)).ConfigureAwait(false);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or SaveFormatException or JsonException or ArgumentException or InvalidOperationException)
            {
                return await WriteFailed(operation, error, active, snapshot.Meta.Gen).ConfigureAwait(false);
            }
            var result = new SaveResult(SaveStatus.Saved, active, saved.Info.Meta!.Gen);
            return await PublishWriteResult(result, () =>
            {
                _slots[active] = saved; _line = saved.Info.Meta!.Line; _newLine = false; _authorizedOverwrite = false;
                _thumbnail = saved.Save!.Document.Thumbnail; _unknown = saved.Save.Document.Sections;
                _interval = 0;
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException error) when (_stop.IsCancellationRequested && error.CancellationToken == _lifetime)
        { throw; }
        catch (Exception error)
        {
            return await WriteFailed(operation, error, null, 0).ConfigureAwait(false);
        }
        finally { _operation.Release(); }
    }

    private Task<SaveResult> WriteFailed(SaveWriteOperation operation,
        [FailureHandler("SYS-SAVE-FLOW")] Exception error, SlotId? slot, ulong generation) =>
        PublishWriteResult(operation.Fail(_runtime.Diagnostics, error, slot ?? Active, generation));

    private async Task<SaveResult> PublishWriteResult(SaveResult result, Action? apply = null)
    {
        try { return await OnFrame(() => { apply?.Invoke(); Publish(result); return result; }).ConfigureAwait(false); }
        catch (OperationCanceledException error) when (_stop.IsCancellationRequested && error.CancellationToken == _lifetime)
        { return result; }
    }

    private sealed record Snapshot(SaveMeta Meta, WorldSaveMoment Moment, IReadOnlyList<CapturedSection> Sections,
        IReadOnlyDictionary<string, RawSaveSection> Unknown, byte[]? Thumbnail);
    private Snapshot? Capture(SaveReason reason, bool forced, ulong generation, byte[]? thumbnail)
    {
        if (RestoreFaulted) throw new InvalidOperationException("save.error.restore_world");
        if (_runtime.World.Instance == 0) throw new InvalidOperationException("A world ended before its queued save snapshot.");
        // Never combine the old map's stable maps segment with a new map's observer/weather state.
        if (MapTransitioning ||
            reason == SaveReason.Interval && !Playing)
            return null;
        var map = _runtime.Maps.Current;
        var moment = _runtime.WorldRestore.GetSaveMoment();
        var visited = _runtime.Maps.Visits.Select(v => v.Map).Append(map).Distinct().OrderBy(m => m.Value, StringComparer.Ordinal).ToArray();
        var captures = new List<CapturedSection>(_sections.Registered.Count + visited.Length);
        foreach (var registration in _sections.Registered)
            foreach (var sectionMap in registration.Attribute.Scope == SaveScope.Global ? [map] : visited)
            {
                var context = new CaptureContext(reason, sectionMap, forced || !Safe) { Moment = moment };
                captures.Add(new(SaveSections.Key(registration.Attribute, sectionMap), registration, registration.Section.Capture(in context)));
            }
        var world = _runtime.World;
        var observer = _runtime.Observer;
        var region = _runtime.Atlas.RegionAt(observer.Feet.X, observer.Feet.Z);
        var meta = new SaveMeta(SaveContainer.CurrentFormat, Active!.Value, SlotKind.Auto, generation, _newLine ? generation : _line,
            _playSeconds, map, world.WorldHours, world.Clock, world.SeasonTarget, world.Weather.Kind,
            observer.Mode, _runtime.Camera.Pose, observer.Space, region, region is null ? null : "place." + region,
            _runtime.UnixTime(), reason, _runtime.Ruleset.Current.Id.Value, HasThumb: thumbnail is not null);
        return new(meta, moment, captures, _unknown, thumbnail);
    }

    private SaveMeta SettledMetadata(SaveMeta meta, IReadOnlyDictionary<string, RawSaveSection> sections)
    {
        if (_sections.TryRaw<WorldSaveV1>(sections, meta.Map, out var world))
        {
            using var json = SaveContainer.ParseJson(world.Envelope);
            var data = json.RootElement.GetProperty("data");
            if (data.TryGetProperty("worldHours", out var hours) && data.TryGetProperty("clockOffset", out var offset))
                meta = meta with { WorldHours = hours.GetDouble(), Clock = ((hours.GetDouble() + offset.GetDouble()) % 24 + 24) % 24 };
            if (data.TryGetProperty("seasonTarget", out var season) && Enum.TryParse<Season>(season.GetString(), true, out var value))
                meta = meta with { Season = value };
        }
        if (_sections.TryRaw<ViewSaveData>(sections, meta.Map, out var view))
        {
            using var json = SaveContainer.ParseJson(view.Envelope);
            var data = json.RootElement.GetProperty("data");
            var pose = JsonSerializer.Deserialize(data.GetProperty("pose"), SaveSummaryJsonContext.Strict.Pose);
            var mode = JsonSerializer.Deserialize(data.GetProperty("mode"), SaveSummaryJsonContext.Strict.ViewMode);
            var space = data.TryGetProperty("space", out var value) && value.ValueKind != JsonValueKind.Null
                ? JsonSerializer.Deserialize(value, SaveSummaryJsonContext.Strict.NavSpace) : (NavSpace?)null;
            meta = meta with { Pose = pose, Mode = mode, Space = space };
        }
        if (_sections.TryRaw<MapsSave>(sections, meta.Map, out var maps))
        {
            using var json = SaveContainer.ParseJson(maps.Envelope);
            if (json.RootElement.GetProperty("data").TryGetProperty("current", out var current))
                meta = meta with { Map = new(current.GetString() ?? throw new JsonException("Missing current map.")) };
        }
        return meta;
    }

    public Task<LoadResult> NewGame(SlotId? slot, uint? seed = null, WorldPreset? start = null, NewGameBoot? boot = null)
    {
        if (slot is { } selected && !SaveSlots.Auto.Contains(selected)) throw new ArgumentOutOfRangeException(nameof(slot));
        return RestoreDefaultWorld(slot, seed, start, boot);
    }

    private async Task<LoadResult> RestoreDefaultWorld(SlotId? slot, uint? seed, WorldPreset? start, NewGameBoot? boot)
    {
        await _operation.WaitAsync().ConfigureAwait(false);
        try
        {
            await BeginRestoreOperation().ConfigureAwait(false);
            NewGameContext? defaults = null;
            var prepared = await OnFrame(() =>
            {
                var result = _restore!.PrepareDefaultWorld(_options, _sections, _defaultRuleset,
                    slot, seed, start, boot, selected => checked(_slots[selected].MaxGeneration + 1));
                defaults = result.Defaults;
                Active = slot.HasValue ? _options.WriteSlot ?? slot : null; _newLine = slot.HasValue; _line = 0; _playSeconds = 0;
                _authorizedOverwrite = slot.HasValue && Active == slot;
                _thumbnail = null; _unknown = new Dictionary<string, RawSaveSection>();
                return result.Save;
            }).ConfigureAwait(false);
            await PrepareMap(prepared.Document.Meta.Map).ConfigureAwait(false);
            await OnFrame(() => _restore!.RestorePhaseOf(prepared, RestorePhase.Content, [], defaults)).ConfigureAwait(false);
            prepared = await PrepareViewPose(prepared, [], defaults).ConfigureAwait(false);
            var wait = await OnFrame(() =>
            {
                _restore!.RestorePhaseOf(prepared, RestorePhase.Pose, [], defaults);
                return _runtime.WaitDestination(_runtime.Camera.Pose, _runtime.Observer.Space, _runtime.Observer.Mode);
            }).ConfigureAwait(false);
            await wait.ConfigureAwait(false);
            await OnFrame(_restore!.Succeed).ConfigureAwait(false);
            return new(LoadStatus.Loaded, Active, 0, []);
        }
        catch (Exception error) { return await RestoreFailed(slot, error).ConfigureAwait(false); }
        finally { await OnFrame(() => _restore?.End()).ConfigureAwait(false); _operation.Release(); }
    }

    public async Task<LoadResult> Load(SlotId slot)
    {
        if (!SaveSlots.Auto.Contains(slot)) throw new ArgumentOutOfRangeException(nameof(slot));
        await _operation.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_initialized) return new(LoadStatus.Failed, slot, 0, [], "Save service is not initialized.");
            await BeginRestoreOperation().ConfigureAwait(false);
            var scan = await Task.Run(() => _repository!.Scan(slot)).ConfigureAwait(false);
            await OnFrame(() => { _slots[slot] = scan; Publish(null); }).ConfigureAwait(false);
            if (scan.Save is null) return new(scan.Info.Health switch
            {
                SlotHealth.Empty => LoadStatus.Empty,
                SlotHealth.Corrupt or SlotHealth.Newer => LoadStatus.Corrupt,
                _ => LoadStatus.Failed
            }, slot, 0, []);
            return await Restore(scan.Save, _options.WriteSlot ?? slot).ConfigureAwait(false);
        }
        catch (Exception error) { return await RestoreFailed(slot, error).ConfigureAwait(false); }
        finally { await OnFrame(() => _restore?.End()).ConfigureAwait(false); _operation.Release(); }
    }

    public async Task<LoadResult> LoadFile(string path)
    {
        await _operation.WaitAsync().ConfigureAwait(false);
        try
        {
            await BeginRestoreOperation().ConfigureAwait(false);
            var prepared = await Task.Run(() => _repository!.ReadExternal(path)).ConfigureAwait(false);
            return await Restore(prepared, _options.WriteSlot).ConfigureAwait(false);
        }
        catch (SaveFormatException error) when (_restore?.MutationStarted != true)
        { return await RejectExternalFile(error).ConfigureAwait(false); }
        catch (Exception error) { return await RestoreFailed(null, error).ConfigureAwait(false); }
        finally { await OnFrame(() => _restore?.End()).ConfigureAwait(false); _operation.Release(); }
    }

    private async Task<LoadResult> Restore(PreparedSave prepared, SlotId? active)
    {
        var meta = prepared.Document.Meta;
        var repairs = new List<string>();
        string? defaultMapSpawn = null;
        await OnFrame(() =>
        {
            (prepared, defaultMapSpawn) = _restore!.PrepareLoadedWorld(prepared, repairs);
            meta = prepared.Document.Meta;
            Active = active; _line = active == meta.Slot ? meta.Line : 0; _newLine = active != meta.Slot;
            _authorizedOverwrite = false;
            _playSeconds = meta.PlaySeconds; _thumbnail = prepared.Document.Thumbnail; _unknown = prepared.Document.Sections;
        }).ConfigureAwait(false);
        await PrepareMap(meta.Map).ConfigureAwait(false);
        if (defaultMapSpawn is not null)
        {
            meta = await OnFrame(() => meta with { Pose = _runtime.Maps.ResolveSpawn(meta.Map, defaultMapSpawn).ViewPose(meta.Mode) }).ConfigureAwait(false);
            prepared = prepared with { Document = prepared.Document with { Meta = meta } };
        }
        await OnFrame(() => _restore!.RestorePhaseOf(prepared, RestorePhase.Content, repairs)).ConfigureAwait(false);
        prepared = await PrepareViewPose(prepared, repairs).ConfigureAwait(false);
        var wait = await OnFrame(() =>
        {
            _restore!.RestorePhaseOf(prepared, RestorePhase.Pose, repairs);
            return _runtime.WaitDestination(_runtime.Camera.Pose, _runtime.Observer.Space, _runtime.Observer.Mode);
        }).ConfigureAwait(false);
        await wait.ConfigureAwait(false);
        await OnFrame(() =>
        {
            if (defaultMapSpawn is not null && _slots.TryGetValue(meta.Slot, out var slot))
                _slots[meta.Slot] = slot with { Info = slot.Info with { Meta = meta }, Save = prepared };
            _interval = 0; _restore!.Succeed();
        }).ConfigureAwait(false);
        return new(LoadStatus.Loaded, active, meta.Gen, repairs);
    }

    private async Task<PreparedSave> PrepareViewPose(PreparedSave prepared, List<string> repairs, NewGameContext? defaults = null)
    {
        var section = prepared.Sections.FirstOrDefault(item => item.Registration.Section.TypeInfo.Type == typeof(ViewSaveData));
        if (section is null) return prepared;
        section = await OnFrame(() => _restore!.PreparePoseSection(section, repairs, defaults)).ConfigureAwait(false);
        var dto = section.Dto;
        if (dto is not ViewSaveData view) throw new InvalidDataException("The view section must use the shared view DTO.");
        var preparing = await OnFrame(() => (_runtime.PreparePose ?? throw new InvalidOperationException("View restoration requires terrain preparation."))(view.Pose)).ConfigureAwait(false);
        var pose = await preparing.ConfigureAwait(false);
        if (pose != view.Pose) repairs.Add("view.pose");
        return prepared with
        {
            Document = prepared.Document with { Meta = prepared.Document.Meta with { Pose = pose } },
            Sections = prepared.Sections.Select(item => ReferenceEquals(item.Registration, section.Registration) && item.Map == section.Map ? section with { Dto = view with { Pose = pose } } : item).ToArray()
        };
    }

    private bool Restoring => _restore?.IsActive == true;
    private bool RestoreFaulted => _restore?.Blocked == true || _initialized && _runtime.WorldRestore.RestoreFaulted;

    private async Task BeginRestoreOperation()
    {
        var previousMap = await OnFrame(() =>
        {
            if (!_initialized) throw new InvalidOperationException("Save service is not initialized.");
            _request = null; _flush = null; _flushOperation = null;
            return _restore!.Begin();
        }).ConfigureAwait(false);
        // A new restore must not race the previous entry's pending map or pose work.
        // Its Arrived/Failed outcome belongs to the old request; either terminal result allows reconstruction.
        if (previousMap is { IsCompleted: false }) await Task.WhenAny(previousMap).ConfigureAwait(false);
    }

    private Task<LoadResult> RejectExternalFile([FailureHandler("SYS-SAVE-FORMAT")] SaveFormatException error) => OnFrame(() =>
    {
        _restore!.RetainFailure(error);
        var result = new LoadResult(LoadStatus.Corrupt, null, 0, [], _restore.Error!.Message);
        _runtime.Diagnostics.Warning("External save rejected before world mutation.", _restore.Error);
        return result;
    });

    private Task<LoadResult> RestoreFailed(SlotId? slot, [FailureHandler("SYS-SAVE-FLOW")] Exception error) =>
        _runtime is null ? Task.FromException<LoadResult>(error) : OnFrame(() =>
    {
        if (_restore?.MutationStarted != true)
        {
            _restore?.RetainFailure(error);
            _runtime.Diagnostics.Error("Save restoration failed before world mutation.", error);
            return new LoadResult(LoadStatus.Failed, slot, 0, [], error.Message);
        }
        try { _restore.MarkFault(error); }
        catch (Exception abortError) { _restore.RetainAbortFailure(error, abortError); }
        _runtime.Diagnostics.Error("Save restoration failed; the world is blocked until Load or NewGame.", _restore.Error!);
        return new LoadResult(LoadStatus.Failed, slot, 0, [], "save.error.restore_world: " + _restore.Error!.Message);
    });

    private void PublishRestoreFault()
    {
        _request = null;
        Publish(new(SaveStatus.Failed, Active, 0, "save.error.restore_world"));
    }

    private void PublishRestoreSuccess()
    {
        if (State.ErrorKey == "save.error.restore_world") State = State with { Last = null, ErrorKey = null };
        Publish(null);
    }

    private async Task PrepareMap(MapId map)
    {
        var preparation = await OnFrame(() => _runtime.Maps.Prepare(map)).ConfigureAwait(false);
        await preparation.ConfigureAwait(false);
    }

    public async Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout)
    {
        if (reason is not (SaveReason.Quit or SaveReason.ToTitle) || timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(reason));
        if (!_initialized || !Enabled) return new(SaveStatus.Disabled, Active, 0);
        using var deadline = new CancellationTokenSource(timeout);
        if (_runtime.World.Instance == 0)
        {
            if (_writing is null) return new(SaveStatus.Deferred, Active, 0);
            try
            {
                var written = await _writing.WaitAsync(deadline.Token).ConfigureAwait(false);
                return written.Status == SaveStatus.Failed ? written : new(SaveStatus.Deferred, Active, 0);
            }
            catch (OperationCanceledException)
            {
                return await OnFrame(() => { var result = new SaveResult(SaveStatus.Failed, Active, 0, "save.error.timeout"); Publish(result); return result; }).ConfigureAwait(false);
            }
        }
        if (RestoreFaulted) return new(SaveStatus.Deferred, Active, 0, "save.error.restore_world");
        if (Active is null) return new(SaveStatus.Deferred, Active, 0);
        var transition = _runtime.Maps.Transition;
        if (transition is not null)
        {
            try { if (await transition.Done.WaitAsync(deadline.Token).ConfigureAwait(false) != MapTravelResult.Arrived) return new(SaveStatus.Deferred, Active, 0); }
            catch (OperationCanceledException) { return new(SaveStatus.Deferred, Active, 0); }
        }
        // A timed-out write keeps running; retry shares it, rather than racing a second file writer.
        if (_flush is null || _flush.IsCompleted && _flush.Result.Status != SaveStatus.Saved)
        {
            _flush = StartSave(reason, forced: true);
            _flushOperation = _writingOperation;
        }
        try { return await _flushOperation!.WaitForFlush(_flush, reason, Active, deadline.Token).ConfigureAwait(false); }
        catch (OperationCanceledException)
        {
            return await OnFrame(() => { var result = new SaveResult(SaveStatus.Failed, Active, 0, "save.error.timeout"); Publish(result); return result; }).ConfigureAwait(false);
        }
    }

    public byte[]? Thumbnail(SlotId slot) => _slots.TryGetValue(slot, out var value) ? value.Save?.Document.Thumbnail?.ToArray() : null;
    public void WarmSerialization()
    {
        var map = _runtime.Maps.Maps.First(m => m.Id == _runtime.Maps.Default);
        foreach (var section in _sections.Registered)
        {
            var dto = section.Section.CreateDefault(new(1, map.Id, map.World.Default,
                _runtime.Ruleset.Current.DefaultViewMode));
            JsonSerializer.SerializeToUtf8Bytes(dto, section.Section.TypeInfo);
        }
    }

    private void Publish(SaveResult? result)
    {
        _slotInfos = SaveSlots.Auto.Select(s => _slots.TryGetValue(s, out var info) ? info.Info : new(s, SlotKind.Auto, SlotHealth.Empty, null, 0)).ToArray();
        var savedAt = Active is { } active && _slots.TryGetValue(active, out var selected) ? selected.Info.Meta?.SavedAtUnix : null;
        State = new(Active, result?.Status ?? State.Last, result?.Status == SaveStatus.Failed ? result.Error : result?.Status == SaveStatus.Saved ? null : State.ErrorKey,
            savedAt, _repository?.ReadOnly ?? ReadOnlyReason.None);
        SlotsChanged?.Invoke();
    }
    public Task<T> OnFrame<T>(Func<T> function)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_frameGate)
        {
            if (_stopping) return Task.FromCanceled<T>(_lifetime);
            _frames.Enqueue(() =>
            {
                lock (_frameGate)
                {
                    if (_stopping) { completion.TrySetCanceled(_lifetime); return; }
                    try { completion.SetResult(function()); }
                    catch (Exception error)
                    {
                        if (Restoring && _restore?.MutationStarted == true)
                        {
                            try { _restore!.MarkFault(error); }
                            catch (Exception abortError) { _restore!.RetainAbortFailure(error, abortError); }
                        }
                        completion.SetException(error);
                    }
                }
            });
        }
        return completion.Task;
    }
    public Task OnFrame(Action action) => OnFrame(() => { action(); return true; });
    public void StopNewWork()
    {
        lock (_frameGate)
        {
            if (_stopping) return;
            _stopping = true;
            try { _stop.Cancel(); }
            finally { while (_frames.TryDequeue(out var action)) action(); }
        }
    }
    public ValueTask DisposeAsync() => new(_disposal ??= DrainAndRelease());
    private async Task DrainAndRelease()
    {
        Exception? failure = null;
        try { StopNewWork(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        try { _restore?.Dispose(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        var pending = _writes.Cast<Task>().ToList();
        if (_initialization is { } initialization) pending.Add(initialization);
        foreach (var task in pending)
        {
            try { await task.ConfigureAwait(false); }
            catch (OperationCanceledException error) when (_stop.IsCancellationRequested && error.CancellationToken == _lifetime) { }
            catch (Exception error) { failure = RetainedFailure.Append(failure, task.Exception ?? error); }
        }
        try { _repository?.Dispose(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        finally { _repository = null; }
        try { _stop.Dispose(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
