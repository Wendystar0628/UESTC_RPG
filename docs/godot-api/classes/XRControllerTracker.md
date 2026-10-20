<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRControllerTracker

命名空间 `Godot`；class；继承 [`XRPositionalTracker`](XRPositionalTracker.md) → [`XRTracker`](XRTracker.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An instance of this object represents a controller that is tracked.

As controllers are turned on and the [`XRInterface`](XRInterface.md) detects them, instances of this object are automatically added to this list of active tracking objects accessible through the [`XRServer`](XRServer.md).

The [`XRController3D`](XRController3D.md) consumes objects of this type and should be used in your project.

## 构造函数

### XRControllerTracker

```csharp
public XRControllerTracker()
```
