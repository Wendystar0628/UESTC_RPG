<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDShaderFile

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Compiled shader file in SPIR-V form.

See also [`RDShaderSource`](RDShaderSource.md). `RDShaderFile` is only meant to be used with the [`RenderingDevice`](RenderingDevice.md) API. It should not be confused with Godot's own [`Shader`](Shader.md) resource, which is what Godot's various nodes use for high-level shader programming.

## 构造函数

### RDShaderFile

```csharp
public RDShaderFile()
```

## 属性

### BaseError

```csharp
public string BaseError { get; set; }
```

引擎名：`base_error`

The base compilation error message, which indicates errors not related to a specific shader stage if non-empty. If empty, shader compilation is not necessarily successful (check [`RDShaderSpirV`](RDShaderSpirV.md)'s error message members).

### _Versions

```csharp
public Dictionary _Versions { get; set; }
```

## 方法

### GetBaseError

```csharp
public string GetBaseError()
```

引擎名：`get_base_error`

### GetSpirV

```csharp
public RDShaderSpirV GetSpirV(StringName version = null)
```

Returns the SPIR-V intermediate representation for the specified shader `version`.

### GetVersionList

```csharp
public Array<StringName> GetVersionList()
```

引擎名：`get_version_list`

Returns the list of compiled versions for this shader.

### SetBaseError

```csharp
public void SetBaseError(string error)
```

引擎名：`set_base_error`

### SetBytecode

```csharp
public void SetBytecode(RDShaderSpirV bytecode, StringName version = null)
```

引擎名：`set_bytecode`

Sets the SPIR-V `bytecode` that will be compiled for the specified `version`.
