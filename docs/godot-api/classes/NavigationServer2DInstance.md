<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationServer2DInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

NavigationServer2D is the server that handles navigation maps, regions and agents. It does not handle A* navigation from [`AStar2D`](AStar2D.md) or [`AStarGrid2D`](AStarGrid2D.md).

Maps are divided into regions, which are composed of navigation polygons. Together, they define the traversable areas in the 2D world.

**Note:** Most [`NavigationServer2D`](NavigationServer2D.md) changes take effect after the next physics frame and not immediately. This includes all changes made to maps, regions or agents by navigation-related nodes in the scene tree or made through scripts.

For two regions to be connected to each other, they must share a similar edge. An edge is considered connected to another if both of its two vertices are at a distance less than `edge_connection_margin` to the respective other edge's vertex.

You may assign navigation layers to regions with [`NavigationServer2D.RegionSetNavigationLayers`](NavigationServer2D.md), which then can be checked upon when requesting a path with [`NavigationServer2D.MapGetPath`](NavigationServer2D.md). This can be used to allow or deny certain areas for some objects.

To use the collision avoidance system, you may use agents. You can set an agent's target velocity, then the servers will emit a callback with a modified velocity.

**Note:** The collision avoidance system ignores regions. Using the modified velocity directly may move an agent outside of the traversable area. This is a limitation of the collision avoidance system, any more complex situation may require the use of the physics engine.

This server keeps tracks of any call and executes them during the sync phase. This means that you can request any change to the map, using any thread, without worrying.

## 方法

### AgentCreate

```csharp
public Rid AgentCreate()
```

Creates the agent.

### AgentGetAvoidanceEnabled

```csharp
public bool AgentGetAvoidanceEnabled(Rid agent)
```

Return `true` if the specified `agent` uses avoidance.

### AgentGetAvoidanceLayers

```csharp
public uint AgentGetAvoidanceLayers(Rid agent)
```

Returns the `avoidance_layers` bitmask of the specified `agent`.

### AgentGetAvoidanceMask

```csharp
public uint AgentGetAvoidanceMask(Rid agent)
```

Returns the `avoidance_mask` bitmask of the specified `agent`.

### AgentGetAvoidancePriority

```csharp
public float AgentGetAvoidancePriority(Rid agent)
```

Returns the `avoidance_priority` of the specified `agent`.

### AgentGetMap

```csharp
public Rid AgentGetMap(Rid agent)
```

Returns the navigation map [`Rid`](Rid.md) the requested `agent` is currently assigned to.

### AgentGetMaxNeighbors

```csharp
public int AgentGetMaxNeighbors(Rid agent)
```

Returns the maximum number of other agents the specified `agent` takes into account in the navigation.

### AgentGetMaxSpeed

```csharp
public float AgentGetMaxSpeed(Rid agent)
```

Returns the maximum speed of the specified `agent`.

### AgentGetNeighborDistance

```csharp
public float AgentGetNeighborDistance(Rid agent)
```

Returns the maximum distance to other agents the specified `agent` takes into account in the navigation.

### AgentGetPaused

```csharp
public bool AgentGetPaused(Rid agent)
```

Returns `true` if the specified `agent` is paused.

### AgentGetPosition

```csharp
public Vector2 AgentGetPosition(Rid agent)
```

Returns the position of the specified `agent` in world space.

### AgentGetRadius

```csharp
public float AgentGetRadius(Rid agent)
```

Returns the radius of the specified `agent`.

### AgentGetTimeHorizonAgents

```csharp
public float AgentGetTimeHorizonAgents(Rid agent)
```

Returns the minimal amount of time for which the specified `agent`'s velocities that are computed by the simulation are safe with respect to other agents.

### AgentGetTimeHorizonObstacles

```csharp
public float AgentGetTimeHorizonObstacles(Rid agent)
```

Returns the minimal amount of time for which the specified `agent`'s velocities that are computed by the simulation are safe with respect to static avoidance obstacles.

### AgentGetVelocity

```csharp
public Vector2 AgentGetVelocity(Rid agent)
```

Returns the velocity of the specified `agent`.

### AgentHasAvoidanceCallback

```csharp
public bool AgentHasAvoidanceCallback(Rid agent)
```

Return `true` if the specified `agent` has an avoidance callback.

### AgentIsMapChanged

