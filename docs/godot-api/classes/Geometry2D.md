<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Geometry2D

命名空间 `Godot`；static class

Provides a set of helper functions to create geometric shapes, compute intersections between shapes, and process various other geometric operations in 2D.

## 属性

### Singleton

```csharp
public static Geometry2DInstance Singleton { get; }
```

## 方法

### BresenhamLine

```csharp
public static Array<Vector2I> BresenhamLine(Vector2I from, Vector2I to)
```

引擎名：`bresenham_line`

Returns the [Bresenham line](https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm) between the `from` and `to` points. A Bresenham line is a series of pixels that draws a line and is always 1-pixel thick on every row and column of the drawing (never more, never less).

Example code to draw a line between two [`Marker2D`](Marker2D.md) nodes using a series of [`CanvasItem.DrawRect`](CanvasItem.md) calls:

```csharp
func _draw():
for pixel in Geometry2D.bresenham_line($MarkerA.position, $MarkerB.position):
draw_rect(Rect2(pixel, Vector2.ONE), Color.WHITE)

```

### ClipPolygons

```csharp
public static Array<Vector2[]> ClipPolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB)
```

引擎名：`clip_polygons`

Clips `polygonA` against `polygonB` and returns an array of clipped polygons. This performs [`Geometry2D.PolyBooleanOperation.Difference`](Geometry2D.md) between polygons. Returns an empty array if `polygonB` completely overlaps `polygonA`.

If `polygonB` is enclosed by `polygonA`, returns an outer polygon (boundary) and inner polygon (hole) which could be distinguished by calling `IsPolygonClockwise()`.

### ClipPolygons

```csharp
public static Array<Vector2[]> ClipPolygons(Vector2[] polygonA, Vector2[] polygonB)
```

引擎名：`clip_polygons`

Clips `polygonA` against `polygonB` and returns an array of clipped polygons. This performs [`Geometry2D.PolyBooleanOperation.Difference`](Geometry2D.md) between polygons. Returns an empty array if `polygonB` completely overlaps `polygonA`.

If `polygonB` is enclosed by `polygonA`, returns an outer polygon (boundary) and inner polygon (hole) which could be distinguished by calling `IsPolygonClockwise()`.

### ClipPolylineWithPolygon

```csharp
public static Array<Vector2[]> ClipPolylineWithPolygon(ReadOnlySpan<Vector2> polyline, ReadOnlySpan<Vector2> polygon)
```

引擎名：`clip_polyline_with_polygon`

Clips `polyline` against `polygon` and returns an array of clipped polylines. This performs [`Geometry2D.PolyBooleanOperation.Difference`](Geometry2D.md) between the polyline and the polygon. This operation can be thought of as cutting a line with a closed shape.

### ClipPolylineWithPolygon

```csharp
public static Array<Vector2[]> ClipPolylineWithPolygon(Vector2[] polyline, Vector2[] polygon)
```

引擎名：`clip_polyline_with_polygon`

Clips `polyline` against `polygon` and returns an array of clipped polylines. This performs [`Geometry2D.PolyBooleanOperation.Difference`](Geometry2D.md) between the polyline and the polygon. This operation can be thought of as cutting a line with a closed shape.

### ConvexHull

```csharp
public static Vector2[] ConvexHull(ReadOnlySpan<Vector2> points)
```

引擎名：`convex_hull`

Given an array of [`Vector2`](Vector2.md)s, returns the convex hull as a list of points in counterclockwise order. The last point is the same as the first one.

### ConvexHull

```csharp
public static Vector2[] ConvexHull(Vector2[] points)
```

引擎名：`convex_hull`

Given an array of [`Vector2`](Vector2.md)s, returns the convex hull as a list of points in counterclockwise order. The last point is the same as the first one.

### DecomposePolygonInConvex

```csharp
public static Array<Vector2[]> DecomposePolygonInConvex(ReadOnlySpan<Vector2> polygon)
```

引擎名：`decompose_polygon_in_convex`

Decomposes the `polygon` into multiple convex hulls and returns an array of [`Vector2`](Vector2.md)[].

### DecomposePolygonInConvex

```csharp
public static Array<Vector2[]> DecomposePolygonInConvex(Vector2[] polygon)
```

引擎名：`decompose_polygon_in_convex`

