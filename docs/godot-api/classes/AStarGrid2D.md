<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AStarGrid2D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`AStarGrid2D` is a variant of [`AStar2D`](AStar2D.md) that is specialized for partial 2D grids. It is simpler to use because it doesn't require you to manually create points and connect them together. This class also supports multiple types of heuristics, modes for diagonal movement, and a jumping mode to speed up calculations.

To use `AStarGrid2D`, you only need to set the `Region` of the grid, optionally set the `CellSize`, and then call the `Update()` method:

```csharp
AStarGrid2D astarGrid = new AStarGrid2D();
astarGrid.Region = new Rect2I(0, 0, 32, 32);
astarGrid.CellSize = new Vector2I(16, 16);
astarGrid.Update();
GD.Print(astarGrid.GetIdPath(Vector2I.Zero, new Vector2I(3, 4))); // Prints [(0, 0), (1, 1), (2, 2), (3, 3), (3, 4)]
GD.Print(astarGrid.GetPointPath(Vector2I.Zero, new Vector2I(3, 4))); // Prints [(0, 0), (16, 16), (32, 32), (48, 48), (48, 64)]

```

To remove a point from the pathfinding grid, it must be set as "solid" with `SetPointSolid()`.

## 构造函数

### AStarGrid2D

```csharp
public AStarGrid2D()
```

## 属性

### CellShape

```csharp
public AStarGrid2D.CellShapeEnum CellShape { get; set; }
```

引擎名：`cell_shape`

The cell shape. Affects how the positions are placed in the grid. If changed, `Update()` needs to be called before finding the next path.

### CellSize

```csharp
public Vector2 CellSize { get; set; }
```

引擎名：`cell_size`

The size of the point cell which will be applied to calculate the resulting point position returned by `GetPointPath()`. If changed, `Update()` needs to be called before finding the next path.

### DefaultComputeHeuristic

```csharp
public AStarGrid2D.Heuristic DefaultComputeHeuristic { get; set; }
```

引擎名：`default_compute_heuristic`

The default [`AStarGrid2D.Heuristic`](AStarGrid2D.md) which will be used to calculate the cost between two points if `_ComputeCost()` was not overridden.

### DefaultEstimateHeuristic

```csharp
public AStarGrid2D.Heuristic DefaultEstimateHeuristic { get; set; }
```

引擎名：`default_estimate_heuristic`

The default [`AStarGrid2D.Heuristic`](AStarGrid2D.md) which will be used to calculate the cost between the point and the end point if `_EstimateCost()` was not overridden.

### DiagonalMode

```csharp
public AStarGrid2D.DiagonalModeEnum DiagonalMode { get; set; }
```

引擎名：`diagonal_mode`

A specific [`AStarGrid2D.DiagonalModeEnum`](AStarGrid2D.md) mode which will force the path to avoid or accept the specified diagonals.

### JumpingEnabled

```csharp
public bool JumpingEnabled { get; set; }
```

引擎名：`jumping_enabled`

Enables or disables jumping to skip up the intermediate points and speeds up the searching algorithm.

**Note:** Currently, toggling it on disables the consideration of weight scaling in pathfinding.

### Offset

```csharp
public Vector2 Offset { get; set; }
```

引擎名：`offset`

The offset of the grid which will be applied to calculate the resulting point position returned by `GetPointPath()`. If changed, `Update()` needs to be called before finding the next path.

### Region

```csharp
public Rect2I Region { get; set; }
```

引擎名：`region`

The region of grid cells available for pathfinding. If changed, `Update()` needs to be called before finding the next path.

### Size

```csharp
public Vector2I Size { get; set; }
```

引擎名：`size`

The size of the grid (number of cells of size `CellSize` on each axis). If changed, `Update()` needs to be called before finding the next path.

## 方法

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears the grid and sets the `Region` to `Rect2i(0, 0, 0, 0)`.

### FillSolidRegion

```csharp
public void FillSolidRegion(Rect2I region, bool solid = true)
```

引擎名：`fill_solid_region`

Fills the given `region` on the grid with the specified value for the solid flag.

**Note:** Calling `Update()` is not needed after the call of this function.

### FillWeightScaleRegion

```csharp
public void FillWeightScaleRegion(Rect2I region, float weightScale)
```

引擎名：`fill_weight_scale_region`

Fills the given `region` on the grid with the specified value for the weight scale.

**Note:** Calling `Update()` is not needed after the call of this function.

### GetCellShape

```csharp
public AStarGrid2D.CellShapeEnum GetCellShape()
```

引擎名：`get_cell_shape`

### GetCellSize

```csharp
public Vector2 GetCellSize()
```

引擎名：`get_cell_size`

### GetDefaultComputeHeuristic

```csharp
public AStarGrid2D.Heuristic GetDefaultComputeHeuristic()
```

引擎名：`get_default_compute_heuristic`

### GetDefaultEstimateHeuristic

```csharp
public AStarGrid2D.Heuristic GetDefaultEstimateHeuristic()
```

