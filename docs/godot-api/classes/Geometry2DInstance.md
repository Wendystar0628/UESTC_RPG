<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Geometry2DInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

Provides a set of helper functions to create geometric shapes, compute intersections between shapes, and process various other geometric operations in 2D.

## 方法

### BresenhamLine

```csharp
public Array<Vector2I> BresenhamLine(Vector2I from, Vector2I to)
```

Returns the [Bresenham line](https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm) between the `from` and `to` points. A Bresenham line is a series of pixels that draws a line and is always 1-pixel thick on every row and column of the drawing (never more, never less).

Example code to draw a line between two [`Marker2D`](Marker2D.md) nodes using a series of [`CanvasItem.DrawRect`](CanvasItem.md) calls:

```csharp
func _draw():
for pixel in Geometry2D.bresenham_line($MarkerA.position, $MarkerB.position):
draw_rect(Rect2(pixel, Vector2.ONE), Color.WHITE)

```

### ClipPolygons

```csharp
public Array<Vector2[]> ClipPolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB)
```

Clips `polygonA` against `polygonB` and returns an array of clipped polygons. This performs [`Geometry2D.PolyBooleanOperation.Difference`](Geometry2D.md) between polygons. Returns an empty array if `polygonB` completely overlaps `polygonA`.

If `polygonB` is enclosed by `polygonA`, returns an outer polygon (boundary) and inner polygon (hole) which could be distinguished by calling `IsPolygonClockwise()`.

### ClipPolygons

```csharp
public Array<Vector2[]> ClipPolygons(Vector2[] polygonA, Vector2[] polygonB)
```

Clips `polygonA` against `polygonB` and returns an array of clipped polygons. This performs [`Geometry2D.PolyBooleanOperation.Difference`](Geometry2D.md) between polygons. Returns an empty array if `polygonB` completely overlaps `polygonA`.

If `polygonB` is enclosed by `polygonA`, returns an outer polygon (boundary) and inner polygon (hole) which could be distinguished by calling `IsPolygonClockwise()`.

### ClipPolylineWithPolygon

```csharp
public Array<Vector2[]> ClipPolylineWithPolygon(ReadOnlySpan<Vector2> polyline, ReadOnlySpan<Vector2> polygon)
```

Clips `polyline` against `polygon` and returns an array of clipped polylines. This performs [`Geometry2D.PolyBooleanOperation.Difference`](Geometry2D.md) between the polyline and the polygon. This operation can be thought of as cutting a line with a closed shape.

### ClipPolylineWithPolygon

```csharp
public Array<Vector2[]> ClipPolylineWithPolygon(Vector2[] polyline, Vector2[] polygon)
```

Clips `polyline` against `polygon` and returns an array of clipped polylines. This performs [`Geometry2D.PolyBooleanOperation.Difference`](Geometry2D.md) between the polyline and the polygon. This operation can be thought of as cutting a line with a closed shape.

### ConvexHull

```csharp
public Vector2[] ConvexHull(ReadOnlySpan<Vector2> points)
```

Given an array of [`Vector2`](Vector2.md)s, returns the convex hull as a list of points in counterclockwise order. The last point is the same as the first one.

### ConvexHull

```csharp
public Vector2[] ConvexHull(Vector2[] points)
```

Given an array of [`Vector2`](Vector2.md)s, returns the convex hull as a list of points in counterclockwise order. The last point is the same as the first one.

### DecomposePolygonInConvex

```csharp
public Array<Vector2[]> DecomposePolygonInConvex(ReadOnlySpan<Vector2> polygon)
```

Decomposes the `polygon` into multiple convex hulls and returns an array of [`Vector2`](Vector2.md)[].

### DecomposePolygonInConvex

```csharp
public Array<Vector2[]> DecomposePolygonInConvex(Vector2[] polygon)
```

Decomposes the `polygon` into multiple convex hulls and returns an array of [`Vector2`](Vector2.md)[].

