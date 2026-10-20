<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsServer2DExtension

命名空间 `Godot`；class；继承 [`PhysicsServer2DInstance`](PhysicsServer2DInstance.md) → [`GodotObject`](GodotObject.md)

This class extends [`PhysicsServer2D`](PhysicsServer2D.md) by providing additional virtual methods that can be overridden. When these methods are overridden, they will be called instead of the internal methods of the physics server.

Intended for use with GDExtension to create custom implementations of [`PhysicsServer2D`](PhysicsServer2D.md).

## 构造函数

### PhysicsServer2DExtension

```csharp
public PhysicsServer2DExtension()
```

## 方法

### BodyTestMotionIsExcludingBody

```csharp
public bool BodyTestMotionIsExcludingBody(Rid body)
```

引擎名：`body_test_motion_is_excluding_body`

Returns `true` if the body with the given [`Rid`](Rid.md) is being excluded from `_body_test_motion`. See also [`GodotObject.GetInstanceId`](GodotObject.md).

### BodyTestMotionIsExcludingObject

```csharp
public bool BodyTestMotionIsExcludingObject(ulong object)
```

引擎名：`body_test_motion_is_excluding_object`

Returns `true` if the object with the given instance ID is being excluded from `_body_test_motion`. See also [`GodotObject.GetInstanceId`](GodotObject.md).

### _AreaAddShape

```csharp
public virtual void _AreaAddShape(Rid area, Rid shape, Transform2D transform, bool disabled)
```

引擎名：`_area_add_shape`

Overridable version of [`PhysicsServer2D.AreaAddShape`](PhysicsServer2D.md).

### _AreaAttachCanvasInstanceId

```csharp
public virtual void _AreaAttachCanvasInstanceId(Rid area, ulong id)
```

引擎名：`_area_attach_canvas_instance_id`

Overridable version of [`PhysicsServer2D.AreaAttachCanvasInstanceId`](PhysicsServer2D.md).

### _AreaAttachObjectInstanceId

```csharp
public virtual void _AreaAttachObjectInstanceId(Rid area, ulong id)
```

引擎名：`_area_attach_object_instance_id`

Overridable version of [`PhysicsServer2D.AreaAttachObjectInstanceId`](PhysicsServer2D.md).

### _AreaClearShapes

```csharp
public virtual void _AreaClearShapes(Rid area)
```

引擎名：`_area_clear_shapes`

Overridable version of [`PhysicsServer2D.AreaClearShapes`](PhysicsServer2D.md).

### _AreaCreate

```csharp
public virtual Rid _AreaCreate()
```

引擎名：`_area_create`

Overridable version of [`PhysicsServer2D.AreaCreate`](PhysicsServer2D.md).

### _AreaGetCanvasInstanceId

```csharp
public virtual ulong _AreaGetCanvasInstanceId(Rid area)
```

引擎名：`_area_get_canvas_instance_id`

Overridable version of [`PhysicsServer2D.AreaGetCanvasInstanceId`](PhysicsServer2D.md).

### _AreaGetCollisionLayer

```csharp
public virtual uint _AreaGetCollisionLayer(Rid area)
```

引擎名：`_area_get_collision_layer`

Overridable version of [`PhysicsServer2D.AreaGetCollisionLayer`](PhysicsServer2D.md).

### _AreaGetCollisionMask

```csharp
public virtual uint _AreaGetCollisionMask(Rid area)
```

引擎名：`_area_get_collision_mask`

Overridable version of [`PhysicsServer2D.AreaGetCollisionMask`](PhysicsServer2D.md).

### _AreaGetObjectInstanceId

```csharp
public virtual ulong _AreaGetObjectInstanceId(Rid area)
```

引擎名：`_area_get_object_instance_id`

Overridable version of [`PhysicsServer2D.AreaGetObjectInstanceId`](PhysicsServer2D.md).

### _AreaGetParam

```csharp
public virtual Variant _AreaGetParam(Rid area, PhysicsServer2D.AreaParameter param)
```

引擎名：`_area_get_param`

Overridable version of [`PhysicsServer2D.AreaGetParam`](PhysicsServer2D.md).

### _AreaGetShape

```csharp
public virtual Rid _AreaGetShape(Rid area, int shapeIdx)
```

引擎名：`_area_get_shape`

Overridable version of [`PhysicsServer2D.AreaGetShape`](PhysicsServer2D.md).

### _AreaGetShapeCount

```csharp
public virtual int _AreaGetShapeCount(Rid area)
```

引擎名：`_area_get_shape_count`

Overridable version of [`PhysicsServer2D.AreaGetShapeCount`](PhysicsServer2D.md).

### _AreaGetShapeTransform

```csharp
public virtual Transform2D _AreaGetShapeTransform(Rid area, int shapeIdx)
```

引擎名：`_area_get_shape_transform`

Overridable version of [`PhysicsServer2D.AreaGetShapeTransform`](PhysicsServer2D.md).

### _AreaGetSpace

```csharp
public virtual Rid _AreaGetSpace(Rid area)
```

引擎名：`_area_get_space`

