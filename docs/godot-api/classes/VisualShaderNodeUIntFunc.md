<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeUIntFunc

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Accept an unsigned integer scalar (`x`) to the input port and transform it according to `Function`.

## 构造函数

### VisualShaderNodeUIntFunc

```csharp
public VisualShaderNodeUIntFunc()
```

## 属性

### Function

```csharp
public VisualShaderNodeUIntFunc.FunctionEnum Function { get; set; }
```

引擎名：`function`

A function to be applied to the scalar.

## 方法

### GetFunction

```csharp
public VisualShaderNodeUIntFunc.FunctionEnum GetFunction()
```

引擎名：`get_function`

### SetFunction

```csharp
public void SetFunction(VisualShaderNodeUIntFunc.FunctionEnum func)
```

引擎名：`set_function`

## FunctionEnum

命名空间 `Godot`；enum

### 枚举值

- `Negate = 0`：Negates the `x` using `-(x)`.
- `BitwiseNot = 1`：Returns the result of bitwise `NOT` operation on the integer.
- `Max = 2`：Represents the size of the `VisualShaderNodeUIntFunc.FunctionEnum` enum.
