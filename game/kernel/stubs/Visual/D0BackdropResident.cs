using Campus.Contracts;

namespace Campus.Kernel.Stubs.Visual;

/// <summary>D0 whole-map lifetime. Calls originate on the composition thread; only Build runs on a worker.</summary>
public sealed class D0BackdropResident(IRenderHost renderer, Action<Exception> reportDiscarded,
    Func<Func<ContentOutput>, Task<ContentOutput>>? run = null) : IDisposable
{
    private readonly Func<Func<ContentOutput>, Task<ContentOutput>> _run = run ?? (build => Task.Run(build));
    private long _epoch;
    private bool _disposed;
    private RenderHandle? _handle;
    private BuildContext? _context;
    public Task Pending { get; private set; } = Task.CompletedTask;
    public bool Ready { get; private set; }
    public int SurfaceCount { get; private set; }
    public int Triangles { get; private set; }

    public Task Load(BuildContext context, IContentModule module, WpId owner)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (ReferenceEquals(_context, context)) return Pending;
        Unload();
        _context = context;
        var epoch = _epoch;
        Pending = BuildAndMount();
        return Pending;

        async Task BuildAndMount()
        {
            ContentOutput output;
            try { output = await _run(() => module.Build(context, new TileKey(0, 0), LodLevel.L4)); }
            catch (Exception error)
            {
                if (_disposed || epoch != _epoch) { reportDiscarded(error); return; }
                _context = null;
                throw;
            }
            if (_disposed || epoch != _epoch) return;
            try
            {
                if (output.Meshes.Count != 0 || output.Parts.Count != 0 || output.Instances.Count != 0)
                    _handle = renderer.Mount(output, owner);
                SurfaceCount = output.Meshes.Count;
                Triangles = output.Meshes.Sum(mesh => mesh.Index.Length / 3);
                Ready = true;
            }
            catch
            {
                _context = null;
                if (_handle is { } failed)
                {
                    _handle = null;
                    renderer.Unmount(failed);
                }
                SurfaceCount = 0;
                Triangles = 0;
                throw;
            }
        }
    }

    public void Unload()
    {
        ++_epoch; // Even re-entry into the same map ID invalidates old work.
        Ready = false;
        SurfaceCount = 0;
        Triangles = 0;
        _context = null;
        if (_handle is not { } handle) return;
        _handle = null;
        renderer.Unmount(handle);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Unload();
    }
}
