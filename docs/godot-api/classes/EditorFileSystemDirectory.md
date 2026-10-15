<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorFileSystemDirectory

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

A more generalized, low-level variation of the directory concept.

## 构造函数

### EditorFileSystemDirectory

```csharp
public EditorFileSystemDirectory()
```

## 方法

### FindDirIndex

```csharp
public int FindDirIndex(string name)
```

引擎名：`find_dir_index`

Returns the index of the directory with name `name` or `-1` if not found.

### FindFileIndex

```csharp
public int FindFileIndex(string name)
```

引擎名：`find_file_index`

Returns the index of the file with name `name` or `-1` if not found.

### GetFile

```csharp
public string GetFile(int idx)
```

引擎名：`get_file`

Returns the name of the file at index `idx`.

### GetFileCount

```csharp
public int GetFileCount()
```

引擎名：`get_file_count`

Returns the number of files in this directory.

### GetFileImportIsValid

```csharp
public bool GetFileImportIsValid(int idx)
```

引擎名：`get_file_import_is_valid`

Returns `true` if the file at index `idx` imported properly.

### GetFilePath

```csharp
public string GetFilePath(int idx)
```

引擎名：`get_file_path`

Returns the path to the file at index `idx`.

### GetFileScriptClassExtends

```csharp
public string GetFileScriptClassExtends(int idx)
```

引擎名：`get_file_script_class_extends`

Returns the base class of the script class defined in the file at index `idx`. If the file doesn't define a script class using the `class_name` syntax, this will return an empty string.

### GetFileScriptClassName

```csharp
public string GetFileScriptClassName(int idx)
```

引擎名：`get_file_script_class_name`

Returns the name of the script class defined in the file at index `idx`. If the file doesn't define a script class using the `class_name` syntax, this will return an empty string.

### GetFileType

```csharp
public StringName GetFileType(int idx)
```

引擎名：`get_file_type`

Returns the resource type of the file at index `idx`. This returns a string such as `"Resource"` or `"GDScript"`, *not* a file extension such as `".gd"`.

### GetName

```csharp
public string GetName()
```

引擎名：`get_name`

Returns the name of this directory.

### GetParent

```csharp
public EditorFileSystemDirectory GetParent()
```

引擎名：`get_parent`

Returns the parent directory for this directory or `null` if called on a directory at `res://` or `user://`.

### GetPath

```csharp
public string GetPath()
```

引擎名：`get_path`

Returns the path to this directory.

### GetSubdir

```csharp
public EditorFileSystemDirectory GetSubdir(int idx)
```

引擎名：`get_subdir`

Returns the subdirectory at index `idx`.

### GetSubdirCount

```csharp
public int GetSubdirCount()
```

引擎名：`get_subdir_count`

Returns the number of subdirectories in this directory.
