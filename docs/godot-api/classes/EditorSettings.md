<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorSettings

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Object that holds the project-independent editor settings. These settings are generally visible in the **Editor > Editor Settings** menu.

Property names use slash delimiters to distinguish sections. Setting values can be of any [`Variant`](Variant.md) type. It's recommended to use `snake_case` for editor settings to be consistent with the Godot editor itself.

Editor settings are saved automatically when changed.

Accessing the settings can be done using the following methods, such as:

```csharp
EditorSettings settings = EditorInterface.Singleton.GetEditorSettings();
// `settings.set("some/property", value)` also works as this class overrides `_set()` internally.
settings.SetSetting("some/property", Value);
// `settings.get("some/property", value)` also works as this class overrides `_get()` internally.
settings.GetSetting("some/property");
Godot.Collections.Array<Godot.Collections.Dictionary> listOfSettings = settings.GetPropertyList();

```

**Note:** This class shouldn't be instantiated directly. Instead, access the singleton using [`EditorInterface.GetEditorSettings`](EditorInterface.md).

## 构造函数

### EditorSettings

```csharp
public EditorSettings()
```

## 方法

### AddPropertyInfo

```csharp
public void AddPropertyInfo(Dictionary info)
```

引擎名：`add_property_info`

Adds a custom property info to a property. The dictionary must contain:

- `name`: `System.String` (the name of the property)

- `type`: `System.Int32` (see [`Variant.Type`](Variant.md))

- optionally `hint`: `System.Int32` (see [`PropertyHint`](PropertyHint.md)) and `hint_string`: `System.String`

```csharp
var settings = GetEditorInterface().GetEditorSettings();
settings.Set("category/property_name", 0);

var propertyInfo = new Godot.Collections.Dictionary
{
{ "name", "category/propertyName" },
{ "type", Variant.Type.Int },
{ "hint", PropertyHint.Enum },
{ "hint_string", "one,two,three" },
};

settings.AddPropertyInfo(propertyInfo);

```

### AddShortcut

```csharp
public void AddShortcut(string path, Shortcut shortcut)
```

引擎名：`add_shortcut`

Adds a `shortcut` whose path is specified by `path`.

The `path` determines how the shortcut is organized and displayed in the editor's shortcut settings. The path format affects the display as follows:

- `"name"` (no slash): Creates a category named `name` with the shortcut displayed as `name`.

- `"category/name"` (single slash): Displays as `name` in the `category` section.

- `"category/name/extra"` (multiple slashes): Extra path components are ignored, so this behaves the same as `"category/name"`.

**Note:** Shortcuts are only saved to the editor settings if they differ from their original/default state. This means empty shortcuts that were originally empty will not persist between editor sessions and must be re-added. If a shortcut with the same `path` already exists, this method will update it with the new `shortcut` instead of creating a duplicate.

```csharp
# Add a custom shortcut for a plugin action.
var my_shortcut = Shortcut.new()
var input_event = InputEventKey.new()
input_event.keycode = KEY_F5
input_event.ctrl_pressed = true
my_shortcut.events.append(input_event)

# This will appear under the "My Plugin" category as "Reload Data".
EditorInterface.get_editor_settings().add_shortcut("my_plugin/reload_data", my_shortcut)

# This will appear under the "Test Action" category as "Test Action".
EditorInterface.get_editor_settings().add_shortcut("test_action", my_shortcut)

```

### CheckChangedSettingsInGroup

```csharp
public bool CheckChangedSettingsInGroup(string settingPrefix)
```

引擎名：`check_changed_settings_in_group`

Checks if any settings with the prefix `settingPrefix` exist in the set of changed settings. See also `GetChangedSettings()`.

### Erase

```csharp
public void Erase(string property)
```

引擎名：`erase`

Erases the setting whose name is specified by `property`.

### GetChangedSettings

```csharp
public string[] GetChangedSettings()
```

引擎名：`get_changed_settings`

Gets an array of the settings which have been changed since the last save. Note that internally `changed_settings` is cleared after a successful save, so generally the most appropriate place to use this method is when processing `NotificationEditorSettingsChanged`.

### GetFavorites

```csharp
public string[] GetFavorites()
```

引擎名：`get_favorites`

Returns the list of favorite files and directories for this project.

### GetProjectMetadata

```csharp
public Variant GetProjectMetadata(string section, string key, Variant default = default)
```

