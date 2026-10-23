using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;

namespace Campus.Kernel.Save;

/// <summary>Owns restore transactions; in-flight reads and preparation finish before its input lease is released.</summary>
internal sealed class SaveLoading(SaveService owner) : IDisposable
{
    private readonly SaveService _owner = owner;
    private SaveRestoreOperation? _restore;
    private RulesetId _defaultRuleset;

    internal void Initialize()
    {
        _defaultRuleset = _owner.Runtime.Ruleset.Current.Id;
        _restore = new(_owner.Runtime, PublishRestoreFault, PublishRestoreSuccess);
    }
    internal void MarkFrameFailure(Exception error)
    {
        if (!Restoring || _restore?.MutationStarted != true) return;
        try { _restore.MarkFault(error); }
        catch (Exception abortError) { _restore.RetainAbortFailure(error, abortError); }
    }
    private async Task EndOperation()
    {
        try
        {
            if (!_owner.Stopping) await _owner.OnFrame(() => _restore?.End()).ConfigureAwait(false);
        }
        catch (OperationCanceledException error) when (_owner.Stopping && error.CancellationToken == _owner.Lifetime) { }
        finally { _owner.Operation.Release(); }
    }
    public void Dispose() => _restore?.Dispose();
    internal Task<LoadResult> NewGame(SlotId? slot, uint? seed = null, WorldPreset? start = null, NewGameBoot? boot = null)
    {
        if (slot is { } selected && !SaveSlots.Auto.Contains(selected)) throw new ArgumentOutOfRangeException(nameof(slot));
        return RestoreDefaultWorld(slot, seed, start, boot);
    }

    private async Task<LoadResult> RestoreDefaultWorld(SlotId? slot, uint? seed, WorldPreset? start, NewGameBoot? boot)
    {
        await _owner.Operation.WaitAsync(_owner.Lifetime).ConfigureAwait(false);
        try
        {
            await BeginRestoreOperation().ConfigureAwait(false);
            NewGameContext? defaults = null;
            var prepared = await _owner.OnFrame(() =>
            {
                var result = _restore!.PrepareDefaultWorld(_owner.Options, _owner.Sections, _defaultRuleset,
                    slot, seed, start, boot, selected => checked(_owner.Snapshots[selected].MaxGeneration + 1));
                defaults = result.Defaults;
                _owner.Writing.NewWorld(slot);
                return result.Save;
            }).ConfigureAwait(false);
            await PrepareMap(prepared.Document.Meta.Map).ConfigureAwait(false);
            await _owner.OnFrame(() => _restore!.RestorePhaseOf(prepared, RestorePhase.Content, [], defaults)).ConfigureAwait(false);
            prepared = await PrepareViewPose(prepared, [], defaults).ConfigureAwait(false);
            var wait = await _owner.OnFrame(() =>
            {
                _restore!.RestorePhaseOf(prepared, RestorePhase.Pose, [], defaults);
                return _owner.Runtime.WaitDestination(_owner.Runtime.Camera.Pose, _owner.Runtime.Observer.Space, _owner.Runtime.Observer.Mode);
            }).ConfigureAwait(false);
            await wait.ConfigureAwait(false);
            await _owner.OnFrame(_restore!.Succeed).ConfigureAwait(false);
            return new(LoadStatus.Loaded, _owner.Active, 0, []);
        }
        catch (Exception) when (_owner.Stopping) { throw; }
        catch (Exception error) { return await RestoreFailed(slot, error).ConfigureAwait(false); }
        finally { await EndOperation().ConfigureAwait(false); }
    }