```csharp
public bool AgentIsMapChanged(Rid agent)
```

Returns `true` if the map got changed the previous frame.

### AgentSetAvoidanceCallback

```csharp
public void AgentSetAvoidanceCallback(Rid agent, Callable callback)
```

Sets the callback [`Callable`](Callable.md) that gets called after each avoidance processing step for the `agent`. The calculated `safe_velocity` will be dispatched with a signal to the object just before the physics calculations.

**Note:** Created callbacks are always processed independently of the SceneTree state as long as the agent is on a navigation map and not freed. To disable the dispatch of a callback from an agent use `AgentSetAvoidanceCallback()` again with an empty [`Callable`](Callable.md).

### AgentSetAvoidanceEnabled

```csharp
public void AgentSetAvoidanceEnabled(Rid agent, bool enabled)
```

If `enabled` is `true`, the specified `agent` uses avoidance.

### AgentSetAvoidanceLayers

```csharp
public void AgentSetAvoidanceLayers(Rid agent, uint layers)
```

Set the agent's `avoidance_layers` bitmask.

### AgentSetAvoidanceMask

```csharp
public void AgentSetAvoidanceMask(Rid agent, uint mask)
```

Set the agent's `avoidance_mask` bitmask.

### AgentSetAvoidancePriority

```csharp
public void AgentSetAvoidancePriority(Rid agent, float priority)
```

Set the agent's `avoidance_priority` with a `priority` between 0.0 (lowest priority) to 1.0 (highest priority).

The specified `agent` does not adjust the velocity for other agents that would match the `avoidance_mask` but have a lower `avoidance_priority`. This in turn makes the other agents with lower priority adjust their velocities even more to avoid collision with this agent.

### AgentSetMap

```csharp
public void AgentSetMap(Rid agent, Rid map)
```

Puts the agent in the map.

### AgentSetMaxNeighbors

```csharp
public void AgentSetMaxNeighbors(Rid agent, int count)
```

Sets the maximum number of other agents the agent takes into account in the navigation. The larger this number, the longer the running time of the simulation. If the number is too low, the simulation will not be safe.

### AgentSetMaxSpeed

```csharp
public void AgentSetMaxSpeed(Rid agent, float maxSpeed)
```

Sets the maximum speed of the agent. Must be positive.

### AgentSetNeighborDistance

```csharp
public void AgentSetNeighborDistance(Rid agent, float distance)
```

Sets the maximum distance to other agents this agent takes into account in the navigation. The larger this number, the longer the running time of the simulation. If the number is too low, the simulation will not be safe.

### AgentSetPaused

```csharp
public void AgentSetPaused(Rid agent, bool paused)
```

If `paused` is `true` the specified `agent` will not be processed. For example, it will not calculate avoidance velocities or receive avoidance callbacks.

### AgentSetPosition

```csharp
public void AgentSetPosition(Rid agent, Vector2 position)
```

Sets the position of the agent in world space.

### AgentSetRadius

```csharp
public void AgentSetRadius(Rid agent, float radius)
```

Sets the radius of the agent.

### AgentSetTimeHorizonAgents

```csharp
public void AgentSetTimeHorizonAgents(Rid agent, float timeHorizon)
```

The minimal amount of time for which the agent's velocities that are computed by the simulation are safe with respect to other agents. The larger this number, the sooner this agent will respond to the presence of other agents, but the less freedom this agent has in choosing its velocities. A too high value will slow down agents movement considerably. Must be positive.

### AgentSetTimeHorizonObstacles

```csharp
public void AgentSetTimeHorizonObstacles(Rid agent, float timeHorizon)
```

The minimal amount of time for which the agent's velocities that are computed by the simulation are safe with respect to static avoidance obstacles. The larger this number, the sooner this agent will respond to the presence of static avoidance obstacles, but the less freedom this agent has in choosing its velocities. A too high value will slow down agents movement considerably. Must be positive.

### AgentSetVelocity

```csharp
public void AgentSetVelocity(Rid agent, Vector2 velocity)
```

Sets `velocity` as the new wanted velocity for the specified `agent`. The avoidance simulation will try to fulfill this velocity if possible but will modify it to avoid collision with other agent's and obstacles. When an agent is teleported to a new position far away use `AgentSetVelocityForced()` instead to reset the internal velocity state.

