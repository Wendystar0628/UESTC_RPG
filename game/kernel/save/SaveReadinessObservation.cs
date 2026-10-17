namespace Campus.Kernel.Save;

// One WaitDestination invocation owns one holder before it queues any main-frame action.
// Cancellation may finish the worker before the queued action attaches its main-thread-created token.
internal sealed class SaveReadinessObservation : IDisposable
{
    private readonly object _gate = new();
    private bool _stopped;
    private bool _attached;
    private IDisposable? _scope;

    public void Attach(IDisposable scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        bool dispose;
        lock (_gate)
        {
            if (_attached) throw new InvalidOperationException("A save destination observation can attach only once.");
            _attached = true;
            dispose = _stopped;
            if (!dispose) _scope = scope;
        }
        // Tokens only mark termination; neither path publishes UI events on this thread.
        if (dispose) scope.Dispose();
    }

    public void Dispose()
    {
        IDisposable? scope;
        lock (_gate)
        {
            _stopped = true;
            scope = _scope;
            _scope = null;
        }
        scope?.Dispose();
    }
}
