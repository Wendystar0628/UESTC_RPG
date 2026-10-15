<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TileSetSource

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Exposes a set of tiles for a [`TileSet`](TileSet.md) resource.

Tiles in a source are indexed with two IDs, coordinates ID (of type Vector2i) and an alternative ID (of type int), named according to their use in the [`TileSetAtlasSource`](TileSetAtlasSource.md) class.

Depending on the TileSet source type, those IDs might have restrictions on their values, this is why the base `TileSetSource` class only exposes getters for them.

You can iterate over all tiles exposed by a TileSetSource by first iterating over coordinates IDs using `GetTilesCount()` and `GetTileId()`, then over alternative IDs using `GetAlternativeTilesCount()` and `GetAlternativeTileId()`.

**Warning:** `TileSetSource` can only be added to one TileSet at the same time. Calling [`TileSet.AddSource`](TileSet.md) on a second [`TileSet`](TileSet.md) will remove the source from the first one.

## 方法

### GetAlternativeTileId

```csharp
public int GetAlternativeTileId(Vector2I atlasCoords, int index)
```

引擎名：`get_alternative_tile_id`

Returns the alternative ID for the tile with coordinates ID `atlasCoords` at index `index`.

### GetAlternativeTilesCount

```csharp
public int GetAlternativeTilesCount(Vector2I atlasCoords)
```

引擎名：`get_alternative_tiles_count`

Returns the number of alternatives tiles for the coordinates ID `atlasCoords`.

For [`TileSetAtlasSource`](TileSetAtlasSource.md), this always return at least 1, as the base tile with ID 0 is always part of the alternatives list.

Returns -1 if there is not tile at the given coords.

### GetTileId

```csharp
public Vector2I GetTileId(int index)
```

引擎名：`get_tile_id`

Returns the tile coordinates ID of the tile with index `index`.

### GetTilesCount

```csharp
public int GetTilesCount()
```

引擎名：`get_tiles_count`

Returns how many tiles this atlas source defines (not including alternative tiles).

### HasAlternativeTile

```csharp
public bool HasAlternativeTile(Vector2I atlasCoords, int alternativeTile)
```

引擎名：`has_alternative_tile`

Returns if the base tile at coordinates `atlasCoords` has an alternative with ID `alternativeTile`.

### HasTile

```csharp
public bool HasTile(Vector2I atlasCoords)
```

引擎名：`has_tile`

Returns if this atlas has a tile with coordinates ID `atlasCoords`.