### AgentSetVelocityForced

```csharp
public void AgentSetVelocityForced(Rid agent, Vector2 velocity)
```

Replaces the internal velocity in the collision avoidance simulation with `velocity` for the specified `agent`. When an agent is teleported to a new position far away this function should be used in the same frame. If called frequently this function can get agents stuck.

### BakeFromSourceGeometryData

```csharp
public void BakeFromSourceGeometryData(NavigationPolygon navigationPolygon, NavigationMeshSourceGeometryData2D sourceGeometryData, Callable callback = default)
```

Bakes the provided `navigationPolygon` with the data from the provided `sourceGeometryData`. After the process is finished the optional `callback` will be called.

### BakeFromSourceGeometryDataAsync

```csharp
public void BakeFromSourceGeometryDataAsync(NavigationPolygon navigationPolygon, NavigationMeshSourceGeometryData2D sourceGeometryData, Callable callback = default)
```

Bakes the provided `navigationPolygon` with the data from the provided `sourceGeometryData` as an async task running on a background thread. After the process is finished the optional `callback` will be called.

### FreeRid

```csharp
public void FreeRid(Rid rid)
```

Destroys the given RID.

### GetDebugEnabled

```csharp
public bool GetDebugEnabled()
```

Returns `true` when the NavigationServer has debug enabled.

### GetMaps

```csharp
public Array<Rid> GetMaps()
```

Returns all created navigation map [`Rid`](Rid.md)s on the NavigationServer. This returns both 2D and 3D created navigation maps as there is technically no distinction between them.

### GetProcessInfo

```csharp
public int GetProcessInfo(NavigationServer2D.ProcessInfo processInfo)
```

Returns information about the current state of the NavigationServer.

### IsBakingNavigationPolygon

```csharp
public bool IsBakingNavigationPolygon(NavigationPolygon navigationPolygon)
```

Returns `true` when the provided navigation polygon is being baked on a background thread.

### LinkCreate

```csharp
public Rid LinkCreate()
```

Create a new link between two positions on a map.

### LinkGetEnabled

```csharp
public bool LinkGetEnabled(Rid link)
```

Returns `true` if the specified `link` is enabled.

### LinkGetEndPosition

```csharp
public Vector2 LinkGetEndPosition(Rid link)
```

Returns the ending position of this `link`.

### LinkGetEnterCost

```csharp
public float LinkGetEnterCost(Rid link)
```

Returns the enter cost of this `link`.

### LinkGetIterationId

```csharp
public uint LinkGetIterationId(Rid link)
```

Returns the current iteration ID of the navigation link. Every time the navigation link changes and synchronizes, the iteration ID increases. An iteration ID of `0` means the navigation link has never synchronized.

**Note:** The iteration ID will wrap around to `1` after reaching its range limit.

### LinkGetMap

```csharp
public Rid LinkGetMap(Rid link)
```

Returns the navigation map [`Rid`](Rid.md) the requested `link` is currently assigned to.

### LinkGetNavigationLayers

```csharp
public uint LinkGetNavigationLayers(Rid link)
```

Returns the navigation layers for this `link`.

### LinkGetOwnerId

```csharp
public ulong LinkGetOwnerId(Rid link)
```

Returns the `ObjectID` of the object which manages this link.

### LinkGetStartPosition

```csharp
public Vector2 LinkGetStartPosition(Rid link)
```

Returns the starting position of this `link`.

### LinkGetTravelCost

```csharp
public float LinkGetTravelCost(Rid link)
```

Returns the travel cost of this `link`.

### LinkIsBidirectional

```csharp
public bool LinkIsBidirectional(Rid link)
```

Returns whether this `link` can be travelled in both directions.

### LinkSetBidirectional

```csharp
public void LinkSetBidirectional(Rid link, bool bidirectional)
```

Sets whether this `link` can be travelled in both directions.

### LinkSetEnabled

```csharp
public void LinkSetEnabled(Rid link, bool enabled)
```

If `enabled` is `true`, the specified `link` will contribute to its current navigation map.

### LinkSetEndPosition

```csharp
public void LinkSetEndPosition(Rid link, Vector2 position)
```

Sets the exit position for the `link`.

### LinkSetEnterCost

```csharp
public void LinkSetEnterCost(Rid link, float enterCost)
```

Sets the `enterCost` for this `link`.

