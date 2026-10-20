<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialComponentMesh2DList

命名空间 `Godot`；class；继承 [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Object for storing the queries 2D mesh result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

## 构造函数

### OpenXRSpatialComponentMesh2DList

```csharp
public OpenXRSpatialComponentMesh2DList()
```

## 方法

### GetIndices

```csharp
public int[] GetIndices(Rid snapshot, long index)
```

引擎名：`get_indices`

Returns the mesh indices for the entity at this `index`.

### GetTransform

```csharp
public Transform3D GetTransform(long index)
```

引擎名：`get_transform`

Returns the transform for positioning our mesh for the entity at this `index`.

### GetVertices

```csharp
public Vector2[] GetVertices(Rid snapshot, long index)
```

引擎名：`get_vertices`

Returns the mesh vertices for the entity at this `index`.
