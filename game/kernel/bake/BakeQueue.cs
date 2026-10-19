using Campus.Contracts.Bake;
using System.Collections.Concurrent;

namespace Campus.Kernel.Bake;

/// <summary>Bounded, low-priority worker queue. Repeated Ensure calls promote a pending request instead of duplicating it.</summary>
internal sealed class BakeQueue : IDisposable
{
    private readonly object _sync = new();
    private readonly PriorityQueue<Job, (int Priority, long Order)> _queue = new();
    private readonly Dictionary<BakeKey, Job> _jobs = [];
    private readonly List<Thread> _workers = [];
    private readonly int _maximum;
    private readonly Action _idle;
    private int _limit;
    private int _running;
    private long _order;
    private int _total;
    private int _done;
    private int _failed;
    private bool _disposed;
    public ConcurrentQueue<string> Failures { get; } = new();

    public BakeQueue(int maximum, Action idle) { _maximum = Math.Max(1, maximum); _limit = _maximum; _idle = idle; }
    public void BeginCatalog() { lock (_sync) _total++; }
    public void EndCatalog()
    {
        lock (_sync) _done++;
        NotifyIdle();
    }

    public BakeProgress Progress
    {
        get
        {
            lock (_sync)
            {
                return new(_total, _done, _failed > 0 ? $"生成失败 {_failed} 项" : _done == _total ? "已完成" : "正在本机生成校园");
            }
        }
    }

    public IBakeHandle Ensure(BakeKey key, BakePriority priority, Func<object> build)
    {
        if (!Enum.IsDefined(priority)) throw new ArgumentOutOfRangeException(nameof(priority));
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_jobs.TryGetValue(key, out var found))
            {
                if (priority < found.Priority)
                {
                    found.Priority = priority;
                    if (found.Handle.State == BakeState.Pending) _queue.Enqueue(found, ((int)priority, ++_order));
                    Monitor.PulseAll(_sync);
                }
                return found.Handle;
            }
            var job = new Job(key, priority, build);
            _jobs.Add(key, job);
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

    public void ClearFailures()
    {
        lock (_sync)
            foreach (var key in _jobs.Where(p => p.Value.Handle.State == BakeState.Failed).Select(p => p.Key).ToArray())
            { _jobs.Remove(key); _total--; _done--; _failed--; }
    }

    public void ForgetMap(Campus.Contracts.MapId map)
    {
        lock (_sync)
        {
            foreach (var job in _jobs.Values.Where(j => j.Key.Map == map))
            {
                job.Counted = false; _total--;
                if (job.Handle.State is BakeState.Ready or BakeState.Failed) _done--;
                if (job.Handle.State == BakeState.Failed) _failed--;
                if (job.Handle.State is BakeState.Pending or BakeState.Running)
                    job.Handle.Fail("Map unloaded before this bake request completed.");
            }
            foreach (var key in _jobs.Where(p => p.Key.Map == map).Select(p => p.Key).ToArray()) _jobs.Remove(key);
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
            string? failure = null;
            try { product = job!.Build(); }
            catch (Exception error)
            {
                // Preserve producer/codec frames and nested exceptions before the failure crosses the worker boundary.
                failure = $"{job!.Key.Map.Value}/{job.Key.Module}/{job.Key.Detail} [priority={job.Priority}; worker={Thread.CurrentThread.Name}]\n{error.ToString()}";
            }
            finally
            {
                lock (_sync)
                {
                    if (job!.Counted)
                    {
                        if (failure is null && product is null) failure = "Bake producer returned no product.";
                        if (failure is null) job.Handle.Complete(product!);
                        else { job.Handle.Fail(failure); Failures.Enqueue(failure); }
                    }
                    _running--;
                    if (job!.Counted)
                    {
                        _done++;
                        if (job.Handle.State == BakeState.Failed) _failed++;
                    }
                    if (job!.Priority == BakePriority.Background && job.Handle.State == BakeState.Ready &&
                        _jobs.TryGetValue(job.Key, out var current) && ReferenceEquals(job, current))
                    {
                        _jobs.Remove(job.Key);
                    }
                    Monitor.PulseAll(_sync);
                }
                NotifyIdle();
            }
        }
    }

    private void NotifyIdle()
    {
        lock (_sync)
            if (_disposed || _done != _total) return;
        try { _idle(); }
        catch (Exception error)
        {
            lock (_sync) _failed++;
            Failures.Enqueue("Source index persistence failed: " + error);
        }
    }

    internal void Stop()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var job in _jobs.Values.Where(j => j.Handle.State is BakeState.Pending or BakeState.Running))
            {
                job.Counted = false;
                job.Handle.Fail("Bake service stopped.");
                _done++;
                _failed++;
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

    private sealed class Job(BakeKey key, BakePriority priority, Func<object> build)
    {
        public BakeKey Key { get; } = key;
        public BakePriority Priority { get; set; } = priority;
        public Func<object> Build { get; } = build;
        public Handle Handle { get; } = new();
        public bool Counted { get; set; } = true;
    }

    private sealed class Handle : IBakeHandle
    {
        private int _state;
        private object? _product;
        private string? _error;
        public BakeState State => (BakeState)Volatile.Read(ref _state);
        public object? Product => State == BakeState.Ready ? _product : null;
        public string? Error => State == BakeState.Failed ? _error : null;
        public void Start() => Volatile.Write(ref _state, (int)BakeState.Running);
        public void Complete(object product) { _product = product; Volatile.Write(ref _state, (int)BakeState.Ready); }
        public void Fail(string error) { _error = error; Volatile.Write(ref _state, (int)BakeState.Failed); }
    }
}
