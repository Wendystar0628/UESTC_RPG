<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationServer2D

命名空间 `Godot`；static class

NavigationServer2D is the server that handles navigation maps, regions and agents. It does not handle A* navigation from [`AStar2D`](AStar2D.md) or [`AStarGrid2D`](AStarGrid2D.md).

Maps are divided into regions, which are composed of navigation polygons. Together, they define the traversable areas in the 2D world.

**Note:** Most `NavigationServer2D` changes take effect after the next physics frame and not immediately. This includes all changes made to maps, regions or agents by navigation-related nodes in the scene tree or made through scripts.

For two regions to be connected to each other, they must share a similar edge. An edge is considered connected to another if both of its two vertices are at a distance less than `edge_connection_margin` to the respective other edge's vertex.

You may assign navigation layers to regions with `RegionSetNavigationLayers()`, which then can be checked upon when requesting a path with `MapGetPath()`. This can be used to allow or deny certain areas for some objects.

To use the collision avoidance system, you may use agents. You can set an agent's target velocity, then the servers will emit a callback with a modified velocity.

**Note:** The collision avoidance system ignores regions. Using the modified velocity directly may move an agent outside of the traversable area. This is a limitation of the collision avoidance system, any more complex situation may require the use of the physics engine.

This server keeps tracks of any call and executes them during the sync phase. This means that you can request any change to the map, using any thread, without worrying.

## 属性

### Singleton

```csharp
public static NavigationServer2DInstance Singleton { get; }
```

## 方法

### AgentCreate

```csharp
public static Rid AgentCreate()
```

引擎名：`agent_create`

Creates the agent.

### AgentGetAvoidanceEnabled

```csharp
public static bool AgentGetAvoidanceEnabled(Rid agent)
```

引擎名：`agent_get_avoidance_enabled`

Return `true` if the specified `agent` uses avoidance.

### AgentGetAvoidanceLayers

```csharp
public static uint AgentGetAvoidanceLayers(Rid agent)
```

引擎名：`agent_get_avoidance_layers`

Returns the `avoidance_layers` bitmask of the specified `agent`.

### AgentGetAvoidanceMask

```csharp
public static uint AgentGetAvoidanceMask(Rid agent)
```

引擎名：`agent_get_avoidance_mask`

Returns the `avoidance_mask` bitmask of the specified `agent`.

### AgentGetAvoidancePriority

```csharp
public static float AgentGetAvoidancePriority(Rid agent)
```

引擎名：`agent_get_avoidance_priority`

Returns the `avoidance_priority` of the specified `agent`.

### AgentGetMap

```csharp
public static Rid AgentGetMap(Rid agent)
```

引擎名：`agent_get_map`

Returns the navigation map [`Rid`](Rid.md) the requested `agent` is currently assigned to.

### AgentGetMaxNeighbors

```csharp
public static int AgentGetMaxNeighbors(Rid agent)
```

引擎名：`agent_get_max_neighbors`

Returns the maximum number of other agents the specified `agent` takes into account in the navigation.

### AgentGetMaxSpeed

```csharp
public static float AgentGetMaxSpeed(Rid agent)
```

引擎名：`agent_get_max_speed`

Returns the maximum speed of the specified `agent`.

### AgentGetNeighborDistance

```csharp
public static float AgentGetNeighborDistance(Rid agent)
```

引擎名：`agent_get_neighbor_distance`

Returns the maximum distance to other agents the specified `agent` takes into account in the navigation.

### AgentGetPaused

```csharp
public static bool AgentGetPaused(Rid agent)
```

引擎名：`agent_get_paused`

Returns `true` if the specified `agent` is paused.

### AgentGetPosition

```csharp
public static Vector2 AgentGetPosition(Rid agent)
```

引擎名：`agent_get_position`

Returns the position of the specified `agent` in world space.

### AgentGetRadius

```csharp
public static float AgentGetRadius(Rid agent)
```

引擎名：`agent_get_radius`

Returns the radius of the specified `agent`.

### AgentGetTimeHorizonAgents

```csharp
public static float AgentGetTimeHorizonAgents(Rid agent)
```

引擎名：`agent_get_time_horizon_agents`

Returns the minimal amount of time for which the specified `agent`'s velocities that are computed by the simulation are safe with respect to other agents.

### AgentGetTimeHorizonObstacles

```csharp
public static float AgentGetTimeHorizonObstacles(Rid agent)
```

引擎名：`agent_get_time_horizon_obstacles`

Returns the minimal amount of time for which the specified `agent`'s velocities that are computed by the simulation are safe with respect to static avoidance obstacles.

### AgentGetVelocity

```csharp
public static Vector2 AgentGetVelocity(Rid agent)
```

引擎名：`agent_get_velocity`

Returns the velocity of the specified `agent`.

### AgentHasAvoidanceCallback

```csharp
public static bool AgentHasAvoidanceCallback(Rid agent)
```

引擎名：`agent_has_avoidance_callback`

Return `true` if the specified `agent` has an avoidance callback.

### AgentIsMapChanged

```csharp
public static bool AgentIsMapChanged(Rid agent)
```

引擎名：`agent_is_map_changed`

Returns `true` if the map got changed the previous frame.

