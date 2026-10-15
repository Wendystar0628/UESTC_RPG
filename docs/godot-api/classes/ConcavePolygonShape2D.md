<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ConcavePolygonShape2D

命名空间 `Godot`；class；继承 [`Shape2D`](Shape2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 2D polyline shape, intended for use in physics. Used internally in [`CollisionPolygon2D`](CollisionPolygon2D.md) when it's in [`CollisionPolygon2D.BuildModeEnum.Segments`](CollisionPolygon2D.md) mode.

Being just a collection of interconnected line segments, `ConcavePolygonShape2D` is the most freely configurable single 2D shape. It can be used to form polygons of any nature, or even shapes that don't enclose an area. However, `ConcavePolygonShape2D` is *hollow* even if the interconnected line segments do enclose an area, which often makes it unsuitable for physics or detection.

**Note:** When used for collision, `ConcavePolygonShape2D` is intended to work with static [`CollisionShape2D`](CollisionShape2D.md) nodes like [`StaticBody2D`](StaticBody2D.md) and will likely not behave well for [`CharacterBody2D`](CharacterBody2D.md)s or [`RigidBody2D`](RigidBody2D.md)s in a mode other than Static.

**Warning:** Physics bodies that are small have a chance to clip through this shape when moving fast. This happens because on one frame, the physics body may be on the "outside" of the shape, and on the next frame it may be "inside" it. `ConcavePolygonShape2D` is hollow, so it won't detect a collision.

**Performance:** Due to its complexity, `ConcavePolygonShape2D` is the slowest 2D collision shape to check collisions against. Its use should generally be limited to level geometry. If the polyline is closed, [`CollisionPolygon2D`](CollisionPolygon2D.md)'s [`CollisionPolygon2D.BuildModeEnum.Solids`](CollisionPolygon2D.md) mode can be used, which decomposes the polygon into convex ones; see [`ConvexPolygonShape2D`](ConvexPolygonShape2D.md)'s documentation for instructions.

## 构造函数

### ConcavePolygonShape2D

```csharp
public ConcavePolygonShape2D()
```

## 属性

### Segments

```csharp
public Vector2[] Segments { get; set; }
```

引擎名：`segments`

The array of points that make up the `ConcavePolygonShape2D`'s line segments. The array (of length divisible by two) is naturally divided into pairs (one pair for each segment); each pair consists of the starting point of a segment and the endpoint of a segment.

## 方法

### GetSegments

```csharp
public Vector2[] GetSegments()
```

引擎名：`get_segments`

### SetSegments

```csharp
public void SetSegments(ReadOnlySpan<Vector2> segments)
```

引擎名：`set_segments`

### SetSegments

```csharp
public void SetSegments(Vector2[] segments)
```

引擎名：`set_segments`
