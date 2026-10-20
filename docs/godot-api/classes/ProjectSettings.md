<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ProjectSettings

命名空间 `Godot`；static class

Stores variables that can be accessed from everywhere. Use `GetSetting()`, `SetSetting()` or `HasSetting()` to access them. Variables stored in `project.godot` are also loaded into `ProjectSettings`, making this object very useful for reading custom game configuration options.

When naming a Project Settings property, use the full path to the setting including the category. For example, `"application/config/name"` for the project name. Category and property names can be viewed in the Project Settings dialog.

**Feature tags:** Project settings can be overridden for specific platforms and configurations (debug, release, ...) using [feature tags]($DOCS_URL/tutorials/export/feature_tags.html).

**Overriding:** Any project setting can be overridden by creating a file named `override.cfg` in the project's root directory. This file is in the same format as `project.godot`, and can also be written using [`ConfigFile`](ConfigFile.md). This can also be used in exported projects by placing this file in the same directory as the project binary. Overriding will still take the base project settings' [feature tags]($DOCS_URL/tutorials/export/feature_tags.html) in account. Therefore, make sure to *also* override the setting with the desired feature tags if you want them to override base project settings on all platforms and configurations.

## 属性

### Singleton

```csharp
public static ProjectSettingsInstance Singleton { get; }
```

## 方法

### AddPropertyInfo

```csharp
public static void AddPropertyInfo(Dictionary hint)
```

引擎名：`add_property_info`

Adds a custom property info to a property. The dictionary must contain:

