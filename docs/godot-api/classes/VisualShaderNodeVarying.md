<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeVarying

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Varying values are shader variables that can be passed between shader functions, e.g. from Vertex shader to Fragment shader.

## 属性

### VaryingName

```csharp
public string VaryingName { get; set; }
```

引擎名：`varying_name`

Name of the variable. Must be unique.

### VaryingType

```csharp
public VisualShader.VaryingType VaryingType { get; set; }
```

引擎名：`varying_type`

Type of the variable. Determines where the variable can be accessed.

## 方法

### GetVaryingName

```csharp
public string GetVaryingName()
```

引擎名：`get_varying_name`

### GetVaryingType

```csharp
public VisualShader.VaryingType GetVaryingType()
```

引擎名：`get_varying_type`

### SetVaryingName

```csharp
public void SetVaryingName(string name)
```

引擎名：`set_varying_name`

### SetVaryingType

```csharp
public void SetVaryingType(VisualShader.VaryingType type)
```

引擎名：`set_varying_type`
