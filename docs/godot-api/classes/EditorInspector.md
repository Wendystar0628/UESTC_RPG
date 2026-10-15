<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorInspector

命名空间 `Godot`；class；继承 [`ScrollContainer`](ScrollContainer.md) → [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This is the control that implements property editing in the editor's Settings dialogs, the Inspector dock, etc. To get the `EditorInspector` used in the editor's Inspector dock, use [`EditorInterface.GetInspector`](EditorInterface.md).

`EditorInspector` will show properties in the same order as the array returned by [`GodotObject.GetPropertyList`](GodotObject.md).

If a property's name is path-like (i.e. if it contains forward slashes), `EditorInspector` will create nested sections for "directories" along the path. For example, if a property is named `highlighting/gdscript/node_path_color`, it will be shown as "Node Path Color" inside the "GDScript" section nested inside the "Highlighting" section.

If a property has [`PropertyUsageFlags.Group`](PropertyUsageFlags.md) usage, it will group subsequent properties whose name starts with the property's hint string. The group ends when a property does not start with that hint string or when a new group starts. An empty group name effectively ends the current group. `EditorInspector` will create a top-level section for each group. For example, if a property with group usage is named `Collide With` and its hint string is `collide_with_`, a subsequent `collide_with_area` property will be shown as "Area" inside the "Collide With" section. There is also a special case: when the hint string contains the name of a property, that property is grouped too. This is mainly to help grouping properties like `font`, `font_color` and `font_size` (using the hint string `font_`).

If a property has [`PropertyUsageFlags.Subgroup`](PropertyUsageFlags.md) usage, a subgroup will be created in the same way as a group, and a second-level section will be created for each subgroup.

**Note:** Unlike sections created from path-like property names, `EditorInspector` won't capitalize the name for sections created from groups. So properties with group usage usually use capitalized names instead of snake_cased names.

## 构造函数

### EditorInspector

```csharp
public EditorInspector()
```

## 方法

### CollapseAllFolding

```csharp
public void CollapseAllFolding()
```

引擎名：`collapse_all_folding`

Collapses all foldable sections.

### CreateDefaultInspector

```csharp
public static EditorInspector CreateDefaultInspector(LineEdit filterLineEdit = null)
```

引擎名：`create_default_inspector`

Creates an inspector with the same configuration as the one used in the editor's Inspector dock. When passing a [`LineEdit`](LineEdit.md) into `filterLineEdit`, the inspector will filter its properties based on [`LineEdit.Text`](LineEdit.md) whenever [`LineEdit.TextChanged`](LineEdit.md) is emitted.

### Edit

```csharp
public void Edit(GodotObject object)
```

引擎名：`edit`

Shows the properties of the given `object` in this inspector for editing. To clear the inspector, call this method with `null`.

**Note:** If you want to edit an object in the editor's main inspector, use the `edit_*` methods in [`EditorInterface`](EditorInterface.md) instead.

### ExpandAllFolding

```csharp
public void ExpandAllFolding()
```

引擎名：`expand_all_folding`

Expands all foldable sections.

### ExpandRevertable

```csharp
public void ExpandRevertable()
```

引擎名：`expand_revertable`

Expands only the foldable sections that contain a revertable (i.e. non-default) property.

### GetEditedObject

```csharp
public GodotObject GetEditedObject()
```

引擎名：`get_edited_object`

Returns the object currently selected in this inspector.

### GetSelectedPath

```csharp
public string GetSelectedPath()
```

引擎名：`get_selected_path`

Gets the path of the currently selected property.

### InstantiatePropertyEditor

```csharp
public static EditorProperty InstantiatePropertyEditor(GodotObject object, Variant.Type type, string path, PropertyHint hint, string hintText, uint usage, bool wide = false)
```

引擎名：`instantiate_property_editor`

Creates a property editor that can be used by plugin UI to edit the specified property of an `object`.

## 事件（信号）

### EditedObjectChanged

```csharp
public event Action EditedObjectChanged
```

引擎名：`edited_object_changed`

Emitted when the object being edited by the inspector has changed.

### ObjectIdSelected

```csharp
public event EditorInspector.ObjectIdSelectedEventHandler ObjectIdSelected
```

引擎名：`object_id_selected`

Emitted when the Edit button of an [`GodotObject`](GodotObject.md) has been pressed in the inspector. This is mainly used in the remote scene tree Inspector.

### PropertyDeleted

```csharp
public event EditorInspector.PropertyDeletedEventHandler PropertyDeleted
```

引擎名：`property_deleted`

Emitted when a property is removed from the inspector.

### PropertyEdited

```csharp
public event EditorInspector.PropertyEditedEventHandler PropertyEdited
```

引擎名：`property_edited`

Emitted when a property is edited in the inspector.

### PropertyKeyed

```csharp
public event EditorInspector.PropertyKeyedEventHandler PropertyKeyed
```

引擎名：`property_keyed`

Emitted when a property is keyed in the inspector. Properties can be keyed by clicking the "key" icon next to a property when the Animation panel is toggled.

### PropertySelected

```csharp
public event EditorInspector.PropertySelectedEventHandler PropertySelected
```

引擎名：`property_selected`

Emitted when a property is selected in the inspector.

### PropertyToggled

```csharp
public event EditorInspector.PropertyToggledEventHandler PropertyToggled
```

引擎名：`property_toggled`

Emitted when a boolean property is toggled in the inspector.

**Note:** This signal is never emitted if the internal `autoclear` property enabled. Since this property is always enabled in the editor inspector, this signal is never emitted by the editor itself.

### ResourceSelected

```csharp
public event EditorInspector.ResourceSelectedEventHandler ResourceSelected
```

引擎名：`resource_selected`

Emitted when a resource is selected in the inspector.

### RestartRequested

```csharp
public event Action RestartRequested
```

引擎名：`restart_requested`

Emitted when a property that requires a restart to be applied is edited in the inspector. This is only used in the Project Settings and Editor Settings.

## ObjectIdSelectedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorInspector.ObjectIdSelected`](EditorInspector.md) event of a [`EditorInspector`](EditorInspector.md) class.

### 签名

#### Invoke

```csharp
delegate void ObjectIdSelectedEventHandler(long id)
```

## PropertyDeletedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorInspector.PropertyDeleted`](EditorInspector.md) event of a [`EditorInspector`](EditorInspector.md) class.

### 签名

#### Invoke

```csharp
delegate void PropertyDeletedEventHandler(string property)
```

## PropertyEditedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorInspector.PropertyEdited`](EditorInspector.md) event of a [`EditorInspector`](EditorInspector.md) class.

### 签名

#### Invoke

```csharp
delegate void PropertyEditedEventHandler(string property)
```

## PropertyKeyedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorInspector.PropertyKeyed`](EditorInspector.md) event of a [`EditorInspector`](EditorInspector.md) class.

### 签名

#### Invoke

```csharp
delegate void PropertyKeyedEventHandler(string property, Variant value, bool advance)
```

## PropertySelectedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorInspector.PropertySelected`](EditorInspector.md) event of a [`EditorInspector`](EditorInspector.md) class.

### 签名

#### Invoke

```csharp
delegate void PropertySelectedEventHandler(string property)
```

## PropertyToggledEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorInspector.PropertyToggled`](EditorInspector.md) event of a [`EditorInspector`](EditorInspector.md) class.

### 签名

#### Invoke

```csharp
delegate void PropertyToggledEventHandler(string property, bool checked)
```

## ResourceSelectedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorInspector.ResourceSelected`](EditorInspector.md) event of a [`EditorInspector`](EditorInspector.md) class.

### 签名

#### Invoke

```csharp
delegate void ResourceSelectedEventHandler(Resource resource, string path)
```
