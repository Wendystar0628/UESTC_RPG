<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ParticleProcessMaterial

命名空间 `Godot`；class；继承 [`Material`](Material.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`ParticleProcessMaterial` defines particle properties and behavior. It is used in the `process_material` of the [`GpuParticles2D`](GpuParticles2D.md) and [`GpuParticles3D`](GpuParticles3D.md) nodes. Some of this material's properties are applied to each particle when emitted, while others can have a [`CurveTexture`](CurveTexture.md) or a [`GradientTexture1D`](GradientTexture1D.md) applied to vary numerical or color values over the lifetime of the particle.

## 构造函数

### ParticleProcessMaterial

```csharp
public ParticleProcessMaterial()
```

## 属性

### AlphaCurve

```csharp
public Texture2D AlphaCurve { get; set; }
```

引擎名：`alpha_curve`

The alpha value of each particle's color will be multiplied by this [`CurveTexture`](CurveTexture.md) over its lifetime.

**Note:** `AlphaCurve` multiplies the particle mesh's vertex colors. To have a visible effect on a [`BaseMaterial3D`](BaseMaterial3D.md), [`BaseMaterial3D.VertexColorUseAsAlbedo`](BaseMaterial3D.md) *must* be `true`. For a [`ShaderMaterial`](ShaderMaterial.md), `ALPHA *= COLOR.a;` must be inserted in the shader's `fragment()` function. Otherwise, `AlphaCurve` will have no visible effect.

### Angle

```csharp
public Vector2 Angle { get; set; }
```

引擎名：`angle`

### AngleCurve

```csharp
public Texture2D AngleCurve { get; set; }
```

引擎名：`angle_curve`

Each particle's rotation will be animated along this [`CurveTexture`](CurveTexture.md).

### AngleMax

```csharp
public float AngleMax { get; set; }
```

引擎名：`angle_max`

Maximum initial rotation applied to each particle, in degrees.

Only applied when `ParticleFlagDisableZ` or `ParticleFlagRotateY` are `true` or the [`BaseMaterial3D`](BaseMaterial3D.md) being used to draw the particle is using [`BaseMaterial3D.BillboardModeEnum.Particles`](BaseMaterial3D.md).

### AngleMin

```csharp
public float AngleMin { get; set; }
```

引擎名：`angle_min`

Minimum equivalent of `AngleMax`.

### AngularVelocity

```csharp
public Vector2 AngularVelocity { get; set; }
```

引擎名：`angular_velocity`

### AngularVelocityCurve

```csharp
public Texture2D AngularVelocityCurve { get; set; }
```

引擎名：`angular_velocity_curve`

Each particle's angular velocity (rotation speed) will vary along this [`CurveTexture`](CurveTexture.md) over its lifetime.

### AngularVelocityMax

```csharp
public float AngularVelocityMax { get; set; }
```

引擎名：`angular_velocity_max`

Maximum initial angular velocity (rotation speed) applied to each particle in *degrees* per second.

Only applied when `ParticleFlagDisableZ` or `ParticleFlagRotateY` are `true` or the [`BaseMaterial3D`](BaseMaterial3D.md) being used to draw the particle is using [`BaseMaterial3D.BillboardModeEnum.Particles`](BaseMaterial3D.md).

### AngularVelocityMin

```csharp
public float AngularVelocityMin { get; set; }
```

引擎名：`angular_velocity_min`

Minimum equivalent of `AngularVelocityMax`.

### AnimOffset

```csharp
public Vector2 AnimOffset { get; set; }
```

引擎名：`anim_offset`

### AnimOffsetCurve

```csharp
public Texture2D AnimOffsetCurve { get; set; }
```

引擎名：`anim_offset_curve`

Each particle's animation offset will vary along this [`CurveTexture`](CurveTexture.md).

### AnimOffsetMax

```csharp
public float AnimOffsetMax { get; set; }
```

引擎名：`anim_offset_max`

Maximum animation offset that corresponds to frame index in the texture. `0` is the first frame, `1` is the last one. See [`CanvasItemMaterial.ParticlesAnimation`](CanvasItemMaterial.md).

### AnimOffsetMin

```csharp
public float AnimOffsetMin { get; set; }
```

引擎名：`anim_offset_min`

Minimum equivalent of `AnimOffsetMax`.

### AnimSpeed

```csharp
public Vector2 AnimSpeed { get; set; }
```

引擎名：`anim_speed`

### AnimSpeedCurve

```csharp
public Texture2D AnimSpeedCurve { get; set; }
```

引擎名：`anim_speed_curve`

Each particle's animation speed will vary along this [`CurveTexture`](CurveTexture.md).

### AnimSpeedMax

```csharp
public float AnimSpeedMax { get; set; }
```

引擎名：`anim_speed_max`

Maximum particle animation speed. Animation speed of `1` means that the particles will make full `0` to `1` offset cycle during lifetime, `2` means `2` cycles etc.

With animation speed greater than `1`, remember to enable [`CanvasItemMaterial.ParticlesAnimLoop`](CanvasItemMaterial.md) property if you want the animation to repeat.

### AnimSpeedMin

```csharp
public float AnimSpeedMin { get; set; }
```

引擎名：`anim_speed_min`

Minimum equivalent of `AnimSpeedMax`.

### AttractorInteractionEnabled

```csharp
public bool AttractorInteractionEnabled { get; set; }
```

引擎名：`attractor_interaction_enabled`

If `true`, interaction with particle attractors is enabled. In 3D, attraction only occurs within the area defined by the [`GpuParticles3D`](GpuParticles3D.md) node's [`GpuParticles3D.VisibilityAabb`](GpuParticles3D.md).

### CollisionBounce

```csharp
public float CollisionBounce { get; set; }
```

引擎名：`collision_bounce`

The particles' bounciness. Values range from `0` (no bounce) to `1` (full bounciness). Only effective if `CollisionMode` is [`ParticleProcessMaterial.CollisionModeEnum.Rigid`](ParticleProcessMaterial.md).

### CollisionFriction

```csharp
public float CollisionFriction { get; set; }
```

引擎名：`collision_friction`

The particles' friction. Values range from `0` (frictionless) to `1` (maximum friction). Only effective if `CollisionMode` is [`ParticleProcessMaterial.CollisionModeEnum.Rigid`](ParticleProcessMaterial.md).

### CollisionMode

```csharp
public ParticleProcessMaterial.CollisionModeEnum CollisionMode { get; set; }
```

引擎名：`collision_mode`

The particles' collision mode.

**Note:** 3D Particles can only collide with [`GpuParticlesCollision3D`](GpuParticlesCollision3D.md) nodes, not [`PhysicsBody3D`](PhysicsBody3D.md) nodes. To make particles collide with various objects, you can add [`GpuParticlesCollision3D`](GpuParticlesCollision3D.md) nodes as children of [`PhysicsBody3D`](PhysicsBody3D.md) nodes. In 3D, collisions only occur within the area defined by the [`GpuParticles3D`](GpuParticles3D.md) node's [`GpuParticles3D.VisibilityAabb`](GpuParticles3D.md).

**Note:** 2D Particles can only collide with [`LightOccluder2D`](LightOccluder2D.md) nodes, not [`PhysicsBody2D`](PhysicsBody2D.md) nodes.

### CollisionUseScale

```csharp
public bool CollisionUseScale { get; set; }
```

引擎名：`collision_use_scale`

If `true`, [`GpuParticles3D.CollisionBaseSize`](GpuParticles3D.md) is multiplied by the particle's effective scale (see `ScaleMin`, `ScaleMax`, `ScaleCurve`, and `ScaleOverVelocityCurve`).

### Color

```csharp
public Color Color { get; set; }
```

引擎名：`color`

Each particle's initial color. If the [`GpuParticles2D`](GpuParticles2D.md)'s `texture` is defined, it will be multiplied by this color.

**Note:** `Color` multiplies the particle mesh's vertex colors. To have a visible effect on a [`BaseMaterial3D`](BaseMaterial3D.md), [`BaseMaterial3D.VertexColorUseAsAlbedo`](BaseMaterial3D.md) *must* be `true`. For a [`ShaderMaterial`](ShaderMaterial.md), `ALBEDO *= COLOR.rgb;` must be inserted in the shader's `fragment()` function. Otherwise, `Color` will have no visible effect.

### ColorInitialRamp

```csharp
public Texture2D ColorInitialRamp { get; set; }
```

引擎名：`color_initial_ramp`

Each particle's initial color will vary along this [`GradientTexture1D`](GradientTexture1D.md) (multiplied with `Color`).

**Note:** `ColorInitialRamp` multiplies the particle mesh's vertex colors. To have a visible effect on a [`BaseMaterial3D`](BaseMaterial3D.md), [`BaseMaterial3D.VertexColorUseAsAlbedo`](BaseMaterial3D.md) *must* be `true`. For a [`ShaderMaterial`](ShaderMaterial.md), `ALBEDO *= COLOR.rgb;` must be inserted in the shader's `fragment()` function. Otherwise, `ColorInitialRamp` will have no visible effect.

### ColorRamp

```csharp
public Texture2D ColorRamp { get; set; }
```

引擎名：`color_ramp`

Each particle's color will vary along this [`GradientTexture1D`](GradientTexture1D.md) over its lifetime (multiplied with `Color`).

**Note:** `ColorRamp` multiplies the particle mesh's vertex colors. To have a visible effect on a [`BaseMaterial3D`](BaseMaterial3D.md), [`BaseMaterial3D.VertexColorUseAsAlbedo`](BaseMaterial3D.md) *must* be `true`. For a [`ShaderMaterial`](ShaderMaterial.md), `ALBEDO *= COLOR.rgb;` must be inserted in the shader's `fragment()` function. Otherwise, `ColorRamp` will have no visible effect.

### Damping

```csharp
public Vector2 Damping { get; set; }
```

引擎名：`damping`

### DampingCurve

```csharp
public Texture2D DampingCurve { get; set; }
```

引擎名：`damping_curve`

Damping will vary along this [`CurveTexture`](CurveTexture.md).

### DampingMax

```csharp
public float DampingMax { get; set; }
```

引擎名：`damping_max`

The maximum rate at which particles lose velocity. For example value of `100` means that the particle will go from `100` velocity to `0` in `1` second.

### DampingMin

```csharp
public float DampingMin { get; set; }
```

引擎名：`damping_min`

Minimum equivalent of `DampingMax`.

### Direction

```csharp
public Vector3 Direction { get; set; }
```

引擎名：`direction`

Unit vector specifying the particles' emission direction.

### DirectionalVelocity

```csharp
public Vector2 DirectionalVelocity { get; set; }
```

引擎名：`directional_velocity`

### DirectionalVelocityCurve

```csharp
public Texture2D DirectionalVelocityCurve { get; set; }
```

引擎名：`directional_velocity_curve`

A curve that specifies the velocity along each of the axes of the particle system along its lifetime.

**Note:** Animated velocities will not be affected by damping, use `VelocityLimitCurve` instead.

### DirectionalVelocityMax

```csharp
public float DirectionalVelocityMax { get; set; }
```

引擎名：`directional_velocity_max`

Maximum directional velocity value, which is multiplied by `DirectionalVelocityCurve`.

**Note:** Animated velocities will not be affected by damping, use `VelocityLimitCurve` instead.

### DirectionalVelocityMin

```csharp
public float DirectionalVelocityMin { get; set; }
```

引擎名：`directional_velocity_min`

Minimum directional velocity value, which is multiplied by `DirectionalVelocityCurve`.

**Note:** Animated velocities will not be affected by damping, use `VelocityLimitCurve` instead.

### EmissionBoxExtents

```csharp
public Vector3 EmissionBoxExtents { get; set; }
```

引擎名：`emission_box_extents`

The box's extents if `EmissionShape` is set to [`ParticleProcessMaterial.EmissionShapeEnum.Box`](ParticleProcessMaterial.md).

**Note:** `EmissionBoxExtents` starts from the center point and applies the X, Y, and Z values in both directions. The size is twice the area of the extents.

### EmissionColorTexture

```csharp
public Texture2D EmissionColorTexture { get; set; }
```

引擎名：`emission_color_texture`

Particle color will be modulated by color determined by sampling this texture at the same point as the `EmissionPointTexture`.

**Note:** `EmissionColorTexture` multiplies the particle mesh's vertex colors. To have a visible effect on a [`BaseMaterial3D`](BaseMaterial3D.md), [`BaseMaterial3D.VertexColorUseAsAlbedo`](BaseMaterial3D.md) *must* be `true`. For a [`ShaderMaterial`](ShaderMaterial.md), `ALBEDO *= COLOR.rgb;` must be inserted in the shader's `fragment()` function. Otherwise, `EmissionColorTexture` will have no visible effect.

### EmissionCurve

```csharp
public Texture2D EmissionCurve { get; set; }
```

引擎名：`emission_curve`

Each particle's color will be multiplied by this [`CurveTexture`](CurveTexture.md) over its lifetime.

**Note:** `EmissionCurve` multiplies the particle mesh's vertex colors. To have a visible effect on a [`BaseMaterial3D`](BaseMaterial3D.md), [`BaseMaterial3D.VertexColorUseAsAlbedo`](BaseMaterial3D.md) *must* be `true`. For a [`ShaderMaterial`](ShaderMaterial.md), `ALBEDO *= COLOR.rgb;` must be inserted in the shader's `fragment()` function. Otherwise, `EmissionCurve` will have no visible effect.

### EmissionNormalTexture

```csharp
public Texture2D EmissionNormalTexture { get; set; }
```

引擎名：`emission_normal_texture`

Particle velocity and rotation will be set by sampling this texture at the same point as the `EmissionPointTexture`. Used only in [`ParticleProcessMaterial.EmissionShapeEnum.DirectedPoints`](ParticleProcessMaterial.md). Can be created automatically from mesh or node by selecting "Create Emission Points from Mesh/Node" under the "Particles" tool in the toolbar.

### EmissionPointCount

```csharp
public int EmissionPointCount { get; set; }
```

引擎名：`emission_point_count`

The number of emission points if `EmissionShape` is set to [`ParticleProcessMaterial.EmissionShapeEnum.Points`](ParticleProcessMaterial.md) or [`ParticleProcessMaterial.EmissionShapeEnum.DirectedPoints`](ParticleProcessMaterial.md).

### EmissionPointTexture

```csharp
public Texture2D EmissionPointTexture { get; set; }
```

引擎名：`emission_point_texture`

Particles will be emitted at positions determined by sampling this texture at a random position. Used with [`ParticleProcessMaterial.EmissionShapeEnum.Points`](ParticleProcessMaterial.md) and [`ParticleProcessMaterial.EmissionShapeEnum.DirectedPoints`](ParticleProcessMaterial.md). Can be created automatically from mesh or node by selecting "Create Emission Points from Mesh/Node" under the "Particles" tool in the toolbar.

### EmissionRingAxis

```csharp
public Vector3 EmissionRingAxis { get; set; }
```

引擎名：`emission_ring_axis`

The axis of the ring when using the emitter [`ParticleProcessMaterial.EmissionShapeEnum.Ring`](ParticleProcessMaterial.md).

### EmissionRingConeAngle

```csharp
public float EmissionRingConeAngle { get; set; }
```

引擎名：`emission_ring_cone_angle`

The angle of the cone when using the emitter [`ParticleProcessMaterial.EmissionShapeEnum.Ring`](ParticleProcessMaterial.md). The default angle of 90 degrees results in a ring, while an angle of 0 degrees results in a cone. Intermediate values will result in a ring where one end is larger than the other.

**Note:** Depending on `EmissionRingHeight`, the angle may be clamped if the ring's end is reached to form a perfect cone.

### EmissionRingHeight

```csharp
public float EmissionRingHeight { get; set; }
```

引擎名：`emission_ring_height`

The height of the ring when using the emitter [`ParticleProcessMaterial.EmissionShapeEnum.Ring`](ParticleProcessMaterial.md).

### EmissionRingInnerRadius

```csharp
public float EmissionRingInnerRadius { get; set; }
```

引擎名：`emission_ring_inner_radius`

The inner radius of the ring when using the emitter [`ParticleProcessMaterial.EmissionShapeEnum.Ring`](ParticleProcessMaterial.md).

### EmissionRingRadius

```csharp
public float EmissionRingRadius { get; set; }
```

引擎名：`emission_ring_radius`

The radius of the ring when using the emitter [`ParticleProcessMaterial.EmissionShapeEnum.Ring`](ParticleProcessMaterial.md).

### EmissionShape

```csharp
public ParticleProcessMaterial.EmissionShapeEnum EmissionShape { get; set; }
```

引擎名：`emission_shape`

Particles will be emitted inside this region.

### EmissionShapeOffset

```csharp
public Vector3 EmissionShapeOffset { get; set; }
```

引擎名：`emission_shape_offset`

The offset for the `EmissionShape`, in local space.

### EmissionShapeScale

```csharp
public Vector3 EmissionShapeScale { get; set; }
```

引擎名：`emission_shape_scale`

The scale of the `EmissionShape`, in local space.

### EmissionSphereRadius

```csharp
public float EmissionSphereRadius { get; set; }
```

引擎名：`emission_sphere_radius`

The sphere's radius if `EmissionShape` is set to [`ParticleProcessMaterial.EmissionShapeEnum.Sphere`](ParticleProcessMaterial.md).

### Flatness

```csharp
public float Flatness { get; set; }
```

引擎名：`flatness`

Amount of `Spread` along the Y axis.

### Gravity

```csharp
public Vector3 Gravity { get; set; }
```

引擎名：`gravity`

Gravity applied to every particle.

### HueVariation

```csharp
public Vector2 HueVariation { get; set; }
```

引擎名：`hue_variation`

### HueVariationCurve

```csharp
public Texture2D HueVariationCurve { get; set; }
```

引擎名：`hue_variation_curve`

Each particle's hue will vary along this [`CurveTexture`](CurveTexture.md).

### HueVariationMax

```csharp
public float HueVariationMax { get; set; }
```

引擎名：`hue_variation_max`

Maximum initial hue variation applied to each particle. It will shift the particle color's hue.

### HueVariationMin

```csharp
public float HueVariationMin { get; set; }
```

引擎名：`hue_variation_min`

Minimum equivalent of `HueVariationMax`.

### InheritVelocityRatio

```csharp
public double InheritVelocityRatio { get; set; }
```

引擎名：`inherit_velocity_ratio`

Percentage of the velocity of the respective [`GpuParticles2D`](GpuParticles2D.md) or [`GpuParticles3D`](GpuParticles3D.md) inherited by each particle when spawning.

### InitialVelocity

```csharp
public Vector2 InitialVelocity { get; set; }
```

引擎名：`initial_velocity`

### InitialVelocityMax

```csharp
public float InitialVelocityMax { get; set; }
```

引擎名：`initial_velocity_max`

Maximum initial velocity magnitude for each particle. Direction comes from `Direction` and `Spread`.

### InitialVelocityMin

```csharp
public float InitialVelocityMin { get; set; }
```

引擎名：`initial_velocity_min`

Minimum equivalent of `InitialVelocityMax`.

### LifetimeRandomness

```csharp
public double LifetimeRandomness { get; set; }
```

引擎名：`lifetime_randomness`

Particle lifetime randomness ratio. The equation for the lifetime of a particle is `lifetime * (1.0 - randf() * lifetime_randomness)`. For example, a `LifetimeRandomness` of `0.4` scales the lifetime between `0.6` to `1.0` of its original value.

### LinearAccel

```csharp
public Vector2 LinearAccel { get; set; }
```

引擎名：`linear_accel`

### LinearAccelCurve

```csharp
public Texture2D LinearAccelCurve { get; set; }
```

引擎名：`linear_accel_curve`

Each particle's linear acceleration will vary along this [`CurveTexture`](CurveTexture.md).

### LinearAccelMax

```csharp
public float LinearAccelMax { get; set; }
```

引擎名：`linear_accel_max`

Maximum linear acceleration applied to each particle in the direction of motion.

### LinearAccelMin

```csharp
public float LinearAccelMin { get; set; }
```

引擎名：`linear_accel_min`

Minimum equivalent of `LinearAccelMax`.

### OrbitVelocity

```csharp
public Vector2 OrbitVelocity { get; set; }
```

引擎名：`orbit_velocity`

### OrbitVelocityCurve

```csharp
public Texture2D OrbitVelocityCurve { get; set; }
```

引擎名：`orbit_velocity_curve`

Each particle's orbital velocity will vary along this [`CurveTexture`](CurveTexture.md).

**Note:** For 3D orbital velocity, use a [`CurveXyzTexture`](CurveXyzTexture.md).

**Note:** Animated velocities will not be affected by damping, use `VelocityLimitCurve` instead.

### OrbitVelocityMax

```csharp
public float OrbitVelocityMax { get; set; }
```

引擎名：`orbit_velocity_max`

Maximum orbital velocity applied to each particle. Makes the particles circle around origin. Specified in number of full rotations around origin per second.

**Note:** Animated velocities will not be affected by damping, use `VelocityLimitCurve` instead.

### OrbitVelocityMin

```csharp
public float OrbitVelocityMin { get; set; }
```

引擎名：`orbit_velocity_min`

Minimum equivalent of `OrbitVelocityMax`.

**Note:** Animated velocities will not be affected by damping, use `VelocityLimitCurve` instead.

### ParticleFlagAlignY

```csharp
public bool ParticleFlagAlignY { get; set; }
```

引擎名：`particle_flag_align_y`

Align Y axis of particle with the direction of its velocity.

### ParticleFlagDampingAsFriction

```csharp
public bool ParticleFlagDampingAsFriction { get; set; }
```

引擎名：`particle_flag_damping_as_friction`

Changes the behavior of the damping properties from a linear deceleration to a deceleration based on speed percentage.

### ParticleFlagDisableZ

```csharp
public bool ParticleFlagDisableZ { get; set; }
```

引擎名：`particle_flag_disable_z`

If `true`, particles will not move on the z axis.

### ParticleFlagInheritEmitterScale

```csharp
public bool ParticleFlagInheritEmitterScale { get; set; }
```

引擎名：`particle_flag_inherit_emitter_scale`

If `true`, particles will inherit the scale of the emitter.

**Note:** This has no effect when [`GpuParticles3D.LocalCoords`](GpuParticles3D.md) is `true`, since particles in local space are already affected by the emitter's scale.

### ParticleFlagRotateY

```csharp
public bool ParticleFlagRotateY { get; set; }
```

引擎名：`particle_flag_rotate_y`

If `true`, particles rotate around Y axis by `AngleMin`.

### RadialAccel

```csharp
public Vector2 RadialAccel { get; set; }
```

引擎名：`radial_accel`

### RadialAccelCurve

```csharp
public Texture2D RadialAccelCurve { get; set; }
```

引擎名：`radial_accel_curve`

Each particle's radial acceleration will vary along this [`CurveTexture`](CurveTexture.md).

### RadialAccelMax

```csharp
public float RadialAccelMax { get; set; }
```

引擎名：`radial_accel_max`

Maximum radial acceleration applied to each particle. Makes particle accelerate away from the origin or towards it if negative.

### RadialAccelMin

```csharp
public float RadialAccelMin { get; set; }
```

引擎名：`radial_accel_min`

Minimum equivalent of `RadialAccelMax`.

### RadialVelocity

```csharp
public Vector2 RadialVelocity { get; set; }
```

引擎名：`radial_velocity`

### RadialVelocityCurve

```csharp
public Texture2D RadialVelocityCurve { get; set; }
```

引擎名：`radial_velocity_curve`

A [`CurveTexture`](CurveTexture.md) that defines the velocity over the particle's lifetime away (or toward) the `VelocityPivot`.

**Note:** Animated velocities will not be affected by damping, use `VelocityLimitCurve` instead.

### RadialVelocityMax

```csharp
public float RadialVelocityMax { get; set; }
```

引擎名：`radial_velocity_max`

Maximum radial velocity applied to each particle. Makes particles move away from the `VelocityPivot`, or toward it if negative.

**Note:** Animated velocities will not be affected by damping, use `VelocityLimitCurve` instead.

### RadialVelocityMin

```csharp
public float RadialVelocityMin { get; set; }
```

引擎名：`radial_velocity_min`

Minimum radial velocity applied to each particle. Makes particles move away from the `VelocityPivot`, or toward it if negative.

**Note:** Animated velocities will not be affected by damping, use `VelocityLimitCurve` instead.

### Rotation3DMax

```csharp
public Vector3 Rotation3DMax { get; set; }
```

The maximum 3D orientation, in degrees. Works only in 3D and if `UseRotation3D` is enabled.

### Rotation3DMin

```csharp
public Vector3 Rotation3DMin { get; set; }
```

The minimum 3D orientation, in degrees. Works only in 3D and if `UseRotation3D` is enabled.

### RotationVelocity3DCurve

```csharp
public Texture2D RotationVelocity3DCurve { get; set; }
```

Rotation velocity curve over lifetime, per-axis. Enable `UseRotationVelocity3D` to use this.

### RotationVelocity3DMax

```csharp
public Vector3 RotationVelocity3DMax { get; set; }
```

Maximum 3D rotation velocity on the particle's local axis. Enable `UseRotationVelocity3D` to use this.

### RotationVelocity3DMin

```csharp
public Vector3 RotationVelocity3DMin { get; set; }
```

Minimum 3D rotation velocity on the particle's local axis. Enable `UseRotationVelocity3D` to use this.

### Scale

```csharp
public Vector2 Scale { get; set; }
```

引擎名：`scale`

### Scale3DMax

```csharp
public Vector3 Scale3DMax { get; set; }
```

The maximum value of the random scale vector for each particle.

Works only if `UseScale3D` is enabled.

### Scale3DMin

```csharp
public Vector3 Scale3DMin { get; set; }
```

The minimum value of the random scale vector for each particle.

Works only if `UseScale3D` is enabled.

### ScaleCurve

```csharp
public Texture2D ScaleCurve { get; set; }
```

引擎名：`scale_curve`

Each particle's scale will vary along this [`CurveTexture`](CurveTexture.md) over its lifetime. If a [`CurveXyzTexture`](CurveXyzTexture.md) is supplied instead, the scale will be separated per-axis.

### ScaleMax

```csharp
public float ScaleMax { get; set; }
```

引擎名：`scale_max`

Maximum initial scale applied to each particle.

### ScaleMin

```csharp
public float ScaleMin { get; set; }
```

引擎名：`scale_min`

Minimum equivalent of `ScaleMax`.

### ScaleOverVelocity

```csharp
public Vector2 ScaleOverVelocity { get; set; }
```

引擎名：`scale_over_velocity`

### ScaleOverVelocityCurve

```csharp
public Texture2D ScaleOverVelocityCurve { get; set; }
```

引擎名：`scale_over_velocity_curve`

Either a [`CurveTexture`](CurveTexture.md) or a [`CurveXyzTexture`](CurveXyzTexture.md) that scales each particle based on its velocity.

### ScaleOverVelocityMax

```csharp
public float ScaleOverVelocityMax { get; set; }
```

引擎名：`scale_over_velocity_max`

Maximum velocity value reference for `ScaleOverVelocityCurve`.

`ScaleOverVelocityCurve` will be interpolated between `ScaleOverVelocityMin` and `ScaleOverVelocityMax`.

### ScaleOverVelocityMin

```csharp
public float ScaleOverVelocityMin { get; set; }
```

引擎名：`scale_over_velocity_min`

Minimum velocity value reference for `ScaleOverVelocityCurve`.

`ScaleOverVelocityCurve` will be interpolated between `ScaleOverVelocityMin` and `ScaleOverVelocityMax`.

### Spread

```csharp
public float Spread { get; set; }
```

引擎名：`spread`

Each particle's initial direction range from `+spread` to `-spread` degrees.

### SubEmitterAmountAtCollision

```csharp
public int SubEmitterAmountAtCollision { get; set; }
```

引擎名：`sub_emitter_amount_at_collision`

The amount of particles to spawn from the subemitter node when a collision occurs. When combined with [`ParticleProcessMaterial.CollisionModeEnum.HideOnContact`](ParticleProcessMaterial.md) on the main particles material, this can be used to achieve effects such as raindrops hitting the ground.

**Note:** This value shouldn't exceed [`GpuParticles2D.Amount`](GpuParticles2D.md) or [`GpuParticles3D.Amount`](GpuParticles3D.md) defined on the *subemitter node* (not the main node), relative to the subemitter's particle lifetime. If the number of particles is exceeded, no new particles will spawn from the subemitter until enough particles have expired.

### SubEmitterAmountAtEnd

```csharp
public int SubEmitterAmountAtEnd { get; set; }
```

引擎名：`sub_emitter_amount_at_end`

The amount of particles to spawn from the subemitter node when the particle expires.

**Note:** This value shouldn't exceed [`GpuParticles2D.Amount`](GpuParticles2D.md) or [`GpuParticles3D.Amount`](GpuParticles3D.md) defined on the *subemitter node* (not the main node), relative to the subemitter's particle lifetime. If the number of particles is exceeded, no new particles will spawn from the subemitter until enough particles have expired.

### SubEmitterAmountAtStart

```csharp
public int SubEmitterAmountAtStart { get; set; }
```

引擎名：`sub_emitter_amount_at_start`

The amount of particles to spawn from the subemitter node when the particle spawns.

**Note:** This value shouldn't exceed [`GpuParticles2D.Amount`](GpuParticles2D.md) or [`GpuParticles3D.Amount`](GpuParticles3D.md) defined on the *subemitter node* (not the main node), relative to the subemitter's particle lifetime. If the number of particles is exceeded, no new particles will spawn from the subemitter until enough particles have expired.

### SubEmitterFrequency

```csharp
public double SubEmitterFrequency { get; set; }
```

引擎名：`sub_emitter_frequency`

The frequency at which particles should be emitted from the subemitter node. One particle will be spawned every `SubEmitterFrequency` seconds.

**Note:** This value shouldn't exceed [`GpuParticles2D.Amount`](GpuParticles2D.md) or [`GpuParticles3D.Amount`](GpuParticles3D.md) defined on the *subemitter node* (not the main node), relative to the subemitter's particle lifetime. If the number of particles is exceeded, no new particles will spawn from the subemitter until enough particles have expired.

### SubEmitterKeepVelocity

```csharp
public bool SubEmitterKeepVelocity { get; set; }
```

引擎名：`sub_emitter_keep_velocity`

If `true`, the subemitter inherits the parent particle's velocity when it spawns.

### SubEmitterMode

```csharp
public ParticleProcessMaterial.SubEmitterModeEnum SubEmitterMode { get; set; }
```

引擎名：`sub_emitter_mode`

The particle subemitter mode (see [`GpuParticles2D.SubEmitter`](GpuParticles2D.md) and [`GpuParticles3D.SubEmitter`](GpuParticles3D.md)).

### TangentialAccel

```csharp
public Vector2 TangentialAccel { get; set; }
```

引擎名：`tangential_accel`

### TangentialAccelCurve

```csharp
public Texture2D TangentialAccelCurve { get; set; }
```

引擎名：`tangential_accel_curve`

Each particle's tangential acceleration will vary along this [`CurveTexture`](CurveTexture.md).

### TangentialAccelMax

```csharp
public float TangentialAccelMax { get; set; }
```

引擎名：`tangential_accel_max`

Maximum tangential acceleration applied to each particle. Tangential acceleration is perpendicular to the particle's velocity giving the particles a swirling motion.

### TangentialAccelMin

```csharp
public float TangentialAccelMin { get; set; }
```

引擎名：`tangential_accel_min`

Minimum equivalent of `TangentialAccelMax`.

### TurbulenceEnabled

```csharp
public bool TurbulenceEnabled { get; set; }
```

引擎名：`turbulence_enabled`

If `true`, enables turbulence for the particle system. Turbulence can be used to vary particle movement according to its position (based on a 3D noise pattern). In 3D, [`GpuParticlesAttractorVectorField3D`](GpuParticlesAttractorVectorField3D.md) with [`NoiseTexture3D`](NoiseTexture3D.md) can be used as an alternative to turbulence that works in world space and with multiple particle systems reacting in the same way.

**Note:** Enabling turbulence has a high performance cost on the GPU. Only enable turbulence on a few particle systems at once at most, and consider disabling it when targeting mobile/web platforms.

### TurbulenceInfluence

```csharp
public Vector2 TurbulenceInfluence { get; set; }
```

引擎名：`turbulence_influence`

### TurbulenceInfluenceMax

```csharp
public float TurbulenceInfluenceMax { get; set; }
```

引擎名：`turbulence_influence_max`

Maximum turbulence influence on each particle.

The actual amount of turbulence influence on each particle is calculated as a random value between `TurbulenceInfluenceMin` and `TurbulenceInfluenceMax` and multiplied by the amount of turbulence influence from `TurbulenceInfluenceOverLife`.

### TurbulenceInfluenceMin

```csharp
public float TurbulenceInfluenceMin { get; set; }
```

引擎名：`turbulence_influence_min`

Minimum turbulence influence on each particle.

The actual amount of turbulence influence on each particle is calculated as a random value between `TurbulenceInfluenceMin` and `TurbulenceInfluenceMax` and multiplied by the amount of turbulence influence from `TurbulenceInfluenceOverLife`.

### TurbulenceInfluenceOverLife

```csharp
public Texture2D TurbulenceInfluenceOverLife { get; set; }
```

引擎名：`turbulence_influence_over_life`

Each particle's amount of turbulence will be influenced along this [`CurveTexture`](CurveTexture.md) over its life time.

### TurbulenceInitialDisplacement

```csharp
public Vector2 TurbulenceInitialDisplacement { get; set; }
```

引擎名：`turbulence_initial_displacement`

### TurbulenceInitialDisplacementMax

```csharp
public float TurbulenceInitialDisplacementMax { get; set; }
```

引擎名：`turbulence_initial_displacement_max`

Maximum displacement of each particle's spawn position by the turbulence.

The actual amount of displacement will be a factor of the underlying turbulence multiplied by a random value between `TurbulenceInitialDisplacementMin` and `TurbulenceInitialDisplacementMax`.

### TurbulenceInitialDisplacementMin

```csharp
public float TurbulenceInitialDisplacementMin { get; set; }
```

引擎名：`turbulence_initial_displacement_min`

Minimum displacement of each particle's spawn position by the turbulence.

The actual amount of displacement will be a factor of the underlying turbulence multiplied by a random value between `TurbulenceInitialDisplacementMin` and `TurbulenceInitialDisplacementMax`.

### TurbulenceNoiseScale

```csharp
public float TurbulenceNoiseScale { get; set; }
```

引擎名：`turbulence_noise_scale`

This value controls the overall scale/frequency of the turbulence noise pattern.

A small scale will result in smaller features with more detail while a high scale will result in smoother noise with larger features.

### TurbulenceNoiseSpeed

```csharp
public Vector3 TurbulenceNoiseSpeed { get; set; }
```

引擎名：`turbulence_noise_speed`

A scrolling velocity for the turbulence field. This sets a directional trend for the pattern to move in over time.

The default value of `Vector3(0, 0, 0)` turns off the scrolling.

### TurbulenceNoiseSpeedRandom

```csharp
public float TurbulenceNoiseSpeedRandom { get; set; }
```

引擎名：`turbulence_noise_speed_random`

The in-place rate of change of the turbulence field. This defines how quickly the noise pattern varies over time.

A value of 0.0 will result in a fixed pattern.

### TurbulenceNoiseStrength

```csharp
public float TurbulenceNoiseStrength { get; set; }
```

引擎名：`turbulence_noise_strength`

The turbulence noise strength. Increasing this will result in a stronger, more contrasting, flow pattern.

### UseRotation3D

```csharp
public bool UseRotation3D { get; set; }
```

Enable the usage of `Rotation3DMin` and `Rotation3DMax`.

### UseRotationVelocity3D

```csharp
public bool UseRotationVelocity3D { get; set; }
```

Enable 3D rotation velocity.

### UseScale3D

```csharp
public bool UseScale3D { get; set; }
```

Enable the usage of `Scale3DMin` and `Scale3DMax`.

### VelocityLimitCurve

```csharp
public Texture2D VelocityLimitCurve { get; set; }
```

引擎名：`velocity_limit_curve`

A [`CurveTexture`](CurveTexture.md) that defines the maximum velocity of a particle during its lifetime.

### VelocityPivot

```csharp
public Vector3 VelocityPivot { get; set; }
```

引擎名：`velocity_pivot`

A pivot point used to calculate radial and orbital velocity of particles.

## 方法

### GetAlphaCurve

```csharp
public Texture2D GetAlphaCurve()
```

引擎名：`get_alpha_curve`

### GetCollisionBounce

```csharp
public float GetCollisionBounce()
```

引擎名：`get_collision_bounce`

### GetCollisionFriction

```csharp
public float GetCollisionFriction()
```

引擎名：`get_collision_friction`

### GetCollisionMode

```csharp
public ParticleProcessMaterial.CollisionModeEnum GetCollisionMode()
```

引擎名：`get_collision_mode`

### GetColor

```csharp
public Color GetColor()
```

引擎名：`get_color`

### GetColorInitialRamp

```csharp
public Texture2D GetColorInitialRamp()
```

引擎名：`get_color_initial_ramp`

### GetColorRamp

```csharp
public Texture2D GetColorRamp()
```

引擎名：`get_color_ramp`

### GetDirection

```csharp
public Vector3 GetDirection()
```

引擎名：`get_direction`

### GetEmissionBoxExtents

```csharp
public Vector3 GetEmissionBoxExtents()
```

引擎名：`get_emission_box_extents`

### GetEmissionColorTexture

```csharp
public Texture2D GetEmissionColorTexture()
```

引擎名：`get_emission_color_texture`

### GetEmissionCurve

```csharp
public Texture2D GetEmissionCurve()
```

引擎名：`get_emission_curve`

### GetEmissionNormalTexture

```csharp
public Texture2D GetEmissionNormalTexture()
```

引擎名：`get_emission_normal_texture`

### GetEmissionPointCount

```csharp
public int GetEmissionPointCount()
```

引擎名：`get_emission_point_count`

### GetEmissionPointTexture

```csharp
public Texture2D GetEmissionPointTexture()
```

引擎名：`get_emission_point_texture`

### GetEmissionRingAxis

```csharp
public Vector3 GetEmissionRingAxis()
```

引擎名：`get_emission_ring_axis`

### GetEmissionRingConeAngle

```csharp
public float GetEmissionRingConeAngle()
```

引擎名：`get_emission_ring_cone_angle`

### GetEmissionRingHeight

```csharp
public float GetEmissionRingHeight()
```

引擎名：`get_emission_ring_height`

### GetEmissionRingInnerRadius

```csharp
public float GetEmissionRingInnerRadius()
```

引擎名：`get_emission_ring_inner_radius`

### GetEmissionRingRadius

```csharp
public float GetEmissionRingRadius()
```

引擎名：`get_emission_ring_radius`

### GetEmissionShape

```csharp
public ParticleProcessMaterial.EmissionShapeEnum GetEmissionShape()
```

引擎名：`get_emission_shape`

### GetEmissionShapeOffset

```csharp
public Vector3 GetEmissionShapeOffset()
```

引擎名：`get_emission_shape_offset`

### GetEmissionShapeScale

```csharp
public Vector3 GetEmissionShapeScale()
```

引擎名：`get_emission_shape_scale`

### GetEmissionSphereRadius

```csharp
public float GetEmissionSphereRadius()
```

引擎名：`get_emission_sphere_radius`

### GetFlatness

```csharp
public float GetFlatness()
```

引擎名：`get_flatness`

### GetGravity

```csharp
public Vector3 GetGravity()
```

引擎名：`get_gravity`

### GetInheritVelocityRatio

```csharp
public double GetInheritVelocityRatio()
```

引擎名：`get_inherit_velocity_ratio`

### GetLifetimeRandomness

```csharp
public double GetLifetimeRandomness()
```

引擎名：`get_lifetime_randomness`

### GetParam

```csharp
public Vector2 GetParam(ParticleProcessMaterial.Parameter param)
```

引擎名：`get_param`

Returns the minimum and maximum values of the given `param` as a vector.

The `x` component of the returned vector corresponds to minimum and the `y` component corresponds to maximum.

### GetParamMax

```csharp
public float GetParamMax(ParticleProcessMaterial.Parameter param)
```

引擎名：`get_param_max`

Returns the maximum value range for the given parameter.

### GetParamMin

```csharp
public float GetParamMin(ParticleProcessMaterial.Parameter param)
```

引擎名：`get_param_min`

Returns the minimum value range for the given parameter.

### GetParamTexture

```csharp
public Texture2D GetParamTexture(ParticleProcessMaterial.Parameter param)
```

引擎名：`get_param_texture`

Returns the [`Texture2D`](Texture2D.md) used by the specified parameter.

### GetParticleFlag

```csharp
public bool GetParticleFlag(ParticleProcessMaterial.ParticleFlags particleFlag)
```

引擎名：`get_particle_flag`

Returns `true` if the specified particle flag is enabled.

### GetRotation3DMax

```csharp
public Vector3 GetRotation3DMax()
```

### GetRotation3DMin

```csharp
public Vector3 GetRotation3DMin()
```

### GetRotationVelocity3DCurve

```csharp
public Texture2D GetRotationVelocity3DCurve()
```

### GetRotationVelocity3DMax

```csharp
public Vector3 GetRotationVelocity3DMax()
```

### GetRotationVelocity3DMin

```csharp
public Vector3 GetRotationVelocity3DMin()
```

### GetScale3DMax

```csharp
public Vector3 GetScale3DMax()
```

### GetScale3DMin

```csharp
public Vector3 GetScale3DMin()
```

### GetSpread

```csharp
public float GetSpread()
```

引擎名：`get_spread`

### GetSubEmitterAmountAtCollision

```csharp
public int GetSubEmitterAmountAtCollision()
```

引擎名：`get_sub_emitter_amount_at_collision`

### GetSubEmitterAmountAtEnd

```csharp
public int GetSubEmitterAmountAtEnd()
```

引擎名：`get_sub_emitter_amount_at_end`

### GetSubEmitterAmountAtStart

```csharp
public int GetSubEmitterAmountAtStart()
```

引擎名：`get_sub_emitter_amount_at_start`

### GetSubEmitterFrequency

```csharp
public double GetSubEmitterFrequency()
```

引擎名：`get_sub_emitter_frequency`

### GetSubEmitterKeepVelocity

```csharp
public bool GetSubEmitterKeepVelocity()
```

引擎名：`get_sub_emitter_keep_velocity`

### GetSubEmitterMode

```csharp
public ParticleProcessMaterial.SubEmitterModeEnum GetSubEmitterMode()
```

引擎名：`get_sub_emitter_mode`

### GetTurbulenceEnabled

```csharp
public bool GetTurbulenceEnabled()
```

引擎名：`get_turbulence_enabled`

### GetTurbulenceNoiseScale

```csharp
public float GetTurbulenceNoiseScale()
```

引擎名：`get_turbulence_noise_scale`

### GetTurbulenceNoiseSpeed

```csharp
public Vector3 GetTurbulenceNoiseSpeed()
```

引擎名：`get_turbulence_noise_speed`

### GetTurbulenceNoiseSpeedRandom

```csharp
public float GetTurbulenceNoiseSpeedRandom()
```

引擎名：`get_turbulence_noise_speed_random`

### GetTurbulenceNoiseStrength

```csharp
public float GetTurbulenceNoiseStrength()
```

引擎名：`get_turbulence_noise_strength`

### GetVelocityLimitCurve

```csharp
public Texture2D GetVelocityLimitCurve()
```

引擎名：`get_velocity_limit_curve`

### GetVelocityPivot

```csharp
public Vector3 GetVelocityPivot()
```

引擎名：`get_velocity_pivot`

### IsAttractorInteractionEnabled

```csharp
public bool IsAttractorInteractionEnabled()
```

引擎名：`is_attractor_interaction_enabled`

### IsCollisionUsingScale

```csharp
public bool IsCollisionUsingScale()
```

引擎名：`is_collision_using_scale`

### IsUsingRotation3D

```csharp
public bool IsUsingRotation3D()
```

### IsUsingRotationVelocity3D

```csharp
public bool IsUsingRotationVelocity3D()
```

### IsUsingScale3D

```csharp
public bool IsUsingScale3D()
```

### SetAlphaCurve

```csharp
public void SetAlphaCurve(Texture2D curve)
```

引擎名：`set_alpha_curve`

### SetAttractorInteractionEnabled

```csharp
public void SetAttractorInteractionEnabled(bool enabled)
```

引擎名：`set_attractor_interaction_enabled`

### SetCollisionBounce

```csharp
public void SetCollisionBounce(float bounce)
```

引擎名：`set_collision_bounce`

### SetCollisionFriction

```csharp
public void SetCollisionFriction(float friction)
```

引擎名：`set_collision_friction`

### SetCollisionMode

```csharp
public void SetCollisionMode(ParticleProcessMaterial.CollisionModeEnum mode)
```

引擎名：`set_collision_mode`

### SetCollisionUseScale

```csharp
public void SetCollisionUseScale(bool radius)
```

引擎名：`set_collision_use_scale`

### SetColor

```csharp
public void SetColor(Color color)
```

引擎名：`set_color`

### SetColorInitialRamp

```csharp
public void SetColorInitialRamp(Texture2D ramp)
```

引擎名：`set_color_initial_ramp`

### SetColorRamp

```csharp
public void SetColorRamp(Texture2D ramp)
```

引擎名：`set_color_ramp`

### SetDirection

```csharp
public void SetDirection(Vector3 degrees)
```

引擎名：`set_direction`

### SetEmissionBoxExtents

```csharp
public void SetEmissionBoxExtents(Vector3 extents)
```

引擎名：`set_emission_box_extents`

### SetEmissionColorTexture

```csharp
public void SetEmissionColorTexture(Texture2D texture)
```

引擎名：`set_emission_color_texture`

### SetEmissionCurve

```csharp
public void SetEmissionCurve(Texture2D curve)
```

引擎名：`set_emission_curve`

### SetEmissionNormalTexture

```csharp
public void SetEmissionNormalTexture(Texture2D texture)
```

引擎名：`set_emission_normal_texture`

### SetEmissionPointCount

```csharp
public void SetEmissionPointCount(int pointCount)
```

引擎名：`set_emission_point_count`

### SetEmissionPointTexture

```csharp
public void SetEmissionPointTexture(Texture2D texture)
```

引擎名：`set_emission_point_texture`

### SetEmissionRingAxis

```csharp
public void SetEmissionRingAxis(Vector3 axis)
```

引擎名：`set_emission_ring_axis`

### SetEmissionRingConeAngle

```csharp
public void SetEmissionRingConeAngle(float coneAngle)
```

引擎名：`set_emission_ring_cone_angle`

### SetEmissionRingHeight

```csharp
public void SetEmissionRingHeight(float height)
```

引擎名：`set_emission_ring_height`

### SetEmissionRingInnerRadius

```csharp
public void SetEmissionRingInnerRadius(float innerRadius)
```

引擎名：`set_emission_ring_inner_radius`

### SetEmissionRingRadius

```csharp
public void SetEmissionRingRadius(float radius)
```

引擎名：`set_emission_ring_radius`

### SetEmissionShape

```csharp
public void SetEmissionShape(ParticleProcessMaterial.EmissionShapeEnum shape)
```

引擎名：`set_emission_shape`

### SetEmissionShapeOffset

```csharp
public void SetEmissionShapeOffset(Vector3 emissionShapeOffset)
```

引擎名：`set_emission_shape_offset`

### SetEmissionShapeScale

```csharp
public void SetEmissionShapeScale(Vector3 emissionShapeScale)
```

引擎名：`set_emission_shape_scale`

### SetEmissionSphereRadius

```csharp
public void SetEmissionSphereRadius(float radius)
```

引擎名：`set_emission_sphere_radius`

### SetFlatness

```csharp
public void SetFlatness(float amount)
```

引擎名：`set_flatness`

### SetGravity

```csharp
public void SetGravity(Vector3 accelVec)
```

引擎名：`set_gravity`

### SetInheritVelocityRatio

```csharp
public void SetInheritVelocityRatio(double ratio)
```

引擎名：`set_inherit_velocity_ratio`

### SetLifetimeRandomness

```csharp
public void SetLifetimeRandomness(double randomness)
```

引擎名：`set_lifetime_randomness`

### SetParam

```csharp
public void SetParam(ParticleProcessMaterial.Parameter param, Vector2 value)
```

引擎名：`set_param`

Sets the minimum and maximum values of the given `param`.

The `x` component of the argument vector corresponds to minimum and the `y` component corresponds to maximum.

### SetParamMax

```csharp
public void SetParamMax(ParticleProcessMaterial.Parameter param, float value)
```

引擎名：`set_param_max`

Sets the maximum value range for the given parameter.

### SetParamMin

```csharp
public void SetParamMin(ParticleProcessMaterial.Parameter param, float value)
```

引擎名：`set_param_min`

Sets the minimum value range for the given parameter.

### SetParamTexture

```csharp
public void SetParamTexture(ParticleProcessMaterial.Parameter param, Texture2D texture)
```

引擎名：`set_param_texture`

Sets the [`Texture2D`](Texture2D.md) for the specified [`ParticleProcessMaterial.Parameter`](ParticleProcessMaterial.md).

### SetParticleFlag

```csharp
public void SetParticleFlag(ParticleProcessMaterial.ParticleFlags particleFlag, bool enable)
```

引擎名：`set_particle_flag`

Sets the `particleFlag` to `enable`.

### SetRotation3DMax

```csharp
public void SetRotation3DMax(Vector3 rotation3DMax)
```

### SetRotation3DMin

```csharp
public void SetRotation3DMin(Vector3 rotation3DMin)
```

### SetRotationVelocity3DCurve

```csharp
public void SetRotationVelocity3DCurve(Texture2D rotationVelocity3DCurve)
```

### SetRotationVelocity3DMax

```csharp
public void SetRotationVelocity3DMax(Vector3 rotationVelocity3DMax)
```

### SetRotationVelocity3DMin

```csharp
public void SetRotationVelocity3DMin(Vector3 rotationVelocity3DMin)
```

### SetScale3DMax

```csharp
public void SetScale3DMax(Vector3 scale3DMax)
```

### SetScale3DMin

```csharp
public void SetScale3DMin(Vector3 scale3DMin)
```

### SetSpread

```csharp
public void SetSpread(float degrees)
```

引擎名：`set_spread`

### SetSubEmitterAmountAtCollision

```csharp
public void SetSubEmitterAmountAtCollision(int amount)
```

引擎名：`set_sub_emitter_amount_at_collision`

### SetSubEmitterAmountAtEnd

```csharp
public void SetSubEmitterAmountAtEnd(int amount)
```

引擎名：`set_sub_emitter_amount_at_end`

### SetSubEmitterAmountAtStart

```csharp
public void SetSubEmitterAmountAtStart(int amount)
```

引擎名：`set_sub_emitter_amount_at_start`

### SetSubEmitterFrequency

```csharp
public void SetSubEmitterFrequency(double hz)
```

引擎名：`set_sub_emitter_frequency`

### SetSubEmitterKeepVelocity

```csharp
public void SetSubEmitterKeepVelocity(bool enable)
```

引擎名：`set_sub_emitter_keep_velocity`

### SetSubEmitterMode

```csharp
public void SetSubEmitterMode(ParticleProcessMaterial.SubEmitterModeEnum mode)
```

引擎名：`set_sub_emitter_mode`

### SetTurbulenceEnabled

```csharp
public void SetTurbulenceEnabled(bool turbulenceEnabled)
```

引擎名：`set_turbulence_enabled`

### SetTurbulenceNoiseScale

```csharp
public void SetTurbulenceNoiseScale(float turbulenceNoiseScale)
```

引擎名：`set_turbulence_noise_scale`

### SetTurbulenceNoiseSpeed

```csharp
public void SetTurbulenceNoiseSpeed(Vector3 turbulenceNoiseSpeed)
```

引擎名：`set_turbulence_noise_speed`

### SetTurbulenceNoiseSpeedRandom

```csharp
public void SetTurbulenceNoiseSpeedRandom(float turbulenceNoiseSpeedRandom)
```

引擎名：`set_turbulence_noise_speed_random`

### SetTurbulenceNoiseStrength

```csharp
public void SetTurbulenceNoiseStrength(float turbulenceNoiseStrength)
```

引擎名：`set_turbulence_noise_strength`

### SetUseRotation3D

```csharp
public void SetUseRotation3D(bool usingRotation3D)
```

### SetUseScale3D

```csharp
public void SetUseScale3D(bool usingScale3D)
```

### SetUsingRotationVelocity3D

```csharp
public void SetUsingRotationVelocity3D(bool useRotationVelocity3D)
```

### SetVelocityLimitCurve

```csharp
public void SetVelocityLimitCurve(Texture2D curve)
```

引擎名：`set_velocity_limit_curve`

### SetVelocityPivot

```csharp
public void SetVelocityPivot(Vector3 pivot)
```

引擎名：`set_velocity_pivot`

## 事件（信号）

### EmissionShapeChanged

```csharp
public event Action EmissionShapeChanged
```

引擎名：`emission_shape_changed`

Emitted when this material's emission shape is changed in any way. This includes changes to `EmissionShape`, `EmissionShapeScale`, or `EmissionSphereRadius`, and any other property that affects the emission shape's offset, size, scale, or orientation.

**Note:** This signal is only emitted inside the editor for performance reasons.

## CollisionModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：No collision for particles.
- `Rigid = 1`：[`RigidBody3D`](RigidBody3D.md)-style collision for particles using [`GpuParticlesCollision3D`](GpuParticlesCollision3D.md) nodes.
- `HideOnContact = 2`：Hide particles instantly when colliding with a [`GpuParticlesCollision3D`](GpuParticlesCollision3D.md) node.
- `Max = 3`：Represents the size of the `ParticleProcessMaterial.CollisionModeEnum` enum.

## EmissionShapeEnum

命名空间 `Godot`；enum

### 枚举值

- `Point = 0`：All particles will be emitted from a single point.
- `Sphere = 1`：Particles will be emitted in the volume of a sphere.
- `SphereSurface = 2`：Particles will be emitted on the surface of a sphere.
- `Box = 3`：Particles will be emitted in the volume of a box.
- `Points = 4`：Particles will be emitted at a position determined by sampling a random point on the [`ParticleProcessMaterial.EmissionPointTexture`](ParticleProcessMaterial.md).
- `DirectedPoints = 5`：Particles will be emitted at a position determined by sampling a random point on the [`ParticleProcessMaterial.EmissionPointTexture`](ParticleProcessMaterial.md).
- `Ring = 6`：Particles will be emitted in a ring or cylinder.
- `Max = 7`：Represents the size of the `ParticleProcessMaterial.EmissionShapeEnum` enum.

## Parameter

命名空间 `Godot`；enum

### 枚举值

- `InitialLinearVelocity = 0`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `AngularVelocity = 1`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `OrbitVelocity = 2`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `LinearAccel = 3`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `RadialAccel = 4`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `TangentialAccel = 5`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `Damping = 6`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `Angle = 7`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `Scale = 8`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `HueVariation = 9`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `AnimSpeed = 10`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `AnimOffset = 11`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `RadialVelocity = 15`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `DirectionalVelocity = 16`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `ScaleOverVelocity = 17`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md), [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md), and [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.m…
- `Max = 18`：Represents the size of the `ParticleProcessMaterial.Parameter` enum.
- `TurbVelInfluence = 13`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md) and [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md) to set the turbulence minimum und maximum influence on each particles v…
- `TurbInitDisplacement = 14`：Use with [`ParticleProcessMaterial.SetParamMin`](ParticleProcessMaterial.md) and [`ParticleProcessMaterial.SetParamMax`](ParticleProcessMaterial.md) to set the turbulence minimum and maximum displacement of the particles…
- `TurbInfluenceOverLife = 12`：Use with [`ParticleProcessMaterial.SetParamTexture`](ParticleProcessMaterial.md) to set the turbulence influence over the particles life time.

