<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Vector2I

命名空间 `Godot`；struct

2-element structure that can be used to represent 2D grid coordinates or pairs of integers.

## 构造函数

### Vector2I

```csharp
public Vector2I(int x, int y)
```

Constructs a new `Vector2I` with the given components.

参数：
- `x`：The vector's X component.
- `y`：The vector's Y component.

## 属性

### Down

```csharp
public static Vector2I Down { get; }
```

Down unit vector. Y is down in 2D, so this vector points +Y.

### Item

```csharp
public int this[int index] { get; set; }
```

Access vector components using their index.

### Left

```csharp
public static Vector2I Left { get; }
```

Left unit vector. Represents the direction of left.

### MaxValue

```csharp
public static Vector2I MaxValue { get; }
```

Max vector, a vector with all components equal to `System.Int32.MaxValue`. Can be used as an integer equivalent of [`Vector2.Inf`](Vector2.md).

### MinValue

```csharp
public static Vector2I MinValue { get; }
```

Min vector, a vector with all components equal to `System.Int32.MinValue`. Can be used as a negative integer equivalent of [`Vector2.Inf`](Vector2.md).

### One

```csharp
public static Vector2I One { get; }
```

One vector, a vector with all components set to `1`.

### Right

```csharp
public static Vector2I Right { get; }
```

Right unit vector. Represents the direction of right.

### Up

```csharp
public static Vector2I Up { get; }
```

Up unit vector. Y is down in 2D, so this vector points -Y.

### Zero

```csharp
public static Vector2I Zero { get; }
```

Zero vector, a vector with all components set to `0`.

## 方法

### Abs

```csharp
public Vector2I Abs()
```

Returns a new vector with all components in absolute values (i.e. positive).

返回：A vector with [`Mathf.Abs`](Mathf.md) called on each component.

### Aspect

```csharp
public float Aspect()
```

Returns the aspect ratio of this vector, the ratio of `X` to `Y`.

返回：The `X` component divided by the `Y` component.

### Clamp

```csharp
public Vector2I Clamp(Vector2I min, Vector2I max)
```

Returns a new vector with all components clamped between the
components of `min` and `max` using
[`Mathf.Clamp`](Mathf.md).

参数：
- `min`：The vector with minimum allowed values.
- `max`：The vector with maximum allowed values.

返回：The vector with all components clamped.

### Clamp

```csharp
public Vector2I Clamp(int min, int max)
```

Returns a new vector with all components clamped between the
`min` and `max` using
[`Mathf.Clamp`](Mathf.md).

参数：
- `min`：The minimum allowed value.
- `max`：The maximum allowed value.

返回：The vector with all components clamped.

### Deconstruct

```csharp
public void Deconstruct(out int x, out int y)
```

Helper method for deconstruction into a tuple.

### DistanceSquaredTo

```csharp
public int DistanceSquaredTo(Vector2I to)
```

Returns the squared distance between this vector and `to`.
This method runs faster than `DistanceTo()`, so prefer it if
you need to compare vectors or need the squared distance for some formula.

参数：
- `to`：The other vector to use.

返回：The squared distance between the two vectors.

### DistanceTo

```csharp
public float DistanceTo(Vector2I to)
```

Returns the distance between this vector and `to`.

参数：
- `to`：The other vector to use.

返回：The distance between the two vectors.

### Equals

```csharp
public bool Equals(Vector2I other)
```

Returns `true` if the vectors are equal.

参数：
- `other`：The other vector.

返回：Whether or not the vectors are equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if the vector is equal
to the given object (`obj`).

参数：
- `obj`：The object to compare with.

