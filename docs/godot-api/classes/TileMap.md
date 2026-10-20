<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TileMap

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Node for 2D tile-based maps. Tilemaps use a [`TileSet`](TileSet.md) which contain a list of tiles which are used to create grid-based maps. A TileMap may have several layers, layouting tiles on top of each other.

For performance reasons, all TileMap updates are batched at the end of a frame. Notably, this means that scene tiles from a [`TileSetScenesCollectionSource`](TileSetScenesCollectionSource.md) may be initialized after their parent. This is only queued when inside the scene tree.

To force an update earlier on, call `UpdateInternals()`.

**Note:** For performance and compatibility reasons, the coordinates serialized by `TileMap` are limited to 16-bit signed integers, i.e. the range for X and Y coordinates is from `-32768` to `32767`. When saving tile data, tiles outside this range are wrapped.

## 构造函数

### TileMap

```csharp
public TileMap()
```

## 属性

### CellQuadrantSize

```csharp
public int CellQuadrantSize { get; set; }
```

The TileMap's quadrant size. Optimizes drawing by batching, using chunks of this size.

### CollisionAnimatable

```csharp
public bool CollisionAnimatable { get; set; }
```

引擎名：`collision_animatable`

If enabled, the TileMap will see its collisions synced to the physics tick and change its collision type from static to kinematic. This is required to create TileMap-based moving platform.

**Note:** Enabling `CollisionAnimatable` may have a small performance impact, only do it if the TileMap is moving and has colliding tiles.

### CollisionVisibilityMode

```csharp
public TileMap.VisibilityMode CollisionVisibilityMode { get; set; }
```

引擎名：`collision_visibility_mode`

Show or hide the TileMap's collision shapes. If set to [`TileMap.VisibilityMode.Default`](TileMap.md), this depends on the show collision debug settings.

### NavigationVisibilityMode

```csharp
public TileMap.VisibilityMode NavigationVisibilityMode { get; set; }
```

引擎名：`navigation_visibility_mode`

Show or hide the TileMap's navigation meshes. If set to [`TileMap.VisibilityMode.Default`](TileMap.md), this depends on the show navigation debug settings.

### RenderingQuadrantSize

```csharp
public int RenderingQuadrantSize { get; set; }
```

引擎名：`rendering_quadrant_size`

The TileMap's quadrant size. A quadrant is a group of tiles to be drawn together on a single canvas item, for optimization purposes. `RenderingQuadrantSize` defines the length of a square's side, in the map's coordinate system, that forms the quadrant. Thus, the default quadrant size groups together `16 * 16 = 256` tiles.

The quadrant size does not apply on Y-sorted layers, as tiles are grouped by Y position instead in that case.

**Note:** As quadrants are created according to the map's coordinate system, the quadrant's "square shape" might not look like square in the TileMap's local coordinate system.

### TileSet

```csharp
public TileSet TileSet { get; set; }
```

引擎名：`tile_set`

The [`TileSet`](TileSet.md) used by this `TileMap`. The textures, collisions, and additional behavior of all available tiles are stored here.

## 方法

### AddLayer

```csharp
public void AddLayer(int toPosition)
```

引擎名：`add_layer`

Adds a layer at the given position `toPosition` in the array. If `toPosition` is negative, the position is counted from the end, with `-1` adding the layer at the end of the array.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears all cells.

### ClearLayer

```csharp
public void ClearLayer(int layer)
```

引擎名：`clear_layer`

Clears all cells on the given layer.

If `layer` is negative, the layers are accessed from the last one.

### EraseCell

```csharp
public void EraseCell(int layer, Vector2I coords)
```

引擎名：`erase_cell`

Erases the cell on layer `layer` at coordinates `coords`.

If `layer` is negative, the layers are accessed from the last one.

### FixInvalidTiles

```csharp
public void FixInvalidTiles()
```

引擎名：`fix_invalid_tiles`

Clears cells that do not exist in the tileset.

### ForceUpdate

```csharp
public void ForceUpdate(int layer = -1)
```

引擎名：`force_update`

Forces the TileMap and the layer `layer` to update.

### GetCellAlternativeTile

```csharp
public int GetCellAlternativeTile(int layer, Vector2I coords, bool useProxies = false)
```

