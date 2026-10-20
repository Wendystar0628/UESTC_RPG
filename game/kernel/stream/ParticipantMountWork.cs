using Campus.Contracts;

namespace Campus.Kernel.Stream;

/// <summary>Director-owned completion and stale-key handling around participant work.</summary>
internal sealed class ParticipantMountWork(IStreamMountWork work, Func<bool> current,
    Action mounted, Action<Exception> fail)
    : StreamMountWork(work.Key, work.Priority, work.Revision, work.Kind)
{
    protected override bool IsCurrent => current();

    protected override bool MountStep(double budgetMilliseconds)
    {
        if (!work.Step(budgetMilliseconds)) return false;
        if (work is IStreamMountCancellationState { IsCancelled: true }) { Cancel(); return true; }
        mounted();
        return true;
    }

    protected override void Release() => work.Cancel();
    protected override void Failed(Exception error) => fail(error);
}
