<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Vector4

命名空间 `Godot`；struct

4-element structure that can be used to represent positions in 4D space or any other pair of numeric values.

## 构造函数

### Vector4

```csharp
public Vector4(float x, float y, float z, float w)
```

Constructs a new `Vector4` with the given components.

参数：
- `x`：The vector's X component.
- `y`：The vector's Y component.
- `z`：The vector's Z component.
- `w`：The vector's W component.

## 属性

### Inf

```csharp
public static Vector4 Inf { get; }
```

Infinity vector, a vector with all components set to [`Mathf.Inf`](Mathf.md).

### Item

```csharp
public float this[int index] { get; set; }
```

Access vector components using their index.

### One

```csharp
public static Vector4 One { get; }
```

One vector, a vector with all components set to `1`.

### Zero

```csharp
public static Vector4 Zero { get; }
```

Zero vector, a vector with all components set to `0`.

## 方法

### Abs

```csharp
public Vector4 Abs()
```

引擎名：`abs`

Returns a new vector with all components in absolute values (i.e. positive).

返回：A vector with [`Mathf.Abs`](Mathf.md) called on each component.

### Ceil

```csharp
public Vector4 Ceil()
```

引擎名：`ceil`

Returns a new vector with all components rounded up (towards positive infinity).

返回：A vector with [`Mathf.Ceil`](Mathf.md) called on each component.

### Clamp

```csharp
public Vector4 Clamp(Vector4 min, Vector4 max)
```

引擎名：`clamp`

Returns a new vector with all components clamped between the
components of `min` and `max` using
[`Mathf.Clamp`](Mathf.md).

参数：
- `min`：The vector with minimum allowed values.
- `max`：The vector with maximum allowed values.

返回：The vector with all components clamped.

### Clamp

```csharp
public Vector4 Clamp(float min, float max)
```

引擎名：`clamp`

Returns a new vector with all components clamped between the
`min` and `max` using
[`Mathf.Clamp`](Mathf.md).

参数：
- `min`：The minimum allowed value.
- `max`：The maximum allowed value.

返回：The vector with all components clamped.

### CubicInterpolate

```csharp
public Vector4 CubicInterpolate(Vector4 b, Vector4 preA, Vector4 postB, float weight)
```

引擎名：`cubic_interpolate`

Performs a cubic interpolation between vectors `preA`, this vector,
`b`, and `postB`, by the given amount `weight`.

参数：
- `b`：The destination vector.
- `preA`：A vector before this vector.
- `postB`：A vector after `b`.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The interpolated vector.

### CubicInterpolateInTime

```csharp
public Vector4 CubicInterpolateInTime(Vector4 b, Vector4 preA, Vector4 postB, float weight, float t, float preAT, float postBT)
```

引擎名：`cubic_interpolate_in_time`

Performs a cubic interpolation between vectors `preA`, this vector,
`b`, and `postB`, by the given amount `weight`.
It can perform smoother interpolation than `CubicInterpolate()`
by the time values.

参数：
- `b`：The destination vector.
- `preA`：A vector before this vector.
- `postB`：A vector after `b`.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The interpolated vector.

### Deconstruct

```csharp
public void Deconstruct(out float x, out float y, out float z, out float w)
```

Helper method for deconstruction into a tuple.

### DirectionTo

```csharp
public Vector4 DirectionTo(Vector4 to)
```

引擎名：`direction_to`

Returns the normalized vector pointing from this vector to `to`.

参数：
- `to`：The other vector to point towards.

返回：The direction from this vector to `to`.

### DistanceSquaredTo

```csharp
public float DistanceSquaredTo(Vector4 to)
```

引擎名：`distance_squared_to`

Returns the squared distance between this vector and `to`.
This method runs faster than `DistanceTo()`, so prefer it if
you need to compare vectors or need the squared distance for some formula.

参数：
- `to`：The other vector to use.

返回：The squared distance between the two vectors.

### DistanceTo

```csharp
public float DistanceTo(Vector4 to)
```

引擎名：`distance_to`

Returns the distance between this vector and `to`.

参数：
- `to`：The other vector to use.

返回：The distance between the two vectors.

### Dot