引擎名：`get_cell_alternative_tile`

Returns the tile alternative ID of the cell on layer `layer` at `coords`.

If `useProxies` is `false`, ignores the [`TileSet`](TileSet.md)'s tile proxies, returning the raw alternative identifier. See [`TileSet.MapTileProxy`](TileSet.md).

If `layer` is negative, the layers are accessed from the last one.

### GetCellAtlasCoords

```csharp
public Vector2I GetCellAtlasCoords(int layer, Vector2I coords, bool useProxies = false)
```

引擎名：`get_cell_atlas_coords`

Returns the tile atlas coordinates ID of the cell on layer `layer` at coordinates `coords`. Returns `Vector2i(-1, -1)` if the cell does not exist.

If `useProxies` is `false`, ignores the [`TileSet`](TileSet.md)'s tile proxies, returning the raw atlas coordinate identifier. See [`TileSet.MapTileProxy`](TileSet.md).

If `layer` is negative, the layers are accessed from the last one.

### GetCellSourceId

```csharp
public int GetCellSourceId(int layer, Vector2I coords, bool useProxies = false)
```

引擎名：`get_cell_source_id`

Returns the tile source ID of the cell on layer `layer` at coordinates `coords`. Returns `-1` if the cell does not exist.

If `useProxies` is `false`, ignores the [`TileSet`](TileSet.md)'s tile proxies, returning the raw source identifier. See [`TileSet.MapTileProxy`](TileSet.md).

If `layer` is negative, the layers are accessed from the last one.

### GetCellTileData

```csharp
public TileData GetCellTileData(int layer, Vector2I coords, bool useProxies = false)
```

引擎名：`get_cell_tile_data`

Returns the [`TileData`](TileData.md) object associated with the given cell, or `null` if the cell does not exist or is not a [`TileSetAtlasSource`](TileSetAtlasSource.md).

If `layer` is negative, the layers are accessed from the last one.

```csharp
func get_clicked_tile_power():
var clicked_cell = tile_map.local_to_map(tile_map.get_local_mouse_position())
var data = tile_map.get_cell_tile_data(0, clicked_cell)
if data:
return data.get_custom_data("power")
else:
return 0

```

If `useProxies` is `false`, ignores the [`TileSet`](TileSet.md)'s tile proxies. See [`TileSet.MapTileProxy`](TileSet.md).

### GetCollisionVisibilityMode

```csharp
public TileMap.VisibilityMode GetCollisionVisibilityMode()
```

引擎名：`get_collision_visibility_mode`

### GetCoordsForBodyRid

```csharp
public Vector2I GetCoordsForBodyRid(Rid body)
```

引擎名：`get_coords_for_body_rid`

Returns the coordinates of the tile for given physics body RID. Such RID can be retrieved from [`KinematicCollision2D.GetColliderRid`](KinematicCollision2D.md), when colliding with a tile.

### GetLayerForBodyRid

```csharp
public int GetLayerForBodyRid(Rid body)
```

引擎名：`get_layer_for_body_rid`

Returns the tilemap layer of the tile for given physics body RID. Such RID can be retrieved from [`KinematicCollision2D.GetColliderRid`](KinematicCollision2D.md), when colliding with a tile.

### GetLayerModulate

```csharp
public Color GetLayerModulate(int layer)
```

引擎名：`get_layer_modulate`

Returns a TileMap layer's modulate.

If `layer` is negative, the layers are accessed from the last one.

### GetLayerName

```csharp
public string GetLayerName(int layer)
```

引擎名：`get_layer_name`

Returns a TileMap layer's name.

If `layer` is negative, the layers are accessed from the last one.

### GetLayerNavigationMap

```csharp
public Rid GetLayerNavigationMap(int layer)
```

引擎名：`get_layer_navigation_map`

Returns the [`Rid`](Rid.md) of the [`NavigationServer2D`](NavigationServer2D.md) navigation map assigned to the specified TileMap layer `layer`.

By default the TileMap uses the default [`World2D`](World2D.md) navigation map for the first TileMap layer. For each additional TileMap layer a new navigation map is created for the additional layer.

In order to make [`NavigationAgent2D`](NavigationAgent2D.md) switch between TileMap layer navigation maps use [`NavigationAgent2D.SetNavigationMap`](NavigationAgent2D.md) with the navigation map received from `GetLayerNavigationMap()`.

