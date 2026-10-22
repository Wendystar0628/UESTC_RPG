using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Diagnostics;

namespace Campus.Kernel.Stream;

internal interface IStreamMountCancellationState
{
    bool IsCancelled { get; }
}

/// <summary>One bounded main-thread mount lifecycle; completed resources belong to the participant.</summary>
public abstract class StreamMountWork(BakeKey key, BakePriority priority, long revision, StreamMountKind kind)
    : IStreamMountWork, IStreamMountCancellationState
{
    public BakeKey Key { get; } = key;
    public virtual BakePriority Priority { get; } = priority;
    public long Revision { get; } = revision;
    public StreamMountKind Kind { get; } = kind;
    public bool Complete { get; private set; }
    public bool IsCancelled { get; private set; }
    public Exception? Failure { get; private set; }

    protected abstract bool IsCurrent { get; }
    protected abstract bool MountStep(double budgetMilliseconds);
    protected abstract void Release();
    protected abstract void Failed(Exception error);

    public bool Step(double budgetMilliseconds)
    {
        if (Complete || IsCancelled) return true;
        try
        {
            if (!IsCurrent) { Cancel(); return true; }
            if (budgetMilliseconds <= 0) return false;
            if (!MountStep(budgetMilliseconds)) return false;
            if (!IsCancelled) Complete = true;
            return true;
        }
        catch (Exception error)
        {
            Exception failure = error;
            try { Cancel(); }
            catch (Exception cleanup) { failure = RetainedFailure.Append(failure, cleanup); }
            try { Failed(failure); }
            catch (Exception reporting) { failure = RetainedFailure.Append(failure, reporting); }
            Failure = failure;
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            throw;
        }
    }

    public void Cancel()
    {
        if (IsCancelled || Complete) return;
        IsCancelled = true;
        Release();
    }
}
