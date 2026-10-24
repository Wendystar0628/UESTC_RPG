using System.Runtime.ExceptionServices;
using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Post;

[KernelNode("post.world-transition")]
[KernelService(typeof(IWorldTransitionPresentation))]
public partial class WorldTransitionPresentation : CanvasLayer, IWorldTransitionPresentation, IRegistryExitParticipant
{
    private readonly TextureRect _previous = new()
    {
        MouseFilter = Control.MouseFilterEnum.Ignore,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
    };
    private readonly List<Capture> _owned = [];
    private Capture? _current;
    private Task? _drain;
    private double _elapsed;
    private float _duration;
    private bool _blending;
    private bool _stopping;

    public WorldTransitionPresentation()
    {
        Layer = 5;
        ProcessMode = ProcessModeEnum.Always;
        ProcessPriority = FramePriority.Hud;
    }

    public override void _Ready()
    {
        AddChild(_previous);
        _previous.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _previous.Hide();
    }

    public void CapturePreviousFrame()
    {
        RequireMainThread();
        ObjectDisposedException.ThrowIf(_stopping, this);
        if (!IsInsideTree()) throw new InvalidOperationException("World transitions require a mounted presentation.");
        var source = GetViewport().GetTexture();
        RetireCurrent();
        var capture = new Capture();
        _owned.Add(capture);
        _current = capture;
        capture.Work = RunCapture(capture, source);
        // Safe render mode executes the queued command here. Preserve an immediate native failure
        // at the requesting call instead of replacing it with a later "no capture" error.
        if (capture.Work.IsFaulted) throw capture.Work.Exception!;
    }

