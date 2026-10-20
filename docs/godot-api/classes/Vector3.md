<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Vector3

命名空间 `Godot`；struct

3-element structure that can be used to represent positions in 3D space or any other pair of numeric values.

## 构造函数

### Vector3

```csharp
public Vector3(float x, float y, float z)
```

Constructs a new `Vector3` with the given components.

参数：
- `x`：The vector's X component.
- `y`：The vector's Y component.
- `z`：The vector's Z component.

## 属性

### Back

```csharp
public static Vector3 Back { get; }
```

Back unit vector. Represents the local direction of back,
and the global direction of south.

### Down

```csharp
public static Vector3 Down { get; }
```

Down unit vector.

### Forward

```csharp
public static Vector3 Forward { get; }
```

Forward unit vector. Represents the local direction of forward,
and the global direction of north.

### Inf

```csharp
public static Vector3 Inf { get; }
```

Infinity vector, a vector with all components set to [`Mathf.Inf`](Mathf.md).

### Item

```csharp
public float this[int index] { get; set; }
```

Access vector components using their index.

### Left

```csharp
public static Vector3 Left { get; }
```

Left unit vector. Represents the local direction of left,
and the global direction of west.

### ModelBottom

```csharp
public static Vector3 ModelBottom { get; }
```

Unit vector pointing towards the bottom side (down) of imported 3D assets.

### ModelFront

```csharp
public static Vector3 ModelFront { get; }
```

Unit vector pointing towards the front side (facing forward) of imported 3D assets.

### ModelLeft

```csharp
public static Vector3 ModelLeft { get; }
```

Unit vector pointing towards the left side of imported 3D assets.

### ModelRear

```csharp
public static Vector3 ModelRear { get; }
```

Unit vector pointing towards the rear side (back) of imported 3D assets.

### ModelRight

```csharp
public static Vector3 ModelRight { get; }
```

Unit vector pointing towards the right side of imported 3D assets.

### ModelTop

```csharp
public static Vector3 ModelTop { get; }
```

Unit vector pointing towards the top side (up) of imported 3D assets.

### One

```csharp
public static Vector3 One { get; }
```

One vector, a vector with all components set to `1`.

### Right

```csharp
public static Vector3 Right { get; }
```

Right unit vector. Represents the local direction of right,
and the global direction of east.

### Up

```csharp
public static Vector3 Up { get; }
```

Up unit vector.

### Zero

```csharp
public static Vector3 Zero { get; }
```

Zero vector, a vector with all components set to `0`.

## 方法

### Abs

```csharp
public Vector3 Abs()
```

引擎名：`abs`

Returns a new vector with all components in absolute values (i.e. positive).

返回：A vector with [`Mathf.Abs`](Mathf.md) called on each component.

### AngleTo

```csharp
public float AngleTo(Vector3 to)
```

引擎名：`angle_to`

Returns the unsigned minimum angle to the given vector, in radians.

参数：
- `to`：The other vector to compare this vector to.

返回：The unsigned angle between the two vectors, in radians.

### BezierDerivative

```csharp
public Vector3 BezierDerivative(Vector3 control1, Vector3 control2, Vector3 end, float t)
```

引擎名：`bezier_derivative`

Returns the derivative at the given `t` on the Bezier curve defined by this vector
and the given `control1`, `control2`, and `end` points.

参数：
- `control1`：Control point that defines the bezier curve.
- `control2`：Control point that defines the bezier curve.
- `end`：The destination value for the interpolation.
- `t`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### BezierInterpolate

```csharp
public Vector3 BezierInterpolate(Vector3 control1, Vector3 control2, Vector3 end, float t)
```

引擎名：`bezier_interpolate`

Returns the point at the given `t` on a one-dimensional Bezier curve defined by this vector
and the given `control1`, `control2`, and `end` points.

参数：
- `control1`：Control point that defines the bezier curve.
- `control2`：Control point that defines the bezier curve.
- `end`：The destination vector.
- `t`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The interpolated vector.

