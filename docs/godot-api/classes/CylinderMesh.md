<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CylinderMesh

命名空间 `Godot`；class；继承 [`PrimitiveMesh`](PrimitiveMesh.md) → [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Class representing a cylindrical [`PrimitiveMesh`](PrimitiveMesh.md). This class can be used to create cones by setting either the `TopRadius` or `BottomRadius` properties to `0.0`.

## 构造函数

### CylinderMesh

```csharp
public CylinderMesh()
```

## 属性

### BottomRadius

```csharp
public float BottomRadius { get; set; }
```

引擎名：`bottom_radius`

Bottom radius of the cylinder. If set to `0.0`, the bottom faces will not be generated, resulting in a conic shape. See also `CapBottom`.

### CapBottom

```csharp
public bool CapBottom { get; set; }
```

引擎名：`cap_bottom`

If `true`, generates a cap at the bottom of the cylinder. This can be set to `false` to speed up generation and rendering when the cap is never seen by the camera. See also `BottomRadius`.

**Note:** If `BottomRadius` is `0.0`, cap generation is always skipped even if `CapBottom` is `true`.

### CapTop

```csharp
public bool CapTop { get; set; }
```

引擎名：`cap_top`

If `true`, generates a cap at the top of the cylinder. This can be set to `false` to speed up generation and rendering when the cap is never seen by the camera. See also `TopRadius`.

**Note:** If `TopRadius` is `0.0`, cap generation is always skipped even if `CapTop` is `true`.

### Height

```csharp
public float Height { get; set; }
```

引擎名：`height`

Full height of the cylinder.

### RadialSegments

```csharp
public int RadialSegments { get; set; }
```

引擎名：`radial_segments`

Number of radial segments on the cylinder. Higher values result in a more detailed cylinder/cone at the cost of performance.

### Rings

```csharp
public int Rings { get; set; }
```

引擎名：`rings`

Number of edge rings along the height of the cylinder. Changing `Rings` does not have any visual impact unless a shader or procedural mesh tool is used to alter the vertex data. Higher values result in more subdivisions, which can be used to create smoother-looking effects with shaders or procedural mesh tools (at the cost of performance). When not altering the vertex data using a shader or procedural mesh tool, `Rings` should be kept to its default value.

### TopRadius

```csharp
public float TopRadius { get; set; }
```

引擎名：`top_radius`

Top radius of the cylinder. If set to `0.0`, the top faces will not be generated, resulting in a conic shape. See also `CapTop`.

## 方法

### GetBottomRadius

```csharp
public float GetBottomRadius()
```

引擎名：`get_bottom_radius`

### GetHeight

```csharp
public float GetHeight()
```

引擎名：`get_height`

### GetRadialSegments

```csharp
public int GetRadialSegments()
```

引擎名：`get_radial_segments`

### GetRings

```csharp
public int GetRings()
```

引擎名：`get_rings`

### GetTopRadius

```csharp
public float GetTopRadius()
```

引擎名：`get_top_radius`

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

### SetBottomRadius

```csharp
public void SetBottomRadius(float radius)
```

引擎名：`set_bottom_radius`

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

### SetHeight

```csharp
public void SetHeight(float height)
```

引擎名：`set_height`

### SetRadialSegments

```csharp
public void SetRadialSegments(int segments)
```

引擎名：`set_radial_segments`

### SetRings

```csharp
public void SetRings(int rings)
```

引擎名：`set_rings`

### SetTopRadius

```csharp
public void SetTopRadius(float radius)
```

引擎名：`set_top_radius`