```csharp
public float Dot(Vector4 with)
```

引擎名：`dot`

Returns the dot product of this vector and `with`.

参数：
- `with`：The other vector to use.

返回：The dot product of the two vectors.

### Equals

```csharp
public bool Equals(Vector4 other)
```

Returns `true` if the vectors are exactly equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `other`：The other vector.

返回：Whether or not the vectors are exactly equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if the vector is exactly equal
to the given object (`obj`).
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `obj`：The object to compare with.

返回：Whether or not the vector and the object are equal.

### Floor

```csharp
public Vector4 Floor()
```

引擎名：`floor`

Returns a new vector with all components rounded down (towards negative infinity).

返回：A vector with [`Mathf.Floor`](Mathf.md) called on each component.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Vector4`.

返回：A hash code for this vector.

### Inverse

```csharp
public Vector4 Inverse()
```

引擎名：`inverse`

Returns the inverse of this vector. This is the same as `new Vector4(1 / v.X, 1 / v.Y, 1 / v.Z, 1 / v.W)`.

返回：The inverse of this vector.

### IsEqualApprox

```csharp
public bool IsEqualApprox(Vector4 other)
```

引擎名：`is_equal_approx`

Returns `true` if this vector and `other` are approximately equal,
by running [`Mathf.IsEqualApprox`](Mathf.md) on each component.

参数：
- `other`：The other vector to compare.

返回：Whether or not the vectors are approximately equal.

### IsFinite

```csharp
public bool IsFinite()
```

引擎名：`is_finite`

Returns `true` if this vector is finite, by calling
[`Mathf.IsFinite`](Mathf.md) on each component.

返回：Whether this vector is finite or not.

### IsNormalized

```csharp
public bool IsNormalized()
```

引擎名：`is_normalized`

Returns `true` if the vector is normalized, and `false` otherwise.

返回：A `bool` indicating whether or not the vector is normalized.

### IsZeroApprox

```csharp
public bool IsZeroApprox()
```

引擎名：`is_zero_approx`

Returns `true` if this vector's values are approximately zero,
by running [`Mathf.IsZeroApprox`](Mathf.md) on each component.
This method is faster than using `IsEqualApprox()` with one value
as a zero vector.

返回：Whether or not the vector is approximately zero.

### Length

```csharp
public float Length()
```

引擎名：`length`

Returns the length (magnitude) of this vector.

返回：The length of this vector.

### LengthSquared

```csharp
public float LengthSquared()
```

引擎名：`length_squared`

Returns the squared length (squared magnitude) of this vector.
This method runs faster than `Length()`, so prefer it if
you need to compare vectors or need the squared length for some formula.

返回：The squared length of this vector.

### Lerp

```csharp
public Vector4 Lerp(Vector4 to, float weight)
```

引擎名：`lerp`

Returns the result of the linear interpolation between
this vector and `to` by amount `weight`.

参数：
- `to`：The destination vector for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting vector of the interpolation.

### Max

```csharp
public Vector4 Max(Vector4 with)
```

引擎名：`max`

Returns the result of the component-wise maximum between
this vector and `with`.
Equivalent to `new Vector4(Mathf.Max(X, with.X), Mathf.Max(Y, with.Y), Mathf.Max(Z, with.Z), Mathf.Max(W, with.W))`.

参数：
- `with`：The other vector to use.

返回：The resulting maximum vector.

### Max

```csharp
public Vector4 Max(float with)
```

引擎名：`max`

Returns the result of the component-wise maximum between
this vector and `with`.
Equivalent to `new Vector4(Mathf.Max(X, with), Mathf.Max(Y, with), Mathf.Max(Z, with), Mathf.Max(W, with))`.

参数：
- `with`：The other value to use.

返回：The resulting maximum vector.

### MaxAxisIndex

```csharp
public Vector4.Axis MaxAxisIndex()
```

引擎名：`max_axis_index`

Returns the axis of the vector's highest value. See [`Vector4.Axis`](Vector4.md).
If all components are equal, this method returns [`Vector4.Axis.X`](Vector4.md).

返回：The index of the highest axis.

### Min

```csharp
public Vector4 Min(Vector4 with)
```

引擎名：`min`

Returns the result of the component-wise minimum between
this vector and `with`.
Equivalent to `new Vector4(Mathf.Min(X, with.X), Mathf.Min(Y, with.Y), Mathf.Min(Z, with.Z), Mathf.Min(W, with.W))`.

参数：
- `with`：The other vector to use.

返回：The resulting minimum vector.

### Min

```csharp
public Vector4 Min(float with)
```

引擎名：`min`

Returns the result of the component-wise minimum between
this vector and `with`.
Equivalent to `new Vector4(Mathf.Min(X, with), Mathf.Min(Y, with), Mathf.Min(Z, with), Mathf.Min(W, with))`.

参数：
- `with`：The other value to use.

返回：The resulting minimum vector.

### MinAxisIndex

```csharp
public Vector4.Axis MinAxisIndex()
```

引擎名：`min_axis_index`

Returns the axis of the vector's lowest value. See [`Vector4.Axis`](Vector4.md).
If all components are equal, this method returns [`Vector4.Axis.W`](Vector4.md).

返回：The index of the lowest axis.

### Normalized

```csharp
public Vector4 Normalized()
```

引擎名：`normalized`

Returns the vector scaled to unit length. Equivalent to `v / v.Length()`.

返回：A normalized version of the vector.

### PosMod

```csharp
public Vector4 PosMod(Vector4 modv)
```

Returns a vector composed of the [`Mathf.PosMod`](Mathf.md) of this vector's components
and `modv`'s components.

参数：
- `modv`：A vector representing the divisors of the operation.

返回：A vector with each component [`Mathf.PosMod`](Mathf.md) by `modv`'s components.

### PosMod

```csharp
public Vector4 PosMod(float mod)
```

Returns a vector composed of the [`Mathf.PosMod`](Mathf.md) of this vector's components
and `mod`.

参数：
- `mod`：A value representing the divisor of the operation.

返回：A vector with each component [`Mathf.PosMod`](Mathf.md) by `mod`.

### Round

```csharp
public Vector4 Round()
```

引擎名：`round`

Returns this vector with all components rounded to the nearest integer,
with halfway cases rounded towards the nearest multiple of two.

返回：The rounded vector.

### Sign

```csharp
public Vector4 Sign()
```

引擎名：`sign`

Returns a vector with each component set to one or negative one, depending
on the signs of this vector's components, or zero if the component is zero,
by calling [`Mathf.Sign`](Mathf.md) on each component.

返回：A vector with all components as either `1`, `-1`, or `0`.

### Snapped

```csharp
public Vector4 Snapped(Vector4 step)
```

引擎名：`snapped`

Returns a new vector with each component snapped to the nearest multiple of the corresponding component in `step`.
This can also be used to round to an arbitrary number of decimals.

参数：
- `step`：A vector value representing the step size to snap to.

返回：The snapped vector.

### Snapped

```csharp
public Vector4 Snapped(float step)
```

引擎名：`snapped`

Returns a new vector with each component snapped to the nearest multiple of `step`.
This can also be used to round to an arbitrary number of decimals.

参数：
- `step`：The step size to snap to.

返回：The snapped vector.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Vector4` to a string with the given `format`.

