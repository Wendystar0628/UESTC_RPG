<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TileSet

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A TileSet is a library of tiles for a [`TileMapLayer`](TileMapLayer.md). A TileSet handles a list of [`TileSetSource`](TileSetSource.md), each of them storing a set of tiles.

Tiles can either be from a [`TileSetAtlasSource`](TileSetAtlasSource.md), which renders tiles out of a texture with support for physics, navigation, etc., or from a [`TileSetScenesCollectionSource`](TileSetScenesCollectionSource.md), which exposes scene-based tiles.

Tiles are referenced by using three IDs: their source ID, their atlas coordinates ID, and their alternative tile ID.

A TileSet can be configured so that its tiles expose more or fewer properties. To do so, the TileSet resources use property layers, which you can add or remove depending on your needs.

For example, adding a physics layer allows giving collision shapes to your tiles. Each layer has dedicated properties (physics layer and mask), so you may add several TileSet physics layers for each type of collision you need.

See the functions to add new layers for more information.

## 构造函数

### TileSet

```csharp
public TileSet()
```

## 属性

### TileLayout

```csharp
public TileSet.TileLayoutEnum TileLayout { get; set; }
```

引擎名：`tile_layout`

For all half-offset shapes (Isometric, Hexagonal and Half-Offset square), changes the way tiles are indexed in the [`TileMapLayer`](TileMapLayer.md) grid.

### TileOffsetAxis

```csharp
public TileSet.TileOffsetAxisEnum TileOffsetAxis { get; set; }
```

引擎名：`tile_offset_axis`

For all half-offset shapes (Isometric, Hexagonal and Half-Offset square), determines the offset axis.

### TileShape

```csharp
public TileSet.TileShapeEnum TileShape { get; set; }
```

引擎名：`tile_shape`

The tile shape.

### TileSize

```csharp
public Vector2I TileSize { get; set; }
```

引擎名：`tile_size`

The tile size, in pixels. For all tile shapes, this size corresponds to the encompassing rectangle of the tile shape. This is thus the minimal cell size required in an atlas.

### UVClipping

```csharp
public bool UVClipping { get; set; }
```

引擎名：`uv_clipping`

Enables/Disable uv clipping when rendering the tiles.

## 方法

### AddCustomDataLayer

```csharp
public void AddCustomDataLayer(int toPosition = -1)
```

引擎名：`add_custom_data_layer`

Adds a custom data layer to the TileSet at the given position `toPosition` in the array. If `toPosition` is -1, adds it at the end of the array.

Custom data layers allow assigning custom properties to atlas tiles.

### AddNavigationLayer

```csharp
public void AddNavigationLayer(int toPosition = -1)
```

引擎名：`add_navigation_layer`

Adds a navigation layer to the TileSet at the given position `toPosition` in the array. If `toPosition` is -1, adds it at the end of the array.

Navigation layers allow assigning a navigable area to atlas tiles.

### AddOcclusionLayer

```csharp
public void AddOcclusionLayer(int toPosition = -1)
```

引擎名：`add_occlusion_layer`

Adds an occlusion layer to the TileSet at the given position `toPosition` in the array. If `toPosition` is -1, adds it at the end of the array.

Occlusion layers allow assigning occlusion polygons to atlas tiles.

### AddPattern

```csharp
public int AddPattern(TileMapPattern pattern, int index = -1)
```

引擎名：`add_pattern`

Adds a [`TileMapPattern`](TileMapPattern.md) to be stored in the TileSet resource. If provided, insert it at the given `index`.

### AddPhysicsLayer

```csharp
public void AddPhysicsLayer(int toPosition = -1)
```

引擎名：`add_physics_layer`

Adds a physics layer to the TileSet at the given position `toPosition` in the array. If `toPosition` is -1, adds it at the end of the array.

Physics layers allow assigning collision polygons to atlas tiles.

### AddSource

