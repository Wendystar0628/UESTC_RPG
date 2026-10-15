<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeBlendSpace2D

命名空间 `Godot`；class；继承 [`AnimationRootNode`](AnimationRootNode.md) → [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A resource used by [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md).

`AnimationNodeBlendSpace2D` represents a virtual 2D space on which [`AnimationRootNode`](AnimationRootNode.md)s are placed. Outputs the linear blend of the three adjacent animations using a [`Vector2`](Vector2.md) weight. Adjacent in this context means the three [`AnimationRootNode`](AnimationRootNode.md)s making up the triangle that contains the current value.

You can add vertices to the blend space with `AddBlendPoint()` and automatically triangulate it by setting `AutoTriangles` to `true`. Otherwise, use `AddTriangle()` and `RemoveTriangle()` to triangulate the blend space by hand.

## 构造函数

### AnimationNodeBlendSpace2D

```csharp
public AnimationNodeBlendSpace2D()
```

## 属性

### AutoTriangles

```csharp
public bool AutoTriangles { get; set; }
```

引擎名：`auto_triangles`

If `true`, the blend space is triangulated automatically. The mesh updates every time you add or remove points with `AddBlendPoint()` and `RemoveBlendPoint()`.

### BlendMode

```csharp
public AnimationNodeBlendSpace2D.BlendModeEnum BlendMode { get; set; }
```

引擎名：`blend_mode`

Controls the interpolation between animations.

### CyclicLength

```csharp
public double CyclicLength { get; set; }
```

引擎名：`cyclic_length`

The cycle length in seconds used by [`AnimationNodeBlendSpace2D.SyncModeEnum.CyclicConstant`](AnimationNodeBlendSpace2D.md). All animations are time-scaled so they complete one full cycle in this duration. Must be greater than `0` for cyclic sync to take effect.

### MaxSpace

```csharp
public Vector2 MaxSpace { get; set; }
```

引擎名：`max_space`

The blend space's X and Y axes' upper limit for the points' position. See `AddBlendPoint()`.

### MinSpace

```csharp
public Vector2 MinSpace { get; set; }
```

引擎名：`min_space`

The blend space's X and Y axes' lower limit for the points' position. See `AddBlendPoint()`.

### Snap

```csharp
public Vector2 Snap { get; set; }
```

引擎名：`snap`

Position increment to snap to when moving a point.

### Sync

```csharp
public bool Sync { get; set; }
```

引擎名：`sync`

If `true`, sync mode is enabled (equivalent to [`AnimationNodeBlendSpace2D.SyncModeEnum.Independent`](AnimationNodeBlendSpace2D.md)). This property is kept for backward compatibility.

### SyncMode

```csharp
public AnimationNodeBlendSpace2D.SyncModeEnum SyncMode { get; set; }
```

引擎名：`sync_mode`

Controls how animations are synced when blended. See [`AnimationNodeBlendSpace2D.SyncModeEnum`](AnimationNodeBlendSpace2D.md) for available options.

### Triangles

```csharp
public int[] Triangles { get; set; }
```

引擎名：`triangles`

### XLabel

```csharp
public string XLabel { get; set; }
```

引擎名：`x_label`

Name of the blend space's X axis.

### YLabel

```csharp
public string YLabel { get; set; }
```

引擎名：`y_label`

Name of the blend space's Y axis.

## 方法

### AddBlendPoint

```csharp
public void AddBlendPoint(AnimationRootNode node, Vector2 pos, int atIndex = -1, StringName name = null)
```

引擎名：`add_blend_point`

Adds a new point with `name` that represents a `node` at the position set by `pos`. You can insert it at a specific index using the `atIndex` argument. If you use the default value for `atIndex`, the point is inserted at the end of the blend points array.

**Note:** If no name is provided, safe index is used as reference. In the future, empty names will be deprecated, so explicitly passing a name is recommended.

### AddBlendPoint

```csharp
public void AddBlendPoint(AnimationRootNode node, Vector2 pos, int atIndex)
```

引擎名：`add_blend_point`

Adds a new point with `name` that represents a `node` at the position set by `pos`. You can insert it at a specific index using the `atIndex` argument. If you use the default value for `atIndex`, the point is inserted at the end of the blend points array.

**Note:** If no name is provided, safe index is used as reference. In the future, empty names will be deprecated, so explicitly passing a name is recommended.

### AddTriangle

```csharp
public void AddTriangle(int x, int y, int z, int atIndex = -1)
```

引擎名：`add_triangle`

Creates a new triangle using three points `x`, `y`, and `z`. Triangles can overlap. You can insert the triangle at a specific index using the `atIndex` argument. If you use the default value for `atIndex`, the point is inserted at the end of the blend points array.

### FindBlendPointByName

```csharp
public int FindBlendPointByName(StringName name)
```

引擎名：`find_blend_point_by_name`

Returns the index of the blend point with the given `name`. Returns `-1` if no blend point with that name is found.

### GetAutoTriangles

```csharp
public bool GetAutoTriangles()
```

引擎名：`get_auto_triangles`

### GetBlendMode

```csharp
public AnimationNodeBlendSpace2D.BlendModeEnum GetBlendMode()
```

引擎名：`get_blend_mode`

### GetBlendPointCount

```csharp
public int GetBlendPointCount()
```

引擎名：`get_blend_point_count`

Returns the number of points in the blend space.

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

Returns the [`AnimationRootNode`](AnimationRootNode.md) referenced by the point at index `point`.

### GetBlendPointPosition

```csharp
public Vector2 GetBlendPointPosition(int point)
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
public Vector2 GetMaxSpace()
```

引擎名：`get_max_space`

### GetMinSpace

```csharp
public Vector2 GetMinSpace()
```

引擎名：`get_min_space`

### GetSnap

```csharp
public Vector2 GetSnap()
```

引擎名：`get_snap`

### GetSyncMode

```csharp
public AnimationNodeBlendSpace2D.SyncModeEnum GetSyncMode()
```

引擎名：`get_sync_mode`

### GetTriangleCount

```csharp
public int GetTriangleCount()
```

引擎名：`get_triangle_count`

Returns the number of triangles in the blend space.

### GetTrianglePoint

```csharp
public int GetTrianglePoint(int triangle, int point)
```

引擎名：`get_triangle_point`

Returns the position of the point at index `point` in the triangle of index `triangle`.

### GetXLabel

```csharp
public string GetXLabel()
```

引擎名：`get_x_label`

### GetYLabel

```csharp
public string GetYLabel()
```

引擎名：`get_y_label`

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

Removes the point at index `point` from the blend space.

### RemoveTriangle

```csharp
public void RemoveTriangle(int triangle)
```

引擎名：`remove_triangle`

Removes the triangle at index `triangle` from the blend space.

### ReorderBlendPoint

```csharp
public void ReorderBlendPoint(int fromIndex, int toIndex)
```

引擎名：`reorder_blend_point`

Swaps the blend points at indices `fromIndex` and `toIndex`, exchanging their positions and properties.

### SetAutoTriangles

```csharp
public void SetAutoTriangles(bool enable)
```

引擎名：`set_auto_triangles`

### SetBlendMode

```csharp
public void SetBlendMode(AnimationNodeBlendSpace2D.BlendModeEnum mode)
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
public void SetBlendPointPosition(int point, Vector2 pos)
```

引擎名：`set_blend_point_position`

Updates the position of the point at index `point` in the blend space.

### SetCyclicLength

```csharp
public void SetCyclicLength(double length)
```

引擎名：`set_cyclic_length`

### SetMaxSpace

```csharp
public void SetMaxSpace(Vector2 maxSpace)
```

引擎名：`set_max_space`

### SetMinSpace

```csharp
public void SetMinSpace(Vector2 minSpace)
```

引擎名：`set_min_space`

### SetSnap

```csharp
public void SetSnap(Vector2 snap)
```

引擎名：`set_snap`

### SetSyncMode

```csharp
public void SetSyncMode(AnimationNodeBlendSpace2D.SyncModeEnum syncMode)
```

引擎名：`set_sync_mode`

### SetUseSync

```csharp
public void SetUseSync(bool enable)
```

引擎名：`set_use_sync`

### SetXLabel

```csharp
public void SetXLabel(string text)
```

引擎名：`set_x_label`

### SetYLabel

```csharp
public void SetYLabel(string text)
```

引擎名：`set_y_label`

## 事件（信号）

### TrianglesUpdated

```csharp
public event Action TrianglesUpdated
```

引擎名：`triangles_updated`

Emitted every time the blend space's triangles are created, removed, or when one of their vertices changes position.

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
- `CyclicConstant = 3`：All animations are time-scaled so they complete one cycle in [`AnimationNodeBlendSpace2D.CyclicLength`](AnimationNodeBlendSpace2D.md) seconds, keeping them in sync regardless of their individual lengths.
