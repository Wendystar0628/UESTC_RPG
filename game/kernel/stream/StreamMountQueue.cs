using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Diagnostics;
using Campus.Kernel.Frame;
using Godot;

namespace Campus.Kernel.Stream;

public interface IStreamMountFrame
{
    void Drain(IAppScreenCoverage coverage, Action<double>? finish = null, Action<double>? release = null, Func<bool>? canActivateVisual = null);
}

/// <summary>The sole owner of the shared collision/visual mount budget and covered-screen refresh slices.</summary>
[KernelService(typeof(IStreamMountFrame))]
[KernelService(typeof(IStreamMountQueue))]
public sealed class StreamMountQueue : IStreamMountQueue, IStreamMountFrame
{
    private readonly List<IStreamMountWork> _pending = [];
    private readonly Campus.Kernel.Lod.StreamSettings _settings = new();
    private readonly MountWatchdog _watchdog;
    private readonly Func<double> _clock;
    public StreamMountQueue(Func<double>? clock = null)
    {
        _watchdog = new(_settings.MountMilliseconds);
        _clock = clock ?? (() => FrameClock.MonotonicMilliseconds);
    }
    public Exception? Failure { get; private set; }
    public long Revision { get; private set; } = 1;
    public int Pending => _pending.Count;
    public double LastMilliseconds { get; private set; }
    public int WatchdogTrips => _watchdog.Trips;
    public void Enqueue(IStreamMountWork work)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(work);
            if (work.Revision != Revision) { Cancel(work); return; }
            _pending.Add(work);
        }
        catch (Exception error)
        {
            RecordFailure(error);
            if (work is not null) { Cancel(work); _pending.Remove(work); }
        }
    }
    private static int Compare(IStreamMountWork a, IStreamMountWork b)
    {
        var background = (a.Priority == BakePriority.Background).CompareTo(b.Priority == BakePriority.Background);
        if (background != 0) return background;
        var kind = Rank(a.Kind).CompareTo(Rank(b.Kind));
        if (kind != 0) return kind;
        var priority = a.Priority.CompareTo(b.Priority);
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
            Cancel(work); _pending.RemoveAt(i);
        }
    }
    private void Cancel(IStreamMountWork work)
    {
        try { work.Cancel(); }
        catch (Exception error) { RecordFailure(error); }
    }
    private void RecordFailure([FailureHandler("ENG-PERF-STREAM")] Exception error) =>
        Failure = RetainedFailure.Append(Failure, error);

    public void Drain(IAppScreenCoverage coverage, Action<double>? finish = null, Action<double>? release = null, Func<bool>? canActivateVisual = null)
    {
        try { DrainFrame(coverage.IsCovered, finish, release, canActivateVisual); }
        catch (Exception error)
        {
            RecordFailure(error);
            foreach (var work in _pending) Cancel(work);
            _pending.Clear();
        }
    }

    private void DrainFrame(bool covered, Action<double>? finish, Action<double>? release, Func<bool>? canActivateVisual)
    {
        // Demand changes upgrade the existing work without cancelling or enqueuing another mount.
        var start = _clock();
        _pending.Sort(Compare);
        var frameBudget = covered ? KernelBudgets.StreamCoveredMountMilliseconds : _settings.MountMilliseconds;
        var retiring = canActivateVisual is not null && !canActivateVisual();
        try
        {
            release?.Invoke(covered ? frameBudget : retiring
                ? Math.Max(0, frameBudget - KernelBudgets.CollisionMountMilliseconds) : Math.Min(.25, frameBudget));
        }
        catch (Exception error) { RecordFailure(error); }
        var collisionUsed = false; var visualUsed = false;
        for (var i = 0; i < _pending.Count;)
        {
            var work = _pending[i];
            if (work is IStreamMountCancellationState { IsCancelled: true }) { _pending.RemoveAt(i); continue; }
            if (work.Revision != Revision) { Cancel(work); _pending.RemoveAt(i); continue; }
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
                if (work.Step(budget)) _pending.RemoveAt(i);
                else if (!covered) i++;
            }
            catch (Exception error)
            {
                RecordFailure(error);
                Cancel(work);
                _pending.RemoveAt(i);
            }
        }
        try
        {
            finish?.Invoke(covered || _watchdog.CanActivate(start)
                ? Math.Max(0, frameBudget - (_clock() - start)) : 0);
        }
        catch (Exception error) { RecordFailure(error); }
        LastMilliseconds = _clock() - start;
        if (!covered) _watchdog.Observe(LastMilliseconds, _clock());
    }

}