引擎名：`get_project_metadata`

Returns project-specific metadata for the `section` and `key` specified. If the metadata doesn't exist, `default` will be returned instead. See also `SetProjectMetadata()`.

### GetRecentDirs

```csharp
public string[] GetRecentDirs()
```

引擎名：`get_recent_dirs`

Returns the list of recently visited folders in the file dialog for this project.

### GetSetting

```csharp
public Variant GetSetting(string name)
```

引擎名：`get_setting`

Returns the value of the setting specified by `name`. This is equivalent to using [`GodotObject.Get`](GodotObject.md) on the EditorSettings instance.

### GetShortcut

```csharp
public Shortcut GetShortcut(string path)
```

引擎名：`get_shortcut`

Returns the shortcut specified by `path`. Tries to find a built-in action if no shortcut with the provided path is found in the shortcut list. If found, adds it to the list and returns it, otherwise returns `null`.

### GetShortcutList

```csharp
public string[] GetShortcutList()
```

引擎名：`get_shortcut_list`

Returns the list of stored shortcut paths.

### HasSetting

```csharp
public bool HasSetting(string name)
```

引擎名：`has_setting`

Returns `true` if the setting specified by `name` exists, `false` otherwise.

### HasShortcut

```csharp
public bool HasShortcut(string path)
```

引擎名：`has_shortcut`

Returns `true` if the shortcut specified by `path` exists, `false` otherwise.

### IsShortcut

```csharp
public bool IsShortcut(string path, InputEvent event)
```

引擎名：`is_shortcut`

Returns `true` if the shortcut specified by `path` matches the event specified by `event`, `false` otherwise.

### MarkSettingChanged

```csharp
public void MarkSettingChanged(string setting)
```

引擎名：`mark_setting_changed`

Marks the passed editor setting as being changed, see `GetChangedSettings()`. Only settings which exist (see `HasSetting()`) will be accepted.

### RemoveShortcut

```csharp
public void RemoveShortcut(string path)
```

引擎名：`remove_shortcut`

Removes the shortcut specified by `path`.

### SetBuiltinActionOverride

```csharp
public void SetBuiltinActionOverride(string name, Array<InputEvent> actionsList)
```

引擎名：`set_builtin_action_override`

Overrides the built-in editor action `name` with the input actions defined in `actionsList`.

### SetFavorites

```csharp
public void SetFavorites(ReadOnlySpan<string> dirs)
```

引擎名：`set_favorites`

Sets the list of favorite files and directories for this project.

### SetFavorites

```csharp
public void SetFavorites(string[] dirs)
```

引擎名：`set_favorites`

Sets the list of favorite files and directories for this project.

### SetInitialValue

```csharp
public void SetInitialValue(StringName name, Variant value, bool updateCurrent)
```

引擎名：`set_initial_value`

Sets the initial value of the setting specified by `name` to `value`. This is used to provide a value for the Revert button in the Editor Settings. If `updateCurrent` is `true`, the setting is reset to `value` as well.

### SetProjectMetadata

```csharp
public void SetProjectMetadata(string section, string key, Variant data)
```

引擎名：`set_project_metadata`

Sets project-specific metadata with the `section`, `key` and `data` specified. This metadata is stored outside the project folder and therefore won't be checked into version control. See also `GetProjectMetadata()`.

### SetRecentDirs

```csharp
public void SetRecentDirs(ReadOnlySpan<string> dirs)
```

引擎名：`set_recent_dirs`

Sets the list of recently visited folders in the file dialog for this project.

### SetRecentDirs

```csharp
public void SetRecentDirs(string[] dirs)
```

引擎名：`set_recent_dirs`

Sets the list of recently visited folders in the file dialog for this project.

### SetSetting

```csharp
public void SetSetting(string name, Variant value)
```

引擎名：`set_setting`

Sets the `value` of the setting specified by `name`. This is equivalent to using [`GodotObject.Set`](GodotObject.md) on the EditorSettings instance.

## 事件（信号）

### SettingsChanged

```csharp
public event Action SettingsChanged
```

引擎名：`settings_changed`

Emitted after any editor setting has changed.

## 常量

### NotificationEditorSettingsChanged

```csharp
public const long NotificationEditorSettingsChanged = 10000
```

Emitted after any editor setting has changed. It's used by various editor plugins to update their visuals on theme changes or logic on configuration changes.
