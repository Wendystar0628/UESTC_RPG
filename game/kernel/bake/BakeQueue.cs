using Campus.Contracts.Bake;
using Campus.Contracts.AppFlow;
using Campus.Contracts;
using Campus.Kernel.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Campus.Kernel.Bake;

/// <summary>Bounded, low-priority worker queue. Repeated Ensure calls promote a pending request instead of duplicating it.</summary>
internal sealed class BakeQueue : IDisposable
{
    private readonly object _sync = new();
    private readonly PriorityQueue<Job, (int Priority, long Order)> _queue = new();
    private readonly Dictionary<(MapSession Session, BakeKey Key), Job> _jobs = [];
    private readonly List<Thread> _workers = [];
    private readonly int _maximum;
    private readonly Action _idle;
    private int _limit;
    private int _running;
    private long _order;
    private int _total;
    private int _done;
    private bool _persisting;
    private bool _disposed;
    private readonly List<Exception> _failures = [];
    private Exception? _failure;
    public Exception? Failure { get { lock (_sync) return _failure; } }

    public BakeQueue(int maximum, Action idle) { _maximum = Math.Max(1, maximum); _limit = _maximum; _idle = idle; }
    public void BeginCatalog() { lock (_sync) _total++; }
    public void EndCatalog()
    {
        bool persist;
        lock (_sync) { _done++; persist = BeginPersistence(); }
        if (persist) PersistIdle();
    }

    public BakeProgress Progress
    {
        get
        {
            lock (_sync)
            {
                var failed = 0;
                foreach (var job in _jobs.Values)
                    if (job.Counted && job.Handle.Failure is not null and not OperationCanceledException) failed++;
                return new(_total, Math.Max(0, _done - (_persisting ? 1 : 0)), failed > 0 ? $"生成失败 {failed} 项" : !_persisting && _done == _total ? "已完成" : "正在本机生成校园");
            }
        }
    }

