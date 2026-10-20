<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationServer3DManagerInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

[`NavigationServer3DManager`](NavigationServer3DManager.md) is the API for registering [`NavigationServer3D`](NavigationServer3D.md) implementations and setting the default implementation.

**Note:** It is not possible to switch servers at runtime. This class is only used on startup at the server initialization level.

## 方法

### RegisterServer

```csharp
public void RegisterServer(string name, Callable createCallback)
```

Registers a [`NavigationServer3D`](NavigationServer3D.md) implementation by passing a `name` and a [`Callable`](Callable.md) that returns a [`NavigationServer3D`](NavigationServer3D.md) object.

### SetDefaultServer

```csharp
public void SetDefaultServer(string name, int priority)
```

Sets the default [`NavigationServer3D`](NavigationServer3D.md) implementation to the one identified by `name`, if `priority` is greater than the priority of the current default implementation.
