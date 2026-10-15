<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GpuParticlesAttractorSphere3D

命名空间 `Godot`；class；继承 [`GpuParticlesAttractor3D`](GpuParticlesAttractor3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A spheroid-shaped attractor that influences particles from [`GpuParticles3D`](GpuParticles3D.md) nodes. Can be used to attract particles towards its origin, or to push them away from its origin.

Particle attractors work in real-time and can be moved, rotated and scaled during gameplay. Unlike collision shapes, non-uniform scaling of attractors is also supported.

**Note:** Particle attractors only affect [`GpuParticles3D`](GpuParticles3D.md), not [`CpuParticles3D`](CpuParticles3D.md).

## 构造函数

### GpuParticlesAttractorSphere3D

```csharp
public GpuParticlesAttractorSphere3D()
```

## 属性

### Radius

```csharp
public float Radius { get; set; }
```

The attractor sphere's radius in 3D units.

**Note:** Stretched ellipses can be obtained by using non-uniform scaling on the `GpuParticlesAttractorSphere3D` node.

## 方法

### GetRadius

```csharp
public float GetRadius()
```

### SetRadius

```csharp
public void SetRadius(float radius)
```