### Bounce

```csharp
public Vector3 Bounce(Vector3 normal)
```

引擎名：`bounce`

Returns this vector "bounced off" from a plane defined by the given normal.

参数：
- `normal`：The normal vector defining the plane to bounce off. Must be normalized.

返回：The bounced vector.

### Ceil

```csharp
public Vector3 Ceil()
```

引擎名：`ceil`

Returns a new vector with all components rounded up (towards positive infinity).

返回：A vector with [`Mathf.Ceil`](Mathf.md) called on each component.

### Clamp

```csharp
public Vector3 Clamp(Vector3 min, Vector3 max)
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
public Vector3 Clamp(float min, float max)
```

引擎名：`clamp`

Returns a new vector with all components clamped between the
`min` and `max` using
[`Mathf.Clamp`](Mathf.md).

参数：
- `min`：The minimum allowed value.
- `max`：The maximum allowed value.

返回：The vector with all components clamped.

### Cross

```csharp
public Vector3 Cross(Vector3 with)
```

引擎名：`cross`

Returns the cross product of this vector and `with`.

参数：
- `with`：The other vector.

返回：The cross product vector.

### CubicInterpolate

```csharp
public Vector3 CubicInterpolate(Vector3 b, Vector3 preA, Vector3 postB, float weight)
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
public Vector3 CubicInterpolateInTime(Vector3 b, Vector3 preA, Vector3 postB, float weight, float t, float preAT, float postBT)
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
public void Deconstruct(out float x, out float y, out float z)
```

Helper method for deconstruction into a tuple.

### DirectionTo

```csharp
public Vector3 DirectionTo(Vector3 to)
```

引擎名：`direction_to`

Returns the normalized vector pointing from this vector to `to`.

参数：
- `to`：The other vector to point towards.

返回：The direction from this vector to `to`.

### DistanceSquaredTo

```csharp
public float DistanceSquaredTo(Vector3 to)
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
public float DistanceTo(Vector3 to)
```

引擎名：`distance_to`

Returns the distance between this vector and `to`.

参数：
- `to`：The other vector to use.

返回：The distance between the two vectors.

### Dot

```csharp
public float Dot(Vector3 with)
```

引擎名：`dot`

Returns the dot product of this vector and `with`.

参数：
- `with`：The other vector to use.

返回：The dot product of the two vectors.

### Equals

```csharp
public bool Equals(Vector3 other)
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
public Vector3 Floor()
```

引擎名：`floor`

Returns a new vector with all components rounded down (towards negative infinity).