### LinkSetMap

```csharp
public void LinkSetMap(Rid link, Rid map)
```

Sets the navigation map [`Rid`](Rid.md) for the link.

### LinkSetNavigationLayers

```csharp
public void LinkSetNavigationLayers(Rid link, uint navigationLayers)
```

Set the links's navigation layers. This allows selecting links from a path request (when using [`NavigationServer2D.MapGetPath`](NavigationServer2D.md)).

### LinkSetOwnerId

```csharp
public void LinkSetOwnerId(Rid link, ulong ownerId)
```

Set the `ObjectID` of the object which manages this link.

### LinkSetStartPosition

```csharp
public void LinkSetStartPosition(Rid link, Vector2 position)
```

Sets the entry position for this `link`.

### LinkSetTravelCost

```csharp
public void LinkSetTravelCost(Rid link, float travelCost)
```

Sets the `travelCost` for this `link`.

### MapCreate

```csharp
public Rid MapCreate()
```

Create a new map.

### MapForceUpdate

```csharp
public void MapForceUpdate(Rid map)
```

This function immediately forces synchronization of the specified navigation `map` [`Rid`](Rid.md). By default navigation maps are only synchronized at the end of each physics frame. This function can be used to immediately (re)calculate all the navigation meshes and region connections of the navigation map. This makes it possible to query a navigation path for a changed map immediately and in the same frame (multiple times if needed).

Due to technical restrictions the current NavigationServer command queue will be flushed. This means all already queued update commands for this physics frame will be executed, even those intended for other maps, regions and agents not part of the specified map. The expensive computation of the navigation meshes and region connections of a map will only be done for the specified map. Other maps will receive the normal synchronization at the end of the physics frame. Should the specified map receive changes after the forced update it will update again as well when the other maps receive their update.

Avoidance processing and dispatch of the `safe_velocity` signals is unaffected by this function and continues to happen for all maps and agents at the end of the physics frame.

**Note:** With great power comes great responsibility. This function should only be used by users that really know what they are doing and have a good reason for it. Forcing an immediate update of a navigation map requires locking the NavigationServer and flushing the entire NavigationServer command queue. Not only can this severely impact the performance of a game but it can also introduce bugs if used inappropriately without much foresight.

### MapGetAgents

```csharp
public Array<Rid> MapGetAgents(Rid map)
```

Returns all navigation agents [`Rid`](Rid.md)s that are currently assigned to the requested navigation `map`.

### MapGetCellSize

```csharp
public float MapGetCellSize(Rid map)
```

Returns the map cell size used to rasterize the navigation mesh vertices.

### MapGetClosestPoint

```csharp
public Vector2 MapGetClosestPoint(Rid map, Vector2 toPoint)
```

Returns the navigation mesh surface point closest to the provided `toPoint` on the navigation `map`.

### MapGetClosestPointOwner

```csharp
public Rid MapGetClosestPointOwner(Rid map, Vector2 toPoint)
```

Returns the owner region RID for the navigation mesh surface point closest to the provided `toPoint` on the navigation `map`.

### MapGetEdgeConnectionMargin

```csharp
public float MapGetEdgeConnectionMargin(Rid map)
```

Returns the edge connection margin of the map. The edge connection margin is a distance used to connect two regions.

### MapGetIterationId

```csharp
public uint MapGetIterationId(Rid map)
```

Returns the current iteration id of the navigation map. Every time the navigation map changes and synchronizes the iteration id increases. An iteration id of 0 means the navigation map has never synchronized.

**Note:** The iteration id will wrap back to 1 after reaching its range limit.

### MapGetLinkConnectionRadius

```csharp
public float MapGetLinkConnectionRadius(Rid map)
```

Returns the link connection radius of the map. This distance is the maximum range any link will search for navigation mesh polygons to connect to.

### MapGetLinks

```csharp
public Array<Rid> MapGetLinks(Rid map)
```

Returns all navigation link [`Rid`](Rid.md)s that are currently assigned to the requested navigation `map`.

### MapGetMergeRasterizerCellScale

```csharp
public float MapGetMergeRasterizerCellScale(Rid map)
```

Returns map's internal merge rasterizer cell scale.

### MapGetObstacles

```csharp
public Array<Rid> MapGetObstacles(Rid map)
```

