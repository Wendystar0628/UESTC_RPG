<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeUIntParameter

命名空间 `Godot`；class；继承 [`VisualShaderNodeParameter`](VisualShaderNodeParameter.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A [`VisualShaderNodeParameter`](VisualShaderNodeParameter.md) of type unsigned `System.Int32`. Offers additional customization for range of accepted values.

## 构造函数

### VisualShaderNodeUIntParameter

```csharp
public VisualShaderNodeUIntParameter()
```

## 属性

### DefaultValue

```csharp
public int DefaultValue { get; set; }
```

引擎名：`default_value`

Default value of this parameter, which will be used if not set externally. `DefaultValueEnabled` must be enabled; defaults to `0` otherwise.

### DefaultValueEnabled

```csharp
public bool DefaultValueEnabled { get; set; }
```

引擎名：`default_value_enabled`

If `true`, the node will have a custom default value.

## 方法

### GetDefaultValue

```csharp
public int GetDefaultValue()
```

引擎名：`get_default_value`

### IsDefaultValueEnabled

```csharp
public bool IsDefaultValueEnabled()
```

引擎名：`is_default_value_enabled`

### SetDefaultValue

```csharp
public void SetDefaultValue(int value)
```

引擎名：`set_default_value`

### SetDefaultValueEnabled

```csharp
public void SetDefaultValueEnabled(bool enabled)
```

引擎名：`set_default_value_enabled`
