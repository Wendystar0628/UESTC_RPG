<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Line2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node draws a 2D polyline, i.e. a shape consisting of several points connected by segments. `Line2D` is not a mathematical polyline, i.e. the segments are not infinitely thin. It is intended for rendering and it can be colored and optionally textured.

**Warning:** Certain configurations may be impossible to draw nicely, such as very sharp angles. In these situations, the node uses fallback drawing logic to look decent.

**Note:** `Line2D` is drawn using a 2D mesh.

## 构造函数

### Line2D

```csharp
public Line2D()
```

## 属性

### Antialiased

```csharp
public bool Antialiased { get; set; }
```

引擎名：`antialiased`

If `true`, the polyline's border will be anti-aliased.

**Note:** `Line2D` is not accelerated by batching when being anti-aliased.

### BeginCapMode

```csharp
public Line2D.LineCapMode BeginCapMode { get; set; }
```

引擎名：`begin_cap_mode`

The style of the beginning of the polyline, if `Closed` is `false`.

### Closed

```csharp
public bool Closed { get; set; }
```

引擎名：`closed`

If `true` and the polyline has more than 2 points, the last point and the first one will be connected by a segment.

**Note:** The shape of the closing segment is not guaranteed to be seamless if a `WidthCurve` is provided.

**Note:** The joint between the closing segment and the first segment is drawn first and it samples the `Gradient` and the `WidthCurve` at the beginning. This is an implementation detail that might change in a future version.

### DefaultColor

```csharp
public Color DefaultColor { get; set; }
```

引擎名：`default_color`

The color of the polyline. Will not be used if a gradient is set.

### EndCapMode

```csharp
public Line2D.LineCapMode EndCapMode { get; set; }
```

引擎名：`end_cap_mode`

The style of the end of the polyline, if `Closed` is `false`.

### Gradient

```csharp
public Gradient Gradient { get; set; }
```

引擎名：`gradient`

The gradient is drawn through the whole line from start to finish. The `DefaultColor` will not be used if this property is set.

### JointMode

```csharp
public Line2D.LineJointMode JointMode { get; set; }
```

引擎名：`joint_mode`

The style of the connections between segments of the polyline.

### Points

```csharp
public Vector2[] Points { get; set; }
```

引擎名：`points`

The points of the polyline, interpreted in local 2D coordinates. Segments are drawn between the adjacent points in this array.

### RoundPrecision

```csharp
public int RoundPrecision { get; set; }
```

引擎名：`round_precision`

The smoothness used for rounded joints and caps. Higher values result in smoother corners, but are more demanding to render and update.

### SharpLimit

```csharp
public float SharpLimit { get; set; }
```

引擎名：`sharp_limit`

Determines the miter limit of the polyline. Normally, when `JointMode` is set to [`Line2D.LineJointMode.Sharp`](Line2D.md), sharp angles fall back to using the logic of [`Line2D.LineJointMode.Bevel`](Line2D.md) joints to prevent very long miters. Higher values of this property mean that the fallback to a bevel joint will happen at sharper angles.

### Texture

```csharp
public Texture2D Texture { get; set; }
```

引擎名：`texture`

The texture used for the polyline. Uses `TextureMode` for drawing style.

### TextureMode

```csharp
public Line2D.LineTextureMode TextureMode { get; set; }
```

引擎名：`texture_mode`

The style to render the `Texture` of the polyline.

### Width

```csharp
public float Width { get; set; }
```

引擎名：`width`

The polyline's width.

### WidthCurve

```csharp
public Curve WidthCurve { get; set; }
```

引擎名：`width_curve`

The polyline's width curve. The width of the polyline over its length will be equivalent to the value of the width curve over its domain. The width curve should be a unit [`Curve`](Curve.md).

## 方法

### AddPoint

```csharp
public void AddPoint(Vector2 position, int index = -1)
```

引擎名：`add_point`

Adds a point with the specified `position` relative to the polyline's own position. If no `index` is provided, the new point will be added to the end of the points array.

If `index` is given, the new point is inserted before the existing point identified by index `index`. The indices of the points after the new point get increased by 1. The provided `index` must not exceed the number of existing points in the polyline. See `GetPointCount()`.

### ClearPoints

```csharp
public void ClearPoints()
```

引擎名：`clear_points`

Removes all points from the polyline, making it empty.

### GetAntialiased

```csharp
public bool GetAntialiased()
```

引擎名：`get_antialiased`

### GetBeginCapMode

```csharp
public Line2D.LineCapMode GetBeginCapMode()
```

引擎名：`get_begin_cap_mode`

