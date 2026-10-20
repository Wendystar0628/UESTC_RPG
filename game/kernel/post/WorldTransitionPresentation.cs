using Campus.Contracts;
using Campus.Kernel.Frame;
using Godot;

namespace Campus.Kernel.Post;

[KernelNode("post.world-transition")]
[KernelService(typeof(IWorldTransitionPresentation))]
public partial class WorldTransitionPresentation : CanvasLayer, IWorldTransitionPresentation
{
    private readonly TextureRect _previous = new()
    {
        MouseFilter = Control.MouseFilterEnum.Ignore,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
    };
    private ImageTexture? _texture;
    private TaskCompletionSource? _completion;
    private CancellationTokenSource? _lifetime;
    private double _elapsed;
    private float _duration;
    private bool _blending;

    public WorldTransitionPresentation()
    {
        Layer = 5;
        ProcessMode = ProcessModeEnum.Always;
        ProcessPriority = FramePriority.Hud;
    }

    public override void _Ready()
    {
        _lifetime = new();
        AddChild(_previous);
        _previous.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _previous.Hide();
    }

    public void CapturePreviousFrame()
    {
        if (!IsInsideTree()) throw new InvalidOperationException("World transitions require a mounted presentation.");
        using var image = GetViewport().GetTexture().GetImage();
        var texture = ImageTexture.CreateFromImage(image);
        _completion?.TrySetResult();
        _blending = false;
        _previous.Texture = texture;
        _texture?.Dispose();
        _texture = texture;
        _previous.Modulate = Colors.White;
        _previous.Show();
    }

    public async Task Crossfade(float seconds)
    {
        if (!float.IsFinite(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (_texture is null) throw new InvalidOperationException("Capture the previous scene before crossfading.");
        var texture = _texture;
        var completion = new TaskCompletionSource();
        _completion = completion;
        // Keep the still intact while the new world reaches a real drawn frame.
        try
        {
            await FrameAwaiter.ReadyDrawAsync(this, () => true, TimeSpan.FromSeconds(30), _lifetime!.Token);
            if (!ReferenceEquals(texture, _texture)) return;
            _elapsed = 0;
            _duration = seconds;
            _blending = true;
            await completion.Task;
        }
        catch
        {
            if (ReferenceEquals(texture, _texture)) ReleaseCapture();
            throw;
        }
    }

    public override void _Process(double delta)
    {
        if (!_blending) return;
        _elapsed += Math.Max(0, delta);
        var alpha = 1f - (float)Math.Min(1, _elapsed / _duration);
        _previous.Modulate = new Color(1, 1, 1, alpha);
        if (alpha > 0) return;
        ReleaseCapture();
        _completion?.TrySetResult();
        _completion = null;
    }

    private void ReleaseCapture()
    {
        _blending = false;
        _previous.Hide();
        _previous.Texture = null;
        _texture?.Dispose();
        _texture = null;
    }

    public override void _ExitTree()
    {
        _lifetime?.Cancel();
        _lifetime?.Dispose();
        _lifetime = null;
        _completion?.TrySetCanceled();
        _completion = null;
        ReleaseCapture();
    }
}
