<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CsgCombiner3D

命名空间 `Godot`；class；继承 [`CsgShape3D`](CsgShape3D.md) → [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

For complex arrangements of shapes, it is sometimes needed to add structure to your CSG nodes. The CSGCombiner3D node allows you to create this structure. The node encapsulates the result of the CSG operations of its children. In this way, it is possible to do operations on one set of shapes that are children of one CSGCombiner3D node, and a set of separate operations on a second set of shapes that are children of a second CSGCombiner3D node, and then do an operation that takes the two end results as its input to create the final shape.

**Note:** CSG nodes are intended to be used for level prototyping. Creating CSG nodes has a significant CPU cost compared to creating a [`MeshInstance3D`](MeshInstance3D.md) with a [`PrimitiveMesh`](PrimitiveMesh.md). Moving a CSG node within another CSG node also has a significant CPU cost, so it should be avoided during gameplay.

## 构造函数

### CsgCombiner3D

```csharp
public CsgCombiner3D()
```
