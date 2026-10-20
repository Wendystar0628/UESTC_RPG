<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeBlendSpace1D

命名空间 `Godot`；class；继承 [`AnimationRootNode`](AnimationRootNode.md) → [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A resource used by [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md).

`AnimationNodeBlendSpace1D` represents a virtual axis on which any type of [`AnimationRootNode`](AnimationRootNode.md)s can be added using `AddBlendPoint()`. Outputs the linear blend of the two [`AnimationRootNode`](AnimationRootNode.md)s adjacent to the current value.

You can set the extents of the axis with `MinSpace` and `MaxSpace`.

## 构造函数

### AnimationNodeBlendSpace1D

```csharp
public AnimationNodeBlendSpace1D()
```

## 属性

### BlendMode

```csharp
public AnimationNodeBlendSpace1D.BlendModeEnum BlendMode { get; set; }
```

引擎名：`blend_mode`

Controls the interpolation between animations.

### CyclicLength

```csharp
public double CyclicLength { get; set; }
```

引擎名：`cyclic_length`

The cycle length in seconds used by [`AnimationNodeBlendSpace1D.SyncModeEnum.CyclicConstant`](AnimationNodeBlendSpace1D.md). All animations are time-scaled so they complete one full cycle in this duration. Must be greater than `0` for cyclic sync to take effect.

### MaxSpace

```csharp
public float MaxSpace { get; set; }
```

引擎名：`max_space`

The blend space's axis's upper limit for the points' position. See `AddBlendPoint()`.

### MinSpace

```csharp
public float MinSpace { get; set; }
```

引擎名：`min_space`

The blend space's axis's lower limit for the points' position. See `AddBlendPoint()`.

### Snap

```csharp
public float Snap { get; set; }
```

引擎名：`snap`

Position increment to snap to when moving a point on the axis.

### Sync

```csharp
public bool Sync { get; set; }
```

引擎名：`sync`

If `true`, sync mode is enabled (equivalent to [`AnimationNodeBlendSpace1D.SyncModeEnum.Independent`](AnimationNodeBlendSpace1D.md)). This property is kept for backward compatibility.

### SyncMode

```csharp
public AnimationNodeBlendSpace1D.SyncModeEnum SyncMode { get; set; }
```

引擎名：`sync_mode`

Controls how animations are synced when blended. See [`AnimationNodeBlendSpace1D.SyncModeEnum`](AnimationNodeBlendSpace1D.md) for available options.

### ValueLabel

```csharp
public string ValueLabel { get; set; }
```

引擎名：`value_label`

Label of the virtual axis of the blend space.

## 方法

### AddBlendPoint

```csharp
public void AddBlendPoint(AnimationRootNode node, float pos, int atIndex = -1, StringName name = null)
```

引擎名：`add_blend_point`

Adds a new point with `name` that represents a `node` on the virtual axis at a given position set by `pos`. You can insert it at a specific index using the `atIndex` argument. If you use the default value for `atIndex`, the point is inserted at the end of the blend points array.

**Note:** If no name is provided, safe index is used as reference. In the future, empty names will be deprecated, so explicitly passing a name is recommended.

### AddBlendPoint

```csharp
public void AddBlendPoint(AnimationRootNode node, float pos, int atIndex)
```

引擎名：`add_blend_point`

Adds a new point with `name` that represents a `node` on the virtual axis at a given position set by `pos`. You can insert it at a specific index using the `atIndex` argument. If you use the default value for `atIndex`, the point is inserted at the end of the blend points array.

**Note:** If no name is provided, safe index is used as reference. In the future, empty names will be deprecated, so explicitly passing a name is recommended.

### FindBlendPointByName

```csharp
public int FindBlendPointByName(StringName name)
```

引擎名：`find_blend_point_by_name`

Returns the index of the blend point with the given `name`. Returns `-1` if no blend point with that name is found.

### GetBlendMode

```csharp
public AnimationNodeBlendSpace1D.BlendModeEnum GetBlendMode()
```

引擎名：`get_blend_mode`

### GetBlendPointCount

```csharp
public int GetBlendPointCount()
```

引擎名：`get_blend_point_count`

Returns the number of points on the blend axis.

### GetBlendPointName

```csharp
public StringName GetBlendPointName(int point)
```

引擎名：`get_blend_point_name`

Returns the name of the blend point at index `point`.

### GetBlendPointNode

```csharp
public AnimationRootNode GetBlendPointNode(int point)
```

引擎名：`get_blend_point_node`

Returns the [`AnimationNode`](AnimationNode.md) referenced by the point at index `point`.

### GetBlendPointPosition

```csharp
public float GetBlendPointPosition(int point)
```

引擎名：`get_blend_point_position`

Returns the position of the point at index `point`.

### GetCyclicLength

```csharp
public double GetCyclicLength()
```

引擎名：`get_cyclic_length`

### GetMaxSpace

```csharp
public float GetMaxSpace()
```

引擎名：`get_max_space`

### GetMinSpace

```csharp
public float GetMinSpace()
```

引擎名：`get_min_space`

### GetSnap

```csharp
public float GetSnap()
```

引擎名：`get_snap`

### GetSyncMode

```csharp
public AnimationNodeBlendSpace1D.SyncModeEnum GetSyncMode()
```

引擎名：`get_sync_mode`

### GetValueLabel

```csharp
public string GetValueLabel()
```

引擎名：`get_value_label`

### IsUsingSync

```csharp
public bool IsUsingSync()
```

引擎名：`is_using_sync`

### RemoveBlendPoint

```csharp
public void RemoveBlendPoint(int point)
```

引擎名：`remove_blend_point`

Removes the point at index `point` from the blend axis.

### ReorderBlendPoint

```csharp
public void ReorderBlendPoint(int fromIndex, int toIndex)
```

引擎名：`reorder_blend_point`

Swaps the blend points at indices `fromIndex` and `toIndex`, exchanging their positions and properties.

### SetBlendMode

```csharp
public void SetBlendMode(AnimationNodeBlendSpace1D.BlendModeEnum mode)
```

引擎名：`set_blend_mode`

### SetBlendPointName

```csharp
public void SetBlendPointName(int point, StringName name)
```

引擎名：`set_blend_point_name`

Sets the name of the blend point at index `point`. If the name conflicts with an existing point, a unique name will be generated automatically.

### SetBlendPointNode

```csharp
public void SetBlendPointNode(int point, AnimationRootNode node)
```

引擎名：`set_blend_point_node`

Changes the [`AnimationNode`](AnimationNode.md) referenced by the point at index `point`.

### SetBlendPointPosition

```csharp
public void SetBlendPointPosition(int point, float pos)
```

引擎名：`set_blend_point_position`

Updates the position of the point at index `point` on the blend axis.

### SetCyclicLength

```csharp
public void SetCyclicLength(double length)
```

引擎名：`set_cyclic_length`

### SetMaxSpace

```csharp
public void SetMaxSpace(float maxSpace)
```

引擎名：`set_max_space`

### SetMinSpace

```csharp
public void SetMinSpace(float minSpace)
```

引擎名：`set_min_space`

### SetSnap

```csharp
public void SetSnap(float snap)
```

引擎名：`set_snap`

### SetSyncMode

```csharp
public void SetSyncMode(AnimationNodeBlendSpace1D.SyncModeEnum syncMode)
```

引擎名：`set_sync_mode`

### SetUseSync

```csharp
public void SetUseSync(bool enable)
```

引擎名：`set_use_sync`

### SetValueLabel

```csharp
public void SetValueLabel(string text)
```

引擎名：`set_value_label`

## BlendModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Interpolated = 0`：The interpolation between animations is linear.
- `Discrete = 1`：The blend space plays the animation of the animation node which blending position is closest to.
- `DiscreteCarry = 2`：Similar to `Discrete`, but starts the new animation at the last animation's playback position.

## SyncModeEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Inactive animations are frozen and do not advance.
- `Independent = 1`：Inactive animations advance with a weight of `0`.
- `CyclicMutable = 2`：All animations are time-scaled so they stay in sync, with the cycle length dynamically computed from active blend weights.
- `CyclicConstant = 3`：All animations are time-scaled so they complete one cycle in [`AnimationNodeBlendSpace1D.CyclicLength`](AnimationNodeBlendSpace1D.md) seconds, keeping them in sync regardless of their individual lengths.
