using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Frame;
using Godot;

namespace Campus.Kernel.Stream;

public interface IStreamMountFrame
{
    void Drain(bool covered, Action<double>? finish = null, Action<double>? release = null, Func<bool>? canActivateVisual = null);
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
    private readonly Action<string> _reportError;
    public StreamMountQueue(Func<double>? clock = null, Action<string>? reportError = null)
    {
        _watchdog = new(_settings.MountMilliseconds);
        _clock = clock ?? (() => FrameClock.MonotonicMilliseconds);
        _reportError = reportError ?? (message => GD.PushError(message));
    }
    public long Revision { get; private set; } = 1;
    public int Pending => _pending.Count;
    public int PeakPending { get; private set; }
    public long BlockedByRetirementFrames { get; private set; }
    public double LastMilliseconds { get; private set; }
    public int WatchdogTrips => _watchdog.Trips;
    public void Enqueue(IStreamMountWork work)
    {
        ArgumentNullException.ThrowIfNull(work);
        if (work.Revision != Revision) { work.Cancel(); return; }
        _pending.Add(work);
        PeakPending = Math.Max(PeakPending, _pending.Count);
        _pending.Sort(Compare);
    }
    private static int Compare(IStreamMountWork a, IStreamMountWork b)
    {
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
        for (var i = _pending.Count - 1; i >= 0; i--)
            if (_pending[i].Key.Map == map)
            {
                _pending[i].Cancel(); _pending.RemoveAt(i);
            }
    }
    public void Drain(bool covered, Action<double>? finish = null, Action<double>? release = null, Func<bool>? canActivateVisual = null)
    {
        var start = _clock();
        var frameBudget = covered ? KernelBudgets.StreamCoveredMountMilliseconds : _settings.MountMilliseconds;
        var retiring = canActivateVisual is not null && !canActivateVisual();
        release?.Invoke(covered ? frameBudget : retiring
            ? Math.Max(0, frameBudget - KernelBudgets.CollisionMountMilliseconds) : Math.Min(.25, frameBudget));
        var collisionUsed = false; var visualUsed = false;
        var blockedByRetirement = false;
        for (var i = 0; i < _pending.Count;)
        {
            var work = _pending[i];
            if (work is IStreamMountCancellationState { IsCancelled: true }) { _pending.RemoveAt(i); continue; }
            if (work.Revision != Revision) { work.Cancel(); _pending.RemoveAt(i); continue; }
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
                    if (canActivateVisual is not null && !canActivateVisual()) { blockedByRetirement = true; i++; continue; }
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
                try { work.Cancel(); }
                catch (Exception cancellationError) { _reportError($"Stream mount cancellation failed: {work.Key}: {cancellationError}"); }
                _pending.RemoveAt(i);
                _reportError($"Stream mount failed: {work.Key}: {error}");
            }
        }
        if (blockedByRetirement || retiring && !visualUsed &&
            _pending.Any(work => work.Kind == StreamMountKind.Visual && work is not IStreamMountCancellationState { IsCancelled: true }))
            BlockedByRetirementFrames++;
        finish?.Invoke(covered || _watchdog.CanActivate(start)
            ? Math.Max(0, frameBudget - (_clock() - start)) : 0);
        LastMilliseconds = _clock() - start;
        if (!covered) _watchdog.Observe(LastMilliseconds, _clock());
    }
}
