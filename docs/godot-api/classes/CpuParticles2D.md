<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CpuParticles2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

CPU-based 2D particle node used to create a variety of particle systems and effects.

See also [`GpuParticles2D`](GpuParticles2D.md), which provides the same functionality with hardware acceleration, but may not run on older devices.

## 构造函数

### CpuParticles2D

```csharp
public CpuParticles2D()
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

Maximum initial rotation applied to each particle, in degrees.

### AngleMin

```csharp
public float AngleMin { get; set; }
```

Minimum equivalent of `AngleMax`.

### AngularVelocityCurve

```csharp
public Curve AngularVelocityCurve { get; set; }
```

Each particle's angular velocity will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### AngularVelocityMax

```csharp
public float AngularVelocityMax { get; set; }
```

Maximum initial angular velocity (rotation speed) applied to each particle in *degrees* per second.

### AngularVelocityMin

```csharp
public float AngularVelocityMin { get; set; }
```

Minimum equivalent of `AngularVelocityMax`.

### AnimOffsetCurve

```csharp
public Curve AnimOffsetCurve { get; set; }
```

Each particle's animation offset will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### AnimOffsetMax

```csharp
public float AnimOffsetMax { get; set; }
```

Maximum animation offset that corresponds to frame index in the texture. `0` is the first frame, `1` is the last one. See [`CanvasItemMaterial.ParticlesAnimation`](CanvasItemMaterial.md).

### AnimOffsetMin

```csharp
public float AnimOffsetMin { get; set; }
```

Minimum equivalent of `AnimOffsetMax`.

### AnimSpeedCurve

```csharp
public Curve AnimSpeedCurve { get; set; }
```

Each particle's animation speed will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### AnimSpeedMax

```csharp
public float AnimSpeedMax { get; set; }
```

Maximum particle animation speed. Animation speed of `1` means that the particles will make full `0` to `1` offset cycle during lifetime, `2` means `2` cycles etc.

With animation speed greater than `1`, remember to enable [`CanvasItemMaterial.ParticlesAnimLoop`](CanvasItemMaterial.md) property if you want the animation to repeat.

### AnimSpeedMin

```csharp
public float AnimSpeedMin { get; set; }
```

Minimum equivalent of `AnimSpeedMax`.

### Color

```csharp
public Color Color { get; set; }
```

Each particle's initial color. If `Texture` is defined, it will be multiplied by this color.

### ColorInitialRamp

```csharp
public Gradient ColorInitialRamp { get; set; }
```

Each particle's initial color will vary along this [`Gradient`](Gradient.md) (multiplied with `Color`).

### ColorRamp

```csharp
public Gradient ColorRamp { get; set; }
```

Each particle's color will vary along this [`Gradient`](Gradient.md) over its lifetime (multiplied with `Color`).

### DampingCurve

```csharp
public Curve DampingCurve { get; set; }
```

Damping will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### DampingMax

```csharp
public float DampingMax { get; set; }
```

The maximum rate at which particles lose velocity. For example value of `100` means that the particle will go from `100` velocity to `0` in `1` second.

### DampingMin

```csharp
public float DampingMin { get; set; }
```

Minimum equivalent of `DampingMax`.

### Direction

```csharp
public Vector2 Direction { get; set; }
```

Unit vector specifying the particles' emission direction.

### DrawOrder

```csharp
public CpuParticles2D.DrawOrderEnum DrawOrder { get; set; }
```

Particle draw order.

### EmissionColors

```csharp
public Color[] EmissionColors { get; set; }
```

Sets the [`Color`](Color.md)s to modulate particles by when using [`CpuParticles2D.EmissionShapeEnum.Points`](CpuParticles2D.md) or [`CpuParticles2D.EmissionShapeEnum.DirectedPoints`](CpuParticles2D.md).

### EmissionNormals

```csharp
public Vector2[] EmissionNormals { get; set; }
```

Sets the direction the particles will be emitted in when using [`CpuParticles2D.EmissionShapeEnum.DirectedPoints`](CpuParticles2D.md).

### EmissionPoints

```csharp
public Vector2[] EmissionPoints { get; set; }
```

Sets the initial positions to spawn particles when using [`CpuParticles2D.EmissionShapeEnum.Points`](CpuParticles2D.md) or [`CpuParticles2D.EmissionShapeEnum.DirectedPoints`](CpuParticles2D.md).

### EmissionRectExtents

```csharp
public Vector2 EmissionRectExtents { get; set; }
```

The rectangle's extents if `EmissionShape` is set to [`CpuParticles2D.EmissionShapeEnum.Rectangle`](CpuParticles2D.md).

### EmissionRingInnerRadius

```csharp
public float EmissionRingInnerRadius { get; set; }
```

The ring's inner radius if `EmissionShape` is set to [`CpuParticles2D.EmissionShapeEnum.Ring`](CpuParticles2D.md).

### EmissionRingRadius

```csharp
public float EmissionRingRadius { get; set; }
```

The ring's outer radius if `EmissionShape` is set to [`CpuParticles2D.EmissionShapeEnum.Ring`](CpuParticles2D.md).

### EmissionShape

```csharp
public CpuParticles2D.EmissionShapeEnum EmissionShape { get; set; }
```

Particles will be emitted inside this region.

### EmissionSphereRadius

```csharp
public float EmissionSphereRadius { get; set; }
```

The sphere's radius if `EmissionShape` is set to [`CpuParticles2D.EmissionShapeEnum.Sphere`](CpuParticles2D.md).

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

The particle system's frame rate is fixed to a value. For example, changing the value to 2 will make the particles render at 2 frames per second. Note this does not slow down the simulation of the particle system itself.

### FractDelta

```csharp
public bool FractDelta { get; set; }
```

If `true`, results in fractional delta calculation which has a smoother particles display effect.

### Gravity

```csharp
public Vector2 Gravity { get; set; }
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

