<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorInspectorPlugin

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

`EditorInspectorPlugin` allows adding custom property editors to [`EditorInspector`](EditorInspector.md).

When an object is edited, the `_CanHandle()` function is called and must return `true` if the object type is supported.

If supported, the function `_ParseBegin()` will be called, allowing to place custom controls at the beginning of the class.

Subsequently, the `_ParseCategory()` and `_ParseProperty()` are called for every category and property. They offer the ability to add custom controls to the inspector too.

Finally, `_ParseEnd()` will be called.

On each of these calls, the "add" functions can be called.

To use `EditorInspectorPlugin`, register it using the [`EditorPlugin.AddInspectorPlugin`](EditorPlugin.md) method first.

## 构造函数

### EditorInspectorPlugin

```csharp
public EditorInspectorPlugin()
```

## 方法

### AddCustomControl

```csharp
public void AddCustomControl(Control control)
```

引擎名：`add_custom_control`

Adds a custom control, which is not necessarily a property editor.

### AddPropertyEditor

```csharp
public void AddPropertyEditor(string property, Control editor, bool addToEnd = false, string label = "")
```

引擎名：`add_property_editor`

Adds a property editor for an individual property. The `editor` control must extend [`EditorProperty`](EditorProperty.md).

There can be multiple property editors for a property. If `addToEnd` is `true`, this newly added editor will be displayed after all the other editors of the property whose `addToEnd` is `false`. For example, the editor uses this parameter to add an "Edit Region" button for [`Sprite2D.RegionRect`](Sprite2D.md) below the regular [`Rect2`](Rect2.md) editor.

`label` can be used to choose a custom label for the property editor in the inspector. If left empty, the label is computed from the name of the property instead.

### AddPropertyEditor

```csharp
public void AddPropertyEditor(string property, Control editor, bool addToEnd)
```

引擎名：`add_property_editor`

Adds a property editor for an individual property. The `editor` control must extend [`EditorProperty`](EditorProperty.md).

There can be multiple property editors for a property. If `addToEnd` is `true`, this newly added editor will be displayed after all the other editors of the property whose `addToEnd` is `false`. For example, the editor uses this parameter to add an "Edit Region" button for [`Sprite2D.RegionRect`](Sprite2D.md) below the regular [`Rect2`](Rect2.md) editor.

`label` can be used to choose a custom label for the property editor in the inspector. If left empty, the label is computed from the name of the property instead.

### AddPropertyEditorForMultipleProperties

```csharp
public void AddPropertyEditorForMultipleProperties(string label, ReadOnlySpan<string> properties, Control editor)
```

引擎名：`add_property_editor_for_multiple_properties`

Adds an editor that allows modifying multiple properties. The `editor` control must extend [`EditorProperty`](EditorProperty.md).

### AddPropertyEditorForMultipleProperties

```csharp
public void AddPropertyEditorForMultipleProperties(string label, string[] properties, Control editor)
```

引擎名：`add_property_editor_for_multiple_properties`

Adds an editor that allows modifying multiple properties. The `editor` control must extend [`EditorProperty`](EditorProperty.md).

### _CanHandle

```csharp
public virtual bool _CanHandle(GodotObject object)
```

引擎名：`_can_handle`

Returns `true` if this object can be handled by this plugin.

### _ParseBegin

```csharp
public virtual void _ParseBegin(GodotObject object)
```

引擎名：`_parse_begin`

Called to allow adding controls at the beginning of the property list for `object`.

### _ParseCategory

```csharp
public virtual void _ParseCategory(GodotObject object, string category)
```

引擎名：`_parse_category`

Called to allow adding controls at the beginning of a category in the property list for `object`.

### _ParseEnd

```csharp
public virtual void _ParseEnd(GodotObject object)
```

引擎名：`_parse_end`

Called to allow adding controls at the end of the property list for `object`.

### _ParseGroup

```csharp
public virtual void _ParseGroup(GodotObject object, string group)
```

引擎名：`_parse_group`

Called to allow adding controls at the beginning of a group or a sub-group in the property list for `object`.

### _ParseProperty

```csharp
public virtual bool _ParseProperty(GodotObject object, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide)
```

引擎名：`_parse_property`

Called to allow adding property-specific editors to the property list for `object`. The added editor control must extend [`EditorProperty`](EditorProperty.md). Returning `true` removes the built-in editor for this property, otherwise allows to insert a custom editor before the built-in one.
