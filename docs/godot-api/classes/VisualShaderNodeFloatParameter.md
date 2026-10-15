<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeFloatParameter

命名空间 `Godot`；class；继承 [`VisualShaderNodeParameter`](VisualShaderNodeParameter.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Translated to `uniform float` in the shader language.

## 构造函数

### VisualShaderNodeFloatParameter

```csharp
public VisualShaderNodeFloatParameter()
```

## 属性

### DefaultValue

```csharp
public float DefaultValue { get; set; }
```

引擎名：`default_value`

A default value to be assigned within the shader.

### DefaultValueEnabled

```csharp
public bool DefaultValueEnabled { get; set; }
```

引擎名：`default_value_enabled`

Enables usage of the `DefaultValue`.

### Hint

```csharp
public VisualShaderNodeFloatParameter.HintEnum Hint { get; set; }
```

引擎名：`hint`

A hint applied to the uniform, which controls the values it can take when set through the Inspector.

### Max

```csharp
public float Max { get; set; }
```

引擎名：`max`

Minimum value for range hints. Used if `Hint` is set to [`VisualShaderNodeFloatParameter.HintEnum.Range`](VisualShaderNodeFloatParameter.md) or [`VisualShaderNodeFloatParameter.HintEnum.RangeStep`](VisualShaderNodeFloatParameter.md).

### Min

```csharp
public float Min { get; set; }
```

引擎名：`min`

Maximum value for range hints. Used if `Hint` is set to [`VisualShaderNodeFloatParameter.HintEnum.Range`](VisualShaderNodeFloatParameter.md) or [`VisualShaderNodeFloatParameter.HintEnum.RangeStep`](VisualShaderNodeFloatParameter.md).

### Step

```csharp
public float Step { get; set; }
```

引擎名：`step`

Step (increment) value for the range hint with step. Used if `Hint` is set to [`VisualShaderNodeFloatParameter.HintEnum.RangeStep`](VisualShaderNodeFloatParameter.md).

## 方法

### GetDefaultValue

```csharp
public float GetDefaultValue()
```

引擎名：`get_default_value`

### GetHint

```csharp
public VisualShaderNodeFloatParameter.HintEnum GetHint()
```

引擎名：`get_hint`

### GetMax

```csharp
public float GetMax()
```

引擎名：`get_max`

### GetMin

```csharp
public float GetMin()
```

引擎名：`get_min`

### GetStep

```csharp
public float GetStep()
```

引擎名：`get_step`

### IsDefaultValueEnabled

```csharp
public bool IsDefaultValueEnabled()
```

引擎名：`is_default_value_enabled`

### SetDefaultValue

```csharp
public void SetDefaultValue(float value)
```

引擎名：`set_default_value`

### SetDefaultValueEnabled

```csharp
public void SetDefaultValueEnabled(bool enabled)
```

引擎名：`set_default_value_enabled`

### SetHint

```csharp
public void SetHint(VisualShaderNodeFloatParameter.HintEnum hint)
```

引擎名：`set_hint`

### SetMax

```csharp
public void SetMax(float value)
```

引擎名：`set_max`

### SetMin

```csharp
public void SetMin(float value)
```

引擎名：`set_min`

### SetStep

```csharp
public void SetStep(float value)
```

引擎名：`set_step`

## HintEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：No hint used.
- `Range = 1`：A range hint for scalar value, which limits possible input values between [`VisualShaderNodeFloatParameter.Min`](VisualShaderNodeFloatParameter.md) and [`VisualShaderNodeFloatParameter.Max`](VisualShaderNodeFloatParamete…
- `RangeStep = 2`：A range hint for scalar value with step, which limits possible input values between [`VisualShaderNodeFloatParameter.Min`](VisualShaderNodeFloatParameter.md) and [`VisualShaderNodeFloatParameter.Max`](VisualShaderNodeFlo…
- `Max = 3`：Represents the size of the `VisualShaderNodeFloatParameter.HintEnum` enum.
