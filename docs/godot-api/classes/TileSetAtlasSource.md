<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TileSetAtlasSource

命名空间 `Godot`；class；继承 [`TileSetSource`](TileSetSource.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An atlas is a grid of tiles laid out on a texture. Each tile in the grid must be exposed using `CreateTile()`. Those tiles are then indexed using their coordinates in the grid.

Each tile can also have a size in the grid coordinates, making it more or less cells in the atlas.

Alternatives version of a tile can be created using `CreateAlternativeTile()`, which are then indexed using an alternative ID. The main tile (the one in the grid), is accessed with an alternative ID equal to 0.

Each tile alternate has a set of properties that is defined by the source's [`TileSet`](TileSet.md) layers. Those properties are stored in a TileData object that can be accessed and modified using `GetTileData()`.

As TileData properties are stored directly in the TileSetAtlasSource resource, their properties might also be set using `TileSetAtlasSource.set("<coords_x>:<coords_y>/<alternative_id>/<tile_data_property>")`.

## 构造函数

### TileSetAtlasSource

```csharp
public TileSetAtlasSource()
```

## 属性

### Margins

```csharp
public Vector2I Margins { get; set; }
```

引擎名：`margins`

Margins, in pixels, to offset the origin of the grid in the texture.

### Separation

```csharp
public Vector2I Separation { get; set; }
```

引擎名：`separation`

Separation, in pixels, between each tile texture region of the grid.

### Texture

```csharp
public Texture2D Texture { get; set; }
```

引擎名：`texture`

The atlas texture.

### TextureRegionSize

```csharp
public Vector2I TextureRegionSize { get; set; }
```

引擎名：`texture_region_size`

The base tile size in the texture (in pixel). This size must be bigger than or equal to the TileSet's `tile_size` value.

### UseTexturePadding

```csharp
public bool UseTexturePadding { get; set; }
```

引擎名：`use_texture_padding`

If `true`, generates an internal texture with an additional one pixel padding around each tile. Texture padding avoids a common artifact where lines appear between tiles.

Disabling this setting might lead a small performance improvement, as generating the internal texture requires both memory and processing time when the TileSetAtlasSource resource is modified.

## 方法

### ClearTilesOutsideTexture

```csharp
public void ClearTilesOutsideTexture()
```

引擎名：`clear_tiles_outside_texture`

Removes all tiles that don't fit the available texture area. This method iterates over all the source's tiles, so it's advised to use `HasTilesOutsideTexture()` beforehand.

### CreateAlternativeTile

```csharp
public int CreateAlternativeTile(Vector2I atlasCoords, int alternativeIdOverride = -1)
```

引擎名：`create_alternative_tile`

Creates an alternative tile for the tile at coordinates `atlasCoords`. If `alternativeIdOverride` is -1, give it an automatically generated unique ID, or assigns it the given ID otherwise.

Returns the new alternative identifier, or -1 if the alternative could not be created with a provided `alternativeIdOverride`.

### CreateTile

```csharp
public void CreateTile(Vector2I atlasCoords, Vector2I? size = null)
```

引擎名：`create_tile`

Creates a new tile at coordinates `atlasCoords` with the given `size`.

参数：
- `size`：If the parameter is null, then the default value is `new Vector2I(1, 1)`.

### GetAtlasGridSize

```csharp
public Vector2I GetAtlasGridSize()
```

引擎名：`get_atlas_grid_size`

Returns the atlas grid size, which depends on how many tiles can fit in the texture. It thus depends on the `Texture`'s size, the atlas `Margins`, and the tiles' `TextureRegionSize`.

### GetMargins

```csharp
public Vector2I GetMargins()
```

引擎名：`get_margins`

### GetNextAlternativeTileId

```csharp
public int GetNextAlternativeTileId(Vector2I atlasCoords)
```

引擎名：`get_next_alternative_tile_id`

Returns the alternative ID a following call to `CreateAlternativeTile()` would return.

### GetRuntimeTexture

```csharp
public Texture2D GetRuntimeTexture()
```

引擎名：`get_runtime_texture`

If `UseTexturePadding` is `false`, returns `Texture`. Otherwise, returns an internal [`ImageTexture`](ImageTexture.md) created that includes the padding.

### GetRuntimeTileTextureRegion

```csharp
public Rect2I GetRuntimeTileTextureRegion(Vector2I atlasCoords, int frame)
```

引擎名：`get_runtime_tile_texture_region`

Returns the region of the tile at coordinates `atlasCoords` for the given `frame` inside the texture returned by `GetRuntimeTexture()`.

**Note:** If `UseTexturePadding` is `false`, returns the same as `GetTileTextureRegion()`.

### GetSeparation

```csharp
public Vector2I GetSeparation()
```

引擎名：`get_separation`

### GetTexture

```csharp
public Texture2D GetTexture()
```

引擎名：`get_texture`

### GetTextureRegionSize

```csharp
public Vector2I GetTextureRegionSize()
```

引擎名：`get_texture_region_size`

### GetTileAnimationColumns

```csharp
public int GetTileAnimationColumns(Vector2I atlasCoords)
```

引擎名：`get_tile_animation_columns`

Returns how many columns the tile at `atlasCoords` has in its animation layout.

### GetTileAnimationFrameDuration

```csharp
public float GetTileAnimationFrameDuration(Vector2I atlasCoords, int frameIndex)
```

引擎名：`get_tile_animation_frame_duration`

Returns the animation frame duration of frame `frameIndex` for the tile at coordinates `atlasCoords`.

### GetTileAnimationFramesCount

```csharp
public int GetTileAnimationFramesCount(Vector2I atlasCoords)
```

引擎名：`get_tile_animation_frames_count`

Returns how many animation frames has the tile at coordinates `atlasCoords`.

### GetTileAnimationMode

```csharp
public TileSetAtlasSource.TileAnimationMode GetTileAnimationMode(Vector2I atlasCoords)
```

引擎名：`get_tile_animation_mode`

Returns the tile animation mode of the tile at `atlasCoords`. See also `SetTileAnimationMode()`.

### GetTileAnimationSeparation

```csharp
public Vector2I GetTileAnimationSeparation(Vector2I atlasCoords)
```

引擎名：`get_tile_animation_separation`

Returns the separation (as in the atlas grid) between each frame of an animated tile at coordinates `atlasCoords`.

### GetTileAnimationSpeed

```csharp
public float GetTileAnimationSpeed(Vector2I atlasCoords)
```

引擎名：`get_tile_animation_speed`

Returns the animation speed of the tile at coordinates `atlasCoords`.

### GetTileAnimationTotalDuration

```csharp
public float GetTileAnimationTotalDuration(Vector2I atlasCoords)
```

引擎名：`get_tile_animation_total_duration`

Returns the sum of the sum of the frame durations of the tile at coordinates `atlasCoords`. This value needs to be divided by the animation speed to get the actual animation loop duration.

### GetTileAtCoords

```csharp
public Vector2I GetTileAtCoords(Vector2I atlasCoords)
```

引擎名：`get_tile_at_coords`

If there is a tile covering the `atlasCoords` coordinates, returns the top-left coordinates of the tile (thus its coordinate ID). Returns `Vector2i(-1, -1)` otherwise.

### GetTileData

```csharp
public TileData GetTileData(Vector2I atlasCoords, int alternativeTile)
```

引擎名：`get_tile_data`

Returns the [`TileData`](TileData.md) object for the given atlas coordinates and alternative ID.

### GetTileSizeInAtlas

```csharp
public Vector2I GetTileSizeInAtlas(Vector2I atlasCoords)
```

引擎名：`get_tile_size_in_atlas`

Returns the size of the tile (in the grid coordinates system) at coordinates `atlasCoords`.

### GetTileTextureRegion

```csharp
public Rect2I GetTileTextureRegion(Vector2I atlasCoords, int frame = 0)
```

引擎名：`get_tile_texture_region`

Returns a tile's texture region in the atlas texture. For animated tiles, a `frame` argument might be provided for the different frames of the animation.

### GetTilesToBeRemovedOnChange

```csharp
public Vector2[] GetTilesToBeRemovedOnChange(Texture2D texture, Vector2I margins, Vector2I separation, Vector2I textureRegionSize)
```

引擎名：`get_tiles_to_be_removed_on_change`

Returns an array of tiles coordinates ID that will be automatically removed when modifying one or several of those properties: `texture`, `margins`, `separation` or `textureRegionSize`. This can be used to undo changes that would have caused tiles data loss.

### GetUseTexturePadding

```csharp
public bool GetUseTexturePadding()
```

引擎名：`get_use_texture_padding`

### HasRoomForTile

```csharp
public bool HasRoomForTile(Vector2I atlasCoords, Vector2I size, int animationColumns, Vector2I animationSeparation, int framesCount, Vector2I? ignoredTile = null)
```

引擎名：`has_room_for_tile`

Returns whether there is enough room in an atlas to create/modify a tile with the given properties. If `ignoredTile` is provided, act as is the given tile was not present in the atlas. This may be used when you want to modify a tile's properties.

参数：
- `ignoredTile`：If the parameter is null, then the default value is `new Vector2I(-1, -1)`.

### HasTilesOutsideTexture

```csharp
public bool HasTilesOutsideTexture()
```

引擎名：`has_tiles_outside_texture`

Checks if the source has any tiles that don't fit the texture area (either partially or completely).

### MoveTileInAtlas

```csharp
public void MoveTileInAtlas(Vector2I atlasCoords, Vector2I? newAtlasCoords = null, Vector2I? newSize = null)
```

引擎名：`move_tile_in_atlas`

Move the tile and its alternatives at the `atlasCoords` coordinates to the `newAtlasCoords` coordinates with the `newSize` size. This functions will fail if a tile is already present in the given area.

If `newAtlasCoords` is `Vector2i(-1, -1)`, keeps the tile's coordinates. If `newSize` is `Vector2i(-1, -1)`, keeps the tile's size.

To avoid an error, first check if a move is possible using `HasRoomForTile()`.

参数：
- `newAtlasCoords`：If the parameter is null, then the default value is `new Vector2I(-1, -1)`.
- `newSize`：If the parameter is null, then the default value is `new Vector2I(-1, -1)`.

### RemoveAlternativeTile

```csharp
public void RemoveAlternativeTile(Vector2I atlasCoords, int alternativeTile)
```

引擎名：`remove_alternative_tile`

Remove a tile's alternative with alternative ID `alternativeTile`.

Calling this function with `alternativeTile` equals to 0 will fail, as the base tile alternative cannot be removed.

### RemoveTile

```csharp
public void RemoveTile(Vector2I atlasCoords)
```

引擎名：`remove_tile`

Remove a tile and its alternative at coordinates `atlasCoords`.

### SetAlternativeTileId

```csharp
public void SetAlternativeTileId(Vector2I atlasCoords, int alternativeTile, int newId)
```

引擎名：`set_alternative_tile_id`

Change a tile's alternative ID from `alternativeTile` to `newId`.

Calling this function with `newId` of 0 will fail, as the base tile alternative cannot be moved.

### SetMargins

```csharp
public void SetMargins(Vector2I margins)
```

引擎名：`set_margins`

### SetSeparation

```csharp
public void SetSeparation(Vector2I separation)
```

引擎名：`set_separation`

### SetTexture

```csharp
public void SetTexture(Texture2D texture)
```

引擎名：`set_texture`

### SetTextureRegionSize

```csharp
public void SetTextureRegionSize(Vector2I textureRegionSize)
```

引擎名：`set_texture_region_size`

### SetTileAnimationColumns

```csharp
public void SetTileAnimationColumns(Vector2I atlasCoords, int frameColumns)
```

引擎名：`set_tile_animation_columns`

Sets the number of columns in the animation layout of the tile at coordinates `atlasCoords`. If set to 0, then the different frames of the animation are laid out as a single horizontal line in the atlas.

### SetTileAnimationFrameDuration

```csharp
public void SetTileAnimationFrameDuration(Vector2I atlasCoords, int frameIndex, float duration)
```

引擎名：`set_tile_animation_frame_duration`

Sets the animation frame `duration` of frame `frameIndex` for the tile at coordinates `atlasCoords`.

### SetTileAnimationFramesCount

```csharp
public void SetTileAnimationFramesCount(Vector2I atlasCoords, int framesCount)
```

引擎名：`set_tile_animation_frames_count`

Sets how many animation frames the tile at coordinates `atlasCoords` has.

### SetTileAnimationMode

```csharp
public void SetTileAnimationMode(Vector2I atlasCoords, TileSetAtlasSource.TileAnimationMode mode)
```

引擎名：`set_tile_animation_mode`

Sets the tile animation mode of the tile at `atlasCoords` to `mode`. See also `GetTileAnimationMode()`.

### SetTileAnimationSeparation

```csharp
public void SetTileAnimationSeparation(Vector2I atlasCoords, Vector2I separation)
```

引擎名：`set_tile_animation_separation`

Sets the margin (in grid tiles) between each tile in the animation layout of the tile at coordinates `atlasCoords` has.

### SetTileAnimationSpeed

```csharp
public void SetTileAnimationSpeed(Vector2I atlasCoords, float speed)
```

引擎名：`set_tile_animation_speed`

Sets the animation speed of the tile at coordinates `atlasCoords` has.

### SetUseTexturePadding

```csharp
public void SetUseTexturePadding(bool useTexturePadding)
```

引擎名：`set_use_texture_padding`

## 常量

### TransformFlipH

```csharp
public const long TransformFlipH = 4096
```

Represents cell's horizontal flip flag. Should be used directly with [`TileMapLayer`](TileMapLayer.md) to flip placed tiles by altering their alternative IDs.

```csharp
var alternate_id = $TileMapLayer.get_cell_alternative_tile(Vector2i(2, 2))
if not alternate_id & TileSetAtlasSource.TRANSFORM_FLIP_H:
# If tile is not already flipped, flip it.
$TileMapLayer.set_cell(Vector2i(2, 2), source_id, atlas_coords, alternate_id | TileSetAtlasSource.TRANSFORM_FLIP_H)

```

**Note:** These transformations can be combined to do the equivalent of 0, 90, 180, and 270 degree rotations, as shown below:

```csharp
enum TileTransform {
ROTATE_0 = 0,
ROTATE_90 = TileSetAtlasSource.TRANSFORM_TRANSPOSE | TileSetAtlasSource.TRANSFORM_FLIP_H,
ROTATE_180 = TileSetAtlasSource.TRANSFORM_FLIP_H | TileSetAtlasSource.TRANSFORM_FLIP_V,
ROTATE_270 = TileSetAtlasSource.TRANSFORM_TRANSPOSE | TileSetAtlasSource.TRANSFORM_FLIP_V,
}

```

### TransformFlipV

```csharp
public const long TransformFlipV = 8192
```

Represents cell's vertical flip flag. See `TransformFlipH` for usage.

### TransformTranspose

```csharp
public const long TransformTranspose = 16384
```

Represents cell's transposed flag. See `TransformFlipH` for usage.

## TileAnimationMode

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：Tile animations start at same time, looking identical.
- `RandomStartTimes = 1`：Tile animations start at random times, looking varied.
- `Max = 2`：Represents the size of the `TileSetAtlasSource.TileAnimationMode` enum.
