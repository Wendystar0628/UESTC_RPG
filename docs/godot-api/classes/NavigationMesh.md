<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationMesh

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A navigation mesh is a collection of polygons that define which areas of an environment are traversable to aid agents in pathfinding through complicated spaces.

## 构造函数

### NavigationMesh

```csharp
public NavigationMesh()
```

## 属性

### AgentHeight

```csharp
public float AgentHeight { get; set; }
```

引擎名：`agent_height`

The minimum floor to ceiling height that will still allow the floor area to be considered walkable.

**Note:** While baking, this value will be rounded up to the nearest multiple of `CellHeight`.

### AgentMaxClimb

```csharp
public float AgentMaxClimb { get; set; }
```

引擎名：`agent_max_climb`

The minimum ledge height that is considered to still be traversable.

**Note:** While baking, this value will be rounded down to the nearest multiple of `CellHeight`.

### AgentMaxSlope

```csharp
public float AgentMaxSlope { get; set; }
```

引擎名：`agent_max_slope`

The maximum slope that is considered walkable, in degrees.

### AgentRadius

```csharp
public float AgentRadius { get; set; }
```

引擎名：`agent_radius`

The distance to erode/shrink the walkable area of the heightfield away from obstructions.

**Note:** While baking, this value will be rounded up to the nearest multiple of `CellSize`.

**Note:** The radius must be equal or higher than `0.0`. If the radius is `0.0`, it won't be possible to fix invalid outline overlaps and other precision errors during the baking process. As a result, some obstacles may be excluded incorrectly from the final navigation mesh, or may delete the navigation mesh's polygons.

### BorderSize

```csharp
public float BorderSize { get; set; }
```

引擎名：`border_size`

The size of the non-navigable border around the bake bounding area.

In conjunction with the `FilterBakingAabb` and a `EdgeMaxError` value at `1.0` or below the border size can be used to bake tile aligned navigation meshes without the tile edges being shrunk by `AgentRadius`.

**Note:** If this value is not `0.0`, it will be rounded up to the nearest multiple of `CellSize` during baking.

### CellHeight

```csharp
public float CellHeight { get; set; }
```

引擎名：`cell_height`

The cell height used to rasterize the navigation mesh vertices on the Y axis. Must match with the cell height on the navigation map.

### CellSize

```csharp
public float CellSize { get; set; }
```

引擎名：`cell_size`

The cell size used to rasterize the navigation mesh vertices on the XZ plane. Must match with the cell size on the navigation map.

### DetailSampleDistance

```csharp
public float DetailSampleDistance { get; set; }
```

引擎名：`detail_sample_distance`

The sampling distance to use when generating the detail mesh, in cell unit.

### DetailSampleMaxError

```csharp
public float DetailSampleMaxError { get; set; }
```

引擎名：`detail_sample_max_error`

The maximum distance the detail mesh surface should deviate from heightfield, in cell unit.

### EdgeMaxError

```csharp
public float EdgeMaxError { get; set; }
```

引擎名：`edge_max_error`

The maximum distance a simplified contour's border edges should deviate the original raw contour.

### EdgeMaxLength

```csharp
public float EdgeMaxLength { get; set; }
```

引擎名：`edge_max_length`

The maximum allowed length for contour edges along the border of the mesh. A value of `0.0` disables this feature.

**Note:** While baking, this value will be rounded up to the nearest multiple of `CellSize`.

### FilterBakingAabb

```csharp
public Aabb FilterBakingAabb { get; set; }
```

引擎名：`filter_baking_aabb`

If the baking [`Aabb`](Aabb.md) has a volume the navigation mesh baking will be restricted to its enclosing area.

### FilterBakingAabbOffset

```csharp
public Vector3 FilterBakingAabbOffset { get; set; }
```

引擎名：`filter_baking_aabb_offset`

The position offset applied to the `FilterBakingAabb` [`Aabb`](Aabb.md).

### FilterLedgeSpans

```csharp
public bool FilterLedgeSpans { get; set; }
```

引擎名：`filter_ledge_spans`

If `true`, marks spans that are ledges as non-walkable.

### FilterLowHangingObstacles

```csharp
public bool FilterLowHangingObstacles { get; set; }
```

引擎名：`filter_low_hanging_obstacles`

If `true`, marks non-walkable spans as walkable if their maximum is within `AgentMaxClimb` of a walkable neighbor.

### FilterWalkableLowHeightSpans

```csharp
public bool FilterWalkableLowHeightSpans { get; set; }
```

