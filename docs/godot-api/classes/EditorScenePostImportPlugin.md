<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorScenePostImportPlugin

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This plugin type exists to modify the process of importing scenes, allowing to change the content as well as add importer options at every stage of the process.

## 构造函数

### EditorScenePostImportPlugin

```csharp
public EditorScenePostImportPlugin()
```

## 方法

### AddImportOption

```csharp
public void AddImportOption(string name, Variant value)
```

引擎名：`add_import_option`

Add a specific import option (name and default value only). This function can only be called from `_GetImportOptions()` and `_GetInternalImportOptions()`.

### AddImportOptionAdvanced

```csharp
public void AddImportOptionAdvanced(Variant.Type type, string name, Variant defaultValue, PropertyHint hint = PropertyHint.None, string hintString = "", int usageFlags = 6)
```

引擎名：`add_import_option_advanced`

Add a specific import option. This function can only be called from `_GetImportOptions()` and `_GetInternalImportOptions()`.

### GetOptionValue

```csharp
public Variant GetOptionValue(StringName name)
```

引擎名：`get_option_value`

Query the value of an option. This function can only be called from those querying visibility, or processing.

### _GetImportOptions

```csharp
public virtual void _GetImportOptions(string path)
```

引擎名：`_get_import_options`

Override to add general import options. These will appear in the main import dock on the editor. Add options via `AddImportOption()` and `AddImportOptionAdvanced()`.

### _GetInternalImportOptions

```csharp
public virtual void _GetInternalImportOptions(int category)
```

引擎名：`_get_internal_import_options`

Override to add internal import options. These will appear in the 3D scene import dialog. Add options via `AddImportOption()` and `AddImportOptionAdvanced()`.

### _GetInternalOptionUpdateViewRequired

```csharp
public virtual Variant _GetInternalOptionUpdateViewRequired(int category, string option)
```

引擎名：`_get_internal_option_update_view_required`

Should return `true` if the 3D view of the import dialog needs to update when changing the given option.

### _GetInternalOptionVisibility

```csharp
public virtual Variant _GetInternalOptionVisibility(int category, bool forAnimation, string option)
```

引擎名：`_get_internal_option_visibility`

Should return `true` to show the given option, `false` to hide the given option, or `null` to ignore.

### _GetOptionVisibility

```csharp
public virtual Variant _GetOptionVisibility(string path, bool forAnimation, string option)
```

引擎名：`_get_option_visibility`

Should return `true` to show the given option, `false` to hide the given option, or `null` to ignore.

### _InternalProcess

```csharp
public virtual void _InternalProcess(int category, Node baseNode, Node node, Resource resource)
```

引擎名：`_internal_process`

Process a specific node or resource for a given category.

### _PostProcess

```csharp
public virtual void _PostProcess(Node scene)
```

引擎名：`_post_process`

Post-process the scene. This function is called after the final scene has been configured.

### _PreProcess

```csharp
public virtual void _PreProcess(Node scene)
```

引擎名：`_pre_process`

Pre-process the scene. This function is called right after the scene format loader loaded the scene and no changes have been made.

Pre-process may be used to adjust internal import options in the `"nodes"`, `"meshes"`, `"animations"` or `"materials"` keys inside `get_option_value("_subresources")`.

## InternalImportCategory

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `Node = 0`
- `Mesh3DNode = 1`
- `Mesh = 2`
- `Material = 3`
- `Animation = 4`
- `AnimationNode = 5`
- `Skeleton3DNode = 6`
- `Max = 7`
