using System.Collections.Concurrent;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;
using Campus.Kernel.Diagnostics;

namespace Campus.Kernel.Save;

[KernelService(typeof(SaveService))]
[KernelService(typeof(ISaveService))]
public sealed class SaveService : ISaveService, IDisposable, IAsyncDisposable
{
    private readonly ConcurrentQueue<Action> _frames = new();
    private readonly object _frameGate = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly List<Task> _operations = [];
    private Task? _initialization, _disposal;
    private bool _stopping;
    private long _loadOperation;
    private IReadOnlyList<SlotInfo> _slotInfos = SaveSlots.Auto.Select(s => new SlotInfo(s, SlotKind.Auto, SlotHealth.Empty, null, 0)).ToArray();
    internal CancellationToken Lifetime { get; }
    internal SemaphoreSlim Operation { get; } = new(1, 1);
    internal Dictionary<SlotId, SlotSnapshot> Snapshots { get; } = [];
    internal SaveRuntimeOptions Options { get; private set; } = null!;
    internal SaveRuntimeBindings Runtime { get; private set; } = null!;
    internal SaveSections Sections { get; private set; } = null!;
    internal SaveRepository? Repository { get; private set; }
    internal SaveSchedule Schedule { get; }
    internal SaveWriting Writing { get; }
    internal SaveLoading Loading { get; }
    internal bool Initialized { get; private set; }
    internal bool Stopping => Volatile.Read(ref _stopping);
    public SaveService()
    {
        Lifetime = _stop.Token;
        Schedule = new(this); Writing = new(this); Loading = new(this);
    }
    public bool Enabled => Initialized && Repository?.Enabled == true;
    public IReadOnlyList<SlotInfo> Slots => _slotInfos;
    public SlotId? Continue => _slotInfos.Where(s => s.Health == SlotHealth.Ok)
        .OrderByDescending(s => s.Meta!.SavedAtUnix).ThenBy(s => s.Slot.Value, StringComparer.Ordinal).Select(s => (SlotId?)s.Slot).FirstOrDefault();
    public SlotId? Active { get; internal set; }
    public bool Busy => Writing.Busy;
    public SaveState State { get; private set; } = new(null, null, null, null, ReadOnlyReason.None);
    public event Action? SlotsChanged;
    public event Action<SaveLoadEvent>? LoadCommand;

