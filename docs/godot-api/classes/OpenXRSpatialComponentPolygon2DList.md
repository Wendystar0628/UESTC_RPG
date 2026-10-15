<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialComponentPolygon2DList

命名空间 `Godot`；class；继承 [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Object for storing the queries 2D polygon result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

## 构造函数

### OpenXRSpatialComponentPolygon2DList

```csharp
public OpenXRSpatialComponentPolygon2DList()
```

## 方法

### GetTransform

```csharp
public Transform3D GetTransform(long index)
```

引擎名：`get_transform`

Returns the transform for positioning our polygon for the entity at this `index`.

### GetVertices

```csharp
public Vector2[] GetVertices(Rid snapshot, long index)
```

引擎名：`get_vertices`

Returns the polygon vertices for the entity at this `index`.