返回：Whether or not the vector and the object are equal.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Vector2I`.

返回：A hash code for this vector.

### Length

```csharp
public float Length()
```

Returns the length (magnitude) of this vector.

返回：The length of this vector.

### LengthSquared

```csharp
public int LengthSquared()
```

Returns the squared length (squared magnitude) of this vector.
This method runs faster than `Length()`, so prefer it if
you need to compare vectors or need the squared length for some formula.

返回：The squared length of this vector.

### Max

```csharp
public Vector2I Max(Vector2I with)
```

Returns the result of the component-wise maximum between
this vector and `with`.
Equivalent to `new Vector2I(Mathf.Max(X, with.X), Mathf.Max(Y, with.Y))`.

参数：
- `with`：The other vector to use.

返回：The resulting maximum vector.

### Max

```csharp
public Vector2I Max(int with)
```

Returns the result of the component-wise maximum between
this vector and `with`.
Equivalent to `new Vector2I(Mathf.Max(X, with), Mathf.Max(Y, with))`.

参数：
- `with`：The other value to use.

返回：The resulting maximum vector.

### MaxAxisIndex

```csharp
public Vector2I.Axis MaxAxisIndex()
```

Returns the axis of the vector's highest value. See [`Vector2I.Axis`](Vector2I.md).
If both components are equal, this method returns [`Vector2I.Axis.X`](Vector2I.md).

返回：The index of the highest axis.

### Min

```csharp
public Vector2I Min(Vector2I with)
```

Returns the result of the component-wise minimum between
this vector and `with`.
Equivalent to `new Vector2I(Mathf.Min(X, with.X), Mathf.Min(Y, with.Y))`.

参数：
- `with`：The other vector to use.

返回：The resulting minimum vector.

### Min

```csharp
public Vector2I Min(int with)
```

Returns the result of the component-wise minimum between
this vector and `with`.
Equivalent to `new Vector2I(Mathf.Min(X, with), Mathf.Min(Y, with))`.

参数：
- `with`：The other value to use.

返回：The resulting minimum vector.

### MinAxisIndex

```csharp
public Vector2I.Axis MinAxisIndex()
```

Returns the axis of the vector's lowest value. See [`Vector2I.Axis`](Vector2I.md).
If both components are equal, this method returns [`Vector2I.Axis.Y`](Vector2I.md).

返回：The index of the lowest axis.

### Sign

```csharp
public Vector2I Sign()
```

Returns a vector with each component set to one or negative one, depending
on the signs of this vector's components, or zero if the component is zero,
by calling [`Mathf.Sign`](Mathf.md) on each component.

返回：A vector with all components as either `1`, `-1`, or `0`.

### Snapped

```csharp
public Vector2I Snapped(Vector2I step)
```

Returns a new vector with each component snapped to the closest multiple of the corresponding component in `step`.

参数：
- `step`：A vector value representing the step size to snap to.

返回：The snapped vector.

### Snapped

```csharp
public Vector2I Snapped(int step)
```

Returns a new vector with each component snapped to the closest multiple of `step`.

参数：
- `step`：The step size to snap to.

返回：The snapped vector.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Vector2I` to a string with the given `format`.