Overridable version of [`PhysicsServer2D.AreaGetSpace`](PhysicsServer2D.md).

### _AreaGetTransform

```csharp
public virtual Transform2D _AreaGetTransform(Rid area)
```

引擎名：`_area_get_transform`

Overridable version of [`PhysicsServer2D.AreaGetTransform`](PhysicsServer2D.md).

### _AreaRemoveShape

```csharp
public virtual void _AreaRemoveShape(Rid area, int shapeIdx)
```

引擎名：`_area_remove_shape`

Overridable version of [`PhysicsServer2D.AreaRemoveShape`](PhysicsServer2D.md).

### _AreaSetAreaMonitorCallback

```csharp
public virtual void _AreaSetAreaMonitorCallback(Rid area, Callable callback)
```

引擎名：`_area_set_area_monitor_callback`

Overridable version of [`PhysicsServer2D.AreaSetAreaMonitorCallback`](PhysicsServer2D.md).

### _AreaSetCollisionLayer

```csharp
public virtual void _AreaSetCollisionLayer(Rid area, uint layer)
```

引擎名：`_area_set_collision_layer`

Overridable version of [`PhysicsServer2D.AreaSetCollisionLayer`](PhysicsServer2D.md).

### _AreaSetCollisionMask

```csharp
public virtual void _AreaSetCollisionMask(Rid area, uint mask)
```

引擎名：`_area_set_collision_mask`

Overridable version of [`PhysicsServer2D.AreaSetCollisionMask`](PhysicsServer2D.md).

### _AreaSetMonitorCallback

```csharp
public virtual void _AreaSetMonitorCallback(Rid area, Callable callback)
```

引擎名：`_area_set_monitor_callback`

Overridable version of [`PhysicsServer2D.AreaSetMonitorCallback`](PhysicsServer2D.md).

### _AreaSetMonitorable

```csharp
public virtual void _AreaSetMonitorable(Rid area, bool monitorable)
```

引擎名：`_area_set_monitorable`

Overridable version of [`PhysicsServer2D.AreaSetMonitorable`](PhysicsServer2D.md).

### _AreaSetParam

```csharp
public virtual void _AreaSetParam(Rid area, PhysicsServer2D.AreaParameter param, Variant value)
```

引擎名：`_area_set_param`

Overridable version of [`PhysicsServer2D.AreaSetParam`](PhysicsServer2D.md).

### _AreaSetPickable

```csharp
public virtual void _AreaSetPickable(Rid area, bool pickable)
```

引擎名：`_area_set_pickable`

If set to `true`, allows the area with the given [`Rid`](Rid.md) to detect mouse inputs when the mouse cursor is hovering on it.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `area_set_pickable` method. Corresponds to [`CollisionObject2D.InputPickable`](CollisionObject2D.md).

### _AreaSetShape

```csharp
public virtual void _AreaSetShape(Rid area, int shapeIdx, Rid shape)
```

引擎名：`_area_set_shape`

Overridable version of [`PhysicsServer2D.AreaSetShape`](PhysicsServer2D.md).

### _AreaSetShapeDisabled

```csharp
public virtual void _AreaSetShapeDisabled(Rid area, int shapeIdx, bool disabled)
```

引擎名：`_area_set_shape_disabled`

Overridable version of [`PhysicsServer2D.AreaSetShapeDisabled`](PhysicsServer2D.md).

### _AreaSetShapeTransform

```csharp
public virtual void _AreaSetShapeTransform(Rid area, int shapeIdx, Transform2D transform)
```

引擎名：`_area_set_shape_transform`

Overridable version of [`PhysicsServer2D.AreaSetShapeTransform`](PhysicsServer2D.md).

### _AreaSetSpace

```csharp
public virtual void _AreaSetSpace(Rid area, Rid space)
```

引擎名：`_area_set_space`

Overridable version of [`PhysicsServer2D.AreaSetSpace`](PhysicsServer2D.md).

### _AreaSetTransform

```csharp
public virtual void _AreaSetTransform(Rid area, Transform2D transform)
```

引擎名：`_area_set_transform`

Overridable version of [`PhysicsServer2D.AreaSetTransform`](PhysicsServer2D.md).

### _BodyAddCollisionException

```csharp
public virtual void _BodyAddCollisionException(Rid body, Rid exceptedBody)
```

引擎名：`_body_add_collision_exception`

Overridable version of [`PhysicsServer2D.BodyAddCollisionException`](PhysicsServer2D.md).

### _BodyAddConstantCentralForce

```csharp
public virtual void _BodyAddConstantCentralForce(Rid body, Vector2 force)
```

引擎名：`_body_add_constant_central_force`

Overridable version of [`PhysicsServer2D.BodyAddConstantCentralForce`](PhysicsServer2D.md).

### _BodyAddConstantForce

```csharp
public virtual void _BodyAddConstantForce(Rid body, Vector2 force, Vector2 position)
```

引擎名：`_body_add_constant_force`

Overridable version of [`PhysicsServer2D.BodyAddConstantForce`](PhysicsServer2D.md).

### _BodyAddConstantTorque

