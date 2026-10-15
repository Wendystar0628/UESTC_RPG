<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GpuParticles2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

2D particle node used to create a variety of particle systems and effects. `GpuParticles2D` features an emitter that generates some number of particles at a given rate.

Use the `ProcessMaterial` property to add a [`ParticleProcessMaterial`](ParticleProcessMaterial.md) to configure particle appearance and behavior. Alternatively, you can add a [`ShaderMaterial`](ShaderMaterial.md) which will be applied to all particles.

2D particles can optionally collide with [`LightOccluder2D`](LightOccluder2D.md), but they don't collide with [`PhysicsBody2D`](PhysicsBody2D.md) nodes.

## 构造函数

### GpuParticles2D

```csharp
public GpuParticles2D()
```

## 属性

### Amount

```csharp
public int Amount { get; set; }
```

The number of particles to emit in one emission cycle. The effective emission rate is `(amount * amount_ratio) / lifetime` particles per second. Higher values will increase GPU requirements, even if not all particles are visible at a given time or if `AmountRatio` is decreased.

**Note:** Changing this value will cause the particle system to restart. To avoid this, change `AmountRatio` instead.

### AmountRatio

```csharp
public float AmountRatio { get; set; }
```

The ratio of particles that should actually be emitted. If set to a value lower than `1.0`, this will set the amount of emitted particles throughout the lifetime to `amount * amount_ratio`. Unlike changing `Amount`, changing `AmountRatio` while emitting does not affect already-emitted particles and doesn't cause the particle system to restart. `AmountRatio` can be used to create effects that make the number of emitted particles vary over time.

**Note:** Reducing the `AmountRatio` has no performance benefit, since resources need to be allocated and processed for the total `Amount` of particles regardless of the `AmountRatio`. If you don't intend to change the number of particles emitted while the particles are emitting, make sure `AmountRatio` is set to `1` and change `Amount` to your liking instead.

### CollisionBaseSize

```csharp
public float CollisionBaseSize { get; set; }
```

Multiplier for particle's collision radius. `1.0` corresponds to the size of the sprite. If particles appear to sink into the ground when colliding, increase this value. If particles appear to float when colliding, decrease this value. Only effective if [`ParticleProcessMaterial.CollisionMode`](ParticleProcessMaterial.md) is [`ParticleProcessMaterial.CollisionModeEnum.Rigid`](ParticleProcessMaterial.md) or [`ParticleProcessMaterial.CollisionModeEnum.HideOnContact`](ParticleProcessMaterial.md).

**Note:** Particles always have a spherical collision shape.

### DrawOrder

```csharp
public GpuParticles2D.DrawOrderEnum DrawOrder { get; set; }
```

Particle draw order.

### Emitting

```csharp
public bool Emitting { get; set; }
```

If `true`, particles are being emitted. `Emitting` can be used to start and stop particles from emitting. However, if `OneShot` is `true` setting `Emitting` to `true` will not restart the emission cycle unless all active particles have finished processing. Use the `Finished` signal to be notified once all active particles finish processing.

**Note:** For `OneShot` emitters, due to the particles being computed on the GPU, there may be a short period after receiving the `Finished` signal during which setting this to `true` will not restart the emission cycle.

**Tip:** If your `OneShot` emitter needs to immediately restart emitting particles once `Finished` signal is received, consider calling `Restart()` instead of setting `Emitting`.

### Explosiveness

```csharp
public float Explosiveness { get; set; }
```

How rapidly particles in an emission cycle are emitted. If greater than `0`, there will be a gap in emissions before the next cycle begins.

### FixedFps

```csharp
public int FixedFps { get; set; }
```

The particle system's frame rate is fixed to a value. For example, changing the value to 2 will make the particles render at 2 frames per second. Note this does not slow down the simulation of the particle system itself.

### FractDelta

```csharp
public bool FractDelta { get; set; }
```

If `true`, results in fractional delta calculation which has a smoother particles display effect.

### InterpToEnd

```csharp
public float InterpToEnd { get; set; }
```

Causes all the particles in this node to interpolate towards the end of their lifetime.

**Note:** This only works when used with a [`ParticleProcessMaterial`](ParticleProcessMaterial.md). It needs to be manually implemented for custom process shaders.

### Interpolate

```csharp
public bool Interpolate { get; set; }
```

