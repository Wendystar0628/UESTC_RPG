<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationAgent2D

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A 2D agent used to pathfind to a position while avoiding static and dynamic obstacles. The calculation can be used by the parent node to dynamically move it along the path. Requires navigation data to work correctly.

Dynamic obstacles are avoided using RVO collision avoidance. Avoidance is computed before physics, so the pathfinding information can be used safely in the physics step.

**Note:** After setting the `TargetPosition` property, the `GetNextPathPosition()` method must be used once every physics frame to update the internal path logic of the navigation agent. The vector position it returns should be used as the next movement position for the agent's parent node.

**Note:** Several methods of this class, such as `GetNextPathPosition()`, can trigger a new path calculation. Calling these in your callback to an agent's signal, such as `WaypointReached`, can cause infinite recursion. It is recommended to call these methods in the physics step or, alternatively, delay their call until the end of the frame (see [`GodotObject.CallDeferred`](GodotObject.md) or [`GodotObject.ConnectFlags.Deferred`](GodotObject.md)).

## 构造函数

### NavigationAgent2D

```csharp
public NavigationAgent2D()
```

## 属性

### AvoidanceEnabled

```csharp
public bool AvoidanceEnabled { get; set; }
```

引擎名：`avoidance_enabled`

If `true` the agent is registered for an RVO avoidance callback on the [`NavigationServer2D`](NavigationServer2D.md). When `Velocity` is used and the processing is completed a `safe_velocity` Vector2 is received with a signal connection to `VelocityComputed`. Avoidance processing with many registered agents has a significant performance cost and should only be enabled on agents that currently require it.

### AvoidanceLayers

```csharp
public uint AvoidanceLayers { get; set; }
```

引擎名：`avoidance_layers`

A bitfield determining the avoidance layers for this NavigationAgent. Other agents with a matching bit on the `AvoidanceMask` will avoid this agent.

### AvoidanceMask

```csharp
public uint AvoidanceMask { get; set; }
```

引擎名：`avoidance_mask`

A bitfield determining what other avoidance agents and obstacles this NavigationAgent will avoid when a bit matches at least one of their `AvoidanceLayers`.

### AvoidancePriority

```csharp
public float AvoidancePriority { get; set; }
```

引擎名：`avoidance_priority`

The agent does not adjust the velocity for other agents that would match the `AvoidanceMask` but have a lower `AvoidancePriority`. This in turn makes the other agents with lower priority adjust their velocities even more to avoid collision with this agent.

### DebugEnabled

```csharp
public bool DebugEnabled { get; set; }
```

引擎名：`debug_enabled`

If `true` shows debug visuals for this agent.

### DebugPathCustomColor

```csharp
public Color DebugPathCustomColor { get; set; }
```

引擎名：`debug_path_custom_color`

If `DebugUseCustom` is `true` uses this color for this agent instead of global color.

### DebugPathCustomLineWidth

```csharp
public float DebugPathCustomLineWidth { get; set; }
```

引擎名：`debug_path_custom_line_width`

If `DebugUseCustom` is `true` uses this line width for rendering paths for this agent instead of global line width.

### DebugPathCustomPointSize

```csharp
public float DebugPathCustomPointSize { get; set; }
```

引擎名：`debug_path_custom_point_size`

If `DebugUseCustom` is `true` uses this rasterized point size for rendering path points for this agent instead of global point size.

### DebugUseCustom

```csharp
public bool DebugUseCustom { get; set; }
```

引擎名：`debug_use_custom`

If `true` uses the defined `DebugPathCustomColor` for this agent instead of global color.

### MaxNeighbors

```csharp
public int MaxNeighbors { get; set; }
```

引擎名：`max_neighbors`

The maximum number of neighbors for the agent to consider.

### MaxSpeed

```csharp
public float MaxSpeed { get; set; }
```

引擎名：`max_speed`

The maximum speed that an agent can move.

### NavigationLayers

```csharp
public uint NavigationLayers { get; set; }
```

引擎名：`navigation_layers`

A bitfield determining which navigation layers of navigation regions this agent will use to calculate a path. Changing it during runtime will clear the current navigation path and generate a new one, according to the new navigation layers.

### NeighborDistance