### AgentSetAvoidanceCallback

```csharp
public static void AgentSetAvoidanceCallback(Rid agent, Callable callback)
```

引擎名：`agent_set_avoidance_callback`

Sets the callback [`Callable`](Callable.md) that gets called after each avoidance processing step for the `agent`. The calculated `safe_velocity` will be dispatched with a signal to the object just before the physics calculations.

**Note:** Created callbacks are always processed independently of the SceneTree state as long as the agent is on a navigation map and not freed. To disable the dispatch of a callback from an agent use `AgentSetAvoidanceCallback()` again with an empty [`Callable`](Callable.md).

### AgentSetAvoidanceEnabled

```csharp
public static void AgentSetAvoidanceEnabled(Rid agent, bool enabled)
```

引擎名：`agent_set_avoidance_enabled`

If `enabled` is `true`, the specified `agent` uses avoidance.

### AgentSetAvoidanceLayers

```csharp
public static void AgentSetAvoidanceLayers(Rid agent, uint layers)
```

引擎名：`agent_set_avoidance_layers`

Set the agent's `avoidance_layers` bitmask.

### AgentSetAvoidanceMask

```csharp
public static void AgentSetAvoidanceMask(Rid agent, uint mask)
```

引擎名：`agent_set_avoidance_mask`

Set the agent's `avoidance_mask` bitmask.

### AgentSetAvoidancePriority

```csharp
public static void AgentSetAvoidancePriority(Rid agent, float priority)
```

引擎名：`agent_set_avoidance_priority`

Set the agent's `avoidance_priority` with a `priority` between 0.0 (lowest priority) to 1.0 (highest priority).

The specified `agent` does not adjust the velocity for other agents that would match the `avoidance_mask` but have a lower `avoidance_priority`. This in turn makes the other agents with lower priority adjust their velocities even more to avoid collision with this agent.

### AgentSetMap

```csharp
public static void AgentSetMap(Rid agent, Rid map)
```

引擎名：`agent_set_map`

Puts the agent in the map.

### AgentSetMaxNeighbors

```csharp
public static void AgentSetMaxNeighbors(Rid agent, int count)
```

引擎名：`agent_set_max_neighbors`

Sets the maximum number of other agents the agent takes into account in the navigation. The larger this number, the longer the running time of the simulation. If the number is too low, the simulation will not be safe.

### AgentSetMaxSpeed

```csharp
public static void AgentSetMaxSpeed(Rid agent, float maxSpeed)
```

引擎名：`agent_set_max_speed`

Sets the maximum speed of the agent. Must be positive.

### AgentSetNeighborDistance

```csharp
public static void AgentSetNeighborDistance(Rid agent, float distance)
```

引擎名：`agent_set_neighbor_distance`

Sets the maximum distance to other agents this agent takes into account in the navigation. The larger this number, the longer the running time of the simulation. If the number is too low, the simulation will not be safe.

### AgentSetPaused

```csharp
public static void AgentSetPaused(Rid agent, bool paused)
```

引擎名：`agent_set_paused`

If `paused` is `true` the specified `agent` will not be processed. For example, it will not calculate avoidance velocities or receive avoidance callbacks.

### AgentSetPosition

```csharp
public static void AgentSetPosition(Rid agent, Vector2 position)
```

引擎名：`agent_set_position`

Sets the position of the agent in world space.

### AgentSetRadius

```csharp
public static void AgentSetRadius(Rid agent, float radius)
```

引擎名：`agent_set_radius`

Sets the radius of the agent.

### AgentSetTimeHorizonAgents

```csharp
public static void AgentSetTimeHorizonAgents(Rid agent, float timeHorizon)
```

引擎名：`agent_set_time_horizon_agents`

The minimal amount of time for which the agent's velocities that are computed by the simulation are safe with respect to other agents. The larger this number, the sooner this agent will respond to the presence of other agents, but the less freedom this agent has in choosing its velocities. A too high value will slow down agents movement considerably. Must be positive.

### AgentSetTimeHorizonObstacles

```csharp
public static void AgentSetTimeHorizonObstacles(Rid agent, float timeHorizon)
```

引擎名：`agent_set_time_horizon_obstacles`

The minimal amount of time for which the agent's velocities that are computed by the simulation are safe with respect to static avoidance obstacles. The larger this number, the sooner this agent will respond to the presence of static avoidance obstacles, but the less freedom this agent has in choosing its velocities. A too high value will slow down agents movement considerably. Must be positive.

### AgentSetVelocity

```csharp
public static void AgentSetVelocity(Rid agent, Vector2 velocity)
```

引擎名：`agent_set_velocity`

Sets `velocity` as the new wanted velocity for the specified `agent`. The avoidance simulation will try to fulfill this velocity if possible but will modify it to avoid collision with other agent's and obstacles. When an agent is teleported to a new position far away use `AgentSetVelocityForced()` instead to reset the internal velocity state.

### AgentSetVelocityForced

```csharp
public static void AgentSetVelocityForced(Rid agent, Vector2 velocity)
```

引擎名：`agent_set_velocity_forced`

Replaces the internal velocity in the collision avoidance simulation with `velocity` for the specified `agent`. When an agent is teleported to a new position far away this function should be used in the same frame. If called frequently this function can get agents stuck.

