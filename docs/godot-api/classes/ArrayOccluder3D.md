<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ArrayOccluder3D

命名空间 `Godot`；class；继承 [`Occluder3D`](Occluder3D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`ArrayOccluder3D` stores an arbitrary 3D polygon shape that can be used by the engine's occlusion culling system. This is analogous to [`ArrayMesh`](ArrayMesh.md), but for occluders.

See [`OccluderInstance3D`](OccluderInstance3D.md)'s documentation for instructions on setting up occlusion culling.

## 构造函数

### ArrayOccluder3D

```csharp
public ArrayOccluder3D()
```

## 属性

### Indices

```csharp
public int[] Indices { get; set; }
```

引擎名：`indices`

The occluder's index position. Indices determine which points from the `Vertices` array should be drawn, and in which order.

**Note:** The occluder is always updated after setting this value. If creating occluders procedurally, consider using `SetArrays()` instead to avoid updating the occluder twice when it's created.

### Vertices

```csharp
public Vector3[] Vertices { get; set; }
```

引擎名：`vertices`

The occluder's vertex positions in local 3D coordinates.

**Note:** The occluder is always updated after setting this value. If creating occluders procedurally, consider using `SetArrays()` instead to avoid updating the occluder twice when it's created.

## 方法

### SetArrays

```csharp
public void SetArrays(ReadOnlySpan<Vector3> vertices, ReadOnlySpan<int> indices)
```

引擎名：`set_arrays`

Sets `Indices` and `Vertices`, while updating the final occluder only once after both values are set.

### SetArrays

```csharp
public void SetArrays(Vector3[] vertices, int[] indices)
```

引擎名：`set_arrays`

Sets `Indices` and `Vertices`, while updating the final occluder only once after both values are set.

### SetIndices

```csharp
public void SetIndices(ReadOnlySpan<int> indices)
```

引擎名：`set_indices`

### SetIndices

```csharp
public void SetIndices(int[] indices)
```

引擎名：`set_indices`

### SetVertices

```csharp
public void SetVertices(ReadOnlySpan<Vector3> vertices)
```

引擎名：`set_vertices`

### SetVertices

```csharp
public void SetVertices(Vector3[] vertices)
```

引擎名：`set_vertices`
