<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeColorConstant

命名空间 `Godot`；class；继承 [`VisualShaderNodeConstant`](VisualShaderNodeConstant.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Has two output ports representing RGB and alpha channels of [`Color`](Color.md).

Translated to `vec3 rgb` and `float alpha` in the shader language.

## 构造函数

### VisualShaderNodeColorConstant

```csharp
public VisualShaderNodeColorConstant()
```

## 属性

### Constant

```csharp
public Color Constant { get; set; }
```

引擎名：`constant`

A [`Color`](Color.md) constant which represents a state of this node.

## 方法

### GetConstant

```csharp
public Color GetConstant()
```

引擎名：`get_constant`

### SetConstant

```csharp
public void SetConstant(Color constant)
```

引擎名：`set_constant`
