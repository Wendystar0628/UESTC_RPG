<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CsgCylinder3D

命名空间 `Godot`；class；继承 [`CsgPrimitive3D`](CsgPrimitive3D.md) → [`CsgShape3D`](CsgShape3D.md) → [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node allows you to create a cylinder (or cone) for use with the CSG system.

**Note:** CSG nodes are intended to be used for level prototyping. Creating CSG nodes has a significant CPU cost compared to creating a [`MeshInstance3D`](MeshInstance3D.md) with a [`PrimitiveMesh`](PrimitiveMesh.md). Moving a CSG node within another CSG node also has a significant CPU cost, so it should be avoided during gameplay.

## 构造函数

### CsgCylinder3D

```csharp
public CsgCylinder3D()
```

## 属性

### Cone

```csharp
public bool Cone { get; set; }
```

If `true` a cone is created, the `Radius` will only apply to one side.

### Height

```csharp
public float Height { get; set; }
```

The height of the cylinder.

### Material

```csharp
public Material Material { get; set; }
```

The material used to render the cylinder.

### Radius

```csharp
public float Radius { get; set; }
```

The radius of the cylinder.

### Sides

```csharp
public int Sides { get; set; }
```

The number of sides of the cylinder, the higher this number the more detail there will be in the cylinder.

### SmoothFaces

```csharp
public bool SmoothFaces { get; set; }
```

If `true` the normals of the cylinder are set to give a smooth effect making the cylinder seem rounded. If `false` the cylinder will have a flat shaded look.

## 方法

### GetHeight

```csharp
public float GetHeight()
```

### GetMaterial

```csharp
public Material GetMaterial()
```

### GetRadius

```csharp
public float GetRadius()
```

### GetSides

```csharp
public int GetSides()
```

### GetSmoothFaces

```csharp
public bool GetSmoothFaces()
```

### IsCone

```csharp
public bool IsCone()
```

### SetCone

```csharp
public void SetCone(bool cone)
```

### SetHeight

```csharp
public void SetHeight(float height)
```

### SetMaterial

```csharp
public void SetMaterial(Material material)
```

### SetRadius

```csharp
public void SetRadius(float radius)
```

### SetSides

```csharp
public void SetSides(int sides)
```

### SetSmoothFaces

```csharp
public void SetSmoothFaces(bool smoothFaces)
```
