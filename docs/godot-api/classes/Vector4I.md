<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Vector4I

命名空间 `Godot`；struct

4-element structure that can be used to represent 4D grid coordinates or sets of integers.

## 构造函数

### Vector4I

```csharp
public Vector4I(int x, int y, int z, int w)
```

Constructs a new `Vector4I` with the given components.

参数：
- `x`：The vector's X component.
- `y`：The vector's Y component.
- `z`：The vector's Z component.
- `w`：The vector's W component.

## 属性

### Item

```csharp
public int this[int index] { get; set; }
```

Access vector components using their `index`.

### MaxValue

```csharp
public static Vector4I MaxValue { get; }
```

Max vector, a vector with all components equal to `System.Int32.MaxValue`. Can be used as an integer equivalent of [`Vector4.Inf`](Vector4.md).

### MinValue

```csharp
public static Vector4I MinValue { get; }
```

Min vector, a vector with all components equal to `System.Int32.MinValue`. Can be used as a negative integer equivalent of [`Vector4.Inf`](Vector4.md).

### One

```csharp
public static Vector4I One { get; }
```

One vector, a vector with all components set to `1`.

### Zero

```csharp
public static Vector4I Zero { get; }
```

Zero vector, a vector with all components set to `0`.

## 方法

### Abs

```csharp
public Vector4I Abs()
```

Returns a new vector with all components in absolute values (i.e. positive).

返回：A vector with [`Mathf.Abs`](Mathf.md) called on each component.

### Clamp

```csharp
public Vector4I Clamp(Vector4I min, Vector4I max)
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
public Vector4I Clamp(int min, int max)
```

Returns a new vector with all components clamped between
`min` and `max` using
[`Mathf.Clamp`](Mathf.md).

参数：
- `min`：The minimum allowed value.
- `max`：The maximum allowed value.

返回：The vector with all components clamped.

### Deconstruct

```csharp
public void Deconstruct(out int x, out int y, out int z, out int w)
```

Helper method for deconstruction into a tuple.

### DistanceSquaredTo

```csharp
public int DistanceSquaredTo(Vector4I to)
```

Returns the squared distance between this vector and `to`.
This method runs faster than `DistanceTo()`, so prefer it if
you need to compare vectors or need the squared distance for some formula.

参数：
- `to`：The other vector to use.

返回：The squared distance between the two vectors.

### DistanceTo

```csharp
public float DistanceTo(Vector4I to)
```

Returns the distance between this vector and `to`.

参数：
- `to`：The other vector to use.

返回：The distance between the two vectors.

### Equals

```csharp
public bool Equals(Vector4I other)
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

Serves as the hash function for `Vector4I`.

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
public Vector4I Max(Vector4I with)
```

Returns the result of the component-wise maximum between
this vector and `with`.
Equivalent to `new Vector4I(Mathf.Max(X, with.X), Mathf.Max(Y, with.Y), Mathf.Max(Z, with.Z), Mathf.Max(W, with.W))`.

参数：
- `with`：The other vector to use.

返回：The resulting maximum vector.

### Max

```csharp
public Vector4I Max(int with)
```

Returns the result of the component-wise maximum between
this vector and `with`.
Equivalent to `new Vector4I(Mathf.Max(X, with), Mathf.Max(Y, with), Mathf.Max(Z, with), Mathf.Max(W, with))`.

参数：
- `with`：The other value to use.

返回：The resulting maximum vector.

### MaxAxisIndex

```csharp
public Vector4I.Axis MaxAxisIndex()
```

Returns the axis of the vector's highest value. See [`Vector4I.Axis`](Vector4I.md).
If all components are equal, this method returns [`Vector4I.Axis.X`](Vector4I.md).

返回：The index of the highest axis.

### Min

```csharp
public Vector4I Min(Vector4I with)
```

Returns the result of the component-wise minimum between
this vector and `with`.
Equivalent to `new Vector4I(Mathf.Min(X, with.X), Mathf.Min(Y, with.Y), Mathf.Min(Z, with.Z), Mathf.Min(W, with.W))`.

参数：
- `with`：The other vector to use.

返回：The resulting minimum vector.

### Min

```csharp
public Vector4I Min(int with)
```

Returns the result of the component-wise minimum between
this vector and `with`.
Equivalent to `new Vector4I(Mathf.Min(X, with), Mathf.Min(Y, with), Mathf.Min(Z, with), Mathf.Min(W, with))`.

参数：
- `with`：The other value to use.

返回：The resulting minimum vector.

### MinAxisIndex

```csharp
public Vector4I.Axis MinAxisIndex()
```

Returns the axis of the vector's lowest value. See [`Vector4I.Axis`](Vector4I.md).
If all components are equal, this method returns [`Vector4I.Axis.W`](Vector4I.md).

返回：The index of the lowest axis.

### Sign

```csharp
public Vector4I Sign()
```

Returns a vector with each component set to one or negative one, depending
on the signs of this vector's components, or zero if the component is zero,
by calling [`Mathf.Sign`](Mathf.md) on each component.

返回：A vector with all components as either `1`, `-1`, or `0`.

### Snapped

```csharp
public Vector4I Snapped(Vector4I step)
```

Returns a new vector with each component snapped to the closest multiple of the corresponding component in `step`.

参数：
- `step`：A vector value representing the step size to snap to.

返回：The snapped vector.

### Snapped

```csharp
public Vector4I Snapped(int step)
```

Returns a new vector with each component snapped to the closest multiple of `step`.

