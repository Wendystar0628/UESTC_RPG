using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;

namespace Campus.Kernel.Save;

public sealed record SaveRuntimeOptions(string Directory, string Build, bool Enabled, SlotId? WriteSlot,
    int? SaveAtFrame, uint? WeatherSeed);

/// <summary>Engine adapters are supplied once at boot. None are called on the disk worker.</summary>
public sealed record SaveRuntimeBindings(IWorldState World, IMapService Maps, ICameraRig Camera,
    IObserverState Observer, IRuleset Ruleset, IAtlasService Atlas, IAppLoadingPresentation Loading,
    IAppFlow AppFlow, Action<RulesetId> SelectRuleset, IWorldRestore WorldRestore,
    Func<Pose, NavSpace?, ViewMode, Task> WaitDestination, Func<Task<byte[]?>> CaptureThumbnail, Func<long> UnixTime,
    Func<Pose, Task<Pose>>? PreparePose = null);

[KernelService(typeof(SaveService), Owner = "WP-50")]
[KernelService(typeof(ISaveService), Owner = "WP-50")]
public sealed partial class SaveService : ISaveService, IDisposable
{
    private static readonly SlotId[] AutoSlots = [new("s1"), new("s2"), new("s3")];
    private readonly ConcurrentQueue<Action> _frames = new();
    private readonly SemaphoreSlim _operation = new(1, 1);
    private readonly Dictionary<SlotId, SlotSnapshot> _slots = [];
    private SaveRuntimeOptions _options = null!;
    private SaveRuntimeBindings _runtime = null!;
    private SaveSections _sections = null!;
    private SaveRepository? _repository;
    private IReadOnlyDictionary<string, RawSaveSection> _unknown = new Dictionary<string, RawSaveSection>();
    private PreparedSave? _pendingPose;
    private Task<SaveResult>? _writing;
    private Task<SaveResult>? _flush;
    private SaveReason? _request;
    private double _requestAge, _interval, _playSeconds;
    private ulong _line;
    private bool _newLine, _restoring, _initialized, _disposed, _authorizedOverwrite;
    private bool _restoreMutationStarted, _restoreBlocked, _restoreAbortApplied;
    private IDisposable? _restoreInput;
    private int _frame;
    private byte[]? _thumbnail;
    private RulesetId _defaultRuleset;
    private IReadOnlyList<SlotInfo> _slotInfos = AutoSlots.Select(s => new SlotInfo(s, SlotKind.Auto, SlotHealth.Empty, null, 0)).ToArray();

    public bool Enabled => _initialized && _repository?.Enabled == true;
    public IReadOnlyList<SlotInfo> Slots => _slotInfos;
    public SlotId? Continue => _slotInfos.Where(s => s.Health == SlotHealth.Ok)
        .OrderByDescending(s => s.Meta!.SavedAtUnix).ThenBy(s => s.Slot.Value, StringComparer.Ordinal).Select(s => (SlotId?)s.Slot).FirstOrDefault();
    public SlotId? Active { get; private set; }
    public bool Busy => _writing is { IsCompleted: false };
    public SaveState State { get; private set; } = new(null, null, null, null, ReadOnlyReason.None);
    public bool HasPendingPose => _pendingPose is not null;
    public event Action? SlotsChanged;

