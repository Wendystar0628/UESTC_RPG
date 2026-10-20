<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ShaderInclude

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A shader include file, saved with the `.gdshaderinc` extension. This class allows you to define a custom shader snippet that can be included in a [`Shader`](Shader.md) by using the preprocessor directive `#include`, followed by the file path (e.g. `#include "res://shader_lib.gdshaderinc"`). The snippet doesn't have to be a valid shader on its own.

## 构造函数

### ShaderInclude

```csharp
public ShaderInclude()
```

## 属性

### Code

```csharp
public string Code { get; set; }
```

引擎名：`code`

Returns the code of the shader include file. The returned text is what the user has written, not the full generated code used internally.

## 方法

### GetCode

```csharp
public string GetCode()
```

引擎名：`get_code`

### SetCode

```csharp
public void SetCode(string code)
```

引擎名：`set_code`
