<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ResourceUidInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

Resource UIDs (Unique IDentifiers) allow the engine to keep references between resources intact, even if files are renamed or moved. They can be accessed with `uid://`.

[`ResourceUid`](ResourceUid.md) keeps track of all registered resource UIDs in a project, generates new UIDs, and converts between their string and integer representations.

## 方法

### AddId

```csharp
public void AddId(long id, string path)
```

Adds a new UID value which is mapped to the given resource path.

Fails with an error if the UID already exists, so be sure to check `HasId()` beforehand, or use `SetId()` instead.

### CreateId

```csharp
public long CreateId()
```

Generates a random resource UID which is guaranteed to be unique within the list of currently loaded UIDs.

In order for this UID to be registered, you must call `AddId()` or `SetId()`.

### CreateIdForPath

```csharp
public long CreateIdForPath(string path)
```

Like `CreateId()`, but the UID is seeded with the provided `path` and project name. UIDs generated for that path will be always the same within the current project.

### EnsurePath

```csharp
public static string EnsurePath(string pathOrUid)
```

Returns a path, converting `pathOrUid` if necessary. Fails and returns an empty string if an invalid UID is provided.

### GetIdPath

```csharp
public string GetIdPath(long id)
```

Returns the path that the given UID value refers to.

Fails with an error if the UID does not exist, so be sure to check `HasId()` beforehand.

### HasId

```csharp
public bool HasId(long id)
```

Returns whether the given UID value is known to the cache.

### IdToText

```csharp
public string IdToText(long id)
```

Converts the given UID to a `uid://` string value.

### PathToUid

```csharp
public static string PathToUid(string path)
```

Converts the provided resource `path` to a UID. Returns the unchanged path if it has no associated UID.

### RemoveId

```csharp
public void RemoveId(long id)
```

Removes a loaded UID value from the cache.

Fails with an error if the UID does not exist, so be sure to check `HasId()` beforehand.

### SetId

```csharp
public void SetId(long id, string path)
```

Updates the resource path of an existing UID.

Fails with an error if the UID does not exist, so be sure to check `HasId()` beforehand, or use `AddId()` instead.

### TextToId

```csharp
public long TextToId(string textId)
```

Extracts the UID value from the given `uid://` string.

### UidToPath

```csharp
public static string UidToPath(string uid)
```

Converts the provided `uid` to a path. Prints an error if the UID is invalid.
