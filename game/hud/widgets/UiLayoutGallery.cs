using Campus.Contracts;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// --ui-gallery=layout 的同一页布局样例；关闭时释放整棵树及其材质持有者。
/// 另管画廊页的排版收敛检查（<see cref="AssertSettledAsync"/>），界面轮的大页与界面测试共用这一份判据。
/// </summary>
public static class UiLayoutGallery
{
    // 帧数按 demo 量级取：界面轮固定 60 帧，出现、落定、观察各约 2 s、0.2 s、0.5 s；结构一直在增删就按 4 s 判停滞。
    private const int AppearFrames = 120, SettleFrames = 10, ObserveFrames = 30, StructureFrames = 240;

    /// <summary>
    /// 等 <paramref name="root"/> 下出现可见内容，结构（可见控件集合）连续 <see cref="SettleFrames"/> 帧不变后再看
    /// <see cref="ObserveFrames"/> 帧：同一控件的尺寸在观察窗里变了两帧以上即抛 <see cref="InvalidOperationException"/>，
    /// 这正是多列容器里换行文字"撑宽→压回→再撑宽"的活锁；一次性的后到变化（滚动条出现）不算。
    /// 逐帧在帧首取落定的尺寸而不连 Resized：同帧内容器的多轮排版画不出来，只有跨帧反复才看得见。
    /// </summary>
    public static async Task AssertSettledAsync(Control root, CancellationToken lifetime)
    {
        var previous = new Dictionary<Control, Vector2>();
        var current = new Dictionary<Control, Vector2>();
        var changes = new Dictionary<Control, int>();
        for (var frame = 0; ; frame++)
        {
            current.Clear();
            Collect(root, current);
            if (current.Count > 1) break;
            if (frame >= AppearFrames) throw new TimeoutException($"No visible page appeared under {root.Name} for the layout check.");
            await FrameAwaiter.ProcessAsync(root, lifetime);
        }
        int stable = 0, observed = 0;
        for (var frame = 0; observed < ObserveFrames; frame++)
        {
            (previous, current) = (current, previous);
            await FrameAwaiter.ProcessAsync(root, lifetime);
            current.Clear();
            Collect(root, current);
            if (current.Count != previous.Count || current.Keys.Any(control => !previous.ContainsKey(control)))
            {
                if (frame >= StructureFrames) throw new TimeoutException($"The page under {root.Name} kept adding or removing controls.");
                stable = observed = 0;
                changes.Clear();
                continue;
            }
            if (stable < SettleFrames) { stable++; continue; }
            observed++;
            foreach (var (control, size) in current)
            {
                if (size == previous[control]) continue;
                var count = changes[control] = changes.GetValueOrDefault(control) + 1;
                if (count >= 2)
                    throw new InvalidOperationException($"Layout did not settle: {root.GetPathTo(control)} resized in {count} of {observed} observed frames ({previous[control]} -> {size}).");
            }
        }
    }

    // 只收真实可见的控件；内部子件（滚动条）一并算，隐藏的子树不参与排版。
    private static void Collect(Control root, Dictionary<Control, Vector2> sizes)
    {
        if (!root.Visible) return;
        sizes[root] = root.Size;
        foreach (var child in root.GetChildren(includeInternal: true))
            if (child is Control control) Collect(control, sizes);
    }

    public static Control Create(UiKit kit, Control parent)
    {
        var layout = kit.Layout;
        var root = layout.Host(parent, "LayoutGallery");
        var lease = Campus.Kernel.Registry.Kernel.Get<ICameraRig>().LockInput(InputLockReason.Menu);
        root.TreeExiting += lease.Dispose;
        root.Theme = kit.Theme.Theme;
        var scrim = kit.Box(root, new UiFx(Fill: "scrim", Margin: 0));
        scrim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var safe = layout.SafeFrame(root);
        var frame = layout.Page(safe, UiKit.T("hud.button.settings"), "savesWidth", centered: true);
        foreach (var key in new[] { "settings.theme", "settings.group.storage", "settings.tab.sound" })
            kit.Button(frame.Tabs, ButtonStyle.Tab, UiKit.T(key));

        var group = layout.SettingGroup(frame.Body, UiKit.T("settings.group.interface"));
        var theme = layout.SettingRow(group, UiKit.T("settings.theme"));
        Segmented.Create(kit, theme, [UiKit.T("settings.theme.auto"), UiKit.T("settings.theme.light"), UiKit.T("settings.theme.dark")], selected: 0);
        var resident = layout.SettingRow(group, UiKit.T("settings.resident"));
        Dropdown.Create(kit, resident, [UiKit.T("settings.resident.dark"), UiKit.T("settings.resident.follow")], selected: 0);
        var motion = layout.SettingRow(group, UiKit.T("settings.reduce_motion"));
        DipSwitch.Create(kit, motion, true);
        var scale = layout.SettingRow(group, UiKit.T("settings.ui_scale"));
        Fader.Create(kit, scale, 0.75f, 1.5f, 0.05f, 1, format: v => $"{v * 100:0}%");

        Parts.GroupHeader(kit, frame.Body, UiKit.T("settings.group.storage"));
        var cards = layout.Cards(frame.Body);
        for (var i = 0; i < 3; i++)
        {
            var card = layout.Card(cards);
            kit.Text(card, UiKit.T("saves.title") + $" {i + 1:00}", "h3");
            kit.Text(card, UiKit.T("saves.detail.empty"), colorRole: "text.2");
            kit.Button(card, ButtonStyle.Secondary, UiKit.T("saves.new_here"), pressed: () => OpenDialog(kit, safe, frame));
        }
        kit.Button(frame.Footer, ButtonStyle.Secondary, UiKit.T("ui.common.close"), pressed: root.QueueFree);
        return root;
    }

    private static void OpenDialog(UiKit kit, Control safe, UiPageFrame page)
    {
        UiLayout.SetInteractive(page.Shell, false);
        var host = kit.Layout.Host(safe, "DialogGallery");
        var scrim = kit.Box(host, new UiFx(Fill: "scrim", Margin: 0));
        scrim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var dialog = kit.Layout.Dialog(host, UiKit.T("saves.overwrite.title"));
        kit.Text(dialog.Body, UiKit.T("saves.detail.overwrite"));
        void Close() { UiLayout.SetInteractive(page.Shell, true); host.QueueFree(); }
        kit.Button(dialog.Footer, ButtonStyle.Secondary, UiKit.T("saves.overwrite.cancel"), pressed: Close);
        kit.Button(dialog.Footer, ButtonStyle.Primary, UiKit.T("ui.common.close"), pressed: Close);
    }
}
