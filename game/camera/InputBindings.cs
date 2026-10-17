using System.Text;
using Godot;

namespace Campus.Camera;

/// <summary>Primitive-only ConfigFile storage; parsing is never attempted on object-bearing text.</summary>
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
    private readonly Dictionary<string, int> _keys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _defaults = new(StringComparer.Ordinal);

    public void Initialize(bool readUserSettings)
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
        if (!readUserSettings) return;
        string path = ProjectSettings.GlobalizePath("user://input.cfg");
        if (!TryLoad(path) && !TryLoad(path + ".bak")) GD.Print("Input bindings: defaults (no valid saved file).");
        ApplyAll();
    }

    public int PhysicalKey(string action) => _keys.GetValueOrDefault(action);
    public string KeyLabel(string action)
    {
        var label = DisplayServer.KeyboardGetLabelFromPhysical((Key)PhysicalKey(action));
        using var input = new InputEventKey { PhysicalKeycode = label };
        return input.AsTextPhysicalKeycode();
    }

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

    public bool Save()
    {
        using var cfg = new ConfigFile();
        foreach (string action in Actions) cfg.SetValue("physical", action, _keys[action]);
        string path = ProjectSettings.GlobalizePath("user://input.cfg");
        byte[] bytes = Encoding.UTF8.GetBytes(cfg.EncodeToText());
        try { WriteAtomic(path, bytes); return true; }
        catch (IOException error) { GD.PushWarning($"Input bindings were changed but could not be saved: {error.Message}"); return false; }
        catch (UnauthorizedAccessException error) { GD.PushWarning($"Input bindings could not be saved: {error.Message}"); return false; }
    }

    private bool TryLoad(string path)
    {
        try
        {
            if (!File.Exists(path)) return false;
            if (new FileInfo(path).Length > 64 * 1024) return false;
            string text = File.ReadAllText(path, Encoding.UTF8);
            if (!IsPrimitiveText(text)) { GD.PushWarning("Input bindings rejected object-bearing settings text."); return false; }
            using var cfg = new ConfigFile();
            if (cfg.Parse(text) != Error.Ok || !cfg.HasSection("physical")) return false;
            var candidate = new Dictionary<string, int>(_defaults, StringComparer.Ordinal);
            foreach (string action in Actions)
            {
                if (!cfg.HasSectionKey("physical", action)) continue;
                var value = cfg.GetValue("physical", action);
                if (value.VariantType == Variant.Type.Int && value.AsInt64() is > 0 and <= int.MaxValue
                    && ValidKey(value.AsInt32())) candidate[action] = value.AsInt32();
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
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public static bool IsPrimitiveText(string text) =>
        !new[] { "Object(", "Resource(", "SubResource(", "ExtResource(" }
            .Any(token => text.Contains(token, StringComparison.Ordinal));

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

    private static void WriteAtomic(string path, byte[] bytes)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                using (var stream = new FileStream(path + ".tmp", FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
                { stream.Write(bytes); stream.Flush(true); }
                if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
                else File.Move(path + ".tmp", path);
                return;
            }
            catch (IOException) when (attempt < 3) { Thread.Sleep(50); }
        }
    }
}