返回：A vector with [`Mathf.Floor`](Mathf.md) called on each component.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Vector3`.

返回：A hash code for this vector.

### Inverse

```csharp
public Vector3 Inverse()
```

引擎名：`inverse`

Returns the inverse of this vector. This is the same as `new Vector3(1 / v.X, 1 / v.Y, 1 / v.Z)`.

返回：The inverse of this vector.

### IsEqualApprox

```csharp
public bool IsEqualApprox(Vector3 other)
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
public Vector3 Lerp(Vector3 to, float weight)
```

引擎名：`lerp`

Returns the result of the linear interpolation between
this vector and `to` by amount `weight`.

参数：
- `to`：The destination vector for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting vector of the interpolation.

### LimitLength

```csharp
public Vector3 LimitLength(float length = 1f)
```

引擎名：`limit_length`

Returns the vector with a maximum length by limiting its length to `length`.

参数：
- `length`：The length to limit to.

返回：The vector with its length limited.

### Max

```csharp
public Vector3 Max(Vector3 with)
```

引擎名：`max`

Returns the result of the component-wise maximum between
this vector and `with`.
Equivalent to `new Vector3(Mathf.Max(X, with.X), Mathf.Max(Y, with.Y), Mathf.Max(Z, with.Z))`.

参数：
- `with`：The other vector to use.

返回：The resulting maximum vector.

### Max

```csharp
public Vector3 Max(float with)
```

引擎名：`max`

Returns the result of the component-wise maximum between
this vector and `with`.
Equivalent to `new Vector3(Mathf.Max(X, with), Mathf.Max(Y, with), Mathf.Max(Z, with))`.

参数：
- `with`：The other value to use.

返回：The resulting maximum vector.

### MaxAxisIndex

```csharp
public Vector3.Axis MaxAxisIndex()
```

引擎名：`max_axis_index`

Returns the axis of the vector's highest value. See [`Vector3.Axis`](Vector3.md).
If all components are equal, this method returns [`Vector3.Axis.X`](Vector3.md).

返回：The index of the highest axis.

### Min

```csharp
public Vector3 Min(Vector3 with)
```

引擎名：`min`

Returns the result of the component-wise minimum between
this vector and `with`.
Equivalent to `new Vector3(Mathf.Min(X, with.X), Mathf.Min(Y, with.Y), Mathf.Min(Z, with.Z))`.

参数：
- `with`：The other vector to use.

返回：The resulting minimum vector.

### Min

```csharp
public Vector3 Min(float with)
```

引擎名：`min`

Returns the result of the component-wise minimum between
this vector and `with`.
Equivalent to `new Vector3(Mathf.Min(X, with), Mathf.Min(Y, with), Mathf.Min(Z, with))`.

参数：
- `with`：The other value to use.

返回：The resulting minimum vector.

### MinAxisIndex

```csharp
public Vector3.Axis MinAxisIndex()
```

引擎名：`min_axis_index`

Returns the axis of the vector's lowest value. See [`Vector3.Axis`](Vector3.md).
If all components are equal, this method returns [`Vector3.Axis.Z`](Vector3.md).

返回：The index of the lowest axis.

### MoveToward

```csharp
public Vector3 MoveToward(Vector3 to, float delta)
```

引擎名：`move_toward`

Returns a new vector moved toward `to` by the fixed `delta` amount. Will not go past the final value.

参数：
- `to`：The vector to move towards.
- `delta`：The amount to move towards by.

返回：The resulting vector.

### Normalized

```csharp
public Vector3 Normalized()
```

引擎名：`normalized`

Returns the vector scaled to unit length. Equivalent to `v / v.Length()`.

返回：A normalized version of the vector.

### OctahedronDecode

```csharp
public static Vector3 OctahedronDecode(Vector2 oct)
```

引擎名：`octahedron_decode`

Returns the Vector3 from an octahedral-compressed form created using `OctahedronEncode()` (stored as a Vector2).

参数：
- `oct`：Encoded Vector2

返回：The decoded normalized Vector3.

### OctahedronEncode

```csharp
public Vector2 OctahedronEncode()
```

引擎名：`octahedron_encode`

Returns the octahedral-encoded (oct32) form of this Vector3 as a Vector2. Since a Vector2 occupies 1/3 less memory compared to Vector3,
this form of compression can be used to pass greater amounts of normalized Vector3s without increasing storage or memory requirements.
See also `Normalized()`, `OctahedronDecode()`.
Note: OctahedronEncode can only be used for normalized vectors. OctahedronEncode does not check whether this Vector3 is normalized,
and will return a value that does not decompress to the original value if the Vector3 is not normalized.
Note: Octahedral compression is lossy, although visual differences are rarely perceptible in real world scenarios.

返回：The encoded Vector2.

### Outer

```csharp
public Basis Outer(Vector3 with)
```

引擎名：`outer`

Returns the outer product with `with`.

参数：
- `with`：The other vector.

返回：A [`Basis`](Basis.md) representing the outer product matrix.

### PosMod

```csharp
public Vector3 PosMod(Vector3 modv)
```

Returns a vector composed of the [`Mathf.PosMod`](Mathf.md) of this vector's components
and `modv`'s components.

参数：
- `modv`：A vector representing the divisors of the operation.

返回：A vector with each component [`Mathf.PosMod`](Mathf.md) by `modv`'s components.

### PosMod

```csharp
public Vector3 PosMod(float mod)
```

Returns a vector composed of the [`Mathf.PosMod`](Mathf.md) of this vector's components
and `mod`.

参数：
- `mod`：A value representing the divisor of the operation.

返回：A vector with each component [`Mathf.PosMod`](Mathf.md) by `mod`.

### Project

```csharp
public Vector3 Project(Vector3 onNormal)
```

引擎名：`project`

Returns a new vector resulting from projecting this vector onto the given vector `onNormal`.
The resulting new vector is parallel to `onNormal`.
See also `Slide()`.
Note: If the vector `onNormal` is a zero vector, the components of the resulting new vector will be `System.Single.NaN`.

参数：
- `onNormal`：The vector to project onto.

返回：The projected vector.

### Reflect

```csharp
public Vector3 Reflect(Vector3 normal)
```

引擎名：`reflect`

Returns this vector reflected from a plane defined by the given `normal`.

参数：
- `normal`：The normal vector defining the plane to reflect from. Must be normalized.

返回：The reflected vector.

### Rotated

```csharp
public Vector3 Rotated(Vector3 axis, float angle)
```

引擎名：`rotated`

Rotates this vector around a given `axis` vector by `angle` (in radians).
The `axis` vector must be a normalized vector.

参数：
- `axis`：The vector to rotate around. Must be normalized.
- `angle`：The angle to rotate by, in radians.

返回：The rotated vector.

### Round

```csharp
public Vector3 Round()
```

引擎名：`round`

Returns this vector with all components rounded to the nearest integer,
with halfway cases rounded towards the nearest multiple of two.

返回：The rounded vector.

### Sign

```csharp
public Vector3 Sign()
```

引擎名：`sign`

Returns a vector with each component set to one or negative one, depending
on the signs of this vector's components, or zero if the component is zero,
by calling [`Mathf.Sign`](Mathf.md) on each component.

返回：A vector with all components as either `1`, `-1`, or `0`.

### SignedAngleTo

```csharp
public float SignedAngleTo(Vector3 to, Vector3 axis)
```

引擎名：`signed_angle_to`

Returns the signed angle to the given vector, in radians.
The sign of the angle is positive in a counter-clockwise
direction and negative in a clockwise direction when viewed
from the side specified by the `axis`.

参数：
- `to`：The other vector to compare this vector to.
- `axis`：The reference axis to use for the angle sign.

返回：The signed angle between the two vectors, in radians.

### Slerp

```csharp
public Vector3 Slerp(Vector3 to, float weight)
```

引擎名：`slerp`

Returns the result of the spherical linear interpolation between
this vector and `to` by amount `weight`.

This method also handles interpolating the lengths if the input vectors
have different lengths. For the special case of one or both input vectors
having zero length, this method behaves like `Lerp()`.

参数：
- `to`：The destination vector for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting vector of the interpolation.

### Slide

```csharp
public Vector3 Slide(Vector3 normal)
```

引擎名：`slide`

Returns a new vector resulting from sliding this vector along a plane with normal `normal`.
The resulting new vector is perpendicular to `normal`, and is equivalent to this vector minus its projection on `normal`.
See also `Project()`.
Note: The vector `normal` must be normalized. See also `Normalized()`.

参数：
- `normal`：The normal vector of the plane to slide on.

返回：The slid vector.

### Snapped

```csharp
public Vector3 Snapped(Vector3 step)
```

引擎名：`snapped`

Returns a new vector with each component snapped to the nearest multiple of the corresponding component in `step`.
This can also be used to round to an arbitrary number of decimals.

参数：
- `step`：A vector value representing the step size to snap to.

返回：The snapped vector.

### Snapped

```csharp
public Vector3 Snapped(float step)
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

