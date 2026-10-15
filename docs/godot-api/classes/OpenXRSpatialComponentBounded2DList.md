<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialComponentBounded2DList

命名空间 `Godot`；class；继承 [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Object for storing the queries 2D bounding rectangle result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

## 构造函数

### OpenXRSpatialComponentBounded2DList

```csharp
public OpenXRSpatialComponentBounded2DList()
```

## 方法

### GetCenterPose

```csharp
public Transform3D GetCenterPose(long index)
```

引擎名：`get_center_pose`

Returns the center of our bounding rectangle for the entity at this `index`.

### GetSize

```csharp
public Vector2 GetSize(long index)
```

引擎名：`get_size`

Returns the size of our bounding rectangle for the entity at this `index`.