Enables particle interpolation, which makes the particle movement smoother when their `FixedFps` is lower than the screen refresh rate.

### Lifetime

```csharp
public double Lifetime { get; set; }
```

The amount of time each particle will exist (in seconds). The effective emission rate is `(amount * amount_ratio) / lifetime` particles per second.

### LocalCoords

```csharp
public bool LocalCoords { get; set; }
```

If `true`, particles use the parent node's coordinate space (known as local coordinates). This will cause particles to move and rotate along the `GpuParticles2D` node (and its parents) when it is moved or rotated. If `false`, particles use global coordinates; they will not move or rotate along the `GpuParticles2D` node (and its parents) when it is moved or rotated.

### OneShot

```csharp
public bool OneShot { get; set; }
```

If `true`, only one emission cycle occurs. If set `true` during a cycle, emission will stop at the cycle's end.

### Preprocess

```csharp
public double Preprocess { get; set; }
```

Particle system starts as if it had already run for this many seconds.

**Note:** This can be very expensive if set to a high number as it requires running the particle shader a number of times equal to the `FixedFps` (or 30, if `FixedFps` is 0) for every second. In extreme cases it can even lead to a GPU crash due to the volume of work done in a single frame.

### ProcessMaterial

```csharp
public Material ProcessMaterial { get; set; }
```

[`Material`](Material.md) for processing particles. Can be a [`ParticleProcessMaterial`](ParticleProcessMaterial.md) or a [`ShaderMaterial`](ShaderMaterial.md).

### Randomness

```csharp
public float Randomness { get; set; }
```

Emission lifetime randomness ratio.

### Seed

```csharp
public uint Seed { get; set; }
```

Sets the random seed used by the particle system. Only effective if `UseFixedSeed` is `true`.

### SpeedScale

```csharp
public double SpeedScale { get; set; }
```

Particle system's running speed scaling ratio. A value of `0` can be used to pause the particles.

### SubEmitter

```csharp
public NodePath SubEmitter { get; set; }
```

Path to another `GpuParticles2D` node that will be used as a subemitter (see [`ParticleProcessMaterial.SubEmitterMode`](ParticleProcessMaterial.md)). Subemitters can be used to achieve effects such as fireworks, sparks on collision, bubbles popping into water drops, and more.

**Note:** When `SubEmitter` is set, the target `GpuParticles2D` node will no longer emit particles on its own.

### Texture

```csharp
public Texture2D Texture { get; set; }
```

Particle texture. If `null`, particles will be squares with a size of 1×1 pixels.

**Note:** To use a flipbook texture, assign a new [`CanvasItemMaterial`](CanvasItemMaterial.md) to the `GpuParticles2D`'s [`CanvasItem.Material`](CanvasItem.md) property, then enable [`CanvasItemMaterial.ParticlesAnimation`](CanvasItemMaterial.md) and set [`CanvasItemMaterial.ParticlesAnimHFrames`](CanvasItemMaterial.md), [`CanvasItemMaterial.ParticlesAnimVFrames`](CanvasItemMaterial.md), and [`CanvasItemMaterial.ParticlesAnimLoop`](CanvasItemMaterial.md) to match the flipbook texture.

### TrailEnabled

```csharp
public bool TrailEnabled { get; set; }
```

If `true`, enables particle trails using a mesh skinning system.

**Note:** Unlike [`GpuParticles3D`](GpuParticles3D.md), the number of trail sections and subdivisions is set with the `TrailSections` and `TrailSectionSubdivisions` properties.

### TrailLifetime

```csharp
public double TrailLifetime { get; set; }
```

The amount of time the particle's trail should represent (in seconds). Only effective if `TrailEnabled` is `true`.

### TrailSectionSubdivisions

```csharp
public int TrailSectionSubdivisions { get; set; }
```

The number of subdivisions to use for the particle trail rendering. Higher values can result in smoother trail curves, at the cost of performance due to increased mesh complexity. See also `TrailSections`. Only effective if `TrailEnabled` is `true`.

### TrailSections

```csharp
public int TrailSections { get; set; }
```

The number of sections to use for the particle trail rendering. Higher values can result in smoother trail curves, at the cost of performance due to increased mesh complexity. See also `TrailSectionSubdivisions`. Only effective if `TrailEnabled` is `true`.

### UseFixedSeed

```csharp
public bool UseFixedSeed { get; set; }
```

