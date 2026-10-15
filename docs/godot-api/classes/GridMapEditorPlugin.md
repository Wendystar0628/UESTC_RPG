<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GridMapEditorPlugin

命名空间 `Godot`；class；继承 [`EditorPlugin`](EditorPlugin.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

GridMapEditorPlugin provides access to the [`GridMap`](GridMap.md) editor functionality.

## 构造函数

### GridMapEditorPlugin

```csharp
public GridMapEditorPlugin()
```

## 方法

### ClearSelection

```csharp
public void ClearSelection()
```

引擎名：`clear_selection`

Deselects any currently selected cells.

### GetCurrentGridMap

```csharp
public GridMap GetCurrentGridMap()
```

引擎名：`get_current_grid_map`

Returns the [`GridMap`](GridMap.md) node currently edited by the grid map editor.

### GetSelectedCells

```csharp
public Array GetSelectedCells()
```

引擎名：`get_selected_cells`

Returns an array of [`Vector3I`](Vector3I.md)s with the selected cells' coordinates.

### GetSelectedPaletteItem

```csharp
public int GetSelectedPaletteItem()
```

引擎名：`get_selected_palette_item`

Returns the index of the selected [`MeshLibrary`](MeshLibrary.md) item in the grid map editor's palette or `-1` if no item is selected.

**Note:** The indices might not be in the same order as they appear in the editor's interface.

### GetSelection

```csharp
public Aabb GetSelection()
```

引擎名：`get_selection`

Returns the cell coordinate bounds of the current selection. Use `HasSelection()` to check if there is an active selection.

### HasSelection

```csharp
public bool HasSelection()
```

引擎名：`has_selection`

Returns `true` if there are selected cells.

### SetSelectedPaletteItem

```csharp
public void SetSelectedPaletteItem(int item)
```

引擎名：`set_selected_palette_item`

Selects the [`MeshLibrary`](MeshLibrary.md) item with the given index in the grid map editor's palette. If a negative index is given, no item will be selected. If a value greater than the last index is given, the last item will be selected.

**Note:** The indices might not be in the same order as they appear in the editor's interface.

### SetSelection

```csharp
public void SetSelection(Vector3I begin, Vector3I end)
```

引擎名：`set_selection`

Selects the cells inside the given bounds from `begin` to `end`.
