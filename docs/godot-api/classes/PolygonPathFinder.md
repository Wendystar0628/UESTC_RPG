<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PolygonPathFinder

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

## 构造函数

### PolygonPathFinder

```csharp
public PolygonPathFinder()
```

## 属性

### Data

```csharp
public Dictionary Data { get; set; }
```

引擎名：`data`

## 方法

### FindPath

```csharp
public Vector2[] FindPath(Vector2 from, Vector2 to)
```

引擎名：`find_path`

### GetBounds

```csharp
public Rect2 GetBounds()
```

引擎名：`get_bounds`

### GetClosestPoint

```csharp
public Vector2 GetClosestPoint(Vector2 point)
```

引擎名：`get_closest_point`

### GetIntersections

```csharp
public Vector2[] GetIntersections(Vector2 from, Vector2 to)
```

引擎名：`get_intersections`

### GetPointPenalty

```csharp
public float GetPointPenalty(int idx)
```

引擎名：`get_point_penalty`

### IsPointInside

```csharp
public bool IsPointInside(Vector2 point)
```

引擎名：`is_point_inside`

Returns `true` if `point` falls inside the polygon area.

```csharp
var polygonPathFinder = new PolygonPathFinder();
Vector2[] points =
[
new Vector2(0.0f, 0.0f),
new Vector2(1.0f, 0.0f),
new Vector2(0.0f, 1.0f)
];
int[] connections = [0, 1, 1, 2, 2, 0];
polygonPathFinder.Setup(points, connections);
GD.Print(polygonPathFinder.IsPointInside(new Vector2(0.2f, 0.2f))); // Prints True
GD.Print(polygonPathFinder.IsPointInside(new Vector2(1.0f, 1.0f))); // Prints False

```

### SetPointPenalty

```csharp
public void SetPointPenalty(int idx, float penalty)
```

引擎名：`set_point_penalty`

### Setup

```csharp
public void Setup(ReadOnlySpan<Vector2> points, ReadOnlySpan<int> connections)
```

引擎名：`setup`

Sets up `PolygonPathFinder` with an array of points that define the vertices of the polygon, and an array of indices that determine the edges of the polygon.

The length of `connections` must be even, returns an error if odd.

```csharp
var polygonPathFinder = new PolygonPathFinder();
Vector2[] points =
[
new Vector2(0.0f, 0.0f),
new Vector2(1.0f, 0.0f),
new Vector2(0.0f, 1.0f)
];
int[] connections = [0, 1, 1, 2, 2, 0];
polygonPathFinder.Setup(points, connections);

```

### Setup

```csharp
public void Setup(Vector2[] points, int[] connections)
```

引擎名：`setup`

Sets up `PolygonPathFinder` with an array of points that define the vertices of the polygon, and an array of indices that determine the edges of the polygon.

The length of `connections` must be even, returns an error if odd.

```csharp
var polygonPathFinder = new PolygonPathFinder();
Vector2[] points =
[
new Vector2(0.0f, 0.0f),
new Vector2(1.0f, 0.0f),
new Vector2(0.0f, 1.0f)
];
int[] connections = [0, 1, 1, 2, 2, 0];
polygonPathFinder.Setup(points, connections);

```
