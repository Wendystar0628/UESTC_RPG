using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Godot;

namespace Campus.Hud;

/// <summary>
/// 启动失败页（SYS-TITLE）：确认对话框样式的一张卡，盖在加载遮罩之上并独占输入。
/// 重试与退出共用同一个进行中的任务，任务的失败在 Tick 里交回引擎的未处理异常路径，不在这里吞掉。
/// </summary>
internal sealed class HudFailure
{
    private readonly CanvasLayer _owner;
    private readonly HudRootDependencies _dependencies;
    private readonly HudView _view;
    private readonly HudFrontispiece _frontispiece;
    private readonly Action _syncInput;
    private CancellationTokenSource _lifetime = null!;
    private IAppStartupFailureSession? _session;
    private Task<bool>? _presenting;
    private Panel? _scrim;
    private UiPageFrame? _card;
    private TechButton? _retry, _copy, _quit;
    private Toast? _toast;
    private Task? _action;
    private bool _acknowledged;

    public HudFailure(CanvasLayer owner, HudRootDependencies dependencies, HudView view,
        HudFrontispiece frontispiece, Action syncInput)
    {
        _owner = owner;
        _dependencies = dependencies;
        _view = view;
        _frontispiece = frontispiece;
        _syncInput = syncInput;
    }

    /// <summary>失败页已接管输入：HudInput 据此让其余输入层退出。</summary>
    public bool Active { get; private set; }

    public void Enter() => _lifetime = new CancellationTokenSource();

    /// <summary>--ui-gallery=failure：用一份示例失败走同一条展示路径；重试与退出只走统一退出（码 0），不重启。</summary>
    internal Task<bool> ShowGallery() => Show(new GallerySession(_dependencies.Exit));

    private sealed class GallerySession(IAppExit exit) : IAppStartupFailureSession
    {
        public AppStartupFailure Failure { get; } =
            new(AppStartupFailureCodes.MapPreparation, "prepare-map", "gallery", "user://logs");
        public Task Retry() => exit.FinishQuit(0);
        public Task Quit() => exit.FinishQuit(0);
    }

    public void Exit()
    {
        // 树退出不算丢卡：先撤销确认，再拆。
        _lifetime.Cancel();
        _lifetime.Dispose();
        Release();
        Tick();
    }

    /// <summary>在主线程展示；同一个 session 重复调用得到同一次展示的结果，不重复建卡。</summary>
    public Task<bool> Show(IAppStartupFailureSession session)
    {
        if (!GodotThread.IsMainThread()) throw new InvalidOperationException("The startup failure page is shown on the Godot main thread.");
        if (_session is not null)
        {
            if (!ReferenceEquals(_session, session)) throw new InvalidOperationException("Another startup failure is already shown.");
            return _presenting!;
        }
        if (!_owner.IsInsideTree() || !_view.Root.Visible) return Task.FromResult(false);
        _session = session;
        return _presenting = Present(session);
    }

    private async Task<bool> Present(IAppStartupFailureSession session)
    {
        var lifetime = _lifetime.Token;
        try
        {
            Build(session);
            _view.FailureHost.Visible = true;
            // 加载遮罩只是不再画出来：它的覆盖状态和租约照常走完，进度逻辑不受影响。
            _view.LoadingHost.Modulate = Colors.White with { A = 0 };
            Active = true;
            _dependencies.InputRouter.SetInterfaceMode(true);
            _syncInput();
            // 背景只留扉页层的雾色，不带静帧（SYS-TITLE）。
            await _frontispiece.ShowFog().WaitAsync(lifetime);
            lifetime.ThrowIfCancellationRequested();
            if (_retry!.IsVisibleInTree()) _retry.GrabFocus();
            await FrameAwaiter.ReadyDrawAsync(_owner, CardReady, TimeSpan.FromSeconds(BootTuning.StalledSeconds), lifetime);
            lifetime.ThrowIfCancellationRequested();
            if (!InputTakenOver()) { Release(); return false; }
            _acknowledged = true;
            return true;
        }
        catch
        {
            Release();
            throw;
        }
    }

    private void Build(IAppStartupFailureSession session)
    {
        var kit = _view.Kit;
        var failure = session.Failure;
        // 先取原因文字：未知错误码在建任何节点之前就抛出。
        var reason = UiKit.T(ReasonKey(failure.Code));
        _scrim = kit.Box(_view.FailureHost, new UiFx(Fill: "scrim", Margin: 0));
        _scrim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _scrim.MouseFilter = Control.MouseFilterEnum.Stop;
        _view.FailureHost.MoveChild(_scrim, 0);
        _card = kit.Layout.Dialog(_view.FailureCardHost, UiKit.T("startup.failure.title"));
        _card.Tabs.Visible = false;
        kit.Text(_card.Body, reason);
        kit.Text(_card.Body, failure.Code, "data", "text.2", wrap: false);
        _retry = kit.Button(_card.Footer, ButtonStyle.Primary, UiKit.T("startup.failure.retry"), "button.primary",
            pressed: () => Run(session.Retry));
        _copy = kit.Button(_card.Footer, ButtonStyle.Secondary, UiKit.T("startup.failure.copy"), pressed: () => Copy(failure));
        _quit = kit.Button(_card.Footer, ButtonStyle.Secondary, UiKit.T("startup.failure.quit"), pressed: () => Run(session.Quit));
        _toast = Toast.Create(kit, _view.FailureHintHost);
        SetBusy(_action is not null);
    }

