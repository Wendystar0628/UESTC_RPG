<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorExportPlatform

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Base resource that provides the functionality of exporting a release build of a project to a platform, from the editor. Stores platform-specific metadata such as the name and supported features of the platform, and performs the exporting of projects, PCK files, and ZIP files. Uses an export template for the platform provided at the time of project exporting.

Used in scripting by [`EditorExportPlugin`](EditorExportPlugin.md) to configure platform-specific customization of scenes and resources. See [`EditorExportPlugin._BeginCustomizeScenes()`](EditorExportPlugin.md) and [`EditorExportPlugin._BeginCustomizeResources()`](EditorExportPlugin.md) for more details.

## 方法

### AddMessage

```csharp
public void AddMessage(EditorExportPlatform.ExportMessageType type, string category, string message)
```

引擎名：`add_message`

Adds a message to the export log that will be displayed when exporting ends.

### ClearMessages

```csharp
public void ClearMessages()
```

引擎名：`clear_messages`

Clears the export log.

### CreatePreset

```csharp
public EditorExportPreset CreatePreset()
```

引擎名：`create_preset`

Create a new preset for this platform.

### ExportPack

```csharp
public Error ExportPack(EditorExportPreset preset, bool debug, string path, EditorExportPlatform.DebugFlags flags = EditorExportPlatform.DebugFlags.)
```

引擎名：`export_pack`

Creates a PCK archive at `path` for the specified `preset`.

### ExportPackPatch

```csharp
public Error ExportPackPatch(EditorExportPreset preset, bool debug, string path, ReadOnlySpan<string> patches, EditorExportPlatform.DebugFlags flags)
```

引擎名：`export_pack_patch`

Creates a patch PCK archive at `path` for the specified `preset`, containing only the files that have changed since the last patch.

**Note:** `patches` is an optional override of the set of patches defined in the export preset. When empty the patches defined in the export preset will be used instead.

### ExportPackPatch

```csharp
public Error ExportPackPatch(EditorExportPreset preset, bool debug, string path, string[] patches = null, EditorExportPlatform.DebugFlags flags = EditorExportPlatform.DebugFlags.)
```

引擎名：`export_pack_patch`

Creates a patch PCK archive at `path` for the specified `preset`, containing only the files that have changed since the last patch.

**Note:** `patches` is an optional override of the set of patches defined in the export preset. When empty the patches defined in the export preset will be used instead.

参数：
- `patches`：If the parameter is null, then the default value is `Array.Empty<string>()`.

### ExportProject

```csharp
public Error ExportProject(EditorExportPreset preset, bool debug, string path, EditorExportPlatform.DebugFlags flags = EditorExportPlatform.DebugFlags., bool notify = true)
```

引擎名：`export_project`

Creates a full project at `path` for the specified `preset`. If `notify` is `true`, plugins using [`EditorExportPlugin._ExportBegin()`](EditorExportPlugin.md) will be called during the process.

### ExportProject

```csharp
public Error ExportProject(EditorExportPreset preset, bool debug, string path, EditorExportPlatform.DebugFlags flags)
```

引擎名：`export_project`

Creates a full project at `path` for the specified `preset`. If `notify` is `true`, plugins using [`EditorExportPlugin._ExportBegin()`](EditorExportPlugin.md) will be called during the process.

### ExportProjectFiles

```csharp
public Error ExportProjectFiles(EditorExportPreset preset, bool debug, Callable saveCb, Callable sharedCb = default)
```

引擎名：`export_project_files`

Exports project files for the specified preset. This method can be used to implement custom export format, other than PCK and ZIP. One of the callbacks is called for each exported file.

`saveCb` is called for all exported files and have the following arguments: `file_path: String`, `file_data: PackedByteArray`, `file_index: int`, `file_count: int`, `encryption_include_filters: PackedStringArray`, `encryption_exclude_filters: PackedStringArray`, `encryption_key: PackedByteArray`.

`sharedCb` is called for exported native shared/static libraries and have the following arguments: `file_path: String`, `tags: PackedStringArray`, `target_folder: String`.

**Note:** `file_index` and `file_count` are intended for progress tracking only and aren't necessarily unique and precise.

### ExportZip