引擎名：`get_default_estimate_heuristic`

### GetDiagonalMode

```csharp
public AStarGrid2D.DiagonalModeEnum GetDiagonalMode()
```

引擎名：`get_diagonal_mode`

### GetIdPath

```csharp
public Array<Vector2I> GetIdPath(Vector2I fromId, Vector2I toId)
```

引擎名：`get_id_path`

Returns an array with the IDs of the points that form the path found by AStar2D between the given points. The array is ordered from the starting point to the ending point of the path.

If `fromId` point is disabled, returns an empty array (even if `from_id == to_id`).

If `fromId` point is not disabled, there is no valid path to the target, and `allowPartialPath` is `true`, returns a path to the point closest to the target that can be reached.

**Note:** When `allowPartialPath` is `true` and `toId` is solid the search may take an unusually long time to finish.

### GetIdPath

```csharp
public Array<Vector2I> GetIdPath(Vector2I fromId, Vector2I toId, bool allowPartialPath = false)
```

引擎名：`get_id_path`

Returns an array with the IDs of the points that form the path found by AStar2D between the given points. The array is ordered from the starting point to the ending point of the path.

If `fromId` point is disabled, returns an empty array (even if `from_id == to_id`).

If `fromId` point is not disabled, there is no valid path to the target, and `allowPartialPath` is `true`, returns a path to the point closest to the target that can be reached.

**Note:** When `allowPartialPath` is `true` and `toId` is solid the search may take an unusually long time to finish.

### GetOffset

```csharp
public Vector2 GetOffset()
```

引擎名：`get_offset`

### GetPointDataInRegion

```csharp
public Array<Dictionary> GetPointDataInRegion(Rect2I region)
```

引擎名：`get_point_data_in_region`

Returns an array of dictionaries with point data (`id`: [`Vector2I`](Vector2I.md), `position`: [`Vector2`](Vector2.md), `solid`: `System.Boolean`, `weight_scale`: `System.Single`) within a `region`.

### GetPointPath

```csharp
public Vector2[] GetPointPath(Vector2I fromId, Vector2I toId)
```

引擎名：`get_point_path`

Returns an array with the points that are in the path found by `AStarGrid2D` between the given points. The array is ordered from the starting point to the ending point of the path.

If `fromId` point is disabled, returns an empty array (even if `from_id == to_id`).

If `fromId` point is not disabled, there is no valid path to the target, and `allowPartialPath` is `true`, returns a path to the point closest to the target that can be reached.

**Note:** This method is not thread-safe; it can only be used from a single [`GodotThread`](GodotThread.md) at a given time. Consider using [`Mutex`](Mutex.md) to ensure exclusive access to one thread to avoid race conditions.

Additionally, when `allowPartialPath` is `true` and `toId` is solid the search may take an unusually long time to finish.

### GetPointPath

```csharp
public Vector2[] GetPointPath(Vector2I fromId, Vector2I toId, bool allowPartialPath = false)
```

引擎名：`get_point_path`

Returns an array with the points that are in the path found by `AStarGrid2D` between the given points. The array is ordered from the starting point to the ending point of the path.

If `fromId` point is disabled, returns an empty array (even if `from_id == to_id`).

If `fromId` point is not disabled, there is no valid path to the target, and `allowPartialPath` is `true`, returns a path to the point closest to the target that can be reached.

**Note:** This method is not thread-safe; it can only be used from a single [`GodotThread`](GodotThread.md) at a given time. Consider using [`Mutex`](Mutex.md) to ensure exclusive access to one thread to avoid race conditions.

Additionally, when `allowPartialPath` is `true` and `toId` is solid the search may take an unusually long time to finish.

### GetPointPosition

```csharp
public Vector2 GetPointPosition(Vector2I id)
```

引擎名：`get_point_position`

Returns the position of the point associated with the given `id`.

### GetPointWeightScale

```csharp
public float GetPointWeightScale(Vector2I id)
```

引擎名：`get_point_weight_scale`

Returns the weight scale of the point associated with the given `id`.

### GetRegion

```csharp
public Rect2I GetRegion()
```

引擎名：`get_region`

### GetSize

```csharp
public Vector2I GetSize()
```

引擎名：`get_size`

### IsDirty

```csharp
public bool IsDirty()
```

引擎名：`is_dirty`

Indicates that the grid parameters were changed and `Update()` needs to be called.

### IsInBounds

```csharp
public bool IsInBounds(int x, int y)
```

引擎名：`is_in_bounds`

Returns `true` if the `x` and `y` is a valid grid coordinate (id), i.e. if it is inside `Region`. Equivalent to `region.has_point(Vector2i(x, y))`.

### IsInBoundsv

```csharp
public bool IsInBoundsv(Vector2I id)
```

引擎名：`is_in_boundsv`

Returns `true` if the `id` vector is a valid grid coordinate, i.e. if it is inside `Region`. Equivalent to `region.has_point(id)`.

### IsJumpingEnabled