```csharp
public int AddSource(TileSetSource source, int atlasSourceIdOverride = -1)
```

引擎名：`add_source`

Adds a [`TileSetSource`](TileSetSource.md) to the TileSet. If `atlasSourceIdOverride` is not -1, also set its source ID. Otherwise, a unique identifier is automatically generated.

The function returns the added source ID or -1 if the source could not be added.

**Warning:** A source cannot belong to two TileSets at the same time. If the added source was attached to another `TileSet`, it will be removed from that one.

### AddTerrain

```csharp
public void AddTerrain(int terrainSet, int toPosition = -1)
```

引擎名：`add_terrain`

Adds a new terrain to the given terrain set `terrainSet` at the given position `toPosition` in the array. If `toPosition` is -1, adds it at the end of the array.

### AddTerrainSet

```csharp
public void AddTerrainSet(int toPosition = -1)
```

引擎名：`add_terrain_set`

Adds a new terrain set at the given position `toPosition` in the array. If `toPosition` is -1, adds it at the end of the array.

### CleanupInvalidTileProxies

```csharp
public void CleanupInvalidTileProxies()
```

引擎名：`cleanup_invalid_tile_proxies`

Clears tile proxies pointing to invalid tiles.

### ClearTerrains

```csharp
public void ClearTerrains(int terrainSet)
```

引擎名：`clear_terrains`

Clears all terrain properties for the given terrain set.

### ClearTileProxies

```csharp
public void ClearTileProxies()
```

引擎名：`clear_tile_proxies`

Clears all tile proxies.

### GetAlternativeLevelTileProxy

```csharp
public Array GetAlternativeLevelTileProxy(int sourceFrom, Vector2I coordsFrom, int alternativeFrom)
```

引擎名：`get_alternative_level_tile_proxy`

Returns the alternative-level proxy for the given identifiers. The returned array contains the three proxie's target identifiers (source ID, atlas coords ID and alternative tile ID).

If the TileSet has no proxy for the given identifiers, returns an empty Array.

### GetCoordsLevelTileProxy

```csharp
public Array GetCoordsLevelTileProxy(int sourceFrom, Vector2I coordsFrom)
```

引擎名：`get_coords_level_tile_proxy`

Returns the coordinate-level proxy for the given identifiers. The returned array contains the two target identifiers of the proxy (source ID and atlas coordinates ID).

If the TileSet has no proxy for the given identifiers, returns an empty Array.

### GetCustomDataLayerByName

```csharp
public int GetCustomDataLayerByName(string layerName)
```

引擎名：`get_custom_data_layer_by_name`

Returns the index of the custom data layer identified by the given name.

### GetCustomDataLayerName

```csharp
public string GetCustomDataLayerName(int layerIndex)
```

引擎名：`get_custom_data_layer_name`

Returns the name of the custom data layer identified by the given index.

### GetCustomDataLayerType

```csharp
public Variant.Type GetCustomDataLayerType(int layerIndex)
```

引擎名：`get_custom_data_layer_type`

Returns the type of the custom data layer identified by the given index.

### GetCustomDataLayersCount

```csharp
public int GetCustomDataLayersCount()
```

引擎名：`get_custom_data_layers_count`

Returns the custom data layers count.

### GetNavigationLayerLayerValue

```csharp
public bool GetNavigationLayerLayerValue(int layerIndex, int layerNumber)
```

引擎名：`get_navigation_layer_layer_value`

Returns whether or not the specified navigation layer of the TileSet navigation data layer identified by the given `layerIndex` is enabled, given a navigation_layers `layerNumber` between 1 and 32.

### GetNavigationLayerLayers

```csharp
public uint GetNavigationLayerLayers(int layerIndex)
```

引擎名：`get_navigation_layer_layers`

Returns the navigation layers (as in the Navigation server) of the given TileSet navigation layer.

### GetNavigationLayersCount

```csharp
public int GetNavigationLayersCount()
```

引擎名：`get_navigation_layers_count`