```csharp
public Error ExportZip(EditorExportPreset preset, bool debug, string path, EditorExportPlatform.DebugFlags flags = EditorExportPlatform.DebugFlags.)
```

引擎名：`export_zip`

Create a ZIP archive at `path` for the specified `preset`.

### ExportZipPatch

```csharp
public Error ExportZipPatch(EditorExportPreset preset, bool debug, string path, ReadOnlySpan<string> patches, EditorExportPlatform.DebugFlags flags)
```

引擎名：`export_zip_patch`

Create a patch ZIP archive at `path` for the specified `preset`, containing only the files that have changed since the last patch.

**Note:** `patches` is an optional override of the set of patches defined in the export preset. When empty the patches defined in the export preset will be used instead.

### ExportZipPatch

```csharp
public Error ExportZipPatch(EditorExportPreset preset, bool debug, string path, string[] patches = null, EditorExportPlatform.DebugFlags flags = EditorExportPlatform.DebugFlags.)
```

引擎名：`export_zip_patch`

Create a patch ZIP archive at `path` for the specified `preset`, containing only the files that have changed since the last patch.

**Note:** `patches` is an optional override of the set of patches defined in the export preset. When empty the patches defined in the export preset will be used instead.

参数：
- `patches`：If the parameter is null, then the default value is `Array.Empty<string>()`.

### FindExportTemplate

```csharp
public Dictionary FindExportTemplate(string templateFileName)
```

引擎名：`find_export_template`