引擎名：`filter_walkable_low_height_spans`

If `true`, marks walkable spans as not walkable if the clearance above the span is less than `AgentHeight`.

### GeometryCollisionMask

```csharp
public uint GeometryCollisionMask { get; set; }
```

引擎名：`geometry_collision_mask`

The physics layers to scan for static colliders.

Only used when `GeometryParsedGeometryType` is [`NavigationMesh.ParsedGeometryType.StaticColliders`](NavigationMesh.md) or [`NavigationMesh.ParsedGeometryType.Both`](NavigationMesh.md).

### GeometryParsedGeometryType

```csharp
public NavigationMesh.ParsedGeometryType GeometryParsedGeometryType { get; set; }
```

引擎名：`geometry_parsed_geometry_type`

Determines which type of nodes will be parsed as geometry.

### GeometrySourceGeometryMode

```csharp
public NavigationMesh.SourceGeometryMode GeometrySourceGeometryMode { get; set; }
```

引擎名：`geometry_source_geometry_mode`

The source of the geometry used when baking.

### GeometrySourceGroupName

```csharp
public StringName GeometrySourceGroupName { get; set; }
```

引擎名：`geometry_source_group_name`

The name of the group to scan for geometry.

Only used when `GeometrySourceGeometryMode` is [`NavigationMesh.SourceGeometryMode.GroupsWithChildren`](NavigationMesh.md) or [`NavigationMesh.SourceGeometryMode.GroupsExplicit`](NavigationMesh.md).

### Polygons

```csharp
public Array Polygons { get; set; }
```

引擎名：`polygons`

### RegionMergeSize

```csharp
public float RegionMergeSize { get; set; }
```

引擎名：`region_merge_size`

Any regions with a size smaller than this will be merged with larger regions if possible.

**Note:** This value will be squared to calculate the number of cells. For example, a value of 20 will set the number of cells to 400.

### RegionMinSize

```csharp
public float RegionMinSize { get; set; }
```

引擎名：`region_min_size`

The minimum size of a region for it to be created.

**Note:** This value will be squared to calculate the minimum number of cells allowed to form isolated island areas. For example, a value of 8 will set the number of cells to 64.

### SamplePartitionType

```csharp
public NavigationMesh.SamplePartitionTypeEnum SamplePartitionType { get; set; }
```

引擎名：`sample_partition_type`

Partitioning algorithm for creating the navigation mesh polys.

### Vertices

```csharp
public Vector3[] Vertices { get; set; }
```

引擎名：`vertices`

### VerticesPerPolygon

```csharp
public float VerticesPerPolygon { get; set; }
```

引擎名：`vertices_per_polygon`

The maximum number of vertices allowed for polygons generated during the contour to polygon conversion process.

## 方法

### AddPolygon

```csharp
public void AddPolygon(ReadOnlySpan<int> polygon)
```

引擎名：`add_polygon`

Adds a polygon using the indices of the vertices you get when calling `GetVertices()`.

### AddPolygon

```csharp
public void AddPolygon(int[] polygon)
```

引擎名：`add_polygon`

Adds a polygon using the indices of the vertices you get when calling `GetVertices()`.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears the internal arrays for vertices and polygon indices.

### ClearPolygons

```csharp
public void ClearPolygons()
```

引擎名：`clear_polygons`

Clears the array of polygons, but it doesn't clear the array of vertices.

### CreateFromMesh

```csharp
public void CreateFromMesh(Mesh mesh)
```

引擎名：`create_from_mesh`

Initializes the navigation mesh by setting the vertices and indices according to a [`Mesh`](Mesh.md).

**Note:** The given `mesh` must be of type [`Mesh.PrimitiveType.Triangles`](Mesh.md) and have an index array.

### GetAgentHeight

```csharp
public float GetAgentHeight()
```

引擎名：`get_agent_height`

### GetAgentMaxClimb

```csharp
public float GetAgentMaxClimb()
```

引擎名：`get_agent_max_climb`

### GetAgentMaxSlope

```csharp
public float GetAgentMaxSlope()
```

引擎名：`get_agent_max_slope`

### GetAgentRadius

```csharp
public float GetAgentRadius()
```

引擎名：`get_agent_radius`

### GetBorderSize

```csharp
public float GetBorderSize()
```

引擎名：`get_border_size`

### GetCellHeight

```csharp
public float GetCellHeight()
```

引擎名：`get_cell_height`

### GetCellSize

```csharp
public float GetCellSize()
```

