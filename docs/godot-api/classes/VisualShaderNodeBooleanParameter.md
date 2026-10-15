<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeBooleanParameter

命名空间 `Godot`；class；继承 [`VisualShaderNodeParameter`](VisualShaderNodeParameter.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Translated to `uniform bool` in the shader language.

## 构造函数

### VisualShaderNodeBooleanParameter

```csharp
public VisualShaderNodeBooleanParameter()
```

## 属性

### DefaultValue

```csharp
public bool DefaultValue { get; set; }
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
public bool GetDefaultValue()
```

引擎名：`get_default_value`

### IsDefaultValueEnabled

```csharp
public bool IsDefaultValueEnabled()
```

引擎名：`is_default_value_enabled`

### SetDefaultValue

```csharp
public void SetDefaultValue(bool value)
```

引擎名：`set_default_value`

### SetDefaultValueEnabled

```csharp
public void SetDefaultValueEnabled(bool enabled)
```

引擎名：`set_default_value_enabled`