```csharp
public virtual void _BodyAddConstantTorque(Rid body, float torque)
```

引擎名：`_body_add_constant_torque`

Overridable version of [`PhysicsServer2D.BodyAddConstantTorque`](PhysicsServer2D.md).

### _BodyAddShape

```csharp
public virtual void _BodyAddShape(Rid body, Rid shape, Transform2D transform, bool disabled)
```

引擎名：`_body_add_shape`

Overridable version of [`PhysicsServer2D.BodyAddShape`](PhysicsServer2D.md).

### _BodyApplyCentralForce

```csharp
public virtual void _BodyApplyCentralForce(Rid body, Vector2 force)
```

引擎名：`_body_apply_central_force`

Overridable version of [`PhysicsServer2D.BodyApplyCentralForce`](PhysicsServer2D.md).

### _BodyApplyCentralImpulse

```csharp
public virtual void _BodyApplyCentralImpulse(Rid body, Vector2 impulse)
```

引擎名：`_body_apply_central_impulse`

Overridable version of [`PhysicsServer2D.BodyApplyCentralImpulse`](PhysicsServer2D.md).

### _BodyApplyForce

```csharp
public virtual void _BodyApplyForce(Rid body, Vector2 force, Vector2 position)
```

引擎名：`_body_apply_force`

Overridable version of [`PhysicsServer2D.BodyApplyForce`](PhysicsServer2D.md).

### _BodyApplyImpulse

```csharp
public virtual void _BodyApplyImpulse(Rid body, Vector2 impulse, Vector2 position)
```

引擎名：`_body_apply_impulse`

Overridable version of [`PhysicsServer2D.BodyApplyImpulse`](PhysicsServer2D.md).

### _BodyApplyTorque

```csharp
public virtual void _BodyApplyTorque(Rid body, float torque)
```

引擎名：`_body_apply_torque`

Overridable version of [`PhysicsServer2D.BodyApplyTorque`](PhysicsServer2D.md).

### _BodyApplyTorqueImpulse

```csharp
public virtual void _BodyApplyTorqueImpulse(Rid body, float impulse)
```

引擎名：`_body_apply_torque_impulse`

Overridable version of [`PhysicsServer2D.BodyApplyTorqueImpulse`](PhysicsServer2D.md).

### _BodyAttachCanvasInstanceId

```csharp
public virtual void _BodyAttachCanvasInstanceId(Rid body, ulong id)
```

引擎名：`_body_attach_canvas_instance_id`

Overridable version of [`PhysicsServer2D.BodyAttachCanvasInstanceId`](PhysicsServer2D.md).

### _BodyAttachObjectInstanceId

```csharp
public virtual void _BodyAttachObjectInstanceId(Rid body, ulong id)
```

引擎名：`_body_attach_object_instance_id`

Overridable version of [`PhysicsServer2D.BodyAttachObjectInstanceId`](PhysicsServer2D.md).

### _BodyClearShapes

```csharp
public virtual void _BodyClearShapes(Rid body)
```

引擎名：`_body_clear_shapes`

Overridable version of [`PhysicsServer2D.BodyClearShapes`](PhysicsServer2D.md).

### _BodyCreate

```csharp
public virtual Rid _BodyCreate()
```

引擎名：`_body_create`

Overridable version of [`PhysicsServer2D.BodyCreate`](PhysicsServer2D.md).

### _BodyGetCanvasInstanceId

```csharp
public virtual ulong _BodyGetCanvasInstanceId(Rid body)
```

引擎名：`_body_get_canvas_instance_id`

Overridable version of [`PhysicsServer2D.BodyGetCanvasInstanceId`](PhysicsServer2D.md).

### _BodyGetCollisionExceptions

```csharp
public virtual Array<Rid> _BodyGetCollisionExceptions(Rid body)
```

引擎名：`_body_get_collision_exceptions`

Returns the [`Rid`](Rid.md)s of all bodies added as collision exceptions for the given `body`. See also `_BodyAddCollisionException()` and `_BodyRemoveCollisionException()`.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `body_get_collision_exceptions` method. Corresponds to [`PhysicsBody2D.GetCollisionExceptions`](PhysicsBody2D.md).

### _BodyGetCollisionLayer

```csharp
public virtual uint _BodyGetCollisionLayer(Rid body)
```

引擎名：`_body_get_collision_layer`

Overridable version of [`PhysicsServer2D.BodyGetCollisionLayer`](PhysicsServer2D.md).

### _BodyGetCollisionMask

```csharp
public virtual uint _BodyGetCollisionMask(Rid body)
```

引擎名：`_body_get_collision_mask`

Overridable version of [`PhysicsServer2D.BodyGetCollisionMask`](PhysicsServer2D.md).

### _BodyGetCollisionPriority

```csharp
public virtual float _BodyGetCollisionPriority(Rid body)
```

引擎名：`_body_get_collision_priority`

Overridable version of [`PhysicsServer2D.BodyGetCollisionPriority`](PhysicsServer2D.md).

### _BodyGetConstantForce

```csharp
public virtual Vector2 _BodyGetConstantForce(Rid body)
```