Decomposes the `polygon` into multiple convex hulls and returns an array of [`Vector2`](Vector2.md)[].

### ExcludePolygons

```csharp
public static Array<Vector2[]> ExcludePolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB)
```

引擎名：`exclude_polygons`

Mutually excludes common area defined by intersection of `polygonA` and `polygonB` (see `IntersectPolygons()`) and returns an array of excluded polygons. This performs [`Geometry2D.PolyBooleanOperation.Xor`](Geometry2D.md) between polygons. In other words, returns all but common area between polygons.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

### ExcludePolygons

```csharp
public static Array<Vector2[]> ExcludePolygons(Vector2[] polygonA, Vector2[] polygonB)
```

引擎名：`exclude_polygons`

Mutually excludes common area defined by intersection of `polygonA` and `polygonB` (see `IntersectPolygons()`) and returns an array of excluded polygons. This performs [`Geometry2D.PolyBooleanOperation.Xor`](Geometry2D.md) between polygons. In other words, returns all but common area between polygons.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

### GetClosestPointToSegment

```csharp
public static Vector2 GetClosestPointToSegment(Vector2 point, Vector2 s1, Vector2 s2)
```

引擎名：`get_closest_point_to_segment`

Returns the 2D point on the 2D segment (`s1`, `s2`) that is closest to `point`. The returned point will always be inside the specified segment.

### GetClosestPointToSegmentUncapped

```csharp
public static Vector2 GetClosestPointToSegmentUncapped(Vector2 point, Vector2 s1, Vector2 s2)
```

引擎名：`get_closest_point_to_segment_uncapped`

Returns the 2D point on the 2D line defined by (`s1`, `s2`) that is closest to `point`. The returned point can be inside the segment (`s1`, `s2`) or outside of it, i.e. somewhere on the line extending from the segment.

### GetClosestPointsBetweenSegments

```csharp
public static Vector2[] GetClosestPointsBetweenSegments(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
```

引擎名：`get_closest_points_between_segments`

Given the two 2D segments (`p1`, `q1`) and (`p2`, `q2`), finds those two points on the two segments that are closest to each other. Returns a [`Vector2`](Vector2.md)[] that contains this point on (`p1`, `q1`) as well the accompanying point on (`p2`, `q2`).

### IntersectPolygons

```csharp
public static Array<Vector2[]> IntersectPolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB)
```

引擎名：`intersect_polygons`

Intersects `polygonA` with `polygonB` and returns an array of intersected polygons. This performs [`Geometry2D.PolyBooleanOperation.Intersection`](Geometry2D.md) between polygons. In other words, returns common area shared by polygons. Returns an empty array if no intersection occurs.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

### IntersectPolygons

```csharp
public static Array<Vector2[]> IntersectPolygons(Vector2[] polygonA, Vector2[] polygonB)
```

引擎名：`intersect_polygons`

Intersects `polygonA` with `polygonB` and returns an array of intersected polygons. This performs [`Geometry2D.PolyBooleanOperation.Intersection`](Geometry2D.md) between polygons. In other words, returns common area shared by polygons. Returns an empty array if no intersection occurs.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

### IntersectPolylineWithPolygon

```csharp
public static Array<Vector2[]> IntersectPolylineWithPolygon(ReadOnlySpan<Vector2> polyline, ReadOnlySpan<Vector2> polygon)
```

引擎名：`intersect_polyline_with_polygon`

Intersects `polyline` with `polygon` and returns an array of intersected polylines. This performs [`Geometry2D.PolyBooleanOperation.Intersection`](Geometry2D.md) between the polyline and the polygon. This operation can be thought of as chopping a line with a closed shape.

### IntersectPolylineWithPolygon

```csharp
public static Array<Vector2[]> IntersectPolylineWithPolygon(Vector2[] polyline, Vector2[] polygon)
```

引擎名：`intersect_polyline_with_polygon`

Intersects `polyline` with `polygon` and returns an array of intersected polylines. This performs [`Geometry2D.PolyBooleanOperation.Intersection`](Geometry2D.md) between the polyline and the polygon. This operation can be thought of as chopping a line with a closed shape.

### IsPointInCircle

```csharp
public static bool IsPointInCircle(Vector2 point, Vector2 circlePosition, float circleRadius)
```

引擎名：`is_point_in_circle`

