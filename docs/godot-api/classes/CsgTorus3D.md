<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CsgTorus3D

命名空间 `Godot`；class；继承 [`CsgPrimitive3D`](CsgPrimitive3D.md) → [`CsgShape3D`](CsgShape3D.md) → [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node allows you to create a torus for use with the CSG system.

**Note:** CSG nodes are intended to be used for level prototyping. Creating CSG nodes has a significant CPU cost compared to creating a [`MeshInstance3D`](MeshInstance3D.md) with a [`PrimitiveMesh`](PrimitiveMesh.md). Moving a CSG node within another CSG node also has a significant CPU cost, so it should be avoided during gameplay.

## 构造函数

### CsgTorus3D

```csharp
public CsgTorus3D()
```

## 属性

### InnerRadius

```csharp
public float InnerRadius { get; set; }
```

The inner radius of the torus.

### Material

```csharp
public Material Material { get; set; }
```

The material used to render the torus.

### OuterRadius

```csharp
public float OuterRadius { get; set; }
```

The outer radius of the torus.

### RingSides

```csharp
public int RingSides { get; set; }
```

The number of edges each ring of the torus is constructed of.

### Sides

```csharp
public int Sides { get; set; }
```

The number of slices the torus is constructed of.

### SmoothFaces

```csharp
public bool SmoothFaces { get; set; }
```

If `true` the normals of the torus are set to give a smooth effect making the torus seem rounded. If `false` the torus will have a flat shaded look.

## 方法

### GetInnerRadius

```csharp
public float GetInnerRadius()
```

### GetMaterial

```csharp
public Material GetMaterial()
```

### GetOuterRadius

```csharp
public float GetOuterRadius()
```

### GetRingSides

```csharp
public int GetRingSides()
```

### GetSides

```csharp
public int GetSides()
```

### GetSmoothFaces

```csharp
public bool GetSmoothFaces()
```

### SetInnerRadius

```csharp
public void SetInnerRadius(float radius)
```

### SetMaterial

```csharp
public void SetMaterial(Material material)
```

### SetOuterRadius

```csharp
public void SetOuterRadius(float radius)
```

### SetRingSides

```csharp
public void SetRingSides(int sides)
```

### SetSides

```csharp
public void SetSides(int sides)
```

### SetSmoothFaces

```csharp
public void SetSmoothFaces(bool smoothFaces)
```
