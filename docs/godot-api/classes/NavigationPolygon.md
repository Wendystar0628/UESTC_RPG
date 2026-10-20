<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationPolygon

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A navigation mesh can be created either by baking it with the help of the [`NavigationServer2D`](NavigationServer2D.md), or by adding vertices and convex polygon indices arrays manually.

To bake a navigation mesh at least one outline needs to be added that defines the outer bounds of the baked area.

```csharp
var newNavigationMesh = new NavigationPolygon();
Vector2[] boundingOutline = [new Vector2(0, 0), new Vector2(0, 50), new Vector2(50, 50), new Vector2(50, 0)];
newNavigationMesh.AddOutline(boundingOutline);
NavigationServer2D.BakeFromSourceGeometryData(newNavigationMesh, new NavigationMeshSourceGeometryData2D());
GetNode<NavigationRegion2D>("NavigationRegion2D").NavigationPolygon = newNavigationMesh;

```

Adding vertices and polygon indices manually.

```csharp
var newNavigationMesh = new NavigationPolygon();
Vector2[] newVertices = [new Vector2(0, 0), new Vector2(0, 50), new Vector2(50, 50), new Vector2(50, 0)];
newNavigationMesh.Vertices = newVertices;
int[] newPolygonIndices = [0, 1, 2, 3];
newNavigationMesh.AddPolygon(newPolygonIndices);
GetNode<NavigationRegion2D>("NavigationRegion2D").NavigationPolygon = newNavigationMesh;

```

## 构造函数

### NavigationPolygon

```csharp
public NavigationPolygon()
```

## 属性

### AgentRadius

```csharp
public float AgentRadius { get; set; }
```

引擎名：`agent_radius`

The distance to erode/shrink the walkable surface when baking the navigation mesh.

**Note:** The radius must be equal or higher than `0.0`. If the radius is `0.0`, it won't be possible to fix invalid outline overlaps and other precision errors during the baking process. As a result, some obstacles may be excluded incorrectly from the final navigation mesh, or may delete the navigation mesh's polygons.

### BakingRect

```csharp
public Rect2 BakingRect { get; set; }
```

引擎名：`baking_rect`

If the baking [`Rect2`](Rect2.md) has an area the navigation mesh baking will be restricted to its enclosing area.

### BakingRectOffset

```csharp
public Vector2 BakingRectOffset { get; set; }
```

引擎名：`baking_rect_offset`

The position offset applied to the `BakingRect` [`Rect2`](Rect2.md).

### BorderSize

```csharp
public float BorderSize { get; set; }
```

引擎名：`border_size`

The size of the non-navigable border around the bake bounding area defined by the `BakingRect` [`Rect2`](Rect2.md).

In conjunction with the `BakingRect` the border size can be used to bake tile aligned navigation meshes without the tile edges being shrunk by `AgentRadius`.

### CellSize

```csharp
public float CellSize { get; set; }
```

引擎名：`cell_size`

The cell size used to rasterize the navigation mesh vertices. Must match with the cell size on the navigation map.

### Outlines

```csharp
public Array<Vector2[]> Outlines { get; set; }
```

引擎名：`outlines`

### ParsedCollisionMask

```csharp
public uint ParsedCollisionMask { get; set; }
```

引擎名：`parsed_collision_mask`

The physics layers to scan for static colliders.

Only used when `ParsedGeometryType` is [`NavigationPolygon.ParsedGeometryTypeEnum.StaticColliders`](NavigationPolygon.md) or [`NavigationPolygon.ParsedGeometryTypeEnum.Both`](NavigationPolygon.md).

### ParsedGeometryType

```csharp
public NavigationPolygon.ParsedGeometryTypeEnum ParsedGeometryType { get; set; }
```

引擎名：`parsed_geometry_type`

Determines which type of nodes will be parsed as geometry.

### Polygons

```csharp
public Array<int[]> Polygons { get; set; }
```

引擎名：`polygons`

### SamplePartitionType

```csharp
public NavigationPolygon.SamplePartitionTypeEnum SamplePartitionType { get; set; }
```

引擎名：`sample_partition_type`

Partitioning algorithm for creating the navigation mesh polys.

### SourceGeometryGroupName

```csharp
public StringName SourceGeometryGroupName { get; set; }
```

引擎名：`source_geometry_group_name`

The group name of nodes that should be parsed for baking source geometry.

Only used when `SourceGeometryMode` is [`NavigationPolygon.SourceGeometryModeEnum.GroupsWithChildren`](NavigationPolygon.md) or [`NavigationPolygon.SourceGeometryModeEnum.GroupsExplicit`](NavigationPolygon.md).

