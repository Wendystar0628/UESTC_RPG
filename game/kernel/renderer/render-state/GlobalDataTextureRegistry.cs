using System.Diagnostics.CodeAnalysis;
using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>
/// Thread-safe publication and lease accounting; construction and registration create no GPU resources.
/// Unregistration and shutdown never wait for rendering. A retired texture is released on the render thread
/// only after its last lease ends, including leases for callbacks which have not started yet.
/// </summary>
/// <remarks>
/// Shutdown order: stop producers and new post callbacks, clear their global bindings, then await ShutdownAsync
/// while the main loop and RenderingServer are alive. Already queued callbacks finish and dispose their leases;
/// the task completes after the resulting native texture disposals, before the host calls SceneTree.Quit.
/// Exit-tree disposal cannot replace this earlier asynchronous shutdown step.
/// Dispose is idempotent, rejects future registrations and makes future TryAcquire calls return false.
/// This service does not dispose producer-owned Images, shaders or compute bindings.
/// </remarks>
[KernelService(typeof(IGlobalDataTextureRegistry))]
public sealed class GlobalDataTextureRegistry : IGlobalDataTextureRegistry, IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _published = new(StringComparer.Ordinal);
    // Retired entries stay strongly owned until their render-thread disposal finishes.
    private readonly Dictionary<Rid, Entry> _owned = [];
    private readonly TaskCompletionSource _shutdown = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _closed;

    public ValueTask DisposeAsync() => new(ShutdownAsync());

    public IDisposable RegisterOwned(string name, Texture2D texture, Rid textureRid)
    {
        ValidateName(name);
        ArgumentNullException.ThrowIfNull(texture);
        if (OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Global data textures must be registered on the main thread.");
        if (!GodotObject.IsInstanceValid(texture)) throw new ObjectDisposedException(nameof(texture));
        if (!textureRid.IsValid) throw new ArgumentException("An existing RenderingServer texture RID is required.", nameof(textureRid));

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            if (_published.ContainsKey(name)) throw new InvalidOperationException($"Global data texture is already registered: {name}");
            if (_owned.ContainsKey(textureRid) || _owned.Values.Any(entry => ReferenceEquals(entry.Texture, texture)))
                throw new InvalidOperationException("Texture ownership has already been transferred to this registry.");
            var entry = new Entry(name, texture, textureRid);
            _owned.Add(textureRid, entry);
            _published.Add(name, entry);
            return new Registration(() => Unregister(entry));
        }
    }

    public bool TryAcquire(string name, [NotNullWhen(true)] out GlobalDataTextureLease? lease)
    {
        ValidateName(name);
        lock (_gate)
        {
            if (_closed || !_published.TryGetValue(name, out var entry))
            {
                lease = null;
                return false;
            }
            entry.Readers++;
            lease = new GlobalDataTextureLease(entry.TextureRid, () => ReleaseLease(entry));
            return true;
        }
    }

    public void Dispose()
    {
        Entry[] ready;
        lock (_gate)
        {
            if (_closed) return;
            _closed = true;
            var retired = _published.Values.OrderBy(entry => entry.Name, StringComparer.Ordinal).ToArray();
            _published.Clear();
            foreach (var entry in retired) entry.Published = false;
            ready = retired.Where(MarkReadyForRelease).ToArray();
            CompleteShutdownIfDrained();
        }
        foreach (var entry in ready) QueueRelease(entry);
    }

    public Task ShutdownAsync()
    {
        Dispose();
        return _shutdown.Task;
    }

    private void Unregister(Entry entry)
    {
        bool release;
        lock (_gate)
        {
            if (entry.Published)
            {
                _published.Remove(entry.Name);
                entry.Published = false;
            }
            release = MarkReadyForRelease(entry);
        }
        if (release) QueueRelease(entry);
    }

    private void ReleaseLease(Entry entry)
    {
        bool release;
        lock (_gate)
        {
            entry.Readers--;
            release = MarkReadyForRelease(entry);
        }
        if (release) QueueRelease(entry);
    }

    // Called only with _gate held. Exactly one path can schedule an entry's native release.
    private static bool MarkReadyForRelease(Entry entry)
    {
        if (entry.Published || entry.Readers != 0 || entry.ReleaseQueued) return false;
        entry.ReleaseQueued = true;
        return true;
    }

    private void QueueRelease(Entry entry)
    {
        try
        {
            RenderingServer.CallOnRenderThread(Callable.From(() =>
            {
                try
                {
                    entry.Texture.Dispose();
                    lock (_gate)
                    {
                        _owned.Remove(entry.TextureRid);
                        CompleteShutdownIfDrained();
                    }
                }
                catch (Exception error)
                {
                    // Do not report a successful drain if native disposal failed.
                    _shutdown.TrySetException(error);
                }
            }));
        }
        catch (Exception error)
        {
            // Surface a scheduling failure through the shutdown task instead of losing it during teardown.
            _shutdown.TrySetException(error);
        }
    }

    // Called only with _gate held. Completing the task never inlines its awaiting continuations.
    private void CompleteShutdownIfDrained()
    {
        if (_closed && _owned.Count == 0) _shutdown.TrySetResult();
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name != name.Trim())
            throw new ArgumentException("A global data texture name must be non-empty without surrounding whitespace.", nameof(name));
    }

    private sealed class Entry(string name, Texture2D texture, Rid textureRid)
    {
        internal readonly string Name = name;
        internal readonly Texture2D Texture = texture;
        internal readonly Rid TextureRid = textureRid;
        internal int Readers;
        internal bool Published = true;
        internal bool ReleaseQueued;
    }

    private sealed class Registration(Action release) : IDisposable
    {
        private Action? _release = release;
        public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
    }
}
