<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeIs

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Returns the boolean result of the comparison between `INF` or `NaN` and a scalar parameter.

## 构造函数

### VisualShaderNodeIs

```csharp
public VisualShaderNodeIs()
```

## 属性

### Function

```csharp
public VisualShaderNodeIs.FunctionEnum Function { get; set; }
```

引擎名：`function`

The comparison function.

## 方法

### GetFunction

```csharp
public VisualShaderNodeIs.FunctionEnum GetFunction()
```

引擎名：`get_function`

### SetFunction

```csharp
public void SetFunction(VisualShaderNodeIs.FunctionEnum func)
```

引擎名：`set_function`

## FunctionEnum

命名空间 `Godot`；enum

### 枚举值

- `IsInf = 0`：Comparison with `INF` (Infinity).
- `IsNan = 1`：Comparison with `NaN` (Not a Number; indicates invalid numeric results, such as division by zero).
- `Max = 2`：Represents the size of the `VisualShaderNodeIs.FunctionEnum` enum.