Returns the navigation layers count.

### GetNextSourceId

```csharp
public int GetNextSourceId()
```

引擎名：`get_next_source_id`

Returns a new unused source ID. This generated ID is the same that a call to `AddSource()` would return.

### GetOcclusionLayerLightMask

```csharp
public int GetOcclusionLayerLightMask(int layerIndex)
```

引擎名：`get_occlusion_layer_light_mask`

Returns the light mask of the occlusion layer.

### GetOcclusionLayerSdfCollision

```csharp
public bool GetOcclusionLayerSdfCollision(int layerIndex)
```

引擎名：`get_occlusion_layer_sdf_collision`

Returns if the occluders from this layer use `sdf_collision`.

### GetOcclusionLayersCount

```csharp
public int GetOcclusionLayersCount()
```

引擎名：`get_occlusion_layers_count`

Returns the occlusion layers count.

### GetPattern

```csharp
public TileMapPattern GetPattern(int index = -1)
```

引擎名：`get_pattern`

Returns the [`TileMapPattern`](TileMapPattern.md) at the given `index`.

### GetPatternsCount

```csharp
public int GetPatternsCount()
```

引擎名：`get_patterns_count`

Returns the number of [`TileMapPattern`](TileMapPattern.md) this tile set handles.

### GetPhysicsLayerCollisionLayer

```csharp
public uint GetPhysicsLayerCollisionLayer(int layerIndex)
```

引擎名：`get_physics_layer_collision_layer`

Returns the collision layer (as in the physics server) bodies on the given TileSet's physics layer are in.

### GetPhysicsLayerCollisionMask

```csharp
public uint GetPhysicsLayerCollisionMask(int layerIndex)
```

引擎名：`get_physics_layer_collision_mask`

Returns the collision mask of bodies on the given TileSet's physics layer.

### GetPhysicsLayerCollisionPriority

```csharp
public float GetPhysicsLayerCollisionPriority(int layerIndex)
```

引擎名：`get_physics_layer_collision_priority`

Returns the collision priority of bodies on the given TileSet's physics layer.

### GetPhysicsLayerPhysicsMaterial

```csharp
public PhysicsMaterial GetPhysicsLayerPhysicsMaterial(int layerIndex)
```

引擎名：`get_physics_layer_physics_material`

Returns the physics material of bodies on the given TileSet's physics layer.

### GetPhysicsLayersCount

```csharp
public int GetPhysicsLayersCount()
```

引擎名：`get_physics_layers_count`

Returns the physics layers count.

### GetSource

```csharp
public TileSetSource GetSource(int sourceId)
```

引擎名：`get_source`

Returns the [`TileSetSource`](TileSetSource.md) with ID `sourceId`.

### GetSourceCount

```csharp
public int GetSourceCount()
```

引擎名：`get_source_count`

Returns the number of [`TileSetSource`](TileSetSource.md) in this TileSet.

### GetSourceId

```csharp
public int GetSourceId(int index)
```

引擎名：`get_source_id`

Returns the source ID for source with index `index`.

### GetSourceLevelTileProxy

```csharp
public int GetSourceLevelTileProxy(int sourceFrom)
```

引擎名：`get_source_level_tile_proxy`

Returns the source-level proxy for the given source identifier.

If the TileSet has no proxy for the given identifier, returns -1.

### GetTerrainColor

```csharp
public Color GetTerrainColor(int terrainSet, int terrainIndex)
```

引擎名：`get_terrain_color`

Returns a terrain's color.

### GetTerrainName

```csharp
public string GetTerrainName(int terrainSet, int terrainIndex)
```

引擎名：`get_terrain_name`

Returns a terrain's name.

### GetTerrainSetMode

```csharp
public TileSet.TerrainMode GetTerrainSetMode(int terrainSet)
```

引擎名：`get_terrain_set_mode`

Returns a terrain set mode.

### GetTerrainSetsCount

