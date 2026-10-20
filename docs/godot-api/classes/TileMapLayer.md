<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TileMapLayer

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Node for 2D tile-based maps. A `TileMapLayer` uses a [`TileSet`](TileSet.md) which contain a list of tiles which are used to create grid-based maps. Unlike the [`TileMap`](TileMap.md) node, which is deprecated, `TileMapLayer` has only one layer of tiles. You can use several `TileMapLayer` to achieve the same result as a [`TileMap`](TileMap.md) node.

For performance reasons, all TileMap updates are batched at the end of a frame. Notably, this means that scene tiles from a [`TileSetScenesCollectionSource`](TileSetScenesCollectionSource.md) are initialized after their parent. This is only queued when inside the scene tree.

To force an update earlier on, call `UpdateInternals()`.

**Note:** For performance and compatibility reasons, the coordinates serialized by `TileMapLayer` are limited to 16-bit signed integers, i.e. the range for X and Y coordinates is from `-32768` to `32767`. When saving tile data, tiles outside this range are wrapped.

## 构造函数

### TileMapLayer

```csharp
public TileMapLayer()
```

## 属性

### CollisionEnabled

```csharp
public bool CollisionEnabled { get; set; }
```

引擎名：`collision_enabled`

Enable or disable collisions.

### CollisionVisibilityMode

```csharp
public TileMapLayer.DebugVisibilityMode CollisionVisibilityMode { get; set; }
```

引擎名：`collision_visibility_mode`

Show or hide the `TileMapLayer`'s collision shapes. If set to [`TileMapLayer.DebugVisibilityMode.Default`](TileMapLayer.md), this depends on the show collision debug settings.

### Enabled

```csharp
public bool Enabled { get; set; }
```

引擎名：`enabled`

If `false`, disables this `TileMapLayer` completely (rendering, collision, navigation, scene tiles, etc.)

### NavigationEnabled

```csharp
public bool NavigationEnabled { get; set; }
```

引擎名：`navigation_enabled`

If `true`, navigation regions are enabled.

### NavigationVisibilityMode

```csharp
public TileMapLayer.DebugVisibilityMode NavigationVisibilityMode { get; set; }
```

引擎名：`navigation_visibility_mode`

Show or hide the `TileMapLayer`'s navigation meshes. If set to [`TileMapLayer.DebugVisibilityMode.Default`](TileMapLayer.md), this depends on the show navigation debug settings.

### OcclusionEnabled

```csharp
public bool OcclusionEnabled { get; set; }
```

引擎名：`occlusion_enabled`

Enable or disable light occlusion.

### PhysicsQuadrantSize

```csharp
public int PhysicsQuadrantSize { get; set; }
```

引擎名：`physics_quadrant_size`

The `TileMapLayer`'s physics quadrant size. Within a physics quadrant, cells with similar physics properties are grouped together and their collision shapes get merged. `PhysicsQuadrantSize` defines the length of a square's side, in the map's coordinate system, that forms the quadrant. Thus, the default quadrant size groups together `16 * 16 = 256` tiles.

**Note:** As quadrants are created according to the map's coordinate system, the quadrant's "square shape" might not look like square in the `TileMapLayer`'s local coordinate system.

**Note:** This impacts the value returned by `GetCoordsForBodyRid()`. Higher values will make that function less precise. To get the exact cell coordinates, you need to set `PhysicsQuadrantSize` to `1`, which disables physics chunking.

### RenderingQuadrantSize

```csharp
public int RenderingQuadrantSize { get; set; }
```

引擎名：`rendering_quadrant_size`

The `TileMapLayer`'s rendering quadrant size. A quadrant is a group of tiles to be drawn together on a single canvas item, for optimization purposes. `RenderingQuadrantSize` defines the length of a square's side, in the map's coordinate system, that forms the quadrant. Thus, the default quadrant size groups together `16 * 16 = 256` tiles.

The quadrant size does not apply on a Y-sorted `TileMapLayer`, as tiles are grouped by Y position instead in that case.

**Note:** As quadrants are created according to the map's coordinate system, the quadrant's "square shape" might not look like square in the `TileMapLayer`'s local coordinate system.