- `"name"`: `System.String` (the property's name)

- `"type"`: `System.Int32` (see [`Variant.Type`](Variant.md))

- optionally `"hint"`: `System.Int32` (see [`PropertyHint`](PropertyHint.md)) and `"hint_string"`: `System.String`

```csharp
ProjectSettings.Singleton.Set("category/property_name", 0);

var propertyInfo = new Godot.Collections.Dictionary
{
{ "name", "category/propertyName" },
{ "type", (int)Variant.Type.Int },
{ "hint", (int)PropertyHint.Enum },
{ "hint_string", "one,two,three" },
};

ProjectSettings.AddPropertyInfo(propertyInfo);

```

**Note:** Setting `"usage"` for the property is not supported. Use `SetAsBasic()`, `SetRestartIfChanged()`, and `SetAsInternal()` to modify usage flags.

### CheckChangedSettingsInGroup

```csharp
public static bool CheckChangedSettingsInGroup(string settingPrefix)
```

引擎名：`check_changed_settings_in_group`

Checks if any settings with the prefix `settingPrefix` exist in the set of changed settings. See also `GetChangedSettings()`.

### Clear

```csharp
public static void Clear(string name)
```

引擎名：`clear`

Clears the whole configuration (not recommended, may break things).

### GetChangedSettings

```csharp
public static string[] GetChangedSettings()
```

引擎名：`get_changed_settings`

Gets an array of the settings which have been changed since the last save. Note that internally `changed_settings` is cleared after a successful save, so generally the most appropriate place to use this method is when processing `SettingsChanged`.

### GetGlobalClassList

```csharp
public static Array<Dictionary> GetGlobalClassList()
```

引擎名：`get_global_class_list`

Returns an [`Collections.Array`](Collections.Array.md) of registered global classes. Each global class is represented as a [`Collections.Dictionary`](Collections.Dictionary.md) that contains the following entries:

- `base` is a name of the base class;

- `class` is a name of the registered global class;

- `icon` is a path to a custom icon of the global class, if it has any;

- `language` is a name of a programming language in which the global class is written;

- `path` is a path to a file containing the global class.

**Note:** Both the script and the icon paths are local to the project filesystem, i.e. they start with `res://`.

### GetOrder

```csharp
public static int GetOrder(string name)
```

引擎名：`get_order`

Returns the order of a configuration value (influences when saved to the config file).

### GetSetting

```csharp
public static Variant GetSetting(string name, Variant defaultValue = default)
```

引擎名：`get_setting`

Returns the value of the setting identified by `name`. If the setting doesn't exist and `defaultValue` is specified, the value of `defaultValue` is returned. Otherwise, `null` is returned.

```csharp
GD.Print(ProjectSettings.GetSetting("application/config/name"));
GD.Print(ProjectSettings.GetSetting("application/config/custom_description", "No description specified."));

```

**Note:** This method doesn't take potential feature overrides into account automatically. Use `GetSettingWithOverride()` to handle seamlessly.

See also `HasSetting()` to check whether a setting exists.

### GetSettingWithOverride

```csharp
public static Variant GetSettingWithOverride(StringName name)
```

引擎名：`get_setting_with_override`

Similar to `GetSetting()`, but applies feature tag overrides if any exists and is valid.

**Example:** If the setting override `"application/config/name.windows"` exists, and the following code is executed on a *Windows* operating system, the overridden setting is printed instead:

```csharp
GD.Print(ProjectSettings.GetSettingWithOverride("application/config/name"));

```

### GetSettingWithOverrideAndCustomFeatures

```csharp
public static Variant GetSettingWithOverrideAndCustomFeatures(StringName name, ReadOnlySpan<string> features)
```

引擎名：`get_setting_with_override_and_custom_features`

Similar to `GetSettingWithOverride()`, but applies feature tag overrides instead of current OS features.

### GetSettingWithOverrideAndCustomFeatures

```csharp
public static Variant GetSettingWithOverrideAndCustomFeatures(StringName name, string[] features)
```

引擎名：`get_setting_with_override_and_custom_features`

Similar to `GetSettingWithOverride()`, but applies feature tag overrides instead of current OS features.

### GlobalizePath

```csharp
public static string GlobalizePath(string path)
```

引擎名：`globalize_path`

Returns the absolute, native OS path corresponding to the localized `path` (starting with `res://` or `user://`). The returned path will vary depending on the operating system and user preferences. See [File paths in Godot projects]($DOCS_URL/tutorials/io/data_paths.html) to see what those paths convert to. See also `LocalizePath()`.

**Note:** `GlobalizePath()` with `res://` will not work in an exported project. Instead, prepend the executable's base directory to the path when running from an exported project:

```csharp
var path = ""
if OS.has_feature("editor"):
# Running from an editor binary.
# `path` will contain the absolute path to `hello.txt` located in the project root.
path = ProjectSettings.globalize_path("res://hello.txt")
else:
# Running from an exported project.
# `path` will contain the absolute path to `hello.txt` next to the executable.
# This is *not* identical to using `ProjectSettings.GlobalizePath` with a `res://` path,
# but is close enough in spirit.
path = OS.get_executable_path().get_base_dir().path_join("hello.txt")

```

### HasSetting

```csharp
public static bool HasSetting(string name)
```

引擎名：`has_setting`

Returns `true` if a configuration value is present.

**Note:** In order to be be detected, custom settings have to be either defined with `SetSetting()`, or exist in the `project.godot` file. This is especially relevant when using `SetInitialValue()`.

### LoadResourcePack

```csharp
public static bool LoadResourcePack(string pack, bool replaceFiles = true, int offset = 0)
```

引擎名：`load_resource_pack`

Loads the contents of the .pck or .zip file specified by `pack` into the resource filesystem (`res://`). Returns `true` on success.

**Note:** If a file from `pack` shares the same path as a file already in the resource filesystem, any attempts to load that file will use the file from `pack` unless `replaceFiles` is set to `false`.

**Note:** The optional `offset` parameter can be used to specify the offset in bytes to the start of the resource pack. This is only supported for .pck files.

**Note:** [`DirAccess`](DirAccess.md) will not show changes made to the contents of `res://` after calling this function.

### LocalizePath

```csharp
public static string LocalizePath(string path)
```

引擎名：`localize_path`

Returns the localized path (starting with `res://`) corresponding to the absolute, native OS `path`. See also `GlobalizePath()`.

### Save

```csharp
public static Error Save()
```

引擎名：`save`

Saves the configuration to the `project.godot` file.

**Note:** This method is intended to be used by editor plugins, as modified `ProjectSettings` can't be loaded back in the running app. If you want to change project settings in exported projects, use `SaveCustom()` to save an `override.cfg` file.

### SaveCustom

```csharp
public static Error SaveCustom(string file)
```

引擎名：`save_custom`

Saves the configuration to a custom file. The file extension must be `.godot` (to save in text-based [`ConfigFile`](ConfigFile.md) format) or `.binary` (to save in binary format). You can also save an `override.cfg` file, which is also text, but can be used in exported projects unlike other formats.

### SetAsBasic

```csharp
public static void SetAsBasic(string name, bool basic)
```

引擎名：`set_as_basic`

Defines if the specified setting is considered basic or advanced. Basic settings will always be shown in the project settings. Advanced settings will only be shown if the user enables the "Advanced Settings" option.

### SetAsInternal

```csharp
public static void SetAsInternal(string name, bool internal)
```

引擎名：`set_as_internal`

Defines if the specified setting is considered internal. An internal setting won't show up in the Project Settings dialog. This is mostly useful for addons that need to store their own internal settings without exposing them directly to the user.

### SetInitialValue

```csharp
public static void SetInitialValue(string name, Variant value)
```

引擎名：`set_initial_value`

Sets the specified setting's initial value. This is the value the setting reverts to. The setting should already exist before calling this method. Note that project settings equal to their default value are not saved, so your code needs to account for that.

```csharp
extends EditorPlugin

const SETTING_NAME = "addons/my_setting"
const SETTING_DEFAULT = 10.0

func _enter_tree():
if not ProjectSettings.has_setting(SETTING_NAME):
ProjectSettings.set_setting(SETTING_NAME, SETTING_DEFAULT)

ProjectSettings.set_initial_value(SETTING_NAME, SETTING_DEFAULT)

```

If you have a project setting defined by an `EditorPlugin`, but want to use it in a running project, you will need a similar code at runtime.

### SetOrder

```csharp
public static void SetOrder(string name, int position)
```

引擎名：`set_order`

Sets the order of a configuration value (influences when saved to the config file).

### SetRestartIfChanged

```csharp
public static void SetRestartIfChanged(string name, bool restart)
```

引擎名：`set_restart_if_changed`

Sets whether a setting requires restarting the editor to properly take effect.

**Note:** This is just a hint to display to the user that the editor must be restarted for changes to take effect. Enabling `SetRestartIfChanged()` does *not* delay the setting being set when changed.

### SetSetting

```csharp
public static void SetSetting(string name, Variant value)
```

引擎名：`set_setting`

Sets the value of a setting.

```csharp
ProjectSettings.SetSetting("application/config/name", "Example");

```

This can also be used to erase custom project settings. To do this change the setting value to `null`.

## 事件（信号）

### SettingsChanged

```csharp
public event Action SettingsChanged
```

引擎名：`settings_changed`

Emitted when any setting is changed, up to once per process frame.