Maximum initial hue variation applied to each particle. It will shift the particle color's hue.

### HueVariationMin

```csharp
public float HueVariationMin { get; set; }
```

Minimum equivalent of `HueVariationMax`.

### InitialVelocityMax

```csharp
public float InitialVelocityMax { get; set; }
```

Maximum initial velocity magnitude for each particle. Direction comes from `Direction` and `Spread`.

### InitialVelocityMin

```csharp
public float InitialVelocityMin { get; set; }
```

Minimum equivalent of `InitialVelocityMax`.

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

Maximum linear acceleration applied to each particle in the direction of motion.

### LinearAccelMin

```csharp
public float LinearAccelMin { get; set; }
```

Minimum equivalent of `LinearAccelMax`.

### LocalCoords

```csharp
public bool LocalCoords { get; set; }
```

If `true`, particles use the parent node's coordinate space (known as local coordinates). This will cause particles to move and rotate along the `CpuParticles2D` node (and its parents) when it is moved or rotated. If `false`, particles use global coordinates; they will not move or rotate along the `CpuParticles2D` node (and its parents) when it is moved or rotated.

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

Maximum orbital velocity applied to each particle. Makes the particles circle around origin. Specified in number of full rotations around origin per second.

### OrbitVelocityMin

```csharp
public float OrbitVelocityMin { get; set; }
```

Minimum equivalent of `OrbitVelocityMax`.

### ParticleFlagAlignY

```csharp
public bool ParticleFlagAlignY { get; set; }
```

Align Y axis of particle with the direction of its velocity.

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

Maximum radial acceleration applied to each particle. Makes particle accelerate away from the origin or towards it if negative.

### RadialAccelMin

```csharp
public float RadialAccelMin { get; set; }
```

Minimum equivalent of `RadialAccelMax`.

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

Maximum initial scale applied to each particle.

### ScaleAmountMin

```csharp
public float ScaleAmountMin { get; set; }
```

Minimum equivalent of `ScaleAmountMax`.

### ScaleCurveX

```csharp
public Curve ScaleCurveX { get; set; }
```

Each particle's horizontal scale will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

`SplitScale` must be enabled.

### ScaleCurveY

```csharp
public Curve ScaleCurveY { get; set; }
```

Each particle's vertical scale will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

`SplitScale` must be enabled.

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

If `true`, the scale curve will be split into x and y components. See `ScaleCurveX` and `ScaleCurveY`.

### Spread

```csharp
public float Spread { get; set; }
```

Each particle's initial direction range from `+spread` to `-spread` degrees.

### TangentialAccelCurve

```csharp
public Curve TangentialAccelCurve { get; set; }
```

Each particle's tangential acceleration will vary along this [`Curve`](Curve.md). Should be a unit [`Curve`](Curve.md).

### TangentialAccelMax

```csharp
public float TangentialAccelMax { get; set; }
```

Maximum tangential acceleration applied to each particle. Tangential acceleration is perpendicular to the particle's velocity giving the particles a swirling motion.

### TangentialAccelMin

```csharp
public float TangentialAccelMin { get; set; }
```

Minimum equivalent of `TangentialAccelMax`.

### Texture

```csharp
public Texture2D Texture { get; set; }
```

Particle texture. If `null`, particles will be squares.

### UseFixedSeed

```csharp
public bool UseFixedSeed { get; set; }
```

If `true`, particles will use the same seed for every simulation using the seed defined in `Seed`. This is useful for situations where the visual outcome should be consistent across replays, for example when using Movie Maker mode.

## 方法

### ConvertFromParticles

```csharp
public void ConvertFromParticles(Node particles)
```