### BakeFromSourceGeometryData

```csharp
public static void BakeFromSourceGeometryData(NavigationPolygon navigationPolygon, NavigationMeshSourceGeometryData2D sourceGeometryData, Callable callback = default)
```

引擎名：`bake_from_source_geometry_data`

Bakes the provided `navigationPolygon` with the data from the provided `sourceGeometryData`. After the process is finished the optional `callback` will be called.

### BakeFromSourceGeometryDataAsync

```csharp
public static void BakeFromSourceGeometryDataAsync(NavigationPolygon navigationPolygon, NavigationMeshSourceGeometryData2D sourceGeometryData, Callable callback = default)
```

引擎名：`bake_from_source_geometry_data_async`

Bakes the provided `navigationPolygon` with the data from the provided `sourceGeometryData` as an async task running on a background thread. After the process is finished the optional `callback` will be called.

### FreeRid

```csharp
public static void FreeRid(Rid rid)
```

引擎名：`free_rid`

Destroys the given RID.

### GetDebugEnabled

```csharp
public static bool GetDebugEnabled()
```

引擎名：`get_debug_enabled`

Returns `true` when the NavigationServer has debug enabled.

### GetMaps

```csharp
public static Array<Rid> GetMaps()
```

引擎名：`get_maps`

Returns all created navigation map [`Rid`](Rid.md)s on the NavigationServer. This returns both 2D and 3D created navigation maps as there is technically no distinction between them.

### GetProcessInfo

```csharp
public static int GetProcessInfo(NavigationServer2D.ProcessInfo processInfo)
```

引擎名：`get_process_info`

Returns information about the current state of the NavigationServer.

### IsBakingNavigationPolygon

```csharp
public static bool IsBakingNavigationPolygon(NavigationPolygon navigationPolygon)
```

引擎名：`is_baking_navigation_polygon`

Returns `true` when the provided navigation polygon is being baked on a background thread.

### LinkCreate

```csharp
public static Rid LinkCreate()
```

引擎名：`link_create`

Create a new link between two positions on a map.

### LinkGetEnabled

```csharp
public static bool LinkGetEnabled(Rid link)
```

引擎名：`link_get_enabled`

Returns `true` if the specified `link` is enabled.

### LinkGetEndPosition

```csharp
public static Vector2 LinkGetEndPosition(Rid link)
```

引擎名：`link_get_end_position`

Returns the ending position of this `link`.

### LinkGetEnterCost

```csharp
public static float LinkGetEnterCost(Rid link)
```

引擎名：`link_get_enter_cost`

Returns the enter cost of this `link`.

### LinkGetIterationId

```csharp
public static uint LinkGetIterationId(Rid link)
```

引擎名：`link_get_iteration_id`

Returns the current iteration ID of the navigation link. Every time the navigation link changes and synchronizes, the iteration ID increases. An iteration ID of `0` means the navigation link has never synchronized.

**Note:** The iteration ID will wrap around to `1` after reaching its range limit.

### LinkGetMap

```csharp
public static Rid LinkGetMap(Rid link)
```

引擎名：`link_get_map`

Returns the navigation map [`Rid`](Rid.md) the requested `link` is currently assigned to.

### LinkGetNavigationLayers

```csharp
public static uint LinkGetNavigationLayers(Rid link)
```

引擎名：`link_get_navigation_layers`

Returns the navigation layers for this `link`.

### LinkGetOwnerId

```csharp
public static ulong LinkGetOwnerId(Rid link)
```

引擎名：`link_get_owner_id`

Returns the `ObjectID` of the object which manages this link.

### LinkGetStartPosition

```csharp
public static Vector2 LinkGetStartPosition(Rid link)
```

引擎名：`link_get_start_position`

Returns the starting position of this `link`.

### LinkGetTravelCost

```csharp
public static float LinkGetTravelCost(Rid link)
```

引擎名：`link_get_travel_cost`

Returns the travel cost of this `link`.

### LinkIsBidirectional

```csharp
public static bool LinkIsBidirectional(Rid link)
```

引擎名：`link_is_bidirectional`

Returns whether this `link` can be travelled in both directions.

### LinkSetBidirectional

```csharp
public static void LinkSetBidirectional(Rid link, bool bidirectional)
```

引擎名：`link_set_bidirectional`

Sets whether this `link` can be travelled in both directions.

### LinkSetEnabled

```csharp
public static void LinkSetEnabled(Rid link, bool enabled)
```

引擎名：`link_set_enabled`

If `enabled` is `true`, the specified `link` will contribute to its current navigation map.

### LinkSetEndPosition

```csharp
public static void LinkSetEndPosition(Rid link, Vector2 position)
```

引擎名：`link_set_end_position`

Sets the exit position for the `link`.

### LinkSetEnterCost

```csharp
public static void LinkSetEnterCost(Rid link, float enterCost)
```

引擎名：`link_set_enter_cost`

Sets the `enterCost` for this `link`.

### LinkSetMap

```csharp
public static void LinkSetMap(Rid link, Rid map)
```

引擎名：`link_set_map`

Sets the navigation map [`Rid`](Rid.md) for the link.

### LinkSetNavigationLayers

