<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorProperty

命名空间 `Godot`；class；继承 [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

A custom control for editing properties that can be added to the [`EditorInspector`](EditorInspector.md). It is added via [`EditorInspectorPlugin`](EditorInspectorPlugin.md).

## 构造函数

### EditorProperty

```csharp
public EditorProperty()
```

## 属性

### Checkable

```csharp
public bool Checkable { get; set; }
```

引擎名：`checkable`

Used by the inspector, set to `true` when the property is checkable.

### Checked

```csharp
public bool Checked { get; set; }
```

引擎名：`checked`

Used by the inspector, set to `true` when the property is checked.

### Deletable

```csharp
public bool Deletable { get; set; }
```

引擎名：`deletable`

Used by the inspector, set to `true` when the property can be deleted by the user.

### DrawBackground

```csharp
public bool DrawBackground { get; set; }
```

引擎名：`draw_background`

Used by the inspector, set to `true` when the property background is drawn.

### DrawLabel

```csharp
public bool DrawLabel { get; set; }
```

引擎名：`draw_label`

Used by the inspector, set to `true` when the property label is drawn.

### DrawWarning

```csharp
public bool DrawWarning { get; set; }
```

引擎名：`draw_warning`

Used by the inspector, set to `true` when the property is drawn with the editor theme's warning color. This is used for editable children's properties.

### Keying

```csharp
public bool Keying { get; set; }
```

引擎名：`keying`

Used by the inspector, set to `true` when the property can add keys for animation.

### Label

```csharp
public string Label { get; set; }
```

引擎名：`label`

Set this property to change the label (if you want to show one).

### NameSplitRatio

```csharp
public float NameSplitRatio { get; set; }
```

引擎名：`name_split_ratio`

Space distribution ratio between the label and the editing field.

### ReadOnly

```csharp
public bool ReadOnly { get; set; }
```

引擎名：`read_only`

Used by the inspector, set to `true` when the property is read-only.

### Selectable

```csharp
public bool Selectable { get; set; }
```

引擎名：`selectable`

Used by the inspector, set to `true` when the property is selectable.

### UseFolding

```csharp
public bool UseFolding { get; set; }
```

引擎名：`use_folding`

Used by the inspector, set to `true` when the property is using folding.

## 方法

### AddFocusable

```csharp
public void AddFocusable(Control control)
```

引擎名：`add_focusable`

If any of the controls added can gain keyboard focus, add it here. This ensures that focus will be restored if the inspector is refreshed.

### Deselect

```csharp
public void Deselect()
```

引擎名：`deselect`

Draw property as not selected. Used by the inspector.

### EmitChanged

```csharp
public void EmitChanged(StringName property, Variant value, StringName field = null, bool changing = false)
```

引擎名：`emit_changed`

If one or several properties have changed, this must be called. `field` is used in case your editor can modify fields separately (as an example, Vector3.x). The `changing` argument avoids the editor requesting this property to be refreshed (leave as `false` if unsure).

### GetEditedObject

```csharp
public GodotObject GetEditedObject()
```

引擎名：`get_edited_object`

Returns the edited object.

**Note:** This method could return `null` if the editor has not yet been associated with a property. However, in `_UpdateProperty()` and `_SetReadOnly()`, this value is *guaranteed* to be non-`null`.

### GetEditedProperty

```csharp
public StringName GetEditedProperty()
```

引擎名：`get_edited_property`

Returns the edited property. If your editor is for a single property (added via [`EditorInspectorPlugin._ParseProperty()`](EditorInspectorPlugin.md)), then this will return the property.

**Note:** This method could return `null` if the editor has not yet been associated with a property. However, in `_UpdateProperty()` and `_SetReadOnly()`, this value is *guaranteed* to be non-`null`.

### GetLabel

```csharp
public string GetLabel()
```

引擎名：`get_label`

### GetNameSplitRatio

```csharp
public float GetNameSplitRatio()
```

引擎名：`get_name_split_ratio`

### IsCheckable

```csharp
public bool IsCheckable()
```

引擎名：`is_checkable`

### IsChecked

```csharp
public bool IsChecked()
```

引擎名：`is_checked`

### IsDeletable

```csharp
public bool IsDeletable()
```

引擎名：`is_deletable`

### IsDrawBackground

```csharp
public bool IsDrawBackground()
```

引擎名：`is_draw_background`

### IsDrawLabel

```csharp
public bool IsDrawLabel()
```

引擎名：`is_draw_label`

### IsDrawWarning

```csharp
public bool IsDrawWarning()
```

引擎名：`is_draw_warning`

### IsKeying

```csharp
public bool IsKeying()
```

引擎名：`is_keying`

### IsReadOnly

```csharp
public bool IsReadOnly()
```

引擎名：`is_read_only`

### IsSelectable

```csharp
public bool IsSelectable()
```

引擎名：`is_selectable`

### IsSelected

```csharp
public bool IsSelected()
```

引擎名：`is_selected`

Returns `true` if property is drawn as selected. Used by the inspector.

### IsUsingFolding

```csharp
public bool IsUsingFolding()
```

引擎名：`is_using_folding`

### Select

```csharp
public void Select(int focusable = -1)
```

引擎名：`select`

Draw property as selected. Used by the inspector.

### SetBottomEditor

```csharp
public void SetBottomEditor(Control editor)
```

引擎名：`set_bottom_editor`

Puts the `editor` control below the property label. The control must be previously added using [`Node.AddChild`](Node.md).

### SetCheckable

```csharp
public void SetCheckable(bool checkable)
```

引擎名：`set_checkable`

### SetChecked

```csharp
public void SetChecked(bool checked)
```

引擎名：`set_checked`

### SetDeletable

```csharp
public void SetDeletable(bool deletable)
```

引擎名：`set_deletable`

### SetDrawBackground

```csharp
public void SetDrawBackground(bool drawBackground)
```

引擎名：`set_draw_background`

### SetDrawLabel

```csharp
public void SetDrawLabel(bool drawLabel)
```

引擎名：`set_draw_label`

### SetDrawWarning

```csharp
public void SetDrawWarning(bool drawWarning)
```

引擎名：`set_draw_warning`

### SetKeying

```csharp
public void SetKeying(bool keying)
```

引擎名：`set_keying`

### SetLabel

```csharp
public void SetLabel(string text)
```

引擎名：`set_label`

### SetLabelReference

```csharp
public void SetLabelReference(Control control)
```

引擎名：`set_label_reference`

Used by the inspector, set to a control that will be used as a reference to calculate the size of the label.

### SetNameSplitRatio

```csharp
public void SetNameSplitRatio(float ratio)
```

引擎名：`set_name_split_ratio`

### SetObjectAndProperty

```csharp
public void SetObjectAndProperty(GodotObject object, StringName property)
```

引擎名：`set_object_and_property`

Assigns object and property to edit.

### SetReadOnly

```csharp
public void SetReadOnly(bool readOnly)
```

引擎名：`set_read_only`

### SetSelectable

```csharp
public void SetSelectable(bool selectable)
```

引擎名：`set_selectable`

### SetUseFolding

```csharp
public void SetUseFolding(bool useFolding)
```

引擎名：`set_use_folding`

### UpdateProperty

```csharp
public void UpdateProperty()
```

引擎名：`update_property`

Forces a refresh of the property display.

### _SetReadOnly

```csharp
public virtual void _SetReadOnly(bool readOnly)
```

引擎名：`_set_read_only`

Called when the read-only status of the property is changed. It may be used to change custom controls into a read-only or modifiable state.

### _UpdateProperty

```csharp
public virtual void _UpdateProperty()
```

引擎名：`_update_property`

When this virtual function is called, you must update your editor.

## 事件（信号）

### MultiplePropertiesChanged

```csharp
public event EditorProperty.MultiplePropertiesChangedEventHandler MultiplePropertiesChanged
```

引擎名：`multiple_properties_changed`

Emit it if you want multiple properties modified at the same time. Do not use if added via [`EditorInspectorPlugin._ParseProperty()`](EditorInspectorPlugin.md).

### ObjectIdSelected

```csharp
public event EditorProperty.ObjectIdSelectedEventHandler ObjectIdSelected
```

引擎名：`object_id_selected`

Used by sub-inspectors. Emit it if what was selected was an Object ID.

### PropertyCanRevertChanged

```csharp
public event EditorProperty.PropertyCanRevertChangedEventHandler PropertyCanRevertChanged
```

引擎名：`property_can_revert_changed`

Emitted when the revertability (i.e., whether it has a non-default value and thus is displayed with a revert icon) of a property has changed.

### PropertyChanged

```csharp
public event EditorProperty.PropertyChangedEventHandler PropertyChanged
```

引擎名：`property_changed`

Do not emit this manually, use the `EmitChanged()` method instead.

### PropertyChecked

```csharp
public event EditorProperty.PropertyCheckedEventHandler PropertyChecked
```

引擎名：`property_checked`

Emitted when a property was checked. Used internally.

### PropertyDeleted

```csharp
public event EditorProperty.PropertyDeletedEventHandler PropertyDeleted
```

引擎名：`property_deleted`

Emitted when a property was deleted. Used internally.

### PropertyFavorited

```csharp
public event EditorProperty.PropertyFavoritedEventHandler PropertyFavorited
```

引擎名：`property_favorited`

Emit it if you want to mark a property as favorited, making it appear at the top of the inspector.

### PropertyKeyed

```csharp
public event EditorProperty.PropertyKeyedEventHandler PropertyKeyed
```

引擎名：`property_keyed`

Emit it if you want to add this value as an animation key (check for keying being enabled first).

### PropertyKeyedWithValue

```csharp
public event EditorProperty.PropertyKeyedWithValueEventHandler PropertyKeyedWithValue
```

引擎名：`property_keyed_with_value`

Emit it if you want to key a property with a single value.

### PropertyOverridden

```csharp
public event Action PropertyOverridden
```

引擎名：`property_overridden`

Emitted when a setting override for the current project is requested.

### PropertyPinned

```csharp
public event EditorProperty.PropertyPinnedEventHandler PropertyPinned
```

引擎名：`property_pinned`

Emit it if you want to mark (or unmark) the value of a property for being saved regardless of being equal to the default value.

The default value is the one the property will get when the node is just instantiated and can come from an ancestor scene in the inheritance/instantiation chain, a script or a builtin class.

### ResourceSelected

```csharp
public event EditorProperty.ResourceSelectedEventHandler ResourceSelected
```

引擎名：`resource_selected`

If you want a sub-resource to be edited, emit this signal with the resource.

### Selected

```csharp
public event EditorProperty.SelectedEventHandler Selected
```

引擎名：`selected`

Emitted when selected. Used internally.

## MultiplePropertiesChangedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorProperty.MultiplePropertiesChanged`](EditorProperty.md) event of a [`EditorProperty`](EditorProperty.md) class.

### 签名

#### Invoke

```csharp
delegate void MultiplePropertiesChangedEventHandler(string[] properties, Array value)
```

## ObjectIdSelectedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorProperty.ObjectIdSelected`](EditorProperty.md) event of a [`EditorProperty`](EditorProperty.md) class.

### 签名

#### Invoke

```csharp
delegate void ObjectIdSelectedEventHandler(StringName property, long id)
```

## PropertyCanRevertChangedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorProperty.PropertyCanRevertChanged`](EditorProperty.md) event of a [`EditorProperty`](EditorProperty.md) class.

### 签名

#### Invoke

```csharp
delegate void PropertyCanRevertChangedEventHandler(StringName property, bool canRevert)
```

## PropertyChangedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorProperty.PropertyChanged`](EditorProperty.md) event of a [`EditorProperty`](EditorProperty.md) class.

### 签名

#### Invoke

```csharp
delegate void PropertyChangedEventHandler(StringName property, Variant value, StringName field, bool changing)
```

## PropertyCheckedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorProperty.PropertyChecked`](EditorProperty.md) event of a [`EditorProperty`](EditorProperty.md) class.

### 签名

#### Invoke

```csharp
delegate void PropertyCheckedEventHandler(StringName property, bool checked)
```

## PropertyDeletedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorProperty.PropertyDeleted`](EditorProperty.md) event of a [`EditorProperty`](EditorProperty.md) class.

### 签名

#### Invoke

```csharp
delegate void PropertyDeletedEventHandler(StringName property)
```

## PropertyFavoritedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorProperty.PropertyFavorited`](EditorProperty.md) event of a [`EditorProperty`](EditorProperty.md) class.

### 签名

#### Invoke

```csharp
delegate void PropertyFavoritedEventHandler(StringName property, bool favorited)
```

## PropertyKeyedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorProperty.PropertyKeyed`](EditorProperty.md) event of a [`EditorProperty`](EditorProperty.md) class.

### 签名

#### Invoke

```csharp
delegate void PropertyKeyedEventHandler(StringName property)
```

## PropertyKeyedWithValueEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorProperty.PropertyKeyedWithValue`](EditorProperty.md) event of a [`EditorProperty`](EditorProperty.md) class.

### 签名

#### Invoke

```csharp
delegate void PropertyKeyedWithValueEventHandler(StringName property, Variant value)
```

## PropertyPinnedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorProperty.PropertyPinned`](EditorProperty.md) event of a [`EditorProperty`](EditorProperty.md) class.

### 签名

#### Invoke

```csharp
delegate void PropertyPinnedEventHandler(StringName property, bool pinned)
```

## ResourceSelectedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorProperty.ResourceSelected`](EditorProperty.md) event of a [`EditorProperty`](EditorProperty.md) class.

### 签名

#### Invoke

```csharp
delegate void ResourceSelectedEventHandler(string path, Resource resource)
```

## SelectedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorProperty.Selected`](EditorProperty.md) event of a [`EditorProperty`](EditorProperty.md) class.

### 签名

#### Invoke

```csharp
delegate void SelectedEventHandler(string path, long focusableIdx)
```
