<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeAdd2

命名空间 `Godot`；class；继承 [`AnimationNodeSync`](AnimationNodeSync.md) → [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A resource to add to an [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md). Blends two animations additively based on the amount value.

If the amount is greater than `1.0`, the animation connected to "in" port is blended with the amplified animation connected to "add" port.

If the amount is less than `0.0`, the animation connected to "in" port is blended with the inverted animation connected to "add" port.

## 构造函数

### AnimationNodeAdd2

```csharp
public AnimationNodeAdd2()
```