### SourceGeometryMode

```csharp
public NavigationPolygon.SourceGeometryModeEnum SourceGeometryMode { get; set; }
```

引擎名：`source_geometry_mode`

The source of the geometry used when baking.

### Vertices

```csharp
public Vector2[] Vertices { get; set; }
```

引擎名：`vertices`

## 方法

### AddOutline

```csharp
public void AddOutline(ReadOnlySpan<Vector2> outline)
```

引擎名：`add_outline`

Appends a [`Vector2`](Vector2.md)[] that contains the vertices of an outline to the internal array that contains all the outlines.

### AddOutline

```csharp
public void AddOutline(Vector2[] outline)
```

引擎名：`add_outline`

Appends a [`Vector2`](Vector2.md)[] that contains the vertices of an outline to the internal array that contains all the outlines.

### AddOutlineAtIndex

```csharp
public void AddOutlineAtIndex(ReadOnlySpan<Vector2> outline, int index)
```

引擎名：`add_outline_at_index`

Adds a [`Vector2`](Vector2.md)[] that contains the vertices of an outline to the internal array that contains all the outlines at a fixed position.

### AddOutlineAtIndex

```csharp
public void AddOutlineAtIndex(Vector2[] outline, int index)
```

引擎名：`add_outline_at_index`

Adds a [`Vector2`](Vector2.md)[] that contains the vertices of an outline to the internal array that contains all the outlines at a fixed position.

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

### ClearOutlines

```csharp
public void ClearOutlines()
```

引擎名：`clear_outlines`

Clears the array of the outlines, but it doesn't clear the vertices and the polygons that were created by them.

### ClearPolygons

```csharp
public void ClearPolygons()
```

引擎名：`clear_polygons`

Clears the array of polygons, but it doesn't clear the array of outlines and vertices.

### GetAgentRadius

```csharp
public float GetAgentRadius()
```

引擎名：`get_agent_radius`

### GetBakingRect

```csharp
public Rect2 GetBakingRect()
```

引擎名：`get_baking_rect`

### GetBakingRectOffset

```csharp
public Vector2 GetBakingRectOffset()
```

引擎名：`get_baking_rect_offset`

### GetBorderSize

```csharp
public float GetBorderSize()
```

引擎名：`get_border_size`

### GetCellSize

```csharp
public float GetCellSize()
```

引擎名：`get_cell_size`

### GetNavigationMesh

```csharp
public NavigationMesh GetNavigationMesh()
```

引擎名：`get_navigation_mesh`

Returns the [`NavigationMesh`](NavigationMesh.md) resulting from this navigation polygon. This navigation mesh can be used to update the navigation mesh of a region with the [`NavigationServer3D.RegionSetNavigationMesh`](NavigationServer3D.md) API directly.

### GetOutline

```csharp
public Vector2[] GetOutline(int idx)
```

引擎名：`get_outline`

Returns a [`Vector2`](Vector2.md)[] containing the vertices of an outline that was created in the editor or by script.

### GetOutlineCount

```csharp
public int GetOutlineCount()
```

引擎名：`get_outline_count`

Returns the number of outlines that were created in the editor or by script.

### GetParsedCollisionMask

```csharp
public uint GetParsedCollisionMask()
```

引擎名：`get_parsed_collision_mask`

### GetParsedCollisionMaskValue

```csharp
public bool GetParsedCollisionMaskValue(int layerNumber)
```

引擎名：`get_parsed_collision_mask_value`

Returns whether or not the specified layer of the `ParsedCollisionMask` is enabled, given a `layerNumber` between 1 and 32.

### GetParsedGeometryType