If `layer` is negative, the layers are accessed from the last one.

### GetLayerYSortOrigin

```csharp
public int GetLayerYSortOrigin(int layer)
```

引擎名：`get_layer_y_sort_origin`

Returns a TileMap layer's Y sort origin.

If `layer` is negative, the layers are accessed from the last one.

### GetLayerZIndex

```csharp
public int GetLayerZIndex(int layer)
```

引擎名：`get_layer_z_index`

Returns a TileMap layer's Z-index value.

If `layer` is negative, the layers are accessed from the last one.

### GetLayersCount

```csharp
public int GetLayersCount()
```

引擎名：`get_layers_count`

Returns the number of layers in the TileMap.

### GetNavigationMap

```csharp
public Rid GetNavigationMap(int layer)
```

引擎名：`get_navigation_map`

Returns the [`Rid`](Rid.md) of the [`NavigationServer2D`](NavigationServer2D.md) navigation map assigned to the specified TileMap layer `layer`.

### GetNavigationVisibilityMode

```csharp
public TileMap.VisibilityMode GetNavigationVisibilityMode()
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
public TileMapPattern GetPattern(int layer, Array<Vector2I> coordsArray)
```

引擎名：`get_pattern`

Creates a new [`TileMapPattern`](TileMapPattern.md) from the given layer and set of cells.

If `layer` is negative, the layers are accessed from the last one.

### GetQuadrantSize

```csharp
public int GetQuadrantSize()
```

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

Returns the list of all neighbourings cells to the one at `coords`.

### GetTileset

```csharp
public TileSet GetTileset()
```

引擎名：`get_tileset`

### GetUsedCells

```csharp
public Array<Vector2I> GetUsedCells(int layer)
```

引擎名：`get_used_cells`

Returns a [`Vector2I`](Vector2I.md) array with the positions of all cells containing a tile in the given layer. A cell is considered empty if its source identifier equals -1, its atlas coordinates identifiers is `Vector2(-1, -1)` and its alternative identifier is -1.

If `layer` is negative, the layers are accessed from the last one.

### GetUsedCellsById

```csharp
public Array<Vector2I> GetUsedCellsById(int layer, int sourceId = -1, Vector2I? atlasCoords = null, int alternativeTile = -1)
```

引擎名：`get_used_cells_by_id`

Returns a [`Vector2I`](Vector2I.md) array with the positions of all cells containing a tile in the given layer. Tiles may be filtered according to their source (`sourceId`), their atlas coordinates (`atlasCoords`) or alternative id (`alternativeTile`).

If a parameter has its value set to the default one, this parameter is not used to filter a cell. Thus, if all parameters have their respective default value, this method returns the same result as `GetUsedCells()`.

A cell is considered empty if its source identifier equals -1, its atlas coordinates identifiers is `Vector2(-1, -1)` and its alternative identifier is -1.

If `layer` is negative, the layers are accessed from the last one.

参数：
- `atlasCoords`：If the parameter is null, then the default value is `new Vector2I(-1, -1)`.

### GetUsedRect

```csharp
public Rect2I GetUsedRect()
```

引擎名：`get_used_rect`

Returns a rectangle enclosing the used (non-empty) tiles of the map, including all layers.

### IsCellFlippedH

```csharp
public bool IsCellFlippedH(int layer, Vector2I coords, bool useProxies = false)
```

引擎名：`is_cell_flipped_h`

Returns `true` if the cell on layer `layer` at coordinates `coords` is flipped horizontally. The result is valid only for atlas sources.

### IsCellFlippedV

```csharp
public bool IsCellFlippedV(int layer, Vector2I coords, bool useProxies = false)
```

引擎名：`is_cell_flipped_v`

Returns `true` if the cell on layer `layer` at coordinates `coords` is flipped vertically. The result is valid only for atlas sources.

### IsCellTransposed

```csharp
public bool IsCellTransposed(int layer, Vector2I coords, bool useProxies = false)
```

引擎名：`is_cell_transposed`

Returns `true` if the cell on layer `layer` at coordinates `coords` is transposed. The result is valid only for atlas sources.

