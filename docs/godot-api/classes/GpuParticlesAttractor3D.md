<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GpuParticlesAttractor3D

命名空间 `Godot`；class；继承 [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Particle attractors can be used to attract particles towards the attractor's origin, or to push them away from the attractor's origin.

Particle attractors work in real-time and can be moved, rotated and scaled during gameplay. Unlike collision shapes, non-uniform scaling of attractors is also supported.

Attractors can be temporarily disabled by hiding them, or by setting their `Strength` to `0.0`.

**Note:** Particle attractors only affect [`GpuParticles3D`](GpuParticles3D.md), not [`CpuParticles3D`](CpuParticles3D.md).

## 属性

### Attenuation

```csharp
public float Attenuation { get; set; }
```

The particle attractor's attenuation. Higher values result in more gradual pushing of particles as they come closer to the attractor's origin. Zero or negative values will cause particles to be pushed very fast as soon as the touch the attractor's edges.

### CullMask

```csharp
public uint CullMask { get; set; }
```

The particle rendering layers ([`VisualInstance3D.Layers`](VisualInstance3D.md)) that will be affected by the attractor. By default, all particles are affected by an attractor.

After configuring particle nodes accordingly, specific layers can be unchecked to prevent certain particles from being affected by attractors. For example, this can be used if you're using an attractor as part of a spell effect but don't want the attractor to affect unrelated weather particles at the same position.

Particle attraction can also be disabled on a per-process material basis by setting [`ParticleProcessMaterial.AttractorInteractionEnabled`](ParticleProcessMaterial.md) on the [`GpuParticles3D`](GpuParticles3D.md) node.

### Directionality

```csharp
public float Directionality { get; set; }
```

Adjusts how directional the attractor is. At `0.0`, the attractor is not directional at all: it will attract particles towards its center. At `1.0`, the attractor is fully directional: particles will always be pushed towards local -Z (or +Z if `Strength` is negative).

**Note:** If `Directionality` is greater than `0.0`, the direction in which particles are pushed can be changed by rotating the `GpuParticlesAttractor3D` node.

### Strength

```csharp
public float Strength { get; set; }
```

Adjusts the strength of the attractor. If `Strength` is negative, particles will be pushed in the opposite direction. Particles will be pushed *away* from the attractor's origin if `Directionality` is `0.0`, or towards local +Z if `Directionality` is greater than `0.0`.

## 方法

### GetAttenuation

```csharp
public float GetAttenuation()
```

### GetCullMask

```csharp
public uint GetCullMask()
```

### GetDirectionality

```csharp
public float GetDirectionality()
```

### GetStrength

```csharp
public float GetStrength()
```

### SetAttenuation

```csharp
public void SetAttenuation(float attenuation)
```

### SetCullMask

```csharp
public void SetCullMask(uint mask)
```

### SetDirectionality

```csharp
public void SetDirectionality(float amount)
```

### SetStrength

```csharp
public void SetStrength(float strength)
```
