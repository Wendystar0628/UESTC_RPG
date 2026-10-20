<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Resource

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Resource is the base class for all Godot-specific resource types, serving primarily as data containers. Since they inherit from [`RefCounted`](RefCounted.md), resources are reference-counted and freed when no longer in use. They can also be nested within other resources, and saved on disk. [`PackedScene`](PackedScene.md), one of the most common [`GodotObject`](GodotObject.md)s in a Godot project, is also a resource, uniquely capable of storing and instantiating the [`Node`](Node.md)s it contains as many times as desired.

In GDScript, resources can loaded from disk by their `ResourcePath` using `@GDScript.load` or `@GDScript.preload`.

The engine keeps a global cache of all loaded resources, referenced by paths (see [`ResourceLoader.HasCached`](ResourceLoader.md)). A resource will be cached when loaded for the first time and removed from cache once all references are released. When a resource is cached, subsequent loads using its path will return the cached reference.

**Note:** In C#, resources will not be freed instantly after they are no longer in use. Instead, garbage collection will run periodically and will free resources that are no longer in use. This means that unused resources will remain in memory for a while before being removed.

## 构造函数

### Resource

```csharp
public Resource()
```

## 属性

### ResourceLocalToScene

```csharp
public bool ResourceLocalToScene { get; set; }
```

引擎名：`resource_local_to_scene`

If `true`, the resource is duplicated for each instance of all scenes using it. At run-time, the resource can be modified in one scene without affecting other instances (see [`PackedScene.Instantiate`](PackedScene.md)).

**Note:** Changing this property at run-time has no effect on already created duplicate resources.

### ResourceName

```csharp
public string ResourceName { get; set; }
```

引擎名：`resource_name`

An optional name for this resource. When defined, its value is displayed to represent the resource in the Inspector dock. For built-in scripts, the name is displayed as part of the tab name in the script editor.

**Note:** Some resource formats do not support resource names. You can still set the name in the editor or via code, but it will be lost when the resource is reloaded. For example, only built-in scripts can have a resource name, while scripts stored in separate files cannot.

### ResourcePath

```csharp
public string ResourcePath { get; set; }
```

引擎名：`resource_path`

The unique path to this resource. If it has been saved to disk, the value will be its filepath. If the resource is exclusively contained within a scene, the value will be the [`PackedScene`](PackedScene.md)'s filepath, followed by a unique identifier.

**Note:** Setting this property manually may fail if a resource with the same path has already been previously loaded. If necessary, use `TakeOverPath()`.

### ResourceSceneUniqueId

```csharp
public string ResourceSceneUniqueId { get; set; }
```

引擎名：`resource_scene_unique_id`

A unique identifier relative to this resource's scene. If left empty, the ID is automatically generated when this resource is saved inside a [`PackedScene`](PackedScene.md). If the resource is not inside a scene, this property is empty by default.

**Note:** When the [`PackedScene`](PackedScene.md) is saved, if multiple resources in the same scene use the same ID, only the earliest resource in the scene hierarchy keeps the original ID. The other resources are assigned new IDs from `GenerateSceneUniqueId()`.

**Note:** Setting this property does not emit the `Changed` signal.

**Warning:** When setting, the ID must only consist of letters, numbers, and underscores. Otherwise, it will fail and default to a randomly generated ID.

## 方法

### CopyFromResource

```csharp
public Error CopyFromResource(Resource resource)
```

引擎名：`copy_from_resource`

Copies the data from `resource` into this resource. Both resources must share the same class.

### Duplicate

```csharp
public Resource Duplicate(bool deep = false)
```

引擎名：`duplicate`

Duplicates this resource, returning a new resource with its `export`ed or [`PropertyUsageFlags.Storage`](PropertyUsageFlags.md) properties copied from the original.

If `deep` is `false`, a **shallow** copy is returned: nested [`Collections.Array`](Collections.Array.md), [`Collections.Dictionary`](Collections.Dictionary.md), and `Resource` properties are not duplicated and are shared with the original resource.

If `deep` is `true`, a **deep** copy is returned: all nested arrays, dictionaries, and packed arrays are also duplicated (recursively). Any `Resource` found inside will only be duplicated if it's local, like [`Resource.DeepDuplicateMode.Internal`](Resource.md) used with `DuplicateDeep()`.