    public IBakeHandle Ensure(MapSession session, BakeKey key, BakePriority priority, Func<object> build)
    {
        if (!Enum.IsDefined(priority)) throw new ArgumentOutOfRangeException(nameof(priority));
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (session.Forgotten) throw new OperationCanceledException("Map session has been forgotten.");
            if (session.Map != key.Map) throw new ArgumentException("Bake key belongs to another map session.", nameof(key));
            if (_jobs.TryGetValue((session, key), out var found))
            {
                if (priority < found.Priority)
                {
                    found.Priority = priority;
                    if (found.Handle.State == BakeState.Pending) _queue.Enqueue(found, ((int)priority, ++_order));
                    Monitor.PulseAll(_sync);
                }
                return found.Handle;
            }
            var job = new Job(session, key, priority, build);
            _jobs.Add((session, key), job);
            _total++;
            _queue.Enqueue(job, ((int)priority, ++_order));
            var desired = Math.Min(_limit, _running + _queue.Count);
            while (_workers.Count < desired)
            {
                var worker = new Thread(Work) { IsBackground = true, Name = $"Campus bake {_workers.Count}", Priority = ThreadPriority.BelowNormal };
                _workers.Add(worker);
                worker.Start();
            }
            Monitor.PulseAll(_sync);
            return job.Handle;
        }
    }

    public void SetLimit(int limit)
    {
        lock (_sync)
        {
            _limit = Math.Clamp(limit, 1, _maximum);
            Monitor.PulseAll(_sync);
        }
    }

    public Task ForgetMap(MapSession session)
    {
        Job[] jobs;
        lock (_sync)
        {
            if (session.Drain is not null) return session.Drain;
            session.Forgotten = true;
            jobs = _jobs.Values.Where(job => ReferenceEquals(job.Session, session)).ToArray();
            foreach (var job in jobs)
            {
                if (!job.Counted) continue;
                var pending = job.Handle.State == BakeState.Pending;
                job.Counted = false; _total--;
                if (job.Handle.State is BakeState.Ready or BakeState.Failed) _done--;
                if (job.Handle.State is BakeState.Pending or BakeState.Running)
                    job.Handle.Fail(new OperationCanceledException("Map unloaded before this bake request completed."));
                if (pending) job.Handle.Finish(job.Failure);
            }
            return session.Drain = DrainJobs(jobs);
        }
    }

    private async Task DrainJobs(IReadOnlyList<Job> jobs)
    {
        var draining = Task.WhenAll(jobs.Select(job => job.Handle.Done));
        try { await draining.ConfigureAwait(false); }
        catch (OperationCanceledException) when (draining.IsCanceled) { }
        catch { throw draining.Exception!; }
        finally
        {
            lock (_sync)
                foreach (var job in jobs)
                    if (_jobs.TryGetValue((job.Session, job.Key), out var current) && ReferenceEquals(job, current))
                        _jobs.Remove((job.Session, job.Key));
        }
    }

    private void Work()
    {
        while (true)
        {
            Job? job = null;
            lock (_sync)
            {
                while (!_disposed)
                {
                    if (_running < _limit)
                        while (_queue.TryDequeue(out var candidate, out _))
                        {
                            if (candidate.Handle.State != BakeState.Pending) continue;
                            job = candidate;
                            job.Handle.Start();
                            _running++;
                            break;
                        }
                    if (job is not null) break;
                    Monitor.Wait(_sync);
                }
                if (_disposed) return;
            }
            object? product = null;
            try { product = job!.Build(); }
            catch (OperationCanceledException canceled) { job!.Failure = canceled; }
            catch (Exception error) { FailJob(job!, error); }
            finally
            {
                bool persist;
                lock (_sync)
                {
                    if (job!.Counted && job.Failure is null && product is null)
                        job.Failure = new InvalidDataException("Bake producer returned no product.");
                    if (job.Failure is { } error)
                    {
                        job.Handle.Fail(error);
                        if (error is not OperationCanceledException) RecordFailure(error);
                    }
                    else if (job.Counted) job.Handle.Complete(product!);
                    _running--;
                    if (job!.Counted) _done++;
                    persist = BeginPersistence();
                    Monitor.PulseAll(_sync);
                }
                var persistenceFailure = persist ? PersistIdle() : null;
                if (persistenceFailure is not null)
                {
                    job!.Failure = RetainedFailure.Append(job.Failure, persistenceFailure);
                    job.Handle.Fail(job.Failure);
                }
                job!.Handle.Finish(job.Failure);
                lock (_sync)
                    if (job.Priority == BakePriority.Background && job.Handle.State == BakeState.Ready &&
                        _jobs.TryGetValue((job.Session, job.Key), out var current) && ReferenceEquals(job, current))
                        _jobs.Remove((job.Session, job.Key));
            }
        }
    }

    // The queue owns the worker terminal result and publishes it to its handle and all drain waiters.
    private static void FailJob(Job job, [FailureHandler("ENG-ARCH")] Exception error) => job.Failure = error;

    // Called under _sync with the final completion, before another thread can observe Done == Total.
    private bool BeginPersistence()
    {
        if (_disposed || _done != _total || _persisting) return false;
        _persisting = true;
        return true;
    }

    private Exception? PersistIdle()
    {
        Exception? failure = null;
        while (true)
        {
            int completed;
            lock (_sync) completed = _done;
            try { _idle(); }
            catch (Exception error) { RecordFailure(error); failure = RetainedFailure.Append(failure, error); }
            lock (_sync)
            {
                _persisting = false;
                Monitor.PulseAll(_sync);
                if (_done == completed || !BeginPersistence()) return failure;
            }
        }
    }

    // Classify once at publication; handles and drain tasks keep their original worker errors.
    internal Exception RecordFailure([FailureHandler("ENG-ARCH")] Exception error)
    {
        if (error is OperationCanceledException) return error;
        lock (_sync)
        {
            if (ReferenceEquals(error, _failure)) return error;
            _failures.Add(error);
            if (_failures.Count == 1 && error is AppStartupException)
            {
                _failure = error;
                return error;
            }
            var cause = _failures.Count == 1 ? error : new AggregateException("Bake work failed.", _failures);
            return _failure = new AppStartupException("Local bake work failed.", cause);
        }
    }

    internal void ThrowIfFailed()
    {
        if (Failure is { } failure) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    internal void Stop()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var job in _jobs.Values.Where(j => j.Handle.State is BakeState.Pending or BakeState.Running))
            {
                var running = job.Handle.State == BakeState.Running;
                if (job.Counted) { job.Counted = false; _total--; }
                job.Handle.Fail(new OperationCanceledException("Bake service stopped."));
                if (!running) job.Handle.Finish(null);
            }
            _queue.Clear();
            Monitor.PulseAll(_sync);
        }
    }

    public void Dispose()
    {
        Stop();
        // In-flight builds own their captured map context until completion; no thread is aborted.
        foreach (var worker in _workers)
            if (worker != Thread.CurrentThread) worker.Join();
    }

    // One captured map lifetime owns its requests and drain.
    internal sealed class MapSession(MapId map)
    {
        internal MapId Map { get; } = map;
        internal bool Forgotten;
        internal Task? Drain;
    }

    private sealed class Job(MapSession session, BakeKey key, BakePriority priority, Func<object> build)
    {
        public MapSession Session { get; } = session;
        public BakeKey Key { get; } = key;
        public BakePriority Priority { get; set; } = priority;
        public Func<object> Build { get; } = build;
        public Handle Handle { get; } = new();
        public Exception? Failure { get; set; }
        public bool Counted { get; set; } = true;
    }

    private sealed class Handle : IBakeHandle
    {
        private int _state;
        private object? _product;
        private Exception? _failure;
        public BakeState State => (BakeState)Volatile.Read(ref _state);
        public object? Product => State == BakeState.Ready ? _product : null;
        public Exception? Failure => State == BakeState.Failed ? _failure : null;
        private readonly TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Done => _done.Task;
        public void Finish(Exception? actualFailure)
        {
            // Done represents the worker terminal state, including a real fault discovered after cancellation.
            var failure = actualFailure ?? Failure;
            if (failure is OperationCanceledException) _done.TrySetCanceled();
            else if (failure is not null) _done.TrySetException(failure);
            else _done.TrySetResult();
        }
        public void Start() => Volatile.Write(ref _state, (int)BakeState.Running);
        public void Complete(object product) { _product = product; Volatile.Write(ref _state, (int)BakeState.Ready); }
        public void Fail([FailureHandler("ENG-ARCH")] Exception error) { _failure = error; Volatile.Write(ref _state, (int)BakeState.Failed); }
    }
}
