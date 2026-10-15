<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ResourceFormatSaver

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The engine can save resources when you do it from the editor, or when you use the [`ResourceSaver`](ResourceSaver.md) singleton. This is accomplished thanks to multiple `ResourceFormatSaver`s, each handling its own format and called automatically by the engine.

By default, Godot saves resources as `.tres` (text-based), `.res` (binary) or another built-in format, but you can choose to create your own format by extending this class. Be sure to respect the documented return types and values. You should give it a global class name with `class_name` for it to be registered. Like built-in ResourceFormatSavers, it will be called automatically when saving resources of its recognized type(s). You may also implement a [`ResourceFormatLoader`](ResourceFormatLoader.md).

## 构造函数

### ResourceFormatSaver

```csharp
public ResourceFormatSaver()
```

## 方法

### _GetRecognizedExtensions

```csharp
public virtual string[] _GetRecognizedExtensions(Resource resource)
```

引擎名：`_get_recognized_extensions`

Returns the list of extensions available for saving the resource object, provided it is recognized (see `_Recognize()`).

### _Recognize

```csharp
public virtual bool _Recognize(Resource resource)
```

引擎名：`_recognize`

Returns whether the given resource object can be saved by this saver.

### _RecognizePath

```csharp
public virtual bool _RecognizePath(Resource resource, string path)
```

引擎名：`_recognize_path`

Returns `true` if this saver handles a given save path and `false` otherwise.

If this method is not implemented, the default behavior returns whether the path's extension is within the ones provided by `_GetRecognizedExtensions()`.

### _Save

```csharp
public virtual Error _Save(Resource resource, string path, uint flags)
```

引擎名：`_save`

Saves the given resource object to a file at the target `path`. `flags` is a bitmask composed with [`ResourceSaver.SaverFlags`](ResourceSaver.md) constants.

Returns [`Error.Ok`](Error.md) on success, or an [`Error`](Error.md) constant in case of failure.

### _SetUid

```csharp
public virtual Error _SetUid(string path, long uid)
```

引擎名：`_set_uid`

Sets a new UID for the resource at the given `path`. Returns [`Error.Ok`](Error.md) on success, or an [`Error`](Error.md) constant in case of failure.
