<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Curve

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This resource describes a mathematical curve by defining a set of points and tangents at each point. By default, it ranges between `0` and `1` on the X and Y axes, but these ranges can be changed.

Please note that many resources and nodes assume they are given *unit curves*. A unit curve is a curve whose domain (the X axis) is between `0` and `1`. Some examples of unit curve usage are [`CpuParticles2D.AngleCurve`](CpuParticles2D.md) and [`Line2D.WidthCurve`](Line2D.md).

## 构造函数

### Curve

```csharp
public Curve()
```

## 属性

### BakeResolution

```csharp
public int BakeResolution { get; set; }
```

引擎名：`bake_resolution`

The number of points to include in the baked (i.e. cached) curve data.

### MaxDomain

```csharp
public float MaxDomain { get; set; }
```

引擎名：`max_domain`

The maximum domain (x-coordinate) that points can have.

### MaxValue

```csharp
public float MaxValue { get; set; }
```

引擎名：`max_value`

The maximum value (y-coordinate) that points can have. Tangents can cause higher values between points.

### MinDomain

```csharp
public float MinDomain { get; set; }
```

引擎名：`min_domain`

The minimum domain (x-coordinate) that points can have.

### MinValue

```csharp
public float MinValue { get; set; }
```

引擎名：`min_value`

The minimum value (y-coordinate) that points can have. Tangents can cause lower values between points.

### PointCount

```csharp
public int PointCount { get; set; }
```

引擎名：`point_count`

The number of points describing the curve.

### _Data

```csharp
public Array _Data { get; set; }
```

### _Limits

```csharp
public Array _Limits { get; set; }
```

## 方法

### AddPoint

```csharp
public int AddPoint(Vector2 position, float leftTangent = 0f, float rightTangent = 0f, Curve.TangentMode leftMode = Curve.TangentMode.Free, Curve.TangentMode rightMode = Curve.TangentMode.Free)
```

引擎名：`add_point`

Adds a point to the curve. For each side, if the `*_mode` is [`Curve.TangentMode.Linear`](Curve.md), the `*_tangent` angle (in degrees) uses the slope of the curve halfway to the adjacent point. Allows custom assignments to the `*_tangent` angle if `*_mode` is set to [`Curve.TangentMode.Free`](Curve.md).

### Bake

```csharp
public void Bake()
```

引擎名：`bake`

Recomputes the baked cache of points for the curve.

### CleanDupes

```csharp
public void CleanDupes()
```

引擎名：`clean_dupes`

Removes duplicate points, i.e. points that are less than 0.00001 units (engine epsilon value) away from their neighbor on the curve.

### ClearPoints

```csharp
public void ClearPoints()
```

引擎名：`clear_points`

Removes all points from the curve.

### GetBakeResolution

```csharp
public int GetBakeResolution()
```

引擎名：`get_bake_resolution`

### GetDomainRange

```csharp
public float GetDomainRange()
```

引擎名：`get_domain_range`

Returns the difference between `MinDomain` and `MaxDomain`.

### GetMaxDomain

```csharp
public float GetMaxDomain()
```

引擎名：`get_max_domain`

### GetMaxValue

```csharp
public float GetMaxValue()
```

引擎名：`get_max_value`

### GetMinDomain

```csharp
public float GetMinDomain()
```

引擎名：`get_min_domain`

### GetMinValue

```csharp
public float GetMinValue()
```

引擎名：`get_min_value`

### GetPointCount

```csharp
public int GetPointCount()
```

引擎名：`get_point_count`

### GetPointLeftMode

```csharp
public Curve.TangentMode GetPointLeftMode(int index)
```

引擎名：`get_point_left_mode`

Returns the left [`Curve.TangentMode`](Curve.md) for the point at `index`.

### GetPointLeftTangent

```csharp
public float GetPointLeftTangent(int index)
```

引擎名：`get_point_left_tangent`

