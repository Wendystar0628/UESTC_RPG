<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GDExtensionManager

命名空间 `Godot`；static class

The GDExtensionManager loads, initializes, and keeps track of all available [`GDExtension`](GDExtension.md) libraries in the project.

**Note:** Do not worry about GDExtension unless you know what you are doing.

## 属性

### Singleton

```csharp
public static GDExtensionManagerInstance Singleton { get; }
```

## 方法

### GetExtension

```csharp
public static GDExtension GetExtension(string path)
```

引擎名：`get_extension`

Returns the [`GDExtension`](GDExtension.md) at the given file `path`, or `null` if it has not been loaded or does not exist.

### GetLoadedExtensions

```csharp
public static string[] GetLoadedExtensions()
```

引擎名：`get_loaded_extensions`

Returns the file paths of all currently loaded extensions.

### IsExtensionLoaded

```csharp
public static bool IsExtensionLoaded(string path)
```

引擎名：`is_extension_loaded`

Returns `true` if the extension at the given file `path` has already been loaded successfully. See also `GetLoadedExtensions()`.

### LoadExtension

```csharp
public static GDExtensionManager.LoadStatus LoadExtension(string path)
```

引擎名：`load_extension`

Loads an extension by absolute file path. The `path` needs to point to a valid [`GDExtension`](GDExtension.md). Returns [`GDExtensionManager.LoadStatus.Ok`](GDExtensionManager.md) if successful.

### ReloadExtension

```csharp
public static GDExtensionManager.LoadStatus ReloadExtension(string path)
```

引擎名：`reload_extension`

Reloads the extension at the given file path. The `path` needs to point to a valid [`GDExtension`](GDExtension.md), otherwise this method may return either [`GDExtensionManager.LoadStatus.NotLoaded`](GDExtensionManager.md) or [`GDExtensionManager.LoadStatus.Failed`](GDExtensionManager.md).

**Note:** You can only reload extensions in the editor. In release builds, this method always fails and returns [`GDExtensionManager.LoadStatus.Failed`](GDExtensionManager.md).

### UnloadExtension

```csharp
public static GDExtensionManager.LoadStatus UnloadExtension(string path)
```

引擎名：`unload_extension`

Unloads an extension by file path. The `path` needs to point to an already loaded [`GDExtension`](GDExtension.md), otherwise this method returns [`GDExtensionManager.LoadStatus.NotLoaded`](GDExtensionManager.md).

## 事件（信号）

### ExtensionLoaded

```csharp
public event GDExtensionManager.ExtensionLoadedEventHandler ExtensionLoaded
```

引擎名：`extension_loaded`

Emitted after the editor has finished loading a new extension.

**Note:** This signal is only emitted in editor builds.

### ExtensionUnloading

```csharp
public event GDExtensionManager.ExtensionUnloadingEventHandler ExtensionUnloading
```

引擎名：`extension_unloading`

Emitted before the editor starts unloading an extension.

**Note:** This signal is only emitted in editor builds.

### ExtensionsReloaded

```csharp
public event Action ExtensionsReloaded
```

引擎名：`extensions_reloaded`

Emitted after the editor has finished reloading one or more extensions.

## ExtensionLoadedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GDExtensionManager.ExtensionLoaded`](GDExtensionManager.md) event of a [`GDExtensionManager`](GDExtensionManager.md) class.

### 签名

#### Invoke

```csharp
delegate void ExtensionLoadedEventHandler(GDExtension extension)
```

## ExtensionUnloadingEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GDExtensionManager.ExtensionUnloading`](GDExtensionManager.md) event of a [`GDExtensionManager`](GDExtensionManager.md) class.

### 签名

#### Invoke

```csharp
delegate void ExtensionUnloadingEventHandler(GDExtension extension)
```

## LoadStatus

命名空间 `Godot`；enum

### 枚举值

- `Ok = 0`：The extension has loaded successfully.
- `Failed = 1`：The extension has failed to load, possibly because it does not exist or has missing dependencies.
- `AlreadyLoaded = 2`：The extension has already been loaded.
- `NotLoaded = 3`：The extension has not been loaded.
- `NeedsRestart = 4`：The extension requires the application to restart to fully load.
