<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TriangleMesh

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Creates a bounding volume hierarchy (BVH) tree structure around triangle geometry.

The triangle BVH tree can be used for efficient intersection queries without involving a physics engine.

For example, this can be used in editor tools to select objects with complex shapes based on the mouse cursor position.

**Performance:** Creating the BVH tree for complex geometry is a slow process and best done in a background thread.

## 构造函数

### TriangleMesh

```csharp
public TriangleMesh()
```

## 方法

### CreateFromFaces

```csharp
public bool CreateFromFaces(ReadOnlySpan<Vector3> faces)
```

引擎名：`create_from_faces`

Creates the BVH tree from an array of faces. Each 3 vertices of the input `faces` array represent one triangle (face).

Returns `true` if the tree is successfully built, `false` otherwise.

### CreateFromFaces

```csharp
public bool CreateFromFaces(Vector3[] faces)
```

引擎名：`create_from_faces`

Creates the BVH tree from an array of faces. Each 3 vertices of the input `faces` array represent one triangle (face).

Returns `true` if the tree is successfully built, `false` otherwise.

### GetFaces

```csharp
public Vector3[] GetFaces()
```

引擎名：`get_faces`

Returns a copy of the geometry faces. Each 3 vertices of the array represent one triangle (face).

### IntersectRay

```csharp
public Dictionary IntersectRay(Vector3 begin, Vector3 dir)
```

引擎名：`intersect_ray`

Tests for intersection with a ray starting at `begin` and facing `dir` and extending toward infinity.

If an intersection with a triangle happens, returns a [`Collections.Dictionary`](Collections.Dictionary.md) with the following fields:

`position`: The position on the intersected triangle.

`normal`: The normal of the intersected triangle.

`face_index`: The index of the intersected triangle.

Returns an empty [`Collections.Dictionary`](Collections.Dictionary.md) if no intersection happens.

See also `IntersectSegment()`, which is similar but uses a finite-length segment.

### IntersectSegment

```csharp
public Dictionary IntersectSegment(Vector3 begin, Vector3 end)
```

引擎名：`intersect_segment`

Tests for intersection with a segment going from `begin` to `end`.

If an intersection with a triangle happens returns a [`Collections.Dictionary`](Collections.Dictionary.md) with the following fields:

`position`: The position on the intersected triangle.

`normal`: The normal of the intersected triangle.

`face_index`: The index of the intersected triangle.

Returns an empty [`Collections.Dictionary`](Collections.Dictionary.md) if no intersection happens.

See also `IntersectRay()`, which is similar but uses an infinite-length ray.
