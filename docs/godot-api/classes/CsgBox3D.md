<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CsgBox3D

命名空间 `Godot`；class；继承 [`CsgPrimitive3D`](CsgPrimitive3D.md) → [`CsgShape3D`](CsgShape3D.md) → [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node allows you to create a box for use with the CSG system.

**Note:** CSG nodes are intended to be used for level prototyping. Creating CSG nodes has a significant CPU cost compared to creating a [`MeshInstance3D`](MeshInstance3D.md) with a [`PrimitiveMesh`](PrimitiveMesh.md). Moving a CSG node within another CSG node also has a significant CPU cost, so it should be avoided during gameplay.

## 构造函数

### CsgBox3D

```csharp
public CsgBox3D()
```

## 属性

### Material

```csharp
public Material Material { get; set; }
```

The material used to render the box.

### Size

```csharp
public Vector3 Size { get; set; }
```

The box's width, height and depth.

## 方法

### GetMaterial

```csharp
public Material GetMaterial()
```

### GetSize

```csharp
public Vector3 GetSize()
```

### SetMaterial

```csharp
public void SetMaterial(Material material)
```

### SetSize

```csharp
public void SetSize(Vector3 size)
```
