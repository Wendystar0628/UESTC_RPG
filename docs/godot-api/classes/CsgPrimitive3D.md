<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CsgPrimitive3D

命名空间 `Godot`；class；继承 [`CsgShape3D`](CsgShape3D.md) → [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Parent class for various CSG primitives. It contains code and functionality that is common between them. It cannot be used directly. Instead use one of the various classes that inherit from it.

**Note:** CSG nodes are intended to be used for level prototyping. Creating CSG nodes has a significant CPU cost compared to creating a [`MeshInstance3D`](MeshInstance3D.md) with a [`PrimitiveMesh`](PrimitiveMesh.md). Moving a CSG node within another CSG node also has a significant CPU cost, so it should be avoided during gameplay.

## 属性

### FlipFaces

```csharp
public bool FlipFaces { get; set; }
```

If set, the order of the vertices in each triangle are reversed resulting in the backside of the mesh being drawn.

## 方法

### GetFlipFaces

```csharp
public bool GetFlipFaces()
```

### SetFlipFaces

```csharp
public void SetFlipFaces(bool flipFaces)
```