    internal async Task<LoadResult> Load(SlotId slot, ulong? generation = null)
    {
        if (!SaveSlots.Auto.Contains(slot)) throw new ArgumentOutOfRangeException(nameof(slot));
        if (generation == 0) throw new ArgumentOutOfRangeException(nameof(generation));
        await _owner.Operation.WaitAsync(_owner.Lifetime).ConfigureAwait(false);
        try
        {
            if (!_owner.Initialized) return new(LoadStatus.Failed, slot, 0, [], "Save service is not initialized.");
            await BeginRestoreOperation().ConfigureAwait(false);
            var scan = await Task.Run(() => _owner.Repository!.Scan(slot)).ConfigureAwait(false);
            await _owner.OnFrame(() => { _owner.Snapshots[slot] = scan; _owner.Publish(null); }).ConfigureAwait(false);
            if (scan.Save is null) return new(scan.Info.Health switch
            {
                SlotHealth.Empty => LoadStatus.Empty,
                SlotHealth.Corrupt or SlotHealth.Newer => LoadStatus.Corrupt,
                _ => LoadStatus.Failed
            }, slot, 0, []);
            var prepared = generation is { } selected
                ? await Task.Run(() => _owner.Repository!.ReadEntry(slot, selected)).ConfigureAwait(false) : scan.Save;
            return await Restore(prepared, _owner.Options.WriteSlot ?? slot).ConfigureAwait(false);
        }
        catch (Exception) when (_owner.Stopping) { throw; }
        catch (SaveFormatException error) when (_restore?.MutationStarted != true)
        { return await RejectEntry(slot, generation, error).ConfigureAwait(false); }
        catch (Exception error) { return await RestoreFailed(slot, error).ConfigureAwait(false); }
        finally { await EndOperation().ConfigureAwait(false); }
    }

    internal async Task<LoadResult> LoadFile(string path)
    {
        await _owner.Operation.WaitAsync(_owner.Lifetime).ConfigureAwait(false);
        try
        {
            await BeginRestoreOperation().ConfigureAwait(false);
            var prepared = await Task.Run(() => _owner.Repository!.ReadExternal(path)).ConfigureAwait(false);
            return await Restore(prepared, _owner.Options.WriteSlot).ConfigureAwait(false);
        }
        catch (Exception) when (_owner.Stopping) { throw; }
        catch (SaveFormatException error) when (_restore?.MutationStarted != true)
        { return await RejectExternalFile(error).ConfigureAwait(false); }
        catch (Exception error) { return await RestoreFailed(null, error).ConfigureAwait(false); }
        finally { await EndOperation().ConfigureAwait(false); }
    }

    private async Task<LoadResult> Restore(PreparedSave prepared, SlotId? active)
    {
        var meta = prepared.Document.Meta;
        var repairs = new List<string>();
        string? defaultMapSpawn = null;
        await _owner.OnFrame(() =>
        {
            (prepared, defaultMapSpawn) = _restore!.PrepareLoadedWorld(prepared, repairs);
            meta = prepared.Document.Meta;
            _owner.Writing.LoadedWorld(prepared, active);
        }).ConfigureAwait(false);
        await PrepareMap(meta.Map).ConfigureAwait(false);
        if (defaultMapSpawn is not null)
        {
            meta = await _owner.OnFrame(() => meta with { Pose = _owner.Runtime.Maps.ResolveSpawn(meta.Map, defaultMapSpawn).ViewPose(meta.Mode) }).ConfigureAwait(false);
            prepared = prepared with { Document = prepared.Document with { Meta = meta } };
        }
        await _owner.OnFrame(() => _restore!.RestorePhaseOf(prepared, RestorePhase.Content, repairs)).ConfigureAwait(false);
        prepared = await PrepareViewPose(prepared, repairs).ConfigureAwait(false);
        var wait = await _owner.OnFrame(() =>
        {
            _restore!.RestorePhaseOf(prepared, RestorePhase.Pose, repairs);
            return _owner.Runtime.WaitDestination(_owner.Runtime.Camera.Pose, _owner.Runtime.Observer.Space, _owner.Runtime.Observer.Mode);
        }).ConfigureAwait(false);
        await wait.ConfigureAwait(false);
        await _owner.OnFrame(() =>
        {
            _owner.Schedule.Saved(); _restore!.Succeed();
        }).ConfigureAwait(false);
        return new(LoadStatus.Loaded, active, meta.Gen, repairs);
    }