Sets this node's properties to match a given [`GpuParticles2D`](GpuParticles2D.md) node with an assigned [`ParticleProcessMaterial`](ParticleProcessMaterial.md).

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
public Vector2 GetDirection()
```

### GetDrawOrder

```csharp
public CpuParticles2D.DrawOrderEnum GetDrawOrder()
```

### GetEmissionColors

```csharp
public Color[] GetEmissionColors()
```

### GetEmissionNormals

```csharp
public Vector2[] GetEmissionNormals()
```

### GetEmissionPoints

```csharp
public Vector2[] GetEmissionPoints()
```

### GetEmissionRectExtents

```csharp
public Vector2 GetEmissionRectExtents()
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
public CpuParticles2D.EmissionShapeEnum GetEmissionShape()
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

### GetFractionalDelta

```csharp
public bool GetFractionalDelta()
```

### GetGravity

```csharp
public Vector2 GetGravity()
```

### GetLifetime

```csharp
public double GetLifetime()
```

### GetLifetimeRandomness

```csharp
public double GetLifetimeRandomness()
```

### GetOneShot

```csharp
public bool GetOneShot()
```

### GetParamCurve

```csharp
public Curve GetParamCurve(CpuParticles2D.Parameter param)
```

Returns the [`Curve`](Curve.md) of the parameter specified by [`CpuParticles2D.Parameter`](CpuParticles2D.md).

### GetParamMax

```csharp
public float GetParamMax(CpuParticles2D.Parameter param)
```

Returns the maximum value range for the given parameter.

### GetParamMin

```csharp
public float GetParamMin(CpuParticles2D.Parameter param)
```

Returns the minimum value range for the given parameter.

### GetParticleFlag

```csharp
public bool GetParticleFlag(CpuParticles2D.ParticleFlags particleFlag)
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

### GetTexture

```csharp
public Texture2D GetTexture()
```

### GetUseFixedSeed

```csharp
public bool GetUseFixedSeed()
```

### GetUseLocalCoordinates

```csharp
public bool GetUseLocalCoordinates()
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
public void SetDirection(Vector2 direction)
```

### SetDrawOrder

```csharp
public void SetDrawOrder(CpuParticles2D.DrawOrderEnum order)
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
public void SetEmissionNormals(ReadOnlySpan<Vector2> array)
```

### SetEmissionNormals

```csharp
public void SetEmissionNormals(Vector2[] array)
```

### SetEmissionPoints

```csharp
public void SetEmissionPoints(ReadOnlySpan<Vector2> array)
```

### SetEmissionPoints

```csharp
public void SetEmissionPoints(Vector2[] array)
```

### SetEmissionRectExtents

```csharp
public void SetEmissionRectExtents(Vector2 extents)
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
public void SetEmissionShape(CpuParticles2D.EmissionShapeEnum shape)
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

### SetFractionalDelta

```csharp
public void SetFractionalDelta(bool enable)
```

### SetGravity

```csharp
public void SetGravity(Vector2 accelVec)
```

### SetLifetime

```csharp
public void SetLifetime(double secs)
```

### SetLifetimeRandomness

```csharp
public void SetLifetimeRandomness(double random)
```

### SetOneShot

```csharp
public void SetOneShot(bool enable)
```

### SetParamCurve

```csharp
public void SetParamCurve(CpuParticles2D.Parameter param, Curve curve)
```

Sets the [`Curve`](Curve.md) of the parameter specified by [`CpuParticles2D.Parameter`](CpuParticles2D.md). Should be a unit [`Curve`](Curve.md).

### SetParamMax

```csharp
public void SetParamMax(CpuParticles2D.Parameter param, float value)
```

Sets the maximum value for the given parameter.

### SetParamMin

```csharp
public void SetParamMin(CpuParticles2D.Parameter param, float value)
```

Sets the minimum value for the given parameter.

### SetParticleFlag

```csharp
public void SetParticleFlag(CpuParticles2D.ParticleFlags particleFlag, bool enable)
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
public void SetSpread(float spread)
```

### SetTexture

```csharp
public void SetTexture(Texture2D texture)
```

### SetUseFixedSeed

```csharp
public void SetUseFixedSeed(bool useFixedSeed)
```

### SetUseLocalCoordinates