Returns all navigation obstacle [`Rid`](Rid.md)s that are currently assigned to the requested navigation `map`.

### MapGetPath

```csharp
public Vector2[] MapGetPath(Rid map, Vector2 origin, Vector2 destination, bool optimize, uint navigationLayers = 1)
```

Returns the navigation path to reach the destination from the origin. `navigationLayers` is a bitmask of all region navigation layers that are allowed to be in the path.

### MapGetRandomPoint

```csharp
public Vector2 MapGetRandomPoint(Rid map, uint navigationLayers, bool uniformly)
```

Returns a random position picked from all map region polygons with matching `navigationLayers`.

If `uniformly` is `true`, all map regions, polygons, and faces are weighted by their surface area (slower).

If `uniformly` is `false`, just a random region and a random polygon are picked (faster).

### MapGetRegions

```csharp
public Array<Rid> MapGetRegions(Rid map)
```

Returns all navigation regions [`Rid`](Rid.md)s that are currently assigned to the requested navigation `map`.

### MapGetUseAsyncIterations

```csharp
public bool MapGetUseAsyncIterations(Rid map)
```

Returns `true` if the `map` synchronization uses an async process that runs on a background thread.

### MapGetUseEdgeConnections

```csharp
public bool MapGetUseEdgeConnections(Rid map)
```

Returns whether the navigation `map` allows navigation regions to use edge connections to connect with other navigation regions within proximity of the navigation map edge connection margin.

### MapIsActive

```csharp
public bool MapIsActive(Rid map)
```

Returns `true` if the map is active.

### MapSetActive

```csharp
public void MapSetActive(Rid map, bool active)
```

Sets the map active.

### MapSetCellSize

```csharp
public void MapSetCellSize(Rid map, float cellSize)
```

Sets the map cell size used to rasterize the navigation mesh vertices. Must match with the cell size of the used navigation meshes.

### MapSetEdgeConnectionMargin

```csharp
public void MapSetEdgeConnectionMargin(Rid map, float margin)
```

Set the map edge connection margin used to weld the compatible region edges.

### MapSetLinkConnectionRadius

```csharp
public void MapSetLinkConnectionRadius(Rid map, float radius)
```

Set the map's link connection radius used to connect links to navigation polygons.

### MapSetMergeRasterizerCellScale

```csharp
public void MapSetMergeRasterizerCellScale(Rid map, float scale)
```

Set the map's internal merge rasterizer cell scale used to control merging sensitivity.

### MapSetUseAsyncIterations

```csharp
public void MapSetUseAsyncIterations(Rid map, bool enabled)
```

If `enabled` is `true` the `map` synchronization uses an async process that runs on a background thread.

### MapSetUseEdgeConnections

```csharp
public void MapSetUseEdgeConnections(Rid map, bool enabled)
```

Set the navigation `map` edge connection use. If `enabled` is `true`, the navigation map allows navigation regions to use edge connections to connect with other navigation regions within proximity of the navigation map edge connection margin.

### ObstacleCreate

```csharp
public Rid ObstacleCreate()
```

Creates a new navigation obstacle.

### ObstacleGetAvoidanceEnabled

```csharp
public bool ObstacleGetAvoidanceEnabled(Rid obstacle)
```

Returns `true` if the provided `obstacle` has avoidance enabled.

### ObstacleGetAvoidanceLayers

```csharp
public uint ObstacleGetAvoidanceLayers(Rid obstacle)
```

Returns the `avoidance_layers` bitmask of the specified `obstacle`.

### ObstacleGetMap

```csharp
public Rid ObstacleGetMap(Rid obstacle)
```

Returns the navigation map [`Rid`](Rid.md) the requested `obstacle` is currently assigned to.

### ObstacleGetPaused

```csharp
public bool ObstacleGetPaused(Rid obstacle)
```

Returns `true` if the specified `obstacle` is paused.

### ObstacleGetPosition

```csharp
public Vector2 ObstacleGetPosition(Rid obstacle)
```

Returns the position of the specified `obstacle` in world space.

### ObstacleGetRadius

```csharp
public float ObstacleGetRadius(Rid obstacle)
```

Returns the radius of the specified dynamic `obstacle`.

### ObstacleGetVelocity

```csharp
public Vector2 ObstacleGetVelocity(Rid obstacle)
```

