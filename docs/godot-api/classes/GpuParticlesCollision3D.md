<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GpuParticlesCollision3D

命名空间 `Godot`；class；继承 [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Particle collision shapes can be used to make particles stop or bounce against them.

Particle collision shapes work in real-time and can be moved, rotated and scaled during gameplay. Unlike attractors, non-uniform scaling of collision shapes is *not* supported.

Particle collision shapes can be temporarily disabled by hiding them.

**Note:** [`ParticleProcessMaterial.CollisionMode`](ParticleProcessMaterial.md) must be [`ParticleProcessMaterial.CollisionModeEnum.Rigid`](ParticleProcessMaterial.md) or [`ParticleProcessMaterial.CollisionModeEnum.HideOnContact`](ParticleProcessMaterial.md) on the [`GpuParticles3D`](GpuParticles3D.md)'s process material for collision to work.

**Note:** Particle collision only affects [`GpuParticles3D`](GpuParticles3D.md), not [`CpuParticles3D`](CpuParticles3D.md).

**Note:** Particles pushed by a collider that is being moved will not be interpolated, which can result in visible stuttering. This can be alleviated by setting [`GpuParticles3D.FixedFps`](GpuParticles3D.md) to `0` or a value that matches or exceeds the target framerate.

## 属性

### CullMask

```csharp
public uint CullMask { get; set; }
```

The particle rendering layers ([`VisualInstance3D.Layers`](VisualInstance3D.md)) that will be affected by the collision shape. By default, all particles that have [`ParticleProcessMaterial.CollisionMode`](ParticleProcessMaterial.md) set to [`ParticleProcessMaterial.CollisionModeEnum.Rigid`](ParticleProcessMaterial.md) or [`ParticleProcessMaterial.CollisionModeEnum.HideOnContact`](ParticleProcessMaterial.md) will be affected by a collision shape.

After configuring particle nodes accordingly, specific layers can be unchecked to prevent certain particles from being affected by colliders. For example, this can be used if you're using a collider as part of a spell effect but don't want the collider to affect unrelated weather particles at the same position.

Particle collision can also be disabled on a per-process material basis by setting [`ParticleProcessMaterial.CollisionMode`](ParticleProcessMaterial.md) on the [`GpuParticles3D`](GpuParticles3D.md) node.

## 方法

### GetCullMask

```csharp
public uint GetCullMask()
```

### SetCullMask

```csharp
public void SetCullMask(uint mask)
```