The following exceptions apply:

- Subresource properties with the [`PropertyUsageFlags.AlwaysDuplicate`](PropertyUsageFlags.md) flag are always duplicated (recursively or not, depending on `deep`).

- Subresource properties with the [`PropertyUsageFlags.NeverDuplicate`](PropertyUsageFlags.md) flag are never duplicated.

**Note:** For custom resources, this method will fail if [`GodotObject`](GodotObject.md) has been defined with required parameters.

**Note:** When duplicating with `deep` set to `true`, each resource found, including the one on which this method is called, will be only duplicated once and referenced as many times as needed in the duplicate. For instance, if you are duplicating resource A that happens to have resource B referenced twice, you'll get a new resource A' referencing a new resource B' twice.

### DuplicateDeep

```csharp
public Resource DuplicateDeep(Resource.DeepDuplicateMode deepSubresourcesMode = Resource.DeepDuplicateMode.Internal)
```

引擎名：`duplicate_deep`

Duplicates this resource, deeply, like `Duplicate()` when passing `true`, with extra control over how subresources are handled.

### EmitChanged

```csharp
public void EmitChanged()
```

引擎名：`emit_changed`

Emits the `Changed` signal. This method is called automatically for some built-in resources.

**Note:** For custom resources, it's recommended to call this method whenever a meaningful change occurs, such as a modified property. This ensures that custom [`GodotObject`](GodotObject.md)s depending on the resource are properly updated.

```csharp
var damage:
set(new_value):
if damage != new_value:
damage = new_value
emit_changed()

```

### GenerateSceneUniqueId

```csharp
public static string GenerateSceneUniqueId()
```

引擎名：`generate_scene_unique_id`

Generates a unique identifier for a resource to be contained inside a [`PackedScene`](PackedScene.md), based on the current date, time, and a random value. The returned string is only composed of letters (`a` to `y`) and numbers (`0` to `8`). See also `ResourceSceneUniqueId`.

### GetIdForPath

```csharp
public string GetIdForPath(string path)
```

引擎名：`get_id_for_path`

From the internal cache for scene-unique IDs, returns the ID of this resource for the scene at `path`. If there is no entry, an empty string is returned. Useful to keep scene-unique IDs the same when implementing a VCS-friendly custom resource format by extending [`ResourceFormatLoader`](ResourceFormatLoader.md) and [`ResourceFormatSaver`](ResourceFormatSaver.md).

**Note:** This method is only implemented when running in an editor context. At runtime, it returns an empty string.

### GetLocalScene

```csharp
public Node GetLocalScene()
```

引擎名：`get_local_scene`

If `ResourceLocalToScene` is set to `true` and the resource has been loaded from a [`PackedScene`](PackedScene.md) instantiation, returns the root [`Node`](Node.md) of the scene where this resource is used. Otherwise, returns `null`.

### GetName

```csharp
public string GetName()
```

引擎名：`get_name`

### GetPath

```csharp
public string GetPath()
```

引擎名：`get_path`

### GetRid

```csharp
public Rid GetRid()
```

引擎名：`get_rid`

Returns the [`Rid`](Rid.md) of this resource (or an empty RID). Many resources (such as [`Texture2D`](Texture2D.md), [`Mesh`](Mesh.md), and so on) are high-level abstractions of resources stored in a specialized server ([`DisplayServer`](DisplayServer.md), [`RenderingServer`](RenderingServer.md), etc.), so this function will return the original [`Rid`](Rid.md).

### GetSceneUniqueId

```csharp
public string GetSceneUniqueId()
```

引擎名：`get_scene_unique_id`

### IsBuiltIn

```csharp
public bool IsBuiltIn()
```

引擎名：`is_built_in`

Returns `true` if the resource is saved on disk as a part of another resource's file.

### IsLocalToScene

```csharp
public bool IsLocalToScene()
```

引擎名：`is_local_to_scene`

### ResetState

```csharp
public void ResetState()
```

引擎名：`reset_state`

