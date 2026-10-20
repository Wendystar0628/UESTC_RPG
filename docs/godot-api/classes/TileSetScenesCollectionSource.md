<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TileSetScenesCollectionSource

命名空间 `Godot`；class；继承 [`TileSetSource`](TileSetSource.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

When placed on a [`TileMapLayer`](TileMapLayer.md), tiles from `TileSetScenesCollectionSource` will automatically instantiate an associated scene at the cell's position in the TileMapLayer.

Scenes are instantiated as children of the [`TileMapLayer`](TileMapLayer.md) after it enters the tree, at the end of the frame (their creation is deferred). If you add/remove a scene tile in the [`TileMapLayer`](TileMapLayer.md) that is already inside the tree, the [`TileMapLayer`](TileMapLayer.md) will automatically instantiate/free the scene accordingly.

**Note:** Scene tiles all occupy one tile slot and instead use alternate tile ID to identify scene index. [`TileSetSource.GetTilesCount`](TileSetSource.md) will always return `1`. Use `GetSceneTilesCount()` to get a number of scenes in a `TileSetScenesCollectionSource`.

Use this code if you want to find the scene path at a given tile in [`TileMapLayer`](TileMapLayer.md):

```csharp
int sourceId = tileMapLayer.GetCellSourceId(new Vector2I(x, y));
if (sourceId > -1)
{
TileSetSource source = tileMapLayer.TileSet.GetSource(sourceId);
if (source is TileSetScenesCollectionSource sceneSource)
{
int altId = tileMapLayer.GetCellAlternativeTile(new Vector2I(x, y));
// The assigned PackedScene.
PackedScene scene = sceneSource.GetSceneTileScene(altId);
}
}

```

## 构造函数

### TileSetScenesCollectionSource

```csharp
public TileSetScenesCollectionSource()
```

## 方法

### CreateSceneTile

```csharp
public int CreateSceneTile(PackedScene packedScene, int idOverride = -1)
```

引擎名：`create_scene_tile`

Creates a scene-based tile out of the given scene.

Returns a newly generated unique ID.

### GetNextSceneTileId

```csharp
public int GetNextSceneTileId()
```

引擎名：`get_next_scene_tile_id`

Returns the scene ID a following call to `CreateSceneTile()` would return.

### GetSceneTileDisplayPlaceholder

```csharp
public bool GetSceneTileDisplayPlaceholder(int id)
```

引擎名：`get_scene_tile_display_placeholder`

Returns whether the scene tile with `id` displays a placeholder in the editor.

### GetSceneTileId

```csharp
public int GetSceneTileId(int index)
```

引擎名：`get_scene_tile_id`

Returns the scene tile ID of the scene tile at `index`.

### GetSceneTileScene

```csharp
public PackedScene GetSceneTileScene(int id)
```

引擎名：`get_scene_tile_scene`

Returns the [`PackedScene`](PackedScene.md) resource of scene tile with `id`.

### GetSceneTilesCount

```csharp
public int GetSceneTilesCount()
```

引擎名：`get_scene_tiles_count`

Returns the number or scene tiles this TileSet source has.

### HasSceneTileId

```csharp
public bool HasSceneTileId(int id)
```

引擎名：`has_scene_tile_id`

Returns whether this TileSet source has a scene tile with `id`.

### RemoveSceneTile

```csharp
public void RemoveSceneTile(int id)
```

引擎名：`remove_scene_tile`

Remove the scene tile with `id`.

### SetSceneTileDisplayPlaceholder

```csharp
public void SetSceneTileDisplayPlaceholder(int id, bool displayPlaceholder)
```

引擎名：`set_scene_tile_display_placeholder`

Sets whether or not the scene tile with `id` should display a placeholder in the editor. This might be useful for scenes that are not visible.

### SetSceneTileId

```csharp
public void SetSceneTileId(int id, int newId)
```

引擎名：`set_scene_tile_id`

Changes a scene tile's ID from `id` to `newId`. This will fail if there is already a tile with an ID equal to `newId`.

### SetSceneTileScene

```csharp
public void SetSceneTileScene(int id, PackedScene packedScene)
```

引擎名：`set_scene_tile_scene`

Assigns a [`PackedScene`](PackedScene.md) resource to the scene tile with `id`. This will fail if the scene does not extend [`CanvasItem`](CanvasItem.md), as positioning properties are needed to place the scene on the [`TileMapLayer`](TileMapLayer.md).