```csharp
public static void LinkSetNavigationLayers(Rid link, uint navigationLayers)
```

引擎名：`link_set_navigation_layers`

Set the links's navigation layers. This allows selecting links from a path request (when using `MapGetPath()`).

### LinkSetOwnerId

```csharp
public static void LinkSetOwnerId(Rid link, ulong ownerId)
```

引擎名：`link_set_owner_id`

Set the `ObjectID` of the object which manages this link.

### LinkSetStartPosition

```csharp
public static void LinkSetStartPosition(Rid link, Vector2 position)
```

引擎名：`link_set_start_position`

Sets the entry position for this `link`.

### LinkSetTravelCost

```csharp
public static void LinkSetTravelCost(Rid link, float travelCost)
```

引擎名：`link_set_travel_cost`

Sets the `travelCost` for this `link`.

### MapCreate

```csharp
public static Rid MapCreate()
```

引擎名：`map_create`

Create a new map.

### MapForceUpdate

```csharp
public static void MapForceUpdate(Rid map)
```

引擎名：`map_force_update`

This function immediately forces synchronization of the specified navigation `map` [`Rid`](Rid.md). By default navigation maps are only synchronized at the end of each physics frame. This function can be used to immediately (re)calculate all the navigation meshes and region connections of the navigation map. This makes it possible to query a navigation path for a changed map immediately and in the same frame (multiple times if needed).

Due to technical restrictions the current NavigationServer command queue will be flushed. This means all already queued update commands for this physics frame will be executed, even those intended for other maps, regions and agents not part of the specified map. The expensive computation of the navigation meshes and region connections of a map will only be done for the specified map. Other maps will receive the normal synchronization at the end of the physics frame. Should the specified map receive changes after the forced update it will update again as well when the other maps receive their update.

Avoidance processing and dispatch of the `safe_velocity` signals is unaffected by this function and continues to happen for all maps and agents at the end of the physics frame.

**Note:** With great power comes great responsibility. This function should only be used by users that really know what they are doing and have a good reason for it. Forcing an immediate update of a navigation map requires locking the NavigationServer and flushing the entire NavigationServer command queue. Not only can this severely impact the performance of a game but it can also introduce bugs if used inappropriately without much foresight.

### MapGetAgents

```csharp
public static Array<Rid> MapGetAgents(Rid map)
```

引擎名：`map_get_agents`

Returns all navigation agents [`Rid`](Rid.md)s that are currently assigned to the requested navigation `map`.

### MapGetCellSize

```csharp
public static float MapGetCellSize(Rid map)
```

引擎名：`map_get_cell_size`

Returns the map cell size used to rasterize the navigation mesh vertices.

### MapGetClosestPoint

```csharp
public static Vector2 MapGetClosestPoint(Rid map, Vector2 toPoint)
```

引擎名：`map_get_closest_point`

Returns the navigation mesh surface point closest to the provided `toPoint` on the navigation `map`.

### MapGetClosestPointOwner

```csharp
public static Rid MapGetClosestPointOwner(Rid map, Vector2 toPoint)
```

引擎名：`map_get_closest_point_owner`

Returns the owner region RID for the navigation mesh surface point closest to the provided `toPoint` on the navigation `map`.

### MapGetEdgeConnectionMargin

```csharp
public static float MapGetEdgeConnectionMargin(Rid map)
```

引擎名：`map_get_edge_connection_margin`

Returns the edge connection margin of the map. The edge connection margin is a distance used to connect two regions.

### MapGetIterationId

```csharp
public static uint MapGetIterationId(Rid map)
```

引擎名：`map_get_iteration_id`

Returns the current iteration id of the navigation map. Every time the navigation map changes and synchronizes the iteration id increases. An iteration id of 0 means the navigation map has never synchronized.

**Note:** The iteration id will wrap back to 1 after reaching its range limit.

### MapGetLinkConnectionRadius

```csharp
public static float MapGetLinkConnectionRadius(Rid map)
```

引擎名：`map_get_link_connection_radius`

Returns the link connection radius of the map. This distance is the maximum range any link will search for navigation mesh polygons to connect to.

### MapGetLinks

```csharp
public static Array<Rid> MapGetLinks(Rid map)
```

引擎名：`map_get_links`

Returns all navigation link [`Rid`](Rid.md)s that are currently assigned to the requested navigation `map`.

### MapGetMergeRasterizerCellScale

```csharp
public static float MapGetMergeRasterizerCellScale(Rid map)
```

引擎名：`map_get_merge_rasterizer_cell_scale`

Returns map's internal merge rasterizer cell scale.

### MapGetObstacles

```csharp
public static Array<Rid> MapGetObstacles(Rid map)
```

引擎名：`map_get_obstacles`

Returns all navigation obstacle [`Rid`](Rid.md)s that are currently assigned to the requested navigation `map`.

### MapGetPath

```csharp
public static Vector2[] MapGetPath(Rid map, Vector2 origin, Vector2 destination, bool optimize, uint navigationLayers = 1)
```

引擎名：`map_get_path`

Returns the navigation path to reach the destination from the origin. `navigationLayers` is a bitmask of all region navigation layers that are allowed to be in the path.

### MapGetRandomPoint

