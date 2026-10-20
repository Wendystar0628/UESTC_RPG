<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialComponentPlaneAlignmentList

命名空间 `Godot`；class；继承 [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Object for storing the queries plane alignment result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

## 构造函数

### OpenXRSpatialComponentPlaneAlignmentList

```csharp
public OpenXRSpatialComponentPlaneAlignmentList()
```

## 方法

### GetPlaneAlignment

```csharp
public OpenXRSpatialComponentPlaneAlignmentList.PlaneAlignment GetPlaneAlignment(long index)
```

引擎名：`get_plane_alignment`

Returns the plane alignment for the parent entity at this `index`.

## PlaneAlignment

命名空间 `Godot`；enum

### 枚举值

- `HorizontalUpward = 0`：Plane is facing upward.
- `HorizontalDownward = 1`：Plane is facing downwards.
- `Vertical = 2`：Plane is vertically aligned.
- `Arbitrary = 3`：Plane has an arbitrary alignment.
