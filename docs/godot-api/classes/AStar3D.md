<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AStar3D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A* (A star) is a computer algorithm used in pathfinding and graph traversal, the process of plotting short paths among vertices (points), passing through a given set of edges (segments). It enjoys widespread use due to its performance and accuracy. Godot's A* implementation uses points in 3D space and Euclidean distances by default.

You must add points manually with `AddPoint()` and create segments manually with `ConnectPoints()`. Once done, you can test if there is a path between two points with the `ArePointsConnected()` function, get a path containing indices by `GetIdPath()`, or one containing actual coordinates with `GetPointPath()`.

It is also possible to use non-Euclidean distances. To do so, create a script that extends `AStar3D` and override the methods `_ComputeCost()` and `_EstimateCost()`. Both should take two point IDs and return the distance between the corresponding points.

**Example:** Use Manhattan distance instead of Euclidean distance:

```csharp
using Godot;

[GlobalClass]
public partial class MyAStar3D : AStar3D
{
public override float _ComputeCost(long fromId, long toId)
{
Vector3 fromPoint = GetPointPosition(fromId);
Vector3 toPoint = GetPointPosition(toId);

return Mathf.Abs(fromPoint.X - toPoint.X) + Mathf.Abs(fromPoint.Y - toPoint.Y) + Mathf.Abs(fromPoint.Z - toPoint.Z);
}

public override float _EstimateCost(long fromId, long toId)
{
Vector3 fromPoint = GetPointPosition(fromId);
Vector3 toPoint = GetPointPosition(toId);
return Mathf.Abs(fromPoint.X - toPoint.X) + Mathf.Abs(fromPoint.Y - toPoint.Y) + Mathf.Abs(fromPoint.Z - toPoint.Z);
}
}

```

`_EstimateCost()` should return a lower bound of the distance, i.e. `_estimate_cost(u, v) <= _compute_cost(u, v)`. This serves as a hint to the algorithm because the custom `_ComputeCost()` might be computation-heavy. If this is not the case, make `_EstimateCost()` return the same value as `_ComputeCost()` to provide the algorithm with the most accurate information.

If the default `_EstimateCost()` and `_ComputeCost()` methods are used, or if the supplied `_EstimateCost()` method returns a lower bound of the cost, then the paths returned by A* will be the lowest-cost paths. Here, the cost of a path equals the sum of the `_ComputeCost()` results of all segments in the path multiplied by the `weight_scale`s of the endpoints of the respective segments. If the default methods are used and the `weight_scale`s of all points are set to `1.0`, then this equals the sum of Euclidean distances of all segments in the path.

## 构造函数

### AStar3D

```csharp
public AStar3D()
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
public void AddPoint(long id, Vector3 position, float weightScale = 1f)
```

引擎名：`add_point`

Adds a new point at the given position with the given identifier. The `id` must be 0 or larger, and the `weightScale` must be 0.0 or greater.

The `weightScale` is multiplied by the result of `_ComputeCost()` when determining the overall cost of traveling across a segment from a neighboring point to this point. Thus, all else being equal, the algorithm prefers points with lower `weightScale`s to form a path.

```csharp
var astar = new AStar3D();
astar.AddPoint(1, new Vector3(1, 0, 0), 4); // Adds the point (1, 0, 0) with weight_scale 4 and id 1

```

If there already exists a point for the given `id`, its position and weight scale are updated to the given values.

### ArePointsConnected

```csharp
public bool ArePointsConnected(long id, long toId, bool bidirectional = true)
```

引擎名：`are_points_connected`

Returns whether the two given points are directly connected by a segment. If `bidirectional` is `false`, returns whether movement from `id` to `toId` is possible through this segment.

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
var astar = new AStar3D();
astar.AddPoint(1, new Vector3(1, 1, 0));
astar.AddPoint(2, new Vector3(0, 5, 0));
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
public long GetClosestPoint(Vector3 toPosition, bool includeDisabled = false)
```

引擎名：`get_closest_point`

Returns the ID of the closest point to `toPosition`, optionally taking disabled points into account. Returns `-1` if there are no points in the points pool.

**Note:** If several points are the closest to `toPosition`, the one with the smallest ID will be returned, ensuring a deterministic result.

### GetClosestPositionInSegment

```csharp
public Vector3 GetClosestPositionInSegment(Vector3 toPosition)
```

引擎名：`get_closest_position_in_segment`

Returns the closest position to `toPosition` that resides inside a segment between two connected points.

```csharp
var astar = new AStar3D();
astar.AddPoint(1, new Vector3(0, 0, 0));
astar.AddPoint(2, new Vector3(0, 5, 0));
astar.ConnectPoints(1, 2);
Vector3 res = astar.GetClosestPositionInSegment(new Vector3(3, 3, 0)); // Returns (0, 3, 0)

