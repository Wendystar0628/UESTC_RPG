<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ConvexPolygonShape2D

命名空间 `Godot`；class；继承 [`Shape2D`](Shape2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 2D convex polygon shape, intended for use in physics. Used internally in [`CollisionPolygon2D`](CollisionPolygon2D.md) when it's in [`CollisionPolygon2D.BuildModeEnum.Solids`](CollisionPolygon2D.md) mode.

`ConvexPolygonShape2D` is *solid*, which means it detects collisions from objects that are fully inside it, unlike [`ConcavePolygonShape2D`](ConcavePolygonShape2D.md) which is hollow. This makes it more suitable for both detection and physics.

**Convex decomposition:** A concave polygon can be split up into several convex polygons. This allows dynamic physics bodies to have complex concave collisions (at a performance cost) and can be achieved by using several `ConvexPolygonShape2D` nodes or by using the [`CollisionPolygon2D`](CollisionPolygon2D.md) node in [`CollisionPolygon2D.BuildModeEnum.Solids`](CollisionPolygon2D.md) mode. To generate a collision polygon from a sprite, select the [`Sprite2D`](Sprite2D.md) node, go to the **Sprite2D** menu that appears above the viewport, and choose **Create Polygon2D Sibling**.

**Performance:** `ConvexPolygonShape2D` is faster to check collisions against compared to [`ConcavePolygonShape2D`](ConcavePolygonShape2D.md), but it is slower than primitive collision shapes such as [`CircleShape2D`](CircleShape2D.md) and [`RectangleShape2D`](RectangleShape2D.md). Its use should generally be limited to medium-sized objects that cannot have their collision accurately represented by primitive shapes.

## 构造函数

### ConvexPolygonShape2D

```csharp
public ConvexPolygonShape2D()
```

## 属性

### Points

```csharp
public Vector2[] Points { get; set; }
```

引擎名：`points`

The polygon's list of vertices that form a convex hull. Can be in either clockwise or counterclockwise order.

**Warning:** Only set this property to a list of points that actually form a convex hull. Use `SetPointCloud()` to generate the convex hull of an arbitrary set of points.

## 方法

### GetPoints

```csharp
public Vector2[] GetPoints()
```

引擎名：`get_points`

### SetPointCloud

```csharp
public void SetPointCloud(ReadOnlySpan<Vector2> pointCloud)
```

引擎名：`set_point_cloud`

Based on the set of points provided, this assigns the `Points` property using the convex hull algorithm, removing all unneeded points. See [`Geometry2D.ConvexHull`](Geometry2D.md) for details.

### SetPointCloud

```csharp
public void SetPointCloud(Vector2[] pointCloud)
```

引擎名：`set_point_cloud`

Based on the set of points provided, this assigns the `Points` property using the convex hull algorithm, removing all unneeded points. See [`Geometry2D.ConvexHull`](Geometry2D.md) for details.

### SetPoints

```csharp
public void SetPoints(ReadOnlySpan<Vector2> points)
```

引擎名：`set_points`

### SetPoints

```csharp
public void SetPoints(Vector2[] points)
```

引擎名：`set_points`
