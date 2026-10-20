<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Plane

命名空间 `Godot`；struct

Plane represents a normalized plane equation.
"Over" or "Above" the plane is considered the side of
the plane towards where the normal is pointing.

## 构造函数

### Plane

```csharp
public Plane(Vector3 normal)
```

Constructs a `Plane` from a `normal` vector.
The plane will intersect the origin.

参数：
- `normal`：The normal of the plane, must be a unit vector.

### Plane

```csharp
public Plane(Vector3 normal, Vector3 point)
```

Constructs a `Plane` from a `normal` vector and
a `point` on the plane.

参数：
- `normal`：The normal of the plane, must be a unit vector.
- `point`：The point on the plane.

### Plane

```csharp
public Plane(Vector3 normal, float d)
```

Constructs a `Plane` from a `normal` vector and
the plane's distance to the origin `d`.

参数：
- `normal`：The normal of the plane, must be a unit vector.
- `d`：The plane's distance from the origin. This value is typically non-negative.

### Plane

```csharp
public Plane(Vector3 v1, Vector3 v2, Vector3 v3)
```

Constructs a `Plane` from the three points, given in clockwise order.

参数：
- `v1`：The first point.
- `v2`：The second point.
- `v3`：The third point.

### Plane

```csharp
public Plane(float a, float b, float c, float d)
```

Constructs a `Plane` from four values.
`a`, `b` and `c` become the
components of the resulting plane's `Normal` vector.
`d` becomes the plane's distance from the origin.

参数：
- `a`：The X component of the plane's normal vector.
- `b`：The Y component of the plane's normal vector.
- `c`：The Z component of the plane's normal vector.
- `d`：The plane's distance from the origin. This value is typically non-negative.

## 属性

### D

```csharp
public float D { get; set; }
```

引擎名：`d`

The distance from the origin to the plane (in the direction of
`Normal`). This value is typically non-negative.
In the scalar equation of the plane `ax + by + cz = d`,
this is `d`, while the `(a, b, c)` coordinates are represented
by the `Normal` property.

### Normal

```csharp
public Vector3 Normal { get; set; }
```

引擎名：`normal`

The normal of the plane, which must be a unit vector.
In the scalar equation of the plane `ax + by + cz = d`, this is
the vector `(a, b, c)`, where `d` is the `D` property.

### PlaneXY

```csharp
public static Plane PlaneXY { get; }
```

A `Plane` that extends in the X and Y axes (normal vector points +Z).

### PlaneXZ

```csharp
public static Plane PlaneXZ { get; }
```

A `Plane` that extends in the X and Z axes (normal vector points +Y).

### PlaneYZ

```csharp
public static Plane PlaneYZ { get; }
```

A `Plane` that extends in the Y and Z axes (normal vector points +X).

### X

```csharp
public float X { get; set; }
```

引擎名：`x`

The X component of the plane's normal vector.

### Y

```csharp
public float Y { get; set; }
```

引擎名：`y`

The Y component of the plane's normal vector.

### Z

```csharp
public float Z { get; set; }
```

引擎名：`z`

The Z component of the plane's normal vector.

## 方法

### DistanceTo

```csharp
public float DistanceTo(Vector3 point)
```

引擎名：`distance_to`

Returns the shortest distance from this plane to the position `point`.

参数：
- `point`：The position to use for the calculation.

返回：The shortest distance.

### Equals

```csharp
public bool Equals(Plane other)
```

Returns `true` if this plane and `other` are equal.

参数：
- `other`：The other plane to compare.

返回：Whether or not the planes are exactly equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if this plane and `obj` are equal.

参数：
- `obj`：The other object to compare.

返回：Whether or not the plane and the other object are exactly equal.

### GetCenter

```csharp
public Vector3 GetCenter()
```

引擎名：`get_center`

