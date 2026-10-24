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
    private readonly Dictionary<Task, bool> _operations = [];
    private Action? _slotNotifications;
    private Task? _initialization, _disposal;
    private bool _stopping;
    private long _loadOperation;
    private readonly CommandNotification _commands = new();
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
        var handoff = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = handoff.Task.Unwrap();
        lock (_frameGate)
        {
            ObjectDisposedException.ThrowIf(_stopping, this);
            if (_initialization is not null) throw new InvalidOperationException("Save service was initialized twice.");
            Options = options; Runtime = bindings; Sections = sections;
            Loading.Initialize();
            _initialization = operation;
            _operations.Add(operation, false);
        }
        try { handoff.SetResult(InitializeRepository(options, sections)); }
        catch (Exception error) { handoff.SetException(error); }
        return operation;
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
    internal void PublishLoad(in SaveLoadEvent value) => _commands.Publish(LoadCommand, value);

    internal Task<T> Track<T>(Func<Task<T>> start, SaveLoadEvent? command = null, bool ownsFailure = false)
    {
        var handoff = new TaskCompletionSource<Task<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = handoff.Task.Unwrap();
        bool stopping;
        lock (_frameGate)
        {
            stopping = _stopping;
            if (!stopping)
            {
                foreach (var completed in _operations.Where(pair => pair.Key.IsCompletedSuccessfully).Select(pair => pair.Key).ToArray())
                    _operations.Remove(completed);
                // Register before invoking start: Dispose must also wait for a synchronous subscriber.
                _operations.Add(operation, ownsFailure);
            }
        }
        if (stopping)
        {
            if (command is { } rejected) PublishLoad(rejected with { Phase = SaveLoadPhase.Rejected });
            throw new ObjectDisposedException(nameof(SaveService));
        }
        try { handoff.SetResult(start()); }
        catch (OperationCanceledException error) when (error.CancellationToken.IsCancellationRequested)
        {
            handoff.SetCanceled(error.CancellationToken);
            if (command is { } canceled) PublishLoad(canceled with { Phase = SaveLoadPhase.Canceled });
        }
        catch (Exception error)
        {
            handoff.SetException(error);
            if (command is { } rejected) PublishLoad(rejected with { Phase = SaveLoadPhase.Rejected });
        }
        return operation;
    }
    // A bounded flush may return before the internally owned write; transfer only a failure actually delivered by that flush.
    internal void HandOffFailure(Task operation)
    {
        lock (_frameGate)
            if (_operations.ContainsKey(operation)) _operations[operation] = false;
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
        _slotNotifications += SlotsChanged;
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
            if (_stopping)
            {
                completion.SetCanceled(Lifetime);
                return completion.Task;
            }
            // A claimed frame can finish outside the scheduling lock. Exit still owns its full execution,
            // including synchronous observers, before releasing any runtime binding.
            _operations.Add(completion.Task, false);
            _frames.Enqueue(() =>
            {
                lock (_frameGate)
                {
                    if (_stopping)
                    {
                        completion.TrySetCanceled(Lifetime);
                        _operations.Remove(completion.Task);
                        return;
                    }
                }
                try
                {
                    T result = default!;
                    Exception? failure = null;
                    try { result = function(); }
                    catch (Exception error) { Loading.MarkFrameFailure(error); failure = error; }
                    var observers = _slotNotifications;
                    _slotNotifications = null;
                    _commands.Publish(observers);
                    if (failure is not null) completion.SetException(failure);
                    else completion.SetResult(result);
                }
                finally { lock (_frameGate) _operations.Remove(completion.Task); }
            });
        }
        return completion.Task;
    }
    public Task OnFrame(Action action) => OnFrame(() => { action(); return true; });
    public void StopNewWork()
    {
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_frameGate)
        {
            if (_stopping) return;
            _stopping = true;
            _operations.Add(stopped.Task, false);
        }
        try { _stop.Cancel(); }
        finally
        {
            try { while (_frames.TryDequeue(out var action)) action(); }
            finally
            {
                stopped.TrySetResult();
                lock (_frameGate) _operations.Remove(stopped.Task);
            }
        }
    }
    public ValueTask DisposeAsync()
    {
        TaskCompletionSource done;
        lock (_frameGate)
        {
            if (_disposal is not null) return new(_disposal);
            done = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _disposal = done.Task;
        }
        _ = CompleteDisposal(done);
        return new(done.Task);
    }
    private async Task CompleteDisposal(TaskCompletionSource done)
    {
        try { await DrainAndRelease().ConfigureAwait(false); done.TrySetResult(); }
        catch (Exception error) { done.TrySetException(error); }
    }
    private async Task DrainAndRelease()
    {
        Exception? failure = null;
        try { StopNewWork(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        KeyValuePair<Task, bool>[] pending;
        lock (_frameGate) pending = _operations.ToArray();
        var operationFailures = new List<(Task Operation, Exception Error)>();
        foreach (var (task, _) in pending)
        {
            try { await task.ConfigureAwait(false); }
            catch (OperationCanceledException error) when (_stop.IsCancellationRequested && error.CancellationToken == Lifetime) { }
            catch (Exception error) { operationFailures.Add((task, task.Exception ?? error)); }
        }
        // A flush owns its own completion too; let it finish handing off any observed write failure before collecting ours.
        lock (_frameGate)
            foreach (var (operation, error) in operationFailures)
                if (_operations.TryGetValue(operation, out var ownsFailure) && ownsFailure)
                    failure = RetainedFailure.Append(failure, error);
        try { Loading.Dispose(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        try { Operation.Dispose(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        try { Repository?.Dispose(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        finally { Repository = null; }
        try { _stop.Dispose(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        if (_commands.TakeFailure() is { } notification) failure = RetainedFailure.Append(failure, notification);
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
