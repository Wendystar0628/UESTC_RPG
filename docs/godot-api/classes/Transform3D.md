<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Transform3D

命名空间 `Godot`；struct

3×4 matrix (3 rows, 4 columns) used for 3D linear transformations.
It can represent transformations such as translation, rotation, or scaling.
It consists of a [`Basis`](Basis.md) (first 3 columns) and a
[`Vector3`](Vector3.md) for the origin (last column).

For more information, read this documentation article:
https://docs.godotengine.org/en/latest/tutorials/math/matrices_and_transforms.html

## 构造函数

### Transform3D

```csharp
public Transform3D(Basis basis, Vector3 origin)
```

Constructs a transformation matrix from the given `basis` and
`origin` vector.

参数：
- `basis`：The [`Basis`](Basis.md) to create the basis from.
- `origin`：The origin vector, or column index 3.

### Transform3D

```csharp
public Transform3D(Projection projection)
```

Constructs a transformation matrix from the given `projection`
by trimming the last row of the projection matrix (`projection.X.W`,
`projection.Y.W`, `projection.Z.W`, and `projection.W.W`
are not copied over).

参数：
- `projection`：The [`Projection`](Projection.md) to create the transform from.

### Transform3D

```csharp
public Transform3D(Vector3 column0, Vector3 column1, Vector3 column2, Vector3 origin)
```

Constructs a transformation matrix from 4 vectors (matrix columns).

参数：
- `column0`：The X vector, or column index 0.
- `column1`：The Y vector, or column index 1.
- `column2`：The Z vector, or column index 2.
- `origin`：The origin vector, or column index 3.

### Transform3D

```csharp
public Transform3D(float xx, float yx, float zx, float xy, float yy, float zy, float xz, float yz, float zz, float ox, float oy, float oz)
```

Constructs a transformation matrix from the given components.
Arguments are named such that xy is equal to calling `Basis.X.Y`.

参数：
- `xx`：The X component of the X column vector, accessed via `t.Basis.X.X` or `[0][0]`.
- `yx`：The X component of the Y column vector, accessed via `t.Basis.Y.X` or `[1][0]`.
- `zx`：The X component of the Z column vector, accessed via `t.Basis.Z.X` or `[2][0]`.
- `xy`：The Y component of the X column vector, accessed via `t.Basis.X.Y` or `[0][1]`.
- `yy`：The Y component of the Y column vector, accessed via `t.Basis.Y.Y` or `[1][1]`.
- `zy`：The Y component of the Z column vector, accessed via `t.Basis.Y.Y` or `[2][1]`.
- `xz`：The Z component of the X column vector, accessed via `t.Basis.X.Y` or `[0][2]`.
- `yz`：The Z component of the Y column vector, accessed via `t.Basis.Y.Y` or `[1][2]`.
- `zz`：The Z component of the Z column vector, accessed via `t.Basis.Y.Y` or `[2][2]`.
- `ox`：The X component of the origin vector, accessed via `t.Origin.X` or `[2][0]`.
- `oy`：The Y component of the origin vector, accessed via `t.Origin.Y` or `[2][1]`.
- `oz`：The Z component of the origin vector, accessed via `t.Origin.Z` or `[2][2]`.

## 属性

### FlipX

```csharp
public static Transform3D FlipX { get; }
```

The transform that will flip something along the X axis.

### FlipY

```csharp
public static Transform3D FlipY { get; }
```

The transform that will flip something along the Y axis.

### FlipZ

```csharp
public static Transform3D FlipZ { get; }
```

The transform that will flip something along the Z axis.

### Identity

```csharp
public static Transform3D Identity { get; }
```

The identity transform, with no translation, rotation, or scaling applied.
This is used as a replacement for `Transform()` in GDScript.
Do not use `new Transform()` with no arguments in C#, because it sets all values to zero.

### Item

```csharp
public Vector3 this[int column] { get; set; }
```

Access whole columns in the form of [`Vector3`](Vector3.md).
The fourth column is the `Origin` vector.

参数：
- `column`：Which column vector.

### Item

```csharp
public float this[int column, int row] { get; set; }
```

Access matrix elements in column-major order.
The fourth column is the `Origin` vector.

参数：
- `column`：Which column, the matrix horizontal position.
- `row`：Which row, the matrix vertical position.

## 方法

### AffineInverse

```csharp
public Transform3D AffineInverse()
```

引擎名：`affine_inverse`

Returns the inverse of the transform, under the assumption that
the basis is invertible (must have non-zero determinant).

返回：The inverse transformation matrix.

