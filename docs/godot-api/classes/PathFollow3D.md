<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PathFollow3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node takes its parent [`Path3D`](Path3D.md), and returns the coordinates of a point within it, given a distance from the first vertex.

It is useful for making other nodes follow a path, without coding the movement pattern. For that, the nodes must be children of this node. The descendant nodes will then move accordingly when setting the `Progress` in this node.

## 构造函数

### PathFollow3D

```csharp
public PathFollow3D()
```

## 属性

### CubicInterp

```csharp
public bool CubicInterp { get; set; }
```

引擎名：`cubic_interp`

If `true`, the position between two cached points is interpolated cubically, and linearly otherwise.

The points along the [`Curve3D`](Curve3D.md) of the [`Path3D`](Path3D.md) are precomputed before use, for faster calculations. The point at the requested offset is then calculated interpolating between two adjacent cached points. This may present a problem if the curve makes sharp turns, as the cached points may not follow the curve closely enough.

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

The distance from the first vertex, measured in 3D units along the path. Changing this value sets this node's position to a point within the path.

### ProgressRatio

```csharp
public float ProgressRatio { get; set; }
```

引擎名：`progress_ratio`

The distance from the first vertex, considering 0.0 as the first vertex and 1.0 as the last. This is just another way of expressing the progress within the path, as the progress supplied is multiplied internally by the path's length.

It can be set or get only if the `PathFollow3D` is the child of a [`Path3D`](Path3D.md) which is part of the scene tree, and that this [`Path3D`](Path3D.md) has a [`Curve3D`](Curve3D.md) with a non-zero length. Otherwise, trying to set this field will print an error, and getting this field will return `0.0`.

### RotationMode

```csharp
public PathFollow3D.RotationModeEnum RotationMode { get; set; }
```

引擎名：`rotation_mode`

Allows or forbids rotation on one or more axes, depending on the [`PathFollow3D.RotationModeEnum`](PathFollow3D.md) constants being used.

### TiltEnabled

```csharp
public bool TiltEnabled { get; set; }
```

引擎名：`tilt_enabled`

If `true`, the tilt property of [`Curve3D`](Curve3D.md) takes effect.

### UseModelFront

```csharp
public bool UseModelFront { get; set; }
```

引擎名：`use_model_front`

If `true`, the node moves on the travel path with orienting the +Z axis as forward. See also `Vector3.Forward` and `Vector3.ModelFront`.

### VOffset

```csharp
public float VOffset { get; set; }
```

引擎名：`v_offset`

The node's offset perpendicular to the curve.

## 方法

### CorrectPosture

```csharp
public static Transform3D CorrectPosture(Transform3D transform, PathFollow3D.RotationModeEnum rotationMode)
```

引擎名：`correct_posture`

Correct the `transform`. `rotationMode` implicitly specifies how posture (forward, up and sideway direction) is calculated.

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

### GetRotationMode

```csharp
public PathFollow3D.RotationModeEnum GetRotationMode()
```

引擎名：`get_rotation_mode`

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

### IsTiltEnabled

```csharp
public bool IsTiltEnabled()
```

引擎名：`is_tilt_enabled`

### IsUsingModelFront

```csharp
public bool IsUsingModelFront()
```

引擎名：`is_using_model_front`

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

### SetRotationMode

```csharp
public void SetRotationMode(PathFollow3D.RotationModeEnum rotationMode)
```

引擎名：`set_rotation_mode`

### SetTiltEnabled

```csharp
public void SetTiltEnabled(bool enabled)
```

引擎名：`set_tilt_enabled`

### SetUseModelFront

```csharp
public void SetUseModelFront(bool enabled)
```

引擎名：`set_use_model_front`

### SetVOffset

```csharp
public void SetVOffset(float vOffset)
```

引擎名：`set_v_offset`

## RotationModeEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Forbids the PathFollow3D to rotate.
- `Y = 1`：Allows the PathFollow3D to rotate in the Y axis only.
- `Xy = 2`：Allows the PathFollow3D to rotate in both the X, and Y axes.
- `Xyz = 3`：Allows the PathFollow3D to rotate in any axis.
- `Oriented = 4`：Uses the up vector information in a [`Curve3D`](Curve3D.md) to enforce orientation.
