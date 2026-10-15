<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationRootNode

命名空间 `Godot`；class；继承 [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`AnimationRootNode` is a base class for [`AnimationNode`](AnimationNode.md)s that hold a complete animation. A complete animation refers to the output of an [`AnimationNodeOutput`](AnimationNodeOutput.md) in an [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md) or the output of another `AnimationRootNode`. Used for [`AnimationTree.TreeRoot`](AnimationTree.md) or in other `AnimationRootNode`s.

Examples of built-in root nodes include [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md) (allows blending nodes between each other using various modes), [`AnimationNodeStateMachine`](AnimationNodeStateMachine.md) (allows to configure blending and transitions between nodes using a state machine pattern), [`AnimationNodeBlendSpace2D`](AnimationNodeBlendSpace2D.md) (allows linear blending between **three** [`AnimationNode`](AnimationNode.md)s), [`AnimationNodeBlendSpace1D`](AnimationNodeBlendSpace1D.md) (allows linear blending only between **two** [`AnimationNode`](AnimationNode.md)s).

## 构造函数

### AnimationRootNode

```csharp
public AnimationRootNode()
```
