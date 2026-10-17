using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Bake;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

public enum SettingsPage : byte { Keys, General, Graphics, Sound, Rebind, Licenses }

/// <summary>
/// 设置卡（UI-VISUAL、UI-NAV、SYS-GRAPHICS）：贴左的大页，620×1016，切角 14，随主题；不压暗，右侧 2/3 留给实景。
/// 页：键位、设置、画面（按 KnobSpec 生成，只经 IQualityService）、声音（只经 IAudioService）、改键、许可；底部"返回标题""退出"。
/// 打开期间经 ICameraRig.LockInput(Menu) 占用观者输入。内容在打开时才建、关上就释放：引擎只在释放节点时归还
/// 实例参数槽（<see cref="UiKit.Holders"/>），隐藏的大页不该和 3D 流式实例抢同一个池；画面页没应用的改动跨开关保留。
/// </summary>
public partial class SettingsCard : Control
{
    private static readonly SettingsPage[] Order = [SettingsPage.Keys, SettingsPage.General, SettingsPage.Graphics, SettingsPage.Sound, SettingsPage.Rebind, SettingsPage.Licenses];
    private static readonly (string Key, QualityKnob[] Knobs)[] Groups =
    [
        ("display", [QualityKnob.DisplayMode, QualityKnob.VSync, QualityKnob.FrameCap, QualityKnob.Vrr]),
        ("clarity", [QualityKnob.RenderScale, QualityKnob.FsrSharpness, QualityKnob.AntiAliasing, QualityKnob.Anisotropy]),
        ("lighting", [QualityKnob.ShadowQuality, QualityKnob.ShadowDistance, QualityKnob.SmallCasters]),
        ("post", [QualityKnob.GlowQuality, QualityKnob.TiltShift, QualityKnob.LensFlare, QualityKnob.LightShafts, QualityKnob.Vignette]),
        ("scene", [QualityKnob.DetailScale, QualityKnob.MeshLodThreshold, QualityKnob.GeometricLeaves, QualityKnob.FoliageDensity, QualityKnob.GrassRadius]),
        ("effects", [QualityKnob.ReflectionInterval, QualityKnob.ParticleDensity, QualityKnob.Adaptive]),
        ("advanced", [QualityKnob.GraphicsApi, QualityKnob.GpuIndex]),
    ];

    private readonly Dictionary<SettingsPage, Control> _pages = [];
    private readonly HashSet<SettingsPage> _filled = [];
    private readonly List<TechButton> _tabs = [];
    private readonly Dictionary<QualityKnob, float> _pending = [];
    private UiKit _kit = null!;
    private IInputRouter? _router;   // 改键与键名经 IInputRouter（WP-06）；还没注册时退回 InputMap，只在内存里生效
    private HudSettings _settings = null!;
    private Panel _tabBar = null!;
    private ScrollContainer _scroll = null!;
    private Label? _applyStatus;
    private TechButton? _apply;
    private TechButton _toTitle = null!;
    private Segmented? _preset;
    private IDisposable? _lock;
    private SettingsPage _page = SettingsPage.Graphics;
    private string? _rebindAction;
    private IDisposable? _rebindLock;
    private bool _built;

    public event Action? CloseRequested;
    public event Action? QuitRequested;
    public event Action? ToTitleRequested;
    public SettingsPage Page => _page;
    public bool Capturing => _rebindAction is not null;

    public static SettingsCard Create(UiKit kit, Control parent, HudSettings settings, IInputRouter? router)
    {
        var card = new SettingsCard
        {
            _kit = kit,
            _settings = settings,
            _router = router,
            Position = new Vector2(32, 32),
            Size = kit.Tokens.V2("metrics", "size", "settingsCard"),
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false,
        };
        parent.AddChild(card);
        return card;
    }

    private static T Svc<T>() where T : class => Campus.Kernel.Registry.Kernel.Get<T>();

