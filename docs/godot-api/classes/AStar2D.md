<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AStar2D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An implementation of the A* algorithm, used to find the shortest path between two vertices on a connected graph in 2D space.

See [`AStar3D`](AStar3D.md) for a more thorough explanation on how to use this class. `AStar2D` is a wrapper for [`AStar3D`](AStar3D.md) that enforces 2D coordinates.

## 构造函数

### AStar2D

```csharp
public AStar2D()
```

## 属性

### NeighborFilterEnabled

```csharp
public bool NeighborFilterEnabled { get; set; }
```

引擎名：`neighbor_filter_enabled`

If `true` enables the filtering of neighbors via `_FilterNeighbor()`.

## 方法

### AddPoint

```csharp
public void AddPoint(long id, Vector2 position, float weightScale = 1f)
```

引擎名：`add_point`

Adds a new point at the given position with the given identifier. The `id` must be 0 or larger, and the `weightScale` must be 0.0 or greater.

The `weightScale` is multiplied by the result of `_ComputeCost()` when determining the overall cost of traveling across a segment from a neighboring point to this point. Thus, all else being equal, the algorithm prefers points with lower `weightScale`s to form a path.

```csharp
var astar = new AStar2D();
astar.AddPoint(1, new Vector2(1, 0), 4); // Adds the point (1, 0) with weight_scale 4 and id 1

```

If there already exists a point for the given `id`, its position and weight scale are updated to the given values.

### ArePointsConnected

```csharp
public bool ArePointsConnected(long id, long toId, bool bidirectional = true)
```

引擎名：`are_points_connected`

Returns whether there is a connection/segment between the given points. If `bidirectional` is `false`, returns whether movement from `id` to `toId` is possible through this segment.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears all the points and segments.

### ConnectPoints

```csharp
public void ConnectPoints(long id, long toId, bool bidirectional = true)
```

引擎名：`connect_points`

Creates a segment between the given points. If `bidirectional` is `false`, only movement from `id` to `toId` is allowed, not the reverse direction.

```csharp
var astar = new AStar2D();
astar.AddPoint(1, new Vector2(1, 1));
astar.AddPoint(2, new Vector2(0, 5));
astar.ConnectPoints(1, 2, false);

```

### DisconnectPoints

```csharp
public void DisconnectPoints(long id, long toId, bool bidirectional = true)
```

引擎名：`disconnect_points`

Deletes the segment between the given points. If `bidirectional` is `false`, only movement from `id` to `toId` is prevented, and a unidirectional segment possibly remains.

### GetAvailablePointId

```csharp
public long GetAvailablePointId()
```

引擎名：`get_available_point_id`

Returns the next available point ID with no point associated to it.

### GetClosestPoint

```csharp
public long GetClosestPoint(Vector2 toPosition, bool includeDisabled = false)
```

引擎名：`get_closest_point`

Returns the ID of the closest point to `toPosition`, optionally taking disabled points into account. Returns `-1` if there are no points in the points pool.

**Note:** If several points are the closest to `toPosition`, the one with the smallest ID will be returned, ensuring a deterministic result.

### GetClosestPositionInSegment

```csharp
public Vector2 GetClosestPositionInSegment(Vector2 toPosition)
```

引擎名：`get_closest_position_in_segment`

Returns the closest position to `toPosition` that resides inside a segment between two connected points.

```csharp
var astar = new AStar2D();
astar.AddPoint(1, new Vector2(0, 0));
astar.AddPoint(2, new Vector2(0, 5));
astar.ConnectPoints(1, 2);
Vector2 res = astar.GetClosestPositionInSegment(new Vector2(3, 3)); // Returns (0, 3)

```

The result is in the segment that goes from `y = 0` to `y = 5`. It's the closest position in the segment to the given point.

### GetIdPath

```csharp
public long[] GetIdPath(long fromId, long toId)
```

引擎名：`get_id_path`

Returns an array with the IDs of the points that form the path found by AStar2D between the given points. The array is ordered from the starting point to the ending point of the path.

