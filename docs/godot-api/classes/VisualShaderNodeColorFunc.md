<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeColorFunc

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Accept a [`Color`](Color.md) to the input port and transform it according to `Function`.

## 构造函数

### VisualShaderNodeColorFunc

```csharp
public VisualShaderNodeColorFunc()
```

## 属性

### Function

```csharp
public VisualShaderNodeColorFunc.FunctionEnum Function { get; set; }
```

引擎名：`function`

A function to be applied to the input color.

## 方法

### GetFunction

```csharp
public VisualShaderNodeColorFunc.FunctionEnum GetFunction()
```

引擎名：`get_function`

### SetFunction

```csharp
public void SetFunction(VisualShaderNodeColorFunc.FunctionEnum func)
```

引擎名：`set_function`

## FunctionEnum

命名空间 `Godot`；enum

### 枚举值

- `Grayscale = 0`：Converts the color to grayscale using the following formula:
- `Hsv2Rgb = 1`：Converts HSV vector to RGB equivalent.
- `Rgb2Hsv = 2`：Converts RGB vector to HSV equivalent.
- `Sepia = 3`：Applies sepia tone effect using the following formula:
- `LinearToSrgb = 4`：Converts color from linear encoding to nonlinear sRGB encoding using the following formula:
- `SrgbToLinear = 5`：Converts color from nonlinear sRGB encoding to linear encoding using the following formula:
- `Max = 6`：Represents the size of the `VisualShaderNodeColorFunc.FunctionEnum` enum.
