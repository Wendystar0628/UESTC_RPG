<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GpuParticles3D

命名空间 `Godot`；class；继承 [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

3D particle node used to create a variety of particle systems and effects. `GpuParticles3D` features an emitter that generates some number of particles at a given rate.

Use `ProcessMaterial` to add a [`ParticleProcessMaterial`](ParticleProcessMaterial.md) to configure particle appearance and behavior. Alternatively, you can add a [`ShaderMaterial`](ShaderMaterial.md) which will be applied to all particles.

## 构造函数

### GpuParticles3D

```csharp
public GpuParticles3D()
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

The base diameter for particle collision in meters. If particles appear to sink into the ground when colliding, increase this value. If particles appear to float when colliding, decrease this value. Only effective if [`ParticleProcessMaterial.CollisionMode`](ParticleProcessMaterial.md) is [`ParticleProcessMaterial.CollisionModeEnum.Rigid`](ParticleProcessMaterial.md) or [`ParticleProcessMaterial.CollisionModeEnum.HideOnContact`](ParticleProcessMaterial.md).

**Note:** Particles always have a spherical collision shape.

### DrawOrder

```csharp
public GpuParticles3D.DrawOrderEnum DrawOrder { get; set; }
```

Particle draw order.

**Note:** [`GpuParticles3D.DrawOrderEnum.Index`](GpuParticles3D.md) is the only option that supports motion vectors for effects like TAA. It is suggested to use this draw order if the particles are opaque to fix ghosting artifacts.

### DrawPass1

```csharp
public Mesh DrawPass1 { get; set; }
```

[`Mesh`](Mesh.md) that is drawn for the first draw pass.

### DrawPass2

```csharp
public Mesh DrawPass2 { get; set; }
```

[`Mesh`](Mesh.md) that is drawn for the second draw pass.

### DrawPass3

```csharp
public Mesh DrawPass3 { get; set; }
```

[`Mesh`](Mesh.md) that is drawn for the third draw pass.

### DrawPass4

```csharp
public Mesh DrawPass4 { get; set; }
```

[`Mesh`](Mesh.md) that is drawn for the fourth draw pass.

### DrawPasses

```csharp
public int DrawPasses { get; set; }
```

The number of draw passes when rendering particles.

### DrawSkin

```csharp
public Skin DrawSkin { get; set; }
```

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

Time ratio between each emission. If `0`, particles are emitted continuously. If `1`, all particles are emitted simultaneously.

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

If `true`, particles use the parent node's coordinate space (known as local coordinates). This will cause particles to move and rotate along the `GpuParticles3D` node (and its parents) when it is moved or rotated. If `false`, particles use global coordinates; they will not move or rotate along the `GpuParticles3D` node (and its parents) when it is moved or rotated.

### OneShot

```csharp
public bool OneShot { get; set; }
```

If `true`, only the number of particles equal to `Amount` will be emitted.

### Preprocess

```csharp
public double Preprocess { get; set; }
```

Amount of time to preprocess the particles before animation starts. Lets you start the animation some time after particles have started emitting.

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

Emission randomness ratio.

### Seed

```csharp
public uint Seed { get; set; }
```

Sets the random seed used by the particle system. Only effective if `UseFixedSeed` is `true`.

### SpeedScale

```csharp
public double SpeedScale { get; set; }
```

Speed scaling ratio. A value of `0` can be used to pause the particles.

### SubEmitter

```csharp
public NodePath SubEmitter { get; set; }
```

Path to another `GpuParticles3D` node that will be used as a subemitter (see [`ParticleProcessMaterial.SubEmitterMode`](ParticleProcessMaterial.md)). Subemitters can be used to achieve effects such as fireworks, sparks on collision, bubbles popping into water drops, and more.

**Note:** When `SubEmitter` is set, the target `GpuParticles3D` node will no longer emit particles on its own.

### TrailEnabled

```csharp
public bool TrailEnabled { get; set; }
```

If `true`, enables particle trails using a mesh skinning system. Designed to work with [`RibbonTrailMesh`](RibbonTrailMesh.md) and [`TubeTrailMesh`](TubeTrailMesh.md).

**Note:** [`BaseMaterial3D.UseParticleTrails`](BaseMaterial3D.md) must also be enabled on the particle mesh's material. Otherwise, setting `TrailEnabled` to `true` will have no effect.

**Note:** Unlike [`GpuParticles2D`](GpuParticles2D.md), the number of trail sections and subdivisions is set in the [`RibbonTrailMesh`](RibbonTrailMesh.md) or the [`TubeTrailMesh`](TubeTrailMesh.md)'s properties.

### TrailLifetime

```csharp
public double TrailLifetime { get; set; }
```

The amount of time the particle's trail should represent (in seconds). Only effective if `TrailEnabled` is `true`.

### TransformAlign

```csharp
public GpuParticles3D.TransformAlignEnum TransformAlign { get; set; }
```

The alignment of particles. Use this for billboarding and aligning to velocity.

### TransformAlignAxis

```csharp
public RenderingServer.ParticlesTransformAlignAxis TransformAlignAxis { get; set; }
```

When using transform align local billboard, which axis to use for the billboarding. Supports only X or Y.

### TransformAlignChannelFilter

```csharp
public RenderingServer.ParticlesTransformAlignCustomSrc TransformAlignChannelFilter { get; set; }
```

In the case of billboarded particles, which custom channel to read from to calculate their angle.

### UseFixedSeed

```csharp
public bool UseFixedSeed { get; set; }
```

If `true`, particles will use the same seed for every simulation using the seed defined in `Seed`. This is useful for situations where the visual outcome should be consistent across replays, for example when using Movie Maker mode.

### VisibilityAabb

```csharp
public Aabb VisibilityAabb { get; set; }
```

The [`Aabb`](Aabb.md) that determines the node's region which needs to be visible on screen for the particle system to be active. [`GeometryInstance3D.ExtraCullMargin`](GeometryInstance3D.md) is added on each of the AABB's axes. Particle collisions and attraction will only occur within this area.

Grow the box if particles suddenly appear/disappear when the node enters/exits the screen. The [`Aabb`](Aabb.md) can be grown via code or with the **Particles → Generate AABB** editor tool.

**Note:** `VisibilityAabb` is overridden by [`GeometryInstance3D.CustomAabb`](GeometryInstance3D.md) if that property is set to a non-default value.

## 方法

### CaptureAabb

```csharp
public Aabb CaptureAabb()
```

Returns the axis-aligned bounding box that contains all the particles that are active in the current frame.

### ConvertFromParticles

```csharp
public void ConvertFromParticles(Node particles)
```

Sets this node's properties to match a given [`CpuParticles3D`](CpuParticles3D.md) node.

### EmitParticle

```csharp
public void EmitParticle(Transform3D xform, Vector3 velocity, Color color, Color custom, uint flags)
```

Emits a single particle. Whether `xform`, `velocity`, `color` and `custom` are applied depends on the value of `flags`. See [`GpuParticles3D.EmitFlags`](GpuParticles3D.md).

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
public GpuParticles3D.DrawOrderEnum GetDrawOrder()
```

### GetDrawPassMesh

```csharp
public Mesh GetDrawPassMesh(int pass)
```

Returns the [`Mesh`](Mesh.md) that is drawn at index `pass`.

### GetDrawPasses

```csharp
public int GetDrawPasses()
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

### GetSkin

```csharp
public Skin GetSkin()
```

### GetSpeedScale

```csharp
public double GetSpeedScale()
```

### GetSubEmitter

```csharp
public NodePath GetSubEmitter()
```

### GetTrailLifetime

```csharp
public double GetTrailLifetime()
```

### GetTransformAlign

```csharp
public GpuParticles3D.TransformAlignEnum GetTransformAlign()
```

### GetTransformAlignAxis

```csharp
public RenderingServer.ParticlesTransformAlignAxis GetTransformAlignAxis()
```

### GetTransformAlignChannelFilter

```csharp
public RenderingServer.ParticlesTransformAlignCustomSrc GetTransformAlignChannelFilter()
```

### GetUseFixedSeed

```csharp
public bool GetUseFixedSeed()
```

### GetUseLocalCoordinates

```csharp
public bool GetUseLocalCoordinates()
```

### GetVisibilityAabb

```csharp
public Aabb GetVisibilityAabb()
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
public void SetDrawOrder(GpuParticles3D.DrawOrderEnum order)
```

### SetDrawPassMesh

```csharp
public void SetDrawPassMesh(int pass, Mesh mesh)
```

Sets the [`Mesh`](Mesh.md) that is drawn at index `pass`.

### SetDrawPasses

```csharp
public void SetDrawPasses(int passes)
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
public void SetOneShot(bool enable)
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

### SetSkin

```csharp
public void SetSkin(Skin skin)
```

### SetSpeedScale

```csharp
public void SetSpeedScale(double scale)
```

### SetSubEmitter

```csharp
public void SetSubEmitter(NodePath path)
```

### SetTrailEnabled

```csharp
public void SetTrailEnabled(bool enabled)
```

### SetTrailLifetime

```csharp
public void SetTrailLifetime(double secs)
```

### SetTransformAlign

```csharp
public void SetTransformAlign(GpuParticles3D.TransformAlignEnum align)
```

### SetTransformAlignAxis

```csharp
public void SetTransformAlignAxis(RenderingServer.ParticlesTransformAlignAxis align)
```

### SetTransformAlignChannelFilter

```csharp
public void SetTransformAlignChannelFilter(RenderingServer.ParticlesTransformAlignCustomSrc channelFilter)
```

### SetUseFixedSeed

```csharp
public void SetUseFixedSeed(bool useFixedSeed)
```

### SetUseLocalCoordinates

```csharp
public void SetUseLocalCoordinates(bool enable)
```

### SetVisibilityAabb

```csharp
public void SetVisibilityAabb(Aabb aabb)
```

## 事件（信号）

### Finished

```csharp
public event Action Finished
```

Emitted when all active particles have finished processing. To immediately restart the emission cycle, call `Restart()`.

This signal is never emitted when `OneShot` is disabled, as particles will be emitted and processed continuously.

**Note:** For `OneShot` emitters, due to the particles being computed on the GPU, there may be a short period after receiving the signal during which setting `Emitting` to `true` will not restart the emission cycle. This delay is avoided by instead calling `Restart()`.

## 常量

### MaxDrawPasses

```csharp
public const long MaxDrawPasses = 4
```

Maximum number of draw passes supported.

## DrawOrderEnum

命名空间 `Godot`；enum

### 枚举值

- `Index = 0`：Particles are drawn in the order emitted.
- `Lifetime = 1`：Particles are drawn in order of remaining lifetime.
- `ReverseLifetime = 2`：Particles are drawn in reverse order of remaining lifetime.
- `ViewDepth = 3`：Particles are drawn in order of depth.

## EmitFlags

命名空间 `Godot`；enum

### 枚举值

- `Position = 1`：Particle starts at the specified position.
- `RotationScale = 2`：Particle starts with specified rotation and scale.
- `Velocity = 4`：Particle starts with the specified velocity vector, which defines the emission direction and speed.
- `Color = 8`：Particle starts with specified color.
- `Custom = 16`：Particle starts with specified `CUSTOM` data.

## TransformAlignEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Do not align particle transforms relative to the camera or velocity.
- `ZBillboard = 1`：Align each particle's Z axis to face the camera.
- `YToVelocity = 2`：Align each particle's Y axis to the velocity vector.
- `ZBillboardYToVelocity = 3`：Align each particle's Z axis to face the camera and Y axis to the velocity vector.
- `LocalBillboard = 4`：Align each particle's Z axis to face the camera, while preserving a given axis (X or Y).
