using Campus.Contracts;
using Campus.Contracts.Bake;

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
    public BakePriority Priority { get; } = priority;
    public long Revision { get; } = revision;
    public StreamMountKind Kind { get; } = kind;
    public bool Complete { get; private set; }
    public bool IsCancelled { get; private set; }
    public string? Error { get; private set; }

    protected abstract bool IsCurrent { get; }
    protected abstract bool MountStep(double budgetMilliseconds);
    protected abstract void Release();
    protected abstract void Failed(string error);

    public bool Step(double budgetMilliseconds)
    {
        if (Complete || IsCancelled) return true;
        if (!IsCurrent) { Cancel(); return true; }
        if (budgetMilliseconds <= 0) return false;
        try
        {
            if (!MountStep(budgetMilliseconds)) return false;
            if (!IsCancelled) Complete = true;
            return true;
        }
        catch (Exception error)
        {
            Error = error.GetBaseException().Message;
            try { Failed(Error); }
            finally { Cancel(); }
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
