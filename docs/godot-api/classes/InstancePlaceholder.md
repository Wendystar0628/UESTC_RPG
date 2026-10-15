<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InstancePlaceholder

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Turning on the option **Load As Placeholder** for an instantiated scene in the editor causes it to be replaced by an `InstancePlaceholder` when running the game, this will not replace the node in the editor. This makes it possible to delay actually loading the scene until calling `CreateInstance()`. This is useful to avoid loading large scenes all at once by loading parts of it selectively.

**Note:** Like [`Node`](Node.md), `InstancePlaceholder` does not have a transform. This causes any child nodes to be positioned relatively to the [`Viewport`](Viewport.md) origin, rather than their parent as displayed in the editor. Replacing the placeholder with a scene with a transform will transform children relatively to their parent again.

## 方法

### CreateInstance

```csharp
public Node CreateInstance(bool replace = false, PackedScene customScene = null)
```

引擎名：`create_instance`

Call this method to actually load in the node. The created node will be placed as a sibling *above* the `InstancePlaceholder` in the scene tree. The [`Node`](Node.md)'s reference is also returned for convenience.

**Note:** `CreateInstance()` is not thread-safe. Use [`GodotObject.CallDeferred`](GodotObject.md) if calling from a thread.

### GetInstancePath

```csharp
public string GetInstancePath()
```

引擎名：`get_instance_path`

Gets the path to the [`PackedScene`](PackedScene.md) resource file that is loaded by default when calling `CreateInstance()`. Not thread-safe. Use [`GodotObject.CallDeferred`](GodotObject.md) if calling from a thread.

### GetStoredValues

```csharp
public Dictionary GetStoredValues(bool withOrder = false)
```

引擎名：`get_stored_values`

Returns the list of properties that will be applied to the node when `CreateInstance()` is called.

If `withOrder` is `true`, a key named `.order` (note the leading period) is added to the dictionary. This `.order` key is an [`Collections.Array`](Collections.Array.md) of `System.String` property names specifying the order in which properties will be applied (with index 0 being the first).
