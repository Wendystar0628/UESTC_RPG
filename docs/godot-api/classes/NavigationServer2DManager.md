<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationServer2DManager

命名空间 `Godot`；static class

`NavigationServer2DManager` is the API for registering [`NavigationServer2D`](NavigationServer2D.md) implementations and setting the default implementation.

**Note:** It is not possible to switch servers at runtime. This class is only used on startup at the server initialization level.

## 属性

### Singleton

```csharp
public static NavigationServer2DManagerInstance Singleton { get; }
```

## 方法

### RegisterServer

```csharp
public static void RegisterServer(string name, Callable createCallback)
```

引擎名：`register_server`

Registers a [`NavigationServer2D`](NavigationServer2D.md) implementation by passing a `name` and a [`Callable`](Callable.md) that returns a [`NavigationServer2D`](NavigationServer2D.md) object.

### SetDefaultServer

```csharp
public static void SetDefaultServer(string name, int priority)
```

引擎名：`set_default_server`

Sets the default [`NavigationServer2D`](NavigationServer2D.md) implementation to the one identified by `name`, if `priority` is greater than the priority of the current default implementation.
