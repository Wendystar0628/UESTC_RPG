<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeVec4Parameter

命名空间 `Godot`；class；继承 [`VisualShaderNodeParameter`](VisualShaderNodeParameter.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Translated to `uniform vec4` in the shader language.

## 构造函数

### VisualShaderNodeVec4Parameter

```csharp
public VisualShaderNodeVec4Parameter()
```

## 属性

### DefaultValue

```csharp
public Vector4 DefaultValue { get; set; }
```

引擎名：`default_value`

A default value to be assigned within the shader.

### DefaultValueEnabled

```csharp
public bool DefaultValueEnabled { get; set; }
```

引擎名：`default_value_enabled`

Enables usage of the `DefaultValue`.

## 方法

### GetDefaultValue

```csharp
public Vector4 GetDefaultValue()
```

引擎名：`get_default_value`

### IsDefaultValueEnabled

```csharp
public bool IsDefaultValueEnabled()
```

引擎名：`is_default_value_enabled`

### SetDefaultValue

```csharp
public void SetDefaultValue(Vector4 value)
```

引擎名：`set_default_value`

### SetDefaultValueEnabled

```csharp
public void SetDefaultValueEnabled(bool enabled)
```

引擎名：`set_default_value_enabled`
