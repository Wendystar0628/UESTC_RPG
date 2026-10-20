<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# JacobianIK3D

命名空间 `Godot`；class；继承 [`IterateIK3D`](IterateIK3D.md) → [`ChainIK3D`](ChainIK3D.md) → [`IKModifier3D`](IKModifier3D.md) → [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`JacobianIK3D` calculates rotations for all joints simultaneously, producing natural and smooth movement. It is particularly suited for biological animations.

The resulting twist around the forward vector will always be kept from the previous pose.

**Note:** It converges more slowly than other IK solvers, leading to gentler and less immediate tracking of targets.

## 构造函数

### JacobianIK3D

```csharp
public JacobianIK3D()
```