引擎名：`_body_get_constant_force`

Overridable version of [`PhysicsServer2D.BodyGetConstantForce`](PhysicsServer2D.md).

### _BodyGetConstantTorque

```csharp
public virtual float _BodyGetConstantTorque(Rid body)
```

引擎名：`_body_get_constant_torque`

Overridable version of [`PhysicsServer2D.BodyGetConstantTorque`](PhysicsServer2D.md).

### _BodyGetContactsReportedDepthThreshold

```csharp
public virtual float _BodyGetContactsReportedDepthThreshold(Rid body)
```

引擎名：`_body_get_contacts_reported_depth_threshold`

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `body_get_contacts_reported_depth_threshold` method.

**Note:** This method is currently unused by Godot's default physics implementation.

### _BodyGetContinuousCollisionDetectionMode

```csharp
public virtual PhysicsServer2D.CcdMode _BodyGetContinuousCollisionDetectionMode(Rid body)
```

引擎名：`_body_get_continuous_collision_detection_mode`

Overridable version of [`PhysicsServer2D.BodyGetContinuousCollisionDetectionMode`](PhysicsServer2D.md).

### _BodyGetDirectState

```csharp
public virtual PhysicsDirectBodyState2D _BodyGetDirectState(Rid body)
```

引擎名：`_body_get_direct_state`

Overridable version of [`PhysicsServer2D.BodyGetDirectState`](PhysicsServer2D.md).

### _BodyGetMaxContactsReported

```csharp
public virtual int _BodyGetMaxContactsReported(Rid body)
```

引擎名：`_body_get_max_contacts_reported`

Overridable version of [`PhysicsServer2D.BodyGetMaxContactsReported`](PhysicsServer2D.md).

### _BodyGetMode

```csharp
public virtual PhysicsServer2D.BodyMode _BodyGetMode(Rid body)
```

引擎名：`_body_get_mode`

Overridable version of [`PhysicsServer2D.BodyGetMode`](PhysicsServer2D.md).

### _BodyGetObjectInstanceId

```csharp
public virtual ulong _BodyGetObjectInstanceId(Rid body)
```

引擎名：`_body_get_object_instance_id`

Overridable version of [`PhysicsServer2D.BodyGetObjectInstanceId`](PhysicsServer2D.md).

### _BodyGetParam

```csharp
public virtual Variant _BodyGetParam(Rid body, PhysicsServer2D.BodyParameter param)
```

引擎名：`_body_get_param`

Overridable version of [`PhysicsServer2D.BodyGetParam`](PhysicsServer2D.md).

### _BodyGetShape

```csharp
public virtual Rid _BodyGetShape(Rid body, int shapeIdx)
```

引擎名：`_body_get_shape`

Overridable version of [`PhysicsServer2D.BodyGetShape`](PhysicsServer2D.md).

### _BodyGetShapeCount

```csharp
public virtual int _BodyGetShapeCount(Rid body)
```

引擎名：`_body_get_shape_count`

Overridable version of [`PhysicsServer2D.BodyGetShapeCount`](PhysicsServer2D.md).

### _BodyGetShapeTransform

```csharp
public virtual Transform2D _BodyGetShapeTransform(Rid body, int shapeIdx)
```

引擎名：`_body_get_shape_transform`

Overridable version of [`PhysicsServer2D.BodyGetShapeTransform`](PhysicsServer2D.md).

### _BodyGetSpace

```csharp
public virtual Rid _BodyGetSpace(Rid body)
```

引擎名：`_body_get_space`

Overridable version of [`PhysicsServer2D.BodyGetSpace`](PhysicsServer2D.md).

### _BodyGetState

```csharp
public virtual Variant _BodyGetState(Rid body, PhysicsServer2D.BodyState state)
```

引擎名：`_body_get_state`

Overridable version of [`PhysicsServer2D.BodyGetState`](PhysicsServer2D.md).

### _BodyIsOmittingForceIntegration

```csharp
public virtual bool _BodyIsOmittingForceIntegration(Rid body)
```

引擎名：`_body_is_omitting_force_integration`

Overridable version of [`PhysicsServer2D.BodyIsOmittingForceIntegration`](PhysicsServer2D.md).

### _BodyRemoveCollisionException

```csharp
public virtual void _BodyRemoveCollisionException(Rid body, Rid exceptedBody)
```

引擎名：`_body_remove_collision_exception`

Overridable version of [`PhysicsServer2D.BodyRemoveCollisionException`](PhysicsServer2D.md).

### _BodyRemoveShape

```csharp
public virtual void _BodyRemoveShape(Rid body, int shapeIdx)
```

引擎名：`_body_remove_shape`

Overridable version of [`PhysicsServer2D.BodyRemoveShape`](PhysicsServer2D.md).

### _BodyResetMassProperties

```csharp
public virtual void _BodyResetMassProperties(Rid body)
```

引擎名：`_body_reset_mass_properties`

Overridable version of [`PhysicsServer2D.BodyResetMassProperties`](PhysicsServer2D.md).

### _BodySetAxisVelocity

