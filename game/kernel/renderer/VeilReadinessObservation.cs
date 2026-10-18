using Campus.Contracts;

namespace Campus.Kernel.Renderer;

// Local to one timeline. Only its owner frame advances time or mutates the scope collection.
internal sealed class VeilReadinessObservation
{
    private readonly int _ownerThread = Environment.CurrentManagedThreadId;
    private readonly List<Scope> _scopes = [];
    private bool _published;
    private bool _publishedFirstShaderCompilation;
    public bool Waiting => _scopes.Any(static scope => scope.Visible);
    public bool FirstShaderCompilation => _scopes.Any(static scope => scope.FirstShaderCompilation);

    public IDisposable Begin(VeilWaitKind kind, bool firstShaderCompilation)
    {
        RequireOwner();
        var first = kind switch
        {
            VeilWaitKind.Readiness => false,
            VeilWaitKind.BootFrontispiece => firstShaderCompilation,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var scope = new Scope(kind, first);
        _scopes.Add(scope);
        return scope;
    }

    public bool Advance(double delta)
    {
        RequireOwner();
        _scopes.RemoveAll(static scope => scope.Disposed);
        foreach (var scope in _scopes) scope.Elapsed += delta;
        var next = Waiting;
        var nextFirstShaderCompilation = FirstShaderCompilation;
        if (_published == next && _publishedFirstShaderCompilation == nextFirstShaderCompilation) return false;
        _published = next;
        _publishedFirstShaderCompilation = nextFirstShaderCompilation;
        return true;
    }

    public bool Stop()
    {
        RequireOwner();
        foreach (var scope in _scopes) scope.Dispose();
        _scopes.Clear();
        var changed = _published || _publishedFirstShaderCompilation;
        _published = false;
        _publishedFirstShaderCompilation = false;
        return changed;
    }

    private void RequireOwner()
    {
        if (Environment.CurrentManagedThreadId != _ownerThread)
            throw new InvalidOperationException("Readiness observations begin and advance on their owning kernel frame thread.");
    }

    private sealed class Scope(VeilWaitKind kind, bool firstShaderCompilation) : IDisposable
    {
        private int _disposed;
        public double Elapsed { get; set; }
        public bool Disposed => Volatile.Read(ref _disposed) != 0;
        public bool FirstShaderCompilation => !Disposed && firstShaderCompilation;
        public bool Visible => !Disposed && (kind switch
        {
            VeilWaitKind.Readiness => Elapsed > 1.5,
            VeilWaitKind.BootFrontispiece => Elapsed >= 4,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        });
        // Never call UI or mutate the collection from worker continuations.
        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
    }
}