Returns `true` if `point` is inside the circle or if it's located exactly *on* the circle's boundary, otherwise returns `false`.

### IsPointInPolygon

```csharp
public static bool IsPointInPolygon(Vector2 point, ReadOnlySpan<Vector2> polygon)
```

引擎名：`is_point_in_polygon`

Returns `true` if `point` is inside `polygon` or if it's located exactly *on* polygon's boundary, otherwise returns `false`.

### IsPointInPolygon

```csharp
public static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
```

引擎名：`is_point_in_polygon`

Returns `true` if `point` is inside `polygon` or if it's located exactly *on* polygon's boundary, otherwise returns `false`.

### IsPolygonClockwise

```csharp
public static bool IsPolygonClockwise(ReadOnlySpan<Vector2> polygon)
```

引擎名：`is_polygon_clockwise`

Returns `true` if `polygon`'s vertices are ordered in clockwise order, otherwise returns `false`.

**Note:** Assumes a Cartesian coordinate system where `+x` is right and `+y` is up. If using screen coordinates (`+y` is down), the result will need to be flipped (i.e. a `true` result will indicate counter-clockwise).

### IsPolygonClockwise

```csharp
public static bool IsPolygonClockwise(Vector2[] polygon)
```

引擎名：`is_polygon_clockwise`

Returns `true` if `polygon`'s vertices are ordered in clockwise order, otherwise returns `false`.

**Note:** Assumes a Cartesian coordinate system where `+x` is right and `+y` is up. If using screen coordinates (`+y` is down), the result will need to be flipped (i.e. a `true` result will indicate counter-clockwise).

### LineIntersectsLine

```csharp
public static Variant LineIntersectsLine(Vector2 fromA, Vector2 dirA, Vector2 fromB, Vector2 dirB)
```

引擎名：`line_intersects_line`

Returns the point of intersection between the two lines (`fromA`, `dirA`) and (`fromB`, `dirB`). Returns a [`Vector2`](Vector2.md), or `null` if the lines are parallel.

`from` and `dir` are *not* endpoints of a line segment or ray but the slope (`dir`) and a known point (`from`) on that line. To get the intersection between two line segments, use `SegmentIntersectsSegment()`.

```csharp
var fromA = Vector2.Zero;
var dirA = Vector2.Right;
var fromB = Vector2.Down;

// Returns new Vector2(1, 0)
Geometry2D.LineIntersectsLine(fromA, dirA, fromB, new Vector2(1, -1));
// Returns new Vector2(-1, 0)
Geometry2D.LineIntersectsLine(fromA, dirA, fromB, new Vector2(-1, -1));
// Returns null
Geometry2D.LineIntersectsLine(fromA, dirA, fromB, Vector2.Right);

```

### MakeAtlas

```csharp
public static Dictionary MakeAtlas(ReadOnlySpan<Vector2> sizes)
```

引擎名：`make_atlas`

Given an array of [`Vector2`](Vector2.md)s representing tiles, builds an atlas. The returned dictionary has two keys: `points` is a [`Vector2`](Vector2.md)[] that specifies the positions of each tile, `size` contains the overall size of the whole atlas as [`Vector2I`](Vector2I.md).

### MakeAtlas

```csharp
public static Dictionary MakeAtlas(Vector2[] sizes)
```

引擎名：`make_atlas`

Given an array of [`Vector2`](Vector2.md)s representing tiles, builds an atlas. The returned dictionary has two keys: `points` is a [`Vector2`](Vector2.md)[] that specifies the positions of each tile, `size` contains the overall size of the whole atlas as [`Vector2I`](Vector2I.md).

### MergePolygons

```csharp
public static Array<Vector2[]> MergePolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB)
```

引擎名：`merge_polygons`

Merges (combines) `polygonA` and `polygonB` and returns an array of merged polygons. This performs [`Geometry2D.PolyBooleanOperation.Union`](Geometry2D.md) between polygons.

The operation may result in an outer polygon (boundary) and multiple inner polygons (holes) produced which could be distinguished by calling `IsPolygonClockwise()`.

### MergePolygons

```csharp
public static Array<Vector2[]> MergePolygons(Vector2[] polygonA, Vector2[] polygonB)
```

引擎名：`merge_polygons`

