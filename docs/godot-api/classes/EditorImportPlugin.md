<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorImportPlugin

命名空间 `Godot`；class；继承 [`ResourceImporter`](ResourceImporter.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

`EditorImportPlugin`s provide a way to extend the editor's resource import functionality. Use them to import resources from custom files or to provide alternatives to the editor's existing importers.

EditorImportPlugins work by associating with specific file extensions and a resource type. See `_GetRecognizedExtensions()` and `_GetResourceType()`. They may optionally specify some import presets that affect the import process. EditorImportPlugins are responsible for creating the resources and saving them in the `.godot/imported` directory (see `ProjectSettings.application/config/use_hidden_project_data_directory`).

Below is an example EditorImportPlugin that imports a [`Mesh`](Mesh.md) from a file with the extension ".special" or ".spec":

```csharp
using Godot;

public partial class MySpecialPlugin : EditorImportPlugin
{
public override string _GetImporterName()
{
return "my.special.plugin";
}

public override string _GetVisibleName()
{
return "Special Mesh";
}

public override string[] _GetRecognizedExtensions()
{
return ["special", "spec"];
}

public override string _GetSaveExtension()
{
return "mesh";
}

public override string _GetResourceType()
{
return "Mesh";
}

public override int _GetPresetCount()
{
return 1;
}

public override string _GetPresetName(int presetIndex)
{
return "Default";
}

public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetImportOptions(string path, int presetIndex)
{
return
[
new Godot.Collections.Dictionary
{
{ "name", "myOption" },
{ "default_value", false },
},
];
}

public override Error _Import(string sourceFile, string savePath, Godot.Collections.Dictionary options, Godot.Collections.Array<string> platformVariants, Godot.Collections.Array<string> genFiles)
{
using var file = FileAccess.Open(sourceFile, FileAccess.ModeFlags.Read);
if (file.GetError() != Error.Ok)
{
return Error.Failed;
}

var mesh = new ArrayMesh();
// Fill the Mesh with data read in "file", left as an exercise to the reader.
string filename = $"{savePath}.{_GetSaveExtension()}";
return ResourceSaver.Save(mesh, filename);
}
}

```

To use `EditorImportPlugin`, register it using the [`EditorPlugin.AddImportPlugin`](EditorPlugin.md) method first.

## 构造函数

### EditorImportPlugin

```csharp
public EditorImportPlugin()
```

## 方法

### AppendImportExternalResource

```csharp
public Error AppendImportExternalResource(string path, Dictionary customOptions = null, string customImporter = "", Variant generatorParameters = default)
```

引擎名：`append_import_external_resource`

This function can only be called during the `_Import()` callback and it allows manually importing resources from it. This is useful when the imported file generates external resources that require importing (as example, images). Custom parameters for the ".import" file can be passed via the `customOptions`. Additionally, in cases where multiple importers can handle a file, the `customImporter` can be specified to force a specific one. This function performs a resource import and returns immediately with a success or error code. `generatorParameters` defines optional extra metadata which will be stored as `generator_parameters` in the `remap` section of the `.import` file, for example to store a md5 hash of the source data.

### _CanImportThreaded

```csharp
public virtual bool _CanImportThreaded()
```

引擎名：`_can_import_threaded`

Tells whether this importer can be run in parallel on threads, or, on the contrary, it's only safe for the editor to call it from the main thread, for one file at a time.

If this importer's implementation is thread-safe and can be run in parallel, override this with `true` to optimize for concurrency.

If not overridden, returns `false`.

### _GetFormatVersion

```csharp
public virtual int _GetFormatVersion()
```

引擎名：`_get_format_version`

Gets the format version of this importer. Increment this version when making incompatible changes to the format of the imported resources.

If not overridden, the format version is `0`.

### _GetImportOptions

```csharp
public virtual Array<Dictionary> _GetImportOptions(string path, int presetIndex)
```

引擎名：`_get_import_options`

Gets the options and default values for the preset at this index. Returns an Array of Dictionaries with the following keys: `name`, `default_value`, `property_hint` (optional), `hint_string` (optional), `usage` (optional).

### _GetImportOrder

```csharp
public virtual int _GetImportOrder()
```

引擎名：`_get_import_order`

Gets the order of this importer to be run when importing resources. Importers with *lower* import orders will be called first, and higher values will be called later. Use this to ensure the importer runs after the dependencies are already imported. The default import order is `0` unless overridden by a specific importer. See [`ResourceImporter.ImportOrder`](ResourceImporter.md) for some predefined values.

### _GetImporterName

```csharp
public virtual string _GetImporterName()
```

引擎名：`_get_importer_name`

Gets the unique name of the importer.

### _GetOptionVisibility

```csharp
public virtual bool _GetOptionVisibility(string path, StringName optionName, Dictionary options)
```

引擎名：`_get_option_visibility`

Gets whether the import option specified by `optionName` should be visible in the Import dock. The default implementation always returns `true`, making all options visible. This is mainly useful for hiding options that depend on others if one of them is disabled.

```csharp
public override bool _GetOptionVisibility(string path, StringName optionName, Godot.Collections.Dictionary options)
{
// Only show the lossy quality setting if the compression mode is set to "Lossy".
if (optionName == "compress/lossy_quality" && options.ContainsKey("compress/mode"))
{
return (int)options["compress/mode"] == CompressLossy; // This is a constant you set
}

return true;
}

```

### _GetPresetCount

```csharp
public virtual int _GetPresetCount()
```

引擎名：`_get_preset_count`

Gets the number of initial presets defined by the plugin. Use `_GetImportOptions()` to get the default options for the preset and `_GetPresetName()` to get the name of the preset.

By default, there are no presets.

### _GetPresetName

```csharp
public virtual string _GetPresetName(int presetIndex)
```

引擎名：`_get_preset_name`

Gets the name of the options preset at this index.

### _GetPriority

```csharp
public virtual float _GetPriority()
```

引擎名：`_get_priority`

Gets the priority of this plugin for the recognized extension. Higher priority plugins will be preferred. The default priority is `1.0`.

### _GetRecognizedExtensions

```csharp
public virtual string[] _GetRecognizedExtensions()
```

引擎名：`_get_recognized_extensions`

Gets the list of file extensions to associate with this loader (case-insensitive). e.g. `["obj"]`.

### _GetResourceType

```csharp
public virtual string _GetResourceType()
```

引擎名：`_get_resource_type`

Gets the Godot resource type associated with this loader. e.g. `"Mesh"` or `"Animation"`.

### _GetSaveExtension

```csharp
public virtual string _GetSaveExtension()
```

引擎名：`_get_save_extension`

Gets the extension used to save this resource in the `.godot/imported` directory (see `ProjectSettings.application/config/use_hidden_project_data_directory`).

### _GetVisibleName

```csharp
public virtual string _GetVisibleName()
```

引擎名：`_get_visible_name`

Gets the name to display in the import window. You should choose this name as a continuation to "Import as", e.g. "Import as Special Mesh".

### _Import

```csharp
public virtual Error _Import(string sourceFile, string savePath, Dictionary options, Array<string> platformVariants, Array<string> genFiles)
```

引擎名：`_import`

Imports `sourceFile` with the import `options` specified. Should return [`Error.Ok`](Error.md) if the import is successful, other values indicate failure.

The imported resource is expected to be saved to `save_path + "." + _get_save_extension()`. If a different variant is preferred for a [feature tag]($DOCS_URL/tutorials/export/feature_tags.html), save the variant to `save_path + "." + tag + "." + _get_save_extension()` and add the feature tag to `platformVariants`.

If additional resource files are generated in the resource filesystem (`res://`), add their full path to `genFiles` so that the editor knows they depend on `sourceFile`.

This method must be overridden to do the actual importing work. See this class' description for an example of overriding this method.
