<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeIntFunc

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Accept an integer scalar (`x`) to the input port and transform it according to `Function`.

## 构造函数

### VisualShaderNodeIntFunc

```csharp
public VisualShaderNodeIntFunc()
```

## 属性

### Function

```csharp
public VisualShaderNodeIntFunc.FunctionEnum Function { get; set; }
```

引擎名：`function`

A function to be applied to the scalar.

## 方法

### GetFunction

```csharp
public VisualShaderNodeIntFunc.FunctionEnum GetFunction()
```

引擎名：`get_function`

### SetFunction

```csharp
public void SetFunction(VisualShaderNodeIntFunc.FunctionEnum func)
```

引擎名：`set_function`

## FunctionEnum

命名空间 `Godot`；enum

### 枚举值

- `Abs = 0`：Returns the absolute value of the parameter.
- `Negate = 1`：Negates the `x` using `-(x)`.
- `Sign = 2`：Extracts the sign of the parameter.
- `BitwiseNot = 3`：Returns the result of bitwise `NOT` operation on the integer.
- `Max = 4`：Represents the size of the `VisualShaderNodeIntFunc.FunctionEnum` enum.
