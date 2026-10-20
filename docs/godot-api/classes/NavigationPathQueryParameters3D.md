<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationPathQueryParameters3D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

By changing various properties of this object, such as the start and target position, you can configure path queries to the [`NavigationServer3D`](NavigationServer3D.md).

## 构造函数

### NavigationPathQueryParameters3D

```csharp
public NavigationPathQueryParameters3D()
```

## 属性

### ExcludedRegions

```csharp
public Array<Rid> ExcludedRegions { get; set; }
```

引擎名：`excluded_regions`

The list of region [`Rid`](Rid.md)s that will be excluded from the path query. Use [`NavigationRegion3D.GetRid`](NavigationRegion3D.md) to get the [`Rid`](Rid.md) associated with a [`NavigationRegion3D`](NavigationRegion3D.md) node.

**Note:** The returned array is copied and any changes to it will not update the original property value. To update the value you need to modify the returned array, and then set it to the property again.

### IncludedRegions

```csharp
public Array<Rid> IncludedRegions { get; set; }
```

引擎名：`included_regions`

The list of region [`Rid`](Rid.md)s that will be included by the path query. Use [`NavigationRegion3D.GetRid`](NavigationRegion3D.md) to get the [`Rid`](Rid.md) associated with a [`NavigationRegion3D`](NavigationRegion3D.md) node. If left empty all regions are included. If a region ends up being both included and excluded at the same time it will be excluded.

**Note:** The returned array is copied and any changes to it will not update the original property value. To update the value you need to modify the returned array, and then set it to the property again.

### Map

```csharp
public Rid Map { get; set; }
```

引擎名：`map`

The navigation map [`Rid`](Rid.md) used in the path query.

### MetadataFlags

```csharp
public NavigationPathQueryParameters3D.PathMetadataFlags MetadataFlags { get; set; }
```

引擎名：`metadata_flags`

Additional information to include with the navigation path.

### NavigationLayers

```csharp
public uint NavigationLayers { get; set; }
```

引擎名：`navigation_layers`

The navigation layers the query will use (as a bitmask).

### PathPostprocessing

```csharp
public NavigationPathQueryParameters3D.PathPostProcessing PathPostprocessing { get; set; }
```

引擎名：`path_postprocessing`

The path postprocessing applied to the raw path corridor found by the `PathfindingAlgorithm`.

### PathReturnMaxLength

```csharp
public float PathReturnMaxLength { get; set; }
```

引擎名：`path_return_max_length`

The maximum allowed length of the returned path in world units. A path will be clipped when going over this length. A value of `0` or below counts as disabled.

### PathReturnMaxRadius

```csharp
public float PathReturnMaxRadius { get; set; }
```

引擎名：`path_return_max_radius`

The maximum allowed radius in world units that the returned path can be from the path start. The path will be clipped when going over this radius. A value of `0` or below counts as disabled.

**Note:** This will perform a sphere shaped clip operation on the path with the first path position being the sphere's center position.

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
public NavigationPathQueryParameters3D.PathfindingAlgorithmEnum PathfindingAlgorithm { get; set; }
```

引擎名：`pathfinding_algorithm`

The pathfinding algorithm used in the path query.

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

### StartPosition

```csharp
public Vector3 StartPosition { get; set; }
```

引擎名：`start_position`

The pathfinding start position in global coordinates.

### TargetPosition

```csharp
public Vector3 TargetPosition { get; set; }
```

引擎名：`target_position`

The pathfinding target position in global coordinates.

## 方法

### GetExcludedRegions

```csharp
public Array<Rid> GetExcludedRegions()
```

引擎名：`get_excluded_regions`

### GetIncludedRegions

```csharp
public Array<Rid> GetIncludedRegions()
```

引擎名：`get_included_regions`

### GetMap

```csharp
public Rid GetMap()
```

引擎名：`get_map`

### GetMetadataFlags

```csharp
public NavigationPathQueryParameters3D.PathMetadataFlags GetMetadataFlags()
```

引擎名：`get_metadata_flags`

### GetNavigationLayers

```csharp
public uint GetNavigationLayers()
```

引擎名：`get_navigation_layers`

### GetPathPostprocessing

```csharp
public NavigationPathQueryParameters3D.PathPostProcessing GetPathPostprocessing()
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
public NavigationPathQueryParameters3D.PathfindingAlgorithmEnum GetPathfindingAlgorithm()
```

引擎名：`get_pathfinding_algorithm`

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

### GetStartPosition

```csharp
public Vector3 GetStartPosition()
```

引擎名：`get_start_position`

### GetTargetPosition

```csharp
public Vector3 GetTargetPosition()
```

引擎名：`get_target_position`

### SetExcludedRegions

```csharp
public void SetExcludedRegions(Array<Rid> regions)
```

引擎名：`set_excluded_regions`

### SetIncludedRegions

```csharp
public void SetIncludedRegions(Array<Rid> regions)
```

引擎名：`set_included_regions`

### SetMap

```csharp
public void SetMap(Rid map)
```

引擎名：`set_map`

### SetMetadataFlags

```csharp
public void SetMetadataFlags(NavigationPathQueryParameters3D.PathMetadataFlags flags)
```

引擎名：`set_metadata_flags`

### SetNavigationLayers

```csharp
public void SetNavigationLayers(uint navigationLayers)
```

引擎名：`set_navigation_layers`

### SetPathPostprocessing

```csharp
public void SetPathPostprocessing(NavigationPathQueryParameters3D.PathPostProcessing pathPostprocessing)
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
public void SetPathfindingAlgorithm(NavigationPathQueryParameters3D.PathfindingAlgorithmEnum pathfindingAlgorithm)
```

引擎名：`set_pathfinding_algorithm`

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

### SetStartPosition

```csharp
public void SetStartPosition(Vector3 startPosition)
```

引擎名：`set_start_position`

### SetTargetPosition

```csharp
public void SetTargetPosition(Vector3 targetPosition)
```

引擎名：`set_target_position`

## PathMetadataFlags

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Don't include any additional metadata about the returned path.
- `Types = 1`：Include the type of navigation primitive (region or link) that each point of the path goes through.
- `Rids = 2`：Include the [`Rid`](Rid.md)s of the regions and links that each point of the path goes through.
- `Owners = 4`：Include the `ObjectID`s of the [`GodotObject`](GodotObject.md)s which manage the regions and links each point of the path goes through.
- `All = 7`：Include all available metadata about the returned path.

## PathPostProcessing

命名空间 `Godot`；enum

### 枚举值

- `Corridorfunnel = 0`：Applies a funnel algorithm to the raw path corridor found by the pathfinding algorithm.
- `Edgecentered = 1`：Centers every path position in the middle of the traveled navigation mesh polygon edge.
- `None = 2`：Applies no postprocessing and returns the raw path corridor as found by the pathfinding algorithm.

## PathfindingAlgorithmEnum

命名空间 `Godot`；enum

### 枚举值

- `Astar = 0`：The path query uses the default A* pathfinding algorithm.