```csharp
public int GetTerrainSetsCount()
```

引擎名：`get_terrain_sets_count`

Returns the terrain sets count.

### GetTerrainsCount

```csharp
public int GetTerrainsCount(int terrainSet)
```

引擎名：`get_terrains_count`

Returns the number of terrains in the given terrain set.

### GetTileLayout

```csharp
public TileSet.TileLayoutEnum GetTileLayout()
```

引擎名：`get_tile_layout`

### GetTileOffsetAxis

```csharp
public TileSet.TileOffsetAxisEnum GetTileOffsetAxis()
```

引擎名：`get_tile_offset_axis`

### GetTileShape

```csharp
public TileSet.TileShapeEnum GetTileShape()
```

引擎名：`get_tile_shape`

### GetTileSize

```csharp
public Vector2I GetTileSize()
```

引擎名：`get_tile_size`

### HasAlternativeLevelTileProxy

```csharp
public bool HasAlternativeLevelTileProxy(int sourceFrom, Vector2I coordsFrom, int alternativeFrom)
```

引擎名：`has_alternative_level_tile_proxy`

Returns if there is an alternative-level proxy for the given identifiers.

### HasCoordsLevelTileProxy

```csharp
public bool HasCoordsLevelTileProxy(int sourceFrom, Vector2I coordsFrom)
```

引擎名：`has_coords_level_tile_proxy`

Returns if there is a coodinates-level proxy for the given identifiers.

### HasCustomDataLayerByName

```csharp
public bool HasCustomDataLayerByName(string layerName)
```

引擎名：`has_custom_data_layer_by_name`

Returns if there is a custom data layer named `layerName`.

### HasSource

```csharp
public bool HasSource(int sourceId)
```

引擎名：`has_source`

Returns if this TileSet has a source for the given source ID.

### HasSourceLevelTileProxy

```csharp
public bool HasSourceLevelTileProxy(int sourceFrom)
```

引擎名：`has_source_level_tile_proxy`

Returns if there is a source-level proxy for the given source ID.

### IsUVClipping

```csharp
public bool IsUVClipping()
```

引擎名：`is_uv_clipping`

### MapTileProxy

```csharp
public Array MapTileProxy(int sourceFrom, Vector2I coordsFrom, int alternativeFrom)
```

引擎名：`map_tile_proxy`

According to the configured proxies, maps the provided identifiers to a new set of identifiers. The source ID, atlas coordinates ID and alternative tile ID are returned as a 3 elements Array.

This function first look for matching alternative-level proxies, then coordinates-level proxies, then source-level proxies.

If no proxy corresponding to provided identifiers are found, returns the same values the ones used as arguments.

### MoveCustomDataLayer

```csharp
public void MoveCustomDataLayer(int layerIndex, int toPosition)
```

引擎名：`move_custom_data_layer`

Moves the custom data layer at index `layerIndex` to the given position `toPosition` in the array. Also updates the atlas tiles accordingly.

### MoveNavigationLayer

```csharp
public void MoveNavigationLayer(int layerIndex, int toPosition)
```

引擎名：`move_navigation_layer`

Moves the navigation layer at index `layerIndex` to the given position `toPosition` in the array. Also updates the atlas tiles accordingly.

### MoveOcclusionLayer

```csharp
public void MoveOcclusionLayer(int layerIndex, int toPosition)
```

引擎名：`move_occlusion_layer`

Moves the occlusion layer at index `layerIndex` to the given position `toPosition` in the array. Also updates the atlas tiles accordingly.

### MovePhysicsLayer

```csharp
public void MovePhysicsLayer(int layerIndex, int toPosition)
```

引擎名：`move_physics_layer`

Moves the physics layer at index `layerIndex` to the given position `toPosition` in the array. Also updates the atlas tiles accordingly.

### MoveTerrain

```csharp
public void MoveTerrain(int terrainSet, int terrainIndex, int toPosition)
```

