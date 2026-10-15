<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ResourceFormatLoader

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Godot loads resources in the editor or in exported games using ResourceFormatLoaders. They are queried automatically via the [`ResourceLoader`](ResourceLoader.md) singleton, or when a resource with internal dependencies is loaded. Each file type may load as a different resource type, so multiple ResourceFormatLoaders are registered in the engine.

Extending this class allows you to define your own loader. Be sure to respect the documented return types and values. You should give it a global class name with `class_name` for it to be registered. Like built-in ResourceFormatLoaders, it will be called automatically when loading resources of its handled type(s). You may also implement a [`ResourceFormatSaver`](ResourceFormatSaver.md).

**Note:** You can also extend `EditorImportPlugin` if the resource type you need exists but Godot is unable to load its format. Choosing one way over another depends on if the format is suitable or not for the final exported game. For example, it's better to import `.png` textures as `.ctex` ([`CompressedTexture2D`](CompressedTexture2D.md)) first, so they can be loaded with better efficiency on the graphics card.

## 构造函数

### ResourceFormatLoader

```csharp
public ResourceFormatLoader()
```

## 方法

### _Exists

```csharp
public virtual bool _Exists(string path)
```

引擎名：`_exists`

### _GetClassesUsed

```csharp
public virtual string[] _GetClassesUsed(string path)
```

引擎名：`_get_classes_used`

### _GetDependencies

```csharp
public virtual string[] _GetDependencies(string path, bool addTypes)
```

引擎名：`_get_dependencies`

Should return the dependencies for the resource at the given `path`. Each dependency is a string composed of one to three sections separated by `::`, with trailing empty sections omitted:

- The first section should contain the UID if the resource has one. Otherwise, it should contain the file path.

- The second section should contain the class name of the dependency if `addTypes` is `true`. Otherwise, it should be empty.

- The third section should contain the fallback path if the resource has a UID. Otherwise, it should be empty.

```csharp
func _get_dependencies(path, add_types):
return [
"uid://fqgvuwrkuixh::Script::res://script.gd",
"uid://fqgvuwrkuixh::::res://script.gd",
"res://script.gd::Script",
"res://script.gd",
]

```

**Note:** Custom resource types defined by scripts aren't known by the [`ClassDB`](ClassDB.md), so `"Resource"` can be used for the class name.

### _GetRecognizedExtensions

```csharp
public virtual string[] _GetRecognizedExtensions()
```

引擎名：`_get_recognized_extensions`

Gets the list of extensions for files this loader is able to read.

### _GetResourceScriptClass

```csharp
public virtual string _GetResourceScriptClass(string path)
```

引擎名：`_get_resource_script_class`

Returns the script class name associated with the [`Resource`](Resource.md) under the given `path`. If the resource has no script or the script isn't a named class, it should return `""`.

### _GetResourceType

```csharp
public virtual string _GetResourceType(string path)
```

引擎名：`_get_resource_type`

Gets the class name of the resource associated with the given path. If the loader cannot handle it, it should return `""`.

**Note:** Custom resource types defined by scripts aren't known by the [`ClassDB`](ClassDB.md), so you might just return `"Resource"` for them.

### _GetResourceUid

```csharp
public virtual long _GetResourceUid(string path)
```

引擎名：`_get_resource_uid`

Should return the unique ID for the resource associated with the given path. If this method is not overridden, a `.uid` file is generated along with the resource file, containing the unique ID.

### _HandlesType

```csharp
public virtual bool _HandlesType(StringName type)
```

引擎名：`_handles_type`

Tells which resource class this loader can load.

**Note:** Custom resource types defined by scripts aren't known by the [`ClassDB`](ClassDB.md), so you might just handle `"Resource"` for them.

### _Load

```csharp
public virtual Variant _Load(string path, string originalPath, bool useSubThreads, int cacheMode)
```

引擎名：`_load`

Loads a resource when the engine finds this loader to be compatible. If the loaded resource is the result of an import, `originalPath` will target the source file. Returns a [`Resource`](Resource.md) object on success, or an [`Error`](Error.md) constant in case of failure.

The `cacheMode` property defines whether and how the cache should be used or updated when loading the resource. See [`ResourceFormatLoader.CacheMode`](ResourceFormatLoader.md) for details.

### _RecognizePath

```csharp
public virtual bool _RecognizePath(string path, StringName type)
```

引擎名：`_recognize_path`

Tells whether or not this loader should load a resource from its resource path for a given type.

If it is not implemented, the default behavior returns whether the path's extension is within the ones provided by `_GetRecognizedExtensions()`, and if the type is within the ones provided by `_GetResourceType()`.

### _RenameDependencies

```csharp
public virtual Error _RenameDependencies(string path, Dictionary renames)
```

引擎名：`_rename_dependencies`

If implemented, renames dependencies within the given resource and saves it. `renames` is a dictionary `{ String => String }` mapping old dependency paths to new paths.

Returns [`Error.Ok`](Error.md) on success, or an [`Error`](Error.md) constant in case of failure.

## CacheMode

命名空间 `Godot`；enum

### 枚举值

- `Ignore = 0`：Neither the main resource (the one requested to be loaded) nor any of its subresources are retrieved from cache nor stored into it.
- `Reuse = 1`：The main resource (the one requested to be loaded), its subresources, and its dependencies (external resources) are retrieved from cache if present, instead of loaded.
- `Replace = 2`：Like `Reuse`, but the cache is checked for the main resource (the one requested to be loaded) as well as for each of its subresources.
- `IgnoreDeep = 3`：Like `Ignore`, but propagated recursively down the tree of dependencies (external resources).
- `ReplaceDeep = 4`：Like `Replace`, but propagated recursively down the tree of dependencies (external resources).
