<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CpuParticles3D

命名空间 `Godot`；class；继承 [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

CPU-based 3D particle node used to create a variety of particle systems and effects.

See also [`GpuParticles3D`](GpuParticles3D.md), which provides the same functionality with hardware acceleration, but may not run on older devices.

## 构造函数

### CpuParticles3D

```csharp
public CpuParticles3D()
```

## 属性

### Amount

```csharp
public int Amount { get; set; }
```

Number of particles emitted in one emission cycle.

### AngleCurve

```csharp
public Curve AngleCurve { get; set; }
```

Each particle's rotation will be animated along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### AngleMax

```csharp
public float AngleMax { get; set; }
```

Maximum angle.

### AngleMin

```csharp
public float AngleMin { get; set; }
```

Minimum angle.

### AngularVelocityCurve

```csharp
public Curve AngularVelocityCurve { get; set; }
```

Each particle's angular velocity (rotation speed) will vary along this [`Curve`](Curve.md) over its lifetime. Should be a unit [`Curve`](Curve.md).

### AngularVelocityMax

```csharp
public float AngularVelocityMax { get; set; }
```

Maximum initial angular velocity (rotation speed) applied to each particle in *degrees* per second.

### AngularVelocityMin

```csharp
public float AngularVelocityMin { get; set; }
```

Minimum initial angular velocity (rotation speed) applied to each particle in *degrees* per second.

### AnimOffsetCurve

```csharp
public Curve AnimOffsetCurve { get; set; }
```

Each particle's animation offset will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### AnimOffsetMax

```csharp
public float AnimOffsetMax { get; set; }
```

Maximum animation offset.

### AnimOffsetMin

```csharp
public float AnimOffsetMin { get; set; }
```

Minimum animation offset.

### AnimSpeedCurve

```csharp
public Curve AnimSpeedCurve { get; set; }
```

Each particle's animation speed will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### AnimSpeedMax

```csharp
public float AnimSpeedMax { get; set; }
```

Maximum particle animation speed.

### AnimSpeedMin

```csharp
public float AnimSpeedMin { get; set; }
```

Minimum particle animation speed.

### Color

```csharp
public Color Color { get; set; }
```

Each particle's initial color.

**Note:** `Color` multiplies the particle mesh's vertex colors. To have a visible effect on a [`BaseMaterial3D`](BaseMaterial3D.md), [`BaseMaterial3D.VertexColorUseAsAlbedo`](BaseMaterial3D.md) *must* be `true`. For a [`ShaderMaterial`](ShaderMaterial.md), `ALBEDO *= COLOR.rgb;` must be inserted in the shader's `fragment()` function. Otherwise, `Color` will have no visible effect.

### ColorInitialRamp

```csharp
public Gradient ColorInitialRamp { get; set; }
```

Each particle's initial color will vary along this [`Gradient`](Gradient.md) (multiplied with `Color`).

**Note:** `ColorInitialRamp` multiplies the particle mesh's vertex colors. To have a visible effect on a [`BaseMaterial3D`](BaseMaterial3D.md), [`BaseMaterial3D.VertexColorUseAsAlbedo`](BaseMaterial3D.md) *must* be `true`. For a [`ShaderMaterial`](ShaderMaterial.md), `ALBEDO *= COLOR.rgb;` must be inserted in the shader's `fragment()` function. Otherwise, `ColorInitialRamp` will have no visible effect.

### ColorRamp

```csharp
public Gradient ColorRamp { get; set; }
```

Each particle's color will vary along this [`Gradient`](Gradient.md) over its lifetime (multiplied with `Color`).

**Note:** `ColorRamp` multiplies the particle mesh's vertex colors. To have a visible effect on a [`BaseMaterial3D`](BaseMaterial3D.md), [`BaseMaterial3D.VertexColorUseAsAlbedo`](BaseMaterial3D.md) *must* be `true`. For a [`ShaderMaterial`](ShaderMaterial.md), `ALBEDO *= COLOR.rgb;` must be inserted in the shader's `fragment()` function. Otherwise, `ColorRamp` will have no visible effect.

### DampingCurve

```csharp
public Curve DampingCurve { get; set; }
```

Damping will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### DampingMax

```csharp
public float DampingMax { get; set; }
```

Maximum damping.

### DampingMin

```csharp
public float DampingMin { get; set; }
```

Minimum damping.

### Direction

```csharp
public Vector3 Direction { get; set; }
```

Unit vector specifying the particles' emission direction.

### DrawOrder

```csharp
public CpuParticles3D.DrawOrderEnum DrawOrder { get; set; }
```

Particle draw order.

### EmissionBoxExtents

```csharp
public Vector3 EmissionBoxExtents { get; set; }
```

The rectangle's extents if `EmissionShape` is set to [`CpuParticles3D.EmissionShapeEnum.Box`](CpuParticles3D.md).

### EmissionColors

```csharp
public Color[] EmissionColors { get; set; }
```

Sets the [`Color`](Color.md)s to modulate particles by when using [`CpuParticles3D.EmissionShapeEnum.Points`](CpuParticles3D.md) or [`CpuParticles3D.EmissionShapeEnum.DirectedPoints`](CpuParticles3D.md).

**Note:** `EmissionColors` multiplies the particle mesh's vertex colors. To have a visible effect on a [`BaseMaterial3D`](BaseMaterial3D.md), [`BaseMaterial3D.VertexColorUseAsAlbedo`](BaseMaterial3D.md) *must* be `true`. For a [`ShaderMaterial`](ShaderMaterial.md), `ALBEDO *= COLOR.rgb;` must be inserted in the shader's `fragment()` function. Otherwise, `EmissionColors` will have no visible effect.

### EmissionNormals

```csharp
public Vector3[] EmissionNormals { get; set; }
```

Sets the direction the particles will be emitted in when using [`CpuParticles3D.EmissionShapeEnum.DirectedPoints`](CpuParticles3D.md).

### EmissionPoints

```csharp
public Vector3[] EmissionPoints { get; set; }
```

Sets the initial positions to spawn particles when using [`CpuParticles3D.EmissionShapeEnum.Points`](CpuParticles3D.md) or [`CpuParticles3D.EmissionShapeEnum.DirectedPoints`](CpuParticles3D.md).

### EmissionRingAxis

```csharp
public Vector3 EmissionRingAxis { get; set; }
```

The axis of the ring when using the emitter [`CpuParticles3D.EmissionShapeEnum.Ring`](CpuParticles3D.md).

### EmissionRingConeAngle

```csharp
public float EmissionRingConeAngle { get; set; }
```

The angle of the cone when using the emitter [`CpuParticles3D.EmissionShapeEnum.Ring`](CpuParticles3D.md). The default angle of 90 degrees results in a ring, while an angle of 0 degrees results in a cone. Intermediate values will result in a ring where one end is larger than the other.

**Note:** Depending on `EmissionRingHeight`, the angle may be clamped if the ring's end is reached to form a perfect cone.

### EmissionRingHeight

```csharp
public float EmissionRingHeight { get; set; }
```

The height of the ring when using the emitter [`CpuParticles3D.EmissionShapeEnum.Ring`](CpuParticles3D.md).

### EmissionRingInnerRadius

```csharp
public float EmissionRingInnerRadius { get; set; }
```

The inner radius of the ring when using the emitter [`CpuParticles3D.EmissionShapeEnum.Ring`](CpuParticles3D.md).

### EmissionRingRadius

```csharp
public float EmissionRingRadius { get; set; }
```

The radius of the ring when using the emitter [`CpuParticles3D.EmissionShapeEnum.Ring`](CpuParticles3D.md).

### EmissionShape

```csharp
public CpuParticles3D.EmissionShapeEnum EmissionShape { get; set; }
```

Particles will be emitted inside this region.

### EmissionSphereRadius

```csharp
public float EmissionSphereRadius { get; set; }
```

The sphere's radius if [`CpuParticles3D.EmissionShapeEnum`](CpuParticles3D.md) is set to [`CpuParticles3D.EmissionShapeEnum.Sphere`](CpuParticles3D.md).

### Emitting

```csharp
public bool Emitting { get; set; }
```

If `true`, particles are being emitted. `Emitting` can be used to start and stop particles from emitting. However, if `OneShot` is `true` setting `Emitting` to `true` will not restart the emission cycle until after all active particles finish processing. You can use the `Finished` signal to be notified once all active particles finish processing.

### Explosiveness

```csharp
public float Explosiveness { get; set; }
```

How rapidly particles in an emission cycle are emitted. If greater than `0`, there will be a gap in emissions before the next cycle begins.

### FixedFps

```csharp
public int FixedFps { get; set; }
```

The particle system's frame rate is fixed to a value. For example, changing the value to 2 will make the particles render at 2 frames per second. Note this does not slow down the particle system itself.

### Flatness

```csharp
public float Flatness { get; set; }
```

Amount of `Spread` in Y/Z plane. A value of `1` restricts particles to X/Z plane.

### FractDelta

```csharp
public bool FractDelta { get; set; }
```

If `true`, results in fractional delta calculation which has a smoother particles display effect.

### Gravity

```csharp
public Vector3 Gravity { get; set; }
```

Gravity applied to every particle.

### HueVariationCurve

```csharp
public Curve HueVariationCurve { get; set; }
```

Each particle's hue will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### HueVariationMax

```csharp
public float HueVariationMax { get; set; }
```

Maximum hue variation.

### HueVariationMin

```csharp
public float HueVariationMin { get; set; }
```

Minimum hue variation.

### InitialVelocityMax

```csharp
public float InitialVelocityMax { get; set; }
```

Maximum value of the initial velocity.

### InitialVelocityMin

```csharp
public float InitialVelocityMin { get; set; }
```

Minimum value of the initial velocity.

### Lifetime

```csharp
public double Lifetime { get; set; }
```

Amount of time each particle will exist.

### LifetimeRandomness

```csharp
public double LifetimeRandomness { get; set; }
```

Particle lifetime randomness ratio.

### LinearAccelCurve

```csharp
public Curve LinearAccelCurve { get; set; }
```

Each particle's linear acceleration will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### LinearAccelMax

```csharp
public float LinearAccelMax { get; set; }
```

Maximum linear acceleration.

### LinearAccelMin

```csharp
public float LinearAccelMin { get; set; }
```

Minimum linear acceleration.

### LocalCoords

```csharp
public bool LocalCoords { get; set; }
```

If `true`, particles use the parent node's coordinate space (known as local coordinates). This will cause particles to move and rotate along the `CpuParticles3D` node (and its parents) when it is moved or rotated. If `false`, particles use global coordinates; they will not move or rotate along the `CpuParticles3D` node (and its parents) when it is moved or rotated.

### Mesh

```csharp
public Mesh Mesh { get; set; }
```

The [`Mesh`](Mesh.md) used for each particle. If `null`, particles will be spheres.

### OneShot

```csharp
public bool OneShot { get; set; }
```

If `true`, only one emission cycle occurs. If set `true` during a cycle, emission will stop at the cycle's end.

### OrbitVelocityCurve

```csharp
public Curve OrbitVelocityCurve { get; set; }
```

Each particle's orbital velocity will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### OrbitVelocityMax

```csharp
public float OrbitVelocityMax { get; set; }
```

Maximum orbit velocity.

### OrbitVelocityMin

```csharp
public float OrbitVelocityMin { get; set; }
```

Minimum orbit velocity.

### ParticleFlagAlignY

```csharp
public bool ParticleFlagAlignY { get; set; }
```

Align Y axis of particle with the direction of its velocity.

### ParticleFlagDisableZ

```csharp
public bool ParticleFlagDisableZ { get; set; }
```

If `true`, particles will not move on the Z axis.

### ParticleFlagRotateY

```csharp
public bool ParticleFlagRotateY { get; set; }
```

If `true`, particles rotate around Y axis by `AngleMin`.

### Preprocess

```csharp
public double Preprocess { get; set; }
```

Particle system starts as if it had already run for this many seconds.

### RadialAccelCurve

```csharp
public Curve RadialAccelCurve { get; set; }
```

Each particle's radial acceleration will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### RadialAccelMax

```csharp
public float RadialAccelMax { get; set; }
```

Maximum radial acceleration.

### RadialAccelMin

```csharp
public float RadialAccelMin { get; set; }
```

Minimum radial acceleration.

### Randomness

```csharp
public float Randomness { get; set; }
```

Emission lifetime randomness ratio.

### ScaleAmountCurve

```csharp
public Curve ScaleAmountCurve { get; set; }
```

Each particle's scale will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### ScaleAmountMax

```csharp
public float ScaleAmountMax { get; set; }
```

Maximum scale.

### ScaleAmountMin

```csharp
public float ScaleAmountMin { get; set; }
```

Minimum scale.

### ScaleCurveX

```csharp
public Curve ScaleCurveX { get; set; }
```

Curve for the scale over life, along the x axis.

### ScaleCurveY

```csharp
public Curve ScaleCurveY { get; set; }
```

Curve for the scale over life, along the y axis.

### ScaleCurveZ

```csharp
public Curve ScaleCurveZ { get; set; }
```

Curve for the scale over life, along the z axis.

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

### SplitScale

```csharp
public bool SplitScale { get; set; }
```

If set to `true`, three different scale curves can be specified, one per scale axis.

### Spread

```csharp
public float Spread { get; set; }
```

Each particle's initial direction range from `+spread` to `-spread` degrees. Applied to X/Z plane and Y/Z planes.

### TangentialAccelCurve

```csharp
public Curve TangentialAccelCurve { get; set; }
```

Each particle's tangential acceleration will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### TangentialAccelMax

```csharp
public float TangentialAccelMax { get; set; }
```

Maximum tangent acceleration.

### TangentialAccelMin

```csharp
public float TangentialAccelMin { get; set; }
```

Minimum tangent acceleration.

### UseFixedSeed

```csharp
public bool UseFixedSeed { get; set; }
```

If `true`, particles will use the same seed for every simulation using the seed defined in `Seed`. This is useful for situations where the visual outcome should be consistent across replays, for example when using Movie Maker mode.

### VisibilityAabb

```csharp
public Aabb VisibilityAabb { get; set; }
```

The [`Aabb`](Aabb.md) that determines the node's region which needs to be visible on screen for the particle system to be active.

Grow the box if particles suddenly appear/disappear when the node enters/exits the screen. The [`Aabb`](Aabb.md) can be grown via code or with the **Particles → Generate AABB** editor tool.

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

Sets this node's properties to match a given [`GpuParticles3D`](GpuParticles3D.md) node with an assigned [`ParticleProcessMaterial`](ParticleProcessMaterial.md).

### GetAmount

```csharp
public int GetAmount()
```

### GetColor

```csharp
public Color GetColor()
```

### GetColorInitialRamp

```csharp
public Gradient GetColorInitialRamp()
```

### GetColorRamp

```csharp
public Gradient GetColorRamp()
```

### GetDirection

```csharp
public Vector3 GetDirection()
```

### GetDrawOrder

```csharp
public CpuParticles3D.DrawOrderEnum GetDrawOrder()
```

### GetEmissionBoxExtents

```csharp
public Vector3 GetEmissionBoxExtents()
```

### GetEmissionColors

```csharp
public Color[] GetEmissionColors()
```

### GetEmissionNormals

```csharp
public Vector3[] GetEmissionNormals()
```

### GetEmissionPoints

```csharp
public Vector3[] GetEmissionPoints()
```

### GetEmissionRingAxis

```csharp
public Vector3 GetEmissionRingAxis()
```

### GetEmissionRingConeAngle

```csharp
public float GetEmissionRingConeAngle()
```

### GetEmissionRingHeight

```csharp
public float GetEmissionRingHeight()
```

### GetEmissionRingInnerRadius

```csharp
public float GetEmissionRingInnerRadius()
```

### GetEmissionRingRadius

```csharp
public float GetEmissionRingRadius()
```

### GetEmissionShape

```csharp
public CpuParticles3D.EmissionShapeEnum GetEmissionShape()
```

### GetEmissionSphereRadius

```csharp
public float GetEmissionSphereRadius()
```

### GetExplosivenessRatio

```csharp
public float GetExplosivenessRatio()
```

### GetFixedFps

```csharp
public int GetFixedFps()
```

### GetFlatness

```csharp
public float GetFlatness()
```

### GetFractionalDelta

```csharp
public bool GetFractionalDelta()
```

### GetGravity

```csharp
public Vector3 GetGravity()
```

### GetLifetime

```csharp
public double GetLifetime()
```

### GetLifetimeRandomness

```csharp
public double GetLifetimeRandomness()
```

### GetMesh

```csharp
public Mesh GetMesh()
```

### GetOneShot

```csharp
public bool GetOneShot()
```

### GetParamCurve

```csharp
public Curve GetParamCurve(CpuParticles3D.Parameter param)
```

Returns the [`Curve`](Curve.md) of the parameter specified by [`CpuParticles3D.Parameter`](CpuParticles3D.md).

### GetParamMax

```csharp
public float GetParamMax(CpuParticles3D.Parameter param)
```

Returns the maximum value range for the given parameter.

### GetParamMin

```csharp
public float GetParamMin(CpuParticles3D.Parameter param)
```

Returns the minimum value range for the given parameter.

### GetParticleFlag

```csharp
public bool GetParticleFlag(CpuParticles3D.ParticleFlags particleFlag)
```

Returns the enabled state of the given particle flag.

### GetPreProcessTime

```csharp
public double GetPreProcessTime()
```

### GetRandomnessRatio

```csharp
public float GetRandomnessRatio()
```

### GetScaleCurveX

```csharp
public Curve GetScaleCurveX()
```

### GetScaleCurveY

```csharp
public Curve GetScaleCurveY()
```

### GetScaleCurveZ

```csharp
public Curve GetScaleCurveZ()
```

### GetSeed

```csharp
public uint GetSeed()
```

### GetSpeedScale

```csharp
public double GetSpeedScale()
```

### GetSplitScale

```csharp
public bool GetSplitScale()
```

### GetSpread

```csharp
public float GetSpread()
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

Restarts the particle emitter.

If `keepSeed` is `true`, the current random seed will be preserved. Useful for seeking and playback.

### Restart

```csharp
public void Restart(bool keepSeed = false)
```

Restarts the particle emitter.

If `keepSeed` is `true`, the current random seed will be preserved. Useful for seeking and playback.

### SetAmount

```csharp
public void SetAmount(int amount)
```

### SetColor

```csharp
public void SetColor(Color color)
```

### SetColorInitialRamp

```csharp
public void SetColorInitialRamp(Gradient ramp)
```

### SetColorRamp

```csharp
public void SetColorRamp(Gradient ramp)
```

### SetDirection

```csharp
public void SetDirection(Vector3 direction)
```

### SetDrawOrder

```csharp
public void SetDrawOrder(CpuParticles3D.DrawOrderEnum order)
```

### SetEmissionBoxExtents

```csharp
public void SetEmissionBoxExtents(Vector3 extents)
```

### SetEmissionColors

```csharp
public void SetEmissionColors(Color[] array)
```

### SetEmissionColors

```csharp
public void SetEmissionColors(ReadOnlySpan<Color> array)
```

### SetEmissionNormals

```csharp
public void SetEmissionNormals(ReadOnlySpan<Vector3> array)
```

### SetEmissionNormals

```csharp
public void SetEmissionNormals(Vector3[] array)
```

### SetEmissionPoints

```csharp
public void SetEmissionPoints(ReadOnlySpan<Vector3> array)
```

### SetEmissionPoints

```csharp
public void SetEmissionPoints(Vector3[] array)
```

### SetEmissionRingAxis

```csharp
public void SetEmissionRingAxis(Vector3 axis)
```

### SetEmissionRingConeAngle

```csharp
public void SetEmissionRingConeAngle(float coneAngle)
```

### SetEmissionRingHeight

```csharp
public void SetEmissionRingHeight(float height)
```

### SetEmissionRingInnerRadius

```csharp
public void SetEmissionRingInnerRadius(float innerRadius)
```

### SetEmissionRingRadius

```csharp
public void SetEmissionRingRadius(float radius)
```

### SetEmissionShape

```csharp
public void SetEmissionShape(CpuParticles3D.EmissionShapeEnum shape)
```

### SetEmissionSphereRadius

```csharp
public void SetEmissionSphereRadius(float radius)
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

### SetFlatness

```csharp
public void SetFlatness(float amount)
```

### SetFractionalDelta

```csharp
public void SetFractionalDelta(bool enable)
```

### SetGravity

```csharp
public void SetGravity(Vector3 accelVec)
```

### SetLifetime

```csharp
public void SetLifetime(double secs)
```

### SetLifetimeRandomness

```csharp
public void SetLifetimeRandomness(double random)
```

### SetMesh

```csharp
public void SetMesh(Mesh mesh)
```

### SetOneShot

```csharp
public void SetOneShot(bool enable)
```

### SetParamCurve

```csharp
public void SetParamCurve(CpuParticles3D.Parameter param, Curve curve)
```

Sets the [`Curve`](Curve.md) of the parameter specified by [`CpuParticles3D.Parameter`](CpuParticles3D.md). Should be a unit [`Curve`](Curve.md).

### SetParamMax

```csharp
public void SetParamMax(CpuParticles3D.Parameter param, float value)
```

Sets the maximum value for the given parameter.

### SetParamMin

```csharp
public void SetParamMin(CpuParticles3D.Parameter param, float value)
```

Sets the minimum value for the given parameter.

### SetParticleFlag

```csharp
public void SetParticleFlag(CpuParticles3D.ParticleFlags particleFlag, bool enable)
```

Enables or disables the given particle flag.

### SetPreProcessTime

```csharp
public void SetPreProcessTime(double secs)
```

### SetRandomnessRatio

```csharp
public void SetRandomnessRatio(float ratio)
```

### SetScaleCurveX

```csharp
public void SetScaleCurveX(Curve scaleCurve)
```

### SetScaleCurveY

```csharp
public void SetScaleCurveY(Curve scaleCurve)
```

### SetScaleCurveZ

```csharp
public void SetScaleCurveZ(Curve scaleCurve)
```

### SetSeed

```csharp
public void SetSeed(uint seed)
```

### SetSpeedScale

```csharp
public void SetSpeedScale(double scale)
```

### SetSplitScale

```csharp
public void SetSplitScale(bool splitScale)
```

### SetSpread

```csharp
public void SetSpread(float degrees)
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

Emitted when all active particles have finished processing. When `OneShot` is disabled, particles will process continuously, so this is never emitted.

## DrawOrderEnum

命名空间 `Godot`；enum

### 枚举值

- `Index = 0`：Particles are drawn in the order emitted.
- `Lifetime = 1`：Particles are drawn in order of remaining lifetime.
- `ViewDepth = 2`：Particles are drawn in order of depth.

## EmissionShapeEnum

命名空间 `Godot`；enum

### 枚举值

- `Point = 0`：All particles will be emitted from a single point.
- `Sphere = 1`：Particles will be emitted in the volume of a sphere.
- `SphereSurface = 2`：Particles will be emitted on the surface of a sphere.
- `Box = 3`：Particles will be emitted in the volume of a box.
- `Points = 4`：Particles will be emitted at a position chosen randomly among [`CpuParticles3D.EmissionPoints`](CpuParticles3D.md).
- `DirectedPoints = 5`：Particles will be emitted at a position chosen randomly among [`CpuParticles3D.EmissionPoints`](CpuParticles3D.md).
- `Ring = 6`：Particles will be emitted in a ring or cylinder.
- `Max = 7`：Represents the size of the `CpuParticles3D.EmissionShapeEnum` enum.

## Parameter

命名空间 `Godot`；enum

### 枚举值

- `InitialLinearVelocity = 0`：Use with [`CpuParticles3D.SetParamMin`](CpuParticles3D.md), [`CpuParticles3D.SetParamMax`](CpuParticles3D.md), and [`CpuParticles3D.SetParamCurve`](CpuParticles3D.md) to set initial velocity properties.
- `AngularVelocity = 1`：Use with [`CpuParticles3D.SetParamMin`](CpuParticles3D.md), [`CpuParticles3D.SetParamMax`](CpuParticles3D.md), and [`CpuParticles3D.SetParamCurve`](CpuParticles3D.md) to set angular velocity properties.
- `OrbitVelocity = 2`：Use with [`CpuParticles3D.SetParamMin`](CpuParticles3D.md), [`CpuParticles3D.SetParamMax`](CpuParticles3D.md), and [`CpuParticles3D.SetParamCurve`](CpuParticles3D.md) to set orbital velocity properties.
- `LinearAccel = 3`：Use with [`CpuParticles3D.SetParamMin`](CpuParticles3D.md), [`CpuParticles3D.SetParamMax`](CpuParticles3D.md), and [`CpuParticles3D.SetParamCurve`](CpuParticles3D.md) to set linear acceleration properties.
- `RadialAccel = 4`：Use with [`CpuParticles3D.SetParamMin`](CpuParticles3D.md), [`CpuParticles3D.SetParamMax`](CpuParticles3D.md), and [`CpuParticles3D.SetParamCurve`](CpuParticles3D.md) to set radial acceleration properties.
- `TangentialAccel = 5`：Use with [`CpuParticles3D.SetParamMin`](CpuParticles3D.md), [`CpuParticles3D.SetParamMax`](CpuParticles3D.md), and [`CpuParticles3D.SetParamCurve`](CpuParticles3D.md) to set tangential acceleration properties.
- `Damping = 6`：Use with [`CpuParticles3D.SetParamMin`](CpuParticles3D.md), [`CpuParticles3D.SetParamMax`](CpuParticles3D.md), and [`CpuParticles3D.SetParamCurve`](CpuParticles3D.md) to set damping properties.
- `Angle = 7`：Use with [`CpuParticles3D.SetParamMin`](CpuParticles3D.md), [`CpuParticles3D.SetParamMax`](CpuParticles3D.md), and [`CpuParticles3D.SetParamCurve`](CpuParticles3D.md) to set angle properties.
- `Scale = 8`：Use with [`CpuParticles3D.SetParamMin`](CpuParticles3D.md), [`CpuParticles3D.SetParamMax`](CpuParticles3D.md), and [`CpuParticles3D.SetParamCurve`](CpuParticles3D.md) to set scale properties.
- `HueVariation = 9`：Use with [`CpuParticles3D.SetParamMin`](CpuParticles3D.md), [`CpuParticles3D.SetParamMax`](CpuParticles3D.md), and [`CpuParticles3D.SetParamCurve`](CpuParticles3D.md) to set hue variation properties.
- `AnimSpeed = 10`：Use with [`CpuParticles3D.SetParamMin`](CpuParticles3D.md), [`CpuParticles3D.SetParamMax`](CpuParticles3D.md), and [`CpuParticles3D.SetParamCurve`](CpuParticles3D.md) to set animation speed properties.
- `AnimOffset = 11`：Use with [`CpuParticles3D.SetParamMin`](CpuParticles3D.md), [`CpuParticles3D.SetParamMax`](CpuParticles3D.md), and [`CpuParticles3D.SetParamCurve`](CpuParticles3D.md) to set animation offset properties.
- `Max = 12`：Represents the size of the `CpuParticles3D.Parameter` enum.

## ParticleFlags

命名空间 `Godot`；enum

### 枚举值

- `AlignYToVelocity = 0`：Use with [`CpuParticles3D.SetParticleFlag`](CpuParticles3D.md) to set [`CpuParticles3D.ParticleFlagAlignY`](CpuParticles3D.md).
- `RotateY = 1`：Use with [`CpuParticles3D.SetParticleFlag`](CpuParticles3D.md) to set [`CpuParticles3D.ParticleFlagRotateY`](CpuParticles3D.md).
- `DisableZ = 2`：Use with [`CpuParticles3D.SetParticleFlag`](CpuParticles3D.md) to set [`CpuParticles3D.ParticleFlagDisableZ`](CpuParticles3D.md).
- `Max = 3`：Represents the size of the `CpuParticles3D.ParticleFlags` enum.