引擎名：`get_cell_size`

### GetCollisionMask

```csharp
public uint GetCollisionMask()
```

引擎名：`get_collision_mask`

### GetCollisionMaskValue

```csharp
public bool GetCollisionMaskValue(int layerNumber)
```

引擎名：`get_collision_mask_value`

Returns whether or not the specified layer of the `GeometryCollisionMask` is enabled, given a `layerNumber` between 1 and 32.

### GetDetailSampleDistance

```csharp
public float GetDetailSampleDistance()
```

引擎名：`get_detail_sample_distance`

### GetDetailSampleMaxError

```csharp
public float GetDetailSampleMaxError()
```

引擎名：`get_detail_sample_max_error`

### GetEdgeMaxError

```csharp
public float GetEdgeMaxError()
```

引擎名：`get_edge_max_error`

### GetEdgeMaxLength

```csharp
public float GetEdgeMaxLength()
```

引擎名：`get_edge_max_length`

### GetFilterBakingAabb

```csharp
public Aabb GetFilterBakingAabb()
```

引擎名：`get_filter_baking_aabb`

### GetFilterBakingAabbOffset

```csharp
public Vector3 GetFilterBakingAabbOffset()
```

引擎名：`get_filter_baking_aabb_offset`

### GetFilterLedgeSpans

```csharp
public bool GetFilterLedgeSpans()
```

引擎名：`get_filter_ledge_spans`

### GetFilterLowHangingObstacles

```csharp
public bool GetFilterLowHangingObstacles()
```

引擎名：`get_filter_low_hanging_obstacles`

### GetFilterWalkableLowHeightSpans

```csharp
public bool GetFilterWalkableLowHeightSpans()
```

引擎名：`get_filter_walkable_low_height_spans`

### GetParsedGeometryType

```csharp
public NavigationMesh.ParsedGeometryType GetParsedGeometryType()
```

引擎名：`get_parsed_geometry_type`

### GetPolygon

```csharp
public int[] GetPolygon(int idx)
```

引擎名：`get_polygon`

Returns a `System.Int32`[] containing the indices of the vertices of a created polygon.

### GetPolygonCount

```csharp
public int GetPolygonCount()
```

引擎名：`get_polygon_count`

Returns the number of polygons in the navigation mesh.

### GetRegionMergeSize

```csharp
public float GetRegionMergeSize()
```

引擎名：`get_region_merge_size`

### GetRegionMinSize

```csharp
public float GetRegionMinSize()
```

引擎名：`get_region_min_size`

### GetSamplePartitionType

```csharp
public NavigationMesh.SamplePartitionTypeEnum GetSamplePartitionType()
```

引擎名：`get_sample_partition_type`

### GetSourceGeometryMode

```csharp
public NavigationMesh.SourceGeometryMode GetSourceGeometryMode()
```

引擎名：`get_source_geometry_mode`

### GetSourceGroupName

```csharp
public StringName GetSourceGroupName()
```

引擎名：`get_source_group_name`

### GetVertices

```csharp
public Vector3[] GetVertices()
```

引擎名：`get_vertices`

Returns a [`Vector3`](Vector3.md)[] containing all the vertices being used to create the polygons.

### GetVerticesPerPolygon

```csharp
public float GetVerticesPerPolygon()
```

引擎名：`get_vertices_per_polygon`

### SetAgentHeight

```csharp
public void SetAgentHeight(float agentHeight)
```

引擎名：`set_agent_height`

### SetAgentMaxClimb

```csharp
public void SetAgentMaxClimb(float agentMaxClimb)
```

引擎名：`set_agent_max_climb`

### SetAgentMaxSlope

```csharp
public void SetAgentMaxSlope(float agentMaxSlope)
```

引擎名：`set_agent_max_slope`

### SetAgentRadius

```csharp
public void SetAgentRadius(float agentRadius)
```

引擎名：`set_agent_radius`

### SetBorderSize

```csharp
public void SetBorderSize(float borderSize)
```

引擎名：`set_border_size`

### SetCellHeight

```csharp
public void SetCellHeight(float cellHeight)
```

引擎名：`set_cell_height`

### SetCellSize

```csharp
public void SetCellSize(float cellSize)
```

引擎名：`set_cell_size`

### SetCollisionMask

```csharp
public void SetCollisionMask(uint mask)
```

引擎名：`set_collision_mask`

### SetCollisionMaskValue

```csharp
public void SetCollisionMaskValue(int layerNumber, bool value)
```