### IsCollisionAnimatable

```csharp
public bool IsCollisionAnimatable()
```

引擎名：`is_collision_animatable`

### IsLayerEnabled

```csharp
public bool IsLayerEnabled(int layer)
```

引擎名：`is_layer_enabled`

Returns if a layer is enabled.

If `layer` is negative, the layers are accessed from the last one.

### IsLayerNavigationEnabled

```csharp
public bool IsLayerNavigationEnabled(int layer)
```

引擎名：`is_layer_navigation_enabled`

Returns if a layer's built-in navigation regions generation is enabled.

### IsLayerYSortEnabled

```csharp
public bool IsLayerYSortEnabled(int layer)
```

引擎名：`is_layer_y_sort_enabled`

Returns if a layer Y-sorts its tiles.

If `layer` is negative, the layers are accessed from the last one.

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

Returns for the given coordinate `coordsInPattern` in a [`TileMapPattern`](TileMapPattern.md) the corresponding cell coordinates if the pattern was pasted at the `positionInTilemap` coordinates (see `SetPattern()`). This mapping is required as in half-offset tile shapes, the mapping might not work by calculating `position_in_tile_map + coords_in_pattern`.

### MapToLocal

```csharp
public Vector2 MapToLocal(Vector2I mapPosition)
```

引擎名：`map_to_local`

Returns the centered position of a cell in the TileMap's local coordinate space. To convert the returned value into global coordinates, use [`Node2D.ToGlobal`](Node2D.md). See also `LocalToMap()`.

**Note:** This may not correspond to the visual position of the tile, i.e. it ignores the [`TileData.TextureOrigin`](TileData.md) property of individual tiles.

### MoveLayer

```csharp
public void MoveLayer(int layer, int toPosition)
```

引擎名：`move_layer`

Moves the layer at index `layer` to the given position `toPosition` in the array.

### NotifyRuntimeTileDataUpdate

```csharp
public void NotifyRuntimeTileDataUpdate(int layer = -1)
```

引擎名：`notify_runtime_tile_data_update`

Notifies the TileMap node that calls to `_UseTileDataRuntimeUpdate()` or `_TileDataRuntimeUpdate()` will lead to different results. This will thus trigger a TileMap update.

If `layer` is provided, only notifies changes for the given layer. Providing the `layer` argument (when applicable) is usually preferred for performance reasons.

**Warning:** Updating the TileMap is computationally expensive and may impact performance. Try to limit the number of calls to this function to avoid unnecessary update.

**Note:** This does not trigger a direct update of the TileMap, the update will be done at the end of the frame as usual (unless you call `UpdateInternals()`).

### RemoveLayer

```csharp
public void RemoveLayer(int layer)
```

引擎名：`remove_layer`

Removes the layer at index `layer`.

### SetCell

```csharp
public void SetCell(int layer, Vector2I coords, int sourceId = -1, Vector2I? atlasCoords = null, int alternativeTile = 0)
```

引擎名：`set_cell`

Sets the tile identifiers for the cell on layer `layer` at coordinates `coords`. Each tile of the [`TileSet`](TileSet.md) is identified using three parts:

- The source identifier `sourceId` identifies a [`TileSetSource`](TileSetSource.md) identifier. See [`TileSet.SetSourceId`](TileSet.md),

- The atlas coordinates identifier `atlasCoords` identifies a tile coordinates in the atlas (if the source is a [`TileSetAtlasSource`](TileSetAtlasSource.md)). For [`TileSetScenesCollectionSource`](TileSetScenesCollectionSource.md) it should always be `Vector2i(0, 0)`),

- The alternative tile identifier `alternativeTile` identifies a tile alternative in the atlas (if the source is a [`TileSetAtlasSource`](TileSetAtlasSource.md)), and the scene for a [`TileSetScenesCollectionSource`](TileSetScenesCollectionSource.md).

If `sourceId` is set to `-1`, `atlasCoords` to `Vector2i(-1, -1)` or `alternativeTile` to `-1`, the cell will be erased. An erased cell gets **all** its identifiers automatically set to their respective invalid values, namely `-1`, `Vector2i(-1, -1)` and `-1`.

If `layer` is negative, the layers are accessed from the last one.