### ExcludePolygons

```csharp
public Array<Vector2[]> ExcludePolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB)
```

Mutually excludes common area defined by intersection of `polygonA` and `polygonB` (see `IntersectPolygons()`) and returns an array of excluded polygons. This performs [`Geometry2D.PolyBooleanOperation.Xor`](Geometry2D.md) between polygons. In other words, returns all but common area between polygons.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

### ExcludePolygons

```csharp
public Array<Vector2[]> ExcludePolygons(Vector2[] polygonA, Vector2[] polygonB)
```

Mutually excludes common area defined by intersection of `polygonA` and `polygonB` (see `IntersectPolygons()`) and returns an array of excluded polygons. This performs [`Geometry2D.PolyBooleanOperation.Xor`](Geometry2D.md) between polygons. In other words, returns all but common area between polygons.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

### GetClosestPointToSegment

```csharp
public Vector2 GetClosestPointToSegment(Vector2 point, Vector2 s1, Vector2 s2)
```

Returns the 2D point on the 2D segment (`s1`, `s2`) that is closest to `point`. The returned point will always be inside the specified segment.

### GetClosestPointToSegmentUncapped

```csharp
public Vector2 GetClosestPointToSegmentUncapped(Vector2 point, Vector2 s1, Vector2 s2)
```

Returns the 2D point on the 2D line defined by (`s1`, `s2`) that is closest to `point`. The returned point can be inside the segment (`s1`, `s2`) or outside of it, i.e. somewhere on the line extending from the segment.

### GetClosestPointsBetweenSegments

```csharp
public Vector2[] GetClosestPointsBetweenSegments(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
```

Given the two 2D segments (`p1`, `q1`) and (`p2`, `q2`), finds those two points on the two segments that are closest to each other. Returns a [`Vector2`](Vector2.md)[] that contains this point on (`p1`, `q1`) as well the accompanying point on (`p2`, `q2`).

### IntersectPolygons

```csharp
public Array<Vector2[]> IntersectPolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB)
```

Intersects `polygonA` with `polygonB` and returns an array of intersected polygons. This performs [`Geometry2D.PolyBooleanOperation.Intersection`](Geometry2D.md) between polygons. In other words, returns common area shared by polygons. Returns an empty array if no intersection occurs.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

### IntersectPolygons

```csharp
public Array<Vector2[]> IntersectPolygons(Vector2[] polygonA, Vector2[] polygonB)
```

Intersects `polygonA` with `polygonB` and returns an array of intersected polygons. This performs [`Geometry2D.PolyBooleanOperation.Intersection`](Geometry2D.md) between polygons. In other words, returns common area shared by polygons. Returns an empty array if no intersection occurs.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

### IntersectPolylineWithPolygon

```csharp
public Array<Vector2[]> IntersectPolylineWithPolygon(ReadOnlySpan<Vector2> polyline, ReadOnlySpan<Vector2> polygon)
```

Intersects `polyline` with `polygon` and returns an array of intersected polylines. This performs [`Geometry2D.PolyBooleanOperation.Intersection`](Geometry2D.md) between the polyline and the polygon. This operation can be thought of as chopping a line with a closed shape.

### IntersectPolylineWithPolygon

```csharp
public Array<Vector2[]> IntersectPolylineWithPolygon(Vector2[] polyline, Vector2[] polygon)
```

Intersects `polyline` with `polygon` and returns an array of intersected polylines. This performs [`Geometry2D.PolyBooleanOperation.Intersection`](Geometry2D.md) between the polyline and the polygon. This operation can be thought of as chopping a line with a closed shape.

### IsPointInCircle

```csharp
public bool IsPointInCircle(Vector2 point, Vector2 circlePosition, float circleRadius)
```

Returns `true` if `point` is inside the circle or if it's located exactly *on* the circle's boundary, otherwise returns `false`.

### IsPointInPolygon

```csharp
public bool IsPointInPolygon(Vector2 point, ReadOnlySpan<Vector2> polygon)
```

