using System.Diagnostics.CodeAnalysis;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Godot;

namespace Campus.Contracts;

/// <summary>
/// RG32F vegetation near-pool readiness: R is actual ready (0/1); G is zero when settled, otherwise a signed FadeClock
/// start biased by WorldTime.AnimLoopSeconds (positive enter, negative exit). Exit keeps R=1 until its fade finishes.
/// No mipmaps or colour conversion; nearest sampling with repeat disabled. World X is the column, world Z the row.
/// Texel (0,0) covers the L0 integer cell containing Rect.Min, including a non-aligned map rectangle.
/// </summary>
public static class VegetationNearReadyEncoding
{
    public const int CellMetres = TileGrid.BaseEdge;
    public const float StartBiasSeconds = WorldTime.AnimLoopSeconds;

    public static float EncodeStart(float clock, bool entering) => (entering ? 1f : -1f) * (clock + StartBiasSeconds);

    public static TileKey Origin(MapRect rect) => TileGrid.At(rect.Min);

    public static int Width(MapRect rect) => checked((int)MathF.Ceiling(rect.Max.X / CellMetres) - Origin(rect).X);

    public static int Height(MapRect rect) => checked((int)MathF.Ceiling(rect.Max.Y / CellMetres) - Origin(rect).Z);

    /// <summary>Out-of-range owner cells are not ready; never clamp them onto an edge texel.</summary>
    public static bool TryTexel(MapRect rect, TileKey tile, out int x, out int y)
    {
        var origin = Origin(rect);
        x = tile.X - origin.X;
        y = tile.Z - origin.Z;
        return x >= 0 && y >= 0 && x < Width(rect) && y < Height(rect);
    }
}

/// <summary>
/// RGBA32F near-tree transition texels: (source level, target level, FadeClock start, reserved zero).
/// Steady texels have equal source/target levels. A pool's contiguous block is addressed by VegTransitionBase + INSTANCE_ID.
/// Compaction changes texel placement only; the producer retains each original tree record's transition state.
/// </summary>
public static class VegetationTransitionEncoding
{
    public const int TextureWidth = 1024;
    public const int Channels = 4;
}

/// <summary>
/// Kernel handoff for producer-owned global data textures. Palette, materials, sky and vegetation retain their own
/// production/update responsibilities. No texture is created or shader global read by this service.
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
    /// main and render threads are the same. Dispose starts exit-tree shutdown without awaiting native disposal.
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

    public GlobalDataTextureLease(Rid textureRid, Action release)
    {
        _textureRid = textureRid;
        _release = release;
    }

    /// <summary>The RenderingServer texture RID; valid only while this lease is alive.</summary>
    public Rid TextureRid => Volatile.Read(ref _release) is not null ? _textureRid
        : throw new ObjectDisposedException(nameof(GlobalDataTextureLease));

    /// <summary>
    /// Call inside the current frame's render callback. SetImage can replace the underlying RD texture, so
    /// game/kernel/post/ must obtain this RID again each frame and refresh dependent bindings when it changes.
    /// Pass srgb=true for palette colour sampling and false for mat_table data. An invalid RID means the
    /// current renderer has no RD texture; skip that compute work. Never free this borrowed RID.
    /// </summary>
    public Rid GetRdTextureOnRenderThread(bool srgb = false) => RenderingServer.TextureGetRdTexture(TextureRid, srgb);

    public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
}
