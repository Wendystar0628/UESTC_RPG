<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GDExtension

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The `GDExtension` resource type represents a [shared library](https://en.wikipedia.org/wiki/Shared_library) which can expand the functionality of the engine. The [`GDExtensionManager`](GDExtensionManager.md) singleton is responsible for loading, reloading, and unloading `GDExtension` resources.

**Note:** GDExtension itself is not a scripting language and has no relation to [`GDScript`](GDScript.md) resources.

## 构造函数

### GDExtension

```csharp
public GDExtension()
```

## 方法

### CloseLibrary

```csharp
public void CloseLibrary()
```

### GetMinimumLibraryInitializationLevel

```csharp
public GDExtension.InitializationLevel GetMinimumLibraryInitializationLevel()
```

引擎名：`get_minimum_library_initialization_level`

Returns the lowest level required for this extension to be properly initialized (see the [`GDExtension.InitializationLevel`](GDExtension.md) enum).

### InitializeLibrary

```csharp
public void InitializeLibrary(GDExtension.InitializationLevel level)
```

### IsLibraryOpen

```csharp
public bool IsLibraryOpen()
```

引擎名：`is_library_open`

Returns `true` if this extension's library has been opened.

### OpenLibrary

```csharp
public Error OpenLibrary(string path, string entrySymbol)
```

## InitializationLevel

命名空间 `Godot`；enum

### 枚举值

- `Core = 0`：The library is initialized at the same time as the core features of the engine.
- `Servers = 1`：The library is initialized at the same time as the engine's servers (such as [`RenderingServer`](RenderingServer.md) or [`PhysicsServer3D`](PhysicsServer3D.md)).
- `Scene = 2`：The library is initialized at the same time as the engine's scene-related classes.
- `Editor = 3`：The library is initialized at the same time as the engine's editor classes.