引擎名：`move_terrain`

Moves the terrain at index `terrainIndex` for terrain set `terrainSet` to the given position `toPosition` in the array. Also updates the atlas tiles accordingly.

### MoveTerrainSet

```csharp
public void MoveTerrainSet(int terrainSet, int toPosition)
```

引擎名：`move_terrain_set`

Moves the terrain set at index `terrainSet` to the given position `toPosition` in the array. Also updates the atlas tiles accordingly.

### RemoveAlternativeLevelTileProxy

```csharp
public void RemoveAlternativeLevelTileProxy(int sourceFrom, Vector2I coordsFrom, int alternativeFrom)
```

引擎名：`remove_alternative_level_tile_proxy`

Removes an alternative-level proxy for the given identifiers.

### RemoveCoordsLevelTileProxy

```csharp
public void RemoveCoordsLevelTileProxy(int sourceFrom, Vector2I coordsFrom)
```

引擎名：`remove_coords_level_tile_proxy`

Removes a coordinates-level proxy for the given identifiers.

### RemoveCustomDataLayer

```csharp
public void RemoveCustomDataLayer(int layerIndex)
```

引擎名：`remove_custom_data_layer`

Removes the custom data layer at index `layerIndex`. Also updates the atlas tiles accordingly.

### RemoveNavigationLayer

```csharp
public void RemoveNavigationLayer(int layerIndex)
```

引擎名：`remove_navigation_layer`

Removes the navigation layer at index `layerIndex`. Also updates the atlas tiles accordingly.

### RemoveOcclusionLayer

```csharp
public void RemoveOcclusionLayer(int layerIndex)
```

引擎名：`remove_occlusion_layer`

Removes the occlusion layer at index `layerIndex`. Also updates the atlas tiles accordingly.

### RemovePattern

```csharp
public void RemovePattern(int index)
```

引擎名：`remove_pattern`

Remove the [`TileMapPattern`](TileMapPattern.md) at the given index.

### RemovePhysicsLayer

```csharp
public void RemovePhysicsLayer(int layerIndex)
```

引擎名：`remove_physics_layer`

Removes the physics layer at index `layerIndex`. Also updates the atlas tiles accordingly.

### RemoveSource

```csharp
public void RemoveSource(int sourceId)
```

引擎名：`remove_source`

Removes the source with the given source ID.

### RemoveSourceLevelTileProxy

```csharp
public void RemoveSourceLevelTileProxy(int sourceFrom)
```

引擎名：`remove_source_level_tile_proxy`

Removes a source-level tile proxy.

### RemoveTerrain

```csharp
public void RemoveTerrain(int terrainSet, int terrainIndex)
```

引擎名：`remove_terrain`

Removes the terrain at index `terrainIndex` in the given terrain set `terrainSet`. Also updates the atlas tiles accordingly.

### RemoveTerrainSet

```csharp
public void RemoveTerrainSet(int terrainSet)
```

引擎名：`remove_terrain_set`

Removes the terrain set at index `terrainSet`. Also updates the atlas tiles accordingly.

### SetAlternativeLevelTileProxy

```csharp
public void SetAlternativeLevelTileProxy(int sourceFrom, Vector2I coordsFrom, int alternativeFrom, int sourceTo, Vector2I coordsTo, int alternativeTo)
```

引擎名：`set_alternative_level_tile_proxy`

Create an alternative-level proxy for the given identifiers. A proxy will map set of tile identifiers to another set of identifiers.

Proxied tiles can be automatically replaced in TileMapLayer nodes using the editor.

### SetCoordsLevelTileProxy

```csharp
public void SetCoordsLevelTileProxy(int sourceFrom, Vector2I coordsFrom, int sourceTo, Vector2I coordsTo)
```

引擎名：`set_coords_level_tile_proxy`

Creates a coordinates-level proxy for the given identifiers. A proxy will map set of tile identifiers to another set of identifiers. The alternative tile ID is kept the same when using coordinates-level proxies.

