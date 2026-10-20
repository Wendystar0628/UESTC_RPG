<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GpuParticlesAttractorVectorField3D

命名空间 `Godot`；class；继承 [`GpuParticlesAttractor3D`](GpuParticlesAttractor3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A box-shaped attractor with varying directions and strengths defined in it that influences particles from [`GpuParticles3D`](GpuParticles3D.md) nodes.

Unlike [`GpuParticlesAttractorBox3D`](GpuParticlesAttractorBox3D.md), `GpuParticlesAttractorVectorField3D` uses a `Texture` to affect attraction strength within the box. This can be used to create complex attraction scenarios where particles travel in different directions depending on their location. This can be useful for weather effects such as sandstorms.

Particle attractors work in real-time and can be moved, rotated and scaled during gameplay. Unlike collision shapes, non-uniform scaling of attractors is also supported.

**Note:** Particle attractors only affect [`GpuParticles3D`](GpuParticles3D.md), not [`CpuParticles3D`](CpuParticles3D.md).

## 构造函数

### GpuParticlesAttractorVectorField3D

```csharp
public GpuParticlesAttractorVectorField3D()
```

## 属性

### Size

```csharp
public Vector3 Size { get; set; }
```

The size of the vector field box in 3D units.

### Texture

```csharp
public Texture3D Texture { get; set; }
```

The 3D texture to be used. Values are linearly interpolated between the texture's pixels.

**Note:** To get better performance, the 3D texture's resolution should reflect the `Size` of the attractor. Since particle attraction is usually low-frequency data, the texture can be kept at a low resolution such as 64×64×64.

## 方法

### GetSize

```csharp
public Vector3 GetSize()
```

### GetTexture

```csharp
public Texture3D GetTexture()
```

### SetSize

```csharp
public void SetSize(Vector3 size)
```

### SetTexture

```csharp
public void SetTexture(Texture3D texture)
```
