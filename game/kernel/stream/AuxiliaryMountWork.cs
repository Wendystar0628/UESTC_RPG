using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Stream;

/// <summary>Director-owned completion and stale-key handling around participant work.</summary>
internal sealed class AuxiliaryMountWork(IStreamMountWork work, Func<bool> current,
    Action mounted, Action<string> fail) : IStreamMountWork, IStreamMountCancellationState
{
    private bool _complete;
    public bool IsCancelled { get; private set; }
    public BakeKey Key => work.Key;
    public BakePriority Priority => work.Priority;
    public long Revision => work.Revision;
    public StreamMountKind Kind => StreamMountKind.Auxiliary;

    public bool Step(double budgetMilliseconds)
    {
        if (IsCancelled || _complete) return true;
        if (!current()) { Cancel(); return true; }
        try
        {
            if (!work.Step(budgetMilliseconds)) return false;
            _complete = true; mounted(); return true;
        }
        catch (Exception error) { fail($"Auxiliary mount failed: {Key}: {error.Message}"); Cancel(); return true; }
    }

    public void Cancel()
    {
        if (IsCancelled || _complete) return;
        IsCancelled = true; work.Cancel();
    }
}
