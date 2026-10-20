<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeTransformConstant

命名空间 `Godot`；class；继承 [`VisualShaderNodeConstant`](VisualShaderNodeConstant.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A constant [`Transform3D`](Transform3D.md), which can be used as an input node.

## 构造函数

### VisualShaderNodeTransformConstant

```csharp
public VisualShaderNodeTransformConstant()
```

## 属性

### Constant

```csharp
public Transform3D Constant { get; set; }
```

引擎名：`constant`

A [`Transform3D`](Transform3D.md) constant which represents the state of this node.

## 方法

### GetConstant

```csharp
public Transform3D GetConstant()
```

引擎名：`get_constant`

### SetConstant

```csharp
public void SetConstant(Transform3D constant)
```

引擎名：`set_constant`