    // 错误码到原因文字的键；契约追加错误码时这里同批补一行，未知码直接报错。
    private static string ReasonKey(string code) => code switch
    {
        AppStartupFailureCodes.Installation => "startup.failure.reason.installation",
        AppStartupFailureCodes.DataInvalid => "startup.failure.reason.data_invalid",
        AppStartupFailureCodes.Stalled => "startup.failure.reason.stalled",
        AppStartupFailureCodes.MapPreparation => "startup.failure.reason.map_preparation",
        AppStartupFailureCodes.BakeFailed => "startup.failure.reason.bake_failed",
        AppStartupFailureCodes.Unexpected => "startup.failure.reason.unexpected",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown startup failure code.")
    };

    private static string CopyText(AppStartupFailure failure) => UiKit.T("startup.failure.report",
        ("reason", UiKit.T(ReasonKey(failure.Code))), ("code", failure.Code), ("phase", failure.Phase),
        ("build", failure.Build), ("logs", failure.LogDirectory));

    private void Copy(AppStartupFailure failure)
    {
        if (_action is not null) return;
        DisplayServer.ClipboardSet(CopyText(failure));
        _toast!.Show(UiKit.T("startup.failure.copied"));
    }

    // 进行中的任务只有一个：置灰的按钮仍可被点中，所以这里按任务是否在跑挡掉重复调用。
    private void Run(Func<Task> action)
    {
        if (_action is not null) return;
        _action = action();
        SetBusy(true);
    }

    private void SetBusy(bool busy)
    {
        foreach (var button in new[] { _retry, _copy, _quit })
        {
            if (button is null) continue;
            button.OffReason = UiKit.T("startup.failure.busy");
            button.Off = busy;
        }
    }

    /// <summary>
    /// 每帧两件事：取回结束的任务，失败原样抛进 _Process，走引擎的未处理异常路径；
    /// 已确认的失败页若在树退出之前丢了卡（被移出树、释放或隐藏），立即走统一退出，不留空窗口。
    /// 失败页保持输入，直到树退出才释放（<see cref="Exit"/>）。
    /// </summary>
    public void Tick()
    {
        var completed = _action is { IsCompleted: true } done ? done : null;
        if (completed is not null)
        {
            _action = null;
            SetBusy(false);
        }
        if (_acknowledged && _session is { } session && _owner.IsInsideTree() && !CardAlive())
        {
            _acknowledged = false;
            Run(session.Quit);
        }
        completed?.GetAwaiter().GetResult();
    }

    // 卡（外壳与面板）、遮罩和宿主都要真正可见；任何一个被隐藏、移出树或释放都算丢卡。
    private bool CardAlive() => _card is { } card && GodotObject.IsInstanceValid(card.Shell) && card.Shell.IsVisibleInTree() &&
        GodotObject.IsInstanceValid(card.Panel) && card.Panel.IsVisibleInTree() &&
        _scrim is { } scrim && GodotObject.IsInstanceValid(scrim) && scrim.IsVisibleInTree() && _view.FailureHost.IsVisibleInTree();

    // 卡已排好版、默认焦点已在“重试”上；之后由 FrameAwaiter 再等一帧真实绘制。
    private bool CardReady() => _card is { } card && _owner.IsInsideTree() && _view.FailureHost.IsVisibleInTree() &&
        card.Panel.Size.X > 0 && card.Panel.Size.Y > 0 && _retry!.HasFocus() &&
        _owner.GetViewport().GetVisibleRect().Size is { X: > 0, Y: > 0 };

    private bool InputTakenOver() => Active && Input.MouseMode == Input.MouseModeEnum.Visible &&
        _dependencies.InputRouter.InterfaceMode &&
        _view.FailureHost.MouseBehaviorRecursive == Control.MouseBehaviorRecursiveEnum.Inherited &&
        new[] { _view.PageHost, _view.DialogHost, _view.Corners, _view.LoadingHost }
            .All(host => host.MouseBehaviorRecursive == Control.MouseBehaviorRecursiveEnum.Disabled);

    private void Release()
    {
        Active = false;
        _acknowledged = false;
        _session = null;
        foreach (var node in new Node?[] { _scrim, _card?.Shell, _toast })
        {
            if (node is null) continue;
            node.GetParent()?.RemoveChild(node);
            node.QueueFree();
        }
        _scrim = null;
        _card = null;
        _retry = _copy = _quit = null;
        _toast = null;
        _view.FailureHost.Visible = false;
        _view.LoadingHost.Modulate = Colors.White;
        _syncInput();
    }
}
