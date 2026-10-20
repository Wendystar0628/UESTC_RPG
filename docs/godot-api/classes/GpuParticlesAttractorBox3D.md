<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GpuParticlesAttractorBox3D

命名空间 `Godot`；class；继承 [`GpuParticlesAttractor3D`](GpuParticlesAttractor3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A box-shaped attractor that influences particles from [`GpuParticles3D`](GpuParticles3D.md) nodes. Can be used to attract particles towards its origin, or to push them away from its origin.

Particle attractors work in real-time and can be moved, rotated and scaled during gameplay. Unlike collision shapes, non-uniform scaling of attractors is also supported.

**Note:** Particle attractors only affect [`GpuParticles3D`](GpuParticles3D.md), not [`CpuParticles3D`](CpuParticles3D.md).

## 构造函数

### GpuParticlesAttractorBox3D

```csharp
public GpuParticlesAttractorBox3D()
```

## 属性

### Size

```csharp
public Vector3 Size { get; set; }
```

The attractor box's size in 3D units.

## 方法

### GetSize

```csharp
public Vector3 GetSize()
```

### SetSize

```csharp
public void SetSize(Vector3 size)
```
