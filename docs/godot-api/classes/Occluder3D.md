<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Occluder3D

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`Occluder3D` stores an occluder shape that can be used by the engine's occlusion culling system.

See [`OccluderInstance3D`](OccluderInstance3D.md)'s documentation for instructions on setting up occlusion culling.

## 方法

### GetIndices

```csharp
public int[] GetIndices()
```

引擎名：`get_indices`

Returns the occluder shape's vertex indices.

### GetVertices

```csharp
public Vector3[] GetVertices()
```

引擎名：`get_vertices`

Returns the occluder shape's vertex positions.
