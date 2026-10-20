<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorExportPreset

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Represents the configuration of an export preset, as created by the editor's export dialog. An `EditorExportPreset` instance is intended to be used a read-only configuration passed to the [`EditorExportPlatform`](EditorExportPlatform.md) methods when exporting the project.

## 方法

### AreAdvancedOptionsEnabled

```csharp
public bool AreAdvancedOptionsEnabled()
```

引擎名：`are_advanced_options_enabled`

Returns `true` if the "Advanced" toggle is enabled in the export dialog.

### GetCustomFeatures

```csharp
public string GetCustomFeatures()
```

引擎名：`get_custom_features`

Returns a comma-separated list of custom features added to this preset, as a string. See [Feature tags]($DOCS_URL/tutorials/export/feature_tags.html) in the documentation for more information.

### GetCustomizedFiles

```csharp
public Dictionary GetCustomizedFiles()
```

引擎名：`get_customized_files`

Returns a dictionary of files selected in the "Resources" tab of the export dialog. The dictionary's keys are file paths, and its values are the corresponding export modes: `"strip"`, `"keep"`, or `"remove"`. See also `GetFileExportMode()`.

### GetCustomizedFilesCount

```csharp
public int GetCustomizedFilesCount()
```

引擎名：`get_customized_files_count`

Returns the number of files selected in the "Resources" tab of the export dialog.

### GetEncryptDirectory

```csharp
public bool GetEncryptDirectory()
```

引擎名：`get_encrypt_directory`

Returns `true` if PCK directory encryption is enabled in the export dialog.

### GetEncryptPck

```csharp
public bool GetEncryptPck()
```

引擎名：`get_encrypt_pck`

Returns `true` if PCK encryption is enabled in the export dialog.

### GetEncryptionExFilter

```csharp
public string GetEncryptionExFilter()
```

引擎名：`get_encryption_ex_filter`

Returns file filters to exclude during PCK encryption.

### GetEncryptionInFilter

```csharp
public string GetEncryptionInFilter()
```

引擎名：`get_encryption_in_filter`

Returns file filters to include during PCK encryption.

### GetEncryptionKey

```csharp
public string GetEncryptionKey()
```

引擎名：`get_encryption_key`

Returns PCK encryption key.

### GetExcludeFilter

```csharp
public string GetExcludeFilter()
```

引擎名：`get_exclude_filter`

Returns file filters to exclude during export.

### GetExportFilter

```csharp
public EditorExportPreset.ExportFilter GetExportFilter()
```

引擎名：`get_export_filter`

Returns export file filter mode selected in the "Resources" tab of the export dialog.

### GetExportPath

```csharp
public string GetExportPath()
```

引擎名：`get_export_path`

Returns export target path.

### GetFileExportMode

```csharp
public EditorExportPreset.FileExportMode GetFileExportMode(string path, EditorExportPreset.FileExportMode default = EditorExportPreset.FileExportMode.NotCustomized)
```

引擎名：`get_file_export_mode`

Returns file export mode for the specified file.

### GetFilesToExport

```csharp
public string[] GetFilesToExport()
```

引擎名：`get_files_to_export`

Returns array of files to export.

### GetIncludeFilter

```csharp
public string GetIncludeFilter()
```

引擎名：`get_include_filter`

Returns file filters to include during export.

### GetOrEnv

```csharp
public Variant GetOrEnv(StringName name, string envVar)
```

引擎名：`get_or_env`

Returns export option value or value of environment variable if it is set.

### GetPatches

```csharp
public string[] GetPatches()
```

引擎名：`get_patches`

Returns the list of packs on which to base a patch export on.

### GetPresetName

```csharp
public string GetPresetName()
```

引擎名：`get_preset_name`

Returns this export preset's name.

### GetProjectSetting

```csharp
public Variant GetProjectSetting(StringName name)
```

引擎名：`get_project_setting`

Returns the value of the setting identified by `name` using export preset feature tag overrides instead of current OS features.

### GetScriptExportMode

```csharp
public EditorExportPreset.ScriptExportMode GetScriptExportMode()
```

引擎名：`get_script_export_mode`

Returns the export mode used by GDScript files. `0` for "Text", `1` for "Binary tokens", and `2` for "Compressed binary tokens (smaller files)".

### GetVersion

```csharp
public string GetVersion(StringName name, bool windowsVersion)
```

引擎名：`get_version`

Returns the preset's version number, or fall back to the `ProjectSettings.application/config/version` project setting if set to an empty string.

If `windowsVersion` is `true`, formats the returned version number to be compatible with Windows executable metadata.

### Has

```csharp
public bool Has(StringName property)
```

引擎名：`has`

Returns `true` if the preset has the property named `property`.

### HasExportFile

```csharp
public bool HasExportFile(string path)
```

引擎名：`has_export_file`

Returns `true` if the file at the specified `path` will be exported.

### IsDedicatedServer

```csharp
public bool IsDedicatedServer()
```

引擎名：`is_dedicated_server`

Returns `true` if the dedicated server export mode is selected in the export dialog.

### IsRunnable

```csharp
public bool IsRunnable()
```

引擎名：`is_runnable`

Returns `true` if the "Runnable" toggle is enabled in the export dialog.

## ExportFilter

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `ExportAllResources = 0`
- `ExportSelectedScenes = 1`
- `ExportSelectedResources = 2`
- `ExcludeSelectedResources = 3`
- `ExportCustomized = 4`

## FileExportMode

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `NotCustomized = 0`
- `Strip = 1`
- `Keep = 2`
- `Remove = 3`

## ScriptExportMode

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `Text = 0`
- `BinaryTokens = 1`
- `BinaryTokensCompressed = 2`