    private void Build()
    {
        _built = true;
        var k = _kit;
        var chamfer = k.Tokens.Chamfer("page");
        var frost = k.Materials.Frost is { } frostMaterial && _settings.Frost && !_settings.ReduceTransparency;
        if (frost)
        {
            AddChild(new BackBufferCopy { CopyMode = BackBufferCopy.CopyModeEnum.Rect, Rect = new Rect2(Vector2.Zero, Size) });
            var blur = new ColorRect { Size = Size, Color = Colors.White, MouseFilter = MouseFilterEnum.Ignore, Material = k.Materials.Frost };
            AddChild(blur);
            blur.SetInstanceShaderParameter("chamfer", chamfer);
        }
        var fill = _settings.ReduceTransparency ? "surface.solid" : frost ? "surface.frost" : "surface.panel";
        k.Box(this, new Rect2(Vector2.Zero, Size), new UiFx(Fill: fill, Line: "line.panel", Chamfer: chamfer, Flags: UiFlags.Highlight | UiFlags.Via));

        const float x = 24;
        var width = Size.X - 48;
        k.Text(this, UiKit.T("settings.title"), "h2", "text.1", new Vector2(x, 18));
        k.Text(this, "SETTINGS", "mono-caps", "text.2", new Vector2(x + 62, 30));
        var close = k.Button(this, ButtonStyle.Icon, "", new Rect2(Size.X - 24 - 32, 18, 32, 32), padX: 4, icon: "close", iconMax: 20, padY: 4,
            pressed: () => CloseRequested?.Invoke());
        close.TooltipText = UiKit.T("ui.common.close") + " · Esc";

        float tx = x;
        for (var i = 0; i < Order.Length; i++)
        {
            var page = Order[i];
            var label = UiKit.T("settings.tab." + Key(page)) + " " + (i + 1);
            var probe = k.Text(this, label, "body", "text.2", Vector2.Zero);
            var w = k.Width(probe) + 4;
            probe.QueueFree();
            var tab = k.Button(this, ButtonStyle.Tab, label, new Rect2(tx, 64, w, 38), "body", 0, padY: 2);
            tab.Pressed += () => Show(page);
            _tabs.Add(tab);
            tx += w + 22;
        }
        _tabBar = k.Box(this, new Rect2(x, 100, 40, 2), new UiFx(Fill: "primary.line"));
        k.Box(this, new Rect2(x, 102, width, 1), new UiFx(Fill: "line.hair"));

        _scroll = new ScrollContainer
        {
            Position = new Vector2(x - 8, 114),
            Size = new Vector2(width + 16, Size.Y - 114 - 76),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            MouseFilter = MouseFilterEnum.Pass,
        };
        AddChild(_scroll);
        _scroll.GetVScrollBar().ThemeTypeVariation = "ScrollThin";
        if (k.Materials.Text is { } scrollMaterial) _scroll.GetVScrollBar().Material = scrollMaterial;
        k.Recolor(_scroll.GetVScrollBar(), "line.scroll");
        var stack = new VBoxContainer { CustomMinimumSize = new Vector2(width + 8, 0) };
        _scroll.AddChild(stack);
        foreach (var page in Order)
        {
            // 各页的内容在第一次切到它时才填（Fill），开卡只付当前这一页的代价
            var content = new Control { CustomMinimumSize = new Vector2(width + 8, 0), MouseFilter = MouseFilterEnum.Pass, Visible = false };
            stack.AddChild(content);
            _pages[page] = content;
        }

        var footY = Size.Y - 64;
        k.Box(this, new Rect2(x, footY - 12, width, 1), new UiFx(Fill: "line.hair"));
        _toTitle = k.Button(this, ButtonStyle.Secondary, UiKit.T("settings.to_title"), new Rect2(x, footY, 132, 40), pressed: () => ToTitleRequested?.Invoke());
        k.Button(this, ButtonStyle.Secondary, UiKit.T("settings.quit"), new Rect2(Size.X - x - 132, footY, 132, 40), pressed: () => QuitRequested?.Invoke());
    }

    private static string Key(SettingsPage page) => page switch
    {
        SettingsPage.Keys => "keys",
        SettingsPage.General => "general",
        SettingsPage.Graphics => "graphics",
        SettingsPage.Sound => "sound",
        SettingsPage.Rebind => "rebind",
        SettingsPage.Licenses => "licenses",
        _ => throw new ArgumentOutOfRangeException(nameof(page)),
    };