Returns the velocity of the specified dynamic `obstacle`.

### ObstacleGetVertices

```csharp
public Vector2[] ObstacleGetVertices(Rid obstacle)
```

Returns the outline vertices for the specified `obstacle`.

### ObstacleSetAvoidanceEnabled

```csharp
public void ObstacleSetAvoidanceEnabled(Rid obstacle, bool enabled)
```

If `enabled` is `true`, the provided `obstacle` affects avoidance using agents.

### ObstacleSetAvoidanceLayers

```csharp
public void ObstacleSetAvoidanceLayers(Rid obstacle, uint layers)
```

Set the obstacles's `avoidance_layers` bitmask.

### ObstacleSetMap

```csharp
public void ObstacleSetMap(Rid obstacle, Rid map)
```

Sets the navigation map [`Rid`](Rid.md) for the obstacle.

### ObstacleSetPaused

```csharp
public void ObstacleSetPaused(Rid obstacle, bool paused)
```

If `paused` is `true` the specified `obstacle` will not be processed. For example, it will no longer affect avoidance velocities.

### ObstacleSetPosition

```csharp
public void ObstacleSetPosition(Rid obstacle, Vector2 position)
```

Sets the position of the obstacle in world space.

### ObstacleSetRadius

```csharp
public void ObstacleSetRadius(Rid obstacle, float radius)
```

Sets the radius of the dynamic obstacle.

### ObstacleSetVelocity

```csharp
public void ObstacleSetVelocity(Rid obstacle, Vector2 velocity)
```

Sets `velocity` of the dynamic `obstacle`. Allows other agents to better predict the movement of the dynamic obstacle. Only works in combination with the radius of the obstacle.

### ObstacleSetVertices

```csharp
public void ObstacleSetVertices(Rid obstacle, ReadOnlySpan<Vector2> vertices)
```

Sets the outline vertices for the obstacle. If the vertices are winded in clockwise order agents will be pushed in by the obstacle, else they will be pushed out.

### ObstacleSetVertices

```csharp
public void ObstacleSetVertices(Rid obstacle, Vector2[] vertices)
```

Sets the outline vertices for the obstacle. If the vertices are winded in clockwise order agents will be pushed in by the obstacle, else they will be pushed out.

### ParseSourceGeometryData

```csharp
public void ParseSourceGeometryData(NavigationPolygon navigationPolygon, NavigationMeshSourceGeometryData2D sourceGeometryData, Node rootNode, Callable callback = default)
```

Parses the [`SceneTree`](SceneTree.md) for source geometry according to the properties of `navigationPolygon`. Updates the provided `sourceGeometryData` resource with the resulting data. The resource can then be used to bake a navigation mesh with `BakeFromSourceGeometryData()`. After the process is finished the optional `callback` will be called.

**Note:** This function needs to run on the main thread or with a deferred call as the SceneTree is not thread-safe.

**Performance:** While convenient, reading data arrays from [`Mesh`](Mesh.md) resources can affect the frame rate negatively. The data needs to be received from the GPU, stalling the [`RenderingServer`](RenderingServer.md) in the process. For performance prefer the use of e.g. collision shapes or creating the data arrays entirely in code.

### QueryPath

```csharp
public void QueryPath(NavigationPathQueryParameters2D parameters, NavigationPathQueryResult2D result)
```

Queries a path in a given navigation map. Start and target position and other parameters are defined through [`NavigationPathQueryParameters2D`](NavigationPathQueryParameters2D.md). Updates the provided [`NavigationPathQueryResult2D`](NavigationPathQueryResult2D.md) result object with the path among other results requested by the query. After the process is finished the optional `callback` will be called.

### QueryPath

```csharp
public void QueryPath(NavigationPathQueryParameters2D parameters, NavigationPathQueryResult2D result, Callable callback = default)
```

Queries a path in a given navigation map. Start and target position and other parameters are defined through [`NavigationPathQueryParameters2D`](NavigationPathQueryParameters2D.md). Updates the provided [`NavigationPathQueryResult2D`](NavigationPathQueryResult2D.md) result object with the path among other results requested by the query. After the process is finished the optional `callback` will be called.

### RegionCreate

```csharp
public Rid RegionCreate()
```

Creates a new region.

### RegionGetBounds

```csharp
public Rect2 RegionGetBounds(Rid region)
```

