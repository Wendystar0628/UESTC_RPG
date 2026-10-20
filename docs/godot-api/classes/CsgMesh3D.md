<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CsgMesh3D

命名空间 `Godot`；class；继承 [`CsgPrimitive3D`](CsgPrimitive3D.md) → [`CsgShape3D`](CsgShape3D.md) → [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This CSG node allows you to use any mesh resource as a CSG shape, provided it is *manifold*. A manifold shape is closed, does not self-intersect, does not contain internal faces and has no edges that connect to more than two faces. See also [`CsgPolygon3D`](CsgPolygon3D.md) for drawing 2D extruded polygons to be used as CSG nodes.

**Note:** CSG nodes are intended to be used for level prototyping. Creating CSG nodes has a significant CPU cost compared to creating a [`MeshInstance3D`](MeshInstance3D.md) with a [`PrimitiveMesh`](PrimitiveMesh.md). Moving a CSG node within another CSG node also has a significant CPU cost, so it should be avoided during gameplay.

## 构造函数

### CsgMesh3D

```csharp
public CsgMesh3D()
```

## 属性

### Material

```csharp
public Material Material { get; set; }
```

The [`Material`](Material.md) used in drawing the CSG shape.

### Mesh

```csharp
public Mesh Mesh { get; set; }
```

The [`Mesh`](Mesh.md) resource to use as a CSG shape.

**Note:** Some [`Mesh`](Mesh.md) types such as [`PlaneMesh`](PlaneMesh.md), [`PointMesh`](PointMesh.md), [`QuadMesh`](QuadMesh.md), and [`RibbonTrailMesh`](RibbonTrailMesh.md) are excluded from the type hint for this property, as these primitives are non-*manifold* and thus not compatible with the CSG algorithm.

**Note:** When using an [`ArrayMesh`](ArrayMesh.md), all vertex attributes except [`Mesh.ArrayType.Vertex`](Mesh.md), [`Mesh.ArrayType.Normal`](Mesh.md) and [`Mesh.ArrayType.TexUV`](Mesh.md) are left unused. Only [`Mesh.ArrayType.Vertex`](Mesh.md) and [`Mesh.ArrayType.TexUV`](Mesh.md) will be passed to the GPU.

[`Mesh.ArrayType.Normal`](Mesh.md) is only used to determine which faces require the use of flat shading. By default, CSGMesh will ignore the mesh's vertex normals, recalculate them for each vertex and use a smooth shader. If a flat shader is required for a face, ensure that all vertex normals of the face are approximately equal.

## 方法

### GetMaterial

```csharp
public Material GetMaterial()
```

### GetMesh

```csharp
public Mesh GetMesh()
```

### SetMaterial

```csharp
public void SetMaterial(Material material)
```

### SetMesh

```csharp
public void SetMesh(Mesh mesh)
```