```csharp
public virtual void _BodySetAxisVelocity(Rid body, Vector2 axisVelocity)
```

引擎名：`_body_set_axis_velocity`

Overridable version of [`PhysicsServer2D.BodySetAxisVelocity`](PhysicsServer2D.md).

### _BodySetCollisionLayer

```csharp
public virtual void _BodySetCollisionLayer(Rid body, uint layer)
```

引擎名：`_body_set_collision_layer`

Overridable version of [`PhysicsServer2D.BodySetCollisionLayer`](PhysicsServer2D.md).

### _BodySetCollisionMask

```csharp
public virtual void _BodySetCollisionMask(Rid body, uint mask)
```

引擎名：`_body_set_collision_mask`

Overridable version of [`PhysicsServer2D.BodySetCollisionMask`](PhysicsServer2D.md).

### _BodySetCollisionPriority

```csharp
public virtual void _BodySetCollisionPriority(Rid body, float priority)
```

引擎名：`_body_set_collision_priority`

Overridable version of [`PhysicsServer2D.BodySetCollisionPriority`](PhysicsServer2D.md).

### _BodySetConstantForce

```csharp
public virtual void _BodySetConstantForce(Rid body, Vector2 force)
```

引擎名：`_body_set_constant_force`

Overridable version of [`PhysicsServer2D.BodySetConstantForce`](PhysicsServer2D.md).

### _BodySetConstantTorque

```csharp
public virtual void _BodySetConstantTorque(Rid body, float torque)
```

引擎名：`_body_set_constant_torque`

Overridable version of [`PhysicsServer2D.BodySetConstantTorque`](PhysicsServer2D.md).

### _BodySetContactsReportedDepthThreshold

```csharp
public virtual void _BodySetContactsReportedDepthThreshold(Rid body, float threshold)
```

引擎名：`_body_set_contacts_reported_depth_threshold`

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `body_set_contacts_reported_depth_threshold` method.

**Note:** This method is currently unused by Godot's default physics implementation.

### _BodySetContinuousCollisionDetectionMode

```csharp
public virtual void _BodySetContinuousCollisionDetectionMode(Rid body, PhysicsServer2D.CcdMode mode)
```

引擎名：`_body_set_continuous_collision_detection_mode`

Overridable version of [`PhysicsServer2D.BodySetContinuousCollisionDetectionMode`](PhysicsServer2D.md).

### _BodySetForceIntegrationCallback

```csharp
public virtual void _BodySetForceIntegrationCallback(Rid body, Callable callable, Variant userdata)
```

引擎名：`_body_set_force_integration_callback`

Overridable version of [`PhysicsServer2D.BodySetForceIntegrationCallback`](PhysicsServer2D.md).

### _BodySetMaxContactsReported

```csharp
public virtual void _BodySetMaxContactsReported(Rid body, int amount)
```

引擎名：`_body_set_max_contacts_reported`

Overridable version of [`PhysicsServer2D.BodySetMaxContactsReported`](PhysicsServer2D.md).

### _BodySetMode

```csharp
public virtual void _BodySetMode(Rid body, PhysicsServer2D.BodyMode mode)
```

引擎名：`_body_set_mode`

Overridable version of [`PhysicsServer2D.BodySetMode`](PhysicsServer2D.md).

### _BodySetOmitForceIntegration

```csharp
public virtual void _BodySetOmitForceIntegration(Rid body, bool enable)
```

引擎名：`_body_set_omit_force_integration`

Overridable version of [`PhysicsServer2D.BodySetOmitForceIntegration`](PhysicsServer2D.md).

### _BodySetParam

```csharp
public virtual void _BodySetParam(Rid body, PhysicsServer2D.BodyParameter param, Variant value)
```

引擎名：`_body_set_param`

Overridable version of [`PhysicsServer2D.BodySetParam`](PhysicsServer2D.md).

### _BodySetPickable

```csharp
public virtual void _BodySetPickable(Rid body, bool pickable)
```

引擎名：`_body_set_pickable`

If set to `true`, allows the body with the given [`Rid`](Rid.md) to detect mouse inputs when the mouse cursor is hovering on it.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `body_set_pickable` method. Corresponds to [`CollisionObject2D.InputPickable`](CollisionObject2D.md).

### _BodySetShape

```csharp
public virtual void _BodySetShape(Rid body, int shapeIdx, Rid shape)
```

引擎名：`_body_set_shape`

Overridable version of [`PhysicsServer2D.BodySetShape`](PhysicsServer2D.md).

### _BodySetShapeAsOneWayCollision

```csharp
public virtual void _BodySetShapeAsOneWayCollision(Rid body, int shapeIdx, bool enable, float margin, Vector2 direction)
```

引擎名：`_body_set_shape_as_one_way_collision`

Overridable version of [`PhysicsServer2D.BodySetShapeAsOneWayCollision`](PhysicsServer2D.md).

### _BodySetShapeDisabled

```csharp
public virtual void _BodySetShapeDisabled(Rid body, int shapeIdx, bool disabled)
```

引擎名：`_body_set_shape_disabled`

