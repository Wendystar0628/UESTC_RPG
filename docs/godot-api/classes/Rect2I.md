<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Rect2I

命名空间 `Godot`；struct

2D axis-aligned bounding box using integers. Rect2I consists of a position, a size, and
several utility functions. It is typically used for fast overlap tests.

## 构造函数

### Rect2I

```csharp
public Rect2I(Vector2I position, Vector2I size)
```

Constructs a `Rect2I` from a position and size.

参数：
- `position`：The position.
- `size`：The size.

### Rect2I

```csharp
public Rect2I(Vector2I position, int width, int height)
```

Constructs a `Rect2I` from a position, width, and height.

参数：
- `position`：The position.
- `width`：The width.
- `height`：The height.

### Rect2I

```csharp
public Rect2I(int x, int y, Vector2I size)
```

Constructs a `Rect2I` from x, y, and size.

参数：
- `x`：The position's X coordinate.
- `y`：The position's Y coordinate.
- `size`：The size.

### Rect2I

```csharp
public Rect2I(int x, int y, int width, int height)
```

Constructs a `Rect2I` from x, y, width, and height.

参数：
- `x`：The position's X coordinate.
- `y`：The position's Y coordinate.
- `width`：The width.
- `height`：The height.

## 属性

### Area

```csharp
public int Area { get; }
```

The area of this `Rect2I`.
See also `HasArea()`.

### End

```csharp
public Vector2I End { get; set; }
```

Ending corner. This is calculated as `Position` plus `Size`.
Setting this value will change the size.

### Position

```csharp
public Vector2I Position { get; set; }
```

Beginning corner. Typically has values lower than `End`.

### Size

```csharp
public Vector2I Size { get; set; }
```

Size from `Position` to `End`. Typically all components are positive.
If the size is negative, you can use `Abs()` to fix it.

## 方法

### Abs

```csharp
public Rect2I Abs()
```

Returns a `Rect2I` with equivalent position and size, modified so that
the top-left corner is the origin and width and height are positive.

返回：The modified `Rect2I`.

### Encloses

```csharp
public bool Encloses(Rect2I b)
```

Returns `true` if this `Rect2I` completely encloses another one.

参数：
- `b`：The other `Rect2I` that may be enclosed.

返回：A `bool` for whether or not this `Rect2I` encloses `b`.

### Equals

```csharp
public bool Equals(Rect2I other)
```

Returns `true` if this rect and `other` are equal.

参数：
- `other`：The other rect to compare.

返回：Whether or not the rects are equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if this rect and `obj` are equal.

参数：
- `obj`：The other object to compare.

返回：Whether or not the rect and the other object are equal.

### Expand

```csharp
public Rect2I Expand(Vector2I to)
```

Returns this `Rect2I` expanded to include a given point.

参数：
- `to`：The point to include.

返回：The expanded `Rect2I`.

### GetCenter

```csharp
public Vector2I GetCenter()
```

Returns the center of the `Rect2I`, which is equal
to `Position` + (`Size` / 2).
If `Size` is an odd number, the returned center
value will be rounded towards `Position`.

返回：The center.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Rect2I`.

返回：A hash code for this rect.

### Grow

```csharp
public Rect2I Grow(int by)
```

Returns a copy of the `Rect2I` grown by the specified amount
on all sides.

参数：
- `by`：The amount to grow by.

返回：The grown `Rect2I`.

### GrowIndividual

```csharp
public Rect2I GrowIndividual(int left, int top, int right, int bottom)
```

Returns a copy of the `Rect2I` grown by the specified amount
on each side individually.

参数：
- `left`：The amount to grow by on the left side.
- `top`：The amount to grow by on the top side.
- `right`：The amount to grow by on the right side.
- `bottom`：The amount to grow by on the bottom side.

返回：The grown `Rect2I`.

### GrowSide

```csharp
public Rect2I GrowSide(Side side, int by)
```

Returns a copy of the `Rect2I` grown by the specified amount
on the specified [`Side`](Side.md).

参数：
- `side`：The side to grow.
- `by`：The amount to grow by.

返回：The grown `Rect2I`.

### HasArea

```csharp
public bool HasArea()
```

Returns `true` if the `Rect2I` has
area, and `false` if the `Rect2I`
is linear, empty, or has a negative `Size`.
See also `Area`.

返回：A `bool` for whether or not the `Rect2I` has area.

### HasPoint

```csharp
public bool HasPoint(Vector2I point)
```

Returns `true` if the `Rect2I` contains a point,
or `false` otherwise.

参数：
- `point`：The point to check.

返回：A `bool` for whether or not the `Rect2I` contains `point`.

### Intersection

```csharp
public Rect2I Intersection(Rect2I b)
```

Returns the intersection of this `Rect2I` and `b`.
If the rectangles do not intersect, an empty `Rect2I` is returned.

参数：
- `b`：The other `Rect2I`.

返回：The intersection of this `Rect2I` and `b`,
or an empty `Rect2I` if they do not intersect.

### Intersects

```csharp
public bool Intersects(Rect2I b)
```

Returns `true` if the `Rect2I` overlaps with `b`
(i.e. they have at least one point in common).

参数：
- `b`：The other `Rect2I` to check for intersections with.

返回：A `bool` for whether or not they are intersecting.

### Merge

```csharp
public Rect2I Merge(Rect2I b)
```

Returns a larger `Rect2I` that contains this `Rect2I` and `b`.

参数：
- `b`：The other `Rect2I`.

返回：The merged `Rect2I`.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Rect2I` to a string with the given `format`.

返回：A string representation of this rect.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Rect2I` to a string.

返回：A string representation of this rect.

## 运算符

### op_Equality

```csharp
public static bool op_Equality(Rect2I left, Rect2I right)
```

Returns `true` if the
`Rect2I`s are exactly equal.

参数：
- `left`：The left rect.
- `right`：The right rect.

返回：Whether or not the rects are equal.

### op_Explicit

```csharp
public static Rect2I op_Explicit(Rect2 value)
```

Converts a [`Rect2`](Rect2.md) to a `Rect2I`.

参数：
- `value`：The rect to convert.

### op_Implicit

```csharp
public static Rect2 op_Implicit(Rect2I value)
```

Converts this `Rect2I` to a [`Rect2`](Rect2.md).

参数：
- `value`：The rect to convert.

### op_Inequality

```csharp
public static bool op_Inequality(Rect2I left, Rect2I right)
```

Returns `true` if the
`Rect2I`s are not equal.

参数：
- `left`：The left rect.
- `right`：The right rect.

返回：Whether or not the rects are not equal.
