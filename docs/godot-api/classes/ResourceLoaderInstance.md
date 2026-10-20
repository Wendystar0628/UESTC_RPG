<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ResourceLoaderInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

A singleton used to load resource files from the filesystem.

It uses the many [`ResourceFormatLoader`](ResourceFormatLoader.md) classes registered in the engine (either built-in or from a plugin) to load files into memory and convert them to a format that can be used by the engine.

**Note:** You have to import the files into the engine first to load them using `Load()`. If you want to load [`Image`](Image.md)s at run-time, you may use [`Image.Load`](Image.md). If you want to import audio files, you can use the snippet described in [`AudioStreamMP3.Data`](AudioStreamMP3.md).

**Note:** Non-resource files such as plain text files cannot be read using [`ResourceLoader`](ResourceLoader.md). Use [`FileAccess`](FileAccess.md) for those files instead, and be aware that non-resource files are not exported by default (see notes in the [`FileAccess`](FileAccess.md) class description for instructions on exporting them).

## 方法

### AddResourceFormatLoader

```csharp
public void AddResourceFormatLoader(ResourceFormatLoader formatLoader, bool atFront = false)
```

Registers a new [`ResourceFormatLoader`](ResourceFormatLoader.md). The ResourceLoader will use the ResourceFormatLoader as described in `Load()`.

This method is performed implicitly for ResourceFormatLoaders written in GDScript (see [`ResourceFormatLoader`](ResourceFormatLoader.md) for more information).

### Exists

```csharp
public bool Exists(string path, string typeHint = "")
```

Returns whether a recognized resource exists for the given `path`.

An optional `typeHint` can be used to further specify the [`Resource`](Resource.md) type that should be handled by the [`ResourceFormatLoader`](ResourceFormatLoader.md). Anything that inherits from [`Resource`](Resource.md) can be used as a type hint, for example [`Image`](Image.md).

**Note:** If you use [`Resource.TakeOverPath`](Resource.md), this method will return `true` for the taken path even if the resource wasn't saved (i.e. exists only in resource cache).

### GetCachedRef

```csharp
public Resource GetCachedRef(string path)
```

Returns the cached resource reference for the given `path`.

**Note:** If the resource is not cached, the returned [`Resource`](Resource.md) will be invalid.

### GetDependencies

```csharp
public string[] GetDependencies(string path)
```

Returns the dependencies for the resource at the given `path`.

Each dependency is a string that can be divided into sections by `::`. There can be either one section or three sections, with the second section always being empty. When there is one section, it contains the file path. When there are three sections, the first section contains the UID and the third section contains the fallback path.

```csharp
for dependency in ResourceLoader.get_dependencies(path):
if dependency.contains("::"):
print(dependency.get_slice("::", 0)) # Prints the UID.
print(dependency.get_slice("::", 2)) # Prints the fallback path.
else:
print(dependency) # Prints the path.

```

### GetRecognizedExtensionsForType

```csharp
public string[] GetRecognizedExtensionsForType(string type)
```

Returns the list of recognized extensions for a resource type.

### GetResourceUid

```csharp
public long GetResourceUid(string path)
```

Returns the ID associated with a given resource path, or `-1` when no such ID exists.

### HasCached

```csharp
public bool HasCached(string path)
```

Returns whether a cached resource is available for the given `path`.

Once a resource has been loaded by the engine, it is cached in memory for faster access, and future calls to the `Load()` method will use the cached version. The cached resource can be overridden by using [`Resource.TakeOverPath`](Resource.md) on a new resource for that same path.

### ListDirectory

```csharp
public string[] ListDirectory(string directoryPath)
```

Lists a directory, returning all resources and subdirectories contained within. The resource files have the original file names as visible in the editor before exporting. The directories have `"/"` appended.

```csharp
# Prints ["extra_data/", "model.gltf", "model.tscn", "model_slime.png"]
print(ResourceLoader.list_directory("res://assets/enemies/slime"))

```

**Note:** The order of files and directories returned by this method is not deterministic, and can vary between operating systems.

**Note:** To normally traverse the filesystem, see [`DirAccess`](DirAccess.md).

### Load

```csharp
public Resource Load(string path, string typeHint = "", ResourceLoader.CacheMode cacheMode = ResourceLoader.CacheMode.Reuse)
```

Loads a resource at the given `path`, caching the result for further access.

The registered [`ResourceFormatLoader`](ResourceFormatLoader.md)s are queried sequentially to find the first one which can handle the file's extension, and then attempt loading. If loading fails, the remaining ResourceFormatLoaders are also attempted.

An optional `typeHint` can be used to further specify the [`Resource`](Resource.md) type that should be handled by the [`ResourceFormatLoader`](ResourceFormatLoader.md). Anything that inherits from [`Resource`](Resource.md) can be used as a type hint, for example [`Image`](Image.md).

The `cacheMode` property defines whether and how the cache should be used or updated when loading the resource.

Returns an empty resource if no [`ResourceFormatLoader`](ResourceFormatLoader.md) could handle the file, and prints an error if no file is found at the specified path.

GDScript has a simplified `@GDScript.load` built-in method which can be used in most situations, leaving the use of [`ResourceLoader`](ResourceLoader.md) for more advanced scenarios.

**Note:** If `ProjectSettings.editor/export/convert_text_resources_to_binary` is `true`, `@GDScript.load` will not be able to read converted files in an exported project. If you rely on run-time loading of files present within the PCK, set `ProjectSettings.editor/export/convert_text_resources_to_binary` to `false`.

**Note:** Relative paths will be prefixed with `"res://"` before loading, to avoid unexpected results make sure your paths are absolute.

### LoadThreadedGet

```csharp
public Resource LoadThreadedGet(string path)
```

Returns the resource loaded by `LoadThreadedRequest()`.

If this is called before the loading thread is done (i.e. `LoadThreadedGetStatus()` is not [`ResourceLoader.ThreadLoadStatus.Loaded`](ResourceLoader.md)), the calling thread will be blocked until the resource has finished loading. However, it's recommended to use `LoadThreadedGetStatus()` to known when the load has actually completed.

### LoadThreadedGetStatus

```csharp
public ResourceLoader.ThreadLoadStatus LoadThreadedGetStatus(string path, Array progress = null)
```

Returns the status of a threaded loading operation started with `LoadThreadedRequest()` for the resource at `path`.

An array variable can optionally be passed via `progress`, and will return a one-element array containing the ratio of completion of the threaded loading (between `0.0` and `1.0`).

**Note:** The recommended way of using this method is to call it during different frames (e.g., in [`Node._Process()`](Node.md), instead of a loop).

### LoadThreadedRequest

```csharp
public Error LoadThreadedRequest(string path, string typeHint = "", bool useSubThreads = false, ResourceLoader.CacheMode cacheMode = ResourceLoader.CacheMode.Reuse)
```

Loads the resource using threads. If `useSubThreads` is `true`, multiple threads will be used to load the resource, which makes loading faster, but may affect the main thread (and thus cause game slowdowns).

The `cacheMode` parameter defines whether and how the cache should be used or updated when loading the resource.

### RemoveResourceFormatLoader

```csharp
public void RemoveResourceFormatLoader(ResourceFormatLoader formatLoader)
```

Unregisters the given [`ResourceFormatLoader`](ResourceFormatLoader.md).

### SetAbortOnMissingResources

```csharp
public void SetAbortOnMissingResources(bool abort)
```

Changes the behavior on missing sub-resources. The default behavior is to abort loading.
