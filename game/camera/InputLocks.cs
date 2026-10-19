using Campus.Contracts;

namespace Campus.Camera;

public sealed class InputLocks
{
    private readonly int[] _counts = new int[Enum.GetValues<InputLockReason>().Length];
    public bool Locked { get; private set; }
    public int Count(InputLockReason reason) => _counts[Index(reason)];
    public IDisposable Acquire(InputLockReason reason)
    {
        _counts[Index(reason)]++;
        Locked = true;
        return new Lease(this, reason);
    }
    private void Release(InputLockReason reason)
    {
        _counts[Index(reason)]--;
        Locked = _counts.Any(count => count > 0);
    }
    private static int Index(InputLockReason reason) => reason switch
    {
        InputLockReason.Atlas or InputLockReason.AppFlow or InputLockReason.MapTravel or
        InputLockReason.Menu or InputLockReason.Rebind => (int)reason,
        _ => throw new ArgumentOutOfRangeException(nameof(reason))
    };
    private sealed class Lease(InputLocks owner, InputLockReason reason) : IDisposable
    {
        private InputLocks? _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Release(reason);
    }
}