```csharp
public static Vector2 MapGetRandomPoint(Rid map, uint navigationLayers, bool uniformly)
```

引擎名：`map_get_random_point`

Returns a random position picked from all map region polygons with matching `navigationLayers`.

If `uniformly` is `true`, all map regions, polygons, and faces are weighted by their surface area (slower).

If `uniformly` is `false`, just a random region and a random polygon are picked (faster).

### MapGetRegions

```csharp
public static Array<Rid> MapGetRegions(Rid map)
```

引擎名：`map_get_regions`

Returns all navigation regions [`Rid`](Rid.md)s that are currently assigned to the requested navigation `map`.

### MapGetUseAsyncIterations

```csharp
public static bool MapGetUseAsyncIterations(Rid map)
```

引擎名：`map_get_use_async_iterations`

Returns `true` if the `map` synchronization uses an async process that runs on a background thread.

### MapGetUseEdgeConnections

```csharp
public static bool MapGetUseEdgeConnections(Rid map)
```

引擎名：`map_get_use_edge_connections`

Returns whether the navigation `map` allows navigation regions to use edge connections to connect with other navigation regions within proximity of the navigation map edge connection margin.

### MapIsActive

```csharp
public static bool MapIsActive(Rid map)
```

引擎名：`map_is_active`

Returns `true` if the map is active.

### MapSetActive

```csharp
public static void MapSetActive(Rid map, bool active)
```

引擎名：`map_set_active`

Sets the map active.

### MapSetCellSize

```csharp
public static void MapSetCellSize(Rid map, float cellSize)
```

引擎名：`map_set_cell_size`

Sets the map cell size used to rasterize the navigation mesh vertices. Must match with the cell size of the used navigation meshes.

### MapSetEdgeConnectionMargin

```csharp
public static void MapSetEdgeConnectionMargin(Rid map, float margin)
```

引擎名：`map_set_edge_connection_margin`

Set the map edge connection margin used to weld the compatible region edges.

### MapSetLinkConnectionRadius

```csharp
public static void MapSetLinkConnectionRadius(Rid map, float radius)
```

引擎名：`map_set_link_connection_radius`

Set the map's link connection radius used to connect links to navigation polygons.

### MapSetMergeRasterizerCellScale

```csharp
public static void MapSetMergeRasterizerCellScale(Rid map, float scale)
```

引擎名：`map_set_merge_rasterizer_cell_scale`

Set the map's internal merge rasterizer cell scale used to control merging sensitivity.

### MapSetUseAsyncIterations

```csharp
public static void MapSetUseAsyncIterations(Rid map, bool enabled)
```

引擎名：`map_set_use_async_iterations`

If `enabled` is `true` the `map` synchronization uses an async process that runs on a background thread.

### MapSetUseEdgeConnections

```csharp
public static void MapSetUseEdgeConnections(Rid map, bool enabled)
```

引擎名：`map_set_use_edge_connections`

Set the navigation `map` edge connection use. If `enabled` is `true`, the navigation map allows navigation regions to use edge connections to connect with other navigation regions within proximity of the navigation map edge connection margin.

### ObstacleCreate

```csharp
public static Rid ObstacleCreate()
```

引擎名：`obstacle_create`

Creates a new navigation obstacle.

### ObstacleGetAvoidanceEnabled

```csharp
public static bool ObstacleGetAvoidanceEnabled(Rid obstacle)
```

引擎名：`obstacle_get_avoidance_enabled`

Returns `true` if the provided `obstacle` has avoidance enabled.

### ObstacleGetAvoidanceLayers

```csharp
public static uint ObstacleGetAvoidanceLayers(Rid obstacle)
```

引擎名：`obstacle_get_avoidance_layers`

Returns the `avoidance_layers` bitmask of the specified `obstacle`.

### ObstacleGetMap

```csharp
public static Rid ObstacleGetMap(Rid obstacle)
```

引擎名：`obstacle_get_map`

Returns the navigation map [`Rid`](Rid.md) the requested `obstacle` is currently assigned to.

### ObstacleGetPaused

```csharp
public static bool ObstacleGetPaused(Rid obstacle)
```

引擎名：`obstacle_get_paused`

Returns `true` if the specified `obstacle` is paused.

### ObstacleGetPosition

```csharp
public static Vector2 ObstacleGetPosition(Rid obstacle)
```

引擎名：`obstacle_get_position`

Returns the position of the specified `obstacle` in world space.

### ObstacleGetRadius

```csharp
public static float ObstacleGetRadius(Rid obstacle)
```

引擎名：`obstacle_get_radius`

Returns the radius of the specified dynamic `obstacle`.

### ObstacleGetVelocity

```csharp
public static Vector2 ObstacleGetVelocity(Rid obstacle)
```

引擎名：`obstacle_get_velocity`

Returns the velocity of the specified dynamic `obstacle`.

### ObstacleGetVertices

```csharp
public static Vector2[] ObstacleGetVertices(Rid obstacle)
```

引擎名：`obstacle_get_vertices`

Returns the outline vertices for the specified `obstacle`.

### ObstacleSetAvoidanceEnabled

```csharp
public static void ObstacleSetAvoidanceEnabled(Rid obstacle, bool enabled)
```

