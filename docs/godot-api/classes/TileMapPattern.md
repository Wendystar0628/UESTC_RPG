<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TileMapPattern

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This resource holds a set of cells to help bulk manipulations of [`TileMap`](TileMap.md).

A pattern always starts at the `(0, 0)` coordinates and cannot have cells with negative coordinates.

## 构造函数

### TileMapPattern

```csharp
public TileMapPattern()
```

## 方法

### GetCellAlternativeTile

```csharp
public int GetCellAlternativeTile(Vector2I coords)
```

引擎名：`get_cell_alternative_tile`

Returns the tile alternative ID of the cell at `coords`.

### GetCellAtlasCoords

```csharp
public Vector2I GetCellAtlasCoords(Vector2I coords)
```

引擎名：`get_cell_atlas_coords`

Returns the tile atlas coordinates ID of the cell at `coords`.

### GetCellSourceId

```csharp
public int GetCellSourceId(Vector2I coords)
```

引擎名：`get_cell_source_id`

Returns the tile source ID of the cell at `coords`.

### GetSize

```csharp
public Vector2I GetSize()
```

引擎名：`get_size`

Returns the size, in cells, of the pattern.

### GetUsedCells

```csharp
public Array<Vector2I> GetUsedCells()
```

引擎名：`get_used_cells`

Returns the list of used cell coordinates in the pattern.

### HasCell

```csharp
public bool HasCell(Vector2I coords)
```

引擎名：`has_cell`

Returns whether the pattern has a tile at the given coordinates.

### IsEmpty

```csharp
public bool IsEmpty()
```

引擎名：`is_empty`

Returns whether the pattern is empty or not.

### RemoveCell

```csharp
public void RemoveCell(Vector2I coords, bool updateSize)
```

引擎名：`remove_cell`

Remove the cell at the given coordinates.

### SetCell

```csharp
public void SetCell(Vector2I coords, int sourceId = -1, Vector2I? atlasCoords = null, int alternativeTile = -1)
```

引擎名：`set_cell`

Sets the tile identifiers for the cell at coordinates `coords`. See [`TileMap.SetCell`](TileMap.md).

参数：
- `atlasCoords`：If the parameter is null, then the default value is `new Vector2I(-1, -1)`.

### SetSize

```csharp
public void SetSize(Vector2I size)
```

引擎名：`set_size`

Sets the size of the pattern.
