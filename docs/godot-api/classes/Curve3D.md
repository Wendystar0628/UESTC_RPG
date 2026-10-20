<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Curve3D

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class describes a Bézier curve in 3D space. It is mainly used to give a shape to a [`Path3D`](Path3D.md), but can be manually sampled for other purposes.

It keeps a cache of precalculated points along the curve, to speed up further calculations.

## 构造函数

### Curve3D

```csharp
public Curve3D()
```

## 属性

### BakeInterval

```csharp
public float BakeInterval { get; set; }
```

引擎名：`bake_interval`

The distance in meters between two adjacent cached points. Changing it forces the cache to be recomputed the next time the `GetBakedPoints()` or `GetBakedLength()` function is called. The smaller the distance, the more points in the cache and the more memory it will consume, so use with care.

### Closed

```csharp
public bool Closed { get; set; }
```

引擎名：`closed`

If `true`, and the curve has more than 2 control points, the last point and the first one will be connected in a loop.

### PointCount

```csharp
public int PointCount { get; set; }
```

引擎名：`point_count`

The number of points describing the curve.

### UpVectorEnabled

```csharp
public bool UpVectorEnabled { get; set; }
```

引擎名：`up_vector_enabled`

If `true`, the curve will bake up vectors used for orientation. This is used when [`PathFollow3D.RotationMode`](PathFollow3D.md) is set to [`PathFollow3D.RotationModeEnum.Oriented`](PathFollow3D.md). Changing it forces the cache to be recomputed.

### _Data

```csharp
public Dictionary _Data { get; set; }
```

## 方法

### AddPoint

```csharp
public void AddPoint(Vector3 position, Vector3? in = null, Vector3? out = null, int index = -1)
```

引擎名：`add_point`

Adds a point with the specified `position` relative to the curve's own position, with control points `in` and `out`. Appends the new point at the end of the point list.

If `index` is given, the new point is inserted before the existing point identified by index `index`. Every existing point starting from `index` is shifted further down the list of points. The index must be greater than or equal to `0` and must not exceed the number of existing points in the line. See `PointCount`.

参数：
- `in`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.
- `out`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### ClearPoints

```csharp
public void ClearPoints()
```

引擎名：`clear_points`

Removes all points from the curve.

### GetBakeInterval

```csharp
public float GetBakeInterval()
```

引擎名：`get_bake_interval`

### GetBakedLength

```csharp
public float GetBakedLength()
```

引擎名：`get_baked_length`

Returns the total length of the curve, based on the cached points. Given enough density (see `BakeInterval`), it should be approximate enough.

### GetBakedPoints

```csharp
public Vector3[] GetBakedPoints()
```

引擎名：`get_baked_points`

Returns the cache of points as a [`Vector3`](Vector3.md)[].

### GetBakedTilts

```csharp
public float[] GetBakedTilts()
```

引擎名：`get_baked_tilts`

Returns the cache of tilts as a `System.Single`[].

### GetBakedUpVectors

```csharp
public Vector3[] GetBakedUpVectors()
```

引擎名：`get_baked_up_vectors`

Returns the cache of up vectors as a [`Vector3`](Vector3.md)[].

If `UpVectorEnabled` is `false`, the cache will be empty.

### GetClosestOffset

```csharp
public float GetClosestOffset(Vector3 toPoint)
```

引擎名：`get_closest_offset`

Returns the closest offset to `toPoint`. This offset is meant to be used in `SampleBaked()` or `SampleBakedUpVector()`.

`toPoint` must be in this curve's local space.

### GetClosestPoint

```csharp
public Vector3 GetClosestPoint(Vector3 toPoint)
```

引擎名：`get_closest_point`