引擎名：`obstacle_set_avoidance_enabled`

If `enabled` is `true`, the provided `obstacle` affects avoidance using agents.

### ObstacleSetAvoidanceLayers

```csharp
public static void ObstacleSetAvoidanceLayers(Rid obstacle, uint layers)
```

引擎名：`obstacle_set_avoidance_layers`

Set the obstacles's `avoidance_layers` bitmask.

### ObstacleSetMap

```csharp
public static void ObstacleSetMap(Rid obstacle, Rid map)
```

引擎名：`obstacle_set_map`

Sets the navigation map [`Rid`](Rid.md) for the obstacle.

### ObstacleSetPaused

```csharp
public static void ObstacleSetPaused(Rid obstacle, bool paused)
```

引擎名：`obstacle_set_paused`

If `paused` is `true` the specified `obstacle` will not be processed. For example, it will no longer affect avoidance velocities.

### ObstacleSetPosition

```csharp
public static void ObstacleSetPosition(Rid obstacle, Vector2 position)
```

引擎名：`obstacle_set_position`

Sets the position of the obstacle in world space.

### ObstacleSetRadius

```csharp
public static void ObstacleSetRadius(Rid obstacle, float radius)
```

引擎名：`obstacle_set_radius`

Sets the radius of the dynamic obstacle.

### ObstacleSetVelocity

```csharp
public static void ObstacleSetVelocity(Rid obstacle, Vector2 velocity)
```

引擎名：`obstacle_set_velocity`

Sets `velocity` of the dynamic `obstacle`. Allows other agents to better predict the movement of the dynamic obstacle. Only works in combination with the radius of the obstacle.

### ObstacleSetVertices

```csharp
public static void ObstacleSetVertices(Rid obstacle, ReadOnlySpan<Vector2> vertices)
```

引擎名：`obstacle_set_vertices`

Sets the outline vertices for the obstacle. If the vertices are winded in clockwise order agents will be pushed in by the obstacle, else they will be pushed out.

### ObstacleSetVertices

```csharp
public static void ObstacleSetVertices(Rid obstacle, Vector2[] vertices)
```

引擎名：`obstacle_set_vertices`

Sets the outline vertices for the obstacle. If the vertices are winded in clockwise order agents will be pushed in by the obstacle, else they will be pushed out.

### ParseSourceGeometryData

```csharp
public static void ParseSourceGeometryData(NavigationPolygon navigationPolygon, NavigationMeshSourceGeometryData2D sourceGeometryData, Node rootNode, Callable callback = default)
```

引擎名：`parse_source_geometry_data`

Parses the [`SceneTree`](SceneTree.md) for source geometry according to the properties of `navigationPolygon`. Updates the provided `sourceGeometryData` resource with the resulting data. The resource can then be used to bake a navigation mesh with `BakeFromSourceGeometryData()`. After the process is finished the optional `callback` will be called.

**Note:** This function needs to run on the main thread or with a deferred call as the SceneTree is not thread-safe.

**Performance:** While convenient, reading data arrays from [`Mesh`](Mesh.md) resources can affect the frame rate negatively. The data needs to be received from the GPU, stalling the [`RenderingServer`](RenderingServer.md) in the process. For performance prefer the use of e.g. collision shapes or creating the data arrays entirely in code.

### QueryPath

```csharp
public static void QueryPath(NavigationPathQueryParameters2D parameters, NavigationPathQueryResult2D result)
```

引擎名：`query_path`

Queries a path in a given navigation map. Start and target position and other parameters are defined through [`NavigationPathQueryParameters2D`](NavigationPathQueryParameters2D.md). Updates the provided [`NavigationPathQueryResult2D`](NavigationPathQueryResult2D.md) result object with the path among other results requested by the query. After the process is finished the optional `callback` will be called.

### QueryPath

```csharp
public static void QueryPath(NavigationPathQueryParameters2D parameters, NavigationPathQueryResult2D result, Callable callback = default)
```

引擎名：`query_path`

Queries a path in a given navigation map. Start and target position and other parameters are defined through [`NavigationPathQueryParameters2D`](NavigationPathQueryParameters2D.md). Updates the provided [`NavigationPathQueryResult2D`](NavigationPathQueryResult2D.md) result object with the path among other results requested by the query. After the process is finished the optional `callback` will be called.

### RegionCreate

```csharp
public static Rid RegionCreate()
```

引擎名：`region_create`

Creates a new region.

### RegionGetBounds

```csharp
public static Rect2 RegionGetBounds(Rid region)
```

引擎名：`region_get_bounds`

Returns the axis-aligned rectangle for the `region`'s transformed navigation mesh.

### RegionGetClosestPoint

```csharp
public static Vector2 RegionGetClosestPoint(Rid region, Vector2 toPoint)
```

引擎名：`region_get_closest_point`

Returns the navigation mesh surface point closest to the provided `toPoint` on the navigation `region`.

### RegionGetConnectionPathwayEnd

```csharp
public static Vector2 RegionGetConnectionPathwayEnd(Rid region, int connection)
```

引擎名：`region_get_connection_pathway_end`

Returns the ending point of a connection door. `connection` is an index between 0 and the return value of `RegionGetConnectionsCount()`.

### RegionGetConnectionPathwayStart