```csharp
public bool IsJumpingEnabled()
```

引擎名：`is_jumping_enabled`

### IsPointSolid

```csharp
public bool IsPointSolid(Vector2I id)
```

引擎名：`is_point_solid`

Returns `true` if a point is disabled for pathfinding. By default, all points are enabled.

### SetCellShape

```csharp
public void SetCellShape(AStarGrid2D.CellShapeEnum cellShape)
```

引擎名：`set_cell_shape`

### SetCellSize

```csharp
public void SetCellSize(Vector2 cellSize)
```

引擎名：`set_cell_size`

### SetDefaultComputeHeuristic

```csharp
public void SetDefaultComputeHeuristic(AStarGrid2D.Heuristic heuristic)
```

引擎名：`set_default_compute_heuristic`

### SetDefaultEstimateHeuristic

```csharp
public void SetDefaultEstimateHeuristic(AStarGrid2D.Heuristic heuristic)
```

引擎名：`set_default_estimate_heuristic`

### SetDiagonalMode

```csharp
public void SetDiagonalMode(AStarGrid2D.DiagonalModeEnum mode)
```

引擎名：`set_diagonal_mode`

### SetJumpingEnabled

```csharp
public void SetJumpingEnabled(bool enabled)
```

引擎名：`set_jumping_enabled`

### SetOffset

```csharp
public void SetOffset(Vector2 offset)
```

引擎名：`set_offset`

### SetPointSolid

```csharp
public void SetPointSolid(Vector2I id, bool solid = true)
```

引擎名：`set_point_solid`

Disables or enables the specified point for pathfinding. Useful for making an obstacle. By default, all points are enabled.

**Note:** Calling `Update()` is not needed after the call of this function.

### SetPointWeightScale

```csharp
public void SetPointWeightScale(Vector2I id, float weightScale)
```

引擎名：`set_point_weight_scale`

Sets the `weightScale` for the point with the given `id`. The `weightScale` is multiplied by the result of `_ComputeCost()` when determining the overall cost of traveling across a segment from a neighboring point to this point.

**Note:** Calling `Update()` is not needed after the call of this function.

### SetRegion

```csharp
public void SetRegion(Rect2I region)
```

引擎名：`set_region`

### SetSize

```csharp
public void SetSize(Vector2I size)
```

引擎名：`set_size`

### Update

```csharp
public void Update()
```

引擎名：`update`

Updates the internal state of the grid according to the parameters to prepare it to search the path. Needs to be called if parameters like `Region`, `CellSize` or `Offset` are changed. `IsDirty()` will return `true` if this is the case and this needs to be called.

**Note:** All point data (solidity and weight scale) will be cleared.

### _ComputeCost

```csharp
public virtual float _ComputeCost(Vector2I fromId, Vector2I toId)
```

引擎名：`_compute_cost`

Called when computing the cost between two connected points.

Note that this function is hidden in the default `AStarGrid2D` class.

### _EstimateCost

```csharp
public virtual float _EstimateCost(Vector2I fromId, Vector2I endId)
```

引擎名：`_estimate_cost`

Called when estimating the cost between a point and the path's ending point.

Note that this function is hidden in the default `AStarGrid2D` class.

## CellShapeEnum

命名空间 `Godot`；enum

### 枚举值

- `Square = 0`：Rectangular cell shape.
- `IsometricRight = 1`：Diamond cell shape (for isometric look).
- `IsometricDown = 2`：Diamond cell shape (for isometric look).
- `Max = 3`：Represents the size of the `AStarGrid2D.CellShapeEnum` enum.

## DiagonalModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Always = 0`：The pathfinding algorithm will ignore solid neighbors around the target cell and allow passing using diagonals.
- `Never = 1`：The pathfinding algorithm will ignore all diagonals and the way will be always orthogonal.
- `AtLeastOneWalkable = 2`：The pathfinding algorithm will avoid using diagonals if at least two obstacles have been placed around the neighboring cells of the specific path segment.
- `OnlyIfNoObstacles = 3`：The pathfinding algorithm will avoid using diagonals if any obstacle has been placed around the neighboring cells of the specific path segment.
- `Max = 4`：Represents the size of the `AStarGrid2D.DiagonalModeEnum` enum.

## Heuristic

命名空间 `Godot`；enum

### 枚举值

- `Euclidean = 0`：The [Euclidean heuristic](https://en.wikipedia.org/wiki/Euclidean_distance) to be used for the pathfinding using the following formula:
- `Manhattan = 1`：The [Manhattan heuristic](https://en.wikipedia.org/wiki/Taxicab_geometry) to be used for the pathfinding using the following formula:
- `Octile = 2`：The Octile heuristic to be used for the pathfinding using the following formula:
- `Chebyshev = 3`：The [Chebyshev heuristic](https://en.wikipedia.org/wiki/Chebyshev_distance) to be used for the pathfinding using the following formula:
- `Max = 4`：Represents the size of the `AStarGrid2D.Heuristic` enum.