Locates export template for the platform, and returns [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys: `path: String` and `error: String`. This method is provided for convenience and custom export platforms aren't required to use it or keep export templates stored in the same way official templates are.

### GenExportFlags

```csharp
public string[] GenExportFlags(EditorExportPlatform.DebugFlags flags)
```

引擎名：`gen_export_flags`

Generates array of command line arguments for the default export templates for the debug flags and editor settings.

### GetCurrentPresets

```csharp
public Array GetCurrentPresets()
```

引擎名：`get_current_presets`

Returns array of [`EditorExportPreset`](EditorExportPreset.md)s for this platform.

### GetForcedExportFiles

```csharp
public static string[] GetForcedExportFiles()
```

引擎名：`get_forced_export_files`

Returns array of core file names that always should be exported regardless of preset config.

### GetForcedExportFiles

```csharp
public static string[] GetForcedExportFiles(EditorExportPreset preset = null)
```

引擎名：`get_forced_export_files`

Returns array of core file names that always should be exported regardless of preset config.

### GetInternalExportFiles

```csharp
public Dictionary GetInternalExportFiles(EditorExportPreset preset, bool debug)
```

引擎名：`get_internal_export_files`

Returns additional files that should always be exported regardless of preset configuration, and are not part of the project source. The returned [`Collections.Dictionary`](Collections.Dictionary.md) contains filename keys (`System.String`) and their corresponding raw data (`System.Byte`[]).

### GetMessageCategory

```csharp
public string GetMessageCategory(int index)
```

引擎名：`get_message_category`

Returns the message category for the message with the given `index`.

### GetMessageCount

```csharp
public int GetMessageCount()
```

引擎名：`get_message_count`

Returns the number of messages in the export log.

### GetMessageText

```csharp
public string GetMessageText(int index)
```

引擎名：`get_message_text`

Returns the text for the message with the given `index`.

### GetMessageType

```csharp
public EditorExportPlatform.ExportMessageType GetMessageType(int index)
```

引擎名：`get_message_type`

Returns the type for the message with the given `index`.

### GetOsName

```csharp
public string GetOsName()
```

引擎名：`get_os_name`

Returns the name of the export operating system handled by this `EditorExportPlatform` class, as a friendly string. Possible return values are `Windows`, `Linux`, `macOS`, `Android`, `iOS`, and `Web`.

### GetWorstMessageType

```csharp
public EditorExportPlatform.ExportMessageType GetWorstMessageType()
```

引擎名：`get_worst_message_type`

Returns most severe message type currently present in the export log.

### SavePack

```csharp
public Dictionary SavePack(EditorExportPreset preset, bool debug, string path, bool embed = false)
```

引擎名：`save_pack`

Saves PCK archive and returns [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys: `result: Error`, `so_files: Array` (array of the shared/static objects which contains dictionaries with the following keys: `path: String`, `tags: PackedStringArray`, and `target_folder: String`).

If `embed` is `true`, PCK content is appended to the end of `path` file and return [`Collections.Dictionary`](Collections.Dictionary.md) additionally include following keys: `embedded_start: int` (embedded PCK offset) and `embedded_size: int` (embedded PCK size).

### SavePackPatch

```csharp
public Dictionary SavePackPatch(EditorExportPreset preset, bool debug, string path)
```

引擎名：`save_pack_patch`

Saves patch PCK archive and returns [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys: `result: Error`, `so_files: Array` (array of the shared/static objects which contains dictionaries with the following keys: `path: String`, `tags: PackedStringArray`, and `target_folder: String`).

### SaveZip

```csharp
public Dictionary SaveZip(EditorExportPreset preset, bool debug, string path)
```

引擎名：`save_zip`

Saves ZIP archive and returns [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys: `result: Error`, `so_files: Array` (array of the shared/static objects which contains dictionaries with the following keys: `path: String`, `tags: PackedStringArray`, and `target_folder: String`).

### SaveZipPatch

```csharp
public Dictionary SaveZipPatch(EditorExportPreset preset, bool debug, string path)
```

引擎名：`save_zip_patch`

Saves patch ZIP archive and returns [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys: `result: Error`, `so_files: Array` (array of the shared/static objects which contains dictionaries with the following keys: `path: String`, `tags: PackedStringArray`, and `target_folder: String`).

### SshPushToRemote

```csharp
public Error SshPushToRemote(string host, string port, ReadOnlySpan<string> scpArgs, string srcFile, string dstFile)
```

引擎名：`ssh_push_to_remote`

Uploads specified file over SCP protocol to the remote host.

### SshPushToRemote

```csharp
public Error SshPushToRemote(string host, string port, string[] scpArgs, string srcFile, string dstFile)
```

引擎名：`ssh_push_to_remote`

Uploads specified file over SCP protocol to the remote host.

### SshRunOnRemote

```csharp
public Error SshRunOnRemote(string host, string port, ReadOnlySpan<string> sshArg, string cmdArgs, Array output, int portFwd)
```

引擎名：`ssh_run_on_remote`

Executes specified command on the remote host via SSH protocol and returns command output in the `output`.

### SshRunOnRemote

```csharp
public Error SshRunOnRemote(string host, string port, string[] sshArg, string cmdArgs, Array output = null, int portFwd = -1)
```

引擎名：`ssh_run_on_remote`

Executes specified command on the remote host via SSH protocol and returns command output in the `output`.

### SshRunOnRemoteNoWait

```csharp
public long SshRunOnRemoteNoWait(string host, string port, ReadOnlySpan<string> sshArgs, string cmdArgs, int portFwd)
```

引擎名：`ssh_run_on_remote_no_wait`

Executes specified command on the remote host via SSH protocol and returns process ID (on the remote host) without waiting for command to finish.

### SshRunOnRemoteNoWait

```csharp
public long SshRunOnRemoteNoWait(string host, string port, string[] sshArgs, string cmdArgs, int portFwd = -1)
```

引擎名：`ssh_run_on_remote_no_wait`

Executes specified command on the remote host via SSH protocol and returns process ID (on the remote host) without waiting for command to finish.

## DebugFlags

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `DumbClient = 1`：Flag is set if the remotely debugged project is expected to use the remote file system.
- `RemoteDebug = 2`：Flag is set if remote debug is enabled.
- `RemoteDebugLocalhost = 4`：Flag is set if remotely debugged project is running on the localhost.
- `ViewCollisions = 8`：Flag is set if the "Visible Collision Shapes" remote debug option is enabled.
- `ViewNavigation = 16`：Flag is set if the "Visible Navigation" remote debug option is enabled.

## ExportMessageType

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `None = 0`：Invalid message type used as the default value when no type is specified.
- `Info = 1`：Message type for informational messages that have no effect on the export.
- `Warning = 2`：Message type for warning messages that should be addressed but still allow to complete the export.
- `Error = 3`：Message type for error messages that must be addressed and fail the export.