参数：
- `step`：The step size to snap to.

返回：The snapped vector.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Vector4I` to a string with the given `format`.

返回：A string representation of this vector.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Vector4I` to a string.

返回：A string representation of this vector.

## 运算符

### op_Addition

```csharp
public static Vector4I op_Addition(Vector4I left, Vector4I right)
```

Adds each component of the `Vector4I`
with the components of the given `Vector4I`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The added vector.

### op_Division

```csharp
public static Vector4I op_Division(Vector4I vec, Vector4I divisorv)
```

Divides each component of the `Vector4I`
by the components of the given `Vector4I`.

参数：
- `vec`：The dividend vector.
- `divisorv`：The divisor vector.

返回：The divided vector.

### op_Division

```csharp
public static Vector4I op_Division(Vector4I vec, int divisor)
```

Divides each component of the `Vector4I`
by the given `int`.

参数：
- `vec`：The dividend vector.
- `divisor`：The divisor value.

返回：The divided vector.

### op_Equality

```csharp
public static bool op_Equality(Vector4I left, Vector4I right)
```

Returns `true` if the vectors are equal.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the vectors are equal.

### op_Explicit

```csharp
public static Vector4I op_Explicit(Vector4 value)
```

Converts a [`Vector4`](Vector4.md) to a `Vector4I` by truncating
components' fractional parts (rounding towards zero). For a different
behavior consider passing the result of [`Vector4.Ceil`](Vector4.md),
[`Vector4.Floor`](Vector4.md) or [`Vector4.Round`](Vector4.md) to this conversion operator instead.

参数：
- `value`：The vector to convert.

### op_GreaterThan

```csharp
public static bool op_GreaterThan(Vector4I left, Vector4I right)
```

Compares two `Vector4I` vectors by first checking if
the X value of the `left` vector is greater than
the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y, Z and finally W values of the two vectors.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is greater than the right.

### op_GreaterThanOrEqual

```csharp
public static bool op_GreaterThanOrEqual(Vector4I left, Vector4I right)
```

Compares two `Vector4I` vectors by first checking if
the X value of the `left` vector is greater than
or equal to the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y, Z and finally W values of the two vectors.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is greater than or equal to the right.

### op_Implicit

```csharp
public static Vector4 op_Implicit(Vector4I value)
```

Converts this `Vector4I` to a [`Vector4`](Vector4.md).

参数：
- `value`：The vector to convert.

### op_Inequality

```csharp
public static bool op_Inequality(Vector4I left, Vector4I right)
```

Returns `true` if the vectors are not equal.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the vectors are not equal.

### op_LessThan

```csharp
public static bool op_LessThan(Vector4I left, Vector4I right)
```

Compares two `Vector4I` vectors by first checking if
the X value of the `left` vector is less than
the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y, Z and finally W values of the two vectors.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is less than the right.

### op_LessThanOrEqual

```csharp
public static bool op_LessThanOrEqual(Vector4I left, Vector4I right)
```

Compares two `Vector4I` vectors by first checking if
the X value of the `left` vector is less than
or equal to the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y, Z and finally W values of the two vectors.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is less than or equal to the right.

### op_Modulus

```csharp
public static Vector4I op_Modulus(Vector4I vec, Vector4I divisorv)
```

Gets the remainder of each component of the `Vector4I`
with the components of the given `Vector4I`.
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
public static Vector4I op_Modulus(Vector4I vec, int divisor)
```

Gets the remainder of each component of the `Vector4I`
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
public static Vector4I op_Multiply(Vector4I left, Vector4I right)
```

Multiplies each component of the `Vector4I`
by the components of the given `Vector4I`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The multiplied vector.

### op_Multiply

```csharp
public static Vector4I op_Multiply(Vector4I vec, int scale)
```

Multiplies each component of the `Vector4I`
by the given `int`.

参数：
- `vec`：The vector to multiply.
- `scale`：The scale to multiply by.

返回：The multiplied vector.

### op_Multiply

```csharp
public static Vector4I op_Multiply(int scale, Vector4I vec)
```

Multiplies each component of the `Vector4I`
by the given `int`.

参数：
- `scale`：The scale to multiply by.
- `vec`：The vector to multiply.

返回：The multiplied vector.

### op_Subtraction

```csharp
public static Vector4I op_Subtraction(Vector4I left, Vector4I right)
```

Subtracts each component of the `Vector4I`
by the components of the given `Vector4I`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The subtracted vector.

### op_UnaryNegation

```csharp
public static Vector4I op_UnaryNegation(Vector4I vec)
```

Returns the negative value of the `Vector4I`.
This is the same as writing `new Vector4I(-v.X, -v.Y, -v.Z, -v.W)`.
This operation flips the direction of the vector while
keeping the same magnitude.

参数：
- `vec`：The vector to negate/flip.

返回：The negated/flipped vector.

## 字段

### W

```csharp
public int W
```

The vector's W component. Also accessible by using the index position `[3]`.

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

### Z

```csharp
public int Z
```

The vector's Z component. Also accessible by using the index position `[2]`.

## Axis

命名空间 `Godot`；enum

Enumerated index values for the axes.
Returned by [`Vector4I.MaxAxisIndex`](Vector4I.md) and [`Vector4I.MinAxisIndex`](Vector4I.md).

### 枚举值

- `X = 0`：The vector's X axis.
- `Y = 1`：The vector's Y axis.
- `Z = 2`：The vector's Z axis.
- `W = 3`：The vector's W axis.