    public Task Crossfade(float seconds)
    {
        RequireMainThread();
        if (!float.IsFinite(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        var capture = _current ?? throw new InvalidOperationException("Capture the previous scene before crossfading.");
        if (!capture.Duration.TrySetResult(seconds))
            throw new InvalidOperationException("This captured frame is already crossfading.");
        return capture.Work;
    }

    private async Task RunCapture(Capture capture, ViewportTexture source)
    {
        Rid target = default;
        Texture2Drd? texture = null;
        Exception? failure = null;
        try
        {
            // Queue before the caller changes world globals. Resolve the borrowed RD RID only inside
            // this callback, so resize commands cannot invalidate it between lookup and copy.
            var sourceRid = source.GetRid();
            target = await OnRenderThread(device => CopyFrame(device, sourceRid));
            GC.KeepAlive(source);
            if (!capture.Stop.IsCancellationRequested)
            {
                texture = new Texture2Drd();
                texture.TextureRdRid = target;
                // The setter itself queues render work. Drain that binding before publication or disposal;
                // this is a command-queue acknowledgement, not a GPU fence or readback.
                await OnRenderThread(static _ => true);
                if (texture.TextureRdRid != target)
                    throw new InvalidOperationException("The transition texture was not bound to its GPU snapshot.");
                if (!capture.Stop.IsCancellationRequested)
                    await PresentCapture(capture, texture);
            }
        }
        catch (OperationCanceledException) when (capture.Stop.IsCancellationRequested) { }
        catch (Exception error) { failure = error; }

        try
        {
            if (ReferenceEquals(capture, _current)) DetachCurrent();
        }
        catch (Exception error) { failure = failure is null ? error : new AggregateException(failure, error); }
        try { await ReleaseGpu(texture, target); }
        catch (Exception error) { failure = failure is null ? error : new AggregateException(failure, error); }
        capture.Stop.Dispose();
        // Failed work remains owned until registry drain, even if CapturePreviousFrame had no consumer.
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
        _owned.Remove(capture);
    }

    private async Task PresentCapture(Capture capture, Texture2Drd texture)
    {
        _previous.Texture = texture;
        _previous.Modulate = Colors.White;
        _previous.Show();
        var seconds = await capture.Duration.Task;
        capture.Stop.Token.ThrowIfCancellationRequested();
        // Keep the still intact while the new world reaches a real drawn frame.
        await FrameAwaiter.ReadyDrawAsync(this, () => true, TimeSpan.FromSeconds(30), capture.Stop.Token);
        capture.Stop.Token.ThrowIfCancellationRequested();
        _elapsed = 0;
        _duration = seconds;
        _blending = true;
        await capture.Faded.Task;
    }

    public override void _Process(double delta)
    {
        if (!_blending) return;
        _elapsed += Math.Max(0, delta);
        var alpha = 1f - (float)Math.Min(1, _elapsed / _duration);
        _previous.Modulate = new Color(1, 1, 1, alpha);
        if (alpha > 0) return;
        var capture = _current!;
        DetachCurrent();
        capture.Faded.TrySetResult();
    }

    private void DetachCurrent()
    {
        _blending = false;
        _previous.Hide();
        _previous.Texture = null;
        _current = null;
    }

    private void RetireCurrent()
    {
        if (_current is not { } capture) return;
        DetachCurrent();
        capture.Stop.Cancel();
        capture.Duration.TrySetResult(0);
        capture.Faded.TrySetResult();
    }

    public void PrepareForExit()
    {
        RequireMainThread();
        if (_stopping) return;
        _stopping = true;
        RetireCurrent();
    }

    public Task DrainForExitAsync()
    {
        PrepareForExit();
        return _drain ??= DrainCaptures();
    }

    private async Task DrainCaptures()
    {
        var pending = Task.WhenAll(_owned.Select(capture => capture.Work));
        try { await pending; }
        catch { throw pending.Exception!; }
        _owned.Clear();
    }

    public override void _ExitTree()
    {
        PrepareForExit();
        if (_owned.Any(capture => !capture.Work.IsCompleted))
            throw new InvalidOperationException("Registry exit must drain transition snapshots before removing their viewport.");
    }

    private static Rid CopyFrame(RenderingDevice device, Rid sourceTexture)
    {
        var source = RenderingServer.TextureGetRdTexture(sourceTexture);
        if (!source.IsValid || !device.TextureIsValid(source))
            throw new InvalidOperationException("The viewport has no valid GPU texture to capture.");
        using var sourceFormat = device.TextureGetFormat(source);
        if (sourceFormat.TextureType != RenderingDevice.TextureType.Type2D || sourceFormat.ArrayLayers != 1 ||
            sourceFormat.Samples != RenderingDevice.TextureSamples.Samples1 || sourceFormat.Width == 0 || sourceFormat.Height == 0 ||
            (sourceFormat.UsageBits & RenderingDevice.TextureUsageBits.CanCopyFromBit) == 0)
            throw new InvalidOperationException("The viewport GPU texture does not support a resolved 2D snapshot copy.");
        using var format = new RDTextureFormat
        {
            Format = sourceFormat.Format,
            Width = sourceFormat.Width,
            Height = sourceFormat.Height,
            Depth = 1,
            ArrayLayers = 1,
            Mipmaps = 1,
            TextureType = RenderingDevice.TextureType.Type2D,
            Samples = RenderingDevice.TextureSamples.Samples1,
            UsageBits = RenderingDevice.TextureUsageBits.SamplingBit | RenderingDevice.TextureUsageBits.CanCopyToBit,
        };
        using var view = new RDTextureView();
        var target = device.TextureCreate(format, view);
        if (!target.IsValid) throw new InvalidOperationException("Could not allocate the transition GPU snapshot.");
        try
        {
            var error = device.TextureCopy(source, target, Vector3.Zero, Vector3.Zero,
                new Vector3(format.Width, format.Height, 1), 0, 0, 0, 0);
            if (error != Error.Ok) throw new InvalidOperationException($"Transition GPU snapshot copy failed: {error}.");
            return target;
        }
        catch (Exception error)
        {
            try { device.FreeRid(target); }
            catch (Exception releaseError) { throw new AggregateException(error, releaseError); }
            throw;
        }
    }

    private static async Task ReleaseGpu(Texture2Drd? texture, Rid target)
    {
        Exception? failure = null;
        // Texture2Drd owns shared views, not the original RD allocation. Unbind/dispose those first;
        // the following queued release runs after their RenderingServer frees and uses RD's deferred destruction.
        try { texture?.Dispose(); }
        catch (Exception error) { failure = error; }
        if (target.IsValid)
        {
            try { await OnRenderThread(device => { device.FreeRid(target); return true; }); }
            catch (Exception error) { failure = failure is null ? error : new AggregateException(failure, error); }
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static Task<T> OnRenderThread<T>(Func<RenderingDevice, T> action)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            RenderingServer.CallOnRenderThread(Callable.From(() =>
            {
                try
                {
                    var device = RenderingServer.GetRenderingDevice()
                        ?? throw new InvalidOperationException("World transitions require the project's RenderingDevice renderer.");
                    completion.TrySetResult(action(device));
                }
                catch (Exception error) { completion.TrySetException(error); }
            }));
        }
        catch (Exception error) { completion.TrySetException(error); }
        return completion.Task;
    }

    private static void RequireMainThread()
    {
        if (!GodotThread.IsMainThread()) throw new InvalidOperationException("World transitions begin on the Godot main thread.");
    }

    private sealed class Capture
    {
        internal readonly CancellationTokenSource Stop = new();
        internal readonly TaskCompletionSource<float> Duration = new();
        internal readonly TaskCompletionSource Faded = new();
        internal Task Work = Task.CompletedTask;
    }
}