```csharp
public float NeighborDistance { get; set; }
```

引擎名：`neighbor_distance`

The distance to search for other agents.

### PathDesiredDistance

```csharp
public float PathDesiredDistance { get; set; }
```

引擎名：`path_desired_distance`

The distance threshold before a path point is considered to be reached. This allows agents to not have to hit a path point on the path exactly, but only to reach its general area. If this value is set too high, the NavigationAgent will skip points on the path, which can lead to it leaving the navigation mesh. If this value is set too low, the NavigationAgent will be stuck in a repath loop because it will constantly overshoot the distance to the next point on each physics frame update.

### PathMaxDistance

```csharp
public float PathMaxDistance { get; set; }
```

引擎名：`path_max_distance`

The maximum distance the agent is allowed away from the ideal path to the final position. This can happen due to trying to avoid collisions. When the maximum distance is exceeded, it recalculates the ideal path.

### PathMetadataFlags

```csharp
public NavigationPathQueryParameters2D.PathMetadataFlags PathMetadataFlags { get; set; }
```

引擎名：`path_metadata_flags`

Additional information to return with the navigation path.

### PathPostprocessing

```csharp
public NavigationPathQueryParameters2D.PathPostProcessing PathPostprocessing { get; set; }
```

引擎名：`path_postprocessing`

The path postprocessing applied to the raw path corridor found by the `PathfindingAlgorithm`.

### PathReturnMaxLength

```csharp
public float PathReturnMaxLength { get; set; }
```

引擎名：`path_return_max_length`

The maximum allowed length of the returned path in world units. A path will be clipped when going over this length.

### PathReturnMaxRadius

```csharp
public float PathReturnMaxRadius { get; set; }
```

引擎名：`path_return_max_radius`

The maximum allowed radius in world units that the returned path can be from the path start. The path will be clipped when going over this radius. Compared to `PathReturnMaxLength`, this allows the agent to go that much further, if they need to walk around a corner.

**Note:** This will perform a sphere clip considering only the actual navigation mesh path points with the first path position being the sphere's center.

### PathSearchMaxDistance

```csharp
public float PathSearchMaxDistance { get; set; }
```

引擎名：`path_search_max_distance`

The maximum distance a searched polygon can be away from the start polygon before the pathfinding cancels the search for a path to the (possibly unreachable or very far away) target position polygon. In this case the pathfinding resets and builds a path from the start polygon to the polygon that was found closest to the target position so far. A value of `0` or below counts as unlimited. In case of unlimited the pathfinding will search all polygons connected with the start polygon until either the target position polygon is found or all available polygon search options are exhausted.

### PathSearchMaxPolygons

```csharp
public int PathSearchMaxPolygons { get; set; }
```

引擎名：`path_search_max_polygons`

The maximum number of polygons that are searched before the pathfinding cancels the search for a path to the (possibly unreachable or very far away) target position polygon. In this case the pathfinding resets and builds a path from the start polygon to the polygon that was found closest to the target position so far. A value of `0` or below counts as unlimited. In case of unlimited the pathfinding will search all polygons connected with the start polygon until either the target position polygon is found or all available polygon search options are exhausted.

### PathfindingAlgorithm

```csharp
public NavigationPathQueryParameters2D.PathfindingAlgorithmEnum PathfindingAlgorithm { get; set; }
```

引擎名：`pathfinding_algorithm`

The pathfinding algorithm used in the path query.

### Radius

```csharp
public float Radius { get; set; }
```

引擎名：`radius`

The radius of the avoidance agent. This is the "body" of the avoidance agent and not the avoidance maneuver starting radius (which is controlled by `NeighborDistance`).

Does not affect normal pathfinding. To change an actor's pathfinding radius bake [`NavigationPolygon`](NavigationPolygon.md) resources with a different [`NavigationPolygon.AgentRadius`](NavigationPolygon.md) property and use different navigation maps for each actor size.

### SimplifyEpsilon

```csharp
public float SimplifyEpsilon { get; set; }
```

引擎名：`simplify_epsilon`

The path simplification amount in worlds units.

### SimplifyPath

```csharp
public bool SimplifyPath { get; set; }
```

引擎名：`simplify_path`

