<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Curve2D

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class describes a Bézier curve in 2D space. It is mainly used to give a shape to a [`Path2D`](Path2D.md), but can be manually sampled for other purposes.

It keeps a cache of precalculated points along the curve, to speed up further calculations.

## 构造函数

### Curve2D

```csharp
public Curve2D()
```

## 属性

### BakeInterval

```csharp
public float BakeInterval { get; set; }
```

引擎名：`bake_interval`

The distance in pixels between two adjacent cached points. Changing it forces the cache to be recomputed the next time the `GetBakedPoints()` or `GetBakedLength()` function is called. The smaller the distance, the more points in the cache and the more memory it will consume, so use with care.

### PointCount

```csharp
public int PointCount { get; set; }
```

引擎名：`point_count`

The number of points describing the curve.

### _Data

```csharp
public Dictionary _Data { get; set; }
```

## 方法

### AddPoint

```csharp
public void AddPoint(Vector2 position, Vector2? in = null, Vector2? out = null, int index = -1)
```

引擎名：`add_point`

Adds a point with the specified `position` relative to the curve's own position, with control points `in` and `out`. Appends the new point at the end of the point list.

If `index` is given, the new point is inserted before the existing point identified by index `index`. Every existing point starting from `index` is shifted further down the list of points. The index must be greater than or equal to `0` and must not exceed the number of existing points in the line. See `PointCount`.

参数：
- `in`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.
- `out`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

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
public Vector2[] GetBakedPoints()
```

引擎名：`get_baked_points`

Returns the cache of points as a [`Vector2`](Vector2.md)[].

### GetClosestOffset

```csharp
public float GetClosestOffset(Vector2 toPoint)
```

引擎名：`get_closest_offset`

Returns the closest offset to `toPoint`. This offset is meant to be used in `SampleBaked()`.

`toPoint` must be in this curve's local space.

### GetClosestPoint

```csharp
public Vector2 GetClosestPoint(Vector2 toPoint)
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
public Vector2 GetPointIn(int idx)
```

引擎名：`get_point_in`

Returns the position of the control point leading to the vertex `idx`. The returned position is relative to the vertex `idx`. If the index is out of bounds, the function sends an error to the console, and returns `(0, 0)`.

### GetPointOut

```csharp
public Vector2 GetPointOut(int idx)
```

引擎名：`get_point_out`

Returns the position of the control point leading out of the vertex `idx`. The returned position is relative to the vertex `idx`. If the index is out of bounds, the function sends an error to the console, and returns `(0, 0)`.

### GetPointPosition

```csharp
public Vector2 GetPointPosition(int idx)
```

引擎名：`get_point_position`

Returns the position of the vertex `idx`. If the index is out of bounds, the function sends an error to the console, and returns `(0, 0)`.

### RemovePoint

```csharp
public void RemovePoint(int idx)
```

引擎名：`remove_point`

Deletes the point `idx` from the curve. Sends an error to the console if `idx` is out of bounds.

### Sample

```csharp
public Vector2 Sample(int idx, float t)
```

引擎名：`sample`

Returns the position between the vertex `idx` and the vertex `idx + 1`, where `t` controls if the point is the first vertex (`t = 0.0`), the last vertex (`t = 1.0`), or in between. Values of `t` outside the range (`0.0 <= t <= 1.0`) give strange, but predictable results.

If `idx` is out of bounds it is truncated to the first or last vertex, and `t` is ignored. If the curve has no points, the function sends an error to the console, and returns `(0, 0)`.

### SampleBaked

```csharp
public Vector2 SampleBaked(float offset = 0f, bool cubic = false)
```

引擎名：`sample_baked`

Returns a point within the curve at position `offset`, where `offset` is measured as a pixel distance along the curve.

To do that, it finds the two cached points where the `offset` lies between, then interpolates the values. This interpolation is cubic if `cubic` is set to `true`, or linear if set to `false`.

Cubic interpolation tends to follow the curves better, but linear is faster (and often, precise enough).

### SampleBakedWithRotation

```csharp
public Transform2D SampleBakedWithRotation(float offset = 0f, bool cubic = false)
```

引擎名：`sample_baked_with_rotation`

Similar to `SampleBaked()`, but returns [`Transform2D`](Transform2D.md) that includes a rotation along the curve, with `Transform2D.Origin` as the point position and the `Transform2D.X` vector pointing in the direction of the path at that point. Returns an empty transform if the length of the curve is `0`.

```csharp
var baked = curve.sample_baked_with_rotation(offset)
# The returned Transform2D can be set directly.
transform = baked
# You can also read the origin and rotation separately from the returned Transform2D.
position = baked.get_origin()
rotation = baked.get_rotation()

```

### Samplef

```csharp
public Vector2 Samplef(float fofs)
```

引擎名：`samplef`

Returns the position at the vertex `fofs`. It calls `Sample()` using the integer part of `fofs` as `idx`, and its fractional part as `t`.

### SetBakeInterval

```csharp
public void SetBakeInterval(float distance)
```

引擎名：`set_bake_interval`

### SetPointCount

```csharp
public void SetPointCount(int count)
```

引擎名：`set_point_count`

### SetPointIn

```csharp
public void SetPointIn(int idx, Vector2 position)
```

引擎名：`set_point_in`

Sets the position of the control point leading to the vertex `idx`. If the index is out of bounds, the function sends an error to the console. The position is relative to the vertex.

### SetPointOut

```csharp
public void SetPointOut(int idx, Vector2 position)
```

引擎名：`set_point_out`

Sets the position of the control point leading out of the vertex `idx`. If the index is out of bounds, the function sends an error to the console. The position is relative to the vertex.

### SetPointPosition

```csharp
public void SetPointPosition(int idx, Vector2 position)
```

引擎名：`set_point_position`

Sets the position for the vertex `idx`. If the index is out of bounds, the function sends an error to the console.

### Tessellate

```csharp
public Vector2[] Tessellate(int maxStages = 5, float toleranceDegrees = 4f)
```

引擎名：`tessellate`

Returns a list of points along the curve, with a curvature controlled point density. That is, the curvier parts will have more points than the straighter parts.

This approximation makes straight segments between each point, then subdivides those segments until the resulting shape is similar enough.

`maxStages` controls how many subdivisions a curve segment may face before it is considered approximate enough. Each subdivision splits the segment in half, so the default 5 stages may mean up to 32 subdivisions per curve segment. Increase with care!

`toleranceDegrees` controls how many degrees the midpoint of a segment may deviate from the real curve, before the segment has to be subdivided.

### TessellateEvenLength

```csharp
public Vector2[] TessellateEvenLength(int maxStages = 5, float toleranceLength = 20f)
```

引擎名：`tessellate_even_length`

Returns a list of points along the curve, with almost uniform density. `maxStages` controls how many subdivisions a curve segment may face before it is considered approximate enough. Each subdivision splits the segment in half, so the default 5 stages may mean up to 32 subdivisions per curve segment. Increase with care!

`toleranceLength` controls the maximal distance between two neighboring points, before the segment has to be subdivided.
