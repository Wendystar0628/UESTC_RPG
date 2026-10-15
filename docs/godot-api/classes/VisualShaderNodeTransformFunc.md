<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeTransformFunc

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Computes an inverse or transpose function on the provided [`Transform3D`](Transform3D.md).

## 构造函数

### VisualShaderNodeTransformFunc

```csharp
public VisualShaderNodeTransformFunc()
```

## 属性

### Function

```csharp
public VisualShaderNodeTransformFunc.FunctionEnum Function { get; set; }
```

引擎名：`function`

The function to be computed.

## 方法

### GetFunction

```csharp
public VisualShaderNodeTransformFunc.FunctionEnum GetFunction()
```

引擎名：`get_function`

### SetFunction

```csharp
public void SetFunction(VisualShaderNodeTransformFunc.FunctionEnum func)
```

引擎名：`set_function`

## FunctionEnum

命名空间 `Godot`；enum

### 枚举值

- `Inverse = 0`：Perform the inverse operation on the [`Transform3D`](Transform3D.md) matrix.
- `Transpose = 1`：Perform the transpose operation on the [`Transform3D`](Transform3D.md) matrix.
- `Max = 2`：Represents the size of the `VisualShaderNodeTransformFunc.FunctionEnum` enum.
