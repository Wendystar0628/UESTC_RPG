using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

/// <summary>浏览存档，或由开始页选择新游戏覆盖的位置。</summary>
public enum SaveIntent : byte { Browse, NewGame }

/// <summary>居中的可滚动存档页；卡片随安全框宽度换列，卡片见 SaveSlotCard，覆盖确认见 SaveConfirm。</summary>
public partial class SavePage : Control
{
    private readonly List<SaveSlotCard> _cards = [];
    private UiKit _kit = null!;
    private ISaveService _saves = null!;
    private IMapService _maps = null!;
    private SaveConfirm _confirm = null!;
    private TechButton _primary = null!;
    private TechButton? _overwrite;
    private Label _detail = null!;
    private IDisposable? _lock;
    private Tween? _fade;
    private int _selected;
    private bool _fromTitle;
    private bool _reported;
    private SaveIntent _intent;
    private bool _watching;
    private bool _busy;

    public event Action? CloseRequested;
    /// <summary>读取或在此开始只发请求，进入、关页与失败提示归界面根（HudPages）。</summary>
    public event Action<EnterRequest>? EnterRequested;
    // 输入占用与界面面上报不在界面根依赖里，仍按服务取。
    private static T Svc<T>() where T : class => Campus.Kernel.Registry.Kernel.Get<T>();

    internal static SavePage Create(UiKit kit, Control parent, Control dialogParent, ISaveService saves, IMapService maps)
    {
        var page = new SavePage
        {
            _kit = kit,
            _saves = saves,
            _maps = maps,
            _confirm = new SaveConfirm(kit, dialogParent, saves, maps),
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false,
        };
        parent.AddChild(page);
        page.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return page;
    }

    public override void _Ready() => SetProcess(false);

    // 写盘开始与结束都不单独发事件（结束时的 SlotsChanged 发在写入任务完成之前），开着时逐帧比对 Busy 切写入中标签。
    public override void _Process(double delta)
    {
        if (_saves.Busy != _busy) UpdateWriting();
    }

    public void Open(bool fromTitle, SaveIntent intent = SaveIntent.Browse)
    {
        _fromTitle = fromTitle;
        _intent = intent;
        _fade?.Kill();
        ReleaseContent();
        Build(keep: null);
        Visible = true;
        SetProcess(true);
        _lock ??= Svc<ICameraRig>().LockInput(InputLockReason.Menu);
        // 打开期间跟着存档变化重建卡片（自动保存、回标题后台写完），关页退订。
        if (!_watching) { _saves.SlotsChanged += Refresh; _watching = true; }
        Modulate = new Color(1, 1, 1, 0);
        _fade = CreateTween();
        _fade.TweenProperty(this, "modulate:a", 1f, _kit.Motion.Fade(0.36f));
        if (_cards.Count > 0) FocusWhenInteractive(_cards[_selected].Hit);
        PublishOpen();
    }

    /// <summary>大页关上立即移出树并排队释放，不留下隐藏的 UiKit.Holders。</summary>
    public void Close()
    {
        Visible = false;
        SetProcess(false);
        _lock?.Dispose();
        _lock = null;
        if (_watching) { _saves.SlotsChanged -= Refresh; _watching = false; }
        _fade?.Kill();
        _fade = null;
        ReleaseContent();
        PublishOpen();
    }

