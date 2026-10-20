<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MeshLibrary

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A library of meshes. Contains a list of [`Mesh`](Mesh.md) resources, each with a name and ID. Each item can also include collision and navigation shapes. This resource is used in [`GridMap`](GridMap.md).

## 构造函数

### MeshLibrary

```csharp
public MeshLibrary()
```

## 方法

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears the library.

### CreateItem

```csharp
public void CreateItem(int id)
```

引擎名：`create_item`

Creates a new item in the library with the given ID.

You can get an unused ID from `GetLastUnusedItemId()`.

### FindItemByName

```csharp
public int FindItemByName(string name)
```

引擎名：`find_item_by_name`

Returns the first item with the given name, or `-1` if no item is found.

### GetItemCount

```csharp
public int GetItemCount()
```

引擎名：`get_item_count`

Returns the number of items present in the library.

### GetItemList

```csharp
public int[] GetItemList()
```

引擎名：`get_item_list`

Returns the list of item IDs in use.

### GetItemMesh

```csharp
public Mesh GetItemMesh(int id)
```

引擎名：`get_item_mesh`

Returns the item's mesh.

### GetItemMeshCastShadow

```csharp
public RenderingServer.ShadowCastingSetting GetItemMeshCastShadow(int id)
```

引擎名：`get_item_mesh_cast_shadow`

Returns the item's shadow casting mode.

### GetItemMeshTransform

```csharp
public Transform3D GetItemMeshTransform(int id)
```

引擎名：`get_item_mesh_transform`

Returns the transform applied to the item's mesh.

### GetItemName

```csharp
public string GetItemName(int id)
```

引擎名：`get_item_name`

Returns the item's name.

### GetItemNavigationLayers

```csharp
public uint GetItemNavigationLayers(int id)
```

引擎名：`get_item_navigation_layers`

Returns the item's navigation layers bitmask.

### GetItemNavigationMesh

```csharp
public NavigationMesh GetItemNavigationMesh(int id)
```

引擎名：`get_item_navigation_mesh`

Returns the item's navigation mesh.

### GetItemNavigationMeshTransform

```csharp
public Transform3D GetItemNavigationMeshTransform(int id)
```

引擎名：`get_item_navigation_mesh_transform`

Returns the transform applied to the item's navigation mesh.

### GetItemPreview

```csharp
public Texture2D GetItemPreview(int id)
```

引擎名：`get_item_preview`

When running in the editor, returns a generated item preview (a 3D rendering in isometric perspective). When used in a running project, returns the manually-defined item preview which can be set using `SetItemPreview()`. Returns an empty [`Texture2D`](Texture2D.md) if no preview was manually set in a running project.

### GetItemShapes

```csharp
public Array GetItemShapes(int id)
```

引擎名：`get_item_shapes`

Returns an item's collision shapes.

The array consists of each [`Shape3D`](Shape3D.md) followed by its [`Transform3D`](Transform3D.md).

### GetLastUnusedItemId

```csharp
public int GetLastUnusedItemId()
```

引擎名：`get_last_unused_item_id`

Gets an unused ID for a new item.

### RemoveItem

```csharp
public void RemoveItem(int id)
```

引擎名：`remove_item`

Removes the item.

### SetItemMesh

```csharp
public void SetItemMesh(int id, Mesh mesh)
```

引擎名：`set_item_mesh`

Sets the item's mesh.

### SetItemMeshCastShadow

```csharp
public void SetItemMeshCastShadow(int id, RenderingServer.ShadowCastingSetting shadowCastingSetting)
```

引擎名：`set_item_mesh_cast_shadow`

Sets the item's shadow casting mode to `shadowCastingSetting`.

### SetItemMeshTransform

```csharp
public void SetItemMeshTransform(int id, Transform3D meshTransform)
```

引擎名：`set_item_mesh_transform`

Sets the transform to apply to the item's mesh.

### SetItemName

```csharp
public void SetItemName(int id, string name)
```

引擎名：`set_item_name`

Sets the item's name.

This name is shown in the editor. It can also be used to look up the item later using `FindItemByName()`.

### SetItemNavigationLayers

```csharp
public void SetItemNavigationLayers(int id, uint navigationLayers)
```

引擎名：`set_item_navigation_layers`

Sets the item's navigation layers bitmask.

### SetItemNavigationMesh

```csharp
public void SetItemNavigationMesh(int id, NavigationMesh navigationMesh)
```

引擎名：`set_item_navigation_mesh`

Sets the item's navigation mesh.

### SetItemNavigationMeshTransform

```csharp
public void SetItemNavigationMeshTransform(int id, Transform3D navigationMesh)
```

引擎名：`set_item_navigation_mesh_transform`

Sets the transform to apply to the item's navigation mesh.

### SetItemPreview

```csharp
public void SetItemPreview(int id, Texture2D texture)
```

引擎名：`set_item_preview`

Sets a texture to use as the item's preview icon in the editor.

### SetItemShapes

```csharp
public void SetItemShapes(int id, Array shapes)
```

引擎名：`set_item_shapes`

Sets an item's collision shapes.

The array should consist of [`Shape3D`](Shape3D.md) objects, each followed by a [`Transform3D`](Transform3D.md) that will be applied to it. For shapes that should not have a transform, use `Transform3D.Identity`.