### TileMapData

```csharp
public byte[] TileMapData { get; set; }
```

引擎名：`tile_map_data`

The raw tile map data as a byte array.

### TileSet

```csharp
public TileSet TileSet { get; set; }
```

引擎名：`tile_set`

The [`TileSet`](TileSet.md) used by this layer. The textures, collisions, and additional behavior of all available tiles are stored here.

### UseKinematicBodies

```csharp
public bool UseKinematicBodies { get; set; }
```

引擎名：`use_kinematic_bodies`

If `true`, this `TileMapLayer` collision shapes will be instantiated as kinematic bodies. This can be needed for moving `TileMapLayer` nodes (i.e. moving platforms).

### XDrawOrderReversed

```csharp
public bool XDrawOrderReversed { get; set; }
```

引擎名：`x_draw_order_reversed`

If [`CanvasItem.YSortEnabled`](CanvasItem.md) is enabled, setting this to `true` will reverse the order the tiles are drawn on the X-axis.

### YSortOrigin

```csharp
public int YSortOrigin { get; set; }
```

引擎名：`y_sort_origin`

This Y-sort origin value is added to each tile's Y-sort origin value. This allows, for example, to fake a different height level. This can be useful for top-down view games.

## 方法

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears all cells.

### EraseCell

```csharp
public void EraseCell(Vector2I coords)
```

引擎名：`erase_cell`

Erases the cell at coordinates `coords`.

### FixInvalidTiles

```csharp
public void FixInvalidTiles()
```

引擎名：`fix_invalid_tiles`

Clears cells containing tiles that do not exist in the `TileSet`.

### GetCellAlternativeTile

```csharp
public int GetCellAlternativeTile(Vector2I coords)
```

引擎名：`get_cell_alternative_tile`

Returns the tile alternative ID of the cell at coordinates `coords`.

### GetCellAtlasCoords

```csharp
public Vector2I GetCellAtlasCoords(Vector2I coords)
```

引擎名：`get_cell_atlas_coords`

Returns the tile atlas coordinates ID of the cell at coordinates `coords`. Returns `Vector2i(-1, -1)` if the cell does not exist.

### GetCellSourceId

```csharp
public int GetCellSourceId(Vector2I coords)
```

引擎名：`get_cell_source_id`

Returns the tile source ID of the cell at coordinates `coords`. Returns `-1` if the cell does not exist.

### GetCellTileData

```csharp
public TileData GetCellTileData(Vector2I coords)
```

引擎名：`get_cell_tile_data`

Returns the [`TileData`](TileData.md) object associated with the given cell, or `null` if the cell does not exist or is not a [`TileSetAtlasSource`](TileSetAtlasSource.md).

```csharp
func get_clicked_tile_power():
var clicked_cell = tile_map_layer.local_to_map(tile_map_layer.get_local_mouse_position())
var data = tile_map_layer.get_cell_tile_data(clicked_cell)
if data:
return data.get_custom_data("power")
else:
return 0

```

### GetCollisionVisibilityMode

```csharp
public TileMapLayer.DebugVisibilityMode GetCollisionVisibilityMode()
```

引擎名：`get_collision_visibility_mode`

### GetCoordsForBodyRid

```csharp
public Vector2I GetCoordsForBodyRid(Rid body)
```

引擎名：`get_coords_for_body_rid`

Returns the coordinates of the physics quadrant (see `PhysicsQuadrantSize`) for given physics body [`Rid`](Rid.md). Such an [`Rid`](Rid.md) can be retrieved from [`KinematicCollision2D.GetColliderRid`](KinematicCollision2D.md), when colliding with a tile.

**Note:** Higher values of `PhysicsQuadrantSize` will make this function less precise. To get the exact cell coordinates, you need to set `PhysicsQuadrantSize` to `1`, which disables physics chunking.

### GetNavigationMap

```csharp
public Rid GetNavigationMap()
```

引擎名：`get_navigation_map`

Returns the [`Rid`](Rid.md) of the [`NavigationServer2D`](NavigationServer2D.md) navigation used by this `TileMapLayer`.

