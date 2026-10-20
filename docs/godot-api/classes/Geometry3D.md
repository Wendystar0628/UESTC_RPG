<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Geometry3D

命名空间 `Godot`；static class

Provides a set of helper functions to create geometric shapes, compute intersections between shapes, and process various other geometric operations in 3D.

## 属性

### Singleton

```csharp
public static Geometry3DInstance Singleton { get; }
```

## 方法

### BuildBoxPlanes

```csharp
public static Array<Plane> BuildBoxPlanes(Vector3 extents)
```

引擎名：`build_box_planes`

Returns an array with 6 [`Plane`](Plane.md)s that describe the sides of a box centered at the origin. The box size is defined by `extents`, which represents one (positive) corner of the box (i.e. half its actual size).

### BuildCapsulePlanes

```csharp
public static Array<Plane> BuildCapsulePlanes(float radius, float height, int sides, int lats, Vector3.Axis axis = Vector3.Axis.Z)
```

引擎名：`build_capsule_planes`

Returns an array of [`Plane`](Plane.md)s closely bounding a faceted capsule centered at the origin with radius `radius` and height `height`. The parameter `sides` defines how many planes will be generated for the side part of the capsule, whereas `lats` gives the number of latitudinal steps at the bottom and top of the capsule. The parameter `axis` describes the axis along which the capsule is oriented (0 for X, 1 for Y, 2 for Z).

### BuildCylinderPlanes

```csharp
public static Array<Plane> BuildCylinderPlanes(float radius, float height, int sides, Vector3.Axis axis = Vector3.Axis.Z)
```

引擎名：`build_cylinder_planes`

Returns an array of [`Plane`](Plane.md)s closely bounding a faceted cylinder centered at the origin with radius `radius` and height `height`. The parameter `sides` defines how many planes will be generated for the round part of the cylinder. The parameter `axis` describes the axis along which the cylinder is oriented (0 for X, 1 for Y, 2 for Z).

### ClipPolygon

```csharp
public static Vector3[] ClipPolygon(ReadOnlySpan<Vector3> points, Plane plane)
```

引擎名：`clip_polygon`

Clips the polygon defined by the points in `points` against the `plane` and returns the points of the clipped polygon.

### ClipPolygon

```csharp
public static Vector3[] ClipPolygon(Vector3[] points, Plane plane)
```

引擎名：`clip_polygon`

Clips the polygon defined by the points in `points` against the `plane` and returns the points of the clipped polygon.

### ComputeConvexMeshPoints

```csharp
public static Vector3[] ComputeConvexMeshPoints(Array<Plane> planes)
```

引擎名：`compute_convex_mesh_points`

Calculates and returns all the vertex points of a convex shape defined by an array of `planes`.

### GetClosestPointToSegment

```csharp
public static Vector3 GetClosestPointToSegment(Vector3 point, Vector3 s1, Vector3 s2)
```

引擎名：`get_closest_point_to_segment`

Returns the 3D point on the 3D segment (`s1`, `s2`) that is closest to `point`. The returned point will always be inside the specified segment.

### GetClosestPointToSegmentUncapped

```csharp
public static Vector3 GetClosestPointToSegmentUncapped(Vector3 point, Vector3 s1, Vector3 s2)
```

引擎名：`get_closest_point_to_segment_uncapped`

Returns the 3D point on the 3D line defined by (`s1`, `s2`) that is closest to `point`. The returned point can be inside the segment (`s1`, `s2`) or outside of it, i.e. somewhere on the line extending from the segment.

### GetClosestPointsBetweenSegments

```csharp
public static Vector3[] GetClosestPointsBetweenSegments(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2)
```

引擎名：`get_closest_points_between_segments`

Given the two 3D segments (`p1`, `p2`) and (`q1`, `q2`), finds those two points on the two segments that are closest to each other. Returns a [`Vector3`](Vector3.md)[] that contains this point on (`p1`, `p2`) as well the accompanying point on (`q1`, `q2`).

### GetTriangleBarycentricCoords

```csharp
public static Vector3 GetTriangleBarycentricCoords(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
```

引擎名：`get_triangle_barycentric_coords`

Returns a [`Vector3`](Vector3.md) containing weights based on how close a 3D position (`point`) is to a triangle's different vertices (`a`, `b` and `c`). This is useful for interpolating between the data of different vertices in a triangle. One example use case is using this to smoothly rotate over a mesh instead of relying solely on face normals.

