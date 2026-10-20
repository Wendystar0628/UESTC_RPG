<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeSub2

命名空间 `Godot`；class；继承 [`AnimationNodeSync`](AnimationNodeSync.md) → [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A resource to add to an [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md). Blends two animations subtractively based on the amount value.

This animation node is usually used for pre-calculation to cancel out any extra poses from the animation for the "add" animation source in [`AnimationNodeAdd2`](AnimationNodeAdd2.md) or [`AnimationNodeAdd3`](AnimationNodeAdd3.md).

In general, the blend value should be in the `[0.0, 1.0]` range, but values outside of this range can be used for amplified or inverted animations.

**Note:** This calculation is different from using a negative value in [`AnimationNodeAdd2`](AnimationNodeAdd2.md), since the transformation matrices do not satisfy the commutative law. `AnimationNodeSub2` multiplies the transformation matrix of the inverted animation from the left side, while negative [`AnimationNodeAdd2`](AnimationNodeAdd2.md) multiplies it from the right side.

## 构造函数

### AnimationNodeSub2

```csharp
public AnimationNodeSub2()
```
