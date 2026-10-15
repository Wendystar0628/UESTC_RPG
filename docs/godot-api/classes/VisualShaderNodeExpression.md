<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeExpression

命名空间 `Godot`；class；继承 [`VisualShaderNodeGroupBase`](VisualShaderNodeGroupBase.md) → [`VisualShaderNodeResizableBase`](VisualShaderNodeResizableBase.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Custom Godot Shading Language expression, with a custom number of input and output ports.

The provided code is directly injected into the graph's matching shader function (`vertex`, `fragment`, or `light`), so it cannot be used to declare functions, varyings, uniforms, or global constants. See [`VisualShaderNodeGlobalExpression`](VisualShaderNodeGlobalExpression.md) for such global definitions.

## 构造函数

### VisualShaderNodeExpression

```csharp
public VisualShaderNodeExpression()
```

## 属性

### Expression

```csharp
public string Expression { get; set; }
```

引擎名：`expression`

An expression in Godot Shading Language, which will be injected at the start of the graph's matching shader function (`vertex`, `fragment`, or `light`), and thus cannot be used to declare functions, varyings, uniforms, or global constants.

## 方法

### GetExpression

```csharp
public string GetExpression()
```

引擎名：`get_expression`

### SetExpression

```csharp
public void SetExpression(string expression)
```

引擎名：`set_expression`
