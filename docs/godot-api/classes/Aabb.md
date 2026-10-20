<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Aabb

命名空间 `Godot`；struct

Axis-Aligned Bounding Box. AABB consists of a position, a size, and
several utility functions. It is typically used for fast overlap tests.

## 构造函数

### Aabb

```csharp
public Aabb(Vector3 position, Vector3 size)
```

Constructs an `Aabb` from a position and size.

参数：
- `position`：The position.
- `size`：The size, typically positive.

### Aabb

```csharp
public Aabb(Vector3 position, float width, float height, float depth)
```

Constructs an `Aabb` from a `position`,
`width`, `height`, and `depth`.

参数：
- `position`：The position.
- `width`：The width, typically positive.
- `height`：The height, typically positive.
- `depth`：The depth, typically positive.

### Aabb

```csharp
public Aabb(float x, float y, float z, Vector3 size)
```

Constructs an `Aabb` from `x`,
`y`, `z`, and `size`.

参数：
- `x`：The position's X coordinate.
- `y`：The position's Y coordinate.
- `z`：The position's Z coordinate.
- `size`：The size, typically positive.

### Aabb

```csharp
public Aabb(float x, float y, float z, float width, float height, float depth)
```

Constructs an `Aabb` from `x`,
`y`, `z`, `width`,
`height`, and `depth`.

参数：
- `x`：The position's X coordinate.
- `y`：The position's Y coordinate.
- `z`：The position's Z coordinate.
- `width`：The width, typically positive.
- `height`：The height, typically positive.
- `depth`：The depth, typically positive.

## 属性

### End

```csharp
public Vector3 End { get; set; }
```

Ending corner. This is calculated as `Position` plus
`Size`. Setting this value will change the size.

### Position

```csharp
public Vector3 Position { get; set; }
```

Beginning corner. Typically has values lower than `End`.

### Size

```csharp
public Vector3 Size { get; set; }
```

Size from `Position` to `End`. Typically all components are positive.
If the size is negative, you can use `Abs()` to fix it.

### Volume

```csharp
public float Volume { get; }
```

The volume of this `Aabb`.
See also `HasVolume()`.

## 方法

### Abs

```csharp
public Aabb Abs()
```

Returns an `Aabb` with equivalent position and size, modified so that
the most-negative corner is the origin and the size is positive.

返回：The modified `Aabb`.

### Encloses

```csharp
public bool Encloses(Aabb with)
```

Returns `true` if this `Aabb` completely encloses another one.

参数：
- `with`：The other `Aabb` that may be enclosed.

返回：A `bool` for whether or not this `Aabb` encloses `with`.

### Equals

```csharp
public bool Equals(Aabb other)
```

Returns `true` if the AABBs are exactly equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `other`：The other AABB.

返回：Whether or not the AABBs are exactly equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if the AABB is exactly equal
to the given object (`obj`).
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `obj`：The object to compare with.

返回：Whether or not the AABB and the object are equal.

### Expand

```csharp
public Aabb Expand(Vector3 point)
```

Returns this `Aabb` expanded to include a given point.

参数：
- `point`：The point to include.

返回：The expanded `Aabb`.

### GetCenter

```csharp
public Vector3 GetCenter()
```

Returns the center of the `Aabb`, which is equal
to `Position` + (`Size` / 2).

返回：The center.

### GetEndpoint

```csharp
public Vector3 GetEndpoint(int idx)
```

Gets the position of one of the 8 endpoints of the `Aabb`.

参数：
- `idx`：Which endpoint to get.

