<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RibbonTrailMesh

命名空间 `Godot`；class；继承 [`PrimitiveMesh`](PrimitiveMesh.md) → [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`RibbonTrailMesh` represents a straight ribbon-shaped mesh with variable width. The ribbon is composed of a number of flat or cross-shaped sections, each with the same `SectionLength` and number of `SectionSegments`. A `Curve` is sampled along the total length of the ribbon, meaning that the curve determines the size of the ribbon along its length.

This primitive mesh is usually used for particle trails.

## 构造函数

### RibbonTrailMesh

```csharp
public RibbonTrailMesh()
```

## 属性

### Curve

```csharp
public Curve Curve { get; set; }
```

引擎名：`curve`

Determines the size of the ribbon along its length. The size of a particular section segment is obtained by multiplying the baseline `Size` by the value of this curve at the given distance. For values smaller than `0`, the faces will be inverted. Should be a unit [`Curve`](Curve.md).

### SectionLength

```csharp
public float SectionLength { get; set; }
```

引擎名：`section_length`

The length of a section of the ribbon.

### SectionSegments

```csharp
public int SectionSegments { get; set; }
```

引擎名：`section_segments`

The number of segments in a section. The `Curve` is sampled on each segment to determine its size. Higher values result in a more detailed ribbon at the cost of performance.

### Sections

```csharp
public int Sections { get; set; }
```

引擎名：`sections`

The total number of sections on the ribbon.

### Shape

```csharp
public RibbonTrailMesh.ShapeEnum Shape { get; set; }
```

引擎名：`shape`

Determines the shape of the ribbon.

### Size

```csharp
public float Size { get; set; }
```

引擎名：`size`

The baseline size of the ribbon. The size of a particular section segment is obtained by multiplying this size by the value of the `Curve` at the given distance.

## 方法

### GetCurve

```csharp
public Curve GetCurve()
```

引擎名：`get_curve`

### GetSectionLength

```csharp
public float GetSectionLength()
```

引擎名：`get_section_length`

### GetSectionSegments

```csharp
public int GetSectionSegments()
```

引擎名：`get_section_segments`

### GetSections

```csharp
public int GetSections()
```

引擎名：`get_sections`

### GetShape

```csharp
public RibbonTrailMesh.ShapeEnum GetShape()
```

引擎名：`get_shape`

### GetSize

```csharp
public float GetSize()
```

引擎名：`get_size`

### SetCurve

```csharp
public void SetCurve(Curve curve)
```

引擎名：`set_curve`

### SetSectionLength

```csharp
public void SetSectionLength(float sectionLength)
```

引擎名：`set_section_length`

### SetSectionSegments

```csharp
public void SetSectionSegments(int sectionSegments)
```

引擎名：`set_section_segments`

### SetSections

```csharp
public void SetSections(int sections)
```

引擎名：`set_sections`

### SetShape

```csharp
public void SetShape(RibbonTrailMesh.ShapeEnum shape)
```

引擎名：`set_shape`

### SetSize

```csharp
public void SetSize(float size)
```

引擎名：`set_size`

## ShapeEnum

命名空间 `Godot`；enum

### 枚举值

- `Flat = 0`：Gives the mesh a single flat face.
- `Cross = 1`：Gives the mesh two perpendicular flat faces, making a cross shape.
