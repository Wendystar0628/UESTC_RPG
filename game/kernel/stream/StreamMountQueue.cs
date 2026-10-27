using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Diagnostics;
using Campus.Kernel.Frame;
using Godot;

namespace Campus.Kernel.Stream;

public interface IStreamMountFrame
{
    bool RetirementFailed { get; }
    void Drain(IAppScreenCoverage coverage, Action? prepare = null, Action<double>? finish = null, Action<double>? release = null, Func<bool>? canActivateVisual = null, Func<bool>? canReleaseRetired = null);
}

/// <summary>The sole owner of the shared collision/visual mount budget and covered-screen refresh slices.</summary>
[KernelService(typeof(IStreamMountFrame))]
[KernelService(typeof(IStreamMountQueue))]
public sealed class StreamMountQueue : IStreamMountQueue, IStreamMountFrame
{
    private readonly List<IStreamMountWork> _pending = [];
    private readonly List<IStreamReleaseWork> _retiring = [];
    private int _retirementCursor;
    private readonly Campus.Kernel.Lod.StreamSettings _settings = new();
    private readonly MountWatchdog _watchdog;
    private readonly Func<double> _clock;
    public StreamMountQueue(Func<double>? clock = null)
    {
        _watchdog = new(_settings.MountMilliseconds);
        _clock = clock ?? (() => FrameClock.MonotonicMilliseconds);
    }
    public Exception? Failure { get; private set; }
    public bool RetirementFailed { get; private set; }
    public long Revision { get; private set; } = 1;
    public int Pending => _pending.Count;
    public int PendingRetirements => _retiring.Count;
    public double LastMilliseconds { get; private set; }
    public int WatchdogTrips => _watchdog.Trips;
    public void Enqueue(IStreamMountWork work)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(work);
            if (work.Revision != Revision) { Retire(work); return; }
            _pending.Add(work);
        }
        catch (Exception error)
        {
            RecordFailure(error);
            if (work is not null) Retire(work);
        }
    }
    private static int Compare(IStreamMountWork a, IStreamMountWork b)
    {
        var background = (a.Priority == BakePriority.Background).CompareTo(b.Priority == BakePriority.Background);
        if (background != 0) return background;
        var kind = Rank(a.Kind).CompareTo(Rank(b.Kind));
        if (kind != 0) return kind;
        var priority = ((byte)a.Priority).CompareTo((byte)b.Priority);
        if (priority != 0) return priority;
        var module = StringComparer.Ordinal.Compare(a.Key.Module, b.Key.Module);
        return module != 0 ? module : StringComparer.Ordinal.Compare(a.Key.Detail, b.Key.Detail);
    }
    private static int Rank(StreamMountKind kind) => kind switch
    {
        StreamMountKind.Collision => 0,
        StreamMountKind.Visual => 1,
        StreamMountKind.Auxiliary => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public void RemoveMap(MapId map)
    {
        Revision++;
        LastMilliseconds = 0;
        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            var work = _pending[i];
            try
            {
                if (work.Key.Map != map) continue;
            }
            catch (Exception error) { RecordFailure(error); }
            Retire(work);
        }
    }
    public void Retire(IStreamReleaseWork work)
    {
        ArgumentNullException.ThrowIfNull(work);
        for (var index = _pending.Count - 1; index >= 0; index--)
            if (ReferenceEquals(_pending[index], work)) _pending.RemoveAt(index);
        foreach (var owner in _retiring) if (ReferenceEquals(owner, work)) return;
        _retiring.Add(work);
        try { if (work is IStreamMountWork mount) mount.Cancel(); }
        catch (Exception error) { RecordFailure(error); }
    }
    private void RecordFailure([FailureHandler("ENG-PERF-STREAM")] Exception error) =>
        Failure = RetainedFailure.Append(Failure, error);

    public void Drain(IAppScreenCoverage coverage, Action? prepare = null, Action<double>? finish = null, Action<double>? release = null, Func<bool>? canActivateVisual = null, Func<bool>? canReleaseRetired = null)
    {
        RetirementFailed = false;
        LastMilliseconds = 0;
        try { DrainFrame(coverage.IsCovered, prepare, finish, release, canActivateVisual, canReleaseRetired); }
        catch (Exception error)
        {
            RecordFailure(error);
            while (_pending.Count > 0) Retire(_pending[^1]);
            try { release?.Invoke(0); } catch (Exception cleanup) { RetirementFailed = true; RecordFailure(cleanup); }
            try { if (canReleaseRetired?.Invoke() != false) AdvanceRetirement(0); }
            catch (Exception cleanup) { RetirementFailed = true; RecordFailure(cleanup); }
        }
    }

    private void DrainFrame(bool covered, Action? prepare, Action<double>? finish, Action<double>? release, Func<bool>? canActivateVisual, Func<bool>? canReleaseRetired)
    {
        // Demand changes upgrade the existing work without cancelling or enqueuing another mount.
        var start = _clock();
        prepare?.Invoke();
        _pending.Sort(Compare);
        var frameBudget = covered ? KernelBudgets.StreamCoveredMountMilliseconds : _settings.MountMilliseconds;
        var collisionUsed = false; var visualUsed = false;
        for (var i = 0; i < _pending.Count;)
        {
            var work = _pending[i];
            if (work is IStreamMountCancellationState { IsCancelled: true }) { Retire(work); continue; }
            if (work.Revision != Revision) { Retire(work); continue; }
            var elapsed = _clock() - start;
            var remaining = frameBudget - elapsed;
            if (remaining <= 0) break;
            double budget;
            switch (work.Kind)
            {
                case StreamMountKind.Collision:
                    if (!covered && collisionUsed) { i++; continue; }
                    collisionUsed = true;
                    budget = covered ? remaining : Math.Min(KernelBudgets.CollisionMountMilliseconds, remaining); break;
                case StreamMountKind.Visual:
                    if (canActivateVisual is not null && !canActivateVisual()) { i++; continue; }
                    if (!covered && (visualUsed || !_watchdog.CanActivate(start))) { i++; continue; }
                    visualUsed = true;
                    budget = covered ? remaining : Math.Min(_watchdog.VisualBudgetMilliseconds, remaining); break;
                case StreamMountKind.Auxiliary:
                    budget = remaining; break;
                default: throw new ArgumentOutOfRangeException(nameof(work.Kind));
            }
            try
            {
                bool completed;
                var stepStart = _clock();
                try { completed = work.Step(budget); }
                finally { LastMilliseconds += _clock() - stepStart; }
                if (completed)
                {
                    if (work is IStreamMountCancellationState { IsCancelled: true }) Retire(work);
                    else _pending.RemoveAt(i);
                }
                else if (!covered) i++;
            }
            catch (Exception error)
            {
                RecordFailure(error);
                Retire(work);
            }
        }
        try
        {
            finish?.Invoke(covered || _watchdog.CanActivate(start)
                ? Math.Max(0, frameBudget - (_clock() - start)) : 0);
        }
        catch (Exception error) { RecordFailure(error); }
        try { release?.Invoke(Math.Max(0, frameBudget - (_clock() - start))); }
        catch (Exception error) { RetirementFailed = true; RecordFailure(error); }
        // Mounting has first use of the shared slice. One bounded retirement unit must still
        // progress when an indivisible native call consumed its remainder, after detach is safe (ENG-PERF-STREAM).
        for (var attempts = canReleaseRetired?.Invoke() != false ? _retiring.Count : 0; attempts > 0; attempts--)
        {
            var remaining = Math.Max(0, frameBudget - (_clock() - start));
            AdvanceRetirement(remaining);
            if (remaining <= 0 || _clock() - start >= frameBudget) break;
        }
        if (!covered) _watchdog.Observe(LastMilliseconds, _clock());
    }

    private void AdvanceRetirement(double budgetMilliseconds)
    {
        if (_retiring.Count == 0) return;
        _retirementCursor %= _retiring.Count;
        var owner = _retiring[_retirementCursor];
        try
        {
            if (owner.ReleaseStep(budgetMilliseconds)) { _retiring.RemoveAt(_retirementCursor); return; }
        }
        // The queue retains both the owner and its original failure for StreamDirector's map drain.
        catch (Exception error) { RetirementFailed = true; RecordFailure(error); }
        _retirementCursor++;
    }

}
