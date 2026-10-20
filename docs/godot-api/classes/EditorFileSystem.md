<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorFileSystem

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This object holds information of all resources in the filesystem, their types, etc.

**Note:** This class shouldn't be instantiated directly. Instead, access the singleton using [`EditorInterface.GetResourceFilesystem`](EditorInterface.md).

## 方法

### GetFileType

```csharp
public string GetFileType(string path)
```

引擎名：`get_file_type`

Returns the resource type of the file, given the full path. This returns a string such as `"Resource"` or `"GDScript"`, *not* a file extension such as `".gd"`.

### GetFilesystem

```csharp
public EditorFileSystemDirectory GetFilesystem()
```

引擎名：`get_filesystem`

Gets the root directory object.

### GetFilesystemPath

```csharp
public EditorFileSystemDirectory GetFilesystemPath(string path)
```

引擎名：`get_filesystem_path`

Returns a view into the filesystem at `path`.

### GetScanningProgress

```csharp
public float GetScanningProgress()
```

引擎名：`get_scanning_progress`

Returns the scan progress for 0 to 1 if the FS is being scanned.

### IsImporting

```csharp
public bool IsImporting()
```

引擎名：`is_importing`

Returns `true` if resources are currently being imported.

### IsScanning

```csharp
public bool IsScanning()
```

引擎名：`is_scanning`

Returns `true` if the filesystem is being scanned.

### ReimportFiles

```csharp
public void ReimportFiles(ReadOnlySpan<string> files)
```

引擎名：`reimport_files`

Reimports a set of files. Call this if these files or their `.import` files were directly edited by script or an external program.

If the file type changed or the file was newly created, use `UpdateFile()` or `Scan()`.

**Note:** This function blocks until the import is finished. However, the main loop iteration, including timers and [`Node._Process()`](Node.md), will occur during the import process due to progress bar updates. Avoid calls to `ReimportFiles()` or `Scan()` while an import is in progress.

### ReimportFiles

```csharp
public void ReimportFiles(string[] files)
```

引擎名：`reimport_files`

Reimports a set of files. Call this if these files or their `.import` files were directly edited by script or an external program.

If the file type changed or the file was newly created, use `UpdateFile()` or `Scan()`.

**Note:** This function blocks until the import is finished. However, the main loop iteration, including timers and [`Node._Process()`](Node.md), will occur during the import process due to progress bar updates. Avoid calls to `ReimportFiles()` or `Scan()` while an import is in progress.

### Scan

```csharp
public void Scan()
```

引擎名：`scan`

Scan the filesystem for changes.

### ScanSources

```csharp
public void ScanSources()
```

引擎名：`scan_sources`

Check if the source of any imported resource changed.

### UpdateFile

```csharp
public void UpdateFile(string path)
```

引擎名：`update_file`

Add a file in an existing directory, or schedule file information to be updated on editor restart. Can be used to update text files saved by an external program.

This will not import the file. To reimport, call `ReimportFiles()` or `Scan()` methods.

## 事件（信号）

### FilesystemChanged

```csharp
public event Action FilesystemChanged
```

引擎名：`filesystem_changed`

Emitted if the filesystem changed.

### ResourcesReimported

```csharp
public event EditorFileSystem.ResourcesReimportedEventHandler ResourcesReimported
```

引擎名：`resources_reimported`

Emitted if a resource is reimported.

### ResourcesReimporting

```csharp
public event EditorFileSystem.ResourcesReimportingEventHandler ResourcesReimporting
```

引擎名：`resources_reimporting`

Emitted before a resource is reimported.

### ResourcesReload

```csharp
public event EditorFileSystem.ResourcesReloadEventHandler ResourcesReload
```

引擎名：`resources_reload`

Emitted if at least one resource is reloaded when the filesystem is scanned.

### ScriptClassesUpdated

```csharp
public event Action ScriptClassesUpdated
```

引擎名：`script_classes_updated`

Emitted when the list of global script classes gets updated.

### SourcesChanged

```csharp
public event EditorFileSystem.SourcesChangedEventHandler SourcesChanged
```

引擎名：`sources_changed`

Emitted if the source of any imported file changed.

## ResourcesReimportedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorFileSystem.ResourcesReimported`](EditorFileSystem.md) event of a [`EditorFileSystem`](EditorFileSystem.md) class.

### 签名

#### Invoke

```csharp
delegate void ResourcesReimportedEventHandler(string[] resources)
```

## ResourcesReimportingEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorFileSystem.ResourcesReimporting`](EditorFileSystem.md) event of a [`EditorFileSystem`](EditorFileSystem.md) class.

### 签名

#### Invoke

```csharp
delegate void ResourcesReimportingEventHandler(string[] resources)
```

## ResourcesReloadEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorFileSystem.ResourcesReload`](EditorFileSystem.md) event of a [`EditorFileSystem`](EditorFileSystem.md) class.

### 签名

#### Invoke

```csharp
delegate void ResourcesReloadEventHandler(string[] resources)
```

## SourcesChangedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorFileSystem.SourcesChanged`](EditorFileSystem.md) event of a [`EditorFileSystem`](EditorFileSystem.md) class.

### 签名

#### Invoke

```csharp
delegate void SourcesChangedEventHandler(bool exist)
```
