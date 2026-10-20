<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorSceneFormatImporter

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

`EditorSceneFormatImporter` allows to define an importer script for a third-party 3D format.

To use `EditorSceneFormatImporter`, register it using the [`EditorPlugin.AddSceneFormatImporterPlugin`](EditorPlugin.md) method first.

## 构造函数

### EditorSceneFormatImporter

```csharp
public EditorSceneFormatImporter()
```

## 方法

### AddImportOption

```csharp
public void AddImportOption(string name, Variant value)
```

引擎名：`add_import_option`

Add a specific import option (name and default value only). This function can only be called from `_GetImportOptions()`.

### AddImportOptionAdvanced

```csharp
public void AddImportOptionAdvanced(Variant.Type type, string name, Variant defaultValue, PropertyHint hint = PropertyHint.None, string hintString = "", int usageFlags = 6)
```

引擎名：`add_import_option_advanced`

Add a specific import option. This function can only be called from `_GetImportOptions()`.

### _GetExtensions

```csharp
public virtual string[] _GetExtensions()
```

引擎名：`_get_extensions`

Return supported file extensions for this scene importer.

### _GetImportOptions

```csharp
public virtual void _GetImportOptions(string path)
```

引擎名：`_get_import_options`

Override to add general import options. These will appear in the main import dock on the editor. Add options via `AddImportOption()` and `AddImportOptionAdvanced()`.

**Note:** All `EditorSceneFormatImporter` and [`EditorScenePostImportPlugin`](EditorScenePostImportPlugin.md) instances will add options for all files. It is good practice to check the file extension when `path` is non-empty.

When the user is editing project settings, `path` will be empty. It is recommended to add all options when `path` is empty to allow the user to customize Import Defaults.

### _GetOptionVisibility

```csharp
public virtual Variant _GetOptionVisibility(string path, bool forAnimation, string option)
```

引擎名：`_get_option_visibility`

Should return `true` to show the given option, `false` to hide the given option, or `null` to ignore.

### _ImportScene

```csharp
public virtual GodotObject _ImportScene(string path, uint flags, Dictionary options)
```

引擎名：`_import_scene`

Perform the bulk of the scene import logic here, for example using [`GltfDocument`](GltfDocument.md) or [`FbxDocument`](FbxDocument.md).

## ImportFlags

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `Scene = 1`：Unused flag (this has no effect when enabled).
- `Animation = 2`：Import animations from the 3D scene.
- `FailOnMissingDependencies = 4`：Unused flag (this has no effect when enabled).
- `GenerateTangentArrays = 8`：If `true`, generate vertex tangents using [Mikktspace](http://www.mikktspace.com/) if the input meshes don't have tangent data.
- `UseNamedSkinBinds = 16`：If checked, use named [`Skin`](Skin.md)s for animation.
- `DiscardMeshesAndMaterials = 32`：Ignore meshes and materials on import.
- `ForceDisableMeshCompression = 64`：If `true`, mesh compression will not be used.
