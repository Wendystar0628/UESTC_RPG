<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ResourceSaverInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

A singleton for saving resource types to the filesystem.

It uses the many [`ResourceFormatSaver`](ResourceFormatSaver.md) classes registered in the engine (either built-in or from a plugin) to save resource data to text-based (e.g. `.tres` or `.tscn`) or binary files (e.g. `.res` or `.scn`).

## 方法

### AddResourceFormatSaver

```csharp
public void AddResourceFormatSaver(ResourceFormatSaver formatSaver, bool atFront = false)
```

Registers a new [`ResourceFormatSaver`](ResourceFormatSaver.md). The ResourceSaver will use the ResourceFormatSaver as described in `Save()`.

This method is performed implicitly for ResourceFormatSavers written in GDScript (see [`ResourceFormatSaver`](ResourceFormatSaver.md) for more information).

### GetRecognizedExtensions

```csharp
public string[] GetRecognizedExtensions(Resource type)
```

Returns the list of extensions available for saving a resource of a given type.

### GetResourceIdForPath

```csharp
public long GetResourceIdForPath(string path, bool generate = false)
```

Returns the resource ID for the given path. If `generate` is `true`, a new resource ID will be generated if one for the path is not found. If `generate` is `false` and the path is not found, [`ResourceUid.InvalidId`](ResourceUid.md) is returned.

### RemoveResourceFormatSaver

```csharp
public void RemoveResourceFormatSaver(ResourceFormatSaver formatSaver)
```

Unregisters the given [`ResourceFormatSaver`](ResourceFormatSaver.md).

### Save

```csharp
public Error Save(Resource resource, string path = "", ResourceSaver.SaverFlags flags = ResourceSaver.SaverFlags.None)
```

Saves a resource to disk to the given path, using a [`ResourceFormatSaver`](ResourceFormatSaver.md) that recognizes the resource object. If `path` is empty, [`ResourceSaver`](ResourceSaver.md) will try to use [`Resource.ResourcePath`](Resource.md).

The `flags` bitmask can be specified to customize the save behavior.

Returns [`Error.Ok`](Error.md) on success.

**Note:** When the project is running, any generated UID associated with the resource will not be saved as the required code is only executed in editor mode.

### SetUid

```csharp
public Error SetUid(string resource, long uid)
```

Sets the UID of the given `resource` path to `uid`. You can generate a new UID using [`ResourceUid.CreateId`](ResourceUid.md).

Since resources will normally get a UID automatically, this method is only useful in very specific cases.