Merges (combines) `polygonA` and `polygonB` and returns an array of merged polygons. This performs [`Geometry2D.PolyBooleanOperation.Union`](Geometry2D.md) between polygons.

The operation may result in an outer polygon (boundary) and multiple inner polygons (holes) produced which could be distinguished by calling `IsPolygonClockwise()`.

### OffsetPolygon

```csharp
public static Array<Vector2[]> OffsetPolygon(ReadOnlySpan<Vector2> polygon, float delta, Geometry2D.PolyJoinType joinType)
```

引擎名：`offset_polygon`

Inflates or deflates `polygon` by `delta` units (pixels). If `delta` is positive, makes the polygon grow outward. If `delta` is negative, shrinks the polygon inward. Returns an array of polygons because inflating/deflating may result in multiple discrete polygons. Returns an empty array if `delta` is negative and the absolute value of it approximately exceeds the minimum bounding rectangle dimensions of the polygon.

Each polygon's vertices will be rounded as determined by `joinType`.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

**Note:** To translate the polygon's vertices specifically, multiply them to a [`Transform2D`](Transform2D.md):

```csharp
Vector2[] polygon = [new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 100), new Vector2(0, 100)];
var offset = new Vector2(50, 50);
polygon = new Transform2D(0, offset) * polygon;
GD.Print((Variant)polygon); // Prints [(50, 50), (150, 50), (150, 150), (50, 150)]

```

### OffsetPolygon

```csharp
public static Array<Vector2[]> OffsetPolygon(Vector2[] polygon, float delta, Geometry2D.PolyJoinType joinType = Geometry2D.PolyJoinType.Square)
```

引擎名：`offset_polygon`

Inflates or deflates `polygon` by `delta` units (pixels). If `delta` is positive, makes the polygon grow outward. If `delta` is negative, shrinks the polygon inward. Returns an array of polygons because inflating/deflating may result in multiple discrete polygons. Returns an empty array if `delta` is negative and the absolute value of it approximately exceeds the minimum bounding rectangle dimensions of the polygon.

Each polygon's vertices will be rounded as determined by `joinType`.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

**Note:** To translate the polygon's vertices specifically, multiply them to a [`Transform2D`](Transform2D.md):

```csharp
Vector2[] polygon = [new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 100), new Vector2(0, 100)];
var offset = new Vector2(50, 50);
polygon = new Transform2D(0, offset) * polygon;
GD.Print((Variant)polygon); // Prints [(50, 50), (150, 50), (150, 150), (50, 150)]

```

### OffsetPolyline

```csharp
public static Array<Vector2[]> OffsetPolyline(ReadOnlySpan<Vector2> polyline, float delta, Geometry2D.PolyJoinType joinType, Geometry2D.PolyEndType endType)
```

引擎名：`offset_polyline`

Inflates or deflates `polyline` by `delta` units (pixels), producing polygons. If `delta` is positive, makes the polyline grow outward. Returns an array of polygons because inflating/deflating may result in multiple discrete polygons. If `delta` is negative, returns an empty array.

Each polygon's vertices will be rounded as determined by `joinType`.

Each polygon's endpoints will be rounded as determined by `endType`.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

### OffsetPolyline

```csharp
public static Array<Vector2[]> OffsetPolyline(Vector2[] polyline, float delta, Geometry2D.PolyJoinType joinType = Geometry2D.PolyJoinType.Square, Geometry2D.PolyEndType endType = Geometry2D.PolyEndType.Square)
```

引擎名：`offset_polyline`

Inflates or deflates `polyline` by `delta` units (pixels), producing polygons. If `delta` is positive, makes the polyline grow outward. Returns an array of polygons because inflating/deflating may result in multiple discrete polygons. If `delta` is negative, returns an empty array.

Each polygon's vertices will be rounded as determined by `joinType`.

Each polygon's endpoints will be rounded as determined by `endType`.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

### PointIsInsideTriangle

```csharp
public static bool PointIsInsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
```

引擎名：`point_is_inside_triangle`

Returns if `point` is inside the triangle specified by `a`, `b` and `c`.

### SegmentIntersectsCircle

```csharp
public static float SegmentIntersectsCircle(Vector2 segmentFrom, Vector2 segmentTo, Vector2 circlePosition, float circleRadius)
```

引擎名：`segment_intersects_circle`