Overridable version of [`PhysicsServer2D.BodySetShapeDisabled`](PhysicsServer2D.md).

### _BodySetShapeTransform

```csharp
public virtual void _BodySetShapeTransform(Rid body, int shapeIdx, Transform2D transform)
```

引擎名：`_body_set_shape_transform`

Overridable version of [`PhysicsServer2D.BodySetShapeTransform`](PhysicsServer2D.md).

### _BodySetSpace

```csharp
public virtual void _BodySetSpace(Rid body, Rid space)
```

引擎名：`_body_set_space`

Overridable version of [`PhysicsServer2D.BodySetSpace`](PhysicsServer2D.md).

### _BodySetState

```csharp
public virtual void _BodySetState(Rid body, PhysicsServer2D.BodyState state, Variant value)
```

引擎名：`_body_set_state`

Overridable version of [`PhysicsServer2D.BodySetState`](PhysicsServer2D.md).

### _BodySetStateSyncCallback

```csharp
public virtual void _BodySetStateSyncCallback(Rid body, Callable callable)
```

引擎名：`_body_set_state_sync_callback`

Assigns the `body` to call the given `callable` during the synchronization phase of the loop, before `_Step()` is called. See also `_Sync()`.

Overridable version of [`PhysicsServer2D.BodySetStateSyncCallback`](PhysicsServer2D.md).

### _CapsuleShapeCreate

```csharp
public virtual Rid _CapsuleShapeCreate()
```

引擎名：`_capsule_shape_create`

Overridable version of [`PhysicsServer2D.CapsuleShapeCreate`](PhysicsServer2D.md).

### _CircleShapeCreate

```csharp
public virtual Rid _CircleShapeCreate()
```

引擎名：`_circle_shape_create`

Overridable version of [`PhysicsServer2D.CircleShapeCreate`](PhysicsServer2D.md).

### _ConcavePolygonShapeCreate

```csharp
public virtual Rid _ConcavePolygonShapeCreate()
```

引擎名：`_concave_polygon_shape_create`

Overridable version of [`PhysicsServer2D.ConcavePolygonShapeCreate`](PhysicsServer2D.md).

### _ConvexPolygonShapeCreate

```csharp
public virtual Rid _ConvexPolygonShapeCreate()
```

引擎名：`_convex_polygon_shape_create`

Overridable version of [`PhysicsServer2D.ConvexPolygonShapeCreate`](PhysicsServer2D.md).

### _DampedSpringJointGetParam

```csharp
public virtual float _DampedSpringJointGetParam(Rid joint, PhysicsServer2D.DampedSpringParam param)
```

引擎名：`_damped_spring_joint_get_param`

Overridable version of [`PhysicsServer2D.DampedSpringJointGetParam`](PhysicsServer2D.md).

### _DampedSpringJointSetParam

```csharp
public virtual void _DampedSpringJointSetParam(Rid joint, PhysicsServer2D.DampedSpringParam param, float value)
```

引擎名：`_damped_spring_joint_set_param`

Overridable version of [`PhysicsServer2D.DampedSpringJointSetParam`](PhysicsServer2D.md).

### _EndSync

```csharp
public virtual void _EndSync()
```

引擎名：`_end_sync`

Called to indicate that the physics server has stopped synchronizing. It is in the loop's iteration/physics phase, and can access physics objects even if running on a separate thread. See also `_Sync()`.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `end_sync` method.

### _Finish

```csharp
public virtual void _Finish()
```

引擎名：`_finish`

Called when the main loop finalizes to shut down the physics server. See also [`MainLoop._Finalize()`](MainLoop.md) and `PhysicsServer2DExtension`.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `finish` method.

### _FlushQueries

```csharp
public virtual void _FlushQueries()
```

引擎名：`_flush_queries`

Called every physics step before `_Step()` to process all remaining queries.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `flush_queries` method.

### _FreeRid

```csharp
public virtual void _FreeRid(Rid rid)
```

引擎名：`_free_rid`

Overridable version of [`PhysicsServer2D.FreeRid`](PhysicsServer2D.md).

### _GetProcessInfo

```csharp
public virtual int _GetProcessInfo(PhysicsServer2D.ProcessInfo processInfo)
```

引擎名：`_get_process_info`

Overridable version of [`PhysicsServer2D.GetProcessInfo`](PhysicsServer2D.md).

### _Init

```csharp
public virtual void _Init()
```

引擎名：`_init`

Called when the main loop is initialized and creates a new instance of this physics server. See also [`MainLoop._Initialize()`](MainLoop.md) and `_Finish()`.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `init` method.

### _IsFlushingQueries

```csharp
public virtual bool _IsFlushingQueries()
```

引擎名：`_is_flushing_queries`

Overridable method that should return `true` when the physics server is processing queries. See also `_FlushQueries()`.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `is_flushing_queries` method.

### _JointClear

```csharp
public virtual void _JointClear(Rid joint)
```

引擎名：`_joint_clear`

Overridable version of [`PhysicsServer2D.JointClear`](PhysicsServer2D.md).

### _JointCreate

