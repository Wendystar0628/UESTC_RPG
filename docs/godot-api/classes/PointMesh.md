<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PointMesh

命名空间 `Godot`；class；继承 [`PrimitiveMesh`](PrimitiveMesh.md) → [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A `PointMesh` is a primitive mesh composed of a single point. Instead of relying on triangles, points are rendered as a single rectangle on the screen with a constant size. They are intended to be used with particle systems, but can also be used as a cheap way to render billboarded sprites (for example in a point cloud).

In order to be displayed, point meshes must be used with a material that has a point size. The point size can be accessed in a shader with the `POINT_SIZE` built-in, or in a [`BaseMaterial3D`](BaseMaterial3D.md) by setting the [`BaseMaterial3D.UsePointSize`](BaseMaterial3D.md) and [`BaseMaterial3D.PointSize`](BaseMaterial3D.md) properties.

**Note:** When using point meshes, properties that normally affect vertices will be ignored, including [`BaseMaterial3D.BillboardMode`](BaseMaterial3D.md), [`BaseMaterial3D.Grow`](BaseMaterial3D.md), and [`BaseMaterial3D.CullMode`](BaseMaterial3D.md).

## 构造函数

### PointMesh

```csharp
public PointMesh()
```