    public void Open(SettingsPage page, bool fromTitle)
    {
        if (!_built) Build();
        Show(page);
        _toTitle.Off = fromTitle;
        _toTitle.OffReason = UiKit.T("settings.to_title.off");
        if (!Visible)
        {
            Visible = true;
            _lock ??= TryLock(InputLockReason.Menu);
            Modulate = new Color(1, 1, 1, 0);
            var t = CreateTween();
            t.TweenProperty(this, "modulate:a", 1f, _kit.Motion.Fade(0.26f));
            if (_kit.Motion.Spatial)
            {
                Position = new Vector2(24, 32);
                t.Parallel().TweenProperty(this, "position:x", 32f, 0.26f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
            }
        }
        RefreshGraphics();
    }

    public void Close()
    {
        EndCapture();
        Visible = false;
        _lock?.Dispose();
        _lock = null;
        Release();
    }

    /// <summary>释放全部内容（排队到帧末，删在本帧绘制之前）；下次打开重建。</summary>
    private void Release()
    {
        foreach (var child in GetChildren()) child.QueueFree();
        _pages.Clear();
        _filled.Clear();
        _tabs.Clear();
        _rebindRows.Clear();
        _preset = null;
        _applyStatus = null;
        _apply = null;
        _built = false;
    }

    private void Fill(SettingsPage page)
    {
        if (!_filled.Add(page)) return;
        var content = _pages[page];
        var width = Size.X - 48;
        var height = page switch
        {
            SettingsPage.Keys => BuildKeys(content, width),
            SettingsPage.General => BuildGeneral(content, width),
            SettingsPage.Graphics => BuildGraphics(content, width),
            SettingsPage.Sound => BuildSound(content, width),
            SettingsPage.Rebind => BuildRebind(content, width),
            SettingsPage.Licenses => BuildLicenses(content, width),
            _ => throw new InvalidOperationException("Unknown settings page."),
        };
        content.CustomMinimumSize = new Vector2(width + 8, height);
    }

    private static IDisposable? TryLock(InputLockReason reason)
    {
        try { return Svc<ICameraRig>().LockInput(reason); }
        catch (InvalidOperationException) { return null; }
    }

    private void Show(SettingsPage page)
    {
        _page = page;
        Fill(page);
        foreach (var (p, content) in _pages) content.Visible = p == page;
        for (var i = 0; i < _tabs.Count; i++) _tabs[i].Selected = Order[i] == page;
        var tab = _tabs[Array.IndexOf(Order, page)];
        _tabBar.Position = new Vector2(tab.Position.X, 100);
        _tabBar.Size = new Vector2(tab.Size.X, 2);
        _scroll.ScrollVertical = 0;
    }

    // ---------------- 行 ----------------
    private float Row(Control page, float y, string name, bool hint = false)
    {
        _kit.Text(page, name, "body", "text.1", new Vector2(8, y + 8));
        return y;
    }

    private const float ControlX = 216;

    // ---------------- 键位 ----------------
    private float BuildKeys(Control page, float width)
    {
        float y = 8;
        (string Key, string[] Actions)[] rows =
        [
            ("keys.menu", ["menu"]), ("keys.move", ["move_forward", "move_left", "move_backward", "move_right"]), ("keys.updown", ["move_up", "move_down"]),
            ("keys.sprint", ["sprint"]), ("keys.time", ["time_panel"]), ("keys.forward", ["time_forward"]), ("keys.flow", ["toggle_flow"]),
            ("keys.season", ["next_season"]), ("keys.weather", ["weather"]), ("keys.mode", ["toggle_mode"]), ("keys.map", ["map"]),
            ("keys.index", ["atlas_index"]), ("keys.minimap", ["minimap"]), ("keys.help", ["help"]), ("keys.clean", ["clean_view"]),
        ];
        Parts.GroupHeader(_kit, page, new Vector2(8, y), width, UiKit.T("settings.group.keys"));
        y += 24;
        foreach (var (key, actions) in rows)
        {
            Row(page, y, UiKit.T(key));
            float kx = ControlX;
            foreach (var action in actions)
            {
                if (!InputMap.HasAction(action)) continue;
                kx += Parts.Keycap(_kit, page, new Vector2(kx, y + 8), KeyLabel(action)) + 6;
            }
            y += 40;
        }
        _kit.Text(page, UiKit.T("keys.note"), "small", "text.2", new Vector2(8, y + 8), width: width - 8, wrap: true);
        return y + 64;
    }

    /// <summary>键名按当前键盘布局显示（UI-NAV）：物理键码经 KeyboardGetLabelFromPhysical 换成布局上的字。</summary>
    public string KeyLabel(string action)
    {
        if (_router is not null)
        {
            var physical = _router.PhysicalKey(action);
            return physical == 0 ? "—" : KeyName((Key)physical);
        }
        foreach (var e in InputMap.ActionGetEvents(action))
        {
            if (e is not InputEventKey key) continue;
            return KeyName(key.PhysicalKeycode != Godot.Key.None ? key.PhysicalKeycode : key.Keycode);
        }
        return "—";
    }

    private static string KeyName(Key code)
    {
        var label = DisplayServer.KeyboardGetLabelFromPhysical(code);
        var text = OS.GetKeycodeString(label != Godot.Key.None ? label : code);
        return text switch { "Escape" => "Esc", "Period" => ".", "Space" => "Space", _ => text };
    }

    // ---------------- 设置 ----------------
    private float BuildGeneral(Control page, float width)
    {
        float y = 8;
        Parts.GroupHeader(_kit, page, new Vector2(8, y), width, UiKit.T("settings.group.interface"));
        y += 24;
        Row(page, y, UiKit.T("settings.theme"));
        var theme = Segmented.Create(_kit, page, new Vector2(ControlX, y + 2), [72, 72, 72],
            [UiKit.T("settings.theme.auto"), UiKit.T("settings.theme.light"), UiKit.T("settings.theme.dark")], false, selected: (int)_settings.Theme);
        theme.Changed += i => _settings.SetTheme((UiThemeMode)i);
        y += 40;
        Row(page, y, "　" + UiKit.T("settings.resident"));
        var resident = Segmented.Create(_kit, page, new Vector2(ControlX, y + 2), [72, 72],
            [UiKit.T("settings.resident.dark"), UiKit.T("settings.resident.follow")], false, selected: (int)_settings.Resident);
        resident.Changed += i => _settings.SetResident((ResidentTheme)i);
        y += 40;
        Row(page, y, UiKit.T("settings.frost"));
        var frost = DipSwitch.Create(_kit, page, new Vector2(ControlX, y + 10), _settings.Frost, false);
        frost.Toggled += _settings.SetFrost;
        y += 40;
        Row(page, y, UiKit.T("settings.reduce_transparency"));
        var opaque = DipSwitch.Create(_kit, page, new Vector2(ControlX, y + 10), _settings.ReduceTransparency, false);
        opaque.Toggled += _settings.SetReduceTransparency;
        y += 40;
        Row(page, y, UiKit.T("settings.reduce_motion"));
        var motion = Segmented.Create(_kit, page, new Vector2(ControlX, y + 2), [88, 64, 64],
            [UiKit.T("settings.follow_system"), UiKit.T("ui.common.on"), UiKit.T("ui.common.off")], false, selected: (int)_settings.ReduceMotion);
        motion.Changed += i => _settings.SetReduceMotion((ReduceMotionMode)i);
        y += 40;
        Row(page, y, UiKit.T("settings.ui_scale"));
        var scale = Fader.Create(_kit, page, new Vector2(ControlX + 6, y + 2), 200, 0.75f, 1.5f, 0.05f, _settings.UiScale, false, v => $"{v * 100:0}%");
        scale.Changed += _settings.SetUiScale;
        y += 40;
        Row(page, y, UiKit.T("settings.single_keys"));
        var keys = DipSwitch.Create(_kit, page, new Vector2(ControlX, y + 10), _settings.SingleKeyShortcuts, false);
        keys.Toggled += _settings.SetSingleKeyShortcuts;
        y += 52;

        Parts.GroupHeader(_kit, page, new Vector2(8, y), width, UiKit.T("settings.group.storage"));
        y += 24;
        Row(page, y, UiKit.T("settings.storage"));
        var storage = _kit.Text(page, "", "data", "text.2", new Vector2(ControlX, y + 11));
        var clear = _kit.Button(page, ButtonStyle.Secondary, UiKit.T("settings.storage.clear"), new Rect2(width - 132 + 8, y, 132, 40));
        void SyncStorage()
        {
            try
            {
                var bake = Svc<IBakeService>();
                var p = bake.Progress;
                var state = p.Total == 0 || p.Done >= p.Total ? UiKit.T("settings.storage.done")
                    : UiKit.T("settings.storage.running", ("pct", $"{p.Done * 100 / Math.Max(1, p.Total)}"));
                storage.Text = $"{bake.CacheBytes / 1048576.0:0.0} MB · {state}";
            }
            catch (InvalidOperationException) { storage.Text = "—"; }
        }
        clear.Pressed += () => { try { Svc<IBakeService>().Clear(); } catch (InvalidOperationException) { } SyncStorage(); };
        SyncStorage();
        page.VisibilityChanged += SyncStorage;
        y += 40;
        Row(page, y, UiKit.T("settings.save"));
        var save = _kit.Text(page, "", "data", "text.2", new Vector2(ControlX, y + 11));
        void SyncSave()
        {
            try
            {
                var s = Svc<ISaveService>().State;
                save.Text = s.ReadOnly switch
                {
                    ReadOnlyReason.None => s.Slot is { } slot ? $"{slot.Value.ToUpperInvariant()} · {(s.Last == SaveStatus.Failed ? UiKit.T("settings.save.failed") : UiKit.T("settings.save.ok"))}" : UiKit.T("settings.save.none"),
                    ReadOnlyReason.SaveOff => UiKit.T("settings.save.off"),
                    ReadOnlyReason.OtherWindow => UiKit.T("settings.save.other_window"),
                    _ => throw new InvalidOperationException("Unknown read-only reason."),
                };
                _kit.Recolor(save, s.Last == SaveStatus.Failed ? "danger.text" : "text.2");
            }
            catch (InvalidOperationException) { save.Text = "—"; }
        }
        SyncSave();
        page.VisibilityChanged += SyncSave;
        return y + 56;
    }

    // ---------------- 画面（SYS-GRAPHICS：只经 IQualityService.Plan / Apply） ----------------
    private float BuildGraphics(Control page, float width)
    {
        IQualityService quality;
        try { quality = Svc<IQualityService>(); }
        catch (InvalidOperationException) { _kit.Text(page, UiKit.T("settings.graphics.unavailable"), "small", "text.2", new Vector2(8, 8)); return 48; }
        float y = 8;
        _preset = Segmented.Create(_kit, page, new Vector2(8, y), [112, 112, 114, 112, 122],
            [UiKit.T("quality.preset.smooth"), UiKit.T("quality.preset.balanced"), UiKit.T("quality.preset.fine"), UiKit.T("quality.preset.ultra"), UiKit.T("quality.preset.custom")],
            false, selected: (int)(_planned?.Preset ?? quality.Requested.Preset));
        _preset.Changed += i =>
        {
            if ((QualityPreset)i == QualityPreset.Custom) return;
            _pending.Clear();
            Plan(new QualityProfile((QualityPreset)i, new Dictionary<QualityKnob, float>(), quality.Requested.Adaptive));
        };
        y += 46;
        var rec = UiKit.T("quality.recommended", ("preset", UiKit.T("quality.preset." + PresetKey(quality.Recommended))), ("gpu", RenderingServer.GetVideoAdapterName()));
        _kit.Text(page, rec, "small", "text.2", new Vector2(8, y), width: width - 8);
        y += 34;
        var specs = quality.Knobs.ToDictionary(s => s.Knob);
        foreach (var (group, knobs) in Groups)
        {
            Parts.GroupHeader(_kit, page, new Vector2(8, y), width, UiKit.T("quality.group." + group));
            y += 24;
            foreach (var knob in knobs)
            {
                if (!specs.TryGetValue(knob, out var spec)) continue;
                Row(page, y, UiKit.T(spec.LabelKey));
                KnobControl(page, new Vector2(ControlX, y + 2), spec, _pending.TryGetValue(knob, out var pending) ? pending : quality.Effective.Requested(knob));
                if (spec.Apply is ApplyClass.UnderVeil or ApplyClass.Restart)
                    Parts.Tag(_kit, page, new Vector2(width - 48, y + 8), UiKit.T(spec.Apply == ApplyClass.Restart ? "quality.tag.restart" : "quality.tag.veil"), spec.Apply == ApplyClass.UnderVeil);
                y += 40;
            }
            y += 12;
        }
        _kit.Text(page, UiKit.T("quality.footnote"), "small", "text.2", new Vector2(8, y), width: width - 8, wrap: true);
        y += 52;
        _applyStatus = _kit.Text(page, "", "small", "text.2", new Vector2(8, y + 12));
        _apply = _kit.Button(page, ButtonStyle.Primary, UiKit.T("quality.apply"), new Rect2(width - 152 + 8, y, 152, 44), "button.primary", 20,
            pressed: () => _ = ApplyPending());
        _apply.OffReason = UiKit.T("quality.apply.nothing");
        _apply.Off = true;
        if (_planned is { } planned) ShowPlan(quality.Plan(planned));   // 上次关卡时没应用的改动
        return y + 60;
    }

    private void KnobControl(Control page, Vector2 at, KnobSpec spec, float value)
    {
        var steps = spec.Step > 0 ? (int)Math.Round((spec.Max - spec.Min) / spec.Step) + 1 : 0;
        if (spec.Step >= 1 && steps is >= 2 and <= 5)
        {
            var labels = new string[steps];
            var widths = new float[steps];
            for (var i = 0; i < steps; i++)
            {
                var v = spec.Min + i * spec.Step;
                var key = $"{spec.LabelKey}.{v:0}";
                labels[i] = Campus.Hud.I18n.Tr.Has(key) ? UiKit.T(key) : steps == 2 ? UiKit.T(v > 0 ? "ui.common.on" : "ui.common.off") : $"{v:0}";
                widths[i] = steps <= 3 ? 72 : 56;
            }
            var seg = Segmented.Create(_kit, page, at, widths, labels, false, selected: (int)Math.Round((value - spec.Min) / spec.Step));
            seg.Changed += i => Change(spec.Knob, spec.Min + i * spec.Step);
            return;
        }
        var step = spec.Step > 0 ? spec.Step : (spec.Max - spec.Min) / 20f;
        var fader = Fader.Create(_kit, page, at + new Vector2(6, 0), 200, spec.Min, spec.Max, step, value, false, v => FormatKnob(spec, v));
        fader.Changed += v => Change(spec.Knob, v);
    }

    private static string FormatKnob(KnobSpec spec, float v) => spec.Knob switch
    {
        QualityKnob.RenderScale => $"{v:0.00}×",
        QualityKnob.ShadowDistance => $"{v:0} m",
        QualityKnob.GrassRadius => $"{v:0} m",
        QualityKnob.FoliageDensity or QualityKnob.ParticleDensity => $"{v * 100:0}%",
        _ => $"{v:0.##}",
    };

    private void Change(QualityKnob knob, float value)
    {
        IQualityService quality;
        try { quality = Svc<IQualityService>(); }
        catch (InvalidOperationException) { return; }
        _pending[knob] = value;
        _preset?.Select((int)QualityPreset.Custom, notify: false);
        var overrides = new Dictionary<QualityKnob, float>(quality.Requested.Overrides);
        foreach (var (k, v) in _pending) overrides[k] = v;
        Plan(new QualityProfile(QualityPreset.Custom, overrides, quality.Requested.Adaptive));
    }

    private QualityProfile? _planned;

    private void Plan(QualityProfile next)
    {
        var plan = Svc<IQualityService>().Plan(next);
        _planned = next;
        ShowPlan(plan);
        if (plan.Heaviest is not (ApplyClass.UnderVeil or ApplyClass.Restart) && plan.Rejected.Count == 0) _ = ApplyPending();
    }

    private void ShowPlan(ApplyPlan plan)
    {
        if (_applyStatus is null || _apply is null) return;
        var veil = plan.Heaviest == ApplyClass.UnderVeil;
        var restart = plan.Heaviest == ApplyClass.Restart;
        _applyStatus.Text = plan.Deltas.Count == 0 && _pending.Count == 0 ? ""
            : UiKit.T(restart ? "quality.pending.restart" : veil ? "quality.pending.veil" : "quality.pending.now", ("n", $"{Math.Max(plan.Deltas.Count, _pending.Count)}"));
        _apply.Off = plan.Rejected.Count > 0;
    }

    private async Task ApplyPending()
    {
        if (_planned is not { } next) return;
        var result = await Svc<IQualityService>().Apply(next, ApplyOrigin.User);
        _pending.Clear();
        _planned = null;
        if (_applyStatus is null || _apply is null) return;   // 等应用的这段时间里卡已经关了，或画面页还没填
        _applyStatus.Text = result.Status switch
        {
            ApplyStatus.Applied => result.RestartPending ? UiKit.T("quality.result.restart") : "",
            ApplyStatus.Deferred => UiKit.T("quality.result.deferred"),
            ApplyStatus.Rejected => UiKit.T("quality.result.rejected"),
            _ => throw new InvalidOperationException("Unknown apply status."),
        };
        _apply.Off = true;
    }

    private void RefreshGraphics()
    {
        if (_preset is null || _pending.Count > 0) return;
        try { _preset.Select((int)Svc<IQualityService>().Requested.Preset, notify: false); }
        catch (InvalidOperationException) { }
    }

    private static string PresetKey(QualityPreset p) => p switch
    {
        QualityPreset.Smooth => "smooth",
        QualityPreset.Balanced => "balanced",
        QualityPreset.Fine => "fine",
        QualityPreset.Ultra => "ultra",
        QualityPreset.Custom => "custom",
        _ => throw new ArgumentOutOfRangeException(nameof(p)),
    };

    // ---------------- 声音（SYS-AUDIO：只调 IAudioService.ApplySettings） ----------------
    private float BuildSound(Control page, float width)
    {
        IAudioService audio;
        try { audio = Svc<IAudioService>(); }
        catch (InvalidOperationException) { return 48; }
        float y = 8;
        Parts.GroupHeader(_kit, page, new Vector2(8, y), width, UiKit.T("sound.group.volume"));
        y += 24;
        (string Key, Func<AudioSettings, float> Get, Func<AudioSettings, float, AudioSettings> Set)[] volumes =
        [
            ("sound.master", s => s.Master, (s, v) => s with { Master = v }),
            ("sound.ambience", s => s.Ambience, (s, v) => s with { Ambience = v }),
            ("sound.foley", s => s.Foley, (s, v) => s with { Foley = v }),
            ("sound.ui", s => s.Ui, (s, v) => s with { Ui = v }),
            ("sound.music", s => s.Music, (s, v) => s with { Music = v }),
        ];
        foreach (var (key, get, set) in volumes)
        {
            Row(page, y, UiKit.T(key));
            var fader = Fader.Create(_kit, page, new Vector2(ControlX + 6, y + 2), 200, 0, 1, 0.05f, get(audio.Settings), false, v => $"{v * 100:0}%");
            fader.Changed += v => audio.ApplySettings(set(audio.Settings, v));
            y += 40;
        }
        y += 12;
        Parts.GroupHeader(_kit, page, new Vector2(8, y), width, UiKit.T("sound.group.calm"));
        y += 24;
        (string Key, bool Value, Func<AudioSettings, bool, AudioSettings> Set)[] toggles =
        [
            ("sound.ui_sounds", audio.Settings.UiSounds, (s, v) => s with { UiSounds = v }),
            ("sound.hover", audio.Settings.HoverSounds, (s, v) => s with { HoverSounds = v }),
            ("sound.quiet", audio.Settings.QuietMode, (s, v) => s with { QuietMode = v }),
            ("sound.mono", audio.Settings.Mono, (s, v) => s with { Mono = v }),
        ];
        foreach (var (key, value, set) in toggles)
        {
            Row(page, y, UiKit.T(key));
            var sw = DipSwitch.Create(_kit, page, new Vector2(ControlX, y + 10), value, false);
            sw.Toggled += v => audio.ApplySettings(set(audio.Settings, v));
            y += 40;
        }
        Row(page, y, UiKit.T("sound.sudden"));
        var sudden = Segmented.Create(_kit, page, new Vector2(ControlX, y + 2), [72, 72, 72],
            [UiKit.T("sound.sudden.normal"), UiKit.T("sound.sudden.softer"), UiKit.T("ui.common.off")], false, selected: (int)audio.Settings.Sudden);
        sudden.Changed += i => audio.ApplySettings(audio.Settings with { Sudden = (SuddenSoundMode)i });
        y += 40;
        Row(page, y, UiKit.T("sound.background"));
        var background = Segmented.Create(_kit, page, new Vector2(ControlX, y + 2), [72, 72, 72],
            [UiKit.T("sound.background.keep"), UiKit.T("sound.background.lower"), UiKit.T("sound.background.mute")], false, selected: (int)audio.Settings.Background);
        background.Changed += i => audio.ApplySettings(audio.Settings with { Background = (BackgroundBehavior)i });
        return y + 56;
    }

    // ---------------- 改键（UI-NAV：按物理键改绑，一个物理键只能绑一个动作） ----------------
    private readonly Dictionary<string, Control> _rebindRows = [];

    private float BuildRebind(Control page, float width)
    {
        float y = 8;
        string[] actions = ["move_forward", "move_backward", "move_left", "move_right", "move_up", "move_down", "sprint", "time_panel", "time_forward",
            "toggle_flow", "next_season", "weather", "toggle_mode", "map", "atlas_index", "minimap", "help", "clean_view"];
        Parts.GroupHeader(_kit, page, new Vector2(8, y), width, UiKit.T("rebind.group"));
        y += 24;
        foreach (var action in actions)
        {
            if (!InputMap.HasAction(action)) continue;
            var row = new Control { Position = new Vector2(0, y), Size = new Vector2(width, 40), MouseFilter = MouseFilterEnum.Ignore };
            page.AddChild(row);
            _rebindRows[action] = row;
            BuildRebindRow(row, action, capturing: false);
            y += 40;
        }
        _kit.Text(page, UiKit.T("rebind.note"), "small", "text.2", new Vector2(8, y + 8), width: width - 8, wrap: true);
        return y + 56;
    }

    private void BuildRebindRow(Control row, string action, bool capturing)
    {
        foreach (var child in row.GetChildren()) child.QueueFree();
        var item = _kit.Button(row, ButtonStyle.Item, "", new Rect2(0, 0, row.Size.X, 40), padX: 8);
        item.Pressed += () => BeginCapture(action);
        _kit.Text(row, UiKit.T("rebind.action." + action), "body", "text.1", new Vector2(8, 8));
        if (capturing) _kit.Text(row, UiKit.T("rebind.waiting"), "small", "text.accent", new Vector2(ControlX, 10));
        else Parts.Keycap(_kit, row, new Vector2(ControlX, 8), KeyLabel(action));
    }

    private void BeginCapture(string action)
    {
        EndCapture();
        _rebindAction = action;
        _rebindLock = TryLock(InputLockReason.Rebind);
        BuildRebindRow(_rebindRows[action], action, capturing: true);
    }

    private void EndCapture()
    {
        if (_rebindAction is { } previous && _rebindRows.TryGetValue(previous, out var row)) BuildRebindRow(row, previous, capturing: false);
        _rebindAction = null;
        _rebindLock?.Dispose();
        _rebindLock = null;
    }

    /// <summary>HudRoot 在改键捕获期间把下一个按键交到这里；Esc 取消这次捕获。返回冲突时的提示键。</summary>
    public string? Capture(InputEventKey key)
    {
        if (_rebindAction is not { } action) return null;
        if (key.PhysicalKeycode == Godot.Key.Escape) { EndCapture(); return null; }
        if (_router is not null)
        {
            // 一个物理键只能绑一个动作，冲突时拒绝；成功后由 WP-06 写 user://input.cfg
            if (!_router.TryRebind(action, (int)key.PhysicalKeycode, out var conflict))
                return conflict is not null && Campus.Hud.I18n.Tr.Has("rebind.action." + conflict)
                    ? UiKit.T("rebind.conflict", ("action", UiKit.T("rebind.action." + conflict)))
                    : UiKit.T("rebind.rejected", ("reason", conflict ?? ""));
            _router.SaveBindings();
            EndCapture();
            return null;
        }
        foreach (var other in InputMap.GetActions())
        {
            var name = other.ToString();
            if (name == action || name.StartsWith("ui_", StringComparison.Ordinal)) continue;
            foreach (var e in InputMap.ActionGetEvents(name))
                if (e is InputEventKey k && k.PhysicalKeycode == key.PhysicalKeycode)
                    return UiKit.T("rebind.conflict", ("action", UiKit.T("rebind.action." + name)));
        }
        InputMap.ActionEraseEvents(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key.PhysicalKeycode });
        EndCapture();
        return null;
    }

    // ---------------- 许可 ----------------
    private float BuildLicenses(Control page, float width)
    {
        float y = 8;
        foreach (var key in new[] { "licenses.self", "licenses.osm", "licenses.fonts", "licenses.godot" })
        {
            var label = _kit.Text(page, UiKit.T(key), "small", "text.1", new Vector2(8, y), width: width - 8, wrap: true);
            y += label.GetCombinedMinimumSize().Y + 16;
        }
        return y + 16;
    }
}
