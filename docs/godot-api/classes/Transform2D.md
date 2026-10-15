<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Transform2D

命名空间 `Godot`；struct

2×3 matrix (2 rows, 3 columns) used for 2D linear transformations.
It can represent transformations such as translation, rotation, or scaling.
It consists of a three [`Vector2`](Vector2.md) values: x, y, and the origin.

For more information, read this documentation article:
https://docs.godotengine.org/en/latest/tutorials/math/matrices_and_transforms.html

## 构造函数

### Transform2D

```csharp
public Transform2D(Vector2 xAxis, Vector2 yAxis, Vector2 originPos)
```

Constructs a transformation matrix from 3 vectors (matrix columns).

参数：
- `xAxis`：The X vector, or column index 0.
- `yAxis`：The Y vector, or column index 1.
- `originPos`：The origin vector, or column index 2.

### Transform2D

```csharp
public Transform2D(float rotation, Vector2 origin)
```

Constructs a transformation matrix from a `rotation` value and
`origin` vector.

参数：
- `rotation`：The rotation of the new transform, in radians.
- `origin`：The origin vector, or column index 2.

### Transform2D

```csharp
public Transform2D(float rotation, Vector2 scale, float skew, Vector2 origin)
```

Constructs a transformation matrix from a `rotation` value,
`scale` vector, `skew` value, and
`origin` vector.

参数：
- `rotation`：The rotation of the new transform, in radians.
- `scale`：The scale of the new transform.
- `skew`：The skew of the new transform, in radians.
- `origin`：The origin vector, or column index 2.

### Transform2D

```csharp
public Transform2D(float xx, float xy, float yx, float yy, float ox, float oy)
```

Constructs a transformation matrix from the given components.
Arguments are named such that xy is equal to calling `X.Y`.

参数：
- `xx`：The X component of the X column vector, accessed via `t.X.X` or `[0][0]`.
- `xy`：The Y component of the X column vector, accessed via `t.X.Y` or `[0][1]`.
- `yx`：The X component of the Y column vector, accessed via `t.Y.X` or `[1][0]`.
- `yy`：The Y component of the Y column vector, accessed via `t.Y.Y` or `[1][1]`.
- `ox`：The X component of the origin vector, accessed via `t.Origin.X` or `[2][0]`.
- `oy`：The Y component of the origin vector, accessed via `t.Origin.Y` or `[2][1]`.

## 属性

### FlipX

```csharp
public static Transform2D FlipX { get; }
```

The transform that will flip something along the X axis.

### FlipY

```csharp
public static Transform2D FlipY { get; }
```

The transform that will flip something along the Y axis.

### Identity

```csharp
public static Transform2D Identity { get; }
```

The identity transform, with no translation, rotation, or scaling applied.
This is used as a replacement for `Transform2D()` in GDScript.
Do not use `new Transform2D()` with no arguments in C#, because it sets all values to zero.

### Item

```csharp
public Vector2 this[int column] { get; set; }
```

Access whole columns in the form of [`Vector2`](Vector2.md).
The third column is the `Origin` vector.

参数：
- `column`：Which column vector.

### Item

```csharp
public float this[int column, int row] { get; set; }
```

Access matrix elements in column-major order.
The third column is the `Origin` vector.

参数：
- `column`：Which column, the matrix horizontal position.
- `row`：Which row, the matrix vertical position.

### Rotation

```csharp
public float Rotation { get; }
```

Returns the transform's rotation (in radians).

### Scale

```csharp
public Vector2 Scale { get; }
```

Returns the scale.

### Skew

```csharp
public float Skew { get; }
```

Returns the transform's skew (in radians).

## 方法

### AffineInverse

```csharp
public Transform2D AffineInverse()
```

引擎名：`affine_inverse`

Returns the inverse of the transform, under the assumption that
the basis is invertible (must have non-zero determinant).

返回：The inverse transformation matrix.

### BasisXform

```csharp
public Vector2 BasisXform(Vector2 v)
```

引擎名：`basis_xform`

Returns a vector transformed (multiplied) by the basis matrix.
This method does not account for translation (the `Origin` vector).

参数：
- `v`：A vector to transform.

返回：The transformed vector.

### BasisXformInv

```csharp
public Vector2 BasisXformInv(Vector2 v)
```

引擎名：`basis_xform_inv`