By default this returns the default [`World2D`](World2D.md) navigation map, unless a custom map was provided using `SetNavigationMap()`.

### GetNavigationVisibilityMode

```csharp
public TileMapLayer.DebugVisibilityMode GetNavigationVisibilityMode()
```

引擎名：`get_navigation_visibility_mode`

### GetNeighborCell

```csharp
public Vector2I GetNeighborCell(Vector2I coords, TileSet.CellNeighbor neighbor)
```

引擎名：`get_neighbor_cell`

Returns the neighboring cell to the one at coordinates `coords`, identified by the `neighbor` direction. This method takes into account the different layouts a TileMap can take.

### GetPattern

```csharp
public TileMapPattern GetPattern(Array<Vector2I> coordsArray)
```

引擎名：`get_pattern`

Creates and returns a new [`TileMapPattern`](TileMapPattern.md) from the given array of cells. See also `SetPattern()`.

### GetPhysicsQuadrantSize

```csharp
public int GetPhysicsQuadrantSize()
```

引擎名：`get_physics_quadrant_size`

### GetRenderingQuadrantSize

```csharp
public int GetRenderingQuadrantSize()
```

引擎名：`get_rendering_quadrant_size`

### GetSurroundingCells

```csharp
public Array<Vector2I> GetSurroundingCells(Vector2I coords)
```

引擎名：`get_surrounding_cells`

Returns the list of all neighboring cells to the one at `coords`. Any neighboring cell is one that is touching edges, so for a square cell 4 cells would be returned, for a hexagon 6 cells are returned.

### GetTileMapDataAsArray

```csharp
public byte[] GetTileMapDataAsArray()
```

引擎名：`get_tile_map_data_as_array`

### GetTileSet

```csharp
public TileSet GetTileSet()
```

引擎名：`get_tile_set`

### GetUsedCells

```csharp
public Array<Vector2I> GetUsedCells()
```

引擎名：`get_used_cells`

Returns a [`Vector2I`](Vector2I.md) array with the positions of all cells containing a tile. A cell is considered empty if its source identifier equals `-1`, its atlas coordinate identifier is `Vector2(-1, -1)` and its alternative identifier is `-1`.

### GetUsedCellsById

```csharp
public Array<Vector2I> GetUsedCellsById(int sourceId = -1, Vector2I? atlasCoords = null, int alternativeTile = -1)
```

引擎名：`get_used_cells_by_id`

Returns a [`Vector2I`](Vector2I.md) array with the positions of all cells containing a tile. Tiles may be filtered according to their source (`sourceId`), their atlas coordinates (`atlasCoords`), or alternative id (`alternativeTile`).

If a parameter has its value set to the default one, this parameter is not used to filter a cell. Thus, if all parameters have their respective default values, this method returns the same result as `GetUsedCells()`.

A cell is considered empty if its source identifier equals `-1`, its atlas coordinate identifier is `Vector2(-1, -1)` and its alternative identifier is `-1`.

参数：
- `atlasCoords`：If the parameter is null, then the default value is `new Vector2I(-1, -1)`.

### GetUsedRect

```csharp
public Rect2I GetUsedRect()
```

引擎名：`get_used_rect`

Returns a rectangle enclosing the used (non-empty) tiles of the map.

### GetYSortOrigin

```csharp
public int GetYSortOrigin()
```

引擎名：`get_y_sort_origin`

### HasBodyRid

```csharp
public bool HasBodyRid(Rid body)
```

引擎名：`has_body_rid`

Returns whether the provided `body` [`Rid`](Rid.md) belongs to one of this `TileMapLayer`'s cells.

### IsCellFlippedH

```csharp
public bool IsCellFlippedH(Vector2I coords)
```

引擎名：`is_cell_flipped_h`

Returns `true` if the cell at coordinates `coords` is flipped horizontally. The result is valid only for atlas sources.

### IsCellFlippedV

```csharp
public bool IsCellFlippedV(Vector2I coords)
```

引擎名：`is_cell_flipped_v`

Returns `true` if the cell at coordinates `coords` is flipped vertically. The result is valid only for atlas sources.

### IsCellTransposed

