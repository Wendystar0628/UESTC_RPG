using System.Diagnostics.CodeAnalysis;
using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>
/// Kernel handoff for existing global data textures: WP-37 owns palette production and WP-04 owns mat_table
/// production. No texture is created or shader global read by this service.
/// </summary>
public interface IGlobalDataTextureRegistry : IDisposable
{
    /// <summary>
    /// Publishes an already created texture and its RenderingServer RID on the main thread.
    /// Ownership transfers only on success: the caller must then never dispose the texture or free its RID.
    /// Update/SetImage remain the producer's responsibility while the returned registration is active.
    /// </summary>
    /// <remarks>
    /// textureRid must be this texture's existing RenderingServer RID, not a RenderingDevice RID. The producer
    /// obtains it after creating the texture; registration itself does not call GetRid or allocate GPU storage.
    /// Stop updates and remove the global binding before disposing the registration. Disposal unpublishes
    /// immediately without waiting for existing readers; the registry releases native resources afterwards.
    /// </remarks>
    IDisposable RegisterOwned(string name, Texture2D texture, Rid textureRid);

    /// <summary>
    /// Acquires one read lease on the main or render thread. Returns false when absent or after shutdown.
    /// Acquire before queuing a render callback and dispose inside its finally block after issuing its commands.
    /// Do not retain a bare RID after the lease ends, or dispose a lease concurrently with its use.
    /// </summary>
    bool TryAcquire(string name, [NotNullWhen(true)] out GlobalDataTextureLease? lease);

    /// <summary>
    /// Closes registration/acquisition immediately and asynchronously waits for all owned textures to finish
    /// native disposal, including textures pinned by callbacks queued before shutdown. Repeated calls return
    /// the same task. Call before SceneTree.Quit while the main loop and RenderingServer are still running.
    /// </summary>
    /// <remarks>
    /// First stop producer updates and new post callbacks, and clear their global bindings. Already acquired
    /// leases must still be returned by their callbacks. Await this task; never use Wait, Result or a blocking
    /// render-thread fence. The engine must keep processing callbacks until completion, including when the
    /// main and render threads are the same. Dispose alone is a non-awaiting exit-tree fallback.
    /// </remarks>
    Task ShutdownAsync();
}

/// <summary>
/// Pins one registered texture's lifetime. It caches only the RenderingServer RID, never a RenderingDevice RID.
/// A lease can be transferred from main to render thread; only its current owner may use or dispose it.
/// </summary>
public sealed class GlobalDataTextureLease : IDisposable
{
    private readonly Rid _textureRid;
    private Action? _release;

    internal GlobalDataTextureLease(Rid textureRid, Action release)
    {
        _textureRid = textureRid;
        _release = release;
    }

    /// <summary>The RenderingServer texture RID; valid only while this lease is alive.</summary>
    public Rid TextureRid => Volatile.Read(ref _release) is not null ? _textureRid
        : throw new ObjectDisposedException(nameof(GlobalDataTextureLease));

    /// <summary>
    /// Call inside the current frame's render callback. SetImage can replace the underlying RD texture, so
    /// WP-05 must obtain this RID again each frame and refresh dependent bindings when it changes.
    /// Pass srgb=true for palette colour sampling and false for mat_table data. An invalid RID means the
    /// current renderer has no RD texture; skip that compute work. Never free this borrowed RID.
    /// </summary>
    public Rid GetRdTextureOnRenderThread(bool srgb = false) => RenderingServer.TextureGetRdTexture(TextureRid, srgb);

    public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
}

/// <summary>
/// Thread-safe publication and lease accounting; construction and registration create no GPU resources.
/// Unregistration and shutdown never wait for rendering. A retired texture is released on the render thread
/// only after its last lease ends, including leases for callbacks which have not started yet.
/// </summary>
/// <remarks>
/// Shutdown order: stop producers and new post callbacks, clear their global bindings, then await ShutdownAsync
/// while the main loop and RenderingServer are alive. Already queued callbacks finish and dispose their leases;
/// the task completes after the resulting native texture disposals, before the host calls SceneTree.Quit.
/// The exit-tree fallback may call Dispose but cannot replace this earlier asynchronous shutdown step.
/// Dispose is idempotent, rejects future registrations and makes future TryAcquire calls return false.
/// This service does not dispose producer-owned Images, shaders or compute bindings.
/// </remarks>
[KernelService(typeof(IGlobalDataTextureRegistry), Owner = "WP-00")]
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
