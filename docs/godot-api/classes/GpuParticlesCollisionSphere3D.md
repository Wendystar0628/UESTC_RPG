<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GpuParticlesCollisionSphere3D

命名空间 `Godot`；class；继承 [`GpuParticlesCollision3D`](GpuParticlesCollision3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A sphere-shaped 3D particle collision shape affecting [`GpuParticles3D`](GpuParticles3D.md) nodes.

Particle collision shapes work in real-time and can be moved, rotated and scaled during gameplay. Unlike attractors, non-uniform scaling of collision shapes is *not* supported.

**Note:** [`ParticleProcessMaterial.CollisionMode`](ParticleProcessMaterial.md) must be [`ParticleProcessMaterial.CollisionModeEnum.Rigid`](ParticleProcessMaterial.md) or [`ParticleProcessMaterial.CollisionModeEnum.HideOnContact`](ParticleProcessMaterial.md) on the [`GpuParticles3D`](GpuParticles3D.md)'s process material for collision to work.

**Note:** Particle collision only affects [`GpuParticles3D`](GpuParticles3D.md), not [`CpuParticles3D`](CpuParticles3D.md).

## 构造函数

### GpuParticlesCollisionSphere3D

```csharp
public GpuParticlesCollisionSphere3D()
```

## 属性

### Radius

```csharp
public float Radius { get; set; }
```

The collision sphere's radius in 3D units.

## 方法

### GetRadius

```csharp
public float GetRadius()
```

### SetRadius

```csharp
public void SetRadius(float radius)
```