Returns a vector transformed (multiplied) by the inverse basis matrix,
under the assumption that the basis is orthonormal (i.e. rotation/reflection
is fine, scaling/skew is not).
This method does not account for translation (the `Origin` vector).
`transform.BasisXformInv(vector)` is equivalent to `transform.Inverse().BasisXform(vector)`. See `Inverse()`.
For non-orthonormal transforms (e.g. with scaling) `transform.AffineInverse().BasisXform(vector)` can be used instead. See `AffineInverse()`.

参数：
- `v`：A vector to inversely transform.

返回：The inversely transformed vector.

### Determinant

```csharp
public float Determinant()
```

引擎名：`determinant`

Returns the determinant of the basis matrix. If the basis is
uniformly scaled, then its determinant equals the square of the
scale factor.

A negative determinant means the basis was flipped, so one part of
the scale is negative. A zero determinant means the basis isn't
invertible, and is usually considered invalid.

返回：The determinant of the basis matrix.

### Equals

```csharp
public bool Equals(Transform2D other)
```

Returns `true` if the transforms are exactly equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `other`：The other transform to compare.

返回：Whether or not the matrices are exactly equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if the transform is exactly equal
to the given object (`obj`).
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `obj`：The object to compare with.

返回：Whether or not the transform and the object are exactly equal.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Transform2D`.

返回：A hash code for this transform.

### InterpolateWith

```csharp
public Transform2D InterpolateWith(Transform2D transform, float weight)
```

引擎名：`interpolate_with`

Interpolates this transform to the other `transform` by `weight`.

参数：
- `transform`：The other transform.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The interpolated transform.

### Inverse

```csharp
public Transform2D Inverse()
```

引擎名：`inverse`

Returns the inverse of the transform, under the assumption that
the transformation basis is orthonormal (i.e. rotation/reflection
is fine, scaling/skew is not). Use `AffineInverse()` for
non-orthonormal transforms (e.g. with scaling).

返回：The inverse matrix.

### IsEqualApprox

```csharp
public bool IsEqualApprox(Transform2D other)
```

引擎名：`is_equal_approx`

Returns `true` if this transform and `other` are approximately equal,
by running [`Vector2.IsEqualApprox`](Vector2.md) on each component.

参数：
- `other`：The other transform to compare.

返回：Whether or not the matrices are approximately equal.

### IsFinite

```csharp
public bool IsFinite()
```

引擎名：`is_finite`

Returns `true` if this transform is finite, by calling
[`Mathf.IsFinite`](Mathf.md) on each component.

返回：Whether this vector is finite or not.

### Orthonormalized

```csharp
public Transform2D Orthonormalized()
```

引擎名：`orthonormalized`

Returns the transform with the basis orthogonal (90 degrees),
and normalized axis vectors (scale of 1 or -1).

返回：The orthonormalized transform.

### Rotated

```csharp
public Transform2D Rotated(float angle)
```

引擎名：`rotated`

Rotates the transform by `angle` (in radians).
The operation is done in the parent/global frame, equivalent to
multiplying the matrix from the left.

参数：
- `angle`：The angle to rotate, in radians.

返回：The rotated transformation matrix.

### RotatedLocal

```csharp
public Transform2D RotatedLocal(float angle)
```

引擎名：`rotated_local`

Rotates the transform by `angle` (in radians).
The operation is done in the local frame, equivalent to
multiplying the matrix from the right.

参数：
- `angle`：The angle to rotate, in radians.

返回：The rotated transformation matrix.

### Scaled

```csharp
public Transform2D Scaled(Vector2 scale)
```

引擎名：`scaled`

Scales the transform by the given scaling factor.
The operation is done in the parent/global frame, equivalent to
multiplying the matrix from the left.

参数：
- `scale`：The scale to introduce.

返回：The scaled transformation matrix.

### ScaledLocal

```csharp
public Transform2D ScaledLocal(Vector2 scale)
```

引擎名：`scaled_local`

Scales the transform by the given scaling factor.
The operation is done in the local frame, equivalent to
multiplying the matrix from the right.

参数：
- `scale`：The scale to introduce.

返回：The scaled transformation matrix.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Transform2D` to a string with the given `format`.

