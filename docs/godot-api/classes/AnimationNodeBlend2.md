<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeBlend2

命名空间 `Godot`；class；继承 [`AnimationNodeSync`](AnimationNodeSync.md) → [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A resource to add to an [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md). Blends two animations linearly based on the amount value.

In general, the blend value should be in the `[0.0, 1.0]` range. Values outside of this range can blend amplified or inverted animations, however, [`AnimationNodeAdd2`](AnimationNodeAdd2.md) works better for this purpose.

## 构造函数

### AnimationNodeBlend2

```csharp
public AnimationNodeBlend2()
```