If `fromId` point is disabled, returns an empty array (even if `from_id == to_id`).

If `fromId` point is not disabled, there is no valid path to the target, and `allowPartialPath` is `true`, returns a path to the point closest to the target that can be reached.

**Note:** When `allowPartialPath` is `true` and `toId` is disabled the search may take an unusually long time to finish.

```csharp
var astar = new AStar2D();
astar.AddPoint(1, new Vector2(0, 0));
astar.AddPoint(2, new Vector2(0, 1), 1); // Default weight is 1
astar.AddPoint(3, new Vector2(1, 1));
astar.AddPoint(4, new Vector2(2, 0));

astar.ConnectPoints(1, 2, false);
astar.ConnectPoints(2, 3, false);
astar.ConnectPoints(4, 3, false);
astar.ConnectPoints(1, 4, false);
long[] res = astar.GetIdPath(1, 3); // Returns [1, 2, 3]

```

If you change the 2nd point's weight to 3, then the result will be `[1, 4, 3]` instead, because now even though the distance is longer, it's "easier" to get through point 4 than through point 2.

### GetIdPath

```csharp
public long[] GetIdPath(long fromId, long toId, bool allowPartialPath = false)
```

引擎名：`get_id_path`

Returns an array with the IDs of the points that form the path found by AStar2D between the given points. The array is ordered from the starting point to the ending point of the path.

If `fromId` point is disabled, returns an empty array (even if `from_id == to_id`).

If `fromId` point is not disabled, there is no valid path to the target, and `allowPartialPath` is `true`, returns a path to the point closest to the target that can be reached.

**Note:** When `allowPartialPath` is `true` and `toId` is disabled the search may take an unusually long time to finish.

```csharp
var astar = new AStar2D();
astar.AddPoint(1, new Vector2(0, 0));
astar.AddPoint(2, new Vector2(0, 1), 1); // Default weight is 1
astar.AddPoint(3, new Vector2(1, 1));
astar.AddPoint(4, new Vector2(2, 0));

astar.ConnectPoints(1, 2, false);
astar.ConnectPoints(2, 3, false);
astar.ConnectPoints(4, 3, false);
astar.ConnectPoints(1, 4, false);
long[] res = astar.GetIdPath(1, 3); // Returns [1, 2, 3]

```

If you change the 2nd point's weight to 3, then the result will be `[1, 4, 3]` instead, because now even though the distance is longer, it's "easier" to get through point 4 than through point 2.

### GetPointCapacity

```csharp
public long GetPointCapacity()
```

引擎名：`get_point_capacity`

Returns the capacity of the structure backing the points, useful in conjunction with `ReserveSpace()`.

### GetPointConnections

```csharp
public long[] GetPointConnections(long id)
```

引擎名：`get_point_connections`

Returns an array with the IDs of the points that form the connection with the given point.

```csharp
var astar = new AStar2D();
astar.AddPoint(1, new Vector2(0, 0));
astar.AddPoint(2, new Vector2(0, 1));
astar.AddPoint(3, new Vector2(1, 1));
astar.AddPoint(4, new Vector2(2, 0));

astar.ConnectPoints(1, 2, true);
astar.ConnectPoints(1, 3, true);

long[] neighbors = astar.GetPointConnections(1); // Returns [2, 3]

```

### GetPointCount

```csharp
public long GetPointCount()
```

引擎名：`get_point_count`

Returns the number of points currently in the points pool.

### GetPointIds

```csharp
public long[] GetPointIds()
```

引擎名：`get_point_ids`

Returns an array of all point IDs.

### GetPointPath

```csharp
public Vector2[] GetPointPath(long fromId, long toId)
```

引擎名：`get_point_path`

Returns an array with the points that are in the path found by AStar2D between the given points. The array is ordered from the starting point to the ending point of the path.

If `fromId` point is disabled, returns an empty array (even if `from_id == to_id`).

If `fromId` point is not disabled, there is no valid path to the target, and `allowPartialPath` is `true`, returns a path to the point closest to the target that can be reached.