```csharp
public static Vector2 RegionGetConnectionPathwayStart(Rid region, int connection)
```

引擎名：`region_get_connection_pathway_start`

Returns the starting point of a connection door. `connection` is an index between 0 and the return value of `RegionGetConnectionsCount()`.

### RegionGetConnectionsCount

```csharp
public static int RegionGetConnectionsCount(Rid region)
```

引擎名：`region_get_connections_count`

Returns how many connections this `region` has with other regions in the map.

### RegionGetEnabled

```csharp
public static bool RegionGetEnabled(Rid region)
```

引擎名：`region_get_enabled`

Returns `true` if the specified `region` is enabled.

### RegionGetEnterCost

```csharp
public static float RegionGetEnterCost(Rid region)
```

引擎名：`region_get_enter_cost`

Returns the enter cost of this `region`.

### RegionGetIterationId

```csharp
public static uint RegionGetIterationId(Rid region)
```

引擎名：`region_get_iteration_id`

Returns the current iteration ID of the navigation region. Every time the navigation region changes and synchronizes, the iteration ID increases. An iteration ID of `0` means the navigation region has never synchronized.

**Note:** The iteration ID will wrap around to `1` after reaching its range limit.

### RegionGetMap

```csharp
public static Rid RegionGetMap(Rid region)
```

引擎名：`region_get_map`

Returns the navigation map [`Rid`](Rid.md) the requested `region` is currently assigned to.

### RegionGetNavigationLayers

```csharp
public static uint RegionGetNavigationLayers(Rid region)
```

引擎名：`region_get_navigation_layers`

Returns the region's navigation layers.

### RegionGetOwnerId

```csharp
public static ulong RegionGetOwnerId(Rid region)
```

引擎名：`region_get_owner_id`

Returns the `ObjectID` of the object which manages this region.

### RegionGetRandomPoint

```csharp
public static Vector2 RegionGetRandomPoint(Rid region, uint navigationLayers, bool uniformly)
```

引擎名：`region_get_random_point`

Returns a random position picked from all region polygons with matching `navigationLayers`.

If `uniformly` is `true`, all region polygons and faces are weighted by their surface area (slower).

If `uniformly` is `false`, just a random polygon and face is picked (faster).

### RegionGetTransform

```csharp
public static Transform2D RegionGetTransform(Rid region)
```

引擎名：`region_get_transform`

Returns the global transformation of this `region`.

### RegionGetTravelCost

```csharp
public static float RegionGetTravelCost(Rid region)
```

引擎名：`region_get_travel_cost`

Returns the travel cost of this `region`.

### RegionGetUseAsyncIterations

```csharp
public static bool RegionGetUseAsyncIterations(Rid region)
```

引擎名：`region_get_use_async_iterations`

Returns `true` if the `region` uses an async synchronization process that runs on a background thread.

### RegionGetUseEdgeConnections

```csharp
public static bool RegionGetUseEdgeConnections(Rid region)
```

引擎名：`region_get_use_edge_connections`

Returns whether the navigation `region` is set to use edge connections to connect with other navigation regions within proximity of the navigation map edge connection margin.

### RegionOwnsPoint

```csharp
public static bool RegionOwnsPoint(Rid region, Vector2 point)
```

引擎名：`region_owns_point`

Returns `true` if the provided `point` in world space is currently owned by the provided navigation `region`. Owned in this context means that one of the region's navigation mesh polygon faces has a possible position at the closest distance to this point compared to all other navigation meshes from other navigation regions that are also registered on the navigation map of the provided region.

If multiple navigation meshes have positions at equal distance the navigation region whose polygons are processed first wins the ownership. Polygons are processed in the same order that navigation regions were registered on the NavigationServer.

**Note:** If navigation meshes from different navigation regions overlap (which should be avoided in general) the result might not be what is expected.

### RegionSetEnabled

```csharp
public static void RegionSetEnabled(Rid region, bool enabled)
```

引擎名：`region_set_enabled`

If `enabled` is `true` the specified `region` will contribute to its current navigation map.

### RegionSetEnterCost

```csharp
public static void RegionSetEnterCost(Rid region, float enterCost)
```

引擎名：`region_set_enter_cost`

Sets the `enterCost` for this `region`.

### RegionSetMap

```csharp
public static void RegionSetMap(Rid region, Rid map)
```

引擎名：`region_set_map`

Sets the map for the region.

### RegionSetNavigationLayers

```csharp
public static void RegionSetNavigationLayers(Rid region, uint navigationLayers)
```

引擎名：`region_set_navigation_layers`

Set the region's navigation layers. This allows selecting regions from a path request (when using `MapGetPath()`).

### RegionSetNavigationPolygon

```csharp
public static void RegionSetNavigationPolygon(Rid region, NavigationPolygon navigationPolygon)
```

引擎名：`region_set_navigation_polygon`

Sets the `navigationPolygon` for the region.

### RegionSetOwnerId

```csharp
public static void RegionSetOwnerId(Rid region, ulong ownerId)
```

引擎名：`region_set_owner_id`

Set the `ObjectID` of the object which manages this region.

### RegionSetTransform

```csharp
public static void RegionSetTransform(Rid region, Transform2D transform)
```

