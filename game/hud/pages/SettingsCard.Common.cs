using Campus.Contracts;
using Campus.Contracts.Settings;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

public partial class SettingsCard
{
    private static T Svc<T>() where T : class => Campus.Kernel.Registry.Kernel.Get<T>();
    private static string Key(SettingsPage page) => page switch
    {
        SettingsPage.Keys => "keys", SettingsPage.General => "general", SettingsPage.Graphics => "graphics",
        SettingsPage.Sound => "sound", SettingsPage.Rebind => "rebind", SettingsPage.Licenses => "licenses",
        _ => throw new ArgumentOutOfRangeException(nameof(page)),
    };
    private static IDisposable Lock(InputLockReason reason) => Svc<ICameraRig>().LockInput(reason);

    private DipSwitch Toggle(Control parent, SettingDefinition<bool> definition, bool value, Action<bool> changed)
    {
        var row = _kit.Layout.SettingRow(parent, UiKit.T(definition.LabelKey));
        var control = DipSwitch.Create(_kit, row, value);
        control.AccessibilityName = UiKit.T(definition.LabelKey);
        control.Toggled += changed;
        return control;
    }

    private Dropdown Choice(Control parent, SettingDefinition<int> definition, int value, Action<int> changed)
    {
        var options = definition.Options ?? throw new InvalidOperationException("Setting options are required.");
        var selected = options.ToList().FindIndex(option => option.Value == value);
        if (selected < 0) selected = options.ToList().FindIndex(option => option.Value == definition.Default);
        var row = _kit.Layout.SettingRow(parent, UiKit.T(definition.LabelKey));
        var control = Dropdown.Create(_kit, row, options.Select(option => UiKit.T(option.LabelKey)).ToArray(), selected);
        control.AccessibilityName = UiKit.T(definition.LabelKey);
        control.Changed += index => changed(options[index].Value);
        return control;
    }

    private Fader Slider(Control parent, SettingDefinition<float> definition, float value, Action<float> changed, Func<float, string> format)
    {
        var range = definition.Range ?? throw new InvalidOperationException("Setting range is required.");
        var row = _kit.Layout.SettingRow(parent, UiKit.T(definition.LabelKey));
        var control = Fader.Create(_kit, row, (float)range.Min, (float)range.Max, (float)range.Step, value, format: format);
        control.AccessibilityName = UiKit.T(definition.LabelKey);
        control.Changed += changed;
        return control;
    }

    // InputMap owns the action list. Existing action translations select the exposed keyboard controls.
    private static IEnumerable<string> KeyboardActions() => InputMap.GetActions()
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
