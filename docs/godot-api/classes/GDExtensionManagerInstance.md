<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GDExtensionManagerInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

The GDExtensionManager loads, initializes, and keeps track of all available [`GDExtension`](GDExtension.md) libraries in the project.

**Note:** Do not worry about GDExtension unless you know what you are doing.

## 方法

### GetExtension

```csharp
public GDExtension GetExtension(string path)
```

Returns the [`GDExtension`](GDExtension.md) at the given file `path`, or `null` if it has not been loaded or does not exist.

### GetLoadedExtensions

```csharp
public string[] GetLoadedExtensions()
```

Returns the file paths of all currently loaded extensions.

### IsExtensionLoaded

```csharp
public bool IsExtensionLoaded(string path)
```

Returns `true` if the extension at the given file `path` has already been loaded successfully. See also `GetLoadedExtensions()`.

### LoadExtension

```csharp
public GDExtensionManager.LoadStatus LoadExtension(string path)
```

Loads an extension by absolute file path. The `path` needs to point to a valid [`GDExtension`](GDExtension.md). Returns [`GDExtensionManager.LoadStatus.Ok`](GDExtensionManager.md) if successful.

### ReloadExtension

```csharp
public GDExtensionManager.LoadStatus ReloadExtension(string path)
```

Reloads the extension at the given file path. The `path` needs to point to a valid [`GDExtension`](GDExtension.md), otherwise this method may return either [`GDExtensionManager.LoadStatus.NotLoaded`](GDExtensionManager.md) or [`GDExtensionManager.LoadStatus.Failed`](GDExtensionManager.md).

**Note:** You can only reload extensions in the editor. In release builds, this method always fails and returns [`GDExtensionManager.LoadStatus.Failed`](GDExtensionManager.md).

### UnloadExtension

```csharp
public GDExtensionManager.LoadStatus UnloadExtension(string path)
```

Unloads an extension by file path. The `path` needs to point to an already loaded [`GDExtension`](GDExtension.md), otherwise this method returns [`GDExtensionManager.LoadStatus.NotLoaded`](GDExtensionManager.md).

## 事件（信号）

### ExtensionLoaded

```csharp
public event GDExtensionManagerInstance.ExtensionLoadedEventHandler ExtensionLoaded
```

Emitted after the editor has finished loading a new extension.

**Note:** This signal is only emitted in editor builds.

### ExtensionUnloading

```csharp
public event GDExtensionManagerInstance.ExtensionUnloadingEventHandler ExtensionUnloading
```

Emitted before the editor starts unloading an extension.

**Note:** This signal is only emitted in editor builds.

### ExtensionsReloaded

```csharp
public event Action ExtensionsReloaded
```

Emitted after the editor has finished reloading one or more extensions.

## ExtensionLoadedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GDExtensionManagerInstance.ExtensionLoaded`](GDExtensionManagerInstance.md) event of a [`GDExtensionManagerInstance`](GDExtensionManagerInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void ExtensionLoadedEventHandler(GDExtension extension)
```

## ExtensionUnloadingEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GDExtensionManagerInstance.ExtensionUnloading`](GDExtensionManagerInstance.md) event of a [`GDExtensionManagerInstance`](GDExtensionManagerInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void ExtensionUnloadingEventHandler(GDExtension extension)
```
