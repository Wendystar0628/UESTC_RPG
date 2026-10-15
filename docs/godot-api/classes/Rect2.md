<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Rect2

命名空间 `Godot`；struct

2D axis-aligned bounding box. Rect2 consists of a position, a size, and
several utility functions. It is typically used for fast overlap tests.

## 构造函数

### Rect2

```csharp
public Rect2(Vector2 position, Vector2 size)
```

Constructs a `Rect2` from a position and size.

参数：
- `position`：The position.
- `size`：The size.

### Rect2

```csharp
public Rect2(Vector2 position, float width, float height)
```

Constructs a `Rect2` from a position, width, and height.

参数：
- `position`：The position.
- `width`：The width.
- `height`：The height.

### Rect2

```csharp
public Rect2(float x, float y, Vector2 size)
```

Constructs a `Rect2` from x, y, and size.

参数：
- `x`：The position's X coordinate.
- `y`：The position's Y coordinate.
- `size`：The size.

### Rect2

```csharp
public Rect2(float x, float y, float width, float height)
```

Constructs a `Rect2` from x, y, width, and height.

参数：
- `x`：The position's X coordinate.
- `y`：The position's Y coordinate.
- `width`：The width.
- `height`：The height.

## 属性

### Area

```csharp
public float Area { get; }
```

The area of this `Rect2`.
See also `HasArea()`.

### End

```csharp
public Vector2 End { get; set; }
```

引擎名：`end`

Ending corner. This is calculated as `Position` plus `Size`.
Setting this value will change the size.

### Position

```csharp
public Vector2 Position { get; set; }
```

引擎名：`position`

Beginning corner. Typically has values lower than `End`.

### Size

```csharp
public Vector2 Size { get; set; }
```

引擎名：`size`

Size from `Position` to `End`. Typically all components are positive.
If the size is negative, you can use `Abs()` to fix it.

## 方法

### Abs

```csharp
public Rect2 Abs()
```

引擎名：`abs`

Returns a `Rect2` with equivalent position and size, modified so that
the top-left corner is the origin and width and height are positive.

返回：The modified `Rect2`.

### Encloses

```csharp
public bool Encloses(Rect2 b)
```

引擎名：`encloses`

Returns `true` if this `Rect2` completely encloses another one.

参数：
- `b`：The other `Rect2` that may be enclosed.

返回：A `bool` for whether or not this `Rect2` encloses `b`.

### Equals

```csharp
public bool Equals(Rect2 other)
```

Returns `true` if this rect and `other` are equal.

参数：
- `other`：The other rect to compare.

返回：Whether or not the rects are exactly equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if this rect and `obj` are equal.

参数：
- `obj`：The other object to compare.

返回：Whether or not the rect and the other object are exactly equal.

### Expand

```csharp
public Rect2 Expand(Vector2 to)
```

引擎名：`expand`

Returns this `Rect2` expanded to include a given point.

参数：
- `to`：The point to include.

返回：The expanded `Rect2`.

### GetCenter

```csharp
public Vector2 GetCenter()
```

引擎名：`get_center`

Returns the center of the `Rect2`, which is equal
to `Position` + (`Size` / 2).

返回：The center.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Rect2`.

返回：A hash code for this rect.

### GetSupport

```csharp
public Vector2 GetSupport(Vector2 direction)
```

引擎名：`get_support`

Returns the support point in a given direction.
This is useful for collision detection algorithms.

参数：
- `direction`：The direction to find support for.

返回：A vector representing the support.

### Grow

```csharp
public Rect2 Grow(float by)
```

引擎名：`grow`

Returns a copy of the `Rect2` grown by the specified amount
on all sides.

参数：
- `by`：The amount to grow by.

返回：The grown `Rect2`.

### GrowIndividual

```csharp
public Rect2 GrowIndividual(float left, float top, float right, float bottom)
```

引擎名：`grow_individual`

Returns a copy of the `Rect2` grown by the specified amount
on each side individually.

参数：
- `left`：The amount to grow by on the left side.
- `top`：The amount to grow by on the top side.
- `right`：The amount to grow by on the right side.
- `bottom`：The amount to grow by on the bottom side.

返回：The grown `Rect2`.

### GrowSide

```csharp
public Rect2 GrowSide(Side side, float by)
```

引擎名：`grow_side`

Returns a copy of the `Rect2` grown by the specified amount
on the specified [`Side`](Side.md).

参数：
- `side`：The side to grow.
- `by`：The amount to grow by.

返回：The grown `Rect2`.

### HasArea

```csharp
public bool HasArea()
```

引擎名：`has_area`

Returns `true` if the `Rect2` has
area, and `false` if the `Rect2`
is linear, empty, or has a negative `Size`.
See also `Area`.

返回：A `bool` for whether or not the `Rect2` has area.

### HasPoint

```csharp
public bool HasPoint(Vector2 point)
```

引擎名：`has_point`

Returns `true` if the `Rect2` contains a point,
or `false` otherwise.

参数：
- `point`：The point to check.

返回：A `bool` for whether or not the `Rect2` contains `point`.

### Intersection

```csharp
public Rect2 Intersection(Rect2 b)
```

引擎名：`intersection`

Returns the intersection of this `Rect2` and `b`.
If the rectangles do not intersect, an empty `Rect2` is returned.

参数：
- `b`：The other `Rect2`.

返回：The intersection of this `Rect2` and `b`,
or an empty `Rect2` if they do not intersect.

### Intersects

```csharp
public bool Intersects(Rect2 b, bool includeBorders = false)
```

引擎名：`intersects`

Returns `true` if the `Rect2` overlaps with `b`
(i.e. they have at least one point in common).

If `includeBorders` is `true`,
they will also be considered overlapping if their borders touch,
even without intersection.

参数：
- `b`：The other `Rect2` to check for intersections with.
- `includeBorders`：Whether or not to consider borders.

返回：A `bool` for whether or not they are intersecting.

### IsEqualApprox

```csharp
public bool IsEqualApprox(Rect2 other)
```

引擎名：`is_equal_approx`

Returns `true` if this rect and `other` are approximately equal,
by running [`Vector2.IsEqualApprox`](Vector2.md) on each component.

参数：
- `other`：The other rect to compare.

返回：Whether or not the rects are approximately equal.

### IsFinite

```csharp
public bool IsFinite()
```

引擎名：`is_finite`

Returns `true` if this `Rect2` is finite, by calling
[`Mathf.IsFinite`](Mathf.md) on each component.

返回：Whether this vector is finite or not.

### Merge

```csharp
public Rect2 Merge(Rect2 b)
```

引擎名：`merge`

Returns a larger `Rect2` that contains this `Rect2` and `b`.

参数：
- `b`：The other `Rect2`.

返回：The merged `Rect2`.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Rect2` to a string with the given `format`.

返回：A string representation of this rect.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Rect2` to a string.

返回：A string representation of this rect.

## 运算符

### op_Equality

```csharp
public static bool op_Equality(Rect2 left, Rect2 right)
```

Returns `true` if the
`Rect2`s are exactly equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left rect.
- `right`：The right rect.

返回：Whether or not the rects are exactly equal.

### op_Inequality

```csharp
public static bool op_Inequality(Rect2 left, Rect2 right)
```

Returns `true` if the
`Rect2`s are not equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left rect.
- `right`：The right rect.

返回：Whether or not the rects are not equal.
