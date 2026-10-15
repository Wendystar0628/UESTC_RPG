<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GDScriptWorkspace

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Provides language server functionality related to the workspace.

## 构造函数

### GDScriptWorkspace

```csharp
public GDScriptWorkspace()
```

## 方法

### ApplyNewSignal

```csharp
public void ApplyNewSignal(GodotObject obj, string function, ReadOnlySpan<string> args)
```

引擎名：`apply_new_signal`

### ApplyNewSignal

```csharp
public void ApplyNewSignal(GodotObject obj, string function, string[] args)
```

引擎名：`apply_new_signal`

### DidDeleteFiles

```csharp
public void DidDeleteFiles(Dictionary params)
```

### GenerateScriptApi

```csharp
public Dictionary GenerateScriptApi(string path)
```

引擎名：`generate_script_api`

Returns the interface of the script in a machine-readable format.

### GetFilePath

```csharp
public string GetFilePath(string uri)
```

引擎名：`get_file_path`

Converts a URI to a file path.

### GetFileUri

```csharp
public string GetFileUri(string path)
```

引擎名：`get_file_uri`

Converts a file path to a URI.

### ParseLocalScript

```csharp
public Error ParseLocalScript(string path)
```

引擎名：`parse_local_script`

### ParseScript

```csharp
public Error ParseScript(string path, string content)
```

引擎名：`parse_script`

### PublishDiagnostics

```csharp
public void PublishDiagnostics(string path)
```

引擎名：`publish_diagnostics`
