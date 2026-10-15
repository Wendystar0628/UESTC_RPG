<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GpuParticlesCollisionSdf3D

命名空间 `Godot`；class；继承 [`GpuParticlesCollision3D`](GpuParticlesCollision3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A baked signed distance field 3D particle collision shape affecting [`GpuParticles3D`](GpuParticles3D.md) nodes.

Signed distance fields (SDF) allow for efficiently representing approximate collision shapes for convex and concave objects of any shape. This is more flexible than [`GpuParticlesCollisionHeightField3D`](GpuParticlesCollisionHeightField3D.md), but it requires a baking step.

**Baking:** The signed distance field texture can be baked by selecting the `GpuParticlesCollisionSdf3D` node in the editor, then clicking **Bake SDF** at the top of the 3D viewport. Any *visible* [`MeshInstance3D`](MeshInstance3D.md)s within the `Size` will be taken into account for baking, regardless of their [`GeometryInstance3D.GIMode`](GeometryInstance3D.md).

**Note:** Baking a `GpuParticlesCollisionSdf3D`'s `Texture` is only possible within the editor, as there is no bake method exposed for use in exported projects. However, it's still possible to load pre-baked [`Texture3D`](Texture3D.md)s into its `Texture` property in an exported project.

**Note:** [`ParticleProcessMaterial.CollisionMode`](ParticleProcessMaterial.md) must be [`ParticleProcessMaterial.CollisionModeEnum.Rigid`](ParticleProcessMaterial.md) or [`ParticleProcessMaterial.CollisionModeEnum.HideOnContact`](ParticleProcessMaterial.md) on the [`GpuParticles3D`](GpuParticles3D.md)'s process material for collision to work.

**Note:** Particle collision only affects [`GpuParticles3D`](GpuParticles3D.md), not [`CpuParticles3D`](CpuParticles3D.md).

## 构造函数

### GpuParticlesCollisionSdf3D

```csharp
public GpuParticlesCollisionSdf3D()
```

## 属性

### BakeMask

```csharp
public uint BakeMask { get; set; }
```

The visual layers to account for when baking the particle collision SDF. Only [`MeshInstance3D`](MeshInstance3D.md)s whose [`VisualInstance3D.Layers`](VisualInstance3D.md) match with this `BakeMask` will be included in the generated particle collision SDF. By default, all objects are taken into account for the particle collision SDF baking.

### Resolution

```csharp
public GpuParticlesCollisionSdf3D.ResolutionEnum Resolution { get; set; }
```

The bake resolution to use for the signed distance field `Texture`. The texture must be baked again for changes to the `Resolution` property to be effective. Higher resolutions have a greater performance cost and take more time to bake. Higher resolutions also result in larger baked textures, leading to increased VRAM and storage space requirements. To improve performance and reduce bake times, use the lowest resolution possible for the object you're representing the collision of.

### Size

```csharp
public Vector3 Size { get; set; }
```

The collision SDF's size in 3D units. To improve SDF quality, the `Size` should be set as small as possible while covering the parts of the scene you need.

### Texture

```csharp
public Texture3D Texture { get; set; }
```

The 3D texture representing the signed distance field.

### Thickness

```csharp
public float Thickness { get; set; }
```

The collision shape's thickness. Unlike other particle colliders, `GpuParticlesCollisionSdf3D` is actually hollow on the inside. `Thickness` can be increased to prevent particles from tunneling through the collision shape at high speeds, or when the `GpuParticlesCollisionSdf3D` is moved.

## 方法

### GetBakeMask

```csharp
public uint GetBakeMask()
```

### GetBakeMaskValue

```csharp
public bool GetBakeMaskValue(int layerNumber)
```

Returns whether or not the specified layer of the `BakeMask` is enabled, given a `layerNumber` between 1 and 32.

### GetResolution

```csharp
public GpuParticlesCollisionSdf3D.ResolutionEnum GetResolution()
```

### GetSize

```csharp
public Vector3 GetSize()
```

### GetTexture

```csharp
public Texture3D GetTexture()
```

### GetThickness

```csharp
public float GetThickness()
```

### SetBakeMask

```csharp
public void SetBakeMask(uint mask)
```

### SetBakeMaskValue

```csharp
public void SetBakeMaskValue(int layerNumber, bool value)
```

Based on `value`, enables or disables the specified layer in the `BakeMask`, given a `layerNumber` between 1 and 32.

### SetResolution

```csharp
public void SetResolution(GpuParticlesCollisionSdf3D.ResolutionEnum resolution)
```

### SetSize

```csharp
public void SetSize(Vector3 size)
```

### SetTexture

```csharp
public void SetTexture(Texture3D texture)
```

### SetThickness

```csharp
public void SetThickness(float thickness)
```

## ResolutionEnum

命名空间 `Godot`；enum

### 枚举值

- `Resolution16 = 0`：Bake a 16×16×16 signed distance field.
- `Resolution32 = 1`：Bake a 32×32×32 signed distance field.
- `Resolution64 = 2`：Bake a 64×64×64 signed distance field.
- `Resolution128 = 3`：Bake a 128×128×128 signed distance field.
- `Resolution256 = 4`：Bake a 256×256×256 signed distance field.
- `Resolution512 = 5`：Bake a 512×512×512 signed distance field.
- `Max = 6`：Represents the size of the `GpuParticlesCollisionSdf3D.ResolutionEnum` enum.
