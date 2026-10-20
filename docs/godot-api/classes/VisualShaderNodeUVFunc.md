<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeUVFunc

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

UV functions are similar to [`Vector2`](Vector2.md) functions, but the input port of this node uses the shader's UV value by default.

## 构造函数

### VisualShaderNodeUVFunc

```csharp
public VisualShaderNodeUVFunc()
```

## 属性

### Function

```csharp
public VisualShaderNodeUVFunc.FunctionEnum Function { get; set; }
```

引擎名：`function`

A function to be applied to the texture coordinates.

## 方法

### GetFunction

```csharp
public VisualShaderNodeUVFunc.FunctionEnum GetFunction()
```

引擎名：`get_function`

### SetFunction

```csharp
public void SetFunction(VisualShaderNodeUVFunc.FunctionEnum func)
```

引擎名：`set_function`

## FunctionEnum

命名空间 `Godot`；enum

### 枚举值

- `Panning = 0`：Translates `uv` by using `scale` and `offset` values using the following formula: `uv = uv + offset * scale`.
- `Scaling = 1`：Scales `uv` by using `scale` and `pivot` values using the following formula: `uv = (uv - pivot) * scale + pivot`.
- `Max = 2`：Represents the size of the `VisualShaderNodeUVFunc.FunctionEnum` enum.