```csharp
public bool IsCellTransposed(Vector2I coords)
```

引擎名：`is_cell_transposed`

Returns `true` if the cell at coordinates `coords` is transposed. The result is valid only for atlas sources.

### IsCollisionEnabled

```csharp
public bool IsCollisionEnabled()
```

引擎名：`is_collision_enabled`

### IsEnabled

```csharp
public bool IsEnabled()
```

引擎名：`is_enabled`

### IsNavigationEnabled

```csharp
public bool IsNavigationEnabled()
```

引擎名：`is_navigation_enabled`

### IsOcclusionEnabled

```csharp
public bool IsOcclusionEnabled()
```

引擎名：`is_occlusion_enabled`

### IsUsingKinematicBodies

```csharp
public bool IsUsingKinematicBodies()
```

引擎名：`is_using_kinematic_bodies`

### IsXDrawOrderReversed

```csharp
public bool IsXDrawOrderReversed()
```

引擎名：`is_x_draw_order_reversed`

### LocalToMap

```csharp
public Vector2I LocalToMap(Vector2 localPosition)
```

引擎名：`local_to_map`

Returns the map coordinates of the cell containing the given `localPosition`. If `localPosition` is in global coordinates, consider using [`Node2D.ToLocal`](Node2D.md) before passing it to this method. See also `MapToLocal()`.

### MapPattern

```csharp
public Vector2I MapPattern(Vector2I positionInTilemap, Vector2I coordsInPattern, TileMapPattern pattern)
```

引擎名：`map_pattern`

Returns for the given coordinates `coordsInPattern` in a [`TileMapPattern`](TileMapPattern.md) the corresponding cell coordinates if the pattern was pasted at the `positionInTilemap` coordinates (see `SetPattern()`). This mapping is required as in half-offset tile shapes, the mapping might not work by calculating `position_in_tile_map + coords_in_pattern`.

### MapToLocal

```csharp
public Vector2 MapToLocal(Vector2I mapPosition)
```

引擎名：`map_to_local`

Returns the centered position of a cell in the `TileMapLayer`'s local coordinate space. To convert the returned value into global coordinates, use [`Node2D.ToGlobal`](Node2D.md). See also `LocalToMap()`.

**Note:** This may not correspond to the visual position of the tile, i.e. it ignores the [`TileData.TextureOrigin`](TileData.md) property of individual tiles.

### NotifyRuntimeTileDataUpdate

```csharp
public void NotifyRuntimeTileDataUpdate()
```

引擎名：`notify_runtime_tile_data_update`

Notifies the `TileMapLayer` node that calls to `_UseTileDataRuntimeUpdate()` or `_TileDataRuntimeUpdate()` will lead to different results. This will thus trigger a `TileMapLayer` update.

**Warning:** Updating the `TileMapLayer` is computationally expensive and may impact performance. Try to limit the number of calls to this function to avoid unnecessary update.

**Note:** This does not trigger a direct update of the `TileMapLayer`, the update will be done at the end of the frame as usual (unless you call `UpdateInternals()`).

### SetCell

```csharp
public void SetCell(Vector2I coords, int sourceId = -1, Vector2I? atlasCoords = null, int alternativeTile = 0)
```

引擎名：`set_cell`

Sets the tile identifiers for the cell at coordinates `coords`. Each tile of the [`TileSet`](TileSet.md) is identified using three parts:

- The source identifier `sourceId` identifies a [`TileSetSource`](TileSetSource.md) identifier. See [`TileSet.SetSourceId`](TileSet.md),

- The atlas coordinate identifier `atlasCoords` identifies a tile coordinates in the atlas (if the source is a [`TileSetAtlasSource`](TileSetAtlasSource.md)). For [`TileSetScenesCollectionSource`](TileSetScenesCollectionSource.md) it should always be `Vector2i(0, 0)`,

- The alternative tile identifier `alternativeTile` identifies a tile alternative in the atlas (if the source is a [`TileSetAtlasSource`](TileSetAtlasSource.md)), and the scene for a [`TileSetScenesCollectionSource`](TileSetScenesCollectionSource.md).

