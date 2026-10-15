<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeIntParameter

命名空间 `Godot`；class；继承 [`VisualShaderNodeParameter`](VisualShaderNodeParameter.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A [`VisualShaderNodeParameter`](VisualShaderNodeParameter.md) of type `System.Int32`. Offers additional customization for range of accepted values.

## 构造函数

### VisualShaderNodeIntParameter

```csharp
public VisualShaderNodeIntParameter()
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

### EnumNames

```csharp
public string[] EnumNames { get; set; }
```

引擎名：`enum_names`

The names used for the enum select in the editor. `Hint` must be [`VisualShaderNodeIntParameter.HintEnum.Enum`](VisualShaderNodeIntParameter.md) for this to take effect.

### Hint

```csharp
public VisualShaderNodeIntParameter.HintEnum Hint { get; set; }
```

引擎名：`hint`

Range hint of this node. Use it to customize valid parameter range.

### Max

```csharp
public int Max { get; set; }
```

引擎名：`max`

The maximum value this parameter can take. `Hint` must be either [`VisualShaderNodeIntParameter.HintEnum.Range`](VisualShaderNodeIntParameter.md) or [`VisualShaderNodeIntParameter.HintEnum.RangeStep`](VisualShaderNodeIntParameter.md) for this to take effect.

### Min

```csharp
public int Min { get; set; }
```

引擎名：`min`

The minimum value this parameter can take. `Hint` must be either [`VisualShaderNodeIntParameter.HintEnum.Range`](VisualShaderNodeIntParameter.md) or [`VisualShaderNodeIntParameter.HintEnum.RangeStep`](VisualShaderNodeIntParameter.md) for this to take effect.

### Step

```csharp
public int Step { get; set; }
```

引擎名：`step`

The step between parameter's values. Forces the parameter to be a multiple of the given value. `Hint` must be [`VisualShaderNodeIntParameter.HintEnum.RangeStep`](VisualShaderNodeIntParameter.md) for this to take effect.

## 方法

### GetDefaultValue

```csharp
public int GetDefaultValue()
```

引擎名：`get_default_value`

### GetEnumNames

```csharp
public string[] GetEnumNames()
```

引擎名：`get_enum_names`

### GetHint

```csharp
public VisualShaderNodeIntParameter.HintEnum GetHint()
```

引擎名：`get_hint`

### GetMax

```csharp
public int GetMax()
```

引擎名：`get_max`

### GetMin

```csharp
public int GetMin()
```

引擎名：`get_min`

### GetStep

```csharp
public int GetStep()
```

引擎名：`get_step`

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

### SetEnumNames

```csharp
public void SetEnumNames(ReadOnlySpan<string> names)
```

引擎名：`set_enum_names`

### SetEnumNames

```csharp
public void SetEnumNames(string[] names)
```

引擎名：`set_enum_names`

### SetHint

```csharp
public void SetHint(VisualShaderNodeIntParameter.HintEnum hint)
```

引擎名：`set_hint`

### SetMax

```csharp
public void SetMax(int value)
```

引擎名：`set_max`

### SetMin

```csharp
public void SetMin(int value)
```

引擎名：`set_min`

### SetStep

```csharp
public void SetStep(int value)
```

引擎名：`set_step`

## HintEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：The parameter will not constrain its value.
- `Range = 1`：The parameter's value must be within the specified [`VisualShaderNodeIntParameter.Min`](VisualShaderNodeIntParameter.md)/[`VisualShaderNodeIntParameter.Max`](VisualShaderNodeIntParameter.md) range.
- `RangeStep = 2`：The parameter's value must be within the specified range, with the given [`VisualShaderNodeIntParameter.Step`](VisualShaderNodeIntParameter.md) between values.
- `Enum = 3`：The parameter uses an enum to associate preset values to names in the editor.
- `Max = 4`：Represents the size of the `VisualShaderNodeIntParameter.HintEnum` enum.
