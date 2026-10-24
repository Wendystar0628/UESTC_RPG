using Campus.Contracts;

namespace Campus.Kernel.Diagnostics;

/// <summary>BCL multicast delegates stop at the first exception. Command observations must reach every
/// subscriber without changing the owner's result; its exit boundary receives the original failures.</summary>
internal sealed class CommandNotification
{
    private readonly object _gate = new();
    private Exception? _failure;

    internal void Publish(Action? observers)
    {
        if (observers is null) return;
        foreach (Action observer in observers.GetInvocationList())
        {
            try { observer(); }
            catch (Exception error) { Retain(error); }
        }
    }

    internal void Publish<T>(Action<T>? observers, T value)
    {
        if (observers is null) return;
        foreach (Action<T> observer in observers.GetInvocationList())
        {
            try { observer(value); }
            catch (Exception error) { Retain(error); }
        }
    }

    internal void Publish<T1, T2>(Action<T1, T2>? observers, T1 first, T2 second)
    {
        if (observers is null) return;
        foreach (Action<T1, T2> observer in observers.GetInvocationList())
        {
            try { observer(first, second); }
            catch (Exception error) { Retain(error); }
        }
    }

    private void Retain([FailureHandler("ENG-ARCH")] Exception error)
    {
        lock (_gate) _failure = RetainedFailure.Append(_failure, error);
    }

    internal Exception? TakeFailure()
    {
        lock (_gate)
        {
            var failure = _failure;
            _failure = null;
            return failure;
        }
    }
}