Returns the axis-aligned rectangle for the `region`'s transformed navigation mesh.

### RegionGetClosestPoint

```csharp
public Vector2 RegionGetClosestPoint(Rid region, Vector2 toPoint)
```

Returns the navigation mesh surface point closest to the provided `toPoint` on the navigation `region`.

### RegionGetConnectionPathwayEnd

```csharp
public Vector2 RegionGetConnectionPathwayEnd(Rid region, int connection)
```

Returns the ending point of a connection door. `connection` is an index between 0 and the return value of `RegionGetConnectionsCount()`.

### RegionGetConnectionPathwayStart

```csharp
public Vector2 RegionGetConnectionPathwayStart(Rid region, int connection)
```

Returns the starting point of a connection door. `connection` is an index between 0 and the return value of `RegionGetConnectionsCount()`.

### RegionGetConnectionsCount

```csharp
public int RegionGetConnectionsCount(Rid region)
```

Returns how many connections this `region` has with other regions in the map.

### RegionGetEnabled

```csharp
public bool RegionGetEnabled(Rid region)
```

Returns `true` if the specified `region` is enabled.

### RegionGetEnterCost

```csharp
public float RegionGetEnterCost(Rid region)
```

Returns the enter cost of this `region`.

### RegionGetIterationId

```csharp
public uint RegionGetIterationId(Rid region)
```

Returns the current iteration ID of the navigation region. Every time the navigation region changes and synchronizes, the iteration ID increases. An iteration ID of `0` means the navigation region has never synchronized.

**Note:** The iteration ID will wrap around to `1` after reaching its range limit.

### RegionGetMap

```csharp
public Rid RegionGetMap(Rid region)
```

Returns the navigation map [`Rid`](Rid.md) the requested `region` is currently assigned to.

### RegionGetNavigationLayers

```csharp
public uint RegionGetNavigationLayers(Rid region)
```

Returns the region's navigation layers.

### RegionGetOwnerId

```csharp
public ulong RegionGetOwnerId(Rid region)
```

Returns the `ObjectID` of the object which manages this region.

### RegionGetRandomPoint

```csharp
public Vector2 RegionGetRandomPoint(Rid region, uint navigationLayers, bool uniformly)
```

Returns a random position picked from all region polygons with matching `navigationLayers`.

If `uniformly` is `true`, all region polygons and faces are weighted by their surface area (slower).

If `uniformly` is `false`, just a random polygon and face is picked (faster).

### RegionGetTransform

```csharp
public Transform2D RegionGetTransform(Rid region)
```

Returns the global transformation of this `region`.

### RegionGetTravelCost

```csharp
public float RegionGetTravelCost(Rid region)
```

Returns the travel cost of this `region`.

### RegionGetUseAsyncIterations

```csharp
public bool RegionGetUseAsyncIterations(Rid region)
```

Returns `true` if the `region` uses an async synchronization process that runs on a background thread.

### RegionGetUseEdgeConnections

```csharp
public bool RegionGetUseEdgeConnections(Rid region)
```

Returns whether the navigation `region` is set to use edge connections to connect with other navigation regions within proximity of the navigation map edge connection margin.

### RegionOwnsPoint

```csharp
public bool RegionOwnsPoint(Rid region, Vector2 point)
```

Returns `true` if the provided `point` in world space is currently owned by the provided navigation `region`. Owned in this context means that one of the region's navigation mesh polygon faces has a possible position at the closest distance to this point compared to all other navigation meshes from other navigation regions that are also registered on the navigation map of the provided region.

If multiple navigation meshes have positions at equal distance the navigation region whose polygons are processed first wins the ownership. Polygons are processed in the same order that navigation regions were registered on the NavigationServer.

**Note:** If navigation meshes from different navigation regions overlap (which should be avoided in general) the result might not be what is expected.

### RegionSetEnabled

```csharp
public void RegionSetEnabled(Rid region, bool enabled)
```

If `enabled` is `true` the specified `region` will contribute to its current navigation map.

### RegionSetEnterCost

```csharp
public void RegionSetEnterCost(Rid region, float enterCost)
```

Sets the `enterCost` for this `region`.

### RegionSetMap

```csharp
public void RegionSetMap(Rid region, Rid map)
```

Sets the map for the region.

### RegionSetNavigationLayers