**Note:** This method is not thread-safe; it can only be used from a single [`GodotThread`](GodotThread.md) at a given time. Consider using [`Mutex`](Mutex.md) to ensure exclusive access to one thread to avoid race conditions.

Additionally, when `allowPartialPath` is `true` and `toId` is disabled the search may take an unusually long time to finish.

### GetPointPath

```csharp
public Vector2[] GetPointPath(long fromId, long toId, bool allowPartialPath = false)
```

引擎名：`get_point_path`

Returns an array with the points that are in the path found by AStar2D between the given points. The array is ordered from the starting point to the ending point of the path.

If `fromId` point is disabled, returns an empty array (even if `from_id == to_id`).

If `fromId` point is not disabled, there is no valid path to the target, and `allowPartialPath` is `true`, returns a path to the point closest to the target that can be reached.

**Note:** This method is not thread-safe; it can only be used from a single [`GodotThread`](GodotThread.md) at a given time. Consider using [`Mutex`](Mutex.md) to ensure exclusive access to one thread to avoid race conditions.

Additionally, when `allowPartialPath` is `true` and `toId` is disabled the search may take an unusually long time to finish.

### GetPointPosition

```csharp
public Vector2 GetPointPosition(long id)
```

引擎名：`get_point_position`

Returns the position of the point associated with the given `id`.

### GetPointWeightScale

```csharp
public float GetPointWeightScale(long id)
```

引擎名：`get_point_weight_scale`

Returns the weight scale of the point associated with the given `id`.

### HasPoint

```csharp
public bool HasPoint(long id)
```

引擎名：`has_point`

Returns whether a point associated with the given `id` exists.

### IsNeighborFilterEnabled

```csharp
public bool IsNeighborFilterEnabled()
```

引擎名：`is_neighbor_filter_enabled`

### IsPointDisabled

```csharp
public bool IsPointDisabled(long id)
```

引擎名：`is_point_disabled`

Returns whether a point is disabled or not for pathfinding. By default, all points are enabled.

### RemovePoint

```csharp
public void RemovePoint(long id)
```

引擎名：`remove_point`

Removes the point associated with the given `id` from the points pool.

### ReserveSpace

```csharp
public void ReserveSpace(long numNodes)
```

引擎名：`reserve_space`

Reserves space internally for `numNodes` points. Useful if you're adding a known large number of points at once, such as points on a grid.

### SetNeighborFilterEnabled

```csharp
public void SetNeighborFilterEnabled(bool enabled)
```

引擎名：`set_neighbor_filter_enabled`

### SetPointDisabled

```csharp
public void SetPointDisabled(long id, bool disabled = true)
```

引擎名：`set_point_disabled`

Disables or enables the specified point for pathfinding. Useful for making a temporary obstacle.

### SetPointPosition

```csharp
public void SetPointPosition(long id, Vector2 position)
```

引擎名：`set_point_position`

Sets the `position` for the point with the given `id`.

### SetPointWeightScale

```csharp
public void SetPointWeightScale(long id, float weightScale)
```

引擎名：`set_point_weight_scale`

Sets the `weightScale` for the point with the given `id`. The `weightScale` is multiplied by the result of `_ComputeCost()` when determining the overall cost of traveling across a segment from a neighboring point to this point.

### _ComputeCost

```csharp
public virtual float _ComputeCost(long fromId, long toId)
```

引擎名：`_compute_cost`

Called when computing the cost between two connected points.

Note that this function is hidden in the default `AStar2D` class.

### _EstimateCost

```csharp
public virtual float _EstimateCost(long fromId, long endId)
```

引擎名：`_estimate_cost`

Called when estimating the cost between a point and the path's ending point.

Note that this function is hidden in the default `AStar2D` class.

### _FilterNeighbor

```csharp
public virtual bool _FilterNeighbor(long fromId, long neighborId)
```

引擎名：`_filter_neighbor`

Called when neighboring enters processing and if `NeighborFilterEnabled` is `true`. If `true` is returned the point will not be processed.

Note that this function is hidden in the default `AStar2D` class.
