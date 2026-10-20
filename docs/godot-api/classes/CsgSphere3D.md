<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CsgSphere3D

命名空间 `Godot`；class；继承 [`CsgPrimitive3D`](CsgPrimitive3D.md) → [`CsgShape3D`](CsgShape3D.md) → [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node allows you to create a sphere for use with the CSG system.

**Note:** CSG nodes are intended to be used for level prototyping. Creating CSG nodes has a significant CPU cost compared to creating a [`MeshInstance3D`](MeshInstance3D.md) with a [`PrimitiveMesh`](PrimitiveMesh.md). Moving a CSG node within another CSG node also has a significant CPU cost, so it should be avoided during gameplay.

## 构造函数

### CsgSphere3D

```csharp
public CsgSphere3D()
```

## 属性

### Material

```csharp
public Material Material { get; set; }
```

The material used to render the sphere.

### RadialSegments

```csharp
public int RadialSegments { get; set; }
```

Number of vertical slices for the sphere.

### Radius

```csharp
public float Radius { get; set; }
```

Radius of the sphere.

### Rings

```csharp
public int Rings { get; set; }
```

Number of horizontal slices for the sphere.

### SmoothFaces

```csharp
public bool SmoothFaces { get; set; }
```

If `true` the normals of the sphere are set to give a smooth effect making the sphere seem rounded. If `false` the sphere will have a flat shaded look.

## 方法

### GetMaterial

```csharp
public Material GetMaterial()
```

### GetRadialSegments

```csharp
public int GetRadialSegments()
```

### GetRadius

```csharp
public float GetRadius()
```

### GetRings

```csharp
public int GetRings()
```

### GetSmoothFaces

```csharp
public bool GetSmoothFaces()
```

### SetMaterial

```csharp
public void SetMaterial(Material material)
```

### SetRadialSegments

```csharp
public void SetRadialSegments(int radialSegments)
```

### SetRadius

```csharp
public void SetRadius(float radius)
```

### SetRings

```csharp
public void SetRings(int rings)
```

### SetSmoothFaces

```csharp
public void SetSmoothFaces(bool smoothFaces)
```