### GetCurve

```csharp
public Curve GetCurve()
```

引擎名：`get_curve`

### GetDefaultColor

```csharp
public Color GetDefaultColor()
```

引擎名：`get_default_color`

### GetEndCapMode

```csharp
public Line2D.LineCapMode GetEndCapMode()
```

引擎名：`get_end_cap_mode`

### GetGradient

```csharp
public Gradient GetGradient()
```

引擎名：`get_gradient`

### GetJointMode

```csharp
public Line2D.LineJointMode GetJointMode()
```

引擎名：`get_joint_mode`

### GetPointCount

```csharp
public int GetPointCount()
```

引擎名：`get_point_count`

Returns the number of points in the polyline.

### GetPointPosition

```csharp
public Vector2 GetPointPosition(int index)
```

引擎名：`get_point_position`

Returns the position of the point at index `index`.

### GetPoints

```csharp
public Vector2[] GetPoints()
```

引擎名：`get_points`

### GetRoundPrecision

```csharp
public int GetRoundPrecision()
```

引擎名：`get_round_precision`

### GetSharpLimit

```csharp
public float GetSharpLimit()
```

引擎名：`get_sharp_limit`

### GetTexture

```csharp
public Texture2D GetTexture()
```

引擎名：`get_texture`

### GetTextureMode

```csharp
public Line2D.LineTextureMode GetTextureMode()
```

引擎名：`get_texture_mode`

### GetWidth

```csharp
public float GetWidth()
```

引擎名：`get_width`

### IsClosed

```csharp
public bool IsClosed()
```

引擎名：`is_closed`

### RemovePoint

```csharp
public void RemovePoint(int index)
```

引擎名：`remove_point`

Removes the point at index `index` from the polyline.

### SetAntialiased

```csharp
public void SetAntialiased(bool antialiased)
```

引擎名：`set_antialiased`

### SetBeginCapMode

```csharp
public void SetBeginCapMode(Line2D.LineCapMode mode)
```

引擎名：`set_begin_cap_mode`

### SetClosed

```csharp
public void SetClosed(bool closed)
```

引擎名：`set_closed`

### SetCurve

```csharp
public void SetCurve(Curve curve)
```

引擎名：`set_curve`

### SetDefaultColor

```csharp
public void SetDefaultColor(Color color)
```

引擎名：`set_default_color`

### SetEndCapMode

```csharp
public void SetEndCapMode(Line2D.LineCapMode mode)
```

引擎名：`set_end_cap_mode`

### SetGradient

```csharp
public void SetGradient(Gradient color)
```

引擎名：`set_gradient`

### SetJointMode

```csharp
public void SetJointMode(Line2D.LineJointMode mode)
```

引擎名：`set_joint_mode`

### SetPointPosition

```csharp
public void SetPointPosition(int index, Vector2 position)
```

引擎名：`set_point_position`

Overwrites the position of the point at the given `index` with the supplied `position`.

### SetPoints

```csharp
public void SetPoints(ReadOnlySpan<Vector2> points)
```

引擎名：`set_points`

### SetPoints

```csharp
public void SetPoints(Vector2[] points)
```

引擎名：`set_points`

### SetRoundPrecision

```csharp
public void SetRoundPrecision(int precision)
```

引擎名：`set_round_precision`

### SetSharpLimit

```csharp
public void SetSharpLimit(float limit)
```

引擎名：`set_sharp_limit`

### SetTexture

```csharp
public void SetTexture(Texture2D texture)
```

引擎名：`set_texture`

### SetTextureMode

```csharp
public void SetTextureMode(Line2D.LineTextureMode mode)
```

引擎名：`set_texture_mode`

### SetWidth

```csharp
public void SetWidth(float width)
```

引擎名：`set_width`

## LineCapMode

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Draws no line cap.
- `Box = 1`：Draws the line cap as a box, slightly extending the first/last segment.
- `Round = 2`：Draws the line cap as a semicircle attached to the first/last segment.

## LineJointMode

命名空间 `Godot`；enum

### 枚举值

- `Sharp = 0`：Makes the polyline's joints pointy, connecting the sides of the two segments by extending them until they intersect.
- `Bevel = 1`：Makes the polyline's joints bevelled/chamfered, connecting the sides of the two segments with a simple line.
- `Round = 2`：Makes the polyline's joints rounded, connecting the sides of the two segments with an arc.

## LineTextureMode

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Takes the left pixels of the texture and renders them over the whole polyline.
- `Tile = 1`：Tiles the texture over the polyline.
- `Stretch = 2`：Stretches the texture across the polyline.
