<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TubeTrailMesh

命名空间 `Godot`；class；继承 [`PrimitiveMesh`](PrimitiveMesh.md) → [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`TubeTrailMesh` represents a straight tube-shaped mesh with variable width. The tube is composed of a number of cylindrical sections, each with the same `SectionLength` and number of `SectionRings`. A `Curve` is sampled along the total length of the tube, meaning that the curve determines the radius of the tube along its length.

This primitive mesh is usually used for particle trails.

## 构造函数

### TubeTrailMesh

```csharp
public TubeTrailMesh()
```

## 属性

### CapBottom

```csharp
public bool CapBottom { get; set; }
```

引擎名：`cap_bottom`

If `true`, generates a cap at the bottom of the tube. This can be set to `false` to speed up generation and rendering when the cap is never seen by the camera.

### CapTop

```csharp
public bool CapTop { get; set; }
```

引擎名：`cap_top`

If `true`, generates a cap at the top of the tube. This can be set to `false` to speed up generation and rendering when the cap is never seen by the camera.

### Curve

```csharp
public Curve Curve { get; set; }
```

引擎名：`curve`

Determines the radius of the tube along its length. The radius of a particular section ring is obtained by multiplying the baseline `Radius` by the value of this curve at the given distance. For values smaller than `0`, the faces will be inverted. Should be a unit [`Curve`](Curve.md).

### RadialSteps

```csharp
public int RadialSteps { get; set; }
```

引擎名：`radial_steps`

The number of sides on the tube. For example, a value of `5` means the tube will be pentagonal. Higher values result in a more detailed tube at the cost of performance.

### Radius

```csharp
public float Radius { get; set; }
```

引擎名：`radius`

The baseline radius of the tube. The radius of a particular section ring is obtained by multiplying this radius by the value of the `Curve` at the given distance.

### SectionLength

```csharp
public float SectionLength { get; set; }
```

引擎名：`section_length`

The length of a section of the tube.

### SectionRings

```csharp
public int SectionRings { get; set; }
```

引擎名：`section_rings`

The number of rings in a section. The `Curve` is sampled on each ring to determine its radius. Higher values result in a more detailed tube at the cost of performance.

### Sections

```csharp
public int Sections { get; set; }
```

引擎名：`sections`

The total number of sections on the tube.

## 方法

### GetCurve

```csharp
public Curve GetCurve()
```

引擎名：`get_curve`

### GetRadialSteps

```csharp
public int GetRadialSteps()
```

引擎名：`get_radial_steps`

### GetRadius

```csharp
public float GetRadius()
```

引擎名：`get_radius`

### GetSectionLength

```csharp
public float GetSectionLength()
```

引擎名：`get_section_length`

### GetSectionRings

```csharp
public int GetSectionRings()
```

引擎名：`get_section_rings`

### GetSections

```csharp
public int GetSections()
```

引擎名：`get_sections`

### IsCapBottom

```csharp
public bool IsCapBottom()
```

引擎名：`is_cap_bottom`

### IsCapTop

```csharp
public bool IsCapTop()
```

引擎名：`is_cap_top`

### SetCapBottom

```csharp
public void SetCapBottom(bool capBottom)
```

引擎名：`set_cap_bottom`

### SetCapTop

```csharp
public void SetCapTop(bool capTop)
```

引擎名：`set_cap_top`

### SetCurve

```csharp
public void SetCurve(Curve curve)
```

引擎名：`set_curve`

### SetRadialSteps

```csharp
public void SetRadialSteps(int radialSteps)
```

引擎名：`set_radial_steps`

### SetRadius

```csharp
public void SetRadius(float radius)
```

引擎名：`set_radius`

### SetSectionLength

```csharp
public void SetSectionLength(float sectionLength)
```

引擎名：`set_section_length`

### SetSectionRings

```csharp
public void SetSectionRings(int sectionRings)
```

引擎名：`set_section_rings`

### SetSections

```csharp
public void SetSections(int sections)
```

引擎名：`set_sections`
