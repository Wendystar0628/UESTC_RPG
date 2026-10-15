<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SoftBody3D

命名空间 `Godot`；class；继承 [`MeshInstance3D`](MeshInstance3D.md) → [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A deformable 3D physics mesh. Used to create elastic or deformable objects such as cloth, rubber, or other flexible materials.

Additionally, `SoftBody3D` is subject to wind forces defined in [`Area3D`](Area3D.md) (see [`Area3D.WindSourcePath`](Area3D.md), [`Area3D.WindForceMagnitude`](Area3D.md), and [`Area3D.WindAttenuationFactor`](Area3D.md)).

**Note:** It's recommended to use Jolt Physics when using `SoftBody3D` instead of the default GodotPhysics3D, as Jolt Physics' soft body implementation is faster and more reliable. You can switch the physics engine using the `ProjectSettings.physics/3d/physics_engine` project setting.

## 构造函数

### SoftBody3D

```csharp
public SoftBody3D()
```

## 属性

### CollisionLayer

```csharp
public uint CollisionLayer { get; set; }
```

引擎名：`collision_layer`

The physics layers this SoftBody3D **is in**. Collision objects can exist in one or more of 32 different layers. See also `CollisionMask`.

**Note:** Object A can detect a contact with object B only if object B is in any of the layers that object A scans. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

### CollisionMask

```csharp
public uint CollisionMask { get; set; }
```

引擎名：`collision_mask`

The physics layers this SoftBody3D **scans**. Collision objects can scan one or more of 32 different layers. See also `CollisionLayer`.

**Note:** Object A can detect a contact with object B only if object B is in any of the layers that object A scans. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

### DampingCoefficient

```csharp
public float DampingCoefficient { get; set; }
```

引擎名：`damping_coefficient`

The body's damping coefficient. Higher values will slow down the body more noticeably when forces are applied.

### DisableMode

```csharp
public SoftBody3D.DisableModeEnum DisableMode { get; set; }
```

引擎名：`disable_mode`

Defines the behavior in physics when [`Node.ProcessMode`](Node.md) is set to [`Node.ProcessModeEnum.Disabled`](Node.md).

### DragCoefficient

```csharp
public float DragCoefficient { get; set; }
```

引擎名：`drag_coefficient`

The body's drag coefficient. Higher values increase this body's air resistance.

**Note:** This value is currently unused by Godot's default physics implementation.

### LinearStiffness

```csharp
public float LinearStiffness { get; set; }
```

引擎名：`linear_stiffness`

Higher values will result in a stiffer body, while lower values will increase the body's ability to bend. The value can be between `0.0` and `1.0` (inclusive).

### ParentCollisionIgnore

```csharp
public NodePath ParentCollisionIgnore { get; set; }
```

引擎名：`parent_collision_ignore`

[`NodePath`](NodePath.md) to a [`CollisionObject3D`](CollisionObject3D.md) this SoftBody3D should avoid clipping.

### PressureCoefficient

```csharp
public float PressureCoefficient { get; set; }
```

引擎名：`pressure_coefficient`

The pressure coefficient of this soft body. Simulate pressure build-up from inside this body. Higher values increase the strength of this effect.

### RayPickable

```csharp
public bool RayPickable { get; set; }
```

引擎名：`ray_pickable`

If `true`, the `SoftBody3D` will respond to [`RayCast3D`](RayCast3D.md)s.

### ShrinkingFactor

```csharp
public float ShrinkingFactor { get; set; }
```

引擎名：`shrinking_factor`

Scales the rest lengths of `SoftBody3D`'s edge constraints. Positive values shrink the mesh, while negative values expand it. For example, a value of `0.1` shortens the edges of the mesh by 10%, while `-0.1` expands the edges by 10%.

**Note:** `ShrinkingFactor` is best used on surface meshes with pinned points.

### SimulationPrecision

```csharp
public int SimulationPrecision { get; set; }
```

引擎名：`simulation_precision`

Increasing this value will improve the resulting simulation, but can affect performance. Use with care.

### TotalMass

```csharp
public float TotalMass { get; set; }
```

引擎名：`total_mass`

The SoftBody3D's mass.

## 方法

### AddCollisionExceptionWith

```csharp
public void AddCollisionExceptionWith(Node body)
```

引擎名：`add_collision_exception_with`

Adds a body to the list of bodies that this body can't collide with.

### ApplyCentralForce

```csharp
public void ApplyCentralForce(Vector3 force)
```

引擎名：`apply_central_force`

Distributes and applies a force to all points. A force is time dependent and meant to be applied every physics update.

### ApplyCentralImpulse

```csharp
public void ApplyCentralImpulse(Vector3 impulse)
```

引擎名：`apply_central_impulse`

Distributes and applies an impulse to all points.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

### ApplyForce

```csharp
public void ApplyForce(int pointIndex, Vector3 force)
```

引擎名：`apply_force`

Applies a force to a point. A force is time dependent and meant to be applied every physics update.

### ApplyImpulse

```csharp
public void ApplyImpulse(int pointIndex, Vector3 impulse)
```

引擎名：`apply_impulse`

Applies an impulse to a point.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

### GetCollisionExceptions

```csharp
public Array<PhysicsBody3D> GetCollisionExceptions()
```

引擎名：`get_collision_exceptions`

Returns an array of nodes that were added as collision exceptions for this body.

### GetCollisionLayer

```csharp
public uint GetCollisionLayer()
```

引擎名：`get_collision_layer`

### GetCollisionLayerValue

```csharp
public bool GetCollisionLayerValue(int layerNumber)
```

引擎名：`get_collision_layer_value`

Returns whether or not the specified layer of the `CollisionLayer` is enabled, given a `layerNumber` between 1 and 32.

### GetCollisionMask

```csharp
public uint GetCollisionMask()
```

引擎名：`get_collision_mask`

### GetCollisionMaskValue

```csharp
public bool GetCollisionMaskValue(int layerNumber)
```

引擎名：`get_collision_mask_value`

Returns whether or not the specified layer of the `CollisionMask` is enabled, given a `layerNumber` between 1 and 32.

### GetDampingCoefficient

```csharp
public float GetDampingCoefficient()
```

引擎名：`get_damping_coefficient`

### GetDisableMode

```csharp
public SoftBody3D.DisableModeEnum GetDisableMode()
```

引擎名：`get_disable_mode`

### GetDragCoefficient

```csharp
public float GetDragCoefficient()
```

引擎名：`get_drag_coefficient`

### GetLinearStiffness

```csharp
public float GetLinearStiffness()
```

引擎名：`get_linear_stiffness`

### GetParentCollisionIgnore

```csharp
public NodePath GetParentCollisionIgnore()
```

引擎名：`get_parent_collision_ignore`

### GetPhysicsRid

```csharp
public Rid GetPhysicsRid()
```

引擎名：`get_physics_rid`

Returns the internal [`Rid`](Rid.md) used by the [`PhysicsServer3D`](PhysicsServer3D.md) for this body.

### GetPointTransform

```csharp
public Vector3 GetPointTransform(int pointIndex)
```

引擎名：`get_point_transform`

Returns local translation of a vertex in the surface array.

### GetPressureCoefficient

```csharp
public float GetPressureCoefficient()
```

引擎名：`get_pressure_coefficient`

### GetShrinkingFactor

```csharp
public float GetShrinkingFactor()
```

引擎名：`get_shrinking_factor`

### GetSimulationPrecision

```csharp
public int GetSimulationPrecision()
```

引擎名：`get_simulation_precision`

### GetTotalMass

```csharp
public float GetTotalMass()
```

引擎名：`get_total_mass`

### IsPointPinned

```csharp
public bool IsPointPinned(int pointIndex)
```

引擎名：`is_point_pinned`

Returns `true` if vertex is set to pinned.

### IsRayPickable

```csharp
public bool IsRayPickable()
```

引擎名：`is_ray_pickable`

### RemoveCollisionExceptionWith

```csharp
public void RemoveCollisionExceptionWith(Node body)
```

引擎名：`remove_collision_exception_with`

Removes a body from the list of bodies that this body can't collide with.

### SetCollisionLayer

```csharp
public void SetCollisionLayer(uint collisionLayer)
```

引擎名：`set_collision_layer`

### SetCollisionLayerValue

```csharp
public void SetCollisionLayerValue(int layerNumber, bool value)
```

引擎名：`set_collision_layer_value`

Based on `value`, enables or disables the specified layer in the `CollisionLayer`, given a `layerNumber` between 1 and 32.

### SetCollisionMask

```csharp
public void SetCollisionMask(uint collisionMask)
```

引擎名：`set_collision_mask`

### SetCollisionMaskValue

```csharp
public void SetCollisionMaskValue(int layerNumber, bool value)
```

引擎名：`set_collision_mask_value`

Based on `value`, enables or disables the specified layer in the `CollisionMask`, given a `layerNumber` between 1 and 32.

### SetDampingCoefficient

```csharp
public void SetDampingCoefficient(float dampingCoefficient)
```

引擎名：`set_damping_coefficient`

### SetDisableMode

```csharp
public void SetDisableMode(SoftBody3D.DisableModeEnum mode)
```

引擎名：`set_disable_mode`

### SetDragCoefficient

```csharp
public void SetDragCoefficient(float dragCoefficient)
```

引擎名：`set_drag_coefficient`

### SetLinearStiffness

```csharp
public void SetLinearStiffness(float linearStiffness)
```

引擎名：`set_linear_stiffness`

### SetParentCollisionIgnore

```csharp
public void SetParentCollisionIgnore(NodePath parentCollisionIgnore)
```

引擎名：`set_parent_collision_ignore`

### SetPointPinned

```csharp
public void SetPointPinned(int pointIndex, bool pinned, NodePath attachmentPath = null, int insertAt = -1)
```

引擎名：`set_point_pinned`

Sets the pinned state of a surface vertex. When set to `true`, the optional `attachmentPath` can define a [`Node3D`](Node3D.md) the pinned vertex will be attached to.

### SetPointPinned

```csharp
public void SetPointPinned(int pointIndex, bool pinned, NodePath attachmentPath)
```

引擎名：`set_point_pinned`

Sets the pinned state of a surface vertex. When set to `true`, the optional `attachmentPath` can define a [`Node3D`](Node3D.md) the pinned vertex will be attached to.

### SetPressureCoefficient

```csharp
public void SetPressureCoefficient(float pressureCoefficient)
```

引擎名：`set_pressure_coefficient`

### SetRayPickable

```csharp
public void SetRayPickable(bool rayPickable)
```

引擎名：`set_ray_pickable`

### SetShrinkingFactor

```csharp
public void SetShrinkingFactor(float shrinkingFactor)
```

引擎名：`set_shrinking_factor`

### SetSimulationPrecision

```csharp
public void SetSimulationPrecision(int simulationPrecision)
```

引擎名：`set_simulation_precision`

### SetTotalMass

```csharp
public void SetTotalMass(float mass)
```

引擎名：`set_total_mass`

## DisableModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Remove = 0`：When [`Node.ProcessMode`](Node.md) is set to [`Node.ProcessModeEnum.Disabled`](Node.md), remove from the physics simulation to stop all physics interactions with this [`SoftBody3D`](SoftBody3D.md).
- `KeepActive = 1`：When [`Node.ProcessMode`](Node.md) is set to [`Node.ProcessModeEnum.Disabled`](Node.md), do not affect the physics simulation.
