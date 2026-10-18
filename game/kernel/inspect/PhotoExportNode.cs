using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Kernel.Renderer;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>One current-view export, using the existing HUD lease and viewport readback.</summary>
[KernelService(typeof(IPhotoExport), Owner = "WP-01")]
[KernelNode("photo-export", Owner = "WP-01")]
public partial class PhotoExportNode : Node, IPhotoExport
{
    private IAppFlow _flow = null!;
    private IVeilState _veil = null!;
    private readonly CancellationTokenSource _exit = new();
    private TaskCompletionSource<PhotoExportResult>? _active;
    private TaskCompletionSource<PhotoExportResult>? _pending;
    private IDisposable? _hiddenHud;
    private string? _path;
    private bool _exiting;

    public override void _Ready()
    {
        _flow = Services.Get<IAppFlow>();
        _veil = Services.Get<IVeilState>();
        RenderingServer.FramePostDraw += OnFramePostDraw;
    }

    public Task<PhotoExportResult> ExportCurrentAsync()
    {
        if (_exiting || !IsInsideTree() || !IsNodeReady() || !CanCapture())
            return Task.FromResult(new PhotoExportResult(PhotoExportStatus.Unavailable, null));
        if (_active is { Task.IsCompleted: false })
            return Task.FromResult(new PhotoExportResult(PhotoExportStatus.Busy, _path));

        _active = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _path = null;
        try
        {
            _path = System.IO.Path.Combine(OS.GetUserDataDir(), "photos", $"photo-{Time.GetTicksUsec()}.png");
            _hiddenHud = CapturePresentation.HideHud();
            _pending = _active;
        }
        catch (Exception error)
        {
            GD.PushWarning($"Photo export could not start ({error.GetType().Name}).");
            _active.TrySetResult(new(PhotoExportStatus.Failed, _path));
        }
        return _active.Task;
    }

    private bool CanCapture() => _flow.State == AppState.Playing && _veil.Opacity == 0
        && DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Minimized;

    private void OnFramePostDraw()
    {
        if (_pending is not { } completion) return;
        _pending = null;
        Image? image = null;
        try
        {
            if (!CanCapture())
            {
                completion.TrySetResult(new(PhotoExportStatus.Unavailable, _path));
                return;
            }
            image = GetViewport().GetTexture().GetImage();
            if (image.IsEmpty()) throw new IOException("The photo viewport returned an empty image.");
        }
        catch (Exception error)
        {
            image?.Dispose();
            GD.PushWarning($"Photo readback failed ({error.GetType().Name}).");
            completion.TrySetResult(new(PhotoExportStatus.Failed, _path));
            return;
        }
        finally
        {
            _hiddenHud?.Dispose();
            _hiddenHud = null;
        }
        // The worker owns only its detached Image and file; HUD and tree state stay on the main thread.
        _ = EncodeAndSaveAsync(image, _path!, completion, _exit.Token);
    }

    private static async Task EncodeAndSaveAsync(Image image, string requestedPath,
        TaskCompletionSource<PhotoExportResult> completion, CancellationToken cancellation)
    {
        string path = requestedPath;
        try
        {
            var result = await Task.Run(() =>
            {
                using (image)
                {
                    cancellation.ThrowIfCancellationRequested();
                    var bytes = image.SavePngToBuffer();
                    if (bytes.Length == 0) throw new IOException("Photo PNG encoding failed.");
                    cancellation.ThrowIfCancellationRequested();
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(requestedPath)!);
                    for (var suffix = 0; ; suffix++)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        path = suffix == 0 ? requestedPath
                            : System.IO.Path.ChangeExtension(requestedPath, null) + $"-{suffix}.png";
                        FileStream stream;
                        try { stream = new(path, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.Read); }
                        catch (IOException) when (File.Exists(path)) { continue; }
                        try
                        {
                            using (stream) stream.Write(bytes);
                        }
                        catch
                        {
                            File.Delete(path);
                            throw;
                        }
                        return new PhotoExportResult(PhotoExportStatus.Saved, path);
                    }
                }
            }).ConfigureAwait(false);
            completion.TrySetResult(result);
        }
        catch (OperationCanceledException) { completion.TrySetResult(new(PhotoExportStatus.Unavailable, path)); }
        catch (Exception error)
        {
            GD.PushWarning($"Photo export failed ({error.GetType().Name}).");
            completion.TrySetResult(new(PhotoExportStatus.Failed, path));
        }
    }

    public override void _ExitTree()
    {
        _exiting = true;
        RenderingServer.FramePostDraw -= OnFramePostDraw;
        _exit.Cancel();
        _hiddenHud?.Dispose();
        _hiddenHud = null;
        _pending = null;
        _active?.TrySetResult(new(PhotoExportStatus.Unavailable, _path));
        _exit.Dispose();
    }
}
