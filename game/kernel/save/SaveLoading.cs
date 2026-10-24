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
    internal Task<LoadResult> NewGame(SaveLoadEvent command, SlotId? slot, uint? seed = null, WorldPreset? start = null, NewGameBoot? boot = null)
    {
        if (slot is { } selected && !SaveSlots.Auto.Contains(selected)) throw new ArgumentOutOfRangeException(nameof(slot));
        return RestoreDefaultWorld(command, slot, seed, start, boot);
    }

    private async Task<LoadResult> RestoreDefaultWorld(SaveLoadEvent command, SlotId? slot, uint? seed, WorldPreset? start, NewGameBoot? boot)
    {
        LoadResult? outcome = null;
        bool ended = false, canceled = false;
        try
        {
            var waiting = _owner.Operation.WaitAsync(_owner.Lifetime);
            if (!waiting.IsCompleted) _owner.PublishLoad(command with { Phase = SaveLoadPhase.Queued });
            await waiting.ConfigureAwait(false);
            try
            {
                _owner.PublishLoad(command with { Phase = SaveLoadPhase.Started });
                await BeginRestoreOperation().ConfigureAwait(false);
                NewGameContext? defaults = null;
                var repairs = new List<string>();
                var prepared = await _owner.OnFrame(() =>
                {
                    var result = _restore!.PrepareDefaultWorld(_owner.Options, _owner.Sections, _defaultRuleset,
                        slot, seed, start, boot, selected => checked(_owner.Snapshots[selected].MaxGeneration + 1));
                    defaults = result.Defaults;
                    _owner.Writing.NewWorld(slot);
                    return result.Save;
                }).ConfigureAwait(false);
                await PrepareMap(prepared.Document.Meta.Map).ConfigureAwait(false);
                await _owner.OnFrame(() => _restore!.RestorePhaseOf(prepared, RestorePhase.Content, repairs, defaults)).ConfigureAwait(false);
                prepared = await PrepareViewPose(prepared, repairs, defaults).ConfigureAwait(false);
                var wait = await _owner.OnFrame(() =>
                {
                    _restore!.RestorePhaseOf(prepared, RestorePhase.Pose, repairs, defaults);
                    return _owner.Runtime.WaitDestination(_owner.Runtime.Camera.Pose, _owner.Runtime.Observer.Space, _owner.Runtime.Observer.Mode);
                }).ConfigureAwait(false);
                await wait.ConfigureAwait(false);
                await _owner.OnFrame(_restore!.Succeed).ConfigureAwait(false);
                outcome = new(LoadStatus.Loaded, _owner.Active, 0, repairs);
                return outcome;
            }
            catch (Exception) when (_owner.Stopping) { throw; }
            catch (Exception error) { outcome = await RestoreFailed(slot, error).ConfigureAwait(false); return outcome; }
            finally
            {
                await EndOperation().ConfigureAwait(false);
                ended = true;
            }
        }
        catch (OperationCanceledException error) when (_owner.Stopping && error.CancellationToken == _owner.Lifetime)
        {
            canceled = true;
            throw;
        }
        finally
        {
            _owner.PublishLoad(command with
            {
                Phase = ended && outcome is not null ? SaveLoadPhase.Completed :
                    canceled ? SaveLoadPhase.Canceled : SaveLoadPhase.Faulted,
                Result = ended && outcome is not null ? outcome : null
            });
        }
    }

    internal async Task<LoadResult> Load(SaveLoadEvent command, SlotId slot, ulong? generation = null)
    {
        LoadResult? outcome = null;
        bool ended = false, canceled = false, rejected = false;
        try
        {
            if (!SaveSlots.Auto.Contains(slot))
            {
                rejected = true;
                _owner.PublishLoad(command with { Phase = SaveLoadPhase.Rejected });
                throw new ArgumentOutOfRangeException(nameof(slot));
            }
            if (generation == 0)
            {
                rejected = true;
                _owner.PublishLoad(command with { Phase = SaveLoadPhase.Rejected });
                throw new ArgumentOutOfRangeException(nameof(generation));
            }
            var waiting = _owner.Operation.WaitAsync(_owner.Lifetime);
            if (!waiting.IsCompleted) _owner.PublishLoad(command with { Phase = SaveLoadPhase.Queued });
            await waiting.ConfigureAwait(false);
            try
            {
                _owner.PublishLoad(command with { Phase = SaveLoadPhase.Started });
                if (!_owner.Initialized)
                {
                    outcome = new(LoadStatus.Failed, slot, 0, [], "Save service is not initialized.");
                    return outcome;
                }
                await BeginRestoreOperation().ConfigureAwait(false);
                var scan = await Task.Run(() => _owner.Repository!.Scan(slot)).ConfigureAwait(false);
                await _owner.OnFrame(() => { _owner.Snapshots[slot] = scan; _owner.Publish(null); }).ConfigureAwait(false);
                if (scan.Save is null)
                {
                    outcome = new(scan.Info.Health switch
                    {
                        SlotHealth.Empty => LoadStatus.Empty,
                        SlotHealth.Corrupt or SlotHealth.Newer => LoadStatus.Corrupt,
                        _ => LoadStatus.Failed
                    }, slot, 0, []);
                    return outcome;
                }
                var prepared = generation is { } selected
                    ? await Task.Run(() => _owner.Repository!.ReadEntry(slot, selected)).ConfigureAwait(false) : scan.Save;
                outcome = await Restore(prepared, _owner.Options.WriteSlot ?? slot).ConfigureAwait(false);
                return outcome;
            }
            catch (Exception) when (_owner.Stopping) { throw; }
            catch (SaveFormatException error) when (_restore?.MutationStarted != true)
            { outcome = await RejectEntry(slot, generation, error).ConfigureAwait(false); return outcome; }
            catch (Exception error) { outcome = await RestoreFailed(slot, error).ConfigureAwait(false); return outcome; }
            finally
            {
                await EndOperation().ConfigureAwait(false);
                ended = true;
            }
        }
        catch (OperationCanceledException error) when (_owner.Stopping && error.CancellationToken == _owner.Lifetime)
        {
            canceled = true;
            throw;
        }
        finally
        {
            if (!rejected)
                _owner.PublishLoad(command with
                {
                    Phase = ended && outcome is not null ? SaveLoadPhase.Completed :
                        canceled ? SaveLoadPhase.Canceled : SaveLoadPhase.Faulted,
                    Result = ended && outcome is not null ? outcome : null
                });
        }
    }

    internal async Task<LoadResult> LoadFile(SaveLoadEvent command, string path)
    {
        LoadResult? outcome = null;
        bool ended = false, canceled = false;
        try
        {
            var waiting = _owner.Operation.WaitAsync(_owner.Lifetime);
            if (!waiting.IsCompleted) _owner.PublishLoad(command with { Phase = SaveLoadPhase.Queued });
            await waiting.ConfigureAwait(false);
            try
            {
                _owner.PublishLoad(command with { Phase = SaveLoadPhase.Started });
                await BeginRestoreOperation().ConfigureAwait(false);
                var prepared = await Task.Run(() => _owner.Repository!.ReadExternal(path)).ConfigureAwait(false);
                outcome = await Restore(prepared, _owner.Options.WriteSlot).ConfigureAwait(false);
                return outcome;
            }
            catch (Exception) when (_owner.Stopping) { throw; }
            catch (SaveFormatException error) when (_restore?.MutationStarted != true)
            { outcome = await RejectExternalFile(error).ConfigureAwait(false); return outcome; }
            catch (Exception error) { outcome = await RestoreFailed(null, error).ConfigureAwait(false); return outcome; }
            finally
            {
                await EndOperation().ConfigureAwait(false);
                ended = true;
            }
        }
        catch (OperationCanceledException error) when (_owner.Stopping && error.CancellationToken == _owner.Lifetime)
        {
            canceled = true;
            throw;
        }
        finally
        {
            _owner.PublishLoad(command with
            {
                Phase = ended && outcome is not null ? SaveLoadPhase.Completed :
                    canceled ? SaveLoadPhase.Canceled : SaveLoadPhase.Faulted,
                Result = ended && outcome is not null ? outcome : null
            });
        }
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
        var sections = prepared.Sections.ToArray();
        var viewIndex = Array.FindIndex(sections, item => item.Registration.Section.TypeInfo.Type == typeof(ViewSaveData));
        var walkIndex = Array.FindIndex(sections, item => item.Registration.Section.TypeInfo.Type == typeof(WalkSaveData));
        if (viewIndex < 0 && walkIndex < 0) return prepared;
        foreach (var index in new[] { viewIndex, walkIndex }.Where(index => index >= 0))
            sections[index] = await _owner.OnFrame(() => _restore!.PreparePoseSection(sections[index], repairs, defaults)).ConfigureAwait(false);
        WalkSaveData? walk = null;
        if (walkIndex >= 0)
        {
            var section = sections[walkIndex];
            if (section.Dto is not WalkSaveData data) throw new InvalidDataException("The walk section must use the shared walking DTO.");
            var context = new RestoreContext { Map = section.Map, FromVersion = section.FromVersion, Missing = false };
            var preparing = await _owner.OnFrame(() => (_owner.Runtime.PrepareWalk
                ?? throw new InvalidOperationException("Walk restoration requires terrain and history preparation."))(data, context, _owner.Lifetime)).ConfigureAwait(false);
            walk = await preparing.ConfigureAwait(false);
            repairs.AddRange(context.Repairs);
            sections[walkIndex] = section with { Dto = walk };
        }
        if (viewIndex < 0) return prepared with { Sections = sections };
        var viewSection = sections[viewIndex];
        if (viewSection.Dto is not ViewSaveData view) throw new InvalidDataException("The view section must use the shared view DTO.");
        var target = view.Mode == ViewMode.Walk && walk is not null ? walk.EyePose() : view.Pose;
        var preparation = await _owner.OnFrame(() => (_owner.Runtime.PreparePose
            ?? throw new InvalidOperationException("View restoration requires terrain preparation."))(target)).ConfigureAwait(false);
        var pose = await preparation.ConfigureAwait(false);
        if (pose != view.Pose) repairs.Add("view.pose");
        sections[viewIndex] = viewSection with { Dto = view with { Pose = pose } };
        return prepared with
        {
            Document = prepared.Document with { Meta = prepared.Document.Meta with { Pose = pose } },
            Sections = sections
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
