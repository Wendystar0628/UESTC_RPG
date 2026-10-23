using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;
using Campus.Kernel.Diagnostics;

namespace Campus.Kernel.Save;

/// <summary>Owns the current world's save line, snapshots and writes; disk transactions use SaveWriteOperation.</summary>
internal sealed class SaveWriting(SaveService owner)
{
    private readonly SaveService _owner = owner;
    private IReadOnlyDictionary<string, RawSaveSection> _unknown = new Dictionary<string, RawSaveSection>();
    private Task? _work;
    private Task<SaveResult>? _writing, _flush;
    private SaveWriteOperation? _writingOperation, _flushOperation;
    private ulong _line;
    private bool _newLine, _authorizedOverwrite;
    private byte[]? _thumbnail;
    internal bool Busy => _work is { IsCompleted: false };

    internal void BeginRestore() { _flush = null; _flushOperation = null; }
    internal void Tick()
    {
        if (_flush is { IsCompletedSuccessfully: true }) { _flush = null; _flushOperation = null; }
    }
    internal void NewWorld(SlotId? slot)
    {
        _owner.Active = slot.HasValue ? _owner.Options.WriteSlot ?? slot : null;
        _newLine = slot.HasValue; _line = 0;
        _authorizedOverwrite = slot.HasValue && _owner.Active == slot;
        _thumbnail = null; _unknown = new Dictionary<string, RawSaveSection>();
        _owner.Schedule.Restored(0);
    }
    internal void LoadedWorld(PreparedSave prepared, SlotId? active)
    {
        var meta = prepared.Document.Meta;
        _owner.Active = active; _line = active == meta.Slot ? meta.Line : 0; _newLine = active != meta.Slot;
        _authorizedOverwrite = false;
        _thumbnail = prepared.Document.Thumbnail; _unknown = prepared.Document.Sections;
        _owner.Schedule.Restored(meta.PlaySeconds);
    }
    internal Task<SaveResult> SaveNow(SaveReason reason)
    {
        ObjectDisposedException.ThrowIf(_owner.Stopping, this);
        if (reason is not (SaveReason.NewGame or SaveReason.Test or SaveReason.Manual)) throw new ArgumentOutOfRangeException(nameof(reason));
        if (_owner.Initialized && _owner.Runtime.World.Instance == 0)
            return Task.FromResult(new SaveResult(SaveStatus.Deferred, _owner.Active, 0,
                _owner.Loading.RestoreFaulted ? SaveErrorKeys.RestoreWorld : null));
        return StartSave(reason, forced: true);
    }

    internal Task<SaveResult> StartSave(SaveReason reason, bool forced)
    {
        ObjectDisposedException.ThrowIf(_owner.Stopping, this);
        var operation = new SaveWriteOperation(_owner.Active);
        var instance = _owner.Initialized ? _owner.Runtime.World.Instance : 0;
        _writingOperation = operation;
        return _writing = _owner.Track(() => RunMutation(() => SaveOperation(operation, reason, forced, instance), instance));
    }

