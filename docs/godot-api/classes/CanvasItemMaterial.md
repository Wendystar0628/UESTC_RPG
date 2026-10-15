<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CanvasItemMaterial

命名空间 `Godot`；class；继承 [`Material`](Material.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`CanvasItemMaterial`s provide a means of modifying the textures associated with a CanvasItem. They specialize in describing blend and lighting behaviors for textures. Use a [`ShaderMaterial`](ShaderMaterial.md) to more fully customize a material's interactions with a [`CanvasItem`](CanvasItem.md).

## 构造函数

### CanvasItemMaterial

```csharp
public CanvasItemMaterial()
```

## 属性

### BlendMode

```csharp
public CanvasItemMaterial.BlendModeEnum BlendMode { get; set; }
```

引擎名：`blend_mode`

The manner in which a material's rendering is applied to underlying textures.

### LightMode

```csharp
public CanvasItemMaterial.LightModeEnum LightMode { get; set; }
```

引擎名：`light_mode`

The manner in which material reacts to lighting.

### ParticlesAnimHFrames

```csharp
public int ParticlesAnimHFrames { get; set; }
```

引擎名：`particles_anim_h_frames`

The number of columns in the spritesheet assigned as [`Texture2D`](Texture2D.md) for a [`GpuParticles2D`](GpuParticles2D.md) or [`CpuParticles2D`](CpuParticles2D.md).

**Note:** This property is only used and visible in the editor if `ParticlesAnimation` is `true`.

### ParticlesAnimLoop

```csharp
public bool ParticlesAnimLoop { get; set; }
```

引擎名：`particles_anim_loop`

If `true`, the particles animation will loop.

**Note:** This property is only used and visible in the editor if `ParticlesAnimation` is `true`.

### ParticlesAnimVFrames

```csharp
public int ParticlesAnimVFrames { get; set; }
```

引擎名：`particles_anim_v_frames`

The number of rows in the spritesheet assigned as [`Texture2D`](Texture2D.md) for a [`GpuParticles2D`](GpuParticles2D.md) or [`CpuParticles2D`](CpuParticles2D.md).

**Note:** This property is only used and visible in the editor if `ParticlesAnimation` is `true`.

### ParticlesAnimation

```csharp
public bool ParticlesAnimation { get; set; }
```

引擎名：`particles_animation`

If `true`, enable spritesheet-based animation features when assigned to [`GpuParticles2D`](GpuParticles2D.md) and [`CpuParticles2D`](CpuParticles2D.md) nodes. The [`ParticleProcessMaterial.AnimSpeedMax`](ParticleProcessMaterial.md) or [`CpuParticles2D.AnimSpeedMax`](CpuParticles2D.md) should also be set to a positive value for the animation to play.

This property (and other `particles_anim_*` properties that depend on it) has no effect on other types of nodes.

## 方法

### GetBlendMode

```csharp
public CanvasItemMaterial.BlendModeEnum GetBlendMode()
```

引擎名：`get_blend_mode`

### GetLightMode

```csharp
public CanvasItemMaterial.LightModeEnum GetLightMode()
```

引擎名：`get_light_mode`

### GetParticlesAnimHFrames

```csharp
public int GetParticlesAnimHFrames()
```

引擎名：`get_particles_anim_h_frames`

### GetParticlesAnimLoop

```csharp
public bool GetParticlesAnimLoop()
```

引擎名：`get_particles_anim_loop`

### GetParticlesAnimVFrames

```csharp
public int GetParticlesAnimVFrames()
```

引擎名：`get_particles_anim_v_frames`

### GetParticlesAnimation

```csharp
public bool GetParticlesAnimation()
```

引擎名：`get_particles_animation`

### SetBlendMode

```csharp
public void SetBlendMode(CanvasItemMaterial.BlendModeEnum blendMode)
```

引擎名：`set_blend_mode`

### SetLightMode

```csharp
public void SetLightMode(CanvasItemMaterial.LightModeEnum lightMode)
```

引擎名：`set_light_mode`

### SetParticlesAnimHFrames

```csharp
public void SetParticlesAnimHFrames(int frames)
```

引擎名：`set_particles_anim_h_frames`

### SetParticlesAnimLoop

```csharp
public void SetParticlesAnimLoop(bool loop)
```

引擎名：`set_particles_anim_loop`

### SetParticlesAnimVFrames

```csharp
public void SetParticlesAnimVFrames(int frames)
```

引擎名：`set_particles_anim_v_frames`

### SetParticlesAnimation

```csharp
public void SetParticlesAnimation(bool particlesAnim)
```

引擎名：`set_particles_animation`

## BlendModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Mix = 0`：Mix blending mode.
- `Add = 1`：Additive blending mode.
- `Sub = 2`：Subtractive blending mode.
- `Mul = 3`：Multiplicative blending mode.
- `PremultAlpha = 4`：Mix blending mode.

## LightModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Normal = 0`：Render the material using both light and non-light sensitive material properties.
- `Unshaded = 1`：Render the material as if there were no light.
- `LightOnly = 2`：Render the material as if there were only light.