```csharp
public virtual Rid _JointCreate()
```

引擎名：`_joint_create`

Overridable version of [`PhysicsServer2D.JointCreate`](PhysicsServer2D.md).

### _JointDisableCollisionsBetweenBodies

```csharp
public virtual void _JointDisableCollisionsBetweenBodies(Rid joint, bool disable)
```

引擎名：`_joint_disable_collisions_between_bodies`

Overridable version of [`PhysicsServer2D.JointDisableCollisionsBetweenBodies`](PhysicsServer2D.md).

### _JointGetParam

```csharp
public virtual float _JointGetParam(Rid joint, PhysicsServer2D.JointParam param)
```

引擎名：`_joint_get_param`

Overridable version of [`PhysicsServer2D.JointGetParam`](PhysicsServer2D.md).

### _JointGetType

```csharp
public virtual PhysicsServer2D.JointType _JointGetType(Rid joint)
```

引擎名：`_joint_get_type`

Overridable version of [`PhysicsServer2D.JointGetType`](PhysicsServer2D.md).

### _JointIsDisabledCollisionsBetweenBodies

```csharp
public virtual bool _JointIsDisabledCollisionsBetweenBodies(Rid joint)
```

引擎名：`_joint_is_disabled_collisions_between_bodies`

Overridable version of [`PhysicsServer2D.JointIsDisabledCollisionsBetweenBodies`](PhysicsServer2D.md).

### _JointMakeDampedSpring

```csharp
public virtual void _JointMakeDampedSpring(Rid joint, Vector2 anchorA, Vector2 anchorB, Rid bodyA, Rid bodyB)
```

引擎名：`_joint_make_damped_spring`

Overridable version of [`PhysicsServer2D.JointMakeDampedSpring`](PhysicsServer2D.md).

### _JointMakeGroove

```csharp
public virtual void _JointMakeGroove(Rid joint, Vector2 aGroove1, Vector2 aGroove2, Vector2 bAnchor, Rid bodyA, Rid bodyB)
```

引擎名：`_joint_make_groove`

Overridable version of [`PhysicsServer2D.JointMakeGroove`](PhysicsServer2D.md).

### _JointMakePin

```csharp
public virtual void _JointMakePin(Rid joint, Vector2 anchor, Rid bodyA, Rid bodyB)
```

引擎名：`_joint_make_pin`

Overridable version of [`PhysicsServer2D.JointMakePin`](PhysicsServer2D.md).

### _JointSetParam

```csharp
public virtual void _JointSetParam(Rid joint, PhysicsServer2D.JointParam param, float value)
```

引擎名：`_joint_set_param`

Overridable version of [`PhysicsServer2D.JointSetParam`](PhysicsServer2D.md).

### _PinJointGetFlag

```csharp
public virtual bool _PinJointGetFlag(Rid joint, PhysicsServer2D.PinJointFlag flag)
```

引擎名：`_pin_joint_get_flag`

Overridable version of [`PhysicsServer2D.PinJointGetFlag`](PhysicsServer2D.md).

### _PinJointGetParam

```csharp
public virtual float _PinJointGetParam(Rid joint, PhysicsServer2D.PinJointParam param)
```

引擎名：`_pin_joint_get_param`

Overridable version of [`PhysicsServer2D.PinJointGetParam`](PhysicsServer2D.md).

### _PinJointSetFlag

```csharp
public virtual void _PinJointSetFlag(Rid joint, PhysicsServer2D.PinJointFlag flag, bool enabled)
```

引擎名：`_pin_joint_set_flag`

Overridable version of [`PhysicsServer2D.PinJointSetFlag`](PhysicsServer2D.md).

### _PinJointSetParam

```csharp
public virtual void _PinJointSetParam(Rid joint, PhysicsServer2D.PinJointParam param, float value)
```

引擎名：`_pin_joint_set_param`

Overridable version of [`PhysicsServer2D.PinJointSetParam`](PhysicsServer2D.md).

### _RectangleShapeCreate

```csharp
public virtual Rid _RectangleShapeCreate()
```

引擎名：`_rectangle_shape_create`

Overridable version of [`PhysicsServer2D.RectangleShapeCreate`](PhysicsServer2D.md).

### _SegmentShapeCreate

```csharp
public virtual Rid _SegmentShapeCreate()
```

引擎名：`_segment_shape_create`

Overridable version of [`PhysicsServer2D.SegmentShapeCreate`](PhysicsServer2D.md).

### _SeparationRayShapeCreate

```csharp
public virtual Rid _SeparationRayShapeCreate()
```

引擎名：`_separation_ray_shape_create`

Overridable version of [`PhysicsServer2D.SeparationRayShapeCreate`](PhysicsServer2D.md).

### _SetActive

```csharp
public virtual void _SetActive(bool active)
```

引擎名：`_set_active`

Overridable version of [`PhysicsServer2D.SetActive`](PhysicsServer2D.md).

### _ShapeGetCustomSolverBias

```csharp
public virtual float _ShapeGetCustomSolverBias(Rid shape)
```

引擎名：`_shape_get_custom_solver_bias`