    public Task Initialize(SaveRuntimeOptions options, SaveRuntimeBindings bindings, SaveSections sections)
    {
        lock (_frameGate)
        {
            ObjectDisposedException.ThrowIf(_stopping, this);
            if (_initialization is not null) throw new InvalidOperationException("Save service was initialized twice.");
            Options = options; Runtime = bindings; Sections = sections;
            Loading.Initialize();
            return _initialization = Track(() => InitializeRepository(options, sections));
        }
    }
    private async Task InitializeRepository(SaveRuntimeOptions options, SaveSections sections)
    {
        using var data = await SaveInitialization.Prepare(options, sections, Lifetime).ConfigureAwait(false);
        await OnFrame(() =>
        {
            Repository = data.TakeRepository();
            foreach (var slot in data.Slots) Snapshots[slot.Info.Slot] = slot;
            Initialized = true;
            Publish(null);
        }).ConfigureAwait(false);
    }
    /// <summary>Called only by the save node after StreamMount; queued callbacks are snapshot/restore boundaries.</summary>
    public void Tick(double delta)
    {
        var callbacks = _frames.Count;
        for (var i = 0; i < callbacks && _frames.TryDequeue(out var action); i++) action();
        if (Initialized && !Stopping) Schedule.Tick(delta);
    }
    public void Request(SaveReason reason) => Schedule.Request(reason);
    public Task<SaveResult> SaveNow(SaveReason reason) => Writing.SaveNow(reason);
    public Task<LoadResult> NewGame(SlotId? slot, uint? seed = null, WorldPreset? start = null, NewGameBoot? boot = null)
    {
        var command = NewLoadCommand(SaveLoadKind.NewGame, slot);
        return Track(() => Loading.NewGame(command, slot, seed, start, boot), command);
    }
    public Task<LoadResult> Load(SlotId slot)
    {
        var command = NewLoadCommand(SaveLoadKind.Slot, slot);
        return Track(() => Loading.Load(command, slot), command);
    }
    public Task<LoadResult> Load(SlotId slot, ulong gen)
    {
        var command = NewLoadCommand(SaveLoadKind.Slot, slot, gen);
        return Track(() => Loading.Load(command, slot, gen), command);
    }
    public Task<DeleteResult> DeleteEntry(SlotId slot, ulong gen) => Writing.Delete(slot, gen);
    public Task<DeleteResult> DeleteSlot(SlotId slot) => Writing.Delete(slot, null);
    public Task<LoadResult> LoadFile(string path)
    {
        var command = NewLoadCommand(SaveLoadKind.File, null, file: path);
        return Track(() => Loading.LoadFile(command, path), command);
    }
    public Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout) => Track(() => Writing.FlushForQuit(reason, timeout));

    internal SaveLoadEvent NewLoadCommand(SaveLoadKind kind, SlotId? slot,
        ulong? generation = null, string? file = null) =>
        new(Interlocked.Increment(ref _loadOperation), kind, SaveLoadPhase.Queued,
            slot, generation, file, null);
    internal void PublishLoad(in SaveLoadEvent value) => LoadCommand?.Invoke(value);

    internal Task<T> Track<T>(Func<Task<T>> start, SaveLoadEvent? command = null) =>
        (Task<T>)Track(() => (Task)start(), command);
    private Task Track(Func<Task> start, SaveLoadEvent? command = null)
    {
        lock (_frameGate)
        {
            if (_stopping && command is { } rejected)
                PublishLoad(rejected with { Phase = SaveLoadPhase.Rejected });
            ObjectDisposedException.ThrowIf(_stopping, this);
            _operations.RemoveAll(task => task.IsCompletedSuccessfully);
            Task task;
            try { task = start(); }
            catch
            {
                if (command is { } failed) PublishLoad(failed with { Phase = SaveLoadPhase.Rejected });
                throw;
            }
            _operations.Add(task); return task;
        }
    }
    public byte[]? Thumbnail(SlotId slot) => Snapshots.TryGetValue(slot, out var value) && value.Info.Meta is { } meta
        ? Thumbnail(slot, meta.Gen) : null;
    public byte[]? Thumbnail(SlotId slot, ulong gen) => Snapshots.TryGetValue(slot, out var value) && value.Thumbnails.TryGetValue(gen, out var bytes)
        ? bytes.ToArray() : null;
    public void WarmSerialization()
    {
        var map = Runtime.Maps.Maps.First(m => m.Id == Runtime.Maps.Default);
        foreach (var section in Sections.Registered)
        {
            var dto = section.Section.CreateDefault(new(1, map.Id, map.World.Default,
                Runtime.Ruleset.Current.DefaultViewMode));
            JsonSerializer.SerializeToUtf8Bytes(dto, section.Section.TypeInfo);
        }
    }

    internal void Publish(SaveResult? result)
    {
        _slotInfos = SaveSlots.Auto.Select(s => Snapshots.TryGetValue(s, out var info) ? info.Info : new(s, SlotKind.Auto, SlotHealth.Empty, null, 0)).ToArray();
        var savedAt = Active is { } active && Snapshots.TryGetValue(active, out var selected) ? selected.Info.Meta?.SavedAtUnix : null;
        var last = State.Slot == Active ? State.Last : null;
        var error = State.Slot == Active ? State.ErrorKey : null;
        if (result is not null && result.Slot == Active)
        {
            last = result.Status;
            error = result.Status == SaveStatus.Failed ? result.Error : null;
        }
        State = new(Active, last, error, savedAt, Repository?.ReadOnly ?? ReadOnlyReason.None);
        SlotsChanged?.Invoke();
    }
    internal void ClearRestoreError()
    {
        if (State.ErrorKey == SaveErrorKeys.RestoreWorld) State = State with { Last = null, ErrorKey = null };
    }
    public Task<T> OnFrame<T>(Func<T> function)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_frameGate)
        {
            if (_stopping) return Task.FromCanceled<T>(Lifetime);
            _frames.Enqueue(() =>
            {
                lock (_frameGate)
                {
                    if (_stopping) { completion.TrySetCanceled(Lifetime); return; }
                    try { completion.SetResult(function()); }
                    catch (Exception error)
                    {
                        Loading.MarkFrameFailure(error);
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
    public ValueTask DisposeAsync()
    {
        lock (_frameGate) return new(_disposal ??= DrainAndRelease());
    }
    private async Task DrainAndRelease()
    {
        Exception? failure = null;
        try { StopNewWork(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        Task[] pending;
        lock (_frameGate) pending = _operations.ToArray();
        foreach (var task in pending)
        {
            try { await task.ConfigureAwait(false); }
            catch (OperationCanceledException error) when (_stop.IsCancellationRequested && error.CancellationToken == Lifetime) { }
            catch (Exception error) { failure = RetainedFailure.Append(failure, task.Exception ?? error); }
        }
        try { Loading.Dispose(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        try { Operation.Dispose(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        try { Repository?.Dispose(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        finally { Repository = null; }
        try { _stop.Dispose(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