返回：An endpoint of the `Aabb`.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Aabb`.

返回：A hash code for this AABB.

### GetLongestAxis

```csharp
public Vector3 GetLongestAxis()
```

Returns the normalized longest axis of the `Aabb`.

返回：A vector representing the normalized longest axis of the `Aabb`.

### GetLongestAxisIndex

```csharp
public Vector3.Axis GetLongestAxisIndex()
```

Returns the [`Vector3.Axis`](Vector3.md) index of the longest axis of the `Aabb`.

返回：A [`Vector3.Axis`](Vector3.md) index for which axis is longest.

### GetLongestAxisSize

```csharp
public float GetLongestAxisSize()
```

Returns the scalar length of the longest axis of the `Aabb`.

返回：The scalar length of the longest axis of the `Aabb`.

### GetShortestAxis

```csharp
public Vector3 GetShortestAxis()
```

Returns the normalized shortest axis of the `Aabb`.

返回：A vector representing the normalized shortest axis of the `Aabb`.

### GetShortestAxisIndex

```csharp
public Vector3.Axis GetShortestAxisIndex()
```

Returns the [`Vector3.Axis`](Vector3.md) index of the shortest axis of the `Aabb`.

返回：A [`Vector3.Axis`](Vector3.md) index for which axis is shortest.

### GetShortestAxisSize

```csharp
public float GetShortestAxisSize()
```

Returns the scalar length of the shortest axis of the `Aabb`.

返回：The scalar length of the shortest axis of the `Aabb`.

### GetSupport

```csharp
public Vector3 GetSupport(Vector3 dir)
```

Returns the support point in a given direction.
This is useful for collision detection algorithms.

参数：
- `dir`：The direction to find support for.

返回：A vector representing the support.

### Grow

```csharp
public Aabb Grow(float by)
```

Returns a copy of the `Aabb` grown a given amount of units towards all the sides.

参数：
- `by`：The amount to grow by.

返回：The grown `Aabb`.

### HasPoint

```csharp
public bool HasPoint(Vector3 point)
```

Returns `true` if the `Aabb` contains a point,
or `false` otherwise.

参数：
- `point`：The point to check.

返回：A `bool` for whether or not the `Aabb` contains `point`.

### HasSurface

```csharp
public bool HasSurface()
```

Returns `true` if the `Aabb`
has a surface or a length, and `false`
if the `Aabb` is empty (all components
of `Size` are zero or negative).

返回：A `bool` for whether or not the `Aabb` has surface.

### HasVolume

```csharp
public bool HasVolume()
```

Returns `true` if the `Aabb` has
area, and `false` if the `Aabb`
is linear, empty, or has a negative `Size`.
See also `Volume`.

返回：A `bool` for whether or not the `Aabb` has volume.

### Intersection

```csharp
public Aabb Intersection(Aabb with)
```

Returns the intersection of this `Aabb` and `with`.

参数：
- `with`：The other `Aabb`.

返回：The clipped `Aabb`.

### Intersects

```csharp
public bool Intersects(Aabb with)
```

Returns `true` if the `Aabb` overlaps with `with`
(i.e. they have at least one point in common).

参数：
- `with`：The other `Aabb` to check for intersections with.

返回：A `bool` for whether or not they are intersecting.

### IntersectsPlane

```csharp
public bool IntersectsPlane(Plane plane)
```

Returns `true` if the `Aabb` is on both sides of `plane`.

参数：
- `plane`：The [`Plane`](Plane.md) to check for intersection.

返回：A `bool` for whether or not the `Aabb` intersects the [`Plane`](Plane.md).

### IntersectsSegment

```csharp
public bool IntersectsSegment(Vector3 from, Vector3 to)
```

Returns `true` if the `Aabb` intersects
the line segment between `from` and `to`.

参数：
- `from`：The start of the line segment.
- `to`：The end of the line segment.

返回：A `bool` for whether or not the `Aabb` intersects the line segment.

### IsEqualApprox

```csharp
public bool IsEqualApprox(Aabb other)
```

Returns `true` if this AABB and `other` are approximately equal,
by running [`Vector3.IsEqualApprox`](Vector3.md) on each component.

参数：
- `other`：The other AABB to compare.

返回：Whether or not the AABBs structures are approximately equal.

### IsFinite

```csharp
public bool IsFinite()
```

Returns `true` if this `Aabb` is finite, by calling
[`Mathf.IsFinite`](Mathf.md) on each component.

返回：Whether this vector is finite or not.

### Merge

```csharp
public Aabb Merge(Aabb with)
```

Returns a larger `Aabb` that contains this `Aabb` and `with`.

参数：
- `with`：The other `Aabb`.

返回：The merged `Aabb`.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Aabb` to a string with the given `format`.

返回：A string representation of this AABB.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Aabb` to a string.

返回：A string representation of this AABB.

## 运算符

### op_Equality

```csharp
public static bool op_Equality(Aabb left, Aabb right)
```

Returns `true` if the AABBs are exactly equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left AABB.
- `right`：The right AABB.

返回：Whether or not the AABBs are exactly equal.

### op_Inequality

```csharp
public static bool op_Inequality(Aabb left, Aabb right)
```

Returns `true` if the AABBs are not equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left AABB.
- `right`：The right AABB.

返回：Whether or not the AABBs are not equal.