    private async Task<PreparedSave> PrepareViewPose(PreparedSave prepared, List<string> repairs, NewGameContext? defaults = null)
    {
        var section = prepared.Sections.FirstOrDefault(item => item.Registration.Section.TypeInfo.Type == typeof(ViewSaveData));
        if (section is null) return prepared;
        section = await _owner.OnFrame(() => _restore!.PreparePoseSection(section, repairs, defaults)).ConfigureAwait(false);
        var dto = section.Dto;
        if (dto is not ViewSaveData view) throw new InvalidDataException("The view section must use the shared view DTO.");
        var preparing = await _owner.OnFrame(() => (_owner.Runtime.PreparePose ?? throw new InvalidOperationException("View restoration requires terrain preparation."))(view.Pose)).ConfigureAwait(false);
        var pose = await preparing.ConfigureAwait(false);
        if (pose != view.Pose) repairs.Add("view.pose");
        return prepared with
        {
            Document = prepared.Document with { Meta = prepared.Document.Meta with { Pose = pose } },
            Sections = prepared.Sections.Select(item => ReferenceEquals(item.Registration, section.Registration) && item.Map == section.Map ? section with { Dto = view with { Pose = pose } } : item).ToArray()
        };
    }

    internal bool Restoring => _restore?.IsActive == true;
    internal bool RestoreFaulted => _restore?.Blocked == true || _owner.Initialized && _owner.Runtime.WorldRestore.RestoreFaulted;

    private async Task BeginRestoreOperation()
    {
        var previousMap = await _owner.OnFrame(() =>
        {
            if (!_owner.Initialized) throw new InvalidOperationException("Save service is not initialized.");
            _owner.Schedule.ClearRequest(); _owner.Writing.BeginRestore();
            return _restore!.Begin();
        }).ConfigureAwait(false);
        // A new restore must not race the previous entry's pending map or pose work.
        // Its Arrived/Failed outcome belongs to the old request; either terminal result allows reconstruction.
        if (previousMap is { IsCompleted: false }) await Task.WhenAny(previousMap).ConfigureAwait(false);
    }

    private Task<LoadResult> RejectEntry(SlotId slot, ulong? generation, [FailureHandler("SYS-SAVE-FORMAT")] SaveFormatException error) => _owner.OnFrame(() =>
    {
        _restore!.RetainFailure(error);
        _owner.Runtime.Diagnostics.Warning("Requested save entry rejected before world mutation.", error);
        return new LoadResult(LoadStatus.Corrupt, slot, generation ?? 0, [], SaveErrorKeys.Verification);
    });

    private Task<LoadResult> RejectExternalFile([FailureHandler("SYS-SAVE-FORMAT")] SaveFormatException error) => _owner.OnFrame(() =>
    {
        _restore!.RetainFailure(error);
        var result = new LoadResult(LoadStatus.Corrupt, null, 0, [], _restore.Error!.Message);
        _owner.Runtime.Diagnostics.Warning("External save rejected before world mutation.", _restore.Error);
        return result;
    });

    private Task<LoadResult> RestoreFailed(SlotId? slot, [FailureHandler("SYS-SAVE-FLOW")] Exception error) =>
        _owner.Runtime is null ? Task.FromException<LoadResult>(error) : _owner.OnFrame(() =>
    {
        if (_restore?.MutationStarted != true)
        {
            _restore?.RetainFailure(error);
            _owner.Runtime.Diagnostics.Error("Save restoration failed before world mutation.", error);
            return new LoadResult(LoadStatus.Failed, slot, 0, [], error.Message);
        }
        try { _restore.MarkFault(error); }
        catch (Exception abortError) { _restore.RetainAbortFailure(error, abortError); }
        _owner.Runtime.Diagnostics.Error("Save restoration failed; the world is blocked until Load or NewGame.", _restore.Error!);
        return new LoadResult(LoadStatus.Failed, slot, 0, [], SaveErrorKeys.RestoreWorld + ": " + _restore.Error!.Message);
    });

    private void PublishRestoreFault()
    {
        _owner.Schedule.ClearRequest();
        _owner.Publish(new(SaveStatus.Failed, _owner.Active, 0, SaveErrorKeys.RestoreWorld));
    }

    private void PublishRestoreSuccess()
    {
        _owner.ClearRestoreError();
        _owner.Publish(null);
    }

    private async Task PrepareMap(MapId map)
    {
        var preparation = await _owner.OnFrame(() => _owner.Runtime.Maps.Prepare(map)).ConfigureAwait(false);
        await preparation.ConfigureAwait(false);
    }

}
