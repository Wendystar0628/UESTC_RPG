<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialComponentPlaneSemanticLabelList

命名空间 `Godot`；class；继承 [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Object for storing the queries plane semantic label result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

## 构造函数

### OpenXRSpatialComponentPlaneSemanticLabelList

```csharp
public OpenXRSpatialComponentPlaneSemanticLabelList()
```

## 方法

### GetPlaneSemanticLabel

```csharp
public OpenXRSpatialComponentPlaneSemanticLabelList.PlaneSemanticLabel GetPlaneSemanticLabel(long index)
```

引擎名：`get_plane_semantic_label`

Returns the plane semantic label for the parent entity at this `index`.

## PlaneSemanticLabel

命名空间 `Godot`；enum

### 枚举值

- `Uncategorized = 1`：Uncategorized plane.
- `Floor = 2`：Plane represents a floor.
- `Wall = 3`：Plane represents a wall.
- `Ceiling = 4`：Plane represents a ceiling.
- `Table = 5`：Plane represents the surface of a table.