Should return the custom solver bias of the given `shape`, which defines how much bodies are forced to separate on contact when this shape is involved.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `shape_get_custom_solver_bias` method. Corresponds to [`Shape2D.CustomSolverBias`](Shape2D.md).

### _ShapeGetData

```csharp
public virtual Variant _ShapeGetData(Rid shape)
```

引擎名：`_shape_get_data`

Overridable version of [`PhysicsServer2D.ShapeGetData`](PhysicsServer2D.md).

### _ShapeGetType

```csharp
public virtual PhysicsServer2D.ShapeType _ShapeGetType(Rid shape)
```

引擎名：`_shape_get_type`

Overridable version of [`PhysicsServer2D.ShapeGetType`](PhysicsServer2D.md).

### _ShapeSetCustomSolverBias

```csharp
public virtual void _ShapeSetCustomSolverBias(Rid shape, float bias)
```

引擎名：`_shape_set_custom_solver_bias`

Should set the custom solver bias for the given `shape`. It defines how much bodies are forced to separate on contact.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `shape_get_custom_solver_bias` method. Corresponds to [`Shape2D.CustomSolverBias`](Shape2D.md).

### _ShapeSetData

```csharp
public virtual void _ShapeSetData(Rid shape, Variant data)
```

引擎名：`_shape_set_data`

Overridable version of [`PhysicsServer2D.ShapeSetData`](PhysicsServer2D.md).

### _SpaceCreate

```csharp
public virtual Rid _SpaceCreate()
```

引擎名：`_space_create`

Overridable version of [`PhysicsServer2D.SpaceCreate`](PhysicsServer2D.md).

### _SpaceGetContactCount

```csharp
public virtual int _SpaceGetContactCount(Rid space)
```

引擎名：`_space_get_contact_count`

Should return how many contacts have occurred during the last physics step in the given `space`. See also `_SpaceGetContacts()` and `_SpaceSetDebugContacts()`.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `space_get_contact_count` method.

### _SpaceGetContacts

```csharp
public virtual Vector2[] _SpaceGetContacts(Rid space)
```

引擎名：`_space_get_contacts`

Should return the positions of all contacts that have occurred during the last physics step in the given `space`. See also `_SpaceGetContactCount()` and `_SpaceSetDebugContacts()`.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `space_get_contacts` method.

### _SpaceGetDirectState

```csharp
public virtual PhysicsDirectSpaceState2D _SpaceGetDirectState(Rid space)
```

引擎名：`_space_get_direct_state`

Overridable version of [`PhysicsServer2D.SpaceGetDirectState`](PhysicsServer2D.md).

### _SpaceGetParam

```csharp
public virtual float _SpaceGetParam(Rid space, PhysicsServer2D.SpaceParameter param)
```

引擎名：`_space_get_param`

Overridable version of [`PhysicsServer2D.SpaceGetParam`](PhysicsServer2D.md).

### _SpaceIsActive

```csharp
public virtual bool _SpaceIsActive(Rid space)
```

引擎名：`_space_is_active`

Overridable version of [`PhysicsServer2D.SpaceIsActive`](PhysicsServer2D.md).

### _SpaceSetActive

```csharp
public virtual void _SpaceSetActive(Rid space, bool active)
```

引擎名：`_space_set_active`

Overridable version of [`PhysicsServer2D.SpaceSetActive`](PhysicsServer2D.md).

### _SpaceSetDebugContacts

```csharp
public virtual void _SpaceSetDebugContacts(Rid space, int maxContacts)
```

引擎名：`_space_set_debug_contacts`

Used internally to allow the given `space` to store contact points, up to `maxContacts`. This is automatically set for the main [`World2D`](World2D.md)'s space when [`SceneTree.DebugCollisionsHint`](SceneTree.md) is `true`, or by checking "Visible Collision Shapes" in the editor. Only works in debug builds.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `space_set_debug_contacts` method.

### _SpaceSetParam

```csharp
public virtual void _SpaceSetParam(Rid space, PhysicsServer2D.SpaceParameter param, float value)
```

引擎名：`_space_set_param`

Overridable version of [`PhysicsServer2D.SpaceSetParam`](PhysicsServer2D.md).

### _Step

```csharp
public virtual void _Step(float step)
```

引擎名：`_step`

Called every physics step to process the physics simulation. `step` is the time elapsed since the last physics step, in seconds. It is usually the same as the value returned by [`Node.GetPhysicsProcessDeltaTime`](Node.md).

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `step` method.

### _Sync

```csharp
public virtual void _Sync()
```

引擎名：`_sync`

Called to indicate that the physics server is synchronizing and cannot access physics states if running on a separate thread. See also `_EndSync()`.

Overridable version of [`PhysicsServer2D`](PhysicsServer2D.md)'s internal `sync` method.

### _WorldBoundaryShapeCreate

```csharp
public virtual Rid _WorldBoundaryShapeCreate()
```

引擎名：`_world_boundary_shape_create`

Overridable version of [`PhysicsServer2D.WorldBoundaryShapeCreate`](PhysicsServer2D.md).
