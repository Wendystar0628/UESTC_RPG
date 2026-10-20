<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PathFollow2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node takes its parent [`Path2D`](Path2D.md), and returns the coordinates of a point within it, given a distance from the first vertex.

It is useful for making other nodes follow a path, without coding the movement pattern. For that, the nodes must be children of this node. The descendant nodes will then move accordingly when setting the `Progress` in this node.

## 构造函数

### PathFollow2D

```csharp
public PathFollow2D()
```

## 属性

### CubicInterp

```csharp
public bool CubicInterp { get; set; }
```

引擎名：`cubic_interp`

If `true`, the position between two cached points is interpolated cubically, and linearly otherwise.

The points along the [`Curve2D`](Curve2D.md) of the [`Path2D`](Path2D.md) are precomputed before use, for faster calculations. The point at the requested offset is then calculated interpolating between two adjacent cached points. This may present a problem if the curve makes sharp turns, as the cached points may not follow the curve closely enough.

There are two answers to this problem: either increase the number of cached points and increase memory consumption, or make a cubic interpolation between two points at the cost of (slightly) slower calculations.

### HOffset

```csharp
public float HOffset { get; set; }
```

引擎名：`h_offset`

The node's offset along the curve.

### Loop

```csharp
public bool Loop { get; set; }
```

引擎名：`loop`

If `true`, any offset outside the path's length will wrap around, instead of stopping at the ends. Use it for cyclic paths.

### Progress

```csharp
public float Progress { get; set; }
```

引擎名：`progress`

The distance along the path, in pixels. Changing this value sets this node's position to a point within the path.

### ProgressRatio

```csharp
public float ProgressRatio { get; set; }
```

引擎名：`progress_ratio`

The distance along the path as a number in the range 0.0 (for the first vertex) to 1.0 (for the last). This is just another way of expressing the progress within the path, as the offset supplied is multiplied internally by the path's length.

It can be set or get only if the `PathFollow2D` is the child of a [`Path2D`](Path2D.md) which is part of the scene tree, and that this [`Path2D`](Path2D.md) has a [`Curve2D`](Curve2D.md) with a non-zero length. Otherwise, trying to set this field will print an error, and getting this field will return `0.0`.

### Rotates

```csharp
public bool Rotates { get; set; }
```

引擎名：`rotates`

If `true`, this node rotates to follow the path, with the +X direction facing forward on the path.

### VOffset

```csharp
public float VOffset { get; set; }
```

引擎名：`v_offset`

The node's offset perpendicular to the curve.

## 方法

### GetCubicInterpolation

```csharp
public bool GetCubicInterpolation()
```

引擎名：`get_cubic_interpolation`

### GetHOffset

```csharp
public float GetHOffset()
```

引擎名：`get_h_offset`

### GetProgress

```csharp
public float GetProgress()
```

引擎名：`get_progress`

### GetProgressRatio

```csharp
public float GetProgressRatio()
```

引擎名：`get_progress_ratio`

### GetVOffset

```csharp
public float GetVOffset()
```

引擎名：`get_v_offset`

### HasLoop

```csharp
public bool HasLoop()
```

引擎名：`has_loop`

### IsRotating

```csharp
public bool IsRotating()
```

引擎名：`is_rotating`

### SetCubicInterpolation

```csharp
public void SetCubicInterpolation(bool enabled)
```

引擎名：`set_cubic_interpolation`

### SetHOffset

```csharp
public void SetHOffset(float hOffset)
```

引擎名：`set_h_offset`

### SetLoop

```csharp
public void SetLoop(bool loop)
```

引擎名：`set_loop`

### SetProgress

```csharp
public void SetProgress(float progress)
```

引擎名：`set_progress`

### SetProgressRatio

```csharp
public void SetProgressRatio(float ratio)
```

引擎名：`set_progress_ratio`

### SetRotates

```csharp
public void SetRotates(bool enabled)
```

引擎名：`set_rotates`

### SetVOffset

```csharp
public void SetVOffset(float vOffset)
```

引擎名：`set_v_offset`