Given the 2D segment (`segmentFrom`, `segmentTo`), returns the position on the segment (as a number between 0 and 1) at which the segment hits the circle that is located at position `circlePosition` and has radius `circleRadius`. If the segment does not intersect the circle, -1 is returned (this is also the case if the line extending the segment would intersect the circle, but the segment does not).

### SegmentIntersectsSegment

```csharp
public static Variant SegmentIntersectsSegment(Vector2 fromA, Vector2 toA, Vector2 fromB, Vector2 toB)
```

引擎名：`segment_intersects_segment`

Checks if two line segments intersect, with line `a` between `fromA` and `toA` and line `b` between `fromB` and `toB`. If the line segments intersect, the point of intersection is returned as a [`Vector2`](Vector2.md). If no intersection takes place, `null` is returned.

### TriangulateDelaunay

```csharp
public static int[] TriangulateDelaunay(ReadOnlySpan<Vector2> points)
```

引擎名：`triangulate_delaunay`

Triangulates the area specified by discrete set of `points` such that no point is inside the circumcircle of any resulting triangle. Returns a `System.Int32`[] where each triangle consists of three consecutive point indices into `points` (i.e. the returned array will have `n * 3` elements, with `n` being the number of found triangles). If the triangulation did not succeed, an empty `System.Int32`[] is returned.

### TriangulateDelaunay

```csharp
public static int[] TriangulateDelaunay(Vector2[] points)
```

引擎名：`triangulate_delaunay`

Triangulates the area specified by discrete set of `points` such that no point is inside the circumcircle of any resulting triangle. Returns a `System.Int32`[] where each triangle consists of three consecutive point indices into `points` (i.e. the returned array will have `n * 3` elements, with `n` being the number of found triangles). If the triangulation did not succeed, an empty `System.Int32`[] is returned.

### TriangulatePolygon

```csharp
public static int[] TriangulatePolygon(ReadOnlySpan<Vector2> polygon)
```

引擎名：`triangulate_polygon`

Triangulates the polygon specified by the points in `polygon`. Returns a `System.Int32`[] where each triangle consists of three consecutive point indices into `polygon` (i.e. the returned array will have `n * 3` elements, with `n` being the number of found triangles). Output triangles will always be counter clockwise, and the contour will be flipped if it's clockwise. If the triangulation did not succeed, an empty `System.Int32`[] is returned.

### TriangulatePolygon

```csharp
public static int[] TriangulatePolygon(Vector2[] polygon)
```

引擎名：`triangulate_polygon`

Triangulates the polygon specified by the points in `polygon`. Returns a `System.Int32`[] where each triangle consists of three consecutive point indices into `polygon` (i.e. the returned array will have `n * 3` elements, with `n` being the number of found triangles). Output triangles will always be counter clockwise, and the contour will be flipped if it's clockwise. If the triangulation did not succeed, an empty `System.Int32`[] is returned.

## PolyBooleanOperation

命名空间 `Godot`；enum

### 枚举值

- `Union = 0`：Create regions where either subject or clip polygons (or both) are filled.
- `Difference = 1`：Create regions where subject polygons are filled except where clip polygons are filled.
- `Intersection = 2`：Create regions where both subject and clip polygons are filled.
- `Xor = 3`：Create regions where either subject or clip polygons are filled but not where both are filled.

## PolyEndType

命名空间 `Godot`；enum

### 枚举值

- `Polygon = 0`：Endpoints are joined using the [`Geometry2D.PolyJoinType`](Geometry2D.md) value and the path filled as a polygon.
- `Joined = 1`：Endpoints are joined using the [`Geometry2D.PolyJoinType`](Geometry2D.md) value and the path filled as a polyline.
- `Butt = 2`：Endpoints are squared off with no extension.
- `Square = 3`：Endpoints are squared off and extended by `delta` units.
- `Round = 4`：Endpoints are rounded off and extended by `delta` units.

## PolyJoinType

命名空间 `Godot`；enum

### 枚举值

- `Square = 0`：Squaring is applied uniformally at all convex edge joins at `1 * delta`.
- `Round = 1`：While flattened paths can never perfectly trace an arc, they are approximated by a series of arc chords.
- `Miter = 2`：There's a necessary limit to mitered joins since offsetting edges that join at very acute angles will produce excessively long and narrow "spikes".
