<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialComponentAnchorList

命名空间 `Godot`；class；继承 [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Object for storing the queries anchor result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

## 构造函数

### OpenXRSpatialComponentAnchorList

```csharp
public OpenXRSpatialComponentAnchorList()
```

## 方法

### GetEntityPose

```csharp
public Transform3D GetEntityPose(long index)
```

引擎名：`get_entity_pose`

Returns the transform for the entity at this `index`.