```csharp
public void SetUseLocalCoordinates(bool enable)
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

## EmissionShapeEnum

命名空间 `Godot`；enum

### 枚举值

- `Point = 0`：All particles will be emitted from a single point.
- `Sphere = 1`：Particles will be emitted in the volume of a sphere flattened to two dimensions.
- `SphereSurface = 2`：Particles will be emitted on the surface of a sphere flattened to two dimensions.
- `Rectangle = 3`：Particles will be emitted in the area of a rectangle.
- `Points = 4`：Particles will be emitted at a position chosen randomly among [`CpuParticles2D.EmissionPoints`](CpuParticles2D.md).
- `DirectedPoints = 5`：Particles will be emitted at a position chosen randomly among [`CpuParticles2D.EmissionPoints`](CpuParticles2D.md).
- `Ring = 6`：Particles will be emitted in the area of a ring parameterized by its outer and inner radius.
- `Max = 7`：Represents the size of the `CpuParticles2D.EmissionShapeEnum` enum.

## Parameter

命名空间 `Godot`；enum

### 枚举值

- `InitialLinearVelocity = 0`：Use with [`CpuParticles2D.SetParamMin`](CpuParticles2D.md), [`CpuParticles2D.SetParamMax`](CpuParticles2D.md), and [`CpuParticles2D.SetParamCurve`](CpuParticles2D.md) to set initial velocity properties.
- `AngularVelocity = 1`：Use with [`CpuParticles2D.SetParamMin`](CpuParticles2D.md), [`CpuParticles2D.SetParamMax`](CpuParticles2D.md), and [`CpuParticles2D.SetParamCurve`](CpuParticles2D.md) to set angular velocity properties.
- `OrbitVelocity = 2`：Use with [`CpuParticles2D.SetParamMin`](CpuParticles2D.md), [`CpuParticles2D.SetParamMax`](CpuParticles2D.md), and [`CpuParticles2D.SetParamCurve`](CpuParticles2D.md) to set orbital velocity properties.
- `LinearAccel = 3`：Use with [`CpuParticles2D.SetParamMin`](CpuParticles2D.md), [`CpuParticles2D.SetParamMax`](CpuParticles2D.md), and [`CpuParticles2D.SetParamCurve`](CpuParticles2D.md) to set linear acceleration properties.
- `RadialAccel = 4`：Use with [`CpuParticles2D.SetParamMin`](CpuParticles2D.md), [`CpuParticles2D.SetParamMax`](CpuParticles2D.md), and [`CpuParticles2D.SetParamCurve`](CpuParticles2D.md) to set radial acceleration properties.
- `TangentialAccel = 5`：Use with [`CpuParticles2D.SetParamMin`](CpuParticles2D.md), [`CpuParticles2D.SetParamMax`](CpuParticles2D.md), and [`CpuParticles2D.SetParamCurve`](CpuParticles2D.md) to set tangential acceleration properties.
- `Damping = 6`：Use with [`CpuParticles2D.SetParamMin`](CpuParticles2D.md), [`CpuParticles2D.SetParamMax`](CpuParticles2D.md), and [`CpuParticles2D.SetParamCurve`](CpuParticles2D.md) to set damping properties.
- `Angle = 7`：Use with [`CpuParticles2D.SetParamMin`](CpuParticles2D.md), [`CpuParticles2D.SetParamMax`](CpuParticles2D.md), and [`CpuParticles2D.SetParamCurve`](CpuParticles2D.md) to set angle properties.
- `Scale = 8`：Use with [`CpuParticles2D.SetParamMin`](CpuParticles2D.md), [`CpuParticles2D.SetParamMax`](CpuParticles2D.md), and [`CpuParticles2D.SetParamCurve`](CpuParticles2D.md) to set scale properties.
- `HueVariation = 9`：Use with [`CpuParticles2D.SetParamMin`](CpuParticles2D.md), [`CpuParticles2D.SetParamMax`](CpuParticles2D.md), and [`CpuParticles2D.SetParamCurve`](CpuParticles2D.md) to set hue variation properties.
- `AnimSpeed = 10`：Use with [`CpuParticles2D.SetParamMin`](CpuParticles2D.md), [`CpuParticles2D.SetParamMax`](CpuParticles2D.md), and [`CpuParticles2D.SetParamCurve`](CpuParticles2D.md) to set animation speed properties.
- `AnimOffset = 11`：Use with [`CpuParticles2D.SetParamMin`](CpuParticles2D.md), [`CpuParticles2D.SetParamMax`](CpuParticles2D.md), and [`CpuParticles2D.SetParamCurve`](CpuParticles2D.md) to set animation offset properties.
- `Max = 12`：Represents the size of the `CpuParticles2D.Parameter` enum.

## ParticleFlags

命名空间 `Godot`；enum

### 枚举值

- `AlignYToVelocity = 0`：Use with [`CpuParticles2D.SetParticleFlag`](CpuParticles2D.md) to set [`CpuParticles2D.ParticleFlagAlignY`](CpuParticles2D.md).
- `RotateY = 1`：Present for consistency with 3D particle nodes, not used in 2D.
- `DisableZ = 2`：Present for consistency with 3D particle nodes, not used in 2D.
- `Max = 3`：Represents the size of the `CpuParticles2D.ParticleFlags` enum.
