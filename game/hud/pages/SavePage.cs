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

/// <summary>
/// 居中的可滚动存档页：上面三张存档卡随安全框宽度换列（SaveSlotCard），下面列出选中存档位的条目（SaveEntryList），覆盖与删除的确认见 SaveConfirm。
/// 开始页上可读取整位或其中一份、删一份手动档或删整位；游戏内只看，主按钮是立即保存（SYS-SAVE、DECISION-317）。
/// </summary>
public partial class SavePage : Control
{
    private enum FocusSpot : byte { Card, Entry, Primary, Danger }

    private readonly List<SaveSlotCard> _cards = [];
    private UiKit _kit = null!;
    private ISaveService _saves = null!;
    private IMapService _maps = null!;
    private SaveConfirm _confirm = null!;
    private Control _entryHost = null!;
    private SaveEntryList? _entries;
    private TechButton _primary = null!;
    // 新游戏选位时是"覆盖并开始"，开始页浏览时是删除（整位或一份手动档），游戏内没有。
    private TechButton? _danger;
    private Label _detail = null!;
    private IDisposable? _lock;
    private Tween? _fade;
    private int _selected;
    private ulong? _entry;
    private bool _fromTitle;
    private bool _reported;
    private SaveIntent _intent;
    private bool _watching;

    public event Action? CloseRequested;
    /// <summary>读取或在此开始只发请求，进入、关页与失败提示归界面根（HudPages）。</summary>
    public event Action<EnterRequest>? EnterRequested;
    /// <summary>游戏内立即保存（手动池已满时已经确认过替换），写盘与回执归界面根。</summary>
    public event Action? SaveNowRequested;
    /// <summary>开始页上确认过的删除：代号为 null 是删整条世界线，否则删那一份手动档；执行与失败提示归界面根。</summary>
    public event Action<SlotId, ulong?>? DeleteRequested;
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

