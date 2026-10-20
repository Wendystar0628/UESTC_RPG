using Campus.Contracts;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

internal sealed class SettingsCardBindings
{
    private readonly UiKit _kit;
    private readonly IInputRouter _router;
    private string? _rebindAction;
    private IDisposable? _rebindLock;

    public SettingsCardBindings(UiKit kit, IInputRouter router)
    {
        _kit = kit;
        _router = router;
    }

    public bool Capturing => _rebindAction is not null;
    public event Action? BindingsChanged;

    private readonly Dictionary<string, TechButton> _rebindRows = [];

    public System.Collections.IEnumerable Build(Control page)
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
        _rebindLock = Campus.Kernel.Registry.Kernel.Get<ICameraRig>().LockInput(InputLockReason.Rebind);
        var button = _rebindRows[action];
        button.Text = UiKit.T("rebind.waiting");
        button.Selected = true;
    }

    public void EndCapture()
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
        BindingsChanged?.Invoke();
        return null;
    }

    public void Release() => _rebindRows.Clear();

    // InputMap owns the action list. Existing action translations select the exposed keyboard controls.
    public static IEnumerable<string> KeyboardActions() => InputMap.GetActions()
        .Select(action => action.ToString())
        .Where(action => Campus.Hud.I18n.Tr.Has("rebind.action." + action) && InputMap.ActionGetEvents(action).Any(input => input is InputEventKey));

    public string KeyLabel(string action)
    {
        var physical = _router.PhysicalKey(action);
        if (physical == 0)
            physical = InputMap.ActionGetEvents(action).OfType<InputEventKey>().Select(key => (int)key.PhysicalKeycode).FirstOrDefault();
        return physical == 0 ? "—" : KeyName((Key)physical);
    }

    private static string KeyName(Key code)
    {
        var label = DisplayServer.KeyboardGetLabelFromPhysical(code);
        var text = OS.GetKeycodeString(label != Godot.Key.None ? label : code);
        return text switch { "Escape" => "Esc", "Period" => ".", "Space" => "Space", _ => text };
    }
}
