<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ShaderIncludeDB

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

This object contains shader fragments from Godot's internal shaders. These can be used when access to internal uniform buffers and/or internal functions is required for instance when composing compositor effects or compute shaders. Only fragments for the current rendering device are loaded.

## 构造函数

### ShaderIncludeDB

```csharp
public ShaderIncludeDB()
```

## 方法

### GetBuiltInIncludeFile

```csharp
public static string GetBuiltInIncludeFile(string fileName)
```

引擎名：`get_built_in_include_file`

Returns the code for the built-in shader fragment. You can also access this in your shader code through `#include "filename"`.

### HasBuiltInIncludeFile

```csharp
public static bool HasBuiltInIncludeFile(string fileName)
```

引擎名：`has_built_in_include_file`

Returns `true` if an include file with this name exists.

### ListBuiltInIncludeFiles

```csharp
public static string[] ListBuiltInIncludeFiles()
```

引擎名：`list_built_in_include_files`

Returns a list of built-in include files that are currently registered.