```csharp
public void RegionSetNavigationLayers(Rid region, uint navigationLayers)
```

Set the region's navigation layers. This allows selecting regions from a path request (when using [`NavigationServer2D.MapGetPath`](NavigationServer2D.md)).

### RegionSetNavigationPolygon

```csharp
public void RegionSetNavigationPolygon(Rid region, NavigationPolygon navigationPolygon)
```

Sets the `navigationPolygon` for the region.

### RegionSetOwnerId

```csharp
public void RegionSetOwnerId(Rid region, ulong ownerId)
```

Set the `ObjectID` of the object which manages this region.

### RegionSetTransform

```csharp
public void RegionSetTransform(Rid region, Transform2D transform)
```

Sets the global transformation for the region.

### RegionSetTravelCost

```csharp
public void RegionSetTravelCost(Rid region, float travelCost)
```

Sets the `travelCost` for this `region`.

### RegionSetUseAsyncIterations

```csharp
public void RegionSetUseAsyncIterations(Rid region, bool enabled)
```

If `enabled` is `true` the `region` uses an async synchronization process that runs on a background thread.

### RegionSetUseEdgeConnections

```csharp
public void RegionSetUseEdgeConnections(Rid region, bool enabled)
```

If `enabled` is `true`, the navigation `region` will use edge connections to connect with other navigation regions within proximity of the navigation map edge connection margin.

### SetActive

```csharp
public void SetActive(bool active)
```

Control activation of this server.

### SetDebugEnabled

```csharp
public void SetDebugEnabled(bool enabled)
```

If `true` enables debug mode on the NavigationServer.

### SimplifyPath

```csharp
public Vector2[] SimplifyPath(ReadOnlySpan<Vector2> path, float epsilon)
```

Returns a simplified version of `path` with less critical path points removed. The simplification amount is in worlds units and controlled by `epsilon`. The simplification uses a variant of Ramer-Douglas-Peucker algorithm for curve point decimation.

Path simplification can be helpful to mitigate various path following issues that can arise with certain agent types and script behaviors. E.g. "steering" agents or avoidance in "open fields".

### SimplifyPath

```csharp
public Vector2[] SimplifyPath(Vector2[] path, float epsilon)
```

Returns a simplified version of `path` with less critical path points removed. The simplification amount is in worlds units and controlled by `epsilon`. The simplification uses a variant of Ramer-Douglas-Peucker algorithm for curve point decimation.

Path simplification can be helpful to mitigate various path following issues that can arise with certain agent types and script behaviors. E.g. "steering" agents or avoidance in "open fields".

### SourceGeometryParserCreate

```csharp
public Rid SourceGeometryParserCreate()
```

Creates a new source geometry parser. If a [`Callable`](Callable.md) is set for the parser with `SourceGeometryParserSetCallback()` the callback will be called for every single node that gets parsed whenever `ParseSourceGeometryData()` is used.

### SourceGeometryParserSetCallback

```csharp
public void SourceGeometryParserSetCallback(Rid parser, Callable callback)
```

Sets the `callback` [`Callable`](Callable.md) for the specific source geometry `parser`. The [`Callable`](Callable.md) will receive a call with the following parameters:

- `navigation_mesh` - The [`NavigationPolygon`](NavigationPolygon.md) reference used to define the parse settings. Do NOT edit or add directly to the navigation mesh.

- `source_geometry_data` - The [`NavigationMeshSourceGeometryData2D`](NavigationMeshSourceGeometryData2D.md) reference. Add custom source geometry for navigation mesh baking to this object.

- `node` - The [`Node`](Node.md) that is parsed.

## 事件（信号）

### AvoidanceDebugChanged

```csharp
public event Action AvoidanceDebugChanged
```

Emitted when avoidance debug settings are changed. Only available in debug builds.

### MapChanged

```csharp
public event NavigationServer2DInstance.MapChangedEventHandler MapChanged
```

Emitted when a navigation map is updated, when a region moves or is modified.

### NavigationDebugChanged

```csharp
public event Action NavigationDebugChanged
```

Emitted when navigation debug settings are changed. Only available in debug builds.

## MapChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`NavigationServer2DInstance.MapChanged`](NavigationServer2DInstance.md) event of a [`NavigationServer2DInstance`](NavigationServer2DInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void MapChangedEventHandler(Rid map)
```