    public void Open(bool fromTitle, SaveIntent intent = SaveIntent.Browse)
    {
        _fromTitle = fromTitle;
        _intent = intent;
        _fade?.Kill();
        ReleaseContent();
        Build(keep: null, keepEntry: null);
        Visible = true;
        _lock ??= Svc<ICameraRig>().LockInput(InputLockReason.Menu);
        // 打开期间跟着存档变化原位重建（自动保存、立即保存、删除、回标题后台写完），关页退订。
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

    /// <summary>
    /// 存档变化后原位重建：保留选中的存档位（s1–s3 恒为三项）与仍在列表里的那一份；
    /// 写盘开始与真正结束各发布一次（SYS-SAVE-FLOW），"保存中"与立即保存的断电随重建切换。焦点原在页内或确认框上就交回原处。
    /// </summary>
    private void Refresh()
    {
        if (!Visible) return;
        var focus = GetViewport().GuiGetFocusOwner();
        var refocus = _confirm.IsOpen || (focus is not null && IsAncestorOf(focus));
        var spot = _confirm.IsOpen ? FocusSpot.Card
            : focus == _primary ? FocusSpot.Primary
            : focus is not null && focus == _danger ? FocusSpot.Danger
            : _entries?.Contains(focus) == true ? FocusSpot.Entry : FocusSpot.Card;
        var keep = _selected;
        var keepEntry = _entry;
        ReleaseContent();
        Build(keep, keepEntry);
        if (refocus && _cards.Count > 0) FocusWhenInteractive(FocusTarget(spot));
    }

    private Control FocusTarget(FocusSpot spot)
    {
        Control? target = spot switch
        {
            FocusSpot.Primary => _primary,
            FocusSpot.Danger => _danger,
            FocusSpot.Entry => _entry is { } gen ? _entries?.Hit(gen) : null,
            FocusSpot.Card => null,
            _ => throw new ArgumentOutOfRangeException(nameof(spot)),
        };
        return target is { Visible: true } ? target : _cards[_selected].Hit;
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
        _entries = null;
        _danger = null;
    }

    private void Build(int? keep, ulong? keepEntry)
    {
        var scrim = _kit.Box(this, new UiFx(Fill: "scrim", Margin: 0));
        scrim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var frame = _kit.Layout.Page(this, UiKit.T(_intent == SaveIntent.NewGame ? "saves.title.new_game" : "saves.title"), "savesWidth", centered: true);
        frame.Tabs.Visible = false;
        // 只读签条：本窗口不保存的原因与设置卡的保存状态同一套文案。
        if (ReadOnlyText() is { } readOnly)
            _kit.Text(frame.Header, readOnly, "small", "text.2", wrap: false).SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        _kit.Text(frame.Header, "SAVES", "mono-caps", "text.2", wrap: false).SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        _kit.Button(frame.Header, ButtonStyle.Icon, "", icon: "close", pressed: () => CloseRequested?.Invoke());

        var slots = _saves.Slots;
        var grid = _kit.Layout.Cards(frame.Body);
        for (var i = 0; i < slots.Count; i++)
        {
            var index = i;
            var card = SaveSlotCard.Build(_kit, grid, slots[i], latest: slots[i].Slot == _saves.Continue,
                current: !_fromTitle && slots[i].Slot == _saves.Active, _saves, _maps);
            card.Hit.Pressed += () => Select(index, null);
            card.Hit.FocusEntered += () => Select(index, null);
            _cards.Add(card);
        }
        UpdateWriting();
        _entryHost = _kit.Layout.Stack(frame.Body, groups: false);
        _detail = _kit.Text(frame.Body, "", "small", "text.2");
        _kit.Button(frame.Footer, ButtonStyle.Secondary, UiKit.T("ui.common.back"), pressed: () => CloseRequested?.Invoke());
        _primary = _kit.Button(frame.Footer, ButtonStyle.Primary, "", "button.primary", pressed: Act);
        if (_intent == SaveIntent.NewGame)
            _danger = _kit.Button(frame.Footer, ButtonStyle.Danger, UiKit.T("saves.overwrite"), "button.primary", pressed: ShowOverwrite);
        else if (_fromTitle)
            _danger = _kit.Button(frame.Footer, ButtonStyle.Danger, "", "button.primary", pressed: ShowDelete);
        _selected = Math.Clamp(keep ?? slots.ToList().FindIndex(s => s.Slot == _saves.Continue), 0, Math.Max(0, slots.Count - 1));
        if (slots.Count == 0) { _primary.Off = true; return; }
        BuildEntries(slots[_selected]);
        Select(_selected, keepEntry is { } gen && slots[_selected].Entries.Any(e => e.Gen == gen) ? gen : null);
    }

    /// <summary>写入中只标在当前存档位上（自动保存、立即保存与离开时的保存都写它）。</summary>
    private void UpdateWriting()
    {
        var busy = _saves.Busy;
        var slots = _saves.Slots;
        for (var i = 0; i < _cards.Count; i++) _cards[i].SetWriting(busy && slots[i].Slot == _saves.Active);
    }

    private void BuildEntries(SlotInfo slot)
    {
        foreach (var child in _entryHost.GetChildren())
        {
            _entryHost.RemoveChild(child);
            child.QueueFree();
        }
        _entries = SaveEntryList.Build(_kit, _entryHost, slot, _saves, _maps);
        _entries.Selected += gen => Select(_selected, gen);
    }

    /// <summary>选中一个存档位（<paramref name="entry"/> 为 null）或其中一份条目；换了存档位就换条目列表。</summary>
    private void Select(int index, ulong? entry)
    {
        var slots = _saves.Slots;
        if (index != _selected) BuildEntries(slots[index]);
        _selected = index;
        _entry = entry;
        for (var i = 0; i < _cards.Count; i++) _cards[i].SetSelected(i == index);
        _entries?.SetSelected(entry);
        var slot = slots[index];
        if (!_fromTitle) UpdateSaveNow();
        else if (_intent == SaveIntent.NewGame) UpdateNewGame(slot);
        else UpdateBrowse(slot, Entry(slot));
        _detail.Text = slot.Health switch
        {
            SlotHealth.Empty => UiKit.T("saves.detail.empty"),
            SlotHealth.Ok => UiKit.T(_intent == SaveIntent.NewGame ? "saves.detail.overwrite" : "saves.detail.ok"),
            SlotHealth.Corrupt => UiKit.T(_intent == SaveIntent.NewGame ? "saves.detail.corrupt_overwrite" : "saves.detail.corrupt"),
            SlotHealth.Newer => UiKit.T("saves.detail.newer"),
            _ => throw new InvalidOperationException("Unknown slot health."),
        };
    }

    private SaveEntry? Entry(SlotInfo slot) => _entry is { } gen ? slot.Entries.FirstOrDefault(e => e.Gen == gen) : null;

    // 游戏内：主按钮是立即保存，写到当前存档位，与选中哪张卡无关。
    private void UpdateSaveNow()
    {
        _primary.Text = UiKit.T("saves.save_now");
        var blocked = SaveNowBlock();
        if (blocked is not null) _primary.OffReason = blocked;
        _primary.Off = blocked is not null;
    }

    /// <summary>立即保存断电的原因：本窗口不保存、这局不写存档位、正在写盘、当前存档位来自更新版本；能存为 null。</summary>
    private string? SaveNowBlock()
    {
        if (ReadOnlyText() is { } readOnly) return readOnly;
        if (_saves.Active is not { } active) return UiKit.T("saves.save_now.no_slot");
        if (_saves.Busy) return UiKit.T("saves.writing");
        return Info(active)?.Health == SlotHealth.Newer ? UiKit.T("saves.newer") : null;
    }

    // 新游戏选位：空位"在这里开始"，占用的位（含坏档与来自更新版本的）一律经危险确认覆盖（SYS-SAVE-FORMAT）。
    private void UpdateNewGame(SlotInfo slot)
    {
        var overwrite = slot.Health != SlotHealth.Empty;
        _primary.Visible = !overwrite;
        _primary.Text = UiKit.T("saves.new_here");
        _primary.Off = false;
        _danger!.Visible = overwrite;
    }

    // 开始页浏览：读取整位（最新可读的一份）或选中的那一份；删除在选中卡时是整位，选中手动档时是那一份。
    private void UpdateBrowse(SlotInfo slot, SaveEntry? entry)
    {
        _primary.Text = UiKit.T(slot.Health == SlotHealth.Empty ? "saves.new_here" : "saves.load");
        var readable = entry is null
            ? slot.Health is SlotHealth.Empty or SlotHealth.Ok
            : SaveEntryPools.Readable(slot, entry);
        _primary.OffReason = UiKit.T(slot.Health == SlotHealth.Newer ? "saves.newer" : "saves.corrupt");
        _primary.Off = !readable;
        _danger!.Visible = entry is null ? slot.Health != SlotHealth.Empty : SaveEntryPools.IsManual(entry);
        _danger.Text = UiKit.T(entry is null ? "saves.delete_slot" : "saves.delete_entry");
        var blocked = ReadOnlyText() ?? (entry is not null && slot.Health == SlotHealth.Newer ? UiKit.T("saves.newer") : null);
        if (blocked is not null) _danger.OffReason = blocked;
        _danger.Off = blocked is not null;
    }

    private SlotInfo? Info(SlotId slot) => _saves.Slots.FirstOrDefault(s => s.Slot == slot);

    private string? ReadOnlyText() => _saves.State.ReadOnly switch
    {
        ReadOnlyReason.None => null,
        ReadOnlyReason.SaveOff => UiKit.T("settings.save.off"),
        ReadOnlyReason.OtherWindow => UiKit.T("settings.save.other_window"),
        _ => throw new InvalidOperationException("Unknown read-only reason."),
    };

    private void ShowOverwrite()
    {
        if (_confirm.IsOpen) return;
        FocusWhenInteractive(_confirm.ShowOverwrite(_saves.Slots[_selected], Act, HideConfirm));
    }

    private void ShowDelete()
    {
        if (_confirm.IsOpen) return;
        var slot = _saves.Slots[_selected];
        var entry = Entry(slot);
        var cancel = entry is null
            ? _confirm.ShowDeleteSlot(slot, () => ConfirmDelete(slot.Slot, null), HideConfirm)
            : _confirm.ShowDeleteEntry(slot.Slot, entry, () => ConfirmDelete(slot.Slot, entry.Gen), HideConfirm);
        FocusWhenInteractive(cancel);
    }

    private void ConfirmDelete(SlotId slot, ulong? gen)
    {
        HideConfirm();
        DeleteRequested?.Invoke(slot, gen);
    }

    private void HideConfirm()
    {
        _confirm.Release();
        if (_cards.Count > 0) FocusWhenInteractive(_danger is { Visible: true } ? _danger : _cards[_selected].Hit);
    }

    private static void FocusWhenInteractive(Control target)
    {
        // 根宿主先仲裁上层输入；隐藏 HUD 和被上层遮挡的页面不争抢焦点。
        if (target.IsVisibleInTree() && target.GetFocusModeWithOverride() != FocusModeEnum.None) target.GrabFocus();
    }

    /// <summary>Esc 优先取消确认。</summary>
    public bool DismissConfirm()
    {
        if (!_confirm.IsOpen) return false;
        HideConfirm();
        return true;
    }

    private void Act()
    {
        // 不能进入或保存时主按钮与覆盖都断电，按钮本身拦住按下（TechButton），这里不再守门。
        if (!_fromTitle) { SaveNow(); return; }
        var slot = _saves.Slots[_selected];
        var request = _intent == SaveIntent.NewGame || slot.Health == SlotHealth.Empty
            ? new EnterRequest(EnterKind.NewGame, slot.Slot)
            : new EnterRequest(EnterKind.LoadSlot, slot.Slot) { Gen = _entry };
        EnterRequested?.Invoke(request);
    }

    /// <summary>手动池满了先确认替换最旧的一份手动档并附它的缩略卡，确认后才发保存（SYS-SAVE、DECISION-317）。</summary>
    private void SaveNow()
    {
        if (_confirm.IsOpen) return;
        if (_saves.Active is { } active && Info(active) is { } slot && SaveEntryPools.ReplacedByManualSave(slot) is { } oldest)
        {
            FocusWhenInteractive(_confirm.ShowReplaceManual(active, oldest, () =>
            {
                HideConfirm();
                SaveNowRequested?.Invoke();
            }, HideConfirm));
            return;
        }
        SaveNowRequested?.Invoke();
    }
}