[Here is a more detailed explanation of barycentric coordinates.](https://en.wikipedia.org/wiki/Barycentric_coordinate_system)

### RayIntersectsTriangle

```csharp
public static Variant RayIntersectsTriangle(Vector3 from, Vector3 dir, Vector3 a, Vector3 b, Vector3 c)
```

引擎名：`ray_intersects_triangle`

Tests if the 3D ray starting at `from` with the direction of `dir` intersects the triangle specified by `a`, `b` and `c`. If yes, returns the point of intersection as [`Vector3`](Vector3.md). If no intersection takes place, returns `null`.

### SegmentIntersectsConvex

```csharp
public static Vector3[] SegmentIntersectsConvex(Vector3 from, Vector3 to, Array planes)
```

引擎名：`segment_intersects_convex`

Given a convex hull defined though the [`Plane`](Plane.md)s in the array `planes`, tests if the segment (`from`, `to`) intersects with that hull. If an intersection is found, returns a [`Vector3`](Vector3.md)[] containing the point the intersection and the hull's normal. Otherwise, returns an empty array.

### SegmentIntersectsConvex

```csharp
public static Vector3[] SegmentIntersectsConvex(Vector3 from, Vector3 to, Array<Plane> planes)
```

引擎名：`segment_intersects_convex`

Given a convex hull defined though the [`Plane`](Plane.md)s in the array `planes`, tests if the segment (`from`, `to`) intersects with that hull. If an intersection is found, returns a [`Vector3`](Vector3.md)[] containing the point the intersection and the hull's normal. Otherwise, returns an empty array.

### SegmentIntersectsCylinder

```csharp
public static Vector3[] SegmentIntersectsCylinder(Vector3 from, Vector3 to, float height, float radius)
```

引擎名：`segment_intersects_cylinder`

Checks if the segment (`from`, `to`) intersects the cylinder with height `height` that is centered at the origin and has radius `radius`. If no, returns an empty [`Vector3`](Vector3.md)[]. If an intersection takes place, the returned array contains the point of intersection and the cylinder's normal at the point of intersection.

### SegmentIntersectsSphere

```csharp
public static Vector3[] SegmentIntersectsSphere(Vector3 from, Vector3 to, Vector3 spherePosition, float sphereRadius)
```

引擎名：`segment_intersects_sphere`

Checks if the segment (`from`, `to`) intersects the sphere that is located at `spherePosition` and has radius `sphereRadius`. If no, returns an empty [`Vector3`](Vector3.md)[]. If yes, returns a [`Vector3`](Vector3.md)[] containing the point of intersection and the sphere's normal at the point of intersection.

### SegmentIntersectsTriangle

```csharp
public static Variant SegmentIntersectsTriangle(Vector3 from, Vector3 to, Vector3 a, Vector3 b, Vector3 c)
```

引擎名：`segment_intersects_triangle`

Tests if the segment (`from`, `to`) intersects the triangle `a`, `b`, `c`. If yes, returns the point of intersection as [`Vector3`](Vector3.md). If no intersection takes place, returns `null`.

### TetrahedralizeDelaunay

```csharp
public static int[] TetrahedralizeDelaunay(ReadOnlySpan<Vector3> points)
```

引擎名：`tetrahedralize_delaunay`

Tetrahedralizes the volume specified by a discrete set of `points` in 3D space, ensuring that no point lies within the circumsphere of any resulting tetrahedron. The method returns a `System.Int32`[] where each tetrahedron consists of four consecutive point indices into the `points` array (resulting in an array with `n * 4` elements, where `n` is the number of tetrahedra found). If the tetrahedralization is unsuccessful, an empty `System.Int32`[] is returned.

### TetrahedralizeDelaunay

```csharp
public static int[] TetrahedralizeDelaunay(Vector3[] points)
```

引擎名：`tetrahedralize_delaunay`

Tetrahedralizes the volume specified by a discrete set of `points` in 3D space, ensuring that no point lies within the circumsphere of any resulting tetrahedron. The method returns a `System.Int32`[] where each tetrahedron consists of four consecutive point indices into the `points` array (resulting in an array with `n * 4` elements, where `n` is the number of tetrahedra found). If the tetrahedralization is unsuccessful, an empty `System.Int32`[] is returned.