返回：A string representation of this vector.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Vector4` to a string.

返回：A string representation of this vector.

## 运算符

### op_Addition

```csharp
public static Vector4 op_Addition(Vector4 left, Vector4 right)
```

Adds each component of the `Vector4`
with the components of the given `Vector4`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The added vector.

### op_Division

```csharp
public static Vector4 op_Division(Vector4 vec, Vector4 divisorv)
```

Divides each component of the `Vector4`
by the components of the given `Vector4`.

参数：
- `vec`：The dividend vector.
- `divisorv`：The divisor vector.

返回：The divided vector.

### op_Division

```csharp
public static Vector4 op_Division(Vector4 vec, float divisor)
```

Divides each component of the `Vector4`
by the given `System.Single`.

参数：
- `vec`：The dividend vector.
- `divisor`：The divisor value.

返回：The divided vector.

### op_Equality

```csharp
public static bool op_Equality(Vector4 left, Vector4 right)
```

Returns `true` if the vectors are exactly equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the vectors are exactly equal.

### op_GreaterThan

```csharp
public static bool op_GreaterThan(Vector4 left, Vector4 right)
```

Compares two `Vector4` vectors by first checking if
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
public static bool op_GreaterThanOrEqual(Vector4 left, Vector4 right)
```

Compares two `Vector4` vectors by first checking if
the X value of the `left` vector is greater than
or equal to the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y, Z and finally W values of the two vectors.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is greater than or equal to the right.

