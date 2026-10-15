<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GpuParticlesCollisionBox3D

命名空间 `Godot`；class；继承 [`GpuParticlesCollision3D`](GpuParticlesCollision3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A box-shaped 3D particle collision shape affecting [`GpuParticles3D`](GpuParticles3D.md) nodes.

Particle collision shapes work in real-time and can be moved, rotated and scaled during gameplay. Unlike attractors, non-uniform scaling of collision shapes is *not* supported.

**Note:** [`ParticleProcessMaterial.CollisionMode`](ParticleProcessMaterial.md) must be [`ParticleProcessMaterial.CollisionModeEnum.Rigid`](ParticleProcessMaterial.md) or [`ParticleProcessMaterial.CollisionModeEnum.HideOnContact`](ParticleProcessMaterial.md) on the [`GpuParticles3D`](GpuParticles3D.md)'s process material for collision to work.

**Note:** Particle collision only affects [`GpuParticles3D`](GpuParticles3D.md), not [`CpuParticles3D`](CpuParticles3D.md).

## 构造函数

### GpuParticlesCollisionBox3D

```csharp
public GpuParticlesCollisionBox3D()
```

## 属性

### Size

```csharp
public Vector3 Size { get; set; }
```

The collision box's size in 3D units.

## 方法

### GetSize

```csharp
public Vector3 GetSize()
```

### SetSize

```csharp
public void SetSize(Vector3 size)
```