Returns the left tangent angle (in degrees) for the point at `index`.

### GetPointPosition

```csharp
public Vector2 GetPointPosition(int index)
```

引擎名：`get_point_position`

Returns the curve coordinates for the point at `index`.

### GetPointRightMode

```csharp
public Curve.TangentMode GetPointRightMode(int index)
```

引擎名：`get_point_right_mode`

Returns the right [`Curve.TangentMode`](Curve.md) for the point at `index`.

### GetPointRightTangent

```csharp
public float GetPointRightTangent(int index)
```

引擎名：`get_point_right_tangent`

Returns the right tangent angle (in degrees) for the point at `index`.

### GetValueRange

```csharp
public float GetValueRange()
```

引擎名：`get_value_range`

Returns the difference between `MinValue` and `MaxValue`.

### RemovePoint

```csharp
public void RemovePoint(int index)
```

引擎名：`remove_point`

Removes the point at `index` from the curve.

### Sample

```csharp
public float Sample(float offset)
```

引擎名：`sample`

Returns the Y value for the point that would exist at the X position `offset` along the curve.

### SampleBaked

```csharp
public float SampleBaked(float offset)
```

引擎名：`sample_baked`

Returns the Y value for the point that would exist at the X position `offset` along the curve using the baked cache. Bakes the curve's points if not already baked.

### SetBakeResolution

```csharp
public void SetBakeResolution(int resolution)
```

引擎名：`set_bake_resolution`

### SetMaxDomain

```csharp
public void SetMaxDomain(float max)
```

引擎名：`set_max_domain`

### SetMaxValue

```csharp
public void SetMaxValue(float max)
```

引擎名：`set_max_value`

### SetMinDomain

```csharp
public void SetMinDomain(float min)
```

引擎名：`set_min_domain`

### SetMinValue

```csharp
public void SetMinValue(float min)
```

引擎名：`set_min_value`

### SetPointCount

```csharp
public void SetPointCount(int count)
```

引擎名：`set_point_count`

### SetPointLeftMode

```csharp
public void SetPointLeftMode(int index, Curve.TangentMode mode)
```

引擎名：`set_point_left_mode`

Sets the left [`Curve.TangentMode`](Curve.md) for the point at `index` to `mode`.

### SetPointLeftTangent

```csharp
public void SetPointLeftTangent(int index, float tangent)
```

引擎名：`set_point_left_tangent`

Sets the left tangent angle for the point at `index` to `tangent`.

### SetPointOffset

```csharp
public int SetPointOffset(int index, float offset)
```

引擎名：`set_point_offset`

Assigns the horizontal position `offset` to the point at `index`.

### SetPointRightMode

```csharp
public void SetPointRightMode(int index, Curve.TangentMode mode)
```

引擎名：`set_point_right_mode`

Sets the right [`Curve.TangentMode`](Curve.md) for the point at `index` to `mode`.

### SetPointRightTangent

```csharp
public void SetPointRightTangent(int index, float tangent)
```

引擎名：`set_point_right_tangent`

Sets the right tangent angle for the point at `index` to `tangent`.

### SetPointValue

```csharp
public void SetPointValue(int index, float y)
```

引擎名：`set_point_value`

Assigns the vertical position `y` to the point at `index`.

## 事件（信号）

### DomainChanged

```csharp
public event Action DomainChanged
```

引擎名：`domain_changed`

Emitted when `MaxDomain` or `MinDomain` is changed.

### RangeChanged

```csharp
public event Action RangeChanged
```

引擎名：`range_changed`

Emitted when `MaxValue` or `MinValue` is changed.

## TangentMode

命名空间 `Godot`；enum

### 枚举值

- `Free = 0`：The tangent on this side of the point is user-defined.
- `Linear = 1`：The curve calculates the tangent on this side of the point as the slope halfway towards the adjacent point.
- `ModeCount = 2`：The total number of available tangent modes.