Returns the center of the plane, the point on the plane closest to the origin.
The point where the normal line going through the origin intersects the plane.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Plane`.

返回：A hash code for this plane.

### HasPoint

```csharp
public bool HasPoint(Vector3 point, float tolerance = 1E-06f)
```

引擎名：`has_point`

Returns `true` if point is inside the plane.
Comparison uses a custom minimum tolerance threshold.

参数：
- `point`：The point to check.
- `tolerance`：The tolerance threshold.

返回：A `bool` for whether or not the plane has the point.

### Intersect3

```csharp
public Vector3? Intersect3(Plane b, Plane c)
```

Returns the intersection point of the three planes: `b`, `c`,
and this plane. If no intersection is found, `null` is returned.

参数：
- `b`：One of the three planes to use in the calculation.
- `c`：One of the three planes to use in the calculation.

返回：The intersection, or `null` if none is found.

### IntersectsRay

```csharp
public Vector3? IntersectsRay(Vector3 from, Vector3 dir)
```

引擎名：`intersects_ray`

Returns the intersection point of a ray consisting of the position `from`
and the direction normal `dir` with this plane.
If no intersection is found, `null` is returned.

参数：
- `from`：The start of the ray.
- `dir`：The direction of the ray, normalized.

返回：The intersection, or `null` if none is found.

### IntersectsSegment

```csharp
public Vector3? IntersectsSegment(Vector3 begin, Vector3 end)
```

引擎名：`intersects_segment`

Returns the intersection point of a line segment from
position `begin` to position `end` with this plane.
If no intersection is found, `null` is returned.

参数：
- `begin`：The start of the line segment.
- `end`：The end of the line segment.

返回：The intersection, or `null` if none is found.

### IsEqualApprox

```csharp
public bool IsEqualApprox(Plane other)
```

引擎名：`is_equal_approx`

Returns `true` if this plane and `other` are
approximately equal, by running [`Mathf.IsEqualApprox`](Mathf.md) on each component.

参数：
- `other`：The other plane to compare.

返回：Whether or not the planes are approximately equal.

### IsFinite

```csharp
public bool IsFinite()
```

引擎名：`is_finite`

Returns `true` if this plane is finite, by calling
[`Mathf.IsFinite`](Mathf.md) on each component.

返回：Whether this vector is finite or not.

### IsPointOver

```csharp
public bool IsPointOver(Vector3 point)
```

引擎名：`is_point_over`

Returns `true` if `point` is located above the plane.

参数：
- `point`：The point to check.

返回：A `bool` for whether or not the point is above the plane.

### Normalized

```csharp
public Plane Normalized()
```

引擎名：`normalized`

Returns the plane scaled to unit length.

返回：A normalized version of the plane.

### Project

```csharp
public Vector3 Project(Vector3 point)
```

引擎名：`project`

Returns the orthogonal projection of `point` into the plane.

参数：
- `point`：The point to project.

返回：The projected point.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Plane` to a string with the given `format`.

返回：A string representation of this plane.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Plane` to a string.

返回：A string representation of this plane.

## 运算符

### op_Equality

```csharp
public static bool op_Equality(Plane left, Plane right)
```

Returns `true` if the
`Plane`s are exactly equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left rect.
- `right`：The right rect.

返回：Whether or not the planes are exactly equal.

### op_Inequality

```csharp
public static bool op_Inequality(Plane left, Plane right)
```

Returns `true` if the
`Plane`s are not equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left rect.
- `right`：The right rect.

返回：Whether or not the planes are not equal.

### op_UnaryNegation

```csharp
public static Plane op_UnaryNegation(Plane plane)
```

Returns the negative value of the `Plane`.
This is the same as writing `new Plane(-p.Normal, -p.D)`.
This operation flips the direction of the normal vector and
also flips the distance value, resulting in a Plane that is
in the same place, but facing the opposite direction.

参数：
- `plane`：The plane to negate/flip.

返回：The negated/flipped plane.