    private async Task<T> RunMutation<T>(Func<Task<T>> start, long? instance = null) where T : class
    {
        var work = start();
        _work = work;
        Exception? failure = null;
        T? result = null;
        try { await _owner.OnFrame(() => _owner.Publish(null)).ConfigureAwait(false); }
        catch (OperationCanceledException error) when (_owner.Stopping && error.CancellationToken == _owner.Lifetime) { }
        catch (Exception error) { failure = error; }
        try { result = await work.ConfigureAwait(false); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        // Busy reads the real operation Task: its finally has released the gate before this event.
        try
        {
            await _owner.OnFrame(() =>
            {
                var sameWorld = instance is null || !_owner.Initialized || _owner.Runtime.World.Instance == 0 || _owner.Runtime.World.Instance == instance;
                _owner.Publish(sameWorld ? result as SaveResult : null);
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException error) when (_owner.Stopping && error.CancellationToken == _owner.Lifetime) { }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        return result!;
    }

    private async Task<SaveResult> SaveOperation(SaveWriteOperation operation, SaveReason reason, bool forced, long instance)
    {
        await _owner.Operation.WaitAsync(_owner.Lifetime).ConfigureAwait(false);
        var slot = operation.Slot;
        ulong generation = 0;
        try
        {
            if (await _owner.OnFrame(() => _owner.Loading.RestoreFaulted).ConfigureAwait(false))
                return new(SaveStatus.Failed, slot, 0, SaveErrorKeys.RestoreWorld);
            var disabled = await _owner.OnFrame(() => !_owner.Enabled || slot is null || _owner.Active != slot ||
                _owner.Runtime.World.Instance == 0 || _owner.Runtime.World.Instance != instance || _owner.Loading.Restoring).ConfigureAwait(false);
            if (disabled) return new(_owner.Enabled ? SaveStatus.Deferred : SaveStatus.Disabled, slot, 0);
            await _owner.OnFrame(_owner.Schedule.Attempted).ConfigureAwait(false);
            var active = slot!.Value;
            var current = await Task.Run(() => _owner.Repository!.Scan(active)).ConfigureAwait(false);
            await _owner.OnFrame(() => _owner.Snapshots[active] = current).ConfigureAwait(false);
            if (current.Info.Health == SlotHealth.Newer && !(_authorizedOverwrite && _newLine && reason == SaveReason.NewGame))
                return new(SaveStatus.Disabled, active, 0, SaveErrorKeys.NewerSlot);
            if (!_authorizedOverwrite && current.Info.Health == SlotHealth.Corrupt)
                return new(SaveStatus.Disabled, active, 0, SaveErrorKeys.ReadOnlySlot);
            byte[]? thumbnail = _thumbnail;
            if (reason is SaveReason.Quit or SaveReason.ToTitle or SaveReason.NewGame or SaveReason.Manual)
            {
                thumbnail = await operation.CaptureThumbnail(_owner.Runtime.Diagnostics, async () =>
                {
                    var capture = await _owner.OnFrame(_owner.Runtime.CaptureThumbnail).ConfigureAwait(false);
                    return await capture.ConfigureAwait(false);
                }, thumbnail).ConfigureAwait(false);
            }
            generation = checked(current.MaxGeneration + 1);
            var snapshot = await _owner.OnFrame(() => Capture(reason, forced, generation, thumbnail)).ConfigureAwait(false);
            if (snapshot is null) return new(SaveStatus.Deferred, active, 0);
            var document = await BuildDocument(snapshot).ConfigureAwait(false);
            var replaceUnusable = _authorizedOverwrite;
            operation.MarkSnapshotDetached();
            var saved = await Task.Run(() => operation.Commit(_owner.Repository!, document, replaceUnusable: replaceUnusable)).ConfigureAwait(false);
            var result = new SaveResult(SaveStatus.Saved, active, document.Meta.Gen);
            return await ApplyWrittenSnapshot(result, () =>
            {
                _owner.Snapshots[active] = saved;
                if (_owner.Runtime.World.Instance != instance) return;
                _line = document.Meta.Line; _newLine = false; _authorizedOverwrite = false;
                _thumbnail = document.Thumbnail; _unknown = document.Sections;
                _owner.Schedule.Saved();
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException error) when (_owner.Stopping && error.CancellationToken == _owner.Lifetime)
        { throw; }
        catch (Exception error)
        {
            return operation.Fail(_owner.Runtime.Diagnostics, error, slot, generation);
        }
        finally { _owner.Operation.Release(); }
    }

    private Task<SaveDocument> BuildDocument(Snapshot snapshot) => Task.Run(() =>
    {
        var raw = _owner.Sections.Serialize(snapshot.Sections, snapshot.Unknown, snapshot.Meta.Map, snapshot.Moment);
        return new SaveDocument(snapshot.Meta, raw, snapshot.Thumbnail);
    });

    internal Task<DeleteResult> Delete(SlotId slot, ulong? generation)
    {
        ObjectDisposedException.ThrowIf(_owner.Stopping, this);
        if (!SaveSlots.Auto.Contains(slot)) throw new ArgumentOutOfRangeException(nameof(slot));
        if (generation == 0) throw new ArgumentOutOfRangeException(nameof(generation));
        if (!CanDelete) return Task.FromResult(new DeleteResult(DeleteStatus.Rejected, slot, generation));
        return _owner.Track(() => RunMutation(() => DeleteOperation(slot, generation)));
    }

    private bool CanDelete => _owner.Enabled && _owner.Runtime.AppFlow.State == AppState.Title &&
        _owner.Runtime.World.Instance == 0 && !_owner.Loading.Restoring;

    private async Task<DeleteResult> DeleteOperation(SlotId slot, ulong? generation)
    {
        await _owner.Operation.WaitAsync(_owner.Lifetime).ConfigureAwait(false);
        try
        {
            var allowed = await _owner.OnFrame(() => CanDelete).ConfigureAwait(false);
            if (!allowed) return new(DeleteStatus.Rejected, slot, generation);
            if (generation is { } selected)
            {
                var current = await Task.Run(() => _owner.Repository!.Scan(slot)).ConfigureAwait(false);
                await _owner.OnFrame(() => _owner.Snapshots[slot] = current).ConfigureAwait(false);
                if (current.Info.Health == SlotHealth.Newer ||
                    current.Info.Entries.SingleOrDefault(entry => entry.Gen == selected)?.Meta?.Reason != SaveReason.Manual)
                    return new(DeleteStatus.Rejected, slot, generation);
            }
            if (!await _owner.OnFrame(() => CanDelete).ConfigureAwait(false)) return new(DeleteStatus.Rejected, slot, generation);
            var snapshot = await Task.Run(() => generation is { } entryGeneration
                ? _owner.Repository!.DeleteEntry(slot, entryGeneration) : _owner.Repository!.DeleteSlot(slot)).ConfigureAwait(false);
            try
            {
                await _owner.OnFrame(() =>
                {
                    _owner.Snapshots[slot] = snapshot;
                    if (generation is null && _owner.Active == slot) _owner.Active = null;
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException error) when (_owner.Stopping && error.CancellationToken == _owner.Lifetime) { }
            return new(DeleteStatus.Deleted, slot, generation);
        }
        catch (OperationCanceledException error) when (_owner.Stopping && error.CancellationToken == _owner.Lifetime) { throw; }
        catch (Exception error) { return DeleteFailed(slot, generation, error); }
        finally { _owner.Operation.Release(); }
    }

    private DeleteResult DeleteFailed(SlotId slot, ulong? generation, [FailureHandler("SYS-SAVE-FLOW")] Exception error)
    {
        var result = new SaveWriteOperation().Fail(_owner.Runtime.Diagnostics, error, slot, generation ?? 0);
        return new(DeleteStatus.Failed, slot, generation, result.Error);
    }

    private async Task<SaveResult> ApplyWrittenSnapshot(SaveResult result, Action apply)
    {
        try { return await _owner.OnFrame(() => { apply(); return result; }).ConfigureAwait(false); }
        catch (OperationCanceledException error) when (_owner.Stopping && error.CancellationToken == _owner.Lifetime)
        { return result; }
    }

    private sealed record Snapshot(SaveMeta Meta, WorldSaveMoment Moment, IReadOnlyList<CapturedSection> Sections,
        IReadOnlyDictionary<string, RawSaveSection> Unknown, byte[]? Thumbnail);
    private Snapshot? Capture(SaveReason reason, bool forced, ulong generation, byte[]? thumbnail)
    {
        if (_owner.Loading.RestoreFaulted) throw new InvalidOperationException(SaveErrorKeys.RestoreWorld);
        if (_owner.Runtime.World.Instance == 0) throw new InvalidOperationException("A world ended before its queued save snapshot.");
        // Never combine the old map's stable maps segment with a new map's observer/weather state.
        if (_owner.Schedule.MapTransitioning ||
            reason == SaveReason.Interval && !_owner.Schedule.Playing)
            return null;
        var map = _owner.Runtime.Maps.Current;
        var moment = _owner.Runtime.WorldRestore.GetSaveMoment();
        var observer = _owner.Runtime.Camera.CaptureSettledObserver(map);
        if (observer.Map != map) throw new InvalidOperationException("Observer snapshot belongs to another map.");
        var visited = _owner.Runtime.Maps.Visits.Select(v => v.Map).Append(map).Distinct().OrderBy(m => m.Value, StringComparer.Ordinal).ToArray();
        var captures = new List<CapturedSection>(_owner.Sections.Registered.Count + visited.Length);
        foreach (var registration in _owner.Sections.Registered)
            foreach (var sectionMap in registration.Attribute.Scope == SaveScope.Global ? [map] : visited)
            {
                var context = new CaptureContext(reason, sectionMap, forced || !_owner.Schedule.Safe) { Moment = moment, Observer = observer };
                captures.Add(new(SaveSections.Key(registration.Attribute, sectionMap), registration, registration.Section.Capture(in context)));
            }
        var view = observer.View;
        var region = _owner.Runtime.Atlas.RegionAt(view.Pose.Position.X, view.Pose.Position.Z);
        double clock = ((moment.WorldHours + moment.ClockOffset) % 24 + 24) % 24;
        var meta = new SaveMeta(SaveContainer.CurrentFormat, _owner.Active!.Value, SlotKind.Auto, generation, _newLine ? generation : _line,
            _owner.Schedule.PlaySeconds, observer.Map, moment.WorldHours, clock, moment.Season, _owner.Runtime.World.Weather.Kind,
            view.Mode, view.Pose, view.Space, region, region is null ? null : "place." + region,
            _owner.Runtime.UnixTime(), reason, _owner.Runtime.Ruleset.Current.Id.Value, HasThumb: thumbnail is not null);
        return new(meta, moment, captures, _unknown, thumbnail);
    }

    internal async Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout)
    {
        if (reason is not (SaveReason.Quit or SaveReason.ToTitle) || timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(reason));
        if (!_owner.Initialized || !_owner.Enabled) return new(SaveStatus.Disabled, _owner.Active, 0);
        using var deadline = new CancellationTokenSource(timeout);
        if (_owner.Runtime.World.Instance == 0)
        {
            if (_writing is null) return new(SaveStatus.Deferred, _owner.Active, 0);
            var writing = _writing;
            var operation = _writingOperation!;
            try
            {
                var written = await writing.WaitAsync(deadline.Token).ConfigureAwait(false);
                return written.Status == SaveStatus.Failed ? written : new(SaveStatus.Deferred, operation.Slot, 0);
            }
            catch (OperationCanceledException)
            {
                return await _owner.OnFrame(() => { var result = new SaveResult(SaveStatus.Failed, operation.Slot, 0, SaveErrorKeys.Timeout); _owner.Publish(result); return result; }).ConfigureAwait(false);
            }
        }
        if (_owner.Loading.RestoreFaulted) return new(SaveStatus.Deferred, _owner.Active, 0, SaveErrorKeys.RestoreWorld);
        if (_owner.Active is null) return new(SaveStatus.Deferred, _owner.Active, 0);
        var transition = _owner.Runtime.Maps.Transition;
        if (transition is not null)
        {
            try { if (await transition.Done.WaitAsync(deadline.Token).ConfigureAwait(false) != MapTravelResult.Arrived) return new(SaveStatus.Deferred, _owner.Active, 0); }
            catch (OperationCanceledException) { return new(SaveStatus.Deferred, _owner.Active, 0); }
        }
        // A timed-out write keeps running; retry shares it, rather than racing a second file writer.
        if (_flush is null || _flush.IsCompleted && _flush.Result.Status != SaveStatus.Saved)
        {
            _flush = StartSave(reason, forced: true);
            _flushOperation = _writingOperation;
        }
        var flush = _flush;
        var flushOperation = _flushOperation!;
        try { return await flushOperation.WaitForFlush(flush, reason, flushOperation.Slot, deadline.Token).ConfigureAwait(false); }
        catch (OperationCanceledException)
        {
            return await _owner.OnFrame(() => { var result = new SaveResult(SaveStatus.Failed, flushOperation.Slot, 0, SaveErrorKeys.Timeout); _owner.Publish(result); return result; }).ConfigureAwait(false);
        }
    }

}