If `true`, particles will use the same seed for every simulation using the seed defined in `Seed`. This is useful for situations where the visual outcome should be consistent across replays, for example when using Movie Maker mode.

### VisibilityRect

```csharp
public Rect2 VisibilityRect { get; set; }
```

The [`Rect2`](Rect2.md) that determines the node's region which needs to be visible on screen for the particle system to be active.

Grow the rect if particles suddenly appear/disappear when the node enters/exits the screen. The [`Rect2`](Rect2.md) can be grown via code or with the **Particles → Generate Visibility Rect** editor tool.

## 方法

### CaptureRect

```csharp
public Rect2 CaptureRect()
```

Returns a rectangle containing the positions of all existing particles.

**Note:** When using threaded rendering this method synchronizes the rendering thread. Calling it often may have a negative impact on performance.

### ConvertFromParticles

```csharp
public void ConvertFromParticles(Node particles)
```

Sets this node's properties to match a given [`CpuParticles2D`](CpuParticles2D.md) node.

### EmitParticle

```csharp
public void EmitParticle(Transform2D xform, Vector2 velocity, Color color, Color custom, uint flags)
```

Emits a single particle. Whether `xform`, `velocity`, `color` and `custom` are applied depends on the value of `flags`. See [`GpuParticles2D.EmitFlags`](GpuParticles2D.md).

The default ParticleProcessMaterial will overwrite `color` and use the contents of `custom` as `(rotation, age, animation, lifetime)`.

**Note:** `EmitParticle()` is only supported on the Forward+ and Mobile rendering methods, not Compatibility.

### GetAmount

```csharp
public int GetAmount()
```

### GetAmountRatio

```csharp
public float GetAmountRatio()
```

### GetCollisionBaseSize

```csharp
public float GetCollisionBaseSize()
```

### GetDrawOrder

```csharp
public GpuParticles2D.DrawOrderEnum GetDrawOrder()
```

### GetExplosivenessRatio

```csharp
public float GetExplosivenessRatio()
```

### GetFixedFps

```csharp
public int GetFixedFps()
```

### GetFractionalDelta

```csharp
public bool GetFractionalDelta()
```

### GetInterpToEnd

```csharp
public float GetInterpToEnd()
```

### GetInterpolate

```csharp
public bool GetInterpolate()
```

### GetLifetime

```csharp
public double GetLifetime()
```

### GetOneShot

```csharp
public bool GetOneShot()
```

### GetPreProcessTime

```csharp
public double GetPreProcessTime()
```

### GetProcessMaterial

```csharp
public Material GetProcessMaterial()
```

### GetRandomnessRatio

```csharp
public float GetRandomnessRatio()
```

### GetSeed

```csharp
public uint GetSeed()
```

### GetSpeedScale

```csharp
public double GetSpeedScale()
```

### GetSubEmitter

```csharp
public NodePath GetSubEmitter()
```

### GetTexture

```csharp
public Texture2D GetTexture()
```

### GetTrailLifetime

```csharp
public double GetTrailLifetime()
```

### GetTrailSectionSubdivisions

```csharp
public int GetTrailSectionSubdivisions()
```

### GetTrailSections

```csharp
public int GetTrailSections()
```

### GetUseFixedSeed

```csharp
public bool GetUseFixedSeed()
```

### GetUseLocalCoordinates

```csharp
public bool GetUseLocalCoordinates()
```

### GetVisibilityRect

```csharp
public Rect2 GetVisibilityRect()
```

### IsEmitting

```csharp
public bool IsEmitting()
```

### IsTrailEnabled

```csharp
public bool IsTrailEnabled()
```

### RequestParticlesProcess

```csharp
public void RequestParticlesProcess(float processTime)
```

Requests the particles to process for extra process time during a single frame.

`processTime` defines the time that the particles will process while emitting is on. `processTimeResidual` defines the time that particles will process with emitting turned off for the simulation. When combined with `SpeedScale` set to `0.0`, this is useful to be able to seek a particle system timeline.

### RequestParticlesProcess

```csharp
public void RequestParticlesProcess(float processTime, float processTimeResidual = 0f)
```

Requests the particles to process for extra process time during a single frame.

`processTime` defines the time that the particles will process while emitting is on. `processTimeResidual` defines the time that particles will process with emitting turned off for the simulation. When combined with `SpeedScale` set to `0.0`, this is useful to be able to seek a particle system timeline.