引擎名：`set_collision_mask_value`

Based on `value`, enables or disables the specified layer in the `GeometryCollisionMask`, given a `layerNumber` between 1 and 32.

### SetDetailSampleDistance

```csharp
public void SetDetailSampleDistance(float detailSampleDist)
```

引擎名：`set_detail_sample_distance`

### SetDetailSampleMaxError

```csharp
public void SetDetailSampleMaxError(float detailSampleMaxError)
```

引擎名：`set_detail_sample_max_error`

### SetEdgeMaxError

```csharp
public void SetEdgeMaxError(float edgeMaxError)
```

引擎名：`set_edge_max_error`

### SetEdgeMaxLength

```csharp
public void SetEdgeMaxLength(float edgeMaxLength)
```

引擎名：`set_edge_max_length`

### SetFilterBakingAabb

```csharp
public void SetFilterBakingAabb(Aabb bakingAabb)
```

引擎名：`set_filter_baking_aabb`

### SetFilterBakingAabbOffset

```csharp
public void SetFilterBakingAabbOffset(Vector3 bakingAabbOffset)
```

引擎名：`set_filter_baking_aabb_offset`

### SetFilterLedgeSpans

```csharp
public void SetFilterLedgeSpans(bool filterLedgeSpans)
```

引擎名：`set_filter_ledge_spans`

### SetFilterLowHangingObstacles

```csharp
public void SetFilterLowHangingObstacles(bool filterLowHangingObstacles)
```

引擎名：`set_filter_low_hanging_obstacles`

### SetFilterWalkableLowHeightSpans

```csharp
public void SetFilterWalkableLowHeightSpans(bool filterWalkableLowHeightSpans)
```

引擎名：`set_filter_walkable_low_height_spans`

### SetParsedGeometryType

```csharp
public void SetParsedGeometryType(NavigationMesh.ParsedGeometryType geometryType)
```

引擎名：`set_parsed_geometry_type`

### SetRegionMergeSize

```csharp
public void SetRegionMergeSize(float regionMergeSize)
```

引擎名：`set_region_merge_size`

### SetRegionMinSize

```csharp
public void SetRegionMinSize(float regionMinSize)
```

引擎名：`set_region_min_size`

### SetSamplePartitionType

```csharp
public void SetSamplePartitionType(NavigationMesh.SamplePartitionTypeEnum samplePartitionType)
```

引擎名：`set_sample_partition_type`

### SetSourceGeometryMode

```csharp
public void SetSourceGeometryMode(NavigationMesh.SourceGeometryMode mask)
```

引擎名：`set_source_geometry_mode`

### SetSourceGroupName

```csharp
public void SetSourceGroupName(StringName mask)
```

引擎名：`set_source_group_name`

### SetVertices

```csharp
public void SetVertices(ReadOnlySpan<Vector3> vertices)
```

引擎名：`set_vertices`

Sets the vertices that can be then indexed to create polygons with the `AddPolygon()` method.

### SetVertices

```csharp
public void SetVertices(Vector3[] vertices)
```

引擎名：`set_vertices`

Sets the vertices that can be then indexed to create polygons with the `AddPolygon()` method.

### SetVerticesPerPolygon

```csharp
public void SetVerticesPerPolygon(float verticesPerPolygon)
```

引擎名：`set_vertices_per_polygon`

## ParsedGeometryType

命名空间 `Godot`；enum

### 枚举值

- `MeshInstances = 0`：Parses mesh instances as geometry.
- `StaticColliders = 1`：Parses [`StaticBody3D`](StaticBody3D.md) colliders as geometry.
- `Both = 2`：Both `MeshInstances` and `StaticColliders`.
- `Max = 3`：Represents the size of the `NavigationMesh.ParsedGeometryType` enum.

## SamplePartitionTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Watershed = 0`：Watershed partitioning.
- `Monotone = 1`：Monotone partitioning.
- `Layers = 2`：Layer partitioning.
- `Max = 3`：Represents the size of the `NavigationMesh.SamplePartitionTypeEnum` enum.

## SourceGeometryMode

命名空间 `Godot`；enum

### 枚举值

- `RootNodeChildren = 0`：Scans the child nodes of the root node recursively for geometry.
- `GroupsWithChildren = 1`：Scans nodes in a group and their child nodes recursively for geometry.
- `GroupsExplicit = 2`：Uses nodes in a group for geometry.
- `Max = 3`：Represents the size of the `NavigationMesh.SourceGeometryMode` enum.
