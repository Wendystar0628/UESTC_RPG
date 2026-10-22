using KernelServices = Campus.Kernel.Registry.Kernel;
using Campus.Contracts.Diagnostics;
using Campus.Contracts.Settings;
using Godot;

namespace Campus.Camera;

/// <summary>Physical key codes and native locations, saved together in the input settings section.</summary>
public sealed class InputBindings
{
    public const string CursorAction = "show_cursor";
    public static readonly string[] Actions =
    [
        "move_forward", "move_backward", "move_left", "move_right", "move_up", "move_down", "sprint",
        "jump", "crouch", "map", "atlas_index", "minimap", "toggle_mode", "time_panel", "time_forward",
        "toggle_flow", "next_season", "weather", "help", "clean_view", "recover", "waypoint_previous",
        "waypoint_next", "waypoint_0", "waypoint_1", "waypoint_2", "waypoint_3", "waypoint_4",
        "waypoint_5", "waypoint_6", "waypoint_7", "waypoint_8", "waypoint_9", "photo_export", CursorAction
    ];
    private const string Section = "input";
    private readonly Dictionary<string, int> _keys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _locations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SettingDefinition<int>> _definitions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SettingDefinition<int>> _locationDefinitions = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, SettingDefinition<int>> Definitions => _definitions;

    public void Initialize(ISettingsStore? settings)
    {
        foreach (string action in Actions)
        {
            if (!InputMap.HasAction(action)) throw new InvalidDataException($"Missing input action: {action}");
            foreach (var input in InputMap.ActionGetEvents(action))
            {
                if (input is not InputEventKey key) continue;
                _definitions[action] = new(action, (int)key.PhysicalKeycode, "input." + action, new(1, int.MaxValue, 1));
                _keys[action] = _definitions[action].Default;
                _locationDefinitions[action] = new(LocationKey(action), (int)key.Location,
                    "input." + LocationKey(action), new((int)KeyLocation.Unspecified, (int)KeyLocation.Right, 1));
                _locations[action] = (int)key.Location;
                break;
            }
        }
        if (settings is null) return;
        if (!TryLoad(settings)) GD.Print("Input bindings: defaults (no valid saved bindings).");
        ApplyAll();
    }

    public int PhysicalKey(string action) => _keys.GetValueOrDefault(action);
    public bool Rebind(string action, int physicalKey, int physicalLocation, out string? conflict)
    {
        conflict = null;
        if (!Actions.Contains(action, StringComparer.Ordinal) || !ValidKey(physicalKey)
            || !Enum.IsDefined((KeyLocation)physicalLocation)) return false;
        string canonical = Canonical(action);
        foreach (string other in Actions)
        {
            if (Canonical(other) == canonical || !Overlaps(_keys[other], _locations[other], physicalKey, physicalLocation)) continue;
            conflict = other;
            return false;
        }
        foreach (string linked in Actions.Where(a => Canonical(a) == canonical))
        {
            _keys[linked] = physicalKey;
            _locations[linked] = physicalLocation;
            Apply(linked);
        }
        return true;
    }

    public bool Save(ISettingsStore settings)
    {
        var section = settings.Section(Section);
        foreach (string action in Actions)
        {
            section.Set(action, _keys[action]);
            section.Set(LocationKey(action), _locations[action]);
        }
        if (settings.TrySave()) return true;
        KernelServices.Get<IDiagnosticLog>().Warning("Input bindings", "Input bindings were changed but could not be saved.");
        return false;
    }

    private bool TryLoad(ISettingsStore settings)
    {
        var section = settings.Section(Section);
        if (!Actions.Any(section.Has)) return false;
        var candidate = _definitions.ToDictionary(pair => pair.Key, pair => pair.Value.Default, StringComparer.Ordinal);
        var locations = _locationDefinitions.ToDictionary(pair => pair.Key, pair => pair.Value.Default, StringComparer.Ordinal);
        foreach (string action in Actions)
        {
            int value = section.Get(_definitions[action]);
            if (value > 0 && ValidKey(value)) candidate[action] = value;
            locations[action] = section.Get(_locationDefinitions[action]);
        }
        // Aliases share a physical control across mutually exclusive fly/walk modes.
        candidate["jump"] = candidate["move_up"];
        candidate["crouch"] = candidate["move_down"];
        locations["jump"] = locations["move_up"];
        locations["crouch"] = locations["move_down"];
        var used = new List<(int Key, int Location, string Canonical)>();
        foreach (string action in Actions)
        {
            string canonical = Canonical(action);
            int key = candidate[action];
            if (used.Any(existing => existing.Canonical != canonical && Overlaps(existing.Key, existing.Location, key, locations[action])))
                return false;
            used.Add((key, locations[action], canonical));
        }
        foreach (string action in Actions)
        {
            _keys[action] = candidate[action];
            _locations[action] = locations[action];
        }
        return true;
    }

    private static string LocationKey(string action) => action + ".location";
    private static bool Overlaps(int key, int location, int otherKey, int otherLocation) =>
        key == otherKey && (location == (int)KeyLocation.Unspecified
            || otherLocation == (int)KeyLocation.Unspecified || location == otherLocation);
    private static bool ValidKey(int key) => key > 0 && key != (int)Key.Escape && Enum.IsDefined((Key)key);
    private static string Canonical(string action) => action switch
    {
        "jump" => "move_up",
        "crouch" => "move_down",
        _ => action
    };
    private void ApplyAll() { foreach (string action in Actions) Apply(action); }
    private void Apply(string action)
    {
        InputMap.ActionEraseEvents(action);
        InputMap.ActionAddEvent(action, new InputEventKey
        {
            PhysicalKeycode = (Key)_keys[action], Location = (KeyLocation)_locations[action]
        });
    }
}