返回：A string representation of this vector.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Vector2I` to a string.

返回：A string representation of this vector.

## 运算符

### op_Addition

```csharp
public static Vector2I op_Addition(Vector2I left, Vector2I right)
```

Adds each component of the `Vector2I`
with the components of the given `Vector2I`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The added vector.

### op_Division

```csharp
public static Vector2I op_Division(Vector2I vec, Vector2I divisorv)
```

Divides each component of the `Vector2I`
by the components of the given `Vector2I`.

参数：
- `vec`：The dividend vector.
- `divisorv`：The divisor vector.

返回：The divided vector.

### op_Division

```csharp
public static Vector2I op_Division(Vector2I vec, int divisor)
```

Divides each component of the `Vector2I`
by the given `int`.

参数：
- `vec`：The dividend vector.
- `divisor`：The divisor value.

返回：The divided vector.

### op_Equality

```csharp
public static bool op_Equality(Vector2I left, Vector2I right)
```

Returns `true` if the vectors are equal.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the vectors are equal.

### op_Explicit

```csharp
public static Vector2I op_Explicit(Vector2 value)
```

Converts a [`Vector2`](Vector2.md) to a `Vector2I` by truncating
components' fractional parts (rounding towards zero). For a different
behavior consider passing the result of [`Vector2.Ceil`](Vector2.md),
[`Vector2.Floor`](Vector2.md) or [`Vector2.Round`](Vector2.md) to this conversion operator instead.

参数：
- `value`：The vector to convert.

### op_GreaterThan

```csharp
public static bool op_GreaterThan(Vector2I left, Vector2I right)
```

Compares two `Vector2I` vectors by first checking if
the X value of the `left` vector is greater than
the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is greater than the right.

### op_GreaterThanOrEqual

```csharp
public static bool op_GreaterThanOrEqual(Vector2I left, Vector2I right)
```

Compares two `Vector2I` vectors by first checking if
the X value of the `left` vector is greater than
or equal to the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is greater than or equal to the right.

### op_Implicit

```csharp
public static Vector2 op_Implicit(Vector2I value)
```

Converts this `Vector2I` to a [`Vector2`](Vector2.md).

参数：
- `value`：The vector to convert.

### op_Inequality

```csharp
public static bool op_Inequality(Vector2I left, Vector2I right)
```

Returns `true` if the vectors are not equal.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the vectors are not equal.

### op_LessThan

```csharp
public static bool op_LessThan(Vector2I left, Vector2I right)
```

Compares two `Vector2I` vectors by first checking if
the X value of the `left` vector is less than
the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is less than the right.

### op_LessThanOrEqual

```csharp
public static bool op_LessThanOrEqual(Vector2I left, Vector2I right)
```

Compares two `Vector2I` vectors by first checking if
the X value of the `left` vector is less than
or equal to the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is less than or equal to the right.

### op_Modulus

```csharp
public static Vector2I op_Modulus(Vector2I vec, Vector2I divisorv)
```

Gets the remainder of each component of the `Vector2I`
with the components of the given `Vector2I`.
This operation uses truncated division, which is often not desired
as it does not work well with negative numbers.
Consider using [`Mathf.PosMod`](Mathf.md) instead
if you want to handle negative numbers.

参数：
- `vec`：The dividend vector.
- `divisorv`：The divisor vector.

返回：The remainder vector.

### op_Modulus

```csharp
public static Vector2I op_Modulus(Vector2I vec, int divisor)
```

Gets the remainder of each component of the `Vector2I`
with the components of the given `int`.
This operation uses truncated division, which is often not desired
as it does not work well with negative numbers.
Consider using [`Mathf.PosMod`](Mathf.md) instead
if you want to handle negative numbers.

参数：
- `vec`：The dividend vector.
- `divisor`：The divisor value.

返回：The remainder vector.

### op_Multiply

```csharp
public static Vector2I op_Multiply(Vector2I left, Vector2I right)
```

Multiplies each component of the `Vector2I`
by the components of the given `Vector2I`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The multiplied vector.

### op_Multiply

```csharp
public static Vector2I op_Multiply(Vector2I vec, int scale)
```

Multiplies each component of the `Vector2I`
by the given `int`.

参数：
- `vec`：The vector to multiply.
- `scale`：The scale to multiply by.

返回：The multiplied vector.

### op_Multiply

```csharp
public static Vector2I op_Multiply(int scale, Vector2I vec)
```

Multiplies each component of the `Vector2I`
by the given `int`.

参数：
- `scale`：The scale to multiply by.
- `vec`：The vector to multiply.

返回：The multiplied vector.

### op_Subtraction

```csharp
public static Vector2I op_Subtraction(Vector2I left, Vector2I right)
```

Subtracts each component of the `Vector2I`
by the components of the given `Vector2I`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The subtracted vector.

### op_UnaryNegation

```csharp
public static Vector2I op_UnaryNegation(Vector2I vec)
```

Returns the negative value of the `Vector2I`.
This is the same as writing `new Vector2I(-v.X, -v.Y)`.
This operation flips the direction of the vector while
keeping the same magnitude.

参数：
- `vec`：The vector to negate/flip.

返回：The negated/flipped vector.

## 字段

### X

```csharp
public int X
```

The vector's X component. Also accessible by using the index position `[0]`.

### Y

```csharp
public int Y
```

The vector's Y component. Also accessible by using the index position `[1]`.

## Axis

命名空间 `Godot`；enum

Enumerated index values for the axes.
Returned by [`Vector2I.MaxAxisIndex`](Vector2I.md) and [`Vector2I.MinAxisIndex`](Vector2I.md).

### 枚举值

- `X = 0`：The vector's X axis.
- `Y = 1`：The vector's Y axis.
