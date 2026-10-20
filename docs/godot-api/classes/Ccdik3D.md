<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Ccdik3D

命名空间 `Godot`；class；继承 [`IterateIK3D`](IterateIK3D.md) → [`ChainIK3D`](ChainIK3D.md) → [`IKModifier3D`](IKModifier3D.md) → [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`Ccdik3D` is rotation based IK, enabling fast and effective tracking even with large joint rotations. It's especially suitable for chains with limitations, providing smoother and more stable target tracking compared to [`Fabrik3D`](Fabrik3D.md).

The resulting twist around the forward vector will always be kept from the previous pose.

**Note:** When the target is close to the root, it can cause unnatural movement, including joint flips and oscillations.

## 构造函数

### Ccdik3D

```csharp
public Ccdik3D()
```
