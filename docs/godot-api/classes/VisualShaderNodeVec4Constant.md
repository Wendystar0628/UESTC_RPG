<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeVec4Constant

命名空间 `Godot`；class；继承 [`VisualShaderNodeConstant`](VisualShaderNodeConstant.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A constant 4D vector, which can be used as an input node.

## 构造函数

### VisualShaderNodeVec4Constant

```csharp
public VisualShaderNodeVec4Constant()
```

## 属性

### Constant

```csharp
public Quaternion Constant { get; set; }
```

引擎名：`constant`

A 4D vector (represented as a [`Quaternion`](Quaternion.md)) constant which represents the state of this node.

### ConstantV4

```csharp
public Vector4 ConstantV4 { get; set; }
```

引擎名：`constant_v4`

## 方法

### GetConstant

```csharp
public Quaternion GetConstant()
```

引擎名：`get_constant`

### SetConstant

```csharp
public void SetConstant(Quaternion constant)
```

引擎名：`set_constant`