If `true` a simplified version of the path will be returned with less critical path points removed. The simplification amount is controlled by `SimplifyEpsilon`. The simplification uses a variant of Ramer-Douglas-Peucker algorithm for curve point decimation.

Path simplification can be helpful to mitigate various path following issues that can arise with certain agent types and script behaviors. E.g. "steering" agents or avoidance in "open fields".

### TargetDesiredDistance

```csharp
public float TargetDesiredDistance { get; set; }
```

引擎名：`target_desired_distance`

The distance threshold before the target is considered to be reached. On reaching the target, `TargetReached` is emitted and navigation ends (see `IsNavigationFinished()` and `NavigationFinished`).

You can make navigation end early by setting this property to a value greater than `PathDesiredDistance` (navigation will end before reaching the last waypoint).

You can also make navigation end closer to the target than each individual path position by setting this property to a value lower than `PathDesiredDistance` (navigation won't immediately end when reaching the last waypoint). However, if the value set is too low, the agent will be stuck in a repath loop because it will constantly overshoot the distance to the target on each physics frame update.

### TargetPosition

```csharp
public Vector2 TargetPosition { get; set; }
```

引擎名：`target_position`

If set, a new navigation path from the current agent position to the `TargetPosition` is requested from the NavigationServer.

### TimeHorizonAgents

```csharp
public float TimeHorizonAgents { get; set; }
```

引擎名：`time_horizon_agents`

The minimal amount of time for which this agent's velocities, that are computed with the collision avoidance algorithm, are safe with respect to other agents. The larger the number, the sooner the agent will respond to other agents, but less freedom in choosing its velocities. A too high value will slow down agents movement considerably. Must be positive.

### TimeHorizonObstacles

```csharp
public float TimeHorizonObstacles { get; set; }
```

引擎名：`time_horizon_obstacles`

The minimal amount of time for which this agent's velocities, that are computed with the collision avoidance algorithm, are safe with respect to static avoidance obstacles. The larger the number, the sooner the agent will respond to static avoidance obstacles, but less freedom in choosing its velocities. A too high value will slow down agents movement considerably. Must be positive.

### Velocity

```csharp
public Vector2 Velocity { get; set; }
```

引擎名：`velocity`

Sets the new wanted velocity for the agent. The avoidance simulation will try to fulfill this velocity if possible but will modify it to avoid collision with other agents and obstacles. When an agent is teleported to a new position, use `SetVelocityForced()` as well to reset the internal simulation velocity.

## 方法

### DistanceToTarget

```csharp
public float DistanceToTarget()
```

引擎名：`distance_to_target`

Returns the distance to the target position, using the agent's global position. The user must set `TargetPosition` in order for this to be accurate.

### GetAvoidanceEnabled

```csharp
public bool GetAvoidanceEnabled()
```

引擎名：`get_avoidance_enabled`

### GetAvoidanceLayerValue

```csharp
public bool GetAvoidanceLayerValue(int layerNumber)
```

引擎名：`get_avoidance_layer_value`

Returns whether or not the specified layer of the `AvoidanceLayers` bitmask is enabled, given a `layerNumber` between 1 and 32.

### GetAvoidanceLayers

```csharp
public uint GetAvoidanceLayers()
```

引擎名：`get_avoidance_layers`

### GetAvoidanceMask

```csharp
public uint GetAvoidanceMask()
```

引擎名：`get_avoidance_mask`

### GetAvoidanceMaskValue

```csharp
public bool GetAvoidanceMaskValue(int maskNumber)
```

引擎名：`get_avoidance_mask_value`

Returns whether or not the specified mask of the `AvoidanceMask` bitmask is enabled, given a `maskNumber` between 1 and 32.

### GetAvoidancePriority

```csharp
public float GetAvoidancePriority()
```

引擎名：`get_avoidance_priority`

### GetCurrentNavigationPath

```csharp
public Vector2[] GetCurrentNavigationPath()
```

引擎名：`get_current_navigation_path`

Returns this agent's current path from start to finish in global coordinates. The path only updates when the target position is changed or the agent requires a repath. The path array is not intended to be used in direct path movement as the agent has its own internal path logic that would get corrupted by changing the path array manually. Use the intended `GetNextPathPosition()` once every physics frame to receive the next path point for the agents movement as this function also updates the internal path logic.

### GetCurrentNavigationPathIndex

```csharp
public int GetCurrentNavigationPathIndex()
```

引擎名：`get_current_navigation_path_index`

Returns which index the agent is currently on in the navigation path's [`Vector2`](Vector2.md)[].

### GetCurrentNavigationResult

```csharp
public NavigationPathQueryResult2D GetCurrentNavigationResult()
```

引擎名：`get_current_navigation_result`

Returns the path query result for the path the agent is currently following.

### GetDebugEnabled

```csharp
public bool GetDebugEnabled()
```

引擎名：`get_debug_enabled`

### GetDebugPathCustomColor

```csharp
public Color GetDebugPathCustomColor()
```

引擎名：`get_debug_path_custom_color`

### GetDebugPathCustomLineWidth

```csharp
public float GetDebugPathCustomLineWidth()
```

引擎名：`get_debug_path_custom_line_width`

### GetDebugPathCustomPointSize

```csharp
public float GetDebugPathCustomPointSize()
```

引擎名：`get_debug_path_custom_point_size`

### GetDebugUseCustom

```csharp
public bool GetDebugUseCustom()
```

引擎名：`get_debug_use_custom`

### GetFinalPosition

```csharp
public Vector2 GetFinalPosition()
```

引擎名：`get_final_position`

Returns the reachable final position of the current navigation path in global coordinates. This position can change if the agent needs to update the navigation path which makes the agent emit the `PathChanged` signal.

### GetMaxNeighbors

```csharp
public int GetMaxNeighbors()
```

引擎名：`get_max_neighbors`

### GetMaxSpeed

```csharp
public float GetMaxSpeed()
```

引擎名：`get_max_speed`

### GetNavigationLayerValue

```csharp
public bool GetNavigationLayerValue(int layerNumber)
```

引擎名：`get_navigation_layer_value`

Returns whether or not the specified layer of the `NavigationLayers` bitmask is enabled, given a `layerNumber` between 1 and 32.

### GetNavigationLayers

```csharp
public uint GetNavigationLayers()
```

引擎名：`get_navigation_layers`

### GetNavigationMap

```csharp
public Rid GetNavigationMap()
```

引擎名：`get_navigation_map`

Returns the [`Rid`](Rid.md) of the navigation map for this NavigationAgent node. This function returns always the map set on the NavigationAgent node and not the map of the abstract agent on the NavigationServer. If the agent map is changed directly with the NavigationServer API the NavigationAgent node will not be aware of the map change. Use `SetNavigationMap()` to change the navigation map for the NavigationAgent and also update the agent on the NavigationServer.

### GetNeighborDistance

```csharp
public float GetNeighborDistance()
```

引擎名：`get_neighbor_distance`

### GetNextPathPosition

```csharp
public Vector2 GetNextPathPosition()
```

引擎名：`get_next_path_position`

Returns the next position in global coordinates that can be moved to, making sure that there are no static objects in the way. If the agent does not have a navigation path, it will return the position of the agent's parent. The use of this function once every physics frame is required to update the internal path logic of the NavigationAgent.

### GetPathDesiredDistance

```csharp
public float GetPathDesiredDistance()
```

引擎名：`get_path_desired_distance`

### GetPathLength

```csharp
public float GetPathLength()
```

引擎名：`get_path_length`

Returns the length of the currently calculated path. The returned value is `0.0`, if the path is still calculating or no calculation has been requested yet.

### GetPathMaxDistance

```csharp
public float GetPathMaxDistance()
```

引擎名：`get_path_max_distance`

### GetPathMetadataFlags

```csharp
public NavigationPathQueryParameters2D.PathMetadataFlags GetPathMetadataFlags()
```

引擎名：`get_path_metadata_flags`

### GetPathPostprocessing

```csharp
public NavigationPathQueryParameters2D.PathPostProcessing GetPathPostprocessing()
```

引擎名：`get_path_postprocessing`

### GetPathReturnMaxLength

```csharp
public float GetPathReturnMaxLength()
```

引擎名：`get_path_return_max_length`

### GetPathReturnMaxRadius

```csharp
public float GetPathReturnMaxRadius()
```

引擎名：`get_path_return_max_radius`

### GetPathSearchMaxDistance

```csharp
public float GetPathSearchMaxDistance()
```

引擎名：`get_path_search_max_distance`

### GetPathSearchMaxPolygons

```csharp
public int GetPathSearchMaxPolygons()
```

引擎名：`get_path_search_max_polygons`

### GetPathfindingAlgorithm

```csharp
public NavigationPathQueryParameters2D.PathfindingAlgorithmEnum GetPathfindingAlgorithm()
```

引擎名：`get_pathfinding_algorithm`

### GetRadius

```csharp
public float GetRadius()
```

引擎名：`get_radius`

### GetRid

```csharp
public Rid GetRid()
```

引擎名：`get_rid`

Returns the [`Rid`](Rid.md) of this agent on the [`NavigationServer2D`](NavigationServer2D.md).

### GetSimplifyEpsilon

```csharp
public float GetSimplifyEpsilon()
```

引擎名：`get_simplify_epsilon`

### GetSimplifyPath

```csharp
public bool GetSimplifyPath()
```

引擎名：`get_simplify_path`

### GetTargetDesiredDistance

```csharp
public float GetTargetDesiredDistance()
```

引擎名：`get_target_desired_distance`

### GetTargetPosition

```csharp
public Vector2 GetTargetPosition()
```

引擎名：`get_target_position`

### GetTimeHorizonAgents

```csharp
public float GetTimeHorizonAgents()
```

引擎名：`get_time_horizon_agents`

### GetTimeHorizonObstacles

```csharp
public float GetTimeHorizonObstacles()
```

引擎名：`get_time_horizon_obstacles`

### GetVelocity

```csharp
public Vector2 GetVelocity()
```

引擎名：`get_velocity`

### IsNavigationFinished

```csharp
public bool IsNavigationFinished()
```

引擎名：`is_navigation_finished`

Returns `true` if the agent's navigation has finished. If the target is reachable, navigation ends when the target is reached. If the target is unreachable, navigation ends when the last waypoint of the path is reached.

**Note:** While `true` prefer to stop calling update functions like `GetNextPathPosition()`. This avoids jittering the standing agent due to calling repeated path updates.

### IsTargetReachable

```csharp
public bool IsTargetReachable()
```

引擎名：`is_target_reachable`

Returns `true` if `GetFinalPosition()` is within `TargetDesiredDistance` of the `TargetPosition`.

### IsTargetReached

```csharp
public bool IsTargetReached()
```

引擎名：`is_target_reached`

Returns `true` if the agent reached the target, i.e. the agent moved within `TargetDesiredDistance` of the `TargetPosition`. It may not always be possible to reach the target but it should always be possible to reach the final position. See `GetFinalPosition()`.

### SetAvoidanceEnabled

```csharp
public void SetAvoidanceEnabled(bool enabled)
```

引擎名：`set_avoidance_enabled`

### SetAvoidanceLayerValue

```csharp
public void SetAvoidanceLayerValue(int layerNumber, bool value)
```

引擎名：`set_avoidance_layer_value`

Based on `value`, enables or disables the specified layer in the `AvoidanceLayers` bitmask, given a `layerNumber` between 1 and 32.

### SetAvoidanceLayers

```csharp
public void SetAvoidanceLayers(uint layers)
```

引擎名：`set_avoidance_layers`

### SetAvoidanceMask

```csharp
public void SetAvoidanceMask(uint mask)
```

引擎名：`set_avoidance_mask`

### SetAvoidanceMaskValue

```csharp
public void SetAvoidanceMaskValue(int maskNumber, bool value)
```

引擎名：`set_avoidance_mask_value`

Based on `value`, enables or disables the specified mask in the `AvoidanceMask` bitmask, given a `maskNumber` between 1 and 32.

### SetAvoidancePriority

```csharp
public void SetAvoidancePriority(float priority)
```

引擎名：`set_avoidance_priority`

### SetDebugEnabled

```csharp
public void SetDebugEnabled(bool enabled)
```

引擎名：`set_debug_enabled`

### SetDebugPathCustomColor

```csharp
public void SetDebugPathCustomColor(Color color)
```

引擎名：`set_debug_path_custom_color`

### SetDebugPathCustomLineWidth

```csharp
public void SetDebugPathCustomLineWidth(float lineWidth)
```

引擎名：`set_debug_path_custom_line_width`

### SetDebugPathCustomPointSize

```csharp
public void SetDebugPathCustomPointSize(float pointSize)
```

引擎名：`set_debug_path_custom_point_size`

### SetDebugUseCustom

```csharp
public void SetDebugUseCustom(bool enabled)
```

引擎名：`set_debug_use_custom`

### SetMaxNeighbors

```csharp
public void SetMaxNeighbors(int maxNeighbors)
```

引擎名：`set_max_neighbors`

### SetMaxSpeed

```csharp
public void SetMaxSpeed(float maxSpeed)
```

引擎名：`set_max_speed`

### SetNavigationLayerValue

```csharp
public void SetNavigationLayerValue(int layerNumber, bool value)
```

引擎名：`set_navigation_layer_value`

Based on `value`, enables or disables the specified layer in the `NavigationLayers` bitmask, given a `layerNumber` between 1 and 32.

### SetNavigationLayers

```csharp
public void SetNavigationLayers(uint navigationLayers)
```

引擎名：`set_navigation_layers`

### SetNavigationMap

```csharp
public void SetNavigationMap(Rid navigationMap)
```

引擎名：`set_navigation_map`

Sets the [`Rid`](Rid.md) of the navigation map this NavigationAgent node should use and also updates the `agent` on the NavigationServer.

### SetNeighborDistance

```csharp
public void SetNeighborDistance(float neighborDistance)
```

引擎名：`set_neighbor_distance`

### SetPathDesiredDistance

```csharp
public void SetPathDesiredDistance(float desiredDistance)
```

引擎名：`set_path_desired_distance`

### SetPathMaxDistance

```csharp
public void SetPathMaxDistance(float maxSpeed)
```

引擎名：`set_path_max_distance`

### SetPathMetadataFlags

```csharp
public void SetPathMetadataFlags(NavigationPathQueryParameters2D.PathMetadataFlags flags)
```

引擎名：`set_path_metadata_flags`

### SetPathPostprocessing

```csharp
public void SetPathPostprocessing(NavigationPathQueryParameters2D.PathPostProcessing pathPostprocessing)
```

引擎名：`set_path_postprocessing`

### SetPathReturnMaxLength

```csharp
public void SetPathReturnMaxLength(float length)
```

引擎名：`set_path_return_max_length`

### SetPathReturnMaxRadius

```csharp
public void SetPathReturnMaxRadius(float radius)
```

引擎名：`set_path_return_max_radius`

### SetPathSearchMaxDistance

```csharp
public void SetPathSearchMaxDistance(float distance)
```

引擎名：`set_path_search_max_distance`

### SetPathSearchMaxPolygons

```csharp
public void SetPathSearchMaxPolygons(int maxPolygons)
```

引擎名：`set_path_search_max_polygons`

### SetPathfindingAlgorithm

```csharp
public void SetPathfindingAlgorithm(NavigationPathQueryParameters2D.PathfindingAlgorithmEnum pathfindingAlgorithm)
```

引擎名：`set_pathfinding_algorithm`

### SetRadius

```csharp
public void SetRadius(float radius)
```

引擎名：`set_radius`

### SetSimplifyEpsilon

```csharp
public void SetSimplifyEpsilon(float epsilon)
```

引擎名：`set_simplify_epsilon`

### SetSimplifyPath

```csharp
public void SetSimplifyPath(bool enabled)
```

引擎名：`set_simplify_path`

### SetTargetDesiredDistance

```csharp
public void SetTargetDesiredDistance(float desiredDistance)
```

引擎名：`set_target_desired_distance`

### SetTargetPosition

```csharp
public void SetTargetPosition(Vector2 position)
```

引擎名：`set_target_position`

### SetTimeHorizonAgents

```csharp
public void SetTimeHorizonAgents(float timeHorizon)
```

引擎名：`set_time_horizon_agents`

### SetTimeHorizonObstacles

```csharp
public void SetTimeHorizonObstacles(float timeHorizon)
```

引擎名：`set_time_horizon_obstacles`

### SetVelocity

```csharp
public void SetVelocity(Vector2 velocity)
```

引擎名：`set_velocity`

### SetVelocityForced

```csharp
public void SetVelocityForced(Vector2 velocity)
```

引擎名：`set_velocity_forced`

Replaces the internal velocity in the collision avoidance simulation with `velocity`. When an agent is teleported to a new position this function should be used in the same frame. If called frequently this function can get agents stuck.

## 事件（信号）

### LinkReached

```csharp
public event NavigationAgent2D.LinkReachedEventHandler LinkReached
```

引擎名：`link_reached`

Signals that the agent reached a navigation link. Emitted when the agent moves within `PathDesiredDistance` of the next position of the path when that position is a navigation link.

The details dictionary may contain the following keys depending on the value of `PathMetadataFlags`:

- `position`: The start position of the link that was reached.

- `type`: Always [`NavigationPathQueryResult2D.PathSegmentType.Link`](NavigationPathQueryResult2D.md).

- `rid`: The [`Rid`](Rid.md) of the link.

- `owner`: The object which manages the link (usually [`NavigationLink2D`](NavigationLink2D.md)).

- `link_entry_position`: If `owner` is available and the owner is a [`NavigationLink2D`](NavigationLink2D.md), it will contain the global position of the link's point the agent is entering.

- `link_exit_position`: If `owner` is available and the owner is a [`NavigationLink2D`](NavigationLink2D.md), it will contain the global position of the link's point which the agent is exiting.

### NavigationFinished

```csharp
public event Action NavigationFinished
```

引擎名：`navigation_finished`

Signals that the agent's navigation has finished. If the target is reachable, navigation ends when the target is reached. If the target is unreachable, navigation ends when the last waypoint of the path is reached. This signal is emitted only once per loaded path.

This signal will be emitted just after `TargetReached` when the target is reachable.

### PathChanged

```csharp
public event Action PathChanged
```

引擎名：`path_changed`

Emitted when the agent had to update the loaded path:

- because path was previously empty.

- because navigation map has changed.

- because agent pushed further away from the current path segment than the `PathMaxDistance`.

### TargetReached

```csharp
public event Action TargetReached
```

引擎名：`target_reached`

Signals that the agent reached the target, i.e. the agent moved within `TargetDesiredDistance` of the `TargetPosition`. This signal is emitted only once per loaded path.

This signal will be emitted just before `NavigationFinished` when the target is reachable.

It may not always be possible to reach the target but it should always be possible to reach the final position. See `GetFinalPosition()`.

### VelocityComputed

```csharp
public event NavigationAgent2D.VelocityComputedEventHandler VelocityComputed
```

引擎名：`velocity_computed`

Notifies when the collision avoidance velocity is calculated. Emitted every update as long as `AvoidanceEnabled` is `true` and the agent has a navigation map.

### WaypointReached

```csharp
public event NavigationAgent2D.WaypointReachedEventHandler WaypointReached
```

引擎名：`waypoint_reached`

Signals that the agent reached a waypoint. Emitted when the agent moves within `PathDesiredDistance` of the next position of the path.

The details dictionary may contain the following keys depending on the value of `PathMetadataFlags`:

- `position`: The position of the waypoint that was reached.

- `type`: The type of navigation primitive (region or link) that contains this waypoint.

- `rid`: The [`Rid`](Rid.md) of the containing navigation primitive (region or link).

- `owner`: The object which manages the containing navigation primitive (region or link).

## LinkReachedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`NavigationAgent2D.LinkReached`](NavigationAgent2D.md) event of a [`NavigationAgent2D`](NavigationAgent2D.md) class.

### 签名

#### Invoke

```csharp
delegate void LinkReachedEventHandler(Dictionary details)
```

## VelocityComputedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`NavigationAgent2D.VelocityComputed`](NavigationAgent2D.md) event of a [`NavigationAgent2D`](NavigationAgent2D.md) class.

### 签名

#### Invoke

```csharp
delegate void VelocityComputedEventHandler(Vector2 safeVelocity)
```

## WaypointReachedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`NavigationAgent2D.WaypointReached`](NavigationAgent2D.md) event of a [`NavigationAgent2D`](NavigationAgent2D.md) class.

### 签名

#### Invoke

```csharp
delegate void WaypointReachedEventHandler(Dictionary details)
```