### Equals

```csharp
public bool Equals(Transform3D other)
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

Serves as the hash function for `Transform3D`.

返回：A hash code for this transform.

### InterpolateWith

```csharp
public Transform3D InterpolateWith(Transform3D transform, float weight)
```

引擎名：`interpolate_with`

Returns a transform interpolated between this transform and another
`transform` by a given `weight`
(on the range of 0.0 to 1.0).

参数：
- `transform`：The other transform.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The interpolated transform.

### Inverse

```csharp
public Transform3D Inverse()
```

引擎名：`inverse`

Returns the inverse of the transform, under the assumption that
the transformation basis is orthonormal (i.e. rotation/reflection
is fine, scaling/skew is not). Use `AffineInverse()` for
non-orthonormal transforms (e.g. with scaling).

返回：The inverse matrix.

### IsEqualApprox

```csharp
public bool IsEqualApprox(Transform3D other)
```

引擎名：`is_equal_approx`

Returns `true` if this transform and `other` are approximately equal,
by running [`Vector3.IsEqualApprox`](Vector3.md) on each component.

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

### LookingAt

```csharp
public Transform3D LookingAt(Vector3 target, Vector3 up)
```

引擎名：`looking_at`

Returns a copy of the transform rotated such that the forward axis (-Z)
points towards the `target` position.
The up axis (+Y) points as close to the `up` vector
as possible while staying perpendicular to the forward axis.
The resulting transform is orthonormalized.
The existing rotation, scale, and skew information from the original transform is discarded.
The `target` and `up` vectors cannot be zero,
cannot be parallel to each other, and are defined in global/parent space.

参数：
- `target`：The object to look at.
- `up`：The relative up direction.
- `useModelFront`：If true, then the model is oriented in reverse,
towards the model front axis (+Z, Vector3.ModelFront),
which is more useful for orienting 3D models.

返回：The resulting transform.

### LookingAt

```csharp
public Transform3D LookingAt(Vector3 target, Vector3? up = null, bool useModelFront = false)
```

引擎名：`looking_at`

Returns a copy of the transform rotated such that the forward axis (-Z)
points towards the `target` position.
The up axis (+Y) points as close to the `up` vector
as possible while staying perpendicular to the forward axis.
The resulting transform is orthonormalized.
The existing rotation, scale, and skew information from the original transform is discarded.
The `target` and `up` vectors cannot be zero,
cannot be parallel to each other, and are defined in global/parent space.

参数：
- `target`：The object to look at.
- `up`：The relative up direction.
- `useModelFront`：If true, then the model is oriented in reverse,
towards the model front axis (+Z, Vector3.ModelFront),
which is more useful for orienting 3D models.

返回：The resulting transform.

### Orthonormalized

```csharp
public Transform3D Orthonormalized()
```

引擎名：`orthonormalized`

Returns the transform with the basis orthogonal (90 degrees),
and normalized axis vectors (scale of 1 or -1).

返回：The orthonormalized transform.

### Rotated

```csharp
public Transform3D Rotated(Vector3 axis, float angle)
```

引擎名：`rotated`

Rotates the transform around the given `axis` by `angle` (in radians).
The axis must be a normalized vector.
The operation is done in the parent/global frame, equivalent to
multiplying the matrix from the left.

参数：
- `axis`：The axis to rotate around. Must be normalized.
- `angle`：The angle to rotate, in radians.

返回：The rotated transformation matrix.

### RotatedLocal

```csharp
public Transform3D RotatedLocal(Vector3 axis, float angle)
```

引擎名：`rotated_local`

Rotates the transform around the given `axis` by `angle` (in radians).
The axis must be a normalized vector.
The operation is done in the local frame, equivalent to
multiplying the matrix from the right.

参数：
- `axis`：The axis to rotate around. Must be normalized.
- `angle`：The angle to rotate, in radians.

返回：The rotated transformation matrix.

### Scaled

```csharp
public Transform3D Scaled(Vector3 scale)
```

引擎名：`scaled`

Scales the transform by the given 3D `scale` factor.
The operation is done in the parent/global frame, equivalent to
multiplying the matrix from the left.

参数：
- `scale`：The scale to introduce.

返回：The scaled transformation matrix.

### ScaledLocal

```csharp
public Transform3D ScaledLocal(Vector3 scale)
```

引擎名：`scaled_local`

Scales the transform by the given 3D `scale` factor.
The operation is done in the local frame, equivalent to
multiplying the matrix from the right.

参数：
- `scale`：The scale to introduce.

返回：The scaled transformation matrix.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Transform3D` to a string with the given `format`.