### op_Inequality

```csharp
public static bool op_Inequality(Vector4 left, Vector4 right)
```

Returns `true` if the vectors are not equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the vectors are not equal.

### op_LessThan

```csharp
public static bool op_LessThan(Vector4 left, Vector4 right)
```

Compares two `Vector4` vectors by first checking if
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
public static bool op_LessThanOrEqual(Vector4 left, Vector4 right)
```

Compares two `Vector4` vectors by first checking if
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
public static Vector4 op_Modulus(Vector4 vec, Vector4 divisorv)
```

Gets the remainder of each component of the `Vector4`
with the components of the given `Vector4`.
This operation uses truncated division, which is often not desired
as it does not work well with negative numbers.
Consider using `PosMod()` instead
if you want to handle negative numbers.

参数：
- `vec`：The dividend vector.
- `divisorv`：The divisor vector.

返回：The remainder vector.

### op_Modulus

```csharp
public static Vector4 op_Modulus(Vector4 vec, float divisor)
```

Gets the remainder of each component of the `Vector4`
with the components of the given `System.Single`.
This operation uses truncated division, which is often not desired
as it does not work well with negative numbers.
Consider using `PosMod()` instead
if you want to handle negative numbers.

参数：
- `vec`：The dividend vector.
- `divisor`：The divisor value.

返回：The remainder vector.

### op_Multiply

```csharp
public static Vector4 op_Multiply(Vector4 left, Vector4 right)
```

Multiplies each component of the `Vector4`
by the components of the given `Vector4`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The multiplied vector.

### op_Multiply

```csharp
public static Vector4 op_Multiply(Vector4 vec, float scale)
```

Multiplies each component of the `Vector4`
by the given `System.Single`.

参数：
- `vec`：The vector to multiply.
- `scale`：The scale to multiply by.

返回：The multiplied vector.

### op_Multiply

```csharp
public static Vector4 op_Multiply(float scale, Vector4 vec)
```

Multiplies each component of the `Vector4`
by the given `System.Single`.

参数：
- `scale`：The scale to multiply by.
- `vec`：The vector to multiply.

返回：The multiplied vector.

### op_Subtraction

```csharp
public static Vector4 op_Subtraction(Vector4 left, Vector4 right)
```

Subtracts each component of the `Vector4`
by the components of the given `Vector4`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The subtracted vector.

### op_UnaryNegation

```csharp
public static Vector4 op_UnaryNegation(Vector4 vec)
```

Returns the negative value of the `Vector4`.
This is the same as writing `new Vector4(-v.X, -v.Y, -v.Z, -v.W)`.
This operation flips the direction of the vector while
keeping the same magnitude.
With floats, the number zero can be either positive or negative.

参数：
- `vec`：The vector to negate/flip.

返回：The negated/flipped vector.

## 字段

### W

```csharp
public float W
```

The vector's W component. Also accessible by using the index position `[3]`.

### X

```csharp
public float X
```

The vector's X component. Also accessible by using the index position `[0]`.

### Y

```csharp
public float Y
```

The vector's Y component. Also accessible by using the index position `[1]`.

### Z

```csharp
public float Z
```

The vector's Z component. Also accessible by using the index position `[2]`.

## Axis

命名空间 `Godot`；enum

Enumerated index values for the axes.
Returned by [`Vector4.MaxAxisIndex`](Vector4.md) and [`Vector4.MinAxisIndex`](Vector4.md).

### 枚举值

- `X = 0`：The vector's X axis.
- `Y = 1`：The vector's Y axis.
- `Z = 2`：The vector's Z axis.
- `W = 3`：The vector's W axis.
