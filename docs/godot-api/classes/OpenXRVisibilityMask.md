<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRVisibilityMask

命名空间 `Godot`；class；继承 [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

The visibility mask allows us to black out the part of the render result that is invisible due to lens distortion.

As this is rendered first, it prevents fragments with expensive lighting calculations to be processed as they are discarded through z-checking.

## 构造函数

### OpenXRVisibilityMask

```csharp
public OpenXRVisibilityMask()
```