If `sourceId` is set to `-1`, `atlasCoords` to `Vector2i(-1, -1)`, or `alternativeTile` to `-1`, the cell will be erased. An erased cell gets **all** its identifiers automatically set to their respective invalid values, namely `-1`, `Vector2i(-1, -1)` and `-1`.

参数：
- `atlasCoords`：If the parameter is null, then the default value is `new Vector2I(-1, -1)`.

### SetCellsTerrainConnect

```csharp
public void SetCellsTerrainConnect(Array<Vector2I> cells, int terrainSet, int terrain, bool ignoreEmptyTerrains = true)
```

引擎名：`set_cells_terrain_connect`

Update all the cells in the `cells` coordinates array so that they use the given `terrain` for the given `terrainSet`. If an updated cell has the same terrain as one of its neighboring cells, this function tries to join the two. This function might update neighboring tiles if needed to create correct terrain transitions.

If `ignoreEmptyTerrains` is `true`, empty terrains will be ignored when trying to find the best fitting tile for the given terrain constraints.

**Note:** To work correctly, this method requires the `TileMapLayer`'s TileSet to have terrains set up with all required terrain combinations. Otherwise, it may produce unexpected results.

### SetCellsTerrainPath

```csharp
public void SetCellsTerrainPath(Array<Vector2I> path, int terrainSet, int terrain, bool ignoreEmptyTerrains = true)
```

引擎名：`set_cells_terrain_path`

Update all the cells in the `path` coordinates array so that they use the given `terrain` for the given `terrainSet`. The function will also connect two successive cell in the path with the same terrain. This function might update neighboring tiles if needed to create correct terrain transitions.

If `ignoreEmptyTerrains` is `true`, empty terrains will be ignored when trying to find the best fitting tile for the given terrain constraints.

**Note:** To work correctly, this method requires the `TileMapLayer`'s TileSet to have terrains set up with all required terrain combinations. Otherwise, it may produce unexpected results.

### SetCollisionEnabled

```csharp
public void SetCollisionEnabled(bool enabled)
```

引擎名：`set_collision_enabled`

### SetCollisionVisibilityMode

```csharp
public void SetCollisionVisibilityMode(TileMapLayer.DebugVisibilityMode visibilityMode)
```

引擎名：`set_collision_visibility_mode`

### SetEnabled

```csharp
public void SetEnabled(bool enabled)
```

引擎名：`set_enabled`

### SetNavigationEnabled

```csharp
public void SetNavigationEnabled(bool enabled)
```

引擎名：`set_navigation_enabled`

### SetNavigationMap

```csharp
public void SetNavigationMap(Rid map)
```

引擎名：`set_navigation_map`

Sets a custom `map` as a [`NavigationServer2D`](NavigationServer2D.md) navigation map. If not set, uses the default [`World2D`](World2D.md) navigation map instead.

### SetNavigationVisibilityMode

```csharp
public void SetNavigationVisibilityMode(TileMapLayer.DebugVisibilityMode showNavigation)
```

引擎名：`set_navigation_visibility_mode`

### SetOcclusionEnabled

```csharp
public void SetOcclusionEnabled(bool enabled)
```

引擎名：`set_occlusion_enabled`

### SetPattern

```csharp
public void SetPattern(Vector2I position, TileMapPattern pattern)
```

引擎名：`set_pattern`

Pastes the [`TileMapPattern`](TileMapPattern.md) at the given `position` in the tile map. See also `GetPattern()`.

### SetPhysicsQuadrantSize

```csharp
public void SetPhysicsQuadrantSize(int size)
```

引擎名：`set_physics_quadrant_size`

### SetRenderingQuadrantSize

```csharp
public void SetRenderingQuadrantSize(int size)
```

引擎名：`set_rendering_quadrant_size`

### SetTileMapDataFromArray

```csharp
public void SetTileMapDataFromArray(ReadOnlySpan<byte> tileMapLayerData)
```

引擎名：`set_tile_map_data_from_array`

### SetTileMapDataFromArray

```csharp
public void SetTileMapDataFromArray(byte[] tileMapLayerData)
```

引擎名：`set_tile_map_data_from_array`

