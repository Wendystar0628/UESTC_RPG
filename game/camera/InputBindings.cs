using Campus.Contracts.Settings;
using Godot;

namespace Campus.Camera;

/// <summary>Physical key bindings, stored as action name to key code in the input section of settings.json.</summary>
public sealed class InputBindings
{
    public static readonly string[] Actions =
    [
        "move_forward", "move_backward", "move_left", "move_right", "move_up", "move_down", "sprint",
        "jump", "crouch", "map", "atlas_index", "minimap", "toggle_mode", "time_panel", "time_forward",
        "toggle_flow", "next_season", "weather", "help", "clean_view", "recover", "waypoint_previous",
        "waypoint_next", "waypoint_0", "waypoint_1", "waypoint_2", "waypoint_3", "waypoint_4",
        "waypoint_5", "waypoint_6", "waypoint_7", "waypoint_8", "waypoint_9"
    ];
    private const string Section = "input";
    private readonly Dictionary<string, int> _keys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _defaults = new(StringComparer.Ordinal);

    public void Initialize(ISettingsStore? settings)
    {
        foreach (string action in Actions)
        {
            if (!InputMap.HasAction(action)) throw new InvalidDataException($"Missing input action: {action}");
            foreach (var input in InputMap.ActionGetEvents(action))
            {
                if (input is not InputEventKey key) continue;
                _keys[action] = _defaults[action] = (int)key.PhysicalKeycode;
                break;
            }
        }
        if (settings is null) return;
        if (!TryLoad(settings)) GD.Print("Input bindings: defaults (no valid saved bindings).");
        ApplyAll();
    }

    public int PhysicalKey(string action) => _keys.GetValueOrDefault(action);
    public bool Rebind(string action, int physicalKey, out string? conflict)
    {
        conflict = null;
        if (!Actions.Contains(action, StringComparer.Ordinal) || !ValidKey(physicalKey)) return false;
        string canonical = Canonical(action);
        foreach (string other in Actions)
        {
            if (Canonical(other) == canonical || _keys.GetValueOrDefault(other) != physicalKey) continue;
            conflict = other;
            return false;
        }
        foreach (string linked in Actions.Where(a => Canonical(a) == canonical))
        {
            _keys[linked] = physicalKey;
            Apply(linked, physicalKey);
        }
        return true;
    }

    public bool Save(ISettingsStore settings)
    {
        var section = settings.Section(Section);
        foreach (string action in Actions) section.Set(action, _keys[action]);
        if (settings.TrySave()) return true;
        GD.PushWarning("Input bindings were changed but could not be saved.");
        return false;
    }

    private bool TryLoad(ISettingsStore settings)
    {
        var section = settings.Section(Section);
        if (!Actions.Any(section.Has)) return false;
        var candidate = new Dictionary<string, int>(_defaults, StringComparer.Ordinal);
        foreach (string action in Actions)
        {
            int value = section.Int(action, 0, 1, int.MaxValue);
            if (value > 0 && ValidKey(value)) candidate[action] = value;
        }
        // Aliases share a physical control across mutually exclusive fly/walk modes.
        candidate["jump"] = candidate["move_up"];
        candidate["crouch"] = candidate["move_down"];
        var used = new Dictionary<int, string>();
        foreach (string action in Actions)
        {
            string canonical = Canonical(action);
            int key = candidate[action];
            if (used.TryGetValue(key, out string? existing) && existing != canonical) return false;
            used[key] = canonical;
        }
        foreach (string action in Actions) _keys[action] = candidate[action];
        return true;
    }

    private static bool ValidKey(int key) => key > 0 && key != (int)Key.Escape && Enum.IsDefined((Key)key);
    private static string Canonical(string action) => action switch
    {
        "jump" => "move_up",
        "crouch" => "move_down",
        _ => action
    };
    private void ApplyAll() { foreach (string action in Actions) Apply(action, _keys[action]); }
    private static void Apply(string action, int key)
    {
        InputMap.ActionEraseEvents(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = (Key)key });
    }
}