### Restart

```csharp
public void Restart()
```

Restarts the particle emission cycle, clearing existing particles. To avoid particles vanishing from the viewport, wait for the `Finished` signal before calling.

**Note:** The `Finished` signal is only emitted by `OneShot` emitters.

If `keepSeed` is `true`, the current random seed will be preserved. Useful for seeking and playback.

### Restart

```csharp
public void Restart(bool keepSeed = false)
```

Restarts the particle emission cycle, clearing existing particles. To avoid particles vanishing from the viewport, wait for the `Finished` signal before calling.

**Note:** The `Finished` signal is only emitted by `OneShot` emitters.

If `keepSeed` is `true`, the current random seed will be preserved. Useful for seeking and playback.

### SetAmount

```csharp
public void SetAmount(int amount)
```

### SetAmountRatio

```csharp
public void SetAmountRatio(float ratio)
```

### SetCollisionBaseSize

```csharp
public void SetCollisionBaseSize(float size)
```

### SetDrawOrder

```csharp
public void SetDrawOrder(GpuParticles2D.DrawOrderEnum order)
```

### SetEmitting

```csharp
public void SetEmitting(bool emitting)
```

### SetExplosivenessRatio

```csharp
public void SetExplosivenessRatio(float ratio)
```

### SetFixedFps

```csharp
public void SetFixedFps(int fps)
```

### SetFractionalDelta

```csharp
public void SetFractionalDelta(bool enable)
```

### SetInterpToEnd

```csharp
public void SetInterpToEnd(float interp)
```

### SetInterpolate

```csharp
public void SetInterpolate(bool enable)
```

### SetLifetime

```csharp
public void SetLifetime(double secs)
```

### SetOneShot

```csharp
public void SetOneShot(bool secs)
```

### SetPreProcessTime

```csharp
public void SetPreProcessTime(double secs)
```

### SetProcessMaterial

```csharp
public void SetProcessMaterial(Material material)
```

### SetRandomnessRatio

```csharp
public void SetRandomnessRatio(float ratio)
```

### SetSeed

```csharp
public void SetSeed(uint seed)
```

### SetSpeedScale

```csharp
public void SetSpeedScale(double scale)
```

### SetSubEmitter

```csharp
public void SetSubEmitter(NodePath path)
```

### SetTexture

```csharp
public void SetTexture(Texture2D texture)
```

### SetTrailEnabled

```csharp
public void SetTrailEnabled(bool enabled)
```

### SetTrailLifetime

```csharp
public void SetTrailLifetime(double secs)
```

### SetTrailSectionSubdivisions

```csharp
public void SetTrailSectionSubdivisions(int subdivisions)
```

### SetTrailSections

```csharp
public void SetTrailSections(int sections)
```

### SetUseFixedSeed

```csharp
public void SetUseFixedSeed(bool useFixedSeed)
```

### SetUseLocalCoordinates

```csharp
public void SetUseLocalCoordinates(bool enable)
```

### SetVisibilityRect

```csharp
public void SetVisibilityRect(Rect2 visibilityRect)
```

## 事件（信号）

### Finished

```csharp
public event Action Finished
```

Emitted when all active particles have finished processing. To immediately restart the emission cycle, call `Restart()`.

This signal is never emitted when `OneShot` is disabled, as particles will be emitted and processed continuously.

**Note:** For `OneShot` emitters, due to the particles being computed on the GPU, there may be a short period after receiving the signal during which setting `Emitting` to `true` will not restart the emission cycle. This delay is avoided by instead calling `Restart()`.

## DrawOrderEnum

命名空间 `Godot`；enum

### 枚举值

- `Index = 0`：Particles are drawn in the order emitted.
- `Lifetime = 1`：Particles are drawn in order of remaining lifetime.
- `ReverseLifetime = 2`：Particles are drawn in reverse order of remaining lifetime.

## EmitFlags

命名空间 `Godot`；enum

### 枚举值

- `Position = 1`：Particle starts at the specified position.
- `RotationScale = 2`：Particle starts with specified rotation and scale.
- `Velocity = 4`：Particle starts with the specified velocity vector, which defines the emission direction and speed.
- `Color = 8`：Particle starts with specified color.
- `Custom = 16`：Particle starts with specified `CUSTOM` data.