```csharp
public NavigationPolygon.ParsedGeometryTypeEnum GetParsedGeometryType()
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

Returns the count of all polygons.

### GetSamplePartitionType

```csharp
public NavigationPolygon.SamplePartitionTypeEnum GetSamplePartitionType()
```

引擎名：`get_sample_partition_type`

### GetSourceGeometryGroupName

```csharp
public StringName GetSourceGeometryGroupName()
```

引擎名：`get_source_geometry_group_name`

### GetSourceGeometryMode

```csharp
public NavigationPolygon.SourceGeometryModeEnum GetSourceGeometryMode()
```

引擎名：`get_source_geometry_mode`

### GetVertices

```csharp
public Vector2[] GetVertices()
```

引擎名：`get_vertices`

Returns a [`Vector2`](Vector2.md)[] containing all the vertices being used to create the polygons.

### MakePolygonsFromOutlines

```csharp
public void MakePolygonsFromOutlines()
```

引擎名：`make_polygons_from_outlines`

Creates polygons from the outlines added in the editor or by script.

### RemoveOutline

```csharp
public void RemoveOutline(int idx)
```

引擎名：`remove_outline`

Removes an outline created in the editor or by script. You have to call `MakePolygonsFromOutlines()` for the polygons to update.

### SetAgentRadius

```csharp
public void SetAgentRadius(float agentRadius)
```

引擎名：`set_agent_radius`

### SetBakingRect

```csharp
public void SetBakingRect(Rect2 rect)
```

引擎名：`set_baking_rect`

### SetBakingRectOffset

```csharp
public void SetBakingRectOffset(Vector2 rectOffset)
```

引擎名：`set_baking_rect_offset`

### SetBorderSize

```csharp
public void SetBorderSize(float borderSize)
```

引擎名：`set_border_size`

### SetCellSize

```csharp
public void SetCellSize(float cellSize)
```

引擎名：`set_cell_size`

### SetOutline

```csharp
public void SetOutline(int idx, ReadOnlySpan<Vector2> outline)
```

引擎名：`set_outline`

Changes an outline created in the editor or by script. You have to call `MakePolygonsFromOutlines()` for the polygons to update.

### SetOutline

```csharp
public void SetOutline(int idx, Vector2[] outline)
```

引擎名：`set_outline`

Changes an outline created in the editor or by script. You have to call `MakePolygonsFromOutlines()` for the polygons to update.

### SetParsedCollisionMask

```csharp
public void SetParsedCollisionMask(uint mask)
```

引擎名：`set_parsed_collision_mask`

### SetParsedCollisionMaskValue

```csharp
public void SetParsedCollisionMaskValue(int layerNumber, bool value)
```

引擎名：`set_parsed_collision_mask_value`

Based on `value`, enables or disables the specified layer in the `ParsedCollisionMask`, given a `layerNumber` between 1 and 32.

### SetParsedGeometryType

```csharp
public void SetParsedGeometryType(NavigationPolygon.ParsedGeometryTypeEnum geometryType)
```

引擎名：`set_parsed_geometry_type`

### SetSamplePartitionType

```csharp
public void SetSamplePartitionType(NavigationPolygon.SamplePartitionTypeEnum samplePartitionType)
```

引擎名：`set_sample_partition_type`

### SetSourceGeometryGroupName

```csharp
public void SetSourceGeometryGroupName(StringName groupName)
```

引擎名：`set_source_geometry_group_name`

### SetSourceGeometryMode

```csharp
public void SetSourceGeometryMode(NavigationPolygon.SourceGeometryModeEnum geometryMode)
```

引擎名：`set_source_geometry_mode`

### SetVertices

```csharp
public void SetVertices(ReadOnlySpan<Vector2> vertices)
```

引擎名：`set_vertices`

Sets the vertices that can be then indexed to create polygons with the `AddPolygon()` method.

### SetVertices

```csharp
public void SetVertices(Vector2[] vertices)
```

引擎名：`set_vertices`

Sets the vertices that can be then indexed to create polygons with the `AddPolygon()` method.

## ParsedGeometryTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `MeshInstances = 0`：Parses mesh instances as obstruction geometry.
- `StaticColliders = 1`：Parses [`StaticBody2D`](StaticBody2D.md) and [`TileMap`](TileMap.md) colliders as obstruction geometry.
- `Both = 2`：Both `MeshInstances` and `StaticColliders`.
- `Max = 3`：Represents the size of the `NavigationPolygon.ParsedGeometryTypeEnum` enum.

## SamplePartitionTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `ConvexPartition = 0`：Convex partitioning that results in a navigation mesh with convex polygons.
- `Triangulate = 1`：Triangulation partitioning that results in a navigation mesh with triangle polygons.
- `Max = 2`：Represents the size of the `NavigationPolygon.SamplePartitionTypeEnum` enum.

## SourceGeometryModeEnum

命名空间 `Godot`；enum

### 枚举值

- `RootNodeChildren = 0`：Scans the child nodes of the root node recursively for geometry.
- `GroupsWithChildren = 1`：Scans nodes in a group and their child nodes recursively for geometry.
- `GroupsExplicit = 2`：Uses nodes in a group for geometry.
- `Max = 3`：Represents the size of the `NavigationPolygon.SourceGeometryModeEnum` enum.