返回：A string representation of this transform.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Transform2D` to a string.

返回：A string representation of this transform.

### Translated

```csharp
public Transform2D Translated(Vector2 offset)
```

引擎名：`translated`

Translates the transform by the given `offset`.
The operation is done in the parent/global frame, equivalent to
multiplying the matrix from the left.

参数：
- `offset`：The offset to translate by.

返回：The translated matrix.

### TranslatedLocal

```csharp
public Transform2D TranslatedLocal(Vector2 offset)
```

引擎名：`translated_local`

Translates the transform by the given `offset`.
The operation is done in the local frame, equivalent to
multiplying the matrix from the right.

参数：
- `offset`：The offset to translate by.

返回：The translated matrix.

## 运算符

### op_Equality

```csharp
public static bool op_Equality(Transform2D left, Transform2D right)
```

Returns `true` if the transforms are exactly equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left transform.
- `right`：The right transform.

返回：Whether or not the transforms are exactly equal.

### op_Inequality

```csharp
public static bool op_Inequality(Transform2D left, Transform2D right)
```

Returns `true` if the transforms are not equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left transform.
- `right`：The right transform.

返回：Whether or not the transforms are not equal.

### op_Multiply

```csharp
public static Rect2 op_Multiply(Rect2 rect, Transform2D transform)
```

Returns a Rect2 transformed (multiplied) by the inverse transformation matrix,
under the assumption that the transformation basis is orthonormal (i.e. rotation/reflection
is fine, scaling/skew is not).
`rect * transform` is equivalent to `transform.Inverse() * rect`. See `Inverse()`.
For transforming by inverse of an affine transformation (e.g. with scaling) `transform.AffineInverse() * rect` can be used instead. See `AffineInverse()`.

参数：
- `rect`：A Rect2 to inversely transform.
- `transform`：The transformation to apply.

返回：The inversely transformed Rect2.

### op_Multiply

```csharp
public static Rect2 op_Multiply(Transform2D transform, Rect2 rect)
```

Returns a Rect2 transformed (multiplied) by the transformation matrix.

参数：
- `transform`：The transformation to apply.
- `rect`：A Rect2 to transform.

返回：The transformed Rect2.

### op_Multiply

```csharp
public static Transform2D op_Multiply(Transform2D left, Transform2D right)
```

Composes these two transformation matrices by multiplying them
together. This has the effect of transforming the second transform
(the child) by the first transform (the parent).

参数：
- `left`：The parent transform.
- `right`：The child transform.

返回：The composed transform.

### op_Multiply

```csharp
public static Vector2 op_Multiply(Transform2D transform, Vector2 vector)
```

Returns a Vector2 transformed (multiplied) by the transformation matrix.

参数：
- `transform`：The transformation to apply.
- `vector`：A Vector2 to transform.

返回：The transformed Vector2.

### op_Multiply

```csharp
public static Vector2 op_Multiply(Vector2 vector, Transform2D transform)
```

Returns a Vector2 transformed (multiplied) by the inverse transformation matrix,
under the assumption that the transformation basis is orthonormal (i.e. rotation/reflection
is fine, scaling/skew is not).
`vector * transform` is equivalent to `transform.Inverse() * vector`. See `Inverse()`.
For transforming by inverse of an affine transformation (e.g. with scaling) `transform.AffineInverse() * vector` can be used instead. See `AffineInverse()`.

参数：
- `vector`：A Vector2 to inversely transform.
- `transform`：The transformation to apply.

返回：The inversely transformed Vector2.

### op_Multiply

```csharp
public static Vector2[] op_Multiply(Transform2D transform, Vector2[] array)
```

Returns a copy of the given Vector2[] transformed (multiplied) by the transformation matrix.

参数：
- `transform`：The transformation to apply.
- `array`：A Vector2[] to transform.

返回：The transformed copy of the Vector2[].

### op_Multiply

```csharp
public static Vector2[] op_Multiply(Vector2[] array, Transform2D transform)
```

Returns a copy of the given Vector2[] transformed (multiplied) by the inverse transformation matrix,
under the assumption that the transformation basis is orthonormal (i.e. rotation/reflection
is fine, scaling/skew is not).
`array * transform` is equivalent to `transform.Inverse() * array`. See `Inverse()`.
For transforming by inverse of an affine transformation (e.g. with scaling) `transform.AffineInverse() * array` can be used instead. See `AffineInverse()`.

参数：
- `array`：A Vector2[] to inversely transform.
- `transform`：The transformation to apply.

返回：The inversely transformed copy of the Vector2[].

## 字段

### Origin

```csharp
public Vector2 Origin
```

The origin vector (column 2, the third column). Equivalent to array index `[2]`.
The origin vector represents translation.

### X

```csharp
public Vector2 X
```

The basis matrix's X vector (column 0). Equivalent to array index `[0]`.

### Y

```csharp
public Vector2 Y
```

The basis matrix's Y vector (column 1). Equivalent to array index `[1]`.