## ParticleFlags

命名空间 `Godot`；enum

### 枚举值

- `AlignYToVelocity = 0`：Use with [`ParticleProcessMaterial.SetParticleFlag`](ParticleProcessMaterial.md) to set [`ParticleProcessMaterial.ParticleFlagAlignY`](ParticleProcessMaterial.md).
- `RotateY = 1`：Use with [`ParticleProcessMaterial.SetParticleFlag`](ParticleProcessMaterial.md) to set [`ParticleProcessMaterial.ParticleFlagRotateY`](ParticleProcessMaterial.md).
- `DisableZ = 2`：Use with [`ParticleProcessMaterial.SetParticleFlag`](ParticleProcessMaterial.md) to set [`ParticleProcessMaterial.ParticleFlagDisableZ`](ParticleProcessMaterial.md).
- `DampingAsFriction = 3`
- `InheritEmitterScale = 4`
- `Max = 5`：Represents the size of the `ParticleProcessMaterial.ParticleFlags` enum.

## SubEmitterModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：The subemitter is disabled.
- `Constant = 1`：The submitter is emitted on the constant interval defined by [`ParticleProcessMaterial.SubEmitterFrequency`](ParticleProcessMaterial.md).
- `AtEnd = 2`：The subemitter is emitted at the end of the particle's lifetime.
- `AtCollision = 3`：The subemitter is emitted when the particle collides.
- `AtStart = 4`：The subemitter is emitted when the particle spawns.
- `Max = 5`：Represents the size of the `ParticleProcessMaterial.SubEmitterModeEnum` enum.
