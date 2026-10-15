<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialComponentMesh3DList

命名空间 `Godot`；class；继承 [`OpenXRSpatialComponentData`](OpenXRSpatialComponentData.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Object for storing the queries 3d mesh result data when calling [`OpenXRSpatialEntityExtension.QuerySnapshot`](OpenXRSpatialEntityExtension.md).

## 构造函数

### OpenXRSpatialComponentMesh3DList

```csharp
public OpenXRSpatialComponentMesh3DList()
```

## 方法

### GetMesh

```csharp
public Mesh GetMesh(long index)
```

引擎名：`get_mesh`

Returns the mesh for the entity at this `index`.

### GetTransform

```csharp
public Transform3D GetTransform(long index)
```

引擎名：`get_transform`

Returns the transform for positioning our mesh for the entity at this `index`.
