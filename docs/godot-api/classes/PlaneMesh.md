<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PlaneMesh

命名空间 `Godot`；class；继承 [`PrimitiveMesh`](PrimitiveMesh.md) → [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Class representing a planar [`PrimitiveMesh`](PrimitiveMesh.md). This flat mesh does not have a thickness. By default, this mesh is aligned on the X and Z axes; this default rotation isn't suited for use with billboarded materials. For billboarded materials, change `Orientation` to [`PlaneMesh.OrientationEnum.Z`](PlaneMesh.md).

**Note:** When using a large textured `PlaneMesh` (e.g. as a floor), you may stumble upon UV jittering issues depending on the camera angle. To solve this, increase `SubdivideDepth` and `SubdivideWidth` until you no longer notice UV jittering.

## 构造函数

### PlaneMesh

```csharp
public PlaneMesh()
```

## 属性

### CenterOffset

```csharp
public Vector3 CenterOffset { get; set; }
```

引擎名：`center_offset`

Offset of the generated plane. Useful for particles.

### Orientation

```csharp
public PlaneMesh.OrientationEnum Orientation { get; set; }
```

引擎名：`orientation`

Direction that the `PlaneMesh` is facing.

### Size

```csharp
public Vector2 Size { get; set; }
```

引擎名：`size`

Size of the generated plane.

### SubdivideDepth

```csharp
public int SubdivideDepth { get; set; }
```

引擎名：`subdivide_depth`

Number of subdivision along the Z axis.

### SubdivideWidth

```csharp
public int SubdivideWidth { get; set; }
```

引擎名：`subdivide_width`

Number of subdivision along the X axis.

## 方法

### GetCenterOffset

```csharp
public Vector3 GetCenterOffset()
```

引擎名：`get_center_offset`

### GetOrientation

```csharp
public PlaneMesh.OrientationEnum GetOrientation()
```

引擎名：`get_orientation`

### GetSize

```csharp
public Vector2 GetSize()
```

引擎名：`get_size`

### GetSubdivideDepth

```csharp
public int GetSubdivideDepth()
```

引擎名：`get_subdivide_depth`

### GetSubdivideWidth

```csharp
public int GetSubdivideWidth()
```

引擎名：`get_subdivide_width`

### SetCenterOffset

```csharp
public void SetCenterOffset(Vector3 offset)
```

引擎名：`set_center_offset`

### SetOrientation

```csharp
public void SetOrientation(PlaneMesh.OrientationEnum orientation)
```

引擎名：`set_orientation`

### SetSize

```csharp
public void SetSize(Vector2 size)
```

引擎名：`set_size`

### SetSubdivideDepth

```csharp
public void SetSubdivideDepth(int subdivide)
```

引擎名：`set_subdivide_depth`

### SetSubdivideWidth

```csharp
public void SetSubdivideWidth(int subdivide)
```

引擎名：`set_subdivide_width`

## OrientationEnum

命名空间 `Godot`；enum

### 枚举值

- `X = 0`：[`PlaneMesh`](PlaneMesh.md) will face the positive X-axis.
- `Y = 1`：[`PlaneMesh`](PlaneMesh.md) will face the positive Y-axis.
- `Z = 2`：[`PlaneMesh`](PlaneMesh.md) will face the positive Z-axis.