引擎名：`region_set_transform`

Sets the global transformation for the region.

### RegionSetTravelCost

```csharp
public static void RegionSetTravelCost(Rid region, float travelCost)
```

引擎名：`region_set_travel_cost`

Sets the `travelCost` for this `region`.

### RegionSetUseAsyncIterations

```csharp
public static void RegionSetUseAsyncIterations(Rid region, bool enabled)
```

引擎名：`region_set_use_async_iterations`

If `enabled` is `true` the `region` uses an async synchronization process that runs on a background thread.

### RegionSetUseEdgeConnections

```csharp
public static void RegionSetUseEdgeConnections(Rid region, bool enabled)
```

引擎名：`region_set_use_edge_connections`

If `enabled` is `true`, the navigation `region` will use edge connections to connect with other navigation regions within proximity of the navigation map edge connection margin.

### SetActive

```csharp
public static void SetActive(bool active)
```

引擎名：`set_active`

Control activation of this server.

### SetDebugEnabled

```csharp
public static void SetDebugEnabled(bool enabled)
```

引擎名：`set_debug_enabled`

If `true` enables debug mode on the NavigationServer.

### SimplifyPath

```csharp
public static Vector2[] SimplifyPath(ReadOnlySpan<Vector2> path, float epsilon)
```

引擎名：`simplify_path`

Returns a simplified version of `path` with less critical path points removed. The simplification amount is in worlds units and controlled by `epsilon`. The simplification uses a variant of Ramer-Douglas-Peucker algorithm for curve point decimation.

Path simplification can be helpful to mitigate various path following issues that can arise with certain agent types and script behaviors. E.g. "steering" agents or avoidance in "open fields".

### SimplifyPath

```csharp
public static Vector2[] SimplifyPath(Vector2[] path, float epsilon)
```

引擎名：`simplify_path`

Returns a simplified version of `path` with less critical path points removed. The simplification amount is in worlds units and controlled by `epsilon`. The simplification uses a variant of Ramer-Douglas-Peucker algorithm for curve point decimation.

Path simplification can be helpful to mitigate various path following issues that can arise with certain agent types and script behaviors. E.g. "steering" agents or avoidance in "open fields".

### SourceGeometryParserCreate

```csharp
public static Rid SourceGeometryParserCreate()
```

引擎名：`source_geometry_parser_create`

Creates a new source geometry parser. If a [`Callable`](Callable.md) is set for the parser with `SourceGeometryParserSetCallback()` the callback will be called for every single node that gets parsed whenever `ParseSourceGeometryData()` is used.

### SourceGeometryParserSetCallback

```csharp
public static void SourceGeometryParserSetCallback(Rid parser, Callable callback)
```

引擎名：`source_geometry_parser_set_callback`

Sets the `callback` [`Callable`](Callable.md) for the specific source geometry `parser`. The [`Callable`](Callable.md) will receive a call with the following parameters:

- `navigation_mesh` - The [`NavigationPolygon`](NavigationPolygon.md) reference used to define the parse settings. Do NOT edit or add directly to the navigation mesh.

- `source_geometry_data` - The [`NavigationMeshSourceGeometryData2D`](NavigationMeshSourceGeometryData2D.md) reference. Add custom source geometry for navigation mesh baking to this object.

- `node` - The [`Node`](Node.md) that is parsed.

## 事件（信号）

### AvoidanceDebugChanged

```csharp
public event Action AvoidanceDebugChanged
```

引擎名：`avoidance_debug_changed`

Emitted when avoidance debug settings are changed. Only available in debug builds.

### MapChanged

```csharp
public event NavigationServer2D.MapChangedEventHandler MapChanged
```

引擎名：`map_changed`

Emitted when a navigation map is updated, when a region moves or is modified.

### NavigationDebugChanged

```csharp
public event Action NavigationDebugChanged
```

引擎名：`navigation_debug_changed`

Emitted when navigation debug settings are changed. Only available in debug builds.

## MapChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`NavigationServer2D.MapChanged`](NavigationServer2D.md) event of a [`NavigationServer2D`](NavigationServer2D.md) class.

### 签名

#### Invoke

```csharp
delegate void MapChangedEventHandler(Rid map)
```

## ProcessInfo

命名空间 `Godot`；enum

### 枚举值

- `ActiveMaps = 0`：Constant to get the number of active navigation maps.
- `RegionCount = 1`：Constant to get the number of active navigation regions.
- `AgentCount = 2`：Constant to get the number of active navigation agents processing avoidance.
- `LinkCount = 3`：Constant to get the number of active navigation links.
- `PolygonCount = 4`：Constant to get the number of navigation mesh polygons.
- `EdgeCount = 5`：Constant to get the number of navigation mesh polygon edges.
- `EdgeMergeCount = 6`：Constant to get the number of navigation mesh polygon edges that were merged due to edge key overlap.
- `EdgeConnectionCount = 7`：Constant to get the number of navigation mesh polygon edges that are considered connected by edge proximity.
- `EdgeFreeCount = 8`：Constant to get the number of navigation mesh polygon edges that could not be merged but may be still connected by edge proximity or with links.
- `ObstacleCount = 9`：Constant to get the number of active navigation obstacles.
