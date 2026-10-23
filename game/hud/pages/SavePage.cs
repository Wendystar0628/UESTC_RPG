using Campus.Contracts;
using Campus.Contracts.AppFlow;
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

    public event Action? CloseRequested;
    /// <summary>读取或在此开始只发请求，进入、关页与失败提示归界面根（HudPages）。</summary>
    public event Action<EnterRequest>? EnterRequested;
    private static T Svc<T>() where T : class => Campus.Kernel.Registry.Kernel.Get<T>();

    internal static SavePage Create(UiKit kit, Control parent, Control dialogParent)
    {
        var page = new SavePage { _kit = kit, _confirm = new SaveConfirm(kit, dialogParent), MouseFilter = MouseFilterEnum.Stop, Visible = false };
        parent.AddChild(page);
        page.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return page;
    }

    public void Open(bool fromTitle, SaveIntent intent = SaveIntent.Browse)
    {
        _fromTitle = fromTitle;
        _intent = intent;
        _fade?.Kill();
        ReleaseContent();
        Build(keep: null);
        Visible = true;
        _lock ??= Svc<ICameraRig>().LockInput(InputLockReason.Menu);
        // 打开期间跟着存档变化重建卡片（自动保存、回标题后台写完），关页退订。
        if (!_watching) { Svc<ISaveService>().SlotsChanged += Refresh; _watching = true; }
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
        _lock?.Dispose();
        _lock = null;
        if (_watching) { Svc<ISaveService>().SlotsChanged -= Refresh; _watching = false; }
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
        var frame = _kit.Layout.Page(this, UiKit.T(_intent == SaveIntent.NewGame ? "saves.title.new_game" : "saves.title"), "savesWidth", centered: true);
        frame.Tabs.Visible = false;
        _kit.Text(frame.Header, "SAVES", "mono-caps", "text.2", wrap: false).SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        _kit.Button(frame.Header, ButtonStyle.Icon, "", icon: "close", pressed: () => CloseRequested?.Invoke());

        var saves = Svc<ISaveService>();
        var slots = saves.Slots;
        var grid = _kit.Layout.Cards(frame.Body);
        for (var i = 0; i < slots.Count; i++)
        {
            var index = i;
            var card = SaveSlotCard.Build(_kit, grid, slots[i], slots[i].Slot == saves.Continue);
            card.Hit.Pressed += () => Select(index);
            card.Hit.FocusEntered += () => Select(index);
            _cards.Add(card);
        }
        _detail = _kit.Text(frame.Body, "", "small", "text.2");
        _kit.Button(frame.Footer, ButtonStyle.Secondary, UiKit.T("ui.common.back"), pressed: () => CloseRequested?.Invoke());
        _primary = _kit.Button(frame.Footer, ButtonStyle.Primary, "", "button.primary", pressed: Act);
        if (_intent == SaveIntent.NewGame)
            _overwrite = _kit.Button(frame.Footer, ButtonStyle.Danger, UiKit.T("saves.overwrite"), "button.primary", pressed: ShowConfirm);
        _selected = Math.Clamp(keep ?? slots.ToList().FindIndex(s => s.Slot == saves.Continue), 0, Math.Max(0, slots.Count - 1));
        if (slots.Count > 0) Select(_selected);
        else _primary.Off = true;
    }

    private void Select(int index)
    {
        var slots = Svc<ISaveService>().Slots;
        _selected = index;
        for (var i = 0; i < _cards.Count; i++) _cards[i].SetSelected(i == index);
        var slot = slots[index];
        var overwrite = _overwrite is not null && slot.Health != SlotHealth.Empty;
        _primary.Visible = !overwrite;
        if (_overwrite is not null) _overwrite.Visible = overwrite;
        _primary.Text = UiKit.T(slot.Meta is null ? "saves.new_here" : "saves.load");
        _primary.OffReason = UiKit.T("saves.only_from_title");
        _primary.Off = !_fromTitle || slot.Health == SlotHealth.Corrupt;
        _detail.Text = slot.Health switch
        {
            SlotHealth.Empty => UiKit.T("saves.detail.empty"),
            SlotHealth.Ok => UiKit.T(overwrite ? "saves.detail.overwrite" : "saves.detail.ok", ("gens", $"{slot.Generations}")),
            SlotHealth.Corrupt => UiKit.T(overwrite ? "saves.detail.corrupt_overwrite" : "saves.detail.corrupt"),
            _ => throw new InvalidOperationException("Unknown slot health."),
        };
    }

    private void ShowConfirm()
    {
        if (_confirm.IsOpen || !_fromTitle) return;
        var slot = Svc<ISaveService>().Slots[_selected];
        FocusWhenInteractive(_confirm.ShowOverwrite(slot.Slot, Act, HideConfirm));
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
        if (!_fromTitle) return;
        var slot = Svc<ISaveService>().Slots[_selected];
        var request = _intent == SaveIntent.NewGame || slot.Meta is null
            ? new EnterRequest(EnterKind.NewGame, slot.Slot)
            : new EnterRequest(EnterKind.LoadSlot, slot.Slot);
        EnterRequested?.Invoke(request);
    }

}
