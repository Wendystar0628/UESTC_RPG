<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CsgPolygon3D

命名空间 `Godot`；class；继承 [`CsgPrimitive3D`](CsgPrimitive3D.md) → [`CsgShape3D`](CsgShape3D.md) → [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

An array of 2D points is extruded to quickly and easily create a variety of 3D meshes. See also [`CsgMesh3D`](CsgMesh3D.md) for using 3D meshes as CSG nodes.

**Note:** CSG nodes are intended to be used for level prototyping. Creating CSG nodes has a significant CPU cost compared to creating a [`MeshInstance3D`](MeshInstance3D.md) with a [`PrimitiveMesh`](PrimitiveMesh.md). Moving a CSG node within another CSG node also has a significant CPU cost, so it should be avoided during gameplay.

## 构造函数

### CsgPolygon3D

```csharp
public CsgPolygon3D()
```

## 属性

### Depth

```csharp
public float Depth { get; set; }
```

When `Mode` is [`CsgPolygon3D.ModeEnum.Depth`](CsgPolygon3D.md), the depth of the extrusion.

### Material

```csharp
public Material Material { get; set; }
```

Material to use for the resulting mesh. The UV maps the top half of the material to the extruded shape (U along the length of the extrusions and V around the outline of the `Polygon`), the bottom-left quarter to the front end face, and the bottom-right quarter to the back end face.

### Mode

```csharp
public CsgPolygon3D.ModeEnum Mode { get; set; }
```

The `Mode` used to extrude the `Polygon`.

### PathContinuousU

```csharp
public bool PathContinuousU { get; set; }
```

When `Mode` is [`CsgPolygon3D.ModeEnum.Path`](CsgPolygon3D.md), by default, the top half of the `Material` is stretched along the entire length of the extruded shape. If `false` the top half of the material is repeated every step of the extrusion.

### PathInterval

```csharp
public float PathInterval { get; set; }
```

When `Mode` is [`CsgPolygon3D.ModeEnum.Path`](CsgPolygon3D.md), the path interval or ratio of path points to extrusions.

### PathIntervalType

```csharp
public CsgPolygon3D.PathIntervalTypeEnum PathIntervalType { get; set; }
```

When `Mode` is [`CsgPolygon3D.ModeEnum.Path`](CsgPolygon3D.md), this will determine if the interval should be by distance ([`CsgPolygon3D.PathIntervalTypeEnum.Distance`](CsgPolygon3D.md)) or subdivision fractions ([`CsgPolygon3D.PathIntervalTypeEnum.Subdivide`](CsgPolygon3D.md)).

### PathJoined

```csharp
public bool PathJoined { get; set; }
```

When `Mode` is [`CsgPolygon3D.ModeEnum.Path`](CsgPolygon3D.md), if `true` the ends of the path are joined, by adding an extrusion between the last and first points of the path.

### PathLocal

```csharp
public bool PathLocal { get; set; }
```

When `Mode` is [`CsgPolygon3D.ModeEnum.Path`](CsgPolygon3D.md), if `true` the [`Transform3D`](Transform3D.md) of the `CsgPolygon3D` is used as the starting point for the extrusions, not the [`Transform3D`](Transform3D.md) of the `PathNode`.

### PathNode

```csharp
public NodePath PathNode { get; set; }
```

When `Mode` is [`CsgPolygon3D.ModeEnum.Path`](CsgPolygon3D.md), the location of the [`Path3D`](Path3D.md) object used to extrude the `Polygon`.

### PathRotation

```csharp
public CsgPolygon3D.PathRotationEnum PathRotation { get; set; }
```

When `Mode` is [`CsgPolygon3D.ModeEnum.Path`](CsgPolygon3D.md), the path rotation method used to rotate the `Polygon` as it is extruded.

### PathRotationAccurate

```csharp
public bool PathRotationAccurate { get; set; }
```

When `Mode` is [`CsgPolygon3D.ModeEnum.Path`](CsgPolygon3D.md), if `true` the polygon will be rotated according to the proper tangent of the path at the sampled points. If `false` an approximation is used, which decreases in accuracy as the number of subdivisions decreases.

### PathSimplifyAngle

```csharp
public float PathSimplifyAngle { get; set; }
```

When `Mode` is [`CsgPolygon3D.ModeEnum.Path`](CsgPolygon3D.md), extrusions that are less than this angle, will be merged together to reduce polygon count.

### PathUDistance

```csharp
public float PathUDistance { get; set; }
```

When `Mode` is [`CsgPolygon3D.ModeEnum.Path`](CsgPolygon3D.md), this is the distance along the path, in meters, the texture coordinates will tile. When set to 0, texture coordinates will match geometry exactly with no tiling.

### Polygon

```csharp
public Vector2[] Polygon { get; set; }
```

The point array that defines the 2D polygon that is extruded. This can be a convex or concave polygon with 3 or more points. The polygon must *not* have any intersecting edges. Otherwise, triangulation will fail and no mesh will be generated.

**Note:** If only 1 or 2 points are defined in `Polygon`, no mesh will be generated.

### SmoothFaces

```csharp
public bool SmoothFaces { get; set; }
```

If `true`, applies smooth shading to the extrusions.

### SpinDegrees

```csharp
public float SpinDegrees { get; set; }
```

When `Mode` is [`CsgPolygon3D.ModeEnum.Spin`](CsgPolygon3D.md), the total number of degrees the `Polygon` is rotated when extruding.

### SpinSides

```csharp
public int SpinSides { get; set; }
```

When `Mode` is [`CsgPolygon3D.ModeEnum.Spin`](CsgPolygon3D.md), the number of extrusions made.

## 方法

### GetDepth

```csharp
public float GetDepth()
```

### GetMaterial

```csharp
public Material GetMaterial()
```

### GetMode

```csharp
public CsgPolygon3D.ModeEnum GetMode()
```

### GetPathInterval

```csharp
public float GetPathInterval()
```

### GetPathIntervalType

```csharp
public CsgPolygon3D.PathIntervalTypeEnum GetPathIntervalType()
```

### GetPathNode

```csharp
public NodePath GetPathNode()
```

### GetPathRotation

```csharp
public CsgPolygon3D.PathRotationEnum GetPathRotation()
```

### GetPathRotationAccurate

```csharp
public bool GetPathRotationAccurate()
```

### GetPathSimplifyAngle

```csharp
public float GetPathSimplifyAngle()
```

### GetPathUDistance

```csharp
public float GetPathUDistance()
```

### GetPolygon

```csharp
public Vector2[] GetPolygon()
```

### GetSmoothFaces

```csharp
public bool GetSmoothFaces()
```

### GetSpinDegrees

```csharp
public float GetSpinDegrees()
```

### GetSpinSides

```csharp
public int GetSpinSides()
```

### IsPathContinuousU

```csharp
public bool IsPathContinuousU()
```

### IsPathJoined

```csharp
public bool IsPathJoined()
```

### IsPathLocal

```csharp
public bool IsPathLocal()
```

### SetDepth

```csharp
public void SetDepth(float depth)
```

### SetMaterial

```csharp
public void SetMaterial(Material material)
```

### SetMode

```csharp
public void SetMode(CsgPolygon3D.ModeEnum mode)
```

### SetPathContinuousU

```csharp
public void SetPathContinuousU(bool enable)
```

### SetPathInterval

```csharp
public void SetPathInterval(float interval)
```

### SetPathIntervalType

```csharp
public void SetPathIntervalType(CsgPolygon3D.PathIntervalTypeEnum intervalType)
```

### SetPathJoined

```csharp
public void SetPathJoined(bool enable)
```

### SetPathLocal

```csharp
public void SetPathLocal(bool enable)
```

### SetPathNode

```csharp
public void SetPathNode(NodePath path)
```

### SetPathRotation

```csharp
public void SetPathRotation(CsgPolygon3D.PathRotationEnum pathRotation)
```

### SetPathRotationAccurate

```csharp
public void SetPathRotationAccurate(bool enable)
```

### SetPathSimplifyAngle

```csharp
public void SetPathSimplifyAngle(float degrees)
```

### SetPathUDistance

```csharp
public void SetPathUDistance(float distance)
```

### SetPolygon

```csharp
public void SetPolygon(ReadOnlySpan<Vector2> polygon)
```

### SetPolygon

```csharp
public void SetPolygon(Vector2[] polygon)
```

### SetSmoothFaces

```csharp
public void SetSmoothFaces(bool smoothFaces)
```

### SetSpinDegrees

```csharp
public void SetSpinDegrees(float degrees)
```

### SetSpinSides

```csharp
public void SetSpinSides(int spinSides)
```

## ModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Depth = 0`：The [`CsgPolygon3D.Polygon`](CsgPolygon3D.md) shape is extruded along the negative Z axis.
- `Spin = 1`：The [`CsgPolygon3D.Polygon`](CsgPolygon3D.md) shape is extruded by rotating it around the Y axis.
- `Path = 2`：The [`CsgPolygon3D.Polygon`](CsgPolygon3D.md) shape is extruded along the [`Path3D`](Path3D.md) specified in [`CsgPolygon3D.PathNode`](CsgPolygon3D.md).

## PathIntervalTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Distance = 0`：When [`CsgPolygon3D.Mode`](CsgPolygon3D.md) is set to [`CsgPolygon3D.ModeEnum.Path`](CsgPolygon3D.md), [`CsgPolygon3D.PathInterval`](CsgPolygon3D.md) will determine the distance, in meters, each interval of the path will…
- `Subdivide = 1`：When [`CsgPolygon3D.Mode`](CsgPolygon3D.md) is set to [`CsgPolygon3D.ModeEnum.Path`](CsgPolygon3D.md), [`CsgPolygon3D.PathInterval`](CsgPolygon3D.md) will subdivide the polygons along the path.

## PathRotationEnum

命名空间 `Godot`；enum

### 枚举值

- `Polygon = 0`：The [`CsgPolygon3D.Polygon`](CsgPolygon3D.md) shape is not rotated.
- `Path = 1`：The [`CsgPolygon3D.Polygon`](CsgPolygon3D.md) shape is rotated along the path, but it is not rotated around the path axis.
- `PathFollow = 2`：The [`CsgPolygon3D.Polygon`](CsgPolygon3D.md) shape follows the path and its rotations around the path axis.