    public async Task Initialize(SaveRuntimeOptions options, SaveRuntimeBindings bindings, SaveSections sections)
    {
        if (_initialized) throw new InvalidOperationException("Save service was initialized twice.");
        _options = options; _runtime = bindings; _sections = sections;
        _defaultRuleset = bindings.Ruleset.Current.Id;
        var data = await Task.Run(() =>
        {
            var repository = new SaveRepository(options.Directory, options.Enabled, sections);
            try { return (Repository: repository, Slots: AutoSlots.Select(repository.Scan).ToArray()); }
            catch { repository.Dispose(); throw; }
        }).ConfigureAwait(false);
        await OnFrame(() =>
        {
            _repository = data.Repository;
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
        if (!_initialized || _disposed) return;
        _frame++;
        var dt = Math.Clamp(double.IsFinite(delta) ? delta : 0, 0, .1);
        if (_request.HasValue) _requestAge += dt;
        if (Playing && !_restoring && !RestoreFaulted)
        {
            _playSeconds += dt; _interval += dt;
            if (_flush is { IsCompleted: true }) _flush = null;
            if (_interval >= 300 && _request is null) Request(SaveReason.Interval);
        }
        if (_options.SaveAtFrame is { } at && _frame >= at && !_saveAtDone && Safe)
        { _saveAtDone = true; _ = SaveNow(SaveReason.Test); }
        if (_request is { } reason && !Busy && (Safe || _requestAge >= 60) && !_restoring &&
            Playing && !HasPendingPose)
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
            if (RestoreFaulted || _restoring || !Playing || _runtime.Loading.Active || MapTransitioning) return false;
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
        if (!_initialized || !Enabled || Active is null || _restoring || RestoreFaulted || HasPendingPose ||
            !Playing || MapTransitioning) return;
        if (_request is null) _requestAge = 0;
        _request = reason;
    }

    public Task<SaveResult> SaveNow(SaveReason reason)
    {
        if (reason is not (SaveReason.NewGame or SaveReason.Test)) throw new ArgumentOutOfRangeException(nameof(reason));
        return StartSave(reason, forced: true);
    }

    private Task<SaveResult> StartSave(SaveReason reason, bool forced)
    {
        var task = SaveOperation(reason, forced);
        _writing = task;
        return task;
    }

    private async Task<SaveResult> SaveOperation(SaveReason reason, bool forced)
    {
        await _operation.WaitAsync().ConfigureAwait(false);
        try
        {
            if (await OnFrame(() => RestoreFaulted).ConfigureAwait(false))
                return new(SaveStatus.Failed, Active, 0, "save.error.restore_world");
            var disabled = await OnFrame(() => !Enabled || Active is null || HasPendingPose || _restoring).ConfigureAwait(false);
            if (disabled) return new(Enabled ? SaveStatus.Deferred : SaveStatus.Disabled, Active, 0);
            var active = Active!.Value;
            var current = await Task.Run(() => _repository!.Scan(active)).ConfigureAwait(false);
            await OnFrame(() => _slots[active] = current).ConfigureAwait(false);
            if (!_authorizedOverwrite && current.Info.Health == SlotHealth.Corrupt)
                return new(SaveStatus.Disabled, active, 0, "save.error.read_only_slot");
            byte[]? thumbnail = _thumbnail;
            if (reason is SaveReason.Quit or SaveReason.ToTitle or SaveReason.NewGame)
            {
                var capture = await OnFrame(_runtime.CaptureThumbnail).ConfigureAwait(false);
                thumbnail = await capture.ConfigureAwait(false) ?? thumbnail;
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
                saved = await Task.Run(() => _repository!.Write(document with { Meta = finalMeta }, replaceUnusable: _authorizedOverwrite)).ConfigureAwait(false);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or SaveFormatException or JsonException or ArgumentException or InvalidOperationException)
            {
                return await OnFrame(() =>
                {
                    var result = new SaveResult(SaveStatus.Failed, active, snapshot.Meta.Gen, ErrorKey(error));
                    Publish(result); return result;
                }).ConfigureAwait(false);
            }
            return await OnFrame(() =>
            {
                _slots[active] = saved; _line = saved.Info.Meta!.Line; _newLine = false; _authorizedOverwrite = false;
                _thumbnail = saved.Save!.Document.Thumbnail; _unknown = saved.Save.Document.Sections;
                _interval = 0;
                var result = new SaveResult(SaveStatus.Saved, active, saved.Info.Meta.Gen);
                Publish(result); return result;
            }).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            return await OnFrame(() => { var result = new SaveResult(SaveStatus.Failed, Active, 0, ErrorKey(error)); Publish(result); return result; }).ConfigureAwait(false);
        }
        finally { _operation.Release(); }
    }

    private sealed record Snapshot(SaveMeta Meta, WorldSaveMoment Moment, IReadOnlyList<CapturedSection> Sections,
        IReadOnlyDictionary<string, RawSaveSection> Unknown, byte[]? Thumbnail);
    private Snapshot? Capture(SaveReason reason, bool forced, ulong generation, byte[]? thumbnail)
    {
        if (RestoreFaulted) throw new InvalidOperationException("save.error.restore_world");
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
        var meta = new SaveMeta(1, _options.Build, Active!.Value, SlotKind.Auto, generation, _newLine ? generation : _line,
            _playSeconds, map, world.WorldHours, world.Clock, world.SeasonTarget, world.Weather.Kind,
            observer.Mode, _runtime.Camera.Pose, observer.Space, region, region is null ? null : "place." + region,
            _runtime.UnixTime(), reason, _runtime.Ruleset.Current.Id.Value, HasThumb: thumbnail is not null);
        return new(meta, moment, captures, _unknown, thumbnail);
    }

    private static SaveMeta SettledMetadata(SaveMeta meta, IReadOnlyDictionary<string, RawSaveSection> sections)
    {
        if (sections.TryGetValue("world", out var world))
        {
            using var json = SaveContainer.ParseJson(world.Envelope);
            var data = json.RootElement.GetProperty("data");
            if (data.TryGetProperty("worldHours", out var hours) && data.TryGetProperty("clockOffset", out var offset))
                meta = meta with { WorldHours = hours.GetDouble(), Clock = ((hours.GetDouble() + offset.GetDouble()) % 24 + 24) % 24 };
            if (data.TryGetProperty("seasonTarget", out var season) && Enum.TryParse<Season>(season.GetString(), true, out var value))
                meta = meta with { Season = value };
        }
        if (sections.TryGetValue("view", out var view))
        {
            using var json = SaveContainer.ParseJson(view.Envelope);
            var data = json.RootElement.GetProperty("data");
            var pose = JsonSerializer.Deserialize(data.GetProperty("pose"), SaveSummaryJsonContext.Strict.Pose);
            var mode = JsonSerializer.Deserialize(data.GetProperty("mode"), SaveSummaryJsonContext.Strict.ViewMode);
            var space = data.TryGetProperty("space", out var value) && value.ValueKind != JsonValueKind.Null
                ? JsonSerializer.Deserialize(value, SaveSummaryJsonContext.Strict.NavSpace) : (NavSpace?)null;
            meta = meta with { Pose = pose, Mode = mode, Space = space };
        }
        if (sections.TryGetValue("maps", out var maps))
        {
            using var json = SaveContainer.ParseJson(maps.Envelope);
            if (json.RootElement.GetProperty("data").TryGetProperty("current", out var current))
                meta = meta with { Map = new(current.GetString() ?? throw new JsonException("Missing current map.")) };
        }
        if (sections.TryGetValue("map:" + meta.Map.Value + "/weather", out var weather))
        {
            using var json = SaveContainer.ParseJson(weather.Envelope);
            var kind = json.RootElement.GetProperty("data").GetProperty("selection").GetProperty("kind");
            meta = meta with { Weather = JsonSerializer.Deserialize(kind, SaveSummaryJsonContext.Strict.WeatherKind) };
        }
        return meta;
    }

    public async Task<LoadResult> Load(SlotId slot, LoadScope scope = LoadScope.Full)
    {
        if (!AutoSlots.Contains(slot) || !Enum.IsDefined(scope)) throw new ArgumentOutOfRangeException(nameof(slot));
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
                SlotHealth.Corrupt => LoadStatus.Corrupt,
                _ => LoadStatus.Failed
            }, slot, 0, []);
            return await Restore(scan.Save, _options.WriteSlot ?? slot, scope).ConfigureAwait(false);
        }
        catch (Exception error) { return await RestoreFailed(slot, error).ConfigureAwait(false); }
        finally { await OnFrame(EndRestoreOperation).ConfigureAwait(false); _operation.Release(); }
    }

    public async Task<LoadResult> LoadFile(string path)
    {
        await _operation.WaitAsync().ConfigureAwait(false);
        try
        {
            await BeginRestoreOperation().ConfigureAwait(false);
            var prepared = await Task.Run(() => _repository!.ReadExternal(path)).ConfigureAwait(false);
            return await Restore(prepared, _options.WriteSlot, LoadScope.Full).ConfigureAwait(false);
        }
        catch (SaveFormatException error) when (!_restoreMutationStarted)
        { return new(LoadStatus.Corrupt, null, 0, [], error.Message); }
        catch (Exception error) { return await RestoreFailed(null, error).ConfigureAwait(false); }
        finally { await OnFrame(EndRestoreOperation).ConfigureAwait(false); _operation.Release(); }
    }

    private async Task<LoadResult> Restore(PreparedSave prepared, SlotId? active, LoadScope scope)
    {
        var meta = prepared.Document.Meta;
        var repairs = new List<string>();
        string? fallbackSpawn = null;
        await OnFrame(() =>
        {
            _restoreMutationStarted = true;
            _runtime.SelectRuleset(new(meta.Ruleset));
            if (!_runtime.Maps.Maps.Any(m => m.Id == meta.Map))
            {
                var fallback = _runtime.Maps.Maps.First(m => m.Id == _runtime.Maps.Default);
                fallbackSpawn = fallback.DefaultSpawn;
                repairs.Add("Missing map replaced with the default map: " + meta.Map.Value);
                meta = meta with { Map = fallback.Id, Mode = _runtime.Ruleset.Current.DefaultViewMode, Space = null };
                prepared = prepared with
                {
                    Document = prepared.Document with { Meta = meta },
                    Sections = prepared.Sections.Select(s => s.Registration.Attribute.Scope == SaveScope.Global
                        ? s with { Map = fallback.Id, Dto = s.Registration.Attribute.Phase == RestorePhase.Pose || s.Registration.Attribute.Id == "maps" ? null : s.Dto }
                        : s).ToArray()
                };
            }
            RestorePhaseOf(prepared, RestorePhase.World, repairs);
            _runtime.WorldRestore.CompleteRestore();
            Active = active; _line = active == meta.Slot ? meta.Line : 0; _newLine = active != meta.Slot;
            _authorizedOverwrite = false;
            _playSeconds = meta.PlaySeconds; _thumbnail = prepared.Document.Thumbnail; _unknown = prepared.Document.Sections;
            _pendingPose = null;
        }).ConfigureAwait(false);
        await Travel(meta.Map, fallbackSpawn is null && scope == LoadScope.Full ? meta.Pose : null,
            scope == LoadScope.Full ? meta.Mode : null, MapTravelCause.LoadSave, fallbackSpawn).ConfigureAwait(false);
        if (fallbackSpawn is not null)
        {
            meta = await OnFrame(() => meta with { Pose = _runtime.Camera.Pose }).ConfigureAwait(false);
            prepared = prepared with { Document = prepared.Document with { Meta = meta } };
        }
        await OnFrame(() => RestorePhaseOf(prepared, RestorePhase.Content, repairs)).ConfigureAwait(false);
        if (scope == LoadScope.Full)
        {
            prepared = await PrepareViewPose(prepared, repairs).ConfigureAwait(false);
            var wait = await OnFrame(() =>
            {
                RestorePhaseOf(prepared, RestorePhase.Pose, repairs);
                return _runtime.WaitDestination(_runtime.Camera.Pose, _runtime.Observer.Space, _runtime.Observer.Mode);
            }).ConfigureAwait(false);
            await wait.ConfigureAwait(false);
        }
        else await OnFrame(() => _pendingPose = prepared).ConfigureAwait(false);
        await OnFrame(() =>
        {
            if (fallbackSpawn is not null && _slots.TryGetValue(meta.Slot, out var slot))
                _slots[meta.Slot] = slot with { Info = slot.Info with { Meta = meta }, Save = prepared };
            _interval = 0; RestoreSucceeded();
        }).ConfigureAwait(false);
        return new(LoadStatus.Loaded, active, meta.Gen, repairs);
    }

    private void RestorePhaseOf(PreparedSave prepared, RestorePhase phase, List<string> repairs, NewGameContext? defaults = null)
    {
        foreach (var item in prepared.Sections.Where(s => s.Registration.Attribute.Phase == phase))
        {
            var context = new RestoreContext { Map = item.Map, FromVersion = item.FromVersion, Missing = item.Dto is null };
            if (item.Repair is not null) context.Repair(item.Repair);
            var map = _runtime.Maps.Maps.FirstOrDefault(m => m.Id == item.Map);
            if (map is null && item.Registration.Attribute.Scope == SaveScope.PerMap)
            { repairs.Add("Removed section for missing map: " + item.Key); continue; }
            map ??= _runtime.Maps.Maps.First(m => m.Id == _runtime.Maps.Default);
            var dto = item.Dto ?? item.Registration.Section.CreateDefault(defaults ??
                new(_runtime.World.WeatherSeed, item.Map, map.World.Default,
                    _runtime.Ruleset.Current.DefaultViewMode));
            item.Registration.Section.Restore(dto, context);
            repairs.AddRange(context.Repairs);
        }
    }

    private async Task<PreparedSave> PrepareViewPose(PreparedSave prepared, List<string> repairs, NewGameContext? defaults = null)
    {
        var section = prepared.Sections.FirstOrDefault(item => item.Registration.Attribute.Id == "view");
        if (section is null) return prepared;
        var dto = await OnFrame(() => section.Dto ?? section.Registration.Section.CreateDefault(defaults ??
            new(_runtime.World.WeatherSeed, section.Map,
                _runtime.Maps.Maps.First(map => map.Id == section.Map).World.Default,
                _runtime.Ruleset.Current.DefaultViewMode))).ConfigureAwait(false);
        if (dto is not ViewSaveData view) throw new InvalidDataException("The view section must use the shared view DTO.");
        var preparing = await OnFrame(() => (_runtime.PreparePose ?? throw new InvalidOperationException("View restoration requires terrain preparation."))(view.Pose)).ConfigureAwait(false);
        var pose = await preparing.ConfigureAwait(false);
        if (pose != view.Pose) repairs.Add("view.pose");
        return prepared with
        {
            Document = prepared.Document with { Meta = prepared.Document.Meta with { Pose = pose } },
            Sections = prepared.Sections.Select(item => ReferenceEquals(item, section) ? item with { Dto = view with { Pose = pose } } : item).ToArray()
        };
    }

    public async Task CompleteResume()
    {
        await _operation.WaitAsync().ConfigureAwait(false);
        try
        {
            if (RestoreFaulted) throw new InvalidOperationException("A faulted world requires Load or NewGame.");
            var prepared = _pendingPose;
            if (prepared is null) return;
            await BeginRestoreOperation().ConfigureAwait(false);
            prepared = await PrepareViewPose(prepared, []).ConfigureAwait(false);
            var wait = await OnFrame(() =>
            {
                _restoreMutationStarted = true;
                RestorePhaseOf(prepared, RestorePhase.Pose, []);
                return _runtime.WaitDestination(_runtime.Camera.Pose, _runtime.Observer.Space, _runtime.Observer.Mode);
            }).ConfigureAwait(false);
            await wait.ConfigureAwait(false);
            await OnFrame(() => { _pendingPose = null; RestoreSucceeded(); }).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            await RestoreFailed(Active, error).ConfigureAwait(false);
            throw;
        }
        finally { await OnFrame(EndRestoreOperation).ConfigureAwait(false); _operation.Release(); }
    }

    private bool RestoreFaulted => _restoreBlocked || _initialized && _runtime.WorldRestore.RestoreFaulted;

    private async Task BeginRestoreOperation()
    {
        var previousMap = await OnFrame(() =>
        {
            if (!_initialized) throw new InvalidOperationException("Save service is not initialized.");
            _restoring = true; _request = null; _restoreMutationStarted = false; _restoreAbortApplied = false; _flush = null;
            _restoreInput ??= _runtime.Camera.LockInput(InputLockReason.MapTravel);
            return _runtime.Maps.Transition?.Done;
        }).ConfigureAwait(false);
        // A new restore must not race the previous entry's pending map or pose work.
        // Its Arrived/Failed outcome belongs to the old request; either terminal result allows reconstruction.
        if (previousMap is { IsCompleted: false }) await Task.WhenAny(previousMap).ConfigureAwait(false);
    }

    private Task<LoadResult> RestoreFailed(SlotId? slot, Exception error) => OnFrame(() =>
    {
        if (!_restoreMutationStarted) return new LoadResult(LoadStatus.Failed, slot, 0, [], error.Message);
        var detail = error.Message;
        try { MarkRestoreFault(); }
        catch (Exception abortError) { detail += "; abort failed: " + abortError.Message; }
        return new LoadResult(LoadStatus.Failed, slot, 0, [], "save.error.restore_world: " + detail);
    });

    private void MarkRestoreFault()
    {
        if (_restoreAbortApplied) return;
        _restoreBlocked = true; _request = null; _pendingPose = null; _restoreAbortApplied = true;
        // This runs inside the failing Save-frame callback, before the next World tick can observe pending state.
        try { _runtime.WorldRestore.AbortRestore(); }
        finally { Publish(new(SaveStatus.Failed, Active, 0, "save.error.restore_world")); }
    }

    private void RestoreSucceeded()
    {
        if (_runtime.WorldRestore.RestoreFaulted)
            throw new InvalidOperationException("World restore has not committed successfully.");
        _restoreBlocked = false;
        if (State.ErrorKey == "save.error.restore_world") State = State with { Last = null, ErrorKey = null };
        Publish(null);
    }

    private void EndRestoreOperation()
    {
        _restoring = false; _restoreMutationStarted = false;
        if (RestoreFaulted) return;
        _restoreInput?.Dispose(); _restoreInput = null;
    }

    private async Task Travel(MapId map, Pose? pose, ViewMode? mode, MapTravelCause cause, string? spawn = null)
    {
        var transition = await OnFrame(() => _runtime.Maps.Travel(new(map, Spawn: spawn, Pose: pose, Mode: mode, Cause: cause))).ConfigureAwait(false);
        if (await transition.Done.ConfigureAwait(false) != MapTravelResult.Arrived) throw new InvalidOperationException("Save map could not be restored.");
    }

    public async Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout)
    {
        if (reason is not (SaveReason.Quit or SaveReason.ToTitle) || timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(reason));
        if (!_initialized || !Enabled) return new(SaveStatus.Disabled, Active, 0);
        if (RestoreFaulted) return new(SaveStatus.Deferred, Active, 0, "save.error.restore_world");
        if (Active is null || HasPendingPose) return new(SaveStatus.Deferred, Active, 0);
        using var deadline = new CancellationTokenSource(timeout);
        var transition = _runtime.Maps.Transition;
        if (transition is not null)
        {
            try { if (await transition.Done.WaitAsync(deadline.Token).ConfigureAwait(false) != MapTravelResult.Arrived) return new(SaveStatus.Deferred, Active, 0); }
            catch (OperationCanceledException) { return new(SaveStatus.Deferred, Active, 0); }
        }
        // A timed-out write keeps running; retry shares it, rather than racing a second file writer.
        if (_flush is null || _flush.IsCompleted && _flush.Result.Status != SaveStatus.Saved)
            _flush = StartSave(reason, forced: true);
        try { return await _flush.WaitAsync(deadline.Token).ConfigureAwait(false); }
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
        _slotInfos = AutoSlots.Select(s => _slots.TryGetValue(s, out var info) ? info.Info : new(s, SlotKind.Auto, SlotHealth.Empty, null, 0)).ToArray();
        var savedAt = Active is { } active && _slots.TryGetValue(active, out var selected) ? selected.Info.Meta?.SavedAtUnix : null;
        State = new(Active, result?.Status ?? State.Last, result?.Status == SaveStatus.Failed ? result.Error : result?.Status == SaveStatus.Saved ? null : State.ErrorKey,
            savedAt, _repository?.ReadOnly ?? ReadOnlyReason.None);
        SlotsChanged?.Invoke();
    }
    private static string ErrorKey(Exception error) => error switch
    {
        UnauthorizedAccessException => "save.error.permission",
        IOException io when (io.HResult & 0xffff) is 39 or 112 => "save.error.disk_full",
        IOException => "save.error.io",
        SaveFormatException => "save.error.verification",
        _ => "save.error.snapshot"
    };
    private static uint NewSeed() => System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(RandomNumberGenerator.GetBytes(4));

    public Task<T> OnFrame<T>(Func<T> function)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _frames.Enqueue(() =>
        {
            try { completion.SetResult(function()); }
            catch (Exception error)
            {
                if (_restoring && _restoreMutationStarted)
                {
                    try { MarkRestoreFault(); }
                    catch (Exception abortError) { error = new AggregateException(error, abortError); }
                }
                completion.SetException(error);
            }
        });
        return completion.Task;
    }
    public Task OnFrame(Action action) => OnFrame(() => { action(); return true; });
    public void Dispose()
    {
        _disposed = true;
        _restoreInput?.Dispose(); _restoreInput = null;
        // If the application deliberately exits during a write, retain the process lock until OS teardown.
        if (!Busy) _repository?.Dispose();
    }
}
