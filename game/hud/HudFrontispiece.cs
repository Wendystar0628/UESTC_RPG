using Campus.Contracts;
using Campus.Contracts.Startup;
using Godot;
using System.Threading;

namespace Campus.Hud;

/// <summary>
/// 开始页的扉页层（SYS-TITLE）：雾色底上贴一张固定的扉页图，界面层的片名块与菜单画在它上面。
/// 只由流程的 Show、Hide 出现与退场，后台资源就绪与否都不动它。
/// </summary>
internal sealed class HudFrontispiece : IAppFrontispiecePresentation
{
    private readonly CanvasLayer _owner;
    private readonly HudRootDependencies _dependencies;
    private readonly HudView _view;
    private readonly TextureRect _image;
    private CancellationTokenSource _lifetime = null!;
    private Task _transition = Task.CompletedTask;
    private bool _covered;
    private Vector2 _drawSize;

    private const string ImagePath = "res://assets/title/frontispiece.webp";

    public HudFrontispiece(CanvasLayer owner, HudRootDependencies dependencies, HudView view)
    {
        _owner = owner;
        _dependencies = dependencies;
        _view = view;
        _image = new TextureRect
        {
            Name = "FrontispieceImage",
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        view.Frontispiece.AddChild(_image);
        _image.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    public bool IsCovered => _covered && HudLoading.CoversViewport(_owner, _view.Frontispiece, _drawSize);

    public void Enter()
    {
        _lifetime = new CancellationTokenSource();
        RenderingServer.Singleton.FramePostDraw += OnDrawn;
    }

    public void Exit()
    {
        RenderingServer.Singleton.FramePostDraw -= OnDrawn;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _covered = false;
        _transition = Task.CompletedTask;
        _view.Frontispiece.Visible = false;
        _image.Texture = null;
    }

    Task IAppFrontispiecePresentation.Show()
    {
        var previous = _transition;
        return _transition = ShowAsync(previous, _lifetime.Token);
    }

    private async Task ShowAsync(Task previous, CancellationToken lifetime)
    {
        await previous.WaitAsync(lifetime);
        lifetime.ThrowIfCancellationRequested();
        _view.Frontispiece.Modulate = Colors.White;
        _view.Frontispiece.Visible = true;
        _image.Texture ??= _dependencies.Resources.Load<Texture2D>(ImagePath);
        await FrameAwaiter.StartupReadyDrawAsync(_owner, () => HudLoading.CoversViewport(_owner, _view.Frontispiece, _owner.GetViewport().GetVisibleRect().Size),
            TimeSpan.FromSeconds(BootTuning.StalledSeconds), lifetime);
        lifetime.ThrowIfCancellationRequested();
        _drawSize = _owner.GetViewport().GetVisibleRect().Size;
        _covered = true;
    }

    Task IAppFrontispiecePresentation.Hide(float seconds)
    {
        var previous = _transition;
        return _transition = HideAsync(previous, seconds, _lifetime.Token);
    }

    private async Task HideAsync(Task previous, float seconds, CancellationToken lifetime)
    {
        await previous.WaitAsync(lifetime);
        lifetime.ThrowIfCancellationRequested();
        if (!_view.Frontispiece.Visible) return;
        _covered = false;
        var duration = _dependencies.Motion.Fade(seconds);
        if (duration > 0)
        {
            var tween = _view.Frontispiece.CreateTween();
            tween.TweenProperty(_view.Frontispiece, "modulate:a", 0f, duration);
            await FrameAwaiter.TweenAsync(tween, _view.Frontispiece, lifetime);
            lifetime.ThrowIfCancellationRequested();
        }
        _view.Frontispiece.Visible = false;
        // 进入游戏后释放扉页，回到开始页时 Show 重新载入同一张（SYS-TITLE）。
        _image.Texture = null;
    }

    private void OnDrawn()
    {
        if (!RenderingServer.RenderLoopEnabled) return;
        var size = _owner.GetViewport().GetVisibleRect().Size;
        if (_covered && HudLoading.CoversViewport(_owner, _view.Frontispiece, size)) _drawSize = size;
    }
}