### SetTileSet

```csharp
public void SetTileSet(TileSet tileSet)
```

引擎名：`set_tile_set`

### SetUseKinematicBodies

```csharp
public void SetUseKinematicBodies(bool useKinematicBodies)
```

引擎名：`set_use_kinematic_bodies`

### SetXDrawOrderReversed

```csharp
public void SetXDrawOrderReversed(bool xDrawOrderReversed)
```

引擎名：`set_x_draw_order_reversed`

### SetYSortOrigin

```csharp
public void SetYSortOrigin(int ySortOrigin)
```

引擎名：`set_y_sort_origin`

### UpdateInternals

```csharp
public void UpdateInternals()
```

引擎名：`update_internals`

Triggers a direct update of the `TileMapLayer`. Usually, calling this function is not needed, as `TileMapLayer` node updates automatically when one of its properties or cells is modified.

However, for performance reasons, those updates are batched and delayed to the end of the frame. Calling this function will force the `TileMapLayer` to update right away instead.

**Warning:** Updating the `TileMapLayer` is computationally expensive and may impact performance. Try to limit the number of updates and how many tiles they impact.

### _TileDataRuntimeUpdate

```csharp
public virtual void _TileDataRuntimeUpdate(Vector2I coords, TileData tileData)
```

引擎名：`_tile_data_runtime_update`

Called with a [`TileData`](TileData.md) object about to be used internally by the `TileMapLayer`, allowing its modification at runtime.

This method is only called if `_UseTileDataRuntimeUpdate()` is implemented and returns `true` for the given tile `coords`.

**Warning:** The `tileData` object's sub-resources are the same as the one in the TileSet. Modifying them might impact the whole TileSet. Instead, make sure to duplicate those resources.

**Note:** If the properties of `tileData` object should change over time, use `NotifyRuntimeTileDataUpdate()` to notify the `TileMapLayer` it needs an update.

### _UpdateCells

```csharp
public virtual void _UpdateCells(Array<Vector2I> coords, bool forcedCleanup)
```

引擎名：`_update_cells`

Called when this `TileMapLayer`'s cells need an internal update. This update may be caused from individual cells being modified or by a change in the `TileSet` (causing all cells to be queued for an update). The first call to this function is always for initializing all the `TileMapLayer`'s cells. `coords` contains the coordinates of all modified cells, roughly in the order they were modified. `forcedCleanup` is `true` when the `TileMapLayer`'s internals should be fully cleaned up. This is the case when:

- The layer is disabled;

- The layer is not visible;

- `TileSet` is set to `null`;

- The node is removed from the tree;

- The node is freed.

Note that any internal update happening while one of these conditions is verified is considered to be a "cleanup". See also `UpdateInternals()`.

**Warning:** Implementing this method may degrade the `TileMapLayer`'s performance.

### _UseTileDataRuntimeUpdate

```csharp
public virtual bool _UseTileDataRuntimeUpdate(Vector2I coords)
```

引擎名：`_use_tile_data_runtime_update`

Should return `true` if the tile at coordinates `coords` requires a runtime update.

**Warning:** Make sure this function only returns `true` when needed. Any tile processed at runtime without a need for it will imply a significant performance penalty.

**Note:** If the result of this function should change, use `NotifyRuntimeTileDataUpdate()` to notify the `TileMapLayer` it needs an update.

## 事件（信号）

### Changed

```csharp
public event Action Changed
```

引擎名：`changed`

Emitted when this `TileMapLayer`'s properties changes. This includes modified cells, properties, or changes made to its assigned [`TileSet`](TileSet.md).

**Note:** This signal may be emitted very often when batch-modifying a `TileMapLayer`. Avoid executing complex processing in a connected function, and consider delaying it to the end of the frame instead (i.e. calling [`GodotObject.CallDeferred`](GodotObject.md)).

## DebugVisibilityMode

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：Hide the collisions or navigation debug shapes in the editor, and use the debug settings to determine their visibility in game (i.e.
- `ForceHide = 2`：Always hide the collisions or navigation debug shapes.
- `ForceShow = 1`：Always show the collisions or navigation debug shapes.