Proxied tiles can be automatically replaced in TileMapLayer nodes using the editor.

### SetCustomDataLayerName

```csharp
public void SetCustomDataLayerName(int layerIndex, string layerName)
```

引擎名：`set_custom_data_layer_name`

Sets the name of the custom data layer identified by the given index. Names are identifiers of the layer therefore if the name is already taken it will fail and raise an error.

### SetCustomDataLayerType

```csharp
public void SetCustomDataLayerType(int layerIndex, Variant.Type layerType)
```

引擎名：`set_custom_data_layer_type`

Sets the type of the custom data layer identified by the given index.

### SetNavigationLayerLayerValue

```csharp
public void SetNavigationLayerLayerValue(int layerIndex, int layerNumber, bool value)
```

引擎名：`set_navigation_layer_layer_value`

Based on `value`, enables or disables the specified navigation layer of the TileSet navigation data layer identified by the given `layerIndex`, given a navigation_layers `layerNumber` between 1 and 32.

### SetNavigationLayerLayers

```csharp
public void SetNavigationLayerLayers(int layerIndex, uint layers)
```

引擎名：`set_navigation_layer_layers`

Sets the navigation layers (as in the navigation server) for navigation regions in the given TileSet navigation layer.

### SetOcclusionLayerLightMask

```csharp
public void SetOcclusionLayerLightMask(int layerIndex, int lightMask)
```

引擎名：`set_occlusion_layer_light_mask`

Sets the occlusion layer (as in the rendering server) for occluders in the given TileSet occlusion layer.

### SetOcclusionLayerSdfCollision

```csharp
public void SetOcclusionLayerSdfCollision(int layerIndex, bool sdfCollision)
```

引擎名：`set_occlusion_layer_sdf_collision`

Enables or disables SDF collision for occluders in the given TileSet occlusion layer.

### SetPhysicsLayerCollisionLayer

```csharp
public void SetPhysicsLayerCollisionLayer(int layerIndex, uint layer)
```

引擎名：`set_physics_layer_collision_layer`

Sets the collision layer (as in the physics server) for bodies in the given TileSet physics layer.

### SetPhysicsLayerCollisionMask

```csharp
public void SetPhysicsLayerCollisionMask(int layerIndex, uint mask)
```

引擎名：`set_physics_layer_collision_mask`

Sets the collision mask for bodies in the given TileSet physics layer.

### SetPhysicsLayerCollisionPriority

```csharp
public void SetPhysicsLayerCollisionPriority(int layerIndex, float priority)
```

引擎名：`set_physics_layer_collision_priority`

Sets the collision priority for bodies in the given TileSet physics layer.

### SetPhysicsLayerPhysicsMaterial

```csharp
public void SetPhysicsLayerPhysicsMaterial(int layerIndex, PhysicsMaterial physicsMaterial)
```

引擎名：`set_physics_layer_physics_material`

Sets the physics material for bodies in the given TileSet physics layer.

### SetSourceId

```csharp
public void SetSourceId(int sourceId, int newSourceId)
```

引擎名：`set_source_id`

Changes a source's ID.

### SetSourceLevelTileProxy

```csharp
public void SetSourceLevelTileProxy(int sourceFrom, int sourceTo)
```

引擎名：`set_source_level_tile_proxy`

Creates a source-level proxy for the given source ID. A proxy will map set of tile identifiers to another set of identifiers. Both the atlas coordinates ID and the alternative tile ID are kept the same when using source-level proxies.

Proxied tiles can be automatically replaced in TileMapLayer nodes using the editor.

### SetTerrainColor

```csharp
public void SetTerrainColor(int terrainSet, int terrainIndex, Color color)
```

引擎名：`set_terrain_color`

Sets a terrain's color. This color is used for identifying the different terrains in the TileSet editor.

### SetTerrainName

```csharp
public void SetTerrainName(int terrainSet, int terrainIndex, string name)
```