    // 上层（界面根、--ui=0）显隐变化也会改变存档页是否真的看得见，随之重报。
    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged) PublishOpen();
    }

    /// <summary>存档页真实可见才报开着（IUiSurfaceSink），不可见或已离树即撤下；值不变不报。</summary>
    private void PublishOpen()
    {
        var open = IsInsideTree() && IsVisibleInTree();
        if (open == _reported) return;
        _reported = open;
        Svc<IUiSurfaceSink>().Report(UiSurface.SavePage, open);
    }

    /// <summary>存档变化后原位重建：保留选中的存档位（s1–s3 恒为三项）；焦点原在页内或确认框上就交回选中卡。</summary>
    private void Refresh()
    {
        if (!Visible) return;
        var focus = GetViewport().GuiGetFocusOwner();
        var refocus = _confirm.IsOpen || (focus is not null && IsAncestorOf(focus));
        var keep = _selected;
        ReleaseContent();
        Build(keep);
        if (refocus && _cards.Count > 0) FocusWhenInteractive(_cards[_selected].Hit);
    }

    private void ReleaseContent()
    {
        _confirm.Release();
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        _cards.Clear();
        _overwrite = null;
    }

    private void Build(int? keep)
    {
        var scrim = _kit.Box(this, new UiFx(Fill: "scrim", Margin: 0));
        scrim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var frame = _kit.Layout.Page(this, UiKit.T(_intent == SaveIntent.NewGame ? "_saves.title.new_game" : "_saves.title"), "savesWidth", centered: true);
        frame.Tabs.Visible = false;
        // 只读签条：本窗口不保存的原因与设置卡的保存状态同一套文案。
        var readOnly = _saves.State.ReadOnly switch
        {
            ReadOnlyReason.None => null,
            ReadOnlyReason.SaveOff => "settings.save.off",
            ReadOnlyReason.OtherWindow => "settings.save.other_window",
            _ => throw new InvalidOperationException("Unknown read-only reason."),
        };
        if (readOnly is not null)
            _kit.Text(frame.Header, UiKit.T(readOnly), "small", "text.2", wrap: false).SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        _kit.Text(frame.Header, "SAVES", "mono-caps", "text.2", wrap: false).SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        _kit.Button(frame.Header, ButtonStyle.Icon, "", icon: "close", pressed: () => CloseRequested?.Invoke());

        var slots = _saves.Slots;
        var grid = _kit.Layout.Cards(frame.Body);
        for (var i = 0; i < slots.Count; i++)
        {
            var index = i;
            var card = SaveSlotCard.Build(_kit, grid, slots[i], latest: slots[i].Slot == _saves.Continue,
                current: !_fromTitle && slots[i].Slot == _saves.Active, _saves, _maps);
            card.Hit.Pressed += () => Select(index);
            card.Hit.FocusEntered += () => Select(index);
            _cards.Add(card);
        }
        UpdateWriting();
        _detail = _kit.Text(frame.Body, "", "small", "text.2");
        _kit.Button(frame.Footer, ButtonStyle.Secondary, UiKit.T("ui.common.back"), pressed: () => CloseRequested?.Invoke());
        _primary = _kit.Button(frame.Footer, ButtonStyle.Primary, "", "button.primary", pressed: Act);
        if (_intent == SaveIntent.NewGame)
            _overwrite = _kit.Button(frame.Footer, ButtonStyle.Danger, UiKit.T("_saves.overwrite"), "button.primary", pressed: ShowConfirm);
        _selected = Math.Clamp(keep ?? slots.ToList().FindIndex(s => s.Slot == _saves.Continue), 0, Math.Max(0, slots.Count - 1));
        if (slots.Count > 0) Select(_selected);
        else _primary.Off = true;
    }

    /// <summary>写入中只标在当前存档位上（自动保存与离开时的保存都写它）。</summary>
    private void UpdateWriting()
    {
        _busy = _saves.Busy;
        var slots = _saves.Slots;
        for (var i = 0; i < _cards.Count; i++) _cards[i].SetWriting(_busy && slots[i].Slot == _saves.Active);
    }

    private void Select(int index)
    {
        var slots = _saves.Slots;
        _selected = index;
        for (var i = 0; i < _cards.Count; i++) _cards[i].SetSelected(i == index);
        var slot = slots[index];
        var overwrite = _overwrite is not null && slot.Health != SlotHealth.Empty;
        _primary.Visible = !overwrite;
        if (_overwrite is not null)
        {
            _overwrite.Visible = overwrite;
            _overwrite.OffReason = UiKit.T("_saves.newer");
            _overwrite.Off = slot.Health == SlotHealth.Newer;
        }
        _primary.Text = UiKit.T(slot.Health == SlotHealth.Empty ? "_saves.new_here" : "_saves.load");
        _primary.OffReason = UiKit.T(!_fromTitle ? "_saves.only_from_title"
            : slot.Health == SlotHealth.Newer ? "_saves.newer" : "_saves.corrupt");
        _primary.Off = !Enterable(slot);
        _detail.Text = slot.Health switch
        {
            SlotHealth.Empty => UiKit.T("_saves.detail.empty"),
            SlotHealth.Ok => UiKit.T(overwrite ? "_saves.detail.overwrite" : "_saves.detail.ok"),
            SlotHealth.Corrupt => UiKit.T(overwrite ? "_saves.detail.corrupt_overwrite" : "_saves.detail.corrupt"),
            SlotHealth.Newer => UiKit.T("_saves.detail.newer"),
            _ => throw new InvalidOperationException("Unknown slot health."),
        };
    }

    /// <summary>能否在这个存档位进入：只从开始页；新游戏可覆盖坏档，来自更新版本的档不读不写（SYS-SAVE-FORMAT）。</summary>
    private bool Enterable(SlotInfo slot) => _fromTitle && slot.Health switch
    {
        SlotHealth.Empty or SlotHealth.Ok => true,
        SlotHealth.Corrupt => _intent == SaveIntent.NewGame,
        SlotHealth.Newer => false,
        _ => throw new InvalidOperationException("Unknown slot health."),
    };

    private void ShowConfirm()
    {
        if (_confirm.IsOpen) return;
        FocusWhenInteractive(_confirm.ShowOverwrite(_saves.Slots[_selected], Act, HideConfirm));
    }

    private void HideConfirm()
    {
        _confirm.Release();
        if (_cards.Count > 0) FocusWhenInteractive(_overwrite is { Visible: true } ? _overwrite : _cards[_selected].Hit);
    }

    private static void FocusWhenInteractive(Control target)
    {
        // 根宿主先仲裁上层输入；隐藏 HUD 和被上层遮挡的页面不争抢焦点。
        if (target.IsVisibleInTree() && target.GetFocusModeWithOverride() != FocusModeEnum.None) target.GrabFocus();
    }

    /// <summary>Esc 优先取消覆盖确认。</summary>
    public bool DismissConfirm()
    {
        if (!_confirm.IsOpen) return false;
        HideConfirm();
        return true;
    }

    private void Act()
    {
        // 不能进入时读取与覆盖都断电，按钮本身拦住按下（TechButton），这里不再守门。
        var slot = _saves.Slots[_selected];
        var request = _intent == SaveIntent.NewGame || slot.Health == SlotHealth.Empty
            ? new EnterRequest(EnterKind.NewGame, slot.Slot)
            : new EnterRequest(EnterKind.LoadSlot, slot.Slot);
        EnterRequested?.Invoke(request);
    }

}
