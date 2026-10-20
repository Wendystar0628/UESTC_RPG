<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeGlobalExpression

命名空间 `Godot`；class；继承 [`VisualShaderNodeExpression`](VisualShaderNodeExpression.md) → [`VisualShaderNodeGroupBase`](VisualShaderNodeGroupBase.md) → [`VisualShaderNodeResizableBase`](VisualShaderNodeResizableBase.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Custom Godot Shader Language expression, which is placed on top of the generated shader. You can place various function definitions inside to call later in [`VisualShaderNodeExpression`](VisualShaderNodeExpression.md)s (which are injected in the main shader functions). You can also declare varyings, uniforms and global constants.

## 构造函数

### VisualShaderNodeGlobalExpression

```csharp
public VisualShaderNodeGlobalExpression()
```