参数：
- `atlasCoords`：If the parameter is null, then the default value is `new Vector2I(-1, -1)`.

### SetCellsTerrainConnect

```csharp
public void SetCellsTerrainConnect(int layer, Array<Vector2I> cells, int terrainSet, int terrain, bool ignoreEmptyTerrains = true)
```

引擎名：`set_cells_terrain_connect`

Update all the cells in the `cells` coordinates array so that they use the given `terrain` for the given `terrainSet`. If an updated cell has the same terrain as one of its neighboring cells, this function tries to join the two. This function might update neighboring tiles if needed to create correct terrain transitions.

If `ignoreEmptyTerrains` is `true`, empty terrains will be ignored when trying to find the best fitting tile for the given terrain constraints.

If `layer` is negative, the layers are accessed from the last one.

**Note:** To work correctly, this method requires the TileMap's TileSet to have terrains set up with all required terrain combinations. Otherwise, it may produce unexpected results.

### SetCellsTerrainPath

```csharp
public void SetCellsTerrainPath(int layer, Array<Vector2I> path, int terrainSet, int terrain, bool ignoreEmptyTerrains = true)
```

引擎名：`set_cells_terrain_path`

Update all the cells in the `path` coordinates array so that they use the given `terrain` for the given `terrainSet`. The function will also connect two successive cell in the path with the same terrain. This function might update neighboring tiles if needed to create correct terrain transitions.

If `ignoreEmptyTerrains` is `true`, empty terrains will be ignored when trying to find the best fitting tile for the given terrain constraints.

If `layer` is negative, the layers are accessed from the last one.

**Note:** To work correctly, this method requires the TileMap's TileSet to have terrains set up with all required terrain combinations. Otherwise, it may produce unexpected results.

### SetCollisionAnimatable

```csharp
public void SetCollisionAnimatable(bool enabled)
```

引擎名：`set_collision_animatable`

### SetCollisionVisibilityMode

```csharp
public void SetCollisionVisibilityMode(TileMap.VisibilityMode collisionVisibilityMode)
```

引擎名：`set_collision_visibility_mode`

### SetLayerEnabled

```csharp
public void SetLayerEnabled(int layer, bool enabled)
```

引擎名：`set_layer_enabled`

Enables or disables the layer `layer`. A disabled layer is not processed at all (no rendering, no physics, etc.).

If `layer` is negative, the layers are accessed from the last one.

### SetLayerModulate

```csharp
public void SetLayerModulate(int layer, Color modulate)
```

引擎名：`set_layer_modulate`

Sets a layer's color. It will be multiplied by tile's color and TileMap's modulate.

If `layer` is negative, the layers are accessed from the last one.

### SetLayerName

```csharp
public void SetLayerName(int layer, string name)
```

引擎名：`set_layer_name`

Sets a layer's name. This is mostly useful in the editor.

If `layer` is negative, the layers are accessed from the last one.

### SetLayerNavigationEnabled

```csharp
public void SetLayerNavigationEnabled(int layer, bool enabled)
```

引擎名：`set_layer_navigation_enabled`

Enables or disables a layer's built-in navigation regions generation. Disable this if you need to bake navigation regions from a TileMap using a [`NavigationRegion2D`](NavigationRegion2D.md) node.

### SetLayerNavigationMap

```csharp
public void SetLayerNavigationMap(int layer, Rid map)
```

引擎名：`set_layer_navigation_map`

Assigns `map` as a [`NavigationServer2D`](NavigationServer2D.md) navigation map for the specified TileMap layer `layer`.

By default the TileMap uses the default [`World2D`](World2D.md) navigation map for the first TileMap layer. For each additional TileMap layer a new navigation map is created for the additional layer.

In order to make [`NavigationAgent2D`](NavigationAgent2D.md) switch between TileMap layer navigation maps use [`NavigationAgent2D.SetNavigationMap`](NavigationAgent2D.md) with the navigation map received from `GetLayerNavigationMap()`.

If `layer` is negative, the layers are accessed from the last one.

### SetLayerYSortEnabled

```csharp
public void SetLayerYSortEnabled(int layer, bool ySortEnabled)
```

引擎名：`set_layer_y_sort_enabled`

Enables or disables a layer's Y-sorting. If a layer is Y-sorted, the layer will behave as a CanvasItem node where each of its tile gets Y-sorted.