Makes the resource clear its non-exported properties. See also `_ResetState()`. Useful when implementing a custom resource format by extending [`ResourceFormatLoader`](ResourceFormatLoader.md) and [`ResourceFormatSaver`](ResourceFormatSaver.md).

### SetIdForPath

```csharp
public void SetIdForPath(string path, string id)
```

引擎名：`set_id_for_path`

In the internal cache for scene-unique IDs, sets the ID of this resource to `id` for the scene at `path`. If `id` is empty, the cache entry for `path` is cleared. Useful to keep scene-unique IDs the same when implementing a VCS-friendly custom resource format by extending [`ResourceFormatLoader`](ResourceFormatLoader.md) and [`ResourceFormatSaver`](ResourceFormatSaver.md).

**Note:** This method is only implemented when running in an editor context.

### SetLocalToScene

```csharp
public void SetLocalToScene(bool enable)
```

引擎名：`set_local_to_scene`

### SetName

```csharp
public void SetName(string name)
```

引擎名：`set_name`

### SetPath

```csharp
public void SetPath(string path)
```

引擎名：`set_path`

### SetPathCache

```csharp
public void SetPathCache(string path)
```

引擎名：`set_path_cache`

Sets the resource's path to `path` without involving the resource cache. Useful for handling [`ResourceFormatLoader.CacheMode`](ResourceFormatLoader.md) values when implementing a custom resource format by extending [`ResourceFormatLoader`](ResourceFormatLoader.md) and [`ResourceFormatSaver`](ResourceFormatSaver.md).

### SetSceneUniqueId

```csharp
public void SetSceneUniqueId(string id)
```

引擎名：`set_scene_unique_id`

### SetupLocalToScene

```csharp
public void SetupLocalToScene()
```

引擎名：`setup_local_to_scene`

Calls `_SetupLocalToScene()`. If `ResourceLocalToScene` is set to `true`, this method is automatically called from [`PackedScene.Instantiate`](PackedScene.md) by the newly duplicated resource within the scene instance.

### TakeOverPath

```csharp
public void TakeOverPath(string path)
```

引擎名：`take_over_path`

Sets the `ResourcePath` to `path`, potentially overriding an existing cache entry for this path. Further attempts to load an overridden resource by path will instead return this resource.

### _GetRid

```csharp
public virtual Rid _GetRid()
```

引擎名：`_get_rid`

Override this method to return a custom [`Rid`](Rid.md) when `GetRid()` is called.

### _ResetState

```csharp
public virtual void _ResetState()
```

引擎名：`_reset_state`

For resources that store state in non-exported properties, such as via [`GodotObject._ValidateProperty()`](GodotObject.md) or [`GodotObject.GetPropertyList`](GodotObject.md), this method must be implemented to clear them.

### _SetPathCache

```csharp
public virtual void _SetPathCache(string path)
```

引擎名：`_set_path_cache`

Override this method to execute additional logic after `SetPathCache()` is called on this object.

### _SetupLocalToScene

```csharp
public virtual void _SetupLocalToScene()
```

引擎名：`_setup_local_to_scene`

Override this method to customize the newly duplicated resource created from [`PackedScene.Instantiate`](PackedScene.md), if the original's `ResourceLocalToScene` is set to `true`.

**Example:** Set a random `damage` value to every local resource from an instantiated scene:

```csharp
extends Resource

var damage = 0

func _setup_local_to_scene():
damage = randi_range(10, 40)

```

## 事件（信号）

### Changed

```csharp
public event Action Changed
```

引擎名：`changed`

Emitted when the resource changes, usually when one of its properties is modified. See also `EmitChanged()`.

**Note:** This signal is not emitted automatically for properties of custom resources. If necessary, a setter needs to be created to emit the signal.

### SetupLocalToSceneRequested

```csharp
public event Action SetupLocalToSceneRequested
```

引擎名：`setup_local_to_scene_requested`

Emitted by a newly duplicated resource with `ResourceLocalToScene` set to `true`.

## DeepDuplicateMode

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：No subresources at all are duplicated.
- `Internal = 1`：Only subresources without a path or with a scene-local path will be duplicated.
- `All = 2`：Every subresource found will be duplicated, even if it has a non-local path.