引擎名：`set_terrain_name`

Sets a terrain's name.

### SetTerrainSetMode

```csharp
public void SetTerrainSetMode(int terrainSet, TileSet.TerrainMode mode)
```

引擎名：`set_terrain_set_mode`

Sets a terrain mode. Each mode determines which bits of a tile shape is used to match the neighboring tiles' terrains.

### SetTileLayout

```csharp
public void SetTileLayout(TileSet.TileLayoutEnum layout)
```

引擎名：`set_tile_layout`

### SetTileOffsetAxis

```csharp
public void SetTileOffsetAxis(TileSet.TileOffsetAxisEnum alignment)
```

引擎名：`set_tile_offset_axis`

### SetTileShape

```csharp
public void SetTileShape(TileSet.TileShapeEnum shape)
```

引擎名：`set_tile_shape`

### SetTileSize

```csharp
public void SetTileSize(Vector2I size)
```

引擎名：`set_tile_size`

### SetUVClipping

```csharp
public void SetUVClipping(bool uVClipping)
```

引擎名：`set_uv_clipping`

## CellNeighbor

命名空间 `Godot`；enum

### 枚举值

- `RightSide = 0`：Neighbor on the right side.
- `RightCorner = 1`：Neighbor in the right corner.
- `BottomRightSide = 2`：Neighbor on the bottom right side.
- `BottomRightCorner = 3`：Neighbor in the bottom right corner.
- `BottomSide = 4`：Neighbor on the bottom side.
- `BottomCorner = 5`：Neighbor in the bottom corner.
- `BottomLeftSide = 6`：Neighbor on the bottom left side.
- `BottomLeftCorner = 7`：Neighbor in the bottom left corner.
- `LeftSide = 8`：Neighbor on the left side.
- `LeftCorner = 9`：Neighbor in the left corner.
- `TopLeftSide = 10`：Neighbor on the top left side.
- `TopLeftCorner = 11`：Neighbor in the top left corner.
- `TopSide = 12`：Neighbor on the top side.
- `TopCorner = 13`：Neighbor in the top corner.
- `TopRightSide = 14`：Neighbor on the top right side.
- `TopRightCorner = 15`：Neighbor in the top right corner.

## TerrainMode

命名空间 `Godot`；enum

### 枚举值

- `CornersAndSides = 0`：Requires both corners and side to match with neighboring tiles' terrains.
- `Corners = 1`：Requires corners to match with neighboring tiles' terrains.
- `Sides = 2`：Requires sides to match with neighboring tiles' terrains.

## TileLayoutEnum

命名空间 `Godot`；enum

### 枚举值

- `Stacked = 0`：Tile coordinates layout where both axis stay consistent with their respective local horizontal and vertical axis.
- `StackedOffset = 1`：Same as `Stacked`, but the first half-offset is negative instead of positive.
- `StairsRight = 2`：Tile coordinates layout where the horizontal axis stay horizontal, and the vertical one goes down-right.
- `StairsDown = 3`：Tile coordinates layout where the vertical axis stay vertical, and the horizontal one goes down-right.
- `DiamondRight = 4`：Tile coordinates layout where the horizontal axis goes up-right, and the vertical one goes down-right.
- `DiamondDown = 5`：Tile coordinates layout where the horizontal axis goes down-right, and the vertical one goes down-left.

## TileOffsetAxisEnum

命名空间 `Godot`；enum

### 枚举值

- `Horizontal = 0`：Horizontal half-offset.
- `Vertical = 1`：Vertical half-offset.

## TileShapeEnum

命名空间 `Godot`；enum

### 枚举值

- `Square = 0`：Rectangular tile shape.
- `Isometric = 1`：Diamond tile shape (for isometric look).
- `HalfOffsetSquare = 2`：Rectangular tile shape with one row/column out of two offset by half a tile.
- `Hexagon = 3`：Hexagonal tile shape.