```

The result is in the segment that goes from `y = 0` to `y = 5`. It's the closest position in the segment to the given point.

### GetIdPath

```csharp
public long[] GetIdPath(long fromId, long toId)
```

引擎名：`get_id_path`

Returns an array with the IDs of the points that form the path found by AStar3D between the given points. The array is ordered from the starting point to the ending point of the path.

If `fromId` point is disabled, returns an empty array (even if `from_id == to_id`).

If `fromId` point is not disabled, there is no valid path to the target, and `allowPartialPath` is `true`, returns a path to the point closest to the target that can be reached.

**Note:** When `allowPartialPath` is `true` and `toId` is disabled the search may take an unusually long time to finish.

```csharp
var astar = new AStar3D();
astar.AddPoint(1, new Vector3(0, 0, 0));
astar.AddPoint(2, new Vector3(0, 1, 0), 1); // Default weight is 1
astar.AddPoint(3, new Vector3(1, 1, 0));
astar.AddPoint(4, new Vector3(2, 0, 0));
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

Returns an array with the IDs of the points that form the path found by AStar3D between the given points. The array is ordered from the starting point to the ending point of the path.

If `fromId` point is disabled, returns an empty array (even if `from_id == to_id`).

If `fromId` point is not disabled, there is no valid path to the target, and `allowPartialPath` is `true`, returns a path to the point closest to the target that can be reached.

**Note:** When `allowPartialPath` is `true` and `toId` is disabled the search may take an unusually long time to finish.

```csharp
var astar = new AStar3D();
astar.AddPoint(1, new Vector3(0, 0, 0));
astar.AddPoint(2, new Vector3(0, 1, 0), 1); // Default weight is 1
astar.AddPoint(3, new Vector3(1, 1, 0));
astar.AddPoint(4, new Vector3(2, 0, 0));
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
var astar = new AStar3D();
astar.AddPoint(1, new Vector3(0, 0, 0));
astar.AddPoint(2, new Vector3(0, 1, 0));
astar.AddPoint(3, new Vector3(1, 1, 0));
astar.AddPoint(4, new Vector3(2, 0, 0));
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
public Vector3[] GetPointPath(long fromId, long toId)
```

引擎名：`get_point_path`

Returns an array with the points that are in the path found by AStar3D between the given points. The array is ordered from the starting point to the ending point of the path.

If `fromId` point is disabled, returns an empty array (even if `from_id == to_id`).

If `fromId` point is not disabled, there is no valid path to the target, and `allowPartialPath` is `true`, returns a path to the point closest to the target that can be reached.

**Note:** This method is not thread-safe; it can only be used from a single [`GodotThread`](GodotThread.md) at a given time. Consider using [`Mutex`](Mutex.md) to ensure exclusive access to one thread to avoid race conditions.

Additionally, when `allowPartialPath` is `true` and `toId` is disabled the search may take an unusually long time to finish.

### GetPointPath

```csharp
public Vector3[] GetPointPath(long fromId, long toId, bool allowPartialPath = false)
```

引擎名：`get_point_path`

Returns an array with the points that are in the path found by AStar3D between the given points. The array is ordered from the starting point to the ending point of the path.

If `fromId` point is disabled, returns an empty array (even if `from_id == to_id`).

If `fromId` point is not disabled, there is no valid path to the target, and `allowPartialPath` is `true`, returns a path to the point closest to the target that can be reached.

**Note:** This method is not thread-safe; it can only be used from a single [`GodotThread`](GodotThread.md) at a given time. Consider using [`Mutex`](Mutex.md) to ensure exclusive access to one thread to avoid race conditions.

Additionally, when `allowPartialPath` is `true` and `toId` is disabled the search may take an unusually long time to finish.

### GetPointPosition

```csharp
public Vector3 GetPointPosition(long id)
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
public void SetPointPosition(long id, Vector3 position)
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

Note that this function is hidden in the default `AStar3D` class.

### _EstimateCost

```csharp
public virtual float _EstimateCost(long fromId, long endId)
```

引擎名：`_estimate_cost`

Called when estimating the cost between a point and the path's ending point.

Note that this function is hidden in the default `AStar3D` class.

### _FilterNeighbor

```csharp
public virtual bool _FilterNeighbor(long fromId, long neighborId)
```

引擎名：`_filter_neighbor`

Called when neighboring point enters processing and if `NeighborFilterEnabled` is `true`. If `true` is returned the point will not be processed.

Note that this function is hidden in the default `AStar3D` class.