Converts this `Vector3` to a string with the given `format`.

返回：A string representation of this vector.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Vector3` to a string.

返回：A string representation of this vector.

## 运算符

### op_Addition

```csharp
public static Vector3 op_Addition(Vector3 left, Vector3 right)
```

Adds each component of the `Vector3`
with the components of the given `Vector3`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The added vector.

### op_Division

```csharp
public static Vector3 op_Division(Vector3 vec, Vector3 divisorv)
```

Divides each component of the `Vector3`
by the components of the given `Vector3`.

参数：
- `vec`：The dividend vector.
- `divisorv`：The divisor vector.

返回：The divided vector.

### op_Division

```csharp
public static Vector3 op_Division(Vector3 vec, float divisor)
```

Divides each component of the `Vector3`
by the given `System.Single`.

参数：
- `vec`：The dividend vector.
- `divisor`：The divisor value.

返回：The divided vector.

### op_Equality

```csharp
public static bool op_Equality(Vector3 left, Vector3 right)
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
public static bool op_GreaterThan(Vector3 left, Vector3 right)
```

Compares two `Vector3` vectors by first checking if
the X value of the `left` vector is greater than
the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors, and then with the Z values.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is greater than the right.

### op_GreaterThanOrEqual

```csharp
public static bool op_GreaterThanOrEqual(Vector3 left, Vector3 right)
```

Compares two `Vector3` vectors by first checking if
the X value of the `left` vector is greater than
or equal to the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors, and then with the Z values.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is greater than or equal to the right.

### op_Inequality

```csharp
public static bool op_Inequality(Vector3 left, Vector3 right)
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
public static bool op_LessThan(Vector3 left, Vector3 right)
```

Compares two `Vector3` vectors by first checking if
the X value of the `left` vector is less than
the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors, and then with the Z values.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is less than the right.

### op_LessThanOrEqual

```csharp
public static bool op_LessThanOrEqual(Vector3 left, Vector3 right)
```

Compares two `Vector3` vectors by first checking if
the X value of the `left` vector is less than
or equal to the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors, and then with the Z values.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is less than or equal to the right.

### op_Modulus

```csharp
public static Vector3 op_Modulus(Vector3 vec, Vector3 divisorv)
```

Gets the remainder of each component of the `Vector3`
with the components of the given `Vector3`.
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
public static Vector3 op_Modulus(Vector3 vec, float divisor)
```

