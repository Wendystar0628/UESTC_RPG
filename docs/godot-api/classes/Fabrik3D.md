<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Fabrik3D

命名空间 `Godot`；class；继承 [`IterateIK3D`](IterateIK3D.md) → [`ChainIK3D`](ChainIK3D.md) → [`IKModifier3D`](IKModifier3D.md) → [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`Fabrik3D` is position based IK, allowing precise and accurate tracking of targets. It's ideal for simple chains without limitations.

The resulting twist around the forward vector will always be kept from the previous pose.

**Note:** When the target is close to the root, it tends to produce zig-zag patterns, resulting in unnatural visual movement.

## 构造函数

### Fabrik3D

```csharp
public Fabrik3D()
```
