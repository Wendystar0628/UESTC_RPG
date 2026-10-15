<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ConcavePolygonShape3D

命名空间 `Godot`；class；继承 [`Shape3D`](Shape3D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 3D trimesh shape, intended for use in physics. Usually used to provide a shape for a [`CollisionShape3D`](CollisionShape3D.md).

Being just a collection of interconnected triangles, `ConcavePolygonShape3D` is the most freely configurable single 3D shape. It can be used to form polyhedra of any nature, or even shapes that don't enclose a volume. However, `ConcavePolygonShape3D` is *hollow* even if the interconnected triangles do enclose a volume, which often makes it unsuitable for physics or detection.

**Note:** When used for collision, `ConcavePolygonShape3D` is intended to work with static [`CollisionShape3D`](CollisionShape3D.md) nodes like [`StaticBody3D`](StaticBody3D.md) and will likely not behave well for [`CharacterBody3D`](CharacterBody3D.md)s or [`RigidBody3D`](RigidBody3D.md)s in a mode other than Static.

**Warning:** Physics bodies that are small have a chance to clip through this shape when moving fast. This happens because on one frame, the physics body may be on the "outside" of the shape, and on the next frame it may be "inside" it. `ConcavePolygonShape3D` is hollow, so it won't detect a collision.

**Performance:** Due to its complexity, `ConcavePolygonShape3D` is the slowest 3D collision shape to check collisions against. Its use should generally be limited to level geometry. For convex geometry, [`ConvexPolygonShape3D`](ConvexPolygonShape3D.md) should be used. For dynamic physics bodies that need concave collision, several [`ConvexPolygonShape3D`](ConvexPolygonShape3D.md)s can be used to represent its collision by using convex decomposition; see [`ConvexPolygonShape3D`](ConvexPolygonShape3D.md)'s documentation for instructions.

## 构造函数

### ConcavePolygonShape3D

```csharp
public ConcavePolygonShape3D()
```

## 属性

### BackfaceCollision

```csharp
public bool BackfaceCollision { get; set; }
```

引擎名：`backface_collision`

If set to `true`, collisions occur on both sides of the concave shape faces. Otherwise they occur only along the face normals.

### Data

```csharp
public Vector3[] Data { get; set; }
```

引擎名：`data`

## 方法

### GetFaces

```csharp
public Vector3[] GetFaces()
```

引擎名：`get_faces`

Returns the faces of the trimesh shape as an array of vertices. The array (of length divisible by three) is naturally divided into triples; each triple of vertices defines a triangle.

### IsBackfaceCollisionEnabled

```csharp
public bool IsBackfaceCollisionEnabled()
```

引擎名：`is_backface_collision_enabled`

### SetBackfaceCollisionEnabled

```csharp
public void SetBackfaceCollisionEnabled(bool enabled)
```

引擎名：`set_backface_collision_enabled`

### SetFaces

```csharp
public void SetFaces(ReadOnlySpan<Vector3> faces)
```

引擎名：`set_faces`

Sets the faces of the trimesh shape from an array of vertices. The `faces` array should be composed of triples such that each triple of vertices defines a triangle.

### SetFaces

```csharp
public void SetFaces(Vector3[] faces)
```

引擎名：`set_faces`

Sets the faces of the trimesh shape from an array of vertices. The `faces` array should be composed of triples such that each triple of vertices defines a triangle.
