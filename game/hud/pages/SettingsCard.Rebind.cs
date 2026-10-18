using Campus.Contracts;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

public partial class SettingsCard
{
    private readonly Dictionary<string, TechButton> _rebindRows = [];

    private System.Collections.IEnumerable BuildRebind(Control page)
    {
        var group = _kit.Layout.SettingGroup(page, UiKit.T("rebind.group"));
        foreach (var action in KeyboardActions())
        {
            var row = _kit.Layout.SettingRow(group, UiKit.T("rebind.action." + action));
            var button = _kit.Button(row, ButtonStyle.Secondary, KeyLabel(action), "keycap", pressed: () => BeginCapture(action));
            button.AccessibilityName = UiKit.T("rebind.action." + action);
            _rebindRows[action] = button;
            yield return null;
        }
        _kit.Text(page, UiKit.T("rebind.note"), "small", "text.2");
    }

    private void BeginCapture(string action)
    {
        EndCapture();
        _rebindAction = action;
        _rebindLock = Lock(InputLockReason.Rebind);
        var button = _rebindRows[action];
        button.Text = UiKit.T("rebind.waiting");
        button.Selected = true;
    }

    private void EndCapture()
    {
        if (_rebindAction is { } previous && _rebindRows.TryGetValue(previous, out var button))
        {
            button.Text = KeyLabel(previous);
            button.Selected = false;
        }
        _rebindAction = null;
        _rebindLock?.Dispose();
        _rebindLock = null;
    }

    /// <summary>HudRoot 在捕获期间把下一个按键交到这里；Esc 取消，冲突返回提示。</summary>
    public string? Capture(InputEventKey key)
    {
        if (_rebindAction is not { } action) return null;
        if (key.PhysicalKeycode == Godot.Key.Escape) { EndCapture(); return null; }
        if (!_router.TryRebind(action, (int)key.PhysicalKeycode, out var conflict))
            return conflict is not null && Campus.Hud.I18n.Tr.Has("rebind.action." + conflict)
                ? UiKit.T("rebind.conflict", ("action", UiKit.T("rebind.action." + conflict)))
                : UiKit.T("rebind.rejected", ("reason", conflict ?? ""));
        _router.SaveBindings();
        EndCapture();
        // 键位页如已打开过，同步反映本次改键；不重建可交互的改键行。
        if (_filled.Remove(SettingsPage.Keys))
        {
            var content = _pages[SettingsPage.Keys];
            foreach (var child in content.GetChildren()) { content.RemoveChild(child); child.QueueFree(); }
        }
        return null;
    }
}