Gets the remainder of each component of the `Vector3`
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
public static Vector3 op_Multiply(Vector3 left, Vector3 right)
```

Multiplies each component of the `Vector3`
by the components of the given `Vector3`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The multiplied vector.

### op_Multiply

```csharp
public static Vector3 op_Multiply(Vector3 vec, float scale)
```

Multiplies each component of the `Vector3`
by the given `System.Single`.

参数：
- `vec`：The vector to multiply.
- `scale`：The scale to multiply by.

返回：The multiplied vector.

### op_Multiply

```csharp
public static Vector3 op_Multiply(float scale, Vector3 vec)
```

Multiplies each component of the `Vector3`
by the given `System.Single`.

参数：
- `scale`：The scale to multiply by.
- `vec`：The vector to multiply.

返回：The multiplied vector.

### op_Subtraction

```csharp
public static Vector3 op_Subtraction(Vector3 left, Vector3 right)
```

Subtracts each component of the `Vector3`
by the components of the given `Vector3`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The subtracted vector.

### op_UnaryNegation

```csharp
public static Vector3 op_UnaryNegation(Vector3 vec)
```

Returns the negative value of the `Vector3`.
This is the same as writing `new Vector3(-v.X, -v.Y, -v.Z)`.
This operation flips the direction of the vector while
keeping the same magnitude.
With floats, the number zero can be either positive or negative.

参数：
- `vec`：The vector to negate/flip.

返回：The negated/flipped vector.

## 字段

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
Returned by [`Vector3.MaxAxisIndex`](Vector3.md) and [`Vector3.MinAxisIndex`](Vector3.md).

### 枚举值

- `X = 0`：The vector's X axis.
- `Y = 1`：The vector's Y axis.
- `Z = 2`：The vector's Z axis.