Y-sorted layers should usually be on different Z-index values than not Y-sorted layers, otherwise, each of those layer will be Y-sorted as whole with the Y-sorted one. This is usually an undesired behavior.

If `layer` is negative, the layers are accessed from the last one.

### SetLayerYSortOrigin

```csharp
public void SetLayerYSortOrigin(int layer, int ySortOrigin)
```

引擎名：`set_layer_y_sort_origin`

Sets a layer's Y-sort origin value. This Y-sort origin value is added to each tile's Y-sort origin value.

This allows, for example, to fake a different height level on each layer. This can be useful for top-down view games.

If `layer` is negative, the layers are accessed from the last one.

### SetLayerZIndex

```csharp
public void SetLayerZIndex(int layer, int zIndex)
```

引擎名：`set_layer_z_index`

Sets a layers Z-index value. This Z-index is added to each tile's Z-index value.

If `layer` is negative, the layers are accessed from the last one.

### SetNavigationMap

```csharp
public void SetNavigationMap(int layer, Rid map)
```

引擎名：`set_navigation_map`

Assigns `map` as a [`NavigationServer2D`](NavigationServer2D.md) navigation map for the specified TileMap layer `layer`.

### SetNavigationVisibilityMode

```csharp
public void SetNavigationVisibilityMode(TileMap.VisibilityMode navigationVisibilityMode)
```

引擎名：`set_navigation_visibility_mode`

### SetPattern

```csharp
public void SetPattern(int layer, Vector2I position, TileMapPattern pattern)
```

引擎名：`set_pattern`

Paste the given [`TileMapPattern`](TileMapPattern.md) at the given `position` and `layer` in the tile map.

If `layer` is negative, the layers are accessed from the last one.

### SetQuadrantSize

```csharp
public void SetQuadrantSize(int quadrantSize)
```

### SetRenderingQuadrantSize

```csharp
public void SetRenderingQuadrantSize(int size)
```

引擎名：`set_rendering_quadrant_size`

### SetTileset

```csharp
public void SetTileset(TileSet tileset)
```

引擎名：`set_tileset`

### UpdateInternals

```csharp
public void UpdateInternals()
```

引擎名：`update_internals`

Triggers a direct update of the TileMap. Usually, calling this function is not needed, as TileMap node updates automatically when one of its properties or cells is modified.

However, for performance reasons, those updates are batched and delayed to the end of the frame. Calling this function will force the TileMap to update right away instead.

**Warning:** Updating the TileMap is computationally expensive and may impact performance. Try to limit the number of updates and how many tiles they impact.

### _TileDataRuntimeUpdate

```csharp
public virtual void _TileDataRuntimeUpdate(int layer, Vector2I coords, TileData tileData)
```

引擎名：`_tile_data_runtime_update`

Called with a TileData object about to be used internally by the TileMap, allowing its modification at runtime.

This method is only called if `_UseTileDataRuntimeUpdate()` is implemented and returns `true` for the given tile `coords` and `layer`.

**Warning:** The `tileData` object's sub-resources are the same as the one in the TileSet. Modifying them might impact the whole TileSet. Instead, make sure to duplicate those resources.

**Note:** If the properties of `tileData` object should change over time, use `NotifyRuntimeTileDataUpdate()` to notify the TileMap it needs an update.

### _UseTileDataRuntimeUpdate

```csharp
public virtual bool _UseTileDataRuntimeUpdate(int layer, Vector2I coords)
```

引擎名：`_use_tile_data_runtime_update`

Should return `true` if the tile at coordinates `coords` on layer `layer` requires a runtime update.

**Warning:** Make sure this function only return `true` when needed. Any tile processed at runtime without a need for it will imply a significant performance penalty.

**Note:** If the result of this function should changed, use `NotifyRuntimeTileDataUpdate()` to notify the TileMap it needs an update.

## 事件（信号）

### Changed

```csharp
public event Action Changed
```

引擎名：`changed`

Emitted when the [`TileSet`](TileSet.md) of this TileMap changes.

## VisibilityMode

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：Use the debug settings to determine visibility.
- `ForceHide = 2`：Always hide.
- `ForceShow = 1`：Always show.
