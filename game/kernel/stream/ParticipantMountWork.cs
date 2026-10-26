using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Stream;

/// <summary>Director-owned completion and stale-key handling around participant work.</summary>
internal sealed class ParticipantMountWork(IStreamMountWork work, Func<bool> current,
    Action mounted, Action released, Action<Exception> fail, Func<BakePriority> priority)
    : StreamMountWork(work.Key, work.Priority, work.Revision, work.Kind)
{
    private bool _released;
    public override BakePriority Priority => priority();
    protected override bool IsCurrent => current();

    protected override bool MountStep(double budgetMilliseconds)
    {
        if (!work.Step(budgetMilliseconds)) return false;
        if (work is IStreamMountCancellationState { IsCancelled: true }) { Cancel(); return true; }
        mounted();
        return true;
    }

    protected override void Cancelled() => work.Cancel();
    protected override bool Release(double budgetMilliseconds)
    {
        if (!work.ReleaseStep(budgetMilliseconds)) return false;
        if (!_released) { _released = true; released(); }
        return true;
    }
    protected override void Failed(Exception error) => fail(error);
}