Returns the closest point on baked segments (in curve's local space) to `toPoint`.

`toPoint` must be in this curve's local space.

### GetPointCount

```csharp
public int GetPointCount()
```

引擎名：`get_point_count`

### GetPointIn

```csharp
public Vector3 GetPointIn(int idx)
```

引擎名：`get_point_in`

Returns the position of the control point leading to the vertex `idx`. The returned position is relative to the vertex `idx`. If the index is out of bounds, the function sends an error to the console, and returns `(0, 0, 0)`.

### GetPointOut

```csharp
public Vector3 GetPointOut(int idx)
```

引擎名：`get_point_out`

Returns the position of the control point leading out of the vertex `idx`. The returned position is relative to the vertex `idx`. If the index is out of bounds, the function sends an error to the console, and returns `(0, 0, 0)`.

### GetPointPosition

```csharp
public Vector3 GetPointPosition(int idx)
```

引擎名：`get_point_position`

Returns the position of the vertex `idx`. If the index is out of bounds, the function sends an error to the console, and returns `(0, 0, 0)`.

### GetPointTilt

```csharp
public float GetPointTilt(int idx)
```

引擎名：`get_point_tilt`

Returns the tilt angle in radians for the point `idx`. If the index is out of bounds, the function sends an error to the console, and returns `0`.

### IsClosed

```csharp
public bool IsClosed()
```

引擎名：`is_closed`

### IsUpVectorEnabled

```csharp
public bool IsUpVectorEnabled()
```

引擎名：`is_up_vector_enabled`

### RemovePoint

```csharp
public void RemovePoint(int idx)
```

引擎名：`remove_point`

Deletes the point `idx` from the curve. Sends an error to the console if `idx` is out of bounds.

### Sample

```csharp
public Vector3 Sample(int idx, float t)
```

引擎名：`sample`

Returns the position between the vertex `idx` and the vertex `idx + 1`, where `t` controls if the point is the first vertex (`t = 0.0`), the last vertex (`t = 1.0`), or in between. Values of `t` outside the range (`0.0 >= t <=1`) give strange, but predictable results.

If `idx` is out of bounds it is truncated to the first or last vertex, and `t` is ignored. If the curve has no points, the function sends an error to the console, and returns `(0, 0, 0)`.

### SampleBaked

```csharp
public Vector3 SampleBaked(float offset = 0f, bool cubic = false)
```

引擎名：`sample_baked`

Returns a point within the curve at position `offset`, where `offset` is measured as a distance in 3D units along the curve. To do that, it finds the two cached points where the `offset` lies between, then interpolates the values. This interpolation is cubic if `cubic` is set to `true`, or linear if set to `false`.

Cubic interpolation tends to follow the curves better, but linear is faster (and often, precise enough).

### SampleBakedUpVector

```csharp
public Vector3 SampleBakedUpVector(float offset, bool applyTilt = false)
```

引擎名：`sample_baked_up_vector`

Returns an up vector within the curve at position `offset`, where `offset` is measured as a distance in 3D units along the curve. To do that, it finds the two cached up vectors where the `offset` lies between, then interpolates the values. If `applyTilt` is `true`, an interpolated tilt is applied to the interpolated up vector.

If the curve has no up vectors, the function sends an error to the console, and returns `(0, 1, 0)`.

### SampleBakedWithRotation

```csharp
public Transform3D SampleBakedWithRotation(float offset = 0f, bool cubic = false, bool applyTilt = false)
```

引擎名：`sample_baked_with_rotation`

Returns a [`Transform3D`](Transform3D.md) with `origin` as point position, `basis.x` as sideway vector, `basis.y` as up vector, `basis.z` as forward vector. When the curve length is 0, there is no reasonable way to calculate the rotation, all vectors aligned with global space axes. See also `SampleBaked()`.

### Samplef

```csharp
public Vector3 Samplef(float fofs)
```

引擎名：`samplef`

Returns the position at the vertex `fofs`. It calls `Sample()` using the integer part of `fofs` as `idx`, and its fractional part as `t`.

### SetBakeInterval

```csharp
public void SetBakeInterval(float distance)
```

引擎名：`set_bake_interval`

### SetClosed

```csharp
public void SetClosed(bool closed)
```

引擎名：`set_closed`

### SetPointCount

```csharp
public void SetPointCount(int count)
```

引擎名：`set_point_count`

### SetPointIn

```csharp
public void SetPointIn(int idx, Vector3 position)
```

引擎名：`set_point_in`

Sets the position of the control point leading to the vertex `idx`. If the index is out of bounds, the function sends an error to the console. The position is relative to the vertex.

### SetPointOut

```csharp
public void SetPointOut(int idx, Vector3 position)
```

引擎名：`set_point_out`

Sets the position of the control point leading out of the vertex `idx`. If the index is out of bounds, the function sends an error to the console. The position is relative to the vertex.

### SetPointPosition

```csharp
public void SetPointPosition(int idx, Vector3 position)
```

引擎名：`set_point_position`

Sets the position for the vertex `idx`. If the index is out of bounds, the function sends an error to the console.

### SetPointTilt

```csharp
public void SetPointTilt(int idx, float tilt)
```

引擎名：`set_point_tilt`

Sets the tilt angle in radians for the point `idx`. If the index is out of bounds, the function sends an error to the console.

The tilt controls the rotation along the look-at axis an object traveling the path would have. In the case of a curve controlling a [`PathFollow3D`](PathFollow3D.md), this tilt is an offset over the natural tilt the [`PathFollow3D`](PathFollow3D.md) calculates.

### SetUpVectorEnabled

```csharp
public void SetUpVectorEnabled(bool enable)
```

引擎名：`set_up_vector_enabled`

### Tessellate

```csharp
public Vector3[] Tessellate(int maxStages = 5, float toleranceDegrees = 4f)
```

引擎名：`tessellate`

Returns a list of points along the curve, with a curvature controlled point density. That is, the curvier parts will have more points than the straighter parts.

This approximation makes straight segments between each point, then subdivides those segments until the resulting shape is similar enough.

`maxStages` controls how many subdivisions a curve segment may face before it is considered approximate enough. Each subdivision splits the segment in half, so the default 5 stages may mean up to 32 subdivisions per curve segment. Increase with care!

`toleranceDegrees` controls how many degrees the midpoint of a segment may deviate from the real curve, before the segment has to be subdivided.

### TessellateEvenLength

```csharp
public Vector3[] TessellateEvenLength(int maxStages = 5, float toleranceLength = 0.2f)
```

引擎名：`tessellate_even_length`

Returns a list of points along the curve, with almost uniform density. `maxStages` controls how many subdivisions a curve segment may face before it is considered approximate enough. Each subdivision splits the segment in half, so the default 5 stages may mean up to 32 subdivisions per curve segment. Increase with care!

`toleranceLength` controls the maximal distance between two neighboring points, before the segment has to be subdivided.
