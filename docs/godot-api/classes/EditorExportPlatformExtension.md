<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorExportPlatformExtension

命名空间 `Godot`；class；继承 [`EditorExportPlatform`](EditorExportPlatform.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

External [`EditorExportPlatform`](EditorExportPlatform.md) implementations should inherit from this class.

To use [`EditorExportPlatform`](EditorExportPlatform.md), register it using the [`EditorPlugin.AddExportPlatform`](EditorPlugin.md) method first.

## 构造函数

### EditorExportPlatformExtension

```csharp
public EditorExportPlatformExtension()
```

## 方法

### GetConfigError

```csharp
public string GetConfigError()
```

引擎名：`get_config_error`

Returns current configuration error message text. This method should be called only from the `_CanExport()`, `_HasValidExportConfiguration()`, or `_HasValidProjectConfiguration()` implementations.

### GetConfigMissingTemplates

```csharp
public bool GetConfigMissingTemplates()
```

引擎名：`get_config_missing_templates`

Returns `true` is export templates are missing from the current configuration. This method should be called only from the `_CanExport()`, `_HasValidExportConfiguration()`, or `_HasValidProjectConfiguration()` implementations.

### SetConfigError

```csharp
public void SetConfigError(string errorText)
```

引擎名：`set_config_error`

Sets current configuration error message text. This method should be called only from the `_CanExport()`, `_HasValidExportConfiguration()`, or `_HasValidProjectConfiguration()` implementations.

### SetConfigMissingTemplates

```csharp
public void SetConfigMissingTemplates(bool missingTemplates)
```

引擎名：`set_config_missing_templates`

Set to `true` is export templates are missing from the current configuration. This method should be called only from the `_CanExport()`, `_HasValidExportConfiguration()`, or `_HasValidProjectConfiguration()` implementations.

### _CanExport

```csharp
public virtual bool _CanExport(EditorExportPreset preset, bool debug)
```

引擎名：`_can_export`

Returns `true` if the specified `preset` is valid and can be exported. Use `SetConfigError()` and `SetConfigMissingTemplates()` to set error details.

Usual implementations call `_HasValidExportConfiguration()` and `_HasValidProjectConfiguration()` to determine if exporting is possible.

### _Cleanup

```csharp
public virtual void _Cleanup()
```

引擎名：`_cleanup`

Called by the editor before platform is unregistered.

### _ExportPack

```csharp
public virtual Error _ExportPack(EditorExportPreset preset, bool debug, string path, EditorExportPlatform.DebugFlags flags)
```

引擎名：`_export_pack`

Creates a PCK archive at `path` for the specified `preset`.

This method is called when "Export PCK/ZIP" button is pressed in the export dialog, with "Export as Patch" disabled, and PCK is selected as a file type.

### _ExportPackPatch

```csharp
public virtual Error _ExportPackPatch(EditorExportPreset preset, bool debug, string path, string[] patches, EditorExportPlatform.DebugFlags flags)
```

引擎名：`_export_pack_patch`

Creates a patch PCK archive at `path` for the specified `preset`, containing only the files that have changed since the last patch.

This method is called when "Export PCK/ZIP" button is pressed in the export dialog, with "Export as Patch" enabled, and PCK is selected as a file type.

**Note:** The patches provided in `patches` have already been loaded when this method is called and are merely provided as context. When empty the patches defined in the export preset have been loaded instead.

### _ExportProject

```csharp
public virtual Error _ExportProject(EditorExportPreset preset, bool debug, string path, EditorExportPlatform.DebugFlags flags)
```

引擎名：`_export_project`

Creates a full project at `path` for the specified `preset`.

This method is called when "Export" button is pressed in the export dialog.

This method implementation can call [`EditorExportPlatform.SavePack`](EditorExportPlatform.md) or [`EditorExportPlatform.SaveZip`](EditorExportPlatform.md) to use default PCK/ZIP export process, or calls [`EditorExportPlatform.ExportProjectFiles`](EditorExportPlatform.md) and implement custom callback for processing each exported file.

### _ExportZip

```csharp
public virtual Error _ExportZip(EditorExportPreset preset, bool debug, string path, EditorExportPlatform.DebugFlags flags)
```

引擎名：`_export_zip`

Create a ZIP archive at `path` for the specified `preset`.

This method is called when "Export PCK/ZIP" button is pressed in the export dialog, with "Export as Patch" disabled, and ZIP is selected as a file type.

### _ExportZipPatch

```csharp
public virtual Error _ExportZipPatch(EditorExportPreset preset, bool debug, string path, string[] patches, EditorExportPlatform.DebugFlags flags)
```

引擎名：`_export_zip_patch`

Create a ZIP archive at `path` for the specified `preset`, containing only the files that have changed since the last patch.

This method is called when "Export PCK/ZIP" button is pressed in the export dialog, with "Export as Patch" enabled, and ZIP is selected as a file type.

**Note:** The patches provided in `patches` have already been loaded when this method is called and are merely provided as context. When empty the patches defined in the export preset have been loaded instead.

### _GetBinaryExtensions

```csharp
public virtual string[] _GetBinaryExtensions(EditorExportPreset preset)
```

引擎名：`_get_binary_extensions`

Returns array of supported binary extensions for the full project export.

### _GetDebugProtocol

```csharp
public virtual string _GetDebugProtocol()
```

引擎名：`_get_debug_protocol`

Returns protocol used for remote debugging. Default implementation return `tcp://`.

### _GetDeviceArchitecture

```csharp
public virtual string _GetDeviceArchitecture(int device)
```

引擎名：`_get_device_architecture`

Returns device architecture for one-click deploy.

### _GetExportOptionVisibility

```csharp
public virtual bool _GetExportOptionVisibility(EditorExportPreset preset, string option)
```

引擎名：`_get_export_option_visibility`

Validates `option` and returns visibility for the specified `preset`. Default implementation return `true` for all options.

### _GetExportOptionWarning

```csharp
public virtual string _GetExportOptionWarning(EditorExportPreset preset, StringName option)
```

引擎名：`_get_export_option_warning`

Validates `option` and returns warning message for the specified `preset`. Default implementation return empty string for all options.

### _GetExportOptions

```csharp
public virtual Array<Dictionary> _GetExportOptions()
```

引擎名：`_get_export_options`

Returns a property list, as an [`Collections.Array`](Collections.Array.md) of dictionaries. Each [`Collections.Dictionary`](Collections.Dictionary.md) must at least contain the `name: StringName` and `type: Variant.Type` entries.

Additionally, the following keys are supported:

- `hint: PropertyHint`

- `hint_string: String`

- `usage: PropertyUsageFlags`

- `class_name: StringName`

- `default_value: Variant`, default value of the property.

- `update_visibility: bool`, if set to `true`, `_GetExportOptionVisibility()` is called for each property when this property is changed.

- `required: bool`, if set to `true`, this property warnings are critical, and should be resolved to make export possible. This value is a hint for the `_HasValidExportConfiguration()` implementation, and not used by the engine directly.

See also [`GodotObject.GetPropertyList`](GodotObject.md).

### _GetLogo

```csharp
public virtual Texture2D _GetLogo()
```

引擎名：`_get_logo`

Returns the platform logo displayed in the export dialog. The logo should be 32×32 pixels, adjusted for the current editor scale (see [`EditorInterface.GetEditorScale`](EditorInterface.md)).

### _GetName

```csharp
public virtual string _GetName()
```

引擎名：`_get_name`

Returns export platform name.

### _GetOptionIcon

```csharp
public virtual Texture2D _GetOptionIcon(int device)
```

引擎名：`_get_option_icon`

Returns the item icon for the specified `device` in the one-click deploy menu. The icon should be 16×16 pixels, adjusted for the current editor scale (see [`EditorInterface.GetEditorScale`](EditorInterface.md)).

### _GetOptionLabel

```csharp
public virtual string _GetOptionLabel(int device)
```

引擎名：`_get_option_label`

Returns one-click deploy menu item label for the specified `device`.

### _GetOptionTooltip

```csharp
public virtual string _GetOptionTooltip(int device)
```

引擎名：`_get_option_tooltip`

Returns one-click deploy menu item tooltip for the specified `device`.

### _GetOptionsCount

```csharp
public virtual int _GetOptionsCount()
```

引擎名：`_get_options_count`

Returns the number of devices (or other options) available in the one-click deploy menu.

### _GetOptionsTooltip

```csharp
public virtual string _GetOptionsTooltip()
```

引擎名：`_get_options_tooltip`

Returns tooltip of the one-click deploy menu button.

### _GetOsName

```csharp
public virtual string _GetOsName()
```

引擎名：`_get_os_name`

Returns target OS name.

### _GetPlatformFeatures

```csharp
public virtual string[] _GetPlatformFeatures()
```

引擎名：`_get_platform_features`

Returns array of platform specific features.

### _GetPresetFeatures

```csharp
public virtual string[] _GetPresetFeatures(EditorExportPreset preset)
```

引擎名：`_get_preset_features`

Returns array of platform specific features for the specified `preset`.

### _GetRunIcon

```csharp
public virtual Texture2D _GetRunIcon()
```

引擎名：`_get_run_icon`

Returns the icon of the one-click deploy menu button. The icon should be 16×16 pixels, adjusted for the current editor scale (see [`EditorInterface.GetEditorScale`](EditorInterface.md)).

### _HasValidExportConfiguration

```csharp
public virtual bool _HasValidExportConfiguration(EditorExportPreset preset, bool debug)
```

引擎名：`_has_valid_export_configuration`

Returns `true` if export configuration is valid.

### _HasValidProjectConfiguration

```csharp
public virtual bool _HasValidProjectConfiguration(EditorExportPreset preset)
```

引擎名：`_has_valid_project_configuration`

Returns `true` if project configuration is valid.

### _Initialize

```csharp
public virtual void _Initialize()
```

引擎名：`_initialize`

Initializes the plugin. Called by the editor when platform is registered.

### _IsExecutable

```csharp
public virtual bool _IsExecutable(string path)
```

引擎名：`_is_executable`

Returns `true` if specified file is a valid executable (native executable or script) for the target platform.

### _PollExport

```csharp
public virtual bool _PollExport()
```

引擎名：`_poll_export`

Returns `true` if one-click deploy options are changed and editor interface should be updated.

### _Run

```csharp
public virtual Error _Run(EditorExportPreset preset, int device, EditorExportPlatform.DebugFlags debugFlags)
```

引擎名：`_run`

This method is called when `device` one-click deploy menu option is selected.

Implementation should export project to a temporary location, upload and run it on the specific `device`, or perform another action associated with the menu item.

### _ShouldUpdateExportOptions

```csharp
public virtual bool _ShouldUpdateExportOptions()
```

引擎名：`_should_update_export_options`

Returns `true` if export options list is changed and presets should be updated.
