<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsServer3DManagerInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

[`PhysicsServer3DManager`](PhysicsServer3DManager.md) is the API for registering [`PhysicsServer3D`](PhysicsServer3D.md) implementations and for setting the default implementation.

**Note:** It is not possible to switch physics servers at runtime. This class is only used on startup at the server initialization level, by Godot itself and possibly by GDExtensions.

## 方法

### RegisterServer

```csharp
public void RegisterServer(string name, Callable createCallback)
```

Register a [`PhysicsServer3D`](PhysicsServer3D.md) implementation by passing a `name` and a [`Callable`](Callable.md) that returns a [`PhysicsServer3D`](PhysicsServer3D.md) object.

### SetDefaultServer

```csharp
public void SetDefaultServer(string name, int priority)
```

Set the default [`PhysicsServer3D`](PhysicsServer3D.md) implementation to the one identified by `name`, if `priority` is greater than the priority of the current default implementation.