Returns `true` if `point` is inside `polygon` or if it's located exactly *on* polygon's boundary, otherwise returns `false`.

### IsPointInPolygon

```csharp
public bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
```

Returns `true` if `point` is inside `polygon` or if it's located exactly *on* polygon's boundary, otherwise returns `false`.

### IsPolygonClockwise

```csharp
public bool IsPolygonClockwise(ReadOnlySpan<Vector2> polygon)
```

Returns `true` if `polygon`'s vertices are ordered in clockwise order, otherwise returns `false`.

**Note:** Assumes a Cartesian coordinate system where `+x` is right and `+y` is up. If using screen coordinates (`+y` is down), the result will need to be flipped (i.e. a `true` result will indicate counter-clockwise).

### IsPolygonClockwise

```csharp
public bool IsPolygonClockwise(Vector2[] polygon)
```

Returns `true` if `polygon`'s vertices are ordered in clockwise order, otherwise returns `false`.

**Note:** Assumes a Cartesian coordinate system where `+x` is right and `+y` is up. If using screen coordinates (`+y` is down), the result will need to be flipped (i.e. a `true` result will indicate counter-clockwise).

### LineIntersectsLine

```csharp
public Variant LineIntersectsLine(Vector2 fromA, Vector2 dirA, Vector2 fromB, Vector2 dirB)
```

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
public Dictionary MakeAtlas(ReadOnlySpan<Vector2> sizes)
```

Given an array of [`Vector2`](Vector2.md)s representing tiles, builds an atlas. The returned dictionary has two keys: `points` is a [`Vector2`](Vector2.md)[] that specifies the positions of each tile, `size` contains the overall size of the whole atlas as [`Vector2I`](Vector2I.md).

### MakeAtlas

```csharp
public Dictionary MakeAtlas(Vector2[] sizes)
```

Given an array of [`Vector2`](Vector2.md)s representing tiles, builds an atlas. The returned dictionary has two keys: `points` is a [`Vector2`](Vector2.md)[] that specifies the positions of each tile, `size` contains the overall size of the whole atlas as [`Vector2I`](Vector2I.md).

### MergePolygons

```csharp
public Array<Vector2[]> MergePolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB)
```

Merges (combines) `polygonA` and `polygonB` and returns an array of merged polygons. This performs [`Geometry2D.PolyBooleanOperation.Union`](Geometry2D.md) between polygons.

The operation may result in an outer polygon (boundary) and multiple inner polygons (holes) produced which could be distinguished by calling `IsPolygonClockwise()`.

### MergePolygons

```csharp
public Array<Vector2[]> MergePolygons(Vector2[] polygonA, Vector2[] polygonB)
```

Merges (combines) `polygonA` and `polygonB` and returns an array of merged polygons. This performs [`Geometry2D.PolyBooleanOperation.Union`](Geometry2D.md) between polygons.

The operation may result in an outer polygon (boundary) and multiple inner polygons (holes) produced which could be distinguished by calling `IsPolygonClockwise()`.

### OffsetPolygon

```csharp
public Array<Vector2[]> OffsetPolygon(ReadOnlySpan<Vector2> polygon, float delta, Geometry2D.PolyJoinType joinType)
```

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
public Array<Vector2[]> OffsetPolygon(Vector2[] polygon, float delta, Geometry2D.PolyJoinType joinType = Geometry2D.PolyJoinType.Square)
```

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
public Array<Vector2[]> OffsetPolyline(ReadOnlySpan<Vector2> polyline, float delta, Geometry2D.PolyJoinType joinType, Geometry2D.PolyEndType endType)
```

Inflates or deflates `polyline` by `delta` units (pixels), producing polygons. If `delta` is positive, makes the polyline grow outward. Returns an array of polygons because inflating/deflating may result in multiple discrete polygons. If `delta` is negative, returns an empty array.

Each polygon's vertices will be rounded as determined by `joinType`.

Each polygon's endpoints will be rounded as determined by `endType`.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

### OffsetPolyline

```csharp
public Array<Vector2[]> OffsetPolyline(Vector2[] polyline, float delta, Geometry2D.PolyJoinType joinType = Geometry2D.PolyJoinType.Square, Geometry2D.PolyEndType endType = Geometry2D.PolyEndType.Square)
```

Inflates or deflates `polyline` by `delta` units (pixels), producing polygons. If `delta` is positive, makes the polyline grow outward. Returns an array of polygons because inflating/deflating may result in multiple discrete polygons. If `delta` is negative, returns an empty array.

Each polygon's vertices will be rounded as determined by `joinType`.

Each polygon's endpoints will be rounded as determined by `endType`.

The operation may result in an outer polygon (boundary) and inner polygon (hole) produced which could be distinguished by calling `IsPolygonClockwise()`.

### PointIsInsideTriangle

```csharp
public bool PointIsInsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
```

Returns if `point` is inside the triangle specified by `a`, `b` and `c`.

### SegmentIntersectsCircle

```csharp
public float SegmentIntersectsCircle(Vector2 segmentFrom, Vector2 segmentTo, Vector2 circlePosition, float circleRadius)
```

Given the 2D segment (`segmentFrom`, `segmentTo`), returns the position on the segment (as a number between 0 and 1) at which the segment hits the circle that is located at position `circlePosition` and has radius `circleRadius`. If the segment does not intersect the circle, -1 is returned (this is also the case if the line extending the segment would intersect the circle, but the segment does not).

### SegmentIntersectsSegment

```csharp
public Variant SegmentIntersectsSegment(Vector2 fromA, Vector2 toA, Vector2 fromB, Vector2 toB)
```

Checks if two line segments intersect, with line `a` between `fromA` and `toA` and line `b` between `fromB` and `toB`. If the line segments intersect, the point of intersection is returned as a [`Vector2`](Vector2.md). If no intersection takes place, `null` is returned.

### TriangulateDelaunay

```csharp
public int[] TriangulateDelaunay(ReadOnlySpan<Vector2> points)
```

Triangulates the area specified by discrete set of `points` such that no point is inside the circumcircle of any resulting triangle. Returns a `System.Int32`[] where each triangle consists of three consecutive point indices into `points` (i.e. the returned array will have `n * 3` elements, with `n` being the number of found triangles). If the triangulation did not succeed, an empty `System.Int32`[] is returned.

### TriangulateDelaunay

```csharp
public int[] TriangulateDelaunay(Vector2[] points)
```

Triangulates the area specified by discrete set of `points` such that no point is inside the circumcircle of any resulting triangle. Returns a `System.Int32`[] where each triangle consists of three consecutive point indices into `points` (i.e. the returned array will have `n * 3` elements, with `n` being the number of found triangles). If the triangulation did not succeed, an empty `System.Int32`[] is returned.

### TriangulatePolygon

```csharp
public int[] TriangulatePolygon(ReadOnlySpan<Vector2> polygon)
```

Triangulates the polygon specified by the points in `polygon`. Returns a `System.Int32`[] where each triangle consists of three consecutive point indices into `polygon` (i.e. the returned array will have `n * 3` elements, with `n` being the number of found triangles). Output triangles will always be counter clockwise, and the contour will be flipped if it's clockwise. If the triangulation did not succeed, an empty `System.Int32`[] is returned.

### TriangulatePolygon

```csharp
public int[] TriangulatePolygon(Vector2[] polygon)
```

Triangulates the polygon specified by the points in `polygon`. Returns a `System.Int32`[] where each triangle consists of three consecutive point indices into `polygon` (i.e. the returned array will have `n * 3` elements, with `n` being the number of found triangles). Output triangles will always be counter clockwise, and the contour will be flipped if it's clockwise. If the triangulation did not succeed, an empty `System.Int32`[] is returned.
