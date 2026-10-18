using Campus.Contracts;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

// 改键页（UI-NAV：按物理键改绑，一个物理键只能绑一个动作）
public partial class SettingsCard
{
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
            // 一个物理键只能绑一个动作，冲突时拒绝；成功后由 WP-06 写 settings.json 的 input 节
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
}
