<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ConvexPolygonShape3D

命名空间 `Godot`；class；继承 [`Shape3D`](Shape3D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 3D convex polyhedron shape, intended for use in physics. Usually used to provide a shape for a [`CollisionShape3D`](CollisionShape3D.md).

`ConvexPolygonShape3D` is *solid*, which means it detects collisions from objects that are fully inside it, unlike [`ConcavePolygonShape3D`](ConcavePolygonShape3D.md) which is hollow. This makes it more suitable for both detection and physics.

**Convex decomposition:** A concave polyhedron can be split up into several convex polyhedra. This allows dynamic physics bodies to have complex concave collisions (at a performance cost) and can be achieved by using several `ConvexPolygonShape3D` nodes. To generate a convex decomposition from a mesh, select the [`MeshInstance3D`](MeshInstance3D.md) node, go to the **Mesh** menu that appears above the viewport, and choose **Create Multiple Convex Collision Siblings**. Alternatively, [`MeshInstance3D.CreateMultipleConvexCollisions`](MeshInstance3D.md) can be called in a script to perform this decomposition at run-time.

**Performance:** `ConvexPolygonShape3D` is faster to check collisions against compared to [`ConcavePolygonShape3D`](ConcavePolygonShape3D.md), but it is slower than primitive collision shapes such as [`SphereShape3D`](SphereShape3D.md) and [`BoxShape3D`](BoxShape3D.md). Its use should generally be limited to medium-sized objects that cannot have their collision accurately represented by primitive shapes.

## 构造函数

### ConvexPolygonShape3D

```csharp
public ConvexPolygonShape3D()
```

## 属性

### Points

```csharp
public Vector3[] Points { get; set; }
```

引擎名：`points`

The list of 3D points forming the convex polygon shape.

## 方法

### GetPoints

```csharp
public Vector3[] GetPoints()
```

引擎名：`get_points`

### SetPoints

```csharp
public void SetPoints(ReadOnlySpan<Vector3> points)
```

引擎名：`set_points`

### SetPoints

```csharp
public void SetPoints(Vector3[] points)
```

引擎名：`set_points`
