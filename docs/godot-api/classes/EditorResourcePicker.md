<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorResourcePicker

命名空间 `Godot`；class；继承 [`HBoxContainer`](HBoxContainer.md) → [`BoxContainer`](BoxContainer.md) → [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This [`Control`](Control.md) node is used in the editor's Inspector dock to allow editing of [`Resource`](Resource.md) type properties. It provides options for creating, loading, saving and converting resources. Can be used with [`EditorInspectorPlugin`](EditorInspectorPlugin.md) to recreate the same behavior.

**Note:** This [`Control`](Control.md) does not include any editor for the resource, as editing is controlled by the Inspector dock itself or sub-Inspectors.

## 构造函数

### EditorResourcePicker

```csharp
public EditorResourcePicker()
```

## 属性

### BaseType

```csharp
public string BaseType { get; set; }
```

引擎名：`base_type`

The base type of allowed resource types. Can be a comma-separated list of several options.

### Editable

```csharp
public bool Editable { get; set; }
```

引擎名：`editable`

If `true`, the value can be selected and edited.

### EditedResource

```csharp
public Resource EditedResource { get; set; }
```

引擎名：`edited_resource`

The edited resource value.

### ToggleMode

```csharp
public bool ToggleMode { get; set; }
```

引擎名：`toggle_mode`

If `true`, the main button with the resource preview works in the toggle mode. Use `SetTogglePressed()` to manually set the state.

## 方法

### GetAllowedTypes

```csharp
public string[] GetAllowedTypes()
```

引擎名：`get_allowed_types`

Returns a list of all allowed types and subtypes corresponding to the `BaseType`. If the `BaseType` is empty, an empty list is returned.

### GetBaseType

```csharp
public string GetBaseType()
```

引擎名：`get_base_type`

### GetEditedResource

```csharp
public Resource GetEditedResource()
```

引擎名：`get_edited_resource`

### IsEditable

```csharp
public bool IsEditable()
```

引擎名：`is_editable`

### IsToggleMode

```csharp
public bool IsToggleMode()
```

引擎名：`is_toggle_mode`

### SetBaseType

```csharp
public void SetBaseType(string baseType)
```

引擎名：`set_base_type`

### SetEditable

```csharp
public void SetEditable(bool enable)
```

引擎名：`set_editable`

### SetEditedResource

```csharp
public void SetEditedResource(Resource resource)
```

引擎名：`set_edited_resource`

### SetToggleMode

```csharp
public void SetToggleMode(bool enable)
```

引擎名：`set_toggle_mode`

### SetTogglePressed

```csharp
public void SetTogglePressed(bool pressed)
```

引擎名：`set_toggle_pressed`

Sets the toggle mode state for the main button. Works only if `ToggleMode` is set to `true`.

### _HandleMenuSelected

```csharp
public virtual bool _HandleMenuSelected(int id)
```

引擎名：`_handle_menu_selected`

This virtual method can be implemented to handle context menu items not handled by default. See `_SetCreateOptions()`.

### _SetCreateOptions

```csharp
public virtual void _SetCreateOptions(GodotObject menuNode)
```

引擎名：`_set_create_options`

This virtual method is called when updating the context menu of an `Editable` `EditorResourcePicker`. Implement this method to override the "New" items section with your own options. `menuNode` is a reference to the [`PopupMenu`](PopupMenu.md) node.

**Note:** Implement `_HandleMenuSelected()` to handle these custom items.

**Note:** Relevant built-in options ("Load", "Copy", "Paste", etc.) are automatically added to the `menuNode` afterwards, using their hard-coded IDs starting from `0`. Custom options need to use non-colliding IDs to be handled properly. Using `id = 100 + custom_option_index` is safe (this is what the default items in the "New" section use).

## 事件（信号）

### ResourceChanged

```csharp
public event EditorResourcePicker.ResourceChangedEventHandler ResourceChanged
```

引擎名：`resource_changed`

Emitted when the value of the edited resource was changed.

### ResourceSelected

```csharp
public event EditorResourcePicker.ResourceSelectedEventHandler ResourceSelected
```

引擎名：`resource_selected`

Emitted when the resource value was set and user clicked to edit it. When `inspect` is `true`, the signal was caused by the context menu "Edit" or "Inspect" option.

## ResourceChangedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorResourcePicker.ResourceChanged`](EditorResourcePicker.md) event of a [`EditorResourcePicker`](EditorResourcePicker.md) class.

### 签名

#### Invoke

```csharp
delegate void ResourceChangedEventHandler(Resource resource)
```

## ResourceSelectedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorResourcePicker.ResourceSelected`](EditorResourcePicker.md) event of a [`EditorResourcePicker`](EditorResourcePicker.md) class.

### 签名

#### Invoke

```csharp
delegate void ResourceSelectedEventHandler(Resource resource, bool inspect)
```