返回：A string representation of this transform.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Transform3D` to a string.

返回：A string representation of this transform.

### Translated

```csharp
public Transform3D Translated(Vector3 offset)
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
public Transform3D TranslatedLocal(Vector3 offset)
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
public static bool op_Equality(Transform3D left, Transform3D right)
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
public static bool op_Inequality(Transform3D left, Transform3D right)
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
public static Aabb op_Multiply(Aabb aabb, Transform3D transform)
```

Returns an AABB transformed (multiplied) by the inverse transformation matrix,
under the assumption that the transformation basis is orthonormal (i.e. rotation/reflection
is fine, scaling/skew is not).
`aabb * transform` is equivalent to `transform.Inverse() * aabb`. See `Inverse()`.
For transforming by inverse of an affine transformation (e.g. with scaling) `transform.AffineInverse() * aabb` can be used instead. See `AffineInverse()`.

参数：
- `aabb`：An AABB to inversely transform.
- `transform`：The transformation to apply.

返回：The inversely transformed AABB.

### op_Multiply

```csharp
public static Aabb op_Multiply(Transform3D transform, Aabb aabb)
```

Returns an AABB transformed (multiplied) by the transformation matrix.

参数：
- `transform`：The transformation to apply.
- `aabb`：An AABB to transform.

返回：The transformed AABB.

### op_Multiply

```csharp
public static Plane op_Multiply(Plane plane, Transform3D transform)
```

Returns a Plane transformed (multiplied) by the inverse transformation matrix.
`plane * transform` is equivalent to `transform.AffineInverse() * plane`. See `AffineInverse()`.

参数：
- `plane`：A Plane to inversely transform.
- `transform`：The transformation to apply.

返回：The inversely transformed Plane.

### op_Multiply

```csharp
public static Plane op_Multiply(Transform3D transform, Plane plane)
```

Returns a Plane transformed (multiplied) by the transformation matrix.

参数：
- `transform`：The transformation to apply.
- `plane`：A Plane to transform.

返回：The transformed Plane.

### op_Multiply

```csharp
public static Transform3D op_Multiply(Transform3D left, Transform3D right)
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
public static Vector3 op_Multiply(Transform3D transform, Vector3 vector)
```

Returns a Vector3 transformed (multiplied) by the transformation matrix.

参数：
- `transform`：The transformation to apply.
- `vector`：A Vector3 to transform.

返回：The transformed Vector3.

### op_Multiply

```csharp
public static Vector3 op_Multiply(Vector3 vector, Transform3D transform)
```

Returns a Vector3 transformed (multiplied) by the inverse transformation matrix,
under the assumption that the transformation basis is orthonormal (i.e. rotation/reflection
is fine, scaling/skew is not).
`vector * transform` is equivalent to `transform.Inverse() * vector`. See `Inverse()`.
For transforming by inverse of an affine transformation (e.g. with scaling) `transform.AffineInverse() * vector` can be used instead. See `AffineInverse()`.

参数：
- `vector`：A Vector3 to inversely transform.
- `transform`：The transformation to apply.

返回：The inversely transformed Vector3.

### op_Multiply

```csharp
public static Vector3[] op_Multiply(Transform3D transform, Vector3[] array)
```

Returns a copy of the given Vector3[] transformed (multiplied) by the transformation matrix.

参数：
- `transform`：The transformation to apply.
- `array`：A Vector3[] to transform.

返回：The transformed copy of the Vector3[].

### op_Multiply

```csharp
public static Vector3[] op_Multiply(Vector3[] array, Transform3D transform)
```

Returns a copy of the given Vector3[] transformed (multiplied) by the inverse transformation matrix,
under the assumption that the transformation basis is orthonormal (i.e. rotation/reflection
is fine, scaling/skew is not).
`array * transform` is equivalent to `transform.Inverse() * array`. See `Inverse()`.
For transforming by inverse of an affine transformation (e.g. with scaling) `transform.AffineInverse() * array` can be used instead. See `AffineInverse()`.

参数：
- `array`：A Vector3[] to inversely transform.
- `transform`：The transformation to apply.

返回：The inversely transformed copy of the Vector3[].

## 字段

### Basis

```csharp
public Basis Basis
```

The [`Basis`](Basis.md) of this transform. Contains the X, Y, and Z basis
vectors (columns 0 to 2) and is responsible for rotation and scale.

### Origin

```csharp
public Vector3 Origin
```

The origin vector (column 3, the fourth column). Equivalent to array index `[3]`.
