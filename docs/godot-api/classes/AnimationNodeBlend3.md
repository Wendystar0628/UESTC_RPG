<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeBlend3

命名空间 `Godot`；class；继承 [`AnimationNodeSync`](AnimationNodeSync.md) → [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A resource to add to an [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md). Blends two animations out of three linearly out of three based on the amount value.

This animation node has three inputs:

- The base animation to blend with

- A "-blend" animation to blend with when the blend amount is negative value

- A "+blend" animation to blend with when the blend amount is positive value

In general, the blend value should be in the `[-1.0, 1.0]` range. Values outside of this range can blend amplified animations, however, [`AnimationNodeAdd3`](AnimationNodeAdd3.md) works better for this purpose.

## 构造函数

### AnimationNodeBlend3

```csharp
public AnimationNodeBlend3()
```
