<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorFileSystemImportFormatSupportQuery

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This class is used to query and configure a certain import format. It is used in conjunction with asset format import plugins.

## 构造函数

### EditorFileSystemImportFormatSupportQuery

```csharp
public EditorFileSystemImportFormatSupportQuery()
```

## 方法

### _GetFileExtensions

```csharp
public virtual string[] _GetFileExtensions()
```

引擎名：`_get_file_extensions`

Return the file extensions supported.

### _IsActive

```csharp
public virtual bool _IsActive()
```

引擎名：`_is_active`

Return whether this importer is active.

### _Query

```csharp
public virtual bool _Query()
```

引擎名：`_query`

Query support. Return `false` if import must not continue.
