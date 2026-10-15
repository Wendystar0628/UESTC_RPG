<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRCamera3D

命名空间 `Godot`；class；继承 [`Camera3D`](Camera3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A camera node which automatically positions itself based on XR tracking data.

In contrast to [`XRController3D`](XRController3D.md), the render thread has access to more up-to-date tracking data, and the location of the `XRCamera3D` node can lag a few milliseconds behind what is used for rendering.

**Note:** If [`Viewport.UseXR`](Viewport.md) is `true`, most of the camera properties are overridden by the active [`XRInterface`](XRInterface.md). The only properties that can be trusted are the near and far planes.

## 构造函数

### XRCamera3D

```csharp
public XRCamera3D()
```
